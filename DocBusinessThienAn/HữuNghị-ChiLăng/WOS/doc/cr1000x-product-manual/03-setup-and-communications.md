---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 40-61
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 6: Thiết lập cấu hình và kết nối truyền thông ban đầu

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 40 đến 61).  
> **Chủ đề chính**: Thiết lập truyền thông USB, RS-232, Ethernet, RNDIS (Virtual Ethernet qua USB), cấu hình qua Web Interface, kiểm tra bằng EZSetup và nạp chương trình Short Cut.

---


<!-- Page 40 -->
6. Setting up the 
CR1000X/CR1000Xe
The basic steps for setting up your data logger to take measurements and store data are 
included in the following sections:
6.1 Setting up communications with the data logger 23
6.2 Testing communications with EZSetup 38
6.3 Making the software connection 39
6.4 Creating a Short Cut data logger program 40
6.5 Sending a program to the data logger 43
6.1 Setting up communications with the data 
logger
The first step in setting up and communicating with your data logger is to configure your 
connection. Communications peripherals, data loggers, and software must all be configured for 
communications.
You can configure your connection using any of the following options. The simplest is via USB. 
For detailed instruction, see:
6.1.1 USB or RS-232 communications 24
6.1.2 Virtual Ethernet over USB (RNDIS) 26
6.1.3 Ethernet communications option 35
For other configurations, see the LoggerNet EZSetup Wizard help. Context-specific help is given 
in each step of the wizard by clicking the Help button in the bottom right corner of the window. 
For complex data logger networks, use Network Planner. For more information on using the 
Network Planner, watch a video at https://www.campbellsci.com/videos/loggernet-software-
network-planner 
 .
 Additional information is found in your specific peripheral manual, and the data logger support 
software manual and help. See also: 
6. Setting up the CR1000X/CR1000Xe     23

<!-- Page 41 -->
- www.campbellsci.com/cellular-communications 
  for links to CELL200-series, RV50X, 
cellular data services, Konect PakBus router,  and other cellular products
- www.campbellsci.com/wi-fi-communications 
  for links to the NL241, and other Wi-Fi 
products
- www.campbellsci.com/spread-spectrum-radios 
  for links to the RF452 and RF407-series 
spread spectrum radios
- www.campbellsci.com/satellite-communications 
  for links to the TX325, TX326, 
HUGHES9502, and other satellite products
- www.campbellsci.com/loggernet 
- www.campbellsci.com/pc400 
Manuals for retired products are found at: www.campbellsci.com/manuals 
 . These include, but 
are not limited to: RF401, RV50, TX321, TX320, and TX312.
6.1.1 USB or RS-232 communications
Setting up a USB or RS-232 connection is a good way to begin communicating with your data 
logger. Because these connections do not require configuration (like an IP address), you need 
only set up the communications between your computer and the data logger. Use the following 
instructions or watch the Quickstart videos at https://www.campbellsci.com/videos 
 .
TIP:
You will physically connect your data logger to your computer in step 6. 
Follow these steps to get started. These settings can be revisited using the data logger support 
software Edit Datalogger Setup option 
 .
 1. Using data logger support software, launch the EZSetup Wizard.
NOTE:
New software installations automatically open the EZSetup Wizard the first time they 
run.
- LoggerNet users, click Setup
 , select the View menu and ensure you are in the EZ 
(Simplified) view, then click Add Datalogger
 . 
- PC400 users, click Add Datalogger
 .
 2. Click Next.
 3. Select your data logger from the list. In the Datalogger Name field, type a meaningful 
name for your data logger (for example, a site identifier or project name), and click Next.
 4. Select the Direct Connect connection type and click Next.
6. Setting up the CR1000X/CR1000Xe     24

<!-- Page 42 -->
 5. If this is the first time connecting this computer to a CR1000X/CR1000Xe via USB, click Install 
USB Driver, select your data logger, click Install, and follow the prompts to install the 
USB driver.             
 6. Plug the data logger into your computer using a USB or RS-232 cable. The USB connection 
