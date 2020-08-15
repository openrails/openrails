.. _appendices:

**********
Appendices
**********
.. _appendices-units-of-measure:

Units of Measure
================

Open Rails supports the same default units of measure as MSTS which are mostly, 
but not exclusively, metric.

When creating models just for Open Rails, we recommend you do not use defaults 
but specify units for all values that represent physical quantities.

As shown below, Open Rails provides a wider choice of units than MSTS.

.. tabularcolumns:: |p{3.5cm}|p{1cm}|p{2.5cm}|p{1.3cm}|p{1.3cm}|p{3.5cm}|

======================= =============== =========== =========== =============== ==============================
Measure                 Default unit    Applies to  OR accepts  MSTS accepts    Comment
======================= =============== =========== =========== =============== ==============================
Mass                    kg                          kg          kg                
\                                                   t           t               metric tonne (1000 kg)
\                                                   lb          lb                
\                                                   t-uk                        Imperial ton (2240 lb)
\                                                   t-us                        US ton (2000 lb)
Distance                                            mm
\                                                   cm          cm
\                       m                           m           m
\                                                   km
\                                                   in          in
\                                                   in/2        in/2            half-inch -- historic 
                                                                                unit for tyre diameters
\                                                   ft
\                                                   mile
Area                                                m^2
\                                                   \*(m^2)     \*(m^2)
\                       ft^2                        ft^2
\                                       \*(ft^2)    \*(ft^2)
Volume                  l               diesel fuel l                           liter
\                                                   m^3
\                                                   \*(m^3)
\                                                   in^3
\                                                   \*(in^3)
\                       ft^3            other       \*(ft^3)    \*(ft^3)        e.g. BoilerVolume
\                                                   g-uk                        Imperial gallons
\                                                   g-us                        US gallons
\                                                   gal                         US gallons
\                                                   gals        gals            US gallons
Time                    s                           s
\                                                   m
\                                                   h
Current                 amp                         amp
\                                                   A
Voltage                 volt                        V
\                                                   kV
Mass Flow                                           g/h
\                                                   kg/h
\                       lb/h                        lb/h        lb/h
Speed                   m/s             other       m/s         m/s             meter per second
\                                                   km/h
\                                                   kph         kph             kilometer per hour
\                                                   kmh         kmh             misspelling accepted by MSTS
\                                       kmph
\                       mph             dynamic     mph         mph             miles per hour 
                                        brake
Frequency               Hz                          Hz                          Hertz
\                                                   rps                         revolutions per second
\                                                   rpm
Force                   N                           N           N               Newton
\                                                   kN          kN
\                                                   lbf                         Pounds force
\                                                   lb
Power                   W                           W                           Watt
\                                                   kW
\                                                   hp                          horsepower
Stiffness               N/m                         N/m         N/m             Newton per meter
Resistance              N/m/s                       N/m/s       N/m/s           Newton per meter per second
\                                                   Ns/m                        Newton seconds per meter
Angular Resistance      N/rad/s                     N/rad/s
Pressure                psi             air         psi                         pounds per square inch
                                        pressure
\                                                   bar                         atmospheres
\                                                   kPa                         KiloPascal
\                       inHg            vacuum      inHg                        inches of mercury 
Pressure Rate of Change psi/s                       psi/s
\                                                   bar/s
\                                                   kpa/s
\                                                   inHg/s
Energy Density          kJ/kg                       kJ/kg                       kiloJoule per kilogram
\                                                   J/g
\                                                   btu/lb                      Board of Trade Units per pound
Temperature Difference  degC                        degC
\                                                   degF
Angle                   radians                     --
\                                                   deg
Angular Speed           rad/s                       --          rad/s
Other                                               --          lb/hp/h         e.g. CoalBurnage
======================= =============== =========== =========== =============== ==============================


.. _appendices-signal-functions:

Signal Functions
================

This is an overview of the functions available in OR for use in signal scripts, known as SIGSCRIPT functions.

Original MSTS Functions
-----------------------
The following are basic MSTS functions:

| BLOCK_STATE
| ROUTE_SET
| NEXT_SIG_LR
| NEXT_SIG_MR
| THIS_SIG_LR
| THIS_SIG_MR
| OPP_SIG_LR
| OPP_SIG_MR
| DIST_MULTI_SIG_MR
| SIG_FEATURE
| DEF_DRAW_STATE


Extended MSTS Functions
-----------------------
The following are extensions of basic MSTS functions.

| **NEXT_NSIG_LR(SIGFN_TYPE, N)**
| Extension of NEXT_SIG_LR
| Returns state of Nth signal of type SIGFN_TYPE.
  Note that state SIGASP_STOP is returned if any intermediate signal of type SIGFN_TYPE is set to that
  state.

