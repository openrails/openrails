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
            if (RenderProcess.ShadowMappingOn)
                DrawWithShadows(graphicsDevice);
            else
                DrawSimple(graphicsDevice);
        }


        /// <summary>
        /// Draw With Shadows  - NOT FINISHED
        /// Executed in the RenderProcess thread 
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public void DrawWithShadows(GraphicsDevice graphicsDevice)
        {
            // Update the lights ViewProjection matrix based on the 
            // current camera frustum
            lightViewProjection = CreateLightViewProjectionMatrix();

            // Render the scene to the shadow map
            CreateShadowMap( graphicsDevice);

            
            // Draw the scene using the shadow map
            graphicsDevice.Clear(Materials.FogColor);

            Materials.ShadowMappingShader.LightViewProj = lightViewProjection;
            Materials.ShadowMappingShader.LightDirection = lightDir;
            Materials.ShadowMappingShader.ShadowMap = RenderProcess.shadowMap;

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

            // Display the shadow map to the screen
            DrawShadowMapToScreen();

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

        /// <summary>
        /// Render the shadow map texture to the screen
        /// </summary>
        void DrawShadowMapToScreen()
        {
            SpriteBatch spriteBatch = Materials.SpriteBatchMaterial.SpriteBatch;
            
            spriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate,
                              SaveStateMode.SaveState);
            spriteBatch.Draw( RenderProcess.shadowMap, new Rectangle(0, 0, 128, 128), Color.White);
            spriteBatch.End();
        }



        /// <summary>
        /// Creates the WorldViewProjection matrix from the perspective of the 
        /// light using the cameras bounding frustum to determine what is visible 
        /// in the scene.
        /// </summary>
        /// <returns>The WorldViewProjection for the light</returns>
        Matrix CreateLightViewProjectionMatrix()
        {
            // Set the new frustum value TODO , move to updateprocess
            cameraFrustum.Matrix = this.XNAViewMatrix * Camera.XNAShadowViewProjection; // TODO RESTORE this.XNAProjectionMatrix; 

            // Matrix with that will rotate in points the direction of the light
            Matrix lightRotation = Matrix.CreateLookAt(Vector3.Zero,
                                                       -lightDir,
                                                       new Vector3( 0,0,1 ) );

            // Get the corners of the frustum
            Vector3[] frustumCorners = cameraFrustum.GetCorners();

            // limit size of the shadowed area
            /* TODO MAKE THIS WORK
            float cameraX = frustumCorners[0].X;
            float cameraZ = frustumCorners[0].Z;
            float cameraY = frustumCorners[0].Y;
            for (int i = 4; i < frustumCorners.Length; i++)
            {
                float dx = frustumCorners[i].X - cameraX;
                if (dx > RenderProcess.ShadowDistanceLimit)
                    frustumCorners[i].X = cameraX + RenderProcess.ShadowDistanceLimit;
                if (dx < RenderProcess.ShadowDistanceLimit)
                    frustumCorners[i].X = cameraX - RenderProcess.ShadowDistanceLimit;

                float dz = frustumCorners[i].Z - cameraZ;
                if (dz > RenderProcess.ShadowDistanceLimit)
                    frustumCorners[i].Z = cameraZ + RenderProcess.ShadowDistanceLimit;
                if (dz < RenderProcess.ShadowDistanceLimit)
                    frustumCorners[i].Z = cameraZ - RenderProcess.ShadowDistanceLimit;

                float dy = frustumCorners[i].Y - cameraY;
                if (dy > RenderProcess.ShadowDistanceLimit)
                    frustumCorners[i].Y = cameraY + RenderProcess.ShadowDistanceLimit;
                if (dy < RenderProcess.ShadowDistanceLimit)
                    frustumCorners[i].Y = cameraY - RenderProcess.ShadowDistanceLimit;
            }*/

            // Transform the positions of the corners into the direction of the light
            for (int i = 0; i < frustumCorners.Length; i++)
            {
                frustumCorners[i] = Vector3.Transform(frustumCorners[i], lightRotation);
            }

            // Find the smallest box around the points
            BoundingBox lightBox = BoundingBox.CreateFromPoints(frustumCorners);

            Vector3 boxSize = lightBox.Max - lightBox.Min;
            Vector3 halfBoxSize = boxSize * 0.5f;

            // The position of the light should be in the center of the back
            // pannel of the box. 
            Vector3 lightPosition = lightBox.Min + halfBoxSize;
            lightPosition.Z = lightBox.Min.Z;

            // We need the position back in world coordinates so we transform 
            // the light position by the inverse of the lights rotation
            lightPosition = Vector3.Transform(lightPosition,
                                              Matrix.Invert(lightRotation));

            // Create the view matrix for the light
            Matrix lightView = Matrix.CreateLookAt(lightPosition,
                                                   lightPosition - lightDir,
                                                   new Vector3(0, 0, 1));

            // Create the projection matrix for the light
            // The projection is orthographic since we are using a directional light
            Matrix lightProjection = Matrix.CreateOrthographic(boxSize.X, boxSize.Y,
                                                               -boxSize.Z, boxSize.Z);

            return lightView * lightProjection;
        }

        /// <summary>
        /// Renders the scene to the floating point render target then 
        /// sets the texture for use when drawing the scene.
        /// </summary>
        void CreateShadowMap( GraphicsDevice graphicsDevice)
        {
            // Set our render target to our floating point render target
            graphicsDevice.SetRenderTarget(0, RenderProcess.shadowRenderTarget);
            // Save the current stencil buffer
            DepthStencilBuffer oldDepthBuffer = graphicsDevice.DepthStencilBuffer;
            // Set the graphics device to use the shadow depth stencil buffer
            graphicsDevice.DepthStencilBuffer = RenderProcess.shadowDepthBuffer;

            // Clear the render target to white or all 1's
            // We set the clear to white since that represents the 
            // furthest the object could be away
            graphicsDevice.Clear(Color.White);
            
            // Draw any occluders in our case that is just the dude model
            Materials.ShadowMaterial.SetState(graphicsDevice, lightViewProjection, lightDir);
            for (int i = 0; i < RenderItemCount; ++i)
            {
                if (RenderItems[i].Material.GetType() == typeof(SceneryMaterial))              // TODO only draw shadow casters
                    Materials.ShadowMaterial.Render(graphicsDevice, null, RenderItems[i].RenderPrimitive,
                        ref RenderItems[i].XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
            }
            Materials.ShadowMaterial.ResetState(graphicsDevice, null);
            

            // Set render target back to the back buffer
            graphicsDevice.SetRenderTarget(0, null);
            // Reset the depth buffer
            graphicsDevice.DepthStencilBuffer = oldDepthBuffer;

            // Return the shadow map as a texture
            RenderProcess.shadowMap = RenderProcess.shadowRenderTarget.GetTexture();
        }


    }


}
