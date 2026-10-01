// COPYRIGHT 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Viewer3D.Common;
using ORTS.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Text.RegularExpressions;

namespace Orts.Viewer3D
{
    public class DynamicTrackViewer
    {
        public Viewer Viewer;
        public WorldPosition WorldPosition;
        public DynamicTrackPrimitive Primitive;

        public DynamicTrackViewer(Viewer viewer)
        {
            Viewer = viewer;
        }

        public DynamicTrackViewer(Viewer viewer, WorldPosition position)
        {
            Viewer = viewer;

            WorldPosition = new WorldPosition(position);

            Quaternion rotation = Quaternion.CreateFromRotationMatrix(WorldPosition.XNAMatrix);

            // Remove X and Z components of rotation to isolate for yaw angle (compass heading) only
            rotation.X = 0.0f;
            rotation.Z = 0.0f;
            // Renormalize the quaternion after deleting X and Z to get the Y component of rotation
            rotation.Normalize();

            Matrix adjusted = Matrix.CreateFromQuaternion(rotation);
            adjusted.Translation = WorldPosition.XNAMatrix.Translation;

            WorldPosition.XNAMatrix = adjusted;
        }

        /// <summary>
        /// PrepareFrame adds any object mesh in-FOV to the RenderItemCollection. 
        /// and marks the last LOD that is in-range.
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            var lodBias = ((float)Viewer.Settings.LODBias / 100 + 1);

            // Offset relative to the camera-tile origin
            int dTileX = WorldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = WorldPosition.TileZ - Viewer.Camera.TileZ;
            Vector3 tileOffsetWrtCamera = new Vector3(dTileX * 2048, 0, -dTileZ * 2048);

            // Find midpoint between track section end and track section root.
            // (Section center for straight; section chord center for arc.)
            Vector3 xnaLODCenter = 0.5f * (Primitive.XNAEnd + WorldPosition.XNAMatrix.Translation +
                                            2 * tileOffsetWrtCamera);
            Primitive.MSTSLODCenter = new Vector3(xnaLODCenter.X, xnaLODCenter.Y, -xnaLODCenter.Z);

            // Ignore any mesh not in field-of-view
            if (!Viewer.Camera.InFov(Primitive.MSTSLODCenter, Primitive.ObjectRadius)) return;

            // Scan LODs in forward order, and find first LOD in-range
            // lodIndex marks first in-range LOD
            LOD lod;
            int lodIndex;
            for (lodIndex = 0; lodIndex < Primitive.TrProfile.LODs.Count; lodIndex++)
            {
                lod = (LOD)Primitive.TrProfile.LODs[lodIndex];
                if (Viewer.Camera.InRange(Primitive.MSTSLODCenter, 0, lod.CutoffRadius * lodBias)) break;
            }
            // Ignore any mesh too far away for the furthest LOD
            if (lodIndex == Primitive.TrProfile.LODs.Count) return;

            // Initialize xnaXfmWrtCamTile to object-tile to camera-tile translation:
            Matrix xnaXfmWrtCamTile = Matrix.CreateTranslation(tileOffsetWrtCamera);
            xnaXfmWrtCamTile = WorldPosition.XNAMatrix * xnaXfmWrtCamTile; // Catenate to world transformation
            // (Transformation is now with respect to camera-tile origin)

            int lastIndex;
            // Add in-view LODs to the RenderItems collection
            if (Primitive.TrProfile.LODMethod == TrProfile.LODMethods.CompleteReplacement)
            {
                // CompleteReplacement case
                lastIndex = lodIndex; // Add only the LOD that is the first in-view
            }
            else
            {
                // ComponentAdditive case
                // Add all LODs from the smallest in-view CutOffRadius to the last
                lastIndex = Primitive.TrProfile.LODs.Count - 1;
            }
            while (lodIndex <= lastIndex)
            {
                lod = (LOD)Primitive.TrProfile.LODs[lodIndex];
                for (int j = lod.PrimIndexStart; j < lod.PrimIndexStop; j++)
                {
                    frame.AddPrimitive(Primitive.ShapePrimitives[j].Material, Primitive.ShapePrimitives[j], RenderPrimitiveGroup.World, ref xnaXfmWrtCamTile, ShapeFlags.None);
                }
                lodIndex++;
            }
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            foreach (LOD lod in Primitive.TrProfile.LODs)
                lod.Mark();
        }

