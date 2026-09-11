# Báo cáo khả thi kỹ thuật — Tích hợp Trạm Quan Trắc Thời Tiết (WOS) — Datalogger CR1000X

**Dự án**: Cao tốc Hữu Nghị – Chi Lăng, phân hệ ITS
**Ngày**: 10/09/2026 · **Phiên bản**: Rev. 2

Nhiệt độ, độ ẩm, lượng mưa, hướng gió — thu thập tự động về backend TA-ITS015 qua Modbus TCP.

---

## Kết luận

| Hạng mục | Kết luận |
|---|---|
| **Khả năng kỹ thuật** | ✅ **Khả thi** — Cảm biến đã nghiệm thu đủ 4 thông số; CR1000X đọc được qua Modbus TCP từ backend C#/.NET bằng thư viện đã xác minh. |
| **Bộ tài liệu kỹ thuật** | ✅ **Đầy đủ** — 6 nguồn PDF gốc, đã chuyển thể 20 file Markdown, phân Tier A/B/C, có kế hoạch triển khai 6 giai đoạn. |

---

## 1. Mục tiêu tích hợp

4 thông số khí tượng thu về theo chu kỳ, từ 3 cảm biến gắn trên cột trạm cao 10 m:

| Thông số | Đơn vị | Cảm biến |
|---|---|---|
| Nhiệt độ không khí | °C | HygroVUE5 (SDI-12) |
| Độ ẩm tương đối | %RH | HygroVUE5 (SDI-12) |
| Lượng mưa | mm | TB4 (thùng lật) |
| Hướng gió | 0–360° | 05103 (ratiometric) |

---

## 2. Bảng trạng thái tổng hợp

| Hạng mục | Trạng thái | Chốt / hành động |
|---|---|---|
| Cảm biến hiện trường (05103, TB4, HygroVUE5) | ✅ Đạt | Nghiệm thu vật tư 3/3 bộ mỗi loại, đúng model — không cần hành động. |
| Datalogger CR1000X | ✅ Đạt | Hỗ trợ sẵn Modbus TCP/RTU server, SDI-12, đếm xung — không cần hành động. |
| Giao thức backend ↔ CR1000X | ✅ Khả thi | Modbus TCP, CR1000X đóng vai trò server. Cần cấu hình `ModbusServer()` + byte order theo CRBasic từng trạm. |
| Thư viện Modbus client C#/.NET | ✅ Đã xác minh | FluentModbus 5.3.2, MIT, netstandard2.0/2.1, còn bảo trì (cập nhật 01/09/2026). |
| Mã nguồn backend TA-ITS015 | ⛔ Chưa có | Triển khai theo kế hoạch 6 giai đoạn đã lập. |
| Số trạm & lý trình (Hữu Nghị – Chi Lăng) | ⚠️ Chưa chốt | Tài liệu hiện có là số liệu tham chiếu từ dự án Trung Lương – Mỹ Thuận. |
| Gói ShareData/C2C 104 "Dữ liệu thời tiết" | ⚠️ Ngoài phạm vi | Đã có spec mapping; xây dựng publisher ở giai đoạn sau. |

---

## 3. Tài liệu kỹ thuật — nguồn PDF & bản dịch

Toàn bộ nằm ở `DocBusinessThienAn/WOS/`. Cột "PDF gốc" là file thật trong `_source/pdf/` (Tier C, chỉ mở khi cần đối chiếu); cột "Bản dịch" là file Markdown dùng để đọc/trả lời (Tier A/B).

| PDF gốc (`_source/pdf/`) | Nội dung | Bản dịch (`doc/`) |
|---|---|---|
| [`s_cr1000x.pdf`](../../_source/pdf/s_cr1000x.pdf) — 8 trang, Rev. 05/2026 | Thông số kỹ thuật đầy đủ: nguồn điện, kênh đo tương tự/xung, chức năng chân — **nguồn chính** | [`cr1000x-specifications.md`](../cr1000x-specifications.md) |
| [`CR1000x_datalogger_PW.pdf`](../../_source/pdf/CR1000x_datalogger_PW.pdf) — 7 trang, Rev. 04/2022 | Cùng datasheet, bản cũ hơn (nhà phân phối PowerWise Systems) — chỉ để đối chiếu revision | [`cr1000x-datalogger-pw-2022.md`](../cr1000x-datalogger-pw-2022.md) |
| [`cr1000x-getting-started-guide.pdf`](../../_source/pdf/cr1000x-getting-started-guide.pdf) — 19 trang | Khởi động nhanh: EZSetup, tạo code Short Cut, kết nối LoggerNet/PC400 | [`cr1000x-getting-started-guide.md`](../cr1000x-getting-started-guide.md) |
| [`cr1000x-product-manual.pdf`](../../_source/pdf/cr1000x-product-manual.pdf) — 334 trang | Cẩm nang đầy đủ, quan trọng nhất cho việc code: | [`00-catalog.md`](../cr1000x-product-manual/00-catalog.md) (mục lục 11 chương) |
| ↳ Chương đấu dây | Sơ đồ chân P1/P2, VX, C1–C8 cho 05103/TB4/HygroVUE5 | [`02-wiring-and-terminal-functions.md`](../cr1000x-product-manual/02-wiring-and-terminal-functions.md) |
| ↳ Chương đo lường | Lệnh `PulseCount()`, `SDI12Recorder()`, đo xung mưa/gió | [`07-measurements.md`](../cr1000x-product-manual/07-measurements.md) |
| ↳ Chương giao thức | `ModbusServer()`, bảng thanh ghi Holding Registers, function code | [`08-communications-protocols.md`](../cr1000x-product-manual/08-communications-protocols.md) |
| [`5. BVTKTC_WOS.pdf`](../../_source/pdf/5.%20BVTKTC_WOS.pdf) — 33 trang, bản vẽ scan | Đấu nối cảm biến thực tế, kết cấu cột, lý trình 3 trạm (dự án tham chiếu) | [`bvtktc-wos.md`](../bvtktc-wos.md) |
| [`C2.TAP III.Q2.1.3`](../../_source/pdf/C2.TAP%20III.Q2.1.3%20NTĐV%20CCTV,VDS,VMS,DTS,WOS,TMS,PBX.pdf) + [`Q2.2.3`](../../_source/pdf/C2.TAP%20III.Q2.2.3%20NTĐV%20CCTV,VDS,VMS,DTS,WOS,TMS,PBX.pdf) — 473 trang, scan | Hồ sơ nghiệm thu vật tư đầu vào (NTĐV) — biên bản "Đạt" 100% danh mục | [`ho-so-nghiem-thu-wos.md`](../ho-so-nghiem-thu-wos.md) |

