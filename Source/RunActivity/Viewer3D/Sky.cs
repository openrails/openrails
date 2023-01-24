// COPYRIGHT 2009 - 2023 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using ORTS.Common;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Processes;

namespace Orts.Viewer3D
{
    public class SkyViewer
    {
        internal readonly SkyPrimitive Primitive;
        internal readonly float WindSpeed;
        internal readonly float WindDirection;
        internal int MoonPhase;
        internal Vector3 SolarDirection;
        internal Vector3 LunarDirection;
        internal double Latitude; // Latitude of current route in radians. -pi/2 = south pole, 0 = equator, pi/2 = north pole.
        internal double Longitude; // Longitude of current route in radians. -pi = west of prime, 0 = prime, pi = east of prime.

        static readonly WorldLatLon WorldLatLon = new WorldLatLon();

        readonly Viewer Viewer;
        readonly Material Material;
        readonly Vector3[] SolarPositionCache = new Vector3[72];
        readonly Vector3[] LunarPositionCache = new Vector3[72];
        readonly SkyInterpolation SkyInterpolation = new SkyInterpolation();

        public SkyViewer(Viewer viewer)
        {
            Viewer = viewer;
            Material = viewer.MaterialManager.Load("Sky");

            // Instantiate classes
            Primitive = new SkyPrimitive(Viewer.RenderProcess);

            // Default wind speed and direction
            // TODO: We should be using Viewer.Simulator.Weather instead of our own local weather fields
            WindSpeed = 5.0f; // m/s (approx 11 mph)
            WindDirection = 4.7f; // radians (approx 270 deg, i.e. westerly)
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            SkyInterpolation.SetSunAndMoonDirection(ref SolarDirection, ref LunarDirection, SolarPositionCache, LunarPositionCache, Viewer.Simulator.ClockTime);

            var xnaSkyWorldLocation = Matrix.CreateTranslation(Viewer.Camera.Location * new Vector3(1, 1, -1));
            frame.AddPrimitive(Material, Primitive, RenderPrimitiveGroup.Sky, ref xnaSkyWorldLocation);
        }

        public void LoadPrep()
        {
            WorldLatLon.ConvertWTC(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location, ref Latitude, ref Longitude);

            // First time around, initialize the following items:
            SkyInterpolation.OldClockTime = Viewer.Simulator.ClockTime % 86400;
            while (SkyInterpolation.OldClockTime < 0)
            {
                SkyInterpolation.OldClockTime += 86400;
            }

            SkyInterpolation.Step1 = SkyInterpolation.Step2 = (int)(SkyInterpolation.OldClockTime / 1200);
            SkyInterpolation.Step2 = SkyInterpolation.Step2 < SkyInterpolation.MaxSteps - 1 ? SkyInterpolation.Step2 + 1 : 0; // limit to max. steps in case activity starts near midnight

            // And the rest depends on the weather (which is changeable)
            Viewer.Simulator.WeatherChanged += (sender, e) => WeatherChanged();
            WeatherChanged();
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Material.Mark();
        }

        void WeatherChanged()
        {
            // TODO: Allow setting the date from route files?
            var seasonType = (int)Viewer.Simulator.Season;
            var date = new SkyDate { OrdinalDate = Latitude >= 0 ? 82 + (seasonType * 91) : (82 + ((seasonType + 2) * 91)) % 365 };
            date.Month = 1 + (date.OrdinalDate / 30);
            date.Day = 21;
            date.Year = 2017;

            // Fill in the sun- and moon-position lookup tables
            for (var i = 0; i < SkyInterpolation.MaxSteps; i++)
            {
                SolarPositionCache[i] = SunMoonPos.SolarAngle(Latitude, Longitude, (float)i / SkyInterpolation.MaxSteps, date);
                LunarPositionCache[i] = SunMoonPos.LunarAngle(Latitude, Longitude, (float)i / SkyInterpolation.MaxSteps, date);
            }

            // Phase of the moon is generated at random, but moon dog only occurs in winter
            MoonPhase = Viewer.Random.Next(8);
            if (MoonPhase == 6 && date.OrdinalDate > 45 && date.OrdinalDate < 330)
            {
                MoonPhase = 3;
            }
        }

        public struct SkyDate
        {
            public int Year;
            public int Month;
            public int Day;
            public int OrdinalDate; // Ordinal date. Range: 0 to 366.
        }
    }

    public class SkyPrimitive : RenderPrimitive
    {
        public const float RadiusM = 6000;
        public const float CloudsAltitudeM = 1000;

        public SkyElement Element;

