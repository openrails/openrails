using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ORTS.Common.Lighting
{
    /// <summary>
    /// Manages all dynamic lights in the world.
    /// </summary>
    public class LightManager
    {
        private readonly List<DynamicLight> _lights = new List<DynamicLight>();
        public IReadOnlyList<DynamicLight> Lights => _lights;

        public void AddLight(DynamicLight light)
        {
            if (light == null)
            {
                System.Diagnostics.Trace.WriteLine("[DYN_LIGHT] Tried to add null light!");
                return;
            }
            System.Diagnostics.Trace.WriteLine($"[DYN_LIGHT] Adding light: Pos={light.Position}, Intensity={light.Intensity}, Radius={light.Radius}");
            if (!_lights.Contains(light))
                _lights.Add(light);
        }

        public void RemoveLight(DynamicLight light)
        {
            _lights.Remove(light);
        }

        public void Clear()
        {
            _lights.Clear();
        }

        // Optionally: culling, LOD, update logic, etc.
        public void Update(float elapsedTime)
        {
            // Placeholder for future per-frame updates (e.g., flicker, animation)
        }
    }
}
