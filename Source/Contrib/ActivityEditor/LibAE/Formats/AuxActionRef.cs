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

using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

using MSTS.Formats;
using MSTS.Parsers;
using ORTS.Common;
using ORTS.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LibAE.Formats
{
    public class ActionFactory<T>
    {
        private ActionFactory() { }

        static readonly Dictionary<string, Func<T>> _dict = new Dictionary<string, Func<T>>();
        static readonly Dictionary<string, string> _shortDescr = new Dictionary<string, string>();
        static readonly Dictionary<string, string> _longDescr = new Dictionary<string, string>();

        public static T Create(string name)
        {
            Func<T> constructor = null;
            if (_dict.TryGetValue(name, out constructor))
                return constructor();

            throw new ArgumentException("No type registered for this name");
        }

        public static void Register(string name, Func<T> ctor, string myShort, string myLong)
        {
            if (_dict.ContainsKey(name))
                return;
            _dict.Add(name, ctor);
            _shortDescr.Add(name, myShort);
            _longDescr.Add(name, myLong);
        }

        public static int Count()
        {
            return _dict.Count();
        }

        public static string GetShortDescr(int indx)
        {
            if (indx < _dict.Count())
            {
                var element = _dict.ElementAt(indx);
                return _shortDescr[element.Key];
            }
            return "";
        }

        public static string GetLongDescr(int indx)
        {
            if (indx < _dict.Count())
            {
                var element = _dict.ElementAt(indx);
                return _longDescr[element.Key];
            }
            return "";
        }

        public static string GetKey(string shortDescr)
        {
            foreach (var action in _shortDescr)
            {
                if (action.Value == shortDescr)
                    return action.Key;
            }
            return null;
        }
    }

    //================================================================================================//
    /// <summary>
    /// ActionContainer
    /// class to manage the available Actions for Editor and RunActivity
    /// </summary>

    public class ActionContainer
    {
        [JsonIgnore]
        public List<AuxActionRef> SpecAuxActions;          // Actions To Do during activity, like WP with specific location
        [JsonProperty("GenActions")]
        public List<KeyValuePair<string,AuxActionRef>> GenAuxActions;          // Action To Do during activity, without specific location
        [JsonIgnore]
        protected List<KeyValuePair<System.Type, AuxActionRef>> GenFunctions;
        [JsonIgnore]
        public List<string> AvailableActions;      //  List of current actions available as string
        public List<string> ActionsDescription;
        public List<string> UsedActions;

        public ActionContainer()
        {
            SpecAuxActions = new List<AuxActionRef>();
            GenAuxActions = new List<KeyValuePair<string, AuxActionRef>>();
            GenFunctions = new List<KeyValuePair<System.Type, AuxActionRef>>();
            AvailableActions = new List<string>();
            ActionsDescription = new List<string>();
            UsedActions = new List<string>();
            //
            //  Register all action, avoiding to give the same index
            AuxActionHorn.Register("AuxActionHorn");
            LoadAvailableActions();
        }

        public List<AuxActionRef> GetGenAuxActions()
        {
            List<AuxActionRef> listAction = new List<AuxActionRef>();
            foreach (var action in GenAuxActions)
            {
                listAction.Add(action.Value);
            }
            return listAction;
        }

        public int GetCountAvailableAction()
        {
            return ActionFactory<AuxActionRef>.Count();
        }

        public string GetShortDescr(int cnt)
        {
            return ActionFactory<AuxActionRef>.GetShortDescr(cnt);
        }

        public string GetLongDescr(int cnt)
        {
            return ActionFactory<AuxActionRef>.GetLongDescr(cnt);
        }

        public bool AddGenAction(string name)
        {
            string keyShort = ActionFactory<AuxActionRef>.GetKey(name);
            if (keyShort == null)
                return false;
            KeyValuePair<string, AuxActionRef>? record = HasGenAction(keyShort);
            if (record != null)
                return false;
            AuxActionRef action = ActionFactory<AuxActionRef>.Create(keyShort);
            record = new KeyValuePair<string, AuxActionRef>(keyShort, action);
            GenAuxActions.Add((KeyValuePair<string, AuxActionRef>)record);
            UsedActions.Add(name);
            return true;
        }

        public bool RemoveGenAction(string name)
        {
            string keyShort = ActionFactory<AuxActionRef>.GetKey(name);
            if (keyShort == null)
                return false;
            KeyValuePair<string, AuxActionRef>? record = HasGenAction(keyShort);
            if (record == null)
                return false;
            GenAuxActions.Remove((KeyValuePair<string, AuxActionRef>)record);
            return true;
        }

        public KeyValuePair<string, AuxActionRef>? HasGenAction(string name)
        {
            foreach (var action in GenAuxActions)
            {
                string info = (string)action.Key;
                if (info == name)
                {
                    return action;
                }
            }
            return null;
        }

        public AuxActionRef GetAction(int indx)
        {
            string shortDescr = ActionFactory<AuxActionRef>.GetShortDescr(indx);
            string keyShort = ActionFactory<AuxActionRef>.GetKey(shortDescr);
            KeyValuePair<string, AuxActionRef>? actionPair = HasGenAction(keyShort);
            if (actionPair != null)
            {
                KeyValuePair<string, AuxActionRef> info = (KeyValuePair<string, AuxActionRef>)actionPair;
                return (AuxActionRef)info.Value;
            }
            return null;
        }

        public void LoadAvailableActions()
        {
            if (GetCountAvailableAction() > 0)
            {
                for (int cnt = 0; cnt < GetCountAvailableAction(); cnt++)
                {
                    AvailableActions.Add(GetShortDescr(cnt));
                    ActionsDescription.Add(GetLongDescr(cnt));
                }
            }
        }

        public bool RemoveGenAction(int indx)
        {
            string shortDescr = UsedActions[indx];
            if (RemoveGenAction(shortDescr))
            {
                UsedActions.RemoveAt(indx);
                return true;
            }
            return false;
        }
    }

    //================================================================================================//
    /// <summary>
    /// AuxActionRef
    /// The main class to define Auxiliary Action through the editor and used by RunActivity
    /// </summary>

    public class AuxActionRef
    {
        [JsonProperty("Location")]
        WorldLocation? Location;
        [JsonProperty("RequiredSpeed")]
        public float RequiredSpeedMpS;
        [JsonProperty("IsGeneric")]
        public bool IsGeneric { get; set; }
        [JsonProperty("EndSignalIndex")]
        public int EndSignalIndex { get; protected set; }
        [JsonProperty("ActionType")]
        public AUX_ACTION ActionType;
        [JsonProperty("Delay")]
        public int Delay;
        [JsonProperty("RequiredDistance")]
        public float RequiredDistance;


        public enum AUX_ACTION
        {
            WAITING_POINT,
            SOUND_HORN,
            SIGNAL_DELAY,
            SIGNAL_DELEGATE,
            CONTROLLED_START,
            NONE
        }

        //================================================================================================//
        /// <summary>
        /// AIAuxActionsRef: Generic Constructor
        /// The specific datas are used to fired the Action.
        /// </summary>

        public AuxActionRef(float requiredSpeedMpS, WorldLocation? location, AUX_ACTION actionType = AuxActionRef.AUX_ACTION.NONE)
        {
            Location = location;
            if (Location == null)
                IsGeneric = true;
            else
                IsGeneric = false;
            RequiredSpeedMpS = requiredSpeedMpS;
            EndSignalIndex = -1;
            ActionType = actionType;
        }

        public AuxActionRef(AUX_ACTION actionType = AuxActionRef.AUX_ACTION.NONE)
        {
            RequiredSpeedMpS = 0;
            IsGeneric = true;
            EndSignalIndex = -1;
            Location = null;
            ActionType = actionType;
        }
    }

    /// <summary>
    /// AuxActionWP
    /// Only used inside the editor (no multiple inheritance)
    /// </summary>

    public class AuxActionWP : AuxActionRef
    {
        public AuxActionWP(float requiredSpeedMpS, WorldLocation location, int delay = 5):
            base(requiredSpeedMpS, location, AuxActionRef.AUX_ACTION.WAITING_POINT)
        {
            Delay = delay;
        }

        public static void Register(string key)
        {
        }

    }

    //  AuxActionHorn is always a Generic Action, no need to specify a location
    public class AuxActionHorn : AuxActionRef
    {
        public AuxActionHorn(float requiredSpeedMpS, WorldLocation? location, int delay = 2) :
            base(requiredSpeedMpS, null, AuxActionRef.AUX_ACTION.SOUND_HORN)
        {
            Delay = delay;
            IsGeneric = true;
            RequiredDistance = 100f;
        }

        public static void Register(string key)
        {
            ActionFactory<AuxActionRef>.Register(key, () => new AuxActionHorn(-1, null, 0),
                "Horn at Level Crossing", "Generic Action used to sound AI Horn when it reach a Level cross");
        }

        public void SaveProperties(AuxActionHorn action)
        {
            Delay = action.Delay;
            RequiredDistance = action.RequiredDistance;
        }
    }

    public class AuxActionSigDelegate : AuxActionRef
    {
        public AuxActionSigDelegate (float requiredSpeedMpS, WorldLocation location, int delay = 10) :
            base(requiredSpeedMpS, location, AuxActionRef.AUX_ACTION.SIGNAL_DELEGATE)
        {
            Delay = delay;
        }

        public static void Register(string key)
        {
        }

    }
}
