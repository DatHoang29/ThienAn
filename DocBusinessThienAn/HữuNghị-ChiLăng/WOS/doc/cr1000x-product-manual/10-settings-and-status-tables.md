---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 219-254
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 16: Bảng thông tin hệ thống & Cài đặt nâng cao

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 219 đến 254).  
> **Chủ đề chính**: Tra cứu chi tiết toàn bộ các trường của bảng DataTableInfo, bảng Status (Battery, CPU, Memory, PakBus, IP, Port Status, SkippedScan, v.v.), CPIStatus và danh mục cài đặt hệ thống Settings (Baudrate, IP, MQTT, NTP, Security, TLS, v.v.).

---


<!-- Page 219 -->
16. Information tables and 
settings (advanced)
Information tables and settings consist of fields, settings, and system information essential to 
setup, programming, and debugging of many advanced CR1000X/CR1000Xe systems. In many 
cases, the info tables and settings keyword can be used to pull that field into a running CRBasic 
program. There are several locations where this system information and settings are stored or 
changed:
- Status table: The Status table is an automatically created data table. View the Status table 
by connecting the data logger to your computer (see Making the software connection (p. 
39) for more information) Station Status 
 , then clicking the Status Table tab.
- DataTableInfo table: The DataTableInfo table is automatically created when a program 
produces other data tables.  View the DataTableInfo table by connecting the data logger to 
your computer (see Making the software connection (p. 39) for more information).
- PC400 users, click the Monitor Data tab and add the DataTableInfo to display it.
- LoggerNet users, select DataTableInfo from the Table Monitor list.
- Settings: Settings can be accessed  from the LoggerNet Connect Screen Datalogger > 
Settings Editor, or using Device Configuration Utility Settings Editor tab. Clicking on a 
setting in Device Configuration Utility also provides information about that setting.
- Terminal Mode: A list of setting field names is also available from the data logger terminal 
mode (from  Device Configuration Utility, click the Terminal tab) using command "F".
- Status, DataTableInfo and Settings values may be accessed programmatically using 
Tablename.Fieldname syntax. For example: Variable = Settings.Fieldname. 
For more information see: https://www.campbellsci.com/blog/programmatically-access-
stored-data-values 
 .
Communications and processor bandwidth are consumed when generating the Status and other 
information tables. If data logger is very tight on processing time, as may occur in very fast, long, 
or complex operations, retrieving these tables repeatedly may cause skipped scans.
Settings that affect memory usage force the data logger program to recompile, which may cause 
loss of data. Before changing settings, it is a good practice to collect your data (see Collecting 
16. Information tables and settings (advanced)     202

<!-- Page 220 -->
data (p. 77) for more information). Examples of settings that force the data logger program to 
recompile:  
- IP address
- IP default gateway
- Subnet mask
- PPP interface
- PPP dial string
- PPP dial response
- Baud rate change on control ports
- Maximum number of TLS server connections
- USR drive size
- PakBus encryption key
- PakBus/TCP server port
- HTTP service port
- FTP service port
- PakBus/TCP service port
- PakBus/TCP client connections
- Communications allocation
16.1 DataTableInfo table system information
The DataTableInfo table is automatically created when a program produces other data tables.  
View the DataTableInfo table by connecting the data logger to your computer (see Making the 
software connection (p. 39) for more information).
 Most fields in the DataTableInfo table are read only and of a numeric data type unless noted. 
Error counters (for example SkippedRecord) may be reset to 0 for troubleshooting purposes.
- LoggerNet users, select DataTableInfo from the Table Monitor list.
- PC400 users, click the Monitor Data tab and add the DataTableInfo to display it.
16.1.1 DataFillDays
Reports the time required to fill a data table.  Each table has its own entry in a two-dimensional 
array. First dimension is for on-board memory. Second dimension is for card memory.
16.1.2 DataRecordSize 
Reports the number of records allocated to a data table.
16.1.3 DataTableName 
Reports the names of data tables. Array elements are in the order the data tables are declared in 
the CRBasic program.
- String data type
16. Information tables and settings (advanced)     203

<!-- Page 221 -->
16.1.4 RecNum
Record number is incremented when any one of the DataTableInfo fields change, for example 
SkippedRecord.
16.1.5 SecsPerRecord
Reports the data output interval for a data table.
16.1.6 SkippedRecord
Reports how many times records have been skipped in a data table. Array elements are in the 
order that data tables are declared in the CRBasic program. Enter 0 to reset.
16.1.7 TimeStamp
Scan time that a record was generated.
- NSEC data type
16.2 Status table system information
The Status table is an automatically created data table. View the Status table by connecting the 
data logger to your computer (see Making the software connection (p. 39) for more information).
 Most fields in the Status table are read only and of a numeric data type unless noted. Error 
counters (for example, WatchdogErrors or SkippedScan) may be reset to 0 for troubleshooting 
purposes.
Status table values may be accessed programatically using SetStatus() or 
Tablename.Fieldname syntax. For example: Variable = Status.Fieldname. For 
more information see: https://www.campbellsci.com/blog/programmatically-access-stored-
data-values 
 .
16.2.1 Battery
Voltage (VDC) of the battery powering the system. Updates once per minute, when viewing the 
Status table, or programatically.
16. Information tables and settings (advanced)     204

<!-- Page 222 -->
16.2.2 BuffDepth
Shows the current pipeline mode processing buffer depth, which indicates how far the 
processing task is currently behind the measurement task. Updated at the conclusion of scan 
processing, prior to waiting for the next scan. 
16.2.3 CalCurrent
Shows the offset calibration factor for the resistor used in 0-20 and 4-20 mA measurements on 
RG terminals. Measured once during production calibration.
16.2.4 CalGain
Array of floating-point values reporting calibration gain (mV) for each integration / range 
combination.
16.2.5 CalOffset
Displays the offset calibration factor for the different voltage ranges.
16.2.6 CalRefOffset
Displays voltage reference temperature compensation offset.
16.2.7 CalRefSlope
Displays  voltage reference temperature compensation slope.
16.2.8 CalVolts
Array of floating-point values reporting a factory calibrated correction factor for the different 
voltage ranges.
16.2.9 CardStatus
Contains a string with the most recent status information for the removable memory card.
- String data type
16. Information tables and settings (advanced)     205

