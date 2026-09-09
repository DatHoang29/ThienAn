---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 140-168
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 13: Các giao thức truyền thông công nghiệp (Modbus, SDI-12, Serial)

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 140 đến 168).  
> **Chủ đề chính**: Giao tiếp nối tiếp RS-232, RS-485, RS-422; Giao thức Modbus RTU & Modbus TCP (Client/Server, thanh ghi Holding, Coils, Function codes); Truyền thông Internet IP/HTTP/FTP; PakBus; và chuẩn cảm biến thời tiết SDI-12 (Transparent mode, Sniffer, Recorder mode).

---


<!-- Page 140 -->
13. Communications protocols
Data loggers communicate with data logger support software, other Campbell Scientific data 
loggers, and other hardware and software using a number of protocols including PakBus, 
Modbus, DNP3 outstation, CPI, SPI, and TCP/IP. Several industry-specific protocols are also 
supported.  CAN-bus is supported when using the Campbell Scientific SDM-CAN 
communications module. See also Communications specifications (p. 264) and Communications 
Protocols and Security Options for Campbell Scientific Data Loggers 
 .
13.1 General serial communications 124
13.2 Modbus communications 130
13.3 Internet communications 140
13.4 DNP3 communications 143
13.5 Serial peripheral interface (SPI) and I2C 143
13.6 PakBus communications 143
13.7 SDI-12 communications 144
Some communications services, such as satellite networks, can be expensive to send and receive 
information.  Best practices for reducing expense include:
- Declare Public only those variables that need to be public. Other variables should be 
declared as Dim.
- Be conservative with use of string variables and string variable sizes.  Make string variables 
as big as they need to be and no more. The default size, if not specified, is 24 bytes, but the 
minimum is 4 bytes.  Declare string variables Public and sample string variables into data 
tables only as needed.
- When using GetVariables() / SendVariables() to send values between data 
loggers, put the data in an array and use one command to get the multiple values.  Using 
one command to get 10 values from an array and swath of 10 is more efficient (requires 
only 1 transaction) than using 10 commands to get 10 single values (requires 10 
transactions). See the CRBasic Editor help for detailed instruction information and program 
examples: https://help.campbellsci.com/crbasic/cr1000x/ 
 .
13. Communications protocols     123

<!-- Page 141 -->
- Set the data logger to be a PakBus router only as needed.  When the data logger is a router, 
and it connects to another router like LoggerNet, it exchanges routing information with that 
router and, possibly (depending on your settings), with other routers in the network. 
Network Planner set this appropriately when it is used. This is also set through the IsRouter 
setting in the Settings Editor. For more information, see the Device Configuration Settings 
Editor IsRouter (p. 222).
- Set PakBus beacons and verify intervals properly.  For example, there is no need to verify 
routes every five minutes if communications are expected only every 6 hours. Network 
Planner will set this appropriately when it is used.  This is also set through the Beacon and 
Verify settings in the Settings Editor. For more information, see the Device Configuration 
Settings Editor Beacon() and Verify() settings.
For information on Designing a PakBus network using the Network Planner tool in LoggerNet, 
watch the following video: https://www.campbellsci.com/videos/loggernet-software-network-
planner 
 .
13.1 General serial communications
The data logger supports two-way serial communications. These communications ports can be 
used with smart sensors that deliver measurement data through serial-data protocols, or with 
devices such as modems, that communicate using serial data protocols.
CRBasic instructions for general serial communications include:
- SerialOpen()
- SerialClose()
- SerialIn()
- SerialInRecord()
- SerialInBlock()
- SerialInChk()
- SerialOut()
- SerialOutBlock()
- SerialBrk()
- SerialFlush()
See this application note for more information on interfacing serial sensors with Campbell 
Scientific data loggers: Serial Sensors | Interfacing with CS Data Loggers 
 .
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
To communicate over a serial port, it is important to be familiar with the protocol used by the 
device with which you will be communicating. Refer to the manual of the sensor or device to find 
its protocol and then select the appropriate options for each CRBasic parameter. See the 
13. Communications protocols     124

<!-- Page 142 -->
application note Interfacing Serial Sensors with Campbell Scientific Dataloggers 
  for more 
programming details and examples.
Configure C terminals as serial ports using Device Configuration Utility or by using the 
SerialOpen() CRBasic instruction. Terminals are configured in pairs for TTL, LVTTL, RS-232, 
and half-duplex RS-422 and RS-485 communications. For full-duplex RS-422 and RS-485, four 
terminals are required.
Figure 13-1. RS-232 single-ended full-duplex communications
Figure 13-2. RS-485/RS-422 differential-pair full-duplex communications
13. Communications protocols     125

<!-- Page 143 -->
Figure 13-3. RS-485 differential-pair half-duplex communications
13.1.1 RS-232
RS-232 supports point-to-point communications between one base (usually the data logger) and 
one external device. See Figure 13-1 (p. 125). Data bits are sent from the base to external devices 
across the transmit (Tx) line with respect to DC ground. The Tx line idle state is between –25 V 
and –3 V, depending on the transmitter. The transition from negative voltage to above 3 V 
begins data transmission.
NOTE:
Most RS-232 devices are also compatible with the data logger using TTL-inverted 
communications.
NOTE:
The data logger uses about -7 V to represent logic 1, and about 5.8 V to represent logic 0.
Figure 13-4. RS-232 Tx voltage with respect to GND
13. Communications protocols     126

