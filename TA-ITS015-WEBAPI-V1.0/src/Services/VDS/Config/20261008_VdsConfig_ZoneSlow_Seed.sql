/*
    Author: KienLT
    Created date: 08/10/2026

    Mục đích: seed 6 tham số sinh trạng thái CHẬM của vùng vào VdsConfig nhóm 'vds_config', để người vận hành
    sửa trên web thay vì sửa appsettings của Services.Vds.

    Service đọc các khoá này ở mỗi lượt nạp cấu hình vận hành (khởi động, chu kỳ Catalog:ReloadSeconds, hoặc
    lệnh refresh qua NATS). Khoá không có trên DB ⇒ service dùng giá trị trong appsettings (Service:ZoneStatus).
    Khoá có nhưng sai định dạng ⇒ service giữ giá trị đang chạy và ghi cảnh báo.

    Định dạng giá trị:
      vds_zone_slow_speed_enabled     1/0, true/false, on/off
      vds_zone_slow_min_vehicles      số nguyên > 0
      vds_zone_slow_share             0.6 | 60 | 60%
      vds_zone_slow_clear_margin_kmh  số ≥ 0 (km/h)
      vds_zone_slow_window            thời lượng CÓ HẬU TỐ: 300s, 5m  (không hậu tố = PHÚT)
      vds_zone_slow_event_ttl         thời lượng CÓ HẬU TỐ: 60s, 1m   (không hậu tố = PHÚT)

    Giá trị seed = mặc định đang có trong appsettings ⇒ chạy script không đổi hành vi.
    Script luỹ đẳng: chỉ thêm khoá chưa có, không ghi đè giá trị người vận hành đã sửa.
*/

DECLARE @tenant NVARCHAR(128) = N'Default';
DECLARE @now DATETIME = GETDATE();

DECLARE @seed TABLE (Code NVARCHAR(128), Name NVARCHAR(512), Value NVARCHAR(512), OrderNo INT, Remark NVARCHAR(2048));
INSERT INTO @seed (Code, Name, Value, OrderNo, Remark) VALUES
 (N'vds_zone_slow_speed_enabled',    N'Vùng chậm: bật đánh giá theo tốc độ',          N'0',   300,
  N'1 = bật, 0 = tắt. Tắt thì vùng chỉ chậm khi nhận sự kiện Slow Moving.'),
 (N'vds_zone_slow_min_vehicles',     N'Vùng chậm: số xe tối thiểu trong cửa sổ',      N'5',   301,
  N'Ít xe hơn thì chưa đủ căn cứ kết luận vùng chậm.'),
 (N'vds_zone_slow_share',            N'Vùng chậm: tỉ lệ xe dưới tốc độ tối thiểu',    N'0.6', 302,
  N'0.6 hoặc 60%. Cần đủ tỉ lệ này số xe chạy dưới V_min của vùng.'),
 (N'vds_zone_slow_clear_margin_kmh', N'Vùng chậm: biên hết chậm (km/h)',               N'5',   303,
  N'Tốc độ TB phải ≥ V_min + giá trị này mới hết chậm.'),
 (N'vds_zone_slow_window',           N'Vùng chậm: cửa sổ đánh giá',                    N'5m',  304,
  N'Có hậu tố s/m/h. Cũng là cửa sổ tính tốc độ TB ghi vào TmsZoneStatus.'),
 (N'vds_zone_slow_event_ttl',        N'Vùng chậm: thời gian giữ sự kiện Slow Moving',  N'60s', 305,
  N'Có hậu tố s/m/h. Hết thời gian kể từ bản tin Slow Moving gần nhất thì vùng hết chậm.');

INSERT INTO [dbo].[VdsConfig]
    (ID, Code, Name, Value, GroupCode, IsSysConfig, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
SELECT CONVERT(NVARCHAR(128), NEWID()), s.Code, s.Name, s.Value, N'vds_config', 0, s.OrderNo, s.Remark, 1,
       @tenant, @now, N'VDS-SEED-20261008'
FROM @seed s
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[VdsConfig] c
    WHERE c.GroupCode = N'vds_config' AND c.Code = s.Code AND c.TenantId = @tenant AND c.IsDelete IS NULL
);
GO