Chi tiết vai trò từng file + Tier Table đầy đủ: [`WOS/README.md`](../../README.md#3-vai-trò-từng-cặp-tài-liệu-pdf-gốc--bản-markdown).

---

## 4. Thư viện C# — Modbus TCP client

**Gói NuGet đã xác minh**: [`FluentModbus`](https://www.nuget.org/packages/FluentModbus) — `5.3.2`, MIT license, target `netstandard2.0` / `netstandard2.1` (tương thích .NET Framework 4.6.1 → .NET 10). Repo GitHub `Apollo3zehn/FluentModbus`, còn bảo trì tích cực (commit gần nhất 19/05/2026, cập nhật 01/09/2026).

```bash
dotnet add package FluentModbus --version 5.3.2
```

**Vì sao chọn FluentModbus thay vì NModbus/EasyModbusTCP**: CR1000X trả số đo dạng số thực (`float`) 32-bit, chiếm 2 thanh ghi liên tiếp, **big-endian**. FluentModbus có sẵn API generic đọc thẳng ra `float` với endianness cấu hình được; NModbus chỉ trả `ushort[]` thô, phải tự ghép byte bằng `BitConverter` cho từng cặp thanh ghi.

```csharp
using FluentModbus;

var client = new ModbusTcpClient();
client.Connect(IPAddress.Parse(station.ModbusHost), station.ModbusPort, ModbusEndianness.BigEndian);

// FC03 – Read Holding Registers (async), mỗi float chiếm 2 thanh ghi 16-bit
Memory<float> values = await client.ReadHoldingRegistersAsync<float>(
    unitIdentifier: station.ModbusUnitId,   // theo cấu hình ModbusServer() trong CRBasic
    startingAddress: 0,                      // đã strip offset 30000/40000 của Campbell
    count: 6,                                // 6 kênh: temp, RH, rain, wspd, wdir, vbat
    cancellationToken);

var temperature = values.Span[0];
client.Disconnect();
```

**Lưu ý kỹ thuật khi implement**:
- `ModbusClient` (lớp cha của `ModbusTcpClient`) có sẵn cả bản đồng bộ `ReadHoldingRegisters<T>(...)` và bất đồng bộ `ReadHoldingRegistersAsync<T>(..., CancellationToken)` — worker polling nên dùng thẳng bản Async, không cần tự bọc `Task.Run(...)`.
- Function code CR1000X hỗ trợ: `01, 02, 03, 04, 05, 06, 15, 16` (theo `cr1000x-product-manual/08-communications-protocols.md`). Chỉ cần **FC03** cho việc đọc.
- Thứ tự byte (`ABCD`/`BADC`/`CDAB`/`DCBA`) là tham số `ModbusOption` trong `ModbusServer()` của CRBasic — **khác nhau theo từng trạm**, không hard-code `ModbusEndianness.BigEndian`, phải đọc cấu hình thực tế mỗi trạm.
- Cảm biến HygroVUE5 (nhiệt độ/độ ẩm) nối CR1000X qua **SDI-12**, không phải Modbus trực tiếp — CR1000X đọc nội bộ bằng `SDI12Recorder()` rồi nạp cùng vào mảng biến mà `ModbusServer()` publish. Từ phía backend C#, tất cả 4 thông số đều đến qua **cùng một kênh Modbus TCP**, không cần biết cảm biến gốc dùng giao thức gì.
- Địa chỉ thanh ghi Campbell công bố theo kiểu 1-based, offset 30000 (input) / 40000 (holding) — phải strip offset trước khi truyền vào `startingAddress` của FluentModbus (0-based).

---

## 5. Việc cần chốt tiếp theo

1. **Xác nhận số trạm & lý trình lắp đặt** cho tuyến Hữu Nghị – Chi Lăng (dữ liệu hiện có là tham chiếu Trung Lương – Mỹ Thuận).
2. **Lấy cấu hình `ModbusServer()` thực tế** (địa chỉ thanh ghi, byte order) từ chương trình CRBasic của từng trạm trước khi triển khai.
3. **Bắt đầu Phase 1** của kế hoạch tích hợp: entity trạm & mở rộng bảng dữ liệu thời tiết.

---

*Nguồn: `DocBusinessThienAn/WOS/` · Kế hoạch chi tiết: [`wos-cr1000x-integration-plan.md`](wos-cr1000x-integration-plan.md)*
