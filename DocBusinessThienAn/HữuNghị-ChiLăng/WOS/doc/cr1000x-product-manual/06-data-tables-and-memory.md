---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 93-115
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 10–11: Xử lý dữ liệu & Quản lý bộ nhớ lưu trữ

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 93 đến 115).  
> **Chủ đề chính**: Định dạng kiểu dữ liệu (IEEE4, FP2, Long, String), cấu trúc bảng dữ liệu (Data tables, Headers, Records), phân bổ bộ nhớ SRAM, bộ nhớ Flash CPU drive và thẻ nhớ ngoài MicroSD (CRD drive).

---


<!-- Page 93 -->
10. Working with data
10.1 Default data tables
By default, the data logger includes three tables: Public, Status, and DataTableInfo. Each of these 
tables only contains the most recent measurements and information.
- The Public table is configured by the data logger program, and updated at the scan interval 
set within the data logger program. It shows measurement and calculation results as they 
are made. 
- The Status table includes information about the health of the data logger and is updated 
only when viewed.
- The DataTableInfo table reports statistics related to data tables. It also only updates when 
viewed.
- User-defined data tables update at the schedule set within the program.
For information on collecting your data, see Collecting data (p. 77).
Use these instructions or follow the Connect Window tutorial 
  to monitor real-time data. 
LoggerNet users, select the Main category and Connect 
  on the LoggerNet toolbar, then select 
the data logger from the Stations list, then click Connect 
 . Once connected, select a table to 
view in the Table Monitor.
PC400 users, click Connect 
 , then Monitor Data. When this tab is first opened for a data logger, 
values from the Public table are displayed. To view data from other tables, click Add 
 , select a 
table or field from the list, then drag it into a cell on the Monitor Data tab.
10. Working with data     76

<!-- Page 94 -->
10.2 Collecting data
The data logger writes to data tables  based on intervals and conditions set in the CRBasic 
program (see Creating data tables in a program (p. 85) for more information). After the program 
has been running for enough time to generate data records, data may be collected by using data 
logger support software. During data collection, data is copied to the computer and still remains 
on the data logger. Collections may be done manually, or automatically through scheduled 
collections set in LoggerNet Setup. Use these instruction or follow the Collect Data Tutorial 
 .
10.2.1 Collecting data using LoggerNet
 1. From the LoggerNet toolbar, click Main and Connect 
 , select the data logger from the 
Stations list, then Connect 
 .
 2. Click Collect Now 
 .
 3. After the data is collected, the Data Collection Results window displays the tables collected 
and where they are stored on the computer.
 4. Select a data file, then View File to view the data. See Viewing historic data (p. 78).
10.2.2 Collecting data using PC400
 1. Click Connect 
  on the main PC400 window. 
 2. Go to the Collect Data tab.
10. Working with data     77

<!-- Page 95 -->
 3. By default, all output tables set up in the data logger program are selected for collection. 
Typically, the default tables (DataTableInfo, Public, and Status) are not collected. 
 4. Select an option for What to Collect. Either option creates a new file if one does not already 
exist.
- New data from data logger (Append to data files): This is the default, and most often 
used option. Collect only the data, in the selected tables, stored since the last data 
collection from this instance of PC400 and append this data to the end of the existing 
files on the computer.
- All data from data logger (Overwrite data files): Collects all of the data in the selected 
tables and overwrites (or replaces) the existing data files on the computer.
 5. Click Start Data Collection.
 6. After the data is collected, the Data Collection Results window displays the tables collected 
and where they are stored on the computer.
 7. Select a data file, then View File to view the data. See Viewing historic data (p. 78).
10.3 Viewing historic data
View Pro 
  contains tools for reviewing data in tabular form as well as several graphical layouts 
for visualization. Use these instructions or follow the View Data Tutorial 
 . 
Once the data logger has had enough time to store multiple records collect and review the data.
 1. To view the most recent data, connect the data logger to your computer and collect your 
data (see Collecting data (p. 77) for more information).
 2. Open View Pro:
- LoggerNet users click Data then View Pro 
  on the LoggerNet toolbar.
- PC400 users click View Data Files via View Pro 
 .
 3. Click Open 
 , navigate to the directory where you saved your tables (the default directory 
is C:\Campbellsci\[your data logger software application]). For example: navigate to the 
C:\Campbellsci\LoggerNet folder and select OneMin.dat.
 4. Click Open.
10. Working with data     78