| **DIST_MULTI_SIG_MR_OF_LR(SIGFN_TYPE, SIGFN_ENDTYPE)**
| Extension of DIST_MULTI_SIG_MR
| The original DIST_MULTI_SIG_MR excluded any heads for which the link (route_set) was not valid.
  However, when signals are routed through route-definition signals rather than through links, this
  exclusion fails and therefor the function does not return the correct state.
  This extended function checks all required heads on each signal, and uses the least restricted aspect on
  this signal as state for this signal. It returns the most restrictive state of the states determined in this
  manner for each intermediate signal until a signal of type SIGFN_ENDTYPE is found.

SIGNAL IDENT Functions
----------------------
When a function is called which requires information from a next signal, a search is performed along the
train’s route to locate the required signal. If multiple information is required from that signal, and
therefor multiple functions are called requiring that next signal, such a search is performed for each
function call.

This process can be made much more efficient by using the signal ident of the required signal. Each
signal in a route has a unique ident. A set of functions is available to obtain the signal ident of the
required signal. Also available are functions which are equivalent to normal signal functions, but use the
signal ident and do not perform a search for the required signal.
Obviously, using these functions it must be checked that the retrieved signal ident is valid (i.e. a valid
signal is found), and the integrity of the variable holding this ident must be ensured (the value must
never be altered).

The following functions are available to obtain the required signal ident.
The functions return the signal ident for the signal as found. If no valid signal is found, the value of -1 is
returned.

| **NEXT_SIG_ID(SIGFN_TYPE)**
| Returns Signal Ident of next signal of type SIGFN_TYPE.

| **NEXT_NSIG_ID(SIGFN_TYPE, N)**
| Returns Signal Ident of Nth signal of type SIGFN_TYPE.

| **OPP_SIG_ID(SIGFN_TYPE)**
| Returns Signal Ident next signal of type SIGFN_TYPE in opposite direction.

The following functions are equivalent to basic functions but use Signal Ident to identify the required
signal.

| **ID_SIG_ENABLED(SigID)**
| Returns 1 if the identified signal is actively enabled (i.e. a train has cleared a route leading to that signal)

| **ID_SIG_LR(SigId, SIGFN_TYPE)**
| Returns the least restricted aspect of the heads with type SIGFN_TYPE of the identified signal.

Note there are other functions which also use the signal ident as detailed below.

Signal SubObject functions
--------------------------
In the original MSTS signal definition, a number of specific Signal SubObjects could be used as flags
(USER_1 … USER_4). Other items (NUMBER_PLATE and GRADIENT) could also be used as flag but were
linked to physical items on the signal. The number of flags available in this way was very restricted.
In OR, an additional functions has been created which can check for any Signal SubObject if this
SubObject is included for this particular signal or not. This function can be used on any type of Signal
SubObject. By setting SubObjects of type ‘DECOR’, additional flags can be defined for any type of signal.
SubObjects defined in this manner need not be physically defined in the shape file. The information is
available at signal level, so all heads on a signal can use this information.
The function uses the SubObject number to identify the required SubObject, the name of the SubObject
is irrelevant. The maximum of total SubObjects for any shape is 32 (no. 0 … 31), this includes the actual
signal heads.

| **HASHEAD(N)**
| Returns ‘true’ (1) if SubObject with number N is available on this signal.

Approach Control Functions
--------------------------
Approach Control is a method used in some signalling systems which holds a signal at danger until the
approaching train is at a specific distance from the signal, or has reduced its speed to below a certain
limit. This functionality is used in situations where a significant reduction in speed is required for the
approaching train, and keeping the signal at danger ensures the train has indeed reduced its speed to
near or below the required limit.

The following set of Approach Control Functions is available in OR.

The required distance and speed can be set as constants (dimensions are m and m/s, these dimensions
are fixed and do not depend on any route setting).

It is also possible to define the required distance or speed in the signal type definition in sigcfg.dat. The
values defined in this way can be retrieved using the pre-defined variables **Approach_Control_Req_Position** and **Approach_Control_Req_Speed**.

| **APPROACH_CONTROL_POSITION(APPROACH_CONTROL_POSITION)**
| The signal will be held at danger until the train has reached the distance ahead of the signal as set.
  The signal will also be held at danger if it is not the first signal ahead of the train, even if the train is
  within the required distance.

| **APPROACH_CONTROL_POSITION_FORCED(APPROACH_CONTROL_POSITION)**
| The signal will be held at danger until the train has reached the distance ahead of the signal as set. The
  signal will also clear even if it is not the first signal ahead of the train.