        /*
         * The sky is formed of 3 layers (back to front):
         * - Cloud-less sky and night sky textures, blended according to time of day, and with sun effect added in (in the shader)
         * - Moon textures (phase is random)
         * - Clouds blended by overcast factor and animated by wind speed and direction
         *
         * Both the cloud-less sky and clouds use sky domes; the sky is
         * perfectly spherical, while the cloud dome is flattened and offset
         * so that it passes closer over the camera but still extends beyond
         * the horizon by the same amount.
         *
         * The sky dome is the top hemisphere of a globe, plus an extension
         * below the horizon to ensure we never get to see the edge. Both the
         * rotational (sides) and horizontal/vertical (steps) segments are
         * split so that the center angles are `DomeComponentDegrees`.
         *
         * It is important that there are enough sides for the texture mapping
         * to look good; otherwise, smooth curves will render as wavy lines.
         * Currently, testing shows 6° is the maximum reasonable angle.
         */
        const int TuneDomeComponentDegrees = 6;

        const int DomeSides = 360 / TuneDomeComponentDegrees;
        const int DomeStepsMain = 90 / TuneDomeComponentDegrees;
        const int DomeStepsExtra = 1;
        const int DomeSteps = DomeStepsMain + DomeStepsExtra;
        const int DomePrimitives = (2 * DomeSides * DomeSteps) - DomeSides;
        const int DomeVertices = 1 + (DomeSides * DomeSteps);
        const int DomeIndexes = 3 * DomePrimitives;

        const int MoonPrimitives = 2;
        const int MoonVertices = 4;
        const int MoonIndexes = 3 * MoonPrimitives;

        const int VertexCount = (2 * DomeVertices) + MoonVertices;
        const int IndexCount = DomeIndexes + MoonIndexes;

        // Calculate the height of the dome from top to bottom of extra steps (below horizon)
        static readonly float DomeHeightM = RadiusM * (float)(1 + Math.Sin(MathHelper.ToRadians(DomeStepsExtra * TuneDomeComponentDegrees)));
        static readonly float CloudsFlatness = 1 - ((RadiusM - CloudsAltitudeM) / DomeHeightM);
        static readonly float CloudsOffsetM = CloudsAltitudeM - (RadiusM * CloudsFlatness);

        readonly VertexPositionNormalTexture[] VertexList;
        readonly short[] IndexList;

        VertexBuffer VertexBuffer;
        IndexBuffer IndexBuffer;

        public SkyPrimitive(RenderProcess renderProcess)
        {
            // Initialize the vertex and index lists
            VertexList = new VertexPositionNormalTexture[VertexCount];
            IndexList = new short[IndexCount];
            var vertexIndex = 0;
            var indexIndex = 0;
            InitializeDomeVertexList(ref vertexIndex, RadiusM);
            InitializeDomeVertexList(ref vertexIndex, RadiusM, CloudsFlatness, CloudsOffsetM);
            InitializeDomeIndexList(ref indexIndex);
            InitializeMoonLists(ref vertexIndex, ref indexIndex);
            Debug.Assert(vertexIndex == VertexCount, $"Did not initialize all verticies; expected {VertexCount}, got {vertexIndex}");
            Debug.Assert(indexIndex == IndexCount, $"Did not initialize all indexes; expected {IndexCount}, got {indexIndex}");

            // Meshes have now been assembled, so put everything into vertex and index buffers
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
        }

        public enum SkyElement
        {
            Sky,
            Moon,
            Clouds,
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(VertexBuffer);
            graphicsDevice.Indices = IndexBuffer;

