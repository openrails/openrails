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

using Microsoft.Xna.Framework;
using ORTS.Common;
using Orts.Parsers.Msts;
using ORTS.Viewer3D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ORTS
{
    /// <summary>
    /// An ORTSFreightAnim object is created for any engine or wagon having an 
    /// ORTSFreightAnims block in its ENG/WAG file. It contains a collection of
    /// ORTSFreightAnim objects.
    /// Called from within the MSTSWagon class.
    /// </summary>
    public class FreightAnimCollection
    {
        public List<ORTSFreightAnim> ORTSFreightAnims = new List<ORTSFreightAnim>();
        public float FreightWeight = 0;
        public float StaticFreightWeight = 0;
        public MSTSWagon.PickupType FreightType = MSTSWagon.PickupType.None;
        public bool MSTSFreightAnimEnabled = true;
        public float WagonEmptyWeight = -1;
        public FreightAnimContinuous LoadedOne = null;
        public FreightAnimDiscrete DiscreteLoadedOne = null;
        public float LoadingStartDelay = 0;
        public float UnloadingStartDelay = 0;
        public bool IsGondola = false;
 

        public FreightAnimCollection(STFReader stf, MSTSWagon wagon)
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
                    ORTSFreightAnims.Add(new FreightAnimContinuous(stf, wagon));
                    if (wagon.WeightLoadController == null) wagon.WeightLoadController = new MSTSNotchController(0, 1, 0.01f);
                    if ((ORTSFreightAnims.Last() as FreightAnimContinuous).FullAtStart)
                    {
                        if (empty)
                        {
                            empty = false;
                            FreightType = wagon.IntakePointList.Last().Type;
                            LoadedOne = ORTSFreightAnims.Last() as FreightAnimContinuous;
                            FreightWeight += LoadedOne.FreightWeightWhenFull;
                            LoadedOne.LoadPerCent = 100;
                        }
                        else
                        {
                            (ORTSFreightAnims.Last() as FreightAnimContinuous).FullAtStart = false;
                            Trace.TraceWarning("The wagon can't be full with two different materials, only first is retained");
                        }
                    }
                 }),
                new STFReader.TokenProcessor("freightanimstatic", ()=>
                {
                    ORTSFreightAnims.Add(new FreightAnimStatic(stf));
                    StaticFreightWeight += (ORTSFreightAnims.Last() as FreightAnimStatic).FreightWeight;
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
        /// Initializes the ORTSFreightAnims
        /// Called from within the MSTSWagonViewer class.
        /// </summary>
        public void CreateShapes(Viewer viewer, string wagonFolderSlash, MSTSWagon wagon)
        {
            foreach (var freightAnim in ORTSFreightAnims)
            {
                if (freightAnim.ShapeFileName != null)
                {
                    freightAnim.FreightShape = new AnimatedShape(viewer, wagonFolderSlash + freightAnim.ShapeFileName + '\0' + wagonFolderSlash, new WorldPosition(wagon.WorldPosition), ShapeFlags.ShadowCaster);
                    var thisFreightShape = freightAnim.FreightShape;
                    if (thisFreightShape.SharedShape.LodControls.Length > 0 && thisFreightShape.SharedShape.LodControls[0].DistanceLevels.Length > 0 && thisFreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length > 0 && thisFreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives.Length > 0 
                        && thisFreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy.Length > 0 )
                        thisFreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy[0] = thisFreightShape.SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy.Length;
                }
 

            }

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
            foreach (var freightAnim in ORTSFreightAnims)
            {
                if (freightAnim is FreightAnimContinuous)
                {
                    if ((freightAnim as FreightAnimContinuous).LinkedIntakePoint.Type == FreightType)
                    {
                        LoadedOne = freightAnim as FreightAnimContinuous;
                        LoadedOne.LoadPerCent = FreightWeight/LoadedOne.FreightWeightWhenFull*100;
                    }
                    else
                    {
                        (freightAnim as FreightAnimContinuous).LoadPerCent = 0;
                    }
                }
            }
            StaticFreightWeight = inf.ReadSingle();
        }

        public FreightAnimCollection(FreightAnimCollection copyFACollection, MSTSWagon wagon)
        {

            ORTSFreightAnims.Clear();

            foreach (ORTSFreightAnim freightAnim in copyFACollection.ORTSFreightAnims)
            {
                if (freightAnim is FreightAnimContinuous)
                {
                    ORTSFreightAnims.Add(new FreightAnimContinuous(freightAnim as FreightAnimContinuous, wagon));
                    if ((ORTSFreightAnims.Last() as FreightAnimContinuous).FullAtStart) LoadedOne = ORTSFreightAnims.Last() as FreightAnimContinuous;
                }
                else if (freightAnim is FreightAnimStatic)
                {
                    ORTSFreightAnims.Add(new FreightAnimStatic(freightAnim as FreightAnimStatic));
                }
                else if (freightAnim is FreightAnimDiscrete)
                {
                    ORTSFreightAnims.Add(new FreightAnimDiscrete(freightAnim as FreightAnimDiscrete));
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
    /// The 3 types of freightanims are inherited from the abstract ORTSFreightAnim class.
    /// </summary>
    public abstract class ORTSFreightAnim
    {
        public string ShapeFileName;
        public AnimatedShape FreightShape;
    }

    public class FreightAnimContinuous : ORTSFreightAnim
    {
        public bool TriggerOnStop;  // Value assumed if property not found.
        public float MaxHeight = 0;
        public float MinHeight = 0;
        public float FreightWeightWhenFull;
        public bool FullAtStart = false;
        public float LoadPerCent = 0;
        public IntakePoint LinkedIntakePoint = null;

        public FreightAnimContinuous(STFReader stf, MSTSWagon wagon)
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
        public FreightAnimContinuous(FreightAnimContinuous freightAnimContin, MSTSWagon wagon)
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

    public class FreightAnimStatic : ORTSFreightAnim
    {
        public enum FreightAnimStaticType
        {
            DEFAULT
        }
        public FreightAnimStaticType SubType;
        public float XOffset = 0;
        public float YOffset = 0;
        public float ZOffset = 0;
        public float FreightWeight = 0;

        public FreightAnimStatic(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
            new STFReader.TokenProcessor("subtype", ()=>
                {
                    var typeString = stf.ReadStringBlock(null);
                    switch (typeString)
	                {
                        default:
                            SubType = FreightAnimStatic.FreightAnimStaticType.DEFAULT;
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
            });
        }

        // for copy
        public FreightAnimStatic(FreightAnimStatic freightAnimStatic)
        {
            SubType = freightAnimStatic.SubType;
            ShapeFileName = freightAnimStatic.ShapeFileName;
            XOffset = freightAnimStatic.XOffset;
            YOffset = freightAnimStatic.YOffset;
            ZOffset = freightAnimStatic.ZOffset;
            FreightWeight = freightAnimStatic.FreightWeight;
        }
    }

    public class FreightAnimDiscrete : ORTSFreightAnim
    {
        public enum FreightAnimDiscreteType
        {
            DEFAULT
        }
        public FreightAnimDiscreteType SubType;
        public float XOffset = 0;
        public float YOffset = 0;
        public float ZOffset = 0;
        public float LoadWeight = 0;
        public bool LoadedAtStart = false;

        public FreightAnimDiscrete(STFReader stf)
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
                                SubType = FreightAnimDiscrete.FreightAnimDiscreteType.DEFAULT;
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
        public FreightAnimDiscrete(FreightAnimDiscrete freightAnimDiscrete)
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
