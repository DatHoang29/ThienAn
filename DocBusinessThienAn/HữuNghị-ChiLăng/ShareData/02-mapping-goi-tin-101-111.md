# Mapping field dữ liệu gói chia sẻ 101–111

## Cách đọc tài liệu

- `Tên field trả về` là đề xuất tên field payload camelCase. Đây là tên để đội tích hợp chốt trong API/JSON, không phải cột CSDL sẵn có.
- `Bảng - cột` là nguồn lấy thực tế từ WebAPI. Một field ghi **Tính toán** phải có quy tắc xử lý trước khi trả ra.
- `— Chưa có` nghĩa là không tìm thấy cột phù hợp trong entity hiện tại; không thay bằng dữ liệu gần đúng.
- `ID`, `Code`, `CreateTime`, `UpdateTime` là cột dùng chung kế thừa từ `EntityTenant` khi được dùng làm khóa/thời điểm quản trị.

> Quy ước đã chốt cho gói sự kiện: `TmsIncident.InfluenceScope` được trả về là trường `direction`; không trả thêm trường `influenceScope`.

## 101. Thông tin chung / luồng giao thông (lấy all/DL được cập nhật)

Nguồn chính: `TmsZoneStatus` (trạng thái mới nhất), `TmsZone` (đoạn đường), `TmsTrafficStatistic` (tổng hợp).

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Mã đoạn đường | `zoneId` | `TmsZoneStatus.ZoneId` | `zone-01` | Join `TmsZoneStatus.ZoneId = TmsZone.ID`. |
| Tên đoạn đường | `zoneName` | `TmsZone.Name` | `Đoạn Km 5+000 – Km 8+000` | Có sẵn. |
| Lý trình đầu | `fromLocationKm` | `TmsZone.FromKmNumber` | `5` | Có sẵn. |
| Lý trình đầu (m) | `fromLocationMet` | `TmsZone.FromMetNumber` | `0` | Có sẵn. |
| Lý trình cuối | `toLocationKm` | `TmsZone.ToKmNumber` | `8` | Có sẵn. |
| Lý trình cuối (m) | `toLocationMet` | `TmsZone.ToMetNumber` | `0` | Có sẵn. |
| Hướng/làn | `laneId` | `TmsZone.LaneId` | `N-B` | Cần chốt bảng mã hướng/làn. |
| Tốc độ trung bình | `averageSpeed` | `TmsZoneStatus.AverageSpeed` | `62.5` | Lấy bản ghi `UpdateTime` mới nhất theo vùng. |
| Tình trạng giao thông | `trafficCondition` | `TmsZoneStatus.Condition` | `slow` | Cần chốt bảng mã condition. |
| Thời điểm dữ liệu | `dataTime` | `TmsZoneStatus.UpdateTime` | `2026-07-31T10:15:00+07:00` | Dùng để kiểm tra SLA 30 giây. |
| Tốc độ tối đa | `speedLimit` | `TmsZone.MaxSpeed` | `80` | Không phải tốc độ thực đo. |
| Số xe | `vehicleCount` | `TmsTrafficStatistic.TotalVehicleNumber` | `45` | Phải kèm khoảng `FromTime`–`ToTime`. |
| Tên/loại đường/chủ quản | `routeName`, `roadType`, `roadAuthority` | — Chưa có | — | `TollRoad.Name` chỉ là tuyến ETC, không đủ thay danh mục đường chung. |
| Áo đường, số làn, vai đường | `pavementType`, `laneCount`, `shoulderWidth` | — Chưa có | — | Cần danh mục đoạn đường riêng. |

## 102. Dữ liệu hình ảnh giao thông (CCTV) (lấy all /DL được cập nhật -> nats)