<!-- Page 223 -->
16.2.10 CommsMemFree 
Memory allocations for communications. Numbers outside of parentheses reflect current 
memory allocation. Numbers inside parentheses reflect the lowest memory size reached.
16.2.11 CompileResults
Contains messages generated at compilation or during runtime. Updated after compile and for 
runtime errors such as variable out of bounds.
- String data type
16.2.12 ErrorCalib
Number of erroneous calibration values measured. Erroneous values are discarded. Updated at 
startup.
16.2.13 FullMemReset
Enter 98765 to start a full-memory reset, all data and programs will be erased.
16.2.14 LastSystemScan
Reports the time of the of the last auto (background) calibration, which runs in a hidden slow-
sequence type scan. See MaxSystemProcTime  (p. 207), SkippedSystemScan  (p. 210), and 
SystemProcTime (p. 211).
16.2.15 LithiumBattery
Voltage of the internal lithium battery. Updated at CR1000X/CR1000Xe power up. For battery 
information, see Internal battery (p. 154).
16.2.16 Low12VCount
Counts the number of times the primary CR1000X/CR1000Xe supply voltage drops below ≈9.0 
VDC. Updates with each Status table update. Reset by entering 0. Incremented prior to scan (slow 
or fast) with measurements if the internal hardware signal is asserted.
16. Information tables and settings (advanced)     206

<!-- Page 224 -->
16.2.17 MaxBuffDepth
Maximum number of buffers the CR1000X/CR1000Xe will use to process lagged measurements. 
Enter 0 to reset.
16.2.18 MaxProcTime
Maximum time (μs) required to run through processing for the current scan. Value is reset when 
the scan exits. Enter 0 to reset. Updated at the conclusion of scan processing, prior to waiting for 
the next scan.
16.2.19 MaxSystemProcTime 
Maximum time (μs) required to process the auto (background) calibration, which runs in a hidden 
slow-sequence type scan. Displays 0 until a background calibration runs. Enter 0 to reset.
- Numeric data type
16.2.20 MeasureOps
Reports the number of task-sequencer opcodes required to do all measurements. Calculated at 
compile time. Includes operation codes for calibration (compile time), auto (background) 
calibration (system), and Slow Sequences. Assumes all measurement instructions run each scan. 
Updated after compile and before running.
16.2.21 MeasureTime
Reports the time (μs) needed to make measurements in the current scan. Calculated at compile 
time. Includes integration and settling time. In pipeline mode, processing occurs concurrent with 
this time so the sum of MeasureTime and ProcessTime is not equal to the required scan time. 
Assumes all measurement instructions will run each scan. Updated when a main scan begins.
16.2.22 MemoryFree
Unallocated final-data memory on the CPU (bytes). All free memory may not be available for data 
tables. As memory is allocated and freed, holes of unallocated memory, which are unusable for 
final-data memory, may be created. Updated after compile completes.
16.2.23 MemorySize
Total final-data memory size (bytes) in the CR1000X/CR1000Xe. Updated at startup. 
16. Information tables and settings (advanced)     207

<!-- Page 225 -->
16.2.24 Messages
Contains a string of manually entered messages.
- String data type
16.2.25 OSDate
Release date of the operating system in the format mm/dd/yyyy. Updated at startup.
- String data type
16.2.26 OSSignature
Signature of the operating system.
16.2.27 OSVersion
Version of the operating system in the CR1000X/CR1000Xe. Updated at OS startup.
- String data type
16.2.28 PakBusRoutes
Lists routes or router neighbors known to the data logger at the time the setting was read. Each 
route is represented by four components separated by commas and enclosed in parentheses: 
(port, via neighbor address, pakbus address, response time in ms). Updates when routes are 
added or deleted.
- String data type
16.2.29 PanelTemp
Current wiring-panel temperature (°C). Updates once per minute, when viewing the Status table, 
or programatically.
16.2.30 PortConfig
Provides information on the configuration settings (input, output, SDM, SDI-12, COM port) for C 
terminals in numeric order of terminals. Default = Input. Updates when the port configuration 
changes.
16. Information tables and settings (advanced)     208

<!-- Page 226 -->
- String data type
16.2.31 PortStatus
States of C terminals configured for control. On/high (true) or off/low (false). Array elements in 
numeric order of C terminals. Default = false. Updates when state changes. Enter -1 to set to true. 
Enter 0 to set to false.
- Boolean data type
16.2.32 ProcessTime
Processing time (μs) of the last scan. Time is measured from the end of the EndScan instruction 
(after the measurement event is set) to the beginning of the EndScan (before the wait for the 
measurement event begins) for the subsequent scan. Calculated on-the-fly. Updated at the 
conclusion of scan processing, prior to waiting for the next scan.
16.2.33 ProgErrors
Number of compile or runtime errors for the running program. Updated after compile.
16.2.34 ProgName
Name of current (running) program; updates at startup.
- String data type
16.2.35 ProgSignature
Signature of the running CRBasic program including comments. Does not change with 
operating-system changes. Updates after compiling the program.
16.2.36 RecNum
Record number increments when the Status Table is requested by support software. Range = 0 
to 232.
- Long data type
16. Information tables and settings (advanced)     209

<!-- Page 227 -->
16.2.37 RevBoard
Electronics board revision in the form xxx.yyy, where xxx = hardware revision number; yyy = clock 
chip software revision. Stored in flash memory. Updated at startup.
- String data type
16.2.38 RunSignature
Signature of the running binary (compiled) program. Value is independent of comments or non-
functional changes. Often changes with operating-system changes. Updates after compiling and 
before running the program.
16.2.39 SerialNumber
CR1000X/CR1000Xe serial number assigned by the factory when the data logger was calibrated. 
Stored in flash memory. Updated at startup.
16.2.40 SkippedScan
Number of skipped program scans (see Checking station status (p. 166) for more information) 
that have occurred while running the CRBasic program. Does not include scans intentionally 
skipped as may occur with the use of ExitScan and Do / Loop instructions. Updated when they 
occur. Enter 0 to reset. 
16.2.41 SkippedSystemScan 
Number of scans skipped in the background calibration. Enter 0 to reset. See LastSystemScan (p. 
206), MaxProcTime (p. 207), and SystemProcTime (p. 211).
16.2.42 StartTime
Time (date and time) the CRBasic program started. Updates at beginning of program compile.
- NSEC data type
16.2.43 StartUpCode
Indicates how the running program was compiled. Updated at startup. 65 = Run on powerup is 
running and normal powerup occurred. 
16. Information tables and settings (advanced)     210

