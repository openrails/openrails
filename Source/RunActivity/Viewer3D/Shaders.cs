// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

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
        readonly EffectParameter world;
        readonly EffectParameter worldViewProjection;
        readonly EffectParameter[] lightViewProjectionShadowProjection;
        readonly EffectParameter[] shadowMapTextures;
        readonly EffectParameter shadowMapLimit;
        readonly EffectParameter zBias_Lighting;
        readonly EffectParameter fog;
        readonly EffectParameter lightVector;
        readonly EffectParameter headlightPosition;
        readonly EffectParameter headlightDirection;
        readonly EffectParameter headlightRcpDistance;
        readonly EffectParameter headlightColor;
        readonly EffectParameter overcast;
        readonly EffectParameter viewerPos;
        readonly EffectParameter imageTextureIsNight;
        readonly EffectParameter nightColorModifier;
        readonly EffectParameter halfNightColorModifier;
        readonly EffectParameter vegetationAmbientModifier;
        readonly EffectParameter eyeVector;
        readonly EffectParameter sideVector;
        readonly EffectParameter imageTexture;
        readonly EffectParameter overlayTexture;

        Vector3 _eyeVector;
        Vector4 _zBias_Lighting;
        Vector3 _sunDirection;
        bool _imageTextureIsNight;

        public void SetViewMatrix(ref Matrix v)
        {
            _eyeVector = Vector3.Normalize(new Vector3(v.M13, v.M23, v.M33));

            eyeVector.SetValue(new Vector4(_eyeVector, Vector3.Dot(_eyeVector, _sunDirection) * 0.5f + 0.5f));
            sideVector.SetValue(Vector3.Normalize(Vector3.Cross(_eyeVector, Vector3.Down)));
        }

        public void SetMatrix(ref Matrix w, ref Matrix vp)
        {
            world.SetValue(w);
            worldViewProjection.SetValue(w * vp);

            const float FullBrightness = 1.0f;
            //const float HalfShadowBrightness = 0.75;
            const float HalfNightBrightness = 0.6f;
            const float ShadowBrightness = 0.5f;
            const float NightBrightness = 0.2f;

            if (_imageTextureIsNight)
            {
                nightColorModifier.SetValue(FullBrightness);
                halfNightColorModifier.SetValue(FullBrightness);
                vegetationAmbientModifier.SetValue(FullBrightness);
            }
            else
            {
                // The following constants define the beginning and the end conditions of
                // the day-night transition. Values refer to the Y postion of LightVector.
                const float startNightTrans = 0.1f;
                const float finishNightTrans = -0.1f;

                var nightEffect = MathHelper.Clamp((_sunDirection.Y - finishNightTrans) / (startNightTrans - finishNightTrans), 0, 1);

                nightColorModifier.SetValue(MathHelper.Lerp(NightBrightness, FullBrightness, nightEffect));
                halfNightColorModifier.SetValue(MathHelper.Lerp(HalfNightBrightness, FullBrightness, nightEffect));
                vegetationAmbientModifier.SetValue(MathHelper.Lerp(ShadowBrightness, FullBrightness, _zBias_Lighting.Y));
            }
        }

        public void SetShadowMap(Matrix[] shadowProjections, Texture2D[] textures, float[] limits)
        {
            for (var i = 0; i < RenderProcess.ShadowMapCount; i++)
            {
                lightViewProjectionShadowProjection[i].SetValue(shadowProjections[i]);
                shadowMapTextures[i].SetValue(textures[i]);
            }
            shadowMapLimit.SetValue(new Vector4(limits[0], limits.Length > 1 ? limits[1] : 0, limits.Length > 2 ? limits[2] : 0, limits.Length > 3 ? limits[3] : 0));
        }

        public float ZBias { get { return _zBias_Lighting.X; } set { _zBias_Lighting.X = value; zBias_Lighting.SetValue(_zBias_Lighting); } }
        public float LightingDiffuse { get { return _zBias_Lighting.Y; } set { _zBias_Lighting.Y = value; zBias_Lighting.SetValue(_zBias_Lighting); } }
        public float LightingSpecular { get { return _zBias_Lighting.Z; } set { _zBias_Lighting.Z = value; _zBias_Lighting.W = value >= 1 ? 1 : 0; zBias_Lighting.SetValue(_zBias_Lighting); } }

        public void SetFog(float depth, ref Color color)
        {
            fog.SetValue(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1f / depth));
        }

        public Vector3 LightVector { set { _sunDirection = value; lightVector.SetValue(value); } }

        public void SetHeadlight(ref Vector3 position, ref Vector3 direction, float distance, float minDotProduct, float fadeTime, float fadeDuration, ref Vector4 color)
        {
            var lighting = fadeTime / fadeDuration;
            if (lighting < 0) lighting = 1 + lighting;
            headlightPosition.SetValue(new Vector4(position, MathHelper.Clamp(lighting, 0, 1)));
            headlightDirection.SetValue(new Vector4(direction, 0.5f * (1 - minDotProduct))); // We want 50% brightness at the given dot product.
            headlightRcpDistance.SetValue(1f / distance); // Needed to be separated (direction * distance) because no pre-shaders are supported in XNA 4
            headlightColor.SetValue(color);
        }

        public void SetHeadlightOff()
        {
            headlightPosition.SetValue(Vector4.Zero);
        }

        public float Overcast { set { overcast.SetValue(new Vector2(value, value / 2)); } }

        public Vector3 ViewerPos { set { viewerPos.SetValue(value); } }

        public bool ImageTextureIsNight { set { _imageTextureIsNight = value; imageTextureIsNight.SetValue(value); } }

        public Texture2D ImageTexture { set { imageTexture.SetValue(value); } }

        public Texture2D OverlayTexture { set { overlayTexture.SetValue(value); } }

        public SceneryShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("SceneryShader"))
        {
            world = Parameters["World"];
            worldViewProjection = Parameters["WorldViewProjection"];
            lightViewProjectionShadowProjection = new EffectParameter[RenderProcess.ShadowMapCountMaximum];
            shadowMapTextures = new EffectParameter[RenderProcess.ShadowMapCountMaximum];
            for (var i = 0; i < RenderProcess.ShadowMapCountMaximum; i++)
            {
                lightViewProjectionShadowProjection[i] = Parameters["LightViewProjectionShadowProjection" + i];
                shadowMapTextures[i] = Parameters["ShadowMapTexture" + i];
            }
            shadowMapLimit = Parameters["ShadowMapLimit"];
            zBias_Lighting = Parameters["ZBias_Lighting"];
            fog = Parameters["Fog"];
            lightVector = Parameters["LightVector"];
            headlightPosition = Parameters["HeadlightPosition"];
            headlightDirection = Parameters["HeadlightDirection"];
            headlightRcpDistance = Parameters["HeadlightRcpDistance"];
            headlightColor = Parameters["HeadlightColor"];
            overcast = Parameters["Overcast"];
            viewerPos = Parameters["ViewerPos"];
            imageTextureIsNight = Parameters["ImageTextureIsNight"];
            nightColorModifier = Parameters["NightColorModifier"];
            halfNightColorModifier = Parameters["HalfNightColorModifier"];
            vegetationAmbientModifier = Parameters["VegetationAmbientModifier"];
            eyeVector = Parameters["EyeVector"];
            sideVector = Parameters["SideVector"];
            imageTexture = Parameters["ImageTexture"];
            overlayTexture = Parameters["OverlayTexture"];
        }
    }

    [CallOnThread("Render")]
    public class ShadowMapShader : Effect
    {
        readonly EffectParameter worldViewProjection;
        readonly EffectParameter sideVector;
        readonly EffectParameter imageBlurStep;
        readonly EffectParameter imageTexture;

        public void SetData(ref Matrix v)
        {
            sideVector.SetValue(v.Right);
        }

        public void SetData(ref Matrix wvp, Texture2D texture)
        {
            worldViewProjection.SetValue(wvp);
            imageTexture.SetValue(texture);
        }

        public void SetBlurData(ref Matrix wvp)
        {
            worldViewProjection.SetValue(wvp);
        }

        public void SetBlurData(Texture2D texture)
        {
            imageTexture.SetValue(texture);
            imageBlurStep.SetValue(texture != null ? 1f / texture.Width : 0);
        }

        public ShadowMapShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("ShadowMap"))
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            sideVector = Parameters["SideVector"];
            imageBlurStep = Parameters["ImageBlurStep"];
            imageTexture = Parameters["ImageTexture"];
        }
    }

    [CallOnThread("Render")]
    public class SkyShader : Effect
    {
        readonly EffectParameter worldViewProjection;
        readonly EffectParameter lightVector;
        readonly EffectParameter time;
        readonly EffectParameter overcast;
        readonly EffectParameter windDisplacement;
        readonly EffectParameter skyColor;
        readonly EffectParameter moonColor;
        readonly EffectParameter moonTexCoord;
        readonly EffectParameter cloudColor;
        readonly EffectParameter rightVector;
        readonly EffectParameter upVector;
        readonly EffectParameter skyMapTexture;
        readonly EffectParameter starMapTexture;
        readonly EffectParameter moonMapTexture;
        readonly EffectParameter moonMaskTexture;
        readonly EffectParameter cloudMapTexture;

        public Vector3 LightVector
        {
            set
            {
                lightVector.SetValue(value);

                cloudColor.SetValue(Day2Night(0.2f, -0.2f, 0.15f, value.Y));
                var skyColor1 = Day2Night(0.25f, -0.25f, -0.5f, value.Y);
                var skyColor2 = MathHelper.Clamp(skyColor1 + 0.55f, 0, 1);
                var skyColor3 = 0.001f / (0.8f * Math.Abs(value.Y - 0.1f));
                skyColor.SetValue(new Vector3(skyColor1, skyColor2, skyColor3));

                // Fade moon during daylight
                var moonColor1 = value.Y > 0.1f ? (1 - value.Y) / 1.5f : 1;
                // Mask stars behind dark side (mask fades in)
                var moonColor2 = _moonPhase != 6 && value.Y < 0.13 ? -6.25f * value.Y + 0.8125f : 0;
                moonColor.SetValue(new Vector2(moonColor1, moonColor2));
            }
        }

        float _time;
        public float Time
        {
            set
            {
                _time = value;
                time.SetValue(value);
            }
        }

        int _moonPhase;
        public float Random
        {
            set 
            { 
                _moonPhase = (int)value; 
                moonTexCoord.SetValue(new Vector2((value % 2) / 2, (int)(value / 2) / 4));
            }
        }

        public float Overcast
        {
            set
            {
                if (value < 0.2f)
                    overcast.SetValue(new Vector3(4 * value + 0.2f, 0.0f, 0.0f));
                else
                    // Coefficients selected by author to achieve the desired appearance
                    overcast.SetValue(new Vector3(MathHelper.Clamp(2 * value - 0.4f, 0, 1), 1.25f - 1.125f * value, 1.15f - 0.75f * value));
            }
        }

        public float WindSpeed { get; set; }

        public float WindDirection
        {
            set 
            { 
                var totalWindDisplacement = 200 * WindSpeed * _time; // This greatly exaggerates the wind speed, but it looks better!
                windDisplacement.SetValue(new Vector2(-(float)Math.Sin(value) * totalWindDisplacement, (float)Math.Cos(value) * totalWindDisplacement));
            }
        }

        public float MoonScale { get; set; }

        public Texture2D SkyMapTexture { set { skyMapTexture.SetValue(value); } }
        public Texture2D StarMapTexture { set { starMapTexture.SetValue(value); } }
        public Texture2D MoonMapTexture { set { moonMapTexture.SetValue(value); } }
        public Texture2D MoonMaskTexture { set { moonMaskTexture.SetValue(value); } }
        public Texture2D CloudMapTexture { set { cloudMapTexture.SetValue(value); } }

        public void SetViewMatrix(ref Matrix view)
        {
            var moonScale = MoonScale;
            if (_moonPhase == 6)
                moonScale *= 2;

            rightVector.SetValue(view.Right * moonScale);
            upVector.SetValue(Vector3.Down * moonScale);
        }

        public void SetMatrix(ref Matrix wvp)
        {
            worldViewProjection.SetValueTranspose(wvp);
        }

        public SkyShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("SkyShader"))
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            lightVector = Parameters["LightVector"];
            time = Parameters["Time"];
            overcast = Parameters["Overcast"];
            windDisplacement = Parameters["WindDisplacement"];
            skyColor = Parameters["SkyColor"];
            moonColor = Parameters["MoonColor"];
            moonTexCoord = Parameters["MoonTexCoord"];
            cloudColor = Parameters["CloudColor"];
            rightVector = Parameters["RightVector"];
            upVector = Parameters["UpVector"];
            skyMapTexture = Parameters["SkyMapTexture"];
            starMapTexture = Parameters["StarMapTexture"];
            moonMapTexture = Parameters["MoonMapTexture"];
            moonMaskTexture = Parameters["MoonMaskTexture"];
            cloudMapTexture = Parameters["CloudMapTexture"];
        }
        
        // This function dims the lighting at night, with a transition period as the sun rises or sets
        float Day2Night(float startNightTrans, float finishNightTrans, float minDarknessCoeff, float sunDirectionY)
        {
            // The following two are used to interpoate between day and night lighting (y = mx + b)
            var slope = (1.0f - minDarknessCoeff) / (startNightTrans - finishNightTrans); // "m"
            var incpt = 1.0f - slope * startNightTrans; // "b"
            // This is the return value used to darken scenery
            float adjustment;
            
            if (sunDirectionY < finishNightTrans)
                adjustment = minDarknessCoeff;
            else if (sunDirectionY > startNightTrans)
                adjustment = 1.0f; // Scenery is fully lit during the day
            else
                adjustment = slope * sunDirectionY + incpt;

            return adjustment;
        }
    }

    [CallOnThread("Render")]
    public class ParticleEmitterShader : Effect
    {
        EffectParameter emitDirection = null;
        EffectParameter emitSize = null;
        EffectParameter tileXY = null;
        EffectParameter currentTime = null;
        EffectParameter wvp = null;
        EffectParameter invView = null;
        EffectParameter texture = null;
        EffectParameter lightVector = null;

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

        public Vector3 LightVector
        {
            set { lightVector.SetValue(value); }
        }

        public ParticleEmitterShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("ParticleEmitterShader"))
        {
            emitDirection = Parameters["emitDirection"];
            emitSize = Parameters["emitSize"];
            currentTime = Parameters["currentTime"];
            wvp = Parameters["worldViewProjection"];
            invView = Parameters["invView"];
            tileXY = Parameters["cameraTileXY"];
            texture = Parameters["particle_Tex"];
            lightVector = Parameters["LightVector"];
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
        readonly EffectParameter worldViewProjection;
        readonly EffectParameter fade;
        readonly EffectParameter lightGlowTexture;

        public Texture2D LightGlowTexture { set { lightGlowTexture.SetValue(value); } }

        public void SetMatrix(ref Matrix wvp)
        {
            worldViewProjection.SetValueTranspose(wvp);
        }

        public void SetFade(Vector2 fadeValues)
        {
            fade.SetValue(fadeValues);
        }

        public LightGlowShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("LightGlowShader"))
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            fade = Parameters["Fade"];
            lightGlowTexture = Parameters["LightGlowTexture"];
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
        readonly EffectParameter world;
        readonly EffectParameter worldViewProjection;
        readonly EffectParameter glassColor;
        readonly EffectParameter screenSize;
        readonly EffectParameter screenTexture;

        public Texture2D Screen
        {
            set
            {
                screenTexture.SetValue(value);
                if (value == null)
                    screenSize.SetValue(new[] { 0, 0 });
                else
                    screenSize.SetValue(new[] { value.Width, value.Height });
            }
        }

        public Color GlassColor { set { glassColor.SetValue(new float[] { value.R / 255, value.G / 255, value.B / 255 }); } }

        public void SetMatrix(Matrix w, ref Matrix wvp)
        {
            world.SetValue(w);
            worldViewProjection.SetValue(wvp);
        }

        public PopupWindowShader(GraphicsDevice graphicsDevice, ContentManager content)
            : base(graphicsDevice, content.Load<Effect>("PopupWindow"))
        {
            world = Parameters["World"];
            worldViewProjection = Parameters["WorldViewProjection"];
            glassColor = Parameters["GlassColor"];
            screenSize = Parameters["ScreenSize"];
            screenTexture = Parameters["ScreenTexture"];
            Parameters["WindowTexture"].SetValue(content.Load<Texture2D>("Window"));
        }
    }

    [CallOnThread("Render")]
    public class CabShader : Effect
    {
        readonly EffectParameter nightColorModifier;
        readonly EffectParameter lightOn;
        readonly EffectParameter light1Pos;
        readonly EffectParameter light2Pos;
        readonly EffectParameter light1Col;
        readonly EffectParameter light2Col;
        readonly EffectParameter texPos;
        readonly EffectParameter texSize;
        readonly EffectParameter imageTexture;

        public void SetTextureData(float x, float y, float width, float height)
        {
            texPos.SetValue(new Vector2(x, y));
            texSize.SetValue(new Vector2(width, height));
        }

        public void SetLightPositions(Vector4 light1Position, Vector4 light2Position)
        {
            light1Pos.SetValue(light1Position);
            light2Pos.SetValue(light2Position);
        }

        public void SetData(Vector3 sunDirection, bool isNightTexture, bool isDashLight, float overcast)
        {
            nightColorModifier.SetValue(MathHelper.Lerp(0.2f + (isDashLight ? 0.15f : 0), 1, isNightTexture ? 1 : MathHelper.Clamp((sunDirection.Y + 0.1f) / 0.2f, 0, 1) * MathHelper.Clamp(1.5f - overcast, 0, 1)));
            lightOn.SetValue(isDashLight);
        }

        public CabShader(GraphicsDevice graphicsDevice, ContentManager content, Vector4 light1Position, Vector4 light2Position, Vector3 light1Color, Vector3 light2Color)
            : base(graphicsDevice, content.Load<Effect>("CabShader"))
        {
            nightColorModifier = Parameters["NightColorModifier"];
            lightOn = Parameters["LightOn"];
            light1Pos = Parameters["Light1Pos"];
            light2Pos = Parameters["Light2Pos"];
            light1Col = Parameters["Light1Col"];
            light2Col = Parameters["Light2Col"];
            texPos = Parameters["TexPos"];
            texSize = Parameters["TexSize"];
            imageTexture = Parameters["ImageTexture"];

            light1Pos.SetValue(light1Position);
            light2Pos.SetValue(light2Position);
            light1Col.SetValue(light1Color);
            light2Col.SetValue(light2Color);
        }
    }
}
