---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 62-83
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 7: Chuẩn bị trước lắp đặt & Cơ chế bảo mật

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 62 đến 83).  
> **Chủ đề chính**: Bảo mật mặc định (Secure by default), lấy mã định danh UID, kiểm tra an ninh mạng Device Configuration Utility, cấu hình HTTP/HTTPS, FTP, Telnet, Ping, mã hóa TLS và chứng chỉ số (Certificates).

---


<!-- Page 62 -->
7. Pre-installation
This section describes some topics that should be considered before installing your station in the 
field.
7.1 Data logger security 45
7.1.1 Secure by default 45
7.1.2 Other Secure by default features 46
7.1.3 Obtaining a Unique Identification Number (UID) 47
7.1.4 Device Configuration Utility Security Check 52
7.1.5 Other security measures reviewed by Device Configuration Utility 57
7.1.6 TLS 58
7.1.7 Additional security measures 62
7.2 Default program 64
7.3 Power budgeting 66
7.1 Data logger security
Data logger security concerns include:
- Collection of sensitive data
- Operation of critical systems
- Networks that are accessible to many individuals
7.1.1 Secure by default
Data loggers with UIDs are secure by default. This means a PakBus Encryption Key is enabled by 
default. The default PakBus Encryption Key is the UID.
The UID can be found on a QR-code on the front of the data logger.
7. Pre-installation     45

<!-- Page 63 -->
CR1000X/CR1000Xe data loggers shipped before the release of UIDs and running operating 
system 8.00 or later may request a UID through Device Configuration Utility. See Obtaining a 
Unique Identification Number (UID) (p. 47). 
NOTE:
Direct USB connections do not require that you enter the PakBus Encryption Key, unless USB 
Not Trusted is enabled in the data logger. 
7.1.2 Other Secure by default features
Anonymous HTTP access is disabled by default. HTTP will be accessible with admin as the 
username and UID as the admin password. See Web services (p. 53) for more information.
FTP is disabled by default. However, when enabled, the UID will be the default FTP password. See 
Network services (p. 54) for more information.
Wi-Fi, when configured as an Access Point (“Create a Network”), will use the UID as the default 
password.
When a data logger has a UID and is reset to factory defaults, the UID will be used as the default 
password.
CAUTION:
Passwords may be changed from the default UID. However, when the data logger is reset to 
factory defaults or a new OS is sent, the default password will revert to the UID.
Check the CR1000X/CR1000Xe operating system version  as outlined in Updating the operating 
system (p. 157), and update as needed.
For more information on security, see Communications Protocols and Security Options for 
Campbell Scientific Data Loggers 
 .
7. Pre-installation     46

<!-- Page 64 -->
7.1.3 Obtaining a Unique Identification Number (UID)
As discussed in Data logger security (p. 45), new data loggers are given a Unique Identification 
Number (UID) at the factory. The UID is also used as a password specific to each data logger. 
Beginning with operating system 8.00 data loggers which were released prior to the UID being 
implemented can also be assigned a UID. This is done using Device Configuration Utility found in 
LoggerNet, PC400, or downloadable directly from the Campbell Scientific website. Device 
Configuration Utility version 2.31 or later is required.
NOTE:
A UID cannot be obtained without a CampbellCloud account. To create an account, see 
Creating a CampbellCloud organization account 
  in the CampbellCloud online manual.
NOTE:
When a UID is assigned to a data logger, it is written to one-time programmable memory. 
Therefore, any future OS updates or factory default resets of the device will not delete the 
UID.
Follow these steps to obtain a UID for your data logger:
 1. Ensure the data logger has the latest operating system installed. Operating systems are 
available from the Campbell Scientific website: 
https://www.campbellsci.com/downloads/operating-systems-datalogger 
 .
 2. Connect the data logger to a computer using a USB cable or TCP/IP connection.
 3. Open Device Configuration Utility on the computer.
7. Pre-installation     47

<!-- Page 65 -->
 4. Select the appropriate data logger from the left side, then select the correct 
communications port or IP address to connect.
 5. Click Connect.
7. Pre-installation     48

<!-- Page 66 -->
 6. If Device Configuration Utility was opened from LoggerNet or PC400, a window opens titled 
Avoid Conflicts with the Local Server. Click OK.
 7. Device Configuration Utility will identify the PakBus address of the data logger and connect 
