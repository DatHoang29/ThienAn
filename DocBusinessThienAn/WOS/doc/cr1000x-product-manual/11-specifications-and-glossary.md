---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 255-334
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 17–18 + Phụ lục A: Thông số mở rộng & Từ điển thuật ngữ khí tượng

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 255 đến 334).  
> **Chủ đề chính**: Bảng thông số kỹ thuật đầy đủ của dòng CR1000Xe và CR1000X, cùng toàn bộ phần giải thích thuật ngữ chuyên ngành đo lường khí tượng và truyền thông công nghiệp (Appendix A Glossary).

---


<!-- Page 255 -->
17. CR1000Xe specifications
Electrical specifications are valid over a -40 to +70 °C, non-condensing environment, unless 
otherwise specified. Extended electrical specifications (noted as XT in specifications) are valid 
over a -55 to +85 °C non-condensing environment. Recalibration is recommended every three 
years. Critical specifications and system configuration should be confirmed with Campbell 
Scientific before purchase.
See CR1000X specifications (p. 252) for CR1000X specifications.
17.1 System specifications 238
17.2 Physical specifications 239
17.3 Power requirements 239
17.4 CR1000Xe power output specifications 240
17.5 Analog measurement specifications 242
17.6 Pulse measurement specifications 247
17.7 Digital input/output specifications 248
17.8 Communications specifications 250
17.9 Standards compliance specifications 251
17.1 System specifications
Processor: Renesas RX63N (32-bit with hardware FPU, running at 100 MHz)
Memory (see Data memory (p. 88) for more information):
- Total onboard: 128 MB of flash + 4 MB battery-backed SRAM
- Data storage: 4 MB SRAM + 72 MB flash (extended data storage automatically used 
for auto-allocated Data Tables not being written to a card)
- CPU drive: 30 MB flash
- OS load: 8 MB flash
- Settings: 1 MB flash
- Reserved (not accessible): 10 MB flash
- Data storage expansion: Removable microSD flash memory, up to 16 GB
Program Execution Period: 1 ms to 1 day
17. CR1000Xe specifications     238

<!-- Page 256 -->
Real-Time Clock:
- Battery backed while external power is disconnected
- Resolution: 1 ms
- Accuracy: ±3 min. per year, optional GPS correction to ±10 µs
Wiring Panel Temperature: Measured using a 10K3A1A BetaTHERM thermistor, located between 
the two rows of analog input terminals.
17.2 Physical specifications
Dimensions: 23.8 x 10.1 x 6.2 cm (9.4 x 4.0 x 2.4 in); additional clearance required for cables and 
wires.  For CAD files, see CR1000X Images and CAD 2D Drawings.
Weight/Mass: 0.86 kg (1.9 lb)        
Case Material: Powder-coated aluminum
Operating Temperature Range: 
- -40 °C to +70 °C (standard)
- -55°C to +85 °C (extended)
Storage Temperature Range: -60 to 85 °C
Operating Humidity: 0 to 100 %RH, non-condensing environment
17.3 Power requirements
Protection: Power inputs are protected against surge, over-voltage, over-current, and reverse 
power. IEC 61000-4 Class 4 level.
Power In Terminal:
- Supply Voltage: 10 to 36 VDC
- Sustained Supply Voltage without Damage: 38 VDC
Vehicle Power Connection: When primary power is pulled from the vehicle power system, a 
second power supply OR charge regulator may be required to overcome the voltage drop at 
vehicle start-up.
USB Power: Functions that will be active with USB 5 VDC applied include sending programs, 
adjusting data logger settings, and making some measurements. If USB is the only power source, 
then the CS I/O port and the 5V, 12V, and SW12 terminals will not be operational. When powered 
by only USB (no other power supplies connected) Status table field Battery = 0.
17. CR1000Xe specifications     239

<!-- Page 257 -->
Internal Lithium Battery: AA, 2.4 Ah, 3.6 VDC (Tadiran TL 5903/S) for battery-backed SRAM and 
clock. 3-year life with no external power source.   See also Internal battery (p. 154).
Average Current Consumption (typ. at 20 °C):
Operating state 12 V Supply voltage 24 V Supply voltage
Idle 2.0 mA <1.0 mA
Active 1 Hz Scan 2.0 mA 1.1 mA
Active 20 Hz Scan 57 mA 36 mA
Serial (RS-232/RS-485) Active + 25 mA Active + 16 mA
Ethernet Power Requirements:    
Ethernet 1 Minute Active + 1 mA Active + 0.7 mA
Ethernet Idle Active + 4 mA Active + 2.6 mA
Ethernet Link Active + 47 mA Active + 31 mA
17.4  CR1000Xe power output specifications
17.4.1 System power output current limits
Temperature (°C)
12 V Supply voltage
Current limit1 (A)
24 V Supply voltage
Current limit1 (A)
–55° 3.4 4.4
–40° 3.4 4.4
20° 3.4 4.4
70° 2.5 4.2
85° 2.1 4.0
1 Limited by self-resetting thermal fuse and maximum regulator output current. 
17.4.2 Shared 12 V and SW12 power output
12V, SW12-1, and SW12-2 provide regulated 12 VDC power. These outputs are disabled when 
operating on only USB power. 
17. CR1000Xe specifications     240

<!-- Page 258 -->
Temperature (°C)
12 V Supply voltage
Current limit1 (A)
24 V Supply voltage
Current limit1 (A)
–55° 3.3 3.3
–40° 3.3 3.3
20° 3.3 3.3
70° 2.5 3.3
85° 2.1 3.3
1 Limited by self-resetting electronic and thermal fuses.
17.4.3 Individual maximum current for 12 V and SW12 
output terminals
Regulated 12 V output. System power output current limits may override one or more of these 
individual limits. These outputs are disabled when operating on only USB power.
- Voltage Output: Regulated 12 V output (±5%)
- Current Limit: 2000 mA
17.4.4 5 V fixed output 
Regulated 5 V output. Supply is shared between the 5V terminal and CS I/O DB9 5 V output.
- Voltage Output: Regulated 5 V output (±5%)
- Current Limit: 230 mA
17.4.5 Control port as power output
Operating at the current limit is OK if voltage fluctuation can be tolerated. Drive capacity is 
determined by the logic level of the VDC supply and the output resistance (Ro) of the C terminal. 
It is expressed as: Vo = 5 V – (Ro • Io), where Vo is the drive limit, and Io is the current required by 
the external device. For example: at the maximum current limit of 10 mA on C1 the voltage level 
would reduce from 5 V to 3.5 V.
- C Terminals:
- Output Resistance (Ro): 150 Ω
- 5 V Logic Level Drive Capacity: 10 mA @ 3.5 VDC; Vo = 5 V - (150 Ω •Io)
- 3.3 V Logic Level Drive Capacity: 10 mA @ 1.8 VDC; Vo = 3.3 V - (150 Ω •Io)
17. CR1000Xe specifications     241

<!-- Page 259 -->
17.4.6 CS I/O pin 1: 5 V fixed output
Regulated 5 V output. Supply is shared between the 5V terminal and CS I/O DB9 5 V output.
- Voltage Output: Regulated 5 V output (±5%)
- Current Limit: 230 mA
17.4.7 CS I/O pin 8: 12 V switched output
Regulated 12 V output. Power output shared with system power output. This output is disabled 
when operating on only USB power.
- Voltage Output: Regulated 12 V output (±5%)
- Current Limit: 800 mA
17.4.8 Voltage excitation
VX: Four independently configurable voltage terminals (VX1-VX4).  When providing voltage 
excitation, a single 16-bit DAC shared by all VX outputs produces a user-specified voltage during 
measurement only. In this case, these terminals are regularly used with resistive-bridge 
measurements (see Resistance measurements [p. 103] for more information). VX terminals can 
also be used to supply a selectable, switched, regulated 3.3 or 5 VDC power source to power 
digital sensors and toggle control lines.
  Range Resolution Accuracy Maximum source/sink 
current1
Voltage 
Excitation ±4 V  0.12 mV ±(0.1% of setting 
+ 2 mV) ±40 mA
Switched, 
Regulated +3.3  or 5 V 3.3 or 5 V ±5% 50 mA
1 Exceeding current limits causes voltage output to become unstable. Voltage should stabilize when current is 
reduced to within stated limits.
17.5 Analog measurement specifications
16 single-ended (SE) or 8 differential (DIFF) terminals individually configurable for voltage, 
thermocouple, current loop, ratiometric, and period average measurements, using a 24-bit ADC. 
One channel at a time is measured.
17. CR1000Xe specifications     242

<!-- Page 260 -->
17.5.1 Voltage measurements
Terminals:
- Differential Configuration: DIFF 1H/1L – 8H/8L
- Single-Ended Configuration: SE1 – SE16
Input Resistance: 20 GΩ typical
Input Voltage Limits: ±5 V
Sustained Input Voltage without Damage: ±20 VDC
DC Common Mode Rejection:
- >120 dB with input reversal
- ≥ 86 dB without input reversal
Normal Mode Rejection: > 70 dB @ 60 Hz
Input Current @ 25 °C: ±1 nA typical
Filter First Notch Frequency (fN1) Range: 0.5 Hz to 31.25 kHz (user specified)
Analog Range and Resolution:
  Differential with input 
reversal
Single-ended and 
differential without input 
reversal
Notch 
frequency
(fN1) (Hz)
Range1
(mV)
RMS
(µV) Bits2 RMS
(µV) Bits2
15000
±5000
±1000
±200
8.2
1.9
0.75
20
20
19
11.8
2.6
1.0
19
19
18
50/603
±5000
±1000
±200
0.6
0.14
0.05
24
23
22
0.88
0.2
0.08
23
23
22
17. CR1000Xe specifications     243

<!-- Page 261 -->
  Differential with input 
reversal
Single-ended and 
differential without input 
reversal
Notch 
frequency
(fN1) (Hz)
Range1
(mV)
RMS
(µV) Bits2 RMS
(µV) Bits2
5
±5000
±1000
±200
0.18
0.04
0.02
25
25
24
0.28
0.07
0.03
25
24
23
1 Range overhead of ~5% on all ranges guarantees that full-scale values will not cause over range
2 Typical effective resolution (ER) in bits; computed from ratio of full-scale range to RMS resolution.
3 50/60 corresponds to rejection of 50 and 60 Hz ac power mains noise.
Accuracy (does not include sensor or measurement noise):
- 0 to 40 °C: ±(0.04% of measurement + offset)
- –40 to 70 °C: ±(0.06% of measurement + offset)
Voltage Measurement Accuracy Offsets:
  Typical offset (µV RMS)
Range (mV) Differential
with input reversal
Single-ended or differential
without input reversal
±5000 ±0.5 ±2
±1000 ±0.25 ±1
±200 ±0.15 ±0.5
Measurement Settling Time: 20 µs to 600 ms; 500 µs default
Multiplexed Measurement Time:
These are not maximum speeds. Multiplexed denotes circuitry inside the data logger that 
switches signals into the ADC.
Where:
M = 1 (default)
M = 2 if reverse differential or measurement offset is used
Setup Time = 150 µs
17. CR1000Xe specifications     244

<!-- Page 262 -->
  Differential
with input reversal
Single-ended or differential
without input reversal
Example fN11 (Hz) Time2 (ms) Time2 (ms)
15000 1.28 0.717
60 34.48 17.31
50 41.15 20.65
5 401.15 200.65
1 Notch frequency (1/integration time).
2 Default settling time of 500 µs used. 
See also Voltage measurements (p. 99).
17.5.2 Resistance measurement specifications
The data logger makes ratiometric-resistance measurements for four- and six-wire full-bridge 
circuits and two-, three-, and four-wire half-bridge circuits using voltage excitation. Excitation 
polarity reversal is available to minimize dc error. Typically, at least one terminal is configured for 
excitation output. Multiple sensors may be able to use a common excitation terminal.
Accuracy:
Assumes input reversal for differential measurements RevDiff and excitation reversal RevEx  
for excitation voltage <1000 mV. Does not include bridge resistor errors or sensor and 
measurement noise.
Ratiometric accuracy, rather than absolute accuracy, determines overall measurement accuracy. 
Offset is the same as specified for analog voltage measurements.
- 0 to 40 °C: ±(0.01% of voltage measurement + offset)
- –40 to 70 °C: ±(0.015% of voltage measurement + offset)
- –55 to 85 °C (XT): ±(0.02% of voltage measurement + offset)
17.5.3 Period-averaging measurement specifications
Use PeriodAvg() to measure the period (in microseconds) or the frequency (in Hz) of a signal 
on a single-ended channel. 
Terminals: SE1-SE16
Accuracy: ±(0.01% of measurement + resolution), where resolution is 0.13 µs divided by the 
number of cycles to be measured
17. CR1000Xe specifications     245

<!-- Page 263 -->
Ranges:
- Minimum signal centered around specified period average threshold.
- Maximum signal centered around data logger ground.
- Maximum frequency = 1/(2 * [minimum pulse width]) for 50% duty cycle signals
Gain
code
option
Voltage
gain
Minimum
peak to peak
signal (mV)
Maximum
peak to peak
signal (V)
Minimum
pulse width (µs)
Maximum
frequency (kHz)
0 1 500 10 2.5 200
1 2.5 50 2 10 50
2 12.5 10 2 62 8
3 64 2 2 100 5
See also Period-averaging measurements (p. 111).
17.5.4 Current-loop measurement specifications
The data logger makes current-loop measurements by measuring across a current-sense resistor 
associated with the RS-485 resistive ground terminal.
Terminals: RG1 and RG2
Sustained Input Voltage without Damage: ±13.1 V
Resistance to Ground: 101 Ω
Current Measurement Shunt Resistance: 10 Ω
Maximum Current Measurement Range: ±80 mA
Sustained Maximum Current without Damage: ±130 mA
Resolution: 
- ±1000 mV range: ≤ 20 nA
- ±200 mV range: ≤ 7.5 nA
Accuracy: ±(0.1% of reading + 100 nA) @ -40 to 70 °C
See also Current-loop measurements (p. 101).
17. CR1000Xe specifications     246

