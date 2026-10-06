---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 18-24
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 1–4: Giới thiệu, Thận trọng, Kiểm tra ban đầu & Thành phần phần cứng

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 18 đến 24).  
> **Chủ đề chính**: Tổng quan Datalogger CR1000X/CR1000Xe, cảnh báo an toàn điện/tĩnh điện, quy trình kiểm tra ban đầu và các thành phần chính của hệ thống đo.

---


<!-- Page 18 -->
1. Introduction
The CR1000X and CR1000Xe are our flagship data loggers that provide measurement and control 
for a wide variety of applications. Their reliability and ruggedness make them excellent choices 
for remote environmental applications, including weather stations, mesonet systems, wind 
profiling, air quality monitoring, hydrological systems, water quality monitoring, and 
hydrometeorological stations.
The CR1000X and CR1000Xe are low-powered devices that measure sensors, drive direct 
communications and telecommunications, analyze data, control external devices, and store data 
and programs in onboard, nonvolatile storage. The electronics are RF-shielded by a unique 
sealed, stainless-steel canister. A battery-backed clock assures accurate timekeeping. The 
onboard, BASIC-like programming language supports data processing and analysis routines.
The Getting Started Guide  
  provides an introduction to data acquisition and walks you 
through a procedure to set up a simple system. You may not find it necessary to progress beyond 
this. However, should you want to dig deeper into the complexity of the data logger functions or 
quickly look for details, extensive information is available in this and other Campbell Scientific 
manuals.
Additional Campbell Scientific publications are available online at www.campbellsci.com 
 . 
Video tutorials  are available at www.campbellsci.com/videos 
 . Generally, if a particular feature 
of the data logger requires a peripheral hardware device, more information is available in the 
manual written for that device.
1. Introduction     1

<!-- Page 19 -->
2. Precautions
READ AND UNDERSTAND the Safety section at the back of this manual. 
An authorized technician shall verify that the installation and use of this product is in accordance 
to the manufacturer’s instructions, recommendations and intended use.
Although the CR1000X/CR1000Xe is rugged, it should be handled as a precision scientific 
instrument.
Maintain a level of calibration appropriate to the application. Campbell Scientific recommends 
factory recalibration every three years.
2. Precautions     2

<!-- Page 20 -->
3. Initial inspection
Upon receiving the CR1000X/CR1000Xe, inspect the packaging and contents for damage. File 
damage claims with the shipping company. 
Immediately check package contents. Thoroughly check all packaging material for product that 
may be concealed. Check model numbers, part numbers, and product descriptions against the 
shipping documents. Model or part numbers are found on each product. Report any 
discrepancies to Campbell Scientific.
Check the CR1000X/CR1000Xe operating system version  as outlined in Updating the operating 
system (p. 157), and update as needed. CR1000X data loggers with Serial Numbers 34000 and 
newer have hardware requiring the use of OS version 5.02 or newer.
3. Initial inspection     3

<!-- Page 21 -->
4. CR1000X/CR1000Xe data 
acquisition system components
A basic data acquisition system consists of sensors, measurement hardware, and a computer with 
programmable software. The objective of a data acquisition system should be high accuracy, 
high precision, and  resolution as high as appropriate for a given application.
The components of a basic data acquisition system are shown in the following figure.
Following is a list of typical data acquisition system components:
- Sensors - Electronic sensors convert the state of a phenomenon to an electrical signal (see 
Sensors (p. 6) for more information).
- Data logger  - The data logger measures electrical signals or reads serial characters. It 
converts the measurement or reading to engineering units, performs calculations, and 
reduces data to statistical values. Data is stored in memory to await transfer to a computer 
by way of an external storage device or a communications link.
4. CR1000X/CR1000Xe data acquisition system components     4