supplies 5 V power as well as a communications link, which is adequate for setup. A 12 V 
battery will be needed for field deployment. If using RS-232, external power must be 
provided to the data logger, and a CPI/RS-232 RJ45 to DB9 cable is required to connect to 
the computer.
NOTE:
The Power LED on the data logger indicates the program and power states. Because 
the data logger ships with a program set to run on power-up, the Power LED flashes 
three times every 10 seconds when powered over USB. When powered with a 12 V 
battery, it flashes once every 10 seconds. When no program is running, the LED is 
always on.
 7. From the COM Port list, select the COM port used for your data logger. It will appear as 
CR1000X/CR1000Xe (COM number). 
 8. USB and RS-232 connections do not typically require a COM Port Communication Delay; 
this type of delay allows time for hardware devices to "wake up" and negotiate a 
communications link. Accept the default value of 00 seconds and click Next.
 9. You must match the baud rate and PakBus address hardware settings of your data logger. 
A USB connection does not require a baud rate selection, keep the default. RS-232 
connections default to 115200 baud.  The default PakBus address is 1.
 10. Set an Extra Response Time if you have a difficult or marginal connection and you want the 
data logger support software to wait a certain amount of time before returning a 
communications failure error. Accept the default value of 00 seconds.
 11. Set a Max Time On-Line to limit the amount of time the data logger remains connected. 
When the data logger is connected, communications with it are terminated when this time 
limit is exceeded. A value of 0 in this field indicates that there is no time limit for 
maintaining a connection to the data logger.
 12. Leave the Neighbor PakBus Address as the default of 0. 
 13. Click Next.
 14. By default, the data logger does not use a security code. Therefore, the Security Code can 
be left at 0. If the code has been changed in the data logger, enter the new code.
The PakBus Encryption Key can be left blank for direct USB connections. 
6. Setting up the CR1000X/CR1000Xe     25

<!-- Page 43 -->
 15. Click Next.
 16. Review the Setup Summary. If you need to make changes, click Previous to return to a 
previous window and change the settings.
 17. Setup is now complete. The EZSetup Wizard allows you to Finish, or you may click Next to 
test communications, set the data logger clock, and send a program to the data logger. See 
Testing communications with EZSetup (p. 38) for more information.
6.1.2 Virtual Ethernet over USB (RNDIS)
The data logger supports RNDIS (virtual Ethernet over USB). This allows the data logger to 
communicate via TCP/IP over USB. Watch a video at 
https://www.campbellsci.com/videos/ethernet-over-usb 
  or use the following instructions.
 1. Supply power to the data logger. If connecting via USB for the first time, you must first 
install USB drivers by using Device Configuration Utility (select your data logger, then on 
the main page, click Install USB Driver). Alternatively, you can install the USB drivers using 
EZ Setup. A USB connection supplies 5 V power (as well as a communications link), which is 
adequate for setup, but a 12 V battery will be needed for field deployment.  
NOTE:
Ensure the data logger is connected directly to the computer USB port (not to a 
USB hub). We recommended always using the same USB port on your computer. 
 2. Physically connect your data logger to your computer using a USB cable, then in Device 
Configuration Utility select your data logger.
 3. By default, the RNDIS address is 192.168.66. You can see the address by selecting IP as the 
Connection Type.
If you wish to change the RNDIS address, click the Settings Editor tab >Advanced sub-tab 
> USB Virtual Ethernet Address (RNDIS) and enter the desired address. Note that 0 for the 
address defaults to 192.168.66.1.
6. Setting up the CR1000X/CR1000Xe     26

<!-- Page 44 -->
 4. The Virtual Ethernet (RNDIS) address can be used to connect to the data logger using 
Device Configuration Utility or other computer software, or to view the data logger internal 
web page in a browser. To view the web page, open a browser and enter the IP address  (for 
example, 192.168.66.1) into the address bar. For more information, see Web interface (p. 
27).
To secure your data logger from others who have access to your network, we recommend that 
you set security. For more information, see Data logger security (p. 45).
NOTE:
Ethernet over USB (RNDIS) is considered a direct communications connection. Therefore, it is 
a trusted connection and Administrator privileges are automatically granted for all 
functionality.
6.1.2.1 Web interface
For data loggers with an IP or RNDIS (virtual Ethernet over USB) connection, the built-in web 
interface provides access to real-time and stored data logger data, status and setting 
information, files, and more. 
For more information on the web interface, watch an instructional video 
at: https://www.campbellsci.com/videos/web-interface 
 .
6. Setting up the CR1000X/CR1000Xe     27

