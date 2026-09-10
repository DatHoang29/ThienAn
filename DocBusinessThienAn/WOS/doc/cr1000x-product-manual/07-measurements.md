---
tier: A
read: full
source: _source/pdf/cr1000x-product-manual.pdf
source_pages: 116-139
extracted: 2026-09-09
---

# CR1000X Product Manual — Chương 12: Kỹ thuật đo lường cảm biến khí tượng

> **Nguồn gốc**: Trích xuất từ `cr1000x-product-manual.pdf` (trang 116 đến 139).  
> **Chủ đề chính**: Đo điện áp đơn cực/vi sai (Single-ended & Differential), đo vòng lặp dòng điện công nghiệp 4–20 mA (RG1, RG2), đo điện trở/nhiệt độ PT100/RTD, đo xung tốc độ gió/vũ lượng mưa TB4, đo dây rung VSPECT và chế độ xử lý Pipeline/Sequential.

---


<!-- Page 116 -->
12. Measurements
12.1 Voltage measurements 99
12.2 Current-loop measurements 101
12.3 Resistance measurements 103
12.4 Thermocouple Measurements 110
12.5 Period-averaging measurements 111
12.6 Pulse measurements 112
12.7 Vibrating wire measurements 120
12.8 Sequential and pipeline processing modes 120
12.1 Voltage measurements
Voltage measurements are made using an Analog-to-Digital Converter (ADC). A high-
impedance Programmable-Gain Amplifier (PGA) amplifies the signal. Internal multiplexers route 
individual terminals within the amplifier. The CRBasic measurement instruction controls the ADC 
gain and configuration – either single-ended or differential input. Information on the differences 
between single-ended and differential measurements can be found here: Deciding between 
single-ended or differential measurements (p. 186).
A voltage measurement proceeds as follows:
 1. Set PGA gain for the voltage range selected with the CRBasic measurement instruction 
parameter Range. Set the ADC for the first notch frequency selected with fN1.
 2. If used, such as with bridge measurements, turn on excitation to the level selected with 
ExmV.
 3. Multiplex selected terminals (SEChan or DiffChan).
 4. Delay for the entered settling time (SettlingTime).
 5. Perform the analog-to-digital conversion.
 6. Repeat for input reversal as determined by parameters RevEx and RevDiff.
 7. Apply multiplier (Mult) and offset (Offset) to measured result.
12. Measurements     99

<!-- Page 117 -->
Conceptually, analog voltage sensors output two signals: high and low.  For example, a sensor 
that outputs 1000 mV on the high signal and 0 mV on the low has an overall output of 1000 mV.  A 
sensor that outputs 2000 mV on the high signal and 1000 mV on the low also has an overall 
output of 1000 mV.  Sometimes, the low signal is simply sensor ground (0 mV).  A single-ended 
measurement measures the high signal with reference to ground; the low signal is tied to 
ground.  A differential measurement measures the high signal with reference to the low signal.  
Each configuration has a purpose, but the differential configuration is usually preferred.
In general, use the smallest input range that accommodates the full-scale output of the sensor. 
This results in the best measurement accuracy and resolution (see Analog measurement 
specifications (p. 256) for more information).
A set overhead reduces the chance of overrange. Overrange limits are available in the 
specifications. The data logger indicates a measurement overrange by returning a NAN  for the 
measurement.
WARNING:
Sustained voltages in excess of ±20 V applied to terminals configured for analog input will 
damage CR1000X/CR1000Xe circuitry.
12.1.1 Single-ended measurements
A single-ended measurement measures the difference in voltage between the terminal 
configured for single-ended input and the reference ground. For example, single-ended channel 
1 is comprised of terminals SE 1 and 
 . Single-ended terminals are labeled in blue.  For more 
information, see Wiring panel and terminal functions (p. 8). The single-ended configuration is 
used with the following CRBasic instructions:
- VoltSE()
- BrHalf()
- BrHalf3W()
- TCSE()
- Therm107()
- Therm108()
- Therm109()
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
12. Measurements     100

