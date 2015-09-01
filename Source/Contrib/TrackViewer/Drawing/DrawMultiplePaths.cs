using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

using Orts.Formats.Msts;
using ORTS.Menu;
using ORTS.TrackViewer.Editing;

namespace ORTS.TrackViewer.Drawing
{
    /// <summary>
    /// Class to Draw multiple paths (all coming from MSTS .pat files). 
    /// No editing, just plain single-color drawing.
    /// Paths will be drawn in slightly different colors to make it (a bit) easier to distinguish between them.
    /// </summary>
    public class DrawMultiplePaths
    {
        
        /// <summary>For each path name, store the full file name of the .pat file</summary>
        Dictionary<string, string> fullPathNames;
        /// <summary>The paths that have already been loaded (.pat file has been read and parsed)</summary>
        Dictionary<string, Trainpath> loadedPaths;
        /// <summary>For each trainpath we have it is own DrawPath (each with their own color)</summary>
        Dictionary<Trainpath, DrawPath> drawPaths;
        /// <summary>List of trainpaths that have been selected</summary>
        List<Trainpath> selectedTrainpaths;
    
        private TrackDB trackDB;
        private TrackSectionsFile tsectionDat;

        /// <summary>
        /// Constructor
        /// </summary>
        public DrawMultiplePaths (RouteData routeData, Collection<Path> paths)
        {
            this.trackDB = routeData.TrackDB;
            this.tsectionDat = routeData.TsectionDat;
            fullPathNames = new Dictionary<string, string>();
            loadedPaths = new Dictionary<string, Trainpath>();
            selectedTrainpaths = new List<Trainpath>();
            drawPaths = new Dictionary<Trainpath, DrawPath>();

            foreach (Path path in paths)
            {
                string pathName = ORTS.TrackViewer.UserInterface.MenuControl.MakePathMenyEntryName(path);
                fullPathNames[pathName] = path.FilePath;
            }
        }

        /// <summary>
        /// Returns the list of all available path names (names to describe the path both to user as to program)
        /// </summary>
        public string[] PathNames()
        {
            return fullPathNames.Keys.ToArray();
        }

        /// <summary>
        /// Return the Trainpath given a pathName. This will load the path if it has not been loaded before.
        /// </summary>
        /// <param name="pathName">The name of the path</param>
        Trainpath TrainpathFromName(string pathName)
        {
            Trainpath newTrainpath;
            if (!loadedPaths.TryGetValue(pathName, out newTrainpath))
            {
                newTrainpath = new Trainpath(trackDB, tsectionDat, fullPathNames[pathName]);
                loadedPaths[pathName] = newTrainpath;
                drawPaths[newTrainpath] = new DrawPath(trackDB, tsectionDat);
            }
            return newTrainpath;

        }

        /// <summary>
        /// Select or deselect one of the paths (select to be drawn)
        /// </summary>
        /// <param name="pathName">Name identifying the path</param>
        /// <param name="enable">set the selection state</param>
        public void SetSelection(string pathName, bool enable)
        {
            Trainpath trainpath = TrainpathFromName(pathName);
            if (enable)
            {
                if (!selectedTrainpaths.Contains(trainpath))
                {
                    selectedTrainpaths.Add(trainpath);
                }
            }
            else
            {
                selectedTrainpaths.Remove(trainpath);
            }
            ReColorAll();
        }

        /// <summary>
        /// Clear all selected trainpaths: no paths should be drawn now.
        /// </summary>
        public void ClearAll()
        {
            selectedTrainpaths.Clear();
            ReColorAll();
        }

        /// <summary>
        /// Recalculate and store the colors of all paths.
        /// Coloring is done automatically depending on the order and amount of selected paths.
        /// </summary>
        void ReColorAll()
        {
            int count = selectedTrainpaths.Count;
            for (int index = 0; index < count; index++)
            {
                ColorScheme shadedColor = DrawColors.ShadeColor(DrawColors.otherPathsReferenceColor, index, count);
                Trainpath trainpath = selectedTrainpaths[index];
                drawPaths[trainpath].colorSchemeMain = shadedColor;
                drawPaths[trainpath].colorSchemeSiding = shadedColor;
            }
        }
 
        /// <summary>
        /// Draws the paths that have been selected
        /// </summary>
        /// <param name="drawArea">Area to draw upon</param>
        public void Draw(DrawArea drawArea)
        {
            foreach (Trainpath trainpath in selectedTrainpaths)
            {
                drawPaths[trainpath].Draw(drawArea, trainpath.FirstNode);
            }
        }

        /// <summary>
        /// Return the color used for drawing the the path when selected, or null when not selected
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public Color? ColorOf(string pathName)
        {
            Trainpath trainPath;
            loadedPaths.TryGetValue(pathName, out trainPath);
            if (trainPath == null || !selectedTrainpaths.Contains(trainPath))
            {
                return null;
            }
            return drawPaths[trainPath].colorSchemeMain.TrackStraight;
        }
    }
}