<!-- Page 144 -->
13.1.2 RS-485
RS-485 supports communications between 32 base and 32 external devices. See Figure 13-3 (p. 
126) and Figure 13-2 (p. 125). Differential voltage between two lines (A & B) transmit data. When 
the voltage of B with respect to A is between -0.2 V and -5 V that is interpreted as logic 0. When 
the differential voltage in the range of positive 0.2 V to 5 V that is interpreted as logic 1.
NOTE:
The CR1000X/CR1000Xe uses about -1 V to represent logic 0, and about 1 V to represent logic 
1.
Figure 13-5. RS-485 Voltage B with respect to A
13.1.3 RS-422
RS-422 communications protocol is similar to RS-485. The difference is that RS-422 ranges from 
-6 V to 6 V instead of -5 V to 5 V. Also, RS-422 only supports communications from 1 base to 10 
external devices, but not return communications from all 10 external devices. In full-duplex point-
to-point (1 base, 1 external) RS-422 communications, both devices can transmit and receive. Half-
duplex can be used in cases where sensors broadcast data to a receiving data logger. See Figure 
13-3 (p. 126) and Figure 13-2 (p. 125).
NOTE:
Use the RS-485 communications type when setting up the data logger for RS-422 
communications. Most RS-422 sensors will work with RS-485 protocol.
13. Communications protocols     127

<!-- Page 145 -->
Figure 13-6. RS-422 Voltage B with respect to A
13.1.4 TTL
TTL supports point-to-point communications between one base and one external device. See 
Figure 13-1 (p. 125). Data bits are sent from base to external device with a voltage between 
transmit (Tx) and ground. The transmit line idle state is 5 V (logic 1). Data is sent after one clock 
cycle once the voltage is pulled low (to 0 V).
Figure 13-7. TTL Tx voltage with respect to GND
13.1.5 LVTTL
The only difference between low-voltage TTL (LVTTL) and TTL is that the voltage range is 0 V to 
3.3 V. See Figure 13-1 (p. 125). 
13. Communications protocols     128

<!-- Page 146 -->
Figure 13-8. LVTTL Tx voltage with respect to GND
13.1.6 TTL-Inverted
The only difference between TTL-inverted and TTL is that the logic is inverted. The idle state for 
TTL-inverted is 0 V instead of 5 V. See Figure 13-1 (p. 125). Data is sent after  the voltage is pulled 
high (to 5 V).
NOTE:
Many RS-232 devices are compatible with this communications protocol.
Figure 13-9. TTL-inverted Tx voltage with respect to GND
13.1.7 LVTTL-Inverted
The only difference between LVTTL-inverted and TTL-inverted is that the voltage range is 0 V to 
3.3 V. See Figure 13-1 (p. 125).
13. Communications protocols     129

<!-- Page 147 -->
Figure 13-10. LVTTL-inverted Tx voltage with respect to GND
13.2 Modbus communications
The data logger supports Modbus RTU, Modbus ASCII, and Modbus TCP protocols and can be 
programmed as a Modbus client (master) or Modbus server (slave). These protocols are often 
used in SCADA networks. Data loggers can communicate using Modbus on all available 
communications ports. The data logger conducts Modbus over TCP using an Ethernet or 
Wireless connection. The data logger supports RTU and ASCII communications modes on RS-232 
and RS-485 connections.
CRBasic Modbus instructions include:
- ModbusClient()
- ModbusServer()
- MoveBytes()
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
For additional information on Modbus, see:
- About Modbus (p. 131)
- Why Modbus Matters: An Introduction 
- How to Access Live Measurement Data Using Modbus 
- Using Campbell Scientific Data Loggers as Modbus Slave Devices in a SCADA Network 
Because Modbus has a set command structure, programming the data logger to get data from 
field instruments can be much simpler than from some other serial sensors. Because Modbus 
uses a common bus and addresses each node, field instruments are effectively multiplexed to a 
data logger without additional hardware.
When doing Modbus communications over RS-232, the data logger, through Device 
Configuration Utility or the Settings editor, can be set to keep communications ports open and 
13. Communications protocols     130

<!-- Page 148 -->
awake, but at higher power usage. Set RS-232Power to Always on. Otherwise, the data logger 
goes into sleep mode after 40 seconds of communications inactivity. Once asleep, two packets 
are required before it will respond. The first packet awakens the data logger; the second packet is 
received as data. This would make a Modbus client fail to poll the data logger, if not using retries. 
More information on Modbus can be found at:
- www.simplyModbus.ca/FAQ.htm 
- www.Modbus.org/tech.php 
- www.lammertbies.nl/comm/info/modbus.html 
13.2.1 About Modbus
Modbus is a communications protocol that enables communications among many devices 
connected to the same network. Modbus is often used in supervisory control and data 
acquisition (SCADA) systems to connect remote terminal units (RTUs) with a supervisory 
computer - allowing them to relay measurement data, device status, control commands, and 
configuration information.
The popularity of Modbus has grown because it is freely available and because its messaging 
structure is independent of the type of physical interface or connection that is used. Modbus can 
coexist with other types of connections on the same physical interface at the same time. You can 
operate the protocol over several data links and physical layers.
Modbus is supported by many industrial devices, including those offered by Campbell Scientific. 
Not only can intelligent devices such as microcontrollers and programmable logic controllers 
(PLCs) communicate using Modbus, but many intelligent sensors have a Modbus interface that 
enables them to send their data to host systems. Examples of using Modbus with Campbell 
Scientific data loggers include:
- Interfacing data loggers and Modbus-enabled sensors.
- Sending and retrieving data between data loggers and other industrial devices.
- Delivering environmental data to SCADA systems.
- Integrating Modbus data into PakBus networks, or PakBus data into Modbus networks.
13. Communications protocols     131