<!-- Page 118 -->
12.1.2 Differential measurements
A differential measurement measures the difference in voltage between two input terminals. For 
example, DIFF channel 1 is comprised of terminals 1H and 1L, with 1H as high and 1L as low. For 
more information, see Wiring panel and terminal functions (p. 8). The differential configuration is 
used with the following CRBasic instructions:
- VoltDiff()
- BrFull()
- BrFull6W()
- BrHalf4W()
- TCDiff()
12.1.2.1 Reverse differential
Differential measurements have the advantage of an input reversal option, RevDiff. When 
RevDiff is set to  True, two differential measurements are made, the first with a positive 
polarity and the second reversed. Subtraction of opposite polarity measurements cancels some 
offset voltages associated with the measurement.
For more information on voltage measurements, see Improving voltage measurement quality (p. 
185) and Analog measurement specifications (p. 256).
12.2 Current-loop measurements
RG terminals can be configured to make analog current measurements using the CurrentSE() 
instruction. When configured to measure current, terminals each have an internal resistance of 
101 Ω in the current measurement loop. The return path of the sensor must be connected directly 
to the RG terminal. The following image shows a simplified schematic of a current measurement.
12. Measurements     101

<!-- Page 119 -->
12.2.1 Example current-loop measurement connections
The following table shows example schematics for connecting typical current sensors and 
devices. See also Current-loop measurement specifications (p. 260).
Sensor type Connection example
2-wire transmitter using data logger power
2-wire transmitter using external power
3-wire transmitter using data logger power
12. Measurements     102

<!-- Page 120 -->
Sensor type Connection example
3-wire transmitter using external power
4-wire transmitter using data logger power
4-wire transmitter using external power
12.3 Resistance measurements
Bridge resistance is determined by measuring the difference between a known voltage applied to 
the excitation (input) of a resistor bridge and the voltage measured on the output arm. The data 
logger supplies a precise voltage excitation via VX terminals. Return voltage is measured on 
12. Measurements     103

<!-- Page 121 -->
analog input terminals configured for single-ended (SE) or differential (DIFF) input. The result of 
the measurement is a ratio  of measured voltages.
See also Resistance measurement specifications (p. 259).
12.3.1 Resistance measurements with voltage excitation
CRBasic instructions for measuring resistance with voltage excitation include:
- BrHalf() - half bridge
- BrHalf3W() - three-wire half bridge
- BrHalf4W() - four-wire half bridge
- BrFull() - four-wire full bridge
- BrFull6W() - six-wire full bridge
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
Resistive-bridge type and
circuit diagram
CRBasic instruction and
fundamental relationship Relational formulas
Half Bridge1
CRBasic Instruction: 
BrHalf()
Fundamental Relationship:
Three Wire Half Bridge1,2
CRBasic Instruction: 
BrHalf3W()
Fundamental Relationship:
12. Measurements     104

<!-- Page 122 -->
Resistive-bridge type and
circuit diagram
CRBasic instruction and
fundamental relationship Relational formulas
Four Wire Half Bridge1,2
CRBasic Instruction: 
BrHalf4W()
Fundamental Relationship:
Full Bridge1,2
CRBasic Instruction: 
BrFull()
Fundamental Relationship:
These relationships apply 
to 
BrFull()
and BrFull6W()
Six Wire Full Bridge1
CRBasic Instruction: 
BrFull6W()
Fundamental Relationship:
1 Key: Vx = excitation voltage; V1, V2 = sensor return voltages; Rf = fixed, bridge or completion resistor; Rs = variable 
or sensing resistor.
2 Campbell Scientific offers terminal input modules to facilitate this measurement.
Offset voltage compensation applies to bridge measurements.  In addition to RevDiff and 
MeasOff parameters  discussed in Minimizing offset voltages (p. 195), CRBasic bridge 
12. Measurements     105

