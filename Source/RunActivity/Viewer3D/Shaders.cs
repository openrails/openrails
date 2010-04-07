/// These are thin wrappers for the .FX files 
/// 
/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Wayne Campbell
/// Contributors:
///    Rick Grout
///     


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
    #region Scenery shader
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

        public bool isNightTexture
        {
            set { isNight_Tex.SetValue(value); }
        }

        public Vector3 ViewerPosition
        {
            set { viewerPos.SetValue(value); }
        }

        public float SpecularPower
        {
            set { specularPower.SetValue(value); }
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

        public Vector3 SunDirection
        {
            set { sunDirection.SetValue(value); }
        }

        public float Overcast
        {
            set { overcast.SetValue(value); }
        }

        public Vector3 HeadlightPosition
        {
            set { headlightPosition.SetValue(value); }
        }

        public Vector3 HeadlightDirection
        {
            set { headlightDirection.SetValue(value); }
        }

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
        EffectParameter isNight_Tex = null;
        EffectParameter viewerPos = null;
        EffectParameter specularPower = null;
        EffectParameter brightness = null;
        EffectParameter saturation = null;
        EffectParameter ambient = null;
        EffectParameter zbias = null;  // TODO TEST
        EffectParameter sunDirection = null;
        EffectParameter overcast = null;
        EffectParameter headlightPosition = null;
        EffectParameter headlightDirection = null;

        public SceneryShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("SceneryShader"))
        {
            mModelToProjection = Parameters["mModelToProjection"];
            mWorldToView = Parameters["mWorldToView"];
            mModelToWorld = Parameters["mModelToWorld"];
            imageMap_Tex = Parameters["imageMap_Tex"];
            normalMap_Tex = Parameters["normalMap_Tex"];
            isNight_Tex = Parameters["isNight_Tex"];
            viewerPos = Parameters["viewerPos"];
            specularPower = Parameters["specularPower"];
            brightness = Parameters["Brightness"];
            saturation = Parameters["Saturation"];
            ambient = Parameters["Ambient"];
            zbias = Parameters["ZBias"];  // TODO TEST
            sunDirection = Parameters["LightVector"];
            overcast = Parameters["overcast"];
            headlightPosition = Parameters["headlightPosition"];
            headlightDirection = Parameters["headlightDirection"];
        }
    }
    #endregion

    #region Shadow mapping shader
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
    #endregion

    #region Sky shader
    public class SkyShader : Effect
    {
        EffectParameter mModelToProjection = null;
        EffectParameter View = null;
        EffectParameter sunDirection = null;
        EffectParameter skyMap_Tex = null;
        EffectParameter starMap_Tex = null;
        EffectParameter moonMap_Tex = null;
        EffectParameter moonMask_Tex = null;
        EffectParameter cloudMap_Tex = null;
        EffectParameter sunpeakColor = null;
        EffectParameter sunriseColor = null;
        EffectParameter sunsetColor = null;
        EffectParameter time = null;
        EffectParameter random = null;
        EffectParameter overcast = null;
        EffectParameter windSpeed = null;
        EffectParameter windDirection = null;
        EffectParameter moonScale = null;

        public Vector3 SunDirection
        {
            set { sunDirection.SetValue(value); }
        }

        public Texture2D SkyTexture
        {
            set { skyMap_Tex.SetValue(value); }
        }
 
        public Texture2D StarTexture
        {
            set { starMap_Tex.SetValue(value); }
        }

        public Texture2D MoonTexture
        {
            set { moonMap_Tex.SetValue(value); }
        }

        public Texture2D MoonMaskTexture
        {
            set { moonMask_Tex.SetValue(value); }
        }

        public Texture2D CloudTexture
        {
            set { cloudMap_Tex.SetValue(value); }
        }

        public Vector4 SunpeakColor
        {
            set { sunpeakColor.SetValue(value); }
        }

        public Vector4 SunriseColor
        {
            set { sunriseColor.SetValue(value); }
        }

        public Vector4 SunsetColor
        {
            set { sunsetColor.SetValue(value); }
        }

        public float Time
        {
            set { time.SetValue(value); }
        }

        public float Random
        {
            set { random.SetValue(value); }
        }

        public float Overcast
        {
            set { overcast.SetValue(value); }
        }

        public float WindSpeed
        {
            set { windSpeed.SetValue(value); }
        }

        public float WindDirection
        {
            set { windDirection.SetValue(value); }
        }

        public float MoonScale
        {
            set { moonScale.SetValue(value); }
        }

        public SkyShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("SkyShader"))
        {
            mModelToProjection = Parameters["mModelToProjection"];
            View = Parameters["mView"];
            skyMap_Tex = Parameters["skyMap_Tex"];
            sunDirection = Parameters["LightVector"];
            starMap_Tex = Parameters["starMap_Tex"];
            sunpeakColor = Parameters["sunpeakColor"];
            sunriseColor = Parameters["sunriseColor"];
            sunsetColor = Parameters["sunsetColor"];
            moonMap_Tex = Parameters["moonMap_Tex"];
            moonMask_Tex = Parameters["moonMask_Tex"];
            cloudMap_Tex = Parameters["cloudMap_Tex"];
            time = Parameters["time"];
            random = Parameters["random"];
            overcast = Parameters["overcast"];
            windSpeed = Parameters["windSpeed"];
            windDirection = Parameters["windDirection"];
            moonScale = Parameters["moonScale"];
        }

        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            mModelToProjection.SetValueTranspose((world * view) * projection);
            View.SetValue(view);
        }
    }
    #endregion

    #region Precipitation shader
    public class PrecipShader : Effect
    {
        EffectParameter mProjection = null;
        EffectParameter mView = null;
        EffectParameter mWorld = null;
        EffectParameter sunDirection = null;
        EffectParameter viewportHeight = null;
        EffectParameter currentTime = null;
        EffectParameter precip_Tex = null;
        EffectParameter weatherType = null;

        public Vector3 SunDirection
        {
            set { sunDirection.SetValue(value); }
        }

        public int ViewportHeight
        {
            set { viewportHeight.SetValue(value); }
        }

        public float CurrentTime
        {
            set { currentTime.SetValue(value); }
        }

        public int WeatherType
        {
            set { weatherType.SetValue(value); }
        }

        public Texture2D PrecipTexture
        {
            set { precip_Tex.SetValue(value); }
        }

        public PrecipShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("PrecipShader"))
        {
            mProjection = Parameters["mProjection"];
            mView = Parameters["mView"];
            mWorld = Parameters["mWorld"];
            sunDirection = Parameters["LightVector"];
            viewportHeight = Parameters["viewportHeight"];
            currentTime = Parameters["currentTime"];
            weatherType = Parameters["weatherType"];
            precip_Tex = Parameters["precip_Tex"];
        }

        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            mProjection.SetValue(projection);
            mView.SetValue(view);
            mWorld.SetValue(world);
        }
    }
    #endregion

    #region Forest shader
    public class ForestShader : Effect
    {
        EffectParameter mView = null;
        EffectParameter mWorld = null;
        EffectParameter mWorldViewProj = null;
        EffectParameter forest_Tex = null;
        EffectParameter sunDirection = null;
        EffectParameter overcast = null;
        EffectParameter headlightPosition = null;
        EffectParameter headlightDirection = null;

        public Texture2D ForestTexture
        {
            set { forest_Tex.SetValue(value); }
        }

        public Vector3 SunDirection
        {
            set { sunDirection.SetValue(value); }
        }

        public float Overcast
        {
            set { overcast.SetValue(value); }
        }

        public Vector3 HeadlightPosition
        {
            set { headlightPosition.SetValue(value); }
        }

        public Vector3 HeadlightDirection
        {
            set { headlightDirection.SetValue(value); }
        }

        public ForestShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("ForestShader"))
        {
            mView = Parameters["mView"];
            mWorld = Parameters["mWorld"];
            mWorldViewProj = Parameters["mWorldViewProj"];
            forest_Tex = Parameters["forest_Tex"];
            sunDirection = Parameters["LightVector"];
            overcast = Parameters["overcast"];
            headlightPosition = Parameters["headlightPosition"];
            headlightDirection = Parameters["headlightDirection"];
        }

        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            mView.SetValue(view);
            mWorld.SetValue(world);
            mWorldViewProj.SetValueTranspose(world *view * projection);
        }
    }
    #endregion

    #region LightGlow shader
    public class LightGlowShader : Effect
    {
        EffectParameter mWorldViewProj = null;
        EffectParameter lightGlow_Tex = null;

        public Texture2D LightGlowTexture
        {
            set { lightGlow_Tex.SetValue(value); }
        }

        public LightGlowShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("LightGlowShader"))
        {
            mWorldViewProj = Parameters["mWorldViewProj"];
            lightGlow_Tex = Parameters["lightGlow_Tex"];
        }

        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            mWorldViewProj.SetValueTranspose(world * view * projection);
        }
    }
    #endregion
}