<!-- Page 149 -->
13.2.2 Modbus protocols
There are three standard variants of Modbus protocols:
- Modbus RTU — Modbus RTU is the most common implementation available for Modbus. 
Used in serial communications, data is transmitted in a binary format. The RTU format 
follows the commands/data with a cyclic redundancy check checksum.
NOTE:
The Modbus RTU protocol standard does not allow a delay between characters of 1.5 
times or more the length of time normally required to receive a character. This is 
analogous to “pizza” being understood, and “piz   za” being gibberish. It's important to 
note that communications hardware used for Modbus RTU, such as radios, must 
transfer data as entire packets without injecting delays in the middle of Modbus 
messages.
- Modbus ASCII — Used in serial communications, data is transmitted as an ASCII 
representation of the hexadecimal values. Timing requirements are loosened, and a simpler 
longitudinal redundancy check checksum is used.
- Modbus TCP/IP or Modbus TCP — Used for communications over TCP/IP networks. The 
TCP/IP format does not require a checksum calculation, as lower layers already provide 
13. Communications protocols     132

<!-- Page 150 -->
checksum protection. The packet structure is similar to RTU, but uses a different header. 
Devices labeled as Modbus gateways will convert from Modbus TCP to Modbus RTU.
Campbell Scientific data loggers support Modbus RTU, Modbus ASCII, and Modbus TCP 
protocols. If the connection is over IP, Campbell Scientific data loggers always use Modbus TCP. 
Modbus server functionality over other comports use RTU. When acting as a client, the data 
logger can be switched between ASCII and RTU protocols using an option in the 
ModbusClient() instruction. See the CRBasic Editor help for detailed instruction information 
and program examples: https://help.campbellsci.com/crbasic/cr1000x/ 
 .
13.2.3 Understanding Modbus Terminology
Many of the object types are named from using Modbus in driving relays: a single-bit physical 
output is called a coil, and a single-bit physical input is called a discrete input or a contact.
Information is stored in the server device in up to four different tables. Two tables store on/off 
discrete values (coils) and two store numerical values (registers). The coils and registers each have 
a read-only table and read/write table.
13.2.4 Connecting Modbus devices
Data loggers can communicate with Modbus on all available communications ports. 
Consideration should be given to proper surge protection of any cabled connection. Between 
systems of significantly different ground potential, optical isolation may be appropriate. For 
additional information on grounds, see Grounds (p. 15).
The common serial interface used for Modbus RTU connections is RS-485 half-duplex, or two-
wire RS-485. This connection uses one differential pair for data, and another wire for a signal 
ground. When twisted pair cable is used, the signal can travel long distances. Resistors are often 
used to reduce noise. Bias resistors are used to give a clean default state on the signal lines. For 
long cable lengths, termination resistors, which are usually 120 ohms, are needed to stop data 
corruption due to reflections. Signal grounds are terminated to earth ground with resistors to 
prevent ground loops, but allow a common mode signal. The resistors to ground are usually 
integral to the equipment. The resistive ground is labeled as  RG on Campbell Scientific 
equipment.
13.2.5 Modbus client-server protocol
Modbus is a client-server protocol. The device requesting the information is called the Modbus 
client, and the devices supplying information are Modbus servers. In a standard Modbus 
13. Communications protocols     133

<!-- Page 151 -->
network, there is one client and up to 247 servers. A client does not have a Modbus address. 
However, each Modbus server on a shared network has a unique address from 1 to 247.
A single Modbus client device initiates commands (requests for information), sending them to 
one or more Modbus server devices on the same network. Only the Modbus client can initiate 
communications. Modbus servers, in turn, remain silent, communicating only when responding 
to requests from the Modbus client.
Every message from the client will begin with the server address, followed by the function code, 
function parameters, and a checksum. The server will respond with a message beginning with its 
address, followed by the function code, data, and a checksum. The amount of data in the packet 
will vary, depending on the command sent to the server. Server devices only process one 
command at a time. So, the client needs to wait for a response, or timeout before sending the 
next command.
A broadcast address is specified to allow simultaneous communications with all servers. Because 
response time of server devices is not specified by the standard, and device manufacturers also 
rarely specify a maximum response time, broadcast features are rarely used. When implementing 
a system, timeouts in the client will need to be adjusted to account for the observed response 
time of the servers.
Campbell Scientific data loggers can be programmed to be a Modbus client or Modbus server - 
or even both at the same time! This proves particularly helpful when your data logger is a part of 
two wider area networks. In one it uses Modbus to query data (as a client) from localized sensors 
or other data sources, and then in the other, it serves that data up (as a server) to another 
Modbus client.
13.2.6 About Modbus programming
Modbus capability of the data logger must be enabled through configuration or programming. 
See the CRBasic Editor help for detailed information on program structure, syntax, and each 
instruction available to the data logger.
CRBasic Modbus instructions include:
- ModbusClient()
- ModbusServer()
- MoveBytes()
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
13. Communications protocols     134