<!-- Page 123 -->
measurement instructions include the RevEx parameter that provides the option to program a 
second set of measurements with the excitation polarity reversed.  Much of the offset error 
inherent in bridge measurements is canceled out by setting RevDiff, RevEx, and MeasOff to 
True.
Measurement speed may be reduced when using RevDiff, MeasOff, and RevEx.  When more 
than one measurement per sensor is necessary, such as occurs with the BrHalf3W(), 
BrHalf4W(), and BrFull6W() instructions, input and excitation reversal are applied 
separately to each measurement.  For example, in the four-wire half-bridge (BrHalf4W()), 
when excitation is reversed, the differential measurement of the voltage drop across the sensor is 
made with excitation at both polarities and then excitation is again applied and reversed for the 
measurement of the voltage drop across the fixed resistor.  The results of the measurements (X) 
must then be processed further to obtain the resistance value, which requires additional program 
execution time.
CRBasic Example 2: Four-wire full-bridge measurement and processing
'This program example demonstrates the measurement and
'processing of a four-wire resistive full bridge.
'In this example, the default measurement stored
'in variable X is deconstructed to determine the
'resistance of the R1 resistor, which is the variable
'resistor in most sensors that have a four-wire
'full-bridge as the active element.
'Declare Variables
Public X
Public X_1
Public R_1
Public R_2 = 1000 'Resistance of fixed resistor R2
Public R_3 = 1000 'Resistance of fixed resistor R3
Public R_4 = 1000 'Resistance of fixed resistor R4
'Main Program
BeginProg
Scan(500,mSec,1,0)
'Full Bridge Measurement:
BrFull(X,1,mV250,1,Vx1,1,4000,True,True,0,60,1.0,0.0)
X_1 = ((-1 * X) / 1000) + (R_3 / (R_3 + R_4))
R_1 = (R_2 * (1 - X_1)) / X_1
NextScan
EndProg
12. Measurements     106

<!-- Page 124 -->
12.3.2 RTD and PRT
RTDs (resistance temperature detectors) are resistive devices made of platinum, nickel, copper, 
or other material. Platinum RTDs, known as PRTs (platinum resistance thermometers) are very 
accurate temperature measurement sensors.
A PRT element is a specialized resistor with two connection points. Most PRTs are either 100 Ω or 
1000 Ω. This number is the resistance the PRT has at 0 °C. The resistance of a PRT increases as it is 
warmed. Industry standards define how PRTs respond to temperature.
BrHalf4W() or CDM_BrHalf4W() in combination with PRTCalc() are the recommended 
CRBasic instructions for measuring RTDs. 
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
12.3.3 Strain measurements
A principal use of the four-wire full bridge is the measurement of strain gages in structural stress 
analysis.  StrainCalc() calculates microstrain (µɛ) from the formula for the specific bridge 
configuration used.  All strain gages supported by StrainCalc() use the full-bridge schematic. 
'Quarter-bridge', 'half-bridge' and 'full-bridge' refer to the number of active elements in the 
bridge schematic.  In other words, a quarter-bridge strain gage has one active element, a half-
bridge has two, and a full-bridge has four.
StrainCalc() requires a bridge-configuration code.  The following table shows the equation 
used by each configuration code.  Each code can be preceded by a dash (-).  Use a code without 
the dash when the bridge is configured so the output decreases with increasing strain.  Use a 
dashed code when the bridge is configured so the output increases with increasing strain.  A 
dashed code sets the polarity of Vr to negative.
12. Measurements     107

<!-- Page 125 -->
Table 12-1: StrainCalc() configuration codes
BrConfig code Configuration
1
Quarter-bridge strain gage:
2
Half-bridge strain gage. One gage parallel to strain, the other at 90° 
to strain:
3
Half-bridge strain gage. One gage parallel to +ɛ, the other parallel 
to -ɛ:
4
Full-bridge strain gage. Two gages parallel to +ɛ, the other two 
parallel to -ɛ:
12. Measurements     108

<!-- Page 126 -->
Table 12-1: StrainCalc() configuration codes
BrConfig code Configuration
5
Full-bridge strain gage. Half the bridge has two gages parallel to +ɛ  
and -ɛ, and the other half to +νɛ and -νɛ
6
Full-bridge strain gage. Half the bridge has two gages parallel to +ɛ  
and -νɛ , and the other half to -νɛ and +ɛ:
 Where: 
