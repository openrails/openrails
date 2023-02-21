// COPYRIGHT 2014, 2018 by the Open Rails project.
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
using System.Linq;
using System.Text;
using Orts.Formats.Msts;
using ORTS.Common;
using ORTS.TrackViewer.Properties;
using Orts.Simulation;

namespace ORTS.TrackViewer.Drawing
{
    #region DrawableTrackItem (base class)
    /// <summary>
    /// This class represents all track items that are defined in the TrackDatabase (also for road) in a way that allows
    /// us to draw them in trackviewer. This is the base class, all real track items are supposed to be subclasses (because
    /// each has its own texture or symbol to draw, and also its own name.
    /// </summary>
    public abstract class DrawableTrackItem
    {
        /// <summary>WorldLocation of the track item</summary>
        public WorldLocation WorldLocation { get; set; }
        /// <summary>Short description, name of the type of item</summary>
        public string Description { get; set; }
        /// <summary>Index of the original item (TrItemId)</summary>
        public uint Index { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        protected DrawableTrackItem(TrItem originalTrItem)
        {
            this.Index = originalTrItem.TrItemId;
            this.WorldLocation = new WorldLocation(originalTrItem.TileX, originalTrItem.TileZ, originalTrItem.X, originalTrItem.Y, originalTrItem.Z);
            this.Description = "unknown";
        }

        /// <summary>
        /// Factory method. This will return a proper subclass of DrawableTrackItem that represents the correct type of track item.
        /// </summary>
        /// <param name="originalTrItem">The original track item that needs to be represented while drawing</param>
        /// <returns>A drawable trackitem, with proper subclass</returns>
        public static DrawableTrackItem CreateDrawableTrItem(TrItem originalTrItem)
        {
            if (originalTrItem is SignalItem)     { return new DrawableSignalItem(originalTrItem); }
            if (originalTrItem is PlatformItem)   { return new DrawablePlatformItem(originalTrItem); }
            if (originalTrItem is SidingItem)     { return new DrawableSidingItem(originalTrItem); }
            if (originalTrItem is SpeedPostItem)  { return new DrawableSpeedPostItem(originalTrItem); }
            if (originalTrItem is HazzardItem)    { return new DrawableHazardItem(originalTrItem); }
            if (originalTrItem is PickupItem)     { return new DrawablePickupItem(originalTrItem); }
            if (originalTrItem is LevelCrItem)    { return new DrawableLevelCrItem(originalTrItem); }
            if (originalTrItem is SoundRegionItem){ return new DrawableSoundRegionItem(originalTrItem); }
            if (originalTrItem is RoadLevelCrItem){ return new DrawableRoadLevelCrItem(originalTrItem); }
            if (originalTrItem is CarSpawnerItem) { return new DrawableCarSpawnerItem(originalTrItem); }
            if (originalTrItem is CrossoverItem)  { return new DrawableCrossoverItem(originalTrItem); }
            if (originalTrItem is EventItem)      { return new DrawableEvent(originalTrItem); }
            return new DrawableEmptyItem(originalTrItem);
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        /// <returns>true if the item has been drawn</returns>
        internal abstract bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways);
    }
    #endregion

    #region DrawableSignalItem
    /// <summary>
    /// Represents a drawable signal
    /// </summary>
    class DrawableSignalItem : DrawableTrackItem
    {
        /// <summary>direction (forward or backward the signal relative to the direction of the track</summary>
        private Traveller.TravellerDirection direction;

        /// <summary>angle to draw the signal at</summary>
        private float angle;

        /// <summary>Is it a normal signal</summary>
        private bool isNormal;

        /// <summary>Signal Type, which is a name to cross-reference to sigcfg file</summary>
        private string signalType;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableSignalItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            this.Description = "signal";
            this.isNormal = true; // default value
            SignalItem originalSignalItem = originalTrItem as SignalItem;
            this.direction = originalSignalItem.Direction == 0 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward;
            this.signalType = originalSignalItem.SignalType;
        }

        /// <summary>
        /// Find the angle that the signal needs to be drawn at
        /// </summary>
        /// <param name="tsectionDat">Database with track sections</param>
        /// <param name="trackDB">Database with tracks</param>
        /// <param name="tn">TrackNode on which the signal actually is</param>
        public void FindAngle(TrackSectionsFile tsectionDat, TrackDB trackDB, TrackNode tn)
        {
            this.angle = 0;
            try
            {
                Traveller signalTraveller = new Traveller(tsectionDat, trackDB.TrackNodes, tn,
                    this.WorldLocation.TileX, this.WorldLocation.TileZ,
                    this.WorldLocation.Location.X, this.WorldLocation.Location.Z,
                    this.direction);
                this.angle = signalTraveller.RotY;

                // Shift signal a little bit to be able to distinguish backfacing from normal facing
                Microsoft.Xna.Framework.Vector3 shiftedLocation = this.WorldLocation.Location + 
                    0.0001f * new Microsoft.Xna.Framework.Vector3((float) Math.Cos(this.angle), 0f, -(float) Math.Sin(this.angle));
                this.WorldLocation = new WorldLocation(this.WorldLocation.TileX, this.WorldLocation.TileZ, shiftedLocation );
            }
            catch { }
        }