<!-- Page 152 -->
13.2.6.1 Endianness
Endianness refers to the sequential order in which bytes are arranged into larger numerical 
values when stored in memory. Words may be represented in big-endian or little-endian format, 
depending on whether bits or bytes or other components are ordered from the big end (most 
significant bit) or the little end (least significant bit).
In big-endian format, the byte containing the most significant bit is stored first, then the 
following bytes are stored in decreasing significance order, with the byte containing the least 
significant bit stored last. Little-endian format reverses this order: the sequence stores the least 
significant byte first and the most significant byte last. Endianness is used in some Modbus 
programming so it is important to note that the CR1000X/CR1000Xe is a big-endian instrument.
13.2.6.2 Function codes
A function code tells the server which storage entity to access and whether to read from or write 
to that entity. Different devices support different functions (consult the device documentation for 
support information). The most commonly used functions (codes 01, 02, 03, 04, 05, 15, and 16 ) 
are supported by Campbell Scientific data loggers.
Most users only require the read- register functions. Holding registers are read with function 
code  03. Input registers are read with function code  04. This can be confusing, because holding 
registers are usually listed with an offset of 40,000 and input registers with an offset of 30,000. 
Don’t mix up the function codes. Double check the register type in the device documentation.
Function code Action Entity
01 (01 hex) Read Discrete Output Coils
05 (05 hex) Write single Discrete Output Coil
15 (0F hex) Write multiple Discrete Output Coils
02 (02 hex) Read Discrete Input
04 (04 hex) Read Input Registers
03 (03 hex) Read  Holding Registers
06 (06 hex) Write single Holding Register
16 (10 hex)  Write multiple Holding Registers
The write-register functions will only work on holding registers. Function  06 only changes one 16-
bit register, whereas function 16, changes multiple registers. Note, when writing registers, the 
Variable parameter for the ModbusClient() instruction refers to a source, not a 
destination.
13. Communications protocols     135

<!-- Page 153 -->
13.2.7 Modbus information storage
With the Modbus protocol, most of the data values you want to transmit or receive are stored in 
registers. Information is stored in the server device in four different entities. Two store on/off 
discrete values (coils) and two store numerical values (registers). The four entities include:
- Coils – 1-bit registers, used to control discrete outputs (including Boolean values), 
read/write.
- Discrete Input – 1-bit registers, used as inputs, read only.
- Input Registers – 16-bit registers, used as inputs, read only.
- Holding Registers – 16-bit registers; used for inputs, output, configuration data, or any 
requirement for “holding” data; read/write.
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
13.2.7.1 Registers
In a 16-bit memory location, a 4-byte value takes up two registers. The Modbus protocol always 
refers to data registers with a starting address number, and a length to indicate how many 
registers to transfer.
Campbell Scientific uses 1-based numbering (a common convention for numbering registers in 
equipment) in the ModbusClient() instruction. With 1-based numbering, the first data 
location is referred to as register number 1. Some equipment uses 0-based numbering (check the 
equipment documentation). With 0-based numbering, the first register is referred to as 0.
Reading register numbers can be complicated by the fact that register numbers are often written 
with an offset added. Input registers are written with an offset of 30000. So, the first input register 
is written as 30001, with 1-based numbering. Holding registers are numbered with an offset of 
40000. You must remove the offset before writing the number as the Start parameter of 
ModbusClient().
There are rare instances when equipment is designed with the registers mapped including the 
offset. That means 40001 in the documentation is really register number 40001. Those are rare 
instances, and the equipment is deviating from standards. If 1 or 2 don’t work for the Start 
parameter, try 40001 and 40002.
13.2.7.2 Coils
Discrete digital I/O channels in Modbus are referred to as coils. The term coil has its roots in 
digital outputs operating solenoid coils in an industrial environment. Coils may be read only or 
13. Communications protocols     136

<!-- Page 154 -->
read/write. A read only coil would be a digital input. A read/write coil is used as an output. Coils 
are read and manipulated with their own function codes, apart from the registers. Many modern 
devices do not use coils at all.
When working with coils, the data logger requires Boolean variables. When reading coils, each 
Boolean in an array will hold the state of one coil. A value of  True will set the coil, a value of  False 
will unset the coil.
13.2.7.3 Data Types
Modbus does not restrict what data types may be contained within holding and input registers. 
Equipment manufacturers need to indicate what binary data types they are using to store data. 
Registers are 16-bit, so 32-bit data types use 2 registers each. Some devices combine more 
registers together to support longer data types like strings. The ModbusClient() instruction 
has a ModbusOption parameter that supports several different data types.
When data types use more than 1 register per value, the register order within the data value is 
important. Some devices will swap the high and low bytes between registers. You can 
compensate for this by selecting the appropriate ModbusOption.
Byte order is also important when communicating data over Modbus. Big Endian byte order is 
the reverse of Little Endian byte order. It may not always be apparent which a device uses. If you 
receive garbled data, try reversing the byte order. Reversing byte order is done using the 
MoveBytes() instruction. There is an example in CRBasic help for reversing the bytes order of a 
32-bit variable.
After properly reading in a value from a Modbus device, you might have to convert the value to 
proper engineering units. With integer data types, it is common to have the value transmitted in 
hundredths or thousandths.
Unsigned 16-bit integer
The most basic data type used with Modbus is unsigned 16-bit integers. It is the original Modbus 
data type with 1 register per value. On the data logger, declare your destination variable as type  
Long. A  Long is a 32-bit signed integer that contains the value received. Select the appropriate 
ModbusOption to avoid post-processing.
Signed 16-bit integer
Signed 16-bit integers use 1 register per value. On the data logger, declare your destination 
variable as type  Long. A  Long is a 32-bit signed integer that contains the value received . Select 
the appropriate ModbusOption to avoid post-processing.
13. Communications protocols     137

