using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Shared.QuestHa.Helper;

/// <summary>
/// Author: KienLT
/// Description: Đọc số dòng của file Parquet từ metadata ở cuối file (FileMetaData, Thrift compact, trường 3 <c>num_rows</c>) mà không
/// cần thư viện ngoài và không cần node có <c>cairo.sql.copy.root</c>; kiểm luôn magic <c>PAR1</c> ở đầu và cuối (file cụt/hỏng ⇒ lỗi).
/// Kèm băm SHA-256 theo luồng.
/// Created date: 02/11/2026
/// </summary>
internal class QuestParquetHelper
{
    private static readonly byte[] Magic = "PAR1"u8.ToArray();

    /// <summary>Số dòng ghi trong metadata của file; file không phải Parquet hợp lệ ⇒ <see cref="InvalidDataException"/>.</summary>
    public static long ReadRowCount(string path)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length < 12) throw new InvalidDataException("File Parquet quá ngắn.");
        Span<byte> head = stackalloc byte[4];
        file.ReadExactly(head);
        if (!head.SequenceEqual(Magic)) throw new InvalidDataException("Thiếu magic PAR1 ở đầu file.");
        Span<byte> tail = stackalloc byte[8];
        file.Seek(-8, SeekOrigin.End);
        file.ReadExactly(tail);
        if (!tail[4..].SequenceEqual(Magic)) throw new InvalidDataException("Thiếu magic PAR1 ở cuối file (file cụt?).");
        var length = BinaryPrimitives.ReadInt32LittleEndian(tail[..4]);
        if (length <= 0 || length > file.Length - 12) throw new InvalidDataException("Độ dài metadata Parquet không hợp lệ.");
        var footer = new byte[length];
        file.Seek(-8 - length, SeekOrigin.End);
        file.ReadExactly(footer);
        return ReadRowCount(footer);
    }

    /// <summary>Đọc trường <c>num_rows</c> (id 3, i64) của FileMetaData đã tách khỏi file.</summary>
    public static long ReadRowCount(ReadOnlySpan<byte> footer)
    {
        var reader = new CompactReader(footer);
        short lastId = 0;
        while (true)
        {
            var (type, id) = reader.FieldHeader(ref lastId);
            if (type == 0) throw new InvalidDataException("Metadata Parquet thiếu num_rows.");
            if (id == 3 && type == 6) return reader.ZigZag64();
            reader.Skip(type);
        }
    }

    /// <summary>SHA-256 (hex thường) của file, đọc theo luồng.</summary>
    public static async Task<string> Sha256Async(string path, CancellationToken ct)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20, FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(file, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Bộ đọc Thrift compact tối thiểu: đọc header trường, varint/zigzag và bỏ qua mọi kiểu (struct/list/set/map lồng nhau).
    /// Created date: 02/11/2026
    /// </summary>
    private ref struct CompactReader
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _pos;
        private int _depth;

        /// <summary>Bộ đọc trên vùng byte metadata (FileMetaData), con trỏ ở đầu.</summary>
        public CompactReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _pos = 0;
            _depth = 0;
        }

        /// <summary>
        /// Header trường kế tiếp: (kiểu, id); id mã hoá delta so với <paramref name="lastId"/> (được cập nhật). Kiểu 0 = hết struct.
        /// Hết dữ liệu ⇒ <see cref="InvalidDataException"/>.
        /// </summary>
        public (int Type, short Id) FieldHeader(ref short lastId)
        {
            var b = Byte();
            var type = b & 0x0F;
            if (type == 0) return (0, 0);
            var delta = (b >> 4) & 0x0F;
            lastId = delta != 0 ? (short)(lastId + delta) : (short)ZigZag64();
            return (type, lastId);
        }

        /// <summary>Số nguyên có dấu mã hoá varint + zigzag (i16/i32/i64 của Thrift compact). Varint quá dài/cụt ⇒ <see cref="InvalidDataException"/>.</summary>
        public long ZigZag64()
        {
            var v = Varint();
            return (long)(v >> 1) ^ -(long)(v & 1);
        }

        /// <summary>
        /// Bỏ qua một giá trị kiểu <paramref name="type"/> (kể cả struct/list/set/map lồng nhau, sâu tối đa 64).
        /// Kiểu không hỗ trợ, lồng quá sâu hoặc vượt cuối dữ liệu ⇒ <see cref="InvalidDataException"/>.
        /// </summary>
        public void Skip(int type)
        {
            if (++_depth > 64) throw new InvalidDataException("Metadata Parquet lồng quá sâu.");
            switch (type)
            {
                case 1: case 2: break;                       // bool trong header trường
                case 3: _pos++; break;                       // i8
                case 4: case 5: case 6: Varint(); break;     // i16/i32/i64
                case 7: _pos += 8; break;                    // double
                case 8:
                {
                    // Đọc độ dài TRƯỚC rồi mới cộng: "_pos += Varint()" lấy _pos cũ trước khi Varint() tiến con trỏ.
                    var length = checked((int)Varint());
                    _pos += length;
                    break;
                }
                case 9: case 10: SkipList(); break;
                case 11: SkipMap(); break;
                case 12: SkipStruct(); break;
                case 13: _pos += 16; break;                  // uuid
                default: throw new InvalidDataException($"Kiểu Thrift không hỗ trợ: {type}.");
            }
            if (_pos > _data.Length) throw new InvalidDataException("Metadata Parquet bị cắt.");
            _depth--;
        }

        private void SkipStruct()
        {
            short last = 0;
            while (true)
            {
                var (type, _) = FieldHeader(ref last);
                if (type == 0) return;
                Skip(type);
            }
        }

        private void SkipList()
        {
            var b = Byte();
            var size = (b >> 4) & 0x0F;
            var element = b & 0x0F;
            var count = size == 15 ? checked((int)Varint()) : size;
            for (var i = 0; i < count; i++)
            {
                if (element is 1 or 2) _pos++; // bool trong list là một byte
                else Skip(element);
            }
        }

        private void SkipMap()
        {
            var count = checked((int)Varint());
            if (count == 0) return;
            var kinds = Byte();
            for (var i = 0; i < count; i++)
            {
                SkipElement((kinds >> 4) & 0x0F);
                SkipElement(kinds & 0x0F);
            }
        }

        private void SkipElement(int type)
        {
            if (type is 1 or 2) _pos++;
            else Skip(type);
        }

        private byte Byte()
        {
            if (_pos >= _data.Length) throw new InvalidDataException("Metadata Parquet bị cắt.");
            return _data[_pos++];
        }

        private ulong Varint()
        {
            ulong result = 0;
            for (var shift = 0; shift < 64; shift += 7)
            {
                var b = Byte();
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) return result;
            }
            throw new InvalidDataException("Varint Thrift quá dài.");
        }
    }
}
