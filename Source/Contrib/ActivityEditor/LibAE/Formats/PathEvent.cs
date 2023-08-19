// COPYRIGHT 2013, 2014 by the Open Rails project.
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

/// This module ...
/// 
/// Author: Stéfan Paitoni
/// Updates : 
/// 

using System;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using Microsoft.Xna.Framework;
using MSTS;
using ORTS;
using LibAE;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orts.Formats.OR;
using ORTS.Common;


namespace LibAE.Formats
{
    #region Activity_def
    [Flags]
    enum TypeEvent
    {
        ACTIVITY_EVENT = 0,
        ACTIVITY_START = 1,
        ACTIVITY_STOP = 2,
        ACTIVITY_WAIT = 3
    };
#endregion

    #region PathEventItem


    public class PathEventItem : GlobalItem
    {
        [JsonProperty("nameEvent")]
        public string nameEvent { get; set; }
        [JsonProperty("nameVisible")]
        public bool nameVisible;
        [JsonProperty("typeEvent")]
        public int typeEvent;


        public PathEventItem(TypeEditor interfaceType)
        {
            typeItem = (int)TypeEvent.ACTIVITY_EVENT;
            alignEdition(interfaceType, null);
        }

        public override void alignEdition(TypeEditor interfaceType, GlobalItem ownParent)
        {
            if (interfaceType == TypeEditor.ACTIVITY)
            {
                setMovable();
                setLineSnap();
                setEditable();
                setActEdit();
            }
        }

        public void setName(int info)
        {
        }

        public virtual Icon getIcon() { return null; }
    }

    public class ActStartItem : PathEventItem
    {
        Icon StartIcon;
        public ActStartItem(TypeEditor interfaceType)
            : base(interfaceType)
        {
            Stream st;
            Assembly a = Assembly.GetExecutingAssembly();

            typeItem = (int)TypeEvent.ACTIVITY_START;
            st = a.GetManifestResourceStream("LibAE.Icon.Start.ico");
            StartIcon = new System.Drawing.Icon(st);
 
        }

        public void setNameStart(int info)
        {
            nameEvent = "start" + info;
        }

        public override void configCoord(MSTSCoord coord)
        {
            base.configCoord(coord);
            typeItem = (int)TypeItem.ACTIVITY_ITEM;
            nameVisible = false;
        }

        public override void Update(MSTSCoord coord)
        {
            base.configCoord(coord);
        }

        public override Icon getIcon() { return StartIcon; }

    }

    public class ActStopItem : PathEventItem
    {
        Icon StopIcon;

        public ActStopItem(TypeEditor interfaceType)
            : base(interfaceType)
        {
            Stream st;
            Assembly a = Assembly.GetExecutingAssembly();

            typeItem = (int)TypeEvent.ACTIVITY_STOP;
           st = a.GetManifestResourceStream("LibAE.Icon.Stop.ico");
            StopIcon = new System.Drawing.Icon(st);

        }

        public void setNameStop(int info)
        {
            nameEvent = "stop" + info;
        }

        public override void configCoord(MSTSCoord coord)
        {
            base.configCoord(coord);
            typeItem = (int)TypeItem.ACTIVITY_ITEM;
            nameVisible = false;
        }

        public override void Update(MSTSCoord coord)
        {
            base.configCoord(coord);
        }

        public override Icon getIcon() { return StopIcon; }

    }

    public class ActWaitItem : PathEventItem
    {
        Icon WaitIcon;

        public ActWaitItem(TypeEditor interfaceType)
            : base(interfaceType)
        {
            Stream st;
            Assembly a = Assembly.GetExecutingAssembly();

            typeItem = (int)TypeEvent.ACTIVITY_WAIT;
            st = a.GetManifestResourceStream("LibAE.Icon.Wait.ico");
            WaitIcon = new System.Drawing.Icon(st);

        }

        public void setNameWait(int info)
        {
            nameEvent = "wait" + info;
        }

        public override void configCoord(MSTSCoord coord)
        {
            base.configCoord(coord);
            typeItem = (int)TypeItem.ACTIVITY_ITEM;
            nameVisible = false;
        }

        public override void Update(MSTSCoord coord)
        {
            base.configCoord(coord);
        }

        public override Icon getIcon() { return WaitIcon; }

    }


    #endregion


}