<!-- Page 155 -->
Signed 32-bit integer
Signed 32-bit integers require two registers per value. This data type corresponds to the native  
Long variable type in Campbell data loggers. Declare your variables as type  Long before using 
them as the Variable parameter in ModbusClient(). Select the appropriate ModbusOption 
to avoid post-processing.
Unsigned 32-bit integer
Unsigned 32-bit integers require two registers per value. Declare your variables as type  Long 
before using them as the Variable parameter in ModbusClient(). The  Long data type is a 
signed integer, and does not have a range equal to that of an unsigned integer. If the integer 
value exceeds 2,147,483,647 it will display incorrectly as a negative number. If the value does not 
exceed that number, there are no issues with a variable of type  Long holding it.
32-Bit floating point
32-bit floating point values use 2 registers each. This is the default FLOAT data type in Campbell 
Scientific data loggers. Select the appropriate ModbusOption to avoid post-processing.
13.2.8 Modbus tips and troubleshooting
Most of the difficulties with Modbus communications arise from deviations from the standards, 
which are not enforced within Modbus. Whether you are connecting via Modbus to a solar 
inverter, power meter, or flow meter, the information provided here can help you overcome the 
challenges, and successfully gather data into a Campbell data logger. Further information on 
Modbus can be found at:
- www.simplyModbus.ca/FAQ.htm 
- www.Modbus.org/tech.php 
- www.lammertbies.nl/comm/info/modbus.html 
 For additional information on Modbus troubleshooting, see Modbus Troubleshooting Guide 
 .
13.2.8.1 Error codes
Modbus defines several error codes, which are reported back to a client from a server. 
ModbusClient() displays these codes as a negative number. A positive result code indicates 
no response was received.
13. Communications protocols     138

<!-- Page 156 -->
Result code -01: illegal function
The illegal function error is reported back by a Modbus server when either it does not support 
the function at all, or does not support that function code on the requested registers. Different 
devices support different functions (consult the device documentation). If the function code is 
supported, make sure you are not trying to write to a register labeled as read-only. It is common 
for devices to have holding registers where read-only and read/write registers are mapped next 
to each other.
An uncommon cause for the  -01 result is a device with an incomplete implementation of 
Modbus. Some devices do not fully implement parsing Modbus commands. Instead, they are 
hardcoded to respond to certain Modbus messages. The result is that the device will report an 
error when you try selectively polling registers. Try requesting all of the registers together.
Result code -02: illegal data address
The illegal data address error occurs if the server rejects the combination of starting register and 
length used. One possibility, is a mistake in your program on the starting register number. Refer 
to the earlier section about register number and consult the device documentation for support 
information. Also, too long of a length can trigger this error. The ModbusClient() instruction 
uses length as the number of values to poll. With 32-bit data types, it requests twice as many 
registers as the length.
An uncommon cause for the  -02 result is a device with an incomplete implementation of 
Modbus. Some devices do not fully implement parsing Modbus commands. Instead, they are 
hard coded to respond to certain Modbus messages. The result is that the device will report an 
error when you try selectively polling registers. Try requesting all of the registers together.
Result code -11: COM port error
Result code  -11 occurs when the data logger is unable to open the COM port specified. For serial 
connections, this error may indicate an invalid COM port number. For Modbus TCP, it indicates a 
failed socket connection.
If you have a failed socket connection for Modbus TCP, check your TCPOpen() instruction. The 
socket returned from TCPOpen() should be a number less than 99.  Provided the data logger 
has a working network connection, further troubleshooting can be done with a computer 
running Modbus software. Connect the computer to the same network and attempt to open a 
Modbus TCP connection to the problem server device. Once you resolve the connection 
between the computer and the server device, the connection from the data logger should work.
13. Communications protocols     139