<!-- Page 228 -->
16.2.44 StationName
Station name stored in flash memory. This is not the same name as that is entered into your data 
logger support software. This station name can be sampled into a data table, but it is not the 
name that appears in data file headers. Updated at startup or when the name is changed. This 
value is read-only if the data logger is currently running a program with a CardOut() 
instruction.
- String data type
16.2.45 SW12Volts
Status of switched, 12 VDC terminal(s). On/high (true) or off/low (false) Enter -1 to set to true. 
Enter 0 to set to false. Updates when the state changes.
- Boolean data type
16.2.46 SystemProcTime
Time (μs) required to process auto (background) calibration. Default is 0 until background 
calibration runs.
16.2.47 TimeStamp
Scan-time that a record was generated.
- NSEC data type
16.2.48 VarOutOfBound
Number of attempts to write to an array outside of the declared size. The write does not occur. 
Indicates a CRBasic program error. If an array is used in a loop or expression, the pre-compiler 
and compiler do not check to see if an array is accessed out-of-bounds (i.e., accessing an array 
with a variable index such as arr(index) = arr(index–1), where index is a variable). Updated at 
runtime when the error occurs. Enter 0 to reset.
16.2.49 WatchdogErrors
Number of watchdog errors that have occurred while running this program. Resets automatically 
when a new program is compiled. Enter 0 to reset. Updated at startup and at occurrence.
16. Information tables and settings (advanced)     211

<!-- Page 229 -->
16.2.50 WiFiUpdateReq
Shows if WiFi operating system update is available. Update available (true) or not (false). Updates 
when state changes.
- Boolean data type
16.3 CPIStatus system information
The CPIStatus table is automatically created when a program uses the CPI bus.  View the 
CPIStatus table by connecting the data logger to your computer (see Making the software 
connection (p. 39) for more information).
 Most fields in the CPIStatus table are read/write and of a numeric data type unless noted. Error 
counters (for example BuffErr) may be reset to 0 for troubleshooting purposes.
- LoggerNet users, select DataTableInfo from the Table Monitor list.
- PC400 users, click the Monitor Data tab and add DataTableInfo.
For more information on the CPI bus and how to design a CDM network, see the technical paper 
at: https://s.campbellsci.com/documents/us/technical-papers/cpi-bus.pdf 
 .
16.3.1 BusLoad
Percentage of the possible CPI network bandwidth use over the scan interval. BusLoad = Used 
capacity / Maximum capacity. 
- Read only
- Percentage (0.000 to 100)
TIP:
Use CPISpeed() to change the CPI bit rate. The default bit rate is 250 kbps. Use a higher bit 
rate if the BusLoad exceeds 75 percent.
16.3.2 ModuleReportCount
Reports the number of times measurement modules report in to the CPI bus. Modules report in 
on program send or when settings in the CPIStatus table are edited remotely. Activity that could 
cause the number of modules to be reported differently will cause ModuleReportCount to 
increment. Also, if there are devices on the network that are connected but not active, (such as 
those not in the running program) they will report in once minute, advertising their presence, and 
incrementing ModuleReportCount.
16. Information tables and settings (advanced)     212

<!-- Page 230 -->
16.3.3 ActiveModules
Reports the number of measurement modules that are active on the CPI bus.
- Read only
16.3.4 BuffErr (buffer error)
Reports how many times there is an error in the buffer. Enter 0 to reset.
16.3.5 RxErrMax
Reports the maximum number of receive errors. Enter 0 to reset.
16.3.6 TxErrMax
Reports the maximum number of transmit errors. Enter 0 to reset.
16.3.7 FrameErr (frame errors)
Reports how many times a frame has an error. Enter 0 to reset.
16.3.8 ModuleInfo array
Reports: CDM Type, Serial Number, Device Name, CPI Address, Activity, OS Version.
- String data type
- Read only
Possible responses and meanings in the Activity field are below:
- Active: The module is connected to the CPI bus and is making measurements according to 
the data logger program.
- Offline:  The module was present after startup but is no longer responding.
- Unused:  The module is or was connected and powered but is not included in the data 
logger program.
- Wait Config:  The module has not yet responded to a data logger attempts to configure it.
- Config Fail:  The module could not be configured. A configuration error message is 
appended to this response.
- CAN Errors, resetting CPI: The CDM module is not used in the data logger program.
16. Information tables and settings (advanced)     213

<!-- Page 231 -->
16.4 Settings
Settings can be accessed  from the LoggerNet Connect Screen Datalogger > Setting Editor, or 
using Device Configuration Utility Settings Editor tab. Settings are organized in tabs and can be 
searched for.
Most Settings are read/write and of a numeric data type unless noted.
Settings may be accessed programatically using SetSetting() or Tablename.Fieldname 
syntax. For example: Variable = Settings.Fieldname. For more information 
see: https://www.campbellsci.com/blog/programmatically-access-stored-data-values 
 .
NOTE:
A list of Settings fieldnames is also available from the data logger terminal mode using 
command F.
General data logger settings are listed below. For MQTT, GOES, and WIFI settings see: 
- MQTT settings (p. 230).
- GOES settings (p. 234) 
- Wi-Fi settings (p. 1)
16.4.1 Baudrate
This setting governs the baud rate that the data logger will use for a given port in order to 
support serial communications. For some ports (COM), this setting also controls whether the port 
will be enabled for serial communications.
Some ports (RS-232 and CS I/O ME) support auto-baud synchronization while the other ports 
support only fixed baud. With auto-baud synchronization, the data logger will attempt to match 
the baud rate to the rate used by another device based upon the receipt of serial framing errors 
and invalid packets.
16.4.2 Beacon
This setting, in units of seconds, governs the rate at which the data logger will broadcast PakBus 
messages on the associated port in order to discover any new PakBus neighboring nodes. If this 
16. Information tables and settings (advanced)     214

<!-- Page 232 -->
setting value is set to a value of 0 or 65,535, the data logger will not broadcast beacon messages 
on this port.
This setting will also govern the default verification interval if the value of the Verify() setting  for 
the associated port is zero. If the value of this setting is non-zero, and the value of the Verify 
setting is zero, the effective verify interval will be calculated as 2.5 times the value for this setting. 
If both the value of this setting and the value of the Verify setting is zero, the effective verify 
interval will be 300 seconds (five minutes).
16.4.3 CentralRouters
This setting specifies a list of PakBus addresses for routers that are able to work as Central 
Routers. By specifying a non-empty list for this setting, the data logger will be configured as a 
Branch Router meaning that it will not be required to keep track of neighbors of any routers 
except those in its own branch. Configured in this fashion, the data logger will ignore any 
neighbor lists received from addresses in the central routers setting and will forward any 
messages that it receives to the nearest default router if it does not have the destination address 
for those messages in its routing table.
- String data type
16.4.4 CommsMemAlloc
Replaces PakBusNodes. Controls the amount of memory allocated for PakBus routing and 
communications in general. Increase the value of this setting if you require more memory 
dedicated to communications. Increase this value if the data logger will be used for routing a 
large number of PakBus nodes (>50). Increase this value if your data logger is dropping 
connections during short periods of high TCP/IP traffic. This setting will effect the values reported 
in CommsMemFree  (p. 206).
16.4.5 ConfigComx
Specifies the configuration for a data logger control port as it relates to serial communications. It 
is significant only when the associated port baud rate setting is set to something other than 
Disabled. This setting denotes the physical layer properties used for communications. It does not 
indicate the port's current configuration as it relates to standard or inverted logic. Options 
include:
- RS-232: Configures the port as RS-232 with standard voltage levels.
16. Information tables and settings (advanced)     215