Nguồn chính: `CctvDevice`; vị trí hiện chỉ có thể nối logic tới `TmsEquipment` qua IP.

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Mã camera | `cameraCode` | `TmsEquipment.Code` | `FIX-01` | Join `CctvDevice.Ip = TmsEquipment.Ip`; dùng `Code` của thiết bị TMS làm mã camera trả về. |
| Tên camera | `cameraName` | `CctvDevice.Name` | `CCTV Km5+500` | Join camera với thiết bị qua IP. |
| Ảnh chụp | `snapshot` | `CctvDevice.Snapshot` | `base64` | Có sẵn. |
| Thời điểm ảnh chụp | `snapshotTime` | `CctvDevice.SnapshotTime` | `2026-07-31T10:15:02+07:00` | Có sẵn. |
| Trạng thái camera | `deviceState` | `CctvDevice.DeviceState` | `1` | Cần chốt enum. |
| Vị trí Km | `locationKm` | `TmsEquipment.KmNumber` | `5` | Tạm nối `CctvDevice.Ip = TmsEquipment.Ip`; cần bổ sung `CctvDevice.EquipmentId` để chính thức. |
| Vị trí m | `locationMet` | `TmsEquipment.MetNumber` | `500` | Cùng quy tắc nối IP nêu trên. |
| Hướng | `direction` | `TmsEquipment.DirectionId` | `1` | Cùng quy tắc nối IP; cần bảng mã. |
| Tốc độ/loại xe từ ảnh | `speed`, `vehicleType` | — Chưa có | — | Chỉ xuất nếu pipeline AI/CCTV ghi kết quả vào bảng riêng hoặc có quy tắc nguồn rõ ràng. |

## 103. Dữ liệu dò xe (VDS) (sẽ lấy mới nhất từ thời điểm trước đó >= key)

Nguồn chính: `TmsTrafficData` (mỗi xe), `TmsTrafficStatistic` (tổng hợp), `TmsZoneStatus` (tình trạng vùng).

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Mã bản ghi phát hiện | `detectionId` | `TmsTrafficData.ID` | `vds-000123` | Có sẵn. |
| Thời gian phát hiện | `detectTime` | `TmsTrafficData.DetectTime` | `2026-07-31T10:15:12+07:00` | Có sẵn. |
| Loại xe | `vehicleType` | `TmsTrafficData.Type` | `truck` | Cần bảng mã loại xe (theo chiều dài). |
| Biển số | `licensePlate` | `TmsTrafficData.LicensePlate` | `29A-123.45` | Chỉ chia sẻ khi được phê duyệt quy định dữ liệu cá nhân. |
| Tốc độ xe | `speed` | `TmsTrafficData.Speed` | `68.4` | Đơn vị cần chốt, thông thường km/h. |
| Làn | `lane` | `TmsTrafficData.Lane` | `lane-2` | Có sẵn. |
| Hướng | `direction` | `TmsTrafficData.Direction` | `N-B` | Cần bảng mã. |
| Vị trí nguồn | `locationRoute` | `TmsTrafficData.Location` | `main_route` | Là mã/chuỗi vị trí. |
| Thiết bị ghi nhận | `equipmentId` | `TmsTrafficData.EquipmentId` | `vds-01` | Dùng để nối vị trí vật lý. |
| Vị trí Km | `locationKm` | `TmsEquipment.KmNumber` | `5` | Join `TmsTrafficData.EquipmentId = TmsEquipment.ID`. |
| Vị trí m | `locationMet` | `TmsEquipment.MetNumber` | `500` | Cùng join thiết bị. |

## 107. Thông tin sự kiện giao thông (sẽ lấy mới nhất từ thời điểm trước đó >= key/ giá trị cũ sẽ update trạng thái)

