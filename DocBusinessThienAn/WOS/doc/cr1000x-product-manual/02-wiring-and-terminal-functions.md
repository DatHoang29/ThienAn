---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 25-39
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 5: Bảng đấu dây và chức năng các chân (Wiring Panel & Terminals)

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 25 đến 39).  
> **Chủ đề chính**: Chi tiết các cọc đấu nối: Nguồn cấp (POWER IN), nguồn ra (12V, SW12, 5V), tiếp địa chống sét, các cổng tương tự (SE/DIFF), cổng xung (P1, P2), cổng truyền thông C1-C8 (SDI-12, RS-232, RS-485, SDM, CS I/O).

---


<!-- Page 25 -->
5. Wiring panel and terminal 
functions
 
The CR1000X/CR1000Xe wiring panel provides ports and removable terminals for connecting 
sensors, power, and communications devices. It is protected against surge, over-voltage, over-
current, and reverse power. The wiring panel is the interface to most data logger functions so 
studying it is a good way to get acquainted with the data logger. Functions of the terminals are 
broken down into the following categories: 
- Analog input
- Pulse counting
- Analog output
- Communications
- Digital I/O
- Power input
- Power output
- Power ground
- Signal ground 
Figure 5-1. CR1000Xe wiring panel
5. Wiring panel and terminal functions     8

<!-- Page 26 -->
Figure 5-2. CR1000X Wiring panel
Table 5-1: Analog input terminal functions
SE
DIFF
 1   2 
┌1┐
H   L
 3   4 
┌2┐
H   L
 5   6 
┌3┐
H   L
7   8
┌4┐
H   L
9  10
┌5┐
H   L
11  12
┌6┐
H   L
13  14
┌7┐
H   L
15  16
┌8┐
H   L
RG1 RG2
Single-Ended 
Voltage ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓    
Differential 
Voltage H L H L H L H L H L H L H L H L    
Ratiometric/Bridge ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓    
Thermocouple ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓    
Current Loop                                 ✓ ✓ 
Period Average ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓    
 
Table 5-2: Pulse counting terminal functions
  P1 P2 C1-C8
Switch-Closure ✓ ✓ ✓ 
High Frequency ✓ ✓ ✓ 
Low-level AC ✓ ✓  
5. Wiring panel and terminal functions     9

<!-- Page 27 -->
 
NOTE:
Conflicts can occur when a control port pair is used for different instructions (TimerInput(), 
PulseCount(), SDI12Recorder(), WaitDigTrig()). For example, if C1 is used for 
SDI12Recorder(), C2 cannot be used for TimerInput(), PulseCount(), or 
WaitDigTrig().
 
Table 5-3: Analog output terminal functions
  VX1-VX4
Switched Voltage Excitation ✓ 
 
Table 5-4: Voltage Output
  C1-C81 VX1-VX4 5V 12V SW12-1 SW12-2 SW12-
CSIO
5 VDC ✓ ✓ ✓        
3.3 VDC ✓ ✓          
12 VDC       ✓ ✓ ✓ 
✓ 
(CR1000Xe 
only)
1C terminal voltage levels are configured in pairs. The default voltage output from C terminals is 5 V. Use the 
PortPairConfig instruction in CRBasic to configure a C terminal pair to output 3.3 V.
 
Table 5-5: Communications terminal functions
  C1                     C2 C3 C4 C5 C6 C7 C8 RS-
232/CPI
SDI-12 ✓   ✓   ✓   ✓    
GPS PPS Rx Tx Rx Tx Rx Tx Rx  
TTL 0-5 V1 Tx Rx Tx Rx Tx Rx Tx Rx  
LVTTL 0-3.3 
V1 Tx Rx Tx Rx Tx Rx Tx Rx  
5. Wiring panel and terminal functions     10

<!-- Page 28 -->
Table 5-5: Communications terminal functions
  C1                     C2 C3 C4 C5 C6 C7 C8 RS-
