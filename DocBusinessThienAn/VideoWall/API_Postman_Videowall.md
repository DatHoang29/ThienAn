# Tham chiếu Response thực tế — ISAPI Video Wall Controller

**Thiết bị:** Hikvision `DS-C66S-H88-CL` · SN `GW2405704` · IP `10.10.9.236` · HTTP port `80`
**Nội dung:** mọi API đã gọi thật trên thiết bị này, kèm **đầy đủ trường response với giá trị đo được**, ý nghĩa từng trường, và lưu ý riêng cho từng request.

> Đây là dữ liệu **đo thực tế**, không phải trích tài liệu. Ở nhiều chỗ nó **khác với tài liệu** — những chỗ đó đều được đánh dấu 🔴.
> Ký hiệu: ✅ đã chạy OK · ❌ lỗi · ⚠️ lưu ý · 🔴 khác tài liệu

---

## MỤC LỤC

| # | API | Method | Trạng thái |
|---|---|---|---|
| **A** | **Xác thực** | | |
| A.1 | `/ISAPI/Security/userCheck` | GET | ✅ |
| A.2 | Header `WWW-Authenticate` (3 biến thể quan sát được) | — | ✅ |
| **B** | **Phần cứng & năng lực** | | |
| B.1 | `/ISAPI/DisplayDev/capabilities` | GET | ✅ |
| B.2 | `/ISAPI/DisplayDev/decoingDevice/status?format=json` | GET | ✅ |
| **C** | **Output channel** | | |
| C.1 | `/ISAPI/DisplayDev/Video/outputs/channels` | GET | ✅ |
| C.2 | `/ISAPI/DisplayDev/Video/outputs/channels/<id>` | GET | ✅ |
| **D** | **Video Wall** | | |
| D.1 | `/ISAPI/DisplayDev/VideoWall` | GET | ✅ |
| D.2 | `/ISAPI/DisplayDev/VideoWall/<id>` | PUT | ✅ |
| D.3 | `/ISAPI/DisplayDev/VideoWall/<id>/outputs` | GET | ✅ |
| **E** | **Window** | | |
| E.1 | `/ISAPI/DisplayDev/VideoWall/<id>/windows` | GET | ✅ |
| E.2 | `/ISAPI/DisplayDev/VideoWall/<id>/windows/<VWMWID>` | PUT | ✅ |
| **F** | **Screen control** | | |
| F.1 | `/ISAPI/DisplayDev/ScreenCtrl/closeAll` | PUT | ❌ |
| **G** | **Request lỗi — dùng làm mẫu debug** | | |
| G.1–G.5 | 5 lỗi thực tế và cách đọc | | ❌ |
| **H** | Bảng tổng hợp ID |  | |

---

# A. XÁC THỰC

## A.1. `GET /ISAPI/Security/userCheck` ✅

*Tên trong tài liệu: mục 9.1.12.1 — "Log in to the device by digest"*
**Đây là API dùng để xác nhận "đã kết nối được".** Không có API login nào khác.

### Response thật

```xml
<?xml version="1.0" encoding="UTF-8"?>
<userCheck version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <statusValue>200</statusValue>
  <statusString>OK</statusString>
  <isRiskPassword>false</isRiskPassword>
  <isActivated>true</isActivated>
</userCheck>
```

### Bảng trường

| Trường | Giá trị đo được | Ý nghĩa | Ghi chú |
|---|---|---|---|
| `statusValue` | `200` | `200` = thành công, `401` = thất bại | Đây là mã của ISAPI, độc lập với HTTP status |
| `statusString` | `OK` | `OK` / `Unauthorized` | |
| `isRiskPassword` | `false` | Mật khẩu bị đánh giá là yếu? | `true` → nhiều firmware chặn API |
| `isActivated` | `true` | Thiết bị đã kích hoạt | `false` → phải activate bằng SADP/HiTools trước |

### 🔴 Trường tài liệu có mà firmware KHÔNG trả về

| Trường | Tài liệu mô tả | Hệ quả |
|---|---|---|
| `isDefaultPassword` | Đang dùng mật khẩu mặc định? | Không kiểm tra được qua API |
| `residualValidity` | Số ngày mật khẩu còn hiệu lực | — |
| **`lockStatus`** | `unlock` / `locked` | 🔴 **Không biết được IP có đang bị khóa** |
| **`unlockTime`** | Giây còn lại tới khi mở khóa | 🔴 Không biết phải chờ bao lâu |
| **`retryLoginTime`** | Số lần đăng nhập còn lại | 🔴 **Không biết còn mấy lượt** |

⚠️ **Lưu ý quan trọng nhất của request này:** vì 3 trường khóa IP không được trả về, **backend không thể phát hiện tình trạng bị khóa qua API**. Chỉ biết khi request bắt đầu fail. Do đó: **không retry khi gặp 401.** Chỉ retry với lỗi mạng/timeout.

Tài liệu ghi các trường trên là `opt` (tùy chọn) — nên firmware không trả về là hợp lệ, không phải lỗi. Đừng thiết kế BE phụ thuộc vào chúng.

---

## A.2. Header `WWW-Authenticate` — 3 biến thể quan sát được ✅

Không phải API, nhưng là dữ liệu quan trọng nhất về bảo mật.

### Biến thể 1 — khi `Digest Algorithm Type = MD5/SHA256`

```
WWW-Authenticate: Digest qop="auth", realm="HIK Device",
                  nonce="<hex>", algorithm="MD5", stale="FALSE",
                  Digest qop="auth", realm="HIK Device",
                  nonce="4d6b49794d5549784f446b3659324d774e7a49335a6d453d",
                  algorithm="SHA-256", stale="FALSE"
```

🔴 **Hai challenge trong CÙNG một header**, phân biệt bằng từ khóa `Digest` thứ hai nằm giữa dòng.

⚠️ Đây là chỗ nhiều HTTP client hỏng: chúng tách header bằng dấu phẩy → `nonce` của challenge sau đè lên challenge trước, `algorithm` bị lẫn. **Đã đổi thiết bị về `MD5` để tránh.**

### Biến thể 2 — khi `Digest Algorithm Type = MD5` (cấu hình hiện tại)

```
WWW-Authenticate: Digest qop="auth", realm="HIK Device",
                  nonce="4e6b46444e44637a4e6a673659324d784d44566c59574d3d",
                  stale="FALSE"
```

🔴 **Không có trường `algorithm`.** Theo RFC 7616, thiếu trường này nghĩa là **mặc định MD5**. Client nào đọc trường này vô điều kiện sẽ crash trên `undefined` — nghi vấn chính về bug của Postman.

### Biến thể 3 — nonce hết hạn

```
WWW-Authenticate: Digest qop="auth", realm="HIK Device",
                  nonce="4e6b56444e545177526b593659324d794f4759354d6a633d",
                  stale="TRUE"
```

### Bảng trường

| Trường | Giá trị | Ý nghĩa |
|---|---|---|
| `qop` | `auth` | Quality of protection. Bắt buộc có `nc` và `cnonce` khi tính hash |
| `realm` | `HIK Device` | **Cố định trên dòng thiết bị này** |
| `nonce` | chuỗi hex ~48 ký tự | Đổi mỗi lần. **Hết hạn sau vài phút** |
| `algorithm` | *(không có)* hoặc `MD5`/`SHA-256` | Thiếu = MD5 |
| `stale` | `FALSE` / `TRUE` | ⭐ Xem dưới |

### ⭐ `stale` là trường quan trọng nhất khi debug 401

| `stale` | Nghĩa | Hành động |
|---|---|---|
| **`FALSE`** | 🔴 **Sai username/password** | **DỪNG NGAY.** Sai 2 lần là dừng, tránh khóa IP |
| **`TRUE`** | ✅ **Mật khẩu ĐÚNG**, chỉ `nonce` cũ | An toàn. Lấy nonce mới, thử lại |

Đây là cách duy nhất phân biệt "sai mật khẩu" với "nonce hết hạn" trên firmware này, vì `retryLoginTime` không được trả về.

---

# B. PHẦN CỨNG & NĂNG LỰC

## B.1. `GET /ISAPI/DisplayDev/capabilities` ✅

*Mục 9.7.5.1 — "Get the capability of video wall controller"*
**Request giá trị nhất toàn bộ bộ test.** Một lần gọi ra toàn bộ cờ tính năng, quyết định nhóm API nào dùng được.

### Nhóm cờ cấp cao

| Trường | Giá trị | Ý nghĩa |
|---|---|---|
| `isSupportScreenCtrl` | `true` | Hỗ trợ điều khiển màn hình (qua serial) |
| `isSupportCustomBoardModle` | `true` | Tùy chỉnh model board |
| `isSupportVideoWallOperate` | `true` | Vận hành tường |
| `isSupportVideoWall` | `true` | Có chức năng video wall |

### `VideoCap.VideoInputsCap`