to it. Once connected to the data logger, Device Configuration Utility will open the 
Datalogger tab under Deployment.
7. Pre-installation     49

<!-- Page 67 -->
 8. Select the Cloud Connection tab.
NOTE:
If a PakBus Encryption Key is not set, Device Configuration Utility will prompt you to set 
one before obtaining a UID. To do this, navigate back to the Deployment tab, click Edit 
next to PakBus Encryption Key and enter a key. Once a PakBus Encryption Key is set, it 
will be required for users and other devices to communicate with the data logger. Be 
sure to record it for future reference. Click OK, then Apply and Confirm your setting 
changes. Finally, click OK on the resulting dialog box and return to Step 5.
 9. To receive a UID, enter a valid CampbellCloud Account Name and Password. 
 10. Click Setup
7. Pre-installation     50

<!-- Page 68 -->
 11. Device Configuration Utility uses the entered credentials to connect to CampbellCloud and 
retrieve the UID. A progress bar displays the progress of the UID retrieval process.
 12. When complete, a new window opens with information about the UID, including the new 
UID number. The window includes a prompt to save the changes to the data logger. Click 
Yes to close the window.
7. Pre-installation     51

<!-- Page 69 -->
 13. Another window opens with the new data logger configuration. Scroll down through the 
settings to find the UID number. Record this number for future reference.
 14. Click OK
The data logger now has a UID assigned, enabling its use with CampbellCloud.
Refer to Onboarding a  data logger to CampbellCloud (p. 72) for the next steps to configure a 
data logger for publishing to CampbellCloud.
For more information on obtaining a UID, watch an instructional video at: Unique Identification 
Number (UID) and new security features 
 .
7.1.4 Device Configuration Utility Security Check
A Security Check is provided through Device Configuration Utility, starting with version 2.29. This 
check helps you identify areas where security can be improved.
All suggestions shown in Device Configuration Utility are optional and no changes will be made 
unless you make them.  For example, Device Configuration Utility uses a simple set of criteria to 
suggest a strong password. If you have your own criteria, you can use it. Because every 
deployment can be different, Device Configuration Utility will provide you with the information 
you need to ensure your data logger security is optimized for your application.
7. Pre-installation     52

<!-- Page 70 -->
In general, green, blue, and red icons indicate password strength.
- Green: strong password
- Blue: weak password
- Red: no password set
A strong password has the following:
- Eight or more characters
- One upper case letter
- One lower case letter
- One digit
- One special character
The green, blue, and red icons may also show the potential severity of a security vulnerability.
- Green: good, no action needed
- Blue: advisory information
- Red: action recommended
7.1.4.1 PakBus
PakBus encryption is the best data logger option to secure PakBus communications. For data 
loggers that have a UID, PakBus Encryption is enabled by default. The default PakBus Encryption 
Key is the UID.The  PakBus Encryption (AES-128) Key can be changed or cleared (deleted) in 
Device Configuration Utility. 
CAUTION:
Passwords may be changed from the default UID. However, when the data logger is reset to 
factory defaults or a new OS is sent, the default password will revert to the UID.
A PakBus Encryption Key forces PakBus data to be encrypted during transmission. The Security 
Check will check if the data logger has a PakBus Encryption Key set and a PakBus/TCP Password.
When neither of these is set, the Security Check will indicate that action is recommended.
The Security Check will not suggest setting a PakBus/TCP password if PakBus Encryption is 
already enabled. However, a PakBus/TCP password can be used with PakBus encryption.
7.1.4.2 Web services
The Security Check will check to see if HTTP or HTTPS is enabled. If these protocols are not being 
used but are enabled, we recommend disabling them.
7. Pre-installation     53

<!-- Page 71 -->
HTTP
Anonymous HTTP access is disabled by default. HTTP will be accessible with admin as the 
username and UID as the admin password. 
CAUTION:
Passwords may be changed from the default UID. However, when the data logger is reset to 
factory defaults or a new OS is sent, the default password will revert to the UID.
HTTP is an insecure protocol and can allow access to data logger data, settings, and 
programming. HTTP should only be enabled if it is required, and the data logger is on a secure 
network. If the data logger is not using web API or a hosted web page, then HTTP should be 
disabled. If HTTP protocol is used, user accounts should be configured with user names and 
passwords.  See Device Configuration Utility > Deployment > Network Services > Edit Accounts to 
make changes. 
See Web interface (p. 27) for more information.
HTTPS
HTTPS is a secure protocol, but it is disabled by default. It should only be enabled if necessary, as 
it grants access to data logger data, settings, and programming. If the data logger is not using 
web API or a hosted web page, then HTTPS should be disabled. If you do enable HTTPS, ensure 
that user accounts are also set up for security. Expect delays when using HTTPS, especially when 
first connecting.
7.1.4.3 Network services
The Security Check will check to see if any of the following network services are enabled. These 
services can be used to discover your data logger on an IP network. See Device Configuration 
Utility > Deployment > Network Services tab, to make changes. 
7. Pre-installation     54