232/CPI
RS-232*
Tx Rx Tx Rx
Tx Rx Tx Rx ✓ 
(CR1000Xe only)
RS-485 
(Half 
Duplex)
A- B+ A- B+
A- B+ A- B+  
(CR1000Xe only)
RS-4852 
(Full 
Duplex)
Tx- Tx+ Rx- Rx+
Tx- Tx+ Rx- Rx+  
(CR1000Xe only)
I2C SCL SDA SCL SDA SCL SDA SCL SDA  
SPI SCLK COPI CIPO   SCLK COPI CIPO    
SDM3 Data Clk Enabl   Data Clk Enabl    
CPI/CDM                 ✓ 
*ComC1 and ComC3 on the CR1000X are not designed to handle RS-232 signals, and long-term exposure—such as 
when connecting an RV50(X) modem—can damage the data logger.
1 TTL and LVTTL are configured with the CommsMode option of the SerialOpen instruction in CRBasic.
2 RS-422 compatible.
3 SDM can be on either C1-C3 or C5-C7, but not both at the same time.
Communications functions also include Ethernet and USB.
 
WARNING:
While ComC1–ComC3 are compatible with RS-232 signals on the CR1000Xe, only ComC5 and 
ComC7 are compatible with RS-232 signals on the CR1000X. ComC1 and ComC3 on the 
CR1000X are not designed to handle RS-232 signals, and long-term exposure—such as when 
connecting an RV50(X) modem—can damage the data logger. An advantage of the 
CR1000Xe is that ComC1, ComC3, ComC5, and ComC7 all support RS-232 signals, whereas 
the CR1000X supports RS-232 only on ComC5 and ComC7.
 
5. Wiring panel and terminal functions     11

<!-- Page 29 -->
Table 5-6: Digital I/O terminal functions
  C1-C8
General I/O ✓ 
Pulse-Width Modulation Output ✓ 
Timer Input ✓ 
Interrupt ✓ 
Quadrature ✓ 
5.1 Power input
The data logger requires a power supply. It can receive power from a variety of sources, operate 
for several months on non-rechargeable batteries, and supply power to many sensors and 
devices. The data logger operates with external power connected to the green POWER IN port 
on the face of the wiring panel. See Wiring panel and terminal functions (p. 8). The positive power 
wire connects to the 12V (CR1000X) or + (CR1000Xe) terminal. The negative wire connects to G. 
The power terminals are internally protected against polarity reversal and high voltage transients. 
For the CR1000X, if the voltage on the POWER IN terminals exceeds 19 V, power is shut off to 
certain parts of the data logger to prevent damaging connected sensors or peripherals. For the 
CR1000Xe, limit the voltage on the POWER IN terminals to 38Vdc or less.
The primary power source, which is often a transformer, power converter, or solar panel, 
connects to the charging regulator, as does a rechargeable battery. A third connection connects 
the charging regulator to the terminals of the POWER IN port. A UPS (uninterruptible power 
supply) is often the best power source for long-term installations. If external alkaline power is 
used, the alkaline battery pack is connected directly to the POWER IN port. An external UPS 
consists of a primary-power source, a charging regulator external to the data logger, and an 
external battery.
WARNING:
Sustained supply (input) voltages in excess of those listed in Power requirements can damage 
the transient voltage suppression. For the CR1000Xe, see Power requirements (p. 239). For 
the CR1000X, see Power requirements (p. 253).
Ensure that power supply components match the specifications of the device to which they are 
connected. 
5. Wiring panel and terminal functions     12

