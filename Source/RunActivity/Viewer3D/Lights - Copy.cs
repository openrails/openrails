using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MSTS;

namespace ORTS
{
    public class Lights
    {
        public int numLights;
        public int[] type;
        public int[] headlight;
        public int[] unit;
        public int[] penalty;
        public int[] control;
        public int[] service;
        public int[] timeofday;
        public int[] weather;
        public int[] coupling;
        public int[] cycle;
        public float[] fadein;
        public float[] fadeout;

        public int[] numStates;
        LightStates[,] lightStates;

        public Lights()
        {
        }
        
        public void ReadWagLights(STFReader f)
        {
            string token = f.ReadToken();
            while( token != "" ) // EOF
			{
				if( token == ")" ) break; // throw ( new STFError( f, "Unexpected )" ) );  we should really throw an exception
                                          // but MSTS just ignores the rest of the file, and will also
                else
                {
                    numLights = f.ReadInt();
                    type = new int[numLights];
                    headlight = new int[numLights];
                    unit = new int[numLights];
                    penalty = new int[numLights];
                    control = new int[numLights];
                    service = new int[numLights];
                    timeofday = new int[numLights];
                    weather = new int[numLights];
                    coupling = new int[numLights];
                    cycle = new int[numLights];
                    fadein = new float[numLights];
                    fadeout = new float[numLights];
                    numStates = new int[numLights];
                    for (int i = 0; i < numLights; i++)
                    {
				        token = f.ReadToken();
                        if (0 == String.Compare(token, "Light", true))
                        {
                            f.MustMatch("(");
					        token = f.ReadToken();
                            while (token != ")")
                            {
                                if (token == "") throw (new STFError(f, "Missing )"));
                                else if (0 == String.Compare(token, "comment", true))
                                {
                                    f.ReadDelimitedItem();
                                }
                                else if (0 == String.Compare(token, "Type", true))
                                {
                                    f.MustMatch("(");
                                    type[i] = f.ReadInt();
                                    f.MustMatch(")");
                                }
                                else if (0 == String.Compare(token, "Conditions", true))
                                {
                                    f.MustMatch("(");
                                    token = f.ReadToken();
                                    while (token != ")")
                                    {
                                        if (0 == String.Compare(token, "Headlight", true))
                                        {
                                            f.MustMatch("(");
                                            headlight[i] = f.ReadInt();
                                            f.MustMatch(")");
                                        }
                                        else if (0 == String.Compare(token, "Unit", true))
                                        {
                                            f.MustMatch("(");
                                            unit[i] = f.ReadInt();
                                            f.MustMatch(")");
                                        }
                                        else if (0 == String.Compare(token, "Penalty", true))
                                        {
                                            f.MustMatch("(");
                                            penalty[i] = f.ReadInt();
                                            f.MustMatch(")");
                                        }
                                        else if (0 == String.Compare(token, "Control", true))
                                        {
                                            f.MustMatch("(");
                                            control[i] = f.ReadInt();
                                            f.MustMatch(")");
                                        }
                                        else if (0 == String.Compare(token, "Service", true))
                                        {
                                            f.MustMatch("(");
                                            service[i] = f.ReadInt();
                                            f.MustMatch(")");
                                        }
                                        else if (0 == String.Compare(token, "TimeOfDay", true))
                                        {
                                            f.MustMatch("(");
                                            timeofday[i] = f.ReadInt();
                                            f.MustMatch(")");
                                        }
                                        else if (0 == String.Compare(token, "Weather", true))
                                        {
                                            f.MustMatch("(");
                                            weather[i] = f.ReadInt();
                                            f.MustMatch(")");
                                        }
                                        else if (0 == String.Compare(token, "Coupling", true))
                                        {
                                            f.MustMatch("(");
                                            coupling[i] = f.ReadInt();
                                            f.MustMatch(")");
                                        }
                                        token = f.ReadToken();
                                    }
                                }// else if (0 == String.Compare(token, "Conditions", true))
                                else if (0 == String.Compare(token, "Cycle", true))
                                {
                                    f.MustMatch("(");
                                    cycle[i] = f.ReadInt();
                                    f.MustMatch(")");
                                }
                                else if (0 == String.Compare(token, "FadeIn", true))
                                {
                                    f.MustMatch("(");
                                    fadein[i] = f.ReadFloat();
                                    f.MustMatch(")");
                                }
                                else if (0 == String.Compare(token, "FadeOut", true))
                                {
                                    f.MustMatch("(");
                                    fadeout[i] = f.ReadFloat();
                                    f.MustMatch(")");
                                }
                                else if (0 == String.Compare(token, "States", true))
                                {
                                    f.MustMatch("(");
                                    int nStates = f.ReadInt();
                                    numStates[i] = nStates;
                                    lightStates = new LightStates[i, nStates];
                                    for (int j = 0; j < nStates; j++)
                                    {
                                        lightStates[i, j] = new LightStates();
                                        lightStates[i, j].ReadLightStates(f);
                                    }
                                }// else if (0 == String.Compare(token, "States", true))
                                token = f.ReadToken();
                            }// while (token != ")")
                            token = f.ReadToken();
                        }// if (0 == String.Compare(token, "Light", true))
                        else break;
                    }// for (int i = 0; i < numLights; i++)
                }// else file is readable
			}// while !EOF
        }// ReadWagLights
    }// Lights

    public class LightStates
    {
        public float duration;
        public float transition;
        public float radius;
        public float angle;
        public Vector3 position;
        public Vector3 azimuth;
        public Vector3 elevation;
        public int color;

        public LightStates()
        {
        }

        public void ReadLightStates(STFReader f)
        {
            string token = f.ReadToken();
            if (0 == String.Compare(token, "State", true))
            {
                f.MustMatch("(");
                token = f.ReadToken();
                while (token != ")")
                {
                    if (token == "") throw (new STFError(f, "Missing )"));
                    else if (0 == String.Compare(token, "Duration", true))
                    {
                        f.MustMatch("(");
                        duration = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Transition", true))
                    {
                        f.MustMatch("(");
                        transition = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Radius", true))
                    {
                        f.MustMatch("(");
                        radius = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Angle", true))
                    {
                        f.MustMatch("(");
                        angle = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Position", true))
                    {
                        f.MustMatch("(");
                        position.X = f.ReadFloat();
                        position.Y = f.ReadFloat();
                        position.Z = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Azimuth", true))
                    {
                        f.MustMatch("(");
                        azimuth.X = f.ReadFloat();
                        azimuth.Y = f.ReadFloat();
                        azimuth.Z = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "Elevation", true))
                    {
                        f.MustMatch("(");
                        elevation.X = f.ReadFloat();
                        elevation.Y = f.ReadFloat();
                        elevation.Z = f.ReadFloat();
                        f.MustMatch(")");
                    }
                    else if (0 == String.Compare(token, "LightColour", true))
                    {
                        f.MustMatch("(");
                        color = f.ReadHex();
                        f.MustMatch(")");
                    }
                    token = f.ReadToken();
                }// while (token != ")")
            }// if (0 == String.Compare(token, "State", true))
        }// ReadLightStates
    }// LightStates
}

