using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

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
            Spot
            // Note: Directional lights are reserved for Sun/Moon only
        }

        public Vector3 Position { get; set; }
        public Vector3 Color { get; set; } // RGB, 0-1
        public float Intensity { get; set; }
        public float Radius { get; set; }
        public LightType Type { get; set; }
        public bool Active { get; set; }
        public Vector3 Direction { get; set; } // For spotlights only
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
                if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    LoadFromJsonConfig(path);
                }
                else
                {
                    LoadFromCfgConfig(path);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[DYN_LIGHT] Error loading config {path}: {ex}");
            }
        }

        private void LoadFromJsonConfig(string path)
        {
            var jsonContent = System.IO.File.ReadAllText(path);
            var root = JObject.Parse(jsonContent);
            var lightsArray = root["lights"];

            if (lightsArray != null && lightsArray.Type == JTokenType.Array)
            {
                // Load first light from array (or could create multiple lights)
                var lightObj = lightsArray[0];
                if (lightObj != null)
                {
                    // Color
                    var colorToken = lightObj["color"];
                    if (colorToken != null)
                    {
                        if (colorToken.Type == JTokenType.Array)
                        {
                            var colorArray = colorToken.ToObject<int[]>();
                            if (colorArray.Length >= 3)
                            {
                                Color = new Vector3(colorArray[0] / 255f, colorArray[1] / 255f, colorArray[2] / 255f);
                                Trace.WriteLine($"[DYN_LIGHT] Color set to: {Color}");
                            }
                        }
                    }

                    // Intensity
                    var intensityToken = lightObj["intensity"];
                    if (intensityToken != null && float.TryParse(intensityToken.ToString(), out var intensity))
                    {
                        Intensity = intensity;
                        Trace.WriteLine($"[DYN_LIGHT] Intensity set to: {Intensity}");
                    }

                    // Radius
                    var radiusToken = lightObj["radius"];
                    if (radiusToken != null && float.TryParse(radiusToken.ToString(), out var radius))
                    {
                        Radius = radius;
                        Trace.WriteLine($"[DYN_LIGHT] Radius set to: {Radius}");
                    }

                    // Type
                    var typeToken = lightObj["type"];
                    if (typeToken != null && Enum.TryParse<LightType>(typeToken.ToString(), true, out var type))
                    {
                        Type = type;
                        Trace.WriteLine($"[DYN_LIGHT] Type set to: {Type}");
                    }

                    // Direction (for Spot/Directional)
                    var directionToken = lightObj["direction"];
                    if (directionToken != null && directionToken.Type == JTokenType.Array)
                    {
                        var dirArray = directionToken.ToObject<float[]>();
                        if (dirArray.Length >= 3)
                        {
                            Direction = new Vector3(dirArray[0], dirArray[1], dirArray[2]);
                            Trace.WriteLine($"[DYN_LIGHT] Direction set to: {Direction}");
                        }
                    }

                    // Spot Angle
                    var spotAngleToken = lightObj["spotAngle"];
                    if (spotAngleToken != null && float.TryParse(spotAngleToken.ToString(), out var spotAngle))
                    {
                        SpotAngle = spotAngle;
                        Trace.WriteLine($"[DYN_LIGHT] SpotAngle set to: {SpotAngle}");
                    }
                }
            }
        }

        private void LoadFromCfgConfig(string path)
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
                        Color = new Vector3(
                            float.Parse(parts[0]) / 255f,
                            float.Parse(parts[1]) / 255f,
                            float.Parse(parts[2]) / 255f);
                        Trace.WriteLine($"[DYN_LIGHT] Color set to: {Color}");
                    }
                }
                else if (trimmed.StartsWith("Intensity:", StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(trimmed.Substring(10), out var parsedIntensity))
                    {
                        Intensity = parsedIntensity;
                        Trace.WriteLine($"[DYN_LIGHT] Intensity set to: {Intensity}");
                    }
                }
                else if (trimmed.StartsWith("Radius:", StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(trimmed.Substring(7), out var parsedRadius))
                    {
                        Radius = parsedRadius;
                        Trace.WriteLine($"[DYN_LIGHT] Radius set to: {Radius}");
                    }
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
    }
}