<!-- Page 264 -->
17.6 Pulse measurement specifications
Terminals individually configurable for switch closure, high-frequency pulse, or low-level AC 
measurements. See also Digital input/output specifications (p. 262). Each terminal has its own 
independent 24-bit counter.
NOTE:
Conflicts can occur when a control port pair is used for different instructions (TimerInput(), 
PulseCount(), SDI12Recorder(), WaitDigTrig()). For example, if C1 is used for 
SDI12Recorder(), C2 cannot be used for TimerInput(), PulseCount(), or 
WaitDigTrig().
Sustained Input Voltage without Damage: (P1-P2): ±20 VDC
Sustained Logic Input Voltage without Damage: (C1-C8): +16/-12 VDC
Maximum Counts Per Scan: 224
Input Resistance: 5 kΩ
Accuracy: ±(0.02% of reading + 1/scan)
17.6.1 Low-level AC input
Terminals: P1-P2
Minimum Pull-Down Resistance: 10 kΩ to ground
DC-offset rejection:  Internal AC coupling eliminates DC-offset voltages up to ±0.05 VDC
Input Hysteresis: 12 mV at 1 Hz
Low-Level AC Pulse Input Ranges:
Sine wave (mV RMS) Range (Hz)
20 1.0 to 20
200 0.5 to 200
2000 0.3 to 10,000
5000 0.3 to 20,000
17.6.2 Switch closure input
Terminals: C1-C8, P1-P2
Pull-Up Resistance: 100 kΩ to 5 V
17. CR1000Xe specifications     247

<!-- Page 265 -->
Event: Low (<0.8 V) to High (>2.5 V)
Maximum Input Frequency: 100 Hz
Minimum Switch Closed Time: 5 ms
Minimum Switch Open Time: 5 ms
Maximum Bounce Time: 1 ms open without being counted 
17.6.3 High-frequency input
Terminals: C1-C8, P1-P2
Pull-Up Resistance: 100 kΩ to 5 V
Event: Low (<0.8 V) to High (>2.5 V)
Maximum Input Frequency: 250 kHz
17.7 Digital input/output specifications
Terminals configurable for digital input and output (I/O) including status high/low, pulse width 
modulation, external interrupt, edge timing, switch closure pulse counting, high-frequency pulse 
counting, plus UART, RS-232, RS-422, RS-485, SDM, SDI-12, I2C, and SPI serial-communications 
functions. Terminals are configurable in pairs for 5 V or 3.3 V logic for some functions. 
NOTE:
Conflicts can occur when a control port pair is used for different instructions (TimerInput(), 
PulseCount(), SDI12Recorder(), WaitDigTrig()). For example, if C1 is used for 
SDI12Recorder(), C2 cannot be used for TimerInput(), PulseCount(), or 
WaitDigTrig().
Terminals: C1-C8
Sustained Logic Input Voltage without Damage: +16/-12 VDC
Logic Levels and Drive Current:
Terminal pair configuration 5 V source 3.3 V source
Logic low  ≤ 1.5 V ≤ 0.8 V
Logic high ≥ 3.5 V ≥ 2.5 V
C1 - C8 10 mA @ 3.5V 10 mA @ 1.85V
17. CR1000Xe specifications     248

<!-- Page 266 -->
17.7.1 Switch closure input
Terminals: C1-C8, P1-P2
Pull-Up Resistance: 100 kΩ to 5 V
Event: Low (<0.8 V) to High (>2.5 V)
Maximum Input Frequency: 150 Hz
Minimum Switch Closed Time: 5 ms
Minimum Switch Open Time: 6 ms
Maximum Bounce Time: 1 ms open without being counted 
17.7.2 High-frequency input
Terminals: C1-C8, P1-P2
Pull-Up Resistance: 100 kΩ to 5 V
Event: Low (<0.8 V) to High (>2.5 V)
Maximum Input Frequency: 250 kHz
17.7.3 Edge timing
Terminals: C1-C8
Maximum Input Frequency: ≤ 1 kHz
Resolution: 500 ns
17.7.4 Edge counting
Terminals: C1-C8
Maximum Input Frequency: ≤ 2.3 kHz  
17.7.5 Quadrature input
Terminals: C1-C8 can be configured as digital pairs to monitor the two sensing channels of an 
encoder.
Maximum Frequency: 2.5 kHz
Minimum Pulse Width: 10 µs
17. CR1000Xe specifications     249

<!-- Page 267 -->
17.7.6 Pulse-width modulation
Terminals: C1-C8
Maximum Period: 128 seconds
Resolution:
- 0 – 5 ms: 83.33 ns
- 5 – 300 ms: 5.33 µs
- > 300 ms: 1.95 ms
See also             Pulse measurements (p. 112) and Pulse measurement specifications (p. 260).   
17.8 Communications specifications
A data logger is normally part of a two-way conversation started by a computer. In applications 
with some types of interfaces, the data logger can also initiate the call (callback) when needed. In 
satellite applications, the data logger may simply send bursts of data at programmed times 
without waiting for a response.
Ethernet Port: RJ45 jack, 10/100Base Mbps, full and half duplex, Auto-MDIX, magnetic isolation, 
and TVS surge protection. See also Ethernet communications option (p. 35).
Internet Protocols: Ethernet, PPP,  RNDIS, ICMP/Ping, Auto-IP(APIPA), IPv4, IPv6, UDP, TCP, TLS 
(v1.2),  DNS, DHCP, SLAAC, Telnet, HTTP(S), SFTP, FTP(S), POP3/TLS, NTP, SMTP/TLS, SNMPv3, 
CS I/O IP, MQTT
Additional Protocols: CPI, PakBus, PakBus Encryption, SDM, SDI-12, Modbus RTU / ASCII / TCP, 
DNP3 outstation, custom user definable over serial, NTCIP, NMEA 0183, I2C, SPI
USB: Type C 2.0. Full speed: 12 Mbps. Operates as: 
- Device for computer communications
CS I/O: 9-pin D-sub  connector to interface with Campbell Scientific CS I/O peripherals.
SDI-12 (C1, C3, C5, C7): Four independent SDI-12 compliant terminals are individually configured 
and meet SDI-12 Standard v 1.4.
RS-485 (C1 to C8): Up to two full duplex or four half duplex
RS-422 (C1 to C8): Up to two full duplex or four half duplex
RS-232/CPI: Single RJ45 module port that can operate in one of two modes: CPI or RS-232. CPI 
interfaces with Campbell Scientific CDM measurement peripherals and sensors. RS-232 connects, 
with an adapter cable, to computer, sensor, or communications devices serially. 
17. CR1000Xe specifications     250

<!-- Page 268 -->
CPI: One CPI bus. Up to 1 Mbps data rate. Synchronization of devices to 5 μS. Total cable length 
up to 610 m (2000 ft). Up to 20 devices. CPI is a proprietary interface for communications 
between Campbell Scientific data loggers and Campbell Scientific CDM peripheral devices. It 
consists of a physical layer definition and a data protocol.
Hardwired: Multi-drop, short haul, RS-232, fiber optic
Satellite: GOES, Argos, Inmarsat Hughes, Irridium
17.9 Standards compliance specifications
View compliance and conformity documents at www.campbellsci.com/cr1000x 
 .
Test Applied standard Description
Shock and vibration: MIL-STD 810G methods 
516.6 and 514.6  
Protection:    
Wiring panel IP40  
Measurement module when 
connected to wiring panel IP65  
EMI and ESD immunity:    
ESD  IEC 61000-4-2 ±15 kV air, ±8 kV contact 
discharge
Radiated RF IEC 61000-4-3 10 V/m, 80-1000 MHz
EFT IEC 61000-4-4 4 kV power, 4 kV I/O
Surge  IEC 61000-4-5 4 kV power, 4kV I/O
Conducted RF IEC 61000-4-6 10 V power, 10 V I/O
Emissions and immunity performance criteria available on request.
17. CR1000Xe specifications     251

<!-- Page 269 -->
18. CR1000X specifications
Electrical specifications are valid over a -40 to +70 °C, non-condensing environment, unless 
otherwise specified. Extended electrical specifications (noted as XT in specifications) are valid 
over a -55 to +85 °C non-condensing environment. Recalibration is recommended every three 
years. Critical specifications and system configuration should be confirmed with Campbell 
Scientific before purchase.
See CR1000Xe specifications (p. 238) for CR1000Xe specifications.
18.1 System specifications 252
18.2 Physical specifications 253
18.3 Power requirements 253
18.4 Power output specifications 254
18.5 Analog measurement specifications 256
18.6 Pulse measurement specifications 260
18.7 Digital input/output specifications 262
18.8 Communications specifications 264
18.9 Standards compliance specifications 265
18.1 System specifications
Processor: Renesas RX63N (32-bit with hardware FPU, running at 100 MHz)
Memory (see Data memory (p. 88) for more information):
- Total onboard: 128 MB of flash + 4 MB battery-backed SRAM
- Data storage: 4 MB SRAM + 72 MB flash (extended data storage automatically used 
for auto-allocated Data Tables not being written to a card)
- CPU drive: 30 MB flash
- OS load: 8 MB flash
- Settings: 1 MB flash
- Reserved (not accessible): 10 MB flash
- Data storage expansion: Removable microSD flash memory, up to 16 GB
Program Execution Period: 1 ms to 1 day
18. CR1000X specifications     252

<!-- Page 270 -->
Real-Time Clock:
- Battery backed while external power is disconnected
- Resolution: 1 ms
- Accuracy: ±3 min. per year, optional GPS correction to ±10 µs
Wiring Panel Temperature: Measured using a 10K3A1A BetaTHERM thermistor, located between 
the two rows of analog input terminals.
18.2 Physical specifications
Dimensions: 23.8 x 10.1 x 6.2 cm (9.4 x 4.0 x 2.4 in); additional clearance required for cables and 
wires.  For CAD files, see CR1000X Images and CAD 2D Drawings.
Weight/Mass: 0.86 kg (1.9 lb)        
Case Material: Powder-coated aluminum
Operating Temperature Range: 
- -40 °C to +70 °C (standard)
- -55°C to +85 °C (extended)
Storage Temperature Range: -60 to 85 °C
Operating Humidity: 0 to 100 %RH, non-condensing environment
18.3 Power requirements
Protection: Power inputs are protected against surge, over-voltage, over-current, and reverse 
power. IEC 61000-4 Class 4 level.
Power In Terminal:
- Input Voltage: 10 to 18 VDC
NOTE: To prevent voltage input issues with sensors and peripherals, do not use more 
than 16 V when powering them through the 12V, SW12-1, SW12-2,  or CS I/O port on the 
data logger. 
- Input Current Limit at 12 VDC:
- 4.35 A at -40 °C
- 3 A at 20 °C
- 1.56 A at 85 °C
18. CR1000X specifications     253

<!-- Page 271 -->
- Sustained Input Voltage without Damage: 30 VDC
Transient voltage suppressor (TVS) diodes at the POWER IN terminal clamps transients to 
36 to 40 V. Input voltages greater than 18 V and less than 32 V are tolerated; however, the 
12 V output SW12-1 and SW12-2 are disabled and will not function until the input voltage 
falls below 16 V. Sustained input voltages in excess of 32 V can damage the TVS diodes. If 
the voltage on the POWER IN terminals exceeds 19 V, power is shut off to certain parts of 
the data logger to prevent damaging connected sensors or peripherals.
Vehicle Power Connection: When primary power is pulled from the vehicle power system, a 
second power supply OR charge regulator may be required to overcome the voltage drop at 
vehicle start-up.
USB Power: Functions that will be active with USB 5 VDC applied include sending programs, 
adjusting data logger settings, and making some measurements. If USB is the only power source, 
then the CS I/O port and the 5V, 12V, and SW12 terminals will not be operational. When powered 
by only USB (no other power supplies connected) Status table field Battery = 0.
Internal Lithium Battery: AA, 2.4 Ah, 3.6 VDC (Tadiran TL 5903/S) for battery-backed SRAM and 
clock. 3-year life with no external power source. See also Internal battery (p. 154).
Average Current Drain: 
Assumes 12 VDC on POWER IN terminals.
- Idle: <1 mA, typical
- Active 1 Hz Scan: 1 mA
- Active 20 Hz Scan: 55 mA
- Serial (RS-232/RS-485): Active + 25 mA
- Ethernet Power Requirements:
- Ethernet 1 Minute: Active + 1 mA
- Ethernet Idle: Active + 4 mA
- Ethernet Link: Active + 47 mA
18.4 Power output specifications 
18.4.1 System power out limits (when powered with 
12 VDC)
Temperature (°C) Current limit1 (A)
–40° 4.53
20° 3.00
18. CR1000X specifications     254