<!-- Page 96 -->
10.4 Data types and formats
Data takes different formats as it is created and manipulated in the data logger, as it is displayed 
through software, and as it is retrieved to a computer file. It is important to understand the 
different data types, formats and ranges, and where they are used.
See the CRBasic Editor help for additional data types and formats, and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
Table 10-1: Data types, ranges and resolutions
Data type Description Range Resolution Where used
Float IEEE four-byte 
floating point
+/–1.8 *10^–38 to 
+/–3.4 *10^38
24 bits 
(about 7 digits) variables
Long four-byte 
signed integer
–2,147,483,648 to 
+2,147,483,647 1 bit variables, output
Boolean four-byte 
signed integer –1, 0 True (–1) or 
False ( 0)
variables, 
sample output
String ASCII String     variables, 
sample output
IEEE4 IEEE four-byte 
floating point
+/–1.8 *10^–38 to 
+/–3.4 *10^38
24 bits 
(about 7 digits)
internal calculations, 
output
IEEE8 IEEE eight-byte 
floating point
+/–2.23 *10^–308 to 
+/–1.8 *10^308
53 bits 
(about 16 digits)
internal calculations, 
output
FP2 Campbell Scientific 
two-byte floating point –7999 to +7999 13 bits 
(about 4 digits) output
NSEC eight-byte time stamp   nanoseconds variables, output
10.4.1 Variables
In CRBasic, the declaration of variables (via the DIM or the PUBLIC statement) allows an optional 
type descriptor As that specifies the data type. The data types are Float, Long, Boolean, and 
String. The default type is Float.
Example variables declared with optional data types
Public PTemp As Float, Batt_volt
Public Counter As Long
Public SiteName As String * 24
10. Working with data     79

<!-- Page 97 -->
As Float specifies the default data type. If no data type is explicitly specified with the As 
statement, then Float is assumed. Measurement variables are stored and calculations are 
performed internally in IEEE 4 byte floating point with some operations calculated in double 
precision. A good rule of thumb is that resolution will be better than 1 in the seventh digit.
As Long specifies the variable as a 32 bit integer. There are two possible reasons a user would 
do this: (1) speed, since the CR1000X/CR1000Xe Operating System can do math on integers faster 
than with Floats, and (2) resolution, since the Long has 31 bits compared to the 24 bits in the 
Float. A good application of the As Long declaration is a counter that is expected to get very 
large.
As Boolean specifies the variable as a 4 byte Boolean. Boolean variables are typically used for 
flags and to represent conditions or hardware that have only 2 states (e.g., On/Off, High/Low). A 
Boolean variable uses the same 32 bit long integer format as a Long but can set to only one of 
two values: True, which is represented as –1, and false, which is represented with 0. When a 
Float or Long integer is converted to a Boolean, zero is False (0), any non-zero value will set 
the Boolean to True (-1). The Boolean data type allows application software to display it as an 
On/Off, True/False, Red/Blue, etc.
The CR1000X/CR1000Xe uses –1 rather than some other non-zero number because the AND and 
OR operators are the same for logical statements and binary bitwise comparisons. The number -
1 is expressed in binary with all bits equal to 1, the number 0 has all bits equal to 0. When –1 is 
anded with any other number the result is the other number, ensuring that if the other number is 
non-zero (true), the result will be non-zero.
As String * size specifies the variable as a string of ASCII characters, NULL terminated, 
with an optional size specifying the maximum number of characters in the string. A string is 
convenient in handling serial sensors, dial strings, text messages, etc. When size is not specified, a 
default of 24 characters will be used (23 usable bytes and 1 terminating byte).
As a special case, a string can be declared As String * 1. This allows the efficient storage of a 
single character. The string will take up 4 bytes in memory and when stored in a data table, but it 
will hold only one character.
Structures (StructureType/EndStructureType) are an advanced technique used to 
organize variables and display data in a structured manner. They can significantly shorten 
program code, especially for instructions that output an array of values, such as AVW200(), GPS(), 
and SDI12Recorder(). For example, a single StructureType may be used to organize and 
display data for multiple vibrating wire sensors or many SDI-12 sensors without creating aliases 
for each sensor. See the CRBasic Editor help for detailed instruction information and program 
examples: https://help.campbellsci.com/crbasic/cr1000x/ 
 .
For more information on variables, see Fundamentals of CRBasic Programming Part 1: Variables 
.
10. Working with data     80

