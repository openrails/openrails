// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using System;
using System.Collections.Generic;

namespace Orts.Viewer3D
{
    static class SkyConstants
    {
        // Sky dome constants
        public const int skyRadius = 6000;
        public const int skySides = 24;
        // <CScomment> added a belt of triangles just below 0 level to avoid empty sky below horizon
        public const short skyLevels = 6;
    }

    public class SkyViewer
    {
        Viewer Viewer;
        Material Material;

        // Classes reqiring instantiation
        public SkyPrimitive Primitive;
        WorldLatLon worldLoc; // Access to latitude and longitude calcs (MSTS routes only)
        SunMoonPos skyVectors;

        int seasonType; //still need to remember it as MP now can change it.
        // Latitude of current route in radians. -pi/2 = south pole, 0 = equator, pi/2 = north pole.
        // Longitude of current route in radians. -pi = west of prime, 0 = prime, pi = east of prime.
        public double latitude, longitude;
        // Date of activity
        public struct Date
        {
            // Day, month, year. Format: DD MM YYYY, no leading zeros. 
            public int year;
            public int month;
            public int day;
            // Ordinal date. Range: 0 to 366.
            public int ordinalDate;
        };
        public Date date;
        // Size of the sun- and moon-position lookup table arrays.
        // Must be an integral divisor of 1440 (which is the number of minutes in a day).
        private int maxSteps = 72;
        private double oldClockTime;
        int step1, step2;
        // Phase of the moon
        public int moonPhase;
        // Wind speed and direction
        public float windSpeed;
        public float windDirection;

        // These arrays and vectors define the position of the sun and moon in the world
        Vector3[] solarPosArray = new Vector3[72];
        Vector3[] lunarPosArray = new Vector3[72];
        public Vector3 solarDirection;
        public Vector3 lunarDirection;

        public SkyViewer(Viewer viewer)
        {
            Viewer = viewer;
            Material = viewer.MaterialManager.Load("Sky");

            // Instantiate classes
            Primitive = new SkyPrimitive(Viewer.RenderProcess);
            skyVectors = new SunMoonPos();

            // Set starting values
            seasonType = -1;
            // Default wind speed and direction
            windSpeed = 5.0f; // m/s (approx 11 mph)
            windDirection = 4.7f; // radians (approx 270 deg, i.e. westerly)
        }

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Adjust dome position so the bottom edge is not visible
            Vector3 ViewerXNAPosition = new Vector3(Viewer.Camera.Location.X, Viewer.Camera.Location.Y - 100, -Viewer.Camera.Location.Z);
            Matrix XNASkyWorldLocation = Matrix.CreateTranslation(ViewerXNAPosition);

            if (worldLoc == null)
            {
                // First time around, initialize the following items:
                worldLoc = new WorldLatLon();
                oldClockTime = Viewer.Simulator.ClockTime % 86400;
                while (oldClockTime < 0) oldClockTime += 86400;
                step1 = step2 = (int)(oldClockTime / 1200);
                step2 = step2 < maxSteps - 1 ? step2 + 1 : 0; // limit to max. steps in case activity starts near midnight

                // Get the current latitude and longitude coordinates
                worldLoc.ConvertWTC(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location, ref latitude, ref longitude);
                if (seasonType != (int)Viewer.Simulator.Season)
                {
                    seasonType = (int)Viewer.Simulator.Season;
                    date.ordinalDate = latitude >= 0 ? 82 + seasonType * 91 : (82 + (seasonType + 2) * 91) % 365;
                    // TODO: Set the following three externally from ORTS route files (future)
                    date.month = 1 + date.ordinalDate / 30;
                    date.day = 21;
                    date.year = 2017;
                }
                // Fill in the sun- and moon-position lookup tables
                for (int i = 0; i < maxSteps; i++)
                {
                    solarPosArray[i] = SunMoonPos.SolarAngle(latitude, longitude, ((float)i / maxSteps), date);
                    lunarPosArray[i] = SunMoonPos.LunarAngle(latitude, longitude, ((float)i / maxSteps), date);
                }
                // Phase of the moon is generated at random
                moonPhase = Viewer.Random.Next(8);
                if (moonPhase == 6 && date.ordinalDate > 45 && date.ordinalDate < 330)
                    moonPhase = 3; // Moon dog only occurs in winter
            }