<!-- Page 272 -->
Temperature (°C) Current limit1 (A)
70° 1.83
85° 1.56
1 Limited by self-resetting thermal fuse
18.4.2 12 V and SW12 V power output terminals
12V, SW12-1, and SW12-2: Provide unregulated 12 VDC power  with voltage equal to the Power 
Input supply voltage. These are disabled when operating on USB power only. The 12V terminal is 
limited to the current shown in the previous table.
SW12 current limits
Temperature (°C) Current limit 1 (mA)
–40° 1310
0° 1004
20° 900
50° 690
70° 550
80° 470
1 Thermal fuse hold current. Overload causes voltage drop. 
Disconnect and let cool to reset. Operate at limit if the application 
can tolerate some fluctuation.
18.4.3 5 V fixed output 
5V: One regulated 5 V output. Supply is shared between the 5V terminal and CS I/O DB9 5 V 
output.
- Voltage Output: Regulated 5 V output (±5%)
- Current Limit: 230 mA
18.4.4 C as power output
Operating at the current limit is OK if voltage fluctuation can be tolerated. Drive capacity is 
determined by the logic level of the VDC supply and the output resistance (Ro) of the C terminal. 
It is expressed as: Vo = 5 V – (Ro • Io), where Vo is the drive limit, and Io is the current required by 
18. CR1000X specifications     255

<!-- Page 273 -->
the external device. For example: at the maximum current limit of 10 mA on C1 the voltage level 
would reduce from 5 V to 3.5 V.
- C Terminals:
- Output Resistance (Ro): 150 Ω
- 5 V Logic Level Drive Capacity: 10 mA @ 3.5 VDC; Vo = 5 V - (150 Ω •Io)
- 3.3 V Logic Level Drive Capacity: 10 mA @ 1.8 VDC; Vo = 3.3 V - (150 Ω •Io)
18.4.5 CS I/O pin 1
5 V Logic Level Max Current: 200 mA
18.4.6 Voltage excitation
VX: Four independently configurable voltage terminals (VX1-VX4).  When providing voltage 
excitation, a single 16-bit DAC shared by all VX outputs produces a user-specified voltage during 
measurement only. In this case, these terminals are regularly used with resistive-bridge 
measurements (see Resistance measurements [p. 103] for more information). VX terminals can 
also be used to supply a selectable, switched, regulated 3.3 or 5 VDC power source to power 
digital sensors and toggle control lines.
  Range Resolution Accuracy Maximum source/sink 
current1
Voltage 
Excitation ±4 V  0.06 mV ±(0.1% of setting 
+ 2 mV) ±40 mA
Switched, 
Regulated +3.3  or 5 V 3.3 or 5 V ±5% 50 mA
1 Exceeding current limits causes voltage output to become unstable. Voltage should stabilize when current is 
reduced to within stated limits.
18.5 Analog measurement specifications
16 single-ended (SE) or 8 differential (DIFF) terminals individually configurable for voltage, 
thermocouple, current loop, ratiometric, and period average measurements, using a 24-bit ADC. 
One channel at a time is measured.
18. CR1000X specifications     256

<!-- Page 274 -->
18.5.1 Voltage measurements
Terminals:
- Differential Configuration: DIFF 1H/1L – 8H/8L
- Single-Ended Configuration: SE1 – SE16
Input Resistance: 20 GΩ typical
Input Voltage Limits: ±5 V
Sustained Input Voltage without Damage: ±20 VDC
DC Common Mode Rejection:
- >120 dB with input reversal
- ≥ 86 dB without input reversal
Normal Mode Rejection: > 70 dB @ 60 Hz
Input Current @ 25 °C: ±1 nA typical
Filter First Notch Frequency (fN1) Range: 0.5 Hz to 31.25 kHz (user specified)
Analog Range and Resolution:
  Differential with input 
reversal
Single-ended and differential without input 
reversal
Notch 
frequency
(fN1) (Hz)
Range1
(mV)
RMS
(µV) Bits2 RMS
(µV) Bits2
15000
±5000
±1000
±200
8.2
1.9
0.75
20
20
19
11.8
2.6
1.0
19
19
18
50/603
±5000
±1000
±200
0.6
0.14
0.05
24
23
22
0.88
0.2
0.08
23
23
22
18. CR1000X specifications     257

<!-- Page 275 -->
  Differential with input 
reversal
Single-ended and differential without input 
reversal
Notch 
frequency
(fN1) (Hz)
Range1
(mV)
RMS
(µV) Bits2 RMS
(µV) Bits2
5
±5000
±1000
±200
0.18
0.04
0.02
25
25
24
0.28
0.07
0.03
25
24
23
1 Range overhead of ~5% on all ranges guarantees that full-scale values will not cause over range
2 Typical effective resolution (ER) in bits; computed from ratio of full-scale range to RMS resolution.
3 50/60 corresponds to rejection of 50 and 60 Hz ac power mains noise.
Accuracy (does not include sensor or measurement noise):
- 0 to 40 °C: ±(0.04% of measurement + offset)
- –40 to 70 °C: ±(0.06% of measurement + offset)
Voltage Measurement Accuracy Offsets:
  Typical offset (µV RMS)
Range (mV) Differential
with input reversal
Single-ended or differential
without input reversal
±5000 ±0.5 ±2
±1000 ±0.25 ±1
±200 ±0.15 ±0.5
Measurement Settling Time: 20 µs to 600 ms; 500 µs default
Multiplexed Measurement Time:
These are not maximum speeds. Multiplexed denotes circuitry inside the data logger that 
switches signals into the ADC.
Where:
M = 1 (default)
M = 2 if reverse differential or measurement offset is used
Setup Time = 150 µs
18. CR1000X specifications     258

<!-- Page 276 -->
  Differential
with input reversal
Single-ended or differential
without input reversal
Example fN11 (Hz) Time2 (ms) Time2 (ms)
15000 2.04 1.02
60 35.24 17.62
50 41.9 20.95
5 401.9 200.95
1 Notch frequency (1/integration time).
2 Default settling time of 500 µs used. 
See also Voltage measurements (p. 99).
18.5.2 Resistance measurement specifications
The data logger makes ratiometric-resistance measurements for four- and six-wire full-bridge 
circuits and two-, three-, and four-wire half-bridge circuits using voltage excitation. Excitation 
polarity reversal is available to minimize dc error. Typically, at least one terminal is configured for 
excitation output. Multiple sensors may be able to use a common excitation terminal.
Accuracy:
Assumes input reversal for differential measurements RevDiff and excitation reversal RevEx  
for excitation voltage <1000 mV. Does not include bridge resistor errors or sensor and 
measurement noise.
Ratiometric accuracy, rather than absolute accuracy, determines overall measurement accuracy. 
Offset is the same as specified for analog voltage measurements.
- 0 to 40 °C: ±(0.01% of voltage measurement + offset)
- –40 to 70 °C: ±(0.015% of voltage measurement + offset)
- –55 to 85 °C (XT): ±(0.02% of voltage measurement + offset)
18.5.3 Period-averaging measurement specifications
Use PeriodAvg() to measure the period (in microseconds) or the frequency (in Hz) of a signal 
on a single-ended channel. 
Terminals: SE1-SE16
Accuracy: ±(0.01% of measurement + resolution), where resolution is 0.13 µs divided by the 
number of cycles to be measured
18. CR1000X specifications     259

<!-- Page 277 -->
Ranges:
- Minimum signal centered around specified period average threshold.
- Maximum signal centered around data logger ground.
- Maximum frequency = 1/(2 * [minimum pulse width]) for 50% duty cycle signals
Gain
code
option
Voltage
gain
Minimum
peak to peak
signal (mV)
Maximum
peak to peak
signal (V)
Minimum
pulse width (µs)
Maximum
frequency (kHz)
0 1 500 10 2.5 200
1 2.5 50 2 10 50
2 12.5 10 2 62 8
3 64 2 2 100 5
See also Period-averaging measurements (p. 111).
18.5.4 Current-loop measurement specifications
The data logger makes current-loop measurements by measuring across a current-sense resistor 
associated with the RS-485 resistive ground terminal.
Terminals: RG1 and RG2
Maximum Input Voltage: ±16 V
Resistance to Ground: 101 Ω
Current Measurement Shunt Resistance: 10 Ω
Maximum Current Measurement Range: ±80 mA
Absolute Maximum Current: ±160 mA
Resolution: ≤ 20 nA
Accuracy: ±(0.1% of reading + 100 nA) @ -40 to 70 °C
See also Current-loop measurements (p. 101).
18.6 Pulse measurement specifications
Terminals individually configurable for switch closure, high-frequency pulse, or low-level AC 
measurements. See also Digital input/output specifications (p. 262). Each terminal has its own 
independent 24-bit counter.
18. CR1000X specifications     260

<!-- Page 278 -->
NOTE:
Conflicts can occur when a control port pair is used for different instructions (TimerInput(), 
PulseCount(), SDI12Recorder(), WaitDigTrig()). For example, if C1 is used for 
SDI12Recorder(), C2 cannot be used for TimerInput(), PulseCount(), or 
WaitDigTrig().
Sustained Input Voltage without Damage: ±20 VDC
Maximum Counts Per Scan: 224
Input Resistance: 5 kΩ
Accuracy: ±(0.02% of reading + 1/scan)
18.6.1 Low-level AC input
Terminals: P1-P2
Minimum Pull-Down Resistance: 10 kΩ to ground
DC-offset rejection:  Internal AC coupling eliminates DC-offset voltages up to ±0.05 VDC
Input Hysteresis: 12 mV at 1 Hz
Low-Level AC Pulse Input Ranges:
Sine wave (mV RMS) Range (Hz)
20 1.0 to 20
200 0.5 to 200
2000 0.3 to 10,000
5000 0.3 to 20,000
18.6.2 Switch closure input
Terminals: C1-C8, P1-P2
Pull-Up Resistance: 100 kΩ to 5 V
Event: Low (<0.8 V) to High (>2.5 V)
Maximum Input Frequency: 150 Hz
Minimum Switch Closed Time: 5 ms
Minimum Switch Open Time: 6 ms
Maximum Bounce Time: 1 ms open without being counted 
18. CR1000X specifications     261

<!-- Page 279 -->
18.6.3 High-frequency input
Terminals: C1-C8, P1-P2
Pull-Up Resistance: 100 kΩ to 5 V
Event: Low (<0.8 V) to High (>2.5 V)
Maximum Input Frequency: 250 kHz
18.7 Digital input/output specifications
Terminals configurable for digital input and output (I/O) including status high/low, pulse width 
modulation, external interrupt, edge timing, switch closure pulse counting, high-frequency pulse 
counting, plus UART, RS-232, RS-422, RS-485, SDM, SDI-12, I2C, and SPI serial-communications 
functions. Terminals are configurable in pairs for 5 V or 3.3 V logic for some functions. 
NOTE:
Conflicts can occur when a control port pair is used for different instructions (TimerInput(), 
PulseCount(), SDI12Recorder(), WaitDigTrig()). For example, if C1 is used for 
SDI12Recorder(), C2 cannot be used for TimerInput(), PulseCount(), or 
WaitDigTrig().
Terminals: C1-C8
Sustained Logic Input Voltage without Damage: ±20  VDC
Logic Levels and Drive Current:
Terminal pair configuration 5 V source 3.3 V source
Logic low  ≤ 1.5 V ≤ 0.8 V
Logic high ≥ 3.5 V ≥ 2.5 V
C1 - C8 10 mA @ 3.5V 10 mA @ 1.85V
18.7.1 Switch closure input
Terminals: C1-C8, P1-P2
Pull-Up Resistance: 100 kΩ to 5 V
Event: Low (<0.8 V) to High (>2.5 V)
Maximum Input Frequency: 150 Hz
Minimum Switch Closed Time: 5 ms
18. CR1000X specifications     262

<!-- Page 280 -->
Minimum Switch Open Time: 6 ms
Maximum Bounce Time: 1 ms open without being counted 
18.7.2 High-frequency input
Terminals: C1-C8, P1-P2
Pull-Up Resistance: 100 kΩ to 5 V
Event: Low (<0.8 V) to High (>2.5 V)
Maximum Input Frequency: 250 kHz
18.7.3 Edge timing
Terminals: C1-C8
Maximum Input Frequency: ≤ 1 kHz
Resolution: 500 ns
18.7.4 Edge counting
Terminals: C1-C8
Maximum Input Frequency: ≤ 2.3 kHz  
18.7.5 Quadrature input
Terminals: C1-C8 can be configured as digital pairs to monitor the two sensing channels of an 
encoder.
Maximum Frequency: 2.5 kHz
Minimum Pulse Width: 10 µs
18.7.6 Pulse-width modulation
Terminals: C1-C8
Maximum Period: 36.4 seconds
Resolution:
- 0 – 5 ms: 83.33 ns
- 5 – 325 ms: 5.33 µs
- > 325 ms: 31.25 µs
18. CR1000X specifications     263

