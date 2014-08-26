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
    #region AuxActionRef

    //================================================================================================//
    /// <summary>
    /// AIAuxActions
    /// info used to figure out one auxiliary action along the route.  It's a reference data, not a run data.
    /// </summary>

    public class AIAuxActions
    {
        public float RequiredSpeedMpS;
        public float RequiredDistance;
        public int SubrouteIndex;
        public int RouteIndex;
        public int TCSectionIndex;
        public int Direction;
        protected int TriggerDistance = 0;


        public enum AI_AUX_ACTION
        {
            WAITING_POINT,
            NONE
        }

        public AI_AUX_ACTION NextAction = AI_AUX_ACTION.NONE;

        //================================================================================================//
        /// <summary>
        /// AIAuxActions: Generic Constructor
        /// The specific datas are used to fired the Action.
        /// </summary>

        public AIAuxActions(float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
        {
            RequiredDistance = distance;
            RequiredSpeedMpS = requiredSpeedMpS;
            SubrouteIndex = subrouteIdx;
            RouteIndex = routeIdx;
            TCSectionIndex = sectionIdx;
            Direction = dir;
        }

        //================================================================================================//
        /// <summary>
        /// Handler
        /// Like a fabric, if other informations are needed, please define specific function that can be called on the new object
        /// </summary>


        public virtual AIActionItem Handler(float distance, float speed, float activateDistance, float insertedDistance)
        {
            AIActionItem info = new AuxActionItem(distance, speed, activateDistance, insertedDistance,
                            this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
            return info;
        }

        //================================================================================================//
        /// <summary>
        /// CalculateDistancesToNextAction
        /// PLease, don't use the default function, redefine it.
        /// </summary>

        public virtual float[] CalculateDistancesToNextAction(AITrain thisTrain, float presentSpeedMpS, bool reschedule)
        {
            float[] distancesM = new float[2];
            distancesM[1] = 0.0f;
            distancesM[0] = thisTrain.PresentPosition[0].DistanceTravelledM;

            return (distancesM);
        }

    }

    //================================================================================================//
    /// <summary>
    /// AIActionWP
    /// info used to figure out a Waiting Point along the route.
    /// </summary>

    public class AIActionWP : AIAuxActions
    {
        int Delay;

        public AIActionWP(float distance, float requiredSpeedMpS, int subrouteIdx, int routeIdx, int sectionIdx, int dir)
            : base(distance, requiredSpeedMpS, subrouteIdx, routeIdx, sectionIdx, dir)
        {
            NextAction = AI_AUX_ACTION.WAITING_POINT;
        }

        public override AIActionItem Handler(float distance, float speed, float activateDistance, float insertedDistance)
        {
            AuxActionWPItem info = new AuxActionWPItem(distance, speed, activateDistance, insertedDistance,
                            this, AIActionItem.AI_ACTION_TYPE.AUX_ACTION);
            info.SetDelay(Delay);
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

        public override float[] CalculateDistancesToNextAction(AITrain thisTrain, float presentSpeedMpS, bool reschedule)
        {
            int thisSectionIndex = thisTrain.PresentPosition[0].TCSectionIndex;
            TrackCircuitSection thisSection = thisTrain.signalRef.TrackCircuitList[thisSectionIndex];
            float leftInSectionM = thisSection.Length - thisTrain.PresentPosition[0].TCOffset;

            // get action route index - if not found, return distances < 0

            int actionIndex0 = thisTrain.ValidRoute[0].GetRouteIndex(thisTrain.PresentPosition[0].TCSectionIndex, thisTrain.PresentPosition[0].RouteListIndex);
            float activateDistanceTravelledM = thisTrain.PresentPosition[0].DistanceTravelledM + thisTrain.ValidRoute[0].GetDistanceAlongRoute(actionIndex0, leftInSectionM, TCSectionIndex, this.RequiredDistance, thisTrain.AITrainDirectionForward, thisTrain.signalRef);
            
            // if reschedule, use actual speed

            float triggerDistanceM = TriggerDistance;

            if (reschedule)
            {
                float firstPartTime = 0.0f;
                float firstPartRangeM = 0.0f;
                float secndPartRangeM = 0.0f;
                float remainingRangeM = activateDistanceTravelledM - thisTrain.PresentPosition[0].DistanceTravelledM;

                firstPartTime = presentSpeedMpS / (0.25f * thisTrain.MaxDecelMpSS);
                firstPartRangeM = 0.25f * thisTrain.MaxDecelMpSS * (firstPartTime * firstPartTime);

                if (firstPartRangeM < remainingRangeM && thisTrain.SpeedMpS < thisTrain.TrainMaxSpeedMpS) // if distance left and not at max speed
                // split remaining distance based on relation between acceleration and deceleration
                {
                    secndPartRangeM = (remainingRangeM - firstPartRangeM) * (2.0f * thisTrain.MaxDecelMpSS) / (thisTrain.MaxDecelMpSS + thisTrain.MaxAccelMpSS);
                }

                triggerDistanceM = activateDistanceTravelledM - (firstPartRangeM + secndPartRangeM);
            }
            else

            // use maximum speed
            {
                float deltaTime = thisTrain.TrainMaxSpeedMpS / thisTrain.MaxDecelMpSS;
                float brakingDistanceM = (thisTrain.TrainMaxSpeedMpS * deltaTime) + (0.5f * thisTrain.MaxDecelMpSS * deltaTime * deltaTime);
                triggerDistanceM = activateDistanceTravelledM - brakingDistanceM;
            }

            float[] distancesM = new float[2];
            distancesM[1] = triggerDistanceM;
            distancesM[0] = activateDistanceTravelledM;

            return (distancesM);
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
        public AIAuxActions ActionRef;

        //================================================================================================//
        /// <summary>
        /// AuxActionItem
        /// The basic constructor
        /// </summary>

        public AuxActionItem(float distance, float requiredSpeedMpS, float activateDistance, float insertedDistance,
            AIAuxActions thisItem, AI_ACTION_TYPE thisAction) :
            base (distance, requiredSpeedMpS, activateDistance, insertedDistance, null, thisAction)
        {
            NextAction = AI_ACTION_TYPE.AUX_ACTION;
            ActionRef = thisItem;
        }

        public virtual bool CheckActionValide(AITrain thisTrain, float SpeedMpS, bool reschedule)
        {
            return false;
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
        public int ArrivalTime;
        public int DepartTime;
        public int ActualArrival;
        public int ActualDepart;
        int testTime = 0;

        //================================================================================================//
        /// <summary>
        /// AuxActionWPItem
        /// The specific constructor for WP action
        /// </summary>

        public AuxActionWPItem(float distance, float requiredSpeedMpS, float activateDistance, float insertedDistance,
            AIAuxActions thisItem, AI_ACTION_TYPE thisAction) :
            base(distance, requiredSpeedMpS, activateDistance, insertedDistance, thisItem, thisAction)
        {
            ActualDepart = 0;
            ActualArrival = 0;
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

        public override bool CheckActionValide(AITrain thisTrain, float SpeedMpS, bool reschedule)
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

        public override bool ValidAction(AITrain thisTrain)
        {
            bool actionValid = CheckActionValide(thisTrain, thisTrain.SpeedMpS, true);
            
            if (!actionValid)
            {
                thisTrain.requiredActions.InsertAction(this);
            }
            thisTrain.EndProcessAction(actionValid, this, false);
            return actionValid;
        }

        public override AITrain.AI_MOVEMENT_STATE InitAction(AITrain thisTrain, int presentTime, AITrain.AI_MOVEMENT_STATE movementState)
        {
            int correctedTime = presentTime;
            ActualArrival = correctedTime;
            ActualDepart = correctedTime + Delay;
            //TrainCar locomotive = thisTrain.FindLeadLocomotive();
            //((MSTSLocomotive)locomotive).SignalEvent(Event.Couple);
            return AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
        }

        public override AITrain.AI_MOVEMENT_STATE HandleAction(AITrain thisTrain, int presentTime, AITrain.AI_MOVEMENT_STATE movementState)
        {
            if (ActualDepart > presentTime)
            {
                movementState = AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION;
            }
            else
            {
                movementState = AITrain.AI_MOVEMENT_STATE.END_ACTION;
            }
            return movementState;
        }

        public override AITrain.AI_MOVEMENT_STATE EndAction(AITrain thisTrain, int presentTime, AITrain.AI_MOVEMENT_STATE movementState)
        {
            thisTrain.AuxActions.RemoveAt(0);
            thisTrain.ResetActions(true);
            //TrainCar locomotive = thisTrain.FindLeadLocomotive();
            //((MSTSLocomotive)locomotive).Horn = false;
            return AITrain.AI_MOVEMENT_STATE.STOPPED;
        }

        public override AITrain.AI_MOVEMENT_STATE ProcessAction(AITrain thisTrain, int presentTime, float elapsedClockSeconds, AITrain.AI_MOVEMENT_STATE movementState)
        {
            int correctedTime = presentTime;
            switch (movementState)
            {
                case AITrain.AI_MOVEMENT_STATE.INIT_ACTION:
                    movementState = InitAction(thisTrain, presentTime, movementState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.END_ACTION:
                    movementState = EndAction(thisTrain, presentTime, movementState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.HANDLE_ACTION:
                    movementState = HandleAction(thisTrain, presentTime, movementState);
                    break;
                case AITrain.AI_MOVEMENT_STATE.BRAKING:
                    float distanceToGoM = AITrain.clearingDistanceM;
                    distanceToGoM = ActivateDistanceM - thisTrain.PresentPosition[0].DistanceTravelledM;
                    float NextStopDistanceM = distanceToGoM;
                    if (distanceToGoM < 0f)
                    {
                        thisTrain.AdjustControlsBrakeMore(thisTrain.MaxDecelMpSS, elapsedClockSeconds, 100);
                        thisTrain.AITrainThrottlePercent = 0;

                        if (thisTrain.SpeedMpS < 0.001)
                        {
                            thisTrain.SpeedMpS = 0f;
                            movementState = AITrain.AI_MOVEMENT_STATE.INIT_ACTION;
                         }
                    }

                    break;
                case AITrain.AI_MOVEMENT_STATE.STOPPED:
                    movementState = thisTrain.UpdateStoppedState();
                    break;
                default:
                    break; 
            }
            return movementState;
        }

    }
#endregion
}

#endif