ν : Poisson's Ratio (0 if not applicable).
GF: Gage Factor.
Vr: 0.001 (Source-Zero) if BRConfig code is positive (+).
Vr: –0.001 (Source-Zero) if BRConfig code is negative (–).
and where:
"source": the result of the full-bridge measurement (X = 1000 • V1 / Vx) when multiplier = 1 and offset = 0.
"zero": gage offset to establish an arbitrary zero.
12.3.4 AC excitation
Some resistive sensors require AC excitation.  AC excitation is defined as excitation with equal 
positive (+) and negative (–) duration and magnitude.  These include electrolytic tilt sensors, soil 
moisture blocks, water-conductivity sensors, and wetness-sensing grids.  The use of single 
polarity DC excitation with these sensors can result in polarization of sensor materials and the 
substance measured.  Polarization may cause erroneous measurement, calibration changes, or 
rapid sensor decay.
Other sensors, for example, LVDTs (linear variable differential transformers), require AC excitation 
because they require inductive coupling to provide a signal.  DC excitation in an LVDT will result in 
no measurement.
CRBasic bridge-measurement instructions have the option to reverse polarity to provide AC 
excitation by setting the RevEx parameter to True.
12. Measurements     109

<!-- Page 127 -->
NOTE:
Take precautions against ground loops when measuring sensors that require AC excitation. 
See also Ground loops (p. 181).
For more information, see Accuracy for resistance measurements (p. 110).
12.3.5 Accuracy for resistance measurements
Consult the following technical papers for in-depth treatments of several topics addressing 
voltage measurement quality:
- Preventing and Attacking Measurement Noise Problems 
- Benefits of Input Reversal and Excitation Reversal for Voltage Measurements 
- Voltage Measurement Accuracy, Self- Calibration, and Ratiometric Measurements 
NOTE:
Error discussed in this section and error-related specifications of the CR1000X/CR1000Xe do 
not include error introduced by the sensor, or by the transmission of the sensor signal to the 
data logger.
For accuracy specifications of ratiometric resistance measurements, see Resistance measurement 
specifications (p. 259). Voltage measurement is variable V1 or V2 in resistance measurements. 
Offset is the same as that for simple analog voltage measurements.
Assumptions that support the ratiometric-accuracy specification include:
- Data logger is within factory calibration specification.
- Input reversal for differential measurements and excitation reversal for excitation voltage 
are within specifications.
- Effects due to the following are not included in the specification:
- Bridge-resistor errors
- Sensor noise
- Measurement noise
12.4 Thermocouple Measurements
Thermocouple measurements are special case voltage measurements.
NOTE:Thermocouples are inexpensive and easy to use.  However, they pose several 
challenges to the acquisition of accurate temperature data, particularly when using external 
reference junctions. 
12. Measurements     110

<!-- Page 128 -->
A thermocouple consists of two wires, each of a different metal or alloy, joined at one end to 
form the measurement junction. At the opposite end, each wire connects to terminals of a 
voltage measurement device, such as the data logger. These connections form the reference 
junction. If the two junctions (measurement and reference) are at different temperatures, a 
voltage proportional to the difference is induced in the wires.  This phenomenon is known as the 
Seebeck effect.
Measurement of the voltage between the positive and negative terminals of the voltage-
measurement device provides a direct measure of the temperature difference between the 
measurement and reference junctions. A third metal (for example, solder or data logger 
terminals) between the two dissimilar-metal wires form parasitic-thermocouple junctions, the 
effects of which cancel if the two wires are at the same temperature. Consequently, the two wires 
at the reference junction are placed in close proximity so they remain at the same temperature.
Knowledge of the reference junction temperature provides the determination of a reference 
junction compensation voltage, corresponding to the temperature difference between the 
reference junction and 0°C. This compensation voltage, combined with the measured 
thermocouple voltage, can be used to compute the absolute temperature of the thermocouple 
junction.
TCDiff() and TCSE() thermocouple instructions determine thermocouple temperatures 
using the following sequence. First, the temperature (°C) of the reference junction is determined. 
Next, a reference junction compensation voltage is computed based on the temperature 
difference between the reference junction and 0°C. If the reference junction is the data logger 
analog-input terminals, the temperature is conveniently measured with the PanelTemp() 
instruction. The actual thermocouple voltage is measured and combined with the reference 
junction compensation voltage. It is then used to determine the thermocouple-junction 
temperature based on a polynomial approximation of NIST thermocouple calibrations.
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
12.5 Period-averaging measurements
Use PeriodAvg() to measure the period (in microseconds) or the frequency (in Hz) of a signal 
on a single-ended channel.  For these measurements, the data logger uses a high-frequency 
digital clock to measure time differences between signal transitions, whereas pulse-count 
measurements simply accumulate the number of counts. As a result, period-average 
measurements offer much better frequency resolution per measurement interval than pulse-
count measurements. See also Pulse measurements (p. 112).
SE terminals on the data logger are configurable for measuring the period of a signal. 
12. Measurements     111

