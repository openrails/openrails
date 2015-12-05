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
        public FreightAnimationDiscrete DiscreteLoadedOne = null;
        public float LoadingStartDelay = 0;
        public float UnloadingStartDelay = 0;
        public bool IsGondola = false;
 

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
                new STFReader.TokenProcessor("freightanimcontinuous", ()=>
                {
                    Animations.Add(new FreightAnimationContinuous(stf, wagon));
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
                 }),
                new STFReader.TokenProcessor("freightanimstatic", ()=>
                {
                    Animations.Add(new FreightAnimationStatic(stf));
                    StaticFreightWeight += (Animations.Last() as FreightAnimationStatic).FreightWeight;
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
                    if ((freightAnim as FreightAnimationContinuous).LinkedIntakePoint.Type == FreightType)
                    {
                        LoadedOne = freightAnim as FreightAnimationContinuous;
                        LoadedOne.LoadPerCent = FreightWeight/LoadedOne.FreightWeightWhenFull*100;
                    }
                    else
                    {
                        (freightAnim as FreightAnimationContinuous).LoadPerCent = 0;
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
            });
        }

        // For copy
        public FreightAnimationContinuous(FreightAnimationContinuous freightAnimContin, MSTSWagon wagon)
        {
            wagon.IntakePointList.Add(new IntakePoint(freightAnimContin.LinkedIntakePoint));
            wagon.IntakePointList.Last().LinkedFreightAnim = this;
            LinkedIntakePoint = wagon.IntakePointList.Last();
            ShapeFileName = freightAnimContin.ShapeFileName;
            MaxHeight = freightAnimContin.MaxHeight;
            MinHeight = freightAnimContin.MinHeight;
            FreightWeightWhenFull = freightAnimContin.FreightWeightWhenFull;
            FullAtStart = freightAnimContin.FullAtStart;
            LoadPerCent = freightAnimContin.LoadPerCent;
        }
    }

    public class FreightAnimationStatic : FreightAnimation
    {
        public enum Type
        {
            DEFAULT
        }
        public Type SubType;
        public float XOffset = 0;
        public float YOffset = 0;
        public float ZOffset = 0;
        public float FreightWeight = 0;
        public bool Flipped = false;

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
            FreightWeight = freightAnimStatic.FreightWeight;
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
