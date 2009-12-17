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
    /// This encapsulates a consistent method for setting matrix parameters in the render loop.
    /// </summary>
    public interface Shader
    {
        void SetMatrix(Matrix world, Matrix view, Matrix projection);
    }


    public class BasicShader : BasicEffect, Shader
    {
        public BasicShader(BasicEffect basicEffect)
            : base(basicEffect.GraphicsDevice, basicEffect)
        {
        }

        public void SetMatrix(Matrix xnaWorld, Matrix xnaView, Matrix xnaProjection)
        {
            World = xnaWorld;
            View = xnaView;
            Projection = xnaProjection;
        }
    }


    public class Grey : BasicShader
    {
        public Grey(GraphicsDeviceManager gdm, ContentManager content)
            : base(new BasicEffect(gdm.GraphicsDevice, new EffectPool()))
        {
            base.EnableDefaultLighting();
            base.DirectionalLight0.DiffuseColor = Color.White.ToVector3();
            base.DirectionalLight1.DiffuseColor = Color.LightGray.ToVector3();
            BasicShader x = this;
            base.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            base.DiffuseColor = new Vector3(0.8f, 0.8f, 0.8f);
        }
    }

    public class Red : BasicShader
    {
        public Red(GraphicsDeviceManager gdm, ContentManager content)
            : base(new BasicEffect(gdm.GraphicsDevice, new EffectPool()))
        {
            base.LightingEnabled = false;
            base.AmbientLightColor = new Vector3(1, 0, 0);
            base.DiffuseColor = new Vector3(1, 0, 0);
        }
    }

    public class SceneryShader : Effect, Shader
    {
        EffectParameter mModelToProjection = null;
        EffectParameter mWorldToView = null;
        EffectParameter mModelToWorld = null;
        EffectParameter imageMap_Tex = null;
        EffectParameter normalMap_Tex = null;
        EffectParameter brightness = null;
        EffectParameter saturation = null;
        EffectParameter ambient = null;


        Matrix XNAMatrix;

        public Matrix XNAWorld
        {
            get { return XNAMatrix; }
        }

        public Texture2D Texture
        {
            set { imageMap_Tex.SetValue(value); }
        }

        public Texture2D BumpTexture
        {
            set { normalMap_Tex.SetValue(value); }
        }

        private float brightnessValue = 0.7f;
        public float Brightness
        {
            get { return brightnessValue; }
            set { brightnessValue = value; brightness.SetValue(brightnessValue); }
        }

        private float saturationValue = 0.9f;
        public float Saturation
        {
            get { return saturationValue; }
            set { saturationValue = value; saturation.SetValue(saturationValue); }
        }

        private float ambientValue = 0.5f;
        public float Ambient
        {
            get { return ambientValue; }
            set { ambientValue = value; ambient.SetValue(ambientValue); }
        }

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

            Parameters["LightVector"].SetValue(Vector3.Normalize(new Vector3(1f, .3f, 1f)));
        }

        public void SetMatrix(Matrix world, Matrix view, Matrix projection)
        {
            mModelToProjection.SetValueTranspose((world * view) * projection);
            mWorldToView.SetValue(Matrix.Invert(view));
            mModelToWorld.SetValue(world);
            XNAMatrix = world;
        }
    }

}