| Trường | Giá trị | Ý nghĩa | Lưu ý |
|---|---|---|---|
| `videoInputPortNums` | **`24`** | Số cổng input | 🔴 **Trần khung máy, KHÔNG phải số cổng đang có.** Thực tế 8 (xem B.2) |
| `name` | `min=1 max=64` | Độ dài tên nguồn | |
| `isSupportColorSetting` | `true` | Chỉnh màu nguồn (9.7.4.12/13) | ✅ dùng được |
| `isSupportPostionSetting` | **`false`** | Chỉnh vị trí ảnh | ❌ Bỏ 9.7.4.19 |
| **`isSupportCutOffSetting`** | **`true`** | **Crop ảnh nguồn (9.7.4.16)** | ✅ **Composite window khả thi** |
| `isSupportPictureCapture` | `true` | Chụp ảnh tĩnh nguồn (9.7.4.18) | ✅ Thumbnail preview OK |
| `isSupportText` | `true` | OSD trên nguồn | |
| `SupportSelfdefineResolution.signalType` | `DP,HDMI,DVI` | Loại cổng cho phép tùy chỉnh độ phân giải | |
| `isSupportEDIDResolution` | **`false`** | Đọc/áp EDID | ❌ Phải set độ phân giải thủ công |
| `isSupportJoinSignalCfg` | `true` | **Ghép nguồn (joint signal)** | ✅ Dùng cho composite |
| `isSupportJoinSignalResolution` | `true` | Độ phân giải nguồn ghép | |
| `SupportAudioCfg.signalType` | `DP,HDMI,DVI,VGA` | Cổng có audio | |
| **`isSupportVideoPreview`** | **`false`** | Preview video nguồn | 🔴 ❌ **Không xem preview video qua mạng** |
| **`isSupportSubStreamVideoPreview`** | **`false`** | Preview sub-stream | ❌ |
| `supportHDRPortType` | `DP,SDI,HDMI4K` → `DP` | Cổng hỗ trợ HDR | Board `04HI` (HDMI 1.4) không có HDR |
| `joinSignalPreviewStreamType` | `main` | Loại stream preview nguồn ghép | |

### `VideoCap.VideoOutputsCap`

| Trường | Giá trị | Ý nghĩa | Lưu ý |
|---|---|---|---|
| `videoOutputPortNums` | **`24`** | Số cổng output | 🔴 Trần khung máy. Thực tế 8 |
| `isSupportMultiOutputType` | `true` | Nhiều loại cổng ra | |
| `isSupportMultiResolution` | `true` | Mỗi output một độ phân giải | |
| `isSupportColorSetting` | `true` | Chỉnh màu output | |
| `isSupportWidthHeightSetting` | `true` | Đặt kích thước tùy ý | |
| `isSupportOutputIdentity` | `true` | **Hiện số hiệu lên màn thật** | ⚠️ **Tài liệu không có API này** — tìm bằng DevTools. Rất hữu ích để map cổng ↔ màn không cần rút cáp |
| `isSupportEDIDResolution` | `false` | | ❌ |
| `OutputResolutionCapList` | **rỗng** | Danh sách độ phân giải hỗ trợ | ⚠️ Rỗng ở cấp tổng. Gọi `/channels/<id>/capabilities` để lấy theo từng cổng |
| `isAutoCloseHideDisplayOutput` | `true` | Tự tắt output bị che | |
| `supportOutputBitDepthType` | `LEDSendCard` | Độ sâu màu | Chỉ áp dụng board LED |
| `resolutionCoordinateX/Y` | `min=0 max=65535` | Miền toạ độ pixel thật | ⭐ **65535** — dùng cho `resolutionCoordinate` |

### `VideoCap.VideoStreamingCap`

| Trường | Giá trị | Ý nghĩa |
|---|---|---|
| `streamingNums` | **`2048`** | Số nguồn stream mạng tối đa |
| `isSupportURL` | `true` | Thêm nguồn bằng URL |
| `isSupportDDNS` | `false` | ❌ |
| `isSupportIPAddress` | `true` | Thêm nguồn bằng IP |
| `isSupportDistributedIPSignal` | `true` | Nguồn IP phân tán |
| `isSupportAddBatch` | `true` | Thêm nguồn theo lô |
| `isSupportStreamChanSearch` | `true` | Tìm kiếm kênh stream |
| `isSupportEncryptStream` | `false` | ❌ Stream không mã hoá |
| `isSupportLoopEncryptStream` | `false` | ❌ |
| `name` / `group` | `min=1 max=32` | Độ dài tên / nhóm nguồn |

### `AudioCap`

| Trường | Giá trị | Lưu ý |
|---|---|---|
| `audioInputsPortNums` | `200` | ⚠️ **Bằng chứng rõ nhất rằng các số `*PortNums` là trần lý thuyết** — máy chỉ có 2 cổng audio in vật lý |
| `audioOutputsPortNums` | `200` | |
| `audioOutputMaxNum` | `1` | Số output audio đồng thời |
| `isSptAudioInputBind` | `true` | Gắn audio với video |
| `isSptAudioMatrix` | `true` | Ma trận audio |
| `isSptAudioChanAssoc` | `true` | Liên kết kênh audio |
| `audioInChanNameMaxLength` | `64` | |

### `VideoWallCap` ⭐ nhóm quan trọng nhất cho FE

| Trường | Giá trị | Ý nghĩa | Lưu ý |
|---|---|---|---|
| `maxWallNums` | **`8`** | Số tường tối đa | |
| `maxWindowNums` | **`512`** | **Trần cứng số window** | ⚠️ FE phải chặn trước khi gọi API. Là trần cho **cả tường**, không phải mỗi màn |
| **`baseOutputSize`** | **`1920`** | Đơn vị toạ độ chuẩn hoá | ⭐ **Con số quan trọng nhất.** Mỗi màn = ô **vuông 1920×1920** |
| `isSupportPlan` | `true` | Lịch tự động (9.7.6) | ✅ |
| `isSupportScene` | `true` | Scene (9.7.7) | ✅ |
| `isSupportRoam` | `true` | Window kéo tự do qua nhiều màn | ✅ |
| `isSupportAutoSwitchMainSub` | `false` | Tự chuyển main/sub stream | ❌ Bỏ 9.7.11.15/16 |
| `isSupportBaseMap` | `true` | Ảnh nền (9.7.10) | ✅ |
| `isSupportVirtualLED` | `true` | Chữ chạy (9.7.9) | ✅ |
| `streamFailedMode` | `linkException,lastFrame` | Hiển thị khi mất stream | |
| `supportLEDResolutionVoutType` | `HDMI,DVI,HDMI4K` | | |
| `isSupportWallNPreMonitor` | **`false`** | Preview mạng cho tường | 🔴 ❌ **Bỏ 9.7.2.2, 2.3, 2.14, 2.15, 2.16** |
| `isSupportWallLPreMonitor` | **`false`** | Preview local | ❌ |
| `wallWebkMode` | `all,single` → `all` | Chế độ đánh thức tường | |
| `wallBackMode` | `color,defaultPic` | Nền tường: màu hoặc ảnh | |
| `isSupportDisplayWinNo` | `false` | Hiện số window lên tường | ❌ |
| `isSupportExportBaseMapBatchFile` | `true` | Xuất ảnh nền theo lô | |
| `backgroundRGBColor.RGB` | `min=0 max=64` | ⚠️ `max=64` — không phải 255. Có thể là độ dài chuỗi hex |
| `isSupportSceneSwitchRecoverAudio` | `true` | Phục hồi audio khi đổi scene | |
| `wndLockKeep` | `true` | Giữ trạng thái lock window | |
| `videoWallName` | `min=1 max=64` | Độ dài tên tường |

### `VideoWallCap.SceneCap`

| Trường | Giá trị | Lưu ý |
|---|---|---|
| `maxSceneNums` | **`128`** | Số scene tối đa |
| `isSupportSceneCopy` | `false` | ❌ Không nhân bản scene |
| `isSupportSceneInfo` | `true` | ✅ |
| `isSupportSceneImport` / `Export` | `true` / `true` | ✅ Xuất/nhập scene được |
| `isSupportSaveSceneLogo` | `false` | ❌ |
| `isSupportSaveSceneAudio` | `true` | ✅ Scene lưu audio |
| **`isSupportSaveSceneVirLed`** | **`false`** | 🔴 ❌ **Scene KHÔNG lưu chữ chạy** |
| `isSupportSaveSceneSmartDec` | `true` | ✅ |
| **`isSupportSaveSceneBaseMap`** | **`false`** | 🔴 ❌ **Scene KHÔNG lưu ảnh nền** |
| `isSupportSaveSceneDecOsd` | `false` | ❌ |
| `isSupportSaveSceneDecDelay` | `true` | ✅ |
| `name` | `min=1 max=32` | Độ dài tên scene |

⚠️ **Hệ quả thiết kế:** nếu FE cho người dùng lưu scene kèm virtual LED hoặc wallpaper, hai thứ đó **không được khôi phục** khi chuyển scene. BE phải tự lưu và tự áp lại sau khi `activate`.

### `VideoWallCap.BaseMapCap`

| Trường | Giá trị |
|---|---|
| `baseMapNums` | `3` ảnh nền |
| `supportFileType` | `JPEG,JPG` — **chỉ JPEG** |
| `maxFileSize` | `8` (MB) |
| `maxImageWidth` / `Height` | `1920` / `1080` |
| `imageWidth` / `Height` | `min 1280–1920` / `min 720–1080` |
| `BaseMapAlignUnit` | width `16`, height `8` — kích thước phải là bội số |

### `VideoWallCap.VirtualLEDCap`

| Trường | Giá trị |
|---|---|
| `virtualLEDNums` | `3` / tường |
| `virtualLEDFirstHeight` | `30720` |
| `perWallClockSubtitlesMaxNum` | `1` — đồng hồ |
| `perWallDynamicSubtitlesMaxNum` | `1` — chữ chạy ngang |
| `perWallVerticalSubtitlesMaxNum` | `2` — chữ chạy dọc |
| `dynamicSubtitlesMaxNum` | `4` |
| `isSupportFontRestore` | `true` |
| `isSupportShowWeekInClockSubtitles` | `true` |
| `fontTypeDllMaxSize` | `15` |
| `Rect.MaxVert/HoriSubtitleWindowSize` | `38400 × 30720` |
| `Rect.MinVert/HoriSubtitleWindowSize` | `96 × 96` |
| `ResolutionRect.Max...` | `81920 × 34560` |