<!-- Page 233 -->
- TTL: The port is configured to use TTL, 0 to 5V voltage levels. By default, the port will use 
inverted logic levels. Use SerialOpen() to configure this port for standard TTL logic 
levels.
- LVTTL: The port is configured to use Low Voltage TTL (LVTTL), 0 to 3.3V voltage levels. By 
default, the port will use inverted logic levels. Use SerialOpen() to configure this port 
for standard TTL logic levels.
- RS-485 Half-Duplex PakBus: The port is configured as RS-485 half-duplex (two wire) and 
uses the PakBus/MDROP protocol. This allows reliable PakBus peer-to-peer networking of 
multiple devices including the MD485 and NL100 using the RS-485 interface.
- RS-485 Half-Duplex Transparent: The port is configured as RS-485 half-duplex (two wire). 
This setting is most commonly used when communicating with other non-PakBus RS-485 
devices. Use this setting when communicating with devices such as Modbus RTUs or third-
party serial sensors with RS-485 interfaces.
- RS-485/RS-422 Full Duplex Transparent: The port is configured as full-duplex (four wire). In 
this configuration, four adjacent control ports are required.
16.4.6 CSIOxnetEnable 
 Controls whether the CS I/O IP #1 or #2 TCP/IP interface  should be enabled.
16.4.7 CSIOInfo
Reports the IP address, network mask, and default gateway for each of the data logger's active 
network interfaces. If DHCP is used for the interface, this setting will report the value that was 
configured by the DHCP server.
- String data type
16.4.8 DeleteCardFilesOnMismatch
Controls the behavior of the data logger when it restarts with a different program and it detects 
that data files created by the CardOut() are present but do not match the new program. If this 
value is set to one, the data logger will delete these files so that new files can be stored. If set to a 
value of zero, the data logger will retain the existing files and prevent any data from being 
appended to these files.
16.4.9 DisableLithium
Controls whether the data logger will maintain its real time clock and battery backed memory 
when it loses power. Setting this value to one will cause the data logger clock to lose time on 
16. Information tables and settings (advanced)     216

<!-- Page 234 -->
power loss. If this value is set to one, the data logger will not maintain its program or data after it 
powers down.
This value is useful when the data logger needs to be stored as it will prolong the shelf life of the 
lithium battery almost indefinitely.
If this value is set to one, the data logger will set it to zero when it powers up.
16.4.10 DisableTCPDelayAck
Controls whether the data logger uses TCP Delayed Acknowledgment for all TCP connections. 
The default value (0) enables TCP Delayed Acknowledgment, which is standard behavior on most 
computers and helps improve network efficiency. When TCP Delayed Acknowledgment is 
enabled, the data logger may wait up to 250 ms before acknowledging received data. In some 
cases, this delay can cause slow or uneven data delivery when reading data from a sensor over 
TCP—especially if the sensor uses Nagle’s Algorithm. If you see delays of up to 250 ms between 
data packets, setting DisableTCPDelayAck to 1 may help. A value of 1 disables TCP Delayed 
Acknowledgment, causing the data logger to acknowledge data immediately as it is received. 
This can improve performance with devices that wait for acknowledgments before sending more 
data. See the CRBasic Help for TCPOpen 
  for an example program. 
16.4.11 DNS
This setting specifies the addresses of up to two domain name servers that the data logger can 
use to resolve domain names to IP addresses. Note that if DHCP is used to resolve IP information, 
the addresses obtained via DHCP will be appended to this list.
NOTE:
When setting a static IP address, first manually set a DNS Server Address in Settings Editor > 
Advanced.
- String data type
16.4.12 EthernetInfo
Reports the IP address, network mask, and default gateway for each of the data logger's active 
network interfaces. If DHCP is used for the interface, this setting will report the value that was 
configured by the DHCP server.
- String data type
- Read only
16. Information tables and settings (advanced)     217

<!-- Page 235 -->
16.4.13 EthernetPower
This setting specifies how the data logger controls power to its Ethernet interface and provides a 
way to reduce power consumption when Ethernet is not connected. Options are Always On, 1 
Minute, or Disable. The default is 1 Minute. 
16.4.14 FilesManager
This setting controls how the data logger will handle incoming files with specific extensions from 
various sources. There can be up to four specifications. Each specification has three required 
fields: PakBus Address, File Name, and Count.
- String data type
16.4.15 FTPEnabled
Set to 1 if to enable FTP service. Default is 0. FTP is disabled by default. However, when enabled, 
the UID will be the default FTP password.
16.4.16 FTPPassword
Specifies the password that is used to log in to the FTP server. FTP is disabled by default. 
However, when enabled, the UID will be the default FTP password.
- String data type
16.4.17 FTPPort
Configures the TCP port on which the FTP service is offered. The default value is usually sufficient 
unless a different value needs to be specified to accommodate port mapping rules in a network 
address translation firewall. Default = 21.
16.4.18 FTPUserName
Specifies the user name that is used to log in to the FTP server. An empty string  (the default) 
inactivates the FTP server.                        
- String data type
16. Information tables and settings (advanced)     218

<!-- Page 236 -->
16.4.19 HTTPEnabled
 Set to 1 to enable HTTP (web server) service or 0 to disable it. Anonymous HTTP access is 