<!-- Page 157 -->
13.3 Internet communications
See the Communications specifications (p. 264) for a list of the internet protocols supported by 
the data logger. The most up-to-date information on implementing these protocols is contained 
in CRBasic Editor help.
CRBasic instructions for internet communications include:
- DHCPRenew()
- DNSQuery()
- EmailRelay()
- EmailSend()
- EmailRecv()
- FTPClient()
- HTTPGet()
- HTTPOut()
- HTTPPost()
- HTTPPut
- IPInfo()
- MQTTConnect()
- MQTTPublish()
- MQTTPublishConstTable()
- MQTTPublishMeta()
- MQTTPubishTable()
- NetworkTimeProtocol()
- PingIP()
- PPPOpen()
- PPPClose()
- TCPOpen()
- TCPClose()
- UDPDatagram()
- UDPOpen()
- UDPSocketClose()
- UDPSocketOpen()
- UDPSocketRecv()
- UDPSocketSend()
Once the hardware has been configured, PakBus communications over TCP/IP are possible. 
These functions include the following:
- Sending programs
- Retrieving programs
- Setting the data logger clock
- Collecting data
- Displaying the current record in a data table
Data logger callback to LoggerNet and data logger-to-data logger communications are also 
possible over TCP/IP.  For details and example programs see the CRBasic help. 
See the FTP streaming technical paper 
  for information on using FTPClient() or HTTPPut
() to stream data. 
See Using MQTT with Campbell Scientific Data Loggers 
  for information on using MQTT.
See the HTTP troubleshooting technical paper 
  for HTTP troubleshooting information.
13. Communications protocols     140

<!-- Page 158 -->
13.3.1 IP address
When connected to a server with a list of IP addresses available for assignment, the data logger 
will automatically request and obtain an IP address through DHCP. Once the address is assigned,  
look in the Settings Editor > Ethernet > {information box} to see the assigned IP address.
The CR1000X/CR1000Xe provides a DNS client that can query a DNS server to determine if an IP 
address has been mapped to a hostname. If it has, then the hostname can be used 
interchangeably with the IP address in some data logger instructions.
NOTE:
When setting a static IP address, first manually set a DNS Server Address in Settings Editor > 
Advanced.
13.3.2 HTTPS server
Use Device Configuration Utility to configure the data logger to act as an HTTPS server. 
13.3.3 FTP server
An FTP server facilitates file transfers. Use Device Configuration Utility to configure the data 
logger to act as an FTP server. This is useful when receiving and storing images from an Ethernet 
enabled device such as a camera. 
Select FTPEnabled (p. 218) and assign a User Name and Password.
NOTE:
FTP is disabled by default. However, when enabled, the UID will be the default FTP password.
CAUTION:
Passwords may be changed from the default UID. However, when the data logger is reset to 
factory defaults or a new OS is sent, the default password will revert to the UID.
13. Communications protocols     141

<!-- Page 159 -->
Allocate memory where the received files will be stored. Often this is on the USR drive. Data 
memory (p. 88)
WARNING:
Partitioning or changing the size of the USR drive will delete stored data from tables. Collect 
data first.
Specify the memory drive in the path when putting or getting files. For example, to put a file 
named image.jpg on the USR drive, use a command similar to put image.jpg /USR/image.jpg.
13. Communications protocols     142

<!-- Page 160 -->
NOTE:
Use FTPclient() to send files to a remote server. This is different than setting up the data 
logger to act as an FTP server. See the FTP Streaming technical paper 
  and FTP 
Troubleshooting technical paper 
  for more information. 
13.4 DNP3 communications
DNP3 is designed to optimize transmission of data and control commands from a master 
computer to one or more remote devices or outstations. The  data logger allows DNP3 
communications on all available communications ports. CRBasic DNP3 instructions include:
- DNP()
- DNPUpdate()
- DNPVariable()
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
When DNPUpdate() is used to set up the data logger as a remote (slave) device, up to three 
DNP3 clients (masters) are supported.
For additional information on DNP3 see:
- DNP3 with Campbell Scientific Dataloggers 
- Getting to Know DNP3 
- How to Access Your Measurement Data Using DNP3 
13.5 Serial peripheral interface (SPI) and I2C
Serial Peripheral Interface is a clocked synchronous interface, used for short distance 
communications, generally between embedded devices. I2C is a multi-controller (master), multi-
peripheral (slave), packet switched, single-ended, serial computer bus. I2C is typically used for 
attaching lower-speed peripheral ICs to processors and microcontrollers in short-distance, intra-
board communications. I2C and SPI are protocols supported by the operating system.  See 
CRBasic Editor help for instructions that support these protocols.
For additional information on I2C, see www.i2c-bus.org 
 .
13.6 PakBus communications
PakBus is a Campbell Scientific communications protocol. By using signed data packets, PakBus 
increases the number of communications and networking options available to the data logger. 
13. Communications protocols     143

<!-- Page 161 -->
The data logger allows PakBus communications on all available communications ports.  For 
additional information, see The Many Possibilities of PakBus Networking 
  blog article.
