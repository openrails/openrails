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
using Newtonsoft.Json;
using Orts.Formats.Msts;
using Orts.Formats.OR;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.Signalling;
using ORTS.Common;

namespace Orts.Simulation.AIs
{
    #region AuxActionsContainer

    //================================================================================================//
    /// <summary>
    /// AuxActionsContainer
    /// Used to manage all the action ref object.
    /// </summary>
    public class AuxActionsContainer : ActionContainer
    {
        public Train.DistanceTravelledActions genRequiredActions = new Train.DistanceTravelledActions(); // Distance travelled Generic action list for AITrain
        public Train.DistanceTravelledActions specRequiredActions = new Train.DistanceTravelledActions();

        readonly Train ThisTrain;

        public AuxActionsContainer(Train thisTrain, ORRouteConfig orRouteConfig)
        {
            if (thisTrain is AITrain train && orRouteConfig != null)
            {
                SetGenAuxActions(train, orRouteConfig);
            }
            else
            {
                GenAuxActions = new List<KeyValuePair<string, AuxActionRef>>();
            }
            ThisTrain = thisTrain;
        }

        public AuxActionsContainer(Train thisTrain, BinaryReader inf, string routePath)
        {
            //SpecAuxActions = new List<AuxActionRef>();
            ////GenAuxActions = new List<AuxActionRef>();
            //GenFunctions = new List<KeyValuePair<System.Type, AuxActionRef>>();
            //if (thisTrain is AITrain)
            //{
            //    GenAuxActions = SetGenAuxActions((AITrain)thisTrain, null);
            //}
            ThisTrain = thisTrain;
            if (thisTrain is AITrain train && thisTrain.Simulator.orRouteConfig != null)
            {
                SetGenAuxActions(train, thisTrain.Simulator.orRouteConfig);
            }

            int cntAuxActionSpec = inf.ReadInt32();
            for (int idx = 0; idx < cntAuxActionSpec; idx++)
            {
                int cntAction = inf.ReadInt32();
                AuxActionRef action;
                string actionRef = inf.ReadString();
                AuxActionRef.AUX_ACTION nextAction = (AuxActionRef.AUX_ACTION)Enum.Parse(typeof(AuxActionRef.AUX_ACTION), actionRef);
                switch (nextAction)
                {
                    case AuxActionRef.AUX_ACTION.WAITING_POINT:
                        action = new AIActionWPRef(thisTrain, inf);
                        SpecAuxActions.Add(action);
                        break;
                    case AuxActionRef.AUX_ACTION.SOUND_HORN:
                        action = new AIActionHornRef(thisTrain, inf);
                        SpecAuxActions.Add(action);
                        break;
                    case AuxActionRef.AUX_ACTION.SIGNAL_DELEGATE:
                        action = new AIActSigDelegateRef(thisTrain, inf);
                        var hasWPActionAssociated = inf.ReadBoolean();
                        if (hasWPActionAssociated && SpecAuxActions.Count > 0)
                        {
                            var candidateAssociate = SpecAuxActions[SpecAuxActions.Count - 1];
                            if (candidateAssociate is AIActionWPRef && (candidateAssociate as AIActionWPRef).TCSectionIndex == (action as AIActSigDelegateRef).TCSectionIndex)
                            {
                                (action as AIActSigDelegateRef).AssociatedWPAction = candidateAssociate as AIActionWPRef;
                            }
                        }
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
            File.AppendAllText(@"C:\temp\checkpath.txt", "SaveAuxContainer, count :" + SpecAuxActions.Count + 
                "Position in file: " + outf.BaseStream.Position + "\n");
#endif
            AITrain aiTrain = ThisTrain as AITrain;
            if (SpecAuxActions.Count > 0 && SpecAuxActions[0] != null &&
                    specRequiredActions.First != null && specRequiredActions.First.Value is AuxActSigDelegate @delegate)

                // SigDelegate WP is running
                if (@delegate.currentMvmtState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION &&
                    !(@delegate.ActionRef as AIActSigDelegateRef).IsAbsolute)
                {
                    int remainingDelay = @delegate.ActualDepart - currentClock;
                    AIActSigDelegateRef actionRef = @delegate.ActionRef as AIActSigDelegateRef;
                    actionRef.AssociatedWPAction?.SetDelay(remainingDelay);
                    actionRef.Delay = remainingDelay;
                }
            if (!(ThisTrain == ThisTrain.Simulator.OriginalPlayerTrain && (ThisTrain.TrainType == Train.TRAINTYPE.AI_PLAYERDRIVEN ||
                ThisTrain.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING || ThisTrain.TrainType == Train.TRAINTYPE.PLAYER || ThisTrain.TrainType == Train.TRAINTYPE.AI)))
            {

                if (ThisTrain is AITrain && ((aiTrain.MovementState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION && aiTrain.nextActionInfo != null &&
                aiTrain.nextActionInfo.NextAction == AIActionItem.AI_ACTION_TYPE.AUX_ACTION && aiTrain.nextActionInfo is AuxActionWPItem)
                || (aiTrain.AuxActionsContain.SpecAuxActions.Count > 0 &&
                aiTrain.AuxActionsContain.SpecAuxActions[0] is AIActionWPRef && (aiTrain.AuxActionsContain.SpecAuxActions[0] as AIActionWPRef).keepIt != null &&
                (aiTrain.AuxActionsContain.SpecAuxActions[0] as AIActionWPRef).keepIt.currentMvmtState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION)))
                // WP is running
                {
                    // Do nothing if it is an absolute WP
                    if (!(aiTrain.AuxActionsContain.SpecAuxActions.Count > 0 && aiTrain.AuxActionsContain.SpecAuxActions[0] is AIActionWPRef &&
                        (aiTrain.AuxActionsContain.SpecAuxActions[0] as AIActionWPRef).Delay >= 30000 && (aiTrain.AuxActionsContain.SpecAuxActions[0] as AIActionWPRef).Delay < 40000))
                    {
                        var remainingDelay = aiTrain.nextActionInfo != null && aiTrain.nextActionInfo is AuxActionWPItem item
                            ? item.ActualDepart - currentClock
                            : ((AIActionWPRef)SpecAuxActions[0]).keepIt.ActualDepart - currentClock;
                        ((AIActionWPRef)SpecAuxActions[0]).SetDelay(remainingDelay);
                    }
                }
            }
            foreach (var action in SpecAuxActions)
            {
                ((AIAuxActionsRef)action).save(outf, cnt);
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
        protected List<KeyValuePair<string, AuxActionRef>> ConvertActions(Train thisTrain, ActionContainer actionContainer)
        {
            List<KeyValuePair<string, AuxActionRef>> converted = new List<KeyValuePair<string, AuxActionRef>>();
            foreach (var action in actionContainer.GenAuxActions)
            {
                if (action.Value.GetType() == typeof(AuxActionHorn))
                {
                    AIActionHornRef horn = new AIActionHornRef(thisTrain, (AuxActionHorn)action.Value, 0);
                    List<KeyValuePair<System.Type, AuxActionRef>> listInfo = horn.GetCallFunction();
                    foreach (var function in listInfo)
                        GenFunctions.Add(function);
                    KeyValuePair<string, AuxActionRef> info = new KeyValuePair<string, AuxActionRef>(action.Key, horn);
                    converted.Add(info);
                }
                else if (action.Value.GetType() == typeof(AuxControlStart))
                {
                    AIActionControlStartRef controlStart = new AIActionControlStartRef(thisTrain, (AuxControlStart)action.Value, 0);
                    List<KeyValuePair<System.Type, AuxActionRef>> listInfo = controlStart.GetCallFunction();
                    foreach (var function in listInfo)
                        GenFunctions.Add(function);
                    KeyValuePair<string, AuxActionRef> info = new KeyValuePair<string, AuxActionRef>(action.Key, controlStart);
                    converted.Add(info);
                    // If we use the ControllStart, then we must allow Generic WP to be created.
                    AIActionWPRef wp = new AIActionWPRef(thisTrain, (AuxControlStart)action.Value, 0);
                    listInfo = wp.GetCallFunction();
                    foreach (var function in listInfo)
                        GenFunctions.Add(function);
                    info = new KeyValuePair<string, AuxActionRef>(action.Key, controlStart);
                    converted.Add(info);
                }
            }
            return converted;
        }

        protected List<KeyValuePair<string, AuxActionRef>> SetGenAuxActions(AITrain thisTrain, ORRouteConfig orRouteConfig) // Add here the new Generic Action
        {
            List<KeyValuePair<string, AuxActionRef>> loaded = null;
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
            if (orRouteConfig == null)
            {
                loaded = new List<KeyValuePair<string, AuxActionRef>>();
                return loaded;
            }
            else
            {
                loaded = ConvertActions(thisTrain, orRouteConfig.ActionContainer);
            }

            var activity = thisTrain.Simulator.Activity;
            if (activity != null && activity.Tr_Activity.Tr_Activity_File.AIBlowsHornAtLevelCrossings && SpecAuxActions.Count == 0)
            {
                AuxActionHorn auxActionHorn = new AuxActionHorn(isGeneric: true, hornPattern: activity.Tr_Activity.Tr_Activity_File.AILevelCrossingHornPattern);
                AIActionHornRef horn = new AIActionHornRef(thisTrain, auxActionHorn, 0);
                List<KeyValuePair<System.Type, AuxActionRef>> listInfo = horn.GetCallFunction();
                foreach (var function in listInfo)
                    GenFunctions.Add(function);
            }

            //loaded = orRouteConfig.getGenAuxAction();
            return loaded;
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
                        AIAuxActionsRef called = (AIAuxActionsRef)fonction.Value;
                        if (called.HasAction(ThisTrain.Number, location))
                            return false;
                        AIActionItem newAction = called.CheckGenActions(location, aiTrain, list);
                        if (newAction != null)
                        {
                            if (newAction is AuxActionWPItem)
                                specRequiredActions.InsertAction(newAction);
                            else
                                genRequiredActions.InsertAction(newAction);
                        }
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
            if (ThisTrain is AITrain)
            {
                AITrain aiTrain = ThisTrain as AITrain;
                foreach (var fonction in GenFunctions)
                {
                    if (typeSource == fonction.Key)   //  Caller object is a LevelCrossing
                    {
                        AIAuxActionsRef called = (AIAuxActionsRef)fonction.Value;
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
                if (actionItem.RequiredDistance >= ThisTrain.DistanceTravelledM)
                    continue;
                if (actionItem is AuxActSigDelegate)
                {
                    var actionRef = (actionItem as AuxActSigDelegate).ActionRef;
                    if ((actionRef as AIActSigDelegateRef).IsAbsolute)
                        continue;
                }
                itemList.Add(actionItem);
            }
            foreach (var action in itemList)
            {
                AITrain.AI_MOVEMENT_STATE tmpMvt;
                AIActionItem actionItem = action as AIActionItem;
                tmpMvt = actionItem.ProcessAction(aiTrain, presentTime, elapsedClockSeconds, movementState);
                if (tmpMvt != movementState)
                    MvtState = tmpMvt; // Try to avoid override of changed state of previous action
            }
            return MvtState;
        }

        public void Remove(AuxActionItem action)
        {
            bool ret = false;
            bool remove = true;
            if (action.ActionRef.IsGeneric)
            {
                if (genRequiredActions.Count > 0)
                    ret = genRequiredActions.Remove(action);
                if (!ret && specRequiredActions.Count > 0)
                {
                    if (((AIAuxActionsRef)action.ActionRef).CallFreeAction(ThisTrain))
                        RemoveSpecReqAction(action);
                }
            }
            if (action.ActionRef.ActionType == AuxActionRef.AUX_ACTION.SOUND_HORN)
            {
                if (specRequiredActions.Contains(action)) RemoveSpecReqAction(action);
                else remove = false;
            }
            if (CountSpec() > 0 && remove == true)
                SpecAuxActions.Remove(action.ActionRef);
            if (ThisTrain is AITrain train)
                train.ResetActions(true);
        }

        public void RemoveAt(int posit)
        {
            SpecAuxActions.RemoveAt(posit);
        }

        public AuxActionRef this[int key]
        {
            get
            {
                return key >= SpecAuxActions.Count ? null : SpecAuxActions[key];
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
        //  SPA: Added for use with new AIActionItems
        /// <summary>
        /// Create Specific Auxiliary Action, like WP
        /// <\summary>
        public void SetAuxAction(Train thisTrain)
        {
            if (SpecAuxActions.Count <= 0)
                return;
            AIAuxActionsRef thisAction;
            int specAuxActionsIndex = 0;
            bool requiredActionsInserted = false;
            while (specAuxActionsIndex <= SpecAuxActions.Count - 1)
            {
                while (SpecAuxActions.Count > 0)
                {
                    thisAction = (AIAuxActionsRef)SpecAuxActions[specAuxActionsIndex];

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

                thisAction = (AIAuxActionsRef)SpecAuxActions[specAuxActionsIndex];
                bool validAction = false;
                float[] distancesM;
                while (!validAction)
                {
                    if (thisTrain is AITrain && thisTrain.TrainType != Train.TRAINTYPE.AI_PLAYERDRIVEN)
                    {
                        AITrain aiTrain = thisTrain as AITrain;
                        distancesM = thisAction.CalculateDistancesToNextAction(aiTrain, aiTrain.TrainMaxSpeedMpS, true);
                    }
                    else
                    {
                        distancesM = thisAction.CalculateDistancesToNextAction(thisTrain, thisTrain.SpeedMpS, true);
                    }
                    //<CSComment> Next block does not seem useful. distancesM[0] includes distanceTravelledM, so it practically can be 0 only at start of game
                    /*                if (distancesM[0]< 0f)
                                    {
                                        SpecAuxActions.RemoveAt(0);
                                        if (SpecAuxActions.Count == 0)
                                        {
                                            return;
                                        }

                                        thisAction = (AIAuxActionsRef)SpecAuxActions[0];
                                        if (thisAction.SubrouteIndex > thisTrain.TCRoute.activeSubpath) return;
                                    }
                                    else */
                    {
                        float requiredSpeedMpS = 0;
                        if (requiredActionsInserted && thisAction is AIActSigDelegateRef @ref && !@ref.IsAbsolute) return;
                        validAction = true;
                        AIActionItem newAction = ((AIAuxActionsRef)SpecAuxActions[specAuxActionsIndex]).Handler(distancesM[1], requiredSpeedMpS, distancesM[0], thisTrain.DistanceTravelledM);
                        if (newAction != null)
                        {
                            if (thisTrain is AITrain && newAction is AuxActionWPItem)   // Put only the WP for AI into the requiredAction, other are on the container
                            {
                                bool found = false;
                                requiredActionsInserted = true;
                                if ((thisTrain.TrainType == Train.TRAINTYPE.AI_PLAYERDRIVEN || thisTrain.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING) && thisTrain.requiredActions.Count > 0)
                                {
                                    // Check if action already inserted
                                    foreach (Train.DistanceTravelledItem item in thisTrain.requiredActions)
                                    {
                                        if (item is AuxActionWPItem)
                                        {
                                            found = true;
                                            continue;
                                        }
                                    }
                                }
                                if (!found)
                                {
                                    thisTrain.requiredActions.InsertAction(newAction);
                                    continue;
                                    // ((AITrain)thisTrain).nextActionInfo = newAction; // Action must be restored through required actions only
                                }
                            }
                            else
                            {
                                specRequiredActions.InsertAction(newAction);
                                if (newAction is AuxActionWPItem || newAction is AuxActSigDelegate)
                                    return;
                            }
                        }
                    }
                }
                specAuxActionsIndex++;
            }
        }

        //================================================================================================//
        //  
        /// <summary>
        /// Reset WP Aux Action, if any
        /// <\summary>
        public void ResetAuxAction(Train thisTrain)
        {
            if (SpecAuxActions.Count <= 0)
                return;
            AIAuxActionsRef thisAction;
            thisAction = (AIAuxActionsRef)SpecAuxActions[0];
            if (thisAction.SubrouteIndex != thisTrain.TCRoute.activeSubpath) return;
            thisAction.LinkedAuxAction = false;
            return;
        }

        //================================================================================================//
        //  
        /// <summary>
        /// Move next Aux Action, if in same section, under train in case of decoupling
        /// <\summary>
        public void MoveAuxAction(Train thisTrain)
        {
            AITrain thisAITrain = (AITrain)thisTrain;
            if (SpecAuxActions.Count <= 0)
                return;
            AIAuxActionsRef thisAction;
            thisAction = (AIAuxActionsRef)SpecAuxActions[0];
            if (thisAction is AIActionWPRef && thisAction.SubrouteIndex == thisTrain.TCRoute.activeSubpath && thisAction.TCSectionIndex == thisTrain.PresentPosition[0].TCSectionIndex)
            {
                // Waiting point is just in the same section where the train is; move it under the train
                AuxActionWPItem thisWPItem;
                if (thisAITrain.nextActionInfo != null && thisAITrain.nextActionInfo is AuxActionWPItem item)
                {
                    thisWPItem = item;
                    if (thisWPItem.ActionRef == thisAction)
                    {
                        thisWPItem.ActivateDistanceM = thisTrain.PresentPosition[0].DistanceTravelledM - 5;
                        thisAction.LinkedAuxAction = true;
                    }
                }
                thisAction.RequiredDistance = thisTrain.PresentPosition[0].TCOffset - 5;
            }
        }

        //================================================================================================//
        //  
        /// <summary>
        /// Move next Aux Action, if in same section and in next subpath (reversal in between), under train in case of decoupling
        /// <\summary>
        public void MoveAuxActionAfterReversal(Train thisTrain)
        {
            if (SpecAuxActions.Count <= 0)
                return;
            AIAuxActionsRef thisAction;
            thisAction = (AIAuxActionsRef)SpecAuxActions[0];
            if (thisAction is AIActionWPRef && thisAction.SubrouteIndex == thisTrain.TCRoute.activeSubpath + 1 && thisAction.TCSectionIndex == thisTrain.PresentPosition[1].TCSectionIndex)
            {
                // Waiting point is just in the same section where the train is; move it under the train
                int thisSectionIndex = thisTrain.PresentPosition[1].TCSectionIndex;
                TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
                thisAction.RequiredDistance = thisSection.Length - thisTrain.PresentPosition[1].TCOffset - 5;
            }
        }

        public AuxActionRef GetGenericAuxAction(AuxActionRef.AUX_ACTION typeReq)
        {
            return GenAuxActions.GetGenericAuxAction(typeReq);
        }

        public void Add(AuxActionRef action)
        {
            SpecAuxActions.Add(action);
        }
    }

    public static class AIAuxActionRefList
    {
        public static AuxActionRef GetGenericAuxAction(this List<KeyValuePair<string, AuxActionRef>> list, AuxActionRef.AUX_ACTION typeReq)
        {
            foreach (var AuxAction in list)
            {
                AIAuxActionsRef thisAction = AuxAction.Value as AIAuxActionsRef;
                if (thisAction.NextAction == typeReq)
                    return thisAction;
            }
            return null;
        }

        public static List<AuxActionRef> GetValidActions(this List<KeyValuePair<string, AuxActionRef>> list, AITrain thisTrain, float presentSpeedMpS, bool reschedule)
        {
            List<AuxActionRef> listAction = new List<AuxActionRef>();
            if (list == null)
                return listAction;
            var previousList = list;
            bool getFirst = false;
            try
            {
                foreach (var action in previousList)
                {
                    AIAuxActionsRef thisAction = action.Value as AIAuxActionsRef;
                    float[] distancesM = thisAction.CalculateDistancesToNextAction(thisTrain, presentSpeedMpS, reschedule);
                    if (!thisAction.IsGeneric && !getFirst)
                    {
                        if (distancesM[0] < 0f)
                        {
                            previousList.RemoveAt(0);
                            continue;
                        }
                        listAction.Insert(0, thisAction);
                        getFirst = true;
                    }
                    if (distancesM[0] == 0f || distancesM[0] == float.MaxValue)
                    {
                        listAction.Add(thisAction);
                    }
                }
            }
            catch
            {
                // TODO
            }

            return listAction;
        }
    }
    #endregion

    #region AuxActionRef
    ////================================================================================================//
    ///// <summary>
    ///// AuxActionRef
    ///// info used to figure out one auxiliary action along the route.  It's a reference data, not a run data.
    ///// </summary>
    public class AIAuxActionsRef : AuxActionRef
    {
        public int SubrouteIndex;
        public int RouteIndex;
        public int TCSectionIndex;
        public int Direction;
        protected int TriggerDistance = 0;
        public bool LinkedAuxAction = false;
        protected List<KeyValuePair<int, WorldLocation>> AskingTrain;
        public SignalObject SignalReferenced = null;
        public float RequiredSpeedMpS;
        public float RequiredDistance;
        public int Delay;
        public int OriginalDelay;
        public int EndSignalIndex { get; protected set; }

        public AUX_ACTION NextAction = AUX_ACTION.NONE;

        //================================================================================================//
        /// <summary>
        /// AIAuxActionsRef: Generic Constructor
        /// The specific datas are used to fire the Action.
        /// </summary>
        public AIAuxActionsRef(float requiredSpeedMps, WorldLocation location)
        {
        }

        public AIAuxActionsRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir, AuxActionRef.AUX_ACTION actionType = AuxActionRef.AUX_ACTION.NONE) :
            base(actionType, false)                 //null, requiredSpeedMpS, , -1, )
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

        public AIAuxActionsRef(Train thisTrain, BinaryReader inf, AuxActionRef.AUX_ACTION actionType = AuxActionRef.AUX_ACTION.NONE)
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
            EndSignalIndex = inf.ReadInt32();
            if (EndSignalIndex >= 0)
                SetSignalObject(thisTrain.signalRef.SignalObjects[EndSignalIndex]);
            else
                SetSignalObject(null);
            ActionType = actionType;
        }

        public virtual List<KeyValuePair<System.Type, AuxActionRef>> GetCallFunction()
        {
            return default;
        }

        //================================================================================================//
        /// <summary>
        /// Handler
        /// Like a fabric, if other informations are needed, please define specific function that can be called on the new object
        /// </summary>
        public virtual AIActionItem Handler(params object[] list)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                info = new AuxActionItem(this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
                info.SetParam((float)list[0], (float)list[1], (float)list[2], (float)list[3]);

                //info = new AuxActionItem(distance, speed, activateDistance, insertedDistance,
                //           this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
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

            return distancesM;
        }

        public virtual float[] GetActivationDistances(Train thisTrain, WorldLocation location)
        {
            float[] distancesM = new float[2];
            distancesM[1] = float.MaxValue;
            distancesM[0] = float.MaxValue;

            return distancesM;
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
            outf.Write(EndSignalIndex);

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
                int number = info.Key;
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
                int number = info.Key;
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
        public AuxActionWPItem keepIt = null;

        public AIActionWPRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir, AuxActionRef.AUX_ACTION.WAITING_POINT)
        {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "New AIAuxActionRef (WP) for train " + thisTrain.Number +
                " Required Distance " + distance + ", speed " + requiredSpeedMpS + ", and dir " + dir + "\n");
            File.AppendAllText(@"C:\temp\checkpath.txt", "\t\tSection id: " + subrouteIdx + "." + routeIdx + "." + sectionIdx 
                + "\n"); 
#endif
            NextAction = AUX_ACTION.WAITING_POINT;
        }

        public AIActionWPRef(Train thisTrain, BinaryReader inf)
            : base(thisTrain, inf)
        {
            Delay = inf.ReadInt32();
            OriginalDelay = inf.ReadInt32();
            NextAction = AUX_ACTION.WAITING_POINT;
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tRestore one WPAuxAction" +
                "Position in file: " + inf.BaseStream.Position +
                " type Action: " + NextAction.ToString() +
                " Delay: " + Delay + "\n");
#endif
        }

        public AIActionWPRef(Train thisTrain, AuxControlStart myBase, int nop = 0)
            : base(thisTrain, 0f, 0f, 0, 0, 0, 0, myBase.ActionType)
        {
            Delay = myBase.ActivationDelay; // Generic WP Delay will be the Control Start ActivationDelay
            NextAction = AUX_ACTION.WAITING_POINT;
            IsGeneric = myBase.IsGeneric;
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
            outf.Write(OriginalDelay);
        }

        public override AIActionItem Handler(params object[] list)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                LinkedAuxAction = true;
                info = new AuxActionWPItem(this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
                info.SetParam((float)list[0], (float)list[1], (float)list[2], (float)list[3]);
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
            return info;
        }

        public override AIActionItem CheckGenActions(WorldLocation location, AITrain thisTrain, params object[] list)
        {
            AIActionItem newAction = null;
            int SpeedMps = (int)thisTrain.SpeedMpS;
            TrainCar locomotive = thisTrain.FindLeadLocomotive();
            if (Math.Abs(SpeedMps) <= Simulator.MaxStoppedMpS)   //  We call the handler to generate an actionRef
            {
                newAction = Handler(0f, 0f, thisTrain.DistanceTravelledM, thisTrain.DistanceTravelledM);

                Register(thisTrain.Number, location);
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "Caller registered for\n");
#endif
            }
            return newAction;
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
            float[] distancesM = new float[2];

            int thisSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - thisTrain.PresentPosition[0].TCOffset;

            // Get action route index - if not found, return distances < 0
            int actionIndex0 = thisTrain.PresentPosition[0].RouteListIndex;
            int actionRouteIndex = thisTrain.ValidRoute[0].GetRouteIndex(TCSectionIndex, actionIndex0);
            float activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, actionRouteIndex, this.RequiredDistance, thisTrain.AITrainDirectionForward, thisTrain.signalRef);


            // If reschedule, use actual speed
            float triggerDistanceM = TriggerDistance;

            if (thisTrain.TrainType != Train.TRAINTYPE.AI_PLAYERDRIVEN)
            {

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

                        if (firstPartRangeM < remainingRangeM && thisTrain.SpeedMpS < thisTrain.TrainMaxSpeedMpS) // If distance left and not at max speed
                        {
                            // Split remaining distance based on relation between acceleration and deceleration
                            secndPartRangeM = (remainingRangeM - firstPartRangeM) * (2.0f * aiTrain.MaxDecelMpSS) / (aiTrain.MaxDecelMpSS + aiTrain.MaxAccelMpSS);
                        }

                        triggerDistanceM = activateDistanceTravelledM - (firstPartRangeM + secndPartRangeM);
                    }
                    else
                    {
                        // Use maximum speed
                        float deltaTime = thisTrain.TrainMaxSpeedMpS / aiTrain.MaxDecelMpSS;
                        float brakingDistanceM = (thisTrain.TrainMaxSpeedMpS * deltaTime) + (0.5f * aiTrain.MaxDecelMpSS * deltaTime * deltaTime);
                        triggerDistanceM = activateDistanceTravelledM - brakingDistanceM;
                    }
                }
                else
                {
                    activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, actionRouteIndex, this.RequiredDistance, true, thisTrain.signalRef);
                    triggerDistanceM = activateDistanceTravelledM;
                }

                distancesM[1] = triggerDistanceM;
                if (activateDistanceTravelledM < thisTrain.PresentPosition[0].DistanceTravelledM &&
                    thisTrain.PresentPosition[0].DistanceTravelledM - activateDistanceTravelledM < thisTrain.Length)
                    activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM;
                distancesM[0] = activateDistanceTravelledM;

                return distancesM;
            }
            else
            {
                activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, actionRouteIndex, this.RequiredDistance, true, thisTrain.signalRef);
                triggerDistanceM = activateDistanceTravelledM - Math.Min(this.RequiredDistance, 300);

                if (activateDistanceTravelledM < thisTrain.PresentPosition[0].DistanceTravelledM &&
                    thisTrain.PresentPosition[0].DistanceTravelledM - activateDistanceTravelledM < thisTrain.Length)
                {
                    activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM;
                    triggerDistanceM = activateDistanceTravelledM;
                }
                distancesM[1] = triggerDistanceM;
                distancesM[0] = activateDistanceTravelledM;

                return distancesM;
            }
        }