Nguồn chính: `TmsIncident`; đây là gói cần dùng bảng mapping dưới đây.

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Mã sự cố | `incidentCode` | `TmsIncident.Code` | `INC-001` | Cột kế thừa `EntityTenant`. |
| Tên sự cố | `incidentName` | `TmsIncident.Name` | `Va chạm giao thông` | Có sẵn. |
| Loại sự cố (mã) | `eventTypeId` | `TmsIncident.EventTypeId` | `collision` | Join `TmsEventType.ID`. |
| Loại sự cố (tên) | `eventTypeName` | `TmsEventType.Name` | `Tai nạn giao thông` | Join `TmsIncident.EventTypeId = TmsEventType.ID`. |
| Thời gian xảy ra | `occurredTime` | `TmsIncident.StartDate` | `2026-07-31T10:05:00+07:00` | Không dùng `CreatedDate`. |
| Vị trí Km | `locationKm` | `TmsIncident.KmNumber` | `5` | Ví dụ theo yêu cầu. |
| Vị trí m | `locationMet` | `TmsIncident.MetNumber` | `500` | Ví dụ theo yêu cầu. |
| Tuyến/vị trí logic | `locationRoute` | `TmsIncident.Location` | `main_route` | Ví dụ theo yêu cầu; hiện là chuỗi/mã. |
| Hướng di chuyển | `direction` | `TmsIncident.InfluenceScope` | `2` | Mapping theo quy ước đã chốt. |
| Số người bị thương | `injuredCount` | `TmsIncident.InjuredNumber` | `2` | Có sẵn. |
| Số xe liên quan | `vehicleCount` | `TmsIncident.VehicleNumber` | `3` | Có sẵn. |
| Trạng thái sự cố | `incidentState` | `TmsIncident.State` | `processing` | Cần bảng mã trạng thái. |
| Mô tả | `description` | `TmsIncident.Description` | `Va chạm hai xe...` | Có sẵn. |
| Nguồn tiếp nhận | `source` | `TmsIncident.Source` | `cctv` | Cần bảng mã nguồn. |
| Cơ quan quản lý | `managementAgency` | — Chưa có | — | `CreatedBy`/`ApprovedBy` chỉ là tài khoản xử lý. |

## 108. Thông tin hiển thị trên biển báo điện tử (VMS)  (lấy all /DL được cập nhật -> nats)

Nguồn hiện trạng: `VmsCurrent`; nguồn lịch sử/lệnh: `TmsSignalLog`; nguồn lịch: `VmsProcess`.

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Mã thiết bị VMS | `equipmentCode` | `TmsEquipment.Code` | `VMS-01` | Join `VmsCurrent.EquipmentId = TmsEquipment.ID`. |
| Tên VMS | `vmsName` | `VmsCurrent.Name` | `VMS Km5+500` | Có sẵn. |
| Vị trí Km/m | `locationKm`, `locationMet` | `TmsEquipment.KmNumber`, `MetNumber` | `5`, `500` | Join bằng `EquipmentId`. |
| Hướng/làn | `direction`, `laneId` | `TmsEquipment.DirectionId`, `LaneId` | `1`, `N-B` | Cần bảng mã. |
| Nội dung hiển thị | `displayContent` | `VmsCurrent.RowData` | `{"Row1":"GIAM TOC","Row2":"CO SU CO"}` | Parse JSON trước khi trả. |
| Ảnh hiển thị | `displayImageUrl` | `VmsCurrent.Url` | `base64` | Có sẵn. |
| Kích thước biển | `displaySize` | `VmsCurrent.Size` | `384x64` | Có sẵn. |
| Mức ưu tiên | `priority` | `VmsCurrent.Priority` | `1` | Có sẵn. |
| Thời gian thực thi | `executedTime` | `VmsCurrent.ExecutedDate` | `2026-07-31T10:10:00+07:00` | Trạng thái hiện tại. |

## 109. Thông tin thu phí (ETC) (sẽ lấy mới nhất từ thời điểm trước đó >= key)