Advantages of PakBus include:
- Simultaneous communications between the data logger and other devices.
- Peer-to-peer communications - no computer required. Special CRBasic instructions 
simplify transferring data between data loggers for distributed decision making or control.
- Data consolidation - other PakBus data loggers can be used as "sensors" to consolidate all 
data into one data logger.
- Routing - the data logger can act as a router, passing on messages intended for another 
Campbell Scientific data logger. PakBus supports automatic route detection and selection.
- Short distance networks - a data logger can talk to another data logger over distances up 
to 30 feet by connecting transmit, receive, and ground wires between the data loggers.
In a PakBus network, each data logger is assigned a unique address. The default PakBus address 
in most devices is 1. To communicate with the data logger, the data logger support software must 
know the data logger PakBus address. The PakBus address is changed using Device Configuration 
Utility, data logger Settings Editor, or PakBus Graph software.
13.7 SDI-12 communications
SDI-12 is a 1200 baud communications protocol that supports many smart sensors, probes and 
devices. The data logger supports SDI-12 communications through two modes — transparent 
mode and programmed mode (see SDI-12 ports (p. 18) for wiring terminal information).
Conflicts can occur when a control port pair is used for different instructions (TimerInput(), 
PulseCount(), SDI12Recorder(), WaitDigTrig()). For example, if C1 is used for 
SDI12Recorder(), C2 cannot be used for TimerInput(), PulseCount(), or 
WaitDigTrig().
Transparent mode facilitates sensor setup and troubleshooting. It allows commands to be 
manually issued and the full sensor response viewed. Transparent mode does not record data. 
See SDI-12 transparent mode (p. 178) for more information.
Programmed mode automates much of the SDI-12 protocol and provides for data recording. See 
SDI-12 programmed mode/recorder mode (p. 149) for more information.
13. Communications protocols     144

<!-- Page 162 -->
CRBasic SDI-12 instructions include:
- SDI12Recorder()
- SDI12SensorSetup()
- SDI12SensorResponse()
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
The data logger uses SDI-12 version 1.4. 
See also
- www.sdi-12.org 
- SDI-12 Sensors Troubleshooting tips 
     
13.7.1 SDI-12 transparent mode
All SDI-12 probes have just three wires—a signal, ground, and 12 V power line. They are 
connected to the data logger according to the following table.
Table 13-1: SDI-12 probe connections
Wire function Data logger connection
SDI-12 signal C
Shield G
Power 12V
Power ground G
System operators can manually interrogate and enter settings in probes, connected to the data 
logger, using transparent mode. Transparent mode is useful in troubleshooting SDI-12 systems 
because it allows direct communications with probes.
Transparent mode may need to wait for commands issued by the programmed mode to finish 
before sending responses.  While in transparent mode, the data logger programs may not 
execute.  Data logger security may need to be unlocked before transparent mode can be 
activated.
Transparent mode is entered while the computer is communicating with the data logger through 
a terminal emulator program such as through  Device Configuration Utility or other data logger 
support software. Keyboard displays cannot be used. For how-to instructions for communicating 
directly with an SDI-12 sensor using a terminal emulator, watch this 
video: https://www.campbellsci.com/videos/sdi12-sensors-transparent-mode 
 .
13. Communications protocols     145

<!-- Page 163 -->
To enter the SDI-12 transparent mode, enter the data logger support software terminal emulator:
 1. Press Enter until the data logger responds with the prompt CR1000X>. 
 2. Type SDI12 at the prompt and press Enter.
 3.  In response, the query Select SDI12 Port is presented with a list of available ports.  
Enter the port number assigned to the terminal to which the SDI-12 sensor is connected, 
and press Enter.  For example, 1 is entered for terminal C1.
 4.  An Entering SDI12 Terminal response indicates that SDI-12 transparent mode is 
active and ready to transmit SDI-12 commands and display responses.
13.7.1.1 Watch command (sniffer mode)
The terminal-mode utility allows monitoring of SDI-12 traffic by using the watch command 
(sniffer mode). Watch an instructional video: https://www.campbellsci.com/videos/sdi12-sensors-
watch-or-sniffer-mode 
  or use the following instructions.
 1. Enter the transparent mode as described previously.
 2. Press Enter until a CR1000X> prompt appears.
 3. Type W and then press Enter.
 4. In response, the query Select SDI12 Port: is presented with a list of available ports. 
Enter the port number assigned to the terminal to which the SDI-12 sensor is connected, 
and press Enter.
 5. In answer to Enter timeout (secs): type 100 and press Enter.
 6. In response to the query ASCII (Y)?, type Y and press Enter.
 7. SDI-12 communications are then opened for viewing.
13. Communications protocols     146

<!-- Page 164 -->
13.7.1.2 SDI-12 transparent mode commands
SDI-12 commands and responses are defined by the SDI-12 Support Group (www.sdi-12.org 
 ) 
and are available in the SDI-12 Specification 
 . Sensor manufacturers determine which 
commands to support. Commands have three components:
- Sensor address ( a): A single character and the first character of the command.  Sensors are 
usually assigned a default address of zero by the manufacturer.  The wildcard address ( ?) is 
used in the Address Query command.  Some manufacturers may allow it to be used in 
other commands. SDI-12 sensors accept addresses 0 through 9, a through z, and A through 
Z.
- Command body (for example,  M1): An upper case letter (the “command”) followed by 
alphanumeric qualifiers.
- Command termination ( !): An exclamation mark.
An active sensor responds to each command.  Responses have several standard forms and 
terminate with <CR><LF> (carriage return–line feed).
13.7.1.3 aXLOADOS! command
aXLOADOS! is an example of an SDI-12 transparent mode command. It is used to send an 
operating system (OS) update from a data logger to an SDI-12 sensor. 
NOTE:
Verify with the sensor manufacturer that the sensor supports this command. 
Use aXLOADOS! in the following procedure to send an OS update to an SDI-12 sensor.
 1. Supply power to the data logger. If connecting via USB for the first time, you must first 
