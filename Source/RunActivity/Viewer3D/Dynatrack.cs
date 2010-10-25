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

        public void PrepareFrame(RenderFrame frame, ElapsedTime elapsedTime)
        {
            // Offset relative to the camera-tile origin
            int dTileX = worldPosition.TileX - Viewer.Camera.TileX;
            int dTileZ = worldPosition.TileZ - Viewer.Camera.TileZ;
            Vector3 tileOffsetWrtCamera = new Vector3(dTileX * 2048, 0, -dTileZ * 2048);

            // Find midpoint between auxpoint and track section root
            Vector3 xnaLODCenter = 0.5f * (dtrackMesh.XNAEnd + worldPosition.XNAMatrix.Translation +
                                            2 * tileOffsetWrtCamera);
            dtrackMesh.MSTSLODCenter = new Vector3(xnaLODCenter.X, xnaLODCenter.Y, -xnaLODCenter.Z);

            if (Viewer.Camera.CanSee(dtrackMesh.MSTSLODCenter, dtrackMesh.ObjectRadius, 500))
            {
                // Initialize xnaXfmWrtCamTile to object-tile to camera-tile translation:
                Matrix xnaXfmWrtCamTile = Matrix.CreateTranslation(tileOffsetWrtCamera);
                xnaXfmWrtCamTile = worldPosition.XNAMatrix * xnaXfmWrtCamTile; // Catenate to world transformation
                // (Transformation is now with respect to camera-tile origin)

                frame.AddPrimitive(dtrackMaterial, dtrackMesh, RenderPrimitiveGroup.World, ref xnaXfmWrtCamTile, ShapeFlags.AutoZBias);
            }
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
                        using (STFReader f = new STFReader(filespec))
                        {
                            // "EXPERIMENTAL" header is temporary
                            if (f.SIMISsignature != "EXPERIMENTAL")
                                throw new STFException(f, "Invalid header");
                            else
                            {
                                string token = f.ReadItem();
                                while (token != "") // EOF
                                {
                                    if (token == "(") throw new STFException(f, "Unexpected (");
                                    else if (token == ")") throw new STFException(f, "Unexpected )");
                                    else if (0 == String.Compare(token, "TrProfile", true))
                                        TrackProfile = new TrProfile(f); // .dat file constructor
                                    else f.SkipBlock();
                                    token = f.ReadItem();
                                }
                                if (TrackProfile == null) throw new STFException(f, "Track profile DAT constructor failed.");
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
        public TrProfile(STFReader f)
        {
            NumVertices = 0;
            NumSegments = 0;

            Name = "Default Dynatrack profile";
            Image1Name = "acleantrack1.ace";
            Image1sName = "acleantrack1.ace";
            Image2Name = "acleantrack2.ace";

            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                switch (token)
                {
                    case "Name":
                        Name = f.ReadItemBlock(null);
                        break;
                    case "Image1Name":
                        Image1Name = f.ReadItemBlock(null);
                        break;
                    case "Image1sName":
                        Image1sName = f.ReadItemBlock(null);
                        break;
                    case "Image2Name":
                        Image2Name = f.ReadItemBlock(null);
                        break;
                    case "LODItem":
                        LODItem lod = new LODItem(f, this);
                        LODItems.Add(lod); // Append to LODItems array
                        break;
                    default:
                        f.SkipBlock();
                        break;
                }
                token = f.ReadItem();
            } // while token

            // Checks for required member variables: 
            // Name not required.
            // Image1Name, Image1sName, and Image2Name initialized as MSTS defaults.
            if (LODItems.Count == 0) throw new STFException(f, "Missing LODItems");

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
                            lod.CutoffRadius = float.Parse(reader.GetAttribute("CutoffRadius"));
                            lod.MipMapLevelOfDetailBias = float.Parse(reader.GetAttribute("MipMapLevelOfDetailBias"));
                            lod.AlphaBlendEnable = bool.Parse(reader.GetAttribute("AlphaBlendEnable"));
                            lod.AlphaTestEnable = bool.Parse(reader.GetAttribute("AlphaTestEnable"));
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
/*
        /// <summary>
        /// TrProfile constructor from XML profile file (uses XMLDocument)
        /// </summary>
        public TrProfile(string filespec) 
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filespec);
            XmlElement root = doc.DocumentElement;

            Console.WriteLine(); // Terminate pending line
            uint depth = 0;
            VisitElement(root, ref depth); // Traverse tree starting at root
        } // end TrProfile(filename) constructor

        private void VisitElement(XmlElement element, ref uint depth)
        {
            for (uint i = 0; i < depth; i++) Console.Write("  "); // Indent
            Console.Write("{0}[{1}]", element.Name, element.ChildNodes.Count); // ReportInformation
            XmlAttributeCollection attributes = element.Attributes;
            switch (element.Name)
            {
                case "TrProfile":
                    ParseTrProfile(attributes);
                    break;
                case "LODItem":
                    break;
                case "Polyline":
                    break;
                case "Vertex":
                    break;
                default:
                    break;
            }
            Console.WriteLine();

            if (!element.HasChildNodes) return;

            depth++; // Increase depth when going down
            foreach (XmlElement child in element.ChildNodes) // Recurse each child
            {
                VisitElement(child, ref depth);
            }
            depth--; // Decrease depth when exiting
        } // end VisitElement
*/
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
            lod.AlphaTestEnable = false;
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
            lod.AlphaTestEnable = false;
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
            lod.AlphaTestEnable = false;
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
        /*
                public void SaveAsXML(string filename)
                {
                    // Create a new XML document
                    XmlDocument xmlDoc = new XmlDocument();

                    // Create and append a root element
                    XmlElement rootElem = xmlDoc.CreateElement("TrProfile");
                    xmlDoc.AppendChild(rootElem);
                    // Add root member variables as attributes
                    AddAttrib(xmlDoc, rootElem, "Name", this.Name);
                    AddAttrib(xmlDoc, rootElem, "Image1Name", this.Image1Name);
                    AddAttrib(xmlDoc, rootElem, "Image1sName", this.Image1sName);
                    AddAttrib(xmlDoc, rootElem, "Image2Name", this.Image2Name);

                    // Add child LOD elements
                    foreach (LODItem lod in LODItems)
                    {
                        // Create and append a child LOD element
                        XmlElement lodElement = xmlDoc.CreateElement("LODItem");
                        rootElem.AppendChild(lodElement);
                        // Add LOD member variables as attributes
                        AddAttrib(xmlDoc, lodElement, "Name", lod.Name);
                        AddAttrib(xmlDoc, lodElement, "CutoffRadius", lod.CutoffRadius.ToString());
                        AddAttrib(xmlDoc, lodElement, "MipMapLevelOfDetailBias",
                            lod.MipMapLevelOfDetailBias.ToString());
                        AddAttrib(xmlDoc, lodElement, "AlphaBlendEnable",
                            lod.AlphaBlendEnable.ToString());
                        AddAttrib(xmlDoc, lodElement, "AlphaTestEnable",
                            lod.AlphaTestEnable.ToString());

                        // Add child polyline elements
                        foreach (Polyline pl in lod.Polylines)
                        {
                            // Create and add a child polyline element
                            XmlElement plElement = xmlDoc.CreateElement("Polyline");
                            lodElement.AppendChild(plElement);

                            // Add Polyline member variables as attributes
                            AddAttrib(xmlDoc, plElement, "Name", pl.Name);
                            AddAttrib(xmlDoc, plElement, "DeltaTexCoord", string.Format("{0} {1}",
                                pl.DeltaTexCoord.X, pl.DeltaTexCoord.Y));

                            // Add child vertex elements
                            uint vIndex = 0;
                            foreach (Vertex v in pl.Vertices)
                            {
                                // Create and add a child vertex element
                                XmlElement vElement = xmlDoc.CreateElement("Vertex");
                                plElement.AppendChild(vElement);

                                // Add vertex member variables as attributes
                                AddAttrib(xmlDoc, vElement, "Position", string.Format("{0} {1} {2}",
                                    v.Position.X, v.Position.Y, v.Position.Z));
                                AddAttrib(xmlDoc, vElement, "Normal", string.Format("{0} {1} {2}",
                                    v.Normal.X, v.Normal.Y, v.Normal.Z));
                                AddAttrib(xmlDoc, vElement, "TexCoord", string.Format("{0} {1}",
                                    v.TexCoord.X, v.TexCoord.Y));

                                vIndex++;
                            }
                        } // end foreach pl
                    } // end foreach lod

                    FileInfo xmlFile = new FileInfo(filename);
                    using (StreamWriter stream = xmlFile.CreateText())
                    {
                        stream.Write(xmlDoc.OuterXml);
                    }
                } // end SaveAsXML

                private void ParseTrProfile(XmlAttributeCollection attributes)
                {
                    string name = attributes["Name"].Value;
                    uint numLODItems = uint.Parse(attributes["NumLODItems"].Value);
                    string image1Name = attributes["Image1Name"].Value;
                    string image1sName = attributes["Image1sName"].Value;
                    string image2Name = attributes["Image2Name"].Value;
                    //string foo = attributes["foo"].Value; // This leads to a NullReferenceException
                } // end ParseTrProfile

                void AddAttrib(XmlDocument xmlDoc, XmlElement xmlElem, string attribName, string attribValue)
                {
                    XmlAttribute xmlAttrib = xmlDoc.CreateAttribute(attribName);
                    xmlAttrib.Value = attribValue;
                    xmlElem.Attributes.Append(xmlAttrib);
                } // end Attrib()
*/
    } // end TrProfile

    public class LODItem
    {
        public ArrayList Polylines = new ArrayList();  // Array of arrays of vertices 
        
        public string Name;                            // e.g., "Rail sides"
        public float CutoffRadius;                     // Distance beyond which LOD is not seen
        public float MipMapLevelOfDetailBias;
        public bool AlphaBlendEnable;
        public bool AlphaTestEnable;

        /// <summary>
        /// LODITem constructor (default & XML)
        /// </summary>
        public LODItem(string name)
        {
            Name = name;
        } // end LODItem() constructor

        /// <summary>
        /// LODITem constructor (DAT)
        /// </summary>
        public LODItem(STFReader f, TrProfile parent)
        {
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                switch (token)
                {
                    case "Name":
                        Name = f.ReadItemBlock(null);
                        break;
                    case "CutoffRadius":
                        CutoffRadius = f.ReadFloatBlock(STFReader.UNITS.Any, null);
                        break;
                    case "MipMapLevelOfDetailBias":
                        MipMapLevelOfDetailBias = f.ReadFloatBlock(STFReader.UNITS.Any, null);
                        break;
                    case "AlphaBlendEnable":
                        AlphaBlendEnable = f.ReadBoolBlock(true);
                        break;
                    case "AlphaTestEnable":
                        AlphaTestEnable = f.ReadBoolBlock(true);
                        break;
                    case "Polyline":
                        Polyline pl = new Polyline(f);
                        Polylines.Add(pl); // Append to Polylines array
                        parent.Accum(pl.Vertices.Count);
                        break;
                    default:
                        f.SkipBlock();
                        break;
                }
                token = f.ReadItem();
            } // while token

            // Checks for required member variables:
            // Name not required.
            if (CutoffRadius == 0) throw new STFException(f, "Missing CutoffRadius");
            // MipMapLevelOfDetail bias initializes to 0.
            // AlphaBlendEnable initializes to false.
            // AlphaTestEnable initializes to false.
            if (Polylines.Count == 0) throw new STFException(f, "Missing Polylines");

        } // end LODItem() constructor
    } // end LODItem

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
        public Polyline(STFReader f)
        {
            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                switch (token)
                {
                    case "Name":
                        Name = f.ReadItemBlock(null);
                        break;
                    case "DeltaTexCoord":
                        f.MustMatch("(");
                        DeltaTexCoord.X = f.ReadFloat(STFReader.UNITS.Any, null);
                        DeltaTexCoord.Y = f.ReadFloat(STFReader.UNITS.Any, null);
                        f.SkipRestOfBlock();
                        break;
                    case "Vertex":
                        Vertex v = new Vertex(f);
                        Vertices.Add(v); // Append to Vertices array
                        break;
                    default:
                        f.SkipBlock();
                        break;
                }
                token = f.ReadItem();
            } // while token

            // Checks for required member variables: 
            // Name not required.
            if (DeltaTexCoord == Vector2.Zero) throw new STFException(f, "Missing DeltaTexCoord");
            if (Vertices.Count == 0) throw new STFException(f, "Missing Vertices");
        } // end Polyline() constructor
    } // end Polyline

    public struct Vertex
    {
        public Vector3 Position;                           // Position vector (x, y, z)
        public Vector3 Normal;                             // Normal vector (nx, ny, nz)
        public Vector2 TexCoord;                           // Texture coordinate (u, v)
/*
        public Vertex()
        {
            Position = new Vector3();
            Normal = new Vector3();
            TexCoord = new Vector2(); 
        }
*/
        // Vertex constructor (default)
        public Vertex(float x, float y, float z, float nx, float ny, float nz, float u, float v)
        {
            Position = new Vector3(x, y, z);
            Normal = new Vector3(nx, ny, nz);
            TexCoord = new Vector2(u, v);
        } // end Vertex() constructor

        // Vertex constructor (DAT)
        public Vertex(STFReader f)
        {
            Position = new Vector3();
            Normal = new Vector3();
            TexCoord = new Vector2();            

            f.MustMatch("(");
            string token = f.ReadItem();
            while (token != ")")
            {
                if (token == "") throw new STFException(f, "Missing )");
                switch (token)
                {
                    case "Position":
                        f.MustMatch("(");
                        Position.X = f.ReadFloat(STFReader.UNITS.Any, null);
                        Position.Y = f.ReadFloat(STFReader.UNITS.Any, null);
                        Position.Z = 0.0f;
                        f.SkipRestOfBlock();
                        break;
                    case "Normal":
                        f.MustMatch("(");
                        Normal.X = f.ReadFloat(STFReader.UNITS.Any, null);
                        Normal.Y = f.ReadFloat(STFReader.UNITS.Any, null);
                        Normal.Z = f.ReadFloat(STFReader.UNITS.Any, null);
                        f.SkipRestOfBlock();
                        break;
                    case "TexCoord":
                        f.MustMatch("(");
                        TexCoord.X = f.ReadFloat(STFReader.UNITS.Any, null);
                        TexCoord.Y = f.ReadFloat(STFReader.UNITS.Any, null);
                        f.SkipRestOfBlock();
                        break;
                    default:
                        f.SkipBlock();
                        break;
                }
                token = f.ReadItem();
            } // while token

            // Checks for required member variables
            // No way to check for missing Position.
            if (Normal == Vector3.Zero) throw new STFException(f, "Improper Normal");
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
        public int DrawIndex;       // Used by Draw to determine which primitive to draw.
        public Vector3 XNAEnd;      // Location of termination-of-section (as opposed to root)
        public float ObjectRadius;  // Radius of bounding sphere
        public Vector3 MSTSLODCenter; // Center of bounding sphere
        public struct GridItem
        {
            public uint VertexOrigin;// Start index for first vertex in LOD
            public uint VertexLength;// Number of vertices in LOD
            public uint IndexOrigin; // Start index for first triangle in LOD
            public uint IndexLength; // Number of triangle vertex indicies in LOD
            //public float CutoffRadius; // Distance beyond which LOD is not seen
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
                IndexBuffer = new IndexBuffer(graphicsDevice, sizeof(short) * NumIndices, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                IndexBuffer.SetData<short>(TriangleListIndices);
            }
        }
        #endregion
    }
    #endregion
}