⚠️ `Rect` max = `38400 × 30720`. Với `baseOutputSize = 1920` → `38400 / 1920 = 20` cột, `30720 / 1920 = 16` hàng. **Đây là bằng chứng độc lập rằng toạ độ chuẩn hoá vượt xa 1920** — xác nhận `range:[0,1920]` trong tài liệu là sai.

### Cờ API cấp thiết bị — ⭐ nhóm giải đáp "API tài liệu ghi thiếu"

| Trường | Giá trị | Ý nghĩa |
|---|---|---|
| `isSupportOpenSourceCert` | `true` | |
| `isSupportFontLibraryImport` / `Cfg` | `true` | Nhập font cho virtual LED |
| `maxFontLibraryNum` | `min=1 max=4` → `1` | |
| **`isSupportDecoingDeviceStatus`** | `true` | 🔴 **Node cũng viết sai chính tả `Decoing`** — xác nhận URL `/decoingDevice/status` đúng là thiếu chữ `d` |
| `isSupportBoardStreamExportCfg` | `true` | 9.7.2.9/2.10 ✅ |
| `isSupportPictureDecode` | `false` | ❌ Không decode ảnh tĩnh |
| **`isSupportGetVideoWallScale`** | **`true`** | ⭐ API tài liệu chỉ ghi ở chương 8 — **có thật** |
| **`isSupportModifyVideoWallScale`** | **`true`** | ⭐ **Có thật** |
| **`isSupportVideoOutputChannelStatus`** | **`true`** | ⚠️ Có API trạng thái output riêng — **tài liệu không mô tả**, tìm bằng DevTools |
| `isSupportAddSignalSourceGroup` | `true` | Nhóm nguồn ✅ |
| `isSupportDeleteSignalSourceGroup` | `true` | ✅ |
| `isSupportModifySignalSourceGroup` | `true` | ✅ |
| `isSupportGetSignalSourceGroup` | `true` | ✅ |
| `isSupportActualChannelAdd` | `true` | Thêm kênh thực |
| `isSupportGetWallLEDComfortParams` | `true` | Tham số dễ chịu mắt cho LED |
| `isSupportModifyWallLEDComfortParams` | `true` | |
| `supportWallLEDComfortParamsType` | `LEDSendCard` | Chỉ board LED |
| `isSupportSearchSignalSource` | `true` | ✅ |
| `isSupportDeleteSignalSource` | `true` | ✅ |
| `isSupportUpdateSignalSourceID` | `true` | ✅ |
| `isSupportModifySignalSource` | `true` | ✅ |
| `isSupportAddSignalSource` | `true` | ✅ |
| `isSupportGetPreEditInfo` | `true` | Pre-editing (9.7.11.17) ✅ |
| `isSupportIrregularLEDParams` | `false` | ❌ LED không đều |
| `isSupportSignalSourceNoSignalParams` | `true` | Hiển thị khi mất tín hiệu ✅ |
| `isSupportUpdateVideoWallParamsCompleted` | `true` | ⭐ Chưa có trong Excel |
| `isSupportSearchUpdateVideoWallParams` | `true` | ⭐ Chưa có trong Excel |
| `isSupportDeleteVideoWallPara` | `true` | ⭐ **Xoá tường** — chưa có trong Excel |
| `isSupportAddVideoWallPara` | `true` | ⭐ **Thêm tường** — chưa có trong Excel |
| `isSupportSignalSyncParams` | `true` | Đồng bộ tín hiệu |
| `isSupportLowLatencyParams` | `true` | Chế độ độ trễ thấp |
| `isSupportOutputRotateParams` | `true` | **Xoay output** — cho màn dọc |
| `isSupportMainScreenSyncTag` | `true` | |
| `isSupportSearchMainScreenSyncTag` | `true` | |
| `isSupportSyncMainScreen` | `true` | |
| `isSupportOutputBitDepth` | `true` | |
| `isSupportSearchOutputBitDepth` | `true` | |
| `isSupportSignalHDRMode` | `true` | |
| `isSupportSearchSignalHDRMode` | `true` | |
| `isSupportDecoingDeviceMode` | `true` | |
| `isSupportBoardRowCol` | `true` | Vị trí board theo hàng/cột |
| `isSupportDefaultDecodeDelayParams` | `true` | 9.7.2.11–13 ✅ |
| `isSupportSceneControlParams` | `true` | 9.7.7.7–9 ✅ |
| `isSupportSpareWorkStatus` | `true` | **Hot standby** |
| `isSupportStandbyOutputParams` | `true` | Output dự phòng |
| `isSupportBoardBackupRange` | `true` | Backup board |
| **`isSupportSyncPeerWallParams`** | `true` | ⭐ **Đồng bộ tham số giữa các tường** — đáng khai thác cho kiến trúc 4 controller |
| `isSupportHotStandbyADParams` | `true` | |
| `isSupportSceneSwitchParams` | `true` | |
| `isSupportOutputUniformResolution` | `false` | ❌ |
| `isSupportMatrixOutputSwtitch` | `false` | ❌ (lưu ý: tài liệu viết sai `Swtitch`) |
| `isSupportMatrixParams` | `false` | ❌ |
| `isSupportMatrixAssociatedParams` | `false` | ❌ |
| `SIPServerCap.isSupportEnDecodeSeparateCfg` | `false` | ❌ |
| `AlarmCap.isSupportAlarmLinkage` | `false` | ❌ Không liên động cảnh báo |

### ⚠️ Lưu ý của request này

1. **Gọi request này ĐẦU TIÊN** khi BE kết nối một controller. Mọi quyết định "nhóm API nào dùng được" đều nằm ở đây.
2. **Các số `*PortNums` là trần khung máy, không phải số cổng thật.** Muốn số thật → dùng B.2 hoặc C.1.
3. Response rất dài. Nên lưu ra file, hoặc dùng script trích field.
4. **Cache lại kết quả.** Nó chỉ đổi khi thay board hoặc nâng firmware.

---

## B.2. `GET /ISAPI/DisplayDev/decoingDevice/status?format=json` ✅

*Mục 9.7.2.1 — "Get decoding device status"*
🔴 **URL viết sai chính tả: `decoingDevice` thiếu chữ `d`.** Gõ đúng `decodingDevice` → 404. Firmware sai giống hệt tài liệu (xác nhận qua node `isSupportDecoingDeviceStatus`).
⚠️ Đây là API JSON đầu tiên → **script Digest phải dùng `getPathWithQuery()`**, nếu dùng `getPath()` sẽ mất `?format=json` và bị 401.

### `DevCaseStatus` — khung máy

| Trường | Giá trị | Ý nghĩa |
|---|---|---|
| `height` | **`"4.5U"`** | Chiều cao khung. 🔴 Datasheet chỉ ghi S6=2U và S12=4U — giá trị `4.5U` không có trong datasheet công khai |
| `row` | `8` | Số hàng slot |
| `col` | `2` | Số cột slot |

→ Lưới slot **8 × 2**. Cột 1 = input, cột 2 = output (tách riêng cố định).

### `MainBoardStatusList[0]`

| Trường | Giá trị | Ý nghĩa |
|---|---|---|
| `ID` | `1` | |
| `row` / `col` | `1` / `2` | Vị trí trong lưới |
| `rowCover` / `colCover` | `1` / `1` | Số hàng/cột chiếm |
| `runTime` | `12444` | Giây kể từ khởi động (~3,5 giờ) |
| `CPUUtilization` | `0` | % |
| `memoryUtilization` | `36` | % |
| `exceptionList` | `[]` | Danh sách lỗi — rỗng = tốt |
| `status` | `normal` | |

**`serialPortList`** — 🔴 xác nhận cảnh báo về cổng RJ-45:

| ID | row/col | `serialPortType` | `status` |
|---|---|---|---|
| 1 | 1/1 | **`RJ45Console`** | `unknown` |
| 2 | 1/2 | **`reusePort`** | `unknown` |

⚠️ **Hai jack RJ-45 này KHÔNG phải cổng mạng.** `RJ45Console` = serial console (115200 baud). `reusePort` = RS-232/485 dùng chung. Cắm cáp mạng vào đây không có tác dụng — nguyên nhân phổ biến nhất của "ping không thấy máy".
⚠️ `status: unknown` = chưa cấu hình → **đây là lý do `closeAll` trả `invalidOperation`** (xem F.1).

**`NetworkInterfaceList`** — chỉ có **MỘT** cổng mạng:

| Trường | Giá trị |
|---|---|
| `row` / `col` | `1` / `3` |
| `ipV4Address` | `10.10.9.236` |
| **`portNo`** | **`8000`** | ⚠️ Đây là **cổng SDK**, khác cổng `80` của ISAPI |
| `MACAddress` | `fc:9f:fd:cf:f1:c8` |
| `ethernetPortStatus` | `connected` |
| `sendingRate` / `receivingRate` | `0` / `3040` | bps |
| `lineRate` | `1048576000` | ~1 Gbps |

**Các cổng khác trên main board:**

| Nhóm | Số lượng | row/col | `status` |
|---|---|---|---|
| `genlockPortList` | 2 | 1/4, 1/5 | `notconnect` |
| `USBInterfaceList` | 1 | 1/6 | **`connected`** |
| `audioInputList` | 2 | 1/7, 1/8 | `notconnect` |
| `audioOutputList` | 2 | 1/9, 1/10 | `notconnect` |

### `BackplaneStatusList[0]`

| Trường | Giá trị | Lưu ý |
|---|---|---|
| `ID` | `1` | |
| **`backplaneTemperature`** | **`60`** | °C. Datasheet cho nhiệt độ **môi trường** 0–50 °C; đây là nhiệt độ **trong máy**. ⚠️ Nên đặt ngưỡng cảnh báo ~65–70 °C |