disabled by default. HTTP will be accessible with admin as the username and UID as the admin 
password.
16.4.20 HTTPHeader
Specifies additions to the HTTP header in the web service response. It can include multiple lines. 
Example: Access-Control-Allow-Origin: *
- String data type
16.4.21 HTTPPort
Configures the TCP port on which the HTTP (web server) service is offered. Generally, the default 
value is sufficient unless a different value needs to be specified to accommodate port-mapping 
rules in a network-address translation firewall. Default = 80.                         
16.4.22 HTTPSEnabled
Set to 1 to enable the HTTPS (secure web server) service.
16.4.23 HTTPSPort
Configures the TCP port on which the HTTPS (secure web server) service is offered. Generally, the 
default value is sufficient unless a different value needs to be specified to accommodate port 
mapping rules in a network address translation firewall.
16.4.24 IncludeFile
This setting specifies the name of a file to be implicitly included at the end of the current CRBasic 
program or can be run as the default program. In order to work as an include file, the file 
referenced by this setting cannot contain a BeginProg() statement or define any variable 
names or tables that are defined in the main program file.
This setting must specify both the name of the file to run as well as on the device (CPU:, USR:, or 
CRD:) on which the file is located. The extension of the file must also be valid for a data logger 
program (.CRB, .DLD, .CR1X). 
See also File management via powerup.ini (p. 161).
16. Information tables and settings (advanced)     219

<!-- Page 237 -->
- String data type
16.4.25 IPAddressCSIO
An array that specifies the CS I/O IP addresses for internet interfaces like the NL200 and NL240, 
which use the CS I/O bridge protocol. By default, the NL200 uses CS I/O IP address #1, and the 
NL240 uses CS I/O IP address #2. The "Interface Identifier" setting in the NL2xx can be used to 
change the default CS I/O IP address array number. If zero (the default) is specified for the IP 
address, the data logger will use DHCP to configure the IP address, network mask, and default 
gateway for that interface. 
- String data type
16.4.26 IPBroadcastFiltered
Set to one if all broadcast IP packets should be filtered from IP interfaces. Do not set this if you 
use the IP discovery feature of the Device Configuration Utility or of LoggerLink. If this is set to 
one, the data logger will fail to respond to the broadcast requests.
Default = 0.
16.4.27 IPAddressEth
Specifies the IP address for the internet interface connected via the peripheral port to devices 
such as the NL115 and NL120. If this value is specified as "0.0.0.0" (the default), the data logger will 
use DHCP to configure the effective value for this setting as well as the Ethernet Default 
Gateway and Ethernet Subnet Mask settings. This setting is the equivalent to the 
IPAddressEth status table variable.
- String data type
16.4.28 IPGateway
Specifies the IP address of the network gateway on the same subnet as the Ethernet interface. If 
the value of the Ethernet IP Address setting is set to "0.0.0.0" (the default), the data logger will 
configure the effective value of this setting using DHCP. 
- String data type
16. Information tables and settings (advanced)     220

<!-- Page 238 -->
16.4.29 IPGatewayCSIO
These settings specify the IP addresses of the router on the subnet to which the first or second CS 
I/O bridge internet interface is connected. The data logger will forward all non-local IP packets to 
this address when it has no other route. If the CS I/O IP Address  setting is set to a value of 
"0.0.0.0", the data logger will configure the effective value of this setting using DHCP.
- String data type
16.4.30 IPMaskCSIO
These settings specify the subnet masks for the CS I/O bridge mode internet interface. If the 
corresponding CS I/O Address setting is set to a value of "0.0.0.0", the data logger will configure 
the effective value of this setting using DHCP.
- String data type
16.4.31 IPMaskEth
Specifies the subnet mask for the Ethernet interface. If the value of the Ethernet IP Address 
setting is set to "0.0.0.0" (the default), the data logger will configure the effective value of this 
setting using DHCP.
- String data type
16.4.32 IPTrace
Discontinued; aliased to IPTraceComport
16.4.33 IPTraceCode
Controls what type of information is sent on the port specified by IPTraceComport and via Telnet. 
Each bit in this integer represents a certain aspect of tracing that can be turned on or off. Values 
for particular bits are described in the Device Configuration Utility. Default = 0, no messages 
generated.
16.4.34 IPTraceComport
Specifies the port (if any) on which TCP/IP trace information is sent. Information type is controlled 
by IPTraceCode.
16. Information tables and settings (advanced)     221

<!-- Page 239 -->
16.4.35 IsRouter
This setting controls whether the data logger is configured as a router or as a leaf node. If the 
value of this setting is true, the data logger will be configured to act as a PakBus router. That is, it 
will be able to forward PakBus packets from one port to another. To perform its routing duties, a 
data logger configured as a router will maintain its own list of neighbors and send this list to 
other routers in the PakBus network. It will also obtain and receive neighbor lists from other 
routers.
If the value of this setting is false, the data logger will be configured to act as a leaf node. In this 
configuration, the data logger will not be able to forward packets from one port to another and it 
will not maintain a list of neighbors. Under this configuration, the data logger can still 
communicate with other data loggers and wireless sensors. It cannot, however, be used as a 
means of reaching those other data loggers. The default value is false.
- Boolean data type
16.4.36 KeepAliveURL (Ping keep alive URL)
The URL to send a ping to when there has been no network activity for the KeepAliveMin interval. 
If there is no ping response then the network connection is reestablished. 
- String data type
16.4.37 KeepAliveMin (Ping keep alive timeout value)
When there has been no network activity for this amount of time, in seconds, a ping will be sent 
to the KeepAliveURL. Default = 0 which disables keep alive pings.
- Long data type (allowed values: 0,5,10,15,30,60,120,180,240,300,360,480,720)
16.4.38 MaxPacketSize
Specifies the maximum number of bytes per data collection packet.
16.4.39 Neighbors
This setting specifies, for a given port, the explicit list of PakBus node addresses that the data 
logger will accept as neighbors. If the list is empty (the default value) any node will be accepted 
as a neighbor. This setting will not affect the acceptance of a neighbor if that neighbor's address 
is greater than 3999.
16. Information tables and settings (advanced)     222

<!-- Page 240 -->
- String data type
16.4.40 NTPServer
This setting specifies an NTP Server to be queried (once per day) to adjust the data logger clock. 
This setting uses the UTC Offset setting. If UTC Offset setting is not set, it is assumed to be 0.
- String data type
16.4.41 PakBusAddress
This setting specifies the PakBus address for this device. Valid values are in the range 1 to 4094. 
The value for this setting must be chosen such that the address of the device will be unique in the 
scope of the data logger network. Duplication of PakBus addresses can lead to failures and 
unpredictable behavior in the PakBus network.
When a device has an allowed neighbor list for a port, any device that has an address greater 
than or equal to 4000 will be allowed to connect to that device regardless of the allowed 
neighbor list.
16.4.42 PakBus Encryption Key
The PakBusEncryptionKey setting specifies text that will be used to generate the key for 
encrypting PakBus messages sent or received by this data logger. If this value is specified as an 
empty string, the data logger will not use PakBus encryption. If this value is specified as a non-
empty string, however, the data logger will not respond to any PakBus message unless that 
message has been encrypted. 
Beginning with operating system 8.00, the data logger is configured to be secure by default. 
Therefore, For data loggers that have a UID, PakBus Encryption is enabled by default. The default 
PakBus Encryption Key is the UID.
- String data type
16.4.43 PakBusNodes
Discontinued; aliased to CommsMemAlloc
16. Information tables and settings (advanced)     223

