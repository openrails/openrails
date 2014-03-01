// COPYRIGHT 2014 by the Open Rails project.
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
//
// Based on RunActivity\ORTS\InputSettings.cs
// But we copy some of the code because we need different commands here
//
// Here all possible key commands are defined (enumerated) as well as linked to a specific key or key combination.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using ORTS.Settings;

namespace ORTS.TrackViewer.UserInterface
{

    public enum TVUserCommands
    {
        ZoomIn,
        ZoomOut,
        ZoomReset,
        ZoomToTile,
        ShiftLeft,
        ShiftRight,
        ShiftUp,
        ShiftDown,
        ShiftToLocation,
        ExtendPath,
        ExtendPathFull,
        ReducePath,
        ReducePathFull,
        DebugDumpKeymap,
        Quit,
        Debug,
        ToggleShowSidings,
        ToggleShowSidingNames,
        ToggleShowPlatforms,
        ToggleShowPlatformNames,
        ToggleShowSignals,
        ToggleShowPATFile,
        ToggleShowTrainpath,
        ToggleShowSpeedLimits,
        ToggleShowMilePosts,
        MouseZoomSlow,
        EditorRedo,
        EditorUndo,
    }

    /* not used, because ORTS.Settings.KeyModifiers is used
    [Flags]
    public enum KeyModifierss
    {
        None = 0,
        Shift = 1,
        Control = 2,
        Alt = 4
    }
    */

    public static class TVInputSettings
    {
        public static UserCommandInput[] Commands = new UserCommandInput[Enum.GetNames(typeof(TVUserCommands)).Length];
        
        public static readonly string[] KeyboardLayout = new[] {
            "[01 ]   [3B ][3C ][3D ][3E ]   [3F ][40 ][41 ][42 ]   [43 ][44 ][57 ][58 ]   [37 ][46 ][11D]",
            "                                                                                            ",
            "[29 ][02 ][03 ][04 ][05 ][06 ][07 ][08 ][09 ][0A ][0B ][0C ][0D ][0E     ]   [52 ][47 ][49 ]",
            "[0F   ][10 ][11 ][12 ][13 ][14 ][15 ][16 ][17 ][18 ][19 ][1A ][1B ][2B   ]   [53 ][4F ][51 ]",
            "[3A     ][1E ][1F ][20 ][21 ][22 ][23 ][24 ][25 ][26 ][27 ][28 ][1C      ]                  ",
            "[2A       ][2C ][2D ][2E ][2F ][30 ][31 ][32 ][33 ][34 ][35 ][36         ]        [48 ]     ",
            "[1D   ][    ][38  ][39                          ][    ][    ][    ][1D   ]   [4B ][50 ][4D ]",
        };

        public static void SetDefaults()
        { 
            Commands[(int)TVUserCommands.ZoomIn]     = new ORTS.Settings.UserCommandKeyInput(0x0D);
            Commands[(int)TVUserCommands.ZoomOut]    = new ORTS.Settings.UserCommandKeyInput(0x0C);
            Commands[(int)TVUserCommands.ZoomReset]  = new ORTS.Settings.UserCommandKeyInput(0x13);
            Commands[(int)TVUserCommands.ZoomToTile] = new ORTS.Settings.UserCommandKeyInput(0x2C);
            Commands[(int)TVUserCommands.ShiftLeft]  = new ORTS.Settings.UserCommandKeyInput(0x1E);
            Commands[(int)TVUserCommands.ShiftRight] = new ORTS.Settings.UserCommandKeyInput(0x20);
            Commands[(int)TVUserCommands.ShiftUp]    = new ORTS.Settings.UserCommandKeyInput(0x11);
            Commands[(int)TVUserCommands.ShiftDown]  = new ORTS.Settings.UserCommandKeyInput(0x1F);
            Commands[(int)TVUserCommands.ShiftToLocation] = new ORTS.Settings.UserCommandKeyInput(0x2E);

            Commands[(int)TVUserCommands.ToggleShowSpeedLimits] = new ORTS.Settings.UserCommandKeyInput(0x3F);
            Commands[(int)TVUserCommands.ToggleShowMilePosts] = new ORTS.Settings.UserCommandKeyInput(0x3F, ORTS.Settings.KeyModifiers.Shift);
            Commands[(int)TVUserCommands.ToggleShowSignals] = new ORTS.Settings.UserCommandKeyInput(0x41);
            Commands[(int)TVUserCommands.ToggleShowPlatforms] = new ORTS.Settings.UserCommandKeyInput(0x42);
            Commands[(int)TVUserCommands.ToggleShowPlatformNames] = new ORTS.Settings.UserCommandKeyInput(0x42, ORTS.Settings.KeyModifiers.Shift);
            Commands[(int)TVUserCommands.ToggleShowSidings] = new ORTS.Settings.UserCommandKeyInput(0x43);
            Commands[(int)TVUserCommands.ToggleShowSidingNames] = new ORTS.Settings.UserCommandKeyInput(0x43, ORTS.Settings.KeyModifiers.Shift);
            Commands[(int)TVUserCommands.ToggleShowTrainpath] = new ORTS.Settings.UserCommandKeyInput(0x57);
            Commands[(int)TVUserCommands.ToggleShowPATFile] = new ORTS.Settings.UserCommandKeyInput(0x57, ORTS.Settings.KeyModifiers.Shift);
            

            Commands[(int)TVUserCommands.ExtendPath] = new ORTS.Settings.UserCommandKeyInput(0x49);
            Commands[(int)TVUserCommands.ExtendPathFull] = new ORTS.Settings.UserCommandKeyInput(0x49, ORTS.Settings.KeyModifiers.Shift);
            Commands[(int)TVUserCommands.ReducePath] = new ORTS.Settings.UserCommandKeyInput(0x51);
            Commands[(int)TVUserCommands.ReducePathFull] = new ORTS.Settings.UserCommandKeyInput(0x51, ORTS.Settings.KeyModifiers.Shift);

            Commands[(int)TVUserCommands.Quit]       = new ORTS.Settings.UserCommandKeyInput(0x10);
            Commands[(int)TVUserCommands.Debug]      = new ORTS.Settings.UserCommandKeyInput(0x34);
            Commands[(int)TVUserCommands.DebugDumpKeymap] = new ORTS.Settings.UserCommandKeyInput(0x3B, ORTS.Settings.KeyModifiers.Alt);

            Commands[(int)TVUserCommands.MouseZoomSlow] = new UserCommandModifierInput(Settings.KeyModifiers.Shift);

            Commands[(int)TVUserCommands.EditorUndo] = new ORTS.Settings.UserCommandKeyInput(0x2C, ORTS.Settings.KeyModifiers.Control);
            Commands[(int)TVUserCommands.EditorRedo] = new ORTS.Settings.UserCommandKeyInput(0x15, ORTS.Settings.KeyModifiers.Control);
        }

    }
}