<!-- Page 30 -->
NOTE: For the CR1000X, to prevent voltage input issues with sensors and peripherals, do not 
use more than 16 V when powering them through the 12V, SW12-1, SW12-2,  or CS I/O port on 
the data logger.
When connecting power, switch off the power supply, insert the connector, then turn the power 
supply on. See Troubleshooting power supplies (p. 174) for more information.
The CR1000X/CR1000Xe can receive power via the POWER IN port as well as 5 VDC via a USB 
connection. If both POWER IN and USB are connected, power will be supplied by whichever has 
the highest voltage. If USB is the only power source, then the CS I/O port and the 12V, SW12, and 
5V terminals will not be operational. When powered by USB (no other power supplies connected) 
Status field Battery = 0. Functions that will be active with a 5 VDC source ( USB) include sending 
programs, adjusting data logger settings, and making some measurements.
NOTE:
The Status field Battery value and the destination variable from the Battery() instruction 
(often called batt_volt in the Public table) reference the external battery voltage. For 
information about  the internal battery, see Internal battery (p. 154).
5.1.1 Powering a data logger with a vehicle
If a data logger is powered by a motor-vehicle power supply, a second power supply may be 
needed. When starting the motor of the vehicle, battery voltage often drops below the voltage 
required for data logger operation. This may cause the data logger to stop measurements until 
the voltage again equals or exceeds the lower limit. A second supply or charge regulator can be 
provided to prevent measurement lapses during vehicle starting.
In vehicle applications, the earth ground lug should be firmly attached to the vehicle chassis with 
12 AWG wire or larger.
5.1.2 Power LED indicator 
When the data logger is powered, the Power LED will turn on according to power and program 
states:
- Off: No power, no program running.
- 1 flash every 10 seconds: Powered from BAT, program running.
- 3 flashes every 10 seconds: Powered via USB, program running.
- Always on: Powered, no program running.
5. Wiring panel and terminal functions     13

<!-- Page 31 -->
5.2 Power output
The data logger can be used as a power source for communications devices, sensors and 
peripherals. Take precautions to prevent damage to these external devices due to over- or 
under-voltage conditions, and to minimize errors. Additionally, exceeding current limits causes 
voltage output to become unstable. Voltage should stabilize once current is again reduced to 
within stated limits. 
The 12V power outputs differ between the CR1000X and CR1000Xe models. Unlike the CR1000X, 
the CR1000Xe provides a regulated 12V power output. See 12V Power output comparison 
between CR1000X and CR1000Xe (p. 14) for details.
Table 5-7: 12V Power output comparison between CR1000X and CR1000Xe
Port(s) CR1000X CR1000Xe Benefits of
CR1000Xe
12V, 
SW12-1, 
SW12-2
 Unregulated 12V power 
directly from the battery.
12V outputs allow up to 
~1A per channel, with a 
maximum of 3A per system
Regulated 12 Vdc power. 
12V outputs support up to 
2A per channel, with a 
maximum of 3.5A per 
system
Guarantees 12V output to 
power sensors and 
peripherals, even when a 
24V power supply is used
12V over 
CS I/O
 Unregulated 12V power 
over CS I/O port  via battery 
for simpler wiring of CS 
measurement peripherals 
and radios
Regulated 12V power and 
SW12 control are accessible 
via pin 8 of the CS I/O port
Allows for a hard power 
reset on external CS 
modems without the 
requirement for an inline 
device
Additional power outputs include:
- 5V: regulated 5 VDC.  The 5 VDC supply is regulated to within a few millivolts of 5 VDC, as 
long as the main power supply for the data logger does not drop below the minimum 
supply voltage. It is designed to power sensors or devices requiring a 5 VDC power supply.  
It is not intended as an excitation source for bridge measurements. The current output is 
shared with the CS I/O port; meaning the total current must remain within the specified 
limit. See 5 V fixed output  (p. 241) specifications for the CR1000Xe and 5 V fixed output  (p. 
255) specifications for the CR1000X.
CAUTION:
 For the CR1000X (not CR1000Xe), the voltage levels at the 12V and switched SW12 terminals, 
as well as pin  8 on the CS I/O port, are tied closely to the voltage levels of the main power 
5. Wiring panel and terminal functions     14

<!-- Page 32 -->
supply.  Therefore, if 16 VDC is supplied to the POWER IN 12V and G  terminals, the 12V, SW12 
terminals, and pin  8 on the CS I/O port will also supply 16 VDC to connected peripherals.  This 
could damage any connected peripheral or sensor not designed to handle that voltage.
- VX terminals:  supply precise output voltage used by analog sensors to generate high 
resolution and accurate signals.  In this case, these terminals are regularly used with 
resistive-bridge measurements (see Resistance measurements (p. 103) for more 
information). Using the SWVX() instruction, VX terminals can also supply a selectable, 
switched, regulated 3.3 or 5 VDC power source to power digital sensors and toggle control 
lines.
- C terminals: can be set low or high as output terminals . With limited drive capacity, digital 
output terminals are normally used to operate external relay-driver circuits. See also Digital 
input/output specifications (p. 262).
See also Power output specifications  (p. 254).
5.3 Grounds
Proper grounding lends stability and protection to a data acquisition system. Grounding the data 
logger with its peripheral devices and sensors is critical in all applications.  Proper grounding will 
ensure maximum ESD  protection and measurement accuracy. It is the easiest and least expensive 
insurance against data loss, and often the most neglected. The following terminals are provided 
for connection of sensor and data logger grounds:
- Signal Ground (
 ) - reference for single-ended analog inputs, excitation returns, and a 