<!-- Page 22 -->
- Data Retrieval and Communications - Data is copied (not moved) from the data logger, 
usually to a computer, by one or more methods using data logger support software. Most  
communications options are bi-directional, which allows programs and settings to be sent 
to the data logger. For more information, see Sending a program to the data logger (p. 43).
- Datalogger Support Software - Software retrieves data, sends programs, and sets settings. 
The software manages the communications link and has options for data display.
- Programmable Logic Control - Some data acquisition systems require the control of 
external devices to facilitate a measurement or to control a device based on 
measurements. This data logger is adept at programmable logic control. See 
Programmable logic control (p. 21) for more information.
- Measurement and Control Peripherals - Sometimes, system requirements exceed the 
capacity of the data logger. The excess can usually be handled by addition of input and 
output expansion modules.
4.1 The CR1000X/CR1000Xe Datalogger
The CR1000X/CR1000Xe is  used in a broad range of measurement and control projects. Rugged 
enough for extreme conditions and reliable enough for remote environments, it plays a critical 
role in numerous complex applications. Used in applications all over the world, it is a powerful 
core component for your data acquisition system.
4.1.1 Overview
The CR1000X/CR1000Xe data logger is the main part of a data acquisition system (see 
CR1000X/CR1000Xe data acquisition system components (p. 4) for more information). It has a 
central-processing unit (CPU), analog and digital measurement inputs, analog and digital 
outputs, and memory. An operating system (firmware) coordinates the functions of these parts in 
conjunction with the onboard clock and the CRBasic application program.
The CR1000X/CR1000Xe can simultaneously provide measurement and communications 
functions. Low power consumption allows the data logger to operate for extended time on a 
battery recharged with a solar panel, eliminating the need for ac power. The CR1000X/CR1000Xe 
temporarily suspends operations when primary power drops below 9.6 V, reducing the possibility 
of inaccurate measurements.
The electronics are RF shielded and protected by the sealed, stainless-steel canister, making the 
CR1000X/CR1000Xe economical, small, and very rugged. A battery-backed clock assures accurate 
timekeeping.
4. CR1000X/CR1000Xe data acquisition system components     5

<!-- Page 23 -->
4.1.2 Operations
The CR1000X/CR1000Xe measures almost any sensor with an electrical response, drives direct 
communications and telecommunications, reduces data to statistical values, performs 
calculations, and controls external devices. After measurements are made, data is stored in 
onboard, nonvolatile memory. Because most applications do not require that every 
measurement be recorded, the program usually combines several measurements into 
computational or statistical summaries, such as averages and standard deviations.
4.1.3 Programs
A program directs the data logger on how and when sensors are measured, calculations are 
made, data is stored, and devices are controlled. The application program for the 
CR1000X/CR1000Xe is written in CRBasic, a programming language that includes measurement, 
data processing, and analysis routines, as well as the standard BASIC instruction set. For simple 
applications, Short Cut, a user-friendly program generator, can be used to generate the program. 
See also:
- Creating a Short Cut data logger program (p. 40)
- https://www.campbellsci.com/videos/datalogger-programming 
For more demanding programs, use the full-featured CRBasic Editor. The CRBasic Editor help 
contains program structure details, instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
4.2 Sensors
Sensors transduce phenomena into measurable electrical forms by modulating voltage, current, 
resistance, status, or pulse output signals.  Suitable sensors do this with accuracy and precision.  
Smart sensors have internal measurement and processing components and simply output a 
digital value in binary, hexadecimal, or ASCII character form. 
Most electronic sensors, regardless of manufacturer, will interface with the data logger.  Some 
sensors require external signal conditioning.  The performance of some sensors is enhanced with 
specialized input modules. The data logger, sometimes with the assistance of various peripheral 
devices, can measure or read nearly all electronic sensor output types.
4. CR1000X/CR1000Xe data acquisition system components     6

<!-- Page 24 -->
The following list may not be comprehensive.  A library of sensor manuals and application notes is 
available at www.campbellsci.com/support 
  to assist in measuring many sensor types.
- Analog
- Voltage
- Current
- Strain
- Thermocouple
- Resistive bridge
- Pulse
- High frequency
- Switch-closure
- Low-level AC
- Quadrature
- Period average
- Vibrating wire (through interface modules)
- Smart sensors
- SDI-12
- RS-232
- Modbus
- DNP3
- TCP/IP 
- RS-422
- RS-485
4. CR1000X/CR1000Xe data acquisition system components     7
