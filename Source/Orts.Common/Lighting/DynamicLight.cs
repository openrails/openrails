using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace ORTS.Common.Lighting
{
    /// <summary>
    /// Represents a dynamic, real-time light source in the world.
    /// </summary>
    public class DynamicLight
    {
        public enum LightType
        {
            Point,
            Spot,
            Directional
        }

        public Vector3 Position { get; set; }
        public Vector3 Color { get; set; } // RGB, 0-1
        public float Intensity { get; set; }
        public float Radius { get; set; }
        public LightType Type { get; set; }
        public bool Active { get; set; }
        public Vector3 Direction { get; set; } // For spot/directional
        public float SpotAngle { get; set; } // For spotlights (degrees)

        public DynamicLight(Vector3 position, Vector3 color, float intensity, float radius, LightType type = LightType.Point)
        {
            Position = position;
            Color = color;
            Intensity = intensity;
            Radius = radius;
            Type = type;
            Active = true;
            Direction = Vector3.Zero;
            SpotAngle = 45f;
        }

        public DynamicLight()
        {
            Position = Vector3.Zero;
            Color = new Vector3(1, 1, 1);
            Intensity = 2.0f; // Changed default from 1.0f to 2.0f
            Radius = 10.0f;
            Type = LightType.Point;
            Active = true;
            Direction = Vector3.Zero;
            SpotAngle = 45f;
        }

        public void LoadFromConfig(string path)
        {
            Trace.WriteLine($"[DYN_LIGHT] Loading config: {path}");
            try
            {
                var lines = System.IO.File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("Color:", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Substring(6).Split(',');
                        if (parts.Length == 3)
                        {
                            Color = new Microsoft.Xna.Framework.Vector3(
                                float.Parse(parts[0]) / 255f,
                                float.Parse(parts[1]) / 255f,
                                float.Parse(parts[2]) / 255f);
                            Trace.WriteLine($"[DYN_LIGHT] Color set to: {Color}");
                        }
                    }
                    else if (trimmed.StartsWith("Intensity:", StringComparison.OrdinalIgnoreCase))
                    {
                        float parsedIntensity;
                        if (float.TryParse(trimmed.Substring(10), out parsedIntensity))
                        {
                            Intensity = parsedIntensity;
                            Trace.WriteLine($"[DYN_LIGHT] Intensity set to: {Intensity}");
                        }
                        else
                        {
                            Trace.WriteLine($"[DYN_LIGHT] Invalid intensity value in config: {trimmed.Substring(10)}");
                        }
                    }
                    else if (trimmed.StartsWith("Radius:", StringComparison.OrdinalIgnoreCase))
                    {
                        Radius = float.Parse(trimmed.Substring(7));
                        Trace.WriteLine($"[DYN_LIGHT] Radius set to: {Radius}");
                    }
                    else if (trimmed.StartsWith("Type:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Enum.TryParse(trimmed.Substring(5).Trim(), true, out LightType type))
                        {
                            Type = type;
                            Trace.WriteLine($"[DYN_LIGHT] Type set to: {Type}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[DYN_LIGHT] Error loading config {path}: {ex}");
            }
        }
    }
}
