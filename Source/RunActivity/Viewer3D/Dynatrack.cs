/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
/// 
/// Principal Author:
///    Rick Grout
/// Contributors:
///    Walt Niehoff
///    

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml; 
using System.Xml.Schema;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using MSTS;

namespace ORTS
{
    public enum DynatrackTextures
    {
        none = 0,
        Image1, Image1s, Image2
    }

    #region Dynatrack
    public class Dynatrack
    {
        /// <summary>
        /// Decompose an MSTS multi-subsection dynamic track section into multiple single-subsection sections.
        /// </summary>
        /// <param name="viewer">Viewer reference.</param>
        /// <param name="dTrackList">DynatrackDrawer list.</param>
        /// <param name="dTrackObj">Dynamic track section to decompose.</param>
        /// <param name="worldMatrix">Position matrix.</param>
        public static void Decompose(Viewer3D viewer, List<DynatrackDrawer> dTrackList, DyntrackObj dTrackObj, 
            WorldPosition worldMatrix)
        {
            // DYNAMIC TRACK
            // =============
            // Objectives:
            // 1-Decompose multi-subsection DT into individual sections.  
            // 2-Create updated transformation objects (instances of WorldPosition) to reflect 
            //   root of next subsection.
            // 3-Distribute elevation change for total section through subsections. (ABANDONED)
            // 4-For each meaningful subsection of dtrack, build a separate DynatrackMesh.
            //
            // Method: Iterate through each subsection, updating WorldPosition for the root of
            // each subsection.  The rotation component changes only in heading.  The translation 
            // component steps along the path to reflect the root of each subsection.

            // The following vectors represent local positioning relative to root of original (5-part) section:
            Vector3 localV = Vector3.Zero; // Local position (in x-z plane)
            Vector3 localProjectedV; // Local next position (in x-z plane)
            Vector3 displacement;  // Local displacement (from y=0 plane)
            Vector3 heading = Vector3.Forward; // Local heading (unit vector)

            float realRun; // Actual run for subsection based on path


            WorldPosition nextRoot = new WorldPosition(worldMatrix); // Will become initial root
            Vector3 sectionOrigin = worldMatrix.XNAMatrix.Translation; // Save root position
            worldMatrix.XNAMatrix.Translation = Vector3.Zero; // worldMatrix now rotation-only

            // Iterate through all subsections
            for (int iTkSection = 0; iTkSection < dTrackObj.trackSections.Count; iTkSection++)
            {
                float length = dTrackObj.trackSections[iTkSection].param1; // meters if straight; radians if curved
                if (length == 0.0) continue; // Consider zero-length subsections vacuous

                // Create new DT object copy; has only one meaningful subsection
                DyntrackObj subsection = new DyntrackObj(dTrackObj, iTkSection);

                //uint uid = subsection.trackSections[0].UiD; // for testing

                // Create a new WorldPosition for this subsection, initialized to nextRoot,
                // which is the WorldPosition for the end of the last subsection.
                // In other words, beginning of present subsection is end of previous subsection.
                WorldPosition root = new WorldPosition(nextRoot);

                // Now we need to compute the position of the end (nextRoot) of this subsection,
                // which will become root for the next subsection.

                // Clear nextRoot's translation vector so that nextRoot matrix contains rotation only
                nextRoot.XNAMatrix.Translation = Vector3.Zero;

                // Straight or curved subsection?
                if (subsection.trackSections[0].isCurved == 0) // Straight section
                {   // Heading stays the same; translation changes in the direction oriented
                    // Rotate Vector3.Forward to orient the displacement vector
                    localProjectedV = localV + length * heading;
                    displacement = TDBTraveller.MSTSInterpolateAlongStraight(localV, heading, length,
                                                            worldMatrix.XNAMatrix, out localProjectedV);
                    realRun = length;
                }
                else // Curved section
                {   // Both heading and translation change 
                    // nextRoot is found by moving from Point-of-Curve (PC) to
                    // center (O)to Point-of-Tangent (PT).
                    float radius = subsection.trackSections[0].param2; // meters
                    Vector3 left = radius * Vector3.Cross(Vector3.Up, heading); // Vector from PC to O
                    Matrix rot = Matrix.CreateRotationY(-length); // Heading change (rotation about O)
                    // Shared method returns displacement from present world position and, by reference,
                    // local position in x-z plane of end of this section
                    displacement = TDBTraveller.MSTSInterpolateAlongCurve(localV, left, rot,
                                            worldMatrix.XNAMatrix, out localProjectedV);

                    heading = Vector3.Transform(heading, rot); // Heading change
                    nextRoot.XNAMatrix = rot * nextRoot.XNAMatrix; // Store heading change
                    realRun = radius * ((length > 0) ? length : -length); // Actual run (meters)
                }

                // Update nextRoot with new translation component
                nextRoot.XNAMatrix.Translation = sectionOrigin + displacement;

                // THE FOLLOWING COMMENTED OUT CODE IS NOT COMPATIBLE WITH THE NEW MESH GENERATION METHOD.
                // IF deltaY IS STORED AS ANYTHING OTHER THAN 0, THE VALUE WILL GET USED FOR MESH GENERATION,
                // AND BOTH THE TRANSFORMATION AND THE ELEVATION CHANGE WILL GET USED, IN ESSENCE DOUBLE COUNTING.
                /*
                // Update subsection ancillary data
                subsection.trackSections[0].realRun = realRun;
                if (iTkSection == 0)
                {
                    subsection.trackSections[0].deltaY = displacement.Y;
                }
                else
                {
                    // Increment-to-increment change in elevation
                    subsection.trackSections[0].deltaY = nextRoot.XNAMatrix.Translation.Y - root.XNAMatrix.Translation.Y;
                }
                */

                // Create a new DynatrackDrawer for the subsection
                dTrackList.Add(new DynatrackDrawer(viewer, subsection, root, nextRoot));
                localV = localProjectedV; // Next subsection
            }
        } // end Decompose

    } // end class Dynatrack
    #endregion