<!-- Page 281 -->
See also             Pulse measurements (p. 112) and Pulse measurement specifications (p. 260).   
18.8 Communications specifications
A data logger is normally part of a two-way conversation started by a computer. In applications 
with some types of interfaces, the data logger can also initiate the call (callback) when needed. In 
satellite applications, the data logger may simply send bursts of data at programmed times 
without waiting for a response.
Ethernet Port: RJ45 jack, 10/100Base Mbps, full and half duplex, Auto-MDIX, magnetic isolation, 
and TVS surge protection. See also Ethernet communications option (p. 35).
Internet Protocols: Ethernet, PPP,  RNDIS, ICMP/Ping, Auto-IP(APIPA), IPv4, IPv6, UDP, TCP, TLS 
(v1.2),  DNS, DHCP, SLAAC, Telnet, HTTP(S), SFTP, FTP(S), POP3/TLS, NTP, SMTP/TLS, SNMPv3, 
CS I/O IP, MQTT
Additional Protocols: CPI, PakBus, PakBus Encryption, SDM, SDI-12, Modbus RTU / ASCII / TCP, 
DNP3 outstation, custom user definable over serial, NTCIP, NMEA 0183, I2C, SPI
USB Device: Micro-B device for computer connectivity
USB: Type C 2.0. Full speed: 12 Mbps. Operates as: 
- Device for computer communications
CS I/O: 9-pin D-sub  connector to interface with Campbell Scientific CS I/O peripherals.
SDI-12 (C1, C3, C5, C7): Four independent SDI-12 compliant terminals are individually configured 
and meet SDI-12 Standard v 1.4.
RS-485 (C5 to C8): One full duplex or two half duplex
RS-422 (C5 to C8): One full duplex or two half duplex
RS-232/CPI: Single RJ45 module port that can operate in one of two modes: CPI or RS-232. CPI 
interfaces with Campbell Scientific CDM measurement peripherals and sensors. RS-232 connects, 
with an adapter cable, to computer, sensor, or communications devices serially. 
CPI: One CPI bus. Up to 1 Mbps data rate. Synchronization of devices to 5 μS. Total cable length 
up to 610 m (2000 ft). Up to 20 devices. CPI is a proprietary interface for communications 
between Campbell Scientific data loggers and Campbell Scientific CDM peripheral devices. It 
consists of a physical layer definition and a data protocol.
Hardwired: Multi-drop, short haul, RS-232, fiber optic
Satellite: GOES, Argos, Inmarsat Hughes, Irridium
18. CR1000X specifications     264

<!-- Page 282 -->
18.9 Standards compliance specifications
View compliance and conformity documents at www.campbellsci.com/cr1000x 
 .
Test Applied standard Description
Shock and vibration: MIL-STD 810G methods 
516.6 and 514.6  
Protection:    
Wiring panel IP40  
Measurement module when 
connected to wiring panel IP65  
EMI and ESD immunity:    
ESD  IEC 61000-4-2 ±15 kV air, ±8 kV contact 
discharge
Radiated RF IEC 61000-4-3 10 V/m, 80-1000 MHz
EFT IEC 61000-4-4 4 kV power, 4 kV I/O
Surge  IEC 61000-4-5 4 kV power, 4kV I/O
Conducted RF IEC 61000-4-6 10 V power, 10 V I/O
Emissions and immunity performance criteria available on request.
18. CR1000X specifications     265

<!-- Page 283 -->
Appendix A. Glossary
A
AC
Alternating current (see VAC).
accuracy
The degree to which the result of a measurement, calculation, or specification conforms to 
the correct value or a standard.
ADC
Analog to digital conversion. The process that translates analog voltage levels to digital 
values.
Aggregate Type
Denotes the aggregate type for the incoming measurement into CampbellCloud. For 
example, if the incoming measurement is a minimum value (for example, minimum battery 
voltage), set aggregate type to minimum.
alias
A second name assigned to variable in CRBasic.
allowed neighbor list
In PakBus networking, an allowed neighbor list is a list of neighbors with which a device will 
communicate. If a device address is entered in an allowed neighbor list, a hello exchange will 
be initiated with that device. Any device with an address between 1 and 3999 that is not 
entered in the allowed neighbor list will be filtered from communicating with the device 
using the list.
Appendix A. Glossary     266

<!-- Page 284 -->
amperes (A)
Base unit for electric current. Used to quantify the capacity of a power source or the 
requirements of a power-consuming device.
analog
Data presented as continuously variable electrical signals.
API
Application Programming Interface
application
Also called app for short. A group of functions for related tasks.
argument
Part of a procedure call (or command execution).
array
A group of variables as declared in CRBasic.
ASCII/ANSI
Abbreviation for American Standard Code for Information Interchange / American National 
Standards Institute. An encoding scheme in which numbers from 0-127 (ASCII) or 0-255 
(ANSI) are used to represent pre-defined alphanumeric characters. Each number is usually 
stored and transmitted as 8 binary digits (8 bits), resulting in 1 byte of storage per character 
of text.
asset
Primarily this is a data source such as a data logger or CR1000X/CR1000Xe. It can also be 
another piece of hardware.
Appendix A. Glossary     267

<!-- Page 285 -->
asynchronous
The transmission of data between a transmitting and a receiving device occurs as a series of 
zeros and ones. For the data to be "read" correctly, the receiving device must begin reading 
at the proper point in the series. In asynchronous communications, this coordination is 
accomplished by having each character surrounded by one or more start and stop bits 
which designate the beginning and ending points of the information. Also indicates the 
sending and receiving devices are not synchronized using a clock signal.
AWG
AWG ("gauge") is the accepted unit when identifying wire diameters.  Larger AWG values 
indicate smaller cross-sectional diameter wires.  Smaller AWG values indicate large-diameter 
wires. For example, a 14 AWG wire is often used for grounding because it can carry large 
currents. 22 AWG wire is often used as sensor wire since only small currents are carried when 
measurements are made.
B
baud rate
The rate at which data is transmitted.
beacon
A signal broadcasted to other devices in a PakBus network to identify "neighbor" devices. A 
beacon in a PakBus network ensures that all devices in the network are aware of other 
devices that are viable.
binary
Describes data represented by a series of zeros and ones. Also describes the state of a 
switch, either being on or off.
BOOL8
A one-byte data type that holds eight bits (0 or 1) of information. BOOL8 uses less space 
than the 32 bit BOOLEAN data type.
Appendix A. Glossary     268

<!-- Page 286 -->
boolean
Name given a function, the result of which is either true or false.
boolean data type
Typically used for flags and to represent conditions or hardware that have only two states 
(true or false) such as flags and control ports.
burst
Refers to a burst of measurements. Analogous to a burst of light, a burst of measurements is 
intense, such that it features a series of measurements in rapid succession, and is not 
continuous.
C
calibration wizard
The calibration wizard facilitates the use of the CRBasic field calibration instructions FieldCal
() and FieldCalStrain().  It is found in LoggerNet (4.0 and later) or RTDAQ.
callback
A name given to the process by which the data logger initiates communications with a 
computer running appropriate Campbell Scientific data logger support software. Also 
known as "Initiate Comms."
CampbellGo
A companion mobile field app for CampbellCloud, available for iOS and Android
CardConvert software
A utility to retrieve binary final data from memory cards and convert the data to ASCII or 
other formats.
CD100
An optional enclosure mounted keyboard/display for use with data loggers.
Appendix A. Glossary     269

<!-- Page 287 -->
CDM/CPI
CPI is a proprietary interface for communications between Campbell Scientific data loggers 
and Campbell Scientific CDM peripheral devices. It consists of a physical layer definition and 
a data protocol.
CF
CompactFlash®
Classification
Refers to the primary classification of a measurement, such as tempreature, relative 
humidity, or precipitation.
code
A CRBasic program, or a portion of a program.
Collect button
Button or command in data logger support software that facilitates collection-on-demand 
of final-data memory. This feature is found in PC400, LoggerNet, and RTDAQ software.
Collect Now button
Button or command in data logger support software that facilitates collection-on-demand 
of final-data memory.  This feature is found in PC400, LoggerNet, and RTDAQ software.
COM port
COM is a generic name given to physical and virtual serial communications ports.
COM1
When configured as a communications port, terminals C1 and C2 act as a pair to form Com1.
command
An instruction or signal that causes a computer to perform one of its basic functions (usually 
in CRBasic).
Appendix A. Glossary     270

<!-- Page 288 -->
command line
One line in a CRBasic program. Maximum length, even with the line continuation characters 
<space> <underscore> ( _), is 512 characters. A command line usually consists of one 
program statement, but it may consist of multiple program statements separated by a 
<colon> (:).
CompactFlash
CompactFlash® (CF) is a memory-card technology used in some Campbell Scientific card-
storage modules.
compile
The software process of converting human-readable program code to binary machine code. 
Data logger user programs are compiled internally by the data logger operating system.
conditioned output
The output of a sensor after scaling factors are applied.
connector
A connector is a device that allows one or more electron conduits (wires, traces, leads, etc) to 
be connected or disconnected as a group. A connector consists of two parts — male and 
female. For example, a common household ac power receptacle is the female portion of a 
connector. The plug at the end of a lamp power cord is the male portion of the connector.
constant
A packet of memory given an alpha-numeric name and assigned a fixed number.
control I/O
C terminals configured for controlling or monitoring a device.
CoraScript
CoraScript is a command-line interpreter associated with LoggerNet data logger support 
software.
Appendix A. Glossary     271

<!-- Page 289 -->
CPU
Central processing unit. The brains of the data logger.
cr
Carriage return.
CRBasic
Campbell Scientific's BASIC-like programming language that supports analog and digital 
measurements, data processing and analysis routines, hardware control, and many 
communications protocols.
CRBasic Editor
The CRBasic programming editor; stand-alone software and also included with LoggerNet, 
PC400, and RTDAQ software.
CRC
Cyclic Redundancy Check
CRD
An optional memory drive that resides on a memory card.
CS I/O
Campbell Scientific proprietary input/output port. Also, the proprietary serial 
communications protocol that occurs over the CS I/O port.
CVI
Communications verification interval. The interval at which a PakBus™ device verifies the 
accessibility of neighbors in its neighbor list. If a neighbor does not communicate for a 
period of time equal to 2.5 times the CVI, the device will send up to four Hellos. If no 
response is received, the neighbor is removed from the neighbor list.
Appendix A. Glossary     272

<!-- Page 290 -->
D
DAC
Digital to analog conversion. The process that translates digital voltage levels to analog 
values.
data bits
Number of bits used to describe the data and fit between the start and stop bit. Sensors 
typically use 7 or 8 data bits.
data cache
The data cache is a set of binary files kept on the hard disk of the computer running the data 
logger support software. A binary file is created for each table in each data logger. These 
files mimic the storage areas in data logger memory, and by default are two times the size of 
the data logger storage area. When the software collects data from a data logger, the data is 
stored in the binary file for that data logger. Various software functions retrieve data from 
the data cache instead of the data logger directly. This allows the simultaneous sharing of 
data among software functions.
data logger support software
LoggerNet, RTDAQ, and PC400 - these Campbell Scientific software applications include at 
least the following functions: data logger communications, downloading programs, clock 
setting, and retrieval of measurement data.
data output interval
The interval between each write of a record to a final-storage memory data table.
data output processing instructions
CRBasic instructions that process data values for eventual output to final-data memory. 
Examples of output-processing instructions include Totalize(), Maximize(), Minimize(), and 
Average(). Data sources for these instructions are values or strings in variable memory. The 
results of intermediate calculations are stored in data output processing memory to await 
the output trigger. The ultimate destination of data generated by data output processing 
instructions is usually final-storage memory, but the CRBasic program can be written to 
Appendix A. Glossary     273

<!-- Page 291 -->
divert to variable memory by the CRBasic program for further processing. The transfer of 
processed summaries to final-data memory takes place when the Trigger argument in the 
DataTable() instruction is set to True.
data output processing memory
Memory automatically allocated for intermediate calculations performed by CRBasic data 
output processing instructions. Data output processing memory cannot be monitored.
data point
A data value which is sent to final-data memory as the result of a data-output processing 
instruction. Data points output at the same time make up a record in a data table.
data source
An asset that sends data to CampbellCloud. This includes data loggers and the Aspen 10 
edge device.
data table
A concept that describes how data is organized in memory, or in files that result from 
collecting data in memory. The fundamental data table is created by the CRBasic program as 
a result of the DataTable() instruction and resides in binary form in memory. The data table 
structure resides in the data cache, in discrete data files, and in files that result from 
collecting final-data memory with data logger support software.
DC
Direct current.
DCE
Data Communications Equipment. While the term has much wider meaning, in the limited 
context of practical use with the data logger, it denotes the pin configuration, gender, and 
function of an RS-232 port. The RS-232 port on the data logger is DCE. Interfacing a DCE 
device to a DCE device requires a null-modem cable.
Appendix A. Glossary     274

<!-- Page 292 -->
desiccant
A hygroscopic material that absorbs water vapor from the surrounding air. When placed in a 
sealed enclosure, such as a data logger enclosure, it prevents condensation.
Device Configuration Utility
Software tool used to set up data loggers and peripherals, and to configure PakBus settings 
before those devices are deployed in the field and/or added to networks. Also called 
DevConfig.
DHCP
Dynamic Host Configuration Protocol. A TCP/IP application protocol.
differential
A sensor or measurement terminal wherein the analog voltage signal is carried on two wires. 
The phenomenon measured is proportional to the difference in voltage between the two 
wires.
Dim
A CRBasic command for declaring and dimensioning variables. Variables declared with Dim 
remain hidden during data logger operations.
dimension
To code a CRBasic program for a variable array as shown in the following examples: DIM 
example(3) creates the three variables example(1), example(2), and example(3); DIM 
example(3,3) creates nine variables; DIM example(3,3,3) creates 27 variables.
DNP3
Distributed Network Protocol is a set of communications protocols used between 
components in process automation systems. Its main use is in utilities such as electric and 
water companies.
DNS
Domain name server. A TCP/IP application protocol.
Appendix A. Glossary     275