Nguồn chính: `TollTransactionIn` và `TollTransactionOut`; danh mục làn/trạm dùng để bổ sung mô tả.

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Mã giao dịch | `transactionId` | `TollTransactionIn/Out.TransactionId` | `TXN-10001` | Có sẵn. |
| Thời điểm qua làn vào | `entryTime` | `TollTransactionOut.TransactionDateTimeIn` | `2026-07-31T10:00:00+07:00` | Lấy cùng một giao dịch xe ra để ghép với thời điểm làn ra. |
| Thời điểm qua làn ra | `exitTime` | `TollTransactionOut.TransactionDateTime` | `2026-07-31T10:07:00+07:00` | Có sẵn. |
| Loại xe | `vehicleTypeId` | `TollTransactionIn/Out.VehicleTypeId` | `4` | Nối `TollMasterData` nếu trả tên. |
| Biển số/eTag | `licensePlate`, `tagId` | `PlateEdit`/`Plate`/`PlateLpr`, `TagId` | `29A-123.45`, `E200…` | Ưu tiên biển số sau hậu kiểm. |
| Mã làn | `laneId` | `TollTransactionIn/Out.LaneId` | `L02` | Có sẵn. |
| Tên làn | `laneName` | `TollLane.Name` | `Làn ETC 02` | Join `Transaction.LaneId = TollLane.LaneId`. |
| Mã/tên trạm | `stationId`, `stationName` | `Transaction.StationId`; `TollStation.Name` | `ST01`, `Trạm ABC` | Join theo `StationId`. |
| Giá thu | `tollPrice` | `TollTransactionOut.Price` | `35000.00` | Chỉ có ở giao dịch ra. |
| Lưu lượng theo làn | `vehicleCount` | Tính `COUNT(TransactionId)` theo `LaneId`/cửa sổ thời gian | `20` | Không có cột tổng hợp trực tiếp. |
| Độ dài hàng đợi | `queueLength` | — Chưa có | — | Không suy ra từ số giao dịch. |
| Độ trễ đồng bộ | `syncTime` | `TollTransactionIn/Out.SyncTime` | `2026-07-31T10:00:10+07:00` | Tính SLA từ `SyncTime - TransactionDateTime`. |

## 109. Thông tin thu phí (ETC) Summary (sẽ lấy mới nhất từ thời điểm trước đó >= key)

## Các gói dự kiến làm sau

### 104. Dữ liệu thời tiết (sẽ lấy mới nhất từ thời điểm trước đó >= key)

Nguồn chính: `TmsWeather`.

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Mã trạm/cảm biến | `weatherStationId` | `TmsWeather.RefId` | `ws-01` | Cần xác nhận `RefId` tham chiếu thiết bị nào. |
| Vị trí trạm | `locationDetail` | `TmsWeather.LocationDetail` | `Km 5+500` | Có sẵn. |
| Nhiệt độ không khí | `temperature` | `TmsWeather.Temperature` | `31.2` | Cần chốt đơn vị °C. |
| Độ ẩm | `humidity` | `TmsWeather.Hudmidity` | `78` | Tên cột hiện tại là `Hudmidity`. |
| Tốc độ/hướng gió | `windSpeed`, `windDirection` | `TmsWeather.WindSpeed`, `WindDirection` | `4.5`, `NE` | Có sẵn. |
| Lượng mưa | `rainfall`, `rainfallHour` | `TmsWeather.Rain`, `RainHour` | `3.2`, `12.5` | Cần chốt đơn vị. |
| Tầm nhìn | `visibility` | `TmsWeather.Foresight` | `800` | Cần chốt đơn vị mét. |
| Mô tả/mã thời tiết | `weatherDescription`, `weatherCode` | `TmsWeather.Description`, `ShortDescription` | `Mưa vừa`, `rain` | Chuẩn hóa enum nếu cần. |
| Thời điểm đo | `detectTime` | `TmsWeather.TimeDetect` | `2026-07-31T10:15:00+07:00` | Có sẵn. |
| Nhiệt độ mặt đường/khả dụng đường | `roadSurfaceTemperature`, `roadAvailability` | — Chưa có | — | Không map sang `TmsZoneStatus.Condition`. |