### `SubBoardStatusList` — 13 phần tử (ID 0–12)

| ID | row | col | `status` | `subBoardType` | Ghi chú |
|---|---|---|---|---|---|
| 0 | 2 | 2 | `notInsert` | — | Slot đặc biệt, trống |
| **1** | 3 | 1 | `normal` | **`input`** | 4×HDMI, tất cả `notconnect` |
| **2** | 4 | 1 | `normal` | **`input`** | 4×HDMI, tất cả `notconnect` |
| 3–6 | 5–8 | 1 | `notInsert` | — | Slot input trống |
| **7** | 3 | 2 | `normal` | **`output`** | 4×HDMI, **port 3 & 4 `connected`** 🟢 |
| **8** | 4 | 2 | `normal` | **`output`** | 4×HDMI, tất cả `notconnect` |
| 9–12 | 5–8 | 2 | `notInsert` | — | Slot output trống |

→ **4/12 slot đang dùng: 2 board input + 2 board output = 8 HDMI IN + 8 HDMI OUT.** Xác nhận `H88`. Mở rộng tối đa 24/24.

### Trường của mỗi sub-board đang hoạt động

| Trường | ID 1 | ID 2 | ID 7 | ID 8 |
|---|---|---|---|---|
| `runTime` | `11431` | `8633` | `8777` | **`33`** ⚠️ |
| `CPUUtilization` | `0` | `4` | `0` | `0` |
| `memoryUtilization` | `50` | `43` | `40` | `39` |
| `NetworkInterfaceList[0].netPortList[0].ethernetPortStatus` | `connected` | `connected` | `connected` | `connected` |
| `sendingRate` | `29184` | `15872` | `52736` | `0` |
| `receivingRate` | `1088512` | `1166592` | `1200896` | `1152000` |
| `lineRate` | `1048576000` | ← | ← | ← |
| `exceptionList` | `[]` | `[]` | `[]` | `[]` |
| `status` | `normal` | `normal` | `normal` | `normal` |

⚠️ **Mỗi sub-board có cổng Gigabit riêng** — đây là backplane dạng "network switching" như datasheet ghi. Không phải cổng vật lý bên ngoài.

⚠️ **Board ID 8 có `runTime = 33` giây** trong khi các board khác đã chạy hàng giờ → board vừa khởi động lại. `exceptionList` rỗng nên không có lỗi ghi nhận, nhưng đây vẫn là dấu hiệu đáng theo dõi.

> 💡 **Mẹo giám sát:** theo dõi `runTime` từng board. **Giảm đột ngột = board reboot** — dấu hiệu sự cố xuất hiện **trước cả** khi `exceptionList` có gì. Đây là chỉ báo sớm tốt nhất trong toàn bộ response.

### `SubBoardInterfaceList` — trạng thái từng cổng vật lý ⭐

Board ID 7 (output đầu tiên):

| `ID` | `subBoardInterfaceType` | `outputPortLinkStatus` |
|---|---|---|
| 1 | `HDMI` | `notconnect` |
| 2 | `HDMI` | `notconnect` |
| **3** | `HDMI` | 🟢 **`connected`** |
| **4** | `HDMI` | 🟢 **`connected`** |

| Trường | Giá trị quan sát | Giá trị tài liệu mô tả | Ý nghĩa |
|---|---|---|---|
| `subBoardInterfaceType` | `HDMI` | `HDMI, DVI, VGA, DP, HDMI4K, DP4K, SDI` | Loại cổng |
| **`outputPortLinkStatus`** | `connected` / `notconnect` | ← | ⭐ **Màn hình có cắm cáp không** |
| `signalStatus` | 🔴 **không trả về** | `signal` / `noSignal` / `abnormal` | Cổng input có tín hiệu vào không |
| `decodeWallStatus` | 🔴 **không trả về** | `decoding` / `noDecoding` | Đang phát lên tường không |
| `subBoardInterfaceMode` | 🔴 **không trả về** | `input` / `output` | Chỉ có ý nghĩa khi `subBoardType = inputOutputMixed` |
| `row` / `col` | 🔴 **không trả về** | int | Vị trí cổng trên board |

⚠️ **Firmware dùng chung tên `outputPortLinkStatus` cho cả board input.** Với board input nó nghĩa là "chưa cắm cáp nguồn vào" — cả 8 cổng input đều `notconnect`.

### ⚠️ Lưu ý của request này

1. **Đây là API duy nhất cho biết cấu hình phần cứng thật** (bao nhiêu slot, board gì, board nào chết).
2. Response nặng. **Không dùng để poll thường xuyên.** Cho panel trạng thái màn hình, dùng C.1 (nhẹ hơn nhiều).
3. Là nguồn dữ liệu cho panel health: `backplaneTemperature`, `exceptionList`, `status`, `runTime`, `CPUUtilization`, `memoryUtilization`.
4. **API này KHÔNG cho biết sức khỏe màn hình** — chỉ biết cáp có cắm hay không. Nhiệt độ/giờ chạy của màn chỉ đọc được qua serial transparent transmission (mục 5.9 tài liệu).

---

# C. OUTPUT CHANNEL

## C.1. `GET /ISAPI/DisplayDev/Video/outputs/channels` ✅

*Mục 9.7.3.4 — "Get basic parameters of all video outputs"*
⭐ **API tốt nhất cho panel trạng thái màn hình.** Một request → cả 8 cổng kèm tên, độ phân giải, board/port và trạng thái kết nối.

### Response — 8 phần tử `<VideoOutputChannel>`

| `id` | Hex | `boardID` | `portID` | `name` | `timeSequenceMode` | `imageW×H` | `outputPortAccessStatus` | `port` |
|---|---|---|---|---|---|---|---|---|
| 17235969 | `0x01070001` | 7 | 1 | Output 7-1 | `standard` | 0×0 | `notConnected` | 13191 |
| 17235970 | `0x01070002` | 7 | 2 | Output 7-2 | `standard` | 0×0 | `notConnected` | 13191 |
| **17235971** | `0x01070003` | 7 | 3 | Output 7-3 | `standard` | 0×0 | 🟢 **`normal`** | 13191 |
| **17235972** | `0x01070004` | 7 | 4 | Output 7-4 | `standard` | 0×0 | 🟢 **`normal`** | 13191 |
| 17301505 | `0x01080001` | 8 | 1 | Output 8-1 | **`custom`** | 1920×1080 | `notConnected` | 13791 |
| 17301506 | `0x01080002` | 8 | 2 | Output 8-2 | **`custom`** | 1920×1080 | `notConnected` | 13791 |
| 17301507 | `0x01080003` | 8 | 3 | Output 8-3 | **`custom`** | 1920×1080 | `notConnected` | 13791 |
| 17301508 | `0x01080004` | 8 | 4 | Output 8-4 | **`custom`** | 1920×1080 | `notConnected` | 13791 |

Tất cả: `portType = HDMI`, `resolution = 1920*1080@60HZ`, `deviceID = 0`, `useEDIDResolution = false`, `LEDSendCardResolutionEnabled = false`, `PortInBoard.ipAddress = 10.10.9.236`.

### Bảng trường

| Trường | Ví dụ | Ý nghĩa | Lưu ý |
|---|---|---|---|
| `id` | `17235971` | **ID kênh output** | 🔴 **Không phải 1..8.** Xem công thức mục H |
| `portType` | `HDMI` | Loại cổng | `VGA,CVBS,HDMI,Spot,SDI,DVI,TVI...` |
| `timeSequenceMode` | `standard` / `custom` | Chế độ timing | ⚠️ Board 7 và 8 **khác nhau** |
| `name` | `Output 7-1` | Tên do thiết bị đặt | Sửa được bằng PUT. Tự đặt theo `board-port` — tiện để đối chiếu |
| `OutputResolution.resolution` | `1920*1080@60HZ` | Độ phân giải | Chuỗi enum, chú ý dấu `*` và `@`, chữ `HZ` in hoa |
| `OutputResolution.imageWidth/Height` | `0`/`0` hoặc `1920`/`1080` | Kích thước ảnh | ⚠️ `0` khi `timeSequenceMode = standard`; có giá trị khi `custom` |
| `PortInBoard.boardID` | `7` | ⭐ **Board vật lý** | Khớp với `SubBoardStatusList[].ID` ở B.2 |
| `PortInBoard.portID` | `3` | ⭐ **Cổng trên board** | Khớp với `SubBoardInterfaceList[].ID` ở B.2 |
| `PortInBoard.ipAddress` | `10.10.9.236` | IP nội bộ của board | |
| `PortInBoard.port` | `13191` / `13791` | Cổng nội bộ | Mỗi board một cổng riêng |
| `deviceID` | `0` | | |
| `useEDIDResolution` | `false` | Dùng độ phân giải từ EDID | Khớp `isSupportEDIDResolution = false` |
| `LEDSendCardResolutionEnabled` | `false` | Chế độ LED send card | Không áp dụng (đây là board HDMI) |
| **`outputPortAccessStatus`** | `normal` / `notConnected` | ⭐ **Màn hình có cắm không** | 🔴 **Tài liệu KHÔNG mô tả trường này** — firmware tự có |

### 🔴 Trường tài liệu có mà response danh sách KHÔNG trả về

`outputPortEnabled`, `outputMode`, `outputType`, `CustomMode`, `AdvanceMode`, `outputPortAccessType`, `outputBackgroundType`, `outputBackgroundRGBColor`, `OutputResolutionCapList`.

→ Muốn các trường này phải gọi C.2 (từng kênh).

### ⚠️ Lưu ý của request này

