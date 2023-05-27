// COPYRIGHT 2012 by the Open Rails project.
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

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Orts.Parsers.Msts;

namespace Orts.Simulation.RollingStocks
{
    /*
    [ORTSPhysicsFile(".orts", "ORTSElectricLocomotive")]
    [ORTSPhysicsFile(".orcvf", "ORTSExtendendCVF", "CVF")]
    public partial class MSTSElectricLocomotive
    {
        [ORTSPhysics("Extended Name", "ExtName", "The extended name of the Locomotive", "<None>")]
        public string ExtendedName;

        [ORTSPhysics("Extended Size", "ExtSize", "The extended size of the Locomotive", 42.42)]
        public double ExtendedSize;

        [ORTSPhysics("Second light RGB", "LightColorRGB", "The color of the second cab light", "255 255 255", "CVF")]
        public Vector3 LightColorRGB;
    }
    */
    public partial class MSTSLocomotive
    {
        internal virtual void InitializeFromORTSSpecific(string wagFilePath, object initWhat)
        {
            object[] fattrs;
            if (initWhat == null)
                initWhat = this;
            System.Reflection.MemberInfo info = initWhat.GetType();
            fattrs = Attribute.GetCustomAttributes(info, typeof(ORTSPhysicsFileAttribute), true);

            bool setdef = true;

            foreach (object fattr in fattrs)
            {
                ORTSPhysicsFileAttribute opfa = fattr as ORTSPhysicsFileAttribute;
                STFReader stf = opfa.OpenSTF(wagFilePath);
                bool hasFile = stf != null;

                object[] attrs;
                STFReader.TokenProcessor tp;
                List<STFReader.TokenProcessor> result = new List<STFReader.TokenProcessor>();

                ORTSPhysicsAttribute attr;
                FieldInfo[] fields = initWhat.GetType().GetFields();
                foreach (FieldInfo fi in fields)
                {
                    attrs = fi.GetCustomAttributes(typeof(ORTSPhysicsAttribute), false);
                    if (attrs.Length > 0)
                    {
                        attr = attrs[0] as ORTSPhysicsAttribute;

                        if (setdef)
                            fi.SetValue2(initWhat, attr.DefaultValue);

                        if (hasFile && opfa.FileID == attr.FileID)
                        {
                            AttributeProcessor ap = new AttributeProcessor(initWhat, fi, stf, attr.DefaultValue);
                            tp = new STFReader.TokenProcessor(attr.Token, ap.P);

                            result.Add(tp);
                        }
                    }
                }

                setdef = false;

                if (hasFile)
                {
                    stf.MustMatch(opfa.Token);
                    stf.MustMatch("(");
                    stf.ParseBlock(result.ToArray());
                    stf.Dispose();
                }
            }
        }

        internal virtual void InitializeFromORTSSpecificCopy(MSTSLocomotive locoFrom)
        {
            FieldInfo[] fields = this.GetType().GetFields();
            object[] attrs;
            foreach (FieldInfo fi in fields)
            {
                attrs = fi.GetCustomAttributes(typeof(ORTSPhysicsAttribute), false);
                if (attrs.Length > 0)
                {
                    fi.SetValue(this, fi.GetValue(locoFrom));
                }
            }
        }
    }

