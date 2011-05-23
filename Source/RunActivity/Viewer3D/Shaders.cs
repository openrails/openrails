// COPYRIGHT 2009, 2010, 2011 by the Open Rails project.
// This code is provided to help you understand what Open Rails does and does
// not do. Suggestions and contributions to improve Open Rails are always
// welcome. Use of the code for any other purpose or distribution of the code
// to anyone else is prohibited without specific written permission from
// admin@openrails.org.
//
// This file is the responsibility of the 3D & Environment Team. 

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;

namespace ORTS
{
	[CallOnThread("Render")]
	public class SceneryShader : Effect
    {
		EffectParameter World;
		EffectParameter View;
		EffectParameter WorldViewProjection;
		public void SetMatrix(ref Matrix world, ref Matrix view, ref Matrix viewProj)
		{
			World.SetValue(world);
			View.SetValue(view);
			WorldViewProjection.SetValue(world * viewProj);
		}

		EffectParameter[] LightViewProjectionShadowProjection;
		EffectParameter[] ShadowMapTextures;
		EffectParameter ShadowMapLimit;
		public void SetShadowMap(Matrix[] lightViewProjectionShadowProjection, Texture2D[] shadowMapTextures, float[] limits)
		{
			for (var i = 0; i < RenderProcess.ShadowMapCount; i++)
			{
				LightViewProjectionShadowProjection[i].SetValue(lightViewProjectionShadowProjection[i]);
				ShadowMapTextures[i].SetValue(shadowMapTextures[i]);
			}
            ShadowMapLimit.SetValue(new Vector4(limits[0], limits.Length > 1 ? limits[1] : 0, limits.Length > 2 ? limits[2] : 0, limits.Length > 3 ? limits[3] : 0));
		}

		EffectParameter zbias_lighting;
		public float ZBias { get; set; }
		public float LightingDiffuse { get; set; }
		public float LightingSpecular { get; set; }

		EffectParameter Fog;
		public void SetFog(float depth, ref Color color)
		{
			Fog.SetValue(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, depth));
		}

        EffectParameter lightVector;
        public Vector3 LightVector { set { lightVector.SetValue(value); } }

		EffectParameter HeadlightPosition;
        EffectParameter HeadlightDirection;
        EffectParameter HeadlightColor;
        public void SetHeadlight(ref Vector3 position, ref Vector3 direction, float distance, float minDotProduct, float fadeTime, float fadeDuration, ref Vector4 color)
		{
			var lighting = fadeTime / fadeDuration;
			if (lighting < 0) lighting = 1 + lighting;
			HeadlightPosition.SetValue(new Vector4(position, MathHelper.Clamp(lighting, 0, 1)));
            HeadlightDirection.SetValue(new Vector4(direction * distance, minDotProduct));
            HeadlightColor.SetValue(color);
		}

