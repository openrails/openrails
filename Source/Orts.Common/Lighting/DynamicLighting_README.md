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
- For advanced control, add a `.or-lights.json` file with the same base name as your shape:
  - Example: `DYN_LIGHT_CUSTOM.s` + `DYN_LIGHT_CUSTOM.or-lights.json`
- Supported config properties:
  - `color` - RGB array (0-255) or color name string
  - `intensity` - Float value (default: 1.0)
  - `radius` - Float value in meters (default: 25.0)
  - `type` - "Point", "Spot", or "Directional"
  - `direction` - XYZ array (for Spot/Directional lights)
  - `spotAngle` - Float in degrees (for Spot lights, default: 45.0)
  - `schedule` - "Night_Only", "Day_Only", or "Always" (default: "Always")

#### Example Config (JSON)
```json
{
  "lights": [
    {
      "color": [255, 255, 200],
      "intensity": 1.5,
      "radius": 25.0,
      "type": "Point",
      "schedule": "Night_Only"
    },
    {
      "color": [255, 100, 100],
      "intensity": 2.0,
      "radius": 30.0,
      "type": "Spot",
      "direction": [0, -1, 0],
      "spotAngle": 45.0
    }
  ]
}
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