<!-- Page 129 -->
The measurement is performed as follows: low-level signals are amplified prior to a voltage 
comparator.  The internal voltage comparator is referenced to the programmed threshold.  The 
threshold parameter allows referencing the internal voltage comparator to voltages other than 
0 V.  For example, a threshold of 2500 mV allows a 0 to 5 VDC digital signal to be sensed by the 
internal comparator without the need for additional input conditioning circuitry.  The threshold 
allows direct connection of standard digital signals, but it is not recommended for small-
amplitude sensor signals.
A threshold other than zero results in offset voltage drift, limited accuracy (approximately 
±10 mV) and limited resolution (approximately 1.2 mV).
See also Period-averaging measurement specifications (p. 259).
TIP:
Both pulse count and period-average measurements are used to measure frequency output 
sensors. However, their measurement methods are different. Pulse count measurements use 
dedicated hardware - pulse count accumulators, which are always monitoring the input 
signal, even when the data logger is between program scans. In contrast, period-average 
measurements use program instructions that only monitor the input signal during a program 
scan. Consequently, pulse count scans can occur less frequently than period-average scans.  
Pulse counters may be more susceptible to low-frequency noise because they are always 
"listening", whereas period-averaging measurements may filter the noise by reason of being 
"asleep" most of the time. 
Pulse count measurements are not appropriate for sensors that are powered off between 
scans, whereas period-average measurements work well since they can be placed in the scan 
to execute only when the sensor is powered and transmitting the signal.
12.6 Pulse measurements
The output signal generated by a pulse sensor is a series of voltage waves. The sensor couples its 
output signal to the measured phenomenon by modulating wave frequency.  The data logger 
detects the state transition as each wave varies between voltage extremes (high-to-low or low-
to-high).  Measurements are processed and presented as counts, frequency, or timing data. Both 
pulse count and period-average measurements are used to measure frequency-output sensors. 
For more information, see Period-averaging measurements (p. 111).
12. Measurements     112

<!-- Page 130 -->
The data logger includes terminals that are configurable for pulse input as shown in the following 
image. 
Table 12-2: Pulse input terminals and the input types they can measure
Input type Pulse input terminal
High-frequency
P1
P2
C (all)
Low-level AC P1
P2
Switch-closure
P1
P2
C (all)
Using the PulseCount() instruction, P C terminals are configurable for pulse input to measure 
counts or frequency. Maximum input frequency is dependent on signal voltage. If pulse input 
voltages exceed the maximum voltage, third-party external-signal conditioners should be 
employed.  Do not measure  voltages greater than 2019 V.
12. Measurements     113

<!-- Page 131 -->
NOTE:
Conflicts can occur when a control port pair is used for different instructions (TimerInput(), 
PulseCount(), SDI12Recorder(), WaitDigTrig()). For example, if C1 is used for 
SDI12Recorder(), C2 cannot be used for TimerInput(), PulseCount(), or 
WaitDigTrig().
Terminals configured for pulse input have internal filters that reduce electronic noise, and thus 
reduce false counts. Internal AC coupling is used to eliminate DC offset voltages. For tips on 
working with pulse measurements, see Pulse measurement tips (p. 119).
Output can be recorded as counts, frequency or a running average of frequency.
For more information, see              Pulse measurement specifications (p. 260).
See the CRBasic Editor help for detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
12.6.1 Low-level AC measurements
Low-level AC (alternating current or sine-wave) signals can be measured on P terminals. AC 
generator anemometers typically output low-level AC.
Measurement output options include the following:
- Counts
- Frequency (Hz)
- Running average
Rotating magnetic-pickup sensors commonly generate AC voltage ranging from millivolts  at 
low-rotational speeds to several volts at high-rotational speeds.
CRBasic instruction: PulseCount(). See the CRBasic Editor help for detailed instruction 
information and program examples: https://help.campbellsci.com/crbasic/cr1000x/ 
 .
