/* OVERHEAD WIRE
 * 
 * Overhead wire is generated procedurally from data in the track database.
 * 
 */
/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:

/// Contributors:

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
using MSTS;
using System.Threading;

namespace ORTS
{
    /// <summary>
    /// Created by a Viewer
    /// </summary>
    public class WireDrawer
    {
        private Viewer3D Viewer;  // the viewer that we are tracking
        int viewerTileX, viewerTileZ;  // position of the viewer updated once per frame
        List<WirePrimitive> WirePrimitives = new List<WirePrimitive>();  // the currently loaded wire geometry
        //WireMaterial WireMaterial;

        /// <summary>
        /// Called once after the graphics device is ready
        /// to load any static graphics content, background 
        /// processes haven't started yet.
        /// Executes in the RenderProcess thread.
        /// </summary>
        public WireDrawer(Viewer3D viewer)
        {
            Viewer = viewer;

            // load any static content, ie spritebatches, textures, etc

            // if all wire uses the same material,  create your material here.
            WireMaterial WireMaterial = new WireMaterial(viewer.RenderProcess);
        }

        /// <summary>
        /// Called 10 times per second when its safe to read volatile data
        /// from the simulator and viewer classes in preparation
        /// for the Load call.  Copy data to local storage for use 
        /// in the next load call.
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        public void LoadPrep()
        {
            viewerTileX = Viewer.Camera.TileX;
            viewerTileZ = Viewer.Camera.TileZ;

            // read any other volatile data that will be needed by Load
        }

        /// <summary>
        /// Called 10 times a second to load graphics content
        /// that comes and goes as the player and trains move.
        /// Called from background LoaderProcess Thread
        /// Do not access volatile data from the simulator 
        /// and viewer classes during the Load call ( see
        /// LoadPrep() )
        /// Executes in the LoaderProcess thread.
        /// Do not read volatile data managed by the UpdaterProcess
        /// </summary>
        public void Load(RenderProcess renderProcess)
        {
            // This is where the wire primitives list is updated and procedural geometry (perhaps line lists ) is created
            // Unload from WirePrimitives, any wire geometry that is out of viewing range from the 
            // Add to WirePrimitives, geometry for wire coming into viewing range
            // ie for each track segment (TDB) that is in range
            //        WirePrimitives.Add(new WirePrimitive(....));

        }


        /// <summary>
        /// Called every frame to update animations and load the frame contents .
        /// Note:  this doesn't actually draw on the screen surface, but 
        /// instead prepares a list of drawing primitives that will be rendered
        /// later in RenderFrame.Draw() by the RenderProcess thread.
        /// elapsedTime represents the the time since the last call to PrepareFrame
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // THREAD SAFETY WARNING - LoaderProcess could write to WirePrimitives list at any time
            // its probably not OK to iterate through this list because LoaderProcess could change its size at any time
            foreach ( WirePrimitive wirePrimitive in WirePrimitives)
            {
                // if ( wire primitive is in field of view of camera )
                //    frame.AddPrimitive( WireMaterial, wireprimitive, wireprimitive.xnaMatrix );
            }
        }
    } // SceneryDrawer


    /// <summary>
    /// This encapsulates any shaders, sprites, etc needed by the material.
    /// </summary>
	public class WireMaterial : Material
	{
		public WireMaterial(RenderProcess renderProcess)
			: base(null)
		{
			// create a shader if necessary
			// load any static textures etc
		}

		/// <summary>
		/// Called by RenderFrame.Draw() in the RenderProcess thread for each primitive
		/// that was loaded by PrepareFrame
		/// </summary>
        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
		{
			foreach(var item in renderItems)
			    item.RenderPrimitive.Draw(graphicsDevice);
		}
	}

    public class WirePrimitive : RenderPrimitive
    {
        //Matrix xnaMatrix;
        // LineLists etc

        /// <summary>
        /// This is when the object actually renders itself onto the screen.
        /// Do not reference any volatile data.
        /// Executes in the RenderProcess thread called from the Render method of the material class
        /// </summary>
        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // do any draw calls on the graphics device 
            // ie graphicsDevice.Draw( .... )
        }
    }




}
