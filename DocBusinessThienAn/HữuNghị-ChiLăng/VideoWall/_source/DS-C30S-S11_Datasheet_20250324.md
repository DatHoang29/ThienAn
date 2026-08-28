# DS-C30S-S11 — 11-Slot Video Wall Controller

> Nguồn: `DS-C30S-S11_Datasheet_20250324.pdf` (chuyển đổi sang Markdown, giữ nguyên nội dung gốc tiếng Anh).
> Phần "Dimension" trong PDF là bản vẽ kích thước (hình ảnh) nên không tái tạo ở đây.

---

## Features

### Hardware Structure

- Adopts the 4.5 U standard rack design and operational-grade ATCA chassis system.
- Adopts redundant power supply design, 4 intelligent fans for auto temperature adjustment, and 2 main control boards for expansion.
- Adopts the plug-in modular design and 11 slots for hot swappable service boards.
- Adopts the 4.3-inch non-touch screen panel to allow you to view the device status information at any time.
- Provides the indicator lights to allow you to view the device online status and operating status.

### Audio and Video Input

- Supports the video signal source input such as computers, video conference terminals, and ultra-high-definition (UHD) servers. Supports VGA, DVI, HDMI, 4K HDMI, and 4K DP signal input, and network signal source input such as network cameras and NVRs.
- Supports composite audio input and independent audio input. The audio input supports 16 bit, 48K Hz sampling, and dual channel.
- Supports YUV 444 in image collection and output with lossless image quality.
- Support ultra-high-definition fusion and up to sixteen 4K UHD signal access.
- Supports OSD on the input.
- Supports input image clipping to cut the black edge of the input image.

### Audio and Video Output

- Supports DVI, HDMI, and 4K HDMI video signal output and the video signal output via network ports.
- Supports composite audio output and independent audio output.

### Video Decoding

- Supports using the installed decoding board to decode the signal sources of network cameras and NVRs.
- Supports main stream encoding, sub stream decoding, auto-switching to sub stream, and decoding exception prompt.
- Supports up to 256 decoding channels, and simultaneous decoding of 128 channels of 2 MP video to the video wall when the device is fully installed with service boards.
- Supports the mainstream decoding formats such as H.264, H.265, Smart264, Smart265, and MJPEG, and mainstream encapsulation formats such as PS, TS, ES, RTP, and HIK.
- Support 16 MP HD video decoding.

### Video Wall Function

- Supports any large screen splicing of 40 screens when the device is fully installed with service boards.
- Supports window opening and floating windows.
- Support up to eight 4K signal source windows per screen and each signal source window can be divided into 1, 4, 6, 8, 9, and 16 windows.
- Supports displaying the image of a video wall on the connected screen(s) or previewing the image of a video wall on a client.
- Supports 8 background images. The resolution of each background image is 8K.
- Supports 8 video walls. Each video wall allows one background image.
- Supports up to 12 subtitles for the device, up to 3 subtitles for one video wall and configuration of different types of subtitles.
- Supports up to 128 scenes. You can customize the video wall layout and save it as a scene.
- Supports the auto-switching of up to 100 view groups via the HCP client. Supports auto-switching on a single window, on some windows, and on all windows. You can save all auto-switch resources in the scenes and customize the location, scene, and time in each view group.
- Supports double-clicking the sub-window to enlarge its window size and double-clicking the sub-window again to restore its original window size.
- Supports using the HCP client to capture images on the screen and display the captured images on the video wall when the decoding board is installed in the device.
- Supports the live view of network signal sources over RTP or RTSP.

### Device Access and Control

- Supports using the network keyboard or serial port keyboard to control the device, and to realize sub-window changing, group operation and auto-switching, scene changing, PTZ control, and video wall playback.
- Supports using the ONVIF protocol to access the network source devices for decoding.
- Supports using the software to control LCD screens, including screen switch, screen signal source changing, and the adjustment on brightness, contrast, color, sharpness, picture horizontal position, and picture vertical position.
- Supports using the software to control LED screens, including screen switch and screen signal source changing.
- Supports PTZ control and movement of the cameras.

### Maintenance Support

- Supports the access and operation via the control client and web client. The web browser should be IE 8, Chrome 45 and above.
- Supports the access and operation via the mobile client (Android or iOS).
- Supports NAT.
- Supports obtaining and configuring parameters remotely, importing parameters remotely, and exporting parameters remotely.
- Supports obtaining system running status and system logs remotely.
- Supports restarting the device remotely, restoring the default settings, and upgrading the device.
- Supports auto detection and alarm for failures and the device exception alarm function when the boards are online, including network disconnection, IP conflict, invalid access, temperature threshold exceeding, and fan exception.
- Supports user permission management. Different users are assigned with different permissions to use the specified resources and operate the specified video wall modules.
- Supports manual time sync or NTP time sync.

---

## Specification

### Chassis