<!-- Page 293 -->
DTE
Data Terminal Equipment. While the term has much wider meaning, in the limited context of 
practical use with the data logger, it denotes the pin configuration, gender, and function of 
an RS-232 port. The RS-232 port on the data logger is DCE. Attachment of a null-modem 
cable to a DCE device effectively converts it to a DTE device.
duplex
A serial communications protocol. Serial communications can be simplex, half-duplex, or 
full-duplex.
duty cycle
The percentage of available time a feature is in an active state. For example, if the data 
logger is programmed with 1 second scan interval, but the program completes after only 100 
milliseconds, the program can be said to have a 10% duty cycle.
E
earth ground
A grounding rod or other suitable device that electrically ties a system or device to the earth. 
Earth ground is a sink for electrical transients and possibly damaging potentials, such as 
those produced by a nearby lightning strike. Earth ground is the preferred reference 
potential for analog voltage measurements. Note that most objects have a "an electrical 
potential" and the potential at different places on the earth - even a few meters away - may 
be different.
endian
The sequential order in which bytes are arranged into larger numerical values when stored in 
memory.
engineering units
Units that explicitly describe phenomena, as opposed to, for example, the data logger base 
analog-measurement unit of millivolts.
Appendix A. Glossary     276

<!-- Page 294 -->
ESD
Electrostatic discharge.
ESS
Environmental sensor station.
Etc/UTC
UTC time zone refers to Coordinated Universal Time (UTC). The "Etc" prefix is used in certain 
systems (like UNIX/Linux) to provide a standardized label for time zones. Etc/UTC is always 
at the same time, regardless of the time of year.
excitation
Application of a precise voltage, usually to a resistive bridge circuit.
execution interval
The time interval between initiating each execution of a given Scan() of a CRBasic program. If 
the Scan() Interval is evenly divisible into 24 hours (86,400 seconds), it is synchronized with 
the 24 hour clock, so that the program is executed at midnight and every Scan() Interval 
thereafter. The program is executed for the first time at the first occurrence of the Scan() 
Interval after compilation. If the Scan() Interval does not divide evenly into 24 hours, 
execution will start on the first even second after compilation.
execution time
Time required to execute an instruction or group of instructions. If the execution time of a 
program exceeds the Scan() Interval, the program is executed less frequently than 
programmed and the Status table SkippedScan field will increment.
expression
A series of words, operators, or numbers that produce a value or result.
Appendix A. Glossary     277

<!-- Page 295 -->
F
FAT
File Allocation Table - a computer file system architecture and a family of industry-standard 
file systems utilizing it.
FFT
Fast Fourier Transform. A technique for analyzing frequency-spectrum data.
field
Data tables are made up of records and fields. Each row in a table represents a record and 
each column represents a field. The number of fields in a record is determined by the 
number and configuration of output processing instructions that are included as part of the 
DataTable() declaration.
File Control
File Control is a feature of LoggerNet, PC400, Device Configuration Utility, and RTDAQ data 
logger support software. It provides a view of the data logger file system and a menu of file 
management commands.
fill and stop memory
A memory configuration for data tables forcing a data table to stop accepting data when 
full.
final-data memory
The portion of memory allocated for storing data tables. Once data is written to final-data 
memory, it cannot be changed but only overwritten when it becomes the oldest data. Final-
data memory is configured as ring memory by default, with new data overwriting the oldest 
data.
final-storage data
Data that resides in final-data memory.
Appendix A. Glossary     278

<!-- Page 296 -->
Flash
A type of memory media that does not require battery backup. Flash memory, however, has 
a lifetime based on the number of writes to it. The more frequently data is written, the 
shorter the life expectancy.
FLOAT
Four-byte floating-point data type. Default data logger data type for Public or Dim variables. 
Same format as IEEE4.
FP2
Two-byte floating-point data type. Default data logger data type for stored data. While 
IEEE4 four-byte floating point is used for variables and internal calculations, FP2 is adequate 
for most stored data. FP2 provides three or four significant digits of resolution, and requires 
half the memory as IEEE4.
frequency domain
Frequency domain describes data graphed on an X-Y plot with frequency as the X axis. 
VSPECT vibrating wire data is in the frequency domain.
frequency response
Sample rate is how often an instrument reports a result at its output; frequency response is 
how well an instrument responds to fast fluctuations on its input. By way of example, 
sampling a large gage thermocouple at 1 kHz will give a high sample rate but does not 
ensure the measurement has a high frequency response. A fine-wire thermocouple, which 
changes output quickly with changes in temperature, is more likely to have a high frequency 
response.
FTP
File Transfer Protocol. A TCP/IP application protocol.
full-duplex
A serial communications protocol. Simultaneous bi-directional communications. 
Communications between a serial port and a computer is typically full duplex.
Appendix A. Glossary     279

<!-- Page 297 -->
G
garbage
The refuse of the data communications world. When data is sent or received incorrectly 
(there are numerous reasons why this happens), a string of invalid, meaningless characters 
(garbage) often results. Two common causes are: 1) a baud-rate mismatch and 2) 
synchronous data being sent to an asynchronous device and vice versa.
global navigation satellite system
A satellite navigation system with global coverage such as GPS (North America), Galileo 
(Europe), and BeiDou (China).
global variable
A variable available for use throughout a CRBasic program. The term is usually used in 
connection with subroutines, differentiating global variables (those declared using Public or 
Dim) from local variables, which are declared in the Sub() and Function() instructions.
ground
Being or related to an electrical potential of 0 volts.
ground currents
Pulling power from the data logger wiring panel, as is done when using some 
communications devices from other manufacturers, or a sensor that requires a lot of power, 
can cause voltage potential differences between points in data logger circuitry that are 
supposed to be at ground or 0 Volts. This difference in potentials can cause errors when 
measuring single-ended analog voltages.
H
half-duplex
A serial communications protocol. Bi-directional, but not simultaneous, communications. 
SDI-12 is a half-duplex protocol.
Appendix A. Glossary     280

<!-- Page 298 -->
handshake
The exchange of predetermined information between two devices to assure each that it is 
connected to the other.
hello exchange
In a PakBus network, this is the process of verifying a node as a neighbor.
hertz
SI unit of frequency. Cycles or pulses per second.
Hidden station
Stations that are missing location data. They cannot be geo-located on a map.
HTML
Hypertext Markup Language. Programming language used for the creation of web pages.
HTTP
Hypertext Transfer Protocol. A TCP/IP application protocol.
HTTPS
Hypertext Transfer Protocol Secure. A secure version of HTTP.
hysteresis
The dependence of the state of the system on its history.
Hz
SI unit of frequency. Cycles or pulses per second.
Appendix A. Glossary     281

<!-- Page 299 -->
I
I2C
Inter-Integrated Circuit is a multi-controller, multi-peripheral, packet switched, single-
ended, serial computer bus.
IEEE4
Four-byte, floating-point data type. IEEE Standard 754. Same format as Float.
Include file
A file containing CRBasic code to be included at the end of the current CRBasic program, or 
it can be run as the default program.
INF
A data word indicating the result of a function is infinite or undefined.
initiate comms
A name given to a processes by which the data logger initiates communications with a 
computer running LoggerNet. Also known as Callback.
input/output instructions
Used to initiate measurements and store the results in input storage or to set or read 
control/logic ports.
instruction
Usually refers to a CRBasic command.
integer
A number written without a fractional or decimal component. 15 and 7956 are integers; 1.5 
and 79.56 are not.
Appendix A. Glossary     282

<!-- Page 300 -->
intermediate memory
Memory automatically allocated for intermediate calculations performed by CRBasic data 
output processing instructions. Data output processing memory cannot be monitored.
IP
Internet Protocol. A TCP/IP internet protocol.
IP address
A unique address for a device on the internet.
IP trace
Function associated with IP data transmissions. IP trace information was originally accessed 
through the CRBasic instruction IPTrace() and stored in a string variable. Files Manager 
setting is now modified to allow for creation of a file in data logger memory.
isolation
Hardwire communications devices and cables can serve as alternate paths to earth ground 
and entry points into the data logger for electromagnetic noise. Alternate paths to ground 
and electromagnetic noise can cause measurement errors. Using opto-couplers in a 
connecting device allows communications signals to pass, but breaks alternate ground 
paths and may filter some electromagnetic noise.
J
JSON
Java Script Object Notation. A data file format available through the data logger or 
LoggerNet.
K
keep memory
Keep memory is non-volatile memory that preserves some settings during a power-up or 
program start up reset. Examples include PakBus address, station name, beacon intervals, 
Appendix A. Glossary     283

<!-- Page 301 -->
neighbor lists, routing table, and communications timeouts.
keyboard/display
The data logger has an optional external keyboard/display.
L
leaf
A PakBus node at the end of a branch. When in this mode, the data logger is not able to 
forward packets from one of its communications ports to another. It will not maintain a list of 
neighbors, but it still communicates with other PakBus data loggers and wireless sensors. It 
cannot be used as a means of reaching (routing to) other data loggers.
leaf node
A PakBus node at the end of a branch. When in this mode, the data logger is not able to 
forward packets from one of its communications ports to another. It will not maintain a list of 
neighbors, but it still communicates with other PakBus data loggers and wireless sensors. It 
cannot be used as a means of reaching (routing to) other data loggers.
lf
Line feed. Often associated with carriage return (<cr>). <cr><lf>.
linearity
The quality of delivering identical sensitivity throughout the measurement.
local variable
A variable available for use only by the subroutine in which it is declared. The term 
differentiates local variables, which are declared in the Sub() and Function() instructions, 
from global variables, which are declared using Public or Dim.
LoggerLink
Mobile applications that allow a mobile device to communicate with IP, wi-fi, or Bluetooth 
enabled data loggers.
Appendix A. Glossary     284

<!-- Page 302 -->
LoggerNet
Campbell Scientific's data logger support software for programming, communications, and 
data retrieval between data loggers and a computer.
LONG
Data type used when declaring integers.
loop
A series of instructions in a CRBasic program that are repeated for a programmed number of 
times. The loop ends with an End instruction.
loop counter
Increments by one with each pass through a loop.
LSB
Least significant bit (the trailing bit).
LVDT
The linear variable differential transformer (LVDT) is a type of electrical transformer used for 
measuring linear displacement (position).
M
mains power
The national power grid.
manually initiated
Initiated by the user, usually with a Keyboard/Display, as opposed to occurring under 
program control.
Appendix A. Glossary     285

<!-- Page 303 -->
mass storage device
A mass storage device may also be referred to as an auxiliary storage device. The term is 
commonly used to describe USB mass storage devices.
MD5 digest
16 byte checksum of the TCP/IP VTP configuration.
micro SD
Removable memory-card technology.
milli
The SI prefix denoting 1/1000 of a base SI unit.
Modbus
Communications protocol published by Modicon in 1979 for use in programmable logic 
controllers (PLCs).
modem/terminal
Any device that has the following: ability to raise the ring line or be used with an optically 
isolated interface to raise the ring line and put the data logger in the communications 
command state, or an asynchronous serial communications port that can be configured to 
communicate with the data logger.
modulo divide
A math operation. Result equals the remainder after a division.
MQTT
An open communications protocol for the Internet of Things (IoT). MQTT is not an acronym, 
it is simply the name of the protocol. Source: https://www.hivemq.com/blog/mqtt-
essentials-part-1-introducing-mqtt/
Appendix A. Glossary     286

<!-- Page 304 -->
MSB
Most significant bit (the leading bit).
multimeter
An inexpensive and readily available device useful in troubleshooting data acquisition 
system faults.
multiplier
A term, often a parameter in a CRBasic measurement instruction, that designates the slope 
(aka, scaling factor or gain) in a linear function. For example, when converting °C to °F, the 
equation is °F = °C*1.8 + 32. The factor 1.8 is the multiplier.
mV
The SI abbreviation for millivolts.
N
NAN
Not a number. A data word indicating a measurement or processing error. Voltage 
overrange, SDI-12 sensor error, and undefined mathematical results can produce NAN.
neighbor device
Device in a PakBus network that communicates directly with a device without being routed 
through an intermediate device.
network
A group of one or more stations.
Network Planner
Campbell Scientific software designed to help set up datal oggers in PakBus networks so 
that they can communicate with each other and the LoggerNet server. For more 
information, see https://www.campbellsci.com/loggernet.
Appendix A. Glossary     287

<!-- Page 305 -->
NFC
Near field communications
NIST
National Institute of Standards and Technology.
node
Devices in a network — usually a PakBus network. The communications server dials through, 
or communicates with, a node. Nodes are organized as a hierarchy with all nodes accessed 
by the same device (parent node) entered as child nodes. A node can be both a parent and a 
child.
NSEC
Eight-byte data type divided up as four bytes of seconds since 1990 and four bytes of 
nanoseconds into the second.
null modem
A device, usually a multi-conductor cable, which converts an RS-232 port from DCE to DTE 
or from DTE to DCE.
Numeric Monitor
A digital monitor in data logger support software or in a keyboard/display.
O
offset
A term, often a parameter in a CRBasic measurement instruction, that designates the y-
intercept (aka, shifting factor or zeroing factor) in a linear function. For example, when 
converting °C to °F, the equation is °F = °C*1.8 + 32. The factor 32 is the offset.
ohm
The unit of resistance. Symbol is the Greek letter Omega (Ω). 1.0 Ω equals the ratio of 1.0 volt 
divided by 1.0 ampere.
Appendix A. Glossary     288

<!-- Page 306 -->
Ohm's Law
Describes the relationship of current and resistance to voltage. Voltage equals the product 
of current and resistance (V = I • R).
on-line data transfer
Routine transfer of data to a peripheral left on-site. Transfer is controlled by the program 
entered in the data logger.
onboard
A collective term for the tasks that have to complete successfully in order for a data source 
asset to be correctly configured and send data to CampbellCloud. These tasks may be 
automated or require manual user input depending on the data source type. For data 
logger data sources, these tasks include asset claiming, automated sensor identification, 
cellular communications registration, secure Cloud communications, program retrieval, 
successful sensor measurement, and confirmation that Cloud received data.
operating system
The operating system (also known as "firmware") is a set of instructions that controls the 
basic functions of the data logger and enables the use of user written CRBasic programs. 
The operating system is preloaded into the data logger at the factory but can be re-loaded 
or upgraded by you using Device Configuration Utility software. The most recent data 
logger operating system .obj file is available at www.campbellsci.com/downloads.
organization
An entity (individual, business, or group) that uses CampbellCloud services to manage a 
network of stations owned by the entity. Every user must be associated with an organization.
output
A loosely applied term. Denotes a) the information carrier generated by an electronic sensor, 
b) the transfer of data from variable memory to final-data memory, or c) the transfer of 
electric power from the data logger or a peripheral to another device.
Appendix A. Glossary     289