install USB drivers by using Device Configuration Utility (select your data logger, then on 
the main page, click Install USB Driver). Alternatively, you can install the USB drivers using 
EZ Setup. A USB connection supplies 5 V power (as well as a communications link), which is 
adequate for setup, but a 12 V battery will be needed for field deployment. 
 2. Physically connect your data logger to your computer using a USB cable, then in Device 
Configuration Utility select your data logger.
 3. Copy the sensor OS file from the computer to the data logger. 
 a. Select File Control > CPU: drive > Send and navigate to the file on the computer.
 b. Click Open.
13. Communications protocols     147

<!-- Page 165 -->
 c. Click OK.
 4. Enter the transparent mode as described in SDI-12 transparent mode (p. 178).
 5. An Entering SDI12 Terminal response indicates that SDI-12 transparent mode is 
active and ready to transmit SDI-12 commands and display responses.
The load OS command has the following format:
aXLOADOS Baudrate drive:filename!.
For example: 0XLOADOS 9600 CPU:0XF5BA.MOT!. 
 Type the command, including the ending exclamation point (!) then press Enter. 
 6.  The screen will show OS send updates and the bytes sent will continue to increase. The 
process is slow, it can take several minutes, but not hours. 
 7. A SUCCESS message indicates the process is complete. 
13. Communications protocols     148

<!-- Page 166 -->
13.7.2 SDI-12 programmed mode/recorder mode
The data logger can be programmed to read SDI-12 sensors or act as an SDI-12 sensor itself.  The 
SDI12Recorder() instruction automates sending commands and recording responses. With 
this instruction, the commands to poll sensors and retrieve data is done automatically with 
proper elapsed time between the two. The data logger automatically issues retries. See CRBasic 
Editor help for more information on this instruction.
Commands entered into the SDIRecorder() instruction differ slightly in function from similar 
commands entered in transparent mode.  In transparent mode, for example, the operator 
manually enters aM! and aD0! to initiate a measurement and get data, with the operator 
providing the proper time delay between the request for measurement and the request for data.  
In programmed mode, the data logger provides command and timing services within a single 
line of code.  For example, when the SDI12Recorder() instruction is programmed with the M! 
command (note that the SDI-12 address is a separate instruction parameter), the data logger 
issues the aM! and aD0! commands with proper elapsed time between the two.  The data logger 
automatically issues retries and performs other services that make the SDI-12 measurement work 
as trouble free as possible.  
For troubleshooting purposes, responses to SDI-12 commands can be captured in programmed 
mode by placing a variable declared As String in the variable parameter. Variables not 
declared As String will capture only numeric data.
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
13.7.3 Programming the data logger to act as an SDI-12 
sensor
The SDI12SensorSetup() / SDI12SensorResponse() instruction pair programs the data 
logger to behave as an SDI-12 sensor.  A common use of this feature is to copy data from the data 
logger to other Campbell Scientific data loggers over a single data-wire interface (terminal 
configured for SDI-12 to terminal configured for SDI-12), or to copy data to a third-party SDI-12 
recorder.
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
When programmed as an SDI-12 sensor, the data logger will respond to SDI-12 commands M, MC, 
C, CC, R, RC, V, ?, and I.  
When acting as a sensor, the data logger can be assigned only one SDI-12 address per SDI-12 
port.  For example, a data logger will not respond to both 0M! and 1M! on SDI-12 port C1.  
13. Communications protocols     149

<!-- Page 167 -->
However, different SDI-12 ports can have unique SDI-12 addresses.  Use a separate 
SlowSequence for each SDI-12 port configured as a sensor.
13.7.4 SDI-12 power considerations
When a command is sent by the data logger to an SDI-12 probe, all probes on the same SDI-12 
port will wake up. However, only the probe addressed by the data logger will respond.  All other 
probes will remain active until the timeout period expires.
Example:
Probe: Water Content
Power Usage:
- Quiescent: 0.25 mA
- Active: 66 mA
- Measurement: 120 mA
Measurement time: 15 s
Timeout: 15 s
Probes 1, 2, 3, and 4 are connected to SDI-12 port C1.
The time line in the following table shows a 35-second power-usage profile example.
For most applications, total power usage of 318 mA for 15 seconds is not excessive, but if 16 
probes were wired to the same SDI-12 port, the resulting power draw would be excessive. 
Spreading sensors over several SDI-12 terminals helps reduce power consumption.
Table 13-2: Example power use for a network of SDI-12 probes
Time into 
measurement 
processes
Command
All 
probes 
awake
Time out 
expires
Probe 
1 (mA)
Probe 
2 (mA)
Probe 
3 (mA)
Probe 
4 (mA)
Total 
(mA)
Sleep       0.25 0.25 0.25 0.25 1
1 1M! Yes   120 66 66 66 318
2–14       120 66 66 66 318
15     Yes 120 66 66 66 318
16 1D0! Yes   66 66 66 66 264
17-29       66 66 66 66 264
13. Communications protocols     150

<!-- Page 168 -->
Table 13-2: Example power use for a network of SDI-12 probes
Time into 
measurement 
processes
Command
All 
probes 
awake
Time out 
expires
Probe 
1 (mA)
Probe 
2 (mA)
Probe 
3 (mA)
Probe 
4 (mA)
Total 
(mA)
30     Yes 66 66 66 66 264
Sleep       0.25 0.25 0.25 0.25 1
13. Communications protocols     151
