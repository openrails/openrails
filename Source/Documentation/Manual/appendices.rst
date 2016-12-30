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