<!-- Page 72 -->
NOTE:
FTP, Telnet, and Ping services are disabled by default.
FTP
FTP is an insecure protocol that allows access to data logger data and files. FTP is disabled by 
default. However, when enabled, the UID will be the default FTP password.
CAUTION:
Passwords may be changed from the default UID. However, when the data logger is reset to 
factory defaults or a new OS is sent, the default password will revert to the UID.
FTP should only be enabled if it is required. If the FTP file transfer is not needed, then FTP should 
be disabled.  Some data loggers support using SFTP (only as a client); this can improve file 
transfer security. Consider using a public/private key pair for SFTP authentication. Load a .PEM 
format file through the Device Configuration Utility Settings Editor > Advanced tab.
- Maximum key file size: 4 KB public, 4 KB private
- Key exchange methods: 
- ecdh-sha2-nistp256
- ecdh-sha2-nistp384
- ecdh-sha2-nistp521
- diffie-hellman-group-exchange-sha256
- diffie-hellman-group16-sha512
- diffie-hellman-group18-sha512
- diffie-hellman-group14-sha256
- diffie-hellman-group14-sha1
- diffie-hellman-group1-sha1
- diffie-hellman-group-exchange-sha1
- Host key types: 
- ecdsa-sha2-nistp256
- ecdsa-sha2-nistp384
- ecdsa-sha2-nistp521
- ecdsa-sha2-nistp256-cert-v01@openssh.com
7. Pre-installation     55

<!-- Page 73 -->
- ecdsa-sha2-nistp384-cert-v01@openssh.com
- ecdsa-sha2-nistp521-cert-v01@openssh.com
- rsa-sha2-512
- rsa-sha2-256
- ssh-rsa
- ssh-rsa-cert-v01@openssh.com
- Supported ciphers: 
- aes256-ctr
- aes192-ctr
- aes128-ctr
- aes256-cbc
- aes192-cbc
- aes128-cbc
For more information, see How to Generate SFTP Keys Easily 
 .
Telnet
Telnet, disabled by default, is an insecure protocol.  It should only be enabled if it is required, and 
the data logger is on a secure network. PakBus is the preferred way of accessing the data logger 
terminal interface.
Ping
Ping, disabled by default, makes the data logger visible to network scans.  It should only be 
enabled if it is required.
7.1.4.4 Operating System Status
The Security Check will check to see if your data logger is running the latest operating system. 
Newer operating systems may have enhanced security measures. See Updating the operating 
system (p. 157) for more information.
7. Pre-installation     56

<!-- Page 74 -->
7.1.5 Other security measures reviewed by Device 
Configuration Utility
Many of these settings can be accessed  from the Device Configuration Utility Settings Editor tab. 
Settings are organized in tabs and can be searched for.
7.1.5.1 PakBus TCP Enabled
By default, PakBus TCP communications are enabled. Normally this would not be disabled as it 
would prevent data logger support software from connecting to a data logger using TCP.
See Device Configuration Utility > Settings Editor > Network Services tab.
7.1.5.2 Accounts editor
The Security Check only checks password strength when entering passwords through Edit 
Accounts in Device Configuration Utility. It does no other checking.
See Device Configuration Utility > Deployment > Network Services > Edit Accounts to make 
changes. 
7.1.5.3 IP Broadcast Filtered
Set to one if all broadcast IP packets should be filtered from IP interfaces. Do not set this if you 
use the IP discovery feature of the Device Configuration Utility or of LoggerLink. If this is set to 
one, the data logger will fail to respond to the broadcast requests.
See Device Configuration Utility > Settings Editor > Advanced tab.
7. Pre-installation     57