        public override List<KeyValuePair<System.Type, AuxActionRef>> GetCallFunction()
        {
            List<KeyValuePair<System.Type, AuxActionRef>> listInfo = new List<KeyValuePair<System.Type, AuxActionRef>>();

            System.Type managed = typeof(SignalObject);
            KeyValuePair<System.Type, AuxActionRef> info = new KeyValuePair<System.Type, AuxActionRef>(managed, this);
            listInfo.Add(info);
            return listInfo;
        }

    }
    //================================================================================================//
    /// <summary>
    /// AIActionHornRef
    /// Start and Stop the horn
    /// </summary>
    public class AIActionHornRef : AIAuxActionsRef
    {
        /// <summary>
        /// The duration of the horn blast, if specified by an activity event.
        /// </summary>
        private int? DurationS { get; }

        /// <summary>
        /// The horn pattern to use.
        /// </summary>
        private AILevelCrossingHornPattern HornPattern { get; set; }

        public AIActionHornRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir, int? durationS, AILevelCrossingHornPattern hornPattern)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir, AUX_ACTION.SOUND_HORN)
        {
            DurationS = durationS;
            HornPattern = hornPattern;
            NextAction = AUX_ACTION.SOUND_HORN;
        }

        public AIActionHornRef(Train thisTrain, BinaryReader inf)
            : base(thisTrain, inf, AUX_ACTION.SOUND_HORN)
        {
            if (inf.ReadBoolean())
                DurationS = inf.ReadInt32();
            HornPattern = AILevelCrossingHornPattern.Restore(inf);
            NextAction = AUX_ACTION.SOUND_HORN;
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tRestore one WPAuxAction" +
                "Position in file: " + inf.BaseStream.Position +
                " type Action: " + NextAction.ToString() +
                " Delay: " + Delay + "\n");
