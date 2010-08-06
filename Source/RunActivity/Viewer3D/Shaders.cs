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

        public Vector3 SunDirection
        {
            set { sunDirection.SetValue(value); }
        }

        public float Overcast
        {
            set { overcast.SetValue(value); }
        }

        public bool IsHeadlightBright
        {
            set { isHeadlightBright.SetValue(value); }
        }

        public Vector3 HeadlightPosition
        {
            set { headlightPosition.SetValue(value); }
        }

        public Vector3 HeadlightDirection
        {
            set { headlightDirection.SetValue(value); }
        }

        public int StateChange
        {
            set { stateChange.SetValue(value); }
        }

        public float FadeInTime
        {
            set { fadeinTime.SetValue(value); }
        }

        public float FadeOutTime
        {
            set { fadeoutTime.SetValue(value); }
        }

        public float FadeTime
        {
            set { fadeTime.SetValue(value); }
        }

        EffectParameter imageMap_Tex = null;
        EffectParameter normalMap_Tex = null;
        EffectParameter isNight_Tex = null;
        EffectParameter viewerPos = null;
        EffectParameter sunDirection = null;
        EffectParameter overcast = null;
        EffectParameter isHeadlightBright = null;
        EffectParameter headlightPosition = null;
        EffectParameter headlightDirection = null;
        EffectParameter stateChange = null;
        EffectParameter fadeinTime = null;
        EffectParameter fadeoutTime = null;
        EffectParameter fadeTime = null;

		public Matrix World { set { world.SetValue(value); } } EffectParameter world = null;
		public Matrix View { set { view.SetValue(value); } } EffectParameter view = null;
		//public Matrix Projection { set { projection.SetValue(value); } } EffectParameter projection = null;
		public Matrix WorldViewProjection { set { worldviewprojection.SetValue(value); } } EffectParameter worldviewprojection = null;

		public Matrix LightViewProjectionShadowProjection { set { lightviewprojectionshadowprojection.SetValue(value); } } EffectParameter lightviewprojectionshadowprojection = null;
		public Texture2D ShadowMapTexture { set { shadowmaptexture.SetValue(value); } } EffectParameter shadowmaptexture = null;

		public float FogStart { set { fogstart.SetValue(value); } } EffectParameter fogstart = null;
		public float FogDepth { set { fogdepth.SetValue(value); } } EffectParameter fogdepth = null;
		public Color FogColor { set { fogcolor.SetValue(new Vector3(value.R / 255f, value.G / 255f, value.B / 255f)); } } EffectParameter fogcolor = null;

		public float ZBias { set { zbias.SetValue(value); } } EffectParameter zbias = null;

        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            World = world;
            View = view;
            //Projection = projection;
            WorldViewProjection = world * view * projection;
            XNAMatrix = world;
        }

        public SceneryShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("SceneryShader"))
        {
			world = Parameters["World"];
			view = Parameters["View"];
			//projection = Parameters["Projection"];
			worldviewprojection = Parameters["WorldViewProjection"];

			lightviewprojectionshadowprojection = Parameters["LightViewProjectionShadowProjection"];
			shadowmaptexture = Parameters["ShadowMapTexture"];

			fogstart = Parameters["FogStart"];
			fogdepth = Parameters["FogDepth"];
			fogcolor = Parameters["FogColor"];

			zbias = Parameters["ZBias"];

            imageMap_Tex = Parameters["imageMap_Tex"];
            normalMap_Tex = Parameters["normalMap_Tex"];
            isNight_Tex = Parameters["isNight_Tex"];
            viewerPos = Parameters["viewerPos"];
            sunDirection = Parameters["LightVector"];
            overcast = Parameters["overcast"];
            isHeadlightBright = Parameters["isHeadlightBright"];
            headlightPosition = Parameters["headlightPosition"];
            headlightDirection = Parameters["headlightDirection"];
            stateChange = Parameters["stateChange"];
            fadeinTime = Parameters["fadeinTime"];
            fadeoutTime = Parameters["fadeoutTime"];
            fadeTime = Parameters["fadeTime"];
        }
    }
    #endregion

    #region Shadow mapping shader
    /// <summary>
    /// Wrapper for ShadowMap.fx
    /// </summary>
    public class ShadowMapShader : Effect
    {
        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            Parameters["WorldViewProjection"].SetValue(world * view * projection);
        }

        public ShadowMapShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("ShadowMap"))
        {
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

    #region LightGlow shader
    public class LightGlowShader : Effect
    {
        EffectParameter mWorldViewProj = null;
        EffectParameter lightGlow_Tex = null;
        EffectParameter stateChange = null;
        EffectParameter fadeTime = null;

        public Texture2D LightGlowTexture
        {
            set { lightGlow_Tex.SetValue(value); }
        }

        public int StateChange
        {
            set { stateChange.SetValue(value); }
        }

        public float FadeTime
        {
            set { fadeTime.SetValue(value); }
        }

        public LightGlowShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("LightGlowShader"))
        {
            mWorldViewProj = Parameters["mWorldViewProj"];
            lightGlow_Tex = Parameters["lightGlow_Tex"];
            stateChange = Parameters["stateChange"];
            fadeTime = Parameters["fadeTime"];
        }

        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            mWorldViewProj.SetValueTranspose(world * view * projection);
        }
    }
    #endregion

	#region Popup window shader
	/// <summary>
	/// Wrapper for PopupWindow.fx
	/// </summary>
	public class PopupWindowShader : Effect
	{
		public Texture2D Screen
		{
			set
			{
				Parameters["Screen_Tex"].SetValue(value);
				if (value == null)
					Parameters["ScreenSize"].SetValue(new[] { 0, 0 });
				else
					Parameters["ScreenSize"].SetValue(new[] { value.Width, value.Height });
			}
		}

		public Color GlassColor
		{
			set
			{
				Parameters["GlassColor"].SetValue(new float[] { value.R / 255, value.G / 255, value.B / 255 });
			}
		}

		public void SetMatrix(Matrix world, Matrix view, Matrix projection)
		{
			Parameters["World"].SetValue(world);
			Parameters["WorldViewProjection"].SetValue(world * view * projection);
		}

		public PopupWindowShader(GraphicsDevice graphicsDevice, ContentManager content)
			: base(graphicsDevice, content.Load<Effect>("PopupWindow"))
		{
			Parameters["PopupWindowImage_Tex"].SetValue(content.Load<Texture2D>("PopupWindowImage"));
			Parameters["PopupWindowMask_Tex"].SetValue(content.Load<Texture2D>("PopupWindowMask"));
		}
	}
	#endregion
}