<!-- Page 45 -->
Using an RNDIS connection
The CR1000X/CR1000Xe supports RNDIS. This allows the data logger to communicate via TCP/IP 
over USB. 
Use the following steps. 
 1. Supply power to the data logger. A USB connection supplies 5 V power (as well as a 
communications link), which is adequate for setup, but a 12 V battery will be needed for 
field deployment. 
NOTE:
Ensure the data logger is connected directly to the computer USB port (not to a 
USB hub). We recommend always using the same USB port on your computer each 
time you connect. 
6. Setting up the CR1000X/CR1000Xe     28

<!-- Page 46 -->
 2. Ensure that the USB drivers have been installed on your computer. If you have connected 
to a CR1000X/CR1000Xe with this computer, the drivers have been installed. If this is the 
first time using this computer, see USB or RS-232 communications (p. 24). 
 3. The default RNDIS address is 192.168.66.1. Type this address into a web browser to access 
the web interface. 
Using an IP connection
To use a standard IP connection (not RNDIS) to access the web interface, HTTP or HTTPS access 
must be enabled. By default, HTTP is enabled and HTTPS is disabled. Additionally, anonymous 
HTTP access is disabled by default. The default HTTP login credentials are admin for the 
username and the data logger UID as the admin password (if the data logger has a UID). 
There are two ways to edit or set permissions for access. 
6. Setting up the CR1000X/CR1000Xe     29

<!-- Page 47 -->
Through Device Configuration Utility (option 1)
 1. If you do not know the data logger IP address, retrieve it through Device Configuration 
Utility. On the bottom, left side of the screen, select IP as the Connection Type, then click 
the browse button next to the Server Address box.  If you have multiple data loggers in your 
network, more than one data logger may be returned. Ensure you select the correct data 
logger by verifying the data logger serial number or station name (if  assigned).
 2. Enter the PakBus Encryption Key. For data loggers that have a UID, PakBus Encryption is 
enabled by default. The default PakBus Encryption Key is the UID.
 3. To edit or set additional permissions to access the web interface, use Device Configuration 
Utility > Networks Services > Edit Accounts (in older versions this button is labeled Edit 
.csipasswd File). To add permissions, click Add User. Multiple user accounts with differing 
levels of access can be defined for one data logger.
6. Setting up the CR1000X/CR1000Xe     30

<!-- Page 48 -->
Four levels of access are available:
- Anonymous: Read-only access, disabled by default. This account cannot be removed, 
but privileges can be disabled. 
- Read Only: Data collection is unrestricted. Clock and writable variables cannot be 
changed. Programs cannot be viewed, stopped, deleted, or retrieved.
- Read/Write: Data collection is unrestricted. Clock and writable variables can be 
changed. Programs cannot be viewed, stopped, deleted, or retrieved.
- All (Administrator): Data collection is unrestricted. Clock, writable variables, and 
settings can be changed. Programs can be viewed, stopped, deleted, and retrieved. 
Hidden tables can be viewed. Files, including programs can be sent to the data 
logger.
6. Setting up the CR1000X/CR1000Xe     31

<!-- Page 49 -->
 4. Assign an Access Level and Password. 
 5. Type the data logger IP address into a web browser. If prompted, enter the user name and 
password that you just configured through Edit Accounts. 
Through the Web Interface (option 2)
 1. If you do not know the data logger IP address, retrieve it through Device Configuration 
Utility. See the previous step 1.
 2. Once you have the data logger IP address, type it into a web browser.
6. Setting up the CR1000X/CR1000Xe     32

<!-- Page 50 -->
 3. When prompted, enter the user name admin and the UID as the password. You may have 
to enter credentials twice, once for the browser and once for the CR1000X/CR1000Xe. Click 
Log In.
 4. To edit or set additional permissions to access the web interface, select Manage 
Accounts. This is also accessible through the 
  icon. 
6. Setting up the CR1000X/CR1000Xe     33

<!-- Page 51 -->
 5. To add permissions, click Create a New Account. Multiple user accounts with differing 
