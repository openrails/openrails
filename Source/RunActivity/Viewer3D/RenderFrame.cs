using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace ORTS
{
    public abstract class RenderPrimitive
    {
        public float ZBias = 0f;
        public int Sequence = 0;

        /// <summary>
        /// This is when the object actually renders itself onto the screen.
        /// Do not reference any volatile data.
        /// Executes in the RenderProcess thread
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public abstract void Draw(GraphicsDevice graphicsDevice);
    }

    public struct RenderItem
    {
        public Material Material;
        public RenderPrimitive RenderPrimitive;
        public Matrix XNAMatrix;
    }

    public class RenderSorter : IComparer
    {
        // Group similar types (state changes), then group instance ( images )
        int IComparer.Compare(Object x, Object y)
        {
            RenderItem a = (RenderItem)x;
            RenderItem b = (RenderItem)y;
            if (a.Material.GetType() == typeof(SpriteBatchMaterial)) return 1;
            if (b.Material.GetType() == typeof(SpriteBatchMaterial)) return -1;
            if (a.Material.GetType().GetHashCode() > b.Material.GetType().GetHashCode()) return 1;
            if (a.Material.GetType().GetHashCode() < b.Material.GetType().GetHashCode()) return -1;
            if (a.Material.GetHashCode() > b.Material.GetHashCode()) return 1;
            if (a.Material.GetHashCode() < b.Material.GetHashCode()) return -1;
            return 0;
        }

    }

    public class RenderFrame
    {
        private int RenderItemCount = 0;
        RenderItem[] RenderItems = new RenderItem[10000];
        Matrix XNAViewMatrix;
        Matrix XNAProjectionMatrix;
        RenderProcess RenderProcess;

        public RenderFrame(RenderProcess owner)
        {
            RenderProcess = owner;
        }

        public void Clear() 
        { 
            RenderItemCount = 0;  
        }

        public void SetCamera(ref Matrix xnaViewMatrix, ref Matrix xnaProjectionMatrix)
        {
            XNAViewMatrix = xnaViewMatrix;
            XNAProjectionMatrix = xnaProjectionMatrix;
        }
        
        /// <summary>
        /// Executed in the UpdateProcess thread
        /// </summary>
        public void AddPrimitive( Material material, RenderPrimitive primitive, ref Matrix xnaMatrix ) 
        {
            if (RenderItemCount >= RenderItems.Length)
            {
                System.Diagnostics.Debug.Assert(false, "RenderItems Overflow");
                return;
            }
            RenderItems[RenderItemCount].Material = material;
            RenderItems[RenderItemCount].RenderPrimitive = primitive;
            RenderItems[RenderItemCount].XNAMatrix = xnaMatrix;
            ++RenderItemCount;

            // TODO, enhance this:
            //   - handle overflow by enlarging array etc
            //   - to accomodate separate list of shadow casters, 
        }


        /// <summary>
        /// Executed in the UpdateProcess thread
        /// </summary>
        public void Sort()
        {
            // TODO, enhance this:
            //   - to sort translucent primitives
            //   - and to minimize render state changes ( sorting was taking too long! for this )

        }


        BoundingFrustum cameraFrustum = new BoundingFrustum(Matrix.Identity);
        // Light direction
        public Vector3 lightDir =  new Vector3( 0.4f,.8f,0.4f); 
        // ViewProjection matrix from the lights perspective
        public Matrix lightViewProjection;

        /// <summary>
        /// Draw 
        /// Executed in the RenderProcess thread 
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public void Draw(GraphicsDevice graphicsDevice)
        {
              DrawSimple(graphicsDevice);
        }


        /// <summary>
        /// Executed in the RenderProcess thread - simple draw
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public void DrawSimple(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Materials.FogColor);

            for (int iSequence = 0; iSequence < 2; ++iSequence)
                DrawSequence(graphicsDevice, iSequence);

        }

        public void DrawSequence( GraphicsDevice graphicsDevice, int sequence )
        {
            // Render each material on the specified primitive
            // To minimize renderstate changes, the material is
            // told what material was used previously so it can
            // make a decision on what renderstates need to be
            // changed.
            Material prevMaterial = null;
            for (int i = 0; i < RenderItemCount; ++i)
            {
                if (RenderItems[i].RenderPrimitive.Sequence == sequence)
                {
                    Material currentMaterial = RenderItems[i].Material;
                    if (prevMaterial != null) prevMaterial.ResetState(graphicsDevice, currentMaterial);
                    currentMaterial.Render(graphicsDevice, prevMaterial, RenderItems[i].RenderPrimitive,
                            ref RenderItems[i].XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    prevMaterial = currentMaterial;
                }
            }
            if (prevMaterial != null)
                prevMaterial.ResetState(graphicsDevice, null);
        }

    }


}