#endif
        }

        public AIActionHornRef(Train thisTrain, AuxActionHorn myBase, int nop = 0)
            : base(thisTrain, 0f, 0f, 0, 0, 0, 0, myBase.ActionType)
        {
            DurationS = myBase.Delay;
            NextAction = AUX_ACTION.SOUND_HORN;
            IsGeneric = myBase.IsGeneric;
            HornPattern = AILevelCrossingHornPattern.CreateInstance(myBase.Pattern);
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
            outf.Write(DurationS.HasValue);
            if (DurationS.HasValue)
                outf.Write(DurationS.Value);
            HornPattern.Save(outf);
        }

        public override AIActionItem Handler(params object[] list)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                LinkedAuxAction = true;
                info = new AuxActionHornItem(this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION, DurationS, HornPattern);
                info.SetParam((float)list[0], (float)list[1], (float)list[2], (float)list[3]);
            }
            return info;
        }

        //public bool CheckGenActions(System.Type typeSource, float rearDist, float frontDist, WorldLocation location, uint trackNodeIndex)
        public override AIActionItem CheckGenActions(WorldLocation location, AITrain thisTrain, params object[] list)
        {
            AIActionItem newAction = null;
            float rearDist = (float)list[0];
            float frontDist = (float)list[1];
            uint trackNodeIndex = (uint)list[2];
            float minDist = Math.Min(Math.Abs(rearDist), frontDist);

            float[] distances = GetActivationDistances(thisTrain, location);

#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "GenFunctions not yet defined for train:" + thisTrain.Number + 
                " Activation Distance: " + distances[0] + " & train distance: " + (-minDist) + "\n");
