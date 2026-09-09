---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 169-218
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 14–15: Bảo trì hệ thống & Xử lý sự cố kỹ thuật

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 169 đến 218).  
> **Chủ đề chính**: Hiệu chuẩn trạm (Calibration), thay pin lưu điện Lithium (Tadiran TL 5903/S), nâng cấp hệ điều hành OS, chẩn đoán lỗi bảng Status (Watchdog, Skipped Scans, lỗi bộ nhớ), hiện tượng giá trị rỗng NAN/INF, triệt tiêu vòng lặp mass (Ground loops) và lọc nhiễu đo điện áp.

---


<!-- Page 169 -->
14. CR1000X/CR1000Xe 
maintenance
Protect the data logger from humidity and moisture. The data logger is designed to operate over 
a wide range of temperature and humidity. However, when dew point is reached, condensation 
can occur, potentially damaging  data logger electronics. To prevent this, place adequate 
desiccant inside the instrumentation enclosure to control humidity and keep the dew point 
below the temperature of the data logger electronics. Replace the desiccant periodically.
If sending the data logger to Campbell Scientific for calibration or repair, consult first with 
Campbell Scientific. If the data logger is malfunctioning, be prepared to perform some 
troubleshooting procedures. See:
- Tips and troubleshooting (p. 165)
- Does Your Data Logger Need to be Repaired Blog 
- Troubleshooting Data Acquisition System Blog 
Also, consider checking, or posting your question to, the Campbell Scientific user forum 
https://www.campbellsci.com/forum 
 . Our web site www.campbellsci.com 
  has additional 
manuals (with example programs), FAQs, specifications and compatibility information for all of 
our products.
Video tutorials www.campbellsci.com/videos 
  and blog articles www.campbellsci.com/blog 
  
are also useful troubleshooting resources.
 If calibration or repair is needed, the procedure shown on: https://www.campbellsci.com/repair 
 should be followed when sending the product.
14.1 Data logger calibration
Campbell Scientific recommends factory recalibration every three years. During calibration, all 
the input terminals, peripheral and communications ports, operating system, and memory areas 
are checked; and the internal battery is replaced. The data logger is checked to ensure that all 
hardware operates within published specifications before it is returned. To request recalibration 
for a product, see https://www.campbellsci.com/repair 
 .
14. CR1000X/CR1000Xe maintenance     152

<!-- Page 170 -->
It is recommended that you maintain a level of calibration appropriate to the data logger 
application. Consider the following factors when setting a calibration schedule:
- The importance of the measurements
- How long the data logger will be used
- The operating environment
- How the data logger will be handled
See also About background calibration (p. 153).
Campbell calibration certificates confirm that an instrument meets or exceeds published 
specifications and has been calibrated using standards and instruments with accuracies traceable 
to the National Institute of Standards and Technology, an accepted value of a natural physical 
constant, or a ratio calibration technique. The collective measurement uncertainty of the 
calibration process meets or exceeds a 4:1 accuracy ratio. Our facility's policies and procedures 
comply with ISO 9001 standards.
You can download and print Campbell calibration certificates for many of your purchased 
products by logging in to the Campbell Scientific website and going to: 
https://www.campbellsci.com/calcerts 
 . You will need the product's serial number to access the 
certificate. 
Watch an instructional video at: http://www.campbellsci.com/videos/calibration-certs 
 .
In addition to Campbell calibration certificates, ISO1725 Calibration certificates are available. For 
ISO 17025 calibration, the data logger is verified by an independent certifying body accredited by 
A2LA to ISO 17025 standards. A paper certificate of the A2LA ISO/IEC 17025 accredited 
calibration is provided.
NOTE:
ISO 17205 calibration certificates are not available for download from our website. To request 
a ISO17025 calibration certificate, email support@campbellsci.com or, in the United States, 
call 435-227-9100.
14.1.1 About background calibration
The data logger uses an internal voltage reference to routinely self-calibrate and compensate for 
changes caused by changing operating temperatures and aging. Background calibration 
calibrates only the coefficients necessary to the running CRBasic program. These coefficients are 
reported in the Status table as  CalVolts(),  CalGain(),  CalOffset(), and  CalCurrent().
Background calibration will be disabled automatically when the scan rate is too fast for the 
background calibration measurements to occur in addition to the measurements in the program. 
The Calibrate() instruction can be used to override or disable background calibration. 
14. CR1000X/CR1000Xe maintenance     153

<!-- Page 171 -->
Disable background calibration when it interferes with execution of very fast programs and less 
accuracy can be tolerated. With background calibration disabled, measurement accuracy over 
the operational temperature range is specified as less accurate by a factor of 10. That is, over the 
extended temperature range of –55 °C to 85 °C, the accuracy specification of ±0.08 % of reading 
can degrade to ±0.8 % of reading with background calibration disabled. If the temperature of the 
data logger remains the same, there is little calibration drift when background calibration is 
disabled. 
14.2 Internal battery
The lithium battery powers the internal clock and SRAM when the data logger is not powered. 
This voltage is displayed in the LithiumBattery. See LithiumBattery (p. 206) field in the Status 
table.  Replace the battery when voltage is approximately 2.7 VDC. The internal lithium battery life 
is extended when the data logger is installed with an external power source. If the data logger is 
used in a high-temperature application, the battery life is shortened.
To prevent clock and memory issues, it is recommended you proactively replace the battery 
every 2 to 3 years, or more frequently when operating continuously in high temperatures.
NOTE:
The battery is replaced during regular factory recalibration, which is recommended every 3 
years. For more information, see Data logger calibration (p. 152).
When the lithium battery is removed (or is depleted and  primary power to the data logger is 
removed), the CRBasic program and most settings are maintained, but the following are lost:
- Run-now and run-on power-up settings.
- Routing and communications logs (relearned without user intervention).
- Time. Clock will need resetting when the battery is replaced.
- Final-memory data tables.
A replacement lithium battery can be purchased from Campbell Scientific or another supplier.
- AA, 2.4 Ah, 3.6 VDC (Tadiran TL 5903/S) for battery-backed SRAM and clock. 3-year life 
with no external power source.
 See Power requirements (p. 253) for more information.
14. CR1000X/CR1000Xe maintenance     154

<!-- Page 172 -->
WARNING:
Misuse or improper installation of the internal lithium battery can cause severe injury. Fire, 
explosion, and severe burns can result. Do not recharge, disassemble, heat above 100 °C (212 
°F), solder directly to the cell, incinerate, or expose contents to water. Dispose of spent lithium 
batteries properly.
NOTE:
The Status field Battery value and the destination variable from the Battery() instruction 
(often called batt_volt) in the Public table reference the external battery voltage.
For additional information on the internal battery, visit the Campbell Scientific blog article, Get to 
Know Your Data Logger’s Spare Tire: The Lithium Battery 
 .
14.2.1 Replacing the internal battery
It is recommended that you send the data logger in for scheduled calibration, which includes 
internal battery replacement. See Data logger calibration (p. 152).
WARNING:
Any damage made to the data logger during user replacement of the internal battery is not 
covered under warranty. 
 1. Remove the two screws from the back of the panel.
14. CR1000X/CR1000Xe maintenance     155

<!-- Page 173 -->
 2. Pull one edge of the canister away from the wiring panel to loosen it from the internal 
connectors.
 3. Lift the canister edge out of the enclosure tabs.
 4. Remove the nuts, then open the clam shell.
14. CR1000X/CR1000Xe maintenance     156

<!-- Page 174 -->
 5. Remove the lithium battery by gently prying it out with a small flat-bladed screwdriver. 
Replace it with a new battery.
 6. Reassemble the data logger. Take particular care to ensure the canister is reseated tightly 
into the connectors by firmly pressing them together by hand.
14.3 Updating the operating system
Campbell Scientific posts operating system (OS) updates at 
https://www.campbellsci.com/downloads 
  when they become available. It is recommended 
that before deploying instruments, you check operating system versions and update them as 
needed. The data logger operating system version is shown in the Status table, Station Status 
Summary, and Device Configuration Utility Deployment > Datalogger. An operating system may 
be sent through Device Configuration Utility or through program-send procedures.
CAUTION:
CR1000X data loggers with Serial Numbers 34000 and newer have hardware requiring the 
use of OS version 5.02 or newer.
WARNING:
Because sending an OS resets data logger memory and resets all settings on the data logger 
to factory defaults, data loss will certainly occur. Depending on several factors, the data 
logger may also become incapacitated until the new OS is programmed into memory.
TIP:
It is recommended that you retrieve data from the data logger and back up your programs 
and settings before updating your OS. To collect data using LoggerNet, connect to your data 
logger and click Collect Now
 . To backup your data logger, connect to it in Device 
Configuration Utility, click the Backup menu and select Backup Datalogger.
14. CR1000X/CR1000Xe maintenance     157

<!-- Page 175 -->
14.3.1 Sending an operating system to a local data logger
Send an OS using Device Configuration Utility. This method requires a direct connection between 
your data logger and computer.
 1. Download the latest Operating System at https://www.campbellsci.com/downloads 
 .
 2. Locate the .exe download and double-click to run the file. This will extract the .obj  OS file to 
the C:\Campbellsci\Lib\OperatingSystems folder.
 3. Supply power to the data logger. If connecting via USB for the first time, you must first 
install USB drivers by using Device Configuration Utility (select your data logger, then on 
the main page, click Install USB Driver). Alternatively, you can install the USB drivers using 
EZ Setup. A USB connection supplies 5 V power (as well as a communications link), which is 
adequate for setup, but a 12 V battery will be needed for field deployment. 
 4. Physically connect your data logger to your computer using a USB cable, then open Device 
Configuration Utility and select your data logger.
 5. Select the communications port used to communicate with the data logger from the COM 
Port list (you do not need to click Connect).
 6. Click the Send OS tab. At the bottom of the window, click Start.
 7. On the Avoid Conflicts with the Local Server window, click OK.
 8. Navigate to the C:\Campbellsci\Lib\OperatingSystems folder.
 9. Ensure Datalogger Operating System Files (*.obj) is selected in the Files of type list, select 
