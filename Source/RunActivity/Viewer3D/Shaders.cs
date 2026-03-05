// COPYRIGHT 2009 - 2023 by the Open Rails project.
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
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using ORTS.Common;
using Orts.Viewer3D.Processes;

namespace Orts.Viewer3D
{
    public abstract class Shader : Effect
    {
        protected Shader(GraphicsDevice graphicsDevice, string filename)
            : base(graphicsDevice, GetEffectCode(filename))
        {
        }

        static byte[] GetEffectCode(string filename)
        {
            string filePath = Path.Combine(ApplicationInfo.ProcessDirectory, "Content", filename + ".mgfx");
            return File.ReadAllBytes(filePath);
        }
    }

    class ProcessorContext : ContentProcessorContext
    {
        public override TargetPlatform TargetPlatform { get { return TargetPlatform.Windows; } }
        public override GraphicsProfile TargetProfile { get { return GraphicsProfile.HiDef; } }
        public override string BuildConfiguration { get { return string.Empty; } }
        public override string IntermediateDirectory { get { return string.Empty; } }
        public override string OutputDirectory { get { return string.Empty; } }
        public override string OutputFilename { get { return string.Empty; } }

        public override ContentIdentity SourceIdentity { get { return sourceIdentity; } }
        readonly ContentIdentity sourceIdentity = new ContentIdentity();

        public override OpaqueDataDictionary Parameters { get { return parameters; } }
        readonly OpaqueDataDictionary parameters = new OpaqueDataDictionary();

        public override ContentBuildLogger Logger { get { return logger; } }
        readonly ContentBuildLogger logger = new TraceContentBuildLogger();

        public override void AddDependency(string filename) { }
        public override void AddOutputFile(string filename) { }