<!-- Page 241 -->
16.4.44 PakBusPort
This setting specifies the TCP service port for PakBus communications with the data logger. 
Unless firewall issues exist, this setting probably does not need to be changed from its default 
value. Default 6785.
16.4.45 PakBusTCPClients 
This setting specifies outgoing PakBus/TCP connections that the data logger should maintain. Up 
to four addresses can be specified.
- String data type
16.4.46 PakBusTCPEnabled
By default, PakBus TCP communications are enabled. To disable PakBus TCP communications, 
set the PakBusPort setting to 65535.
16.4.47 PakBusTCPPassword
This setting specifies a password that, if not empty, will make the data logger authenticate any 
incoming or outgoing PakBus/TCP connection. This type of authentication is similar to that used 
by CRAM-MD5.
- String data type
16.4.48 PingEnabled
Set to one to enable the ICMP ping service.
16.4.49 PCAP
PCAP is a packet capture (PCAP) file of network packet data (network traffic) that can be opened 
by Wireshark. This setting specifies the network interface, file name, and maximum size of the 
PCAP file. For example: 
- "usr:debug.pcap" saves the file to the USR drive with the file type .pcap.
- ".ring." found in name will create new files once the file size has been reached. 
"crd:debug.ring.pcap" creates crd:debug001.pcap, crd:debug002.pcap...
- If a number follows .ring. then only that number of files will be saved, with the oldest 
deleted. For example: "usr:debug.ring.3.pcap" will save three files.
16. Information tables and settings (advanced)     224

<!-- Page 242 -->
If All Networks is selected as the Network Interface and PPP/Cell is active, then separate files will 
be opened for the PPP/Cell network with "ppp." prefixed on the file name. 
16.4.50 pppDial
Specifies the dial string that would follow the ATD command (#777 for the Redwing CDMA).
Alternatively, this value can specify a list of AT commands where each command is separated by 
a semi-colon (;). When specified in this fashion, the data logger will transmit the string up to the 
semicolon, transmit a carriage return to the modem, and wait for two seconds before proceeding 
with the rest of the dial string (or up to the next semicolon). If multiple semicolons are specified in 
succession, the data logger will add a delay of one second for each additional semicolon. 
If a value of PPP is specified for this setting, will configure the data logger to act as a PPP client 
without any modem dialing. Finally, an empty string (the default) will configure the data logger to 
listen for incoming PPP connections also without any modem dialing.
- String data type
16.4.51 pppDialResponse
Specifies the response expected after dialing a modem before a PPP connection can be 
established.
- String data type
16.4.52 pppInfo
Reports the IP address, network mask, and default gateway for each of the data logger's active 
network interfaces. If DHCP is used for the interface, this setting will report the value that was 
configured by the DHCP server.
- String data type
- Read only
16.4.53 pppInterface
This setting controls which data logger port PPP service will be configured to use.
16.4.54 pppIPAddr
Specifies the IP address that will be used for the PPP interface if that interface is active (the PPP 
Interface setting needs to be set to something other than Inactive).
16. Information tables and settings (advanced)     225

<!-- Page 243 -->
- String data type
16.4.55 pppPassword
Specifies the password that will be used for PPP connections when the value of PPP Interface is 
set to something other than Inactive.
- String data type
16.4.56 pppUsername
Specifies the user name that is used to log in to the PPP server.
- String data type
16.4.57 RouteFilters
This setting configures the data logger to restrict routing or processing of some PakBus message 
types so that a "state changing" message can only be processed or forwarded by this data logger 
if the source address of that message is in one of the source ranges and the destination address 
of that message is in the corresponding destination range. If no ranges are specified (the default), 
the data logger will not apply any routing restrictions. "State changing" message types include 
set variable, table reset, file control send file, set settings, and revert settings.
If a message is encoded using PakBus encryption, the router will forward that message regardless 
of its content. If, however, the routes filter setting is active in the destination node and the 
unencrypted message is of a state changing type, the route filter will be applied by that end 
node.
- String data type
16.4.58 RS232Handshaking 
If non-zero, hardware handshaking is active on the RS-232 port. This setting specifies the 
maximum packet size sent between checking for CTS.
16. Information tables and settings (advanced)     226

<!-- Page 244 -->
16.4.59 RS232Power
Controls whether the RS-232 port will remain active even when communications are not taking 
place. Note that if RS232Handshaking is enabled (handshaking buffer size is non-zero), that this 
setting must be set to Yes.
- Boolean data type
16.4.60 RS232Timeout
RS-232 hardware handshaking timeout. Specifies the time (tens of ms) that the 
CR1000X/CR1000Xe will wait between packets if CTS is not asserted.
16.4.61 Security(1), Security(2), Security(3)
An array of three security codes.  A value of zero for a given level will grant access to that level's 
privileges for any given security code. For more information, see: Additional security measures (p. 
62).
16.4.62 ServicesEnabled
Discontinued; replaced by/aliased to HTTPEnabled, PingEnabled, TelnetEnabled.
16.4.63 TCPClientConnections 
Discontinued; replaced by / aliased to PakBusTCPClients.
16.4.64 TCP_MSS
The maximum TCP segment size. This value represents the maximum TCP payload size. It is used 
to limit TCP packet size. A maximum TCP transmission unit (MTU) can be calculated by adding 
the IP Header size (20 bytes), the TCP Header size (20 bytes), and the payload size.
16.4.65 TCPPort
Discontinued; replaced by / aliased to PakBusPort.
16.4.66 TelnetEnabled
Enables (1) or disables (0) the Telnet service.
16. Information tables and settings (advanced)     227

<!-- Page 245 -->
16.4.67 TLSConnections (Max TLS Server Connections)
This setting controls the number of concurrent TLS (secure or encrypted) client socket 
connections that the data logger will be capable of handling at any given time. This will affect 
FTPS and HTTPS services. This count will be increased by the number of DNP() instructions in the 
data logger program.
This setting will control the amount of RAM that the data logger will use for TLS connections. For 
every connection, approximately 20KBytes of RAM will be required. This will affect the amount of 
memory available for program and data storage. Changing this setting will force the data logger 
to recompile its program so that it can reallocate memory
16.4.68 TLSPassword
This setting specifies the password that will be used to decrypt the TLS Private Key setting.
- String data type
16.4.69 TLSStatus
Reports the current status of the data logger TLS network stack.
- String data type
- Read only
16.4.70 Configure USB
USBConfig controls the configuration of the data logger USB port. When set to a value of 1 it 
configures the data logger to enumerate USB as a virtual com port only. A value of 0 (the default) 
causes the data logger to enumerate as a composite device with both a virtual com port and a 
virtual Ethernet port (RNDIS) available.
Default = 0.
16.4.71 USB Disable
USBDisable controls whether the USB port is enabled or disabled. If set to 0 (the default), the port 
is enabled. 1 disables it. If the setting is changed, a data logger reboot is required for the change 
to take effect.
16. Information tables and settings (advanced)     228

<!-- Page 246 -->
NOTE:
When USB is disabled, you will need to use another communications method (Ethernet, serial, 
CR1000KD) to re-enable it. 
Default = 0.
16.4.72 USBEnumerate
Controls the behavior of the data logger when its USB connector is plugged into the computer. If 
set to a value of 1, the data logger will use its own serial number for identification in the USB 
enumeration. If set to a value of 0 (the default), the data logger will use a fixed serial number in 
the USB enumeration. This behavior controls whether the computer will allocate a new virtual 
serial port for the data logger USB connection or will use a previously allocated (but not currently 
used) virtual serial port. 
Default = 0.
16.4.73 USB Not Trusted
USBNotTrusted controls how the USB port behaves, as a COM port, with respect to security. If set 
to 0 (default), the port will allow PakBus communications to occur when the PakBus Encryption 
Key (p. 223) is set. Also, if RNDIS is enabled (USB Disable (p. 228) = 0), no security challenges will 
be given to gain access via HTTP(S). If set to 1, PakBus communications will follow the 
PakBusEncryption Key setting and RNDIS will require login credentials.
NOTE:
In Device Configuration Utility, RNDIS is treated an IP connection rather than a direct 
connection. As a result, the USBNotTrusted setting does not apply when using RNDIS 
through Device Configuration Utility.
Default = 0.
16.4.74 USB Virtual Ethernet Address (RNDIS)
IPAddressUSB specifies the IP address for the USB Network Interface. When set to 0 it defaults to 
192.168.66.1.
16.4.75 USRDriveFree
Provides information on the available bytes for the USR drive.
- Read only
16. Information tables and settings (advanced)     229

<!-- Page 247 -->
16.4.76 USRDriveSize
Specifies the size in bytes allocated for the USR: ram disk drive. This memory is allocated from the 
memory that the data logger would normally use to store its compiled program or RAM based 
data tables. If this setting is too large, some programs may not be able to compile on the data 
logger.
Setting the USR: Drive Size setting will force the data logger to recompile its program and may 
result in the loss of data.
This setting controls the amount of memory set aside for the USR: size and is only indirectly 
related to the amount of storage within that file system. The amount of space available for 
storing files is always going to be less than this value because of the overhead of file system 
structures.
16.4.77 UTCOffset
Specifies the offset, in seconds, of the data logger's clock from Coordinated Universal Time (UTC, 
or GMT). For example, if the clock is set to Mountain Standard Time in the U.S. (-7 Hours offset 
from UTC) then this setting should be -25200 (-7*3600). This setting is used by the NTP Server 
setting as well as EmailSend() and HTTP(), which require Universal Time in their headers. This 
setting will also be adjusted by the Daylight Savings functions if they adjust the clock.
If a value of -1 is supplied for this setting, no UTC offset will be applied.
16.4.78 Verify
This setting specifies the interval, in units of seconds, that will be reported as the link verification 
interval in the PakBus hello transaction messages. It will indirectly govern the rate at which the 
data logger will attempt to start a hello transaction with a neighbor if no other communications 
have taken place within the interval.
16.4.79 MQTT settings
Access MQTT settings using Device Configuration Utility. Clicking on a setting in Device 
Configuration Utility also provides information about that setting.
Where to find:
- All settings: Settings Editor tab in Device Configuration Utility: MQTT tab, unless noted.
See also MQTT.
16. Information tables and settings (advanced)     230