            // Current solar and lunar position are calculated by interpolation in the lookup arrays.
            // The arrays have intervals of 1200 secs or 20 mins.
            // Using the Lerp() function, so need to calculate the in-between differential
            float diff = GetCelestialDiff();
            // The rest of this increments/decrements the array indices and checks for overshoot/undershoot.
            while (Viewer.Simulator.ClockTime >= (oldClockTime + 1200)) // Plus key, or normal forward in time; <CSComment> better so in case of fast forward
            {
                oldClockTime = oldClockTime + 1200;
                diff = GetCelestialDiff();
                step1++;
                step2++;
                if (step2 >= maxSteps) // Midnight.
                {
                    step2 = 0;
                }
                if (step1 >= maxSteps) // Midnight.
                {
                    step1 = 0;
                }
            }
            if (Viewer.Simulator.ClockTime <= (oldClockTime - 1200)) // Minus key
            {
                oldClockTime = Viewer.Simulator.ClockTime;
                diff = 0;
                step1--;
                step2--;
                if (step1 < 0) // Midnight.
                {
                    step1 = maxSteps - 1;
                }
                if (step2 < 0) // Midnight.
                {
                    step2 = maxSteps - 1;
                }
            }
            solarDirection.X = MathHelper.Lerp(solarPosArray[step1].X, solarPosArray[step2].X, diff);
            solarDirection.Y = MathHelper.Lerp(solarPosArray[step1].Y, solarPosArray[step2].Y, diff);
            solarDirection.Z = MathHelper.Lerp(solarPosArray[step1].Z, solarPosArray[step2].Z, diff);
            lunarDirection.X = MathHelper.Lerp(lunarPosArray[step1].X, lunarPosArray[step2].X, diff);
            lunarDirection.Y = MathHelper.Lerp(lunarPosArray[step1].Y, lunarPosArray[step2].Y, diff);
            lunarDirection.Z = MathHelper.Lerp(lunarPosArray[step1].Z, lunarPosArray[step2].Z, diff);

            frame.AddPrimitive(Material, Primitive, RenderPrimitiveGroup.Sky, ref XNASkyWorldLocation);
        }

        /// <summary>
        /// Returns the advance of time in seconds in units of 20 mins (1200 seconds).
        /// Allows for an offset in hours from a control in the DispatchViewer.
        /// This is a user convenience to reveal in daylight what might be hard to see at night.
        /// </summary>
        /// <returns></returns>
        private float GetCelestialDiff()
        {
            var diffS = (Viewer.Simulator.ClockTime - oldClockTime);
            diffS += (double)(Program.DebugViewer?.DaylightOffsetHrs ?? 0) * 60 * 60;
            return (float)diffS / 1200;
        }