### 105. Dữ liệu định danh phương tiện (AVI/RFID) (All/ update)

Nguồn chính: giao dịch ETC `TollTransactionIn`/`TollTransactionOut`; đăng ký xe là nguồn bổ sung.

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Mã giao dịch | `transactionId` | `TollTransactionIn.TransactionId` / `TollTransactionOut.TransactionId` | `TXN-10001` | Có sẵn. |
| Mã eTag/RFID | `tagId` | `TollTransactionIn.TagId` / `TollTransactionOut.TagId` | `E2003412…` | Có sẵn. |
| Biển số | `licensePlate` | `PlateEdit`, sau đó `Plate`, sau đó `PlateLpr` | `29A-123.45` | Thứ tự ưu tiên đề xuất. |
| Loại xe | `vehicleTypeId` | `VehicleTypeId` | `4` | Nối `TollMasterData` nếu cần tên loại xe. |
| Thời điểm vào/ra | `entryTime`, `exitTime` | `TollTransactionOut.TransactionDateTimeIn`, `TransactionDateTime` | `10:00`, `10:07` | Có thể tính thời gian đi lại. |
| Làn/trạm | `laneId`, `stationId` | `TollTransactionIn/Out.LaneId`, `StationId` | `L02`, `ST01` | Có sẵn. |
| Thông tin đăng ký xe | `vehicleBrand`, `vehicleOwner` | `TmsVehicleRegistration.Brand`, `Owner` | `Toyota`, `Nguyễn A` | Dữ liệu bổ sung, cần kiểm soát quyền riêng tư. |

### 106. Dữ liệu kiểm tra tải trọng xe (WIM) (mới nhất từ >= key)
| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Thời điểm phát hiện | `detectTime` | `TmsTrafficData.DetectTime` | `2026-07-31T10:15:12+07:00` | Chỉ dùng nếu WIM thực sự ghi vào bảng này; hiện chưa có cờ nguồn WIM. |
| Làn/vị trí/tốc độ | `lane`, `locationCode`, `speed` | `TmsTrafficData.Lane`, `Location`, `Speed` | `lane-2`, `main_route`, `56.0` | Có sẵn ở mức dò xe. |
| Kích thước xe | `height`, `width`, `length` | `TmsTrafficData.Height`, `Width`, `Length` | `320`, `250`, `1200` | Cần chốt đơn vị. |
| Tổng tải trọng/tải trục/số trục/quá tải | `grossWeight`, `axleWeights`, `axleCount`, `isOverweight` | — Chưa có | — | Bắt buộc thêm bảng/cột WIM. |

### 110. Trao đổi với người tham gia giao thông (all/  opConfig -> xoa het tru groupCode thuoc WP)

Đây là payload tổng hợp, không có bảng nguồn độc lập.

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Mã thông điệp | `messageId` | — Chưa có | — | Cần tạo ID/outbox để theo dõi phát hành. |
| Nội dung cảnh báo sự cố | `incidentMessage` | `TmsIncident.Name`, `Description` | `Tai nạn tại Km5+500` | Chỉ lấy sự cố còn hiệu lực theo `State`. |
| Nội dung thời tiết | `weatherMessage` | `TmsWeather.Description` | `Mưa vừa, tầm nhìn 800m` | Có thể ghép thêm `Foresight`, `WindSpeed`. |
| Hướng dẫn hiển thị | `guidanceContent` | `VmsCurrent.RowData` / `TmsSignalLog.NewData` | `Giảm tốc độ` | Cần parse JSON. |
| Vị trí/thời điểm | `locationKm`, `locationMet`, `publishedTime` | Nguồn sự cố/VMS; `StartDate` hoặc `ExecuteTime` | `5`, `500`, `...` | Không có thời điểm phát hành độc lập. |
| Kênh phát hành/trạng thái gửi | `channel`, `deliveryState` | — Chưa có | — | Cần bảng notification/outbox. |

