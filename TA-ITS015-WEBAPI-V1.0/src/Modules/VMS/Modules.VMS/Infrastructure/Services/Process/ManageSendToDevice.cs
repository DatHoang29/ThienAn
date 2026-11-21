using Furion;
using Modules.VMS.Controllers.Process.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Modules.VMS.Infrastructure.Services.Process
{
	/// <summary>
	/// Gửi nội dung ra thiết bị VMS và thông báo kết quả qua 2 Event (OnSuccess / OnError).
	///
	/// THAY ĐỔI 22/09/2026 (ChuongLP): KHÔNG còn nạp SDK native trong tiến trình WebAPI.
	/// Thay vào đó phát gói tin lên kênh NATS ta.its.control.vms cho dịch vụ ITS.VMS.Worker thực thi,
	/// chờ gói result rồi bắn Event tương ứng từng thiết bị. Nhờ vậy SDK native (czVMSII/czJetFileII/Snmp)
	/// có crash cũng chỉ sập Worker, WebAPI vẫn sống.
	///
	/// Phần xử lý sau khi có kết quả (cập nhật VmsCurrent, ghi TmsSignalLog, TmsIncidentControl, gửi socket)
	/// GIỮ NGUYÊN ở VmsProcessService — lớp này chỉ lo việc gửi lệnh và báo lại thành công/thất bại.
	///
	/// Author: TuanHA (bản gốc) · ChuongLP (chuyển sang NATS)
	/// </summary>
	public class ManageSendToDevice : IDisposable
	{
		#region event

		/// <summary>Bắn khi MỘT thiết bị gửi thành công.</summary>
		public event EventHandler<EventInfoInput> OnSuccessSendToDeviceEvent;

		/// <summary>Bắn khi MỘT thiết bị gửi thất bại (kể cả timeout / dịch vụ chưa bật).</summary>
		public event EventHandler<EventInfoInput> OnErrorSendToDeviceEvent;

		#endregion

		#region HandleSendToDivice

		/// <summary>
		/// Gửi dữ liệu ra MỘT thiết bị. Giữ lại để tương thích ngược cho các luồng cũ
		/// (timer / schedule / process) đang gọi lẻ từng thiết bị.
		/// Author: TuanHA
		/// Created date: 19/02/2025
		/// </summary>
		public async Task SendToDeviceAsync(DataSendToDevice command)
		{
			if (command == null) return;
			await SendToDeviceMultiAsync(new List<DataSendToDevice> { command });
		}

		/// <summary>
		/// Gửi dữ liệu ra NHIỀU thiết bị bằng MỘT gói tin NATS (có thể nhiều hãng khác nhau),
		/// dịch vụ xử lý song song rồi trả về kết quả chi tiết từng thiết bị.
		///
		/// Bảo đảm quan trọng: MỖI thiết bị trong danh sách LUÔN được bắn đúng 1 Event
		/// (OnSuccess hoặc OnError). Thiếu bảo đảm này thì DB không được cập nhật và FE treo trạng thái.
		///
		/// Author: ChuongLP
		/// Created date: 22/09/2026
		/// </summary>
		public async Task SendToDeviceMultiAsync(List<DataSendToDevice> commands)
		{
			// Bỏ các phần tử không có thông tin thiết bị (PrepareContentSendToDevice có thể trả null).
			var validCommands = (commands ?? new List<DataSendToDevice>())
				.Where(c => c?.EquipmentInfoInput != null && !string.IsNullOrEmpty(c.EquipmentInfoInput.EquipmentId))
				.ToList();

			if (validCommands.Count == 0) return;

			// Giả lập ngay tại BE: không phát NATS, coi như tất cả thành công.
			if (GetBoolConfig("DeviceIntegration:VmsLocal"))
			{
				foreach (var command in validCommands)
					OnSuccessSendToDevice(new EventInfoInput { EquipmentId = command.EquipmentInfoInput.EquipmentId });

				return;
			}

			var request = new VmsControlRequest
			{
				Type = VmsControlType.Request,
				RequestId = VmsControlBridge.NewRequestId(),
				Action = VmsControlAction.SendContent,
				Simulate = GetBoolConfig("DeviceIntegration:VmsSimulate"),
				RequestTime = DateTime.Now,
				Targets = validCommands.Select(BuildTarget).ToList(),
			};

			// Nhúng ảnh vào gói tin để dịch vụ KHÔNG phải tự đi tìm file (cho phép cài dịch vụ ở máy khác).
			// Best-effort: thất bại thì gói vẫn đi với Url như trước, luồng gửi thiết bị không bị chặn.
			TryAttachImage(request, validCommands);

			Debug.WriteLine($"[ManageSendToDevice] Phát lệnh · RequestId={request.RequestId} · {request.Targets.Count} thiết bị · ảnh nhúng: {(!string.IsNullOrEmpty(request.ImageBase64) ? "có" : "không")}");

			// Phát lệnh và chờ kết quả. Hàm này KHÔNG ném exception: timeout hoặc lỗi phát gói
			// đều trả về kết quả thất bại cho toàn bộ thiết bị.
			var result = await VmsControlBridge.SendAsync(request, VmsControlBridge.GetBatchTimeoutSeconds(request.Targets.Count));

			// Bắn Event theo từng thiết bị của REQUEST (không dựa vào danh sách trong result)
			// để thiết bị bị dịch vụ bỏ sót vẫn được ghi nhận thất bại.
			foreach (var command in validCommands)
			{
				var equipmentId = command.EquipmentInfoInput.EquipmentId;
				var eventInfo = new EventInfoInput { EquipmentId = equipmentId };

				var one = result?.Results?.FirstOrDefault(r => r.EquipmentId == equipmentId);
				if (one != null && one.Success)
					OnSuccessSendToDevice(eventInfo);
				else
					OnErrorSendToDevice(eventInfo);
			}
		}

		/// <summary>
		/// Đọc file ảnh từ WebRoot và nhúng base64 vào gói tin.
		///
		/// BEST-EFFORT có chủ đích: mọi trường hợp không nhúng được (nhiều ảnh khác nhau trong mẻ,
		/// file không tồn tại, ảnh quá lớn, lỗi đọc đĩa) đều lặng lẽ bỏ qua để gói tin đi bằng Url —
		/// tức là chạy đúng như trước khi có tính năng này. KHÔNG được ném exception ra ngoài,
		/// vì đây chỉ là bước tối ưu, không phải điều kiện để gửi được thiết bị.
		///
		/// Author: ChuongLP
		/// Created date: 22/09/2026
		/// </summary>
		private static void TryAttachImage(VmsControlRequest request, List<DataSendToDevice> commands)
		{
			try
			{
				// Cả mẻ phải dùng CHUNG một ảnh mới nhúng được ở cấp request.
				// Thực tế luôn đúng vì PrepareContentSendToDevice lấy từ ContentInfoInput.Url dùng chung,
				// nhưng vẫn kiểm tra để tránh gửi sai ảnh nếu sau này luồng gọi thay đổi.
				var urls = commands.Select(c => c.Url).Distinct().ToList();
				if (urls.Count != 1) return;

				var relativeUrl = urls[0];
				if (string.IsNullOrWhiteSpace(relativeUrl)) return;

				// Url do BE sinh ra là đường dẫn TƯƠNG ĐỐI theo WebRoot
				// (VmsImageGenService trả Path.Combine(subFolder, fileName)).
				// Dùng IsPathFullyQualified chứ không phải IsPathRooted: url dạng "\Upload\NoImage.bmp" bị Windows coi là
				// "rooted" (gốc ổ đĩa hiện tại) nên trước đây không bao giờ tìm thấy file và không nhúng được ảnh.
				var path = Path.IsPathFullyQualified(relativeUrl)
					? relativeUrl
					: Path.Combine(App.WebHostEnvironment.WebRootPath, relativeUrl.TrimStart('\\', '/'));

				if (!File.Exists(path)) return;

				var bytes = File.ReadAllBytes(path);

				// Chốt trần payload: base64 phình ~4/3 so với file gốc. Vượt ngưỡng thì KHÔNG nhúng,
				// để NATS không ngắt kết nối vì gói vượt max_payload (mặc định 1MB phía server).
				var maxKb = GetIntConfig("DeviceIntegration:VmsMaxImageKb", 700);
				if (bytes.Length * 4 / 3 > maxKb * 1024) return;

				request.ImageBase64 = Convert.ToBase64String(bytes);
				request.ImageFormat = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ManageSendToDevice] Không nhúng được ảnh, dùng url: {ex.Message}");
			}
		}

		/// <summary>Đóng gói thông tin kết nối thiết bị + nội dung cần gửi thành một target của gói tin.</summary>
		private static VmsControlTarget BuildTarget(DataSendToDevice command)
		{
			var equipmentInfo = command.EquipmentInfoInput;

			return new VmsControlTarget
			{
				EquipmentId = equipmentInfo.EquipmentId,
				Vendor = equipmentInfo.Vendor,
				Ip = equipmentInfo.Ip,
				// Port trong DB là string và thường để trống -> parse an toàn, để dịch vụ tự dùng 80 khi null.
				Port = int.TryParse(equipmentInfo.Port, out var port) && port > 0 ? port : (int?)null,
				Config = equipmentInfo.Config,
				Url = command.Url,
				Brightness = command.Brightness,
			};
		}

		#endregion

		#region OnSuccessSendToDevice / OnErrorSendToDevice

		/// <summary>
		/// Bắn Event gửi thành công cho 1 thiết bị.
		/// LƯU Ý: bản cũ dùng cờ AmountTriggerEvent để chỉ cho bắn success đúng 1 lần
		/// (workaround cho lỗi đăng ký event 2 lần). Cờ này đã được BỎ vì luồng gom nhóm
		/// cần bắn Event cho từng thiết bị; giữ lại sẽ làm mất kết quả các thiết bị thứ 2 trở đi.
		/// Việc đăng ký Event giờ do VmsProcessService thực hiện đúng 1 lần cho cả mẻ.
		/// </summary>
		public void OnSuccessSendToDevice(EventInfoInput e)
		{
			Debug.WriteLine($"Event - OnSuccessSendToDevice - EquipmentId: {e?.EquipmentId}");
			OnSuccessSendToDeviceEvent?.Invoke(this, e);
		}

		/// <summary>Bắn Event gửi thất bại cho 1 thiết bị.</summary>
		public void OnErrorSendToDevice(EventInfoInput e)
		{
			Debug.WriteLine($"Event - OnErrorSendToDevice - EquipmentId: {e?.EquipmentId}");
			OnErrorSendToDeviceEvent?.Invoke(this, e);
		}

		#endregion

		/// <summary>Đọc cờ cấu hình bool, thiếu khóa thì coi như false (không để lỗi cấu hình chặn luồng gửi).</summary>
		private static bool GetBoolConfig(string key)
		{
			try
			{
				return App.GetConfig<bool>(key);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>Đọc số nguyên cấu hình, thiếu khóa hoặc giá trị không hợp lệ thì dùng mặc định.</summary>
		private static int GetIntConfig(string key, int defaultValue)
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

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
}