#endif
            if (distances[0] >= -minDist) // We call the handler to generate an actionRef
            {
                HornPattern = (AILevelCrossingHornPattern)list[3];
                newAction = Handler(distances[0] + thisTrain.DistanceTravelledM, thisTrain.SpeedMpS, distances[0] + thisTrain.DistanceTravelledM, thisTrain.DistanceTravelledM);
                Register(thisTrain.Number, location);
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "Caller registered for\n");
#endif
            }
            return newAction;
        }

        public override List<KeyValuePair<System.Type, AuxActionRef>> GetCallFunction()
        {
            System.Type managed = typeof(LevelCrossings);
            KeyValuePair<System.Type, AuxActionRef> info = new KeyValuePair<System.Type, AuxActionRef>(managed, this);
            List<KeyValuePair<System.Type, AuxActionRef>> listInfo = new List<KeyValuePair<System.Type, AuxActionRef>>
            {
                info
            };
            return listInfo;
        }

        // Start horn whatever the speed.
        public override float[] CalculateDistancesToNextAction(Train thisTrain, float presentSpeedMpS, bool reschedule)
        {
            int thisSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - thisTrain.PresentPosition[0].TCOffset;

            // Get action route index - if not found, return distances < 0
            int actionIndex0 = thisTrain.PresentPosition[0].RouteListIndex;
            int actionRouteIndex = thisTrain.ValidRoute[0].GetRouteIndex(TCSectionIndex, actionIndex0);
            float activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, actionRouteIndex, this.RequiredDistance, thisTrain.AITrainDirectionForward, thisTrain.signalRef);
            float[] distancesM = new float[2];
            distancesM[1] = activateDistanceTravelledM;
            distancesM[0] = activateDistanceTravelledM;

            return distancesM;
        }

        // SPA: We use this fonction and not the one from Train in order to leave control to the AuxAction
        public override float[] GetActivationDistances(Train thisTrain, WorldLocation location)
        {
            float[] distancesM = new float[2];
            distancesM[0] = this.RequiredDistance;   // 
            distancesM[1] = this.RequiredDistance + thisTrain.Length;
            return distancesM;
        }

    }

    //================================================================================================//
    /// <summary>
    /// AIActionControlledStartRef
    /// Used to start a steam engine when it is  an Train
    /// </summary>
    public class AIActionControlStartRef : AIAuxActionsRef
    {
        public int ActionDuration;

        public AIActionControlStartRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir, AuxActionRef.AUX_ACTION actionType = AuxActionRef.AUX_ACTION.CONTROL_START, int duration = 10)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir)
        {
            NextAction = AUX_ACTION.CONTROL_START;
            ActionDuration = duration;
        }

        public AIActionControlStartRef(Train thisTrain, BinaryReader inf, AuxActionRef.AUX_ACTION actionType = AuxActionRef.AUX_ACTION.CONTROL_START)
            : base(thisTrain, inf, actionType)
        {
            Delay = inf.ReadInt32();
            NextAction = AUX_ACTION.CONTROL_START;
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tRestore one WPAuxAction" +
                "Position in file: " + inf.BaseStream.Position +
                " type Action: " + NextAction.ToString() +
                " Delay: " + Delay + "\n");