<!-- Page 75 -->
7.1.5.4 Other communications protocols
Specific protocols such as PPP or MQTT have settings that involve security. The Security Check 
does not consider these settings other than password strength.
7.1.6 TLS
Transport Layer Security (TLS) is an internet communications security protocol. TLS settings  are 
necessary for server applications, not  for client applications. The primary reason for using TLS is 
to encrypt communications between a server and its clients. Using TLS is recommended when 
connecting to a data logger over an IP connection using the web interface. TLS does not affect 
PakBus communications.
Example server application instructions include:
- HTTPS server
- DNP() using the optional DNPTLS parameter
Example client application instructions include:
- HTTPGet(), HTTPPut() and HTTPPost()
- EmailRelay()
- EmailSend() and EmailRecv()
- FTPClient()
- MQTTConnect()
- MQTTPublishTable()
- MQTTPublishConstTable()
CSI Web Server can also use TLS. 
NOTE:
For enhanced security, TLS settings are only shown in Device Configuration Utility when using 
a direct USB connection, or an IP connection using PakBus Encryption.
Use the following steps to configure TLS:
 1. Use the Device Configuration Utility to enable HTTPS and disable HTTP. See Deployment > 
Network Services tab. 
 2. Use the Device Configuration Utility to enable and set up TLS. See Deployment > 
Datalogger > TLS tab. 
7. Pre-installation     58

<!-- Page 76 -->
 a. Increase the number of Max TLS Server Connections to greater than zero. Each additional 
connection uses about 20 KB of memory. For general use, such as publishing web pages, 
use a minimum of five connections. Add more if multiple users may access the hosted 
web pages at the same time. See Web interface (p. 27).
 b. Use Set Private Key and Set Certificate to upload files in .PEM format. These can either be 
self-signed or issued from a trusted third party organization. See Obtaining certificate 
and private key (p. 59) for more information.
- Maximum key file size: 4 KB public, 4 KB private
Review the Will send file path message to ensure you have the correct files.
 3. Apply to save your changes. 
 4. Confirm your TLS security settings by connecting to the data logger using a web browser. 
See Web interface (p. 27). This connection can initially take up to 30 seconds as the data 
logger negotiates the TLS with the web browser. If the default data logger web page loads 
then TLS has been set up correctly.
NOTE:
If the certificates uploaded to the data logger are from an unknown source, such as 
most self-signed certificates, the web browser will likely display a warning. If the issuer 
can be trusted, this warning can be bypassed.
7.1.6.1 Obtaining certificate and private key
This section is provided as general guidance only. Have your IT department provide you with the 
required certificate and key files, or work with them to obtain them. 
From a Certificate Authority
Some things you will need to know before starting the process with a Certificate Authority: 
- Your website domain name, or common name 
- Proof that you control the domain. This could include the email associated with the domain 
name. 
7. Pre-installation     59

<!-- Page 77 -->
The general steps when using an outside source for the certificate and keys are as follows:
 1. Select a Certificate Authority (CA) such as DigiCert, Symantec, or GoDaddy, to generate 
your certificate and key files.
NOTE:
Generally there is a cost associated with the this process, and it may take several days. It 
is common to refile the application several times to get the correct files in the correct 
format. 
 2. Create an account with your selected CA. Sign in. 
 3. Generate a private key and Certificate Signing Request (CSR). Save these files to a secure 
location on your computer. Some Certificate Authorities may offer to generate these for 
you. If not, then they will require the CSR and private key you generated.
NOTE:
This is the private key file you will need later. If the file saved has a .txt extension, 
make a copy and change the extension to .PEM.
TIP:
If your CA generates your private key and CSR, save a copy of both. For your security, 
the CA will not keep a record of either the CSR or private key.  If you fail to save them, 
you will have to generate new ones and this will take additional time.
 4. Provide proof that you control the domain. Often additional instructions are received in an 
email from the CA.
 5. Receive the certificate from the CA. This may take two or more business days. Save this file 
to a secure location on your computer.
 6. Verify that the key file is in .PEM format. The contents of a valid .PEM formatted key will 
look similar to the following when viewed as a text file. The ----BEGIN RSA PRIVATE 
KEY----- header and -----END RSA PRIVATE KEY----- footer indicate that the 
key was generated in the correct format.
7. Pre-installation     60

<!-- Page 78 -->
 7. Verify that the certificate file is in .PEM format. The contents of a valid .PEM formatted 