permission levels can be defined for one data logger. Enter a Username, Password, and 
Permission Level. Click Save Account.
Four levels of access are available: 
- Anonymous: Read-only access, disabled by default. This account cannot be removed, 
but privileges can be disabled. 
- Read Only: Data collection is unrestricted. Clock and writable variables cannot be 
changed. Programs cannot be viewed, stopped, deleted, or retrieved.
- Read/Write: Data collection is unrestricted. Clock and writable variables can be 
changed. Programs cannot be viewed, stopped, deleted, or retrieved.
- All (Administrator): Data collection is unrestricted. Clock, writable variables, and 
settings can be changed. Programs can be viewed, stopped, deleted, and retrieved. 
Hidden tables can be viewed. Files, including programs can be sent to the data 
logger.
Web interface recovery
In the unlikely event that  web interface files are lost or corrupted, a data logger-hosted recovery 
page can be used to restore the web interface. This page can also be used to update the web 
interface when new versions are available. The recovery page is accessed by navigating to: 
datalogger IP Address/recovery. For example: 111.222.333.444/recovery. This will open a 
Recovery page in the browser.
6. Setting up the CR1000X/CR1000Xe     34

<!-- Page 52 -->
The Send File button on the recovery web page allows users to upload .obj or web.obj.gz files to 
restore web interface files on the data logger. The upload process may take several minutes. 
6.1.3 Ethernet communications option
The CR1000X/CR1000Xe offers a 10/100 Ethernet connection. Use Device Configuration Utility to 
enter the data logger IP Address, Subnet Mask, and IP Gateway address. After this, use the 
EZSetup Wizard to set up communications with the data logger. If you already have the data 
logger IP information, you can skip these steps and go directly to Setting up Ethernet 
communications between the data logger and computer (p. 37). Watch a video at 
https://www.campbellsci.com/videos/datalogger-ethernet-configuration 
  or use the following 
instructions.
6.1.3.1 Configuring data logger Ethernet settings
 1. Supply power to the data logger. If connecting via USB for the first time, you must first 
install USB drivers by using Device Configuration Utility (select your data logger, then on 
the main page, click Install USB Driver). Alternatively, you can install the USB drivers using 
EZ Setup. A USB connection supplies 5 V power (as well as a communications link), which is 
adequate for setup, but a 12 V battery will be needed for field deployment. 
 2. Connect an Ethernet cable to the 10/100 Ethernet port on the data logger. The yellow and 
green Ethernet port LEDs display activity approximately one minute after connecting. If you 
do not see activity, contact your network administrator. For more information, see Ethernet 
LEDs (p. 36).
 3. Using data logger support software (LoggerNet, or PC400), open Device Configuration 
Utility 
 .
 4. Select the CR1000X Series data logger from the list
6. Setting up the CR1000X/CR1000Xe     35

<!-- Page 53 -->
 5. Select the port assigned to the data logger from the Communication Port list. If connecting 
via Ethernet, select Use IP Connection.
 6. Beginning with operating system 8.00, the data logger is configured to be secure by 
default. Therefore, for data loggers that have a UID, PakBus Encryption is enabled by 
default. The default PakBus Encryption Key is the UID. Enter the data logger UID or, if the 
setting has been changed, enter the new key.  See Data logger security (p. 45) for more 
information.
 7. Click Connect.
 8. On the Deployment tab, click the Ethernet subtab. 
 9. The Ethernet Power setting allows you to reduce the power consumption of the data 
logger. If there is no Ethernet connection, the data logger will turn off its Ethernet interface 
for the time specified before turning it back on to check for a connection. Select Always On, 
1 Minute, or Disable.
 10. Enter the IP Address, Subnet Mask, and IP Gateway. These values should be provided by 
your network administrator. A static IP address is recommended. If you are using DHCP, 
note the IP address assigned to the data logger on the right side of the window. When the 
IP Address is set to the default, 0.0.0.0, the information displayed on the right side of the 
window updates with the information obtained from the DHCP server. Note, however, that 
this address is not static and may change. An IP address here of 169.254.###.### means 
the data logger was not able to obtain an address from the DHCP server. Contact your 
network administrator for help.
 11. Apply to save your changes.
6.1.3.2 Ethernet LEDs
When the data logger is powered, and Ethernet Power setting is not disabled, the 10/100 Ethernet 
LEDs will show the Ethernet activity:
- Solid Yellow: Valid Ethernet link.
- No Yellow: Invalid Ethernet link.
- Flashing Yellow: Ethernet activity.
- Solid Green: 100 Mbps link.
- No Green: 10 Mbps link.
6. Setting up the CR1000X/CR1000Xe     36

