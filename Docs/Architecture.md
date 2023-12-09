# Open Rails Architecture

This document will describe the overall structure of Open Rails and how we expect different areas of the program to work together.

## Player application model

The player application model describes the desired components and their relationships which make up Open Rails. Each of these will be formed from one or more libraries, as needed, and each library may contain distinct but critically linked subfunctions.

```mermaid
flowchart TB
  Formats["Orts.Formats"]
  Game["Orts.Game"]
  Input["Orts.Input"]
  Multiplayer["Orts.Multiplayer"]
  Parsers["Orts.Parsers"]
  Player["Player"]
  Simulation["Orts.Simulation"]
  Sound["Orts.Sound"]
  UI["Orts.UI"]
  Viewer["Orts.Viewer"]
  Web["Orts.Web"]
  Player --- Game --- UI --- Viewer --- Simulation & Formats
  Player --- Input --- UI & Simulation
  Sound --- Simulation --- Formats & Multiplayer & Web
  Formats --- Parsers
```

## Threading model

The threading in Open Rails has two key threads working together (Render and Updater) to simulate and render the world, with a number of auxiliary threads for other functions.

- Render process [main thread]
  - Read user input
  - Swap next/current frames
  - Resume Updater
  - Render current frame
  - Wait until Updater is finished
- Updater process
  - Suspended until restarted by Render
  - Every 250ms if Loader is suspended: check for anything to load and resume Loader
  - Run simulation
  - Prepare next frame for rendering
- Loader process
  - Suspended until restarted by Updater
  - Load content for simulation and rendering
- Sound process
  - Wait 50ms
  - Update all sound outputs (volumes, 3D position, etc.)
- Watchdog process
  - Every 1s: checks above processes are making forward progress
  - If a process stops responding for more than 10s (60s for Loader), the whole application is terminated with an error containing the hung process' stack trace
- Web Server process
  - Handle all web and API requests

## Simulator object relationships

This tree is a summary of the important object relationships (aggregation) inside the simulation. Each entry is a class whose instances can be accessed from the parent item.

- `Simulator`
  - `Activity`
  - `LevelCrossings`
  - `Signals`
  - `Train` (collection)
    - `TrainCar` (collection)
      - **Physics simulation**
      - `BrakeSystem`
      - `IPowerSupply` (interface)
      - `WheelAxle` (collection)
      - (child `MSTSWagon`) `MSTSCoupling`
      - (child `MSTSLocomotive`) `ScriptedBrakeController`
      - (child `MSTSLocomotive`) `ScriptedTrainControlSystem`
      - (child `MSTSDieselLocomotive`) `DieselEngines`
      - **Visual simulation**
      - `FreightAnimations`
      - `PassengerViewPoint` (collection)
      - `TrainCarPart` (collection)
      - `ViewPoint` (collection)
      - (child `MSTSWagon`) `IntakePoint` (collection)
      - (child `MSTSWagon`) `ParticleEmitterData` (collection)
      - (child `MSTSLocomotive`) `CabView` (collection)
      - (child `MSTSLocomotive`) `CabView3D`
  - `UserSettings`
  - `Weather`

## Simulator class relationships

This tree is a summary of the important class relationships (inheritance) inside the simulation. Each top-level entry is a separate hierarchy of classes.

- `BrakeSystem` (abstract)
  - `MSTSBrakeSystem` (abstract)
    - `AirSinglePipe`
      - `AirTwinPipe`
        - `EPBrakeSystem`
        - `SMEBrakeSystem`
      - `SingleTransferPipe`
    - `ManualBraking`
    - `VacuumSinglePipe`
      - `StraightVacuumSinglePipe`
- `IPowerSupply` (interface)
  - `ILocomotivePowerSupply` (interface)
    - `ScriptedLocomotivePowerSupply` (abstract)
      - `ScriptedControlCarPowerSupply`
      - `ScriptedDieselPowerSupply`
      - `ScriptedDualModePowerSupply`
      - `ScriptedElectricPowerSupply`
    - `SteamPowerSupply`
  - `IPassengerCarPowerSupply` (interface)
    - `ScriptedPassengerCarPowerSupply`
- `Train`
  - `AITrain`
    - `TTTrain`
- `TrainCar` (abstract)
  - `MSTSWagon`
    - `MSTSLocomotive`
      - `MSTSControlTrailerCar`
      - `MSTSDieselLocomotive`
      - `MSTSElectricLocomotive`
      - `MSTSSteamLocomotive`