certificate will look similar to the following when viewed as a text file. The ----BEGIN 
CERTIFICATE----- header and -----END CERTIFICATE----- footer indicate that 
the key was generated in the correct format.
From your IT department
If your IT department provides the key and certificate files, you need to determine if the key 
requires a private key password. To determine if your .PEM formatted key requires a private key 
password:
 1. Open the key file in a text editor.
 2. The following header is an example of the key without a private key password. 
-----BEGIN RSA PRIVATE KEY-----
MIIEpQIBAAKCAQEAo8GRTJKW+grlRfuuUNrlqCc4aodqaRnNd+L+/Wjpz
 3. The following header is an example of the key with a private key password.
-----BEGIN RSA PRIVATE KEY-----
Proc-Type: 4,ENCRYPTED
DEK-Info: DES-EDE3-CBC,556C1115CDA822F5
AHi/3++BAAKCAQEAo8GRTJKW+grlRfuuUNrlqCc4aodqaRnNd+L+/Wjpz
7. Pre-installation     61

<!-- Page 79 -->
 4. If the key header is similar to that shown in step 3, you need to specify a private key 
password. Your IT department should provide this.
NOTE:
Never specify a key password if your key does not have one.
7.1.7 Additional security measures
Following are some additional security measures that may be taken to secure your data logger. 
7.1.7.1 Security codes
The data logger employs a security scheme that includes three levels of security. Security codes 
can effectively lock out innocent tinkering and discourage wannabe hackers on all 
communications links.  However, any serious hacker with physical access to the data logger or to 
the communications hardware can, with only minimal trouble, overcome the five-digit security 
codes. Security codes are held in the data logger Settings Editor. 
The preferred methods of enabling security include the following:
- Device Configuration Utility: Security codes are set on the Deployment> Datalogger tab.
- Network Planner: Security codes can be set as data loggers are added to the network. 
Alternatively, in CRBasic the SetSecurity() instruction can be used. It  is only executed at 
program compile time. This is not recommended because deleting SetSecurity() from a 
CRBasic program is not equivalent to SetSecurity(0,0,0).  Settings persist when a new 
program is downloaded that has no SetSecurity() instruction.
Up to three levels of security can be set.  Valid security codes are  1 through  65535 ( 0 confers no 
security).  Security 1 must be set before  Security 2.   Security 2 must be set before  Security 3. If any 
one of the codes is set to  0, any security code level greater than it will be set to  0.  For example, if  
Security 2 is  0 then  Security 3 is automatically set to  0.  Security codes are unlocked in reverse 
order:  Security 3 before  Security 2,  Security 2 before  Security 1.
7. Pre-installation     62

<!-- Page 80 -->
Table 7-1: Functions affected by security codes
Function Security code 1 set Security code 2 set Security code 3 set
data logger program Cannot change or retrieve
All communications 
prohibited
Settings editor
and Status table Writable variables cannot be changed
Setting clock unrestricted Cannot change or set
Public table unrestricted Writable variables 
cannot be changed
Collecting data unrestricted unrestricted
See Security(1), Security(2), Security(3) (p. 227) for the related fields in the Settings Editor.
7.1.7.2 CRBasic
Encrypt program files if they contain sensitive information. See CRBasic help FileEncrypt() 
or use CRBasic Editor > File > Save and Encrypt.
Hide program files for extra protection. See CRBasic help FileManage() instruction.
7.1.7.3 USB
Beginning with operating system 8.00, the data logger has the following settings to control 
security of the USB port. See Settings (p. 214) for more information. 
USB Disable
USBDisable controls whether the USB port is enabled or disabled. If set to 0 (the default), the port 
is enabled. 1 disables it. If the setting is changed, a data logger reboot is required for the change 
to take effect.
NOTE:
When USB is disabled, you will need to use another communications method (Ethernet, serial, 
CR1000KD) to re-enable it. 
USB Not Trusted
USBNotTrusted controls how the USB port behaves, as a COM port, with respect to security. If set 
to 0 (default), the port will allow PakBus communications to occur when the PakBus Encryption 
Key (p. 223) is set. Also, if RNDIS is enabled (USB Disable (p. 228) = 0), no security challenges will 
7. Pre-installation     63