#endif
        }

        public AIActionControlStartRef(Train thisTrain, AuxControlStart myBase, int nop = 0)
            : base(thisTrain, 0f, 0f, 0, 0, 0, 0, myBase.ActionType)
        {
            Delay = myBase.ActivationDelay;
            NextAction = AUX_ACTION.CONTROL_START;
            IsGeneric = myBase.IsGeneric;
            ActionDuration = myBase.ActionDuration;
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

        public override AIActionItem Handler(params object[] list)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                LinkedAuxAction = true;
                info = new AuxActionControlStartItem(this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
                info.SetParam(1f, (float)list[0], 1f, 1f);
                ((AuxActionControlStartItem)info).SetDelay((int)list[1]);
                ((AuxActionControlStartItem)info).Duration = ActionDuration;
                ((AuxActionControlStartItem)info).PreDelay = Delay;
            }
            return info;
        }

        public override AIActionItem CheckGenActions(WorldLocation location, AITrain thisTrain, params object[] list)
        {
            AIActionItem newAction = null;
            int SpeedMps = (int)thisTrain.SpeedMpS;
            TrainCar locomotive = thisTrain.FindLeadLocomotive();
            if (!(locomotive is MSTSSteamLocomotive))
            {
                return null;
            }
            if (Math.Abs(SpeedMps) <= Simulator.MaxStoppedMpS && (int)list[0] >= Delay) // We call the handler to generate an actionRef
            {
                newAction = Handler(thisTrain.SpeedMpS, (int)list[0]);

                Register(thisTrain.Number, location);
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "Caller registered for\n");
#endif
            }
            return newAction;
        }

        public override List<KeyValuePair<System.Type, AuxActionRef>> GetCallFunction()
        {
            List<KeyValuePair<System.Type, AuxActionRef>> listInfo = new List<KeyValuePair<System.Type, AuxActionRef>>();

            System.Type managed = typeof(AuxActionWPItem);
            KeyValuePair<System.Type, AuxActionRef> info = new KeyValuePair<System.Type, AuxActionRef>(managed, this);
            listInfo.Add(info);
            return listInfo;
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

        // Start horn whatever the speed.
        public override float[] CalculateDistancesToNextAction(Train thisTrain, float presentSpeedMpS, bool reschedule)
        {
            int thisSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - thisTrain.PresentPosition[0].TCOffset;

            // Get action route index - if not found, return distances < 0
            int actionIndex0 = thisTrain.PresentPosition[0].RouteListIndex;
            int actionRouteIndex = thisTrain.ValidRoute[0].GetRouteIndex(TCSectionIndex, actionIndex0);
            float activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, actionRouteIndex, this.RequiredDistance, thisTrain.AITrainDirectionForward, thisTrain.signalRef);
            float[] distancesM = new float[2];
            distancesM[1] = activateDistanceTravelledM;
            distancesM[0] = activateDistanceTravelledM;

            return distancesM;
        }

        // SPA: We use this fonction and not the one from Train in order to leave control to the AuxAction
        public override float[] GetActivationDistances(Train thisTrain, WorldLocation location)
        {
            float[] distancesM = new float[2];
            distancesM[0] = 0f; // Dès 100m (French?)
            distancesM[1] = 0f;
            return distancesM;
        }

    }


    //================================================================================================//
    /// <summary>
    /// AIActionSignalRef
    /// A single Reference object used to add some delay before starting at a Signal
    /// </summary>
    public class AIActionSignalRef : AIAuxActionsRef
    {
        public AIActionSignalRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir, AUX_ACTION.CONTROL_START)
        {
            NextAction = AUX_ACTION.CONTROL_START;
            IsGeneric = true;
        }

        public override AIActionItem Handler(params object[] list)
        {
            AIActionItem info = null;
            if (!LinkedAuxAction || IsGeneric)
            {
                LinkedAuxAction = true;
                info = new AuxActionSignalItem(this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
                info.SetParam((float)list[0], (float)list[1], (float)list[2], (float)list[3]);
                ((AuxActionSignalItem)info).SetDelay(Delay);
            }
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

        //  Start horn whatever the speed
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

            return distancesM;
        }
    }

    //================================================================================================//
    /// <summary>
    /// AIActSigDelegateRef
    /// An action to delegate the Signal management from a WP
    /// </summary>
    public class AIActSigDelegateRef : AIAuxActionsRef
    {
        public bool IsAbsolute = false;
        public AIActionWPRef AssociatedWPAction;
        public float brakeSection;
        protected AuxActSigDelegate AssociatedItem = null; // In order to Unlock the signal when removing Action Reference

        public AIActSigDelegateRef(Train thisTrain, float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir, AIActionWPRef associatedWPAction = null)
            : base(thisTrain, distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir, AUX_ACTION.SIGNAL_DELEGATE)
        {
            AssociatedWPAction = associatedWPAction;
            NextAction = AUX_ACTION.SIGNAL_DELEGATE;
            IsGeneric = true;

            brakeSection = distance; // Set to 1 later when applicable
        }

        public AIActSigDelegateRef(Train thisTrain, BinaryReader inf)
            : base(thisTrain, inf, AUX_ACTION.SIGNAL_DELEGATE)
        {
            Delay = inf.ReadInt32();
            brakeSection = inf.ReadSingle();
            IsAbsolute = inf.ReadBoolean();
            NextAction = AUX_ACTION.SIGNAL_DELEGATE;
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
            File.AppendAllText(@"C:\temp\checkpath.txt", "\tSave one SigDelegate, count :" + cnt +
                "Position in file: " + outf.BaseStream.Position +
                " type Action: " + NextAction.ToString() +
                " Delay: " + Delay + "\n");
#endif
            base.save(outf, cnt);
            outf.Write(Delay);
            outf.Write(brakeSection);
            outf.Write(IsAbsolute);
            var hasWPActionAssociated = false;
            if (AssociatedWPAction != null) hasWPActionAssociated = true;
            outf.Write(hasWPActionAssociated);
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
            {
                return true;
            }

            return false;
        }

        public override AIActionItem Handler(params object[] list)
        {
            if (AssociatedItem != null)
                return null;
            AuxActSigDelegate info = new AuxActSigDelegate(this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
            info.SetParam((float)list[0], (float)list[1], (float)list[2], (float)list[3]);
            AssociatedItem = info;
            return info;
        }

        // SigDelegateRef.
        public override float[] CalculateDistancesToNextAction(Train thisTrain, float presentSpeedMpS, bool reschedule)
        {
            int thisSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - thisTrain.PresentPosition[0].TCOffset;

            // Get action route index - if not found, return distances < 0
            int actionIndex0 = thisTrain.PresentPosition[0].RouteListIndex;
            int actionRouteIndex = thisTrain.ValidRoute[0].GetRouteIndex(TCSectionIndex, actionIndex0);
            float activateDistanceTravelledM = -1;

            if (actionIndex0 != -1 && actionRouteIndex != -1)
                activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, actionRouteIndex, this.RequiredDistance, true, thisTrain.signalRef);

            var currBrakeSection = (thisTrain is AITrain && !(thisTrain.TrainType == Train.TRAINTYPE.AI_PLAYERDRIVEN)) ? 1 : brakeSection;
            float triggerDistanceM = activateDistanceTravelledM - Math.Min(this.RequiredDistance, 300);   //  TODO, add the size of train

            float[] distancesM = new float[2];
            distancesM[1] = triggerDistanceM;
            if (activateDistanceTravelledM < thisTrain.PresentPosition[0].DistanceTravelledM &&
                thisTrain.PresentPosition[0].DistanceTravelledM - activateDistanceTravelledM < thisTrain.Length)
                activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM;
            distancesM[0] = activateDistanceTravelledM;

            return distancesM;
        }

        public void SetEndSignalIndex(int idx)
        {
            EndSignalIndex = idx;
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
        public AuxActionRef ActionRef;
        public bool Triggered = false;
        public bool Processing = false;
        public AITrain.AI_MOVEMENT_STATE currentMvmtState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
        public SignalObject SignalReferenced { get { return ((AIAuxActionsRef)ActionRef).SignalReferenced; } set { } }

        //================================================================================================//
        /// <summary>
        /// AuxActionItem
        /// The basic constructor
        /// </summary>
        public AuxActionItem(AuxActionRef thisItem, AI_ACTION_TYPE thisAction) :
            base(null, thisAction)
        {
            NextAction = AI_ACTION_TYPE.AUX_ACTION;
            ActionRef = thisItem;
        }

        //================================================================================================//
        //
        // Restore
        //
        public AuxActionItem(BinaryReader inf, Signals signalRef)
            : base(inf, signalRef)
        {
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

        public override AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime)
        {
            int correctedTime = presentTime;
            switch (currentMvmtState)
            {
                case AITrain.AI_MOVEMENT_STATE.INIT_ACTION:
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    currentMvmtState = InitAction(thisTrain, presentTime, 0f, currentMvmtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    currentMvmtState = HandleAction(thisTrain, presentTime, 0f, currentMvmtState);
                    break;
                default:
                    break;
            }
            return currentMvmtState;
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
        public AuxActionWPItem(AuxActionRef thisItem, AI_ACTION_TYPE thisAction) :
            base(thisItem, thisAction)
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
            if (ActionRef == null || thisTrain.PresentPosition[0].RouteListIndex == -1)
                return false;
            float[] distancesM = ((AIAuxActionsRef)ActionRef).CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);
            if (thisTrain.TrainType != Train.TRAINTYPE.AI_PLAYERDRIVEN)
            {
                if (RequiredDistance < thisTrain.DistanceTravelledM) // trigger point
                {
                    return true;
                }

                RequiredDistance = distancesM[1];
                ActivateDistanceM = distancesM[0];
                return false;
            }
            else
            {
                return Math.Abs(thisTrain.SpeedMpS) <= 0.1f && distancesM[1] <= thisTrain.DistanceTravelledM;
            }
        }

        public void SetDelay(int delay)
        {
            Delay = delay;
        }

        public override bool ValidAction(Train thisTrain)
        {
            bool actionValid = false;

            actionValid = CanActivate(thisTrain, thisTrain.SpeedMpS, true);
            if (thisTrain is AITrain && thisTrain.TrainType != Train.TRAINTYPE.AI_PLAYERDRIVEN)
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

                // Repeat stopping of train, because it could have been moved by UpdateBrakingState after ProcessAction
                if (aiTrain.TrainType != Train.TRAINTYPE.AI_PLAYERDRIVEN)
                {
                    aiTrain.AdjustControlsBrakeMore(aiTrain.MaxDecelMpSS, elapsedClockSeconds, 100);
                    aiTrain.SpeedMpS = 0;
                }
                int correctedTime = presentTime;
                // If delay between 40000 and 60000 an uncoupling is performed and delay is returned with the two lowest digits of the original one
                aiTrain.TestUncouple(ref Delay);
                // If delay between 30000 and 40000 it is considered an absolute delay in the form 3HHMM, where HH and MM are hour and minute where the delay ends
                thisTrain.TestAbsDelay(ref Delay, correctedTime);
                // If delay equal to 60001 it is considered as a command to unconditionally attach to the nearby train;
                aiTrain.TestUncondAttach(ref Delay);
                // If delay equal to 60002 it is considered as a request for permission to pass signal;
                aiTrain.TestPermission(ref Delay);
                ActualDepart = correctedTime + Delay;
                aiTrain.AuxActionsContain.CheckGenActions(this.GetType(), aiTrain.RearTDBTraveller.WorldLocation, Delay);

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
                if (thisTrain.TrainType != Train.TRAINTYPE.AI_PLAYERDRIVEN)
                {
                    thisTrain.SpeedMpS = 0;
                }
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
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "WP, Action ended for train " + thisTrain.Number + " at " + presentTime + "(STOPPED)\n");
#endif
                    if (thisTrain.AuxActionsContain.CountSpec() > 0)
                        thisTrain.AuxActionsContain.Remove(this);
                    return AITrain.AI_MOVEMENT_STATE.STOPPED;
                }
            }
            else
            {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "WP, Action ended for train " + thisTrain.Number + " at " + presentTime + "(STOPPED)\n");