1. ⭐ **Đây là câu trả lời cho "API nào lấy trạng thái màn hình".** Nhẹ, đầy đủ, đúng định dạng để đổ vào panel.
2. **Gọi request này khi BE khởi tạo kết nối** để dựng bảng map `id ↔ boardID/portID`. **Đừng hardcode ID.**
3. **Làm mới bảng map khi thay board** — cắm thêm board sẽ sinh ID mới.
4. ⚠️ **Board 7 dùng `standard`, board 8 dùng `custom`** — hai board output không đồng nhất. Cần đưa về cùng chế độ trước khi ghép tường trải qua cả hai, nếu không có thể lệch timing.
5. `name` do thiết bị tự đặt theo mẫu `Output <board>-<port>` — thuận tiện, nên giữ quy ước này khi đổi tên.

---

## C.2. `GET /ISAPI/DisplayDev/Video/outputs/channels/<channelID>` ✅

*Mục 9.7.3.5 — "Get parameters of a specific video output"*

Trả về nhiều trường hơn C.1. Đo được trên `17235972`:

| Trường | Giá trị | Lưu ý |
|---|---|---|
| `outputPortEnabled` | **`false`** | 🔴 Xem phân tích dưới |

Các trường khác theo tài liệu (chưa đối chiếu hết): `PortInBoard.boardType`, `deviceID`, `outputMode`, `outputType`, `timeSequenceMode`, `CustomMode.timeSequence*`, `AdvanceMode.horizontal*`/`vertical*`, `name`, `outputPortAccessType`, `outputBackgroundType`, `outputBackgroundRGBColor`.

### 🔴 Kết luận về `outputPortEnabled`

**Trường này KHÔNG phải công tắc bật/tắt cổng HDMI.** Ba dấu hiệu:

1. **Không xuất hiện** trong response danh sách C.1 — dấu hiệu của trường phụ, không phải trường điều khiển chính
2. Trong đặc tả, nằm ngay cạnh `LEDSendCardResolutionEnabled`, `outputPortAccessType`, `outputBackgroundType` — **một cụm trường dành cho LED send card**
3. **Cả hai cổng đang có màn hoạt động bình thường đều báo `false`** — nếu là công tắc thì màn đã tối
4. Set `true` **không có tác dụng**

### ⚠️ Hệ quả: thiết bị KHÔNG có API bật/tắt từng cổng HDMI

Ba lựa chọn còn lại cho nút "Bật/Tắt" trên panel:

| Cách | Hiệu ứng | Điều kiện |
|---|---|---|
| `ScreenCtrl/closeAll` + `OutputID` | Tắt **nguồn** màn thật | ⚠️ Cần dây RS-232/485 + protocol màn hình |
| `DELETE .../windows/<id>` | Màn còn sáng, mất nội dung → về nền | ✅ Làm được ngay |
| Đặt nền đen (`wallBackMode` / `wndStaticMode`) | Màn sáng, hiển thị đen | ✅ Làm được ngay |

⚠️ Nếu không kéo dây serial thì nút "Bật/Tắt" thực chất là **"Hiện/Ẩn nội dung"** — nên đổi nhãn trên UI cho đúng, người dùng bấm "Tắt" mà màn vẫn sáng sẽ tưởng lỗi.

⚠️ **Không có API đọc trạng thái bật/tắt của màn hình.** `ScreenCtrl` là một chiều. Nếu ai cầm remote tắt màn, controller không biết.

### Logic panel đề xuất

```
outputPortAccessStatus = notConnected  → ⚠️  Chưa cắm màn / mất cáp
outputPortAccessStatus = normal        → 🟢  Đang hoạt động
```

Chỉ cần **một** request C.1 là đủ dữ liệu vẽ cả 8 ô trạng thái.

---

# D. VIDEO WALL

## D.1. `GET /ISAPI/DisplayDev/VideoWall` ✅

*Mục 9.7.5.2 — "Get parameters of all video walls"*
⚠️ **Đây là bản RÚT GỌN** — chỉ 5 trường/tường. Bản đầy đủ ở `GET /VideoWall/<id>` (mục 9.7.5.4).

### Response thật

```xml
<?xml version="1.0" encoding="UTF-8"?>
<VideoWallList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <VideoWall>
    <id>1</id>
    <name>VideoWall1</name>
    <wndStaticMode>blackScreen</wndStaticMode>
    <streamFailedMode>lastFrame</streamFailedMode>
    <wallBindOutputStatus>unbound</wallBindOutputStatus>
  </VideoWall>
  <VideoWall>
    <id>2</id>
    <name>HoangNhu</name>
    <wndStaticMode>blackScreen</wndStaticMode>
    <streamFailedMode>lastFrame</streamFailedMode>
    <wallBindOutputStatus>bound</wallBindOutputStatus>
  </VideoWall>
</VideoWallList>
```

### Bảng trường

| Trường | Wall 1 | Wall 2 | Ý nghĩa |
|---|---|---|---|
| `id` | `1` | `2` | **`videoWallID`** — tham số của gần như mọi API còn lại |
| `name` | `VideoWall1` | `HoangNhu` | Tên tường. Sửa được (D.2). Tối đa 64 ký tự |
| `wndStaticMode` | `blackScreen` | `blackScreen` | Hiển thị khi window tĩnh |
| `streamFailedMode` | `lastFrame` | `lastFrame` | Khi mất stream: `noSignal` hoặc `lastFrame` (giữ khung cuối) |
| **`wallBindOutputStatus`** | **`unbound`** | **`bound`** | ⭐ Tường đã gán output chưa |

### ⚠️ Lưu ý của request này

1. ⭐ **Gọi request này TRƯỚC** mọi API videowall khác — nó cấp `videoWallID`. **Đừng đoán là `1`.**
2. **Thiết bị có sẵn 2 tường** dù chỉ 1 tường được dùng. `maxWallNums = 8`.
3. **Wall 1 `unbound` = tường rỗng, không màn nào** → ⭐ **sandbox test an toàn tuyệt đối.** Ghi sai cũng không ai thấy. **Nên test mọi thứ ở đây trước.**
4. **Wall 2 `bound`** = chứa 2 màn hình đang hoạt động. Cẩn thận khi ghi.
5. ⚠️ Tường 2 tên `HoangNhu` — tên người. Nếu máy dùng chung thì hỏi trước khi động vào.
6. 🔴 `wallBindOutputStatus = unbound` của Wall 1 chính là lý do một số lệnh trả `invalidOperation` khi gọi trên tường đó.
7. `streamFailedMode = lastFrame` giải thích vì sao màn vẫn hiện hình dù nguồn đã mất cáp — nó giữ khung cuối.

---

## D.2. `PUT /ISAPI/DisplayDev/VideoWall/<videoWallID>` ✅

*Mục 9.7.5.3 — "Set parameters of a specific video wall"*
⭐ **Lệnh ghi đầu tiên chạy thành công.** Là khuôn mẫu cho mọi PUT khác.

### Request đã dùng

```
PUT {{base}}/ISAPI/DisplayDev/VideoWall/2
Content-Type: application/xml
```

```xml
<?xml version="1.0" encoding="UTF-8"?>
<VideoWall xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>2</id>
  <name>HieuNV</name>
</VideoWall>
```

### Response

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ResponseStatus version="1.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <requestURL></requestURL>
  <statusCode>1</statusCode>
  <statusString>OK</statusString>
  <subStatusCode>ok</subStatusCode>
</ResponseStatus>
```

⚠️ `statusCode` = **`0` HOẶC `1`** đều là thành công. BE phải chấp nhận cả hai.
⚠️ `requestURL` trả về **rỗng** — không dùng được để đối chiếu.

### Trường ghi được (theo mục 9.7.5.3)

🔴 **Gần như MỌI trường đều là `opt`** — chỉ `outputID` bên trong `WallOutput` là `req`. Nghĩa là **chỉ cần gửi field muốn đổi**.

| Trường | Kiểu | Giá trị hợp lệ |
|---|---|---|
| `id` | opt, int | `videoWallID` |
| `name` | opt, string | 1–64 ký tự |
| **`backgroudColor`** | opt, enum | `black`, `blue`, `white` — 🔴 **thiếu chữ `n`**, gõ đúng `background` sẽ sai |
| `autoSwitchMainSub` | opt, bool | Máy này `isSupportAutoSwitchMainSub = false` |
| `wndStaticMode` | opt, enum | `blackScreen` … |
| `streamFailedMode` | opt, enum | `noSignal`, `lastFrame` |
| `zeroLatencyModeEnabled` | opt, bool | |
| `streamTransportType` | opt, enum | `unicast` … |
| `wallBackMode` | opt, enum | `color`, `defaultPic` |
| `backgroundRGBColor.RGB` | opt, string | vd `ff00ff` |
| `sceneSwitchDelayEnabled` | opt, bool | |
| `sceneSwitchDelay` | opt, int | 1–604800 giây |
| `windowsMoveDelayEnabled` | opt, bool | |
| `ledShowMode` | opt, enum | `normal` … |
| `SubStreamAutoSwitch.subWndWidth/Height` | opt, int | |
| `WallOutputList[].WallOutput` | opt, array | `outputID` là **`req`** |
| `WallWindowList[].WallWindow` | opt, array | |

### ⚠️ Lưu ý của request này — quan trọng nhất toàn bộ tài liệu

🔴 **KHÔNG PUT lại nguyên response của GET.** Đây là lỗi đã thực sự xảy ra và cho `badParameters`.

Hai lý do:

1. **Response GET chứa element rỗng.** Ví dụ `<wndOperateMode></wndOperateMode>` — thiết bị chờ một giá trị enum, nhận chuỗi rỗng → `badParameters`.
2. **Response GET chứa trường chỉ-đọc và giá trị tính toán** — `layerIdx` (vd `67108865`), `videoInputChannelID` (vd `16842753`). Không gửi lên được.

✅ **Cách đúng:** gửi **tối thiểu**, chỉ `<id>` + field cần đổi.

📌 **Cách phân biệt tổng quát:** đọc đặc tả **Request Message của mục PUT**, **không phải** Response Message của mục GET. Hai cái khác nhau.

---

## D.3. `GET /ISAPI/DisplayDev/VideoWall/<videoWallID>/outputs` ✅

*Mục 9.7.5.5 — "Get linked screen parameters of all outputs"*
⭐ **Nguồn sự thật cho bố cục tường** — output nào nằm ở ô nào.

### Response thật (Wall 2)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallOutputList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <WallOutput>
    <id>2</id>
    <outputID>17235971</outputID>
    <Rect>
      <Coordinate><x>0</x><y>0</y></Coordinate>
      <width>1920</width>
      <height>1920</height>
    </Rect>
    <outputWinNum>1</outputWinNum>
    <coordinateMode>uniformCoordinate</coordinateMode>
  </WallOutput>
  <WallOutput>
    <id>3</id>
    <outputID>17235972</outputID>
    <Rect>
      <Coordinate><x>0</x><y>1920</y></Coordinate>
      <width>1920</width>
      <height>1920</height>
    </Rect>
    <outputWinNum>1</outputWinNum>
    <coordinateMode>uniformCoordinate</coordinateMode>
  </WallOutput>
</WallOutputList>
```