<!-- Page 81 -->
be given to gain access via HTTP(S). If set to 1, PakBus communications will follow the 
PakBusEncryption Key setting and RNDIS will require login credentials.
NOTE:
In Device Configuration Utility, RNDIS is treated an IP connection rather than a direct 
connection. As a result, the USBNotTrusted setting does not apply when using RNDIS 
through Device Configuration Utility.
7.1.7.4 Other
Monitor your data logger for changes by tracking program and operating system signatures, as 
well as CPU, USR, and CRD file contents.
Secure the physical data logger and power supply under lock and key.
WARNING:
Some security features can be subverted through physical access to the data logger. If 
absolute security is a requirement, the physical data logger must be kept in a secure location.
Some options to secure your data logger from mistakes or tampering include:
- Setting a PakBus/TCP password. The PakBus TCP password controls access to PakBus 
communications over a TCP/IP link. PakBusTCP passwords can be set in Device 
Configuration Utility.
- Disabling FTP or setting an FTP username and password in Device Configuration Utility.
- Disabling HTTP/HTTPS or creating a user account to secure HTTP/HTTPS (see Device 
Configuration Utility > Deployment > Network Services > Edit Accounts to make changes. 
- Enabling HTTPS and disabling HTTP. To prevent data collection via the web interface, both 
HTTP and HTTPS must be disabled.
For additional information on data logger security, see:
- 4 Ways to Make your Data More Secure 
- Available Security Measures for Internet-Connected Dataloggers 
- How to Use Datalogger Security Codes 
- How to Generate SFTP Keys Easily 
7.2 Default program
Many data logger settings can be changed remotely over a communications link. This 
convenience comes with the risk of inadvertently changing settings and disabling 
7. Pre-installation     64

<!-- Page 82 -->
communications. For example, external cellular modems are often controlled by a switched 
12 VDC (SW12) terminal. SW12 is normally off; so, if the program controlling SW12 is disabled, 
such as by changing a setting or sending a new operating system, the cellular modem is switched 
off and the remote data logger will not turn it on. This could require an on-site visit to correct the 
problem unless a special program called default has been installed. 
Having a default.CR1X program stored on the data logger will also ensure that a non-
compiling CRBasic program does not lock out a remote user.
NOTE:
The default program may use the extension .CR1X,.CRB or .DLD.
When a file named default.CR1X is stored on the data logger CPU: drive, it is loaded  if no 
other program takes priority. Program execution priorities are as follows: 
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
See File management via powerup.ini (p. 161) for more information.
The default.CR1X program generally contains instructions to preserve critical datalogger 
settings such as communications settings, but should not be more than a few lines of code.
CRBasic Example 1: default.CR1X example 
'This program turns ON the SW12 switched
'power terminal, for 30 seconds every 60 seconds.
BeginProg
Scan(1,Sec,0,0)
If TimeIsBetween (15,45,60,Sec) Then SW12(SW12_1,1)
NextScan
EndProg
7. Pre-installation     65

<!-- Page 83 -->
Downloading operating systems over communications requires much of the available 
CR1000X/CR1000Xe memory. If the intent is to load operating systems via a communications link 
and have a default.CR1X file in the CR1000X/CR1000Xe, the default.CR1X program 
should not allocate significant memory, as might happen by allocating a large USR: drive. Also, 
do not auto-allocate tables in DataTable() instructions; if it is necessary to use DataTable
() instructions, set small fixed table sizes. Refer to Sending an operating system to a remote data 
logger (p. 158) for information about sending the operating system.
Execution of default.CR1X at power-up can be aborted by holding down the DEL key on a 
CR1000KD Keyboard/Display.
7.3 Power budgeting
In low-power situations, the data logger can operate for several months on non-rechargeable 
batteries. Power systems for longer-term remote applications typically consist of a charging 
source, a charge controller, and a rechargeable battery. When ac line power is available, a VAC-
to-VDC wall adapter, charging regulator, and a rechargeable battery can be used to construct an 
uninterruptible power supply (UPS).
When designing a power supply, consider worst-case power requirements and environmental 
extremes. For example, the power requirement of a weather station may be substantially higher 
during extreme cold, while at the same time, the extreme cold constricts the power available 
from the power supply. System operating time for batteries can be estimated by dividing the 
battery capacity (ampere hours) by the average system current drain (amperes). 
For more information see:
- Power Supplies Application Note 
- Battery Care Blog 
- Troubleshooting Your Solar Panel blog 
- Power Budget Spreadsheet 
- Power Budgeting Video 
See also:
- Power input (p. 12)
- Power output (p. 14)
- Power requirements (p. 253)
- Power output specifications  (p. 254)
7. Pre-installation     66
