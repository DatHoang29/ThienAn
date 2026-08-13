# ISAPI — Videowall Controller (DS-C30S / DS-C60S / DS-C66S)

Markdown conversion of the *ISAPI Controller — Videowall Controller* developer guide (512 pages). Text, tables and XML/JSON payloads are reproduced from the original PDF; figures are extracted to `images/`, and flow / sequence figures are additionally redrawn as Mermaid diagrams next to the original image.

## Chapters

| # | Chapter | File | Size |
| --- | --- | --- | --- |
| 1 | Reading Guide | [01-reading-guide.md](01-reading-guide.md) | 1 KB |
| 2 | Overview | [02-overview.md](02-overview.md) | 3 KB |
| 3 | ISAPI Framework | [03-isapi-framework.md](03-isapi-framework.md) | 8 KB |
| 4 | Quick Start Guide | [04-quick-start-guide.md](04-quick-start-guide.md) | 60 KB |
| 5 | Device Management (General) | [05-device-management.md](05-device-management.md) | 64 KB |
| 6 | Information Security | [06-information-security.md](06-information-security.md) | 6 KB |
| 7 | Video (General) | [07-video-general.md](07-video-general.md) | 12 KB |
| 8 | Decoding and Video Wall | [08-decoding-and-video-wall.md](08-decoding-and-video-wall.md) | 7 KB |
| 9 | API Reference | [09-api-reference.md](09-api-reference.md) | 1,632 KB |
| 10 | How-To Video Guidance | [10-how-to-video-guidance.md](10-how-to-video-guidance.md) | 1 KB |

## Notes

- Chapter 9 (API Reference) holds the bulk of the document: request URLs, query parameters, and annotated XML/JSON request & response messages for every endpoint.
- Inline monospaced text from the PDF is rendered as `inline code`; full payloads are fenced blocks tagged `xml`, `json` or `http`.
- The distributor watermark present on every PDF page has been stripped.
- 24 Mermaid diagrams were added; screenshots and photographs are kept as images only.

## Figure index

| Figure | Source page | File | Mermaid |
| --- | --- | --- | --- |
| 1 | p.1 | [`images/fig-01-p001.png`](images/fig-01-p001.png) | — |
| 2 | p.3 | [`images/fig-02-p003.png`](images/fig-02-p003.png) | — |
| 3 | p.4 | [`images/fig-03-p004.png`](images/fig-03-p004.png) | — |
| 4 | p.5 | [`images/fig-04-p005.png`](images/fig-04-p005.png) | — |
| 5 | p.6 | [`images/fig-05-p006.png`](images/fig-05-p006.png) | ✅ Device activation handshake |
| 6 | p.16 | [`images/fig-06-p016.png`](images/fig-06-p016.png) | ✅ Real-time live view via RTSP |
| 7 | p.19 | [`images/fig-07-p019.png`](images/fig-07-p019.png) | — |
| 8 | p.26 | [`images/fig-08-p026.png`](images/fig-08-p026.png) | ✅ Arming with subscription |
| 9 | p.28 | [`images/fig-09-p028.png`](images/fig-09-p028.png) | ✅ Listening — event message upload |
| 10 | p.29 | [`images/fig-10-p029.png`](images/fig-10-p029.png) | ✅ Listening host configuration |
| 11 | p.32 | [`images/fig-11-p032.png`](images/fig-11-p032.png) | ✅ Device packet capture |
| 12 | p.33 | [`images/fig-12-p033.png`](images/fig-12-p033.png) | ✅ Real-time packet capture |
| 13 | p.34 | [`images/fig-13-p034.png`](images/fig-13-p034.png) | ✅ Packet capture calling flow |
| 14 | p.35 | [`images/fig-14-p035.png`](images/fig-14-p035.png) | ✅ Peripheral upgrade |
| 15 | p.36 | [`images/fig-15-p036.png`](images/fig-15-p036.png) | ✅ NTP time synchronization principle |
| 16 | p.38 | [`images/fig-16-p038.png`](images/fig-16-p038.png) | ✅ Synchronize time via an NTP server |
| 17 | p.39 | [`images/fig-17-p039.png`](images/fig-17-p039.png) | ✅ Configure the device as an NTP server |
| 18 | p.40 | [`images/fig-18-p040.png`](images/fig-18-p040.png) | ✅ Device upgrade |
| 19 | p.42 | [`images/fig-19-p042.png`](images/fig-19-p042.png) | — |
| 20 | p.45 | [`images/fig-20-p045.png`](images/fig-20-p045.png) | ✅ Serial port transparent transmission |
| 21 | p.51 | [`images/fig-21-p051.png`](images/fig-21-p051.png) | — |
| 22 | p.51 | [`images/fig-22-p051.png`](images/fig-22-p051.png) | — |
| 23 | p.52 | [`images/fig-23-p052.png`](images/fig-23-p052.png) | ✅ User types and their login entries |
| 24 | p.54 | [`images/fig-24-p054.png`](images/fig-24-p054.png) | ✅ User management |
| 25 | p.55 | [`images/fig-25-p055.png`](images/fig-25-p055.png) | ✅ Cloud user management |
| 26 | p.56 | [`images/fig-26-p056.png`](images/fig-26-p056.png) | ✅ User permission management |
| 27 | p.58 | [`images/fig-27-p058.png`](images/fig-27-p058.png) | ✅ Certificate management |
| 28 | p.60 | [`images/fig-28-p060.png`](images/fig-28-p060.png) | ✅ Digital channel management |
| 29 | p.62 | [`images/fig-29-p062.png`](images/fig-29-p062.png) | — |
| 30 | p.62 | [`images/fig-30-p062.png`](images/fig-30-p062.png) | ✅ NVR request forwarding |
| 31 | p.63 | [`images/fig-31-p063.png`](images/fig-31-p063.png) | ✅ Transparent transmission via NVR |
| 32 | p.64 | [`images/fig-32-p064.png`](images/fig-32-p064.png) | — |
| 33 | p.64 | [`images/fig-33-p064.png`](images/fig-33-p064.png) | — |
| 34 | p.65 | [`images/fig-34-p065.png`](images/fig-34-p065.png) | ✅ Video wall scene configuration and switching |
| 35 | p.66 | [`images/fig-35-p066.png`](images/fig-35-p066.png) | — |
| 36 | p.67 | [`images/fig-36-p067.png`](images/fig-36-p067.png) | ✅ Window opening on video wall |