<!-- Page 98 -->
10.4.2 Constants
The Const declaration is used to assign a name that can be used in place of a value in the data 
logger CRBasic program. Once a value is assigned to a constant, each time the value is needed in 
the program, the programmer can type in the constant name instead of the value itself. The use 
of the Const declaration can make the program easier to follow, easier to modify, and more 
secure against unintended changes. Unlike variables, constants cannot be changed while the 
program is running.
Constants must be defined before they are used in the program. Constants can be defined in a 
ConstTable/EndConstTable construct allowing them to be changed using the keyboard 
display, the C command in terminal mode, or via a custom menu.
Constants can also be typed For example: Const A as Long = 9999, and Const B as String = 
“MyString”. Valid data types for constants are: Long, Float, Double, and String. Other data 
types return a compile error.
When the CRBasic program compiles, the compiler determines the type of the constant (Long, 
Float, Double, or String) from the expression. This data type is communicated to the 
software. The software formats or restricts the input based on the data type communicated to it 
by the data logger.
You can declare a constant with or without specifying a data type. If a data type is not specified, 
the compiler determines the data type from the expression. For example:  Const A = 9999 will 
use the Long data type. Const A = 9999.0 will use the Floatdata type.
10.4.3 Data storage
Data can be stored in IEEE4 or FP2 formats. The format is selected in the program instruction that 
outputs the data, such as Minimum() and Maximum().
Additionally, data can be stored in IEEE8 format when high precision is needed. For more 
information on double-precision math, watch an instructional video 
at: http://www.campbellsci.com/videos/double-precision 
 .
While Float (IEEE 4 byte floating point) is used for variables and internal calculations, FP2 is 
adequate for most stored data. Campbell Scientific 2 byte floating point (FP2) provides 3 or 4 
significant digits of resolution, and requires half the memory space as IEEE4 (2 bytes per value 
vs 4).
Table 10-2: Resolution and range limits of FP2 data
Zero Minimum magnitude Maximum magnitude
0.000 ±0.001 ±7999.
10. Working with data     81