the new OS .obj file, and click Open to update the OS on the data logger.
 10. The Power and Act LEDs on the CR6  will flash red as the operating system is updated. After 
the flashing stops, return to the Deployment | Datalogger tab and check that the  operating 
system shows the updated version. You may have to press the F5 key to refresh the 
displayed information.
Watch a video: Sending an OS to a Local Datalogger 
 .
14.3.2 Sending an operating system to a remote data 
logger
If you have a data logger that is already deployed, you can update the OS over a 
telecommunications link by sending the OS to the data logger as a program. In most instances, 
sending an OS as a program preserves settings. This allows for sending supported operating 
systems remotely (check the release notes). However, this should be done with great caution as 
14. CR1000X/CR1000Xe maintenance     158

<!-- Page 176 -->
updating the OS may reset the data logger settings, even settings critical to supporting the 
telecommunication link.
The default.CR1X program can be edited to preserve critical data logger settings such as 
communications settings. See Default program (p. 64) for more information.
 1. Download the latest Operating System at https://www.campbellsci.com/downloads 
 .
 2. Locate the .exe download and double-click to run the file. This will extract the .obj OS file to 
the C:\Campbellsci\Lib\OperatingSystems folder.
 3. Using data logger support software, connect to your data logger.
- LoggerNet users, select Main and click Connect
  on the LoggerNet toolbar, select 
the data logger from the Stations list, then click Connect
 .
- PC400 users, select the data logger from the list and click Connect
 .
 4. Select File Control
  at the top of the Connect window.
 5. Click Send
  at the top of the File Control window.
 6. Navigate to the C:\Campbellsci\Lib\OperatingSystems folder.
 7. Ensure Datalogger Operating System Files (*.obj) is selected in the files of type list, select 
the new OS .obj file, and click Open to update the OS on the data logger.
Note the following precautions when sending as a program:
- Any peripherals being powered through the SW12 terminals will be turned off until the 
program logic turns them on again.
14. CR1000X/CR1000Xe maintenance     159

<!-- Page 177 -->
- Operating systems are very large files. Be cautious of data charges. Sending over a direct 
serial or USB connection is recommended, when possible.
14.4 gzip
The CR1000X/CR1000Xe supports the ability to extract the contents of program, operating 
system, and other files that have been created using gzip. The file name must be in the format: 
filename.fileextension.gz (for example: TestPgm.CR1X.gz, CR1000X.Std.01.obj.gz, or 
CR1000X.Std.01.web.obj.gz).
For more information see: www.gzip.org 
 .
Zipping a file can significantly reduce its size, resulting in fewer bytes to transfer when sending a 
zipped file to a data logger. This is especially beneficial over slow, high-latency, or costly 
telecommunications links. Therefore, those using low-baud-rate radios, satellite, or restricted 
cellular data plans should consider gzipping  operating systems and large programs before 
sending.
Compatible files can be created using any utility that supports the gzip file format. Use a file 
tarball (filename.tar.gz) to compress multiple files. Several free utilities provide zipping 
to these formats. 
Send the zipped file to the CPU:, CRD: or USB: drive using data logger support software. Files sent 
using Connect > Send Program will be unzipped automatically. However, the data logger will not 
automatically unzip files that are sent using File Control > Send File. To unzip files sent with File 
Control, mark them as Run Now.
Unzipping and installing file contents takes a long time; expect several minutes for operating 
systems and additional time for .web files. The details of unzipping and installing files from a 
gzip file are as follows: 
 1. The data logger receives the gzip file and restarts.
 2.  The data logger unzips the .gz file to the same drive to which it was sent.
 3. The .tar portion of the file, if available, is processed.
 4. Operating system (.obj or .iobj files) are programmed to the respective destination.
 5. The data logger restarts.
 6. When an .obj file is involved the OS will be loaded by the boot code resulting in another 
restart.
 7. Web user interface (.web) files, if available, are installed. This may take over ten minutes.
14. CR1000X/CR1000Xe maintenance     160

<!-- Page 178 -->
NOTE:
Compression has little effect on an encrypted program (FileEncrypt() ) and on files that 
already employ compression such as JPEG or MP4.
TIP:
The data logger also has the ability to compress files using GZip(). See the CRBasic Editor 
help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
14.5 File management via powerup.ini
Another way to upload a program, install a data logger OS, or format a drive is to create a 
powerup.ini file. The file is created with a text editor and saved to a memory card or SC115 
with the associated files. Alternatively, the powerup.ini file and associated files can be saved 
to the data logger using the data logger support software File Control > Send command. With 
the memory card or SC115 connected, or with the powerup.ini file saved in the data logger 
memory, a power cycle to the data logger begins the process chosen in the powerup.ini file. 
 1. When the CR1000X/CR1000Xe powers up, it executes commands in the powerup.ini file 
(on an attached USB drive or memory card) including commands to set the CRBasic 
program file attributes to Run Now or Run On Power-up.
 2. When the CR1000X/CR1000Xe powers up, a program file marked as Run On Power-up will 
run. If that program includes a file specified by the Include File setting, it will be 
incorporated into the program that runs.
 3. If there is no program file marked Run Now or Run On Power-up (or if the program 
selected to run cannot be compiled), the data logger will run the program specified by the 
IncludeFile setting. For more information see IncludeFile (p. 219).
 4. If the IncludeFile program cannot be compiled or if no program is specified, the data 
logger will attempt to run the program named default.CR1X on its CPU: drive.
 5. If there is no default.CR1X file or it cannot be compiled, the CR1000X/CR1000Xe will not 
automatically run any program.
See Default program (p. 64) for more information. 
Syntax for the powerup.ini file and available options follow.
14.5.1 Syntax
Syntax for powerup.ini is:
Command,File,Device
14. CR1000X/CR1000Xe maintenance     161

<!-- Page 179 -->
where,
- Command is one of the numeric commands in the following table.
- File is the accompanying operating system or user program file.
- Device is the data logger memory drive to which the accompanying operating system or 
user program file is copied (usually CPU).  If left blank or with an invalid option, default 
device will be CPU.  Use the same drive designation as the transporting external device if 
the preference is to not copy the file.
WARNING:
Uploading a program, installing a data logger OS, or formatting a drive may result in data 
loss. Depending on several factors, the data logger may also become incapacitated for a 
time. It is recommended that you retrieve data from the data logger and back up your 
programs before sending a powerup.ini file; otherwise, data may be lost. To collect data using 
LoggerNet, connect to your data logger and click Collect Now 
 . To backup your data 
logger, connect to it in Device Configuration Utility, click the Backup menu and select Backup 
Datalogger.
Table 14-1: Powerup.ini commands
Command Action Details
1 Run always,
preserve data
Copies a program file to a drive and sets the program to both 
Run Now and Run on Power Up.  Data on a memory card from 
the previously running program will be preserved if table 
structures have not changed.  
2 Run on
power up
Copies a program file to a drive and sets the program to Run 
Always unless command 6 or 14 is used to set a separate Run 
Now program. 
5 Format Formats a drive.
6 Run now,
preserve data
Copies a program file to a drive and sets the program to Run 
Now.  Data on a memory card from the previously running 
program will be preserved if table structures have not 
changed.
7 Copy
support files
Copies a file, such as an Include or program support file, to 
the specified drive.
9 Load OS
(File= .obj)
Loads an .obj file to the CPU drive and then loads the .obj 
file as the new data logger operating system.
14. CR1000X/CR1000Xe maintenance     162