| **APPROACH_CONTROL_SPEED(APPROACH_CONTROL_POSITION, APPROACH_CONTROL_SPEED)**
| The signal will be held at danger until the train has reached the distance ahead of the signal as set, and
  the speed has been reduced to below the required limit.
  The speed limit may be set to 0 in which case the train has to come to a stand in front of the signal
  before the signal will be cleared.
  The signal will also be held at danger if it is not the first signal ahead of the train, even if the train is
  within the required distance.

| **APPROACH_CONTROL_NEXT_STOP(APPROACH_CONTROL_POSITION, APPROACH_CONTROL_SPEED)**
| Sometimes, a signal may have approach control but the signal may be held at danger if the next signal is
  not cleared. Normally, if a signal is held for approach control, it will not propagate the signal request,
  meaning that the next signal will never clear. This could lead to a signal lockup, with the first signal held
  for approach control and therefor the next signal cannot clear.
  This function is specifically intended for that situation. It will allow propagation of the clear request even
  if the signal is held at danger for approach control, thus allowing the next signal to clear.
  The working of this function is similar to APPROACH_CONTROL_SPEED.

| **APPROACH_CONTROL_LOCK_CLAIM()**
| If a signal ahead of a train is held at danger, the train may claim sections beyond that signal in order to
  ensure a clear path from that signal as soon as possible. If this function is called in a script sequence 
  which also sets an active approach control, no claims will be made while the signal is held for approach
  control.

CallOn Functions
----------------
CallOn functions allow trains to proceed unto a track section already occupied by another train.
CallOn functions should not be confused with ‘permissive’ signals as often used in North American signal
systems.

A ‘permissive’ signal will always allow a train to proceed on occupied track, following a previous train.
Such signals are generally only used in situations where a signal covers a ‘free line’ section only, i.e. a
section of track without switches or crossings etc.

The CallOn facility, on the other hand, will only allow the train to proceed in certain specific situations
and is primarily used in station and yard areas.

The CallOn functions are specifically intended for use is timetable mode, and are linked directly to a
number of timetable commands. Trains will be allowed to proceed based on these commands.

CallOn functions in timetable mode
''''''''''''''''''''''''''''''''''
The following conditions will allow a train to proceed.

- The route beyond the signal leads into a platform, and the **$callon** parameter is set for the
  related station stop.
- The train has an **$attach**, **$pickup** or **$transfer** command set for a station stop or in the #dispose
  field, and the train in the section beyond the signal is static or is the train as referenced in the
  command (as applicable).
  If the command is set for a station stop, the route beyond the signal must lead into a platform
  allocated to that station.
  If the command is set in the #dispose field, there are no further conditions.
- The train action is part of a **$stable** command in the #dispose field.
- The route beyond the signal is a Pool Storage path, and the train is booked to be stored in that
  pool.

CallOn may also be allowed if the route does not lead into a platform depending on the function call.

CallOn functions in activity mode
'''''''''''''''''''''''''''''''''
CallOn is never allowed if the route beyond the signal leads into a platform.
CallOn may be allowed in other locations depending on the actual function call.

Available functions
'''''''''''''''''''
| **TRAINHASCALLON()**
| **TRAINHASCALLON_RESTRICTED()**
| These functions are similar, except that TRAINHASCALLON will always allow CallOn if the route does not
  lead into a platform, and therefor acts like a ‘permissive’ signal in that situation.
  The function TRAINHASCALLON_RESTRICTED will only allow CallOn when one of the criteria is
  met as detailed above.

SignalNumClearAhead Functions
-----------------------------
The SignalNumClearAhead (SNCA) value sets the number of signals ahead which a signal will need to
clear in order to be able to show the required least restrictive aspect.
The value is set as a constant for each specific signal type in the sigcfg.dat file.
However, it may be that certain signal options require that value to be changed.
For instance, a signal type which optionally can display an advance approach aspect, needs a higher
value for SNCA in case this advance approach is required. This may even depend on the route as set from
that signal. In OR, functions are available to adjust the value of SNCA as required, which prevents the
need to always set the possible highest value which could lead to a signal to clear a route too far ahead.
Note that these functions always use the default value of SNCA as defined in sigscr.dat as starting value.
Repeated calls of these functions will not lead to invalid or absurd values for SNCA.

| **INCREASE_SIGNALNUMCLEARAHEAD(n)**
| Increase the value of SNCA by n, starting from the default value.

| **DECREASE_SIGNALNUMCLEARAHEAD(n)**
| Decrease the value of SNCA by n, starting from the default value.

| **SET_SIGNALNUMCLEARAHEAD(n)**
| Set the value of SNCA to n.

| **RESET_SIGNALNUMCLEARAHEAD()**
| Reset the value of SNCA to the default value.