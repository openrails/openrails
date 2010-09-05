/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Rick Grout
/// Contributors:
///    Wayne Campbell
///     

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace ORTS
{
    #region SkyConstants
    class SkyConstants
    {
        // Sky dome constants
        public const int skyRadius = 6000;
        public const int skySides = 24;
    }
    #endregion

    #region SkyDrawer
    public class SkyDrawer
    {
        Viewer3D Viewer;
        Material skyMaterial;

        // Classes reqiring instantiation
        public SkyMesh SkyMesh;
        WorldLatLon worldLoc = null; // Access to latitude and longitude calcs (MSTS routes only)
        SunMoonPos skyVectors;

        #region Class variables
        // Latitude of current route in radians. -pi/2 = south pole, 0 = equator, pi/2 = north pole.
        // Longitude of current route in radians. -pi = west of prime, 0 = prime, pi = east of prime.
        public double latitude, longitude;
        // Date of activity
        public Vector4 sunpeakColor;
        public Vector4 sunriseColor;
        public Vector4 sunsetColor;
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
        // Overcast factor
        public float overcast;
        public float fogCoeff;
        public int seasonType;

        // These arrays and vectors define the position of the sun and moon in the world
        Vector3[] solarPosArray = new Vector3[72];
        Vector3[] lunarPosArray = new Vector3[72];
        public Vector3 solarDirection;
        public Vector3 lunarDirection;
        #endregion

        #region Constructor
        /// <summary>
        /// SkyDrawer constructor
        /// </summary>
        public SkyDrawer(Viewer3D viewer)
        {
            Viewer = viewer;
            skyMaterial = Materials.Load(Viewer.RenderProcess, "SkyMaterial");

            // Instantiate classes
            SkyMesh = new SkyMesh( Viewer.RenderProcess);
            skyVectors = new SunMoonPos();

            // Set default values
            seasonType = (int)Viewer.Simulator.Season;
            date.ordinalDate = 82 + seasonType * 91;
            // TODO: Set the following three externally from ORTS route files (future)
            date.month = 1 + date.ordinalDate / 30;
            date.day = 21;
            date.year = 2010;
            sunpeakColor = new Vector4(1.0f, 1.0f, 0.95f, 1.0f);
            sunriseColor = new Vector4(0.93f, 0.89f, 0.75f, 1.0f);
            sunsetColor = new Vector4(0.87f, 0.28f, 0.10f, 1.0f);
            // Default wind speed and direction
            windSpeed = 5.0f; // m/s (approx 11 mph)
            windDirection = 4.7f; // radians (approx 270 deg, i.e. westerly)
       }
        #endregion

        /// <summary>
        /// Used to update information affecting the SkyMesh
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Adjust dome position so the bottom edge is not visible
			Vector3 ViewerXNAPosition = new Vector3(Viewer.Camera.Location.X, Viewer.Camera.Location.Y - 100, -Viewer.Camera.Location.Z);
            Matrix XNASkyWorldLocation = Matrix.CreateTranslation(ViewerXNAPosition);

            if (worldLoc == null)
            {
                // First time around, initialize the following items:
                worldLoc = new WorldLatLon();
                oldClockTime = Viewer.Simulator.ClockTime;
                step1 = step2 = (int)(Viewer.Simulator.ClockTime / 1200);
                step2++;
                // Get the current latitude and longitude coordinates
                worldLoc.ConvertWTC(Viewer.Camera.TileX, Viewer.Camera.TileZ, Viewer.Camera.Location, ref latitude, ref longitude);
                // Fill in the sun- and moon-position lookup tables
                for (int i = 0; i < maxSteps; i++)
                {
                    solarPosArray[i] = skyVectors.SolarAngle(latitude, longitude, ((float)i / maxSteps), date);
                    lunarPosArray[i] = skyVectors.LunarAngle(latitude, longitude, ((float)i / maxSteps), date);
                }
                // Phase of the moon is generated at random
                Random random = new Random();
                moonPhase = random.Next(8);
                if (moonPhase == 6 && date.ordinalDate > 45 && date.ordinalDate < 330)
                    moonPhase = 3; // Moon dog only occurs in winter
                // Overcast factor: 0.0=almost no clouds; 0.1=wispy clouds; 1.0=total overcast
                overcast = Viewer.weatherControl.overcast;
                fogCoeff = Viewer.weatherControl.fogCoeff;
            }

////////////////////// T E M P O R A R Y ///////////////////////////

            // The following keyboard commands are used for viewing sky and weather effects in "demo" mode
            // The ( + ) key speeds the time forward, the ( - ) key reverses the time.
            // When the Ctrl key is also pressed, the + and - keys control the amount of overcast.

            if (UserInput.IsKeyDown(Keys.LeftControl))
            {
                if (UserInput.IsKeyDown(Keys.OemPlus))
                    overcast += 0.005f;
                if (UserInput.IsKeyDown(Keys.OemMinus))
                    overcast -= 0.005f;
                MathHelper.Clamp(overcast, 0, 1);
            }
            else
            {
                if (UserInput.IsKeyDown(Keys.OemPlus))
                {
                    Viewer.Simulator.ClockTime += 120; // Two-minute (120 second) increments
                    if( Viewer.PrecipDrawer != null ) Viewer.PrecipDrawer.Reset();
                }
                if (UserInput.IsKeyDown(Keys.OemMinus))
                {
                    Viewer.Simulator.ClockTime -= 120;
                    if( Viewer.PrecipDrawer != null ) Viewer.PrecipDrawer.Reset();
                }
            }

////////////////////////////////////////////////////////////////////

            // Current solar and lunar position are calculated by interpolation in the lookup arrays.
            // Using the Lerp() function, so need to calculate the in-between differential
            float diff = (float)(Viewer.Simulator.ClockTime - oldClockTime) / 1200;
            // The rest of this increments/decrements the array indices and checks for overshoot/undershoot.
            if (Viewer.Simulator.ClockTime >= (oldClockTime + 1200)) // Plus key, or normal forward in time
            {
                step1++;
                step2++;
                oldClockTime = Viewer.Simulator.ClockTime;
                diff = 0;
                if (step1 == maxSteps - 1) // Midnight. Value is 71 for maxSteps = 72
                {
                    step2 = 0;
                }
                if (step1 == maxSteps) // Midnight.
                {
                    step1 = 0;
                }
            }
            if (Viewer.Simulator.ClockTime <= (oldClockTime - 1200)) // Minus key
            {
                step1--;
                step2--;
                oldClockTime = Viewer.Simulator.ClockTime;
                diff = 0;
                if (step1 < 0) // Midnight.
                {
                    step1 = 71;
                }
                if (step2 < 0) // Midnight.
                {
                    step2 = 71;
                }
            }
            solarDirection.X = MathHelper.Lerp(solarPosArray[step1].X, solarPosArray[step2].X, diff);
            solarDirection.Y = MathHelper.Lerp(solarPosArray[step1].Y, solarPosArray[step2].Y, diff);
            solarDirection.Z = MathHelper.Lerp(solarPosArray[step1].Z, solarPosArray[step2].Z, diff);
            lunarDirection.X = MathHelper.Lerp(lunarPosArray[step1].X, lunarPosArray[step2].X, diff);
            lunarDirection.Y = MathHelper.Lerp(lunarPosArray[step1].Y, lunarPosArray[step2].Y, diff);
            lunarDirection.Z = MathHelper.Lerp(lunarPosArray[step1].Z, lunarPosArray[step2].Z, diff);

            frame.AddPrimitive(skyMaterial, SkyMesh, RenderPrimitiveGroup.World, ref XNASkyWorldLocation);
        }
    }
    #endregion

    #region SkyMesh
    public class SkyMesh: RenderPrimitive 
    {
        private VertexBuffer SkyVertexBuffer;
        private static VertexDeclaration SkyVertexDeclaration = null;
        private static IndexBuffer SkyIndexBuffer = null;
        private static int SkyVertexStride;  // in bytes
        public int drawIndex;

        VertexPositionNormalTexture[] vertexList;
        private static short[] triangleListIndices; // Trilist buffer.

        // Sky dome geometry is based on two global variables: the radius and the number of sides
        public int skyRadius = SkyConstants.skyRadius;
        private static int skySides = SkyConstants.skySides;
        public int cloudDomeRadiusDiff = 600; // Amount by which cloud dome radius is smaller than sky dome
        // skyLevels: Used for iterating vertically through the "levels" of the hemisphere polygon
        private static int skyLevels = ((SkyConstants.skySides / 4) - 1);
        // Number of vertices in the sky hemisphere. (each dome = 145 for 24-sided sky dome: 24 x 6 + 1)
        // plus four more for the moon quad
        private static int numVertices = 4 + 2 * (int)((Math.Pow(skySides, 2) / 4) + 1);
        // Number of point indices (each dome = 792 for 24 sides: 5 levels of 24 triangle pairs each
        // plus 24 triangles at the zenith)
        // plus six more for the moon quad
        private static short indexCount = 6 + 2 * ((SkyConstants.skySides * 2 * 3 * ((SkyConstants.skySides / 4) - 1)) + 3 * SkyConstants.skySides);

        /// <summary>
        /// Constructor.
        /// </summary>
        public SkyMesh(RenderProcess renderProcess)
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
            MoonLists(numVertices - 5, indexCount - 6);//(144, 792);
            // Meshes have now been assembled, so put everything into vertex and index buffers
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.VertexDeclaration = SkyVertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(SkyVertexBuffer, 0, SkyVertexStride);
            graphicsDevice.Indices = SkyIndexBuffer;

            switch (drawIndex)
            {
                case 1: // Sky dome
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        0,
                        (numVertices - 4) / 2,
                        0,
                        (indexCount - 6) / 6);
                    break;
                case 2: // Moon
                    graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0,
                    numVertices - 4,
                    4,
                    indexCount - 6,
                    2);
                    break;
                case 3: // Clouds Dome
                    graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        (numVertices - 4) / 2,
                        (numVertices - 4) / 2,
                        (indexCount - 6) / 2,
                        (indexCount - 6) / 6);
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
            // for each vertex
            for (int i = 0; i < (skySides / 4); i++) // (=6 for 24 sides)
                for (int j = 0; j < skySides; j++) // (=24 for top overlay)
                {
                    // The "oblate" factor is used to flatten the dome to an ellipsoid. Used for the inner (cloud)
                    // dome only. Gives the clouds a flatter appearance.
                    float y = (float)Math.Sin(MathHelper.ToRadians((360 / skySides) * i)) * radius * oblate;
                    float yRadius = radius * (float)Math.Cos(MathHelper.ToRadians((360 / skySides) * i));
                    float x = (float)Math.Cos(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * yRadius;
                    float z = (float)Math.Sin(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * yRadius;

                    // UV coordinates - top overlay
                    float uvRadius;
                    uvRadius = 0.5f - (float)(0.5f * i) / (skySides / 4);
                    float uv_u = 0.5f - ((float)Math.Cos(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * uvRadius);
                    float uv_v = 0.5f - ((float)Math.Sin(MathHelper.ToRadians((360 / skySides) * (skySides - j))) * uvRadius);

                    // Store the position, texture coordinates and normal (normalized position vector) for the current vertex
                    vertexList[vertexIndex].Position = new Vector3(x, y, z);
                    vertexList[vertexIndex].TextureCoordinate = new Vector2(uv_u, uv_v);
                    vertexList[vertexIndex].Normal = Vector3.Normalize(new Vector3(x, y, z));
                    vertexIndex++;
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
        private void DomeTriangleList(short index, short pass)
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
            for (int i = 0; i < skyLevels; i++) // (=5 for 24 sides)
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
            if (SkyVertexDeclaration == null)
            {
                SkyVertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
                SkyVertexStride = VertexPositionNormalTexture.SizeInBytes;
            }
            // Initialize the vertex and index buffers, allocating memory for each vertex and index
            SkyVertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * vertexList.Length, BufferUsage.WriteOnly);
            SkyVertexBuffer.SetData(vertexList);
            if (SkyIndexBuffer == null)
            {
                SkyIndexBuffer = new IndexBuffer(graphicsDevice, sizeof(short) * indexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                SkyIndexBuffer.SetData<short>(triangleListIndices);
            }
        }

    } // SkyMesh
    #endregion
}
