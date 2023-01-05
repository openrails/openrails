// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Common;
using Orts.MultiPlayer;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Processes;
using ORTS.Common;
using ORTS.Common.Input;

namespace Orts.Viewer3D
{
    #region MSTSSkyConstants
    public class MSTSSkyConstants
    {
        // Sky dome constants
        public static int skyRadius = 6000;
        public const int skySides = 24;
        public static int skyHeight;
        public const short skyLevels = 4;
        public static bool IsNight = false;
        public static float mstsskyTileu;
        public static float mstsskyTilev;
        public static float mstscloudTileu;
        public static float mstscloudTilev;
        
    }
    #endregion

    #region MSTSSkyDrawer
    public class MSTSSkyDrawer
    {

        Viewer MSTSSkyViewer;
        Material MSTSSkyMaterial;

        // Classes reqiring instantiation
        public MSTSSkyMesh MSTSSkyMesh;
        WorldLatLon mstsskyworldLoc; // Access to latitude and longitude calcs (MSTS routes only)

        int mstsskyseasonType; //still need to remember it as MP now can change it.
        #region Class variables
        // Latitude of current route in radians. -pi/2 = south pole, 0 = equator, pi/2 = north pole.
        // Longitude of current route in radians. -pi = west of prime, 0 = prime, pi = east of prime.
        public double mstsskylatitude, mstsskylongitude;
        // Date of activity

        public Orts.Viewer3D.SkyViewer.SkyDate date;

        private SkyInterpolation skySteps = new SkyInterpolation();

        // Phase of the moon
        public int mstsskymoonPhase;
        // Wind speed and direction
        public float mstsskywindSpeed;
        public float mstsskywindDirection;
        // Overcast level
        public float mstsskyovercastFactor;
        // Fog distance
        public float mstsskyfogDistance;
        public bool isNight = false;

        public List<string> SkyLayers = new List<string>();

        // These arrays and vectors define the position of the sun and moon in the world
        Vector3[] mstsskysolarPosArray = new Vector3[72];
        Vector3[] mstsskylunarPosArray = new Vector3[72];
        public Vector3 mstsskysolarDirection;
        public Vector3 mstsskylunarDirection;
        #endregion

        #region Constructor
        /// <summary>
        /// SkyDrawer constructor
        /// </summary>
        public MSTSSkyDrawer(Viewer viewer)
        {
            MSTSSkyViewer = viewer;
            MSTSSkyMaterial = viewer.MaterialManager.Load("MSTSSky");
            // Instantiate classes
            MSTSSkyMesh = new MSTSSkyMesh(MSTSSkyViewer.RenderProcess);

            //viewer.World.MSTSSky.MSTSSkyMaterial.Viewer.MaterialManager.sunDirection.Y < 0
            // Set starting value
            mstsskyseasonType = -1;

            // Default wind speed and direction
            mstsskywindSpeed = 5.0f; // m/s (approx 11 mph)
            mstsskywindDirection = 4.7f; // radians (approx 270 deg, i.e. westerly)
        }
        #endregion

        /// <summary>
        /// Used to update information affecting the SkyMesh
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
             // Adjust dome position so the bottom edge is not visible
            Vector3 ViewerXNAPosition = new Vector3(MSTSSkyViewer.Camera.Location.X, MSTSSkyViewer.Camera.Location.Y - 100, -MSTSSkyViewer.Camera.Location.Z);
            Matrix XNASkyWorldLocation = Matrix.CreateTranslation(ViewerXNAPosition);

