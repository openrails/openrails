/// These are thin wrappers for the  .FX files 
/// 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
#endregion

namespace ORTS
{
    /// <summary>
    /// Wrapper for SceneryShader.fx
    /// </summary>
    public class SceneryShader : Effect
    {
        public Matrix XNAWorld
        {
            get { return XNAMatrix; }
        }
            Matrix XNAMatrix;

        public Texture2D Texture
        {
            set { imageMap_Tex.SetValue(value); }
        }

        public Texture2D BumpTexture
        {
            set { normalMap_Tex.SetValue(value); }
        }

        public float Brightness
        {
            get { return brightnessValue; }
            set { brightnessValue = value; brightness.SetValue(brightnessValue); }
        }
            private float brightnessValue = 0.7f;

        public float Saturation
        {
            get { return saturationValue; }
            set { saturationValue = value; saturation.SetValue(saturationValue); }
        }
            private float saturationValue = 0.9f;

        public float Ambient
        {
            get { return ambientValue; }
            set { ambientValue = value; ambient.SetValue(ambientValue); }
        }
            private float ambientValue = 0.5f;

        public float ZBias
        {
            get { return zbiasValue; }
            set { zbiasValue = value; zbias.SetValue(zbiasValue); }
        }
            private float zbiasValue = 0.0f;

        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            mModelToProjection.SetValueTranspose((world * view) * projection);
            mWorldToView.SetValue(Matrix.Invert(view));
            mModelToWorld.SetValue(world);
            XNAMatrix = world;
        }

        EffectParameter mModelToProjection = null;
        EffectParameter mWorldToView = null;
        EffectParameter mModelToWorld = null;
        EffectParameter imageMap_Tex = null;
        EffectParameter normalMap_Tex = null;
        EffectParameter brightness = null;
        EffectParameter saturation = null;
        EffectParameter ambient = null;
        EffectParameter zbias = null;  // TODO TEST

        public SceneryShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("SceneryShader"))
        {
            mModelToProjection = Parameters["mModelToProjection"];
            mWorldToView = Parameters["mWorldToView"];
            mModelToWorld = Parameters["mModelToWorld"];
            imageMap_Tex = Parameters["imageMap_Tex"];
            normalMap_Tex = Parameters["normalMap_Tex"];
            brightness = Parameters["Brightness"];
            saturation = Parameters["Saturation"];
            ambient = Parameters["Ambient"];
            zbias = Parameters["ZBias"];  // TODO TEST

            Parameters["LightVector"].SetValue(Vector3.Normalize(new Vector3(1f, .3f, 1f)));
        }
    }


    /// <summary>
    /// Wrapper for DrawModel.fx
    /// </summary>
    public class ShadowMappingShader : Effect
    {
        public Matrix World { set { pWorld.SetValue(value); } } EffectParameter pWorld = null;
        public Matrix View { set { pView.SetValue(value); } } EffectParameter pView = null;
        public Matrix Projection { set { pProjection.SetValue(value); } } EffectParameter pProjection = null;
        public Matrix LightViewProj { set { pLightViewProj.SetValue(value); } } EffectParameter pLightViewProj = null;
        public Color AmbientColor { set { pAmbientColor.SetValue(value.ToVector4()); } } EffectParameter pAmbientColor = null;
        public Vector3 LightDirection { set { pLightDirection.SetValue(value); } } EffectParameter pLightDirection = null;
        public float DepthBias { set { pDepthBias.SetValue(value); } } EffectParameter pDepthBias = null;
        public Texture2D Texture { set { pTexture.SetValue(value); } } EffectParameter pTexture = null;
        public Texture2D ShadowMap { set { pShadowMap.SetValue(value); } } EffectParameter pShadowMap = null;

        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            World = world;
            View = view;
            Projection = projection;
        }

        public ShadowMappingShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("DrawModel"))
        {
            pWorld = Parameters["World"];
            pView = Parameters["View"];
            pProjection = Parameters["Projection"];
            pLightViewProj = Parameters["LightViewProj"];
            pAmbientColor = Parameters["AmbientColor"];
            pLightDirection = Parameters["LightDirection"];
            pDepthBias = Parameters["DepthBias"];
            pTexture = Parameters["Texture"];
            pShadowMap = Parameters["ShadowMap"];
        }

    }
}