Low-level AC signals cannot be measured directly by C terminals. Peripheral terminal expansion 
modules, such as the Campbell Scientific LLAC4, are available for converting low-level AC signals 
to square-wave signals measurable by C terminals.
For more information, see Pulse measurement specifications (p. 260).
12.6.2 High-frequency measurements
High-frequency (square-wave) signals can be measured on terminals: 
- P or C
12. Measurements     114

<!-- Page 132 -->
Common sensors that output high-frequency pulses include:
- Photo-chopper anemometers
- Flow meters
Measurement output optionss include counts, frequency in hertz, and running average.  Note 
that the resolution of a frequency measurement can be different depending on the terminal used 
in the PulseCount() instruction. See the CRBasic help for more information.
The data logger has built-in pull-up and pull-down resistors for different pulse measurements 
which can be accessed using the PulseCount() instruction. Note that pull down options are 
usually used for sensors that source their own power.
12.6.2.1 P terminals
- CRBasic instruction: PulseCount()
High-frequency pulse inputs are routed to an inverting CMOS input buffer with input hysteresis. 
See Pulse measurement specifications (p. 260) for more information.
12.6.2.2 C terminals
- CRBasic instructions: PulseCount()
See Pulse measurement specifications (p. 260) for more information.
12.6.3 Switch-closure and open-collector measurements
Switch-closure and open-collector (also called current-sinking) signals can be measured on 
terminals:
- P or C
Mechanical switch-closures have a tendency to bounce before solidly closing.  Unless filtered, 
bounces can cause multiple counts per event.  The data logger automatically filters bounce.  
Because of the filtering, the maximum switch-closure frequency is less than the maximum high-
frequency measurement frequency.  Sensors that commonly output a switch-closure or an open-
collector signal include:
- Tipping-bucket rain gages
- Switch-closure anemometers
- Flow meters
12. Measurements     115

<!-- Page 133 -->
The data logger has built-in pull-up and pull-down resistors for different pulse measurements 
which can be accessed using the PulseCount() instruction. Note that pull down options are 
usually used for sensors that source their own power.
Data output options include counts, frequency (Hz), and running average.
12.6.3.1 P Terminals
An internal 100 kΩ pull-up resistor pulls an input to 5 VDC with the switch open, whereas a 
switch-closure to ground pulls the input to 0 V. 
- CRBasic instruction: PulseCount(). See the CRBasic Editor help for detailed instruction 
information and program examples: https://help.campbellsci.com/crbasic/cr1000x/ 
 .
Switch Closure on P Terminal Open Collector on P Terminal
12.6.3.2 C terminals
Switch-closure mode is a special case edge-count function that measures dry-contact switch-
closures or open collectors. The operating system filters bounces.
- CRBasic instruction: PulseCount().
See also Pulse measurement specifications (p. 260).
12.6.4 Edge timing and edge counting
Edge time, period, and counts can be measured on P or  C terminals. Feedback control using 
pulse-width modulation (PWM) is an example of an edge timing application.
12. Measurements     116

<!-- Page 134 -->
12.6.4.1 Single edge timing
A single edge or state transition can be measured on C terminals. Measurements can be 
expressed as a time (µs), frequency (Hz) or period (µs). 
CRBasic instruction: TimerInput()
12.6.4.2 Multiple edge counting
Time between edges, time from an edge on the previous terminal, and edges that span the scan 
interval can be measured on  C terminals. Measurements can be expressed as a time (µs), 
frequency (Hz) or period (µs). 
- CRBasic instruction: TimerInput()
12.6.4.3 Timer input NAN conditions
NAN is the result of a TimerInput() measurement if one of the following occurs:
- Measurement timer expires
- The signal frequency is too fast
For more information, see:
- Pulse measurement specifications (p. 260)
- Digital input/output specifications (p. 262)
- Period-averaging measurement specifications (p. 259)
12.6.5 Quadrature measurements
The Quadrature() instruction is used to measure shaft or rotary encoders. A shaft encoder 
outputs a signal to represent the angular position or motion of the shaft. Each encoder will have 
two output signals, an A line and a B line. As the shaft rotates the A and B lines will generate 
digital pulses that can be read, or counted, by the data logger.
In the following example, channel A leads channel B, therefore the encoder is determined to be 
moving in a clockwise direction. If channel B led channel A, it would be determined that the 
encoder was moving in a counterclockwise direction.
12. Measurements     117