        public void LoadPrep()
        {
            worldLoc = new WorldLatLon();
            // Get the current latitude and longitude coordinates
            worldLoc.ConvertWTC(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location, ref latitude, ref longitude);
            seasonType = (int)Viewer.Simulator.Season;
            date.ordinalDate = latitude >= 0 ? 82 + seasonType * 91 : (82 + (seasonType + 2) * 91) % 365;
            date.month = 1 + date.ordinalDate / 30;
            date.day = 21;
            date.year = 2017;
            float fractClockTime = (float)Viewer.Simulator.ClockTime / 86400;
            solarDirection = SunMoonPos.SolarAngle(latitude, longitude, fractClockTime, date);
            worldLoc = null;
            latitude = 0;
            longitude = 0;
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Material.Mark();
        }
    }

    public class SkyPrimitive : RenderPrimitive
    {
        private VertexBuffer SkyVertexBuffer;
        private static IndexBuffer SkyIndexBuffer;
        public int drawIndex;

        VertexPositionNormalTexture[] vertexList;
        private static short[] triangleListIndices; // Trilist buffer.

        // Sky dome geometry is based on two global variables: the radius and the number of sides
        public int skyRadius = SkyConstants.skyRadius;
        private static int skySides = SkyConstants.skySides;
        public int cloudDomeRadiusDiff = 600; // Amount by which cloud dome radius is smaller than sky dome
        // skyLevels: Used for iterating vertically through the "levels" of the hemisphere polygon
        private static int skyLevels = SkyConstants.skyLevels;
        // Number of vertices in the sky hemisphere. (each dome = 169 for 24-sided sky dome: 24 x 7 + 1)
        // plus four more for the moon quad
        private static int numVertices = 4 + 2 * ((skyLevels + 1) * skySides + 1);
        // Number of point indices (each dome = 912 for 24 sides: 7 levels of 24 triangle pairs each
        // plus 24 triangles at the zenith)
        // plus six more for the moon quad
        private static short indexCount = 6 + 2 * (SkyConstants.skySides * 6 *SkyConstants.skyLevels + 3 * SkyConstants.skySides);

        /// <summary>
        /// Constructor.
        /// </summary>
        public SkyPrimitive(RenderProcess renderProcess)
        {
            // Initialize the vertex and point-index buffers
            vertexList = new VertexPositionNormalTexture[numVertices];
            triangleListIndices = new short[indexCount];
            // Sky dome
            DomeVertexList(0, skyRadius, 1.0f);
            DomeTriangleList(0, 0);
            // Cloud dome
            DomeVertexList((numVertices - 4) / 2, skyRadius - cloudDomeRadiusDiff, 0.4f);
            DomeTriangleList((short)((indexCount - 6) / 2), 1);
            // Moon quad
            MoonLists(numVertices - 5, indexCount - 6);
            // Meshes have now been assembled, so put everything into vertex and index buffers
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(SkyVertexBuffer);
            graphicsDevice.Indices = SkyIndexBuffer;

            switch (drawIndex)
            {
                case 1: // Sky dome
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: 0,
                        startIndex: 0,
                        primitiveCount: (indexCount - 6) / 6);
                    break;
                case 2: // Moon
                    graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    baseVertex: 0,
                    startIndex: indexCount - 6,
                    primitiveCount: 2);
                    break;
                case 3: // Clouds Dome
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        baseVertex: 0,
                        startIndex: (indexCount - 6) / 2,
                        primitiveCount: (indexCount - 6) / 6);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Creates the vertex list for each sky dome.
        /// </summary>
        /// <param name="index">The starting vertex number</param>
        /// <param name="radius">The radius of the dome</param>
        /// <param name="oblate">The amount the dome is flattened</param>
        private void DomeVertexList(int index, int radius, float oblate)
        {
            int vertexIndex = index;
            // <CSComment> for night sky texture wrap to maintain stars position, for clouds no wrap for better appearance
            int texDivisor = (oblate == 1.0f) ? skyLevels : skyLevels + 1;

            // for each vertex
            for (int i = 0; i <= (skyLevels); i++) // (=6 for 24 sides)
            {
                // The "oblate" factor is used to flatten the dome to an ellipsoid. Used for the inner (cloud)
                // dome only. Gives the clouds a flatter appearance.
                float y = (float)Math.Sin(MathHelper.ToRadians((360 / skySides) * (i-1))) * radius * oblate;
                float yRadius = radius * (float)Math.Cos(MathHelper.ToRadians((360 / skySides) * (i-1)));
                for (int j = 0; j < skySides; j++) // (=24 for top overlay)
                {

                    float x = (float)Math.Cos(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * yRadius;
                    float z = (float)Math.Sin(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * yRadius;

                    // UV coordinates - top overlay
                    float uvRadius;
                    uvRadius = 0.5f - (float)(0.5f * (i - 1)) / texDivisor;
                    float uv_u = 0.5f - ((float)Math.Cos(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * uvRadius);
                    float uv_v = 0.5f - ((float)Math.Sin(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * uvRadius);

                    // Store the position, texture coordinates and normal (normalized position vector) for the current vertex
                    vertexList[vertexIndex].Position = new Vector3(x, y, z);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_u, uv_v);
                    vertexList[vertexIndex].Normal = Vector3.Normalize(new Vector3(x, y, z));
                    vertexIndex++;
                }
            }
            // Single vertex at zenith
            vertexList[vertexIndex].Position = new Vector3(0, radius, 0);
            vertexList[vertexIndex].Normal = new Vector3(0, 1, 0);
            vertexList[vertexIndex].TextureCoordinate = new Vector2(0.5f, 0.5f); // (top overlay)
        }

        /// <summary>
        /// Creates the triangle index list for each dome.
        /// </summary>
        /// <param name="index">The starting triangle index number</param>
        /// <param name="pass">A multiplier used to arrive at the starting vertex number</param>
        static void DomeTriangleList(short index, short pass)
        {
            // ----------------------------------------------------------------------
            // 24-sided sky dome mesh is built like this:        48 49 50
            // Triangles are wound couterclockwise          71 o--o--o--o
            // because we're looking at the inner              | /|\ | /|
            // side of the hemisphere. Each time               |/ | \|/ |
            // we circle around to the start point          47 o--o--o--o 26
            // on the mesh we have to reset the                |\ | /|\ |
            // vertex number back to the beginning.            | \|/ | \|
            // Using WAC's sw,se,nw,ne coordinate    nw ne  23 o--o--o--o 
            // convention.-->                        sw se        0  1  2
            // ----------------------------------------------------------------------
            short iIndex = index;
            short baseVert = (short)(pass * (short)((numVertices - 4) / 2));
            for (int i = 0; i < skyLevels; i++) // (=6 for 24 sides)
                for (int j = 0; j < skySides; j++) // (=24 for 24 sides)
                {
                    // Vertex indices, beginning in the southwest corner
                    short sw = (short)(baseVert + (j + i * (skySides)));
                    short nw = (short)(sw + skySides); // top overlay mapping
                    short ne = (short)(nw + 1);

                    short se = (short)(sw + 1);

                    if (((i & 1) == (j & 1)))  // triangles alternate
                    {
                        triangleListIndices[iIndex++] = sw;
                        triangleListIndices[iIndex++] = ((ne - baseVert) % skySides == 0) ? (short)(ne - skySides) : ne;
                        triangleListIndices[iIndex++] = nw;
                        triangleListIndices[iIndex++] = sw;
                        triangleListIndices[iIndex++] = ((se - baseVert) % skySides == 0) ? (short)(se - skySides) : se;
                        triangleListIndices[iIndex++] = ((ne - baseVert) % skySides == 0) ? (short)(ne - skySides) : ne;
                    }
                    else
                    {
                        triangleListIndices[iIndex++] = sw;
                        triangleListIndices[iIndex++] = ((se - baseVert) % skySides == 0) ? (short)(se - skySides) : se;
                        triangleListIndices[iIndex++] = nw;
                        triangleListIndices[iIndex++] = ((se - baseVert) % skySides == 0) ? (short)(se - skySides) : se;
                        triangleListIndices[iIndex++] = ((ne - baseVert) % skySides == 0) ? (short)(ne - skySides) : ne;
                        triangleListIndices[iIndex++] = nw;
                    }
                }
            //Zenith triangles (=24 for 24 sides)
            for (int i = 0; i < skySides; i++)
            {
                short sw = (short)(baseVert + (((skySides) * skyLevels) + i));
                short se = (short)(sw + 1);

                triangleListIndices[iIndex++] = sw;
                triangleListIndices[iIndex++] = ((se - baseVert) % skySides == 0) ? (short)(se - skySides) : se;
                triangleListIndices[iIndex++] = (short)(baseVert + (short)((numVertices - 5) / 2)); // The zenith
            }
        }

        /// <summary>
        /// Creates the moon vertex and triangle index lists.
        /// <param name="vertexIndex">The starting vertex number</param>
        /// <param name="iIndex">The starting triangle index number</param>
        /// </summary>
        private void MoonLists(int vertexIndex, int iIndex)
        {
            // Moon vertices
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    vertexIndex++;
                    vertexList[vertexIndex].Position = new Vector3(i, j, 0);
                    vertexList[vertexIndex].Normal = new Vector3(0, 0, 1);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(i, j);
                }

            // Moon indices - clockwise winding
            short msw = (short)(numVertices - 4);
            short mnw = (short)(msw + 1);
            short mse = (short)(mnw + 1);
            short mne = (short)(mse + 1);
            triangleListIndices[iIndex++] = msw;
            triangleListIndices[iIndex++] = mnw;
            triangleListIndices[iIndex++] = mse;
            triangleListIndices[iIndex++] = mse;
            triangleListIndices[iIndex++] = mnw;
            triangleListIndices[iIndex++] = mne;
        }

        /// <summary>
        /// Initializes the sky dome, cloud dome and moon vertex and triangle index list buffers.
        /// </summary>
        private void InitializeVertexBuffers(GraphicsDevice graphicsDevice)
        {
            // Initialize the vertex and index buffers, allocating memory for each vertex and index
            SkyVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertexList.Length, BufferUsage.WriteOnly);
            SkyVertexBuffer.SetData(vertexList);
            if (SkyIndexBuffer == null)
            {
                SkyIndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indexCount, BufferUsage.WriteOnly);
                SkyIndexBuffer.SetData(triangleListIndices);
            }
        }
    }

    public class SkyMaterial : Material
    {
        SkyShader SkyShader;
        Texture2D SkyTexture;
        Texture2D StarTextureN;
        Texture2D StarTextureS;
        Texture2D MoonTexture;
        Texture2D MoonMask;
        Texture2D CloudTexture;
        private Matrix XNAMoonMatrix;
        IEnumerator<EffectPass> ShaderPassesSky;
        IEnumerator<EffectPass> ShaderPassesMoon;
        IEnumerator<EffectPass> ShaderPassesClouds;

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
            FogDay2Night(
                Viewer.World.Sky.solarDirection.Y,
                Viewer.Simulator.Weather.OvercastFactor);

            //if (Viewer.Settings.DistantMountains) SharedMaterialManager.FogCoeff *= (3 * (5 - Viewer.Settings.DistantMountainsFogValue) + 0.5f);

            if (Viewer.World.Sky.latitude > 0) // TODO: Use a dirty flag to determine if it is necessary to set the texture again
                SkyShader.StarMapTexture = StarTextureN;
            else
                SkyShader.StarMapTexture = StarTextureS;
            SkyShader.Random = Viewer.World.Sky.moonPhase; // Keep setting this before LightVector for the preshader to work correctly
            SkyShader.LightVector = Viewer.World.Sky.solarDirection;
            SkyShader.Time = (float)Viewer.Simulator.ClockTime / 100000;
            SkyShader.MoonScale = SkyConstants.skyRadius / 20;
            SkyShader.Overcast = Viewer.Simulator.Weather.OvercastFactor;
            SkyShader.SetFog(Viewer.Simulator.Weather.FogDistance, ref SharedMaterialManager.FogColor);
            SkyShader.WindSpeed = Viewer.World.Sky.windSpeed;
            SkyShader.WindDirection = Viewer.World.Sky.windDirection; // Keep setting this after Time and Windspeed. Calculating displacement here.

            for (var i = 0; i < 5; i++)
                graphicsDevice.SamplerStates[i] = SamplerState.LinearWrap;
            
            // Sky dome
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            SkyShader.CurrentTechnique = SkyShader.Techniques["Sky"];
            Viewer.World.Sky.Primitive.drawIndex = 1;

            graphicsDevice.BlendState = BlendState.Opaque;

            Matrix viewXNASkyProj = XNAViewMatrix * Camera.XNASkyProjection;

            SkyShader.SetViewMatrix(ref XNAViewMatrix);
            ShaderPassesSky.Reset();
            while (ShaderPassesSky.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    SkyShader.SetMatrix(ref wvp);
                    ShaderPassesSky.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }

            // Moon
            SkyShader.CurrentTechnique = SkyShader.Techniques["Moon"];
            Viewer.World.Sky.Primitive.drawIndex = 2;

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            // Send the transform matrices to the shader
            int skyRadius = Viewer.World.Sky.Primitive.skyRadius;
            int cloudRadiusDiff = Viewer.World.Sky.Primitive.cloudDomeRadiusDiff;
            XNAMoonMatrix = Matrix.CreateTranslation(Viewer.World.Sky.lunarDirection * (skyRadius - (cloudRadiusDiff / 2)));
            Matrix XNAMoonMatrixView = XNAMoonMatrix * XNAViewMatrix;

            ShaderPassesMoon.Reset();
            while (ShaderPassesMoon.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAMoonMatrixView * Camera.XNASkyProjection;
                    SkyShader.SetMatrix(ref wvp);
                    ShaderPassesMoon.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }

            // Clouds
            SkyShader.CurrentTechnique = SkyShader.Techniques["Clouds"];
            Viewer.World.Sky.Primitive.drawIndex = 3;

            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            ShaderPassesClouds.Reset();
            while (ShaderPassesClouds.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
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

        const float nightStart = 0.15f; // The sun's Y value where it begins to get dark
        const float nightFinish = -0.05f; // The Y value where darkest fog color is reached and held steady

        // These should be user defined in the Environment files (future)
        static Vector3 startColor = new Vector3(0.647f, 0.651f, 0.655f); // Original daytime fog color - must be preserved!
        static Vector3 finishColor = new Vector3(0.05f, 0.05f, 0.05f); //Darkest nighttime fog color

        /// <summary>
        /// This function darkens the fog color as night begins to fall
        /// as well as with increasing overcast.
        /// </summary>
        /// <param name="sunHeight">The Y value of the sunlight vector</param>
        /// <param name="overcast">The amount of overcast</param>
        static void FogDay2Night(float sunHeight, float overcast)
        {
            Vector3 floatColor;

            if (sunHeight > nightStart)
                floatColor = startColor;
            else if (sunHeight < nightFinish)
                floatColor = finishColor;
            else
            {
                var amount = (sunHeight - nightFinish) / (nightStart - nightFinish);
                floatColor = Vector3.Lerp(finishColor, startColor, amount);
            }

            // Adjust fog color for overcast
            floatColor *= (1 - 0.5f * overcast);
            SharedMaterialManager.FogColor.R = (byte)(floatColor.X * 255);
            SharedMaterialManager.FogColor.G = (byte)(floatColor.Y * 255);
            SharedMaterialManager.FogColor.B = (byte)(floatColor.Z * 255);
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
    }
}
