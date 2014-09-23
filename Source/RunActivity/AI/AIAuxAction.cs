// COPYRIGHT 2013 by the Open Rails project.
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

/* AI
 * 
 * Contains code to initialize and control AI trains.
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using MSTS.Formats;
using MSTS.Parsers;
using ORTS.Common;
using ORTS.Viewer3D.Popups;
#if NEW_ACTION
namespace ORTS
{
    #region AuxActionsContainer
    //================================================================================================//
    /// <summary>
    /// AuxActionsContainer
    /// Used to manage all the action ref object.
    /// </summary>

    public class AuxActionsContainer
    {
        public List<AIAuxActionsRef> SpecAuxActions;          // Actions To Do during activity, like WP with specific location
        public List<AIAuxActionsRef> GenAuxActions;          // Action To Do during activity, without specific location
        public Train.DistanceTravelledActions genRequiredActions = new Train.DistanceTravelledActions(); // distance travelled Generic action list for AITrain
        public Train.DistanceTravelledActions specRequiredActions = new Train.DistanceTravelledActions();

        protected List<KeyValuePair<System.Type, AIAuxActionsRef>> GenFunctions;
        Train ThisTrain;

        public AuxActionsContainer(Train thisTrain)
        {
            SpecAuxActions = new List<AIAuxActionsRef>();
            GenAuxActions = new List<AIAuxActionsRef>();
            GenFunctions = new List<KeyValuePair<System.Type, AIAuxActionsRef>>();
            if (thisTrain is AITrain)
            {
                SetGenAuxActions((AITrain)thisTrain);
            }
            ThisTrain = thisTrain;
        }

        public AuxActionsContainer(Train thisTrain, BinaryReader inf)
        {
            SpecAuxActions = new List<AIAuxActionsRef>();
            GenAuxActions = new List<AIAuxActionsRef>();
            GenFunctions = new List<KeyValuePair<System.Type, AIAuxActionsRef>>();
            if (thisTrain is AITrain)
            {
                SetGenAuxActions((AITrain)thisTrain);
            }
            ThisTrain = thisTrain;
            int cntAuxActionSpec = inf.ReadInt32();
            for (int idx = 0; idx < cntAuxActionSpec; idx++)
            {
                int cntAction = inf.ReadInt32();
                AIAuxActionsRef action;
                string actionRef = inf.ReadString();
                AIAuxActionsRef.AI_AUX_ACTION nextAction = (AIAuxActionsRef.AI_AUX_ACTION)Enum.Parse(typeof(AIAuxActionsRef.AI_AUX_ACTION), actionRef);
                switch (nextAction)
                {
                    case AIAuxActionsRef.AI_AUX_ACTION.WAITING_POINT:
                        action = new AIActionWPRef(thisTrain, inf);
                        SpecAuxActions.Add(action);
                        break;
                    case AIAuxActionsRef.AI_AUX_ACTION.SOUND_HORN:
                        action = new AIActionHornRef(thisTrain, inf);
                        SpecAuxActions.Add(action);
                        break;
                    default:
                        break;
                }
            }
        }

        public void Save(BinaryWriter outf, int currentClock)
        {
            int cnt = 0;
            outf.Write(SpecAuxActions.Count);
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "SaveAIAuxActions, count :" + SpecAuxActions.Count + 
                "Position in file: " + outf.BaseStream.Position + "\n");
#endif
            if (ThisTrain is AITrain && ((AITrain)ThisTrain).MovementState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION)
            {
                AITrain aiTrain = ThisTrain as AITrain;
                if (aiTrain.nextActionInfo != null &&
                    aiTrain.nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.AUX_ACTION &&
                    (AuxActionWPItem)aiTrain.nextActionInfo != null)
                // WP is running
                {
                    int remainingDelay = ((AuxActionWPItem)aiTrain.nextActionInfo).ActualDepart - currentClock;
                    ((AIActionWPRef)SpecAuxActions[0]).SetDelay(remainingDelay);
                }
            }
            foreach (var action in SpecAuxActions)
            {

                action.save(outf, cnt);
                cnt++;
            }
        }
#if false
            int cnt = 0;
            outf.Write(AuxActions.Count);
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "SaveAIAuxActions, count :" + AuxActions.Count + 
                "Position in file: " + outf.BaseStream.Position + "\n");
#endif

            foreach (var action in AuxActions)
            {
                action.save(outf, cnt);
                cnt++;
            }

#endif
        protected void SetGenAuxActions(AITrain thisTrain)  //  Add here the new Generic Action
        {
#if WITH_GEN_ACTION
            //AIActionSignalRef actionSignal = new AIActionSignalRef(thisTrain, 0f, 0f, 0, 0, 0, 0);
            //actionSignal.SetDelay(10);
            //actionSignal.IsGeneric = true;
            //GenAuxActions.Add(actionSignal);
            ////GenFunctions.Add(actionSignal.GetCallFunction());

            AIActionHornRef actionHorn = new AIActionHornRef(thisTrain, 0f, 0f, 0, 0, 0, 0);
            actionHorn.SetDelay(2);
            actionHorn.IsGeneric = true;
            GenAuxActions.Add(actionHorn);
            GenFunctions.Add(actionHorn.GetCallFunction());

            AIActionSteamRef actionSteam = new AIActionSteamRef(thisTrain, 0f, 0f, 0, 0, 0, 0);
            actionSteam.SetDelay(2);
            actionSteam.IsGeneric = true;
            GenAuxActions.Add(actionSteam);
            GenFunctions.Add(actionSteam.GetCallFunction());
#endif
        }

        //public bool CheckGenActions(System.Type typeSource, float rearDist, float frontDist, WorldLocation location, uint trackNodeIndex)
        public bool CheckGenActions(System.Type typeSource, WorldLocation location, params object[] list)
        {
            if (ThisTrain is AITrain)
            {
                AITrain aiTrain = ThisTrain as AITrain;
                foreach (var fonction in GenFunctions)
                {
                    if (typeSource == fonction.Key)   //  Caller object is a LevelCrossing
                    {
#if WITH_PATH_DEBUG
                        File.AppendAllText(@"C:\temp\checkpath.txt", "GenFunctions registered for train " + ThisTrain.Number + "\n");
#endif
                        AIAuxActionsRef called = fonction.Value;
                        if (called.HasAction(ThisTrain.Number, location))
                            return false;
                        AIActionItem newAction = called.CheckGenActions(location, aiTrain, list);
                        if (newAction != null)
                            genRequiredActions.InsertAction(newAction);
                    }
                }
            }
            return false;
        }

        public void RemoveGenActions(System.Type typeSource, WorldLocation location)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "Remove GenAction for train " + ThisTrain.Number + "\n");
#endif
            if (!(ThisTrain is AITrain))
            {
                AITrain aiTrain = ThisTrain as AITrain;
                foreach (var fonction in GenFunctions)
                {
                    if (typeSource == fonction.Key)   //  Caller object is a LevelCrossing
                    {
                        AIAuxActionsRef called = fonction.Value;
                        if (called.HasAction(ThisTrain.Number, location))
                        {
                            called.RemoveReference(aiTrain.Number, location);
                        }
                    }
                }
            }
        }

        public void RemoveSpecReqAction(AuxActionItem thisAction)
        {
            if (thisAction.CanRemove(ThisTrain))
            {
                Train.DistanceTravelledItem thisItem = thisAction;
                specRequiredActions.Remove(thisItem);
            }
        }

        public void ProcessGenAction(AITrain thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            if (genRequiredActions.Count <= 0 || !(ThisTrain is AITrain))
                return;
            AITrain aiTrain = ThisTrain as AITrain;
            List<Train.DistanceTravelledItem> itemList = new List<Train.DistanceTravelledItem>();
            foreach (var action in genRequiredActions)
            {
                AIActionItem actionItem = action as AIActionItem; 
                if (actionItem.RequiredDistance <= ThisTrain.DistanceTravelledM)
                {
                    itemList.Add(actionItem);
                }
            }
            foreach (var action in itemList)
            {
                AIActionItem actionItem = action as AIActionItem;
                actionItem.ProcessAction(aiTrain, presentTime, elapsedClockSeconds, movementState);
            }
        }

        public AITrain.AI_MOVEMENT_STATE ProcessSpecAction(AITrain thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            AITrain.AI_MOVEMENT_STATE MvtState = movementState;
            if (specRequiredActions.Count <= 0 || !(ThisTrain is AITrain))
                return MvtState;
            AITrain aiTrain = ThisTrain as AITrain;
            List<Train.DistanceTravelledItem> itemList = new List<Train.DistanceTravelledItem>();
            foreach (var action in specRequiredActions)
            {
                AIActionItem actionItem = action as AIActionItem;
                if (actionItem.RequiredDistance <= ThisTrain.DistanceTravelledM)
                {
                    itemList.Add(actionItem);
                }
            }
            foreach (var action in itemList)
            {
                AITrain.AI_MOVEMENT_STATE tmpMvt;
                AIActionItem actionItem = action as AIActionItem;
                tmpMvt = actionItem.ProcessAction(aiTrain, presentTime, elapsedClockSeconds, movementState);
                if (tmpMvt != movementState)
                    MvtState = tmpMvt;  //  Try to avoid override of changed state of previous action
            }
            return MvtState;
        }

        public void Remove(AuxActionItem action)
        {
            bool ret = false;
            if (action.ActionRef.IsGeneric)
            {
                if (genRequiredActions.Count > 0)
                    ret = genRequiredActions.Remove(action);
                if (!ret && specRequiredActions.Count > 0)
                {
                    if (action.ActionRef.CallFreeAction(ThisTrain))
                        RemoveSpecReqAction(action);
                }
            }
            if (CountSpec() > 0)
                RemoveAt(0);
            if (ThisTrain is AITrain)
                ((AITrain)ThisTrain).ResetActions(true);
        }

        public void RemoveAt(int posit)
        {
            SpecAuxActions.RemoveAt(posit);
        }

        public AIAuxActionsRef this[int key]
        {
            get
            {
                if (key >= SpecAuxActions.Count)
                    return null;
                return SpecAuxActions[key];
            }
            set
            {
                
            }
        }

        public int Count()
        {
            return CountSpec();
        }

        public int CountSpec()
        {
            return SpecAuxActions.Count;
        }

        //================================================================================================//
        //  SPA:    Added for use with new AIActionItems
        /// <summary>
        /// Create Specific Auxiliary Action, like WP
        /// <\summary>
        public void SetAuxAction(Train thisTrain)
        {
            if (SpecAuxActions.Count <= 0)
                return;
            AIAuxActionsRef thisAction;
            while (SpecAuxActions.Count > 0)
            {
                thisAction = SpecAuxActions[0];

                if (thisAction.SubrouteIndex > thisTrain.TCRoute.activeSubpath)
                {
                    return;
                }
                if (thisAction.SubrouteIndex == thisTrain.TCRoute.activeSubpath) 
                    break;
                else
                {
                    SpecAuxActions.RemoveAt(0);
                    if (SpecAuxActions.Count <= 0) return;
                }
            }

            thisAction = SpecAuxActions[0];
            bool validAction = false;
            float[] distancesM;
            while (!validAction)
            {
                if (thisTrain is AITrain)
                {
                    AITrain aiTrain = thisTrain as AITrain;
                    distancesM = thisAction.CalculateDistancesToNextAction(aiTrain, ((AITrain)aiTrain).TrainMaxSpeedMpS, true);
                }
                else
                {
                    distancesM = thisAction.CalculateDistancesToNextAction(thisTrain, thisTrain.SpeedMpS, true);
                }
                if (distancesM[0] < 0f)
                {
                    SpecAuxActions.RemoveAt(0);
                    if (SpecAuxActions.Count == 0)
                    {
                        return;
                    }

                    thisAction = SpecAuxActions[0];
                    if (thisAction.SubrouteIndex > thisTrain.TCRoute.activeSubpath) return;
                }
                else
                {
                    validAction = true;
                    AIActionItem newAction = SpecAuxActions[0].Handler(distancesM[1], SpecAuxActions[0].RequiredSpeedMpS, distancesM[0], thisTrain.DistanceTravelledM);
                    if (newAction != null)
                    {
                        if (thisTrain is AITrain && newAction is AuxActionWPItem)   // Put only the WP for AI into the requiredAction, other are on the container
                        {
                            thisTrain.requiredActions.InsertAction(newAction);
                            ((AITrain)thisTrain).nextActionInfo = newAction;
                        }
                        else
                            specRequiredActions.InsertAction(newAction);
                    }
                }
            }
        }

        //public void SetAuxAction(Train thisTrain)
        //{
        //    if (SpecAuxActions.Count <= 0)
        //        return;
        //    while (SpecAuxActions.Count > 0)
        //    {
        //        AIAuxActionsRef thistAction = SpecAuxActions[0];

        //        if (thistAction.SubrouteIndex > thisTrain.TCRoute.activeSubpath)
        //        {
        //            return;
        //        }
        //        if (thistAction.SubrouteIndex == thisTrain.TCRoute.activeSubpath) break;
        //        else
        //        {
        //            SpecAuxActions.RemoveAt(0);
        //            if (SpecAuxActions.Count <= 0) return;
        //        }
        //    }

        //    AIAuxActionsRef thisAction = SpecAuxActions[0];
        //    bool validAction = false;

        //}

        public AIAuxActionsRef GetGenericAuxAction(AIAuxActionsRef.AI_AUX_ACTION typeReq)
        {
            return GenAuxActions.GetGenericAuxAction(typeReq);
        }

        public void Add(AIAuxActionsRef action)
        {
            SpecAuxActions.Add(action);
        }
    }

    public static class AIAuxActionRefList
    {
        public static AIAuxActionsRef GetGenericAuxAction(this List<AIAuxActionsRef> list, AIAuxActionsRef.AI_AUX_ACTION typeReq)
        {
            foreach (var AuxAction in list)
            {
                AIAuxActionsRef info = AuxAction;
                if (AuxAction.NextAction == typeReq)
                    return AuxAction;
            }
            return null;
        }

        public static List<AIAuxActionsRef> GetValidActions(this List<AIAuxActionsRef> list, AITrain thisTrain, float presentSpeedMpS, bool reschedule)
        {
            List<AIAuxActionsRef> listAction = new List<AIAuxActionsRef>();
            if (list == null)
                return listAction;
            List<AIAuxActionsRef> previousList = list;
            bool getFirst = false;
            try
            {
                foreach (AIAuxActionsRef action in previousList)
                {
                    float[] distancesM = action.CalculateDistancesToNextAction(thisTrain, presentSpeedMpS, reschedule);
                    if (!action.IsGeneric && !getFirst)
                    {
                        if (distancesM[0] < 0f)
                        {
                            previousList.RemoveAt(0);
                            continue;
                        }
                        listAction.Insert(0, action);
                        getFirst = true;
                    }
                    if (distancesM[0] == 0f || distancesM[0] == float.MaxValue)
                    {
                        listAction.Add(action);
                    }
                }
            }
            catch
            {
            }

            return listAction;
        }
    }


    #endregion
    #region AuxActionRef

    //================================================================================================//
    /// <summary>
    /// AIAuxActionsRef
    /// info used to figure out one auxiliary action along the route.  It's a reference data, not a run data.
    /// </summary>

    public class AIAuxActionsRef
    {
        public float RequiredSpeedMpS;
        public float RequiredDistance;
        public int SubrouteIndex;
        public int RouteIndex;
        public int TCSectionIndex;
        public int Direction;
        protected int TriggerDistance = 0;
        public bool LinkedAuxAction = false;
        public bool IsGeneric { get; set; }
        protected List<KeyValuePair<int, WorldLocation>> AskingTrain;
        public int EndSignalIndex { get; protected set; }
        public SignalObject SignalReferenced = null;

        public enum AI_AUX_ACTION
        {
            WAITING_POINT,
            SOUND_HORN,
            SIGNAL_DELAY,
            SIGNAL_DELEGATE,
            NONE
        }

        public AI_AUX_ACTION NextAction = AI_AUX_ACTION.NONE;

        //================================================================================================//
        /// <summary>
        /// AIAuxActionsRef: Generic Constructor
        /// The specific datas are used to fired the Action.
        /// </summary>

        public AIAuxActionsRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
        {
            RequiredDistance = distance;
            RequiredSpeedMpS = requiredSpeedMpS;
            SubrouteIndex = subrouteIdx;
            RouteIndex = routeIdx;
            TCSectionIndex = sectionIdx;
            Direction = dir;
            AskingTrain = new List<KeyValuePair<int, WorldLocation>>();
            IsGeneric = false;
            EndSignalIndex = -1;
        }

        public AIAuxActionsRef(Train thisTrain, BinaryReader inf)
        {
            RequiredSpeedMpS = inf.ReadSingle();
            RequiredDistance = inf.ReadSingle();
            SubrouteIndex = inf.ReadInt32();
            RouteIndex = inf.ReadInt32();
            TCSectionIndex = inf.ReadInt32();
            Direction = inf.ReadInt32();
            TriggerDistance = inf.ReadInt32();
            IsGeneric = inf.ReadBoolean();
            AskingTrain = new List<KeyValuePair<int, WorldLocation>>();
            EndSignalIndex = -1;
        }

        public virtual KeyValuePair<System.Type, AIAuxActionsRef> GetCallFunction()
        {
            return default(KeyValuePair<System.Type, AIAuxActionsRef>);
        }

        //================================================================================================//
        /// <summary>
        /// Handler
        /// Like a fabric, if other informations are needed, please define specific function that can be called on the new object
        /// </summary>


        public virtual AIActionItem Handler(float distance, float speed, float activateDistance, float insertedDistance)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                info = new AuxActionItem(distance, speed, activateDistance, insertedDistance,
                                this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
            }
            return info;
        }

        //================================================================================================//
        /// <summary>
        /// CalculateDistancesToNextAction
        /// PLease, don't use the default function, redefine it.
        /// </summary>

        public virtual float[] CalculateDistancesToNextAction(Train thisTrain, float presentSpeedMpS, bool reschedule)
        {
            float[] distancesM = new float[2];
            distancesM[1] = 0.0f;
            distancesM[0] = thisTrain.PresentPosition[0].DistanceTravelledM;

            return (distancesM);
        }

        public virtual float[] GetActivationDistances(Train thisTrain, WorldLocation location)
        {
            float[] distancesM = new float[2];
            distancesM[1] = float.MaxValue;
            distancesM[0] = float.MaxValue;

            return (distancesM);
        }
        //================================================================================================//
        //
        // Save
        //

        public virtual void save(BinaryWriter outf, int cnt)
        {
            outf.Write(cnt);
            string info = NextAction.ToString();
            outf.Write(NextAction.ToString());
            outf.Write(RequiredSpeedMpS);
            outf.Write(RequiredDistance);
            outf.Write(SubrouteIndex);
            outf.Write(RouteIndex);
            outf.Write(TCSectionIndex);
            outf.Write(Direction);
            outf.Write(TriggerDistance);
            outf.Write(IsGeneric);
            //if (LinkedAuxAction != null)
            //    outf.Write(LinkedAuxAction.NextAction.ToString());
            //else
            //    outf.Write(AI_AUX_ACTION.NONE.ToString());
        }

                //================================================================================================//
        //
        // Restore
        //

        public void Register(int trainNumber, WorldLocation location)
        {
            AskingTrain.Add(new KeyValuePair<int, WorldLocation>(trainNumber, location));
        }

        //public bool CheckGenActions(System.Type typeSource, float rearDist, float frontDist, WorldLocation location, uint trackNodeIndex)
        public virtual AIActionItem CheckGenActions(WorldLocation location, AITrain thisTrain, params object[] list)
        {
            return null;
        }

        public bool HasAction(int trainNumber, WorldLocation location)
        {
            foreach (var info in AskingTrain)
            {
                int number = (int)info.Key;
                if (number == trainNumber)
                {
                    WorldLocation locationRegistered = info.Value;
                    if (location == locationRegistered)
                        return true;
                }
            }
            return false;
        }

        public virtual bool CallFreeAction(Train ThisTrain)
        {
            return true;
        }

        public void RemoveReference(int trainNumber, WorldLocation location)
        {
            //var info = default (KeyValuePair<int, WorldLocation>);
            foreach (var info in AskingTrain)
            {
                int number = (int)info.Key;
                if (number == trainNumber)
                {
                    WorldLocation locationRegistered = info.Value;
                    if (location == locationRegistered)
                    {
                        AskingTrain.Remove(info);
                        break;
                    }
                }
            }
        }

        public void SetSignalObject(SignalObject signal)
        {
            SignalReferenced = signal;
        }
    }

    //================================================================================================//
    /// <summary>
    /// AIActionWPRef
    /// info used to figure out a Waiting Point along the route.
    /// </summary>

    public class AIActionWPRef : AIAuxActionsRef
    {
        int Delay;
        AuxActionWPItem keepIt = null;

        public AIActionWPRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "New AIAuxActionRef (WP) for train " + thisTrain.Number +
                " Required Distance " + distance + ", speed " + requiredSpeedMpS + ", and dir " + dir + "\n");
            File.AppendAllText(@"C:\temp\checkpath.txt", "\t\tSection id: " + subrouteIdx + "." + routeIdx + "." + sectionIdx 
                + "\n"); 
#endif
            NextAction = AI_AUX_ACTION.WAITING_POINT;

            //  SPA :   Code temporary disabled 
            //if (thisTrain.Simulator.Settings.AuxActionEnabled)
            //{
            //    AIActionHornRef actionHorn = new AIActionHornRef(thisTrain, distance, 99999f, 0, SubrouteIndex, RouteIndex, Direction);
            //    actionHorn.SetDelay(2);
            //    LinkedAuxAction = actionHorn;
            //}
            //else
                //LinkedAuxAction = null;
        }

        public AIActionWPRef(Train thisTrain, BinaryReader inf)
            : base (thisTrain, inf)
        {
            Delay = inf.ReadInt32();
            NextAction = AI_AUX_ACTION.WAITING_POINT;
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tRestore one WPAuxAction" +
                "Position in file: " + inf.BaseStream.Position +
                " type Action: " + NextAction.ToString() +
                " Delay: " + Delay + "\n");
#endif
        }

        public override void save(BinaryWriter outf, int cnt)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tSave one WPAuxAction, count :" + cnt +
                "Position in file: " + outf.BaseStream.Position +
                " type Action: " + NextAction.ToString() +
                " Delay: " + Delay + "\n");
#endif
            base.save(outf, cnt);
            outf.Write(Delay);
        }

        public override AIActionItem Handler(float distance, float speed, float activateDistance, float insertedDistance)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                LinkedAuxAction = true;
                info = new AuxActionWPItem(distance, speed, activateDistance, insertedDistance,
                               this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
                ((AuxActionWPItem)info).SetDelay(Delay);
                keepIt = (AuxActionWPItem)info;
#if WITH_PATH_DEBUG
                File.AppendAllText(@"C:\temp\checkpath.txt", "New action item, type WP with distance " + distance + ", speed " + speed + ", activate distance  " + activateDistance +
                    " and inserted distance " + insertedDistance + " (delay " + Delay + ")\n");
#endif
            }
            else if (LinkedAuxAction)
            {
                info = keepIt;
            }
            return (AIActionItem)info;
        }

        //================================================================================================//
        /// <summary>
        /// SetDelay
        /// To fullfill the waiting delay.
        /// </summary>

        public void SetDelay(int delay)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tDelay set to: " + delay + "\n");
#endif
            Delay = delay;
        }

        public override float[] CalculateDistancesToNextAction(Train thisTrain, float presentSpeedMpS, bool reschedule)
        {
            int thisSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - thisTrain.PresentPosition[0].TCOffset;

            // get action route index - if not found, return distances < 0

            int actionIndex0 = thisTrain.ValidRoute[0].GetRouteIndex(thisTrain.PresentPosition[0].TCSectionIndex, thisTrain.PresentPosition[0].RouteListIndex);
            float activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, TCSectionIndex, this.RequiredDistance, thisTrain.AITrainDirectionForward, thisTrain.signalRef);

            // if reschedule, use actual speed

            float triggerDistanceM = TriggerDistance;

            if (thisTrain is AITrain)
            {
                AITrain aiTrain = thisTrain as AITrain;
                if (reschedule)
                {
                    float firstPartTime = 0.0f;
                    float firstPartRangeM = 0.0f;
                    float secndPartRangeM = 0.0f;
                    float remainingRangeM = activateDistanceTravelledM - thisTrain.PresentPosition[0].DistanceTravelledM;

                    firstPartTime = presentSpeedMpS / (0.25f * aiTrain.MaxDecelMpSS);
                    firstPartRangeM = 0.25f * aiTrain.MaxDecelMpSS * (firstPartTime * firstPartTime);

                    if (firstPartRangeM < remainingRangeM && thisTrain.SpeedMpS < thisTrain.TrainMaxSpeedMpS) // if distance left and not at max speed
                    // split remaining distance based on relation between acceleration and deceleration
                    {
                        secndPartRangeM = (remainingRangeM - firstPartRangeM) * (2.0f * aiTrain.MaxDecelMpSS) / (aiTrain.MaxDecelMpSS + aiTrain.MaxAccelMpSS);
                    }

                    triggerDistanceM = activateDistanceTravelledM - (firstPartRangeM + secndPartRangeM);
                }
                else

                // use maximum speed
                {
                    float deltaTime = thisTrain.TrainMaxSpeedMpS / aiTrain.MaxDecelMpSS;
                    float brakingDistanceM = (thisTrain.TrainMaxSpeedMpS * deltaTime) + (0.5f * aiTrain.MaxDecelMpSS * deltaTime * deltaTime);
                    triggerDistanceM = activateDistanceTravelledM - brakingDistanceM;
                }
            }
            else
            {
                activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, TCSectionIndex, this.RequiredDistance, true, thisTrain.signalRef);
                triggerDistanceM = activateDistanceTravelledM;
            }

            float[] distancesM = new float[2];
            distancesM[1] = triggerDistanceM;
            if (activateDistanceTravelledM < thisTrain.PresentPosition[0].DistanceTravelledM &&
                thisTrain.PresentPosition[0].DistanceTravelledM - activateDistanceTravelledM < thisTrain.Length)
                activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM;
            distancesM[0] = activateDistanceTravelledM;

            return (distancesM);
        }
    }
    //================================================================================================//
    /// <summary>
    /// AIActionHornRef
    /// Start and Stop the horn
    /// </summary>

    public class AIActionHornRef : AIAuxActionsRef
    {
        int Delay;

        public AIActionHornRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir)
        {
            NextAction = AI_AUX_ACTION.SOUND_HORN;
        }

        public AIActionHornRef(Train thisTrain, BinaryReader inf)
            : base (thisTrain, inf)
        {
            Delay = inf.ReadInt32();
            NextAction = AI_AUX_ACTION.SOUND_HORN;
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tRestore one WPAuxAction" +
                "Position in file: " + inf.BaseStream.Position +
                " type Action: " + NextAction.ToString() +
                " Delay: " + Delay + "\n");
#endif
        }

        public override void save(BinaryWriter outf, int cnt)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tSave one HornAuxAction, count :" + cnt +
                "Position in file: " + outf.BaseStream.Position +
                " type Action: " + NextAction.ToString() + 
                " Delay: " + Delay + "\n");
#endif
            base.save(outf, cnt);
            outf.Write(Delay);
        }


        public override AIActionItem Handler(float distance, float speed, float activateDistance, float insertedDistance)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                LinkedAuxAction = true;
                info = new AuxActionHornItem(distance, speed, activateDistance, insertedDistance,
                               this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
                ((AuxActionHornItem)info).SetDelay(Delay);
            }
            return (AIActionItem)info;
        }

        //public bool CheckGenActions(System.Type typeSource, float rearDist, float frontDist, WorldLocation location, uint trackNodeIndex)
        public override AIActionItem CheckGenActions(WorldLocation location, AITrain thisTrain, params object[] list)
        {
            AIActionItem newAction = null;
            float rearDist = (float)list[0];
            float frontDist = (float)list[1];
            uint trackNodeIndex = (uint)list[2];
            float minDist = Math.Min(rearDist, frontDist);

            float[] distances = GetActivationDistances(thisTrain, location);
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "GenFunctions not yet defined for train:" + thisTrain.Number + 
                " Activation Distance: " + distances[0] + " & train distance: " + (-minDist) + "\n");
#endif
            if (distances[0] >= -minDist)   //  We call the handler to generate an actionRef
            {
                newAction = Handler(1f, thisTrain.SpeedMpS, 1f, 1f);
                Register(thisTrain.Number, location);
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "Caller registered for\n");
#endif
            }
            return newAction;
        }

        public override KeyValuePair<System.Type, AIAuxActionsRef> GetCallFunction()
        {
            System.Type managed = typeof(LevelCrossings);
            KeyValuePair<System.Type, AIAuxActionsRef> info = new KeyValuePair<System.Type, AIAuxActionsRef>(managed, this);
            return info;
        }

        //================================================================================================//
        /// <summary>
        /// SetDelay
        /// To fullfill the waiting delay.
        /// </summary>

        public void SetDelay(int delay)
        {
            Delay = delay;
        }

        //  Start horn whatever the speed.

        public override float[] CalculateDistancesToNextAction(Train thisTrain, float presentSpeedMpS, bool reschedule)
        {
            int thisSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - thisTrain.PresentPosition[0].TCOffset;

            // get action route index - if not found, return distances < 0

            int actionIndex0 = thisTrain.ValidRoute[0].GetRouteIndex(thisTrain.PresentPosition[0].TCSectionIndex, thisTrain.PresentPosition[0].RouteListIndex);
            float activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, TCSectionIndex, this.RequiredDistance, thisTrain.AITrainDirectionForward, thisTrain.signalRef);
            float[] distancesM = new float[2];
            distancesM[1] = activateDistanceTravelledM;
            distancesM[0] = activateDistanceTravelledM;

            return (distancesM);
        }

        //  SPA:    We use this fonction and not the one from Train in order to leave control to the AuxAction
        public override float[] GetActivationDistances(Train thisTrain, WorldLocation location)
        {
            float[] distancesM = new float[2];
            distancesM[0] = 100f;   //  Dès 100m
            distancesM[1] = 100f + thisTrain.Length;
            return (distancesM);
        }

    }

    //================================================================================================//
    /// <summary>
    /// AIActionSteamRef
    /// Used to start a steam engine when it is  an Train
    /// </summary>

    public class AIActionSteamRef : AIAuxActionsRef
    {
        int Delay;

        public AIActionSteamRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir)
        {
            NextAction = AI_AUX_ACTION.SOUND_HORN;
        }

        public AIActionSteamRef(Train thisTrain, BinaryReader inf)
            : base(thisTrain, inf)
        {
            Delay = inf.ReadInt32();
            NextAction = AI_AUX_ACTION.SOUND_HORN;
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tRestore one WPAuxAction" +
                "Position in file: " + inf.BaseStream.Position +
                " type Action: " + NextAction.ToString() +
                " Delay: " + Delay + "\n");
#endif
        }

        public override void save(BinaryWriter outf, int cnt)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tSave one HornAuxAction, count :" + cnt +
                "Position in file: " + outf.BaseStream.Position +
                " type Action: " + NextAction.ToString() + 
                " Delay: " + Delay + "\n");
#endif
            base.save(outf, cnt);
            outf.Write(Delay);
        }


        public override AIActionItem Handler(float distance, float speed, float activateDistance, float insertedDistance)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                LinkedAuxAction = true;
                info = new AuxActionSteamItem(distance, speed, activateDistance, insertedDistance,
                                            this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
                ((AuxActionSteamItem)info).SetDelay(Delay);
            }
            return (AIActionItem)info;
        }

        public override AIActionItem CheckGenActions(WorldLocation location, AITrain thisTrain, params object[] list)
        {
            AIActionItem newAction = null;
            float SpeedMps = (float)list[0];
            TrainCar locomotive = thisTrain.FindLeadLocomotive();
            if (!(locomotive is MSTSSteamLocomotive))
            {
                return null;
            }
            if (SpeedMps == 0)   //  We call the handler to generate an actionRef
            {
                newAction = Handler(1f, thisTrain.SpeedMpS, 1f, 1f);
                Register(thisTrain.Number, location);
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "Caller registered for\n");
#endif
            }
            return newAction;
        }

        public override KeyValuePair<System.Type, AIAuxActionsRef> GetCallFunction()
        {
            System.Type managed = typeof(AITrain);
            KeyValuePair<System.Type, AIAuxActionsRef> info = new KeyValuePair<System.Type, AIAuxActionsRef>(managed, this);
            return info;
        }

        //================================================================================================//
        /// <summary>
        /// SetDelay
        /// To fullfill the waiting delay.
        /// </summary>

        public void SetDelay(int delay)
        {
            Delay = delay;
        }

        //  Start horn whatever the speed.

        public override float[] CalculateDistancesToNextAction(Train thisTrain, float presentSpeedMpS, bool reschedule)
        {
            int thisSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - thisTrain.PresentPosition[0].TCOffset;

            // get action route index - if not found, return distances < 0

            int actionIndex0 = thisTrain.ValidRoute[0].GetRouteIndex(thisTrain.PresentPosition[0].TCSectionIndex, thisTrain.PresentPosition[0].RouteListIndex);
            float activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, TCSectionIndex, this.RequiredDistance, thisTrain.AITrainDirectionForward, thisTrain.signalRef);
            float[] distancesM = new float[2];
            distancesM[1] = activateDistanceTravelledM;
            distancesM[0] = activateDistanceTravelledM;

            return (distancesM);
        }

        //  SPA:    We use this fonction and not the one from Train in order to leave control to the AuxAction
        public override float[] GetActivationDistances(Train thisTrain, WorldLocation location)
        {
            float[] distancesM = new float[2];
            distancesM[0] = 0f;   //  Dès 100m
            distancesM[1] = 0f;
            return (distancesM);
        }

    }


    //================================================================================================//
    /// <summary>
    /// AIActionSignalRef
    /// A single Reference object used to add some delay before starting at a Signal
    /// </summary>

    public class AIActionSignalRef : AIAuxActionsRef
    {
        int Delay;

        public AIActionSignalRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir)
        {
            NextAction = AI_AUX_ACTION.SIGNAL_DELAY;
            IsGeneric = true;
        }

        public override AIActionItem Handler(float distance, float speed, float activateDistance, float insertedDistance)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                LinkedAuxAction = true;
                info = new AuxActionSignalItem(distance, speed, activateDistance, insertedDistance,
                                this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
                ((AuxActionSignalItem)info).SetDelay(Delay);
            }
            return (AIActionItem)info;
        }

        //================================================================================================//
        /// <summary>
        /// SetDelay
        /// To fullfill the waiting delay.
        /// </summary>

        public void SetDelay(int delay)
        {
            Delay = delay;
        }

        //  Start horn whatever the speed.

        public override float[] CalculateDistancesToNextAction(Train thisTrain, float presentSpeedMpS, bool reschedule)
        {
            float activateDistanceTravelledM = float.MaxValue;
            float[] distancesM = new float[2];

            if (presentSpeedMpS > 0f)
            {
            }
            else
            {
                activateDistanceTravelledM = 0f;
            }
            distancesM[1] = activateDistanceTravelledM;
            distancesM[0] = activateDistanceTravelledM;

            return (distancesM);
        }
    }
    //================================================================================================//
    /// <summary>
    /// AIActSigDelegateRef
    /// An action to delegate the Signal management from a WP
    /// </summary>

    public class AIActSigDelegateRef : AIAuxActionsRef
    {
        public int Delay;
        public float brakeSection;
        protected AuxActSigDelegate AssociatedItem = null;  //  In order to Unlock the signal when removing Action Reference

        public AIActSigDelegateRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir)
        {
            NextAction = AI_AUX_ACTION.SIGNAL_DELEGATE;
            IsGeneric = true;
            if (thisTrain is AITrain)
                brakeSection = 1;   //  Do not extend the available length for AI Train
            else
                brakeSection = distance;
        }

        public override bool CallFreeAction(Train thisTrain)
        {
            if (AssociatedItem != null && AssociatedItem.SignalReferenced != null)
            {
                if (AssociatedItem.locked)
                    AssociatedItem.SignalReferenced.UnlockForTrain(thisTrain.Number, thisTrain.TCRoute.activeSubpath);
                AssociatedItem.SignalReferenced = null;
                AssociatedItem = null;
                return true;
            }
            else if (AssociatedItem == null)
                return true;
            return false;
        }

        public override AIActionItem Handler(float distance, float speed, float activateDistance, float insertedDistance)
        {
            if (AssociatedItem != null)
                return null;
            AuxActSigDelegate info = new AuxActSigDelegate(distance, speed, activateDistance, insertedDistance,
                            this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
            AssociatedItem = info;  
            return (AIActionItem)info;
        }

        //  Start horn whatever the speed.

        public override float[] CalculateDistancesToNextAction(Train thisTrain, float presentSpeedMpS, bool reschedule)
        {
            int thisSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - thisTrain.PresentPosition[0].TCOffset;

            // get action route index - if not found, return distances < 0

            int actionIndex0 = thisTrain.ValidRoute[0].GetRouteIndex(thisTrain.PresentPosition[0].TCSectionIndex, thisTrain.PresentPosition[0].RouteListIndex);
            float activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, TCSectionIndex, this.RequiredDistance, thisTrain.AITrainDirectionForward, thisTrain.signalRef);

            // if reschedule, use actual speed

            float triggerDistanceM = TriggerDistance;

            //if (thisTrain is AITrain)
            //{
            //    AITrain aiTrain = thisTrain as AITrain;
            //    if (reschedule)
            //    {
            //        float firstPartTime = 0.0f;
            //        float firstPartRangeM = 0.0f;
            //        float secndPartRangeM = 0.0f;
            //        float remainingRangeM = activateDistanceTravelledM - thisTrain.PresentPosition[0].DistanceTravelledM;

            //        firstPartTime = presentSpeedMpS / (0.25f * aiTrain.MaxDecelMpSS);
            //        firstPartRangeM = 0.25f * aiTrain.MaxDecelMpSS * (firstPartTime * firstPartTime);

            //        if (firstPartRangeM < remainingRangeM && thisTrain.SpeedMpS < thisTrain.TrainMaxSpeedMpS) // if distance left and not at max speed
            //        // split remaining distance based on relation between acceleration and deceleration
            //        {
            //            secndPartRangeM = (remainingRangeM - firstPartRangeM) * (2.0f * aiTrain.MaxDecelMpSS) / (aiTrain.MaxDecelMpSS + aiTrain.MaxAccelMpSS);
            //        }

            //        triggerDistanceM = activateDistanceTravelledM - (firstPartRangeM + secndPartRangeM);
            //    }
            //    else

            //    // use maximum speed
            //    {
            //        float deltaTime = thisTrain.TrainMaxSpeedMpS / aiTrain.MaxDecelMpSS;
            //        float brakingDistanceM = (thisTrain.TrainMaxSpeedMpS * deltaTime) + (0.5f * aiTrain.MaxDecelMpSS * deltaTime * deltaTime);
            //        triggerDistanceM = activateDistanceTravelledM - brakingDistanceM;
            //    }
            //}
            //else
            {
                activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, TCSectionIndex, this.RequiredDistance, true, thisTrain.signalRef);
                triggerDistanceM = activateDistanceTravelledM - brakeSection;   //  TODO, add the size of train
            }

            float[] distancesM = new float[2];
            distancesM[1] = triggerDistanceM;
            if (activateDistanceTravelledM < thisTrain.PresentPosition[0].DistanceTravelledM &&
                thisTrain.PresentPosition[0].DistanceTravelledM - activateDistanceTravelledM < thisTrain.Length)
                activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM;
            distancesM[0] = activateDistanceTravelledM;

            return (distancesM);
        }

        public void SetEndSignalIndex(int idx)
        {
            EndSignalIndex = idx;
        }

    }

    #endregion

    #region AuxActionData

    //================================================================================================//
    /// <summary>
    /// AuxActionItem
    /// A specific AIActionItem used at run time to manage a specific Auxiliary Action
    /// </summary>

    public class AuxActionItem : AIActionItem
    {
        public AIAuxActionsRef ActionRef;
        public bool Triggered = false;
        public bool Processing = false;
        protected AITrain.AI_MOVEMENT_STATE currentMvmtState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
        public SignalObject SignalReferenced { get { return ActionRef.SignalReferenced; } set {} }

        //================================================================================================//
        /// <summary>
        /// AuxActionItem
        /// The basic constructor
        /// </summary>

        public AuxActionItem(float distance, float requiredSpeedMpS, float activateDistance, float insertedDistance,
            AIAuxActionsRef thisItem, AI_ACTION_TYPE thisAction) :
            base (distance, requiredSpeedMpS, activateDistance, insertedDistance, null, thisAction)
        {
            NextAction = AI_ACTION_TYPE.AUX_ACTION;
            ActionRef = thisItem;
        }

        public virtual bool CanActivate(Train thisTrain, float SpeedMpS, bool reschedule)
        {
            return false;
        }

        public virtual bool CanRemove(Train thisTrain)
        {
            return true;
        }

        public virtual Boolean ProcessingStarted()
        {
            return false;
        }

        public void SetLinked(AIActionItem linked)
        {
        }

        public override AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            AITrain.AI_MOVEMENT_STATE mvtState = movementState;
            if (ActionRef.IsGeneric)
                mvtState = currentMvmtState;
            int correctedTime = presentTime;
            switch (mvtState)
            {
                case AITrain.AI_MOVEMENT_STATE.INIT_ACTION:
                    movementState = InitAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.END_ACTION:
                    movementState = EndAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    movementState = HandleAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.BRAKING:
                    break;
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    break;
                default:
                    break;
            }
            currentMvmtState = movementState;
            return movementState;
        }

        public override void ProcessAction(Train thisTrain, int presentTime)
        {
            int correctedTime = presentTime;
            switch (currentMvmtState)
            {
                case AITrain.AI_MOVEMENT_STATE.INIT_ACTION:
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    currentMvmtState = InitAction(thisTrain, presentTime, 0f, currentMvmtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.END_ACTION:
                    currentMvmtState = EndAction(thisTrain, presentTime, 0f, currentMvmtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    currentMvmtState = HandleAction(thisTrain, presentTime, 0f, currentMvmtState);
                    break;
                default:
                    break;
            }
            return;
        }



#if WITH_PATH_DEBUG
        public override string AsString(AITrain thisTrain)
        {
            return " AUX(";
        }
#endif
    }

    //================================================================================================//
    /// <summary>
    /// AuxActionWPItem
    /// A specific class used at run time to manage a Waiting Point Action
    /// </summary>

    public class AuxActionWPItem : AuxActionItem
    {
        int Delay;
        public int ActualDepart;

        //================================================================================================//
        /// <summary>
        /// AuxActionWPItem
        /// The specific constructor for WP action
        /// </summary>

        public AuxActionWPItem(float distance, float requiredSpeedMpS, float activateDistance, float insertedDistance,
            AIAuxActionsRef thisItem, AI_ACTION_TYPE thisAction) :
            base(distance, requiredSpeedMpS, activateDistance, insertedDistance, thisItem, thisAction)
        {
            ActualDepart = 0;
        }

        //================================================================================================//
        /// <summary>
        /// AsString
        /// Used by debugging in HUDWindows.
        /// </summary>

        public override string AsString(AITrain thisTrain)
        {
            return " WP(";
        }

        public override bool CanActivate(Train thisTrain, float SpeedMpS, bool reschedule)
        {
            if (ActionRef == null)
                return false;
            float[] distancesM = ActionRef.CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);

            if (RequiredDistance < thisTrain.DistanceTravelledM) // trigger point
            {
                return true;
            }

            RequiredDistance = distancesM[1];
            ActivateDistanceM = distancesM[0];
            return false;
        }

        public void SetDelay(int delay)
        {
            Delay = delay;
        }

        public override bool ValidAction(Train thisTrain)
        {
            bool actionValid = CanActivate(thisTrain, thisTrain.SpeedMpS, true);
            if (thisTrain is AITrain)
            {
                AITrain aiTrain = thisTrain as AITrain;
                if (!actionValid)
                {
                    aiTrain.requiredActions.InsertAction(this);
                }
                aiTrain.EndProcessAction(actionValid, this, false);
            }
            return actionValid;
        }

        public override AITrain.AI_MOVEMENT_STATE InitAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            if (thisTrain is AITrain)
            {
                AITrain aiTrain = thisTrain as AITrain;

                // repeat stopping of train, because it could have been moved by UpdateBrakingState after ProcessAction
                aiTrain.AdjustControlsBrakeMore(aiTrain.MaxDecelMpSS, elapsedClockSeconds, 100);
                int correctedTime = presentTime;
                ActualDepart = correctedTime + Delay;
                aiTrain.AuxActionsContain.CheckGenActions(aiTrain.GetType(), aiTrain.RearTDBTraveller.WorldLocation, Delay);

#if WITH_PATH_DEBUG
                File.AppendAllText(@"C:\temp\checkpath.txt", "WP, init action for train " + aiTrain.Number + " at " + correctedTime + " to " + ActualDepart + "(HANDLE_ACTION)\n");
#endif
            }

            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }

        public override AITrain.AI_MOVEMENT_STATE HandleAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            if (thisTrain is AITrain)
            {
                AITrain aiTrain = thisTrain as AITrain;

                thisTrain.AuxActionsContain.CheckGenActions(this.GetType(), aiTrain.RearTDBTraveller.WorldLocation, ActualDepart - presentTime);

                if (ActualDepart > presentTime)
                {
                    movementState = AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
                }
                else
                {
#if WITH_PATH_DEBUG
                    File.AppendAllText(@"C:\temp\checkpath.txt", "WP, End Handle action for train " + aiTrain.Number + " at " + presentTime + "(END_ACTION)\n");
#endif
                    movementState = AITrain.AI_MOVEMENT_STATE.END_ACTION;
                }
            }
            else
                movementState = AITrain.AI_MOVEMENT_STATE.END_ACTION;
            return movementState;
        }

        public override AITrain.AI_MOVEMENT_STATE EndAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "WP, Action ended for train " + thisTrain.Number + " at " + presentTime + "(STOPPED)\n");
#endif
            return AITrain.AI_MOVEMENT_STATE.STOPPED;
        }

        public override AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            int correctedTime = presentTime;
            switch (movementState)
            {
                case AITrain.AI_MOVEMENT_STATE.INIT_ACTION:
                    movementState = InitAction(thisTrain, presentTime, elapsedClockSeconds, movementState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.END_ACTION:
                    movementState = EndAction(thisTrain, presentTime, elapsedClockSeconds, movementState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    movementState = HandleAction(thisTrain, presentTime, elapsedClockSeconds, movementState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.BRAKING:
                    if (thisTrain is AITrain)
                    {
                        AITrain aiTrain = thisTrain as AITrain;
                        float distanceToGoM = AITrain.clearingDistanceM;
                        distanceToGoM = ActivateDistanceM - aiTrain.PresentPosition[0].DistanceTravelledM;
                        float NextStopDistanceM = distanceToGoM;
                        if (distanceToGoM <= 0f)
                        {
                            aiTrain.AdjustControlsBrakeMore(aiTrain.MaxDecelMpSS, elapsedClockSeconds, 100);
                            aiTrain.AITrainThrottlePercent = 0;

                            if (aiTrain.SpeedMpS < 0.001)
                            {
                                aiTrain.SpeedMpS = 0f;
                                movementState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
                            }
                        }
                        else if (distanceToGoM < AITrain.signalApproachDistanceM && aiTrain.SpeedMpS == 0)
                        {
                            aiTrain.AdjustControlsBrakeMore(aiTrain.MaxDecelMpSS, elapsedClockSeconds, 100);
                            movementState = AITrain.AI_MOVEMENT_STATE.END_ACTION;
                        }
                    }
                    else
                    {
                        if (thisTrain.AuxActionsContain.CountSpec() > 0)
                            thisTrain.AuxActionsContain.RemoveAt(0);
                    }
                    break;
                case AITrain.AI_MOVEMENT_STATE.STOPPED:

                    if (thisTrain.AuxActionsContain.CountSpec() > 0)
                        thisTrain.AuxActionsContain.Remove(this);
#if WITH_PATH_DEBUG
                    else
                    {
                        File.AppendAllText(@"C:\temp\checkpath.txt", "AITRain " + thisTrain.Number + "!  No more AuxActions...\n");
                    }
#endif
                    if (thisTrain is AITrain)
                    {
                        AITrain aiTrain = thisTrain as AITrain;

                        //movementState = thisTrain.UpdateStoppedState();   // Don't call UpdateStoppedState(), WP can't touch Signal
                        movementState = AITrain.AI_MOVEMENT_STATE.BRAKING;
                        aiTrain.ResetActions(true);
#if WITH_PATH_DEBUG
                        File.AppendAllText(@"C:\temp\checkpath.txt", "AITRain " + aiTrain.Number + " is " + movementState.ToString() + " at " + presentTime + "\n");
#endif
                    }
                    break;
                default:
                    break; 
            }
            return movementState;
        }

    }

    //================================================================================================//
    /// <summary>
    /// AuxActionWPItem
    /// A specific class used at run time to manage a Waiting Point Action
    /// </summary>

    public class AuxActionHornItem : AuxActionItem
    {
        int Delay;
        public int ActualDepart;
        

        //================================================================================================//
        /// <summary>
        /// AuxActionWPItem
        /// The specific constructor for WP action
        /// </summary>

        public AuxActionHornItem(float distance, float requiredSpeedMpS, float activateDistance, float insertedDistance,
            AIAuxActionsRef thisItem, AI_ACTION_TYPE thisAction) :
            base(distance, requiredSpeedMpS, activateDistance, insertedDistance, thisItem, thisAction)
        {
            ActualDepart = 0;
        }

        //================================================================================================//
        /// <summary>
        /// AsString
        /// Used by debugging in HUDWindows.
        /// </summary>

        public override string AsString(AITrain thisTrain)
        {
            return " Horn(";
        }

        public override bool CanActivate(Train thisTrain, float SpeedMpS, bool reschedule)
        {
            if (ActionRef == null)
                return false;
            float[] distancesM = ActionRef.CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);

            if (RequiredDistance < thisTrain.DistanceTravelledM) // trigger point
            {
                return true;
            }

            RequiredDistance = distancesM[1];
            ActivateDistanceM = distancesM[0];
            return false;
        }

        public void SetDelay(int delay)
        {
            Delay = delay;
        }

        public override bool ValidAction(Train thisTrain)
        {
            bool actionValid = CanActivate(thisTrain, thisTrain.SpeedMpS, true);
            if (!(thisTrain is AITrain))
            {
                AITrain aiTrain = thisTrain as AITrain;

                if (!actionValid)
                {
                    aiTrain.requiredActions.InsertAction(this);
                }
                aiTrain.EndProcessAction(actionValid, this, false);
            }
            return actionValid;
        }

        public override Boolean ProcessingStarted()
        {
            return Processing;
        }

        public override AITrain.AI_MOVEMENT_STATE InitAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "AITRain " + thisTrain.Number + " is " + movementState.ToString() + " at " + presentTime + "\n");
#endif
            Processing = true;
            int correctedTime = presentTime;
            ActualDepart = correctedTime + Delay;
            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }

        public override AITrain.AI_MOVEMENT_STATE HandleAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            if (ActualDepart >= presentTime)
            {
                if (!Triggered)
                {
#if WITH_PATH_DEBUG
                    File.AppendAllText(@"C:\temp\checkpath.txt", "Do Horn for AITRain " + thisTrain.Number + " , mvt state " + movementState.ToString() + " at " + presentTime + "\n");
#endif
                    TrainCar locomotive = thisTrain.FindLeadLocomotive();
                    ((MSTSLocomotive)locomotive).SignalEvent(Event.HornOn);
                    Triggered = true;
                }
                movementState = AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
            }
            else
            {
                movementState = AITrain.AI_MOVEMENT_STATE.END_ACTION;
            }
            return movementState;
        }

        public override AITrain.AI_MOVEMENT_STATE EndAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            thisTrain.AuxActionsContain.Remove(this);

            if (Triggered)
            {
#if WITH_PATH_DEBUG
                File.AppendAllText(@"C:\temp\checkpath.txt", "Stop Horn for AITRain " + thisTrain.Number + " : mvt state " + movementState.ToString() + " at " + presentTime + "\n");
#endif
                TrainCar locomotive = thisTrain.FindLeadLocomotive();
                ((MSTSLocomotive)locomotive).SignalEvent(Event.HornOff);
            }
            return currentMvmtState;    //  Restore previous MovementState
        }

        public override AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            AITrain.AI_MOVEMENT_STATE mvtState = movementState;
            if (ActionRef.IsGeneric)
                mvtState = currentMvmtState;
            int correctedTime = presentTime;
            switch (mvtState)
            {
                case AITrain.AI_MOVEMENT_STATE.INIT_ACTION:
                    movementState = InitAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.END_ACTION:
                    movementState = EndAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    movementState = HandleAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.BRAKING:
                    float distanceToGoM = AITrain.clearingDistanceM;
                    distanceToGoM = ActivateDistanceM - thisTrain.PresentPosition[0].DistanceTravelledM;
                    float NextStopDistanceM = distanceToGoM;
                    if (distanceToGoM < 0f)
                    {
                        currentMvmtState = movementState;
                        movementState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
                    }

                    break;
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    if (thisTrain is AITrain)
                        movementState = ((AITrain)thisTrain).UpdateStoppedState();
                    break;
                default:
                    break;
            }
            if (ActionRef.IsGeneric)
                currentMvmtState = movementState;

            return movementState;
        }

    }
    //================================================================================================//
    /// <summary>
    /// AuxActionSteamItem
    /// A specific class used at run time to manage the starting of a steam train
    /// </summary>

    public class AuxActionSteamItem : AuxActionItem
    {
        int Delay;
        public int ActualDepart;


        //================================================================================================//
        /// <summary>
        /// AuxActionWPItem
        /// The specific constructor for WP action
        /// </summary>

        public AuxActionSteamItem(float distance, float requiredSpeedMpS, float activateDistance, float insertedDistance,
            AIAuxActionsRef thisItem, AI_ACTION_TYPE thisAction) :
            base(distance, requiredSpeedMpS, activateDistance, insertedDistance, thisItem, thisAction)
        {
            ActualDepart = 0;
        }

        //================================================================================================//
        /// <summary>
        /// AsString
        /// Used by debugging in HUDWindows.
        /// </summary>

        public override string AsString(AITrain thisTrain)
        {
            return " Steam(";
        }

        public override bool CanActivate(Train thisTrain, float SpeedMpS, bool reschedule)
        {
            if (ActionRef == null)
                return false;
            float[] distancesM = ActionRef.CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);

            if (RequiredDistance < thisTrain.DistanceTravelledM) // trigger point
            {
                return true;
            }

            RequiredDistance = distancesM[1];
            ActivateDistanceM = distancesM[0];
            return false;
        }

        public void SetDelay(int delay)
        {
            Delay = delay;
        }

        public override bool ValidAction(Train thisTrain)
        {
            bool actionValid = CanActivate(thisTrain, thisTrain.SpeedMpS, true);
            if (!(thisTrain is AITrain))
            {
                AITrain aiTrain = thisTrain as AITrain;

                if (!actionValid)
                {
                    aiTrain.requiredActions.InsertAction(this);
                }
                aiTrain.EndProcessAction(actionValid, this, false);
            }
            return actionValid;
        }

        public override Boolean ProcessingStarted()
        {
            return Processing;
        }

        public override AITrain.AI_MOVEMENT_STATE InitAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "AITRain " + thisTrain.Number + " is " + movementState.ToString() + " at " + presentTime + "\n");
#endif
            TrainCar locomotive = thisTrain.FindLeadLocomotive();
            if (!(locomotive is MSTSSteamLocomotive))
            {
                return AITrain.AI_MOVEMENT_STATE.END_ACTION;
            }
            Processing = true;
            int correctedTime = presentTime;
            ActualDepart = correctedTime + Delay;
            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }

        public override AITrain.AI_MOVEMENT_STATE HandleAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            TrainCar locomotive = thisTrain.FindLeadLocomotive();
            if (ActualDepart >= presentTime)
            {
                if (!Triggered && ActualDepart >= (presentTime + 7))
                {
                    MSTSSteamLocomotive steamLocomotive = locomotive as MSTSSteamLocomotive;
                    steamLocomotive.StartBlowerIncrease(100);
                    steamLocomotive.ToggleCylinderCocks();
                    Triggered = true;
                }
                movementState = AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
            }
            else
            {
                MSTSSteamLocomotive steamLocomotive = locomotive as MSTSSteamLocomotive;
                steamLocomotive.StartBlowerDecrease(20);

                movementState = AITrain.AI_MOVEMENT_STATE.END_ACTION;
            }
            return movementState;
        }

        public override AITrain.AI_MOVEMENT_STATE EndAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            TrainCar locomotive = thisTrain.FindLeadLocomotive();
            thisTrain.AuxActionsContain.Remove(this);
            if ((locomotive is MSTSSteamLocomotive))
            {
                MSTSSteamLocomotive steamLocomotive = locomotive as MSTSSteamLocomotive;
                steamLocomotive.ToggleCylinderCocks();
            }

            if (Triggered)
            {
#if WITH_PATH_DEBUG
                File.AppendAllText(@"C:\temp\checkpath.txt", "Stop Horn for AITRain " + thisTrain.Number + " : mvt state " + movementState.ToString() + " at " + presentTime + "\n");
#endif
                locomotive = thisTrain.FindLeadLocomotive();
                ((MSTSLocomotive)locomotive).SignalEvent(Event.HornOff);
            }
            return currentMvmtState;    //  Restore previous MovementState
        }

        public override AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            AITrain.AI_MOVEMENT_STATE mvtState = movementState;
            if (ActionRef.IsGeneric)
                mvtState = currentMvmtState;
            int correctedTime = presentTime;
            switch (mvtState)
            {
                case AITrain.AI_MOVEMENT_STATE.INIT_ACTION:
                    movementState = InitAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.END_ACTION:
                    movementState = EndAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    movementState = HandleAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.BRAKING:
                    float distanceToGoM = AITrain.clearingDistanceM;
                    distanceToGoM = ActivateDistanceM - thisTrain.PresentPosition[0].DistanceTravelledM;
                    float NextStopDistanceM = distanceToGoM;
                    if (distanceToGoM < 0f)
                    {
                        currentMvmtState = movementState;
                        movementState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
                    }

                    break;
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    if (thisTrain is AITrain)
                        movementState = ((AITrain)thisTrain).UpdateStoppedState();
                    break;
                default:
                    break;
            }
            if (ActionRef.IsGeneric)
                currentMvmtState = movementState;

            return movementState;
        }

    }
    //================================================================================================//
    /// <summary>
    /// AuxActionWPItem
    /// A specific class used at run time to manage a Waiting Point Action
    /// </summary>

    public class AuxActionSignalItem : AuxActionItem
    {
        int Delay;
        public int ActualDepart;
        bool HornTriggered = false;
        AITrain.AI_MOVEMENT_STATE currentMvmtState;

        //================================================================================================//
        /// <summary>
        /// AuxActionWPItem
        /// The specific constructor for WP action
        /// </summary>

        public AuxActionSignalItem(float distance, float requiredSpeedMpS, float activateDistance, float insertedDistance,
            AIAuxActionsRef thisItem, AI_ACTION_TYPE thisAction) :
            base(distance, requiredSpeedMpS, activateDistance, insertedDistance, thisItem, thisAction)
        {
            ActualDepart = 0;
        }

        //================================================================================================//
        /// <summary>
        /// AsString
        /// Used by debugging in HUDWindows.
        /// </summary>

        public override string AsString(AITrain thisTrain)
        {
            return " SigH(";
        }

        public override bool CanActivate(Train thisTrain, float SpeedMpS, bool reschedule)
        {
            if (ActionRef == null)
                return false;
            float[] distancesM = ActionRef.CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);

            if (RequiredDistance < thisTrain.DistanceTravelledM) // trigger point
            {
                return true;
            }

            RequiredDistance = distancesM[1];
            ActivateDistanceM = distancesM[0];
            return false;
        }

        public void SetDelay(int delay)
        {
            Delay = delay;
        }

        public override bool ValidAction(Train thisTrain)
        {
            bool actionValid = CanActivate(thisTrain, thisTrain.SpeedMpS, true);
            if (!(thisTrain is AITrain))
            {
                AITrain aiTrain = thisTrain as AITrain;
                if (!actionValid)
                {
                    aiTrain.requiredActions.InsertAction(this);
                }
                aiTrain.EndProcessAction(actionValid, this, false);
            }
            return actionValid;
        }

        public override AITrain.AI_MOVEMENT_STATE InitAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            int correctedTime = presentTime;
            ActualDepart = correctedTime + Delay;
            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }

        public override AITrain.AI_MOVEMENT_STATE HandleAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            if (ActualDepart >= presentTime)
            {
                if (!HornTriggered)
                {
                    TrainCar locomotive = thisTrain.FindLeadLocomotive();
                    ((MSTSLocomotive)locomotive).SignalEvent(Event.HornOn);
                    HornTriggered = true;
                }
                movementState = AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
            }
            else
            {
                movementState = AITrain.AI_MOVEMENT_STATE.END_ACTION;
            }
            return movementState;
        }

        public override AITrain.AI_MOVEMENT_STATE EndAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            if (!ActionRef.IsGeneric)
                thisTrain.AuxActionsContain.RemoveAt(0);
            //thisTrain.ResetActions(true);
            if (HornTriggered)
            {
                TrainCar locomotive = thisTrain.FindLeadLocomotive();
                ((MSTSLocomotive)locomotive).SignalEvent(Event.HornOff);
            }
            return currentMvmtState;    //  Restore previous MovementState
        }
    }

    //================================================================================================//
    /// <summary>
    /// AuxActSigDelegate
    /// Used to post pone de signal clear after WP
    /// </summary>

    public class AuxActSigDelegate : AuxActionItem
    {
        public int ActualDepart;
        public bool locked = true;

        //================================================================================================//
        /// <summary>
        /// AuxActionWPItem
        /// The specific constructor for WP action
        /// </summary>

        public AuxActSigDelegate(float distance, float requiredSpeedMpS, float activateDistance, float insertedDistance,
            AIAuxActionsRef thisItem, AI_ACTION_TYPE thisAction) :
            base(distance, requiredSpeedMpS, activateDistance, insertedDistance, thisItem, thisAction)
        {
            ActualDepart = 0;
        }

        //================================================================================================//
        /// <summary>
        /// AsString
        /// Used by debugging in HUDWindows.
        /// </summary>

        public override string AsString(AITrain thisTrain)
        {
            return " SigDlgt(";
        }

        public bool ClearSignal(Train thisTrain)
        {
            if (SignalReferenced != null)
            {
                bool ret = SignalReferenced.requestClearSignal(thisTrain.ValidRoute[0], thisTrain.routedForward, 0, false, null);
                return ret;
            }
            return true;
        }

        public override bool CanActivate(Train thisTrain, float SpeedMpS, bool reschedule)
        {
            if (ActionRef == null || SignalReferenced == null)
            {
                thisTrain.AuxActionsContain.RemoveSpecReqAction(this);
                return false;
            }
            if (ActionRef != null && ActionRef.LinkedAuxAction)
                return false;
            float[] distancesM = ActionRef.CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);
            if (distancesM[0] <= thisTrain.DistanceTravelledM) // trigger point
            {
                if (thisTrain.SpeedMpS > 0f)
                {
                    thisTrain.SetTrainOutOfControl(Train.OUTOFCONTROL.OUT_OF_PATH);
                }
                return true;
            }

            if (!reschedule && thisTrain.SpeedMpS == 0f && distancesM[1] < thisTrain.DistanceTravelledM)
            {
                return true;
            }

            //RequiredDistance = distancesM[1];
            //ActivateDistanceM = distancesM[0];
            return false;
        }

        public override bool CanRemove(Train thisTrain)
        {
            if (Processing && (currentMvmtState == AITrain.AI_MOVEMENT_STATE.STOPPED || currentMvmtState == AITrain.AI_MOVEMENT_STATE.END_ACTION))
                return true;
            if (SignalReferenced == null)
                return true;
            return false;
        }


        public override bool ValidAction(Train thisTrain)
        {
            bool actionValid = CanActivate(thisTrain, thisTrain.SpeedMpS, true);
            if (!actionValid)
            {
                //thisTrain.requiredActions.InsertAction(this);
            }
            return actionValid;
        }

        public override AITrain.AI_MOVEMENT_STATE InitAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            ActualDepart = presentTime + ((AIActSigDelegateRef)ActionRef).Delay;
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "SigDelegate, init action for train " + thisTrain.Number + " at " + 
                presentTime + "(HANDLE_ACTION)\n");
#endif
            Processing = true;
            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }

        public override AITrain.AI_MOVEMENT_STATE HandleAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "WP, End Handle action for train " + thisTrain.Number + 
                " at " + presentTime + "(END_ACTION)\n");
#endif
            if (ActualDepart >= presentTime)
            {

                return movementState;
            }
            if (locked && SignalReferenced != null)
            {
                locked = false;
                if (!SignalReferenced.UnlockForTrain(thisTrain.Number, thisTrain.TCRoute.activeSubpath))
                    locked = true;
            }
            if (ClearSignal(thisTrain))
                movementState = AITrain.AI_MOVEMENT_STATE.END_ACTION;
            return movementState;
        }

        public override AITrain.AI_MOVEMENT_STATE EndAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "WP, Action ended for train " + thisTrain.Number + " at " + presentTime + "(STOPPED)\n");
#endif
            if (thisTrain.AuxActionsContain.CountSpec() > 0)
            {
                thisTrain.AuxActionsContain.Remove(this);
            }
#if WITH_PATH_DEBUG
            else
            {
                File.AppendAllText(@"C:\temp\checkpath.txt", "AITRain " + thisTrain.Number + "!  No more AuxActions...\n");
            }
#endif
            return AITrain.AI_MOVEMENT_STATE.STOPPED;
        }
        public override AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            base.ProcessAction(thisTrain, presentTime, elapsedClockSeconds, movementState);
            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }
    }


#endregion
}

#endif