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
		EffectParameter World;
		EffectParameter View;
		//EffectParameter Projection;
		EffectParameter WorldViewProjection;
		public void SetMatrix(Matrix world, ref Matrix view, ref Matrix viewProj)
		{
			World.SetValue(world);
			View.SetValue(view);
			WorldViewProjection.SetValue(world * viewProj);
		}

		EffectParameter LightViewProjectionShadowProjection;
		EffectParameter ShadowMapTexture;
		public void SetShadowMap(ref Matrix lightViewProjectionShadowProjection, Texture2D shadowMapTexture)
		{
			LightViewProjectionShadowProjection.SetValue(lightViewProjectionShadowProjection);
			ShadowMapTexture.SetValue(shadowMapTexture);
		}

		EffectParameter Fog;
		public void SetFog(float depth, ref Color color)
		{
			Fog.SetValue(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, depth));
		}

		EffectParameter zbias;
		public float ZBias { set { zbias.SetValue(value); } }

        EffectParameter lightVector;
        public Vector3 LightVector { set { lightVector.SetValue(value); } }

		EffectParameter HeadlightPosition;
		EffectParameter HeadlightDirection;
		public void SetHeadlight(ref Vector3 position, ref Vector3 direction, float fadeTime, float fadeDuration)
		{
			var lighting = fadeTime / fadeDuration;
			if (lighting < 0) lighting = 1 + lighting;
			HeadlightPosition.SetValue(new Vector4(position, MathHelper.Clamp(lighting, 0, 1)));
			HeadlightDirection.SetValue(direction);
		}

        EffectParameter overcast;
        public float Overcast { set { overcast.SetValue(value); } }

        EffectParameter viewerPos;
        public Vector3 ViewerPos { set { viewerPos.SetValue(value); } }

        EffectParameter isNight_Tex;
        public bool IsNight_Tex { set { isNight_Tex.SetValue(value); } }

        EffectParameter imageMap_Tex;
        public Texture2D ImageMap_Tex { set { imageMap_Tex.SetValue(value); } }

        EffectParameter normalMap_Tex;
        public Texture2D NormalMap_Tex { set { normalMap_Tex.SetValue(value); } }

        public SceneryShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("SceneryShader"))
        {
			World = Parameters["World"];
			View = Parameters["View"];
			//Projection = Parameters["Projection"];
			WorldViewProjection = Parameters["WorldViewProjection"];

			LightViewProjectionShadowProjection = Parameters["LightViewProjectionShadowProjection"];
			ShadowMapTexture = Parameters["ShadowMapTexture"];

			Fog = Parameters["Fog"];

			zbias = Parameters["ZBias"];

            lightVector = Parameters["LightVector"];

			HeadlightPosition = Parameters["HeadlightPosition"];
			HeadlightDirection = Parameters["HeadlightDirection"];

            overcast = Parameters["overcast"];
            viewerPos = Parameters["viewerPos"];
            isNight_Tex = Parameters["isNight_Tex"];
            imageMap_Tex = Parameters["imageMap_Tex"];
            normalMap_Tex = Parameters["normalMap_Tex"];
        }
    }
    #endregion

    #region Shadow mapping shader
    /// <summary>
    /// Wrapper for ShadowMap.fx
    /// </summary>
	public class ShadowMapShader : Effect
	{
		EffectParameter WorldViewProjection = null;
		EffectParameter ImageTexture = null;

		public void SetData(ref Matrix wvp, Texture2D imageTexture)
		{
			WorldViewProjection.SetValue(wvp);
			ImageTexture.SetValue(imageTexture);
		}

		public ShadowMapShader(GraphicsDevice graphicsDevice, ContentManager content)
			: base(graphicsDevice, content.Load<Effect>("ShadowMap"))
		{
			WorldViewProjection = Parameters["WorldViewProjection"];
			ImageTexture = Parameters["ImageTexture"];
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

        public void SetMatrix(ref Matrix worldViewProj, ref Matrix view)
        {
            mModelToProjection.SetValueTranspose(worldViewProj);
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

        public void SetMatrix(Matrix world, ref Matrix view, ref Matrix projection)
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

        public void SetMatrix(ref Matrix wvp)
        {
            mWorldViewProj.SetValueTranspose(wvp);
        }
    }
    #endregion

	#region Popup window shader
	/// <summary>
	/// Wrapper for PopupWindow.fx
	/// </summary>
	public class PopupWindowShader : Effect
	{
		EffectParameter World;
		EffectParameter WorldViewProjection;
		EffectParameter glassColor;
		EffectParameter ScreenSize;
		EffectParameter Screen_Tex;

		public Texture2D Screen
		{
			set
			{
				Screen_Tex.SetValue(value);
				if (value == null)
					ScreenSize.SetValue(new[] { 0, 0 });
				else
					ScreenSize.SetValue(new[] { value.Width, value.Height });
			}
		}

		public Color GlassColor
		{
			set
			{
				glassColor.SetValue(new float[] { value.R / 255, value.G / 255, value.B / 255 });
			}
		}

		public void SetMatrix(Matrix world, ref Matrix wvp)
		{
			World.SetValue(world);
            WorldViewProjection.SetValue(wvp);
		}

		public PopupWindowShader(GraphicsDevice graphicsDevice, ContentManager content)
			: base(graphicsDevice, content.Load<Effect>("PopupWindow"))
		{
			World = Parameters["World"];
			WorldViewProjection = Parameters["WorldViewProjection"];
			glassColor = Parameters["GlassColor"];
			ScreenSize = Parameters["ScreenSize"];
			Screen_Tex = Parameters["Screen_Tex"];
			Parameters["PopupWindowImage_Tex"].SetValue(content.Load<Texture2D>("PopupWindowImage"));
			Parameters["PopupWindowMask_Tex"].SetValue(content.Load<Texture2D>("PopupWindowMask"));
		}
	}
	#endregion
}