            switch (Element)
            {
                case SkyElement.Sky:
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: 0,
                        startIndex: 0,
                        primitiveCount: DomePrimitives);
                    break;
                case SkyElement.Moon:
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: DomeVertices * 2,
                        startIndex: DomeIndexes,
                        primitiveCount: MoonPrimitives);
                    break;
                case SkyElement.Clouds:
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: DomeVertices,
                        startIndex: 0,
                        primitiveCount: DomePrimitives);
                    break;
            }
        }

        void InitializeDomeVertexList(ref int index, float radius, float flatness = 1, float offset = 0)
        {
            // Single vertex at zenith
            VertexList[index].Position = new Vector3(0, (radius * flatness) + offset, 0);
            VertexList[index].Normal = Vector3.Normalize(VertexList[index].Position);
            VertexList[index].TextureCoordinate = new Vector2(0.5f, 0.5f);
            index++;

            for (var step = 1; step <= DomeSteps; step++)
            {
                var stepCos = (float)Math.Cos(MathHelper.ToRadians(90f * step / DomeStepsMain));
                var stepSin = (float)Math.Sin(MathHelper.ToRadians(90f * step / DomeStepsMain));

                var y = radius * stepCos;
                var d = radius * stepSin;

                for (var side = 0; side < DomeSides; side++)
                {
                    var sideCos = (float)Math.Cos(MathHelper.ToRadians(360f * side / DomeSides));
                    var sideSin = (float)Math.Sin(MathHelper.ToRadians(360f * side / DomeSides));

                    var x = d * sideCos;
                    var z = d * sideSin;

                    var u = 0.5f + ((float)step / DomeStepsMain * sideCos / 2);
                    var v = 0.5f + ((float)step / DomeStepsMain * sideSin / 2);

                    // Store the position, texture coordinates and normal (normalized position vector) for the current vertex
                    VertexList[index].Position = new Vector3(x, (y * flatness) + offset, z);
                    VertexList[index].Normal = Vector3.Normalize(VertexList[index].Position);
                    VertexList[index].TextureCoordinate = new Vector2(u, v);
                    index++;
                }
            }
        }

        void InitializeDomeIndexList(ref int index)
        {
            // Zenith triangles
            for (var side = 0; side < DomeSides; side++)
            {
                IndexList[index++] = 0;
                IndexList[index++] = (short)(1 + ((side + 1) % DomeSides));
                IndexList[index++] = (short)(1 + ((side + 0) % DomeSides));
            }

            for (var step = 1; step < DomeSteps; step++)
            {
                for (var side = 0; side < DomeSides; side++)
                {
                    IndexList[index++] = (short)(1 + ((step - 1) * DomeSides) + ((side + 0) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 0) * DomeSides) + ((side + 1) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 0) * DomeSides) + ((side + 0) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 1) * DomeSides) + ((side + 0) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 1) * DomeSides) + ((side + 1) % DomeSides));
                    IndexList[index++] = (short)(1 + ((step - 0) * DomeSides) + ((side + 1) % DomeSides));
                }
            }
        }

        void InitializeMoonLists(ref int vertexIndex, ref int indexIndex)
        {
            // Moon vertices
            for (var i = 0; i < 2; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    VertexList[vertexIndex].Position = new Vector3(i, j, 0);
                    VertexList[vertexIndex].Normal = new Vector3(0, 0, 1);
                    VertexList[vertexIndex].TextureCoordinate = new Vector2(i, j);
                    vertexIndex++;
                }
            }

            // Moon indices - clockwise winding
            IndexList[indexIndex++] = 0;
            IndexList[indexIndex++] = 1;
            IndexList[indexIndex++] = 2;
            IndexList[indexIndex++] = 1;
            IndexList[indexIndex++] = 3;
            IndexList[indexIndex++] = 2;
        }

        void InitializeVertexBuffers(GraphicsDevice graphicsDevice)
        {
            VertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), VertexList.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(VertexList);
            IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), IndexCount, BufferUsage.WriteOnly);
            IndexBuffer.SetData(IndexList);
        }
    }

    class SkyMaterial : Material
    {
        const float NightStart = 0.15f; // The sun's Y value where it begins to get dark

        const float NightFinish = -0.05f; // The Y value where darkest fog color is reached and held steady

        // These should be user defined in the Environment files (future)
        static readonly Vector3 StartColor = new Vector3(0.647f, 0.651f, 0.655f); // Original daytime fog color - must be preserved!
        static readonly Vector3 FinishColor = new Vector3(0.05f, 0.05f, 0.05f); // Darkest night-time fog color

        readonly SkyShader SkyShader;
        readonly Texture2D SkyTexture;
        readonly Texture2D StarTextureN;
        readonly Texture2D StarTextureS;
        readonly Texture2D MoonTexture;
        readonly Texture2D MoonMask;
        readonly Texture2D CloudTexture;
        readonly IEnumerator<EffectPass> ShaderPassesSky;
        readonly IEnumerator<EffectPass> ShaderPassesMoon;
        readonly IEnumerator<EffectPass> ShaderPassesClouds;

        public SkyMaterial(Viewer viewer)
           : base(viewer, null)
        {
            SkyShader = Viewer.MaterialManager.SkyShader;

            // TODO: This should happen on the loader thread.
            SkyTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "SkyDome1.png"));
            StarTextureN = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Starmap_N.png"));
            StarTextureS = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Starmap_S.png"));
            MoonTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "MoonMap.png"));
            MoonMask = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "MoonMask.png"));
            CloudTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Clouds01.png"));

            ShaderPassesSky = SkyShader.Techniques["Sky"].Passes.GetEnumerator();
            ShaderPassesMoon = SkyShader.Techniques["Moon"].Passes.GetEnumerator();
            ShaderPassesClouds = SkyShader.Techniques["Clouds"].Passes.GetEnumerator();

            SkyShader.SkyMapTexture = SkyTexture;
            SkyShader.StarMapTexture = StarTextureN;
            SkyShader.MoonMapTexture = MoonTexture;
            SkyShader.MoonMaskTexture = MoonMask;
            SkyShader.CloudMapTexture = CloudTexture;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            // Adjust Fog color for day-night conditions and overcast
            FogDay2Night(Viewer.World.Sky.SolarDirection.Y, Viewer.Simulator.Weather.OvercastFactor);

            // TODO: Use a dirty flag to determine if it is necessary to set the texture again
            SkyShader.StarMapTexture = Viewer.World.Sky.Latitude > 0 ? StarTextureN : StarTextureS;

            SkyShader.Random = Viewer.World.Sky.MoonPhase; // Keep setting this before LightVector for the preshader to work correctly
            SkyShader.LightVector = Viewer.World.Sky.SolarDirection;
            SkyShader.Time = (float)Viewer.Simulator.ClockTime / 100000;
            SkyShader.MoonScale = SkyPrimitive.RadiusM / 20;
            SkyShader.Overcast = Viewer.Simulator.Weather.OvercastFactor;
            SkyShader.SetFog(Viewer.Simulator.Weather.FogDistance, ref SharedMaterialManager.FogColor);
            SkyShader.WindSpeed = Viewer.World.Sky.WindSpeed;
            SkyShader.WindDirection = Viewer.World.Sky.WindDirection; // Keep setting this after Time and Windspeed. Calculating displacement here.

            for (var i = 0; i < 5; i++)
            {
                graphicsDevice.SamplerStates[i] = SamplerState.LinearWrap;
            }

            var xnaSkyView = XNAViewMatrix * Camera.XNASkyProjection;
            var xnaMoonMatrix = Matrix.CreateTranslation(Viewer.World.Sky.LunarDirection * SkyPrimitive.RadiusM);
            var xnaMoonView = xnaMoonMatrix * xnaSkyView;
            SkyShader.SetViewMatrix(ref XNAViewMatrix);

            // Sky dome
            SkyShader.CurrentTechnique = SkyShader.Techniques["Sky"];
            Viewer.World.Sky.Primitive.Element = SkyPrimitive.SkyElement.Sky;
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.None;

            ShaderPassesSky.Reset();
            while (ShaderPassesSky.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    var wvp = item.XNAMatrix * xnaSkyView;
                    SkyShader.SetMatrix(ref wvp);
                    ShaderPassesSky.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }

            // Moon
            SkyShader.CurrentTechnique = SkyShader.Techniques["Moon"];
            Viewer.World.Sky.Primitive.Element = SkyPrimitive.SkyElement.Moon;
            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            ShaderPassesMoon.Reset();
            while (ShaderPassesMoon.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    var wvp = item.XNAMatrix * xnaMoonView;
                    SkyShader.SetMatrix(ref wvp);
                    ShaderPassesMoon.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }

            // Clouds
            SkyShader.CurrentTechnique = SkyShader.Techniques["Clouds"];
            Viewer.World.Sky.Primitive.Element = SkyPrimitive.SkyElement.Clouds;
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            ShaderPassesClouds.Reset();
            while (ShaderPassesClouds.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    var wvp = item.XNAMatrix * xnaSkyView;
                    SkyShader.SetMatrix(ref wvp);
                    ShaderPassesClouds.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override bool GetBlending()
        {
            return false;
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(SkyTexture);
            Viewer.TextureManager.Mark(StarTextureN);
            Viewer.TextureManager.Mark(StarTextureS);
            Viewer.TextureManager.Mark(MoonTexture);
            Viewer.TextureManager.Mark(MoonMask);
            Viewer.TextureManager.Mark(CloudTexture);
            base.Mark();
        }

        /// <summary>
        /// This function darkens the fog color as night begins to fall
        /// as well as with increasing overcast.
        /// </summary>
        /// <param name="sunHeight">The Y value of the sunlight vector.</param>
        /// <param name="overcast">The amount of overcast.</param>
        static void FogDay2Night(float sunHeight, float overcast)
        {
            Vector3 floatColor;

            if (sunHeight > NightStart)
            {
                floatColor = StartColor;
            }
            else if (sunHeight < NightFinish)
            {
                floatColor = FinishColor;
            }
            else
            {
                var amount = (sunHeight - NightFinish) / (NightStart - NightFinish);
                floatColor = Vector3.Lerp(FinishColor, StartColor, amount);
            }

            // Adjust fog color for overcast
            floatColor *= 1 - (0.5f * overcast);
            SharedMaterialManager.FogColor.R = (byte)(floatColor.X * 255);
            SharedMaterialManager.FogColor.G = (byte)(floatColor.Y * 255);
            SharedMaterialManager.FogColor.B = (byte)(floatColor.Z * 255);
        }
    }
}