    #region DynatrackDrawer
    public class DynatrackDrawer
    {
        Viewer3D Viewer;
        Material dtrackMaterial;

        // Classes reqiring instantiation
        public DynatrackMesh dtrackMesh;

        #region Class variables
        WorldPosition worldPosition;
        #endregion

        #region Constructor
        /// <summary>
        /// DynatrackDrawer constructor
        /// </summary>
        public DynatrackDrawer(Viewer3D viewer, DyntrackObj dtrack, WorldPosition position, WorldPosition endPosition)
        {
            Viewer = viewer;
            worldPosition = position;
            dtrackMaterial = Materials.Load(Viewer.RenderProcess, "DynatrackMaterial");

            // Instantiate classes
            dtrackMesh = new DynatrackMesh(Viewer.RenderProcess, dtrack, worldPosition, endPosition);
        } // end DynatrackDrawer constructor
        #endregion

        /// <summary>
        /// PrepareFrame adds any object mesh in-FOV to the RenderItemCollection. 
        /// and marks the last LOD that is in-range.
        /// </summary>
        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Offset relative to the camera-tile origin
            int dTileX = worldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = worldPosition.TileZ - Viewer.Camera.TileZ;
            Vector3 tileOffsetWrtCamera = new Vector3(dTileX * 2048, 0, -dTileZ * 2048);

            // Find midpoint between track section end and track section root.
            // (Section center for straight; section chord center for arc.)
            Vector3 xnaLODCenter = 0.5f * (dtrackMesh.XNAEnd + worldPosition.XNAMatrix.Translation +
                                            2 * tileOffsetWrtCamera);
            dtrackMesh.MSTSLODCenter = new Vector3(xnaLODCenter.X, xnaLODCenter.Y, -xnaLODCenter.Z);

            // Ignore any mesh not in field-of-view
            if (!Viewer.Camera.InFOV(dtrackMesh.MSTSLODCenter, dtrackMesh.ObjectRadius)) return;

            // Scan LODs in reverse order, and find first LOD in-range
            LODItem lod;
            int lodIndex = dtrackMesh.LODGrid.Length;
            do
            {
                if (--lodIndex < 0) return; // No LOD in-range
                lod = (LODItem)dtrackMesh.TrProfile.LODItems[lodIndex];
            } while (!Viewer.Camera.InRange(dtrackMesh.MSTSLODCenter, 0, lod.CutoffRadius));
            dtrackMesh.LastIndex = lodIndex; // Mark index farthest in-range LOD

            // Initialize xnaXfmWrtCamTile to object-tile to camera-tile translation:
            Matrix xnaXfmWrtCamTile = Matrix.CreateTranslation(tileOffsetWrtCamera);
            xnaXfmWrtCamTile = worldPosition.XNAMatrix * xnaXfmWrtCamTile; // Catenate to world transformation
            // (Transformation is now with respect to camera-tile origin)