            if (mstsskyworldLoc == null)
            {
                // First time around, initialize the following items:
                mstsskyworldLoc = new WorldLatLon();
                skySteps.OldClockTime = MSTSSkyViewer.Simulator.ClockTime % 86400;
                while (skySteps.OldClockTime < 0) skySteps.OldClockTime += 86400;
                skySteps.Step1 = skySteps.Step2 = (int)(skySteps.OldClockTime / 1200);
                skySteps.Step2 = skySteps.Step2 < skySteps.MaxSteps - 1 ? skySteps.Step2 + 1 : 0; // limit to max. steps in case activity starts near midnight
                                                                                                  // Get the current latitude and longitude coordinates
                mstsskyworldLoc.ConvertWTC(MSTSSkyViewer.Camera.TileX, MSTSSkyViewer.Camera.TileZ, MSTSSkyViewer.Camera.Location, ref mstsskylatitude, ref mstsskylongitude);
                if (mstsskyseasonType != (int)MSTSSkyViewer.Simulator.Season)
                {
                    mstsskyseasonType = (int)MSTSSkyViewer.Simulator.Season;
                    date.OrdinalDate = mstsskylatitude >= 0 ? 82 + mstsskyseasonType * 91 : (82 + (mstsskyseasonType + 2) * 91) % 365;
                    // TODO: Set the following three externally from ORTS route files (future)
                    date.Month = 1 + date.OrdinalDate / 30;
                    date.Day = 21;
                    date.Year = 2017;
                }
                // Fill in the sun- and moon-position lookup tables
                for (int i = 0; i < skySteps.MaxSteps; i++)
                {
                    mstsskysolarPosArray[i] = SunMoonPos.SolarAngle(mstsskylatitude, mstsskylongitude, ((float)i / skySteps.MaxSteps), date);
                    mstsskylunarPosArray[i] = SunMoonPos.LunarAngle(mstsskylatitude, mstsskylongitude, ((float)i / skySteps.MaxSteps), date);
                }
                // Phase of the moon is generated at random
                mstsskymoonPhase = Viewer.Random.Next(8);
                if (mstsskymoonPhase == 6 && date.OrdinalDate > 45 && date.OrdinalDate < 330)
                    mstsskymoonPhase = 3; // Moon dog only occurs in winter
                // Overcast factor: 0.0=almost no clouds; 0.1=wispy clouds; 1.0=total overcast
                //mstsskyovercastFactor = MSTSSkyViewer.World.WeatherControl.overcastFactor;
                mstsskyfogDistance = MSTSSkyViewer.Simulator.Weather.FogDistance;
            }

            MPManager manager = MPManager.Instance();
            if (MPManager.IsClient() && manager.weatherChanged)
            {
                //received message about weather change
                if (manager.overcastFactor >= 0)
                    mstsskyovercastFactor = manager.overcastFactor;

                //received message about weather change
                if (manager.fogDistance > 0)
                    mstsskyfogDistance = manager.fogDistance;

                if (manager.overcastFactor >= 0 || manager.fogDistance > 0)
                {
                    manager.weatherChanged = false;
                    manager.overcastFactor = -1;
                    manager.fogDistance = -1;
                }
            }

            ////////////////////// T E M P O R A R Y ///////////////////////////

            // The following keyboard commands are used for viewing sky and weather effects in "demo" mode.
            // Control- and Control+ for overcast, Shift- and Shift+ for fog and - and + for time.

