using Modules.DataTransporter.ITS.Infrastructure.Services;
using Modules.VMS.Controllers.Process.Dto;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Modules.VMS.Infrastructure.Services.Process
{
	/// <summary>
	/// Cầu nối request/result giữa BE và dịch vụ ITS.VMS.Worker qua kênh NATS ta.its.control.vms.
	///
	/// VÌ SAO CẦN LỚP STATIC NÀY:
	/// Callback nhận gói NATS (đăng ký bằng ItsDataTransporter.ConsumeData) chạy trên thread nền,
	/// KHÔNG nằm trong scope của request HTTP gốc. Instance ManageSendToDevice lúc đó đã Dispose,
	/// VmsProcessService (IScoped) cũng đã hết scope -> không thể bắn Event trực tiếp từ callback.
	/// Cách làm: đăng ký RequestId -> TaskCompletionSource, callback chỉ "đánh thức" task đang chờ;
	/// việc bắn Event và cập nhật DB/log/socket vẫn diễn ra trong luồng request gốc.
	///
	/// Author: ChuongLP
	/// Created date: 22/09/2026
	/// </summary>
	public static class VmsControlBridge
	{
		/// <summary>Các yêu cầu đang chờ kết quả, khóa là RequestId.</summary>
		private static readonly ConcurrentDictionary<string, TaskCompletionSource<VmsControlResult>> _pending = new();

		/// <summary>Timeout mặc định (giây) khi không cấu hình DeviceIntegration:VmsTimeoutSeconds.</summary>
		private const int DefaultTimeoutSeconds = 30;

		/// <summary>
		/// Số giây BE chờ dịch vụ phản hồi. Đọc từ cấu hình để hiện trường tinh chỉnh được
		/// mà không cần build lại (SendImage của Chainzone đi qua cả SDK và API nên có thể vượt 10s).
		/// </summary>
		public static int TimeoutSeconds
		{
			get
			{
				try
				{
					var value = App.GetConfig<int?>("DeviceIntegration:VmsTimeoutSeconds");
					return value.HasValue && value.Value > 0 ? value.Value : DefaultTimeoutSeconds;
				}
				catch
				{
					return DefaultTimeoutSeconds;
				}
			}
		}

		/// <summary>
		/// Timeout cho lệnh gửi NHIỀU thiết bị: dịch vụ xử lý theo từng đợt MaxParallelDevice, mỗi thiết bị tối đa
		/// DeviceTimeoutSeconds, nên tổng thời gian tăng theo số đợt. Dùng timeout đơn lẻ sẽ khiến BE báo lỗi cả mẻ
		/// trong khi dịch vụ vẫn đang gửi thành công. Hai khóa cấu hình phải khớp section Nats của dịch vụ
		/// (mặc định 8 và 25s); không thấp hơn TimeoutSeconds.
		/// </summary>
		public static int GetBatchTimeoutSeconds(int targetCount)
		{
			var parallel = ReadPositiveInt("DeviceIntegration:VmsMaxParallelDevice", 8);
			var perDevice = ReadPositiveInt("DeviceIntegration:VmsDeviceTimeoutSeconds", 25);
			var waves = (Math.Max(targetCount, 1) + parallel - 1) / parallel;
			return Math.Max(TimeoutSeconds, waves * perDevice + 5);
		}

		private static int ReadPositiveInt(string key, int defaultValue)
		{
			try
			{
				var value = App.GetConfig<int?>(key);
				return value.HasValue && value.Value > 0 ? value.Value : defaultValue;
			}
			catch
			{
				return defaultValue;
			}
		}

		/// <summary>Sinh RequestId mới (32 ký tự, không dấu gạch).</summary>
		public static string NewRequestId() => Guid.NewGuid().ToString("N");

		/// <summary>
		/// Phát gói yêu cầu lên NATS rồi CHỜ kết quả tương ứng RequestId.
		/// Hết thời gian chờ mà không có phản hồi (dịch vụ chưa bật, thiết bị đứt cáp...) thì
		/// trả về kết quả thất bại an toàn cho TẤT CẢ thiết bị — bảo đảm BE/FE không treo vĩnh viễn.
		/// author: ChuongLP
		/// Date: 22/09/2026
		/// </summary>
		/// <param name="request">Gói lệnh cần phát.</param>
		/// <param name="timeoutOverrideSeconds">
		/// Số giây chờ riêng cho lệnh này; bỏ trống hoặc &lt;= 0 thì dùng TimeoutSeconds.
		/// Dùng cho lệnh ĐỌC THEO LÔ (vd get-stat toàn bộ thiết bị): dịch vụ xử lý song song theo
		/// từng đợt MaxParallelDevice nên tổng thời gian có thể vượt xa timeout của một lệnh gửi lẻ.
		/// </param>
		public static async Task<VmsControlResult> SendAsync(VmsControlRequest request, int timeoutOverrideSeconds = 0)
		{
            // Nếu request null thì throw để luồng gọi biết và bán event OnError cho từng thiết bị.
            if (request == null) throw new ArgumentNullException(nameof(request)); 

            request.Type = VmsControlType.Request;      // Đảm bảo request.Type luôn đúng, tránh dịch vụ bỏ qua gói lạ.
            if (string.IsNullOrEmpty(request.RequestId)) request.RequestId = NewRequestId(); // Nếu chưa có RequestId thì sinh mới.

            var timeoutSeconds = timeoutOverrideSeconds > 0 ? timeoutOverrideSeconds : TimeoutSeconds;

            // Khai báo để luồng gọi chờ kết quả từ callback.
            var tcs = new TaskCompletionSource<VmsControlResult>(TaskCreationOptions.RunContinuationsAsynchronously); 
            _pending[request.RequestId] = tcs;  // Đăng ký chờ kết quả, callback sẽ TrySetResult khi nhận được gói result.

            try
			{
				// Không handler nào nhận gói (NATS mất kết nối, chưa cấu hình transporter) thì báo lỗi ngay
				// thay vì chờ hết timeout cho một gói chắc chắn không tới được dịch vụ.
				var published = ItsDataTransporter.BroadcastDataWithResult(ItsDataTransporterChannel.ControlVms, request);
				if (!published.Values.Any(ok => ok))
					return BuildFailResult(request, "Không phát được lệnh điều khiển VMS: kênh truyền dữ liệu chưa kết nối.");

				var finished = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds))); // Chờ kết quả hoặc timeout
                if (finished == tcs.Task) 
					return tcs.Task.Result;

				//Debug.WriteLine($"[VmsControlBridge] TIMEOUT {timeoutSeconds}s · RequestId={request.RequestId} · Action={request.Action}");
				return BuildTimeoutResult(request, timeoutSeconds);  // Quá thời gian chờ, trả về kết quả thất bại cho toàn bộ thiết bị
            }
			catch (Exception ex)
			{
				// Lỗi phát gói (NATS chưa kết nối...) cũng phải trả kết quả thất bại cho từng thiết bị,
				// nếu không luồng gọi sẽ không bắn Event nào và DB/FE đứng im.
				Debug.WriteLine($"[VmsControlBridge] Lỗi phát gói: {ex.Message}");
				return BuildFailResult(request, $"Không phát được lệnh điều khiển VMS: {ex.Message}");
			}
			finally
			{
				_pending.TryRemove(request.RequestId, out _);
			}
		}

		/// <summary>
		/// Ghi nhận kết quả nhận từ NATS. Gọi từ callback ConsumeData của kênh ControlVms.
		/// Gói lạ (RequestId không còn chờ, vd đã timeout) bị bỏ qua im lặng.
		/// </summary>
		public static void Complete(VmsControlResult result)
		{
			if (result == null || string.IsNullOrEmpty(result.RequestId)) return;

			if (_pending.TryGetValue(result.RequestId, out var tcs))
				tcs.TrySetResult(result);
		}

		/// <summary>Kết quả thất bại do quá thời gian chờ.</summary>
		private static VmsControlResult BuildTimeoutResult(VmsControlRequest request, int timeoutSeconds)
			=> BuildFailResult(request, $"Dịch vụ điều khiển VMS không phản hồi (Timeout {timeoutSeconds}s)");

        /// <summary>
        /// Dựng kết quả thất bại cho toàn bộ thiết bị trong yêu cầu.
        /// Author: ChuongLP
		/// Date: 22/09/2026
        /// </summary>
        private static VmsControlResult BuildFailResult(VmsControlRequest request, string message)
		{
            // Chuẩn bị kết quả thất bại cho TẤT CẢ thiết bị trong danh sách Targets, để luồng gọi có thể bán event OnError cho từng thiết bị.
            return new VmsControlResult
			{
				Type = VmsControlType.Result,
				RequestId = request.RequestId,
				Action = request.Action,
				Success = false,
				Message = message,
				FinishedAt = DateTime.Now,
				Results = (request.Targets ?? new List<VmsControlTarget>())
					.Select(t => new VmsTargetResult
					{
						EquipmentId = t.EquipmentId,
						Success = false,
						Message = message,
					})
					.ToList(),
			};
		}
	}
}