        /// <summary>
        /// Returns the index of the track profile that best suits the given Viewer and TrVectorSection object,
        /// with an optional shape file path to use as reference.
        /// The result is cached in the vector section object for future use
        /// </summary>
        /// <returns>Integer index of a track profile in viewer.TRPs</returns>
        public static int GetBestTrackProfile(Viewer viewer, TrVectorSection trSection, string shapePath = "")
        {
            int trpIndex;
            if (shapePath == "" && viewer.Simulator.TSectionDat.TrackShapes.ContainsKey(trSection.ShapeIndex))
                shapePath = String.Concat(viewer.Simulator.BasePath, @"\Global\Shapes\", viewer.Simulator.TSectionDat.TrackShapes.Get(trSection.ShapeIndex).FileName);

            if (viewer.TrackProfileIndicies.ContainsKey(shapePath))
                viewer.TrackProfileIndicies.TryGetValue(shapePath, out trpIndex);
            else if (shapePath != "") // Haven't checked this track shape yet
            {
                // Need to load the shape file if not already loaded
                SharedShape trackShape = viewer.ShapeManager.Get(shapePath);
                trpIndex = GetBestTrackProfile(viewer, trackShape);
                viewer.TrackProfileIndicies.Add(shapePath, trpIndex);
            }
            else // Not enough info-use default track profile
                trpIndex = 0;

            return trpIndex;
        }
        // Note: Dynamic track objects will ALWAYS use TRPIndex = 0
        // This is because dynamic tracks don't have shapes, and as such lack the information needed
        // to select a specific track profile.

        /// <summary>
        /// Returns the index of the track profile that best suits a track shape
        /// given a reference to the viewer and the shape file path.
        /// </summary>
        /// <returns>Integer index of a track profile in viewer.TRPs</returns>
        public static int GetBestTrackProfile(Viewer viewer, string shapePath)
        {
            int trpIndex;

            if (viewer.TrackProfileIndicies.ContainsKey(shapePath))
                viewer.TrackProfileIndicies.TryGetValue(shapePath, out trpIndex);
            else if (shapePath != "")
            {
                // Need to load the shape file if not already loaded
                SharedShape trackShape = viewer.ShapeManager.Get(shapePath);
                trpIndex = GetBestTrackProfile(viewer, trackShape);
                viewer.TrackProfileIndicies.Add(shapePath, trpIndex);
            }
            else // Not enough info-use default track profile
                trpIndex = 0;

            return trpIndex;
        }

        /// <summary>
        /// Determines the index of the track profile that would be the most suitable replacement
        /// for the given shared shape object.
        /// </summary>
        public static int GetBestTrackProfile(Viewer viewer, SharedShape shape)
        {
            float score = 0.0f;
            int bestIndex = -1; // If best index -1 is returned, that means none of the track profiles are a good fit

            for (int i = 0; i < viewer.TRPs.Count; i++)
            {
                float bestScore = score;
                score = 0;
                if (viewer.TRPs[i].TrackProfile.IncludeImages == null && viewer.TRPs[i].TrackProfile.ExcludeImages == null
                    && viewer.TRPs[i].TrackProfile.IncludeShapes == null && viewer.TRPs[i].TrackProfile.ExcludeShapes == null)
                {
                    // Default behavior: Attempt to match track shape to track profile using texture names alone
                    // If shape file is missing any textures, we can't use this method
                    if (shape.ImageNames == null)
                    {
                        score = float.NegativeInfinity;
                        continue;
                    }
                    foreach (string image in viewer.TRPs[i].TrackProfile.Images)
                    {
                        if (shape.ImageNames.Contains(image, StringComparer.InvariantCultureIgnoreCase))
                            score++;
                        else // Slight bias against track profiles with extra textures defined
                            score -= 0.05f;
                    }
                    if (score > bestScore && score > 0) // Only continue checking if current profile might be the best one
                    {
                        foreach (string image in shape.ImageNames)
                        {
                            // Strong bias against track profiles that are missing textures
                            if (!viewer.TRPs[i].TrackProfile.Images.Contains(image, StringComparer.InvariantCultureIgnoreCase))
                                score -= 0.25f;
                        }
                    }
                }
                else // Manual override: Match track shape to track profile using user-defined filters
                {
                    // Check if the track shape is excluded by the track profile
                    // If it is excluded, skip processing
                    if (viewer.TRPs[i].TrackProfile.ExcludeShapes != null)
                    {
                        foreach (Regex filter in viewer.TRPs[i].TrackProfile.ExcludeShapes)
                        {
                            if (filter.IsMatch(Path.GetFileNameWithoutExtension(shape.FilePath)))
                            {
                                score = float.NegativeInfinity;
                                break;
                            }
                        }
                    }
                    if (score > float.NegativeInfinity && shape.ImageNames != null
                        && viewer.TRPs[i].TrackProfile.ExcludeImages != null)
                    {
                        foreach (Regex filter in viewer.TRPs[i].TrackProfile.ExcludeImages)
                        {
                            foreach (string image in shape.ImageNames)
                            {
                                if (filter.IsMatch(image))
                                {
                                    score = float.NegativeInfinity;
                                    break;
                                }
                            }
                        }
                    }
                    // If no exclusions are found, check for inclusions instead
                    if (score > float.NegativeInfinity)
                    {
                        // Still need to consider that this shape may need to be excluded if the track profile doesn't include its shape or textures
                        // If the track profile doesn't specify any shapes or textures to include, assume the profile can be used
                        bool shapeIncluded = false;
                        bool imageIncluded = false;
                        if (viewer.TRPs[i].TrackProfile.IncludeShapes != null)
                        {
                            foreach (Regex filter in viewer.TRPs[i].TrackProfile.IncludeShapes)
                            {
                                if (filter.IsMatch(Path.GetFileNameWithoutExtension(shape.FilePath)))
                                {
                                    shapeIncluded = true;
                                    score += 10.0f / viewer.TRPs[i].TrackProfile.IncludeShapes.Count;
                                }
                            }
                        }
                        else // No include filter set for shapes, assume this shape is included
                        {
                            shapeIncluded = true;
                            score += 5.0f;
                        }
                        if (shapeIncluded)
                        {
                            if (viewer.TRPs[i].TrackProfile.IncludeImages != null && shape.ImageNames != null)
                            {
                                foreach (Regex filter in viewer.TRPs[i].TrackProfile.IncludeImages)
                                {
                                    foreach (string image in shape.ImageNames)
                                    {
                                        if (filter.IsMatch(image))
                                        {
                                            imageIncluded = true;
                                            score += 10.0f / viewer.TRPs[i].TrackProfile.IncludeImages.Count;
                                            break;
                                        }
                                    }
                                }
                            }
                            else // No include filter set for textures, assume this shape is included
                            {
                                imageIncluded = true;
                                score += 5.0f;
                            }
                        }
                        // If the shape wasn't included or the textures weren't included, this track profile shouldn't be used
                        if (shapeIncluded == false ||  imageIncluded == false)
                            score = float.NegativeInfinity;
                    }
                }
                if (score > bestScore)
                    bestIndex = i;
                else
                    score = bestScore;
            }

            return bestIndex;
        }
    }

    // A track profile consists of a number of groups used for LOD considerations.  There are LODs,
    // Levels-Of-Detail, each of which contains subgroups.  Here, these subgroups are called "LODItems."  
    // Each group consists of one of more "polylines".  A polyline is a chain of line segments successively 
    // interconnected. A polyline of n segments is defined by n+1 "vertices."  (Use of a polyline allows 
    // for use of more than single segments.  For example, a ballast LOD could be defined as left slope, 
    // level, right slope - a single polyline of four vertices.)

    /// <summary>
    ///  Track profile file class
    /// </summary>
    public class TRPFile
    {
        public TrProfile TrackProfile; // Represents the track profile
        //public RenderProcess RenderProcess; // TODO: Pass this along in function calls

        /// <summary>
        /// Creates a List<TRPFile></TRPFile> instance from a set of track profile file(s)
        /// (XML or STF) or canned. (Precedence is XML [.XML], STF [.STF], default [canned]).
        /// </summary>
        /// <param name="viewer">Viewer.</param>
        /// <param name="routePath">Path to route.</param>
        /// <param name="trpFiles">List of TRPFile(s) created (out).</param>
        /// <returns>Bool indicating if custom track profiles were found (returns FALSE if the only profile is the default profile)</returns>
        public static bool CreateTrackProfile(Viewer viewer, string routePath, out List<TRPFile> trpFiles)
        {
            string path = routePath + @"\TrackProfiles";
            List<string> profileNames = new List<string>();
            trpFiles = new List<TRPFile>();

            if (Directory.Exists(path))
            {
                // The file called "TrProfile" should be used as the default track profile, if present
                string xmlDefault = path + @"\TrProfile.xml";
                string stfDefault = path + @"\TrProfile.stf";

                if (File.Exists(xmlDefault))
                {
                    trpFiles.Add(new TRPFile(viewer, xmlDefault));
                    profileNames.Add(Path.GetFileNameWithoutExtension(xmlDefault));
                }
                else if (File.Exists(stfDefault))
                {
                    trpFiles.Add(new TRPFile(viewer, stfDefault));
                    profileNames.Add(Path.GetFileNameWithoutExtension(stfDefault));
                }
                else // Add the canned (Kuju) track profile if no default is given
                    trpFiles.Add(new TRPFile(viewer, ""));

                // Get all .xml/.stf files that start with "TrProfile"
                string[] xmlProfiles = Directory.GetFiles(path, "TrProfile*.xml");
                string[] stfProfiles = Directory.GetFiles(path, "TrProfile*.stf");

                foreach (string xmlProfile in xmlProfiles)
                {
                    string xmlName = Path.GetFileNameWithoutExtension(xmlProfile);
                    // Don't try to add the default track profile twice
                    if (!profileNames.Contains(xmlName))
                    {
                        trpFiles.Add(new TRPFile(viewer, xmlProfile));
                        profileNames.Add(xmlName);
                    }
                }
                foreach (string stfProfile in stfProfiles)
                {
                    string stfName = Path.GetFileNameWithoutExtension(stfProfile);
                    // If an .stf profile and .xml profile have the same name, prefer the xml profile
                    if (!profileNames.Contains(stfName))
                    {
                        trpFiles.Add(new TRPFile(viewer, stfProfile));
                        profileNames.Add(stfName);
                    }
                }
            }

            // Add canned profile if no profiles were found
            if (trpFiles.Count <= 0)
            {
                trpFiles.Add(new TRPFile(viewer, ""));
                return false;
            }
            else
                return true;

            // FOR DEBUGGING: Writes XML file from current TRP
            //TRP.TrackProfile.SaveAsXML(@"C:/Users/Walt/Desktop/TrProfile.xml");
        }