            // Don't let multiplayer clients adjust the weather.
            if (!MPManager.IsClient())
            {
                // Overcast ranges from 0 (completely clear) to 1 (completely overcast).
                if (UserInput.IsDown(UserCommand.DebugOvercastIncrease)) mstsskyovercastFactor = MathHelper.Clamp(mstsskyovercastFactor + elapsedTime.RealSeconds / 10, 0, 1);
                if (UserInput.IsDown(UserCommand.DebugOvercastDecrease)) mstsskyovercastFactor = MathHelper.Clamp(mstsskyovercastFactor - elapsedTime.RealSeconds / 10, 0, 1);
                // Fog ranges from 10m (can't see anything) to 100km (clear arctic conditions).
                if (UserInput.IsDown(UserCommand.DebugFogIncrease)) mstsskyfogDistance = MathHelper.Clamp(mstsskyfogDistance - elapsedTime.RealSeconds * mstsskyfogDistance, 10, 100000);
                if (UserInput.IsDown(UserCommand.DebugFogDecrease)) mstsskyfogDistance = MathHelper.Clamp(mstsskyfogDistance + elapsedTime.RealSeconds * mstsskyfogDistance, 10, 100000);
            }
            // Don't let clock shift if multiplayer.
            if (!MPManager.IsMultiPlayer())
            {
                // Shift the clock forwards or backwards at 1h-per-second.
                if (UserInput.IsDown(UserCommand.DebugClockForwards)) MSTSSkyViewer.Simulator.ClockTime += elapsedTime.RealSeconds * 3600;
                if (UserInput.IsDown(UserCommand.DebugClockBackwards)) MSTSSkyViewer.Simulator.ClockTime -= elapsedTime.RealSeconds * 3600;
            }
            // Server needs to notify clients of weather changes.
            if (MPManager.IsServer())
            {
                if (UserInput.IsReleased(UserCommand.DebugOvercastIncrease) || UserInput.IsReleased(UserCommand.DebugOvercastDecrease) || UserInput.IsReleased(UserCommand.DebugFogIncrease) || UserInput.IsReleased(UserCommand.DebugFogDecrease))
                {
                    manager.SetEnvInfo(mstsskyovercastFactor, mstsskyfogDistance);
                    MPManager.Notify(new MSGWeather(-1, mstsskyovercastFactor, -1, mstsskyfogDistance).ToString());
                }
            }

            skySteps.SetSunAndMoonDirection(ref mstsskysolarDirection, ref mstsskylunarDirection, mstsskysolarPosArray, mstsskylunarPosArray,
                MSTSSkyViewer.Simulator.ClockTime);

            frame.AddPrimitive(MSTSSkyMaterial, MSTSSkyMesh, RenderPrimitiveGroup.Sky, ref XNASkyWorldLocation);
        }