#endif
                if (thisTrain.AuxActionsContain.CountSpec() > 0)
                    thisTrain.AuxActionsContain.Remove(this);
                return AITrain.AI_MOVEMENT_STATE.STOPPED;
            }
            return movementState;
        }

        public override AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            //int correctedTime = presentTime;
            //switch (movementState)
            //{
            AITrain.AI_MOVEMENT_STATE mvtState = movementState;
            if (ActionRef.IsGeneric)
                mvtState = currentMvmtState;
            int correctedTime = presentTime;
            switch (mvtState)
            {
                case AITrain.AI_MOVEMENT_STATE.INIT_ACTION:
                    movementState = InitAction(thisTrain, presentTime, elapsedClockSeconds, movementState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    movementState = HandleAction(thisTrain, presentTime, elapsedClockSeconds, movementState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.BRAKING:
                    if (thisTrain is AITrain)
                    {
                        AITrain aiTrain = thisTrain as AITrain;
                        float distanceToGoM = thisTrain.activityClearingDistanceM;
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
                        else if (distanceToGoM < AITrain.signalApproachDistanceM && Math.Abs(aiTrain.SpeedMpS) <= 0.1f)
                        {
                            aiTrain.AdjustControlsBrakeMore(aiTrain.MaxDecelMpSS, elapsedClockSeconds, 100);
                            movementState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
                        }
                    }
                    else
                    {
                        if (thisTrain.AuxActionsContain.CountSpec() > 0)
                            thisTrain.AuxActionsContain.RemoveAt(0);
                    }
                    break;
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    if (!(thisTrain is AITrain))
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

                        //movementState = thisTrain.UpdateStoppedState(); // Don't call UpdateStoppedState(), WP can't touch Signal
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
            if (ActionRef.IsGeneric)
                currentMvmtState = movementState;
            return movementState;
        }

    }

    //================================================================================================//
    /// <summary>
    /// AuxActionHornItem
    /// A specific class used at run time to manage a Horn Action
    /// </summary>
    public class AuxActionHornItem : AuxActionItem
    {
        [JsonProperty("DurationS")]
        private int? DurationS { get; }
        [JsonIgnore]
        private AILevelCrossingHornPattern HornPattern { get; }
        [JsonIgnore]
        private int NextStepTimeS { get; set; }
        [JsonIgnore]
        private IEnumerator<int> Execution { get; set; }


        //================================================================================================//
        /// <summary>
        /// AuxActionhornItem
        /// The specific constructor for horn action
        /// </summary>
        public AuxActionHornItem(AuxActionRef thisItem, AI_ACTION_TYPE thisAction, int? durationS, AILevelCrossingHornPattern hornPattern) :
            base(thisItem, thisAction)
        {
            DurationS = durationS;
            HornPattern = hornPattern;
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
            float[] distancesM = ((AIAuxActionsRef)ActionRef).CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);


            if (RequiredDistance < thisTrain.DistanceTravelledM) // trigger point
            {
                return true;
            }

            RequiredDistance = distancesM[1];
            ActivateDistanceM = distancesM[0];
            return false;
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
            if (!Triggered)
            {
                NextStepTimeS = correctedTime;
                var locomotive = (MSTSLocomotive)thisTrain.FindLeadLocomotive();
                Execution = HornPattern.Execute(locomotive, DurationS);
#if WITH_PATH_DEBUG
                    File.AppendAllText(@"C:\temp\checkpath.txt", "Do Horn for AITRain " + thisTrain.Number + " , mvt state " + movementState.ToString() + " at " + presentTime + "\n");
#endif
                Triggered = true;
            }
            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }

        public override AITrain.AI_MOVEMENT_STATE HandleAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            if (Triggered && presentTime > NextStepTimeS)
            {
                // Advance to the next step.
                if (Execution.MoveNext())
                {
                    NextStepTimeS = presentTime + Execution.Current;
                    return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
                }
                else
                {
                    thisTrain.AuxActionsContain.Remove(this);
                    Triggered = false;
                    return currentMvmtState; // Restore previous MovementState
                }
            }
            else
            {
                return movementState;
            }
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
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    movementState = HandleAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.BRAKING:
                    if (this.ActionRef.ActionType != AuxActionRef.AUX_ACTION.SOUND_HORN)
                    {
                        float distanceToGoM = thisTrain.activityClearingDistanceM;
                        distanceToGoM = ActivateDistanceM - thisTrain.PresentPosition[0].DistanceTravelledM;
                        float NextStopDistanceM = distanceToGoM;
                        if (distanceToGoM < 0f)
                        {
                            currentMvmtState = movementState;
                            movementState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
                        }
                    }
                    break;
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    if (thisTrain is AITrain train)
                        movementState = train.UpdateStoppedState(elapsedClockSeconds);
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
    /// AuxActionControlledStartItem
    /// A specific class used at run time to manage the starting of a steam train
    /// </summary>
    public class AuxActionControlStartItem : AuxActionItem
    {
        public enum LOCAL_ACTION
        {
            NOT_STARTED,
            INIT_DONE,
            STEP1,
            STEP2,
            END
        }

        public int PreDelay;    //  PreDelay gives the time in second before the action must take effect
        public int Delay;       //  Delay is the delay of the caller
        public int Duration;    //  ActionDuration gives how long it takes
        public int ActualDepart;
        LOCAL_ACTION localStep = LOCAL_ACTION.NOT_STARTED;

        //================================================================================================//
        /// <summary>
        /// AuxActionControlStartItem
        /// The specific constructor for Controlled Start action
        /// </summary>
        public AuxActionControlStartItem(AuxActionRef thisItem, AI_ACTION_TYPE thisAction) :
            base(thisItem, thisAction)
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
            float[] distancesM = ((AIAuxActionsRef)ActionRef).CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);

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
                return currentMvmtState; // Restore previous MovementState
            }
            Processing = true;
            int correctedTime = presentTime;
            ActualDepart = correctedTime + (Delay - PreDelay);
            localStep = LOCAL_ACTION.INIT_DONE; // NOT_STARTED -> INIT_DONE
            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }

        public override AITrain.AI_MOVEMENT_STATE HandleAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            MSTSNotchController blowerControl;
            float currentBlower = 0;
            TrainCar locomotive = thisTrain.FindLeadLocomotive();
            MSTSSteamLocomotive steamLocomotive = locomotive as MSTSSteamLocomotive;
            blowerControl = steamLocomotive.BlowerController;
            if (!Triggered && localStep == LOCAL_ACTION.INIT_DONE && ActualDepart < presentTime)
            {
                steamLocomotive.StartBlowerIncrease(100);
                steamLocomotive.StartFiringRateIncrease(100.0f);
                steamLocomotive.ToggleCylinderCocks();
                Triggered = true;
                localStep = LOCAL_ACTION.STEP1;
                ActualDepart += Duration;
                currentBlower = blowerControl.CurrentValue * 100;
            }
            else if (Triggered && localStep == LOCAL_ACTION.STEP1 && ActualDepart < presentTime)
            {
                steamLocomotive.StartBlowerDecrease(currentBlower);
                steamLocomotive.StartFiringRateDecrease(0.0f);
                localStep = LOCAL_ACTION.END;
            }
            else if (Triggered && localStep == LOCAL_ACTION.END)
            {
                thisTrain.AuxActionsContain.Remove(this);
                if (locomotive is MSTSSteamLocomotive)
                {
                    steamLocomotive.ToggleCylinderCocks();
                }

                if (Triggered)
                {
#if WITH_PATH_DEBUG
                File.AppendAllText(@"C:\temp\checkpath.txt", "Stop Horn for AITRain " + thisTrain.Number + " : mvt state " + movementState.ToString() + " at " + presentTime + "\n");
#endif
                    locomotive = thisTrain.FindLeadLocomotive();
                    ((MSTSLocomotive)locomotive).ManualHorn = false;
                }
                return currentMvmtState; // Restore previous MovementState
            }
            return movementState;
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
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    movementState = HandleAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.BRAKING:
                    float distanceToGoM = thisTrain.activityClearingDistanceM;
                    distanceToGoM = ActivateDistanceM - thisTrain.PresentPosition[0].DistanceTravelledM;
                    float NextStopDistanceM = distanceToGoM;
                    if (distanceToGoM < 0f)
                    {
                        currentMvmtState = movementState;
                        movementState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
                    }

                    break;
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    if (thisTrain is AITrain train)
                        movementState = train.UpdateStoppedState(elapsedClockSeconds);
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
    /// AuxActionControlStoppedItem
    /// A specific class used at run time to manage the starting of a steam train
    /// </summary>
    public class AuxActionControlStoppedItem : AuxActionItem
    {
        public enum LOCAL_ACTION
        {
            NOT_STARTED,
            INIT_DONE,
            STEP1,
            STEP2,
            END
        }

        public int PreDelay;    //  PreDelay gives the time in second before the action must take effect
        public int Delay;       //  Delay is the delay of the caller
        public int Duration;    //  ActionDuration gives how long it takes
        public int ActualDepart;
        LOCAL_ACTION localStep = LOCAL_ACTION.NOT_STARTED;

        //================================================================================================//
        /// <summary>
        /// AuxActionControlItem
        /// The specific constructor for Control Stopped action
        /// </summary>
        public AuxActionControlStoppedItem(AuxActionRef thisItem, AI_ACTION_TYPE thisAction) :
            base(thisItem, thisAction)
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
            float[] distancesM = ((AIAuxActionsRef)ActionRef).CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);

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
                return currentMvmtState; // Restore previous MovementState
            }
            Processing = true;
            int correctedTime = presentTime;
            ActualDepart = correctedTime + (Delay - PreDelay);
            localStep = LOCAL_ACTION.INIT_DONE; // NOT_STARTED -> INIT_DONE
            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }

        public override AITrain.AI_MOVEMENT_STATE HandleAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            MSTSNotchController blowerControl;
            float currentBlower = 0;
            TrainCar locomotive = thisTrain.FindLeadLocomotive();
            MSTSSteamLocomotive steamLocomotive = locomotive as MSTSSteamLocomotive;
            blowerControl = steamLocomotive.BlowerController;
            if (!Triggered && localStep == LOCAL_ACTION.INIT_DONE && ActualDepart < presentTime)
            {
                steamLocomotive.StartBlowerIncrease(100);
                steamLocomotive.StartFiringRateIncrease(100.0f);
                steamLocomotive.ToggleCylinderCocks();
                Triggered = true;
                localStep = LOCAL_ACTION.STEP1;
                ActualDepart += Duration;
                currentBlower = blowerControl.CurrentValue * 100;
            }
            else if (Triggered && localStep == LOCAL_ACTION.STEP1 && ActualDepart < presentTime)
            {
                steamLocomotive.StartBlowerDecrease(currentBlower);
                steamLocomotive.StartFiringRateDecrease(0.0f);
                localStep = LOCAL_ACTION.END;
            }
            else if (Triggered && localStep == LOCAL_ACTION.END)
            {
                thisTrain.AuxActionsContain.Remove(this);
                if (locomotive is MSTSSteamLocomotive)
                {
                    steamLocomotive.ToggleCylinderCocks();
                }

                if (Triggered)
                {
#if WITH_PATH_DEBUG
                File.AppendAllText(@"C:\temp\checkpath.txt", "Stop Horn for AITRain " + thisTrain.Number + " : mvt state " + movementState.ToString() + " at " + presentTime + "\n");
#endif
                    locomotive = thisTrain.FindLeadLocomotive();
                    ((MSTSLocomotive)locomotive).ManualHorn = false;
                }
                return currentMvmtState; // Restore previous MovementState
            }
            return movementState;
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
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    movementState = HandleAction(thisTrain, presentTime, elapsedClockSeconds, mvtState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.BRAKING:
                    float distanceToGoM = thisTrain.activityClearingDistanceM;
                    distanceToGoM = ActivateDistanceM - thisTrain.PresentPosition[0].DistanceTravelledM;
                    float NextStopDistanceM = distanceToGoM;
                    if (distanceToGoM < 0f)
                    {
                        currentMvmtState = movementState;
                        movementState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
                    }

                    break;
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    if (thisTrain is AITrain train)
                        movementState = train.UpdateStoppedState(elapsedClockSeconds);
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
    /// AuxActionSignalItem
    /// A specific class used at run time to manage a Signal Action
    /// </summary>
    public class AuxActionSignalItem : AuxActionItem
    {
        int Delay;
        public int ActualDepart;
        bool HornTriggered = false;

        //================================================================================================//
        /// <summary>
        /// AuxActionSignalItem
        /// The specific constructor for Signal action
        /// </summary>
        public AuxActionSignalItem(AuxActionRef thisItem, AI_ACTION_TYPE thisAction) :
            base(thisItem, thisAction)
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
            float[] distancesM = ((AIAuxActionsRef)ActionRef).CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);

            if (RequiredDistance < thisTrain.DistanceTravelledM) // Trigger point
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
                    ((MSTSLocomotive)locomotive).ManualHorn = true;
                    HornTriggered = true;
                }
                movementState = AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
            }
            else
            {
                if (!ActionRef.IsGeneric)
                    thisTrain.AuxActionsContain.RemoveAt(0);
                //thisTrain.ResetActions(true);
                if (HornTriggered)
                {
                    TrainCar locomotive = thisTrain.FindLeadLocomotive();
                    ((MSTSLocomotive)locomotive).ManualHorn = false;
                }
                return currentMvmtState; // Restore previous MovementState
            }
            return movementState;
        }

    }

    //================================================================================================//
    /// <summary>
    /// AuxActSigDelegate
    /// Used to postpone the signal clear after WP
    /// </summary>
    public class AuxActSigDelegate : AuxActionItem
    {
        public int ActualDepart;
        public bool locked = true;

        //================================================================================================//
        /// <summary>
        /// AuxActSigDelegate Item
        /// The specific constructor for AuxActSigDelegate action
        /// </summary>
        public AuxActSigDelegate(AuxActionRef thisItem, AI_ACTION_TYPE thisAction) :
            base(thisItem, thisAction)
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
            if (ActionRef != null && ((AIAuxActionsRef)ActionRef).LinkedAuxAction)
                return false;
            float[] distancesM = ((AIAuxActionsRef)ActionRef).CalculateDistancesToNextAction(thisTrain, SpeedMpS, reschedule);
            if (distancesM[0] < thisTrain.DistanceTravelledM && !((AIActSigDelegateRef)ActionRef).IsAbsolute) // trigger point
            {
                if (thisTrain.SpeedMpS > 0f)
                {
                    if (thisTrain is AITrain && thisTrain.TrainType != Train.TRAINTYPE.AI_PLAYERDRIVEN)
                    {
                        thisTrain.SetTrainOutOfControl(Train.OUTOFCONTROL.OUT_OF_PATH);
                        return true;
                    }
                    else return false;
                }
            }

            if (!reschedule && distancesM[1] < thisTrain.DistanceTravelledM && (Math.Abs(thisTrain.SpeedMpS) <= 0.1f ||
                (thisTrain.IsPlayerDriven && currentMvmtState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION)))
            {
                return true;
            }

            if (!reschedule && ((AIActSigDelegateRef)ActionRef).IsAbsolute)
            {
                TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[((AIActSigDelegateRef)ActionRef).TCSectionIndex];
                if (((thisSection.CircuitState.TrainReserved != null && thisSection.CircuitState.TrainReserved.Train == thisTrain) || thisSection.CircuitState.ThisTrainOccupying(thisTrain)) &&
                    ((AIActSigDelegateRef)ActionRef).EndSignalIndex != -1)
                    return true;
            }

            //RequiredDistance = distancesM[1];
            //ActivateDistanceM = distancesM[0];
            return false;
        }

        public override bool CanRemove(Train thisTrain)
        {
            return Processing && (currentMvmtState == AITrain.AI_MOVEMENT_STATE.STOPPED || currentMvmtState == AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION)
|| SignalReferenced == null;
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
            int delay = ((AIActSigDelegateRef)ActionRef).Delay;
            // If delay between 30000 and 40000 it is considered an absolute delay in the form 3HHMM, where HH and MM are hour and minute where the delay ends
            thisTrain.TestAbsDelay(ref delay, presentTime);
            ActualDepart = presentTime + delay;
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
                if (SignalReferenced.HasLockForTrain(thisTrain.Number, thisTrain.TCRoute.activeSubpath))
                    SignalReferenced.UnlockForTrain(thisTrain.Number, thisTrain.TCRoute.activeSubpath);
                else
                {
                    // locked = true;
                    Trace.TraceWarning("SignalObject trItem={0}, trackNode={1}, wasn't locked for train {2}.",
                        SignalReferenced.trItem, SignalReferenced.trackNode, thisTrain.Number);
                }
            }
            if (ClearSignal(thisTrain) || (thisTrain.NextSignalObject[0] != null && (thisTrain.NextSignalObject[0].this_sig_lr(MstsSignalFunction.NORMAL) > MstsSignalAspect.STOP)) ||
                thisTrain.NextSignalObject[0] == null || SignalReferenced != thisTrain.NextSignalObject[0] ||
                thisTrain.PresentPosition[0].TCSectionIndex == thisTrain.ValidRoute[0][thisTrain.ValidRoute[0].Count - 1].TCSectionIndex)
            {
#if WITH_PATH_DEBUG
            File.AppendAllText(@"C:\temp\checkpath.txt", "WP, Action ended for train " + thisTrain.Number + " at " + presentTime + "(STOPPED)\n");
#endif
                if (((AIActSigDelegateRef)ActionRef).AssociatedWPAction != null)
                {
                    var WPAction = ((AIActSigDelegateRef)ActionRef).AssociatedWPAction.keepIt;
                    if (thisTrain.requiredActions.Contains(WPAction))
                    {
                        thisTrain.requiredActions.Remove(WPAction);
                    }
                    if (thisTrain.AuxActionsContain.specRequiredActions.Contains(WPAction))
                        thisTrain.AuxActionsContain.specRequiredActions.Remove(WPAction);
                    if (thisTrain.AuxActionsContain.SpecAuxActions.Contains(((AIActSigDelegateRef)ActionRef).AssociatedWPAction))
                        thisTrain.AuxActionsContain.SpecAuxActions.Remove(((AIActSigDelegateRef)ActionRef).AssociatedWPAction);
                }
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
                return thisTrain is AITrain && (thisTrain as AITrain).MovementState == AITrain.AI_MOVEMENT_STATE.STATION_STOP ? AITrain.AI_MOVEMENT_STATE.STATION_STOP : AITrain.AI_MOVEMENT_STATE.STOPPED;
            }
            return movementState;
        }

        public override AITrain.AI_MOVEMENT_STATE ProcessAction(Train thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            movementState = base.ProcessAction(thisTrain, presentTime, elapsedClockSeconds, movementState);
            return movementState;
        }
    }
    #endregion
}