    #region Attributes
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ORTSPhysicsFileAttribute : Attribute
    {
        public string NamePattern { get; private set; }
        public string Token { get; private set; }
        public string FileID { get; private set; }

        /// <summary>
        /// Constructs an ORTS custom physics Attribute class
        /// <para>Used to specify the STF file extension and initial token.</para>
        /// <para>If multiple physics files are used, other than default, specify the FileID also.</para>
        /// </summary>
        /// <param name="namePattern">Extension of STF file</param>
        /// <param name="token">Token in STF file</param>
        public ORTSPhysicsFileAttribute(string namePattern, string token)
        {
            NamePattern = namePattern;
            Token = token.ToLower();
            FileID = "default";
        }

        /// <summary>
        /// Constructs an ORTS custom physics Attribute class
        /// <para>Used to specify the STF file extension and initial token.</para>
        /// <para>If multiple physics files are used, other than default, specify the FileID also.</para>
        /// </summary>
        /// <param name="namePattern">Extension of STF file</param>
        /// <param name="token">Token in STF file</param>
        /// <param name="fileID">ID of the file, used to separate attributes specified in different files. Default value is 'default'.</param>
        public ORTSPhysicsFileAttribute(string namePattern, string token, string fileID)
        {
            NamePattern = namePattern;
            Token = token.ToLower();
            FileID = fileID;
        }

        public STFReader OpenSTF(string engFile)
        {
            try
            {
                string name = engFile;

                if (!NamePattern.StartsWith("."))
                {
                    int lp = name.LastIndexOf('.');
                    name = name.Substring(0, lp + 1);
                }
                name += NamePattern;

                return new STFReader(name, false);
            }
            catch
            {
                return null;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ORTSPhysicsAttribute : Attribute
    {
        public string Title { get; private set; }
        public string Token { get; private set; }
        public string Description { get; private set; }
        public object DefaultValue { get; private set; }
        public string FileID { get; private set; }

        /// <summary>
        /// Constructs an ORTS custom physics Attribute class
        /// <para>Parsed types are string, bool, int, float, double, Vector3</para>
        /// </summary>
        /// <param name="title">Short, meaningful title of the physics Attribute, displayed in the Editor progam</param>
        /// <param name="token">Token in STF of the physics Attribute</param>
        /// <param name="description">Longer description of the physics Attribute, displayed in the Editor progam</param>
        /// <param name="defaultvalue">Default value of the physics Attribute -- BE CAUTIOUS of the given value's type, it is checked at runtime ONLY!
        /// <para>Vector3 values must be specified as string, numeric values separated by space, ',' or ':'</para></param>
        public ORTSPhysicsAttribute(string title, string token, string description, object defaultvalue)
        {
            Title = title;
            Token = token.ToLower();
            Description = description;
            DefaultValue = defaultvalue;
            FileID = "default";
        }

        /// <summary>
        /// Constructs an ORTS custom physics Attribute class
        /// <para>Parsed types are string, bool, int, float, double, Vector3</para>
        /// </summary>
        /// <param name="title">Short, meaningful title of the physics Attribute, displayed in the Editor progam</param>
        /// <param name="token">Token in STF of the physics Attribute</param>
        /// <param name="description">Longer description of the physics Attribute, displayed in the Editor progam</param>
        /// <param name="defaultvalue">Default value of the physics Attribute -- BE CAUTIOUS of the given value's type, it is checked at runtime ONLY!
        /// <para>Vector3 values must be specified as string, numeric values separated by space, ',' or ':'</para></param>
        /// <param name="fileID">Optional, string ID of the file containing the Attribute. The ID is specified at ORTSPhysicsFileAttribute on the class. Default value is 'default'.</param>
        public ORTSPhysicsAttribute(string title, string token, string description, object defaultvalue, string fileID)
        {
            Title = title;
            Token = token.ToLower();
            Description = description;
            DefaultValue = defaultvalue;
            FileID = fileID;
        }
    }

    public class AttributeProcessor
    {
        public STFReader.Processor P;
        private FieldInfo _fi;

        public AttributeProcessor(object setWhom, FieldInfo fi, STFReader stf, object defaultValue)
        {
            _fi = fi;

            P = () =>
            {
                switch (_fi.FieldType.Name.ToLower())
                {
                    case "int":
                    case "int32":
                        {
                            int? i = defaultValue as int?;
                            _fi.SetValue(setWhom,
                                stf.ReadIntBlock(i));
                            break;
                        }
                    case "bool":
                    case "boolean":
                        {
                            bool? b = defaultValue as bool?;
                            _fi.SetValue(setWhom,
                                stf.ReadBoolBlock(b.Value));
                            break;
                        }
                    case "string":
                        {
                            string s = defaultValue as string;
                            _fi.SetValue(setWhom,
                                stf.ReadStringBlock(s));
                            break;
                        }
                    case "float":
                    case "single":
                        {
                            float? f = defaultValue as float?;
                            _fi.SetValue(setWhom,
                                stf.ReadFloatBlock(STFReader.UNITS.Any, f));
                            break;
                        }
                    case "double":
                        {
                            double? d = defaultValue as double?;
                            _fi.SetValue(setWhom,
                                stf.ReadDoubleBlock(d));
                            break;
                        }
                    case "vector3":
                        {
                            Vector3 v3 = (defaultValue as string).ParseVector3();
                            {
                                _fi.SetValue(setWhom,
                                    stf.ReadVector3Block(STFReader.UNITS.Any, v3));
                            }
                            break;
                        }
                    case "vector4":
                        {
                            Vector4 v4 = (defaultValue as string).ParseVector4();
                            {
                                _fi.SetValue(setWhom,
                                    stf.ReadVector4Block(STFReader.UNITS.Any, v4));
                            }
                            break;
                        }
                    case "color":
                        {
                            Color c = (defaultValue as string).ParseColor();
                            {
                                Vector4 v4 = new Vector4(-1);
                                v4 = stf.ReadVector4Block(STFReader.UNITS.Any, v4);
                                if (v4.W == -1)
                                {
                                    c.A = 255;
                                    c.R = v4.X == -1 ? c.R : (byte)v4.X;
                                    c.G = v4.Y == -1 ? c.G : (byte)v4.Y;
                                    c.B = v4.Z == -1 ? c.B : (byte)v4.Z;
                                }
                                else
                                {
                                    c.A = v4.X == -1 ? c.A : (byte)v4.X;
                                    c.R = v4.Y == -1 ? c.R : (byte)v4.Y;
                                    c.G = v4.Z == -1 ? c.G : (byte)v4.Z;
                                    c.B = v4.W == -1 ? c.B : (byte)v4.W;
                                }
                                _fi.SetValue(setWhom, c);
                            }
                            break;
                        }
                }
            };
        }
    }

    public static class VectorExt
    {
        public static Vector3 ParseVector3(this string s)
        {
            Vector3 v = new Vector3();
            if (!string.IsNullOrEmpty(s))
            {
                string[] ax = s.Split(new char[] { ' ', ',', ':' });
                if (ax.Length == 3)
                {
                    v.X = float.Parse(ax[0]);
                    v.Y = float.Parse(ax[1]);
                    v.Z = float.Parse(ax[2]);
                }
            }
            return v;
        }

        public static Vector4 ParseVector4(this string s)
        {
            Vector4 v = new Vector4();
            if (!string.IsNullOrEmpty(s))
            {
                string[] ax = s.Split(new char[] { ' ', ',', ':' });
                if (ax.Length == 4)
                {
                    v.X = float.Parse(ax[0]);
                    v.Y = float.Parse(ax[1]);
                    v.Z = float.Parse(ax[2]);
                    v.W = float.Parse(ax[3]);
                }
            }
            return v;
        }

        public static Color ParseColor(this string s)
        {
            Color c = new Color();
            if (!string.IsNullOrEmpty(s))
            {
                string[] ax = s.Split(new char[] { ' ', ',', ':' });
                if (ax.Length == 4)
                {
                    c.A = byte.Parse(ax[0]);
                    c.R = byte.Parse(ax[1]);
                    c.G = byte.Parse(ax[2]);
                    c.B = byte.Parse(ax[3]);
                }
                else if (ax.Length == 3)
                {
                    c.A = 255;
                    c.R = byte.Parse(ax[0]);
                    c.G = byte.Parse(ax[1]);
                    c.B = byte.Parse(ax[2]);
                }
            }
            return c;
        }

        public static void SetValue2(this FieldInfo fi, object obj, object value)
        {
            if (fi.FieldType.Name == "Vector3")
            {
                Vector3 v3 = (value as string).ParseVector3();
                fi.SetValue(obj, v3);
            }
            else if (fi.FieldType.Name == "Vector4")
            {
                Vector4 v4 = (value as string).ParseVector4();
                fi.SetValue(obj, v4);
            }
            else if (fi.FieldType.Name == "Color")
            {
                Color c = (value as string).ParseColor();
                fi.SetValue(obj, c);
            }
            else
            {
                fi.SetValue(obj, value);
            }
        }
    }
    #endregion
}