        public void LoadPrep()
        {
            mstsskyworldLoc = new WorldLatLon();
            // Get the current latitude and longitude coordinates
            mstsskyworldLoc.ConvertWTC(MSTSSkyViewer.Camera.TileX, MSTSSkyViewer.Camera.TileZ, MSTSSkyViewer.Camera.Location, ref mstsskylatitude, ref mstsskylongitude);
            mstsskyseasonType = (int)MSTSSkyViewer.Simulator.Season;
            date.OrdinalDate = mstsskylatitude >= 0 ? 82 + mstsskyseasonType * 91 : (82 + (mstsskyseasonType + 2) * 91) % 365;
            date.Month = 1 + date.OrdinalDate / 30;
            date.Day = 21;
            date.Year = 2017;
            float fractClockTime = (float)MSTSSkyViewer.Simulator.ClockTime / 86400;
            mstsskysolarDirection = SunMoonPos.SolarAngle(mstsskylatitude, mstsskylongitude, fractClockTime, date);
            mstsskyworldLoc = null;
            mstsskylatitude = 0;
            mstsskylongitude = 0;
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            MSTSSkyMaterial.Mark();
        }
    }
    #endregion

    #region MSTSSkyMesh
    public class MSTSSkyMesh : RenderPrimitive
    {
        private VertexBuffer MSTSSkyVertexBuffer;
        private static IndexBuffer MSTSSkyIndexBuffer;
        public int drawIndex;
        VertexPositionNormalTexture[] vertexList;
        private static short[] triangleListIndices; // Trilist buffer.

        // Sky dome geometry is based on two global variables: the radius and the number of sides
        public int mstsskyRadius = MSTSSkyConstants.skyRadius;
        private static int mstsskySides = MSTSSkyConstants.skySides;
        public int mstscloudDomeRadiusDiff = 600;
        // skyLevels: Used for iterating vertically through the "levels" of the hemisphere polygon
        private static int mstsskyLevels =  MSTSSkyConstants.skyLevels;
        private static float mstsskytextureu = MSTSSkyConstants.mstsskyTileu;
        private static float mstsskytexturev = MSTSSkyConstants.mstsskyTilev;
        private static float mstscloudtextureu = MSTSSkyConstants.mstscloudTileu;
        private static float mstscloudtexturev = MSTSSkyConstants.mstscloudTilev;
        // Number of vertices in the sky hemisphere. (each dome = 145 for 24-sided sky dome: 24 x 6 + 1)
        // plus four more for the moon quad
        private static int numVertices = 4 + 2 * (int)((mstsskyLevels + 1) * mstsskySides + 1);
        // Number of point indices (each dome = 792 for 24 sides: 5 levels of 24 triangle pairs each
        // plus 24 triangles at the zenith)
        // plus six more for the moon quad
        private static short indexCount = 6 + 2 * ((MSTSSkyConstants.skySides * 6 * ((MSTSSkyConstants.skyLevels + 3)) + 3 * MSTSSkyConstants.skySides));
        /// <summary>
        ///

        /// Constructor
        public MSTSSkyMesh(RenderProcess renderProcess)
        {
            // Initialize the vertex and point-index buffers
            vertexList = new VertexPositionNormalTexture[numVertices];
            triangleListIndices = new short[indexCount];
            // Sky dome
            //MSTSSkyDomeVertexList((numVertices - 4) / 2, mstsskyRadius, 0f, mstsskytextureu, mstsskytexturev);
            MSTSSkyDomeVertexList(0, mstsskyRadius, mstsskytextureu, mstsskytexturev);
            MSTSSkyDomeTriangleList(0, 0);
            // Cloud dome
            MSTSSkyDomeVertexList((numVertices - 4) / 2, mstsskyRadius - mstscloudDomeRadiusDiff, mstscloudtextureu, mstscloudtexturev);
            MSTSSkyDomeTriangleList((short)((indexCount - 6) / 2), 1);
            // Moon quad
            MoonLists(numVertices - 5, indexCount - 6);//(144, 792);
            // Meshes have now been assembled, so put everything into vertex and index buffers
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
        }
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.SetVertexBuffer(MSTSSkyVertexBuffer);
            graphicsDevice.Indices = MSTSSkyIndexBuffer;

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
                        baseVertex: 4,
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
        private void MSTSSkyDomeVertexList(int index, int radius, float tile_u, float tile_v)
        {
            int vertexIndex = index;

            // for each vertex
            for (int i = 0; i <= (mstsskyLevels); i++) // (=6 for 24 sides)
            {
                // The "oblate" factor is used to flatten the dome to an ellipsoid. Used for the inner (cloud)
                // dome only. Gives the clouds a flatter appearance.
                float y = (float)Math.Sin(MathHelper.ToRadians((360 / mstsskySides) * (i - 1))) * radius; //  oblate;
                float yRadius = radius * (float)Math.Cos(MathHelper.ToRadians((360 / mstsskySides) * (i - 1)));
                for (int j = 0; j < mstsskySides; j++) // (=24 for top overlay)
                {

                    float x = (float)Math.Cos(MathHelper.ToRadians((360 / mstsskySides) * (mstsskySides - j))) * yRadius;
                    float z = (float)Math.Sin(MathHelper.ToRadians((360 / mstsskySides) * (mstsskySides - j))) * yRadius;

                    // UV coordinates - top overlay
                    float uvRadius;
                    uvRadius = (0.5f - (float)(0.5f * (i - 1)) / mstsskyLevels );
                    float uv_u = tile_u * (0.5f - ((float)Math.Cos(MathHelper.ToRadians((360 / mstsskySides) * (mstsskySides - j))) * uvRadius));
                    float uv_v = tile_v * (0.5f - ((float)Math.Sin(MathHelper.ToRadians((360 / mstsskySides) * (mstsskySides - j))) * uvRadius ));

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
            vertexList[vertexIndex].TextureCoordinate = new Vector2(0.5f * tile_u, 0.5f *tile_v); // (top overlay)
        }

        /// <summary>
        /// Creates the triangle index list for each dome.
        /// </summary>
        /// <param name="index">The starting triangle index number</param>
        /// <param name="pass">A multiplier used to arrive at the starting vertex number</param>
        /// 
        static void MSTSSkyDomeTriangleList(short index, short pass)
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
            for (int i = 0; i < mstsskyLevels; i++) // (=5 for 24 sides)
                for (int j = 0; j < mstsskySides; j++) // (=24 for 24 sides)
                {
                    // Vertex indices, beginning in the southwest corner
                    short sw = (short)(baseVert + (j + i * (mstsskySides)));
                    short nw = (short)(sw + mstsskySides); // top overlay mapping
                    short ne = (short)(nw + 1);

                    short se = (short)(sw + 1);

                    if (((i & 1) == (j & 1)))  // triangles alternate
                    {
                        triangleListIndices[iIndex++] = sw;
                        triangleListIndices[iIndex++] = ((ne - baseVert) % mstsskySides == 0) ? (short)(ne - mstsskySides) : ne;
                        triangleListIndices[iIndex++] = nw;
                        triangleListIndices[iIndex++] = sw;
                        triangleListIndices[iIndex++] = ((se - baseVert) % mstsskySides == 0) ? (short)(se - mstsskySides) : se;
                        triangleListIndices[iIndex++] = ((ne - baseVert) % mstsskySides == 0) ? (short)(ne - mstsskySides) : ne;
                    }
                    else
                    {
                        triangleListIndices[iIndex++] = sw;
                        triangleListIndices[iIndex++] = ((se - baseVert) % mstsskySides == 0) ? (short)(se - mstsskySides) : se;
                        triangleListIndices[iIndex++] = nw;
                        triangleListIndices[iIndex++] = ((se - baseVert) % mstsskySides == 0) ? (short)(se - mstsskySides) : se;
                        triangleListIndices[iIndex++] = ((ne - baseVert) % mstsskySides == 0) ? (short)(ne - mstsskySides) : ne;
                        triangleListIndices[iIndex++] = nw;
                    }
                }
            //Zenith triangles (=24 for 24 sides)
            for (int i = 0; i < mstsskySides; i++)
            {
                short sw = (short)(baseVert + (((mstsskySides) * mstsskyLevels) + i));
                short se = (short)(sw + 1);

                triangleListIndices[iIndex++] = sw;
                triangleListIndices[iIndex++] = ((se - baseVert) % mstsskySides == 0) ? (short)(se - mstsskySides) : se;
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
            MSTSSkyVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertexList.Length, BufferUsage.WriteOnly);
            MSTSSkyVertexBuffer.SetData(vertexList);
            if (MSTSSkyIndexBuffer == null)
            {
                MSTSSkyIndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indexCount, BufferUsage.WriteOnly);
                MSTSSkyIndexBuffer.SetData(triangleListIndices);
            }
        }

    } // SkyMesh
    #endregion

    #region MSTSSkyMaterial
    public class MSTSSkyMaterial : Material
    {
        SkyShader MSTSSkyShader;
        Texture2D MSTSDayTexture;
        List<Texture2D> MSTSSkyTexture = new List<Texture2D>();
        Texture2D MSTSSkyStarTexture;
        Texture2D MSTSSkyMoonTexture;
        Texture2D MSTSSkyMoonMask;
        List<Texture2D> MSTSSkyCloudTexture = new List<Texture2D>();
        Texture2D MSTSSkySunTexture;
        private Matrix XNAMoonMatrix;
        IEnumerator<EffectPass> ShaderPassesSky;
        IEnumerator<EffectPass> ShaderPassesMoon;
        List<IEnumerator<EffectPass>>ShaderPassesClouds = new List<IEnumerator<EffectPass>>();
        private float mstsskytexturex;
        private float mstsskytexturey;
        private float mstscloudtexturex;
        private float mstscloudtexturey;

        public MSTSSkyMaterial(Viewer viewer)
            : base(viewer, null)
        {
            MSTSSkyShader = Viewer.MaterialManager.SkyShader;
            
            //// TODO: This should happen on the loader thread. 
            if (viewer.ENVFile.SkyLayers != null)
            {
                var mstsskytexture = Viewer.ENVFile.SkyLayers.ToArray();
                int count = Viewer.ENVFile.SkyLayers.Count;
                

                string[] mstsSkyTexture = new string[Viewer.ENVFile.SkyLayers.Count];

                for (int i = 0; i < Viewer.ENVFile.SkyLayers.Count; i++)
                {
                    mstsSkyTexture[i] = Viewer.Simulator.RoutePath + @"\envfiles\textures\" + mstsskytexture[i].TextureName.ToString();
                    MSTSSkyTexture.Add(Orts.Formats.Msts.AceFile.Texture2DFromFile(Viewer.RenderProcess.GraphicsDevice, mstsSkyTexture[i]));
                    if( i == 0 )
                    {
                        MSTSDayTexture = MSTSSkyTexture[i];
                        mstsskytexturex = mstsskytexture[i].TileX;
                        mstsskytexturey = mstsskytexture[i].TileY;

                    }
                    else if(mstsskytexture[i].Fadein_Begin_Time != null)
                    {
                        MSTSSkyStarTexture = MSTSSkyTexture[i];
                        mstsskytexturex = mstsskytexture[i].TileX;
                        mstsskytexturey = mstsskytexture[i].TileY;
                    }
                    else
                    {
                        MSTSSkyCloudTexture.Add(Orts.Formats.Msts.AceFile.Texture2DFromFile(Viewer.RenderProcess.GraphicsDevice, mstsSkyTexture[i]));
                        mstscloudtexturex = mstsskytexture[i].TileX;
                        mstscloudtexturey = mstsskytexture[i].TileY;
                    }
                }

                MSTSSkyConstants.mstsskyTileu = mstsskytexturex;
                MSTSSkyConstants.mstsskyTilev = mstsskytexturey;
                MSTSSkyConstants.mstscloudTileu = mstscloudtexturex;
                MSTSSkyConstants.mstscloudTilev = mstscloudtexturey;
            }
            else
            {
                MSTSSkyTexture.Add(SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "SkyDome1.png")));
                MSTSSkyStarTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Starmap_N.png"));
            }
            if (viewer.ENVFile.SkySatellite != null)
            {
                var mstsskysatellitetexture = Viewer.ENVFile.SkySatellite.ToArray();

                string mstsSkySunTexture = Viewer.Simulator.RoutePath + @"\envfiles\textures\" + mstsskysatellitetexture[0].TextureName.ToString();
                string mstsSkyMoonTexture = Viewer.Simulator.RoutePath + @"\envfiles\textures\" + mstsskysatellitetexture[1].TextureName.ToString();

                MSTSSkySunTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, mstsSkySunTexture);
                MSTSSkyMoonTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, mstsSkyMoonTexture);
            }
            else
                MSTSSkyMoonTexture = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "MoonMap.png"));

            MSTSSkyMoonMask = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "MoonMask.png")); //ToDo:  No MSTS equivalent - will need to be fixed in MSTSSky.cs
            //MSTSSkyCloudTexture[0] = SharedTextureManager.Get(Viewer.RenderProcess.GraphicsDevice, System.IO.Path.Combine(Viewer.ContentPath, "Clouds01.png"));

            ShaderPassesSky = MSTSSkyShader.Techniques["Sky"].Passes.GetEnumerator();
            ShaderPassesMoon = MSTSSkyShader.Techniques["Moon"].Passes.GetEnumerator();


            for (int i = 0; i < Viewer.ENVFile.SkyLayers.Count - 2; i++)
            {
                ShaderPassesClouds.Add(MSTSSkyShader.Techniques["Clouds"].Passes.GetEnumerator());
            }

            MSTSSkyShader.SkyMapTexture = MSTSDayTexture;
            MSTSSkyShader.StarMapTexture = MSTSSkyStarTexture;
            MSTSSkyShader.MoonMapTexture = MSTSSkyMoonTexture;
            MSTSSkyShader.MoonMaskTexture = MSTSSkyMoonMask;
            MSTSSkyShader.CloudMapTexture = MSTSSkyCloudTexture[0];
        }
        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            // Adjust Fog color for day-night conditions and overcast
            FogDay2Night(
                Viewer.World.MSTSSky.mstsskysolarDirection.Y,
                Viewer.World.MSTSSky.mstsskyovercastFactor);


            //if (Viewer.Settings.DistantMountains) SharedMaterialManager.FogCoeff *= (3 * (5 - Viewer.Settings.DistantMountainsFogValue) + 0.5f);

            if (Viewer.World.MSTSSky.mstsskylatitude > 0) // TODO: Use a dirty flag to determine if it is necessary to set the texture again
                MSTSSkyShader.StarMapTexture = MSTSSkyStarTexture;
            MSTSSkyShader.Random = Viewer.World.MSTSSky.mstsskymoonPhase; // Keep setting this before LightVector for the preshader to work correctly
            MSTSSkyShader.LightVector = Viewer.World.MSTSSky.mstsskysolarDirection;
            MSTSSkyShader.Time = (float)Viewer.Simulator.ClockTime / 100000;
            MSTSSkyShader.MoonScale = MSTSSkyConstants.skyRadius / 20;
            MSTSSkyShader.Overcast = Viewer.World.MSTSSky.mstsskyovercastFactor;
            MSTSSkyShader.SetFog(Viewer.World.MSTSSky.mstsskyfogDistance, ref SharedMaterialManager.FogColor);
            MSTSSkyShader.WindSpeed = Viewer.World.MSTSSky.mstsskywindSpeed;
            MSTSSkyShader.WindDirection = Viewer.World.MSTSSky.mstsskywindDirection; // Keep setting this after Time and Windspeed. Calculating displacement here.

            for (var i = 0; i < 5; i++)
                graphicsDevice.SamplerStates[i] = SamplerState.LinearWrap;

            // Sky dome
            graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            Matrix viewXNASkyProj = XNAViewMatrix * Camera.XNASkyProjection;

            MSTSSkyShader.CurrentTechnique = MSTSSkyShader.Techniques["Sky"];
            Viewer.World.MSTSSky.MSTSSkyMesh.drawIndex = 1;
            MSTSSkyShader.SetViewMatrix(ref XNAViewMatrix);
            ShaderPassesSky.Reset();
            while (ShaderPassesSky.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                    MSTSSkyShader.SetMatrix(ref wvp);
                    ShaderPassesSky.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
            MSTSSkyShader.CurrentTechnique = MSTSSkyShader.Techniques["Moon"];
            Viewer.World.MSTSSky.MSTSSkyMesh.drawIndex = 2;

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            // Send the transform matrices to the shader
            int mstsskyRadius = Viewer.World.MSTSSky.MSTSSkyMesh.mstsskyRadius;
            int mstscloudRadiusDiff = Viewer.World.MSTSSky.MSTSSkyMesh.mstscloudDomeRadiusDiff;
            XNAMoonMatrix = Matrix.CreateTranslation(Viewer.World.MSTSSky.mstsskylunarDirection * (mstsskyRadius));
            Matrix XNAMoonMatrixView = XNAMoonMatrix * XNAViewMatrix;

            ShaderPassesMoon.Reset();
            while (ShaderPassesMoon.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    Matrix wvp = item.XNAMatrix * XNAMoonMatrixView * Camera.XNASkyProjection;
                    MSTSSkyShader.SetMatrix(ref wvp);
                    ShaderPassesMoon.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }

            for (int i = 0; i < MSTSSkyCloudTexture.Count; i++)
                if (i == 0)
                {

                    MSTSSkyShader.CurrentTechnique = MSTSSkyShader.Techniques["Clouds"];
                    Viewer.World.MSTSSky.MSTSSkyMesh.drawIndex = 3;

                    graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

                    ShaderPassesClouds[0].Reset();
                    while (ShaderPassesClouds[0].MoveNext())
                    {
                        foreach (var item in renderItems)
                        {
                            Matrix wvp = item.XNAMatrix * viewXNASkyProj;
                            MSTSSkyShader.SetMatrix(ref wvp);
                            ShaderPassesClouds[0].Current.Apply();
                            item.RenderPrimitive.Draw(graphicsDevice);
                        }
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
    }
    #endregion
}