<!-- Page 307 -->
output array
A string of data values output to final-data memory. Output occurs when the data table 
output trigger is True.
output interval
The interval between each write of a record to a data table.
output processing instructions
CRBasic instructions that process data values for eventual output to final-data memory. 
Examples of output-processing instructions include Totalize(), Maximum(), Minimum(), and 
Average(). Data sources for these instructions are values or strings in variable memory. The 
results of intermediate calculations are stored in data output processing memory to await 
the output trigger. The ultimate destination of data generated by data output processing 
instructions is usually final-data memory, but the CRBasic program can be written to divert 
to variable memory for further processing. The transfer of processed summaries to final-
data memory takes place when the Trigger argument in the DataTable() instruction is set to 
True.
output processing memory
Memory automatically allocated for intermediate calculations performed by CRBasic data 
output processing instructions. Data output processing memory cannot be monitored.
owner
An owner is a member of the Owners security group.  By default, the Owners security group 
is created with a new CampbellCloud Organization account. The creator of the organization 
account is automatically added to the Owners security group and can then add other users 
to the group or create other security groups with different permissions. Owners have access 
to all functionality across the range of applications in CampbellCloud (i.e., users within the 
Owners security group have all permissions enabled).
Appendix A. Glossary     290

<!-- Page 308 -->
P
PakBus
™ A proprietary communications protocol developed by Campbell Scientific to facilitate 
communications between Campbell Scientific devices. Similar in concept to IP (Internet 
Protocol), PakBus is a packet-switched network protocol with routing capabilities. A 
registered trademark of Campbell Scientific, Inc.
PakBus Encryption Key
A PakBus Encryption Key is a security feature used with Campbell Scientific data loggers that 
communicate via the PakBus protocol. It helps protect data and restrict unauthorized access 
to the data logger.
PakBus Graph
Software that shows the relationship of various nodes in a PakBus network and allows for 
monitoring and adjustment of some registers in each node.
parameter
Part of a procedure (or command) definition.
PC200W
Retired basic data logger support software for direct connect.
PC400
Free entry-level data logger support software that supports a variety of communications 
options, manual data collection, and data monitoring displays. Short Cut and CRBasic Editor 
are included for creating data logger programs. PC400 does not support scheduled data 
collection or complex communications options such as phone-to-RF.
period average
A measurement technique using a high-frequency digital clock to measure time differences 
between signal transitions. Sensors commonly measured with period average include water-
content reflectometers.
Appendix A. Glossary     291

<!-- Page 309 -->
peripheral
Any device designed for use with the data logger. A peripheral requires the data logger to 
operate. Peripherals include measurement, control, and data retrieval and communications 
modules.
PGA
Programmable Gain Amplifier
ping
A software utility that attempts to contact another device in a network.
pipeline mode
A CRBasic program execution mode wherein instructions are evaluated in groups of like 
instructions, with a set group prioritization.
PLC
Programmable Logic Controllers
Poisson ratio
A ratio used in strain measurements.
ppm
Parts per million.
precision
The amount of agreement between repeated measurements of the same quantity (AKA 
repeatability).
Precision
Specifies the number of decimal places shown for a measurement.
Appendix A. Glossary     292

<!-- Page 310 -->
PreserveVariables
CRBasic instruction that protects Public variables from being erased when a program is 
recompiled.
print device
Any device capable of receiving output over pin 6 (the PE line) in a receive-only mode. 
Printers, "dumb" terminals, and computers in a terminal mode fall in this category.
print peripheral
Any device capable of receiving output over pin 6 (the PE line) in a receive-only mode. 
Printers, "dumb" terminals, and computers in a terminal mode fall in this category.
processing instructions
CRBasic instructions used to further process input-data values and return the result to a 
variable where it can be accessed for output processing.  Arithmetic and transcendental 
functions are included.
program control instructions
Modify the execution sequence of CRBasic instructions. Also used to set or clear flags.
Program Send command
Program Send is a feature of data logger support software.
program statement
A complete program command construct confined to one command line or to multiple 
command lines merged with the line continuation characters <space><underscore> ( _). A 
command line, even with line continuation, cannot exceed 512 characters.
public
A CRBasic command for declaring and dimensioning variables. Variables declared with 
Public can be monitored during data logger operation.
Appendix A. Glossary     293

<!-- Page 311 -->
pulse
An electrical signal characterized by a rapid increase in voltage follow by a short plateau and 
a rapid voltage decrease.
Q
QR code
Quick response barcode
R
ratiometric
Describes a type of measurement or a type of math. Ratiometric usually refers to an aspect 
of resistive-bridge measurements - either the measurement or the math used to process it. 
Measuring ratios and using ratio math eliminates several sources of error from the end 
result.
recipe
A set of files that include the CR1000X/CR1000Xe program, settings and configuration for a 
specific sensor and application.
record
A record is a complete line of data in a data table or data file.  All data in a record share a 
common time stamp.  Data tables are made up of records and fields. Each row in a table 
represents a record and each column represents a field. The number of fields in a record is 
determined by the number and configuration of output processing instructions that are 
included as part of the DataTable() declaration.
regulator
A device for conditioning an electrical power source. Campbell Scientific regulators typically 
condition AC or DC voltages greater than 16 VDC to about 14 VDC.
Appendix A. Glossary     294

<!-- Page 312 -->
resistance
A feature of an electronic circuit that impedes or redirects the flow of electrons through the 
circuit.
resistor
A device that provides a known quantity of resistance.
resolution
The smallest interval measurable.
ring line
Ring line is pulled high by an external device to notify the data logger to commence 
communications. Ring line is pin 3 of the CS I/O port.
ring memory
A memory configuration that allows the oldest data to be overwritten with the newest data. 
This is the default setting for data tables.
ringing
Oscillation of sensor output (voltage or current) that occurs when sensor excitation causes 
parasitic capacitances and inductances to resonate.
RMS
Root-mean square, or quadratic mean. A measure of the magnitude of wave or other 
varying quantities around zero.
RNDIS
Remote Network Driver Interface Specification - a Microsoft protocol that provides a virtual 
Ethernet link via USB.
Appendix A. Glossary     295

<!-- Page 313 -->
router
A device configured as a router is able to forward PakBus packets from one port to another. 
To perform its routing duties, a data logger configured as a router maintains its own list of 
neighbors and sends this list to other routers in the PakBus network. It also obtains and 
receives neighbor lists from other routers. Routers maintain a routing table, which is a list of 
known nodes and routes. A router will only accept and forward packets that are destined for 
known devices. Routers pass their lists of known neighbors to other routers to build the 
network routing system.
RS-232
Recommended Standard 232. A loose standard defining how two computing devices can 
communicate with each other. The implementation of RS-232 in Campbell Scientific data 
loggers to computer communications is quite rigid, but transparent to most users. Features 
in the data logger that implement RS-232 communications with smart sensors are flexible.
RS-422
Communications protocol similar to RS-485. Most RS-422 sensors will work with RS-485 
protocol.
RS-485
Recommended Standard 485. A standard defining how two computing devices can 
communicate with each other.
RTDAQ
Real Time Data Acquisition software for high-speed data acquisition applications. RTDAQ 
supports a variety of telecommunication options, manual data collection, and extensive data 
display. It includes Short Cut for creating data logger programs, as well as full-featured 
program editors.
RTU
Remote Telemetry Unit
Appendix A. Glossary     296

<!-- Page 314 -->
Rx
Receive
S
sample rate
The rate at which measurements are made by the data logger. The measurement sample 
rate is of interest when considering the effect of time skew, or how close in time are a series 
of measurements, or how close a time stamp on a measurement is to the true time the 
phenomenon being measured occurred. A 'maximum sample rate' is the rate at which a 
measurement can repeatedly be made by a single CRBasic instruction.  Sample rate is how 
often an instrument reports a result at its output; frequency response is how well an 
instrument responds to fast fluctuations on its input. By way of example, sampling a large 
gage thermocouple at 1 kHz will give a high sample rate but does not ensure the 
measurement has a high frequency response. A fine-wire thermocouple, which changes 
output quickly with changes in temperature, is more likely to have a high frequency 
response.
SCADA
Supervisory Control And Data Acquisition
scan interval
The time interval between initiating each execution of a given Scan() of a CRBasic program. If 
the Scan() Interval is evenly divisible into 24 hours (86,400 seconds), it is synchronized with 
the 24 hour clock, so that the program is executed at midnight and every Scan() Interval 
thereafter. The program is executed for the first time at the first occurrence of the Scan() 
Interval after compilation. If the Scan() Interval does not divide evenly into 24 hours, 
execution will start on the first even second after compilation.
scan time
When time functions are run inside the Scan() / NextScan construct, time stamps are based 
on when the scan was started according to the data logger clock. Resolution of scan time is 
equal to the length of the scan.
Appendix A. Glossary     297

<!-- Page 315 -->
SDI-12
Serial Data Interface at 1200 baud. Communications protocol for transferring data between 
the data logger and SDI-12 compatible smart sensors.
SDK
Software Development Kit
SDM
Synchronous Device for Measurement. A processor-based peripheral device or sensor that 
communicates with the data logger via hardwire over a short distance using a protocol 
proprietary to Campbell Scientific.
security group
An application used to control user access to applications and their associated permissions. 
Users can be in more than one security group.
Seebeck effect
Induces microvolt level thermal electromotive forces (EMF) across junctions of dissimilar 
metals in the presence of temperature gradients. This is the principle behind thermocouple 
temperature measurement. It also causes small, correctable voltage offsets in data logger 
measurement circuitry.
semaphore
(Measurement semaphore.) In sequential mode, when the main scan executes, it locks the 
resources associated with measurements. In other words, it acquires the measurement 
semaphore. This is at the scan level, so all subscans within the scan (whether they make 
measurements or not), will lock out measurements from slow sequences (including the auto 
self-calibration). Locking measurement resources at the scan level gives non-interrupted 
measurement execution of the main scan.
send button
Send button in data logger support software. Sends a CRBasic program or operating system 
to a data logger.
Appendix A. Glossary     298

<!-- Page 316 -->
sequential mode
A CRBasic program execution mode wherein each statement is evaluated in the order it is 
listed in the program.
serial
A loose term denoting output of a series of ASCII, HEX, or binary characters or numbers in 
electronic form.
Settings Editor
An editor for observing and adjusting settings. Settings Editor is a feature of 
LoggerNet>Connect, PakBus Graph, and Device Configuration Utility.
Short Cut
A CRBasic programming wizard suitable for many data logger applications. Knowledge of 
CRBasic is not required to use Short Cut.
SI
Système Internationale. The uniform international system of metric units. Specifies accepted 
units of measure.
signature
A number which is a function of the data and the sequence of data in memory. It is derived 
using an algorithm that assures a 99.998% probability that if either the data or the data 
sequence changes, the signature changes.
simplex
A serial communications protocol. One-direction data only. Serial communications between 
a serial sensor and the data logger may be simplex.
single-ended
Denotes a sensor or measurement terminal wherein the analog voltage signal is carried on a 
single wire and measured with respect to ground (0 V).
Appendix A. Glossary     299

<!-- Page 317 -->
skipped scans
Occur when the CRBasic program is too long for the scan interval. Skipped scans can cause 
errors in pulse measurements.
slow sequence
A usually slower secondary scan in the CRBasic program. The main scan has priority over a 
slow sequence.
SMS
Short message service. A text messaging service for web and mobile device systems.
SMTP
Simple Mail Transfer Protocol. A TCP/IP application protocol.
SNP
Snapshot file.
SP
Space.
SPI
Serial Peripheral Interface - a clocked synchronous interface, used for short distance 
communications, generally between embedded devices.
SRAM
Static Random-Access Memory
start bit
The bit used to indicate the beginning of data.
state
Whether a device is on or off.
Appendix A. Glossary     300

<!-- Page 318 -->
station
A group of one or more assets
Station Status command
A command available in most data logger support software.
stop bit
The end of the data bits. The stop bit can be 1, 1.5, or 2.
string
A datum or variable consisting of alphanumeric characters.
Subclassification
Refers to the secondary classification of a measurement. For example, a temperature 
classification can have multiple subclassifications, such as air temperature, dew point 
temperature, or soil temperature
support software
Campbell Scientific software that includes at least the following functions: data logger 
communications, downloading programs, clock setting, and retrieval of measurement data.
synchronous
The transmission of data between a transmitting and a receiving device occurs as a series of 
zeros and ones. For the data to be "read" correctly, the receiving device must begin reading 
at the proper point in the series. In synchronous communications, this coordination is 
accomplished by synchronizing the transmitting and receiving devices to a common clock 
signal (see also asynchronous).
system time
When time functions are run outside the Scan() / NextScan construct, the time registered by 
the instruction will be based on the system clock, which has a 10 ms resolution.
Appendix A. Glossary     301