<!-- Page 135 -->
Terminals C1-C8 can be configured as digital  pairs to monitor the two channels of an encoder. 
The  Quadrature() instruction can return:
- The accumulated number of counts from channel A and channel B. Count will increase if 
channel A leads channel B. Count will decrease if channel B leads channel A.
- The net direction.
- Number of counts in the A-leading-B direction.
- Number of counts in the B-leading-A direction.
Counting modes:
- Counting the increase on rising edge of channel A when channel A leads channel B. 
Counting the decrease on falling edge of channel A when channel B leads channel A.
- Counting the increase at each rising and falling edge of channel A when channel A leads 
channel B. Counting the decrease at each rising and falling edge of channel A when 
channel A leads channel B.
- Counting the increase at each rising and falling edge of both channels when channel A 
leads channel B. Counting the decrease at each rising and falling edge of both channels 
when channel B leads channel A.
For more information, see Digital input/output specifications (p. 262).
12. Measurements     118

<!-- Page 136 -->
12.6.6 Pulse measurement tips
The PulseCount() instruction uses dedicated 24-bit counters to accumulate all counts over 
the programmed scan interval. The resolution of pulse counters is one count.  Counters are read 
at the beginning of each scan and then cleared.  Counters will overflow if accumulated counts 
exceed 16,777,216 (224), resulting in erroneous measurements. See the CRBasic Editor help for 
detailed instruction information and program examples: 
https://help.campbellsci.com/crbasic/cr1000x/ 
 .
Counts are the preferred PulseCount() output option when measuring the number of tips 
from a tipping-bucket rain gage or the number of times a door opens.  Many pulse-output 
sensors, such as anemometers and flow meters, are calibrated in terms of frequency (Hz) so are 
usually measured using the PulseCount() frequency-output option.
Use the LLAC4  module to convert non-TTL-level signals, including low-level AC signals, to TTL 
levels for input to C terminals 
Conflicts can occur when a control port pair is used for different instructions (TimerInput(), 
PulseCount(), SDI12Recorder(), WaitDigTrig()). For example, if C1 is used for 
SDI12Recorder(), C2 cannot be used for TimerInput(), PulseCount(), or 
WaitDigTrig().
Understanding the signal to be measured and compatible input terminals and CRBasic 
instructions is helpful. See Pulse input terminals and the input types they can measure (p. 113).
12.6.6.1 Input filters and signal attenuation
Terminals configured for pulse input have internal filters that reduce electronic noise. The 
electronic noise can result in false counts. However, input filters attenuate (reduce) the amplitude 
(voltage) of the signal.  Attenuation is a function of the frequency of the signal. Higher-frequency 
signals are attenuated more. If a signal is attenuated too much, it may not pass the detection 
thresholds required by the pulse count circuitry. See Pulse measurement specifications (p. 260) 
for more information. The listed pulse measurement specifications account for attenuation due 
to input filtering.
12.6.6.2 Pulse count resolution
Longer scan intervals result in better resolution. PulseCount() resolution is 1 pulse per scan. 
On a 1 second scan, the resolution is 1 pulse per second. The resolution on a 10 second scan 
interval is 1 pulse per 10 seconds, which is 0.1 pulses per second. The resolution on a 100 
millisecond interval is 10 pulses per second.
12. Measurements     119

<!-- Page 137 -->
For example, if a flow sensor outputs 4.5 pulses per second and you use a 1 second scan, one 
scan will have 4 pulses and the next 5 pulses. Scan to scan, the flow number will bounce back and 
forth.  If you did a 10 second scan (or saved a total to a 10 second table), you would get 45 pulses. 
The total is 45 pulses for every 10 seconds. An average will correctly show 4.5 pulses per second. 
You wouldn't see the reading bounce on the longer time interval.
12.7 Vibrating wire measurements
The data logger can measure vibrating wire sensors through vibrating-wire interface modules. 
Vibrating wire sensors are the sensor of choice in many environmental and industrial applications 
that need sensor stability over very long periods, such as years or even decades. A thermistor 
included in most sensors can be measured to compensate for temperature errors. 
12.7.1 VSPECT™
Measuring the resonant frequency by means of period averaging is the classic technique, but 
Campbell Scientific has developed static and dynamic spectral-analysis techniques (VSPECT) that 
produce superior noise rejection, higher resolution, diagnostic data, and, in the case of dynamic 
VSPECT, measurements up to 333.3 Hz. For detailed information on VSPECT, see Vibrating Wire 
Spectral Analysis Technology 
 .