ground for sensor shield wires.
- 11 common terminals
- sometimes called analog ground (AG)
- Power Ground (G) - return for 3.3 V, 5 V, 12 V, and digital sensors. Use of G grounds for 
these outputs minimizes potentially large current flow through the analog-voltage-
measurement section of the wiring panel, which can cause single-ended voltage 
measurement errors.
- 4 common terminals
5. Wiring panel and terminal functions     15

<!-- Page 33 -->
- Resistive Ground (RG) -   used for non-isolated 0-20 mA and 4-20 mA current loop 
measurements (see Current-loop measurements (p. 101) for more information). Also used 
for decoupling ground on RS-485 signals. Includes 100 Ω resistance to ground. Maximum 
voltage for RG terminals is ±16 V.
- 2 common terminals
- Earth Ground Lug (
 ) - connection point for heavy-gauge earth-ground wire. A good 
earth connection is necessary to secure the ground potential of the data logger and shunt 
transients away from electronics. Campbell Scientific recommends 14 AWG wire, minimum.
NOTE:
Several ground wires can be connected to the same ground terminal.
A good earth (chassis) ground will minimize damage to the data logger and sensors by providing 
a low-resistance path around the system to a point of low potential. Campbell Scientific 
recommends that all data loggers be earth  grounded. All components of the system (data 
loggers, sensors, external power supplies, mounts, housings) should be referenced to one 
common earth  ground.
In the field, at a minimum, a proper earth ground will consist of a 5-foot copper-sheathed 
grounding rod driven into the earth and connected to the large brass ground lug on the wiring 
panel with a 14 AWG wire. In low-conductive substrates, such as sand, very dry soil, ice, or rock, a 
single ground rod will probably not provide an adequate earth ground. For these situations, 
search for published literature on lightning protection or contact a qualified lightning-protection 
consultant.
In laboratory applications, locating a stable earth ground is challenging, but still necessary. In 
older buildings, new VAC receptacles on older VAC wiring may indicate that a safety ground 
exists when, in fact, the socket is not grounded. If a safety ground does exist, good practice 
dictates to verify that it carries no current. If the integrity of the VAC power ground is in doubt, 
also ground the system through the building plumbing, or use another verified connection to 
earth ground.
See also
- Ground loops (p. 181)
- Minimizing ground potential differences (p. 187)
- Understanding the Importance of Grounding 
- Connecting a Data Acquistion System to Earth Ground 
- Avoiding Ground Loops 
5. Wiring panel and terminal functions     16

<!-- Page 34 -->
5.4 Communications ports
The data logger is equipped with ports that allow communications with other devices and 
networks, such as:
- Computers
- Smart sensors
- Modbus and DNP3 outstation networks
- Ethernet
- Modems
- Campbell Scientific PakBus™ networks
- Other Campbell Scientific data loggers
Campbell Scientific data logger communications ports include:
- CS I/O
- RS-232/CPI
- USB Device
- Ethernet
- C terminals
5.4.1 USB device port
The  USB device port  supports communicating with a computer through data logger support 
software or through virtual Ethernet (RNDIS), and provides 5 VDC power to the data logger 
(powering through the USB port has limitations - details are available in the specifications). The 
data logger USB device port does not support USB flash or thumb drives. Although the USB 
connection supplies 5 V power, a 12 VDC battery will be needed for field deployment.
5.4.2 Ethernet port
The RJ45 10/100 Ethernet port is used for IP communications.
5.4.3 C terminals for communications
C terminals are configurable for the following communications types:
- SDI-12
- RS-232
- RS-422
- RS-485
- TTL (0 to 5 V)
5. Wiring panel and terminal functions     17