### Bảng trường

| Trường | WallOutput 1 | WallOutput 2 | Ý nghĩa | Lưu ý |
|---|---|---|---|---|
| `id` | `2` | `3` | ID bản ghi wall-output | ⚠️ **Không phải số thứ tự ô.** Bắt đầu từ 2, không có id 1. **Đừng suy vị trí từ đây — vị trí nằm ở `Rect`** |
| `outputID` | `17235971` | `17235972` | ⭐ Trỏ tới `id` ở C.1 | Board 7 port 3 và 4 |
| `Rect.Coordinate.x` | `0` | `0` | Toạ độ X | |
| `Rect.Coordinate.y` | `0` | **`1920`** | Toạ độ Y | Màn dưới bắt đầu ở y = 1920 |
| `Rect.width` | `1920` | `1920` | | |
| `Rect.height` | **`1920`** | **`1920`** | | 🔴 **VUÔNG 1920×1920**, không phải 1920×1080 |
| `outputWinNum` | `1` | `1` | Số window đang nằm trên output đó | |
| `coordinateMode` | `uniformCoordinate` | `uniformCoordinate` | Hệ toạ độ | Giá trị còn lại: `resolutionCoordinate` |

### Sơ đồ suy ra

```
          x=0        x=1920
  y=0     ┌──────────┐
          │ 17235971 │  Output 7-3  (WallOutput id=2)
  y=1920  ├──────────┤
          │ 17235972 │  Output 7-4  (WallOutput id=3)
  y=3840  └──────────┘
```

→ Bố cục **1 cột × 2 hàng**, xếp dọc. Không gian toạ độ ảo **1920 × 3840**.

### 🔴 Phát hiện quan trọng nhất: ô lưới là hình VUÔNG

Màn hình vật lý là **1920×1080**, nhưng trong `uniformCoordinate` mỗi màn chiếm ô **1920×1920**. `baseOutputSize = 1920` áp cho **cả chiều rộng lẫn chiều cao**, bất kể panel thật tỉ lệ bao nhiêu.

**Hệ toạ độ ảo KHÔNG giữ tỉ lệ khung hình thật.**

```
wall_width_ảo  = số_cột  × 1920
wall_height_ảo = số_hàng × 1920
```

| | Toạ độ ảo | Vật lý thật |
|---|---|---|
| Rộng | 1920 | 1920 px |
| Cao | **3840** | 2160 px |

Quy đổi khi FE vẽ:

```
x_thật = x_ảo × (rộng_thật_1_màn / 1920)
y_thật = y_ảo × (cao_thật_1_màn  / 1920)     → với 1080p: × 0.5625
```

⚠️ Hệ số phải lấy từ độ phân giải thật ở C.1. **Đừng hardcode.** Với màn 4K hoặc màn dọc, hệ số khác hoàn toàn.

🔴 **Đây là bẫy lớn nhất khi code kéo-thả.** Nếu BE giả định mỗi ô là 1920×1080 thì mọi window sẽ sai vị trí và sai kích thước theo chiều dọc — lệch gần gấp đôi.

### ⚠️ Lưu ý của request này

1. ⭐ **Cắm cáp ≠ đã lên tường.** Màn có thể `outputPortAccessStatus = normal` ở C.1 nhưng **không xuất hiện** ở đây — nghĩa là chưa gán vào lưới. Khi đó màn chỉ hiện nền, không nhận window nào.
2. Wall 1 (`unbound`) gọi request này sẽ trả về **rỗng**.
3. Muốn **gán** output vào tường thì cần `POST` cùng URL — 🔴 **API này tài liệu KHÔNG mô tả**, chỉ xuất hiện trong luồng chương 8.2.2. Cần lấy payload bằng DevTools. **Đây là API còn thiếu quan trọng nhất.**
4. Một output thường **chỉ thuộc được một tường**. Gán output 3/4 sang Wall 1 sẽ gỡ chúng khỏi Wall 2.

---

# E. WINDOW

## E.1. `GET /ISAPI/DisplayDev/VideoWall/<videoWallID>/windows` ✅

*Mục 9.7.11.2 — "Get all windows' parameters"*

### Response thật (Wall 2)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallWindowList version="2.0" xmlns="http://www.isapi.org/ver20/XMLSchema">
  <WallWindow>
    <id>33554433</id>
    <wndOperateMode>uniformCoordinate</wndOperateMode>
    <Rect>
      <Coordinate><x>0</x><y>0</y></Coordinate>
      <width>1920</width>
      <height>1920</height>
    </Rect>
    <layerIdx>67108865</layerIdx>
    <windowMode>1</windowMode>
    <wndShowMode>subWndMode</wndShowMode>
    <SubWindowList>
      <SubWindow>
        <id>1</id>
        <SubWindowParam>
          <signalMode>video input</signalMode>
          <videoInputChannelID>16842753</videoInputChannelID>
        </SubWindowParam>
      </SubWindow>
    </SubWindowList>
    <wndLockKeep>false</wndLockKeep>
  </WallWindow>
  <WallWindow>
    <id>33554434</id>
    ... Rect(0, 1920, 1920, 1920), layerIdx 67108866 ...
    ... cùng videoInputChannelID 16842753 ...
  </WallWindow>
</WallWindowList>
```

### Bảng trường

| Trường | W trên | W dưới | Ý nghĩa | Lưu ý |
|---|---|---|---|---|
| `id` | `33554433` | `33554434` | **`VWMWID`** — ID window | Hex `0x02000001`/`0x02000002`. Prefix `0x02` = loại "window" |
| `wndOperateMode` | `uniformCoordinate` | ← | Hệ toạ độ | ⚠️ **Bắt buộc gửi khi PUT.** Bỏ trống → `badParameters`. Giá trị khác: `resolutionCoordinate` |
| `Rect.Coordinate.x/y` | 0/0 | 0/**1920** | Toạ độ góc trên-trái | |
| `Rect.width/height` | 1920/1920 | 1920/1920 | Kích thước | |
| `layerIdx` | `67108865` | `67108866` | ⭐ **Z-order** | Hex `0x04000001`/`0x04000002`. **Số lớn hơn nằm TRÊN.** Chỉ-đọc — đổi bằng `/top` `/bottom`, không set số tùy ý |
| `windowMode` | `1` | `1` | Số ô chia trong window | Hợp lệ: `1`, `4`, `9`, `16` |
| `wndShowMode` | `subWndMode` | ← | Chế độ hiển thị | Giá trị khác: `fullScreenMode` |
| `wndLockKeep` | `false` | `false` | Khoá window | |
| `SubWindowList[].SubWindow.id` | `1` | `1` | Số hiệu sub-window | Với `windowMode=4` sẽ có id 1–4 |
| `SubWindowParam.signalMode` | `video input` | ← | Loại nguồn | ⚠️ **Có dấu cách** trong giá trị: `"video input"` |
| **`SubWindowParam.videoInputChannelID`** | `16842753` | **`16842753`** | ⭐ Nguồn đang chiếu | Hex `0x01010001` = **input board 1, port 1** |

### 🔴 Trường tài liệu có mà firmware KHÔNG trả về

`Coordinate` (điểm gốc), `ResolutionRect`, `displayWinNo`, `amplifyingSubWndNo`, `wndTopKeep`, `wndOpenKeep`.

### Phát hiện

**Cả 2 window cùng `videoInputChannelID = 16842753`** → hai màn hiển thị **duplicate** cùng một nguồn.

⚠️ Nguồn `16842753` là **input board 1, port 1** — mà B.2 báo cổng đó `notconnect`. Kết hợp với `streamFailedMode = lastFrame`, đây là lý do màn vẫn hiện hình dù nguồn không có cáp (giữ khung cuối).

### ⚠️ Lưu ý của request này — mô hình tư duy

⭐ **Tường là MỘT canvas. Window là những tấm giấy dán lên canvas. Màn hình chỉ là ô cửa nhìn vào canvas.**

```
        canvas 1920 × 3840
y=0     ┌─────────┐
        │    A    │   ← window 33554433, màn TRÊN nhìn vùng này
y=1920  ├─────────┤
        │    B    │   ← window 33554434, màn DƯỚI nhìn vùng này