<!-- Page 180 -->
Table 14-1: Powerup.ini commands
Command Action Details
13 Run always,
erase data
Copies a program to a drive and sets the program to  both 
Run Now and Run on Power Up.  Data on a memory card from 
the previously running program will be erased.
14 Run now,
erase data
Copies a program to a drive and sets the program to Run 
Now.  Data on a memory card from the previously running 
program will be erased.
15 Move file Moves a file, such as an Include or program support file, to 
the specified drive.
14.5.2 Example powerup.ini files
Comments can be added to the file by preceding them with a single-quote character ('). All text 
after the comment mark on the same line is ignored.
TIP:
Test the powerup.ini file and procedures in the lab before going to the field.  Always carry 
a laptop or mobile device (with data logger support software) into difficult- or expensive-to-
access places as backup.
Example: Code Format and Syntax
'Command = numeric power up command
'File = file associated with the action
'Device = device to which File is copied. Defaults to CPU             
'Command,File,Device
13,Write2CRD_2.CR1X,cpu:
Example: Run Program on Power Up
'Copy program file pwrup.CR1X from the external drive to CPU:             
'File will run only when the data logger is powered-up later.
2,pwrup.CR1X,cpu:
Example: Format the USR Drive
5,,usr:
Example: Send OS on Power Up
'Load an operating system (.obj) file into FLASH as the new OS
9,CR1000X.Std.01.obj
14. CR1000X/CR1000Xe maintenance     163

<!-- Page 181 -->
Example: Run Program from SC115 Flash Memory Drive
'A program file is carried on an SC115 Flash Memory drive.             
'Do not copy program file from SC115
'Run program always, erase data.
13,toobigforcpu.CR1X,usb:
Example: Always Run Program, Erase Data
13,pwrup_1.CR1X,cpu:
Example: Run Program Now and Erase Data Now
14,run.CR1X,cpu:
14. CR1000X/CR1000Xe maintenance     164

<!-- Page 182 -->
15. Tips and troubleshooting
Start with these basic procedures if a system is not operating properly.
 1. Ensure your system is well grounded. See Grounds (p. 15). The symptoms of a poorly 
grounded system range from bad measurements, to intermittent communications, to 
damaged hardware.
 2. Using a voltmeter, check the voltage of the primary power source at the POWER IN 
terminals on the face of the data logger, it should be 10 to 18 VDC. 
 3. Check wires and cables for the following:
- Incorrect wiring connections. Make sure each sensor and device are wired to the 
terminals assigned in the program. If the program was written in Short Cut, check 
wiring against the generated wiring diagram. If written in CRBasic Editor, check wiring 
against each measurement and control instruction.
- Loose connection points
- Faulty connectors
- Cut wires
- Damaged insulation, which allows water to migrate into the cable. Water, whether or 
not it comes in contact with wire, can cause system failure. Water may increase the 
dielectric constant of the cable sufficiently to impede sensor signals, or it may 
migrate into the sensor, which will damage sensor electronics.
 4. Check the CRBasic program. If the program was written solely with Short Cut, the program 
is probably not the source of the problem. If the program was written or edited with 
CRBasic Editor, logic and syntax errors could easily have crept in. To troubleshoot, create a 
simpler version of the program, or break it up into multiple smaller units to test individually. 
For example, if a sensor signal-to-data conversion is faulty, create a program that only 
measures that sensor and stores the data, absent from all other inputs and data.
 5. Reset the data logger. Sometimes the easiest way to resolve a problem is by resetting the 
data logger (see Resetting the data logger (p. 173) for more information).
For additional troubleshooting options, see:
15.1 Checking station status 166
15.2 Understanding NAN and INF occurrences 169
15. Tips and troubleshooting     165

<!-- Page 183 -->
15.3 Timekeeping 170
15.4 CRBasic program errors 171
15.5 Resetting the data logger 173
15.6 Troubleshooting power supplies 174
15.7 Using terminal mode 175
15.8 Ground loops 181
15.9 Improving voltage measurement quality 185
15.10 Field calibration 199
15.11 File system error codes 199
15.12 File name and resource errors 200
15.13 Background calibration errors 200
Also, consider checking, or posting your question to, the Campbell Scientific user forum 
https://www.campbellsci.com/forum 
 . Our web site www.campbellsci.com 
  has additional 
manuals (with example programs), FAQs, specifications and compatibility information for all of 
our products.
Video tutorials www.campbellsci.com/videos 
  and blog articles www.campbellsci.com/blog 
  
are also useful troubleshooting resources.
15.1 Checking station status
View the condition of the data logger using Station Status. Here you see the operating system 
version of the data logger, the name of the current program, program compile results, and other 
key indicators. Items that may need your attention appear in red or blue. The following 
information describes the significance of some entries in the station status window. Watch a 
video at: https://www.campbellsci.com/videos/connect-station-status 
  or use the following 
instructions.
15.1.1 Viewing station status
Using your data logger support software, access the Station Status to view the condition of the 
data logger.
- From LoggerNet: Click Connect 
 , then  Station Status 
  to view the Summary tab.
- From PC400: Select the Datalogger menu and  Station Status 
  to view the Summary tab.
15. Tips and troubleshooting     166

<!-- Page 184 -->
15.1.2 Watchdog errors
Watchdog errors indicate that the data logger has crashed and reset itself. Experiencing 
occasional watchdog errors is normal. You can reset the Watchdog error counter  in the Station 
Status > Status Table. 
TIP:
Before resetting the counter, make note of the number accumulated and the date. 
Watchdog errors could be due to:
- Transient voltage
- Incorrectly wired or malfunctioning sensor
- Poor ground connection on the power supply
- Numerous PortSet() instructions back-to-back with no delay
- High-speed serial data on multiple ports with very large data packets or bursts of data
The error "Results for Last Program Compiled: Warning: Watchdog Timer IpTask Triggered" can 
result from:
- The IP communications on the data logger got stuck, and the data logger had to reboot 
itself to recover. Or communications failures may cause the data logger to reopen the IP 
connections more than usual. Check your data logger operating system version; recent 
operating system versions have improved stability of IP communications.
An IP panic watchdog error may be caused by insufficient communications memory. Try 
increasing the Communication Allocation field in Device Configuration Utility.
TIP:
 It is good practice to always retrieve data from the data logger before changing settings; 
otherwise, data may be lost. See Collecting data (p. 77)  for detailed instruction.
15. Tips and troubleshooting     167

<!-- Page 185 -->
If any of these are not the apparent cause, contact Campbell Scientific for assistance (see 
https://www.campbellsci.com/support 
 ). Causes that may require assistance include:
- Memory corruption
- Operating System problem
- Hardware problem
- IP communications problem
Additionally, a watchdogInfo.txt file may be created on the CPU drive when the 
CR1000X/CR1000Xe experiences a software reset (rather than a hardware reset that increments 
the WatchdogErrors field in the Status Table). Postings of watchdoginfo.txt files are rare. If 
ths file appears, contact Campbell Scientific for assistance (see 
https://www.campbellsci.com/support 
 ).
15.1.3 Results for last program compiled
Messages generated by the data logger at program upload and as the program runs are 
reported here. Warnings indicate that an expected feature may not work, but the program will 
still operate. Errors indicate that the program cannot run. For more information, see CRBasic 
program errors (p. 171).
15.1.4 Skipped scans
Skipped scans are caused when a program takes longer to process than the scan interval allows. 
If any scan skips repeatedly, the data logger program may need to be optimized or reduced. For 
more information, see: How to Prevent Skipped Scans and a Sunburn 
 .
15.1.5 Skipped records
Skipped records usually occur because a scan is skipped. They indicate that a record was not 
stored to the data table when it should have been.
15.1.6 Variable out of bounds
Variable-out-of-bounds errors happen when an array is not sized to the demands of the 
program. The data logger attempts to catch out-of-bounds errors at compile time. However, it is 
not always possible; when these errors occur during runtime the variable-out-of-bounds field 
increments. Variable-out-of-bounds errors are always caused by programming problems. 
15. Tips and troubleshooting     168

<!-- Page 186 -->
15.1.7 Battery voltage
If powering through USB, reported battery voltage should be 0 V. If connecting to an external 
power source, battery voltage should be reported at or near 12 V. See also:
- Power input (p. 12)
- Power requirements (p. 253)
15.2 Understanding NAN and INF occurrences
NAN (not a number) and INF (infinite) are data words indicating an exceptional occurrence in 
data logger function or processing.  INF indicates that the program has encountered an 
undefined arithmetic expression, such as 0 ÷ 0.  NAN indicates an invalid measurement. For more 
information, see Tips and Tricks: Who's NAN? 
 .
 NANs are expected in the following conditions:
- Input signals exceed the voltage range chosen for the measurement.
- An invalid SDI-12 command is sent.
- An SDI-12 sensor does not respond or aborts without sending data.
 NAN is a constant that can be used in expressions. This is shown in the following example code 
that sets a CRBasic variable to False when the wind direction is  NAN:
If WindDir = NAN Then
   WDFlag = False
Else
   WDFlag = True
EndIf
If an output processing instruction encounters a NAN in the values being processed,  NAN will be 
stored. For example, if one measurement in a data storage interval results in NAN, then the 
average, maximum and minimum will record NAN. However, because  NAN is a constant, it can 
be used in conjunction with the disable variable parameter (DisableVar) in output processing 
instructions. Use variable = NAN in the DisableVar parameter to discard  NANs from affecting 
the other good values. The following example code discards NAN WindSpeed measurements 
from the Minimum output:
Minimum (1,WindSpeed,FP2,WindSpeed=NAN,False)
NOTE:
There is no such thing as  NAN for integers. Values that are converted from float to integer will 
be expressed in data tables as the most negative number for a given data type. For example, 
15. Tips and troubleshooting     169

<!-- Page 187 -->
the most negative number of data type FP2 is –7999; so,  NAN for FP2 data will appear in a 
data table as –7999. If the data type is Long,  NAN will appear in the data table as –
2147483648.
15.3 Timekeeping
Measurement of time is an essential data logger function. Time measurement with the onboard 
clock enables the data logger to run on a precise interval, attach time stamps to data, measure 
the interval between events, and time the initiation of control functions. Details on clock accuracy 
and resolution are available in the System specifications (p. 252). An internal lithium battery backs 
the clock when the data logger is not externally powered. See Internal battery (p. 154).
15.3.1 Clock best practices
When setting the clock with LoggerNet, initiate it manually during a maintenance period when 
the data logger is not actively writing to Data Tables. Click Set in the Clocks field of the LoggerNet 
Connect Screen.
If you are going to use automated clock check with LoggerNet  (clock settings can be found on the 
LoggerNet Setup Standard View Clock tab). it is recommended that you do this on the order of 
days (not hours). Set an allowed clock deviation that is appropriate for the expected jitter in the 
network, and use the initial time setting to offset the clock check away from storage and 
measurement intervals.
The amount of time required for a Clock Check command to reach the data logger, be 
processed, and for it to send its response is called round-trip time, or time-of-flight. To calculate 
an estimate of this time-of-flight, LoggerNet maintains a history (in order) of the round-trip times 
for the ten previous successful clock check transactions. It adds this average to the time values 
received from the data logger and subtracts it from any adjustment that it might make.
15.3.2 Time stamps
A measurement without an accurate time reference often has little meaning.  Data collected from 
data loggers is stored with time stamps.  How closely a time stamp corresponds to the actual time 
a measurement is taken depends on several factors.
The time stamp in common CRBasic programs matches the time at the beginning of the current 
scan as measured by the real-time data logger clock.  If a scan starts at 15:00:00, data output 
during that scan will have a time stamp of 15:00:00 regardless of the length of the scan, or when 
in the scan a measurement is made.  The possibility exists that a scan will run for some time before 
a measurement is made.  For instance, a scan may start at 15:00:00, execute a time-consuming 
15. Tips and troubleshooting     170

<!-- Page 188 -->
part of the program, then make a measurement at 15:00:00.51.  The time stamp attached to the 
measurement, if the CallTable() instruction is called from within the 
Scan() / NextScan construct, will be 15:00:00, resulting in a time-stamp skew of 510 ms.
15.3.3 Avoiding time skew
Time skew between consecutive measurements is a function of settling and integration times, 
ADC, and the number entered into the Reps parameter of CRBasic instructions.  A close 
approximation is:
time skew = reps * (settling time + integration time + ADC time) + instruction setup 
time
where ADC time equals 170 µs, and instruction setup time is 15 µs.
If reps (repetitions) > 1 (multiple measurements by a single instruction), no setup time 
is required.  If reps = 1 for consecutive voltage instructions, include the setup time for 
each instruction.
Time-stamp skew is not a problem with most applications because:
- Program execution times are usually short; so, time-stamp skew is only a few milliseconds.  
Most measurement requirements allow for a few milliseconds of skew.
- Data processed into averages, maxima, minima, and so forth are composites of several 
measurements.  Associated time stamps only reflect the time of the scan when processing 
calculations were completed; so, the significance of the exact time a specific sample was 
measured diminishes.
Applications measuring and storing sample data wherein exact time stamps are required can be 
adversely affected by time-stamp skew.  Skew can be avoided by:
- Making measurements in the scan before time-consuming code.
- Programming the data logger such that the time stamp reflects the system time rather than 
the scan time using the DataTime() instruction. See the CRBasic Editor help for detailed 
instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
15.4 CRBasic program errors
Analyze data soon after deployment to ensure the data logger is measuring and storing data as 
intended.  Most measurement and data-storage problems are a result of one or more CRBasic 
program bugs. Watch a video: CRBasic > Common Errors - Identifying and fixing common errors 
in the CRBasic programming language .
15. Tips and troubleshooting     171

<!-- Page 189 -->
15.4.1 Program does not compile
When a program is compiled, the CRBasic Editor checks the program for syntax errors and other 
inconsistencies. The results of the check are displayed in a message window at the bottom of the 
main window. If an error can be traced to a specific line in the program, the line number will be 
listed before the error. Double-click an error preceded by a line number and that line will be 
highlighted in the program editing window. Correct programming errors and recompile the 
program. 
Occasionally, the CRBasic Editor compiler states that a program compiles OK; however, the 
program may not compile in the data logger itself.  This is rare, but reasons may include:
- The data logger has a different operating system than the computer compiler. Check the 
two versions if in doubt.  The computer compiler version is shown on the first line of the 
compile results. Update the computer compiler by first downloading the executable OS file 
from www.campbellsci.com 
 . When run, the executable file updates the computer 
compiler. To update the data logger operating system, see Updating the operating system 
(p. 157).
- The program has large memory requirements for data tables or variables and the data 
logger does not have adequate memory.  This normally is flagged at compile time in the 
compile results. If this type of error occurs: 
- Check the CPU drive for copies of old programs.  The data logger keeps copies of all 
program files unless they are deleted, the drive is formatted, or a new operating 
system is loaded with Device Configuration Utility.
- Check the USR drive size. If it is too large it may be using memory needed for the 
program.
- Ensure a memory card is available when a program is attempting to access the CRD 
drive. 
15.4.2 Program compiles but does not run correctly
If the program compiles but does not run correctly, timing discrepancies may be the cause. If a 
program is tight on time, look further at the execution times.  Check the measurement and 
processing times in the Status table (MeasureTime, ProcessTime, MaxProcTime) for all scans, 
then try experimenting with the InstructionTimes() instruction in the program. Analyzing 
InstructionTimes() results can be difficult due to the multitasking nature of the data 
logger, but it can be a useful tool for fine-tuning a program. For more information, see Status 
table system information (p. 204).
15. Tips and troubleshooting     172

<!-- Page 190 -->
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
15.5 Resetting the data logger
A data logger reset is sometimes referred to as a "memory reset." Backing up the current data 
logger configuration before a reset makes it easy to revert to the old settings. To back up the 
data logger configuration, connect to the data logger using Device Configuration Utility, and 
click Backup > Back Up Datalogger. To restore a configuration after the data logger has been 
reset, connect and click Backup > Restore Datalogger.
The following features are available for complete or selective reset of data logger memory:
- Processor reset
- Program send reset
- Manual data table reset
- Formatting memory drives
- Full memory reset
15.5.1 Processor reset
To reset the processor, simply power cycle the data logger. This resets its short-term memory, 
restarts the current program, sets variables to their starting values, and clears communications 
buffers. This does not clear data tables but may result in a skipped record. If the data logger is 
remote, a power cycle can be mimicked in a  Terminal Emulator program (type REBOOT <Enter>).
15.5.2 Program send reset
Final-data memory is erased when user programs are uploaded, unless preserve / erase data 
options are used and the program was not altered.  Preserve / erase data options are presented 
when sending programs using File Control Send command and CRBasic Editor Compile, Save and 
Send.
TIP:
It is good practice to always retrieve data from the data logger before sending a program; 
otherwise, data may be lost. See Collecting data (p. 77) for detailed instruction.
When a program compiles, all variables are initialized. A program is recompiled after a power 
failure or a manual stop. For instances that require variables to be preserved through a program 
recompile, consider PreserveVariables().
15. Tips and troubleshooting     173

<!-- Page 191 -->
15.5.3 Manual data table reset
Data table memory is selectively reset from:
- Datalogger support software: Station Status 
  > Table Fill Times tab, Reset Tables.
- Device Configuration Utility: Data Monitor tab, Reset Table button.
- CR1000KD Keyboard/Display add-on: Data > Reset Data Tables.
15.5.4 Formatting drives
CPU, USR, CRD (memory card required), and USB (module required) drives can be formatted 
individually.  Formatting a drive erases all files on that drive.  If the currently running user program 
is on the drive to be formatted, the program will cease running and  data associated with the 
program are erased.  Drive formatting is performed through the data logger support software File 
Control > Format command.
15.5.5 Full memory reset
Full memory reset occurs when an operating system is sent to the data logger using Device 
Configuration Utility or when entering 98765 in the Status table field FullMemReset. See 
FullMemReset (p. 206). A full memory reset does the following:
- Clears and formats CPU drive (all program files erased)
- Clears data tables
- Clears Status table fields
- Restores settings to default
- Initializes system variables
- Clears communications memory
Full memory reset does not affect the CRD drive directly.  Subsequent user program uploads, 
however, can erase CRD. See Updating the operating system (p. 157) for more information.
15.6 Troubleshooting power supplies
Power supply systems may include batteries, charging regulators, and a primary power source 
such as solar panels or ac/ac or ac/dc transformers attached to mains power.  All components 
may need to be checked if the power supply is not functioning properly. Check connections and 
check polarity of connections.
Base diagnostic: connect the data logger to a new 12 V battery. (A small 12 V battery carrying a 
full charge would be a good thing to carry in your maintenance tool kit.)  Ensure correct polarity 
15. Tips and troubleshooting     174

<!-- Page 192 -->
of the connection.    If the data logger powers up and works, troubleshoot the data logger power 
supply.
When diagnosing or adjusting power equipment supplied by Campbell Scientific, it is 
recommended you consider:
- Battery-voltage test
- Charging-circuit test (when using an unregulated solar panel)
- Charging-circuit test (when using a transformer)
- Adjusting charging circuit
If power supply components are working properly and the system has peripherals with high 
current drain, such as a satellite transmitter, verify that the power supply is designed to provide 
adequate power. For additional information, see Power budgeting (p. 66).
See also
- Measuring Data Logger Output Voltage With a Multimeter 
- Checking Continuity 
- Measuring Current Drain 
15.7 Using terminal mode
Table 15-1 (p. 176) lists terminal mode options. With exception of perhaps the C command, 
terminal options are not necessary to routine CR1000X/CR1000Xe operations.
To enter terminal mode, connect a computer to the CR1000X/CR1000Xe. See Setting up 
communications with the data logger (p. 23). Open a terminal emulator program from Campbell 
Scientific data logger support software: 
- Connect window > Datalogger menu item> Terminal Emulator...
- Device Configuration Utility Terminal tab
After entering a terminal emulator, press Enter a few times until the prompt CR1000X> is 
returned. Terminal commands consist of specific characters followed by Enter. Sending an H and 
Enter will return the terminal emulator menu.
ESC or a 40 second timeout will terminate on-going commands. Concurrent terminal sessions 
are not allowed and will result in dropped communications.
Terminal commands are subject to change. Please consult Campbell Scientific for assistance if 
you are not familiar with the effects of a command.
15. Tips and troubleshooting     175

<!-- Page 193 -->
Table 15-1: CR1000X/CR1000Xe terminal commands
Command Description Use
0 Scan processing time; real 
time in seconds Lists technical data concerning program scans.
1 Serial FLASH data dump  Campbell Scientific engineering tool 
2 Read clock chip Lists binary data concerning the CR1000X/CR1000Xe 
clock chip.
3 Status Lists the CR1000X/CR1000Xe Status table.
4 Card status and compile 
errors
Lists technical data concerning an installed memory 
card.
5 Scan information Technical data regarding the CR1000X/CR1000Xe 
scan.
6 Raw A/D values Technical data regarding analog-to-digital 
conversions.
7 VARS Lists Public table variables.
8 Suspend / start data output
Outputs all table data. This is not recommended as a 
means to collect data, especially over comms. Data 
are dumped as non-error checked ASCII.
9 Read inloc binary Lists binary form of Public table.
A Operating system copyright Lists copyright notice and version of operating 
system.
B Task sequencer op codes Technical data regarding the task sequencer.
C Modify constant table
Edit constants defined with ConstTable / 
EndConstTable. Only active when ConstTable / 
EndConstTable in the active program.
D MTdbg() task monitor  Campbell Scientific engineering tool 
E Compile errors Lists compile errors for the current program 
download attempt.
F Settings and predefined 
constants names Lists predefined constants and settings
G CPU serial flash dump  Campbell Scientific engineering tool 
15. Tips and troubleshooting     176

<!-- Page 194 -->
Table 15-1: CR1000X/CR1000Xe terminal commands
Command Description Use
H Terminal emulator menu Lists main menu.
I Calibration data Lists gains and offsets resulting from internal 
calibration of analog measurement circuitry.
J Download file dump Sends text of current program including comments.
L Peripheral bus read  Campbell Scientific engineering tool 
M Memory check Lists memory-test results.
N File system information Lists files in CR1000X/CR1000Xe memory.
O Data table sizes Lists technical data concerning data-table sizes.
P Serial talk through
Issue commands from keyboard that are passed 
through the logger serial port to the connected 
device. Similar in concept to SDI12 Talk Through. No 
timeout when connected via PakBus.
PING PING <HOST> The HOST can be a DNS name or an IP address. For 
example, PING CAMPBELLSCI.COM.
REBOOT Program recompile
Typing “REBOOT” rapidly will recompile the 
CR1000X/CR1000Xe program immediately after the 
last letter, "T", is entered. Table memory is retained. 
NOTE: When typing REBOOT, characters are not 
echoed (printed on terminal screen).
SDI12 SDI12 talk through
Issue commands from keyboard that are passed 
through the CR1000X/CR1000Xe SDI-12 port to the 
connected device. Similar in concept to Serial Talk 
Through. See also SDI-12 transparent mode (p. 178)
T Unused  
U Data recovery
Provides the means by which data lost when a new 
program is loaded may be recovered. Contact 
Campbell Scientific support.
V Low level memory dump  Campbell Scientific engineering tool 
15. Tips and troubleshooting     177

<!-- Page 195 -->
Table 15-1: CR1000X/CR1000Xe terminal commands
Command Description Use
W Comms Watch (Sniff)
Enables monitoring of CR1000X/CR1000Xe 
communications traffic. No timeout when 
connected via PakBus.
X Peripheral bus module 
identify  Campbell Scientific engineering tool 
15.7.1 Serial talk through and comms watch
The P: Serial Talk Through and W: Comms Watch ("sniff") modes do not have a timeout when 
connected in terminal mode via PakBus. Otherwise, the timeout can be changed from the default 
of 40 seconds to any value ranging from 1 to 86400 seconds (86400 seconds = 1 day).
When using options P or W in a terminal session, consider the following:
- Concurrent terminal sessions are not allowed by the CR1000X/CR1000Xe.
- Opening a new terminal session will close the current terminal session.
- The data logger will attempt to enter a terminal session when it receives non-PakBus 
characters on the RS-232 port or CS I/O port, unless the port is first opened with the 
SerialOpen() instruction.
If the data logger attempts to enter a terminal session on the RS-232 port or CS I/O port because 
of an incoming non-PakBus character, and that port was not opened using SerialOpen(), any 
currently running terminal function, including the comms watch, will immediately stop. So, in 
programs that frequently open and close a serial port, the probability is higher that a non-PakBus 
character will arrive at the closed serial port, thus closing an existing talk-through or comms 
watch session. If this occurs, use the FilesManager setting to send comms watch or sniffer to a 
file.
For more information on Comms Watch, see a video 
at: https://www.campbellsci.com/videos/sdi12-sensors-watch-or-sniffer-mode 
 .
15.7.2 SDI-12 transparent mode
All SDI-12 probes have just three wires—a signal, ground, and 12 V power line. They are 
connected to the data logger according to the following table.
15. Tips and troubleshooting     178

<!-- Page 196 -->
Table 15-2: SDI-12 probe connections
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
To enter the SDI-12 transparent mode, enter the data logger support software terminal emulator:
 1. Press Enter until the data logger responds with the prompt CR1000X>. 
 2. Type SDI12 at the prompt and press Enter.
 3.  In response, the query Select SDI12 Port is presented with a list of available ports.  
Enter the port number assigned to the terminal to which the SDI-12 sensor is connected, 
and press Enter.  For example, 1 is entered for terminal C1.
15. Tips and troubleshooting     179

<!-- Page 197 -->
 4.  An Entering SDI12 Terminal response indicates that SDI-12 transparent mode is 
active and ready to transmit SDI-12 commands and display responses.
15.7.2.1 Watch command (sniffer mode)
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
15.7.2.2 SDI-12 transparent mode commands
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
15. Tips and troubleshooting     180

<!-- Page 198 -->
15.8 Ground loops
A ground loop is a condition in an electrical system that contains multiple conductive paths for 
the flow of electrical current between two nodes. Multiple paths are usually associated with the 
ground or 0 V-potential point of the circuit. Ground loops can result in signal noise, 
communications errors, or a damaging flow of ground current on long cables. Most often, 
ground loops do not have drastic negative effects and may be unavoidable. Special cases exist 
where additional grounding helps shield noise from sensitive signals; however, in these cases, 
multiple ground conductors are usually run tightly in parallel without conductive shielding 
material placed between the parallel grounds. If possible, ground loops should be avoided. 
When problems arise in a system, ground loops may be the source of the problems.
For more information on ground loops, watch an instructional video: Avoiding Ground Loops 
 . 
See also Grounds (p. 15)
15.8.1 Common causes
Some of the common causes of ground loops include the following:
- The drain wire of a shielded cable is connected to the local ground at both ends, and the 
ground is already being carried by a conductor inside the cable. In this case, two wires, one 
on either side of the cable shield, are connected to the ground nodes at both ends of the 
cable.
- A long cable connects the grounds of two electrical devices, and the mounting structure or 
grounding rod also directly connects the grounds of each device to the local earth ground. 
The two paths, in this case, are the connecting cable and earth itself.
- When electrical devices are connected to a common metal chassis such as an instrument 
tower, the structure can create a ground path in parallel to the ground wires in sensor 
cables running over the structure.
- Conductors connected to ground are found in most cables that connect to a data logger. 
These include sensors cables, communications cables, and power cables. Any time one of 
these cables connects to the same two endpoints as another cable, a ground loop is 
formed.
15.8.2 Detrimental effects
The harm from a ground loop can be seen in different ways. One consideration is the 
electromagnetically induced effect. This will manifest as AC noise or an AC pulse. As seen in 
15. Tips and troubleshooting     181

<!-- Page 199 -->
Figure 15-1 (p. 182) the parallel conductive paths form an electrical loop that acts as an antenna to 
pick up electromagnetic energy.
Figure 15-1. Stray AC magnetic fields picked up in loop antenna
- Relatively small electromagnetic energy: This could come from AC current on a nearby 
power cable, or RF energy transmitting through the air, and can cause electrical noise that 
either corrupts an analog signal or disrupts digital communications.
- Larger electromagnetic energy: The antenna loop scenario can have a more damaging 
effect when a large current is discharged nearby. The creation of an electromagnetic pulse 
can induce a surge that damages attached electronic devices. 
Another way ground loops affect a system is by allowing ground current to flow between 
devices. This can be either a DC or AC effect. For various reasons, the voltage potential between 
two different points on the surface of the earth is not always 0 V. Therefore, when two electrical 
devices are both connected to a local earth ground, there may exist a voltage difference between 
the two devices. When a cable is connected between the two devices at different voltages, 
physics dictates than an electrical current must flow between the two points through the cable. 
See Figure 15-2 (p. 183).
15. Tips and troubleshooting     182

<!-- Page 200 -->
Figure 15-2. Leakage current (AC or DC) from nearby load
- One effect of this DC ground current-flow is a voltage offset error in analog measurements. 
Errors of this sort are usually not obvious but can have meaningful effects on 
measurements.
- For digital communications, an offset in the ground voltage reduces the dynamic range of 
the digital signals. This makes them more susceptible to noise corruption. If the ground 
voltage changes by one volt or more, the digital communications could stop working 
because the signals no longer reach the thresholds for determining the state of each bit.
- If the ground voltage differences reach several volts, damaging effects may occur at the 
terminals of the electronics devices. Damage occurs when the maximum allowable voltage 
on the internal components is exceeded.
15.8.3 Severing a ground loop
To avoid or eliminate ground loops, when they are detected, requires severing the loop. 
Suggestions for severing ground loops include:
- Connect the shield wire of a signal cable to ground only at one end of the cable. Leave the 
other end floating (not connected to ground).
- Never intentionally use the shield (or drain wire) of a cable as a signal ground or power 
ground.
- Use the mechanical support structure only as a connection for the safety ground (usually 
the ground lug). Do not intentionally return power ground through the structure.
- Do not use shielded Cat5e cables for Ethernet, CPI or EPI communications.
- For long distance communications protocols such as RS-485, RS-422, and CAN, use a 
Resistive Ground (RG) terminal for the ground connection. The RG terminal has a 100-ohm 
resistor in series with ground to limit the amount of DC current that can flow between the 
two endpoints while keeping the common-mode voltage in range of the transceivers. The 
15. Tips and troubleshooting     183

<!-- Page 201 -->
transceivers themselves have enhanced voltage range inputs allowing for ground voltage 
differences of up to 7 V between endpoints.
- For exceptional cases, use optical or galvanic isolation devices to provide a signal 
connection without any accompanying ground connection. These should be used only 
when ground loops are causing system problems and the other methods of breaking a 
ground loop don’t apply. These devices add expense and tend to consume large amounts 
of power.
15.8.4 Soil moisture example
When measuring soil moisture with a resistance block, or water conductivity with a resistance cell, 
the potential exists for a ground loop error.  In the case of an ionic soil matric potential (soil 
moisture) sensor, a ground loop arises because soil and water provide an alternate path for the 
excitation to return to data logger ground.  This example is modeled in the following image:
With Rg in the resistor network, the signal measured from the sensor is described by the 
following equation:
where
- Vx is the excitation voltage
- Rf is a fixed resistor
- Rs is the sensor resistance
- Rg is the resistance between the excited electrode and data logger earth ground.
15. Tips and troubleshooting     184

<!-- Page 202 -->
RsRf/Rg is the source of error due to the ground loop. When Rg is large, the error is negligible.  
Note that the geometry of the electrodes has a great effect on the magnitude of this error.  The 
Delmhorst gypsum block used in the Campbell Scientific 227 probe has two concentric cylindrical 
electrodes.  The center electrode is used for excitation; because it is encircled by the ground 
electrode, the path for a ground loop through the soil is greatly reduced.  Moisture blocks that 
consist of two parallel plate electrodes are particularly susceptible to ground loop problems.  
Similar considerations apply to the geometry of the electrodes in water conductivity sensors.
The ground electrode of the conductivity or soil moisture probe and the data logger earth 
ground form a galvanic cell, with the water/soil solution acting as the electrolyte.  If current is 
allowed to flow, the resulting oxidation or reduction will soon damage the electrode, just as if DC 
excitation was used to make the measurement.  Campbell Scientific resistive soil probes and 
conductivity probes are built with series capacitors to block this DC current.  In addition to 
preventing sensor deterioration, the capacitors block any DC component from affecting the 
measurement.
15.9 Improving voltage measurement quality
The following topics discuss methods of generally improving voltage measurements:
15.9.1 Deciding between single-ended or differential measurements 186
15.9.2 Minimizing ground potential differences 187
15.9.3 Detecting open inputs 188
15.9.4 Minimizing power-related artifacts 188
15.9.5 Filtering to reduce measurement noise 190
15.9.6 Minimizing settling errors 191
15.9.7 Factors affecting accuracy 193
15.9.8 Minimizing offset voltages 195
Read More:  Consult the following technical papers at www.campbellsci.com/app-notes 
  for in-
depth treatments of several topics addressing voltage measurement quality: 
- Preventing and Attacking Measurement Noise Problems 
- Benefits of Input Reversal and Excitation Reversal for Voltage Measurements 
- Voltage Accuracy, Self-Calibration, and Ratiometric Measurements 
15. Tips and troubleshooting     185

<!-- Page 203 -->
15.9.1 Deciding between single-ended or differential 
measurements
Deciding whether a differential or single-ended measurement is appropriate is usually, by far, the 
most important consideration when addressing voltage measurement quality. The decision 
requires trade-offs of accuracy and precision, noise cancellation, measurement speed, available 
measurement hardware, and fiscal constraints.
In broad terms, analog voltage is best measured differentially because these measurements 
include the following noise reduction features that are not included in single-ended 
measurements.
- Passive Noise Rejection
- No voltage reference offset
- Common-mode noise rejection, which filters capacitively coupled noise
- Active Noise Rejection
- Input reversal
- For more information, see Compensating for offset voltage (p. 197).
Reasons for using single-ended measurements, however, include:
- Not enough differential terminals are available. Differential measurements use twice as 
many analog input terminals as do single-ended measurements.
- Rapid sampling is required. Single-ended measurement time is about half that of 
differential measurement time.
- Sensor is not designed for differential measurements. Some Campbell Scientific sensors are 
not designed for differential measurement, but the drawbacks of a single-ended 
measurement are usually mitigated by large programmed excitation and/or  sensor output 
voltages.
Sensors with a high signal-to-noise ratio, such as a relative-humidity sensor with a full-scale 
output of 0 to 1000 mV, can normally be measured as single-ended without a significant 
reduction in accuracy or precision.
Sensors with a low signal-to-noise ratio, such as thermocouples, should normally be measured 
differentially. However, if the measurement to be made does not require high accuracy or 
precision, such as thermocouples measuring brush-fire temperatures, which can exceed 2500 °C, 
a single-ended measurement may be appropriate. If sensors require differential measurement, 
but adequate input terminals are not available, an analog multiplexer should be acquired to 
expand differential input capacity.
15. Tips and troubleshooting     186

<!-- Page 204 -->
Because a single-ended measurement is referenced to data logger ground, any difference in 
ground potential between the sensor and the data logger will result in an error in the 
measurement. For more information on grounds, see Grounds (p. 15) and Minimizing ground 
potential differences (p. 187).
15.9.2 Minimizing ground potential differences
Low-level, single-ended voltage measurements (<200 mV) are sensitive to ground potential 
fluctuation due to changing return currents from  5V,12V, SW12, and C terminals.  The data logger 
grounding scheme is designed to minimize these fluctuations by separating signal grounds (
 ) 
from power grounds (G).  For more information on data logger grounds, see Grounds (p. 15). To 
take advantage of this design, observe the following rules:
- Connect grounds associated with 5V,12V, SW12, and C terminals to G terminals.
- Connect excitation grounds to the nearest  
  terminal on the same terminal block.
- Connect the low side of single-ended sensors to the nearest 
   terminal on the same 
terminal block.
- Connect shield wires to the 
   terminal nearest the terminals to which the sensor signal 
wires are connected.
If offset problems occur because of shield or ground wires with large current flow, tying the 
problem wires into   terminals next to terminals configured for excitation and pulse-count should 
help. Problem wires can also be tied directly to the ground lug to minimize induced single-ended 
offset voltages.
15.9.2.1 Ground potential differences
Because a single-ended measurement is referenced to data logger ground, any difference in 
ground potential between the sensor and the data logger will result in a measurement error.  
Differential measurements MUST be used when the input ground is known to be at a different 
ground potential from data logger ground.
Ground potential differences are a common problem when measuring full-bridge sensors (strain 
gages, pressure transducers, etc), and when measuring thermocouples in soil.
- Soil Temperature Thermocouple: If the measuring junction of a thermocouple is not 
insulated when in soil or water, and the potential of earth ground is, for example, 1 mV 
greater at the sensor than at the point where the data logger is grounded, the measured 
voltage will be 1 mV greater than the thermocouple output.  With a Type T (copper-
constantan) thermocouple, 1 mV equates to approximately 25 °C measurement error.
15. Tips and troubleshooting     187

<!-- Page 205 -->
- External Signal Conditioner: External instruments with integrated signal conditioners, such 
as an infrared gas analyzer (IRGA), are frequently used to make measurements and send 
analog information to the data logger.  These instruments are often powered by the same 
VAC-line source as the data logger.  Despite being tied to the same ground, differences in 
current drain and wire resistance result in different ground potentials at the two 
instruments.  For this reason, a differential measurement should be made on the analog 
output from the external signal conditioner.
For additional information, see Minimizing offset voltages (p. 195).
15.9.3 Detecting open inputs
A useful option available to single-ended and differential measurements is the detection of open 
inputs due to a broken or disconnected sensor wire.  This prevents otherwise undetectable 
measurement errors.  Range codes appended with C enable open-input detection. For detailed 
information, see the CRBasic help (VoltSE() and VoltDiff() instructions, Range 
parameter)
The C option may not detect an open circuit in the following situations: 
- When the input is not a truly open circuit, such as might occur on a wet cut cable end, the 
open circuit may not be detected because the input capacitor discharges to a normal 
voltage through external leakage to ground within the settling time of the measurement. 
This problem is worse when a long settling time is selected, as more time is given for the 
input capacitors to discharge to a "normal" level.
- If the open circuit is at the end of a very long cable, the test pulse may not charge the cable 
(with its high capacitance) up to a voltage that generates NAN or a distinct error voltage. 
The cable may even act as an aerial and inject noise which also might not read as an error 
voltage.
- The sensor may "object" to the test pulse being connected to its output, even for 100 µs.  
There is little or no risk of damage, but the sensor output may be caused to temporarily 
oscillate.  Programming a longer settling time in the CRBasic measurement instruction to 
allow oscillations to decay before the ADC may mitigate the problem.
15.9.4 Minimizing power-related artifacts
Some VAC-to-VDC power converters produce switching noise or AC ripple as an artifact of the 
ac-to-dc rectification process. Excessive switching noise on the output side of a power supply can 
increase measurement noise, and so increase measurement error. Noise from grid or mains 
15. Tips and troubleshooting     188

<!-- Page 206 -->
power also may be transmitted through the transformer, or induced electromagnetically from 
nearby motors, heaters, or power lines.
High-quality power regulators typically reduce noise due to power regulation. Using the 50 Hz or 
60 Hz first notch frequency (fN1) option for CRBasic analog input measurement instructions  
often improves rejection of noise sourced from power mains. The CRBasic standard deviation 
output instruction, StdDev(), can be used to evaluate measurement noise.
The data logger includes adjustable digital filtering, which serves two purposes:
- Arrive as close as possible to the true input signal
- Filter out measurement noise at specific frequencies, the most common being noise at 50 
Hz or 60 Hz, which originate from mains-power lines.
Filtering time is inversely proportional to the frequency being filtered.
15.9.4.1 Minimizing electronic noise
Electronic noise can cause significant error in a voltage measurement, especially when measuring 
voltages less than 200 mV.  So long as input limitations are observed, the PGA ignores voltages, 
including noise, that are common to each side of a differential-input pair.  This is the common-
mode voltage.  Ignoring (rejecting or canceling) the common-mode voltage is an essential 
feature of the differential input configuration that improves voltage measurements. The 
following image illustrates the common-mode component (Vcm) and the differential-mode 
component (Vdm) of a voltage signal.  Vcm is the average of the voltages on the V+ and V– inputs.  
So, Vcm = (V+ + V–)/2 or the voltage remaining on the inputs when Vdm = 0.  The total voltage on 
the V+ and V– inputs is given as VH = Vcm + Vdm/2, and VL = Vcm – Vdm/2, respectively.
15. Tips and troubleshooting     189

<!-- Page 207 -->
15.9.5 Filtering to reduce measurement noise
An adjustable filter is applied to analog measurements, reducing signal components at selected 
frequencies. The following figure shows the filter frequency response. Using the first notch 
frequency (fN1) parameter, users can select the placement of the filter notches. The first notch 
falls at the specified fN1, and subsequent notches fall at integer multiples of fN1. Commonly, 
fN1 is set at 50 or 60 Hz to filter 50 or 60 Hz signal components, reducing noise from ac power 
mains.
Filtering comes at the expense of measurement time. The time required for filtering is equal to 
1/fN1. For example, setting fN1 equal to 50 will require 1/50 sec (20 ms) for filtering. As fN1 is 
set to smaller values, random noise in the measurement results decreases, while measurement 
time increases. The total time required for a single result includes settling + filtering + overhead. 
Consult the following technical paper at www.campbellsci.com/app-notes 
  for in-depth 
treatment of measurement noise: Preventing and Attacking Measurement Noise Problems 
 .
15.9.5.1 CR1000X/CR1000Xe filtering details
The data logger utilizes a sigma-delta ADC that outputs digitized data at a rate of 31250 samples 
per second. User-specified filtering is achieved by averaging several samples from the ADC. 
Recall that averaging the signal over a period of 1/fN1 seconds will filter signal components at fN1 
15. Tips and troubleshooting     190

<!-- Page 208 -->
Hz. The final result, then, is the average calculated from 31250/fN1 samples. For example, if fN1 
is set to 50 Hz, 625 samples (31250 / 50) are averaged to generate the final filtered result.
The actual fN1 may deviate from the user-specified setting since a whole integer number of 
samples must be averaged. For example, if fN1 is set to 60 Hz, 521 samples (31250 / 60 = 
520.83) will be averaged to produce the filtered result. The rounding of 520.83 to 521 moves the 
actual fN1 to 31250 / 521 = 59.98 Hz.
15.9.6 Minimizing settling errors
Settling time allows an analog voltage signal to rise or fall closer to its true magnitude prior to 
measurement. Default settling times, those resulting when the SettlingTime parameter is set 
to 0, provide sufficient settling in most cases. Additional settling time is often programmed when 
measuring high-resistance (high-impedance) sensors, or when sensors connect to the input 
terminals by long cables. The time to complete a measurement increases with increasing settling 
time. For example, a 1 ms increase in settling time for a bridge instruction with input reversal and 
excitation reversal results in a 4 ms increase in time to perform the instruction.
When sensors require long cable lengths, use the following general practices to minimize settling 
errors:
- Do not use leads with PVC-insulated conductors. PVC has a high dielectric constant, which 
extends input settling time.
- Where possible, run excitation leads and signal leads in separate shields to minimize 
transients.
- When measurement speed is not a prime consideration, additional time can be used to 
ensure ample settling time.
- In difficult cases where measurement speed is a consideration, an appropriate settling time 
can be determined through testing.
15.9.6.1 Measuring settling time
Settling time for a particular sensor and cable can be measured with the CR1000X/CR1000Xe.  
Programming a series of measurements with increasing settling times will yield data that indicate 
at what settling time a further increase results in negligible change in the measured voltage.  The 
programmed settling time at this point indicates the settling time needed for the sensor / cable 
combination.
The following CRBasic Example: Measuring Settling Time presents CRBasic code to help 
determine settling time for a pressure transducer using a high-capacitance semiconductor.  The 
code consists of a series of full-bridge measurements () with increasing settling times. The 
15. Tips and troubleshooting     191

<!-- Page 209 -->
pressure transducer is placed in steady-state conditions so changes in measured voltage are 
attributable to settling time rather than changes in pressure. 
CRBasic Example 3: Measuring settling time
'This program example demonstrates the measurement of settling time
'using a single measurement instruction multiple times in succession.
Public PT(20) 'Variable to hold the measurements
DataTable(Settle,True,100)
Sample(20,PT(),IEEE4)
EndTable
BeginProg
Scan(1,Sec,3,0)
BrFull(PT(1), 1,mV200,1,Vx1,1,2500,True,True, 100,15000,1.0,0)
BrFull(PT(2), 1,mV200,1,Vx1,1,2500,True,True, 200,15000,1.0,0)
BrFull(PT(3), 1,mV200,1,Vx1,1,2500,True,True, 300,15000,1.0,0)
BrFull(PT(4), 1,mV200,1,Vx1,1,2500,True,True, 400,15000,1.0,0)
BrFull(PT(5), 1,mV200,1,Vx1,1,2500,True,True, 500,15000,1.0,0)
BrFull(PT(6), 1,mV200,1,Vx1,1,2500,True,True, 600,15000,1.0,0)
BrFull(PT(7), 1,mV200,1,Vx1,1,2500,True,True, 700,15000,1.0,0)
BrFull(PT(8), 1,mV200,1,Vx1,1,2500,True,True, 800,15000,1.0,0)
BrFull(PT(9), 1,mV200,1,Vx1,1,2500,True,True, 900,15000,1.0,0)
BrFull(PT(10),1,mV200,1,Vx1,1,2500,True,True,1000,15000,1.0,0)
BrFull(PT(11),1,mV200,1,Vx1,1,2500,True,True,1100,15000,1.0,0)
BrFull(PT(12),1,mV200,1,Vx1,1,2500,True,True,1200,15000,1.0,0)
BrFull(PT(13),1,mV200,1,Vx1,1,2500,True,True,1300,15000,1.0,0)
BrFull(PT(14),1,mV200,1,Vx1,1,2500,True,True,1400,15000,1.0,0)
BrFull(PT(15),1,mV200,1,Vx1,1,2500,True,True,1500,15000,1.0,0)
BrFull(PT(16),1,mV200,1,Vx1,1,2500,True,True,1600,15000,1.0,0)
BrFull(PT(17),1,mV200,1,Vx1,1,2500,True,True,1700,15000,1.0,0)
BrFull(PT(18),1,mV200,1,Vx1,1,2500,True,True,1800,15000,1.0,0)
BrFull(PT(19),1,mV200,1,Vx1,1,2500,True,True,1900,15000,1.0,0)
BrFull(PT(20),1,mV200,1,Vx1,1,2500,True,True,2000,15000,1.0,0)
CallTable Settle
NextScan
EndProg
The first six measurements are shown in the following table: 
Table 15-3: Example data from Measuring settling time program
Timestamp
Record 
number
PT(1)
Smp
PT(2)
Smp
PT(3)
Smp
PT(4)
Smp
PT(5)
Smp
PT(6)
Smp
8/3/2017 23:34 0 0.03638599 0.03901386 0.04022673 0.04042887 0.04103531 0.04123745
8/3/2017 23:34 1 0.03658813 0.03921601 0.04002459 0.04042887 0.04103531 0.0414396
8/3/2017 23:34 2 0.03638599 0.03941815 0.04002459 0.04063102 0.04042887 0.04123745
15. Tips and troubleshooting     192

<!-- Page 210 -->
Table 15-3: Example data from Measuring settling time program
Timestamp
Record 
number
PT(1)
Smp
PT(2)
Smp
PT(3)
Smp
PT(4)
Smp
PT(5)
Smp
PT(6)
Smp
8/3/2017 23:34 3 0.03658813 0.03941815 0.03982244 0.04042887 0.04103531 0.04103531
8/3/2017 23:34 4 0.03679027 0.03921601 0.04022673 0.04063102 0.04063102 0.04083316
Each trace in the following image contains all twenty PT() mV/V values (left axis) for a given 
record number and an average value showing the measurements as percent of final reading 
(right axis).  The reading has settled to 99.5% of the final value by the fourteenth measurement, 
which is contained in variable PT(14).  This is suitable accuracy for the application, so a settling 
time of 1400 µs is determined to be adequate.
15.9.7 Factors affecting accuracy
Accuracy describes the difference between a measurement and the true value.  Many factors 
affect accuracy.  This topic discusses the effect percent-of-reading, offset, and resolution have on 
the accuracy  of an analog voltage measurement.  Accuracy is defined as follows:
accuracy = percent-of-reading + offset
15. Tips and troubleshooting     193

<!-- Page 211 -->
where percents-of-reading and offsets are displayed in the Analog measurement specifications 
(p. 256).
NOTE:
Error discussed in this section and error-related specifications of the data logger do not 
include error introduced by the sensor, or by the transmission of the sensor signal to the data 
logger.
15.9.7.1 Measurement accuracy example
The following example illustrates the effect percent-of-reading and offset have on measurement 
accuracy. The effect of offset is usually negligible on large signals.
Example:
- Sensor-signal voltage: approximately 1050 mV
- CRBasic measurement instruction: VoltDiff()
- Programmed input-voltage range (Range) : mV 5000 (±5000 mV)
- Input measurement reversal (RevDiff):  True
- Data logger circuitry temperature: 10° C
Accuracy of the measurement is calculated as follows:
accuracy = percent-of-reading + offset
where
percent-of-reading = 1050 mV • ±0.04%
=±0.42 mV
and
offset = 0.5 µV
Therefore,
accuracy = ±(0.42 mV + 0.5 µV) = ±0.4205 mV
15. Tips and troubleshooting     194

<!-- Page 212 -->
15.9.8 Minimizing offset voltages
Voltage offset can be the source of significant error.  For example, an offset of 3 μV on a 2500 mV 
signal causes an error of only 0.00012%, but the same offset on a 0.25 mV signal causes an error 
of 1.2%. Measurement offset voltages are unavoidable, but can be minimized. Offset voltages 
originate with:
- Ground currents. See Minimizing ground potential differences (p. 187).
- Seebeck effect
- Residual voltage from a previous measurement
Remedies include:
- Connecting power grounds to power ground terminals (G).
- Using input reversal (RevDiff = True) with differential measurements.
- Automatic offset compensation for differential measurements when RevDiff = False.
- Automatic offset compensation for single-ended measurements when MeasOff = 
False.
- Using MeasOff = True for better offset compensation.
- Using excitation reversal (RevEx = True) with bridge measurements.
- Programming longer settling times.
Single-ended measurements are susceptible to voltage drop at the ground terminal caused by 
return currents from another device that is powered from the data logger wiring panel, such as 
another manufacturer's communications modem, or a sensor that requires a lot of power.  
Currents greater than 5 mA are usually undesirable.  The error can be avoided by routing power 
grounds from these other devices to a power ground G terminal, rather than using a signal 
ground (
  ) terminal.  Ground currents can be caused by the excitation of resistive-bridge 
sensors, but these do not usually cause offset error.  These currents typically only flow when a 
voltage excitation is applied.  Return currents associated with voltage excitation cannot influence 
other single-ended measurements because the excitation is usually turned off before the data 
logger moves to the next measurement.  However, if the CRBasic program is written in such a way 
that an excitation terminal is enabled during an unrelated measurement of a small voltage, an 
offset error may occur.
The Seebeck effect results in small thermally induced voltages across junctions of dissimilar 
metals as are common in electronic devices.  Differential measurements are more immune to 
these than are single-ended measurements because of passive voltage cancellation occurring 
between matched high and low pairs such as 1H/1L.  So, use differential measurements when 
15. Tips and troubleshooting     195

<!-- Page 213 -->
measuring critical low-level voltages, especially those below 200 mV, such as are output from 
pyranometers and thermocouples.
When analog voltage signals are measured in series by a single measurement instruction, such as 
occurs when VoltSE() is programmed with Reps = 2 or more, measurements on 
subsequent terminals may be affected by an offset, the magnitude of which is a function of the 
voltage from the previous measurement. While this offset is usually small and negligible when 
measuring large signals, significant error, or NAN, can occur when measuring very small signals. 
This effect is caused by dielectric absorption of the integrator capacitor and cannot be overcome 
by circuit design. Remedies include the following:
- Programing longer settling times.
- Using an individual instruction for each input terminal, the effect of which is to reset the 
integrator circuit prior to filtering.
- Avoiding preceding a very small voltage input with a very large voltage input in a 
measurement sequence if a single measurement instruction must be used.
The following  table lists some of the tools available to minimize the effects of offset voltages:
Table 15-4: Offset voltage compensation options
CRBasic 
measurement 
instruction
Input reversal
(RevDiff=True)
Excitation reversal 
(RevEx=True)
Measure offset 
during measurement
(MeasOff=True)
Measure offset 
during background 
calibration
(RevDiff=False)
(RevEx=False)
(MeasOff=False)
BrHalf()   ü   ü
BrHalf3W()   ü   ü
BrHalf4W() ü ü   ü
BrFull() ü ü   ü
BrFull6W() ü ü   ü
TCDiff() ü     ü
TCSe()     ü ü
VoltDiff() ü     ü
VoltSe()     ü ü
15. Tips and troubleshooting     196

<!-- Page 214 -->
15.9.8.1 Compensating for offset voltage
Differential measurements also have the advantage of an input reversal option, RevDiff.  When 
RevDiff is True, two differential measurements are made, the first with a positive polarity and 
the second reversed.  Subtraction of opposite polarity measurements cancels some offset 
voltages associated with the measurement.
Ratiometric measurements use an excitation voltage to excite the sensor during the 
measurement process.  Reversing excitation polarity also reduces offset voltage error.  Setting the 
RevEx parameter to True programs the measurement for excitation reversal.  Excitation reversal 
results in a polarity change of the measured voltage so that two measurements with opposite 
polarity can be subtracted and divided by 2 for offset reduction similar to input reversal for 
differential measurements.  
For example, if 3 µV offset exists in the measurement circuitry, a 5 mV signal is measured as 5.003 
mV.  When the input or excitation is reversed, the second sub-measurement is –4.997 mV.  
Subtracting the second sub-measurement from the first and then dividing by 2 cancels the offset:
5.003 mV – (–4.997 mV) = 10.000 mV
10.000 mV / 2 = 5.000 mV
Ratiometric differential measurement instructions allow both RevDiff and RevEx to be set 
True. This results in four measurement sequences, which the data logger processes into the 
reported measurement:
- positive excitation polarity with positive differential input polarity
- negative excitation polarity with positive differential input polarity
- positive excitation polarity with negative differential input polarity
- negative excitation polarity with negative differential input polarity
For ratiometric single-ended measurements, such as a BrHalf(), setting RevEx = True 
results in two measurements of opposite excitation polarity that are subtracted and divided by 2 
for offset voltage reduction.  For RevEx = False for ratiometric single-ended measurements, 
an offset-voltage measurement is determined from self-calibration.
When the data logger reverses differential inputs or excitation polarity, it delays the same settling 
time after the reversal as it does before the first sub-measurement. So, there are two delays per 
measurement when either RevDiff or RevEx is used.  If both RevDiff and RevEx are True, 
four sub-measurements are performed; positive and negative excitations with the inputs one way 
and positive and negative excitations with the inputs reversed.  The automatic procedure then is 
as follows:
 1.  Switch to the measurement terminals.
 2.  Set the excitation, settle, and then measure.
15. Tips and troubleshooting     197

<!-- Page 215 -->
 3.  Reverse the excitation,  settle, and then measure.
 4.  Reverse the excitation, reverse the input terminals, settle, measure.
 5.  Reverse the excitation, settle, measure.
There are four delays per measurement.  In cases of excitation reversal, excitation time for each 
polarity is exactly the same to ensure that ionic sensors do not polarize with repetitive 
measurements.
Read More: The Benefits of Input Reversal and Excitation Reversal for Voltage Measurements 
 .
15.9.8.2 Measuring ground reference offset voltage
Single-ended and differential measurements without input reversal use an offset voltage 
measurement with the PGIA inputs grounded. This offset voltage is subtracted from the 
subsequent measurement. For differential measurements without input reversal, this offset 
voltage measurement is performed as part of the routine background calibration of the data 
logger. See About background calibration (p. 153).  Single-ended measurement instructions 
VoltSE() and TCSe() include the MeasOff parameter determines whether the offset 
voltage measured is done at the beginning of the measurement instruction, or as part of self-
calibration. This option provides you with the opportunity to weigh measurement speed against 
measurement accuracy.  When MeasOff = True, a measurement of the single-ended offset 
voltage is made at the beginning of the VoltSE() or TCSe() instruction. When MeasOff = 
False, measurements will be corrected for the offset voltage determined during self-calibration. 
For installations experiencing fluctuating offset voltages, choosing MeasOff = True for the 
VoltSE() or TCSe() instruction results in better offset voltage performance.
If RevDiff, RevEx, or MeasOff is disabled ( = False), offset voltage compensation is  
automatically performed, albeit less effectively, by using measurements from the background 
calibration.  Disabling RevDiff, RevEx, or MeasOff speeds up measurement time; however, 
the increase in speed comes at the cost of accuracy because of the following:
- RevDiff, RevEx, and MeasOff are more effective.
- Background calibrations are performed only periodically, so more time skew occurs 
between the background calibration offsets and the measurements to which they are 
applied.
NOTE:
When measurement duration must be minimal to maximize measurement frequency, 
consider disabling RevDiff, RevEx, and MeasOff when data logger temperatures and 
return currents are slow to change.
15. Tips and troubleshooting     198

<!-- Page 216 -->
15.10 Field calibration
Calibration increases accuracy of a measurement device by adjusting its output, or the 
measurement of its output, to match independently verified quantities. Adjusting sensor output 
directly is preferred, but not always possible or practical.  By adding the FieldCal() or 
FieldCalStrain() instruction to a CRBasic program, measurements of a linear sensor can be 
adjusted by modifying the programmed multiplier and offset applied to the measurement, 
without modifying or recompiling the CRBasic program. See the CRBasic Editor help for detailed 
instruction information and program examples: https://help.campbellsci.com/crbasic/cr1000x/ 
.
15.11 File system error codes
Errors can occur when attempting to access files on any of the available drives. All occurrences 
are rare, but they are most likely to occur when using optional memory cards. Often, formatting 
the drive will resolve the error. The errors display in the File Control messages box or in the 
CardStatus field of the Status table. See Information tables and settings (advanced) (p. 202) for 
more information.
 1 Invalid format
 2 Device capabilities error
 3 Unable to allocate memory for file operation
 4 Max number of available files exceeded
 5 No file entry exists in directory
 6 Disk change occurred
 7 Part of the path (subdirectory) was not found
 8 File at EOF
 9 Bad cluster encountered
 10 No file buffer available
 11 Filename too long or has bad chars
 12 File in path is not a directory
 13 Access permission, opening DIR or LABEL as file, or trying to open file as DIR or mkdir existing 
file
 14 Opening read-only file for write
 15 Disk full (can't allocate new cluster)
 16 Root directory is full
 17 Bad file ptr (pointer) or device not initialized
 18 Device does not support this operation
 19 Bad function argument supplied
 20 Seek out-of-file bounds
 21 Trying to mkdir an existing dir
15. Tips and troubleshooting     199

<!-- Page 217 -->
 22 Bad partition sector signature
 23 Unexpected system ID byte in partition entry
 24 Path already open
 25 Access to uninitialized ram drive
 26 Attempted rename across devices
 27 Subdirectory is not empty
 31 Attempted write to Write Protected disk
 32 No response from drive (Door possibly open)
 33 Address mark or sector not found
 34 Bad sector encountered
 35 DMA memory boundary crossing error
 36 Miscellaneous I/O error
 37 Pipe size of 0 requested
 38 Memory-release error (relmem)
 39 FAT sectors unreadable (all copies)
 40 Bad BPB sector
 41 Time-out waiting for filesystem available
 42 Controller failure error
 43 Pathname exceeds _MAX_PATHNAME
15.12 File name and resource errors
The maximum file name size that can be stored, run as a program, or FTP transferred in the data 
logger is 59 characters. If the name + file extension is longer than 59 characters, an  Invalid 
Filename error is displayed. If several files are stored, each with a long file name, memory 
allocated to the root directory can be exceeded before the actual memory of storing files is 
exceeded. When this occurs, an  Insufficient resources or memory full error is displayed.
15.13 Background calibration errors
Background calibration errors are rare.  When they do occur, the cause is usually an analog input 
that exceeds the input limits of the data logger.
- Check all analog inputs to make sure they are not greater than ±5 VDC by measuring the 
voltage between the input and a G terminal.  Do this with a multimeter.
- Check for condensation, which can sometimes cause leakage from a 12 VDC source 
terminal.
- Check for a lose ground wire on a sensor powered from a 12V or SW12 terminal.
15. Tips and troubleshooting     200

<!-- Page 218 -->
- If a multimeter is not available, disconnect sensors, one at a time, that require power from 9 
to 16 VDC.  If measurements return to normal, you have found the cause.
15. Tips and troubleshooting     201
