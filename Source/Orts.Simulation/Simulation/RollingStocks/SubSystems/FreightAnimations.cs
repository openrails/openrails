// COPYRIGHT 2010, 2011, 2012, 2013, 2014, 2015 by the Open Rails project.
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

using Orts.Parsers.Msts;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Orts.Simulation.RollingStocks.SubSystems
{
    /// <summary>
    /// An FreightAnimations object is created for any engine or wagon having an 
    /// ORTSFreightAnims block in its ENG/WAG file. It contains a collection of
    /// FreightAnimation objects.
    /// Called from within the MSTSWagon class.
    /// </summary>
    public class FreightAnimations
    {
        public List<FreightAnimation> Animations = new List<FreightAnimation>();
        public float FreightWeight = 0;
        public float StaticFreightWeight = 0;
        public MSTSWagon.PickupType FreightType = MSTSWagon.PickupType.None;
        public bool MSTSFreightAnimEnabled = true;
        public float WagonEmptyWeight = -1;
        public FreightAnimationContinuous LoadedOne = null;
        public FreightAnimationContinuous FullPhysicsContinuousOne; // Allow reading of full physics parameters for continuous freight animation
        public FreightAnimationStatic FullPhysicsStaticOne; // Allow reading of full physics for static freight animation
        public FreightAnimationDiscrete DiscreteLoadedOne = null;
        public float LoadingStartDelay = 0;
        public float UnloadingStartDelay = 0;
        public bool IsGondola = false;
 
        // additions to manage consequences of variable weight on friction and brake forces
        public float EmptyORTSDavis_A = -9999;
        public float EmptyORTSDavis_B = -9999;
        public float EmptyORTSDavis_C = -9999;
        public float EmptyORTSWagonFrontalAreaM2 = -9999;
        public float EmptyORTSDavisDragConstant = -9999;
        public float EmptyMaxBrakeForceN = -9999;
        public float EmptyMaxHandbrakeForceN = -9999;
        public float EmptyCentreOfGravityM_Y = -9999; // get centre of gravity after adjusted for freight animation
        public bool ContinuousFreightAnimationsPresent = false; // Flag to indicate that a continuous freight animation is present
        public bool StaticFreightAnimationsPresent = false; // Flag to indicate that a continuous freight animation is present

        public FreightAnimations(STFReader stf, MSTSWagon wagon)
        {
            stf.MustMatch("(");
            bool empty = true;
              stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("mstsfreightanimenabled", ()=>{ MSTSFreightAnimEnabled = stf.ReadBoolBlock(true);}),
                new STFReader.TokenProcessor("wagonemptyweight", ()=>{ WagonEmptyWeight = stf.ReadFloatBlock(STFReader.UNITS.Mass, -1); }),
                new STFReader.TokenProcessor("loadingstartdelay", ()=>{ UnloadingStartDelay = stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
                new STFReader.TokenProcessor("unloadingstartdelay", ()=>{ UnloadingStartDelay = stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
                new STFReader.TokenProcessor("isgondola", ()=>{ IsGondola = stf.ReadBoolBlock(false);}),
                // additions to manage consequences of variable weight on friction and brake forces
                new STFReader.TokenProcessor("emptyortsdavis_a", ()=>{ EmptyORTSDavis_A = stf.ReadFloatBlock(STFReader.UNITS.Force, -1); }),
                new STFReader.TokenProcessor("emptyortsdavis_b", ()=>{ EmptyORTSDavis_B = stf.ReadFloatBlock(STFReader.UNITS.Resistance, -1); }),
                new STFReader.TokenProcessor("emptyortsdavis_c", ()=>{ EmptyORTSDavis_C = stf.ReadFloatBlock(STFReader.UNITS.ResistanceDavisC, -1); }),
                new STFReader.TokenProcessor("emptyortswagonfrontalarea", ()=>{ EmptyORTSWagonFrontalAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, -1); }),
                new STFReader.TokenProcessor("emptyortsdavisdragconstant", ()=>{ EmptyORTSDavisDragConstant = stf.ReadFloatBlock(STFReader.UNITS.Any, -1); }),
                new STFReader.TokenProcessor("emptymaxbrakeforce", ()=>{ EmptyMaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, -1); }),
                new STFReader.TokenProcessor("emptymaxhandbrakeforce", ()=>{ EmptyMaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, -1); }),
                new STFReader.TokenProcessor("emptycentreofgravity_y", ()=>{ EmptyCentreOfGravityM_Y = stf.ReadFloatBlock(STFReader.UNITS.Distance, -1); }),
                new STFReader.TokenProcessor("freightanimcontinuous", ()=>
                {
                    Animations.Add(new FreightAnimationContinuous(stf, wagon));
                    FullPhysicsContinuousOne = Animations.Last() as FreightAnimationContinuous;
                    if (wagon.WeightLoadController == null) wagon.WeightLoadController = new MSTSNotchController(0, 1, 0.01f);
                    if ((Animations.Last() as FreightAnimationContinuous).FullAtStart)
                    {
                        if (empty)
                        {
                            empty = false;
                            FreightType = wagon.IntakePointList.Last().Type;
                            LoadedOne = Animations.Last() as FreightAnimationContinuous;
                            FreightWeight += LoadedOne.FreightWeightWhenFull;
                            LoadedOne.LoadPerCent = 100;
                        }
                        else
                        {
                            (Animations.Last() as FreightAnimationContinuous).FullAtStart = false;
                            Trace.TraceWarning("The wagon can't be full with two different materials, only first is retained");
                        }
                    }
                    ContinuousFreightAnimationsPresent = true;
                 }),
                new STFReader.TokenProcessor("freightanimstatic", ()=>
                {
                    Animations.Add(new FreightAnimationStatic(stf));
                    StaticFreightWeight += (Animations.Last() as FreightAnimationStatic).FreightWeight;
                    StaticFreightAnimationsPresent = true;
                    FullPhysicsStaticOne = Animations.Last() as FreightAnimationStatic;
                }),
/*                new STFReader.TokenProcessor("freightanimdiscrete", ()=>
                {
                    ORTSFreightAnims.Add(new FreightAnimDiscrete(stf));
                    if ((ORTSFreightAnims.Last() as FreightAnimDiscrete).LoadedAtStart)
                    {
                        if (empty)
                        {
                            empty = false;
                            DiscreteLoadedOne = ORTSFreightAnims.Last() as FreightAnimDiscrete;
                            FreightWeight += DiscreteLoadedOne.LoadWeight;
                        }
                        else
                        {
                            (ORTSFreightAnims.Last() as FreightAnimContinuous).FullAtStart = false;
                            Trace.TraceWarning("The wagon can't be full with two different materials, only first is retained");
                        }
                    }
                }),*/
            });
        }

        /// <summary>
        /// Saves the general variable parameters
        /// Called from within the MSTSWagon class.
        /// </summary>
        public void Save(BinaryWriter outf)
        {
            outf.Write(FreightWeight);
            outf.Write((int)FreightType);
            outf.Write(StaticFreightWeight);
        }

        /// <summary>
        /// Restores the general variable parameters
        /// Called from within the MSTSWagon class.
        /// </summary>
        public void Restore(BinaryReader inf)
        {
            FreightWeight = inf.ReadSingle();
            var fType = inf.ReadInt32();
            FreightType = (MSTSWagon.PickupType)fType;
            LoadedOne = null;
            foreach (var freightAnim in Animations)
            {
                if (freightAnim is FreightAnimationContinuous)
                {
                    if ((freightAnim as FreightAnimationContinuous).LinkedIntakePoint !=  null )
                    {
                        if ((freightAnim as FreightAnimationContinuous).LinkedIntakePoint.Type == FreightType)
                        {
                            LoadedOne = freightAnim as FreightAnimationContinuous;
                            LoadedOne.LoadPerCent = FreightWeight / LoadedOne.FreightWeightWhenFull * 100;
                        }
                        else
                        {
                            (freightAnim as FreightAnimationContinuous).LoadPerCent = 0;
                        }
                    }
                }
            }
            StaticFreightWeight = inf.ReadSingle();
        }

        public FreightAnimations(FreightAnimations copyFACollection, MSTSWagon wagon)
        {

            foreach (FreightAnimation freightAnim in copyFACollection.Animations)
            {
                if (freightAnim is FreightAnimationContinuous)
                {
                    Animations.Add(new FreightAnimationContinuous(freightAnim as FreightAnimationContinuous, wagon));
                    if ((Animations.Last() as FreightAnimationContinuous).FullAtStart) LoadedOne = Animations.Last() as FreightAnimationContinuous;
                    
                }
                else if (freightAnim is FreightAnimationStatic)
                {
                    Animations.Add(new FreightAnimationStatic(freightAnim as FreightAnimationStatic));
                }
                else if (freightAnim is FreightAnimationDiscrete)
                {
                    Animations.Add(new FreightAnimationDiscrete(freightAnim as FreightAnimationDiscrete));
                }
            }
            FreightWeight = copyFACollection.FreightWeight;
            FreightType = copyFACollection.FreightType;
            MSTSFreightAnimEnabled = copyFACollection.MSTSFreightAnimEnabled;
            WagonEmptyWeight = copyFACollection.WagonEmptyWeight;
            LoadingStartDelay = copyFACollection.LoadingStartDelay;
            UnloadingStartDelay = copyFACollection.UnloadingStartDelay;
            IsGondola = copyFACollection.IsGondola;

            // additions to manage consequences of variable weight on friction and brake forces
            EmptyORTSDavis_A = copyFACollection.EmptyORTSDavis_A;
            EmptyORTSDavis_B = copyFACollection.EmptyORTSDavis_B;
            EmptyORTSDavis_C = copyFACollection.EmptyORTSDavis_C;
            EmptyORTSWagonFrontalAreaM2 = copyFACollection.EmptyORTSWagonFrontalAreaM2;
            EmptyORTSDavisDragConstant = copyFACollection.EmptyORTSDavisDragConstant;
            EmptyMaxBrakeForceN = copyFACollection.EmptyMaxBrakeForceN;
            EmptyMaxHandbrakeForceN = copyFACollection.EmptyMaxHandbrakeForceN;
            EmptyCentreOfGravityM_Y = copyFACollection.EmptyCentreOfGravityM_Y;
            ContinuousFreightAnimationsPresent = copyFACollection.ContinuousFreightAnimationsPresent;
            StaticFreightAnimationsPresent = copyFACollection.StaticFreightAnimationsPresent;
        }
    }

    /// <summary>
    /// The 3 types of freightanims are inherited from the abstract FreightAnimation class.
    /// </summary>
    public abstract class FreightAnimation
    {
        public string ShapeFileName;
    }

    public class FreightAnimationContinuous : FreightAnimation
    {
        public bool TriggerOnStop;  // Value assumed if property not found.
        public float MaxHeight = 0;
        public float MinHeight = 0;
        public float FreightWeightWhenFull;
        public bool FullAtStart = false;
        public float LoadPerCent = 0;
        public IntakePoint LinkedIntakePoint = null;

        // additions to manage consequences of variable weight on friction and brake forces
        public float FullORTSDavis_A = -9999;
        public float FullORTSDavis_B = -9999;
        public float FullORTSDavis_C = -9999;
        public float FullORTSWagonFrontalAreaM2 = -9999;
        public float FullORTSDavisDragConstant = -9999;
        public float FullMaxBrakeForceN = -9999;
        public float FullMaxHandbrakeForceN = -9999;
        public float FullCentreOfGravityM_Y = -9999; // get centre of gravity after adjusted for freight animation

        public FreightAnimationContinuous(STFReader stf, MSTSWagon wagon)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("intakepoint", ()=>
                {
                    wagon.IntakePointList.Add(new IntakePoint(stf));
                    wagon.IntakePointList.Last().LinkedFreightAnim = this;
                    LinkedIntakePoint = wagon.IntakePointList.Last();
                }),
                new STFReader.TokenProcessor("shape", ()=>{ ShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("maxheight", ()=>{ MaxHeight = stf.ReadFloatBlock(STFReader.UNITS.Distance, 0); }),
                new STFReader.TokenProcessor("minheight", ()=>{ MinHeight = stf.ReadFloatBlock(STFReader.UNITS.Distance, 0); }),
                new STFReader.TokenProcessor("freightweightwhenfull", ()=>{ FreightWeightWhenFull = stf.ReadFloatBlock(STFReader.UNITS.Mass, 0); }),
                new STFReader.TokenProcessor("fullatstart", ()=>{ FullAtStart = stf.ReadBoolBlock(true);}),

                // additions to manage consequences of variable weight on friction and brake forces
                new STFReader.TokenProcessor("fullortsdavis_a", ()=>{ FullORTSDavis_A = stf.ReadFloatBlock(STFReader.UNITS.Force, -1); }),
                new STFReader.TokenProcessor("fullortsdavis_b", ()=>{ FullORTSDavis_B = stf.ReadFloatBlock(STFReader.UNITS.Resistance, -1); }),
                new STFReader.TokenProcessor("fullortsdavis_c", ()=>{ FullORTSDavis_C = stf.ReadFloatBlock(STFReader.UNITS.ResistanceDavisC, -1); }),
                new STFReader.TokenProcessor("fullortswagonfrontalarea", ()=>{ FullORTSWagonFrontalAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, -1); }),
                new STFReader.TokenProcessor("fullortsdavisdragconstant", ()=>{ FullORTSDavisDragConstant = stf.ReadFloatBlock(STFReader.UNITS.Any, -1); }),
                new STFReader.TokenProcessor("fullmaxbrakeforce", ()=>{ FullMaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, -1); }),
                new STFReader.TokenProcessor("fullmaxhandbrakeforce", ()=>{ FullMaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, -1); }),
                new STFReader.TokenProcessor("fullcentreofgravity_y", ()=>{ FullCentreOfGravityM_Y = stf.ReadFloatBlock(STFReader.UNITS.Distance, -1); })
            });
        }

        // For copy
        public FreightAnimationContinuous(FreightAnimationContinuous freightAnimContin, MSTSWagon wagon)
        {
            if (freightAnimContin.LinkedIntakePoint != null)
            {
                wagon.IntakePointList.Add(new IntakePoint(freightAnimContin.LinkedIntakePoint));
                wagon.IntakePointList.Last().LinkedFreightAnim = this;
                LinkedIntakePoint = wagon.IntakePointList.Last();
            }
            ShapeFileName = freightAnimContin.ShapeFileName;
            MaxHeight = freightAnimContin.MaxHeight;
            MinHeight = freightAnimContin.MinHeight;
            FreightWeightWhenFull = freightAnimContin.FreightWeightWhenFull;
            FullAtStart = freightAnimContin.FullAtStart;
            LoadPerCent = freightAnimContin.LoadPerCent;

            // additions to manage consequences of variable weight on friction and brake forces
            FullORTSDavis_A = freightAnimContin.FullORTSDavis_A;
            FullORTSDavis_B = freightAnimContin.FullORTSDavis_B;
            FullORTSDavis_C = freightAnimContin.FullORTSDavis_C;
            FullORTSWagonFrontalAreaM2 = freightAnimContin.FullORTSWagonFrontalAreaM2;
            FullORTSDavisDragConstant = freightAnimContin.FullORTSDavisDragConstant;
            FullMaxBrakeForceN = freightAnimContin.FullMaxBrakeForceN;
            FullMaxHandbrakeForceN = freightAnimContin.FullMaxHandbrakeForceN;
            FullCentreOfGravityM_Y = freightAnimContin.FullCentreOfGravityM_Y;          
        }
    }

    public class FreightAnimationStatic : FreightAnimation
    {
        public enum Type
        {
            DEFAULT
        }
        // index of visibility flag vector
        public enum VisibleFrom
        {
            Outside,
            Cab2D,
            Cab3D
        }
        public Type SubType;
        public float XOffset = 0;
        public float YOffset = 0;
        public float ZOffset = 0;
        public float FreightWeight = 0;
        public bool Flipped = false;
        public bool Cab3DFreightAnim = false;
        public bool[] Visibility = { true, false, false };

        // additions to manage consequences of variable weight on friction and brake forces
        public float FullStaticORTSDavis_A = -9999;
        public float FullStaticORTSDavis_B = -9999;
        public float FullStaticORTSDavis_C = -9999;
        public float FullStaticORTSWagonFrontalAreaM2 = -9999;
        public float FullStaticORTSDavisDragConstant = -9999;
        public float FullStaticMaxBrakeForceN = -9999;
        public float FullStaticMaxHandbrakeForceN = -9999;
        public float FullStaticCentreOfGravityM_Y = -9999; // get centre of gravity after adjusted for freight animation

        public FreightAnimationStatic(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
            new STFReader.TokenProcessor("subtype", ()=>
            {
                var typeString = stf.ReadStringBlock(null);
                switch (typeString)
	            {
                    default:
                        SubType = FreightAnimationStatic.Type.DEFAULT;
                        break;
	            }
            }),
            new STFReader.TokenProcessor("shape", ()=>{ ShapeFileName = stf.ReadStringBlock(null); }),
            new STFReader.TokenProcessor("freightweight", ()=>{ FreightWeight = stf.ReadFloatBlock(STFReader.UNITS.Mass, 0); }),
            new STFReader.TokenProcessor("offset", ()=>{
                stf.MustMatch("(");
                XOffset = stf.ReadFloat(STFReader.UNITS.Distance, 0);
                YOffset = stf.ReadFloat(STFReader.UNITS.Distance, 0);
                ZOffset = stf.ReadFloat(STFReader.UNITS.Distance, 0);
                stf.MustMatch(")");
            }),
            new STFReader.TokenProcessor("flip", ()=>{ Flipped = stf.ReadBoolBlock(true);}),
            new STFReader.TokenProcessor("visibility", ()=>{
                for (int index = 0; index < 3; index++)
                    Visibility[index] = false;
                foreach (var visibilityPlace in stf.ReadStringBlock("").ToLower().Replace(" ", "").Split(','))
                {
                    switch (visibilityPlace)
                    {
                        case "outside":
                            Visibility[(int)VisibleFrom.Outside] = true;
                            break;
                        case "cab2d":
                            Visibility[(int)VisibleFrom.Cab2D] = true;
                            break;
                        case "cab3d":
                            Visibility[(int)VisibleFrom.Cab3D] = true;
                            break;
                        default:
                            break;
                    }
                }
            }),
            // additions to manage consequences of variable weight on friction and brake forces
            new STFReader.TokenProcessor("fullortsdavis_a", ()=>{ FullStaticORTSDavis_A = stf.ReadFloatBlock(STFReader.UNITS.Force, -1); }),
            new STFReader.TokenProcessor("fullortsdavis_b", ()=>{ FullStaticORTSDavis_B = stf.ReadFloatBlock(STFReader.UNITS.Resistance, -1); }),
            new STFReader.TokenProcessor("fullortsdavis_c", ()=>{ FullStaticORTSDavis_C = stf.ReadFloatBlock(STFReader.UNITS.ResistanceDavisC, -1); }),
            new STFReader.TokenProcessor("fullortswagonfrontalarea", ()=>{ FullStaticORTSWagonFrontalAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, -1); }),
            new STFReader.TokenProcessor("fullortsdavisdragconstant", ()=>{ FullStaticORTSDavisDragConstant = stf.ReadFloatBlock(STFReader.UNITS.Any, -1); }),
            new STFReader.TokenProcessor("fullmaxbrakeforce", ()=>{ FullStaticMaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, -1); }),
            new STFReader.TokenProcessor("fullmaxhandbrakeforce", ()=>{ FullStaticMaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, -1); }),
            new STFReader.TokenProcessor("fullcentreofgravity_y", ()=>{ FullStaticCentreOfGravityM_Y = stf.ReadFloatBlock(STFReader.UNITS.Distance, -1); })
            });
        }

        // for copy
        public FreightAnimationStatic(FreightAnimationStatic freightAnimStatic)
        {
            SubType = freightAnimStatic.SubType;
            ShapeFileName = freightAnimStatic.ShapeFileName;
            XOffset = freightAnimStatic.XOffset;
            YOffset = freightAnimStatic.YOffset;
            ZOffset = freightAnimStatic.ZOffset;
            Flipped = freightAnimStatic.Flipped;
            for (int index = 0; index < 3; index++)
                Visibility[index] = freightAnimStatic.Visibility[index];
            FreightWeight = freightAnimStatic.FreightWeight;

            // additions to manage consequences of variable weight on friction and brake forces
            FullStaticORTSDavis_A = freightAnimStatic.FullStaticORTSDavis_A;
            FullStaticORTSDavis_B = freightAnimStatic.FullStaticORTSDavis_B;
            FullStaticORTSDavis_C = freightAnimStatic.FullStaticORTSDavis_C;
            FullStaticORTSWagonFrontalAreaM2 = freightAnimStatic.FullStaticORTSWagonFrontalAreaM2;
            FullStaticORTSDavisDragConstant = freightAnimStatic.FullStaticORTSDavisDragConstant;
            FullStaticMaxBrakeForceN = freightAnimStatic.FullStaticMaxBrakeForceN;
            FullStaticMaxHandbrakeForceN = freightAnimStatic.FullStaticMaxHandbrakeForceN;
            FullStaticCentreOfGravityM_Y = freightAnimStatic.FullStaticCentreOfGravityM_Y;
        }
    }

    public class FreightAnimationDiscrete : FreightAnimation
    {
        public enum Type
        {
            DEFAULT
        }
        public Type SubType;
        public float XOffset = 0;
        public float YOffset = 0;
        public float ZOffset = 0;
        public float LoadWeight = 0;
        public bool LoadedAtStart = false;

        public FreightAnimationDiscrete(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[]
            {
                new STFReader.TokenProcessor("subtype", ()=>
                {
                        var typeString = stf.ReadStringBlock(null);
                        switch (typeString)
	                    {
                            default:
                                SubType = FreightAnimationDiscrete.Type.DEFAULT;
                                break;
	                    }
                }),
                new STFReader.TokenProcessor("shape", ()=>{ ShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("offset", ()=>{
                    stf.MustMatch("(");
                    XOffset = stf.ReadFloat(STFReader.UNITS.Distance, 0);
                    YOffset = stf.ReadFloat(STFReader.UNITS.Distance, 0);
                    ZOffset = stf.ReadFloat(STFReader.UNITS.Distance, 0);
                    stf.MustMatch(")");
                }),
                new STFReader.TokenProcessor("loadweight", ()=>{ LoadWeight = stf.ReadFloatBlock(STFReader.UNITS.Mass, 0); }),
                new STFReader.TokenProcessor("loadedatstart", ()=>{ LoadedAtStart = stf.ReadBoolBlock(true);}),
            });
        }

        // for copy
        public FreightAnimationDiscrete(FreightAnimationDiscrete freightAnimDiscrete)
        {
            SubType = freightAnimDiscrete.SubType;
            ShapeFileName = freightAnimDiscrete.ShapeFileName;
            XOffset = freightAnimDiscrete.XOffset;
            YOffset = freightAnimDiscrete.YOffset;
            ZOffset = freightAnimDiscrete.ZOffset;
            LoadWeight = freightAnimDiscrete.LoadWeight;
            LoadedAtStart = freightAnimDiscrete.LoadedAtStart;
        }
    }
}