y=3840  └─────────┘
```

Hệ quả:

1. Kéo window = **đổi `Rect`**, không phải "chuyển nội dung sang màn khác"
2. Một window nằm vắt qua nhiều màn là **chuyện bình thường**, không phải trường hợp đặc biệt
3. Số window **không liên quan** số màn. `maxWindowNums = 512` là trần cho **cả tường**
4. ⭐ **Window chồng nhau thì `layerIdx` lớn hơn nằm trên.** Kéo một window to ra mà không thấy đổi gì → nhiều khả năng có window khác **đè lên**. Xoá nó, hoặc `PUT .../windows/<id>/top`
5. 🔴 **API không có khái niệm "màn hình đang chiếu gì".** Phải tự đối chiếu `Rect` của window với vùng toạ độ của màn lấy từ D.3

---

## E.2. `PUT /ISAPI/DisplayDev/VideoWall/<vwID>/windows/<VWMWID>` ✅

*Mục 9.7.11.6 — "Set parameters of a specific window"*
⭐ **Test quan trọng nhất:** xác nhận toạ độ vượt được 1920.

### Request đã dùng

```
PUT {{base}}/ISAPI/DisplayDev/VideoWall/2/windows/33554433
Content-Type: application/xml
```

```xml
<?xml version="1.0" encoding="UTF-8"?>
<WallWindow xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <id>33554433</id>
  <wndOperateMode>uniformCoordinate</wndOperateMode>
  <Rect>
    <Coordinate><x>0</x><y>0</y></Coordinate>
    <width>1920</width>
    <height>3840</height>
  </Rect>
</WallWindow>
```

### Response

```xml
<statusCode>1</statusCode>
<statusString>OK</statusString>
<subStatusCode>ok</subStatusCode>
```

### 🔴 KẾT LUẬN: tài liệu SAI về giới hạn toạ độ

Tài liệu ghi `Rect` có `range:[0,1920]` cho x, y, width, height.

**Thực tế: `height = 3840` được chấp nhận, `statusCode 1 / OK`, và hiển thị đúng trên màn thật.**

→ **Giá trị vượt 1920 hoàn toàn hợp lệ.** Bằng chứng độc lập thứ hai: `VirtualLEDCap.Rect` cho phép tới `38400 × 30720` = 20 cột × 16 hàng.

### Ví dụ đặt window

| Muốn | `Rect` |
|---|---|
| Phủ màn trên | `x=0, y=0, w=1920, h=1920` |
| Phủ màn dưới | `x=0, y=1920, w=1920, h=1920` |
| Phủ cả tường | `x=0, y=0, w=1920, h=3840` |
| Nửa trên màn trên | `x=0, y=0, w=1920, h=960` |

### ⚠️ Lưu ý của request này

1. ⭐ **Bắt buộc gửi `wndOperateMode`.** Nó quyết định `Rect` được hiểu theo hệ nào. Bỏ trống → `badParameters`.
2. **Gửi payload tối thiểu** — `id` + `wndOperateMode` + `Rect`. Đừng gửi `layerIdx` hay `SubWindowList` nếu không đổi chúng.
3. ⚠️ **Hiện tượng quan sát được:** sau khi kéo window trên phủ cả tường, **màn trên "giãn" nhưng màn dưới không đổi**. Nguyên nhân: window dưới (`layerIdx` cao hơn) **vẫn còn và đè lên**. Phải `DELETE` nó, hoặc `PUT .../windows/33554433/top`.
4. Muốn dùng pixel thật thì đổi `wndOperateMode = resolutionCoordinate` và dùng khối `<ResolutionRect>` — với tường này là `1920 × 2160`. Miền giá trị theo B.1: `0–65535`.

---

# F. SCREEN CONTROL

## F.1. `PUT /ISAPI/DisplayDev/ScreenCtrl/closeAll` ❌ `invalidOperation`

*Mục 9.7.8.1 — "Close all screens"*

### Request body (đã parse OK)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<ScreenCtrl xmlns="http://www.isapi.org/ver20/XMLSchema" version="2.0">
  <OutputID>3</OutputID>
</ScreenCtrl>
```

### Response

```xml
<statusCode>4</statusCode>
<statusString>Invalid Operation</statusString>
<subStatusCode>invalidOperation</subStatusCode>
```

### 🔴 "Tắt" ở đây nghĩa là TẮT NGUỒN MÀN HÌNH, không phải ngắt tín hiệu

Bằng chứng từ mục 9.1.8: `ScreenCtrlInfo` chỉ có hiệu lực khi cổng serial đặt `workMode = "screenCtrl"`, và nó chứa:

```xml
<ScreenCtrlProto>
  <id>0</id>                   <!-- serial port ID -->
  <protoDesc>...</protoDesc>   <!-- protocol của hãng màn hình -->
</ScreenCtrlProto>
```

→ Controller **gửi lệnh tắt qua cổng RS-232/485** tới màn hình theo giao thức riêng của từng hãng — như bấm nút nguồn trên điều khiển.

### Điều kiện để hoạt động

1. Có **dây RS-232/485** nối controller ↔ màn hình (thường daisy-chain)
2. Cổng serial cấu hình `workMode = screenCtrl`
3. Chọn đúng **protocol của hãng màn hình**

⚠️ **Máy này chưa đủ điều kiện.** B.2 báo `serialPortList` cả 2 cổng `status: "unknown"` — chưa cắm, chưa cấu hình. Đây là nguyên nhân `invalidOperation`.

### 📌 Trường body — sửa lại ghi chú trong Excel

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `VideoWallID` | **opt**, int | Tường cần tắt |
| `OutputID` | **opt**, int | Màn cần tắt |

🔴 **Cả hai đều `opt`** → API tên `closeAll` nhưng **tắt được từng màn riêng lẻ**:

- Không gửi gì → tắt tất cả
- Gửi `VideoWallID` → tắt cả tường đó
- Gửi `OutputID` → tắt đúng 1 màn

→ Ghi chú *"không có API tắt từng màn"* trong Excel **cần sửa**. Cần test lại sau khi có dây serial.

### ⚠️ Lưu ý của request này

1. ⛔ **KHÔNG có API bật lại.** Sau khi tắt phải bật bằng tay hoặc remote.
2. ⚠️ Khi đã đấu dây, lệnh **không có `OutputID`** sẽ tắt **toàn bộ tường**. Luôn gửi `OutputID` khi test.
3. ⚠️ **Không có API đọc trạng thái bật/tắt.** `ScreenCtrl` một chiều.
4. Nghi vấn thứ hai về `invalidOperation`: `OutputID = 3` có thể sai — output phải **đã được gán lên tường**. Test lại bằng body rỗng-cấu-trúc `<ScreenCtrl .../>` để tách hai nguyên nhân.
5. 💡 **Tin tốt cho BE:** thiết bị **báo lỗi thật** thay vì trả OK rồi không làm gì. Phản hồi của `closeAll` đáng tin.

### Đọc sức khỏe màn hình — chỉ có một đường

Mục **5.9 Serial Port Data Transparent Transmission**: biến cổng serial thành đường ống trong suốt, gửi thẳng lệnh gốc của hãng màn:

```
1. PUT /ISAPI/System/Serial/ports/<portID>                        → workMode = transparent
2. PUT /ISAPI/System/Serial/ports/<portID>/Transparent/channels/<chID>
3. PUT .../Transparent/channels/<chID>/open
4. PUT .../Transparent/channels/<chID>/transData     ← gửi lệnh hỏi nhiệt độ
5. GET .../Transparent/channels/<chID>/transData     ← đọc phản hồi
6. PUT .../Transparent/channels/<chID>/close
```

⚠️ Half-duplex — gửi xong mới đọc được.
⚠️ **Một cổng serial chỉ có MỘT `workMode`.** Chọn `transparent` thì **mất `screenCtrl`** (mất `closeAll`).

---

# G. REQUEST LỖI — DÙNG LÀM MẪU DEBUG

`subStatusCode` chỉ thẳng vào chỗ cần sửa. **Luôn đọc `subStatusCode`, đừng đọc `statusString`.**

## G.1. `methodNotAllowed` — URL đúng, sai method

```
GET {{base}}/ISAPI/DisplayDev/ScreenCtrl/closeAll
→ statusCode 4 · Invalid Operation · methodNotAllowed
```

✅ **Lỗi này là tin tốt** — nó chứng minh URL **tồn tại** trên firmware. URL sai sẽ là 404 hoặc `notSupport`.
→ Đổi sang PUT.

## G.2. `badXmlFormat` — method đúng, body sai

```
PUT {{base}}/ISAPI/DisplayDev/ScreenCtrl/closeAll   (không có body)
→ statusCode 5 · Invalid XML Format · badXmlFormat
```

Nguyên nhân: body trống, hoặc Body tab chưa chọn `raw` + `XML`, hoặc file XML lưu dạng **UTF-8 with BOM** (3 byte vô hình ở đầu).

## G.3. `badParameters` — XML parse được, giá trị sai

```
PUT {{base}}/ISAPI/DisplayDev/VideoWall/2   (PUT lại nguyên response GET)
→ statusCode 6 · Invalid XML Content · badParameters
```

Hai nguyên nhân, cùng một gốc:

1. **Element rỗng** — `<wndOperateMode></wndOperateMode>`: thiết bị chờ enum, nhận chuỗi rỗng
2. **Trường chỉ-đọc / giá trị tính toán** — `layerIdx`, `videoInputChannelID`

→ Gửi **tối thiểu**, chỉ field cần đổi.

## G.4. `invalidOperation` — ID không tồn tại

```
GET {{base}}/ISAPI/DisplayDev/Video/outputs/channels/3
→ statusCode 4 · Invalid Operation · invalidOperation
```

🔴 `channelID` **không phải** 1..8. Phải gọi C.1 lấy ID thật (`17235971`…).