<!-- Page 54 -->
6.1.3.3 Setting up Ethernet communications between the data 
logger and computer
Once you have configured the Ethernet settings or obtained the IP information for your data 
logger, you can set up communications between your computer and the data logger over 
Ethernet.  Watch a video at https://www.campbellsci.com/videos/ezsetup-ethernet-connection 
  
or use the following instructions.
This procedure only needs to be followed once per data logger. However, these settings can be 
revised using the data logger support software Edit Datalogger Setup option 
 .
 1. Using data logger support software, open EZSetup.
- LoggerNet users, select Setup 
  from the Main category on the toolbar, click the 
View menu to ensure you are in the EZ (Simplified) view, then click Add Datalogger. 
- PC400 users, click Add Datalogger 
 .
 2. Click Next.
 3. Select the CR1000X Series from the list, enter a name for your station (for example, a site or 
project name), Next.
 4. Select the IP Port connection type and click Next.
 5. Type the data logger IP address followed by a colon, then the port number of the data 
logger in the Internet IP Address box. These were set up through the Ethernet 
communications option (p. 35) step. They can be accessed in Device Configuration Utility 
on the Ethernet subtab. Leading 0s must be omitted. For example:
- IPv4 addresses are entered as 192.168.1.2:6785
- IPv6 addresses must be enclosed in square brackets. They are entered as 
[2001:db8::1234:5678]:6785
 6. The PakBus address must match the hardware settings for your data logger. The default 
PakBus address is 1.
- Set an Extra Response Time if you want the data logger support software to wait a 
certain amount of time before returning a communications failure error.
- LoggerNet and PC400 users can set a Max Time On-Line to limit the amount of time 
the data logger remains connected. When the data logger is contacted, 
communications with it is terminated when this time limit is exceeded. A value of 0 in 
this field indicates that there is no time limit for maintaining a connection to the data 
logger. Next.
6. Setting up the CR1000X/CR1000Xe     37

<!-- Page 55 -->
 7. By default, the data logger does not use a security code. Therefore, the Security Code can 
be left at 0. If the code has been changed in the data logger, enter the new code.
Beginning with operating system 8.00, the data logger is configured to be secure by 
default. Therefore, for data loggers that have a UID, PakBus Encryption is enabled by 
default. The default PakBus Encryption Key is the UID. Enter the data logger UID or, if the 
setting has been changed, enter the new key.  See Data logger security (p. 45) for more 
information.
 8. Review the Communication Setup Summary. If you need to make changes, click Previous to 
return to a previous window and change the settings.
Setup is now complete, and the EZSetup Wizard allows you Finish or select Next. The Next steps 
take you through testing communications, setting the data logger clock, and sending a program 
to the data logger. See Testing communications with EZSetup (p. 38) for more information.
6.2 Testing communications with EZSetup
 1. Advance to, or select,  the Communication Test step in EZ Setup. See USB or RS-232 
communications (p. 24) for more information. 
 2. Ensure the data logger is physically connected to the computer, select Yes to test 
communications, then click Next to initiate the test. To troubleshoot an unsuccessful test, 
see Tips and troubleshooting (p. 165).
 3. With a successful connection, the Connection Time  with the data logger is displayed in the 
lower-left corner of the wizard. Click Next.
 4.  The Datalogger Clock window displays the time for both the data logger and the computer 
(server).
6. Setting up the CR1000X/CR1000Xe     38

<!-- Page 56 -->
- The Adjusted Server Date/Time displays the current reading of the clock for the 
computer running your data logger support software. If the Datalogger Date/Time 
and Adjusted Server Date/Time do not match,  click Set Datalogger Clock to set the 
data logger clock to the computer clock.
- Optionally, specify a positive or negative Time Zone Offset to apply when setting the 
data logger clock. This offset allows you to set the clock for a data logger that is in a 
different time zone than the computer (or to accommodate for changes in daylight 
saving time).
 5. Click Next.
 6. The data logger ships with a default GettingStarted program. If the data logger does not 
have a program, you can choose to send one by clicking Select and Send Program.  Click 
Next. 
 7. LoggerNet only - Use the following instructions or watch the Scheduled/Automatic Data 