<!-- Page 319 -->
T
table
See data table.
task
Grouping of CRBasic program instructions automatically by the data logger compiler. Tasks 
include measurement, SDM or digital, and processing. Tasks are prioritized when the 
CRBasic program runs in pipeline mode. Also, a user-customized function defined through 
LoggerNet Task Master.
TCP/IP
Transmission Control Protocol / Internet Protocol.
TCR
Temperature Coefficient of Resistance. TCR tells how much the resistance of a resistor 
changes as the temperature of the resistor changes. The unit of TCR is ppm/°C (parts-per-
million per degree Celsius). A positive TCR means that resistance increases as temperature 
increases. For example, a resistor with a specification of 10 ppm/°C will not increase in 
resistance by more than 0.000010 Ω per ohm over a 1 °C increase of the resistor temperature 
or by more than .00010 Ω per ohm over a 10 °C increase.
Telnet
A software utility that attempts to contact and interrogate another specific device in a 
network. Telnet is resident in Windows OS.
terminal
Point at which a wire (or wires) connects to a wiring panel or connector. Wires are usually 
secured in terminals by screw- or lever-and-spring actuated gates with small screw- or 
spring-loaded clamps.
terminal emulator
A command-line shell that facilitates the issuance of low-level commands to a data logger or 
some other compatible device. A terminal emulator is available in most data logger support 
Appendix A. Glossary     302

<!-- Page 320 -->
software available from Campbell Scientific.
thermistor
A thermistor is a temperature measurement device with a resistive element that changes in 
resistance with temperature. The change is wide, stable, and well characterized. The output 
of a thermistor is usually non-linear, so measurement requires linearization by means of a 
Steinhart-Hart or polynomial equation. CRBasic instructions Therm107(), Therm108(), and 
Therm109() use Steinhart-Hart equations.
thing
A thing resource is a digital representation of a physical device or logical entity in AWS IoT.
throughput rate
Rate that a measurement can be taken, scaled to engineering units, and the stored in a final-
memory data table. The data logger has the ability to scan sensors at a rate exceeding the 
throughput rate. The primary factor determining throughput rate is the processing 
programmed into the CRBasic program. In sequential-mode operation, all processing called 
for by an instruction must be completed before moving on to the next instruction.
time domain
Time domain describes data graphed on an X-Y plot with time on the X axis. Time series data 
is in the time domain.
TLS
Transport Layer Security. An Internet communications security protocol.
TOA5
Also called ASCII, Long Header. Data stored in a comma separated format. Header 
information for each column is included, along with field names and units of measure if they 
are available. Table output ascii version 5. See the LoggerNet manual appendix for details on 
differnet file formats.
Appendix A. Glossary     303

<!-- Page 321 -->
TOACI1
Also called ASCII, Short Header. Data stored in a comma separated format. Header 
information for each of the columns is included. Table output ASCII version 1. See the 
LoggerNet manual appendix for details on differnet file formats.
TOB1
Binary. Data stored in a binary format. Though this format saves disk storage space, it must 
be converted before it is usable in other programs. Table output binary version 1. See the 
LoggerNet manual appendix for details on differnet file formats.
TOB3
Binary. Data stored to a card in a binary format. Table output binary version 1. See the 
LoggerNet manual appendix for details on differnet file formats.
toggle
To reverse the current power state.
TTL
Transistor-to-Transistor Logic. A serial protocol using 0 VDC and 5 VDC as logic signal levels.
Tx
Transmit
U
UART
Universal Asynchronous Receiver/Transmitter for asynchronous serial communications.
UID
Unique identifier
Appendix A. Glossary     304

<!-- Page 322 -->
UINT2
Data type used for efficient storage of totalized pulse counts, port status (status of 16 ports 
stored in one variable, for example) or integer values that store binary flags.
unconditioned output
The fundamental output of a sensor, or the output of a sensor before scaling factors are 
applied.
Units
Specifies the unit type of the incoming measurement into CampbellCloud. For example, if 
the asset is sending a temperature measurement to CampbellCloud in degrees Celsius, Units 
must be set to degrees Celsius.
UPS
Uninterruptible Power Supply. A UPS can be constructed for most data logger applications 
using ac line power, a solar panel, an ac/ac or ac/dc wall adapter, a charge controller, and a 
rechargeable battery.
URI
Uniform Resource Identifier
URL
Uniform Resource Locator
user
Individuals who have been added to an organization account. Users are assigned 
permissions via the Security Groups application.
user program
The CRBasic program written by you in Short Cut program wizard.
Appendix A. Glossary     305

<!-- Page 323 -->
USR drive
A portion of memory dedicated to the storage of image or other files.
V
VAC
Volts alternating current.
variable
A packet of memory given an alphanumeric name.
VDC
Volts direct current.
VisualWeather
Data logger support software specialized for weather and agricultural applications. The 
software allows you to initialize the setup, interrogate the station, display data, and generate 
reports from one or more weather stations.
volt meter
An inexpensive and readily available device useful in troubleshooting data acquisition 
system faults.
voltage divider
A circuit of resistors that ratiometrically divides voltage. For example, a simple two-resistor 
voltage divider can be used to divide a voltage in half. So, when fed through the voltage 
divider, 1 mV becomes 500 µV, 10 mV becomes 5 mV, and so forth. Resistive-bridge circuits 
are voltage dividers.
volts
SI unit for electrical potential.
Appendix A. Glossary     306

<!-- Page 324 -->
VSPECT™
™ A registered trademark for Campbell Scientific's proprietary spectral-analysis, frequency 
domain, vibrating wire measurement technique.
W
watchdog timer
An error-checking system that examines the processor state, software timers, and program-
related counters when the CRBasic program is running. The following will cause watchdog 
timer resets, which reset the processor and CRBasic program execution: processor bombed, 
processor neglecting standard system updates, counters are outside the limits, voltage 
surges, and voltage transients.  When a reset occurs, a counter is incremented in the 
WatchdogTimer entry of the Status table.  A low number (1 to 10) of watchdog timer resets is 
of concern, but normally indicates that the situation should just be monitored. A large 
number of errors (>10) accumulating over a short period indicates a hardware or software 
problem. Consult with a Campbell Scientific support engineer.
weather-tight
Describes an instrumentation enclosure impenetrable by common environmental 
conditions. During extraordinary weather events, however, seals on the enclosure may be 
breached.
web API
Application Programming Interface
wild card
A character or expression that substitutes for any other character or expression.
X
XML
Extensible markup language.
Appendix A. Glossary     307

<!-- Page 325 -->
Τ
τ
Time constant
Appendix A. Glossary     308

<!-- Page 326 -->
Limited warranty
Covered equipment is warranted/guaranteed against defects in materials and workmanship 
under normal use and service for the period listed on your sales invoice or the product order 
information web page. The covered period begins on the date of shipment unless otherwise 
specified. For a repair to be covered under warranty, the following criteria must be met:
1. There must be a defect in materials or workmanship that affects form, fit, or function of the 
device.
2. The defect cannot be the result of misuse.
3. The defect must have occurred within a specified period of time; and
4. The determination must be made by a qualified technician at a Campbell Scientific Service 
Center/ repair facility.
The following is not covered:
1. Equipment which has been modified or altered in any way without the written permission of 
Campbell Scientific.
2. Batteries; and
3. Any equipment which has been subjected to misuse, neglect, acts of God or damage in transit.
Campbell Scientific regional offices handle repairs for customers within their territories. Please 
see the back page of the manual for a list of regional offices or visit 
www.campbellsci.com/contact 
  to determine which Campbell Scientific office serves your 
country. For directions on how to return equipment, see Assistance.
Other manufacturer's products, that are resold by Campbell Scientific, are warranted only to the 
limits extended by the original manufacturer.
CAMPBELL SCIENTIFIC EXPRESSLY DISCLAIMS AND EXCLUDES ANY IMPLIED WARRANTIES OF
MERCHANTABILITY OR FITNESS FOR A PARTICULAR PURPOSE. Campbell Scientific hereby 
disclaims, to the fullest extent allowed by applicable law, any and all warranties and conditions 
with respect to the products, whether express, implied, or statutory, other than those expressly 
provided herein.
Campbell Scientific will, as a default, return warranted equipment by surface carrier prepaid. 
However, the method of return shipment is at Campbell Scientific's sole discretion. Campbell 
Scientific will not reimburse the claimant for costs incurred in removing and/or reinstalling 
equipment. This warranty and the Company’s obligation thereunder is in lieu of all other 

<!-- Page 327 -->
warranties, expressed or implied, including those of suitability and fitness for a particular 
purpose. Campbell Scientific is not liable for consequential damage.
In the event of any conflict or inconsistency between the provisions of this Warranty and the 
provisions of Campbell Scientific’s Terms, the provisions of Campbell Scientific’s Terms shall 
prevail. Furthermore, Campbell Scientific’s Terms are hereby incorporated by reference into this 
Warranty. To view Terms and conditions that apply to Campbell Scientific, Logan, UT, USA, see 
Terms and Conditions 
 . To view terms and conditions that apply to Campbell Scientific offices 
outside of the United States, contact the regional office that serves your country.
Acknowledgements
lwIP v 2.1.1, LIBSSH2 v. 1.8.0, and Newlib
Copyright 2026 Campbell Scientific.
All rights reserved.
Redistribution and use in source and binary forms, with or without modification, are permitted 
provided that the following conditions are met:
 1. Redistributions of source code must retain the above copyright notice, this list of 
conditions and the following disclaimer.
 2. Redistributions in binary form must reproduce the above copyright notice, this list of 
conditions and the following disclaimer in the documentation and/or other materials 
provided with the distribution.
THIS SOFTWARE IS PROVIDED BY THE AUTHOR “AS IS” AND ANY EXPRESS OR IMPLIED 
WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO 
EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, 
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER 
IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING 
IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
SUCH DAMAGE.
Mbed TLS  v. 3.1.0
Copyright 2026 Campbell Scientific.
Apache License

<!-- Page 328 -->
Version 2.0, January 2004
http://www.apache.org/licenses/
TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION
1. Definitions.
"License" shall mean the terms and conditions for use, reproduction, and distribution as defined 
by Sections 1 through 9 of this document.
"Licensor" shall mean the copyright owner or entity authorized by the copyright owner that is 
granting the License.
"Legal Entity" shall mean the union of the acting entity and all other entities that control, are 
controlled by, or are under common control with that entity. For the purposes of this definition, 
"control" means (i) the power, direct or indirect, to cause the direction or management of such 
entity, whether by contract or otherwise, or (ii) ownership of fifty percent (50%) or more of the 
outstanding shares, or (iii) beneficial ownership of such entity.
"You" (or "Your") shall mean an individual or Legal Entity exercising permissions granted by this 
License.
"Source" form shall mean the preferred form for making modifications, including but not limited 
to software source code, documentation source, and configuration files.
"Object" form shall mean any form resulting from mechanical transformation or translation of a 
Source form, including but not limited to compiled object code, generated documentation, and 
conversions to other media types.
"Work" shall mean the work of authorship, whether in Source or Object form, made available 
under the License, as indicated by a copyright notice that is included in or attached to the work 
(an example is provided in the Appendix below).
"Derivative Works" shall mean any work, whether in Source or Object form, that is based on (or 
derived from) the Work and for which the editorial revisions, annotations, elaborations, or other 
modifications represent, as a whole, an original work of authorship. For the purposes of this 
License, Derivative Works shall not include works that remain separable from, or merely link (or 
bind by name) to the interfaces of, the Work and Derivative Works thereof.
"Contribution" shall mean any work of authorship, including the original version of the Work and 
any modifications or additions to that Work or Derivative Works thereof, that is intentionally 
submitted to Licensor for inclusion in the Work by the copyright owner or by an individual or 
Legal Entity authorized to submit on behalf of the copyright owner. For the purposes of this 
definition, "submitted" means any form of electronic, verbal, or written communication sent to 
the Licensor or its representatives, including but not limited to communication on electronic 
mailing lists, source code control systems, and issue tracking systems that are managed by, or on 

<!-- Page 329 -->
behalf of, the Licensor for the purpose of discussing and improving the Work, but excluding 
communication that is conspicuously marked or otherwise designated in writing by the copyright 
owner as "Not a Contribution."
"Contributor" shall mean Licensor and any individual or Legal Entity on behalf of whom a 
Contribution has been received by Licensor and subsequently incorporated within the Work.
2. Grant of Copyright License. Subject to the terms and conditions of this License, each 
Contributor hereby grants to You a perpetual, worldwide, non-exclusive, no-charge, royalty-free, 
irrevocable copyright license to reproduce, prepare Derivative Works of, publicly display, publicly 
perform, sublicense, and distribute the Work and such Derivative Works in Source or Object 
form.
3. Grant of Patent License. Subject to the terms and conditions of this License, each Contributor 
hereby grants to You a perpetual, worldwide, non-exclusive, no-charge, royalty-free, irrevocable 
(except as stated in this section) patent license to make, have made, use, offer to sell, sell, import, 
and otherwise transfer the Work, where such license applies only to those patent claims 
licensable by such Contributor that are necessarily infringed by their Contribution(s) alone or by 
combination of their Contribution(s) with the Work to which such Contribution(s) was submitted. 
If You institute patent litigation against any entity (including a cross-claim or counterclaim in a 
lawsuit) alleging that the Work or a Contribution incorporated within the Work constitutes direct 
or contributory patent infringement, then any patent licenses granted to You under this License 
for that Work shall terminate as of the date such litigation is filed.
4. Redistribution. You may reproduce and distribute copies of the Work or Derivative Works 
thereof in any medium, with or without modifications, and in Source or Object form, provided 
that You meet the following conditions:
(a) You must give any other recipients of the Work or Derivative Works a copy of this License; and
(b) You must cause any modified files to carry prominent notices stating that You changed the 
files; and
(c) You must retain, in the Source form of any Derivative Works that You distribute, all copyright, 
patent, trademark, and attribution notices from the Source form of the Work, excluding those 
notices that do not pertain to any part of the Derivative Works; and
(d) If the Work includes a "NOTICE" text file as part of its distribution, then any Derivative Works 
that You distribute must include a readable copy of the attribution notices contained within such 
NOTICE file, excluding those notices that do not pertain to any part of the Derivative Works, in at 
least one of the following places: within a NOTICE text file distributed as part of the Derivative 
Works; within the Source form or documentation, if provided along with the Derivative Works; or, 
within a display generated by the Derivative Works, if and wherever such third-party notices 