        public void SetHeadlightOff()
        {
            HeadlightPosition.SetValue(Vector4.Zero);
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

		public void Apply()
		{
			zbias_lighting.SetValue(new Vector3(ZBias, LightingDiffuse, LightingSpecular));
		}

        public SceneryShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("SceneryShader"))
        {
			World = Parameters["World"];
			View = Parameters["View"];
			WorldViewProjection = Parameters["WorldViewProjection"];

			LightViewProjectionShadowProjection = new EffectParameter[RenderProcess.ShadowMapCountMaximum];
			ShadowMapTextures = new EffectParameter[RenderProcess.ShadowMapCountMaximum];
			for (var i = 0; i < RenderProcess.ShadowMapCountMaximum; i++)
			{
				LightViewProjectionShadowProjection[i] = Parameters["LightViewProjectionShadowProjection" + i];
				ShadowMapTextures[i] = Parameters["ShadowMapTexture" + i];
			}
			ShadowMapLimit = Parameters["ShadowMapLimit"];

			Fog = Parameters["Fog"];

			zbias_lighting = Parameters["ZBias_Lighting"];

            lightVector = Parameters["LightVector"];

			HeadlightPosition = Parameters["HeadlightPosition"];
            HeadlightDirection = Parameters["HeadlightDirection"];
            HeadlightColor = Parameters["HeadlightColor"];

            overcast = Parameters["overcast"];
            viewerPos = Parameters["viewerPos"];
            isNight_Tex = Parameters["isNight_Tex"];
            imageMap_Tex = Parameters["imageMap_Tex"];
            normalMap_Tex = Parameters["normalMap_Tex"];
        }
    }

	[CallOnThread("Render")]
	public class ShadowMapShader : Effect
	{
		EffectParameter View = null;
		EffectParameter WorldViewProjection = null;
		EffectParameter ImageTexture = null;
		EffectParameter ImageBlurStep = null;

        public void SetData(ref Matrix view)
        {
            View.SetValue(view);
        }

        public void SetData(ref Matrix wvp, Texture2D imageTexture)
        {
            WorldViewProjection.SetValue(wvp);
            ImageTexture.SetValue(imageTexture);
        }

        public void SetBlurData(ref Matrix view, ref Matrix wvp)
        {
            View.SetValue(view);
            WorldViewProjection.SetValue(wvp);
        }

        public void SetBlurData(Texture2D imageTexture)
        {
            ImageTexture.SetValue(imageTexture);
            ImageBlurStep.SetValue(imageTexture != null ? imageTexture.Width : 0);
        }

        public ShadowMapShader(GraphicsDevice graphicsDevice, ContentManager content)
			: base(graphicsDevice, content.Load<Effect>("ShadowMap"))
		{
			View = Parameters["View"];
			WorldViewProjection = Parameters["WorldViewProjection"];
			ImageTexture = Parameters["ImageTexture"];
			ImageBlurStep = Parameters["ImageBlurStep"];
		}
	}

	[CallOnThread("Render")]
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

    [CallOnThread("Render")]
    public class ParticleEmitterShader : Effect
    {
        EffectParameter colorTint = null;
        EffectParameter emitDirection = null;
        EffectParameter emitSize = null;
        EffectParameter tileXY = null;
        EffectParameter currentTime = null;
        EffectParameter wvp = null;
        EffectParameter invView = null;
        EffectParameter texture = null;

        public float CurrentTime
        {
            set { currentTime.SetValue(value); }
        }

        public Vector2 CameraTileXY
        {
            set { tileXY.SetValue(value); }
        }

        public Texture2D Texture
        {
            set { texture.SetValue(value); }
        }

        public Vector3 EmitDirection
        {
            set { emitDirection.SetValue(value); }
        }

        public float EmitSize
        {
            set { emitSize.SetValue(value); }
        }

        public Color ColorTint
        {
            set { colorTint.SetValue(value.ToVector4()); }
        }

        public ParticleEmitterShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("ParticleEmitterShader"))
        {
            colorTint = Parameters["colorTint"];
            emitDirection = Parameters["emitDirection"];
            emitSize = Parameters["emitSize"];
            currentTime = Parameters["currentTime"];
            wvp = Parameters["worldViewProjection"];
            invView = Parameters["invView"];
            tileXY = Parameters["cameraTileXY"];
            texture = Parameters["particle_Tex"];
        }

        public void SetMatrix(Matrix world, ref Matrix view, ref Matrix projection)
        {
            wvp.SetValue(world * view * projection);
            invView.SetValue(Matrix.Invert(view));
        }
    }

	[CallOnThread("Render")]
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

	[CallOnThread("Render")]
	public class LightGlowShader : Effect
    {
        EffectParameter worldViewProjection = null;
        EffectParameter lightGlowTexture = null;
        EffectParameter fade = null;

        public Texture2D LightGlowTexture
        {
            set { lightGlowTexture.SetValue(value); }
        }

        public LightGlowShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("LightGlowShader"))
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            lightGlowTexture = Parameters["LightGlowTexture"];
            fade = Parameters["Fade"];
        }

        public void SetMatrix(ref Matrix wvp)
        {
            worldViewProjection.SetValueTranspose(wvp);
        }

        public void SetFade(Vector2 fadeValues)
        {
            fade.SetValue(fadeValues);
        }
    }

    [CallOnThread("Render")]
    public class LightConeShader : Effect
    {
        EffectParameter worldViewProjection = null;
        EffectParameter fade = null;

        public LightConeShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("LightConeShader"))
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            fade = Parameters["Fade"];
        }

        public void SetMatrix(ref Matrix wvp)
        {
            worldViewProjection.SetValue(wvp);
        }

        public void SetFade(Vector2 fadeValues)
        {
            fade.SetValue(fadeValues);
        }
    }

	[CallOnThread("Render")]
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
			Parameters["Window_Tex"].SetValue(content.Load<Texture2D>("Window"));
		}
	}

	[CallOnThread("Render")]
	public class CabShader : Effect
    {
        EffectParameter _LightVector = null;
        EffectParameter _isNightTex = null;
        EffectParameter _Texture = null;
        EffectParameter _DashLight1Pos = null;
        EffectParameter _DashLight2Pos = null;
        EffectParameter _DashLight1Col = null;
        EffectParameter _DashLight2Col = null;
        EffectParameter _isDashLight = null;
        EffectParameter _TexPos = null;
        EffectParameter _TexSize = null;
        EffectParameter _Overcast = null;

        Vector2 _Position = new Vector2();
        Vector2 _Size = new Vector2();

        public void SetTexData(float X, float Y, float Width, float Height)
        {
            _Position.X = X;
            _Position.Y = Y;
            _Size.X = Width;
            _Size.Y = Height;
            _TexPos.SetValue(_Position);
            _TexSize.SetValue(_Size);
        }

        public void SetLightPositions(Vector4 DashLight1Pos, Vector4 DashLight2Pos)
        {
            _DashLight1Pos.SetValue(DashLight1Pos);
            _DashLight2Pos.SetValue(DashLight2Pos);
        }

        public void SetData(Vector3 LightVector, 
            bool isNightTexture, bool isDashLight, float Overcast)
        {
            _LightVector.SetValue(LightVector);
            _isDashLight.SetValue(isDashLight);
            _isNightTex.SetValue(isNightTexture);
            _Overcast.SetValue(Overcast);
        }

        public CabShader(GraphicsDevice graphicsDevice, ContentManager content, Vector4 DashLight1Pos, Vector4 DashLight2Pos,
            Vector3 DashLight1Col, Vector3 DashLight2Col)
            : base(graphicsDevice, content.Load<Effect>("CabShader"))
        {
            _LightVector = Parameters["LightVector"];
            _isNightTex = Parameters["isNightTex"];
            _Texture = Parameters["ImageTexture"];
            _DashLight1Pos = Parameters["Light1Pos"];
            _DashLight2Pos = Parameters["Light2Pos"];
            _DashLight1Col = Parameters["Light1Col"];
            _DashLight2Col = Parameters["Light2Col"];
            _isDashLight = Parameters["isLight"];

            _TexPos = Parameters["TexPos"];
            _TexSize = Parameters["TexSize"];
            _Overcast = Parameters["overcast"];

            _DashLight1Pos.SetValue(DashLight1Pos);
            _DashLight2Pos.SetValue(DashLight2Pos);
            _DashLight1Col.SetValue(DashLight1Col);
            _DashLight2Col.SetValue(DashLight2Col);
        }
    }
}
