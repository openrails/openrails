# Open Rails Dynamic Lighting System

## Overview
This system allows creators to place dynamic, real-time light sources in routes, similar to Railworks. Lights can be placed using object name prefixes or advanced config files for full control.

## How to Place Dynamic Lights

### 1. Simple Placement (Name Prefix)
- Place a scenery object with a name starting with one of the following prefixes:
  - `DYN_LIGHT_`
  - `LIGHT_`
  - `OR_LIGHT_`
- Example filenames:
  - `DYN_LIGHT.s` (default white light)
  - `DYN_LIGHT_RED.s` (red light)
  - `DYN_LIGHT_WARM.s` (warm white)
  - `DYN_LIGHT_STATION.s` (station preset)

### 2. Advanced Placement (Config File)
- For advanced control, add a `.cfg` file with the same base name as your shape:
  - Example: `DYN_LIGHT_CUSTOM.s` + `DYN_LIGHT_CUSTOM.cfg`
- Supported config properties:
  - `Color: 255,255,200` (RGB, 0-255)
  - `Intensity: 1.5`
  - `Radius: 25.0`
  - `Type: Point|Spot|Directional`
  - `Schedule: Night_Only` (future use)

#### Example Config
```
Color: 255,255,200
Intensity: 1.5
Radius: 25.0
Type: Point
Schedule: Night_Only
```

## How It Works
- The loader detects objects with the above prefixes and creates a `DynamicLight` instead of a visible object.
- If a `.cfg` file is present, its properties override defaults.
- Up to 4 dynamic lights are passed to the rendering pipeline and used in real-time lighting.

## Backward Compatibility
- Routes without dynamic lights or configs work as before.
- Creators can start simple and add advanced configs as needed.

## Performance
- Only the nearest 4 dynamic lights are active at once (configurable in code).
- Future: culling, LOD, and multiplayer sync can be added.

## For Developers
- See `ORTS.Common.Lighting.DynamicLight` and `LightManager` for core logic.
- See `Scenery.cs` for loader integration.
- See `Materials.cs` and shaders for rendering integration.

---
Open Rails Dynamic Lighting, 2025