### 111. Trao đổi với TT QLĐHGT tuyến (skip)

Đây là envelope tổng hợp cho 101–109; hiện chưa có bảng giao dịch liên trung tâm.

| Yêu cầu | Tên field trả về | Bảng - cột | Giá trị mẫu | Ghi chú |
| --- | --- | --- | --- | --- |
| Loại gói con | `packetType` | Hằng số ứng dụng | `103` | Do contract trao đổi định nghĩa, không phải cột DB. |
| Dữ liệu gói | `payload` | Các mapping 101–109 ở trên | `{...}` | Payload tương ứng theo `packetType`. |
| Thời điểm tạo | `createdTime` | Thời điểm hệ thống khi dựng payload | `2026-07-31T10:15:30+07:00` | Không phải thời gian dữ liệu gốc. |
| Lệnh điều phối | `controlCommand` | `TmsSignalLog.NewData` | `{"speedLimit":60}` | Có dữ liệu lệnh nội bộ, chưa có thông tin đối tác nhận. |
| Trạng thái lệnh | `controlState` | `TmsSignalLog.State` | `executed` | Cần chuẩn hóa enum. |
| Trạng thái kết nối | `connectionState` | — Chưa có bảng lưu bền | — | Hạ tầng có heartbeat runtime nhưng chưa có entity lịch sử/kết nối. |
| ACK, retry, lỗi, đối tác | `ackTime`, `retryCount`, `error`, `partnerId` | — Chưa có | — | Cần bảng outbox/inbox liên trung tâm. |

## Việc cần chốt trước khi lập trình API chia sẻ

1. Chốt contract JSON, bảng mã và đơn vị đo cho các field trả về trong tài liệu này.
2. Bổ sung dữ liệu còn thiếu: danh mục đường chuẩn, WIM và điều kiện mặt đường.
3. Bổ sung khóa `CctvDevice.EquipmentId`; không dùng nối IP cho tích hợp chính thức.
4. Tạo outbox/inbox để theo dõi 110–111: người nhận, payload, ACK, retry và lỗi.

## Entity đã đối chiếu

`TmsTrafficData`, `TmsTrafficStatistic`, `TmsZoneStatus`, `TmsZone`, `TmsWeather`, `TmsIncident`, `TmsEventType`, `TmsEquipment`, `TmsSignalLog`, `VmsCurrent`, `CctvDevice`, `TollTransactionIn`, `TollTransactionOut`, `TollLane`, `TollStation`, `TmsVehicleRegistration`.


TechInfo Flow Process:
BE
1. Tích hợp FE
	a/ Cấu hình: CRUD (Ghi lại lịch sử chi tiết)
	b/ Lịch sử: R
	
2. Chạy tiến trình

(BackgroundService/ Hangfire/....)
Background -> Worker (Windows service ) - > .NET 10 (tận dung dc các code của module đã thực hiện)

- Đọc cấu hình thiết lập (1a)
- Lấy dữ liệu liên quan (theo gói tin - bảng DL) (đối tác - mã gói - chu kỳ gửi 
A 101 30s
A 102 60s
A 103 45s
B 101 50s

- Xử lý ánh xạ dữ liệu theo cấu trúc trả về (gửi đi)

RefId / mã gói / mã doi tac
								
- Lưu trữ dữ liệu trả về theo cấu trúc thư mục quy định
A
	101 ->     thông tin -> X -> lấy DL => map qua viewModel -> json -> lưu ra file theo đúng đường dẫn
								D:/ShareData/Out/{doi tac}/yyyyMM/ddHH/{ma goi tin}/Ma goi tin_yyyyMMHHddmmss.json

- Ghi nhận lịch sử thao tác 