12.8 Sequential and pipeline processing modes
The data logger has two processing modes: sequential mode and pipeline mode.  In sequential 
mode, data logger tasks run more or less in sequence.    In pipeline mode, data logger tasks run 
more or less in parallel. Mode information is included in a message returned by the data logger, 
which is displayed by software when the program is sent and compiled, and it is found in the 
Status Table, CompileResults field.  The CRBasic Editor pre-compiler returns a similar message.
The default mode of operation is pipeline mode. However, when the data logger program is 
compiled, the data logger analyzes the program instructions and automatically determines which 
mode to use. The data logger can be forced to run in either mode by placing the 
PipeLineMode or SequentialMode instruction at the beginning of the program (before the 
BeginProg instruction). 
For additional information, visit the Campbell Scientific blog article, "Understanding CRBasic 
Program Compile Modes: Sequential and Pipeline 
 ." Or watch an instructional video 
at: http://www.campbellsci.com/videos/pipeline-sequential 
 .
12. Measurements     120

<!-- Page 138 -->
12.8.1 Sequential mode
Sequential mode executes instructions in the sequence in which they are written in the program. 
After a measurement is made, the result is converted to a value determined by processing 
arguments that are included in the measurement instruction, and then program execution 
proceeds to the next instruction. This line-by-line execution allows writing conditional 
measurements into the program.
NOTE:
The exact time at which measurements are made in sequential mode may vary if other 
measurements or processing are made conditionally, if there is heavy communications 
activity, or if other interrupts occur (such as accessing a Campbell Scientific memory card).
12.8.2 Pipeline mode
Pipeline mode handles measurement, most digital, and processing tasks separately, and, in many 
cases, simultaneously. Measurements are scheduled to execute at exact times and with the 
highest priority, resulting in more precise timing of measurements, and usually more efficient 
processing and power consumption.
In pipeline mode, it will take less time for the data logger to execute each scan of the program. 
However, because processing can lag behind measurements, there could be instances, such as 
when turning on a sensor using the SW12() instruction, that the sensor might not be on at the 
correct time to make the measurement.
Pipeline scheduling requires that the program be written such that measurements are executed 
every scan. Because multiple tasks are taking place at the same time, the sequence in which the 
instructions are executed may not be in the order in which they appear in the program. 
Therefore, conditional measurements are not allowed in pipeline mode. Because of the precise 
execution of measurement instructions, processing in the current scan (including updating public 
variables and data storage) is delayed until all measurements are complete. Some processing, 
such as transferring variables to control instructions, like PortSet() and ExciteV(), may not 
be completed until the next scan.
When a condition is true for a task to start, it is put in a queue. Because all tasks are given the 
same priority, the task is put at the back of the queue. Every 1 ms (or faster if a new task is 
triggered) the task currently running is paused and put at the back of the queue, and the next 
task in the queue begins running. In this way, all tasks are given equal processing time by the 
data logger.
12. Measurements     121

<!-- Page 139 -->
12.8.3 Slow Sequences
Priority of a slow sequence (SlowSequence) in the data logger will vary, depending upon 
whether the data logger is executing its program in pipeline mode or sequential mode. With the 
important exception of measurements, when running in pipeline mode all sequences in the 
program have the same priority. When running in sequential mode, the main scan has the 
highest priority for measurements, followed by background calibration (which is automatically 
run in a slow sequence), then the first slow sequence, the second slow sequence, and so on. The 
effects of this priority are negligible; however, since, once the tasks begin running, each task is 
allotted a 1 msec time slice, after which, the next task in the queue runs for 1 msec. The data 
logger cycles through the queue until all instructions for all sequences are complete.
12. Measurements     122