<!-- Page 248 -->
NOTE:
A list of Settings fieldnames is also available from the data logger terminal mode using 
command F.
16.4.79.1 MQTT Auto-Publish Data
When enabled, this setting allows data tables to be published to CAMPBELL CLOUD without 
adding MQTTPublishTable() to a data table. See: MQTT Auto Publish Data versus 
MQTTPublishTable() (p. 74).
16.4.79.2 MQTTBaseTopic (MQTT base topic)        
This is the base topic which will automatically be used. Use this setting to override the default 
format: CS/{CAMPBELL CLOUD Account ID}/{MQTT Client Id}/. 
- String data type
16.4.79.3 MQTTCleanSession (MQTT connection)
Assigns the MQTT broker connection type. Persistent sessions save all relevant client information 
on the broker. The client gets messages that it misses offline.
If the connection between the client and broker is interrupted during a Clean session, topics may 
be lost and the client needs to subscribe again. The client does not get messages that it misses 
offline.
- Long data type, allowed values:
- 0 = Clean
- 1 = Persistent (default)
16.4.79.4 MQTTClientID (MQTT client identifier)           
Unique identifier the data logger uses to connect to MQTT broker. The default is the hardware 
type_serial number. Example: CR1000X/CR1000Xe_123.
- String data type, maximum number of characters is 64
16.4.79.5 MQTTEnable (Enable or disable MQTT)
By default, MQTT is disabled. 
16. Information tables and settings (advanced)     231

<!-- Page 249 -->
- Long data type, allowed values:
- 0 = Disable (default)
- 1 = Enable with TLS-Mutual Authentication
- 2 = Enable with TLS
- 3 = Enable MQTT
NOTE:
Mutual authentication has become a widespread practice. Specifically, it is a 
requirement when using AWS IoT MQTT. It is also the practice used by CampbellCloud.
16.4.79.6 MQTTEndpoint (MQTT broker URL)          
Server URL for MQTT broker.
- String data type
16.4.79.7 MQTTKeepAlive (MQTT keep alive)          
When there has been no network activity for this amount of time, in seconds, a ping will be sent 
to the MQTTBrokerURL. Default = 0 which disables keep alive pings. Valid values are in the range 
0 to 65535.
- Long data type
16.4.79.8 MQTTPassword (MQTT password)           
Password, in association with MQTTUserName, required to connect to the MQTT broker. 
- String data type
16.4.79.9 MQTTPortNumber (MQTT port number)        
Port number to connect to the MQTT broker. 
- Long data type, maximum number of characters is 256
16.4.79.10 MQTTStatusInterval (Status information publish interval)    
Time (in minutes) between publishing MQTT status information. This interval determines how 
often the data logger publishes to the topic: {System Base Topic/}statuslnfo. Valid values are in 
the range 0 to 1440.
16. Information tables and settings (advanced)     232