Collection video 
 :
- The Datalogger Table Output Files window displays the data tables available to be 
collected from the data logger and the output file name. By default, all data tables set 
up in the data logger program will be included for collection.  Make note of the 
Output File Name and location. Click Next.
- Check Scheduled Collection Enabled to have LoggerNet automatically collect data 
from the data logger on the Collection Interval entered. When the Base Date and 
Time are in the past, scheduled collection will begin immediately after finishing the 
EZSetup wizard. Do not set up a scheduled collection during this tutorial. Click Next.
 8. Click Finish, or you may click Next to test communications, set the data logger clock, and 
send a program to the data logger. 
6.3 Making the software connection
Once you have configured your hardware connection (see Setting up communications with the 
data logger [p. 23]), your data logger and computer can communicate. Use the Connect screen 
to send a program, set the clock, view real-time data, and manually collect data.
- LoggerNet users, select Main and Connect 
  on the LoggerNet toolbar, select the data 
logger from the Stations list, then Connect 
 .
- PC400 users, select the data logger from the list and click Connect  
 .
To disconnect, click Disconnect 
 .
For more information,  see the Connect Window Tutorial 
 .
6. Setting up the CR1000X/CR1000Xe     39

<!-- Page 57 -->
6.4 Creating a Short Cut data logger program
You must provide a program for the data logger in order for it to make measurements, store 
data, or control external devices. There are several ways to write a program. The simplest is to use 
the program generator called Short Cut. For more complex programming, CRBasic Editor is used. 
The program file may use the extension  .CR1X, .CRB, or .DLD.
Data logger programs are executed on a precise schedule termed the scan interval, based on the 
data logger internal clock.
Measurements are first stored in temporary memory called variables in the Public table. Data 
stored in variables is usually overwritten each scan. Periodically, generally on a time interval, the 
data logger stores data in tables. The data tables are later copied to a computer using your data 
logger support software.
Use Short Cut software to generate a program for your data logger. Short Cut is included with 
your data logger support software.
This section guides you through programming a CR1000X/CR1000Xe data logger to measure the 
voltage of the data logger power supply, the internal temperature of the data logger, and a 
thermocouple. With minor changes, these steps can apply to other measurements.             Use the 
following instructions or watch the Quickstart part 3 video 
 :          
 1. Using data logger support software, launch Short Cut.
- LoggerNet users, click Program then Short Cut 
 .
- PC400 users, click Short Cut 
 .
 2. Click Create New Program.
 3. Select CR1000X Series and click Next.
NOTE:
The first time Short Cut is run, a prompt asks for a noise rejection choice. Select 60 Hz 
Noise Rejection for North America and areas using 60 Hz ac voltage. Select 50 Hz Noise 
Rejection for most of the Eastern Hemisphere and areas that operate at 50 Hz.
A second prompt lists sensor support options. Campbell Scientific, Inc. (US) is usually 
the best fit outside of Europe.
To change the noise rejection or sensor support option for future programs, use the 
Program menu.           
 4. Lists of Available Sensors and Devices and Selected Measurements Available for Output are 
displayed. Battery voltage BattV and internal temperature PTemp_C are selected by 
6. Setting up the CR1000X/CR1000Xe     40

<!-- Page 58 -->
default. During operation, battery and temperature should be recorded at least daily to 
assist in monitoring system status.
 5. Use the Search feature or expand folders to locate your sensor or device. Double-click on a 
sensor or measurement in the Available Sensors and Devices list to configure the device (if 
needed) and add it to the Selected list. For the example program, expand the Sensors and 
Temperature folders and double-click Type T Thermocouple.
 6. If the sensor or device requires configuration, a window displays with configuration 
options. Click Help at the bottom of the window to learn more about any field or option.     
For the example program, accept the default options:
- 1 Type T TC sensor
- Temp_C as the Temperature label, and set the units to Deg C
- PTemp_C as the Reference Temperature Measurement
 7. Click the Wiring tab at the top of the page to see how to wire the sensor to the data logger. 
With the power disconnected from the data logger, insert the wires as directed in the 
diagram. Ensure you clamp the terminal on the wire, not the colored insulation. Use the 
included flat-blade screwdriver to open and close the terminals.
 8. Click OK.
 9. Click Next.
 10. Use the Output Setup options to specify how often to make measurements and how often 
