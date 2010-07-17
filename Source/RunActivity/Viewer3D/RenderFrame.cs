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
        public RenderPrimitive RenderPrimitive;
        public Matrix XNAMatrix;

        public RenderItem(RenderPrimitive renderPrimitive, Matrix xnaMatrix)
        {
            RenderPrimitive = renderPrimitive;
            XNAMatrix = xnaMatrix;
        }
    }

    public class RenderFrame
    {
        Dictionary<Material, List<RenderItem>> RenderItems;
        int RenderMaxSequence = 0;
        Matrix XNAViewMatrix;
        Matrix XNAProjectionMatrix;
        RenderProcess RenderProcess;

        public RenderFrame(RenderProcess owner)
        {
            RenderProcess = owner;
            RenderItems = new Dictionary<Material, List<RenderItem>>();
        }

        public void Clear() 
        {
            foreach (var renderGroup in RenderItems)
                renderGroup.Value.Clear();
        }

        public void SetCamera(ref Matrix xnaViewMatrix, ref Matrix xnaProjectionMatrix)
        {
            XNAViewMatrix = xnaViewMatrix;
            XNAProjectionMatrix = xnaProjectionMatrix;
        }
        
        /// <summary>
        /// Executed in the UpdateProcess thread
        /// </summary>
        public void AddPrimitive(Material material, RenderPrimitive primitive, ref Matrix xnaMatrix) 
        {
            if (!RenderItems.ContainsKey(material))
                RenderItems[material] = new List<RenderItem>();

            RenderItems[material].Add(new RenderItem(primitive, xnaMatrix));

            if (RenderMaxSequence < primitive.Sequence)
                RenderMaxSequence = primitive.Sequence;

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
            Materials.UpdateShaders(RenderProcess, graphicsDevice);
            DrawSimple(graphicsDevice);
        }

        /// <summary>
        /// Executed in the RenderProcess thread - simple draw
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public void DrawSimple(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Materials.FogColor);

            for (int iSequence = 0; iSequence <= RenderMaxSequence; ++iSequence)
                DrawSequence(graphicsDevice, iSequence);

        }

        public void DrawSequence(GraphicsDevice graphicsDevice, int sequence)
        {
            Material prevMaterial = null;
            // Render each material on the specified primitive
            // To minimize renderstate changes, the material is
            // told what material was used previously so it can
            // make a decision on what renderstates need to be
            // changed.
            foreach (var renderGroup in RenderItems)
            {
                foreach (var renderItem in renderGroup.Value)
                {
                    var ri = renderItem;
                    if (renderItem.RenderPrimitive.Sequence == sequence)
                    {
                        Material currentMaterial = renderGroup.Key;
                        if (prevMaterial != null) prevMaterial.ResetState(graphicsDevice, currentMaterial);
                        currentMaterial.Render(graphicsDevice, prevMaterial, renderItem.RenderPrimitive, ref ri.XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                        prevMaterial = currentMaterial;
                    }
                }
            }
            if (prevMaterial != null)
                prevMaterial.ResetState(graphicsDevice, null);
        }
    }
}