        public override TOutput Convert<TInput, TOutput>(TInput input, string processorName, OpaqueDataDictionary processorParameters) { throw new NotImplementedException(); }
        public override TOutput BuildAndLoadAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName) { throw new NotImplementedException(); }
        public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset, string processorName, OpaqueDataDictionary processorParameters, string importerName, string assetName) { throw new NotImplementedException(); }
    }

    class TraceContentBuildLogger : ContentBuildLogger
    {
        public override void LogMessage(string message, params object[] messageArgs) => Trace.TraceInformation(message, messageArgs);
        public override void LogImportantMessage(string message, params object[] messageArgs) => Trace.TraceInformation(message, messageArgs);
        public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs) => Trace.TraceWarning(message, messageArgs);
    }

    [CallOnThread("Render")]
    public class SceneryShader : Shader
    {
        readonly EffectParameter world;
        readonly EffectParameter view;
        readonly EffectParameter projection;
        readonly EffectParameter[] lightViewProjectionShadowProjection;
        readonly EffectParameter[] shadowMapTextures;
        readonly EffectParameter shadowMapLimit;
        readonly EffectParameter zBias_Lighting;
        readonly EffectParameter fog;
        readonly EffectParameter zFar;
        readonly EffectParameter overcast;
        readonly EffectParameter viewerPos;
        readonly EffectParameter imageTextureIsNight;
        readonly EffectParameter nightColorModifier;
        readonly EffectParameter halfNightColorModifier;
        readonly EffectParameter vegetationAmbientModifier;
        readonly EffectParameter signalLightIntensity;
        readonly EffectParameter eyeVector;
        readonly EffectParameter sideVector;

        readonly EffectParameter imageTexture;
        readonly EffectParameter overlayTexture;
        readonly EffectParameter overlayScale;

        // glTF-PBR:
        readonly EffectParameter baseColorFactor;
        readonly EffectParameter emissiveTexture;
        readonly EffectParameter emissiveFactor;
        readonly EffectParameter normalTexture;
        readonly EffectParameter normalScale;
        readonly EffectParameter occlusionTexture;
        readonly EffectParameter metallicRoughnessTexture;
        readonly EffectParameter occlusionFactor;
        readonly EffectParameter clearcoatTexture;
        readonly EffectParameter clearcoatFactor;
        readonly EffectParameter clearcoatRoughnessTexture;
        readonly EffectParameter clearcoatRoughnessFactor;
        readonly EffectParameter clearcoatNormalTexture;
        readonly EffectParameter clearcoatNormalScale;
        readonly EffectParameter referenceAlpha;
        readonly EffectParameter textureCoordinates1;
        readonly EffectParameter textureCoordinates2;
        readonly EffectParameter texturePacking;
        readonly EffectParameter hasNormals;
        readonly EffectParameter hasTangents;
        readonly EffectParameter bonesTexture;
        readonly EffectParameter bonesCount;
        readonly EffectParameter morphConfig;
        readonly EffectParameter morphWeights;
        // Per-frame PBR uniforms:
        readonly EffectParameter environmentMapSpecularTexture;
        readonly EffectParameter environmentMapDiffuseTexture;
        readonly EffectParameter brdfLutTexture;
        readonly EffectParameter numLights;
        readonly EffectParameter lightPositions;
        readonly EffectParameter lightDirections;
        readonly EffectParameter lightColorIntensities;
        readonly EffectParameter lightRangesRcp;
        readonly EffectParameter lightInnerConeCos;
        readonly EffectParameter lightOuterConeCos;
        readonly EffectParameter lightTypes;

        Vector3 _eyeVector;
        Vector4 _zBias_Lighting;
        Vector3 _sunDirection;
        bool _imageTextureIsNight;

        /// <summary>
        /// The position of the sampler states inside the hlsl shader:
        /// baseColor, metallicRoughness, occlusion, normal, emissive
        /// </summary>
        public enum Samplers
        {
            BaseColor = 0,
            Overlay,
            Normal,
            Emissive,
            Occlusion,
            MetallicRoughness,
            Clearcoat,
            ClearcoatRoughness,
            ClearcoatNormal,
        }

        public void SetViewMatrix(ref Matrix v)
        {
            _eyeVector = Vector3.Normalize(new Vector3(v.M13, v.M23, v.M33));

            eyeVector.SetValue(new Vector4(_eyeVector, Vector3.Dot(_eyeVector, _sunDirection) * 0.5f + 0.5f));
            sideVector.SetValue(Vector3.Normalize(Vector3.Cross(_eyeVector, Vector3.Down)));
        }

        public void SetMatrix(Matrix w, ref Matrix v, ref Matrix p)
        {
            world.SetValue(w);
            view.SetValue(v);
            projection.SetValue(p);

            int vIn = Program.Simulator.Settings.DayAmbientLight;
            
            float FullBrightness = (float)vIn / 20.0f ;
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

        public void ClearShadowMap()
        {
            shadowMapLimit.SetValue(Vector4.Zero);
        }

        public float ZBias { get { return _zBias_Lighting.X; } set { _zBias_Lighting.X = value; zBias_Lighting.SetValue(_zBias_Lighting); } }
        public float LightingDiffuse { get { return _zBias_Lighting.Y; } set { _zBias_Lighting.Y = value; zBias_Lighting.SetValue(_zBias_Lighting); } }
        public float LightingSpecular
        {
            get { return _zBias_Lighting.Z; }
            set
            {
                // Setting this exponent of HLSL pow() function to 0 in DX11 leads to undefined result. (HLSL bug?)
                _zBias_Lighting.Z = value >= 1 ? value : 1;
                _zBias_Lighting.W = value >= 1 ? 1 : 0;
                zBias_Lighting.SetValue(_zBias_Lighting);
            }
        }

        public void SetFog(float depth, ref Color color)
        {
            fog.SetValue(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1f / depth));
        }

        public void SetLightVector_ZFar(Vector3 sunDirection, float zFarDistance)
        {
            _sunDirection = sunDirection;
            zFar.SetValue(zFarDistance);
        }

        public float SignalLightIntensity { set { signalLightIntensity.SetValue(value); } }

        public float Overcast { set { overcast.SetValue(new Vector2(value, value / 2)); } }

        public Vector3 ViewerPos { set { viewerPos.SetValue(value); } }

        public bool ImageTextureIsNight { set { _imageTextureIsNight = value; imageTextureIsNight.SetValue(value ? 1f : 0f); } }

        public Texture2D ImageTexture { set { imageTexture.SetValue(value); } }

        public Texture2D OverlayTexture { set { overlayTexture.SetValue(value); } }

        public Texture2D EmissiveTexture { set { emissiveTexture.SetValue(value); } }

        public Texture2D NormalTexture { set { normalTexture.SetValue(value); } }

        public Texture2D MetallicRoughnessTexture { set { metallicRoughnessTexture.SetValue(value); } }

        public Texture2D OcclusionTexture { set { occlusionTexture.SetValue(value); } }

        public Texture2D ClearcoatTexture { set { clearcoatTexture.SetValue(value); } }

        public Texture2D ClearcoatRoughnessTexture { set { clearcoatRoughnessTexture.SetValue(value); } }

        public Texture2D ClearcoatNormalTexture { set { clearcoatNormalTexture.SetValue(value); } }

        public int ReferenceAlpha { set { referenceAlpha.SetValue(value / 255f); } }

        public float OverlayScale { set { overlayScale.SetValue(value); } }

        public Vector4 BaseColorFactor { set { baseColorFactor.SetValue(value); } }
        
        public Vector3 EmissiveFactor { set { emissiveFactor.SetValue(value); } }
        
        public float NormalScale { set { normalScale.SetValue(value); } }
        
        public Vector3 OcclusionFactor { set { occlusionFactor.SetValue(value); } }

        public float ClearcoatFactor { set { clearcoatFactor.SetValue(value); } }

        public float ClearcoatRoughnessFactor { set { clearcoatRoughnessFactor.SetValue(value); } }

        public float ClearcoatNormalScale { set { clearcoatNormalScale.SetValue(value); } }


        public Texture2D EnvironmentMapSpecularTexture { set { environmentMapSpecularTexture.SetValue(value); } }

        public TextureCube EnvironmentMapDiffuseTexture { set { environmentMapDiffuseTexture.SetValue(value); } }

        public Texture2D BrdfLutTexture { set { brdfLutTexture.SetValue(value); } }

        public Vector4 TextureCoordinates1 { set { textureCoordinates1.SetValue(value); } }
        
        public Vector4 TextureCoordinates2 { set { textureCoordinates2.SetValue(value); } }
        
        public float TexturePacking { set { texturePacking.SetValue(value); } }

        public bool HasNormals { set { hasNormals.SetValue(value); } }

        public bool HasTangents { set { hasTangents.SetValue(value); } }

        public Texture2D BonesTexture { set { bonesTexture.SetValue(value); } }

        public float BonesCount { set { bonesCount.SetValue(value); } }

        public int[] MorphConfig { set { morphConfig.SetValue(value); } }
        
        public float[] MorphWeights { set { morphWeights.SetValue(value); } }

        public int NumLights { set { numLights.SetValue(value); } }
        public Vector3[] LightPositions { set { lightPositions.SetValue(value); } }
        public Vector3[] LightDirections { set { lightDirections.SetValue(value); } }
        public Vector3[] LightColorIntensities { set { lightColorIntensities.SetValue(value); } }
        public float[] LightRangesRcp { set { lightRangesRcp.SetValue(value); } }
        public float[] LightInnerConeCos { set { lightInnerConeCos.SetValue(value); } }
        public float[] LightOuterConeCos { set { lightOuterConeCos.SetValue(value); } }
        public float[] LightTypes { set { lightTypes.SetValue(value); } }

        public SceneryShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "SceneryShader")
        {
            world = Parameters["World"];
            view = Parameters["View"];
            projection = Parameters["Projection"];
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
            zFar = Parameters["ZFar"];
            overcast = Parameters["Overcast"];
            viewerPos = Parameters["ViewerPos"];
            imageTextureIsNight = Parameters["ImageTextureIsNight"];
            nightColorModifier = Parameters["NightColorModifier"];
            halfNightColorModifier = Parameters["HalfNightColorModifier"];
            vegetationAmbientModifier = Parameters["VegetationAmbientModifier"];
            signalLightIntensity = Parameters["SignalLightIntensity"];
            eyeVector = Parameters["EyeVector"];
            sideVector = Parameters["SideVector"];
            imageTexture = Parameters["ImageTexture"];
            overlayTexture = Parameters["OverlayTexture"];
            emissiveTexture = Parameters["EmissiveTexture"];
            normalTexture = Parameters["NormalTexture"];
            metallicRoughnessTexture = Parameters["MetallicRoughnessTexture"];
            occlusionTexture = Parameters["OcclusionTexture"];
            clearcoatTexture = Parameters["ClearcoatTexture"];
            clearcoatRoughnessTexture = Parameters["ClearcoatRoughnessTexture"];
            clearcoatNormalTexture = Parameters["ClearcoatNormalTexture"];
            referenceAlpha = Parameters["ReferenceAlpha"];
            overlayScale = Parameters["OverlayScale"];
            baseColorFactor = Parameters["BaseColorFactor"];
            emissiveFactor = Parameters["EmissiveFactor"];
            normalScale = Parameters["NormalScale"];
            occlusionFactor = Parameters["OcclusionFactor"];
            clearcoatFactor = Parameters["ClearcoatFactor"];
            clearcoatRoughnessFactor = Parameters["ClearcoatRoughnessFactor"];
            clearcoatNormalScale = Parameters["ClearcoatNormalScale"];
            textureCoordinates1 = Parameters["TextureCoordinates1"];
            textureCoordinates2 = Parameters["TextureCoordinates2"];
            texturePacking = Parameters["TexturePacking"];
            hasNormals = Parameters["HasNormals"];
            hasTangents = Parameters["HasTangents"];
            bonesTexture = Parameters["BonesTexture"];
            bonesCount = Parameters["BonesCount"];
            morphConfig = Parameters["MorphConfig"];
            morphWeights = Parameters["MorphWeights"];
            environmentMapSpecularTexture = Parameters["EnvironmentMapSpecularTexture"];
            environmentMapDiffuseTexture = Parameters["EnvironmentMapDiffuseTexture"];
            brdfLutTexture = Parameters["BrdfLutTexture"];
            numLights = Parameters["NumLights"];
            lightPositions = Parameters["LightPositions"];
            lightDirections = Parameters["LightDirections"];
            lightColorIntensities = Parameters["LightColorIntensities"];
            lightRangesRcp = Parameters["LightRangesRcp"];
            lightInnerConeCos = Parameters["LightInnerConeCos"];
            lightOuterConeCos = Parameters["LightOuterConeCos"];
            lightTypes = Parameters["LightTypes"];
        }
    }

    [CallOnThread("Render")]
    public class ShadowMapShader : Shader
    {
        readonly EffectParameter worldViewProjection;
        readonly EffectParameter sideVector;
        readonly EffectParameter imageBlurStep;
        readonly EffectParameter imageTexture;
        readonly EffectParameter bonesTexture;
        readonly EffectParameter bonesCount;
        readonly EffectParameter morphConfig;
        readonly EffectParameter morphWeights;

        public void SetData(ref Matrix v)
        {
            var eyeVector = Vector3.Normalize(new Vector3(v.M13, v.M23, v.M33));
            sideVector.SetValue(Vector3.Normalize(Vector3.Cross(eyeVector, Vector3.Down)));
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

        public Texture2D BonesTexture { set { bonesTexture.SetValue(value); } }
        public float BonesCount { set { bonesCount.SetValue(value); } }
        public int[] MorphConfig { set { morphConfig.SetValue(value); } }
        public float[] MorphWeights { set { morphWeights.SetValue(value); } }

        public ShadowMapShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "ShadowMap")
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            sideVector = Parameters["SideVector"];
            imageBlurStep = Parameters["ImageBlurStep"];
            imageTexture = Parameters["ImageTexture"];
            bonesTexture = Parameters["BonesTexture"];
            bonesCount = Parameters["BonesCount"];
            morphConfig = Parameters["MorphConfig"];
            morphWeights = Parameters["MorphWeights"];
        }
    }

    [CallOnThread("Render")]
    public class SkyShader : Shader
    {
        readonly EffectParameter worldViewProjection;
        readonly EffectParameter lightVector;
        readonly EffectParameter time;
        readonly EffectParameter overcast;
        readonly EffectParameter cloudScalePosition;
        readonly EffectParameter skyColor;
        readonly EffectParameter fogColor;
        readonly EffectParameter fog;
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
                lightVector.SetValue(new Vector4(value, 1f / value.Length()));

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

        public void SetFog(float depth, ref Color color)
        {
            fogColor.SetValue(new Vector3(color.R / 255f, color.G / 255f, color.B / 255f));
            fog.SetValue(new Vector4(5000f / depth, 0.015f * MathHelper.Clamp(depth / 5000f, 0, 1), MathHelper.Clamp(depth / 10000f, 0, 1), 0.05f * MathHelper.Clamp(depth / 10000f, 0, 1)));
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
                    overcast.SetValue(new Vector4(5 * value, 0.0f, 0.0f, 0.0f));
                else
                    // Coefficients selected by author to achieve the desired appearance
                    overcast.SetValue(new Vector4(MathHelper.Clamp(2 * value - 0.4f, 0, 1), 1.25f - 1.125f * value, 1.15f - 0.75f * value, 1f));
            }
        }

        public Vector4 CloudScalePosition { set => cloudScalePosition.SetValue(value); }

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

            var eye = Vector3.Normalize(new Vector3(view.M13, view.M23, view.M33));
            var right = Vector3.Cross(eye, Vector3.Up);
            var up = Vector3.Cross(right, eye);

            rightVector.SetValue(right * moonScale);
            upVector.SetValue(up * moonScale);
        }

        public void SetMatrix(ref Matrix wvp)
        {
            worldViewProjection.SetValueTranspose(wvp);
        }

        public SkyShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "SkyShader")
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            lightVector = Parameters["LightVector"];
            time = Parameters["Time"];
            overcast = Parameters["Overcast"];
            cloudScalePosition = Parameters["CloudScalePosition"];
            skyColor = Parameters["SkyColor"];
            fogColor = Parameters["FogColor"];
            fog = Parameters["Fog"];
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
        static float Day2Night(float startNightTrans, float finishNightTrans, float minDarknessCoeff, float sunDirectionY)
        {
            int vIn = Program.Simulator.Settings.DayAmbientLight;
            float dayAmbientLight = (float)vIn / 20.0f ;
              
            // The following two are used to interpoate between day and night lighting (y = mx + b)
            var slope = (dayAmbientLight - minDarknessCoeff) / (startNightTrans - finishNightTrans); // "m"
            var incpt = dayAmbientLight - slope * startNightTrans; // "b"
            // This is the return value used to darken scenery
            float adjustment;

            if (sunDirectionY < finishNightTrans)
                adjustment = minDarknessCoeff;
            else if (sunDirectionY > startNightTrans)
                adjustment = dayAmbientLight; // Scenery is fully lit during the day
            else
                adjustment = slope * sunDirectionY + incpt;

            return adjustment;
        }
    }

    [CallOnThread("Render")]
    public class ParticleEmitterShader : Shader
    {
        EffectParameter emitSize;
        EffectParameter tileXY;
        EffectParameter currentTime;
        EffectParameter wvp;
        EffectParameter invView;
        EffectParameter texture;
        EffectParameter lightVector;
        EffectParameter fog;

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

        public float EmitSize
        {
            set { emitSize.SetValue(value); }
        }

        public Vector3 LightVector
        {
            set { lightVector.SetValue(value); }
        }

        public ParticleEmitterShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "ParticleEmitterShader")
        {
            emitSize = Parameters["emitSize"];
            currentTime = Parameters["currentTime"];
            wvp = Parameters["worldViewProjection"];
            invView = Parameters["invView"];
            tileXY = Parameters["cameraTileXY"];
            texture = Parameters["particle_Tex"];
            lightVector = Parameters["LightVector"];
            fog = Parameters["Fog"];
        }

        public void SetMatrix(Matrix world, ref Matrix view, ref Matrix projection)
        {
            wvp.SetValue(world * view * projection);
            invView.SetValue(Matrix.Invert(view));
        }

        public void SetFog(float depth, ref Color color)
        {
            fog.SetValue(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, MathHelper.Clamp(300f / depth, 0, 1)));
        }
    }

    [CallOnThread("Render")]
    public class LightGlowShader : Shader
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

        public LightGlowShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "LightGlowShader")
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            fade = Parameters["Fade"];
            lightGlowTexture = Parameters["LightGlowTexture"];
        }
    }

    [CallOnThread("Render")]
    public class LightConeShader : Shader
    {
        EffectParameter worldViewProjection;
        EffectParameter fade;

        public LightConeShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "LightConeShader")
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
    public class PopupWindowShader : Shader
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
                    screenSize.SetValue(new Vector2(0, 0));
                else
                    screenSize.SetValue(new Vector2(value.Width, value.Height));
            }
        }

        public Color GlassColor { set { glassColor.SetValue(new Vector3(value.R / 255f, value.G / 255f, value.B / 255f)); } }

        public void SetMatrix(Matrix w, ref Matrix wvp)
        {
            world.SetValue(w);
            worldViewProjection.SetValue(wvp);
        }

        public PopupWindowShader(Viewer viewer, GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "PopupWindow")
        {
            world = Parameters["World"];
            worldViewProjection = Parameters["WorldViewProjection"];
            glassColor = Parameters["GlassColor"];
            screenSize = Parameters["ScreenSize"];
            screenTexture = Parameters["ScreenTexture"];
            // TODO: This should happen on the loader thread.
            Parameters["WindowTexture"].SetValue(SharedTextureManager.LoadInternal(graphicsDevice, System.IO.Path.Combine(viewer.ContentPath, "Window.png")));
        }
    }

    [CallOnThread("Render")]
    public class CabShader : Shader
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

        public CabShader(GraphicsDevice graphicsDevice, Vector4 light1Position, Vector4 light2Position, Vector3 light1Color, Vector3 light2Color)
            : base(graphicsDevice, "CabShader")
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

    [CallOnThread("Render")]
    public class DriverMachineInterfaceShader : Shader
    {
        readonly EffectParameter limitAngle;
        readonly EffectParameter normalColor;
        readonly EffectParameter limitColor;
        readonly EffectParameter pointerColor;
        readonly EffectParameter interventionColor;
        readonly EffectParameter backgroundColor;
        //readonly EffectParameter imageTexture;

        public void SetData(Vector4 angle, Color gaugeColor, Color needleColor, Color overspeedColor)
        {
            limitAngle.SetValue(angle);
            limitColor.SetValue(gaugeColor.ToVector4());
            pointerColor.SetValue(needleColor.ToVector4());
            interventionColor.SetValue(overspeedColor.ToVector4());
        }

        public DriverMachineInterfaceShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "DriverMachineInterfaceShader")
        {
            normalColor = Parameters["NormalColor"];
            limitColor = Parameters["LimitColor"];
            pointerColor = Parameters["PointerColor"];
            backgroundColor = Parameters["BackgroundColor"];
            limitAngle = Parameters["LimitAngle"];
            //imageTexture = Parameters["ImageTexture"];
            interventionColor = Parameters["InterventionColor"];
        }
    }

    [CallOnThread("Render")]
    public class DebugShader : Shader
    {
        readonly EffectParameter worldViewProjection;
        readonly EffectParameter screenSize;
        readonly EffectParameter graphPos;
        readonly EffectParameter graphSample;

        public Vector2 ScreenSize { set { screenSize.SetValue(value); } }

        public Vector4 GraphPos { set { graphPos.SetValue(value); } }

        public Vector2 GraphSample { set { graphSample.SetValue(value); } }

        public DebugShader(GraphicsDevice graphicsDevice)
            : base(graphicsDevice, "DebugShader")
        {
            worldViewProjection = Parameters["WorldViewProjection"];
            screenSize = Parameters["ScreenSize"];
            graphPos = Parameters["GraphPos"];
            graphSample = Parameters["GraphSample"];
        }

        public void SetMatrix(Matrix matrix, ref Matrix viewproj)
        {
            worldViewProjection.SetValue(matrix * viewproj);
        }
    }
}