outputs are to be stored. Type 1 in the How often should the data logger measure its 
sensor(s)? box. Leave the units as Seconds.
 11. Multiple output intervals can be specified, one for each output table (Table1 and Table2 
tabs). For the example program, only one table is needed. Click the Table2 tab and click 
Delete Table.
 12. In the Table Name box, type a name for the table. For example: OneMin.
 13. Select a Data Output Storage Interval. For example: 1 minute.
 14. Click Next.
 15. Select a measurement from the Selected Measurements Available for Output list, then click 
an output processing option to add the measurement to the Selected Measurements for 
Output list. For the example program, select BattV and click the Minimum button to add it 
to the Selected Measurements for Output list. Do not store the exact time that the 
minimum occurred. Repeat this procedure for an Average PTemp_C and Average Temp_C.
6. Setting up the CR1000X/CR1000Xe     41

<!-- Page 59 -->
 16. Click Finish and give the program a meaningful name such as a site identifier. Click Save.
 17. If LoggerNet or other data logger support software is running on your computer, and the 
data logger is connected to the computer (see Making the software connection (p. 39) for 
more information), you can choose to send the program. Generally it is best to collect data 
first; so, we recommend sending the program using the instructions in Sending a program 
to the data logger (p. 43). Click No, do not send the program to the data logger. 
TIP:
It is good practice to always retrieve data from the data logger before sending a 
program; otherwise, data may be lost. See Collecting data (p. 77) for detailed 
instruction.
 18. Make note of the newly generated program location and filename. By default, programs 
created with Short Cut are stored in C:\Campbellsci\SCWin\. 
 19. Close Short Cut.
If your data acquisition requirements are simple, you can probably create and maintain a data 
logger program exclusively with Short Cut. If your data acquisition needs are more complex, the 
files that Short Cut creates are a great source for programming code to start a new program or 
add to an existing custom program using CRBasic. See the CRBasic Editor help for detailed 
information on program structure, syntax, and each instruction available to the data logger 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
NOTE:
Once a Short Cut generated program has been edited with CRBasic Editor, it can no longer be 
modified with Short Cut.
6. Setting up the CR1000X/CR1000Xe     42

<!-- Page 60 -->
6.5 Sending a program to the data logger
TIP:
It is good practice to always retrieve data from the data logger before sending a program; 
otherwise, data may be lost. See Collecting data (p. 77) for detailed instruction. 
Some methods of sending a program give the option to retain data when possible. Regardless of 
the program upload tool used, data will be erased when a new program is sent if any change 
occurs to one or more data table structures in the following list:
- Data table name(s)
- Data output interval or offset
- Number of fields per record
- Number of bytes per field
- Field type, size, name, or position
- Number of records in table
            Use the following instructions or watch the Quickstart part 4 video 
 .
 1. Connect the data logger to your computer (see Making the software connection (p. 39) for 
more information).
- LoggerNet users, select Main and Connect 
  on the LoggerNet toolbar, select the 
data logger from the Stations list, then Connect 
 .
- PC400 users, select the data logger from the list and click Connect  
 .
 2. LoggerNet users, click Send New... (located in the Current Program section on the right side 
of the window).
PC400 users, click  Send Program... (located in the Datalogger Program section on the right 
side of the window).
 3. PC400 users, confirm that you would like to proceed and erase all data tables saved on the 
data logger. Click Yes.
 4. Navigate to the program, select it, and click Open. For example: navigate to 
C:\Campbellsci\SCWin and select MyTemperature.CR1X. Click Open.
 5. LoggerNet users, confirm that you would like to proceed and erase all data tables saved on 
the data logger. Click Yes.
 6. The program is sent and compiled.
 7. Review the Compile Results window for errors, messages and warnings.
6. Setting up the CR1000X/CR1000Xe     43

<!-- Page 61 -->
 8. LoggerNet users, click Details, select the Table Fill Times tab.
PC400 user click OK then click Station Status 
 , select the Table Fill Times tab.
Ensure that the times shown are expected for your application. Click OK.
After sending a program, it is a good idea to monitor the Public table to make sure sensors are 
taking good measurements. See Working with data (p. 76) for more information.
6. Setting up the CR1000X/CR1000Xe     44