<!-- Page 35 -->
- LVTTL (0 to 3.3 V)
- SDM
Some communications types require more than one terminal, and some are only available on 
specific terminals. See Communications specifications (p. 264) for more information.
5.4.3.1 SDI-12 ports
SDI-12 is a 1200 baud protocol that supports many smart sensors. C1, C3, C5, and C7 can be 
configured as SDI-12 ports. Maximum cable lengths depend on the number of sensors 
connected, the type of cable used, and the environment of the application. Refer to the sensor 
manual for guidance.
For more information, see SDI-12 communications (p. 144).
5.4.3.2 RS-232, RS-422, RS-485, TTL, and LVTTL ports
RS-232, RS-422, RS-485, TTL, and LVTTL communications are typically used for the following:
- Reading sensors with serial output
- Creating a multi-drop network
- Communications with other data loggers or devices over long cables
NOTE:
The maximum cable length for RS-232 communication is typically limited to 50 feet (15 
meters) at 19200 baud. Higher baud rates may result in shorter transmission distances due to 
signal degradation. 
RS-485 supports a theoretical maximum point-to-point communication distance of 1200 
meters (4000 feet). To achieve this distance, it's essential to use well-shielded and insulated 
cable, ensure careful installation, apply bus termination, and maintain low data rates (baud) of 
less than 115200 bps.
Configure C terminals as serial ports using Device Configuration Utility or by using the 
SerialOpen() CRBasic instruction. Terminals are configured in pairs for TTL, LVTTL, RS-232, 
and half-duplex RS-422 and RS-485 communications. For full-duplex RS-422 and RS-485, four 
terminals are required. See also Communications protocols (p. 123).
5.4.3.3 SDM ports
SDM is a protocol proprietary to Campbell Scientific that supports several Campbell Scientific 
digital sensor and communications input and output expansion peripherals and select smart 
sensors. It uses a common bus and addresses each node. CRBasic SDM device and sensor 
5. Wiring panel and terminal functions     18

<!-- Page 36 -->
instructions configure terminals C1, C2, and C3 together to create an SDM port. Alternatively, 
terminals C5, C6, and C7 can be configured together to be used as the SDM port by using the 
SDMBeginPort() instruction.
See also Communications specifications (p. 264).
5.4.4 CS I/O port
One nine-pin port, labeled CS I/O, is available for communicating with a computer through 
Campbell Scientific communications interfaces, modems, and peripherals. Campbell Scientific 
recommends keeping CS I/O cables short (maximum of a few feet). See also Communications 
specifications (p. 264).
Table 5-8: CS I/O pinout
Pin
number Function Input (I)
Output (O) Description
1 5 VDC O 5 VDC: sources 5 VDC, used to power peripherals.
2 SG   Signal ground: provides a power return for pin 1 (5V), 
and is used as a reference for voltage levels.
3 RING I Ring: raised by a peripheral to put the 
CR1000X/CR1000Xe in the telecom mode.
4 RXD I Receive data: serial data transmitted by a peripheral are 
received on pin 4.
5 ME O Modem enable: raised when the CR1000X/CR1000Xe 
determines that a modem raised the ring line.
6 SDE O Synchronous device enable: addresses synchronous 
devices (SD); used as an enable line for printers.
7 CLK/HS I/O
Clock/handshake: with the SDE and TXD lines addresses 
and transfers data to SDs. When not used as a clock, pin 
7 can be used as a handshake line; during printer output, 
high enables, low disables.
5. Wiring panel and terminal functions     19