        /// <summary>
        /// Create TrackProfile from a track profile file.  
        /// (Defaults on empty or nonexistent filespec.)
        /// </summary>
        /// <param name="viewer">Viewer 3D.</param>
        /// <param name="filespec">Complete filepath string to track profile file.</param>
        public TRPFile(Viewer viewer, string filespec)
        {
            if (filespec == "")
            {
                // No track profile provided, use default
                TrackProfile = new TrProfile(viewer);
                return;
            }
            FileInfo fileInfo = new FileInfo(filespec);
            if (!fileInfo.Exists)
            {
                TrackProfile = new TrProfile(viewer); // Default profile if no file
            }
            else
            {
                string fext = filespec.Substring(filespec.LastIndexOf('.')); // File extension

                switch (fext.ToUpper())
                {
                    case ".STF": // MSTS-style
                        using (STFReader stf = new STFReader(filespec, false))
                        {
                            // "EXPERIMENTAL" header is temporary
                            if (stf.SimisSignature != "SIMISA@@@@@@@@@@JINX0p0t______")
                            {
                                STFException.TraceWarning(stf, "Invalid header - file will not be processed. Using DEFAULT profile.");
                                TrackProfile = new TrProfile(viewer); // Default profile if no file
                            }
                            else
                                try
                                {
                                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                                        new STFReader.TokenProcessor("trprofile", ()=>{ TrackProfile = new TrProfile(viewer, stf); }),
                                    });
                                }
                                catch (Exception e)
                                {
                                    STFException.TraceWarning(stf, "Track profile STF constructor failed because " + e.Message + ". Using DEFAULT profile.");
                                    TrackProfile = new TrProfile(viewer); // Default profile if no file
                                }
                                finally
                                {
                                    if (TrackProfile == null)
                                    {
                                        STFException.TraceWarning(stf, "Track profile STF constructor failed. Using DEFAULT profile.");
                                        TrackProfile = new TrProfile(viewer); // Default profile if no file
                                    }
                                }
                        }
                        break;

                    case ".XML": // XML-style
                        // Convention: .xsd filename must be the same as .xml filename and in same path.
                        // Form filespec for .xsd file
                        string xsdFilespec = filespec.Substring(0, filespec.LastIndexOf('.')) + ".xsd"; // First part

                        // Specify XML settings
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.ConformanceLevel = ConformanceLevel.Auto; // Fragment, Document, or Auto
                        settings.IgnoreComments = true;
                        settings.IgnoreWhitespace = true;
                        // Settings for validation
                        settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
                        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                        settings.ValidationType = ValidationType.Schema; // Independent external file
                        try
                        {
                            settings.Schemas.Add("TrProfile.xsd", XmlReader.Create(xsdFilespec)); // Add schema from file
                        }
                        catch
                        {
                            Trace.TraceWarning("Track profile XML constructor failed, could not create XML schema " + xsdFilespec);
                            TrackProfile = new TrProfile(viewer);
                            break;
                        }

                        // Create an XML reader for the .xml file
                        using (XmlReader reader = XmlReader.Create(filespec, settings))
                        {
                            TrackProfile = new TrProfile(viewer, reader);
                        }
                        break;

                    default:
                        // File extension not supported; create a default track profile
                        TrackProfile = new TrProfile(viewer);
                        break;
                }
            }
        }

        // ValidationEventHandler callback function
        void ValidationCallback(object sender, ValidationEventArgs args)
        {
            Console.WriteLine(); // Terminate pending Write
            if (args.Severity == XmlSeverityType.Warning)
            {
                Console.WriteLine("XML VALIDATION WARNING:");
            }
            if (args.Severity == XmlSeverityType.Error)
            {
                Console.WriteLine("XML VALIDATION ERROR:");
            }
            Console.WriteLine("{0} (Line {1}, Position {2}):",
                args.Exception.SourceUri, args.Exception.LineNumber, args.Exception.LinePosition);
            Console.WriteLine(args.Message);
            Console.WriteLine("----------");
        }
    }

    // Dynamic track profile class
    public class TrProfile
    {
        public string Name; // e.g., "Default track profile"
        public int ReplicationPitch; //TBD: Replication pitch alternative
        public LODMethods LODMethod = LODMethods.None; // LOD method of control
        public float ChordSpan; // Base method: No. of profiles generated such that span is ChordSpan degrees
        // If a PitchControl is defined, then the base method is compared to the PitchControl method,
        // and the ChordSpan is adjusted to compensate.
        public PitchControls PitchControl = PitchControls.None; // Method of control for profile replication pitch
        public float PitchControlScalar; // Scalar parameter for PitchControls
        public ArrayList LODs = new ArrayList(); // Array of Levels-Of-Detail
        public List<string> Images = new List<string>();
        // Manual overrides for matching track shapes/textures to track profiles
        public List<Regex> IncludeShapes;
        public List<Regex> ExcludeShapes;
        public List<Regex> IncludeImages;
        public List<Regex> ExcludeImages;
        // The gauge of track represented by this track profile
        public float TrackGaugeM;
        /// <summary>
        /// The type of superelevation used (ie: which rail is superelevated)
        /// </summary>
        public enum SuperElevationMethod
        {
            /// <summary>
            /// No superelevation - graphical superelevation disabled
            /// </summary>
            None,

            /// <summary>
            /// Both rails are elevated; inside rail is elevated down, outside rail is elevated up
            /// </summary>
            Both,

            /// <summary>
            /// Inside rail is unchanged, outside rail is elevated up
            /// </summary>
            Outside,

            /// <summary>
            /// Inside rail is elevated down, outside rail is unchanged
            /// </summary>
            Inside,
        }
        public SuperElevationMethod ElevationType = SuperElevationMethod.Outside;

        /// <summary>
        /// Enumeration of LOD control methods
        /// </summary>
        public enum LODMethods
        {
            /// <summary>
            /// None -- No LODMethod specified; defaults to ComponentAdditive.
            /// </summary>
            None = 0,

            /// <summary>
            /// ComponentAdditive -- Each LOD is a COMPONENT that is ADDED as the camera gets closer.
            /// </summary>
            ComponentAdditive = 1,

            /// <summary>
            /// CompleteReplacement -- Each LOD group is a COMPLETE model that REPLACES another as the camera moves.
            /// </summary>
            CompleteReplacement = 2
        }

        /// <summary>
        /// Enumeration of cross section replication pitch control methods.
        /// </summary>
        public enum PitchControls
        {
            /// <summary>
            /// None -- No pitch control method specified.
            /// </summary>
            None = 0,

            /// <summary>
            /// ChordLength -- Constant length of chord.
            /// </summary>
            ChordLength,

            /// <summary>
            /// Chord Displacement -- Constant maximum displacement of chord from arc.
            /// </summary>
            ChordDisplacement
        }

        /// <summary>
        /// TrProfile constructor (default - builds from self-contained data)
        /// <param name="viewer">Viewer.</param>
        /// </summary>
        public TrProfile(Viewer viewer)
        {
            // Default TrProfile constructor

            Name = "Default Dynatrack profile";
            LODMethod = LODMethods.ComponentAdditive;
            ChordSpan = 1.0f; // Base Method: Generates profiles spanning no more than 1 degree

            PitchControl = PitchControls.ChordLength;       // Target chord length
            PitchControlScalar = 10.0f;                     // Hold to no more than 10 meters
            //PitchControl = PitchControls.ChordDisplacement; // Target chord displacement from arc
            //PitchControlScalar = 0.034f;                    // Hold to no more than 34 mm (half rail width)
            TrackGaugeM = viewer.Simulator.RouteTrackGaugeM; // Kuju track profile can be adapted to any gauge of track
            ElevationType = SuperElevationMethod.Outside; // Superelevate the outside rail only to reduce clipping

            LOD lod;            // Local LOD instance
            LODItem lodItem;    // Local LODItem instance
            Polyline pl;        // Local Polyline instance

            // RAILSIDES
            lod = new LOD(700.0f); // Create LOD for railsides with specified CutoffRadius
            lodItem = new LODItem("Railsides");
            lodItem.TexName = "acleantrack2.ace";
            lodItem.ShaderName = "TexDiff";
            lodItem.LightModelName = "OptSpecular0";
            lodItem.AlphaTestMode = 0;
            lodItem.TexAddrModeName = "Wrap";
            lodItem.ESD_Alternative_Texture = 0;
            lodItem.MipMapLevelOfDetailBias = 0;
            LODItem.LoadMaterial(viewer, lodItem);
            var inner = TrackGaugeM / 2f;
            var outer = inner + 0.15f * TrackGaugeM / 1.435f;

            pl = new Polyline(this, "left_outer", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(-outer, .200f, 0.0f, -1f, 0f, 0f, -.139362f, .101563f));
            pl.Vertices.Add(new Vertex(-outer, .325f, 0.0f, -1f, 0f, 0f, -.139363f, .003906f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            pl = new Polyline(this, "left_inner", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(-inner, .325f, 0.0f, 1f, 0f, 0f, -.139363f, .003906f));
            pl.Vertices.Add(new Vertex(-inner, .200f, 0.0f, 1f, 0f, 0f, -.139362f, .101563f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            pl = new Polyline(this, "right_inner", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(inner, .200f, 0.0f, -1f, 0f, 0f, -.139362f, .101563f));
            pl.Vertices.Add(new Vertex(inner, .325f, 0.0f, -1f, 0f, 0f, -.139363f, .003906f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            pl = new Polyline(this, "right_outer", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(outer, .325f, 0.0f, 1f, 0f, 0f, -.139363f, .003906f));
            pl.Vertices.Add(new Vertex(outer, .200f, 0.0f, 1f, 0f, 0f, -.139362f, .101563f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            lod.LODItems.Add(lodItem); // Append this LODItem to LODItems array
            LODs.Add(lod); // Append this LOD to LODs array

            // RAILTOPS
            lod = new LOD(1200.0f); // Create LOD for railtops with specified CutoffRadius
            // Single LODItem in this case
            lodItem = new LODItem("Railtops");
            lodItem.TexName = "acleantrack2.ace";
            lodItem.ShaderName = "TexDiff";
            lodItem.LightModelName = "OptSpecular25";
            lodItem.AlphaTestMode = 0;
            lodItem.TexAddrModeName = "Wrap";
            lodItem.ESD_Alternative_Texture = 0;
            lodItem.MipMapLevelOfDetailBias = 0;
            LODItem.LoadMaterial(viewer, lodItem);

            pl = new Polyline(this, "right", 2);
            pl.DeltaTexCoord = new Vector2(.0744726f, 0f);
            pl.Vertices.Add(new Vertex(-outer, .325f, 0.0f, 0f, 1f, 0f, .232067f, .126953f));
            pl.Vertices.Add(new Vertex(-inner, .325f, 0.0f, 0f, 1f, 0f, .232067f, .224609f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            pl = new Polyline(this, "left", 2);
            pl.DeltaTexCoord = new Vector2(.0744726f, 0f);
            pl.Vertices.Add(new Vertex(inner, .325f, 0.0f, 0f, 1f, 0f, .232067f, .126953f));
            pl.Vertices.Add(new Vertex(outer, .325f, 0.0f, 0f, 1f, 0f, .232067f, .224609f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            lod.LODItems.Add(lodItem); // Append this LODItem to LODItems array
            LODs.Add(lod); // Append this LOD to LODs array

            // BALLAST
            lod = new LOD(float.MaxValue); // Create LOD for ballast with specified CutoffRadius (infinite)
            // Single LODItem in this case
            lodItem = new LODItem("Ballast");
            lodItem.TexName = "acleantrack1.ace";
            lodItem.ShaderName = "BlendATexDiff";
            lodItem.LightModelName = "OptSpecular0";
            lodItem.AlphaTestMode = 0;
            lodItem.TexAddrModeName = "Wrap";
            lodItem.ESD_Alternative_Texture = (int)Helpers.TextureFlags.SnowTrack; // Match MSTS global road/track behaviour.
            lodItem.MipMapLevelOfDetailBias = -1f;
            LODItem.LoadMaterial(viewer, lodItem);

            pl = new Polyline(this, "ballast", 2);
            pl.DeltaTexCoord = new Vector2(0.0f, 0.2088545f);
            pl.Vertices.Add(new Vertex(-2.5f * TrackGaugeM / 1.435f, 0.2f, 0.0f, 0f, 1f, 0f, -.153916f, -.280582f));
            pl.Vertices.Add(new Vertex(2.5f * TrackGaugeM / 1.435f, 0.2f, 0.0f, 0f, 1f, 0f, .862105f, -.280582f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            lod.LODItems.Add(lodItem); // Append this LODItem to LODItems array
            LODs.Add(lod); // Append this LOD to LODs array

            // Add textures
            foreach (LOD level in LODs)
            {
                foreach (LODItem item in level.LODItems)
                {
                    string texFileName = Path.GetFileNameWithoutExtension(item.TexName);
                    if (!Images.Contains(texFileName))
                        Images.Add(texFileName);
                }
            }
        }

        /// <summary>
        /// TrProfile constructor from STFReader-style profile file
        /// </summary>
        public TrProfile(Viewer viewer, STFReader stf)
        {
            Name = "Default Dynatrack profile";

            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("lodmethod", ()=> { LODMethod = GetLODMethod(stf.ReadStringBlock(null)); }),
                new STFReader.TokenProcessor("chordspan", ()=>{ ChordSpan = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("pitchcontrol", ()=> { PitchControl = GetPitchControl(stf.ReadStringBlock(null)); }),
                new STFReader.TokenProcessor("pitchcontrolscalar", ()=>{ PitchControlScalar = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("includedshapes", ()=>{ IncludeShapes = ConvertToRegex(stf.ReadStringBlock(null)); }),
                new STFReader.TokenProcessor("excludedshapes", ()=>{ ExcludeShapes = ConvertToRegex(stf.ReadStringBlock(null)); }),
                new STFReader.TokenProcessor("includedtextures", ()=>{ IncludeImages = ConvertToRegex(stf.ReadStringBlock(null)); }),
                new STFReader.TokenProcessor("excludedtextures", ()=>{ ExcludeImages = ConvertToRegex(stf.ReadStringBlock(null)); }),
                new STFReader.TokenProcessor("trackgauge", ()=>{ TrackGaugeM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("superelevationmethod", ()=> { ElevationType = GetElevMethod(stf.ReadStringBlock(null)); }),
                new STFReader.TokenProcessor("lod", ()=> { LODs.Add(new LOD(viewer, stf)); }),
            });

            if (LODs.Count == 0)
                throw new Exception("missing LODs");
            else // Add textures
            {
                foreach (LOD level in LODs)
                {
                    foreach (LODItem item in level.LODItems)
                    {
                        string texFileName = Path.GetFileNameWithoutExtension(item.TexName);
                        if (!Images.Contains(texFileName))
                            Images.Add(texFileName);
                    }
                }
            }

            if (TrackGaugeM <= 0)
                TrackGaugeM = viewer.Simulator.RouteTrackGaugeM;
        }

        /// <summary>
        /// TrProfile constructor from XML profile file
        /// </summary>
        public TrProfile(Viewer viewer, XmlReader reader)
        {
            if (reader.IsStartElement())
            {
                if (reader.Name == "TrProfile")
                {
                    // root
                    Name = reader.GetAttribute("Name");
                    LODMethod = GetLODMethod(reader.GetAttribute("LODMethod"));
                    ChordSpan = float.Parse(reader.GetAttribute("ChordSpan"));
                    PitchControl = GetPitchControl(reader.GetAttribute("PitchControl"));
                    PitchControlScalar = float.Parse(reader.GetAttribute("PitchControlScalar"));
                    IncludeShapes = ConvertToRegex(reader.GetAttribute("IncludedShapes"));
                    ExcludeShapes = ConvertToRegex(reader.GetAttribute("ExcludedShapes"));
                    IncludeImages = ConvertToRegex(reader.GetAttribute("IncludedTextures"));
                    ExcludeImages = ConvertToRegex(reader.GetAttribute("ExcludedTextures"));
                    TrackGaugeM = float.Parse(reader.GetAttribute("TrackGauge"));
                    ElevationType = GetElevMethod(reader.GetAttribute("SuperElevationMethod"));
                }
                else
                {
                    //TODO: Need to handle ill-formed XML profile
                }
            }
            LOD lod = null;
            LODItem lodItem = null;
            Polyline pl = null;
            Vertex v;
            string[] s;
            char[] sep = new char[] { ' ' };
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case "LOD":
                            lod = new LOD(float.Parse(reader.GetAttribute("CutoffRadius")));
                            LODs.Add(lod);
                            break;
                        case "LODItem":
                            lodItem = new LODItem(reader.GetAttribute("Name"));
                            lodItem.TexName = reader.GetAttribute("TexName");

                            lodItem.ShaderName = reader.GetAttribute("ShaderName");
                            lodItem.LightModelName = reader.GetAttribute("LightModelName");
                            lodItem.AlphaTestMode = int.Parse(reader.GetAttribute("AlphaTestMode"));
                            lodItem.TexAddrModeName = reader.GetAttribute("TexAddrModeName");
                            lodItem.ESD_Alternative_Texture = int.Parse(reader.GetAttribute("ESD_Alternative_Texture"));
                            lodItem.MipMapLevelOfDetailBias = float.Parse(reader.GetAttribute("MipMapLevelOfDetailBias"));

                            LODItem.LoadMaterial(viewer, lodItem);
                            lod.LODItems.Add(lodItem);
                            break;
                        case "Polyline":
                            pl = new Polyline();
                            pl.Name = reader.GetAttribute("Name");
                            s = reader.GetAttribute("DeltaTexCoord").Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            pl.DeltaTexCoord = new Vector2(float.Parse(s[0]), float.Parse(s[1]));
                            lodItem.Polylines.Add(pl);
                            break;
                        case "Vertex":
                            v = new Vertex();
                            s = reader.GetAttribute("Position").Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            v.Position = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                            s = reader.GetAttribute("Normal").Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            v.Normal = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                            s = reader.GetAttribute("TexCoord").Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            v.TexCoord = new Vector2(float.Parse(s[0]), float.Parse(s[1]));
                            v.PositionControl = Vertex.GetPositionControl(reader.GetAttribute("PositionControl"));
                            pl.Vertices.Add(v);
                            lodItem.NumVertices++; // Bump vertex count
                            if (pl.Vertices.Count > 1) lodItem.NumSegments++;
                            break;
                        default:
                            break;
                    }
                }
            }
            if (LODs.Count == 0)
                throw new Exception("missing LODs");
            else // Add textures
            {
                foreach (LOD level in LODs)
                {
                    foreach (LODItem item in level.LODItems)
                    {
                        string texFileName = Path.GetFileNameWithoutExtension(item.TexName);
                        if (!Images.Contains(texFileName))
                            Images.Add(texFileName);
                    }
                }
            }

            if (TrackGaugeM <= 0)
                TrackGaugeM = viewer.Simulator.RouteTrackGaugeM;
        }

        /// <summary>
        /// TrProfile constructor (default - builds from self-contained data)
        /// <param name="viewer">Viewer3D.</param>
        /// <param name="x">Parameter x is a placeholder.</param>
        /// </summary>
        public TrProfile(Viewer viewer, int x)
        {
            // Default TrProfile constructor
            Name = "Default Dynatrack profile";
        }

        /// <summary>
        /// Gets a member of the LODMethods enumeration that corresponds to sLODMethod.
        /// </summary>
        /// <param name="sLODMethod">String that identifies desired LODMethod.</param>
        /// <returns>LODMethod</returns>
        public static LODMethods GetLODMethod(string sLODMethod)
        {
            string s = sLODMethod.ToLower();
            switch (s)
            {
                case "none":
                    return LODMethods.None;

                case "completereplacement":
                    return LODMethods.CompleteReplacement;

                case "componentadditive":
                default:
                    return LODMethods.ComponentAdditive;
            }
        }

        /// <summary>
        /// Gets a member of the SuperElevationMethod enumeration that corresponds to sElevMethod.
        /// </summary>
        /// <param name="sElevMethod">String that identifies desired SuperElevationMethod.</param>
        /// <returns>SuperElevationMethod</returns>
        public static SuperElevationMethod GetElevMethod(string sElevMethod)
        {
            string s = sElevMethod.ToLower();
            switch (s)
            {
                case "none":
                    return SuperElevationMethod.None;
                case "both":
                    return SuperElevationMethod.Both;
                case "inside":
                    return SuperElevationMethod.Inside;
                case "outside":
                default:
                    return SuperElevationMethod.Outside;
            }
        }

        /// <summary>
        /// Gets a member of the PitchControls enumeration that corresponds to sPitchControl.
        /// </summary>
        /// <param name="sPitchControl">String that identifies desired PitchControl.</param>
        /// <returns></returns>
        public static PitchControls GetPitchControl(string sPitchControl)
        {
            string s = sPitchControl.ToLower();
            switch (s)
            {
                case "chordlength":
                    return PitchControls.ChordLength;

                case "chorddisplacement":
                    return PitchControls.ChordDisplacement;

                case "none":
                default:
                    return PitchControls.None; ;

            }
        }

        /// <summary>
        /// Marks the generic track profile, so that its textures never get deleted
        /// </summary>
        [CallOnThread("Loader")]
        public void Mark()
        {
            foreach (LOD lod in LODs)
                lod.Mark();
        }

        /// <summary>
        /// Converts given string into a list of regular expression objects by splitting
        /// the string at each comma to get individual filter substrings, then replacing
        /// any wildcards * or ? with their regex equivalent.
        /// </summary>
        public static List<Regex> ConvertToRegex(string filters)
        {
            List<Regex> regexFilters = null;

            if (filters != null)
            {
                // Split the string of filters into an array of individual filters
                string[] filterList = filters.Replace(" ", "").Replace("\"", "").Split(',');

                regexFilters = new List<Regex>();

                // Convert filters to regular expressions that will use * and ? as wildcards. Case is to be ignored in this instance.
                for (int i = 0; i < filterList.Length; i++)
                    regexFilters.Add(new Regex(string.Concat("^", Regex.Escape(filterList[i]).Replace("\\?", ".").Replace("\\*", ".*"), "$"), RegexOptions.IgnoreCase));
            }

            return regexFilters;
        }
    }

    public class LOD
    {
        public float CutoffRadius; // Distance beyond which LODItem is not seen
        public ArrayList LODItems = new ArrayList(); // Array of arrays of LODItems
        public int PrimIndexStart; // Start index of ShapePrimitive block for this LOD
        public int PrimIndexStop;

        /// <summary>
        /// LOD class constructor
        /// </summary>
        /// <param name="cutoffRadius">Distance beyond which LODItem is not seen</param>
        public LOD(float cutoffRadius)
        {
            CutoffRadius = cutoffRadius;
        }

        public LOD(Viewer viewer, STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("cutoffradius", ()=>{ CutoffRadius = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("loditem", ()=>{
                    LODItem lodItem = new LODItem(viewer, stf);
                    LODItems.Add(lodItem); // Append to Polylines array
                    }),
            });
            if (CutoffRadius == 0) throw new Exception("missing CutoffRadius");
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            foreach (LODItem lodItem in LODItems)
                lodItem.Mark();
        }
    }

    public class LODItem
    {
        public ArrayList Polylines = new ArrayList();  // Array of arrays of vertices 

        public string Name;                            // e.g., "Rail sides"
        public string ShaderName;
        public string LightModelName;
        public int AlphaTestMode;
        public string TexAddrModeName;
        public int ESD_Alternative_Texture; // Equivalent to that of .sd file
        public float MipMapLevelOfDetailBias;

        public string TexName; // Texture file name

        public Material LODMaterial; // SceneryMaterial reference

        // NumVertices and NumSegments used for sizing vertex and index buffers
        public uint NumVertices;                     // Total independent vertices in LOD
        public uint NumSegments;                     // Total line segment count in LOD

        /// <summary>
        /// LODITem constructor (used for default and XML-style profiles)
        /// </summary>
        public LODItem(string name)
        {
            Name = name;
        }

        /// <summary>
        /// LODITem constructor (used for STF-style profile)
        /// </summary>
        public LODItem(Viewer viewer, STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("texname", ()=>{ TexName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("shadername", ()=>{ ShaderName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("lightmodelname", ()=>{ LightModelName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("alphatestmode", ()=>{ AlphaTestMode = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("texaddrmodename", ()=>{ TexAddrModeName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("esd_alternative_texture", ()=>{ ESD_Alternative_Texture = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("mipmaplevelofdetailbias", ()=>{ MipMapLevelOfDetailBias = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("polyline", ()=>{
                    Polyline pl = new Polyline(stf);
                    Polylines.Add(pl); // Append to Polylines array
                    //parent.Accum(pl.Vertices.Count); }),
                    Accum(pl.Vertices.Count); }),
            });

            // Checks for required member variables:
            // Name not required.
            // MipMapLevelOfDetail bias initializes to 0.
            if (Polylines.Count == 0) throw new Exception("missing Polylines");

            LoadMaterial(viewer, this);
        }

        public void Accum(int count)
        {
            // Accumulates total independent vertices and total line segments
            // Used for sizing of vertex and index buffers
            NumVertices += (uint)count;
            NumSegments += (uint)count - 1;
        }

        public static void LoadMaterial(Viewer viewer, LODItem lod)
        {
            var options = Helpers.EncodeMaterialOptions(lod);
            lod.LODMaterial = viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(viewer.Simulator, (Helpers.TextureFlags)lod.ESD_Alternative_Texture, lod.TexName), (int)options, lod.MipMapLevelOfDetailBias);
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            LODMaterial.Mark();
        }
    }

    public class Polyline
    {
        public ArrayList Vertices = new ArrayList();    // Array of vertices 

        public string Name;                             // e.g., "1:1 embankment"
        public Vector2 DeltaTexCoord;                   // Incremental change in (u, v) from one cross section to the next

        /// <summary>
        /// Polyline constructor (DAT)
        /// </summary>
        public Polyline(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("vertex", ()=>{ Vertices.Add(new Vertex(stf)); }),
                new STFReader.TokenProcessor("deltatexcoord", ()=>{
                    stf.MustMatch("(");
                    DeltaTexCoord.X = stf.ReadFloat(STFReader.UNITS.None, null);
                    DeltaTexCoord.Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
            // Checks for required member variables: 
            // Name not required.
            if (DeltaTexCoord == Vector2.Zero) throw new Exception("missing DeltaTexCoord");
            if (Vertices.Count == 0) throw new Exception("missing Vertices");
        }

        /// <summary>
        /// Bare-bones Polyline constructor (used for XML)
        /// </summary>
        public Polyline()
        {
        }

        /// <summary>
        /// Polyline constructor (default)
        /// </summary>
        public Polyline(TrProfile parent, string name, uint num)
        {
            Name = name;
        }
    }

    public struct Vertex
    {
        public Vector3 Position;                           // Position vector (x, y, z)
        public Vector3 Normal;                             // Normal vector (nx, ny, nz)
        public Vector2 TexCoord;                           // Texture coordinate (u, v)

        /// <summary>
        /// Enumeration of controls for vertex displacement
        /// </summary>
        public enum VertexPositionControl
        {
            /// <summary>
            /// None -- The vertex position won't be adjusted by superelevation.
            /// </summary>
            None = 0,

            /// <summary>
            /// All -- The vertex position will be adjusted by superelevation.
            /// </summary>
            All,

            /// <summary>
            /// Inside -- The vertex position will only be adjusted when on the inside of the curve.
            /// </summary>
            Inside,

            /// <summary>
            /// Outside -- The vertex position will only be adjusted when on the outside of the curve.
            /// </summary>
            Outside
        }

        public VertexPositionControl PositionControl;

        // Vertex constructor (default)
        public Vertex(float x, float y, float z, float nx, float ny, float nz, float u, float v,
            VertexPositionControl control = VertexPositionControl.All)
        {
            Position = new Vector3(x, y, z);
            Normal = new Vector3(nx, ny, nz);
            TexCoord = new Vector2(u, v);
            PositionControl = control;
        }

        // Vertex constructor (DAT)
        public Vertex(STFReader stf)
        {
            Vertex v = new Vertex
            {
                Position = new Vector3(),
                Normal = new Vector3(),
                TexCoord = new Vector2(),
                PositionControl = VertexPositionControl.All
            }; // Temp variable used to construct the struct in ParseBlock
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("position", ()=>{
                    stf.MustMatch("(");
                    v.Position.X = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.Position.Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.Position.Z = 0.0f;
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("normal", ()=>{
                    stf.MustMatch("(");
                    v.Normal.X = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.Normal.Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.Normal.Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("texcoord", ()=>{
                    stf.MustMatch("(");
                    v.TexCoord.X = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.TexCoord.Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("positioncontrol", ()=>{
                    v.PositionControl = GetPositionControl(stf.ReadStringBlock(null));
                }),
            });
            this = v;
            // Checks for required member variables
            // No way to check for missing Position.
            if (Normal == Vector3.Zero) throw new Exception("improper Normal");
            // No way to check for missing TexCoord
        }

        /// <summary>
        /// Gets a member of the VertexPositionControl enumeration that corresponds to sPositionControl.
        /// </summary>
        /// <param name="sPositionControl">String that identifies desired PositionControl.</param>
        /// <returns></returns>
        public static VertexPositionControl GetPositionControl(string sPositionControl)
        {
            string s = sPositionControl.ToLower();
            switch (s)
            {
                case "none":
                    return VertexPositionControl.None;

                case "inside":
                    return VertexPositionControl.Inside;

                case "outside":
                    return VertexPositionControl.Outside;

                case "all":
                default:
                    return VertexPositionControl.All;

            }
        }
    }

    public class DynamicTrackPrimitive : ShapePrimitive //RenderPrimitive
    {
        public ShapePrimitive[] ShapePrimitives; // Array of ShapePrimitives

        public VertexPositionNormalTexture[] VertexList; // Array of vertices
        public short[] TriangleListIndices;// Array of indices to vertices for triangles
        public uint VertexIndex;           // Index of current position in VertexList
        public uint IndexIndex;            // Index of current position in TriangleListIndices
        public int NumVertices;            // Number of vertices in the track profile
        public short NumIndices;           // Number of triangle indices

        // LOD member variables:
        //public int FirstIndex;       // Marks first LOD that is in-range
        public Vector3 XNAEnd;      // Location of termination-of-section (as opposed to root)
        public float ObjectRadius;  // Radius of bounding sphere
        public Vector3 MSTSLODCenter; // Center of bounding sphere

        // Geometry member variables:
        public int NumSections;            // Number of cross sections needed to make up a track section.
        public float SegmentLength;        // meters if straight; radians if circular arc
        public int Offset;                 // Counter to indicate which cross section is currently in processing
        public Vector3 DDY;                // Elevation (y) change from one cross section to next
        public Vector3 OldV;               // Deviation from centerline for previous cross section
        public Vector3 OldRadius;          // Radius vector to centerline for previous cross section
        public Matrix Orientation;         // Rotation matrix giving the orientation of the section in local coordinates

        //TODO: Candidates for re-packaging:
        public Matrix SectionRotation;     // Rotates previous profile into next profile position on curve.
        public Vector3 Center;             // Center coordinates of curve radius
        public Vector3 Radius;             // Radius vector to cross section on curve centerline

        // This structure holds the basic geometric parameters of a DT section.
        public struct DtrackData
        {
            public int IsCurved;    // Straight (0) or circular arc (1)
            public float param1;    // Length in meters (straight) or radians (circular arc)
            public float param2;    // Radius for circular arc
            public float deltaY;    // Change in elevation (y) from beginning to end of section
        }
        public DtrackData DTrackData;      // Was: DtrackData[] dtrackData;

        public TrProfile TrProfile;

        /// <summary>
        /// Default constructor
        /// </summary>
        public DynamicTrackPrimitive()
        {
        }

        /// <summary>
        /// Generates the ShapePrimitives for this dynamic track section, must
        /// be called in order for any graphics to be rendered.
        /// </summary>
        public void PreparePrimitives(Viewer viewer)
        {
            // Count all of the LODItems in all the LODs
            int count = 0;
            for (int i = 0; i < TrProfile.LODs.Count; i++)
            {
                LOD lod = (LOD)TrProfile.LODs[i];
                count += lod.LODItems.Count;
            }
            // Allocate ShapePrimitives array for the LOD count
            ShapePrimitives = new ShapePrimitive[count];

            // Build the meshes for all the LODs, filling the vertex and triangle index buffers.
            int primIndex = 0;
            for (int iLOD = 0; iLOD < TrProfile.LODs.Count; iLOD++)
            {
                LOD lod = (LOD)TrProfile.LODs[iLOD];
                lod.PrimIndexStart = primIndex; // Store start index for this LOD
                for (int iLODItem = 0; iLODItem < lod.LODItems.Count; iLODItem++)
                {
                    // Build vertexList and triangleListIndices
                    ShapePrimitives[primIndex] = BuildPrimitive(viewer, iLOD, iLODItem);
                    primIndex++;
                }
                lod.PrimIndexStop = primIndex; // 1 above last index for this LOD
            }
        }

        public override void Mark()
        {
            foreach (var prim in ShapePrimitives)
                prim.Mark();
            base.Mark();
        }

        /// <summary>
        /// Builds a Dynatrack LOD to TrProfile specifications as one vertex buffer and one index buffer.
        /// The order in which the buffers are built reflects the nesting in the TrProfile.  The nesting order is:
        /// (Polylines (Vertices)).  All vertices and indices are built contiguously for an LOD.
        /// </summary>
        /// <param name="viewer">Viewer.</param>
        /// <param name="iLOD">Index of LOD mesh to be generated from profile.</param>
        /// <param name="iLODItem">Index of LOD mesh to be generated from profile.</param>
        public virtual ShapePrimitive BuildPrimitive(Viewer viewer, int iLOD, int iLODItem)
        {
            // Call for track section to initialize itself
            if (DTrackData.IsCurved == 0)
                LinearGen();
            else
                CircArcGen();

            // Count vertices and indices
            LOD lod = (LOD)TrProfile.LODs[iLOD];
            LODItem lodItem = (LODItem)lod.LODItems[iLODItem];
            NumVertices = (int)(lodItem.NumVertices * (NumSections + 1));
            NumIndices = (short)(lodItem.NumSegments * NumSections * 6);
            // (Cells x 2 triangles/cell x 3 indices/triangle)

            // stride is the number of vertices per section, used to connect equivalent verticies between segments
            uint stride = lodItem.NumVertices;

            // Allocate memory for vertices and indices
            VertexList = new VertexPositionNormalTexture[NumVertices]; // numVertices is now aggregate
            TriangleListIndices = new short[NumIndices]; // as is NumIndices

            // Build the mesh for lod
            VertexIndex = 0;
            IndexIndex = 0;
            Offset = 0;

            for (uint i = 0; i <= NumSections; i++)
            {
                Matrix displacement;
                float totLength;

                if (DTrackData.IsCurved == 0)
                    displacement = LinearGen(out totLength);
                else
                    displacement = CircArcGen(out totLength);

                foreach (Polyline pl in lodItem.Polylines)
                {
                    uint plv = 0; // Polyline vertex index
                    foreach (Vertex v in pl.Vertices)
                    {
                        // Generate vertex positions
                        Vector3 p = v.Position;

                        // In some extreme cases, track may have been rotated so much it's upside down
                        // Rotate 180 degrees to restore right side up
                        if (displacement.Up.Y < 0.0f)
                            p = Vector3.Transform(p, Matrix.CreateRotationZ((float)Math.PI));

                        // Move vertex to proper location in 3D space
                        VertexList[VertexIndex].Position = Vector3.Transform(p, displacement);
                        VertexList[VertexIndex].TextureCoordinate = v.TexCoord + pl.DeltaTexCoord * totLength;
                        VertexList[VertexIndex].Normal = v.Normal;

                        if (plv > 0 && VertexIndex > stride)
                        {
                            // Sense for triangles is clockwise
                            // First triangle:
                            TriangleListIndices[IndexIndex++] = (short)VertexIndex;
                            TriangleListIndices[IndexIndex++] = (short)(VertexIndex - 1 - stride);
                            TriangleListIndices[IndexIndex++] = (short)(VertexIndex - 1);
                            // Second triangle:
                            TriangleListIndices[IndexIndex++] = (short)VertexIndex;
                            TriangleListIndices[IndexIndex++] = (short)(VertexIndex - stride);
                            TriangleListIndices[IndexIndex++] = (short)(VertexIndex - 1 - stride);
                        }
                        VertexIndex++;
                        plv++;
                    }
                }
                Offset++;
            }

            // Create and populate a new ShapePrimitive
            var indexBuffer = new IndexBuffer(viewer.GraphicsDevice, typeof(short), NumIndices, BufferUsage.WriteOnly);
            indexBuffer.SetData(TriangleListIndices);
            return new ShapePrimitive(lodItem.LODMaterial, new SharedShape.VertexBufferSet(VertexList, viewer.GraphicsDevice), indexBuffer, NumIndices / 3, new[] { -1 }, 0);
        }

        /// <summary>
        /// Initializes member variables for straight track sections.
        /// </summary>
        public virtual void LinearGen()
        {
            // Define the number of track cross sections in addition to the base.
            // Straight sections can be generated as a single section, no benefits to more sections
            NumSections = 1;

            SegmentLength = DTrackData.param1 / NumSections; // Length of each mesh segment (meters)
        }

        /// <summary>
        /// Initializes member variables for circular arc track sections.
        /// </summary>
        public virtual void CircArcGen()
        {
            // Define the number of track cross sections in addition to the base.
            NumSections = (int)(Math.Abs(MathHelper.ToDegrees(DTrackData.param1)) / TrProfile.ChordSpan);
            // Very small radius track, use minimum of two sections
            if (NumSections == 0)
                NumSections = 2;

            // Use pitch control methods
            switch (TrProfile.PitchControl)
            {
                case TrProfile.PitchControls.None:
                    break; // Good enough
                case TrProfile.PitchControls.ChordLength:
                    // Calculate chord length for NumSections
                    float l = 2.0f * DTrackData.param2 * (float)Math.Sin(0.5f * Math.Abs(DTrackData.param1) / NumSections);
                    if (l > TrProfile.PitchControlScalar)
                    {
                        // Number of sections determined by chord length of PitchControlScalar meters
                        float chordAngle = 2.0f * (float)Math.Asin(0.5f * TrProfile.PitchControlScalar / DTrackData.param2);
                        NumSections = (int)Math.Abs((DTrackData.param1 / chordAngle));
                    }
                    break;
                case TrProfile.PitchControls.ChordDisplacement:
                    // Calculate chord displacement for NumSections
                    float d = DTrackData.param2 * (float)(1.0f - Math.Cos(0.5f * Math.Abs(DTrackData.param1) / NumSections));
                    if (d > TrProfile.PitchControlScalar)
                    {
                        // Number of sections determined by chord displacement of PitchControlScalar meters
                        float chordAngle = 2.0f * (float)Math.Acos(1.0f - TrProfile.PitchControlScalar / DTrackData.param2);
                        NumSections = (int)Math.Abs((DTrackData.param1 / chordAngle));
                    }
                    break;
            }
            // Limit the number of sections to prevent overflowing the number of triangles
            if (NumSections > 250)
                NumSections = 250;
            // Ensure an even number of sections
            if (NumSections % 2 == 1)
                NumSections++;

            // Length of each mesh segment (radians)
            SegmentLength = DTrackData.param1 / NumSections;

            // Get the vector pointing from the origin of the curve to the center in the X-Z plane
            Center = DTrackData.param2 * (DTrackData.param1 < 0 ? Vector3.Left : Vector3.Right);
        }

        /// <summary>
        /// Determines the transform matrix to give the current position and orientation on a straight
        /// section, relative to the local origin of the section.
        /// </summary>
        /// <param name="totLength">Output reference giving the total length along the section in meters.</param>
        /// <returns>Transform matrix used to move vertex from its original position to the appropriate position
        /// along the profile.</returns>
        public virtual Matrix LinearGen(out float totLength)
        {
            Matrix displacement = Matrix.Identity;

            totLength = SegmentLength * Offset;

            // Locate the new center point along the segment
            displacement.Translation = new Vector3(0.0f, 0.0f, -totLength);

            // Rotate the point into 3D space based on segment orientation
            displacement *= Orientation;

            // Remove roll rotation by redefining the left vector to be perpendicular to the global up vector
            displacement.Left = Vector3.Normalize(Vector3.Cross(Vector3.Up, displacement.Forward));
            displacement.Up = Vector3.Cross(displacement.Forward, displacement.Left);

            return displacement;
        }

        /// <summary>
        /// Determines the transform matrix to give the current position and orientation on a circular
        /// section, relative to the local origin of the section.
        /// </summary>
        /// <param name="totLength">Output reference giving the total length along the section in meters.</param>
        /// <returns>Transform matrix used to move vertex from its original position to the appropriate position
        /// along the profile.</returns>
        public virtual Matrix CircArcGen(out float totLength)
        {
            Matrix displacement = Matrix.Identity;

            totLength = Math.Abs(SegmentLength) * DTrackData.param2 * Offset;

            // Determine rotation in the X-Z plane alone
            Matrix curveRotation = Matrix.CreateRotationY(-SegmentLength * Offset);

            // Locate the new center point along the curve
            displacement.Translation = -Center;
            displacement *= curveRotation;
            displacement.Translation += Center;

            // Rotate the point into 3D space based on segment orientation
            displacement *= Orientation;

            // Remove roll rotation by redefining the left vector to be perpendicular to the global up vector
            displacement.Left = Vector3.Normalize(Vector3.Cross(Vector3.Up, displacement.Forward));
            displacement.Up = Vector3.Cross(displacement.Forward, displacement.Left);

            return displacement;
        }
    }
}