            // Add dtrackMesh to the RenderItems collection
            frame.AddPrimitive(dtrackMaterial, dtrackMesh, RenderPrimitiveGroup.World, ref xnaXfmWrtCamTile, ShapeFlags.AutoZBias);
        } // end PrepareFrame
    } // end DynatrackDrawer
    #endregion

    #region DynatrackProfile
    // A track profile consists of a number of groups used for LOD considerations.  Here, these groups
    // are called "LODItems."  Each group consists of one of more "polylines".  A polyline is a 
    // chain of line segments successively interconnected. A polyline of n segments is defined by n+1 "vertices."
    // (Use of a polyline allows for use of more than single segments.  For example, a ballast LOD could be 
    // defined as left slope, level, right slope - a single polyline of four vertices.)

    // Track profile file class
    public class TRPFile
    {
        // A single track profile member variable
        public TrProfile TrackProfile;

        public TRPFile(string filespec)
        {
            if (filespec == "")
            {
                // No track profile provided, use default
                TrackProfile = new TrProfile();
                Trace.Write("(default)");
                return;
            }
            FileInfo fileInfo = new FileInfo(filespec);
            if (!fileInfo.Exists)
            {
                TrackProfile = new TrProfile(); // Default profile if no file
                Trace.Write("(default)");
            }
            else
            {
                string fext = filespec.Substring(filespec.LastIndexOf('.')); // File extension
                switch (fext.ToUpper())
                {
                    case ".DAT": // MSTS-style
                        using (STFReader stf = new STFReader(filespec, false))
                        {
                            // "EXPERIMENTAL" header is temporary
                            if (stf.SimisSignature != "EXPERIMENTAL")
                            {
                                STFException.TraceError(stf, "Invalid header - file will not be processed. Using DEFAULT profile.");
                                TrackProfile = new TrProfile(); // Default profile if no file
                            }
                            else
                                try
                                {
                                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                                        new STFReader.TokenProcessor("trprofile", ()=>{ TrackProfile = new TrProfile(stf); }),
                                    });
                                }
                                catch (Exception e)
                                {
                                    STFException.TraceError(stf, "Track profile DAT constructor failed because " + e.Message + ". Using DEFAULT profile.");
                                    TrackProfile = new TrProfile(); // Default profile if no file
                                }
                                finally
                                {
                                    if (TrackProfile == null)
                                    {
                                        STFException.TraceError(stf, "Track profile DAT constructor failed. Using DEFAULT profile.");
                                        TrackProfile = new TrProfile(); // Default profile if no file
                                    }
                                }
                        }
                        Trace.Write("(.DAT)");
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
                        settings.Schemas.Add("TrProfile.xsd", XmlReader.Create(xsdFilespec)); // Add schema from file

                        // Create an XML reader for the .xml file
                        using (XmlReader reader = XmlReader.Create(filespec, settings))
                        {
                            TrackProfile = new TrProfile(reader);
                        }
                        Trace.Write("(.XML)");
                        break;
                    default:
                        // File extension not supported; create a default track profile
                        TrackProfile = new TrProfile();
                        Trace.Write("(default)");
                        break;
                } // end switch
            }
        } // end TRPFile constructor

        /// <summary>
        /// Creates a TRPFile instance from a track profile file (XML or STF) or canned.
        /// (Precedence is XML [.XML], STF [.DAT], default [canned]).
        /// </summary>
        /// <param name="routePath">Path to route.</param>
        /// <param name="trpFile">TRPFile created (out).</param>
        public static void CreateTrackProfile(string routePath, out TRPFile trpFile)
        {
            //Establish default track profile
            //Trace.Write(" TRP");
            if (Directory.Exists(routePath) && File.Exists(routePath + @"\TrProfile.xml"))
            {
                // XML-style
                trpFile = new TRPFile(routePath + @"\TrProfile.xml");
            }
            else if (Directory.Exists(routePath) && File.Exists(routePath + @"\TrProfile.dat"))
            {
                // MSTS-style
                trpFile = new TRPFile(routePath + @"\TrProfile.dat");
            }
            else
            {
                // default
                trpFile = new TRPFile("");
            }
            // FOR DEBUGGING: Writes XML file from current TRP
            //TRP.TrackProfile.SaveAsXML(@"C:/Users/Walt/Desktop/TrProfile.xml");
        } // end CreateTrackProfile

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

    } // end class TRPFile

    // Dynamic track profile class
    public class TrProfile
    {
        // NumVertices and NumSegments used for sizing vertex and index buffers
        public uint NumVertices;                     // Total independent vertices in profile
        public uint NumSegments;                     // Total line segment count in profile

        public ArrayList LODItems = new ArrayList(); // Array of profile items corresponding to levels-of-detail

        public string Name;                          // e.g., "Default track profile"
        public string Image1Name = "";               // For primary texture image file name
        public string Image1sName = "";              // For wintertime alternate
        public string Image2Name = "";               // For secondary texture image file name

        /// <summary>
        /// TrProfile constructor from STFReader-style profile file
        /// </summary>
        public TrProfile(STFReader stf)
        {
            NumVertices = 0;
            NumSegments = 0;

            Name = "Default Dynatrack profile";
            Image1Name = "acleantrack1.ace";
            Image1sName = "acleantrack1.ace";
            Image2Name = "acleantrack2.ace";

            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("image1name", ()=>{ Image1Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("image1sname", ()=>{ Image1sName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("image2name", ()=>{ Image2Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("loditem", ()=>{ LODItems.Add(new LODItem(stf, this)); }),
            });
            // Checks for required member variables: 
            // Name not required.
            // Image1Name, Image1sName, and Image2Name initialized as MSTS defaults.
            if (LODItems.Count == 0) throw new Exception("missing LODItems");

        } // end TrProfile(STFReader) constructor

        /// <summary>
        /// TrProfile constructor from XML profile file
        /// </summary>
        public TrProfile(XmlReader reader)
        {
            NumVertices = 0;
            NumSegments = 0;

            if (reader.IsStartElement())
            {
                if (reader.Name == "TrProfile")
                {
                    // root
                    Name = reader.GetAttribute("Name");
                    Image1Name = reader.GetAttribute("Image1Name");
                    Image1sName = reader.GetAttribute("Image1sName");
                    Image2Name = reader.GetAttribute("Image2Name");
                }
                else
                {
                    //TODO: Need to handle ill-formed XML profile
                }
            }

            string name;
            LODItem lod = null;
            Polyline pl = null;
            Vertex v;
            string[] s;
            char[] sep = new char[] {' '};
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case "LODItem":
                            name = reader.GetAttribute("Name");
                            lod = new LODItem(name);
                            lod.Texture = LODDefineTexture(reader.GetAttribute("Texture"));
                            if (lod.Texture == DynatrackTextures.none) lod.Texture = LODDefaultTexture();
                            lod.LightingSpecular = float.Parse(reader.GetAttribute("LightingSpecular"));
                            lod.CutoffRadius = float.Parse(reader.GetAttribute("CutoffRadius"));
                            lod.MipMapLevelOfDetailBias = float.Parse(reader.GetAttribute("MipMapLevelOfDetailBias"));
                            lod.AlphaBlendEnable = bool.Parse(reader.GetAttribute("AlphaBlendEnable"));
                            LODItems.Add(lod);
                            break;
                        case "Polyline":
                            pl = new Polyline();
                            pl.Name = reader.GetAttribute("Name");
                            s = reader.GetAttribute("DeltaTexCoord").Split(sep);
                            pl.DeltaTexCoord = new Vector2(float.Parse(s[0]), float.Parse(s[1]));
                            lod.Polylines.Add(pl);
                            break;
                        case "Vertex":
                            v = new Vertex();
                            s = reader.GetAttribute("Position").Split(sep);
                            v.Position = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                            s = reader.GetAttribute("Normal").Split(sep);
                            v.Normal = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                            s = reader.GetAttribute("TexCoord").Split(sep);
                            v.TexCoord = new Vector2(float.Parse(s[0]), float.Parse(s[1]));
                            pl.Vertices.Add(v);
                            NumVertices++; // Bump vertex count
                            if (pl.Vertices.Count > 1) NumSegments++;
                            break;
                        default:
                            break;
                    }
                }
            }
        } // end TrProfile(XmlReader) constructor

        /// <summary>
        /// LODDefineTexture returns a texture based on the texture identifier string.
        /// </summary>
        public DynatrackTextures LODDefineTexture(string textureID)
        {
            DynatrackTextures texture;
            switch (textureID)
            {
                case "Image1":
                    texture = DynatrackTextures.Image1;
                    break;
                case "Image1s":
                    texture = DynatrackTextures.Image1s;
                    break;
                case "Image2":
                    texture = DynatrackTextures.Image2;
                    break;
                case null: // No Texture attribute in the LOD 
                    texture = DynatrackTextures.none;
                    break;
                default: // Everything else
                    texture = DynatrackTextures.Image1;
                    Trace.TraceWarning("Texture " + texture + "not defined; substituting Image1.");
                    break;
            } // end switch (texture)
            return texture;
        } // end LODDefineTexture

        /// <summary>
        /// LODDefaultTexture returns the texture used by the last LOD unless this is the first LOD,
        /// in which case it returns Image1. 
        /// </summary>
        public DynatrackTextures LODDefaultTexture()
        {
                // Use the texture from the last LOD
                int lastIndex = this.LODItems.Count - 1;
                if (lastIndex > 0) return ((LODItem)this.LODItems[lastIndex]).Texture;
                else
                {
                    // If this is the first LOD in the profile and there is no Texture,
                    // use Image1 and flag this with a warning message.
                    Trace.TraceWarning("No Texture specified in initial LOD of track profile; substituting Image1.");
                    return DynatrackTextures.Image1;
                }
        } // end LODDefaultTexture

        /// <summary>
        /// TrProfile constructor (default - builds from self-contained data)
        /// </summary>
        public TrProfile() // Nasty: void return type is not allowed. (See MSDN for compiler error CS0542.)
        {
            // Default TrProfile constructor
            LODItem lod; // Local LODItem instance
            Polyline pl; // Local polyline instance

            // We're going to be counting vertices and segments as we create them; so intialize:
            NumVertices = 0;
            NumSegments = 0;

            Name = "Default Dynatrack profile";
            Image1Name = "acleantrack1.ace";
            Image1sName = "acleantrack1.ace";
            Image2Name = "acleantrack2.ace";

            // Make ballast
            lod = new LODItem("Ballast");
            lod.CutoffRadius = 2000.0f;
            lod.MipMapLevelOfDetailBias = -1;
            lod.AlphaBlendEnable = true;
            lod.LightingSpecular = 0;
            lod.Texture = DynatrackTextures.Image1;
            LODItems.Add(lod); // Append to LODItems array

            pl = new Polyline(this, "ballast", 2);
            pl.DeltaTexCoord = new Vector2(0.0f, 0.2088545f);
            pl.Vertices.Add(new Vertex(-2.5f, 0.2f, 0.0f, 0f, 1f, 0f, -.153916f, -.280582f));
            pl.Vertices.Add(new Vertex(2.5f, 0.2f, 0.0f, 0f, 1f, 0f, .862105f, -.280582f));
            lod.Polylines.Add(pl);
            Accum(pl.Vertices.Count);
            
            // make railtops
            lod = new LODItem("Railtops");
            lod.CutoffRadius = 1200.0f;
            lod.MipMapLevelOfDetailBias = 0;
            lod.AlphaBlendEnable = false;
            lod.LightingSpecular = 25;
            lod.Texture = DynatrackTextures.Image2;
            LODItems.Add(lod); // Append to LODItems array

            pl = new Polyline(this, "right", 2);
            pl.DeltaTexCoord = new Vector2(.0744726f, 0f);
            pl.Vertices.Add(new Vertex(-.8675f, .325f, 0.0f, 0f, 1f, 0f, .232067f, .126953f));
            pl.Vertices.Add(new Vertex(-.7175f, .325f, 0.0f, 0f, 1f, 0f, .232067f, .224609f));
            lod.Polylines.Add(pl);
            Accum(pl.Vertices.Count);
   
            pl = new Polyline(this, "left", 2);
            pl.DeltaTexCoord = new Vector2(.0744726f, 0f);
            pl.Vertices.Add(new Vertex(.7175f, .325f, 0.0f, 0f, 1f, 0f, .232067f, .126953f));
            pl.Vertices.Add(new Vertex(.8675f, .325f, 0.0f, 0f, 1f, 0f, .232067f, .224609f));
            lod.Polylines.Add(pl);
            Accum(pl.Vertices.Count);

            // make railsides
            lod = new LODItem("Railsides");
            lod.CutoffRadius = 700.0f;
            lod.MipMapLevelOfDetailBias = 0;
            lod.AlphaBlendEnable = false;
            lod.LightingSpecular = 0;
            lod.Texture = DynatrackTextures.Image2;
            LODItems.Add(lod); // Append to LODItems array

            pl = new Polyline(this, "left_outer", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(-.8675f, .200f, 0.0f, -1f, 0f, 0f, -.139362f, .101563f));
            pl.Vertices.Add(new Vertex(-.8675f, .325f, 0.0f, -1f, 0f, 0f, -.139363f, .003906f));
            lod.Polylines.Add(pl);
            Accum(pl.Vertices.Count);

            pl = new Polyline(this, "left_inner", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(-.7175f, .325f, 0.0f, 1f, 0f, 0f, -.139363f, .003906f));
            pl.Vertices.Add(new Vertex(-.7175f, .200f, 0.0f, 1f, 0f, 0f, -.139362f, .101563f));
            lod.Polylines.Add(pl);
            Accum(pl.Vertices.Count);

            pl = new Polyline(this, "right_inner", 2); 
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(.7175f, .200f, 0.0f, -1f, 0f, 0f, -.139362f, .101563f));
            pl.Vertices.Add(new Vertex(.7175f, .325f, 0.0f, -1f, 0f, 0f, -.139363f, .003906f));
            lod.Polylines.Add(pl);
            Accum(pl.Vertices.Count);
            
            pl = new Polyline(this, "right_outer", 2); 
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(.8675f, .325f, 0.0f, 1f, 0f, 0f, -.139363f, .003906f));
            pl.Vertices.Add(new Vertex(.8675f, .200f, 0.0f, 1f, 0f, 0f, -.139362f, .101563f));
            lod.Polylines.Add(pl);
            Accum(pl.Vertices.Count);
        } // end TrProfile() constructor

        public void Accum(int count)
        {
            // Accumulates total independent vertices and total line segments
            // Used for sizing of vertex and index buffers
            NumVertices += (uint)count;
            NumSegments += (uint)count - 1;
        } // end Accum

    } // end class TrProfile

    public class LODItem
    {
        public ArrayList Polylines = new ArrayList();  // Array of arrays of vertices 
        
        public string Name;                            // e.g., "Rail sides"
        public float CutoffRadius;                     // Distance beyond which LOD is not seen
        public float MipMapLevelOfDetailBias;
        public float LightingSpecular;
        public bool AlphaBlendEnable;
        public DynatrackTextures Texture;

        /// <summary>
        /// LODITem constructor (default &amp; XML)
        /// </summary>
        public LODItem(string name)
        {
            Name = name;
        } // end LODItem() constructor

        /// <summary>
        /// LODITem constructor (DAT)
        /// </summary>
        public LODItem(STFReader stf, TrProfile parent)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("cutoffradius", ()=>{ CutoffRadius = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("mipmaplevelofdetailbias", ()=>{ MipMapLevelOfDetailBias = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("alphablendenable", ()=>{ AlphaBlendEnable = stf.ReadBoolBlock(true); }),
                new STFReader.TokenProcessor("lightingspecular", ()=>{ LightingSpecular = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("texture", ()=> { Texture = parent.LODDefineTexture(stf.ReadStringBlock(null));
                }),
                new STFReader.TokenProcessor("polyline", ()=>{
                    Polyline pl = new Polyline(stf);
                    Polylines.Add(pl); // Append to Polylines array
                    parent.Accum(pl.Vertices.Count);
                }),
            });

            // Checks for required member variables:
            // Name not required.
            if (Texture == DynatrackTextures.none) Texture = parent.LODDefaultTexture();
            if (CutoffRadius == 0) throw new Exception("missing CutoffRadius");
            // MipMapLevelOfDetail bias initializes to 0.
            // AlphaBlendEnable initializes to false.
            if (Polylines.Count == 0) throw new Exception("missing Polylines");
            if (Texture == DynatrackTextures.none)
            {
                // Texture is not defined in the LOD; use the texture from the last LOD
                int lastIndex = parent.LODItems.Count - 1;
                if (lastIndex > 0) Texture = ((LODItem)parent.LODItems[lastIndex]).Texture;
                else
                {
                    // If this is the first LOD in the profile and there is no Texture,
                    // use Image1 and flag this with a warning message.
                    Texture = DynatrackTextures.Image1;
                    Trace.TraceWarning("No Texture specified in initial LOD of track profile; substituting Image1.");
                }
            }

        } // end LODItem() constructor
    } // end class LODItem

    public class Polyline
    {
        public ArrayList Vertices = new ArrayList();    // Array of vertices 
 
        public string Name;                             // e.g., "1:1 embankment"
        public Vector2 DeltaTexCoord;                   // Incremental change in (u, v) from one cross section to the next

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
        } // end Polyline() constructor

        /// <summary>
        /// Polyline constructor (DAT)
        /// </summary>
        public Polyline(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ stf.ReadStringBlock(null); }),
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
        } // end Polyline() constructor
    } // end Polyline

    public struct Vertex
    {
        public Vector3 Position;                           // Position vector (x, y, z)
        public Vector3 Normal;                             // Normal vector (nx, ny, nz)
        public Vector2 TexCoord;                           // Texture coordinate (u, v)

        // Vertex constructor (default)
        public Vertex(float x, float y, float z, float nx, float ny, float nz, float u, float v)
        {
            Position = new Vector3(x, y, z);
            Normal = new Vector3(nx, ny, nz);
            TexCoord = new Vector2(u, v);
        } // end Vertex() constructor

        // Vertex constructor (DAT)
        public Vertex(STFReader stf)
        {
            Vertex v = new Vertex(); // Temp variable used to construct the struct in ParseBlock
            v.Position = new Vector3();
            v.Normal = new Vector3();
            v.TexCoord = new Vector2();
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
            });
            this = v;
            // Checks for required member variables
            // No way to check for missing Position.
            if (Normal == Vector3.Zero) throw new Exception("improper Normal");
            // No way to check for missing TexCoord
        } // end Vertex() constructor

    } // end Vertex
    #endregion

    #region DynatrackMesh
    public class DynatrackMesh : RenderPrimitive
    {
        VertexDeclaration VertexDeclaration;
        VertexBuffer VertexBuffer;
        IndexBuffer IndexBuffer;

        VertexPositionNormalTexture[] VertexList; // Array of vertices
        short[] TriangleListIndices;// Array of indices to vertices for triangles
        uint VertexIndex = 0;       // Index of current position in VertexList
        uint IndexIndex = 0;        // Index of current position in TriangleListIndices
        int VertexStride;           // in bytes
        int NumVertices;            // Number of vertices in the track profile
        short NumIndices;           // Number of triangle indices

        // LOD member variables:
        public int DrawIndex;       // Used by Draw to determine which LOD to draw.
        public int LastIndex;       // Marks last LOD that is in-range
        public Vector3 XNAEnd;      // Location of termination-of-section (as opposed to root)
        public float ObjectRadius;  // Radius of bounding sphere
        public Vector3 MSTSLODCenter; // Center of bounding sphere
        public struct GridItem
        {
            public uint VertexOrigin;// Start index for first vertex in LOD
            public uint VertexLength;// Number of vertices in LOD
            public uint IndexOrigin; // Start index for first triangle in LOD
            public uint IndexLength; // Number of triangle vertex indicies in LOD
        }
        public GridItem[] LODGrid;   // Grid matrix

        // Geometry member variables:
        int NumSections;            // Number of cross sections needed to make up a track section.
        float SegmentLength;        // meters if straight; radians if circular arc
        Vector3 DDY;                // Elevation (y) change from one cross section to next
        Vector3 OldV;               // Deviation from centerline for previous cross section
        Vector3 OldRadius;          // Radius vector to centerline for previous cross section

        //TODO: Candidates for re-packaging:
        Matrix sectionRotation;     // Rotates previous profile into next profile position on curve.
        Vector3 center;             // Center coordinates of curve radius
        Vector3 radius;             // Radius vector to cross section on curve centerline

        // This structure holds the basic geometric parameters of a DT section.
        public struct DtrackData
        {
            public int IsCurved;    // Straight (0) or circular arc (1)
            public float param1;    // Length in meters (straight) or radians (circular arc)
            public float param2;    // Radius for circular arc
            public float deltaY;    // Change in elevation (y) from beginning to end of section
        }
        DtrackData DTrackData;      // Was: DtrackData[] dtrackData;

        public uint UiD; // Used for debugging only

        public TrProfile TrProfile;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DynatrackMesh(RenderProcess renderProcess, DyntrackObj dtrack, WorldPosition worldPosition, 
                                WorldPosition endPosition)
        {
            // DynatrackMesh is responsible for creating a mesh for a section with a single subsection.
            // It also must update worldPosition to reflect the end of this subsection, subsequently to
            // serve as the beginning of the next subsection.

            UiD = dtrack.trackSections[0].UiD; // Used for debugging only

            // The track cross section (profile) vertex coordinates are hard coded.
            // The coordinates listed here are those of default MSTS "A1t" track.
            // TODO: Read this stuff from a file. Provide the ability to use alternative profiles.

            // In this implementation dtrack has only 1 DT subsection.
            if (dtrack.trackSections.Count != 1)
            {
                throw new ApplicationException(
                    "DynatrackMesh Constructor detected a multiple-subsection dynamic track section. " +
                    "(SectionIdx = " + dtrack.SectionIdx + ")");
            }
            // Initialize a scalar DtrackData object
            DTrackData = new DtrackData();
            DTrackData.IsCurved = (int)dtrack.trackSections[0].isCurved;
            DTrackData.param1 = dtrack.trackSections[0].param1;
            DTrackData.param2 = dtrack.trackSections[0].param2;
            DTrackData.deltaY = dtrack.trackSections[0].deltaY;
            XNAEnd = endPosition.XNAMatrix.Translation;

            TrProfile = renderProcess.Viewer.Simulator.TRP.TrackProfile;

            // Build the mesh, filling the vertex and triangle index buffers.
            BuildMesh(worldPosition); // Build vertexList and triangleListIndices

            if (DTrackData.IsCurved == 0) ObjectRadius = 0.5f * DTrackData.param1; // half-length
            else ObjectRadius = DTrackData.param2 * (float)Math.Sin(0.5 * Math.Abs(DTrackData.param1)); // half chord length

            VertexDeclaration = null;
            VertexBuffer = null;
            IndexBuffer = null;
            InitializeVertexBuffers(renderProcess.GraphicsDevice);
        } // end DynatrackMesh constructor

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (DrawIndex < 0 || DrawIndex >= TrProfile.LODItems.Count) return;

            graphicsDevice.VertexDeclaration = VertexDeclaration;
            graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexStride);
            graphicsDevice.Indices = IndexBuffer;

            graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        (int)LODGrid[DrawIndex].VertexOrigin,
                        (int)LODGrid[DrawIndex].VertexLength,
                        (int)LODGrid[DrawIndex].IndexOrigin,
                        (int)LODGrid[DrawIndex].IndexLength / 3);
        } // end Draw

        #region Vertex and triangle index generators
        /// <summary>
        /// Builds a section of Dynatrack to TrProfile specifications as one vertex buffer and one index buffer.
        /// The order the buffers are built in reflects the nesting in the TrProfile.  The nesting order is:
        /// (LOD items (Polylines (Vertices))).  All vertices and indices are built contiguously for an LOD.
        /// </summary>
        public void BuildMesh(WorldPosition worldPosition)
        {
            LODGrid = new GridItem[TrProfile.LODItems.Count];

            // Call for track section to initialize itself
            if (DTrackData.IsCurved == 0) LinearGen(); else CircArcGen();
            // Count vertices and indices
            NumVertices = (int)(TrProfile.NumVertices * NumSections + TrProfile.NumVertices);
            NumIndices = (short)(TrProfile.NumSegments * NumSections * 6);
            // (Cells x 2 triangles/cell x 3 indices/triangle)

            // Allocate memory for vertices and indices
            VertexList = new VertexPositionNormalTexture[NumVertices]; // numVertices is now aggregate
            TriangleListIndices = new short[NumIndices]; // as is NumIndices

            uint iLOD = 0;
            foreach (LODItem lod in TrProfile.LODItems) 
            {
                LODGrid[iLOD].VertexOrigin = VertexIndex;   // Initial vertex index for this LOD
                LODGrid[iLOD].IndexOrigin = IndexIndex;     // Initial index index for this LOD

                // Initial load of baseline cross section polylines for this LOD only:
                foreach (Polyline pl in lod.Polylines)
                {
                    foreach (Vertex v in pl.Vertices)
                    {
                        VertexList[VertexIndex].Position = v.Position;
                        VertexList[VertexIndex].Normal = v.Normal;
                        VertexList[VertexIndex].TextureCoordinate = v.TexCoord;
                        VertexIndex++;
                    }
                }
                // Number of vertices and indicies for this LOD only
                LODGrid[iLOD].VertexLength = VertexIndex - LODGrid[iLOD].VertexOrigin;  
                LODGrid[iLOD].IndexLength = IndexIndex - LODGrid[iLOD].IndexOrigin;
                // Initial load of base cross section complete

                // Now generate and load subsequent cross sections
                OldRadius = -center;
                uint stride = LODGrid[iLOD].VertexLength;
                for (uint i = 0; i < NumSections; i++)
                {
                    foreach (Polyline pl in lod.Polylines)
                    {
                        uint plv = 0; // Polyline vertex index
                        foreach (Vertex v in pl.Vertices)
                        {
                            if (DTrackData.IsCurved == 0) LinearGen(stride, pl); // Generation call
                            else CircArcGen(stride, pl);

                            if (plv > 0)
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
                        } // end foreach v  
                    } // end foreach pl
                    OldRadius = radius; // Get ready for next segment
                } // end for i
                LODGrid[iLOD].VertexLength = VertexIndex - LODGrid[iLOD].VertexOrigin;
                LODGrid[iLOD].IndexLength = IndexIndex - LODGrid[iLOD].IndexOrigin;
                iLOD++; // Step LOD index
            } // end foreach lod
        } // end BuildMesh

        /// <summary>
        /// Initializes member variables for straight track sections.
        /// </summary>
        void LinearGen()
        {
            // Define the number of track cross sections in addition to the base.
            NumSections = 1;
            //numSections = 10; //TESTING
            // TODO: Generalize count to profile file specification

            SegmentLength = DTrackData.param1 / NumSections; // Length of each mesh segment (meters)
            DDY = new Vector3(0, DTrackData.deltaY / NumSections, 0); // Incremental elevation change
        } // end LinearGen

        /// <summary>
        /// Initializes member variables for circular arc track sections.
        /// </summary>
        void CircArcGen()
        {
            // Define the number of track cross sections in addition to the base.
            // Assume one skewed straight section per degree of curvature
            NumSections = (int)Math.Abs(MathHelper.ToDegrees(DTrackData.param1));
            if (NumSections == 0) NumSections++; // Very small radius track - zero avoidance
            //numSections = 10; //TESTING
            // TODO: Generalize count to profile file specification

            SegmentLength = DTrackData.param1 / NumSections; // Length of each mesh segment (radians)
            DDY = new Vector3(0, DTrackData.deltaY / NumSections, 0); // Incremental elevation change

            // The approach here is to replicate the previous cross section, 
            // rotated into its position on the curve and vertically displaced if on grade.
            // The local center for the curve lies to the left or right of the local origin and ON THE BASE PLANE
            center = DTrackData.param2 * (DTrackData.param1 < 0 ? Vector3.Left : Vector3.Right);
            sectionRotation = Matrix.CreateRotationY(-SegmentLength); // Rotation per iteration (constant)
        } // end CircArcGen

        /// <summary>
        /// Generates vertices for a succeeding cross section (straight track).
        /// </summary>
        /// <param name="stride">Index increment between section-to-section vertices.</param>
        /// <param name="pl"></param>
        void LinearGen(uint stride, Polyline pl)
        {
            Vector3 displacement = new Vector3(0, 0, -SegmentLength) + DDY;
            float wrapLength = displacement.Length();
            Vector2 uvDisplacement = pl.DeltaTexCoord * wrapLength;

            Vector3 p = VertexList[VertexIndex - stride].Position + displacement;
            Vector3 n = VertexList[VertexIndex - stride].Normal;
            Vector2 uv = VertexList[VertexIndex - stride].TextureCoordinate + uvDisplacement; 

            VertexList[VertexIndex].Position = new Vector3(p.X, p.Y, p.Z);
            VertexList[VertexIndex].Normal = new Vector3(n.X, n.Y, n.Z);
            VertexList[VertexIndex].TextureCoordinate = new Vector2(uv.X, uv.Y);
        }

        /// <summary>
        /// /// Generates vertices for a succeeding cross section (circular arc track).
        /// </summary>
        /// <param name="stride">Index increment between section-to-section vertices.</param>
        /// <param name="pl"></param>
        void CircArcGen(uint stride, Polyline pl)
        {
            // Get the previous vertex about the local coordinate system
            OldV = VertexList[VertexIndex - stride].Position - center - OldRadius;
            // Rotate the old radius vector to become the new radius vector
            radius = Vector3.Transform(OldRadius, sectionRotation);
            float wrapLength = (radius - OldRadius).Length(); // Wrap length is centerline chord
            Vector2 uvDisplacement = pl.DeltaTexCoord * wrapLength;

            // Rotate the point about local origin and reposition it (including elevation change)
            Vector3 p = DDY + center + radius + Vector3.Transform(OldV, sectionRotation);
            Vector3 n = VertexList[VertexIndex - stride].Normal;
            Vector2 uv = VertexList[VertexIndex - stride].TextureCoordinate + uvDisplacement; 

            VertexList[VertexIndex].Position = new Vector3(p.X, p.Y, p.Z);
            VertexList[VertexIndex].Normal = new Vector3(n.X, n.Y, n.Z);
            VertexList[VertexIndex].TextureCoordinate = new Vector2(uv.X, uv.Y);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Initializes the vertex and triangle index list buffers.
        /// </summary>
        private void InitializeVertexBuffers(GraphicsDevice graphicsDevice)
        {
            if (VertexDeclaration == null)
            {
                VertexDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionNormalTexture.VertexElements);
                VertexStride = VertexPositionNormalTexture.SizeInBytes;
            }
            // Initialize the vertex and index buffers, allocating memory for each vertex and index
            VertexBuffer = new VertexBuffer(graphicsDevice, VertexPositionNormalTexture.SizeInBytes * VertexList.Length, BufferUsage.WriteOnly);
            VertexBuffer.SetData(VertexList);
            if (IndexBuffer == null)
            {
                IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), NumIndices, BufferUsage.WriteOnly);
                IndexBuffer.SetData(TriangleListIndices);
            }
        }
        #endregion
    }
    #endregion
}