<!-- Page 250 -->
- Long data type
16.4.79.11 MQTTState (MQTT state)
This is a read-only field indicating the current state of the data logger connection to the MQTT 
broker.
- Long data type, possible results: 
- 0 = Disabled / Off
- 10 = Waiting for an IP network interface
- 11 = Connection retry wait
- 20 = Opening TCP connection
- 21 = TCP Open failed
- 22 = TCP connection opened
- 24 = Closing TCP connection
- 26 = TCP connection closed
- 30 = TLS handshake started
- 31 = TLS handshake failed
- 32 = TLS handshake success
- 50 = MQTT session established
- 51 = Waiting for session start response
- 52 = Publishing
- 100 = Onboard started
- 101 = Onboard retry
- 102 = Onboard processing
16.4.79.12 MQTTStateInterval (State publish interval)     
Time (in minutes) between publishing MQTT state information. This interval determines how 
often the data logger publishes to the topic: {System Base Topic/}State. Valid values are in the 
range 0 to 1440. Setting the value to 0 will not disable normal state publishing activity, only 
interval publishing.
- Long data type
16. Information tables and settings (advanced)     233

<!-- Page 251 -->
16.4.79.13 MQTTUserName (MQTT user name)        
User name, in association with MQTTPassword, used to connect to MQTT broker. 
- String data type, maximum number of characters is 256
16.4.79.14 MQTTWillMessage (MQTT last will message)     
Message published on last will topic by broker if disconnected without a disconnect command.
- String data type, maximum number of characters is 256
16.4.79.15 MQTTWillQoS (Quality of service)           
This is an agreement that defines the guarantee of delivery for a specific message.  Higher QoS 
levels are more reliable, but take more time and bandwidth.
- Long data type, allowed values:
- 0 = At most once (default), no confirmation
- 1 = At least once, confirmation required
- 2 = Exactly once using a multi-step handshake
16.4.79.16 MQTTWillRetain (MQTT last will message retained by 
broker)    
Enables or disables the broker to retain MQTTWillMessage.
- Long data type, allowed values:
- 0 = Do not retain (default)
- 1 = Retain
16.4.79.17 MQTTWillTopic (MQTT last will topic)        
Broker will publish the MQTTWillMessage to this topic if disconnected without a disconnect 
command.
- String data type, maximum number of characters is 64
16.4.80 GOES settings
Access GOES settings, using Device Configuration Utility. Clicking on a setting in Device 
Configuration Utility also provides information about that setting. These settings are available for 
16. Information tables and settings (advanced)     234

<!-- Page 252 -->
data loggers that have a TX325 or TX326 attached.
Where to find:
- All settings: Settings Editor tab in Device Configuration Utility: GOES tab, unless noted.
NOTE:
A list of Settings fieldnames is also available from the data logger terminal mode using 
command F.
16.4.80.1 GOESComPort
Port used to communicate with the GOES transmitter.
- Long data type; allowed values:
- 1 = RS-232
- 4 = CS I/O SDC7
- 5 = CS I/O SDC8
- 7 = CS I/O SDC10
- 8 = CS I/O SDC11
- 9 = COMC1
- 10 = COMC3
16.4.80.2 GOESEnabled
Controls whether the data logger polls the GOESComPort to see if a GOES radio is attached.
- Long data type, allowed values:
- 0 = Disable (default). The data logger ignores all other GOES settings.
- 1 = Enable
16.4.80.3 GOESGainSetting
Specifies the effective antenna gain (in units of 0.1 dbi). This is the maximum specified gain for the 
antenna minus the loss in the cable connecting the radio to the antenna.
- Long data type, allowed values:
- 0 = Disable (default). The radio will operate as if the antenna used for its original 
certification is being used.
- 1 to 140 = Enable for 300 Bps transmissions (14 dbi maximum)
- 1 to 200 = Enable for 1200 Bps transmissions (20 dbi maximum)
16. Information tables and settings (advanced)     235

<!-- Page 253 -->
16.4.80.4 GOESMsgWindow
Length, in seconds, of the assigned self-timed transmission window assigned by NESDIS (TX325) 
or EUMETSAT (TX326).  Valid values are in the range 1 to 110 seconds. 
- Long data type
16.4.80.5 GOESPlatformID
8-digit hexadecimal identification number assigned by NESDIS (TX325) or EUMETSAT (TX326).
- String data type
16.4.80.6 GOESRepeatCount
Number of times within the random transmit interval that the GOES transmitter will transmit the 
message data. Valid entries are 1 to 3.
- Long data type
16.4.80.7 GOESRTBaudRate
Baud rate for the random transmissions. Valid settings are 100, 300, or 1200. The baud rate must 
match the NESDIS (TX325) or EUMETSAT (TX326) channel assignment.
- Long data type
16.4.80.8 GOESRTChannel          
Channel used for the random transmission assigned by NESDIS (TX325) or EUMETSAT (TX326).
- Long data type, allowed values:
- 0 = Disable (default). 
- 0 to 566 = Channel 
16.4.80.9 GOESRTInterval
Average time between random transmissions. Maximum interval is 24 hours; minimum interval is 
1 minute.
- String data type entered in the format of “Hours:Minutes:Seconds”.
16. Information tables and settings (advanced)     236

<!-- Page 254 -->
16.4.80.10 GOESSTBaudRate
Baud rate for self-timed transmissions. Valid settings are 300 or 1200. The baud rate must match 
the NESDIS (TX325) or EUMETSAT (TX326) channel assignment.
- Long data type
16.4.80.11 GOESSTChannel
Channel used for the self-timed transmission assigned by NESDIS (TX325) or EUMETSAT (TX326).
- Long data type, allowed values:
- 0 = Disable (default). 
- 0 to 566 = Channel 
16.4.80.12 GOESSTInterval
Time between self-timed transmissions. Maximum interval is 14 days; minimum interval is 1 
minute.
- String data type entered in the format of “Hours:Minutes:Seconds”.
16.4.80.13 GOESSTOffset
Time after midnight for the first self-timed transmission as assigned by NESDIS (TX325) or 
EUMETSAT (TX326).  Maximum offset is 23:59:59. A value of 0 results in no offset.
- String data type entered in the format of “Hours:Minutes:Seconds”.
16. Information tables and settings (advanced)     237