<!-- Page 99 -->
The resolution of FP2 is reduced to 3 significant digits when the first (left most) digit is 8 or 
greater. Thus, it may be necessary to use IEEE4 output or an offset to maintain the desired 
resolution of a measurement. For example, if water level is to be measured and output to the 
nearest 0.01 foot, the level must be less than 80 feet for FP2 output to display the 0.01 foot 
increment. If the water level is expected to range from 50 to 90 feet the data could either be 
output in IEEE4 or could be offset by 20 feet (transforming the range to 30 to 70 feet).
Table 10-3: FP2 decimal location
Absolute value  Decimal location
0 – 7.999 X.XXX
8 – 79.99 XX.XX
80 – 799.9 XXX.X
800 – 7999. XXXX.
NOTE:
String and Boolean variables can be output with the Sample() instruction. Results of 
Sampling a Boolean variable will be either -1 or 0 in the collected Data Table. A Boolean 
displays in the Numeric Monitor Public and Data Tables as true or false.
10.5 About data tables
A data table is essentially a file that resides in data logger memory (for information on data table 
storage. See Data memory (p. 88). The file consists of five or more rows. Each row consists of 
columns, or fields. The first four rows constitute the file header. Subsequent rows contain data 
records. Data tables may store individual measurements, individual calculated values, or 
summary data such as averages, maximums, or minimums.
Typically, files are  written to  based on time or event. The number of data tables is limited to 250, 
which includes the Public, Status, DataTableInfo, and ConstTable. You can retrieve data based on 
a schedule or by manually choosing to collect data using data logger support software. See 
Collecting data (p. 77).
10. Working with data     82

<!-- Page 100 -->
Table 10-4: Example data
TOA5, MyStation, CR1000X, 1142, CR1000X.Std.01, CPU:MyTemperature.CR1X, 1958, OneMin
TIMESTAMP RECORD BattV_Avg PTemp_C_Avg Temp_C_Avg
TS RN Volts Deg C Deg C
    Avg Avg Avg
2019-03-08 14:24:00 0 13.68 21.84 20.71
2019-03-08 14:25:00 1 13.65 21.84 20.63
2019-03-08 14:26:00 2 13.66 21.84 20.63
2019-03-08 14:27:00 3 13.58 21.85 20.62
2019-03-08 14:28:00 4 13.64 21.85 20.52
2019-03-08 14:29:00 5 13.65 21.85 20.64
10.5.1 Table definitions
Each data table is associated with descriptive information, referred to as a“table definition,” that 
becomes part of the file header (first few lines of the file) when data is downloaded to a 
computer. Table definitions include the  data logger type and OS version, name of the CRBasic 
program associated with the data, name of the data table (limited to 20 characters), and 
alphanumeric field names. 
10.5.1.1 Header rows
The first header row of the data table is the environment line, which consists of eight fields. The 
following list describes the fields using the previous table entries as an example:
- TOA5 - Table output format. Changed via LoggerNet Setup 
  Standard View, Data Files 
tab. Other formats include: TOB1 and TOACI1.
- MyStation - Station name. Changed via LoggerNet Setup,  Device Configuration Utility, or 
CRBasic program.
- CR1000X - Data logger model.
- 1142 - Data logger serial number.
- CR1000X.Std.01 - Data logger OS version. 
- CPU:MyTemperature.CR1X - Data logger program name. Changed by sending a new 
program (see Sending a program to the data logger (p. 43) for more information).
- 1958 - Data logger program signature. Changed by revising a program or sending a new 
program (see Sending a program to the data logger (p. 43) for more information).
10. Working with data     83

<!-- Page 101 -->
- OneMin - Table name as declared in the running program (see Creating data tables in a 
program (p. 85) for more information).
The second header row reports field names. Default field names are a combination of the 
variable names (or aliases) from which data is derived, and a three-letter suffix. The suffix is an 
abbreviation of the data process that outputs the data to storage. A list of these abbreviations 
follows in Data processing abbreviations (p. 84). 
If a field is an element of an array, the field name will be followed by a indices within parentheses 
that identify the element in the array. For example, a variable named Values, which is declared 
as a two-by-two array in the data logger program, will be represented by four field names: 
Values(1,1), Values(1,2), Values(2,1), and Values(2,2). There will be one value in 
the second header row for each scalar value defined by the table.
If the default field names are not acceptable to the programmer, the FieldNames() instruction 
can be used in the CRBasic program to customize the names. TIMESTAMP, RECORD, BattV_
Avg, PTemp_C_Avg, and Temp_C_Avg are the default field names in the previous Example 
data (p. 83).
The third header row identifies engineering units for that field. These units are declared at the 
beginning of a CRBasic program using the optional Units() declaration. In Short Cut, units are 
chosen when sensors or measurements are added. Units are strictly for documentation. The data 
logger does not make use of declared units, nor does it check their accuracy.
The fourth header row reports abbreviations of the data process used to produce the field of 
data.
Table 10-5: Data processing abbreviations
Data processing name Abbreviation
Totalize Tot
Average Avg
Maximum Max
Minimum Min
Sample at Max or Min SMM
Standard Deviation Std
Moment MMT
Sample No abbreviation
Histogram Hst
10. Working with data     84

<!-- Page 102 -->
Table 10-5: Data processing abbreviations
Data processing name Abbreviation
Histogram4D H4D
FFT FFT
Covariance Cov
Level Crossing LCr
WindVector WVc
Median Med
ET ETsz
Solar Radiation (from ET) RSo
Time of Max TMx
Time of Min TMn
10.5.1.2 Data records
Subsequent rows are called data records. They include observed data and associated record 
keeping. The first field is a time stamp (TS), and the second field is the record number (RN).
The time stamp shown represents the time at the beginning of the scan in which the data is 
written. Therefore, in record number 3 in the previous Example data (p. 83), Temp_C_Avg shows 
the average of the measurements taken over the minute beginning at 14:26:01 and ending at 
14:27:00. As another example, consider rainfall measured every second with a daily total rainfall 
recorded in a data table written at midnight. The record time stamped 2019-03-08 00:00:00 will 
contain the total rainfall beginning at 2019-03-07 00:00:01 and ending at 2019-03-08 00:00:00.
NOTE:
TableName.Timestamp syntax can be used to return the timestamp of a data table record, 
expressed either as a time into an interval (for example seconds since 1970 or seconds since 
1990) or as a date/time string. For more information, 
see: https://www.campbellsci.com/blog/programmatically-access-stored-data-values 
 .
10.6 Creating data tables in a program
Data is stored in tables as directed by the CRBasic program. In Short Cut, data tables are created 
in the Output steps. See Creating a Short Cut data logger program (p. 40) Data tables are created 
10. Working with data     85

<!-- Page 103 -->
within the CRBasic data logger program using the DataTable()/EndTable instructions. They 
are placed after variable declarations and before the BeginProg instruction. 
Public 'Declare Public Variables
DataTable()
   'Output Trigger Condition(s)
    'Output Processing Instructions 
EndTable
 
'Main Program
BeginProg
Between DataTable() and EndTable() are instructions that define what data to store and 
under what conditions data is stored. A data table must be called by the CRBasic program for 
data processing and storage to occur. Typically, data tables are called by the CallTable() 
instruction once each program scan.
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
For additional information on data tables, watch Fundamentals of CRBasic Programming Part 2: 
Tables 
  and Fundamentals of CRBasic Programming Part 5: Using Conditional Tables 
 .
Use the DataTable() instruction to define the number of records, or rows, allocated to a data 
table. You can set a specific number of records, which is recommended for conditional tables, or 
allow your data logger to auto-allocate table size. With auto-allocation, the data logger balances 
the memory so the tables “fill up” (newest data starts to overwrite the oldest data) at about the 
same time. It is recommended you reserve the use of auto-allocation for data tables that store 
data based only on time (tables that store data based on the DataInterval() instruction). 
Event or conditional tables are usually set to a fixed number of records. View data table fill times 
for your program on the Station Status > Table Fill Times tab (see Checking station status (p. 166) 
for more information). An example of the Table Fill Times tab follows. For information on data 
table storage see Data memory (p. 88).
10. Working with data     86

<!-- Page 104 -->
For additional information on data logger memory, visit the Campbell Scientific blog article, How 
to Know when Your Datalogger Memory is Getting Full 
 .
10. Working with data     87

<!-- Page 105 -->
11. Data memory
The data logger includes three types of memory: SRAM, Flash, and Serial Flash. A memory card 
slot is also available for an optional microSD card. Note that the data logger USB port does not 
support USB flash or thumb drives (see Communications ports (p. 17) for more information). 
- Total onboard: 128 MB of flash + 4 MB battery-backed SRAM
- Data storage: 4 MB SRAM + 72 MB flash (extended data storage automatically used 
for auto-allocated Data Tables not being written to a card)
- CPU drive: 30 MB flash
- OS load: 8 MB flash
- Settings: 1 MB flash
- Reserved (not accessible): 10 MB flash
- Data storage expansion: Removable microSD flash memory, up to 16 GB
11.1 Data tables
Measurement data is primarily stored in data tables within SRAM. Data is usually erased from this 
area when a program is sent to the data logger.
During data table initialization, memory sectors are assigned to each data table according to the 
parameters set in the program. Program options that affect the allocation of memory include the 
Size parameter of the DataTable() instruction, the Interval and Units parameters of 
the DataInterval() instruction. The data logger uses those parameters to assign sectors in a 
way that maximizes the life of its memory. See the CRBasic Editor help for detailed instruction 
information and program examples: https://help.campbellsci.com/crbasic/cr1000x/ 
 .
By default, data memory sectors are organized as ring memory. When the ring is full, oldest data 
is overwritten by newest data. Using the FillStop statement sets a program to stop writing to 
the data table when it is full, and no more data is stored until the table is reset. To see the total 
number of records that can be stored before the oldest data is overwritten, or to reset tables, go 
to Station Status > Table Fill Times in your data logger support software.
Data concerning the data logger memory are posted in the Status and DataTableInfo tables. For 
additional information on these tables, see Information tables and settings (advanced) (p. 202).
For additional information on data logger memory, visit the Campbell Scientific blog article, How 
to Know when Your Datalogger Memory is Getting Full 
 .
11. Data memory     88

<!-- Page 106 -->
11.2 Memory allocation
Data table SRAM and the CPU drive are automatically partitioned by the data logger. The USR 
drive can be partitioned as needed.  The CRD drive is automatically partitioned when a memory 
card is installed. 
The CPU and USR drives use the FAT file system. There is no limit, beyond practicality and 
available memory, to the number of files that can be stored. While a FAT file system is subject to 
fragmentation, performance degradation is not likely to be noticed since the drive has a relatively 
small amount of solid state RAM and is accessed very quickly.
11.3 SRAM
SRAM holds program variables, communications buffers, final-data memory, and, if allocated, 
the USR drive. An internal lithium battery retains this memory when primary power is removed.
The structure of the data logger SRAM memory is as follows:
- Static Memory: This is memory used by the operating system, regardless of the running 
program. This sector is rebuilt at power-up, program recompile, and watchdog events.
- Operating Settings and Properties: Also known as the "Keep" memory, this memory is used 
to store settings such as PakBus address, station name, beacon intervals, and allowed 
neighbor lists. This memory also stores dynamic properties such as known routes and 
communications timeouts.
- CRBasic Program Operating Memory: This memory stores the currently compiled and 
running user program. This sector is rebuilt on power-up, recompile, and watchdog events.
- Variables & Constants: This memory stores constants and public variables used by the 
CRBasic program. Variables may persist through power-up, recompile, and watchdog 
events if the PreserveVariables instruction is in the running program.
- Final-Data Memory: This memory stores data. Auto-allocated tables fill whatever memory 
remains after all other demands are satisfied. A compile error occurs if insufficient memory 
is available for user-allocated data tables. This memory is given lowest priority in SRAM 
memory allocation.
- Communication Memory 1: Memory used for construction and temporary storage of 
PakBus packets.
- Communication Memory 2: Memory used to store the list of known nodes and routes to 
nodes. Routers use more memory than leaf nodes because routes store information about 
11. Data memory     89

<!-- Page 107 -->
other routers in the network. You can increase the Communication Allocation field in 
Device Configuration Utility to increase this memory allocation.
- USR drive: Optionally allocated. Holds image files. Holds a copy of final-data memory when 
TableFile() instruction used. Provides memory for FileRead() and FileWrite() 
operations. Managed in File Control. Status reported in Status table fields USRDriveSize 
and USRDriveFree.
11.3.1 USR drive
Battery-backed SRAM can be partitioned to create a FAT USR drive, analogous to partitioning a 
second drive on a computer hard disk.  Certain types of files are stored to USR to reserve limited 
CPU drive memory for data logger programs and calibration files.  Partitioning also helps prevent 
interference from data table SRAM. The USR drive holds any file type within the constraints of the 
size of the drive and the limitations on filenames. Files typically stored include image files from 
cameras, certain configuration files, files written for FTP retrieval, HTML files for viewing with web 
access, and files created with the TableFile() instruction.  Measurement data can also be 
stored on USR as discrete files by using the TableFile() instruction. Files on USR can be 
collected using data logger support software Retrieve command in File Control, or automatically 
using the LoggerNet Setup > File Retrieval tab functions.
USR is not affected by program recompilation or formatting of other drives.  It will only be reset if 
the USR drive is formatted, a new operating system is loaded, or the size of USR is changed.  USR 
size is set manually by accessing it in the Settings Editor, or programmatically by loading a 
CRBasic program with a USR drive size entered in a SetSetting() instruction. Partition the 
USR drive to at least 11264 bytes in 512-byte increments.  If the value entered is not a multiple of 
512 bytes, the size is rounded up. Maximum size of USR 2990080 bytes.
11. Data memory     90

<!-- Page 108 -->
WARNING:
Partitioning or changing the size of the USR drive will delete stored data from tables. Collect 
data first.
NOTE:
Placing an optional USR size setting in the CRBasic program overrides manual changes to 
USR size.  When USR size is changed manually, the CRBasic program restarts and the 
programmed size for USR takes immediate effect.
Files in the USR drive can be managed through data logger support software File Control or 
through the  FileManage() instruction in CRBasic program.
11.4 Flash memory
The data logger operating system is stored in a separate section of flash memory. To update the 
operating system, see Updating the operating system (p. 157).
Serial flash memory holds the CPU drive, web page, and data logger settings. Because flash 
memory has a limited number of write/erase cycles, care must be taken to avoid continuously 
writing to files on the CPU drive.
11.4.1 CPU drive
The serial flash memory CPU drive contains data logger programs and other files. This memory is 
managed in File Control.
NOTE:
When writing to files under program control, take care to write infrequently to prevent 
premature failure of serial flash memory. Internal chip manufacturers specify the flash 
technology used in Campbell Scientific CPU: drives at about 100,000 write/erase cycles. While 
Campbell Scientific's in-house testing has found the manufacturers' specifications to be very 
conservative, it is prudent to note the risk associated with repeated file writes via program 
control.
Also, see System specifications (p. 252) for information on data logger memory.
11.5 MicroSD (CRD: drive)
The data logger has a microSD card slot for removable, supplemental memory. The card can be 
configured as an extension of the data logger final-data memory or as a repository of discrete 
data files. 
11. Data memory     91

<!-- Page 109 -->
 For additional information on when to use a MicroSD card, watch Fundamentals of CRBasic 
Programming Part 7: Determine if External Memory is Required/Intro to MicroSD 
 .
When storing high-frequency data, or when storing data to cards greater than 2 GB, TableFile
() with Option 64 is recommended to write final storage data to a card. In other applications 
CardOut() can be used to store data to a card.
NOTE:
Sub-folders are not supported.
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
The CRD: drive uses microSD cards exclusively. Campbell Scientific recommends and supports 
only the use of microSD cards obtained from Campbell Scientific. These cards are industrial-
grade and have passed Campbell Scientific hardware testing.  Use of consumer-grade cards 
substantially increases the risk of data loss.  Following are advantages Campbell Scientific cards 
have over less expensive commercial-grade cards:
- Verified compatibility with Campbell Scientific data loggers
- Less susceptible to failure and data loss
- Match the data logger operating temperature range
- Provide faster read/write times
- Include vibration and shock resistance
- Have longer life spans (more read/write cycles)
A "card controller error" indicates that the data logger has failed to communicate with the card. It 
is an error caused by the micro-controller built into the microSD card. Sometimes this error may 
be resolved by reformatting the card. If the error repeats itself, try an industrial-grade card. For 
more information on errors, see File system error codes (p. 199).
A maximum of 30 data tables can be created using CardOut() on a microSD card. When a data 
table is sent to a microSD card, a data table of the same name in SRAM is used as a buffer for 
transferring data to the card. Note that with TableFile(), the number of files stored on the 
card is controlled by the MaxFiles parameter.
When a new program is compiled that sends data to the card, the data logger checks if a card is 
present and if the card has adequate space for the data tables. If no card is present, or if space is 
inadequate, the data logger will warn that the card is not being used.  However, the CRBasic 
program runs anyway and data is stored to SRAM.  When a card is inserted later, data 
accumulated in the SRAM table is copied to the card.
11. Data memory     92

<!-- Page 110 -->
NOTE:
A card must be exchanged before it fills, or the oldest data will be overwritten, by incoming 
new records, and lost. During the card exchange, once the old card is removed, the new card 
must be inserted before the data table in data logger CPU memory rings, or data will be 
overwritten and lost.
A microSD card can also facilitate the use of powerup.ini (see File management via powerup.ini 
(p. 161) for more information). 
11.5.1 Formatting microSD cards
The data logger accepts microSD cards formatted as FAT16 or FAT32; however, FAT32 is 
recommended. Otherwise, some functionality, such as the ability to manage large numbers of 
files (>254) is lost. There are several ways to format cards such as using: File Control, CR1000KD, 
and Windows. Formatting on the data logger is recommended because this ensures correct 
FAT32 format.
11.5.2 MicroSD card precautions
Observe the following precautions when using optional memory cards:
- Before removing a card from the data logger, disable the card by pressing the Eject button 
and wait for the green LED. You then have 15 seconds to remove the card before normal 
operations resume.
- Do not remove a memory card while the drive is active, or data corruption and damage to 
the card may result.
- Prevent data loss by collecting data before sending a program.  Sending a program  to the 
data logger often erases all data.
- See System specifications (p. 252) for information on maximum card size.
11.5.3 Act LED indicator
When the data logger is powered and a microSD card installed, the Act (Activity) LED will turn on 
according to card activity or status:
- Red flash: Card read/write activity
- Solid green: This LED indicates it is OK to remove card. The Eject button must be pressed 
before removing a card to allow the data logger to store buffered data to the card and then 
power it off.
- Solid orange: Error
11. Data memory     93

<!-- Page 111 -->
- Dim/flashing orange: Card has been removed and has been out long enough that CPU 
memory has wrapped and data is being overwritten without being stored to the card.
11.5.4 Card data retrieval
Data stored on cards can be retrieved through a communications link to the data logger or by 
removing the card and carrying it to a computer with a card adapter. With large files, transferring 
the card to a computer may be faster than collecting the data over a communications link. 
 For additional information on card data retrieval, watch Fundamentals of CRBasic Programming 
Part 9: Retrieving Data from a MicroSD Card 
 .
CAUTION:
Removing a card while it is active can cause corrupted data and can damage the card. Always 
press the Eject button and wait for a green light before removing card. Do not switch off the 
data logger power if a card is present and active.
CAUTION:
File Control (in LoggerNet or PC400) should not be used to retrieve an open file (for example, 
a file created by using CardOut() or the latest file created by TableFile(), Option 64) 
from a card. Using File Control to retrieve the data can result in a corrupted data file. 
However, File Control can be used to retrieve closed files such as JPEG images or files (other 
than the latest) created by TableFile(), Option 64.
11.5.4.1 Via a communications link
Data can be copied to a computer via a communications link by using one of Campbell Scientific 
data logger support software packages (for example, LoggerNet or PC400). There is no need to 
distinguish whether the data is to be collected from the CPU memory or a card. The software 
package will look for data in both the CPU memory and the card.
The data logger manages data on a card as final-storage data, accessing the card as needed to 
fill data-collection requests initiated with the Collect button in data logger support software. If 
desired, binary data can be collected by using the File Control utility in data logger support 
software. Before collecting data this way, stop the data logger program to ensure data is not 
written to the card while data is retrieved; this will avoid data corruption.
Fast storage/data-collection constraints
Factors affecting how fast the data logger stores data include the data storage rate, number of 
table values, and number of tables. For more information, see Creating data tables in a program 
(p. 85).
11. Data memory     94

<!-- Page 112 -->
When data logger support software collects data from ring tables that have filled, there is the 
possibility of missing records due to the collection process. When a ring table has filled, the 
oldest data is overwritten by the newest data. LoggerNet and PC400 use a  collection algorithm 
that collects data from multiple tables in small blocks as they collect from all the tables. Collection 
starts with the oldest data for each table.
With filled ring tables, as collection begins, the data collection software queries the data logger 
for the oldest data starting with the first table. When this data block is returned, the software 
goes to the next table and so on until all of the tables are initially collected. By the time LoggerNet 
or PC400 make the second pass requesting more data from the tables, the possibility exists that 
some of that data may have been overwritten.
Normally,  data is collected without gaps; however, if the data logger is storing data fast enough, 
it is possible to get into an always-behind scenario where the data collection never catches up 
and the data logger repeatedly overwrites uncollected data.
CAUTION:
The possibility of missing records is greater when collecting data over high-latency 
communications links, such as RF or busy IP networks. This is due to the high demand of 
communications on processor time.
11.5.4.2 Card transport to computer
With large files, transferring the card to a computer may be faster than collecting the data over a 
communications link.
CAUTION:
Removing a card while it is active can cause corrupted data and can damage the card. Always 
press the Eject button and wait for a green light before removing card. Do not switch off the 
data logger power if a card is present and active.
To remove a card, first press the Eject button. The data logger will copy any buffered data to the 
card and then power the card off. The Act LED will turn green when it is OK to physically remove 
the card. The card will be reactivated after 15 seconds if it is not removed.
When the card is inserted into a computer, the data files can be copied to another drive or used 
directly from the card just as one would from any other disk. In most cases, however, it will be 
necessary to convert the file format before using the data.
Note that for both CardOut() and TableFile() Option 64, data is stored on the card in 
binary (TOB3) format. TOB3 is a binary format that incorporates features to improve reliability of 
cards. TOB3 format is different from the data file formats created when data is collected via a 
communications link, which is ASCII (TOA5) format. Hence, data files that are read directly from 
11. Data memory     95

<!-- Page 113 -->
the card need to be converted into another format to be human readable. You can convert files 
from binary or other formats using CardConvert software that is included in your data logger 
support software.
Converting file formats
 Use CardConvert to convert data to a different format. 
 1.  Open CardConvert.
- On the LoggerNet toolbar select the Data category.
- In PC400 select the Tools menu.
 2. Click Select Card Drive.
 3. Select where the files to be converted are stored and press OK.
 4. Click Change Output Dir and select where to store the converted files.
 5. Place check marks next to the files to be converted. A default destination filename is given. 
It can be changed by right-clicking with the filename highlighted. 
 6. Press Destination File Options to select what file format to convert to and other options. 
 7. Press Start Conversion to begin converting files. Green check marks will appear next to 
each filename as conversion is complete. Refer to the data logger support software manual 
or built-in CardConvert help for more information.
11. Data memory     96

<!-- Page 114 -->
Reinserting the card
If the same card is inserted again into the data logger, the data logger will store all data to the 
card that has been generated since the card was removed that is still in the CPU memory. If the 
data tables have been left on the card, new data will be appended to the end of the old files. If 
the data tables have been deleted, new ones will be created.
CAUTION:
Check the status of the card before leaving the data logger. If a card was not properly 
accepted, the LED will flash orange. In that case, reformat and erase all data contained on the 
card. Formatting or erasing a card might be done on a computer or data logger. See MicroSD 
(CRD: drive) (p. 91) for information on formatting a card.
Card swapping
When transporting a card to a computer to retrieve data, most users will want to use a second 
card to ensure that no data is lost. For this method of collection, use the following steps.
 1. Insert formatted card (“card-A”) into the data logger card slot. See Formatting microSD 
cards (p. 93).
 2. Send program containing TableFile() or CardOut() instruction(s).
 3. When ready to retrieve data (hours, days, or months later), press the Eject button. The LED 
will be red while the most-current data is stored to the card and then turn green. Remove 
the card while the LED is green.
 4. Insert the clean card (“card-B”).
 5. Use CardConvert to copy data from card-A to computer and convert. The default 
CardConvert filename will be TOA5_stationname_tablename.dat. Once the data is copied, 
use Windows Explorer to delete all data files from the card.
 6. At the next card swap, eject card-B, press the Eject button. The LED will be red while the 
most-current data is stored to the card and then turn green. Remove the card while the 
LED is green.
 7. Insert the clean card-A.
 8. Running CardConvert on card-B will result in separate data files containing records since 
card-A was ejected. CardConvert can increment the filename to TOA5_stationname_
tablename_0.dat.
11. Data memory     97

<!-- Page 115 -->
 9. The data files can be joined by using text editing software such as WordPad or a 
spreadsheet such as Excel.
CardConvert   file Card-A record numbers Card-B record numbers
TOA5_tablename.dat 0-100  
TOA5_tablename.dat   101-1234
TOA5_tablename.dat 1235-….  
11. Data memory     98