        /// <summary>
        /// Determine if the current signal is a normal signal (i.s.a. distance, ...)
        /// </summary>
        /// <param name="sigcfgFile">The signal configuration file</param>
        public void DetermineIfNormal(SignalConfigurationFile sigcfgFile)
        {
            isNormal = true; //default
            if (sigcfgFile == null)
            {   // if no sigcfgFile is available, just keep default
                return;
            }
            if (sigcfgFile.SignalTypes.ContainsKey(signalType))
            {
                isNormal = sigcfgFile.SignalTypes[signalType].Function == SignalFunction.NORMAL;
            }
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            if (   drawAlways 
                || Properties.Settings.Default.showAllSignals
                || (Properties.Settings.Default.showSignals && isNormal)
                )
            {
                float size = 7f; // in meters
                int minPixelSize = 5;
                drawArea.DrawTexture(this.WorldLocation, "signal" + colors.NameExtension, size, minPixelSize, colors.None, this.angle);
                return true;
            }
            return false;
        }
    }
    #endregion

    #region DrawableLevelCrItem
    /// <summary>
    /// Represents a drawable level crossing
    /// </summary>
    class DrawableLevelCrItem : DrawableTrackItem
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableLevelCrItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "crossing";
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            if (Properties.Settings.Default.showCrossings || drawAlways)
            {
                drawArea.DrawTexture(this.WorldLocation, "disc", 6f, 0, colors.Crossing);
                return true;
            }
            return false;
        }
    }
    #endregion

    #region DrawableRoadLevelCrItem
    /// <summary>
    /// Represents a drawable level crossing on a road
    /// </summary>
    class DrawableRoadLevelCrItem : DrawableTrackItem
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableRoadLevelCrItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "crossing (road)";
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            if (Properties.Settings.Default.showRoadCrossings || drawAlways)
            {
                drawArea.DrawTexture(this.WorldLocation, "disc", 4f, 0, colors.RoadCrossing);
                return true;
            }
            return false;
        }
    }
    #endregion

    #region DrawableSidingItem
    /// <summary>
    /// Represents a drawable siding item
    /// </summary>
    class DrawableSidingItem : DrawableTrackItem
    {
        private string itemName;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableSidingItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "siding";
            this.itemName = (originalTrItem as SidingItem).ItemName;
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            bool returnValue;
            returnValue = false;
            if (Properties.Settings.Default.showSidingMarkers || drawAlways)
            {
                drawArea.DrawTexture(this.WorldLocation, "disc", 6f, 0, colors.Siding);
                returnValue = true;
            }
            if (Properties.Settings.Default.showSidingNames || drawAlways)
            {
                drawArea.DrawExpandingString(this.WorldLocation, this.itemName);
                returnValue = true;
            }
            return returnValue;
        }
    }
    #endregion

    #region DrawablePlatformItem
    /// <summary>
    /// Represents a drawable platform item
    /// </summary>
    class DrawablePlatformItem : DrawableTrackItem
    {
        private string itemName;
        private string stationName;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawablePlatformItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "platform";
            PlatformItem platform = originalTrItem as PlatformItem;
            this.itemName = platform.ItemName;
            this.stationName = platform.Station;
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            bool returnValue;
            returnValue = false;
            if (Properties.Settings.Default.showPlatformMarkers || drawAlways)
            {
                float size = 9f; // in meters
                int minPixelSize = 7;
                drawArea.DrawTexture(this.WorldLocation, "platform" + colors.NameExtension, size, minPixelSize);
                returnValue = true;
            }
            if (Properties.Settings.Default.showPlatformNames)
            {
                drawArea.DrawExpandingString(this.WorldLocation, this.itemName);
                returnValue = true;
            }
            if (Properties.Settings.Default.showStationNames || 
                (drawAlways && !Properties.Settings.Default.showPlatformNames) )
            {   // if drawAlways and no station nor platform name requested, then also show station
                drawArea.DrawExpandingString(this.WorldLocation, this.stationName);
                returnValue = true;
            }
            
            return returnValue;
        }
    }
    #endregion

    #region DrawblePickupItem
    /// <summary>
    /// Represents a drawable pickup item
    /// </summary>
    class DrawablePickupItem : DrawableTrackItem
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawablePickupItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "pickup";
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            if (Properties.Settings.Default.showPickups || drawAlways)
            {
                float size = 9f; // in meters
                int minPixelSize = 5;
                drawArea.DrawTexture(this.WorldLocation, "pickup" + colors.NameExtension, size, minPixelSize);
                return true;
            }
            return false;
        }
    }
    #endregion

    #region DrawableHazardItem
    /// <summary>
    /// Represents a drawable hazard item
    /// </summary>
    class DrawableHazardItem : DrawableTrackItem
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableHazardItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "hazard";
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            if (Properties.Settings.Default.showHazards || drawAlways)
            {
                float size = 9f; // in meters
                int minPixelSize = 7;
                drawArea.DrawTexture(this.WorldLocation, "hazard" + colors.NameExtension, size, minPixelSize);
                return true;
            }
            return false;
        }
    }
    #endregion

    #region DrawableCarSpawnerItem
    /// <summary>
    /// Represents a drawable car spawner
    /// </summary>
    class DrawableCarSpawnerItem : DrawableTrackItem
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableCarSpawnerItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "carspawner";
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            if (Properties.Settings.Default.showCarSpawners || drawAlways)
            {
                float size = 9f; // in meters
                int minPixelSize = 5;
                drawArea.DrawTexture(this.WorldLocation, "carspawner" + colors.NameExtension, size, minPixelSize);
                return true;
            }
            return false;
        }
    }
    #endregion

    #region DrawableEmptyItem
    /// <summary>
    /// Represents a drawable empty item (so not much to draw then)
    /// </summary>
    class DrawableEmptyItem : DrawableTrackItem
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableEmptyItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "empty";
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            // draw nothing, but do allow it to be mentioned if it exists.
            return true;
        }
    }
    #endregion

    #region DrawableCorssoverItem
    /// <summary>
    /// Represents a drawable cross-over
    /// </summary>
    class DrawableCrossoverItem : DrawableTrackItem
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableCrossoverItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "crossover";
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            if (Properties.Settings.Default.showCrossovers || drawAlways)
            {
                drawArea.DrawTexture(this.WorldLocation, "disc", 3f, 0, colors.EndNode);
                return true;
            }
            return false;
        }
    }
    #endregion

    #region DrawableSpeedPostItem
    /// <summary>
    /// Represents a drawable speedpost (or milepost)
    /// </summary>
    class DrawableSpeedPostItem : DrawableTrackItem
    {
        private SpeedPostItem originalItem;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableSpeedPostItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "speedpost";
            originalItem = originalTrItem as SpeedPostItem;
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            bool returnValue;
            returnValue = false;
            if (originalItem.IsLimit && (Properties.Settings.Default.showSpeedLimits || drawAlways))
            {
                drawArea.DrawTexture(this.WorldLocation, "disc", 6f, 0, colors.Speedpost);
                string speed = originalItem.SpeedInd.ToString(System.Globalization.CultureInfo.CurrentCulture);
                drawArea.DrawExpandingString(this.WorldLocation, speed);
                returnValue = true;
            }
            if (originalItem.IsMilePost && (Properties.Settings.Default.showMileposts || drawAlways))
            {
                drawArea.DrawTexture(this.WorldLocation, "disc", 6f, 0, colors.Speedpost);
                string distance = originalItem.SpeedInd.ToString(System.Globalization.CultureInfo.CurrentCulture);
                drawArea.DrawExpandingString(this.WorldLocation, distance);
                returnValue = true;
            }

            return returnValue;
        }
    }
    #endregion

    #region DrawableSoundRegionItem
    /// <summary>
    /// Represents a drawable sound region
    /// </summary>
    class DrawableSoundRegionItem : DrawableTrackItem
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableSoundRegionItem(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "soundregion";
        }

        /// <summary>
        /// Draw a single track item
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            if (Properties.Settings.Default.showSoundRegions || drawAlways)
            {
                float size = 4f; // in meters
                int minPixelSize = 5;
                drawArea.DrawTexture(this.WorldLocation, "sound" + colors.NameExtension, size, minPixelSize);
                return true;
            }
            return false;
        }
    }
    #endregion

    #region DrawableEvent
    /// <summary>
    /// Represents a drawable event
    /// </summary>
    class DrawableEvent : DrawableTrackItem
    {
        private string ItemName;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="originalTrItem">The original track item that we are representing for drawing</param>
        public DrawableEvent(TrItem originalTrItem)
            : base(originalTrItem)
        {
            Description = "event";
            ItemName = originalTrItem.ItemName;
        }

        /// <summary>
        /// Draw an event
        /// </summary>
        /// <param name="drawArea">The area to draw upon</param>
        /// <param name="colors">The colorscheme to use</param>
        /// <param name="drawAlways">Do we need to draw anyway, independent of settings?</param>
        internal override bool Draw(DrawArea drawArea, ColorScheme colors, bool drawAlways)
        {
            bool returnValue = false;
            if (String.IsNullOrEmpty(Properties.Settings.Default.CurrentActivityName) ||
                (ItemName.StartsWith(Properties.Settings.Default.CurrentActivityName + ":")))
            {
                if (Properties.Settings.Default.showEvents || drawAlways)
                {
                    drawArea.DrawTexture(WorldLocation, "disc", 6f, 0, colors.Event);
                    returnValue = true;
                }
                if (Properties.Settings.Default.showEventNames || drawAlways)
                {
                    drawArea.DrawExpandingString(this.WorldLocation, ItemName);
                    returnValue = true;
                }
            }
            return returnValue;
        }
    }
    #endregion
}
