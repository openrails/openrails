using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace ORTS
{
    public interface RenderPrimitive
    {
        /// <summary>
        /// This is when the object actually renders itself onto the screen.
        /// Do not reference any volatile data.
        /// Executes in the RenderProcess thread
        /// </summary>
        /// <param name="graphicsDevice"></param>
        void Draw(GraphicsDevice graphicsDevice);
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
        bool[] RenderItemsDone = new bool[10000];  // true when rendered
        Matrix XNAViewMatrix;
        Matrix XNAProjectionMatrix;

        IComparer RenderSorter = new RenderSorter();

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

        public void Sort()
        {
            // TODO, enhance this:
            //   - to sort translucent primitives
            //   - and to minimize render state changes ( sorting was taking too long! for this )

        }


        /// <summary>
        /// Executed in the RenderProcess thread
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Color.CornflowerBlue);
            graphicsDevice.RenderState.DepthBias = 0f;
            
            // Render each material on the specified primitive
            // To minimize renderstate changes, the material is
            // told what material was used previously so it can
            // make a decision on what renderstates need to be
            // changed.
            Material prevMaterial = null;

            for (int i = 0; i < RenderItemCount; ++i)  // TODO should I keep this experiment tries to reduce image changes
                RenderItemsDone[i] = false; 

            for (int i = 0; i < RenderItemCount; ++i)
            {
                if (!RenderItemsDone[i])   // this experiment tries to reduce image changes
                {
                    Material currentMaterial = RenderItems[i].Material;
                    if (prevMaterial != null) prevMaterial.ResetState(graphicsDevice, currentMaterial);
                    currentMaterial.Render(graphicsDevice, prevMaterial, RenderItems[i].RenderPrimitive,
                            ref RenderItems[i].XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    prevMaterial = currentMaterial;
                    RenderItemsDone[i] = true;

                    // do any more now that have the same image
                    for (int j = i+1; j < RenderItemCount; ++j)
                        if (!RenderItemsDone[j] &&  RenderItems[j].Material == currentMaterial )
                        {
                            currentMaterial.Render(graphicsDevice, currentMaterial, RenderItems[j].RenderPrimitive,
                                    ref RenderItems[j].XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                            RenderItemsDone[j] = true;
                        }
                }
            }
            if (prevMaterial != null)
                prevMaterial.ResetState(graphicsDevice, null);
        }

        /// <summary>
        /// Executed in the RenderProcess thread
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public void DrawSimple(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Color.CornflowerBlue);
            graphicsDevice.RenderState.DepthBias = 0f;

            // Render each material on the specified primitive
            // To minimize renderstate changes, the material is
            // told what material was used previously so it can
            // make a decision on what renderstates need to be
            // changed.
            Material prevMaterial = null;
            for (int i = 0; i < RenderItemCount; ++i)
            {
                Material currentMaterial = RenderItems[i].Material;
                if (prevMaterial != null) prevMaterial.ResetState(graphicsDevice, currentMaterial);
                currentMaterial.Render(graphicsDevice, prevMaterial, RenderItems[i].RenderPrimitive,
                        ref RenderItems[i].XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                prevMaterial = currentMaterial;
            }
            if (prevMaterial != null)
                prevMaterial.ResetState(graphicsDevice, null);
        }
    }


}