| Item | Value |
|---|---|
| Chassis Height | 4.5 U |
| Bus Type | 10 GB network switching |
| Signal Sampling Quality | YUV 444 |
| Mixed Installation of Service Boards | Supported |
| Main Control Board Slot | 2 |
| Service Board Slot | 11 |
| Installed Main Control Boards | 1 |
| Max. Input Slot | 10 |
| Max. Output Slot | 10 |
| Power Supply Slot | 3 |
| Installed Power Supplies | 1 |
| Fans | 4 |
| Dual Device Hot Backup | Supported |

### Interface

| Item | Value |
|---|---|
| USB Interface No. | 2 × USB 2.0 |
| Serial Interface | 2 × Console port (RJ-45) + 1 × RS-485/RS-232 multiplex interface (RJ-45, baud rate: 115200, valid data bit: 8 bit) |
| Screen Type | 4.3 inch non-touch screen, length × width: 105.42 mm × 67.07 mm (4.15 inch × 2.64 inch), resolution: 480 × 272 |

### Power

| Item | Value |
|---|---|
| Power Interface | 100 VAC to 240 VAC, 50/60 Hz |
| Device Power Consumption | 550 W (full configuration) |

### Environment

| Item | Value |
|---|---|
| Working Temperature | 0 °C ~ 50 °C |
| Working Humidity | 10 ~ 90% |

### Network

| Item | Value |
|---|---|
| Management Network Interface | 2 × 10/100/1000 Mbps auto-sensing Ethernet interface (2 network ports on the switching board and 1 network port reserved on the main control board) |
| Transmission Protocol | SDK, RTSP, ONVIF |

### Video Wall

| Item | Value |
|---|---|
| Video Walls | 8 |
| Video Wall Scale | 40 |
| Split Window | Supported |
| Open Windows | 16 |
| Window Division per Output Port | 1, 4, 6, 8, 9, 16 |
| Input Source Copy Capability | Each output board can duplicate eight 2K images from the input source, but the LED controller board does not have copy capability. |
| Layers Per Port | 8 × 1080p layers or 4 × 4K layers |
| Layers per Device | 512 (fully installed with the output boards) |
| Scenes | 128 |
| Scene Auto-Switch Delay | 400 ms |
| Plans | 128 |
| Live View Resolution | 16-channel D1 or 32-channel CIF; 4-channel D1 or 16-channel CIF when previewing the image of a video wall on a client with all service boards installed in the device |
| UHD Fusions | 16 |
| Background Image | Total: 8; one background image on each video wall. Resolution: 16382 × 8192. Format: JGP, JPEG |
| Subtitles | Total: 12; single video wall: 3 |
| Input OSD | Supported |
| Input Image Clipping | Supported — 200 pixel points on top, bottom, left, and right. |
| Local Signal Source Decoding Delay | 50 ms |
| Network Signal Source Decoding Delay | 200 ms |
| Display Video Wall Image | Supported |

### General

| Item | Value |
|---|---|
| Net Weight | 33.39 kg (73.65 lb.) — full configuration, including 21.84 kg (48.16 lb.) chassis and 1.05 kg (2.31 lb.) for each service board |
| Gross Weight | 51.32 kg (113.13 lb.) — full configuration, including 29.1 kg (64.15 lb.) chassis and 2.02 kg (4.45 lb.) for each service board |
| Dimensions (W × H × D) | 442 mm × 207.8 mm × 447 mm (17.4 inch × 8.18 inch × 17.59 inch) |
| Packing List | 1 × grounding cable, 1 × audio adapter cable, 1 × serial port cable, 1 × AC power cord, 1 × power supply, 1 × regulatory compliance and safety information manual |

### Device Parameters

| Item | Value |
|---|---|
| Device Decoding Capability | 128 channels of 1080p 30 fps |
| Device Splicing Capability | 40 channels |

---

## Dimension

Bản vẽ kích thước (hình trong PDF) — không tái tạo. Kích thước tổng thể: **442 mm × 207.8 mm × 447 mm** (W × H × D).

---

## Accessory (Optional Modules)

| Model | Diễn giải (suy đoán theo quy ước đặt tên — cần đối chiếu tài liệu chính hãng) |
|---|---|
| DS-C30S-02DPI/4K | 2-port 4K DisplayPort input board |
| DS-C30S-02HI/4K | 2-port 4K HDMI input board |
| DS-C30S-04DI | 4-port DVI input board |
| DS-C30S-04HI | 4-port HDMI input board |
| DS-C30S-04VI | 4-port VGA input board |
| DS-C30S-04DO | 4-port DVI output board |
| DS-C30S-02HO/4K | 2-port 4K HDMI output board |
| DS-C30S-DEC | Decoding board |
| DS-C30S-L104 | LED controller board |
| DS-C30S-MCU | Main control board |
| DS-C30S-SW | Switching board |