<!-- Page 330 -->
normally appear. The contents of the NOTICE file are for informational purposes only and do not 
modify the License.
You may add Your own attribution notices within Derivative Works that You distribute, alongside 
or as an addendum to the NOTICE text from the Work, provided that such additional attribution 
notices cannot be construed as modifying the License.
You may add Your own copyright statement to Your modifications and may provide additional or 
different license terms and conditions for use, reproduction, or distribution of Your modifications, 
or for any such Derivative Works as a whole, provided Your use, reproduction, and distribution of 
the Work otherwise complies with the conditions stated in this License.
5. Submission of Contributions. Unless You explicitly state otherwise, any Contribution 
intentionally submitted for inclusion in the Work by You to the Licensor shall be under the terms 
and conditions of this License, without any additional terms or conditions. Notwithstanding the 
above, nothing herein shall supersede or modify the terms of any separate license agreement 
you may have executed with Licensor regarding such Contributions.
6. Trademarks. This License does not grant permission to use the trade names, trademarks, 
service marks, or product names of the Licensor, except as required for reasonable and 
customary use in describing the origin of the Work and reproducing the content of the NOTICE 
file.
7. Disclaimer of Warranty. Unless required by applicable law or agreed to in writing, Licensor 
provides the Work (and each Contributor provides its Contributions) on an "AS IS" BASIS, 
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied, including, 
without limitation, any warranties or conditions of TITLE, NON-INFRINGEMENT, 
MERCHANTABILITY, or FITNESS FOR A PARTICULAR PURPOSE. You are solely responsible for 
determining the appropriateness of using or redistributing the Work and assume any risks 
associated with Your exercise of permissions under this License.
8. Limitation of Liability. In no event and under no legal theory, whether in tort (including 
negligence), contract, or otherwise, unless required by applicable law (such as deliberate and 
grossly negligent acts) or agreed to in writing, shall any Contributor be liable to You for damages, 
including any direct, indirect, special, incidental, or consequential damages of any character 
arising as a result of this License or out of the use or inability to use the Work (including but not 
limited to damages for loss of goodwill, work stoppage, computer failure or malfunction, or any 
and all other commercial damages or losses), even if such Contributor has been advised of the 
possibility of such damages.
9. Accepting Warranty or Additional Liability. While redistributing the Work or Derivative Works 
thereof, You may choose to offer, and charge a fee for, acceptance of support, warranty, 
indemnity, or other liability obligations and/or rights consistent with this License. However, in 
accepting such obligations, You may act only on Your own behalf and on Your sole responsibility, 

<!-- Page 331 -->
not on behalf of any other Contributor, and only if You agree to indemnify, defend, and hold 
each Contributor harmless for any liability incurred by, or claims asserted against, such 
Contributor by reason of your accepting any such warranty or additional liability.
END OF TERMS AND CONDITIONS
APPENDIX: How to apply the Apache License to your work.
To apply the Apache License to your work, attach the following boilerplate notice, with the fields 
enclosed by brackets "[]" replaced with your own identifying information. (Don't include the 
brackets!) The text should be enclosed in the appropriate comment syntax for the file format. We 
also recommend that a file or class name and description of purpose be included on the same 
"printed page" as the copyright notice for easier identification within third-party archives.
Copyright [yyyy] [name of copyright owner] Licensed under the Apache License, Version 2.0 (the 
"License"); you may not use this file except in compliance with the License.
You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License 
is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
either express or implied. See the License for the specific language governing permissions and 
limitations under the License.
Assistance
Products may not be returned without prior authorization. Please inform us before returning 
equipment and obtain a return material authorization (RMA) number whether the repair is under 
warranty/guarantee or not. See Limited warranty for information on covered equipment.
Campbell Scientific regional offices handle repairs for customers within their territories. Please 
see the back page of the manual for a list of regional offices or visit 
www.campbellsci.com/contact 
  to determine which Campbell Scientific office serves your 
country.
When returning equipment, a RMA number must be clearly marked on the outside of the 
package. Please state the faults as clearly as possible.  Quotations for repairs can be given on 
request.
 It is the policy of Campbell Scientific to protect the health of its employees and provide a safe 
working environment. In support of this policy, when equipment is returned to Campbell 
Scientific, Logan, UT, USA, it is mandatory that a “Declaration of Hazardous Material and 
Decontamination” form  be received before the return can be processed.   If the form is not 

<!-- Page 332 -->
received within 5 working days of product receipt or is incomplete, the product will be returned 
to the customer at the customer’s expense. For details on decontamination standards specific to 
your country, please reach out to your regional Campbell Scientific office. 
NOTE:
All goods that cross trade boundaries may be subject to some form of fee (customs 
clearance, duties or import tax). Also, some regional offices require a purchase order upfront 
if a product is out of the warranty period. Please contact your regional Campbell Scientific 
office for details.
Safety
DANGER — MANY HAZARDS ARE ASSOCIATED WITH INSTALLING, USING, MAINTAINING, AND WORKING ON OR AROUND TRIPODS, 
TOWERS, AND ANY ATTACHMENTS TO TRIPODS AND TOWERS SUCH AS SENSORS, CROSSARMS, ENCLOSURES, ANTENNAS, ETC. 
FAILURE TO PROPERLY AND COMPLETELY ASSEMBLE, INSTALL, OPERATE, USE, AND MAINTAIN TRIPODS, TOWERS, AND 
ATTACHMENTS, AND FAILURE TO HEED WARNINGS, INCREASES THE RISK OF DEATH, ACCIDENT, SERIOUS INJURY, PROPERTY 
DAMAGE, AND PRODUCT FAILURE. TAKE ALL REASONABLE PRECAUTIONS TO AVOID THESE HAZARDS. CHECK WITH YOUR 
ORGANIZATION'S SAFETY COORDINATOR (OR POLICY) FOR PROCEDURES AND REQUIRED PROTECTIVE EQUIPMENT PRIOR TO 
PERFORMING ANY WORK.
Use tripods, towers, and attachments to tripods and towers only for purposes for which they are designed. Do not exceed design limits. 
Be familiar and comply with all instructions provided in product manuals. Manuals are available at www.campbellsci.com You are 
responsible for conformance with governing codes and regulations, including safety regulations, and the integrity and location of 
structures or land to which towers, tripods, and any attachments are attached. Installation sites should be evaluated and approved by a 
qualified engineer. If questions or concerns arise regarding installation, use, or maintenance of tripods, towers, attachments, or electrical 
connections, consult with a licensed and qualified engineer or electrician.
General
- Protect from over-voltage.
- Protect electrical equipment from water.
- Protect from electrostatic discharge (ESD).
- Protect from lightning.
- Prior to performing site or installation work, obtain required approvals and permits. Comply with all governing structure-height 
regulations, such as those of the FAA in the USA.
- Use only qualified personnel for installation, use, and maintenance of tripods and towers, and any attachments to tripods and 
towers. The use of licensed and qualified contractors is highly recommended.
- Read all applicable instructions carefully and understand procedures thoroughly before beginning work.
- Wear a hardhat and eye protection, and take other appropriate safety precautions while working on or around tripods and 
towers.
- Do not climb tripods or towers at any time, and prohibit climbing by other persons. Take reasonable precautions to secure tripod 
and tower sites from trespassers.
- Use only manufacturer recommended parts, materials, and tools.
Utility and Electrical
- You can be killed or sustain serious bodily injury if the tripod, tower, or attachments you are installing, constructing, using, or 
maintaining, or a tool, stake, or anchor, come in contact with overhead or underground utility lines.
- Maintain a distance of at least one-and-one-half times structure height, 6 meters (20 feet), or the distance required by applicable 
law, whichever is greater, between overhead utility lines and the structure (tripod, tower, attachments, or tools).
- Prior to performing site or installation work, inform all utility companies and have all underground utilities marked.

<!-- Page 333 -->
- Comply with all electrical codes. Electrical equipment and related grounding devices should be installed by a licensed and 
qualified electrician.
- Only use power sources approved for use in the country of installation to power Campbell Scientific devices.
Elevated Work and Weather
- Exercise extreme caution when performing elevated work.
- Use appropriate equipment and safety practices.
- During installation and maintenance, keep tower and tripod sites clear of un-trained or non-essential personnel. Take 
precautions to prevent elevated tools and objects from dropping.
- Do not perform any work in inclement weather, including wind, rain, snow, lightning, etc.
Internal Battery
- Be aware of fire, explosion, and severe-burn hazards.
- Misuse or improper installation of the internal lithium battery can cause severe injury.
- Do not recharge, disassemble, heat above 100 °C (212 °F), solder directly to the cell, incinerate, or expose contents to 
water. Dispose of spent batteries properly.
Use and disposal of batteries
- Where batteries need to be transported to the installation site, ensure they are packed to prevent the battery terminals shorting 
which could cause a fire or explosion. Especially in the case of lithium batteries, ensure they are packed and transported in a way 
that complies with local shipping regulations and the safety requirements of the carriers involved.
- When installing the batteries follow the installation instructions very carefully. This is to avoid risk of damage to the equipment 
caused by installing the wrong type of battery or reverse connections.
- When disposing of used batteries, it is still important to avoid the risk of shorting. Do not dispose of the batteries in a fire as there 
is risk of explosion and leakage of harmful chemicals into the environment. Batteries should be disposed of at registered 
recycling facilities.
Avoiding unnecessary exposure to radio transmitter radiation
- Where the equipment includes a radio transmitter, precautions should be taken to avoid unnecessary exposure to radiation from 
the antenna. The degree of caution required varies with the power of the transmitter, but as a rule it is best to avoid getting 
closer to the antenna than 20 cm (8 inches) when the antenna is active. In particular keep your head away from the antenna. For 
higher power radios (in excess of 1 W ERP) turn the radio off when servicing the system, unless the antenna is installed away from 
the station, e.g. it is mounted above the system on an arm or pole.
Maintenance
- Periodically (at least yearly) check for wear and damage, including corrosion, stress cracks, frayed cables, loose cable clamps, 
cable tightness, etc. and take necessary corrective actions.
- Periodically (at least yearly) check electrical ground connections.
WHILE EVERY ATTEMPT IS MADE TO EMBODY THE HIGHEST DEGREE OF SAFETY IN ALL CAMPBELL SCIENTIFIC PRODUCTS, THE 
CUSTOMER ASSUMES ALL RISK FROM ANY INJURY RESULTING FROM IMPROPER INSTALLATION, USE, OR MAINTENANCE OF TRIPODS, 
TOWERS, OR ATTACHMENTS TO TRIPODS AND TOWERS SUCH AS SENSORS, CROSSARMS, ENCLOSURES, ANTENNAS, ETC.

<!-- Page 334 -->
Australia
Location:
Phone:
Email:
Website:
Garbutt, QLD Australia
61.7.4401.7700
info@campbellsci.com.au
www.campbellsci.com.au
Brazil
Location:
Phone:
Email:
Website:
São Paulo, SP Brazil
11.3732.3399
vendas@campbellsci.com.br
www.campbellsci.com.br
Canada
Location:
Phone:
Email:
Website:
Edmonton, AB Canada
780.454.2505
dataloggers@campbellsci.ca
www.campbellsci.ca
China
Location:
Phone:
Email:
Website:
Beijing, P. R. China
86.10.6561.0080
info@campbellsci.com.cn
www.campbellsci.com.cn
Costa Rica
Location:
Phone:
Email:
Website:
San Pedro, Costa Rica
506.2280.1564
info@campbellsci.cc
www.campbellsci.cc
France
Location:
Phone:
Email:
Website:
Montrouge, France
0033.0.1.56.45.15.20
info@campbellsci.fr
www.campbellsci.fr
Germany
Location:
Phone:
Email:
Website:
Bremen, Germany
49.0.421.460974.0
info@campbellsci.de
www.campbellsci.de
India
Location:
Phone:
Email:
Website:
New Delhi, DL India
91.11.46500481.482
info@campbellsci.in
www.campbellsci.in
Japan
Location:
Phone:
Email:
Website:
Kawagishi, Toda City, Japan
048.400.5001
jp-info@campbellsci.com
www.campbellsci.co.jp
South Africa
Location:
Phone:
Email:
Website:
Stellenbosch, South Africa
27.21.8809960
sales@campbellsci.co.za
www.campbellsci.co.za
Spain
Location:
Phone:
Email:
Website:
Barcelona, Spain
34.93.2323938
info@campbellsci.es
www.campbellsci.es
Thailand
Location:
Phone:
Email:
Website:
Bangkok, Thailand
66.2.719.3399
info@campbellsci.asia
www.campbellsci.asia
UK
Location:
Phone:
Email:
Website:
Shepshed, Loughborough, UK
44.0.1509.601141
sales@campbellsci.co.uk
www.campbellsci.co.uk
USA
Location:
Phone:
Email:
Website:
Logan, UT USA
435.227.9120
info@campbellsci.com
www.campbellsci.com
Campbell Scientific Regional Offices