<!-- Page 37 -->
Table 5-8: CS I/O pinout
Pin
number Function Input (I)
Output (O) Description
8 12 VDC   Nominal 12 VDC power. Same power as 12V and SW12 
terminals.
9 TXD O
Transmit data: transmits serial data from the data logger 
to peripherals on pin 9; logic-low marking (0V), logic-
high spacing (5V), standard-asynchronous ASCII: eight 
data bits, no parity, one start bit, one stop bit. User 
selectable baud rates: 300, 1200, 2400, 4800, 9600, 
19200, 38400, 115200.
5.4.5 RS-232/CPI port 
The data logger includes one RJ45 module jack labeled RS-232/CPI. CPI is a proprietary interface 
for communications between Campbell Scientific data loggers and Campbell Scientific CDM 
peripheral devices and smart sensors. It consists of a physical layer definition and a data protocol. 
CDM devices are similar to Campbell Scientific SDM devices in concept, but the CPI bus enables 
higher data-throughput rates and use of longer cables. CDM devices require more power to 
operate in general than do SDM devices. CPI ports also enable networking between compatible 
Campbell Scientific data loggers.  Consult the manuals for CDM modules for more information.
CPI port power levels are controlled automatically by the CR1000X/CR1000Xe:
- Off: Not used.
- High power: Fully active.
- Low-power standby: Used whenever possible.
- Low-power bus: Sets bus and modules to low power.
When used with a Campbell Scientific RJ45-to-DB9 converter cable, the RS-232/CPI port can be 
used as an RS-232 port. It defaults to 115200 bps (in autobaud mode), 8 data bits, no parity, and 1 
stop bit. Use Device Configuration Utility or the SerialOpen() CRBasic instruction to change 
these options.
Table 5-9: RS-232/CPI pinout
Pin number Description
1 RS-232: Transmit (Tx)
2 RS-232: Receive (Rx)
5. Wiring panel and terminal functions     20

<!-- Page 38 -->
Table 5-9: RS-232/CPI pinout
Pin number Description
3 100 Ω Res Ground
4 CPI: Data
5 CPI: Data
6 100 Ω Res Ground
7 RS-232 CTS CPI: Sync
8 RS-232 DTR CPI: Sync
9 Not Used
5.5 Programmable logic control
The data logger can control instruments and devices such as:
- Controlling cellular modem or GPS receiver to conserve power.
- Triggering a water sampler to collect a sample.
- Triggering a camera to take a picture.
- Activating an audio or visual alarm.
- Moving a head gate to regulate water flows in a canal system.
- Controlling pH dosing and aeration for water quality purposes.
- Controlling a gas analyzer to stop operation when temperature is too low.
- Controlling irrigation scheduling.
Control decisions can be based on time, an event, or a measured condition. Controlled devices 
can be physically connected to C, VX, or SW12 terminals. Short Cut has provisions for simple 
on/off control. Control modules and relay drivers are available to expand and augment data 
logger control capacity.
- C terminals are selectable as binary inputs, control outputs, or communications ports. 
These terminals can be set low (0 VDC) or high (3.3 or 5 VDC) using the PortSet() or 
WriteIO() instructions. See the CRBasic Editor help for detailed instruction information and 
program examples: https://help.campbellsci.com/crbasic/cr1000x/ 
 . Other functions 
include device-driven interrupts, asynchronous communications and SDI-12 
communications. The high voltage for these terminals defaults to 5 V, but it can be 
5. Wiring panel and terminal functions     21

<!-- Page 39 -->
changed to 3.3 V using the PortPairConfig() instruction. Terminals C4, C5, and C7 
can also be configured for pulse width modulation with a maximum period of 36.4 s. A C 
terminal configured for digital I/O is normally used to operate an external relay-driver 
circuit because the terminal itself has limited drive capacity.
- VX terminals can be set low or high using the PortSet() or SWVX() instruction. For 
more information on these instructions, see the CRBasic help.
- SW12 terminals can be set low (0 V) or high (12 V) using the SW12() instruction (see the 
CRBasic help for more information).
The following  image illustrates a simple application wherein a C terminal configured for digital 
input, and another configured for control output are used to control a device (turn it on or off) 
and monitor the state of the device (whether the device is on or off).
In the case of a cell modem, control is based on time. The modem requires 12 VDC power, so 
connect its power wire to a data logger SW12 terminal. The following code snip turns the modem 
on for the first ten minutes of every hour using the TimeIsBetween() instruction embedded 
in an If/Then logic statement:
If TimeIsBetween (0,10,60,Min)Then
   SW12(SW12_1,1,1) 'Turn phone on.
Else
   SW12(SW12_1,0,1) 'Turn phone off.
EndIf
5. Wiring panel and terminal functions     22