## G.5. `invalidOperation` — tham số không đúng đặc tả

```
GET {{base}}/ISAPI/System/configurationData?secretkey=test&mode=all
→ statusCode 4 · Invalid Operation · invalidOperation
```

Tài liệu mô tả rất sơ sài:

| Tham số | Mô tả trong tài liệu |
|---|---|
| `secretkey` | *"It should be **encrypted** for exporting"* — không nói thuật toán gì |
| `mode` | enum — **mô tả để trống `--`**, không liệt kê giá trị nào |

→ `secretkey=test` sai vì phải là chuỗi **đã mã hoá**. `mode=all` là phỏng đoán.
→ **Dùng giao diện web để export cấu hình:** Maintenance and Security → Maintenance → Export. Hoặc lấy tham số thật bằng DevTools.

## Bảng tổng hợp mã lỗi

### `statusCode`

| Mã | Nghĩa |
|---|---|
| **0, 1** | OK (**cả hai** đều thành công) |
| 2 | Device Busy |
| 3 | Device Error |
| 4 | Invalid Operation |
| 5 | Invalid XML Format |
| 6 | Invalid XML Content |
| 7 | Reboot Required |

### Chuỗi debug chuẩn khi test một API ghi

| Bước | `subStatusCode` | Nghĩa |
|---|---|---|
| GET vào endpoint chỉ nhận PUT | `methodNotAllowed` | **URL đúng**, sai method |
| PUT không body | `badXmlFormat` | Method đúng, body trống/không parse được |
| PUT body có element rỗng | `badParameters` | XML parse được, **giá trị** sai |
| PUT đúng | `ok` + `statusCode 1` | ✅ Xong |
| Gọi ID không tồn tại | `invalidOperation` | Dùng list endpoint lấy ID thật |
| Firmware không hỗ trợ | `notSupport` | Gọi `capabilities` trước |

⚠️ `requestURL` trong `ResponseStatus` luôn trả về **rỗng** trên firmware này — không dùng để đối chiếu request nào lỗi.

---

# H. BẢNG TỔNG HỢP ID

## H.1. ⭐ Công thức ID

Hikvision dùng **ID tổ hợp đóng gói theo byte**, không phải số thứ tự:

```
id = 0x0T000000 | (boardID << 16) | portID
```

Dạng thập phân: `T × 16777216 + boardID × 65536 + portID`

Byte đầu `T` là **mã loại tài nguyên**:

| Loại | Prefix | Ví dụ | Giải mã |
|---|---|---|---|
| Video channel | `0x01` | `17235971` = `0x01070003` | board 7, port 3 |
| Window | `0x02` | `33554433` = `0x02000001` | window #1 |
| Layer (`layerIdx`) | `0x04` | `67108865` = `0x04000001` | layer #1 |

Kiểm chứng: board 7, port 3 → `16777216 + 7×65536 + 3` = **17235971** ✓
Input board 1, port 1 → `16777216 + 1×65536 + 1` = **16842753** ✓

⚠️ **Dùng công thức để HIỂU, nhưng BE phải ĐỌC ID từ API.** Chưa xác nhận công thức đúng với board LED (`20NO`, `16NO/2FO`) hay board decode (`DEC`).

## H.2. Bảng ID đầy đủ của thiết bị này

### Output (8 cổng)

| `id` | Hex | Board | Port | Tên | Có màn? |
|---|---|---|---|---|---|
| 17235969 | `0x01070001` | 7 | 1 | Output 7-1 | — |
| 17235970 | `0x01070002` | 7 | 2 | Output 7-2 | — |
| **17235971** | `0x01070003` | 7 | 3 | Output 7-3 | 🟢 |
| **17235972** | `0x01070004` | 7 | 4 | Output 7-4 | 🟢 |
| 17301505 | `0x01080001` | 8 | 1 | Output 8-1 | — |
| 17301506 | `0x01080002` | 8 | 2 | Output 8-2 | — |
| 17301507 | `0x01080003` | 8 | 3 | Output 8-3 | — |
| 17301508 | `0x01080004` | 8 | 4 | Output 8-4 | — |

### Input (8 cổng — suy từ công thức, ⚠️ chưa xác minh bằng API)

| `id` (dự đoán) | Hex | Board | Port |
|---|---|---|---|
| **16842753** | `0x01010001` | 1 | 1 | ✅ đã xác nhận (đang dùng trong window) |
| 16842754 | `0x01010002` | 1 | 2 | |
| 16842755 | `0x01010003` | 1 | 3 | |
| 16842756 | `0x01010004` | 1 | 4 | |
| 16908289 | `0x01020001` | 2 | 1 | |
| 16908290 | `0x01020002` | 2 | 2 | |
| 16908291 | `0x01020003` | 2 | 3 | |
| 16908292 | `0x01020004` | 2 | 4 | |

⚠️ **Phải xác minh bằng `GET /ISAPI/DisplayDev/Video/inputs/channels`** — request này **chưa chạy lần nào**.

### Tường và window

| Loại | ID | Ghi chú |
|---|---|---|
| Wall 1 | `1` | `unbound` — **sandbox test an toàn** |
| Wall 2 | `2` | `bound` — chứa 2 màn |
| Window trên | `33554433` | `Rect(0, 0, 1920, 1920)`, layerIdx `67108865` |
| Window dưới | `33554434` | `Rect(0, 1920, 1920, 1920)`, layerIdx `67108866` |
| WallOutput | `2`, `3` | Trỏ tới output `17235971`, `17235972` |

---

# PHỤ LỤC — 12 điểm khác biệt với tài liệu

Tổng hợp mọi chỗ 🔴 trong tài liệu này:

| # | Tài liệu nói | Thực tế |
|---|---|---|
| 1 | `Rect range:[0,1920]` | **Sai.** `height=3840` được chấp nhận, hiển thị đúng |
| 2 | `userCheck` có `lockStatus`, `unlockTime`, `retryLoginTime` | **Không trả về** → BE không phát hiện được khóa IP |
| 3 | URL `/decoingDevice/status` | **Đúng là sai chính tả** (thiếu `d`) — firmware sai giống hệt |
| 4 | Trường `backgroudColor` | **Đúng là sai chính tả** (thiếu `n`) |
| 5 | Không mô tả `outputPortAccessStatus` | Firmware **có** trường này — thứ tốt nhất để đọc trạng thái màn |
| 6 | `outputPortEnabled` như công tắc output | **Không phải.** Không bật/tắt được cổng HDMI |
| 7 | `SubBoardInterfaceList` có `signalStatus`, `decodeWallStatus`, `row`, `col` | **Không trả về** |
| 8 | `WallWindow` có `ResolutionRect`, `wndTopKeep`, `displayWinNo`… | **Không trả về** |
| 9 | `closeAll` — Excel ghi "không tắt được từng màn" | Body nhận `OutputID` (`opt`) → **tắt được từng màn** |
| 10 | `configurationData` với `secretkey`/`mode` | Đặc tả không đủ để dùng — `mode` không liệt kê giá trị |
| 11 | `videoInputPortNums` / `videoOutputPortNums` = 24 | **Trần khung máy**, thực tế 8/8. `audioInputsPortNums=200` là bằng chứng |
| 12 | `GetVideoWallScale`, `POST .../outputs`, `POST .../scene` | Chỉ có ở luồng chương 8, **không có mục đặc tả** — capabilities xác nhận là có thật |

---

# API CHƯA CHẠY — cần bổ sung vào tài liệu này

| API | Vì sao cần |
|---|---|
| `GET /ISAPI/DisplayDev/Video/inputs/channels` | ⭐ Xác minh bảng ID input ở H.2. Nền cho panel "Nguồn tín hiệu" |
| `GET /ISAPI/System/deviceInfo` | `model`, `firmwareVersion`, `serialNumber` — cần để so 4 controller |
| `GET /ISAPI/System/Board/status/capabilities` | Nhiệt độ **từng module** (chi tiết hơn backplane) |
| `GET /ISAPI/DisplayDev/VideoWall/capabilities` | Đã đọc qua `DisplayDev/capabilities`, nên gọi riêng để đối chiếu |
| `GET /ISAPI/DisplayDev/VideoWall/<id>/windows/status` | Excel ghi là *"API giám sát lõi màn Monitoring"*, poll 3–5s. **Cần đo response nặng bao nhiêu** |
| `GET /ISAPI/DisplayDev/Video/outputs/channels/<id>/capabilities` | Danh sách độ phân giải hỗ trợ (`OutputResolutionCapList` ở cấp tổng bị rỗng) |
| `POST /ISAPI/DisplayDev/VideoWall/<id>/windows` | ⭐ **Tạo window** — thao tác lõi #1 của FE, chưa test |
| `PUT .../windows/<VWMWID>/sub/<WMSWID>` | ⭐ **Đổi nguồn** — thao tác "kéo nguồn thả vào cửa sổ" |
| `PUT .../windows/<id>/top` và `/bottom` | Z-order |
| `POST .../VideoWall/<id>/outputs` | 🔴 **Gán output vào tường** — API còn thiếu quan trọng nhất |
| `POST .../VideoWall/GetVideoWallScale?format=json` | Capabilities đã xác nhận `true` |
| `GET /ISAPI/System/Serial/capabilities` và `/ports` | Điều kiện cho `closeAll` |

---

*Toàn bộ dữ liệu trong tài liệu này đo thực tế trên `10.10.9.236` (`DS-C66S-H88-CL`, SN `GW2405704`). Đối chiếu với: ISAPI Controller – Videowall Controller (mục 2.1.1, 3.3, 4.2, 5.9, 8.2.2, 9.1.8, 9.1.12.1, 9.7.x) và DS-C66S Series Datasheet (Hikvision, 16/09/2025).*
