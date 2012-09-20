/* TRAINS
 * 
 * Contains code to represent a train as a list of TrainCars and to handle the physics of moving
 * the train through the Track Database.
 * 
 * A train has:
 *  - a list of TrainCars 
 *  - a front and back position in the TDB ( represented by TDBTravellers )
 *  - speed
 *  - MU signals that are relayed from player locomtive to other locomotives and cars such as:
 *      - direction
 *      - throttle percent
 *      - brake percent  ( TODO, this should be changed to brake pipe pressure )
 *      
 *  Individual TrainCars provide information on friction and motive force they are generating.
 *  This is consolidated by the train class into overall movement for the train.
 */

/// COPYRIGHT 2010 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.

 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MSTS;
using ORTS.Popups;
using ORTS.MultiPlayer;

namespace ORTS
{
	public class Train
	{
		public List<TrainCar> Cars = new List<TrainCar>();  // listed front to back
		public int Number;
		public float LastReportedSpeed = 10.0f; //Multiplayer, used to check if the train has stopped, others should know that
		public static int TotalNumber = 0;
		public TrainCar FirstCar { get { return Cars[0]; } }
		public TrainCar LastCar { get { return Cars[Cars.Count - 1]; } }
		public Traveller RearTDBTraveller;   // positioned at the back of the last car in the train
		public Traveller FrontTDBTraveller; // positioned at the front of the train by CalculatePositionOfCars
		public float Length; // length of train from FrontTDBTraveller to RearTDBTraveller
		public float SpeedMpS = 0.0f;  // meters per second +ve forward, -ve when backing
		public float lastSpeedMps = 0.0f;
		public Train UncoupledFrom = null;  // train not to coupled back onto
		public float TotalCouplerSlackM = 0;
		public float MaximumCouplerForceN = 0;
		public int NPull = 0;
		public int NPush = 0;
		private int LeadLocomotiveIndex = -1;
	public bool IsFreight = false;
		public float SlipperySpotDistanceM = 0; // distance to extra slippery part of track
		public float SlipperySpotLengthM = 0;

		// These signals pass through to all cars and locomotives on the train
		public Direction MUDirection = Direction.N; //set by player locomotive to control MU'd locomotives
		public float MUThrottlePercent = 0;  // set by player locomotive to control MU'd locomotives
		public float MUReverserPercent = 100;  // steam engine direction/cutoff control for MU'd locomotives
		public float MUDynamicBrakePercent = -1;  // dynamic brake control for MU'd locomotives, <0 for off
		public float BrakeLine1PressurePSI = 90;     // set by player locomotive to control entire train brakes
		public float BrakeLine2PressurePSI = 0;     // extra line for dual line systems, main reservoir
		public float BrakeLine3PressurePSI = 0;     // extra line just in case, engine brake pressure
		public float BrakeLine4PressurePSI = 0;     // extra line just in case, ep brake control line
		public RetainerSetting RetainerSetting = RetainerSetting.Exhaust;
		public int RetainerPercent = 100;

		public AIPath Path = null;
		public TrackAuthority TrackAuthority = null;  // track authority issued by Dispatcher

		public enum TRAINTYPE
		{
			PLAYER,
			STATIC,
			AI,
			REMOTE
		}

		public TRAINTYPE TrainType = TRAINTYPE.AI;

        public Signal nextSignal = new Signal(null, null, -1); // made public for signalling processing
		public float distanceToSignal = 0.1f;
        public List<ObjectItemInfo> SignalObjectItems;
        public int IndexNextSignal = -1;  // Index in SignalObjectItems for next signal
        public int IndexNextSpeedlimit = -1;  // Index in SignalObjectItems for next speedpost
        public SignalObject NextSignalObject; // direct reference to next signal

		public TrackMonitorSignalAspect TMaspect = TrackMonitorSignalAspect.None;
		public bool spad = false;      // Signal Passed At Danger
		public SignalHead.SIGASP CABAspect = SignalHead.SIGASP.UNKNOWN; // By GeorgeS

        public float RouteMaxSpeedMpS = 0;    // Max speed as set by route (default value)
        public float AllowedMaxSpeedMpS = 0;  // Max speed as allowed
        private float allowedMaxSpeedSignalMpS = 0;  // Max speed as set by signal
        private float allowedMaxSpeedLimitMpS = 0;   // Max speed as set by limit
        private float maxTimeS = 120;         // check ahead for distance covered in 2 mins.
        private float minCheckDistanceM = 5000;  // minimum distance to check ahead

        //To investigate coupler breaks on route
        public int NumOfCouplerBreaks = 0;
        private bool numOfCouplerBreaksNoted = false;

		public TrackLayer EditTrain = null; //WaltN: Temporary facility for track-laying experiments

		/// <summary>
		/// Reference to the Simulator object.
		/// </summary>
		protected Simulator Simulator;

		public bool updateMSGReceived = false; //sometime the train is controled remotely, so need to know when to update location
		public float travelled;//distance travelled, but not exactly

		// For AI control of the train
		public float AITrainBrakePercent
		{
			get { return aiBrakePercent; }
			set { aiBrakePercent = value; foreach (TrainCar car in Cars) car.BrakeSystem.AISetPercent(aiBrakePercent); }
		}
		private float aiBrakePercent = 0;
		public float AITrainThrottlePercent
		{
			get { return MUThrottlePercent; }
			set { MUThrottlePercent = value; }
		}
		public bool AITrainDirectionForward
		{
			get { return MUDirection == Direction.Forward; }
            set { MUDirection = value ? Direction.Forward : Direction.Reverse; MUReverserPercent = value ? 100 : -100; }
		}
		public TrainCar LeadLocomotive
		{
			get { return LeadLocomotiveIndex >= 0 ? Cars[LeadLocomotiveIndex] : null; }
			set
			{
				LeadLocomotiveIndex = -1;
				for (int i = 0; i < Cars.Count; i++)
					if (value == Cars[i] && value.IsDriveable)
					{
						LeadLocomotiveIndex = i;
						//MSTSLocomotive lead = (MSTSLocomotive)Cars[LeadLocomotiveIndex];
						//if (lead.EngineBrakeController != null)
						//    lead.EngineBrakeController.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, 1000);
					}
				if (LeadLocomotiveIndex < 0)
					foreach (TrainCar car in Cars)
						car.BrakeSystem.BrakeLine1PressurePSI = -1;
			}
		}

        public bool Reverse
        {
            get
            {
                return MUDirection == Direction.Reverse;
            }
        }

        public Traveller dFrontTDBTraveller
        {
            get
            {
                if (Reverse)
                {
                    Traveller tr = new Traveller(RearTDBTraveller);
                    tr.ReverseDirection();
                    return tr;
                }
                else
                {
                    return FrontTDBTraveller;
                }
            }
        }

        public Traveller dRearTDBTraveller
        {
            get
            {
                if (Reverse)
                {
                    Traveller tr = new Traveller(FrontTDBTraveller);
                    tr.ReverseDirection();
                    return tr;
                }
                else
                {
                    return RearTDBTraveller;
                }
            }
        }

        public Train(Simulator simulator)
        {
            Simulator = simulator;
			Number = TotalNumber;
			TotalNumber++;
            SignalObjectItems = new List<ObjectItemInfo>();
        }

		// restore game state
		public Train(Simulator simulator, BinaryReader inf)
		{
            Simulator = simulator;
			RestoreCars(simulator, inf);
			CheckFreight();
			SpeedMpS = inf.ReadSingle();
			TrainType = (TRAINTYPE)inf.ReadInt32();
			MUDirection = (Direction)inf.ReadInt32();
			MUThrottlePercent = inf.ReadSingle();
			MUDynamicBrakePercent = inf.ReadSingle();
			BrakeLine1PressurePSI = inf.ReadSingle();
			BrakeLine2PressurePSI = inf.ReadSingle();
			BrakeLine3PressurePSI = inf.ReadSingle();
			BrakeLine4PressurePSI = inf.ReadSingle();
			aiBrakePercent = inf.ReadSingle();
			LeadLocomotiveIndex = inf.ReadInt32();
			RetainerSetting = (RetainerSetting)inf.ReadInt32();
			RetainerPercent = inf.ReadInt32();
			RearTDBTraveller = new Traveller(simulator.TSectionDat, simulator.TDB.TrackDB.TrackNodes, inf);
			SlipperySpotDistanceM = inf.ReadSingle();
			SlipperySpotLengthM = inf.ReadSingle();
			CalculatePositionOfCars(0);
            RouteMaxSpeedMpS         = inf.ReadSingle();
            AllowedMaxSpeedMpS       = inf.ReadSingle();
            allowedMaxSpeedSignalMpS = inf.ReadSingle();
            allowedMaxSpeedLimitMpS  = inf.ReadSingle();

            SignalObjectItems = new List<ObjectItemInfo>();
            InitializeSignals(true);
		}

		// save game state
		public virtual void Save(BinaryWriter outf)
		{
			SaveCars(outf);
			outf.Write(SpeedMpS);
			outf.Write((int)TrainType);
			outf.Write((int)MUDirection);
			outf.Write(MUThrottlePercent);
			outf.Write(MUDynamicBrakePercent);
			outf.Write(BrakeLine1PressurePSI);
			outf.Write(BrakeLine2PressurePSI);
			outf.Write(BrakeLine3PressurePSI);
			outf.Write(BrakeLine4PressurePSI);
			outf.Write(aiBrakePercent);
			outf.Write(LeadLocomotiveIndex);
			outf.Write((int)RetainerSetting);
			outf.Write(RetainerPercent);
			RearTDBTraveller.Save(outf);
			outf.Write(SlipperySpotDistanceM);
			outf.Write(SlipperySpotLengthM);
            outf.Write(RouteMaxSpeedMpS);
            outf.Write(AllowedMaxSpeedMpS);
            outf.Write(allowedMaxSpeedSignalMpS);
            outf.Write(allowedMaxSpeedLimitMpS);
		}

		private void SaveCars(BinaryWriter outf)
		{
			outf.Write(Cars.Count);
			foreach (TrainCar car in Cars)
				RollingStock.Save(outf, car);
		}

		private void RestoreCars(Simulator simulator, BinaryReader inf)
		{
			int count = inf.ReadInt32();
			for (int i = 0; i < count; ++i)
				Cars.Add(RollingStock.Restore(simulator, inf, this, i == 0 ? null : Cars[i - 1]));
		}

        public void InitializeSignals(bool existingSpeedLimits)
        {
            Debug.Assert(Simulator.Signals != null, "Cannot InitializeSignals() without Simulator.Signals.");

	        IndexNextSignal = -1;
	        IndexNextSpeedlimit = -1;
            List<int> tmp = new List<int>();

  //  set overall speed limits if these do not yet exist

            if (!existingSpeedLimits)
            {
                    AllowedMaxSpeedMpS       = RouteMaxSpeedMpS;   // set default
                    allowedMaxSpeedSignalMpS = RouteMaxSpeedMpS;   // set default
                    allowedMaxSpeedLimitMpS  = RouteMaxSpeedMpS;   // set default
            }

  //  get first item from train (irrespective of distance)

            ObjectItemInfo.ObjectItemFindState returnState = 0;
            float distanceToLastObject = 9E29f;  // set to overlarge value

            ObjectItemInfo firstObject = Simulator.Signals.getNextObject(dFrontTDBTraveller, ObjectItemInfo.ObjectItemType.ANY,
                    true, -1, ref returnState);

            if (returnState > 0)
            {
                if (!SignalObjectItems.Exists(oi => oi.ObjectDetails.thisRef == firstObject.ObjectDetails.thisRef))
                    SignalObjectItems.Add(firstObject);
                tmp.Add(firstObject.ObjectDetails.thisRef);
                distanceToLastObject = firstObject.distance_to_train;
            }

  // get next items within max distance

            float maxDistance = Math.Max(AllowedMaxSpeedMpS * maxTimeS, minCheckDistanceM);    // look maxTimeS or minCheckDistance ahead

            ObjectItemInfo nextObject;
            ObjectItemInfo prevObject = firstObject;

            while (returnState > 0 && distanceToLastObject < maxDistance)
            {
                nextObject = Simulator.Signals.getNextObject(prevObject.ObjectDetails, ObjectItemInfo.ObjectItemType.ANY,
                null, -1, ref returnState);

                if (returnState > 0)
                {
                    nextObject.distance_to_train = prevObject.distance_to_train + nextObject.distance_to_object;
                    distanceToLastObject = nextObject.distance_to_train;
                    if (!SignalObjectItems.Exists(oi => oi.ObjectDetails.thisRef == nextObject.ObjectDetails.thisRef))
                        SignalObjectItems.Add(nextObject);
                    tmp.Add(nextObject.ObjectDetails.thisRef);
                    prevObject = nextObject;
                }
            }

            SignalObjectItems.RemoveAll(oi => !tmp.Contains(oi.ObjectDetails.thisRef));

 //
 // get first signal and first speedlimit
 // also initiate nextSignal variable
 //
  
            bool signalFound = false;
            bool speedlimFound = false;

            for (int isig = 0; isig < SignalObjectItems.Count && (!signalFound || !speedlimFound); isig++)
            {
                if (!signalFound)
                {
                    ObjectItemInfo thisObject = SignalObjectItems[isig];
                    if (thisObject.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                    {
                        Signals signals = thisObject.ObjectDetails.signalRef;
                        nextSignal = signals.InitSignalItem(thisObject.ObjectDetails.thisRef);
                        //nextSignal.UpdateTrackOcupancy(dRearTDBTraveller);
                        signalFound = true;
                        IndexNextSignal = isig;
                    }
                }

                if (!speedlimFound)
                {
                    ObjectItemInfo thisObject = SignalObjectItems[isig];
                    if (thisObject.ObjectType == ObjectItemInfo.ObjectItemType.SPEEDLIMIT)
                    {
                        IndexNextSpeedlimit = isig;
                    }
                }
            }

        //
 // If signal in list, set signal reference,
 // else try to get first signal
 //

            NextSignalObject = null;
            if (IndexNextSignal > 0)
            {
                    NextSignalObject = SignalObjectItems[IndexNextSignal].ObjectDetails;
                    distanceToSignal = SignalObjectItems[IndexNextSignal].distance_to_train;
            }
            else
            {
                    ObjectItemInfo firstSignalObject = 
                            Simulator.Signals.getNextObject(dFrontTDBTraveller, ObjectItemInfo.ObjectItemType.SIGNAL,
                            true, -1, ref returnState);

                    if (returnState > 0)
                    {
                            NextSignalObject = firstSignalObject.ObjectDetails;
                            distanceToSignal = firstSignalObject.distance_to_train;
                            Signals signals = NextSignalObject.signalRef;
                            nextSignal = signals.InitSignalItem(NextSignalObject.thisRef);
                            //nextSignal.UpdateTrackOcupancy(dRearTDBTraveller);
                    }
            }

 //
 // determine actual speed limits depending on overall speed and type of train
 //

            updateSpeedInfo();
        }

        //
        //  This method is invoked whenever the train direction has changed or 'G' key pressed 
        //
        public void ResetSignal(bool askPermisiion)
        {
#if DUMP_DISPATCHER
            Simulator.AI.Dispatcher.Dump();

            if (lastclocktime != Simulator.ClockTime)
            {
                dumps.Add(Simulator.ClockTime + 3, Program.Simulator.AI.Dispatcher.Dump);
                lastclocktime = Simulator.ClockTime;
            }
#endif
            nextSignal.Reset(dFrontTDBTraveller, askPermisiion);
            nextSignal.UpdateTrackOcupancy(dRearTDBTraveller);
            spad = false;

        // Clear and recreate full signal list

            int sigtotal = SignalObjectItems.Count;
            SignalObjectItems.RemoveRange(0,sigtotal);

            InitializeSignals(true);                
        }

		//
		//  This method is invoked whenever the train direction has changed or 'G' key pressed 
		//
		public void ResetSignal()
		{
			ObjectItemInfo ob;
			bool force = SignalObjectItems != null &&
				(ob = (SignalObjectItems.Where(o => o.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL).FirstOrDefault())) != null &&
				ob.ObjectDetails.hasPermission == Signal.PERMISSION.GRANTED;

			nextSignal.Reset(dFrontTDBTraveller, false);
			nextSignal.UpdateTrackOcupancy(dRearTDBTraveller);
			spad = false;

			// Clear and recreate full signal list

			int sigtotal = SignalObjectItems.Count;
			SignalObjectItems.RemoveRange(0, sigtotal);

			Simulator.AI.Dispatcher.ExtendTrainAuthorization(this, force);
		}
		
#if DUMP_DISPATCHER
        private Heap<Action> dumps = new Heap<Action>();
        private double lastclocktime = -1;
        private void CheckDump()
        {
            if (dumps.GetSize() > 0)
            {
                if (dumps.GetMinKey() < Program.Simulator.ClockTime)
                {
                    Action a = dumps.DeleteMin();
                    a();
                }
            }
        }

        public void DumpSignals(StringBuilder sta)
        {
            sta.AppendFormat("|Speed|{0:000.0}\r\n\r\n", this.SpeedMpS);
            sta.AppendLine("|Signals");
            if (nextSignal != null)
            {
                int sigid = nextSignal.nextSigRef;
                
                sta.AppendFormat("|NextSigRef|{0}\r\n", sigid);
                if (sigid > -1)
                {
                    SignalObject s = Signal.signalObjects[sigid];
                    s.Dump(sta, dFrontTDBTraveller);
                    sigid = s.nextSignal;
                    sta.AppendLine();
                    sta.AppendFormat("|Next-NextSigRef|{0}\r\n", sigid);
                    if (sigid > -1)
                    {
                        s = Signal.signalObjects[sigid];
                        s.Dump(sta, dFrontTDBTraveller);
                    }
                }
                sta.AppendLine();
                
                sigid = nextSignal.prevSigRef; 
                sta.AppendFormat("|PrevSigRef|{0}\r\n", sigid);
                if (sigid > -1)
                {
                    SignalObject s = Signal.signalObjects[sigid];
                    s.Dump(sta, null);
                }
                sta.AppendLine();

                sigid = nextSignal.rearSigRef;
                sta.AppendFormat("|RearSigRef|{0}\r\n", sigid);
                if (sigid > -1)
                {
                    SignalObject s = Signal.signalObjects[sigid];
                    s.Dump(sta, null);
                }
                sta.AppendLine();
            }
        }
#endif

        // Sets the Lead locomotive to the next in the consist
        public void LeadNextLocomotive()
        {
            // First driveable
            int firstLead = -1;
            // Next driveale to the current
            int nextLead = -1;
            // Count of driveable locos
            int coud = 0;

            for (int i = 0; i < Cars.Count; i++)
            {
                if (Cars[i].IsDriveable)
                {
					//in multiplayer, only wants to change locomotive starts with my name (i.e. original settings of my locomotives)
					if (MPManager.IsMultiPlayer() && !Cars[i].CarID.StartsWith(MPManager.GetUserName() + " ")) continue;
                    // Count the driveables
                    coud++;

                    // Get the first driveable
                    if (firstLead == -1)
                        firstLead = i;

                    // If later than current select the next
                    if (LeadLocomotiveIndex < i && nextLead == -1)
                    {
                        nextLead = i;
                    }
                }
            }

            TrainCar prevLead = LeadLocomotive;

            // If found one after the current
            if (nextLead != -1)
                LeadLocomotiveIndex = nextLead;
            // If not, and have more than one, set the first
            else if (coud > 1)
                LeadLocomotiveIndex = firstLead;
            Orient();
            TrainCar newLead = LeadLocomotive;
            if (prevLead != null && newLead != null && prevLead != newLead)
                newLead.CopyControllerSettings(prevLead);
            if (Program.Simulator.PlayerLocomotive != null && Program.Simulator.PlayerLocomotive.Train == this)
            {
                Program.Simulator.PlayerLocomotive = newLead;
                Program.Simulator.AI.Dispatcher.ReversePlayerAuthorization();
            }
        }

        /// <summary>
        /// Flips the train if necessary so that the train orientation matches the lead locomotive cab direction
        /// </summary>
        public void Orient()
        {
            TrainCar lead = LeadLocomotive;
            if (lead == null || !(lead.Flipped ^ lead.GetCabFlipped()))
                return;
            for (int i = Cars.Count - 1; i > 0; i--)
                Cars[i].CopyCoupler(Cars[i - 1]);
            for (int i = 0; i < Cars.Count / 2; i++)
            {
                int j = Cars.Count - i - 1;
                TrainCar car = Cars[i];
                Cars[i] = Cars[j];
                Cars[j] = car;
            }
            if (LeadLocomotiveIndex >= 0)
                LeadLocomotiveIndex = Cars.Count - LeadLocomotiveIndex - 1;
            for (int i = 0; i < Cars.Count; i++)
                Cars[i].Flipped = !Cars[i].Flipped;
            Traveller t = FrontTDBTraveller;
            FrontTDBTraveller = new Traveller(RearTDBTraveller, Traveller.TravellerDirection.Backward);
            RearTDBTraveller = new Traveller(t, Traveller.TravellerDirection.Backward);
            MUDirection = DirectionControl.Flip(MUDirection);
            MUReverserPercent = -MUReverserPercent;
            InitializeSignals(true);  // Initialize signals with existing speed information
        }

        /// <summary>
        /// Someone is sending an event notification to all cars on this train.
        /// ie doors open, pantograph up, lights on etc.
        /// </summary>
        public void SignalEvent(EventID eventID)
		{
			foreach (TrainCar car in Cars)
				car.SignalEvent(eventID);
		}


		public virtual void Update(float elapsedClockSeconds)
		{
#if DUMP_DISPATCHER
            CheckDump();
#endif
			if (TrainType == TRAINTYPE.REMOTE) {
				//if a MSGMove is received
				if (updateMSGReceived)
				{
					float move = 0.0f;
					var requestedSpeed = SpeedMpS;
					try
					{
						var x = travelled + SpeedMpS * elapsedClockSeconds + (SpeedMpS - lastSpeedMps) / 2 * elapsedClockSeconds;
						this.MUDirection = (Direction)expectedDIr;

						if (Math.Abs(x - expectedTravelled) < 0.2 || Math.Abs(x - expectedTravelled) > 10)
						{
							CalculatePositionOfCars(expectedTravelled - travelled);
							//if something wrong with the switch
							if (this.RearTDBTraveller.TrackNodeIndex != expectedTracIndex)
							{
								Traveller t = new Traveller(Simulator.TSectionDat, Simulator.TDB.TrackDB.TrackNodes, Simulator.TDB.TrackDB.TrackNodes[expectedTracIndex], expectedTileX, expectedTileZ, expectedX, expectedZ, (Traveller.TravellerDirection)expectedTDir);
								
								//move = SpeedMpS > 0 ? 0.001f : -0.001f;
								this.travelled = expectedTravelled;
								this.RearTDBTraveller = t;
								CalculatePositionOfCars(0);

							}
							//}
						}
						else//if the predicted location and reported location are similar, will try to increase/decrease the speed to bridge the gap in 1 second
						{
							SpeedMpS += (expectedTravelled - x) / 1;
							CalculatePositionOfCars(SpeedMpS * elapsedClockSeconds);
						}
					}
					catch (Exception)
					{
						move = expectedTravelled - travelled;
					}
					/*if (Math.Abs(requestedSpeed) < 0.00001 && Math.Abs(SpeedMpS) > 0.01) updateMSGReceived = true; //if requested is stop, but the current speed is still moving
					else*/ updateMSGReceived = false;

				}
				else//no message received, will move at the previous speed
				{
					CalculatePositionOfCars(SpeedMpS * elapsedClockSeconds);
				}

				//update speed for each car, so wheels will rotate
				foreach (TrainCar car in Cars)
				{
					if (car != null)
					{
						if (car.IsDriveable && car is MSTSWagon) (car as MSTSWagon).WheelSpeedMpS = SpeedMpS;
						car.SpeedMpS = SpeedMpS;
						if (car.Flipped) car.SpeedMpS = -car.SpeedMpS;

                        

#if INDIVIDUAL_CONTROL
						if (car is MSTSLocomotive && car.CarID.StartsWith(MPManager.GetUserName()))
						{
							car.Update(elapsedClockSeconds);
						}
#endif
					}
				}
				lastSpeedMps = SpeedMpS;
				//Orient();
				if (MPManager.IsServer())
				{
					Program.Simulator.AI.Dispatcher.RequestAuth(this, true, 0);
					UpdateSignalState();
				}
				return;
			}
		
			PropagateBrakePressure(elapsedClockSeconds);

            TrainCar uncoupleBehindCar = null;
			foreach (TrainCar car in Cars)
			{
				car.MotiveForceN = 0;
#if INDIVIDUAL_CONTROL
				var canUpdate = true;
				if (MPManager.IsMultiPlayer())
				{
					foreach (var p in MPManager.OnlineTrains.Players)
					{
						//if this car is a locomotive of another guy
						if (car is MSTSLocomotive && car.CarID.StartsWith(p.Key)) canUpdate = false;
					}
				}
				
				if (canUpdate) 
#endif
				car.Update(elapsedClockSeconds);
				car.TotalForceN = car.MotiveForceN + car.GravityForceN;
				if (car.Flipped)
				{
					car.TotalForceN = -car.TotalForceN;
					car.SpeedMpS = -car.SpeedMpS;
				}
                if (car.CouplerOverloaded)
                    uncoupleBehindCar = car;
			}

			AddCouplerImpuseForces();
			ComputeCouplerForces();
			UpdateCarSpeeds(elapsedClockSeconds);
			UpdateCouplerSlack(elapsedClockSeconds);

			float distanceM = LastCar.SpeedMpS * elapsedClockSeconds;

			SpeedMpS = 0;
			foreach (TrainCar car1 in Cars)
			{
				SpeedMpS += car1.SpeedMpS;
				if (car1.Flipped)
					car1.SpeedMpS = -car1.SpeedMpS;
			}
			SpeedMpS /= Cars.Count;

			SlipperySpotDistanceM -= SpeedMpS * elapsedClockSeconds;

			CalculatePositionOfCars(distanceM);

			//End-of-route detection
			if (IsEndOfRoute(MUDirection))// FrontTDBTraveller.Direction))
			{
                if (EditTrain == null) //WaltN: !RE_ENABLED This is the normal path
                {
                    Stop();

                    // TODO - Collision detection: If a train hits an object, there should be a
                    //        realistic response.  This includes a train impacting a bumper/buffer.
                    //        It's possible that collision detection will occur BEFORE end-of-
                    //        route detection and will obsolete this test in this location.
                    //        However, the case of an unterminated section should be kept in mind.
                }
                else //WaltN: RE_ENABLED This is the track-laying path
                {
                    Stop();
                    // Using FrontTDBTraveller if moving forward or RearTDBTraveller if moving backwards
                    Traveller t = (MUDirection == Direction.Forward) ? FrontTDBTraveller : RearTDBTraveller;
                    // Initiate a route edit
                    EditTrain.LaySection(t);
                }
			}

			if (!spad) UpdateSignalState();

            // Coupler breaker
            if (uncoupleBehindCar != null)
            {
                if (uncoupleBehindCar.CouplerOverloaded)
                {
                    if (!numOfCouplerBreaksNoted)
                    {
                        NumOfCouplerBreaks++;
                        Trace.WriteLine(String.Format("Num of coupler breaks: {0}", NumOfCouplerBreaks));
                        numOfCouplerBreaksNoted = true;
                    }
                }
                else
                    numOfCouplerBreaksNoted = false;
                if (Simulator.BreakCouplers)
                {
                    Simulator.UncoupleBehind(uncoupleBehindCar);
                    uncoupleBehindCar.CouplerOverloaded = false;
                    Simulator.Confirmer.Warning("Coupler broken!");
                }
                else
                    Simulator.Confirmer.Warning("Coupler overloaded!");
                uncoupleBehindCar = null; 
            }
            else
                numOfCouplerBreaksNoted = false;


		} // end Update


		//
		//  Update the distance to and aspect of next signal
		//
        private Direction _prevDirection = Direction.N;
        private void UpdateSignalState()
		{
            ObjectItemInfo.ObjectItemFindState returnState = ObjectItemInfo.ObjectItemFindState.OBJECT_FOUND;
            bool listChanged = false;
            int lastSignalObjectRef = -1;
            bool signalFound = false;
            bool speedlimFound = false;

            ObjectItemInfo firstObject = null;

            if (this.MUDirection != _prevDirection)
            {
                _prevDirection = MUDirection;
                ResetSignal(false);
            }

        //
        // get distance to first object
        //

            if (SignalObjectItems.Count > 0)
			{
                    firstObject = SignalObjectItems[0];
                    firstObject.distance_to_train = firstObject.ObjectDetails.DistanceTo(dFrontTDBTraveller);

  //
        // remember last signal index
        //

                    if (NextSignalObject != null)
                    {
                            lastSignalObjectRef = NextSignalObject.thisRef;
        			}
		        	nextSignal.UpdateTrackOcupancy(dRearTDBTraveller);
        //
        // check if passed object - if so, remove object
        // if object is speed, set max allowed speed
        //

                    while (firstObject.distance_to_train < 0.0f && SignalObjectItems.Count > 0)
                    {
                            if (firstObject.actual_speed > 0)
                            {
                                    AllowedMaxSpeedMpS = firstObject.actual_speed;
                                    if (firstObject.ObjectDetails.isSignal)
                                    {
                                            allowedMaxSpeedSignalMpS = AllowedMaxSpeedMpS;
                                    }
                                    else
                                    {
                                            allowedMaxSpeedLimitMpS = AllowedMaxSpeedMpS;
                                    }
                            }

                            SignalObjectItems.RemoveAt(0);

                            if (SignalObjectItems.Count > 0)
                            {
                                    firstObject = SignalObjectItems[0];
                                    firstObject.distance_to_train = firstObject.ObjectDetails.DistanceTo(dFrontTDBTraveller);
                            }

                            listChanged = true;
                    }

        //
        // if no objects left on list, find first object whatever the distance
        //

                    if (SignalObjectItems.Count <= 0)
                    {
                         firstObject = Simulator.Signals.getNextObject(dFrontTDBTraveller, ObjectItemInfo.ObjectItemType.ANY,
                                            true, -1, ref returnState);
                         if (returnState > 0)
                         {
                                 SignalObjectItems.Add(firstObject);
                         }
                    }
            }

        //
        // process further if any object available
        //

            if (SignalObjectItems.Count > 0)
            {

        //
        // Update state and speed of first object if signal
        //

                    if (firstObject.ObjectDetails.isSignal)
                    {
                            firstObject.signal_state = firstObject.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
                            ObjectSpeedInfo thisSpeed = firstObject.ObjectDetails.this_lim_speed(SignalHead.SIGFN.NORMAL);
                            firstObject.speed_passenger = thisSpeed.speed_pass;
                            firstObject.speed_freight = thisSpeed.speed_freight;
                            firstObject.speed_flag = thisSpeed.speed_flag;
                    }
 
        //
        // Update all objects in list (except first)
        //

                    float lastDistance = firstObject.distance_to_train;

                    ObjectItemInfo prevObject = firstObject;

                    for (int isig = 1; isig < SignalObjectItems.Count && !signalFound; isig++)
                    {
                            ObjectItemInfo nextObject = SignalObjectItems[isig];
                            nextObject.distance_to_train = prevObject.distance_to_train + nextObject.distance_to_object;
                            lastDistance = nextObject.distance_to_train;

                            if (nextObject.ObjectDetails.isSignal)
                            {
                                    nextObject.signal_state = nextObject.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
                                    ObjectSpeedInfo thisSpeed = nextObject.ObjectDetails.this_lim_speed(SignalHead.SIGFN.NORMAL);
                                    nextObject.speed_passenger = thisSpeed.speed_pass;
                                    nextObject.speed_freight = thisSpeed.speed_freight;
                                    nextObject.speed_flag = thisSpeed.speed_flag;
                            }

                            prevObject = nextObject;
                    }

        //
        // check if next signal aspect is STOP. 
        // If so, no check on list is required
        //

                    SignalHead.SIGASP nextAspect = SignalHead.SIGASP.UNKNOWN;

                    for (int isig = 0; isig < SignalObjectItems.Count && !signalFound; isig++)
                    {
                            ObjectItemInfo nextObject = SignalObjectItems[isig];
                            if (nextObject.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                            {
                                    signalFound = true;
                                    nextAspect = nextObject.signal_state;
                            }
                    }

        //
        // read next items if last item within max distance
        //
            
                    float maxDistance = Math.Max(AllowedMaxSpeedMpS * maxTimeS, minCheckDistanceM);

                    while (lastDistance < maxDistance && returnState > 0 && nextAspect != SignalHead.SIGASP.STOP)
                    {
                            ObjectItemInfo nextObject = Simulator.Signals.getNextObject(prevObject.ObjectDetails, ObjectItemInfo.ObjectItemType.ANY,
                                null, -1, ref returnState);

                            if (returnState > 0)
                            {
                                    nextObject.distance_to_train = prevObject.distance_to_train + nextObject.distance_to_object;
                                    lastDistance = nextObject.distance_to_train;
                                    SignalObjectItems.Add(nextObject);

                                    if (nextObject.ObjectDetails.isSignal)
                                    {
                                            nextObject.signal_state = nextObject.ObjectDetails.this_sig_lr(SignalHead.SIGFN.NORMAL);
                                            ObjectSpeedInfo thisSpeed = nextObject.ObjectDetails.this_lim_speed(SignalHead.SIGFN.NORMAL);
                                            nextObject.speed_passenger = thisSpeed.speed_pass;
                                            nextObject.speed_freight = thisSpeed.speed_freight;
                                            nextObject.speed_flag = thisSpeed.speed_flag;
                                    }

                                    prevObject = nextObject;

                                    listChanged = true;
                            }
                    }

        //
        // if list is changed, get new indices to first signal and speedpost
        //

                    if (listChanged)
                    {
                            signalFound = false;
                            speedlimFound = false;

                            IndexNextSignal = -1;
                            IndexNextSpeedlimit = -1;
                            NextSignalObject = null;

                            for (int isig = 0; isig < SignalObjectItems.Count && (!signalFound || !speedlimFound); isig++)
                            {
                                    ObjectItemInfo nextObject = SignalObjectItems[isig];
                                    if (!signalFound && nextObject.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                                    {
                                            signalFound = true;
                                            IndexNextSignal = isig;
                                    }
                                    else if (!speedlimFound && nextObject.ObjectType == ObjectItemInfo.ObjectItemType.SPEEDLIMIT)
                                    {
                                            speedlimFound = true;
                                            IndexNextSpeedlimit = isig;
                                    }
                            }

        //
        // check if any signal in list, if not get direct from train
        // get state and details
        //

                            if (IndexNextSignal < 0)
                            {
                                    ObjectItemInfo firstSignalObject = 
                                              Simulator.Signals.getNextObject(dFrontTDBTraveller, ObjectItemInfo.ObjectItemType.SIGNAL,
                                            true, -1, ref returnState);

                                    if (returnState > 0)
                                    {
                                            NextSignalObject = firstSignalObject.ObjectDetails;
                                    }
                            }
                            else
                            {
                                    NextSignalObject = SignalObjectItems[IndexNextSignal].ObjectDetails;
                            }
                    }

        //
        // update distance of signal if out of list
        // get state of next signal
        //

                    SignalHead.SIGASP thisState = SignalHead.SIGASP.UNKNOWN;

                    if (IndexNextSignal >= 0)
                    {
                            distanceToSignal = SignalObjectItems[IndexNextSignal].distance_to_train;
                            thisState = SignalObjectItems[IndexNextSignal].signal_state;
                    }
                    else if (NextSignalObject != null)
                    {
                            distanceToSignal = NextSignalObject.DistanceTo(dFrontTDBTraveller);
                            thisState = NextSignalObject.this_sig_lr(SignalHead.SIGFN.NORMAL);
                    }

                    CABAspect = thisState;
                    SignalObject dummyObject = new SignalObject();
                    TMaspect  = dummyObject.TranslateTMAspect(thisState);

        //
        // update track occupancy
        //

                    nextSignal.nextSigRef = NextSignalObject == null ? -1 : NextSignalObject.thisRef;

                    if (lastSignalObjectRef != nextSignal.nextSigRef)
                    {
                            nextSignal.rearSigRef = lastSignalObjectRef;
                            nextSignal.prevSigRef = lastSignalObjectRef;
                    }

			        nextSignal.UpdateTrackOcupancy(dRearTDBTraveller);
		    }

            else if (this is AITrain)
            {
                NextSignalObject = null;
                distanceToSignal = 50000;
            }

 //
 // determine actual speed limits depending on overall speed and type of train
 //

            updateSpeedInfo();

        }

 //
 // set actual speed limit for all objects depending on state and type of train
 //

        public void updateSpeedInfo()
        {
                float validSpeedMpS  = AllowedMaxSpeedMpS;
                float validSpeedSignalMpS = allowedMaxSpeedSignalMpS;
                float validSpeedLimitMpS  = allowedMaxSpeedLimitMpS;

                foreach (ObjectItemInfo thisObject in SignalObjectItems)
                {

 //
 // select speed on type of train - not yet implemented as type is not yet available
 //

                        float actualSpeedMpS = IsFreight ? thisObject.speed_freight : thisObject.speed_passenger;

                        if (thisObject.ObjectDetails.isSignal)
                        {
                                if (actualSpeedMpS > 0 && thisObject.speed_flag == 0)
                                {
                                        validSpeedSignalMpS = actualSpeedMpS;
                                        if (validSpeedSignalMpS > validSpeedLimitMpS)
                                        {
                                                actualSpeedMpS = -1;
                                        }
                                }
                                else
                                {
                                        validSpeedSignalMpS = RouteMaxSpeedMpS;
                                        float newSpeedMpS = Math.Min(validSpeedSignalMpS, validSpeedLimitMpS);

                                        if (newSpeedMpS != validSpeedMpS)
                                        {
                                                actualSpeedMpS = newSpeedMpS;
                                        }
                                        else
                                        {
                                                actualSpeedMpS = -1;
                                        }
                                }
                                thisObject.actual_speed = actualSpeedMpS;
                                if (actualSpeedMpS > 0)
                                {
                                        validSpeedMpS = actualSpeedMpS;
                                }
                        }
                        else
			            {
				                if (actualSpeedMpS > 998f)
				                {
					                actualSpeedMpS = RouteMaxSpeedMpS;
				                }

                                if (actualSpeedMpS > 0)
                                {
                                        validSpeedMpS = actualSpeedMpS;
                                        validSpeedLimitMpS = actualSpeedMpS;
                                }
                                thisObject.actual_speed = actualSpeedMpS;
                        }
                }
        }

 //
 // get aspect of next signal ahead
 //
 
		public SignalHead.SIGASP GetNextSignalAspect()
		{
            SignalHead.SIGASP thisAspect = SignalHead.SIGASP.STOP;
            if (NextSignalObject != null)
            {
                    thisAspect = NextSignalObject.this_sig_lr(SignalHead.SIGFN.NORMAL);
		    }

            return thisAspect;
        }
		/// <summary>
		/// Returns true if (forward == 1) and front of train on TrEndNode
		/// or if (forward == 0) and rear of train on TrEndNode.
		/// </summary>
		private bool IsEndOfRoute(Direction forward)
		{
			// This test detects that a TrEndNode has been encountered.  This can occur if a 
			// train tries to continue through a bumper (buffer).  It can also occur if it
			// progresses beyond an unterminated section (no bumper).

			// Using FrontTDBTraveller if moving forward or RearTDBTraveller if moving backwards
			Traveller t = (forward == Direction.Forward) ? FrontTDBTraveller : RearTDBTraveller;
			if (!t.IsEnd) return false;
			else return true; // Signal end-of-route
		} // end IsEndOfRoute

		/// <summary>
		/// Stops the train ASAP
		/// </summary>
		private void Stop()
		{
			// End of route: Stop ASAP
			SpeedMpS = 0; // Abrupt stop
			//SpeedMpS = -0.85f * SpeedMpS;  // Gives a bounce
			foreach (TrainCar stopping in Cars)
			{
				stopping.SpeedMpS = SpeedMpS;
			}
			AITrainThrottlePercent = 0;
			AITrainBrakePercent = 100;
			// The following does not seem to be essential.  It is commented out because
			// we don't want emergency brake set if we just touch a bumper.  ...WaltN
			//if (LeadLocomotive != null) ((MSTSLocomotive)LeadLocomotive).SetEmergency();
		} // end Stop

		public void InitializeBrakes()
		{
            if( Math.Abs(SpeedMpS) >= 0.01 ) {
                if( Simulator.Confirmer != null ) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Warning( CabControl.InitializeBrakes, CabSetting.Warn );
                return;
            }
			if (Program.Simulator.PlayerLocomotive != null && this == Program.Simulator.PlayerLocomotive.Train)
				if( Simulator.Confirmer != null ) // As Confirmer may not be created until after a restore.
                    Simulator.Confirmer.Confirm( CabControl.InitializeBrakes, CabSetting.Off );

			float maxPressurePSI = 90;
			if (LeadLocomotiveIndex >= 0)
			{
				MSTSLocomotive lead = (MSTSLocomotive)Cars[LeadLocomotiveIndex];
				if (lead.TrainBrakeController != null)
				{
					lead.TrainBrakeController.UpdatePressure(ref BrakeLine1PressurePSI, 1000, ref BrakeLine4PressurePSI);
					maxPressurePSI = lead.TrainBrakeController.GetMaxPressurePSI();
					BrakeLine1PressurePSI = MathHelper.Max(BrakeLine1PressurePSI, maxPressurePSI - lead.TrainBrakeController.GetFullServReductionPSI());
				}
				if (lead.EngineBrakeController != null)
					lead.EngineBrakeController.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, 1000);
			}
			else
			{
				BrakeLine1PressurePSI = BrakeLine3PressurePSI = BrakeLine4PressurePSI = 0;
			}
			BrakeLine2PressurePSI = maxPressurePSI;
			foreach (TrainCar car in Cars)
			{
				car.BrakeSystem.Initialize(LeadLocomotiveIndex < 0, maxPressurePSI);
				if (LeadLocomotiveIndex < 0)
					car.BrakeSystem.BrakeLine1PressurePSI = -1;
			}
		}
		public void SetHandbrakePercent(float percent)
		{
			if (SpeedMpS < -.1 || SpeedMpS > .1)
				return;
			foreach (TrainCar car in Cars)
				car.BrakeSystem.SetHandbrakePercent(percent);
		}
		public void ConnectBrakeHoses()
		{
			if (SpeedMpS < -.1 || SpeedMpS > .1)
				return;
			foreach (TrainCar car in Cars)
				car.BrakeSystem.Connect();
		}
		public void DisconnectBrakes()
		{
			if (SpeedMpS < -.1 || SpeedMpS > .1)
				return;
			int first = -1;
			int last = -1;
			FindLeadLocomotives(ref first, ref last);
			for (int i = 0; i < Cars.Count; i++)
			{
				if (first <= i && i <= last)
					continue;
				TrainCar car = Cars[i];
                car.BrakeSystem.Disconnect();
			}
		}
		public void SetRetainers(bool increase)
		{
			if (SpeedMpS < -.1 || SpeedMpS > .1)
				return;
			if (!increase)
			{
				RetainerSetting = RetainerSetting.Exhaust;
				RetainerPercent = 100;
			}
			else if (RetainerPercent < 100)
				RetainerPercent *= 2;
			else if (RetainerSetting != RetainerSetting.SlowDirect)
			{
				RetainerPercent = 25;
				switch (RetainerSetting)
				{
					case RetainerSetting.Exhaust: RetainerSetting = RetainerSetting.LowPressure; break;
					case RetainerSetting.LowPressure: RetainerSetting = RetainerSetting.HighPressure; break;
					case RetainerSetting.HighPressure: RetainerSetting = RetainerSetting.SlowDirect; break;
				}
			}
			int first = -1;
			int last = -1;
			FindLeadLocomotives(ref first, ref last);
			int step = 100 / RetainerPercent;
			for (int i = 0; i < Cars.Count; i++)
			{
				int j = Cars.Count - 1 - i;
				if (j <= last)
					break;
				Cars[j].BrakeSystem.SetRetainer(i % step == 0 ? RetainerSetting : RetainerSetting.Exhaust);
			}
		}
		public void FindLeadLocomotives(ref int first, ref int last)
		{
			first = last = -1;
			if (LeadLocomotiveIndex >= 0)
			{
				for (int i = LeadLocomotiveIndex; i < Cars.Count && Cars[i].IsDriveable; i++)
					last = i;
				for (int i = LeadLocomotiveIndex; i >= 0 && Cars[i].IsDriveable; i--)
					first = i;
			}
		}

		private void PropagateBrakePressure(float elapsedClockSeconds)
		{
			if (LeadLocomotiveIndex >= 0)
			{
				MSTSLocomotive lead = (MSTSLocomotive)Cars[LeadLocomotiveIndex];
				if (lead.TrainBrakeController != null)
					lead.TrainBrakeController.UpdatePressure(ref BrakeLine1PressurePSI, elapsedClockSeconds, ref BrakeLine4PressurePSI);
				if (lead.EngineBrakeController != null)
					lead.EngineBrakeController.UpdateEngineBrakePressure(ref BrakeLine3PressurePSI, elapsedClockSeconds);
                lead.BrakeSystem.PropagateBrakePressure(elapsedClockSeconds);
			}
			else
			{
				foreach (TrainCar car in Cars)
				{
					if (car.BrakeSystem.BrakeLine1PressurePSI < 0)
						continue;
					car.BrakeSystem.BrakeLine1PressurePSI = BrakeLine1PressurePSI;
					car.BrakeSystem.BrakeLine2PressurePSI = BrakeLine2PressurePSI;
					car.BrakeSystem.BrakeLine3PressurePSI = 0;
				}
			}
		}

		/// <summary>
		/// Cars have been added to the rear of the train, recalc the rearTDBtraveller
		/// </summary>
		public void RepositionRearTraveller()
		{
            var traveller = new Traveller(FrontTDBTraveller, Traveller.TravellerDirection.Backward);
            // The traveller location represents the front of the train.
            var length = 0f;

			// process the cars first to last
            for (var i = 0; i < Cars.Count; ++i)
            {
                var car = Cars[i];
                if (car.WheelAxlesLoaded)
                {
                    car.ComputePosition(traveller, false);
                }
                else
                {
                    var bogieSpacing = car.Length * 0.65f;  // we'll use this approximation since the wagfile doesn't contain info on bogie position

                    // traveller is positioned at the front of the car
                    // advance to the first bogie 
                    traveller.Move((car.Length - bogieSpacing) / 2.0f);
                    var tileX = traveller.TileX;
                    var tileZ = traveller.TileZ;
                    var x = traveller.X;
                    var y = traveller.Y;
                    var z = traveller.Z;
                    traveller.Move(bogieSpacing);

                    // normalize across tile boundaries
                    while (tileX > traveller.TileX) { x += 2048; --tileX; }
                    while (tileX < traveller.TileX) { x -= 2048; ++tileX; }
                    while (tileZ > traveller.TileZ) { z += 2048; --tileZ; }
                    while (tileZ < traveller.TileZ) { z -= 2048; ++tileZ; }

                    // note the railcar sits 0.275meters above the track database path  TODO - is this always consistent?
                    car.WorldPosition.XNAMatrix = Matrix.Identity;
                    if (!car.Flipped)
                    {
                        //  Rotate matrix 180' around Y axis.
                        car.WorldPosition.XNAMatrix.M11 = -1;
                        car.WorldPosition.XNAMatrix.M33 = -1;
                    }
                    car.WorldPosition.XNAMatrix *= Simulator.XNAMatrixFromMSTSCoordinates(traveller.X, traveller.Y + 0.275f, traveller.Z, x, y + 0.275f, z);
                    car.WorldPosition.TileX = traveller.TileX;
                    car.WorldPosition.TileZ = traveller.TileZ;

                    traveller.Move((car.Length - bogieSpacing) / 2.0f);
                }
                if (i < Cars.Count - 1)
                {
                    traveller.Move(car.CouplerSlackM + car.GetCouplerZeroLengthM());
                    length += car.CouplerSlackM + car.GetCouplerZeroLengthM();
                }
                length += car.Length;
            }

			traveller.ReverseDirection();
			RearTDBTraveller = traveller;
            Length = length;
		} // RepositionRearTraveller


		/// <summary>
        /// Check if train is passenger or freight train
        /// </summary>
        public void CheckFreight()
        {
            IsFreight = false;
            foreach (var car in Cars)
            {
                    if (car.IsFreight) IsFreight = true;
            }
        } // CheckFreight

        /// <summary>
		/// Distance is the signed distance the cars are moving.
		/// </summary>
		/// <param name="distance"></param>
		public void CalculatePositionOfCars(float distance)
		{
            var tn = RearTDBTraveller.TN;
			RearTDBTraveller.Move(distance);
			if (distance < 0 && tn != RearTDBTraveller.TN)
				AlignTrailingPointSwitch(tn, RearTDBTraveller.TN, this);

			var traveller = new Traveller(RearTDBTraveller);
			// The traveller location represents the back of the train.
            var length = 0f;

			// process the cars last to first
            for (var i = Cars.Count - 1; i >= 0; --i)
			{
                var car = Cars[i];
                if (i < Cars.Count - 1)
                {
                    traveller.Move(car.CouplerSlackM + car.GetCouplerZeroLengthM());
                    length += car.CouplerSlackM + car.GetCouplerZeroLengthM();
                }
                if (car.WheelAxlesLoaded)
                {
                    car.ComputePosition(traveller, true);
                }
                else
                {
                    var bogieSpacing = car.Length * 0.65f;  // we'll use this approximation since the wagfile doesn't contain info on bogie position

                    // traveller is positioned at the back of the car
                    // advance to the first bogie 
                    traveller.Move((car.Length - bogieSpacing) / 2.0f);
                    var tileX = traveller.TileX;
                    var tileZ = traveller.TileZ;
                    var x = traveller.X;
                    var y = traveller.Y;
                    var z = traveller.Z;
                    traveller.Move(bogieSpacing);

                    // normalize across tile boundaries
                    while (tileX > traveller.TileX) { x += 2048; --tileX; }
                    while (tileX < traveller.TileX) { x -= 2048; ++tileX; }
                    while (tileZ > traveller.TileZ) { z += 2048; --tileZ; }
                    while (tileZ < traveller.TileZ) { z -= 2048; ++tileZ; }


                    // note the railcar sits 0.275meters above the track database path  TODO - is this always consistent?
                    car.WorldPosition.XNAMatrix = Matrix.Identity;
                    if (car.Flipped)
                    {
                        //  Rotate matrix 180' around Y axis.
                        car.WorldPosition.XNAMatrix.M11 = -1;
                        car.WorldPosition.XNAMatrix.M33 = -1;
                    }
                    car.WorldPosition.XNAMatrix *= Simulator.XNAMatrixFromMSTSCoordinates(traveller.X, traveller.Y + 0.275f, traveller.Z, x, y + 0.275f, z);
                    car.WorldPosition.TileX = traveller.TileX;
                    car.WorldPosition.TileZ = traveller.TileZ;

                    traveller.Move((car.Length - bogieSpacing) / 2.0f);  // Move to the front of the car 
                }
                length += car.Length;
            }

			if (distance > 0 && traveller.TN != FrontTDBTraveller.TN)
				AlignTrailingPointSwitch(FrontTDBTraveller.TN, traveller.TN, this);
			FrontTDBTraveller = traveller;
            Length = length;
			travelled += distance;
		} // CalculatePositionOfCars

		// aligns a trailing point switch that was just moved over to match the track the train is on
		public void AlignTrailingPointSwitch(TrackNode from, TrackNode to, Train train)
		{
			if (from.TrJunctionNode != null)
				return;
			TrackNode sw = null;
			if (to.TrJunctionNode != null)
				sw = to;
			else
			{
				foreach (TrPin fp in from.TrPins)
				{
					foreach (TrPin tp in to.TrPins)
					{
						if (fp.Link == tp.Link)
						{
							sw = Program.Simulator.TDB.TrackDB.TrackNodes[fp.Link];
							break;
						}
					}
					if (sw != null)
						break;
				}
			}
			if (sw == null || sw.TrJunctionNode == null)
				return;
			for (int i = 0; i < sw.Outpins; i++)
			{
				if (to == Program.Simulator.TDB.TrackDB.TrackNodes[sw.TrPins[i + sw.Inpins].Link])
				{
					sw.TrJunctionNode.SelectedRoute = i;
					//multiplayer mode will do some message to the server
					if (Simulator.PlayerLocomotive != null && train == Simulator.PlayerLocomotive.Train 
						&& MPManager.IsMultiPlayer() && !MPManager.IsServer())
					{
						MPManager.Notify((new MSGSwitch(MPManager.GetUserName(),
							sw.TrJunctionNode.TN.UiD.TileX, sw.TrJunctionNode.TN.UiD.TileZ, sw.TrJunctionNode.TN.UiD.WorldID, sw.TrJunctionNode.SelectedRoute, false)).ToString());
						//MPManager.Instance().ignoreSwitchStart = Simulator.GameTime;
					}
					return;
				}
			}
		}

		//  Sets this train's speed so that momentum is conserved when otherTrain is coupled to it
		public void SetCoupleSpeed(Train otherTrain, float otherMult)
		{
			float kg1 = 0;
			foreach (TrainCar car in Cars)
				kg1 += car.MassKG;
			float kg2 = 0;
			foreach (TrainCar car in otherTrain.Cars)
				kg2 += car.MassKG;
			SpeedMpS = (kg1 * SpeedMpS + kg2 * otherTrain.SpeedMpS * otherMult) / (kg1 + kg2);
			otherTrain.SpeedMpS = SpeedMpS;
			foreach (TrainCar car1 in Cars)
				car1.SpeedMpS = car1.Flipped ? -SpeedMpS : SpeedMpS;
			foreach (TrainCar car2 in otherTrain.Cars)
				car2.SpeedMpS = car2.Flipped ? -SpeedMpS : SpeedMpS;
		}

		// setups of the left hand side of the coupler force solving equations
		void SetupCouplerForceEquations()
		{
			for (int i = 0; i < Cars.Count - 1; i++)
			{
				TrainCar car = Cars[i];
				car.CouplerForceB = 1 / car.MassKG;
				car.CouplerForceA = -car.CouplerForceB;
				car.CouplerForceC = -1 / Cars[i + 1].MassKG;
				car.CouplerForceB -= car.CouplerForceC;
			}
		}

		// solves coupler force equations
		void SolveCouplerForceEquations()
		{
			float b = Cars[0].CouplerForceB;
			Cars[0].CouplerForceU = Cars[0].CouplerForceR / b;
			for (int i = 1; i < Cars.Count - 1; i++)
			{
				Cars[i].CouplerForceG = Cars[i - 1].CouplerForceC / b;
				b = Cars[i].CouplerForceB - Cars[i].CouplerForceA * Cars[i].CouplerForceG;
				Cars[i].CouplerForceU = (Cars[i].CouplerForceR - Cars[i].CouplerForceA * Cars[i - 1].CouplerForceU) / b;
			}
			for (int i = Cars.Count - 3; i >= 0; i--)
				Cars[i].CouplerForceU -= Cars[i + 1].CouplerForceG * Cars[i + 1].CouplerForceU;
		}

		// removes equations if forces don't match faces in contact
		// returns true if a change is made
		bool FixCouplerForceEquations()
		{
			for (int i = 0; i < Cars.Count - 1; i++)
			{
				TrainCar car = Cars[i];
				if (car.CouplerSlackM < 0 || car.CouplerForceB >= 1)
					continue;
				float maxs1 = car.GetMaximumCouplerSlack1M();
				if (car.CouplerSlackM < maxs1 || car.CouplerForceU > 0)
				{
					SetCouplerForce(car, 0);
					return true;
				}
			}
			for (int i = Cars.Count - 1; i >= 0; i--)
			{
				TrainCar car = Cars[i];
				if (car.CouplerSlackM > 0 || car.CouplerForceB >= 1)
					continue;
				float maxs1 = car.GetMaximumCouplerSlack1M();
				if (car.CouplerSlackM > -maxs1 || car.CouplerForceU < 0)
				{
					SetCouplerForce(car, 0);
					return true;
				}
			}
			return false;
		}

		// changes the coupler force equation for car to make the corresponding force equal to forceN
		void SetCouplerForce(TrainCar car, float forceN)
		{
			car.CouplerForceA = car.CouplerForceC = 0;
			car.CouplerForceB = 1;
			car.CouplerForceR = forceN;
		}

		// removes equations if forces don't match faces in contact
		// returns true if a change is made
		bool FixCouplerImpulseForceEquations()
		{
			for (int i = 0; i < Cars.Count - 1; i++)
			{
				TrainCar car = Cars[i];
				if (car.CouplerSlackM < 0 || car.CouplerForceB >= 1)
					continue;
				if (car.CouplerSlackM < car.CouplerSlack2M || car.CouplerForceU > 0)
				{
					SetCouplerForce(car, 0);
					return true;
				}
			}
			for (int i = Cars.Count - 1; i >= 0; i--)
			{
				TrainCar car = Cars[i];
				if (car.CouplerSlackM > 0 || car.CouplerForceB >= 1)
					continue;
				if (car.CouplerSlackM > -car.CouplerSlack2M || car.CouplerForceU < 0)
				{
					SetCouplerForce(car, 0);
					return true;
				}
			}
			return false;
		}

		// computes and applies coupler impulse forces which force speeds to match when no relative movement is possible
		void AddCouplerImpuseForces()
		{
			if (Cars.Count < 2)
				return;
			SetupCouplerForceEquations();
			for (int i = 0; i < Cars.Count - 1; i++)
			{
				TrainCar car = Cars[i];
				float max = car.CouplerSlack2M;
				if (-max < car.CouplerSlackM && car.CouplerSlackM < max)
				{
					car.CouplerForceB = 1;
					car.CouplerForceA = car.CouplerForceC = car.CouplerForceR = 0;
				}
				else
					car.CouplerForceR = Cars[i + 1].SpeedMpS - car.SpeedMpS;
			}
			do
				SolveCouplerForceEquations();
			while (FixCouplerImpulseForceEquations());
			MaximumCouplerForceN = 0;
			for (int i = 0; i < Cars.Count - 1; i++)
			{
				Cars[i].SpeedMpS += Cars[i].CouplerForceU / Cars[i].MassKG;
				Cars[i + 1].SpeedMpS -= Cars[i].CouplerForceU / Cars[i + 1].MassKG;
				//if (Cars[i].CouplerForceU != 0)
				//    Console.WriteLine("impulse {0} {1} {2} {3} {4}", i, Cars[i].CouplerForceU, Cars[i].CouplerSlackM, Cars[i].SpeedMpS, Cars[i+1].SpeedMpS);
				//if (MaximumCouplerForceN < Math.Abs(Cars[i].CouplerForceU))
				//    MaximumCouplerForceN = Math.Abs(Cars[i].CouplerForceU);
			}
		}

		// computes coupler acceleration balancing forces
		void ComputeCouplerForces()
		{
			for (int i = 0; i < Cars.Count; i++)
				if (Cars[i].SpeedMpS > 0)
					Cars[i].TotalForceN -= (Cars[i].FrictionForceN  + Cars[i].BrakeForceN);
				else if (Cars[i].SpeedMpS < 0)
					Cars[i].TotalForceN += Cars[i].FrictionForceN + Cars[i].BrakeForceN;
			if (Cars.Count < 2)
				return;
			SetupCouplerForceEquations();
			for (int i = 0; i < Cars.Count - 1; i++)
			{
				TrainCar car = Cars[i];
				float max = car.GetMaximumCouplerSlack1M();
				if (-max < car.CouplerSlackM && car.CouplerSlackM < max)
				{
					car.CouplerForceB = 1;
					car.CouplerForceA = car.CouplerForceC = car.CouplerForceR = 0;
				}
				else
					car.CouplerForceR = Cars[i + 1].TotalForceN / Cars[i + 1].MassKG - car.TotalForceN / car.MassKG;
			}
			do
				SolveCouplerForceEquations();
			while (FixCouplerForceEquations());
			for (int i = 0; i < Cars.Count - 1; i++)
			{
				TrainCar car = Cars[i];
				car.TotalForceN += car.CouplerForceU;
				Cars[i + 1].TotalForceN -= car.CouplerForceU;
				if (MaximumCouplerForceN < Math.Abs(car.CouplerForceU))
					MaximumCouplerForceN = Math.Abs(car.CouplerForceU);
				float maxs = car.GetMaximumCouplerSlack2M();
				if (car.CouplerForceU > 0)
				{
					float f = -(car.CouplerSlackM + car.GetMaximumCouplerSlack1M()) * car.GetCouplerStiffnessNpM();
					if (car.CouplerSlackM > -maxs && f > car.CouplerForceU)
						car.CouplerSlack2M = -car.CouplerSlackM;
					else
						car.CouplerSlack2M = maxs;
				}
				else if (car.CouplerForceU == 0)
					car.CouplerSlack2M = maxs;
				else
				{
					float f = (car.CouplerSlackM - car.GetMaximumCouplerSlack1M()) * car.GetCouplerStiffnessNpM();
					if (car.CouplerSlackM < maxs && f > car.CouplerForceU)
						car.CouplerSlack2M = car.CouplerSlackM;
					else
						car.CouplerSlack2M = maxs;
				}
			}
		}
		void UpdateCarSpeeds(float elapsedTime)
		{
			int n = 0;
			foreach (TrainCar car in Cars)
			{
				if (car.SpeedMpS > 0)
				{
					car.SpeedMpS += car.TotalForceN / car.MassKG * elapsedTime;
					if (car.SpeedMpS < 0)
						car.SpeedMpS = 0;
				}
				else if (car.SpeedMpS < 0)
				{
					car.SpeedMpS += car.TotalForceN / car.MassKG * elapsedTime;
					if (car.SpeedMpS > 0)
						car.SpeedMpS = 0;
				}
				else
					n++;
			}
			if (n == 0)
				return;
			// start cars moving forward
			for (int i = 0; i < Cars.Count; i++)
			{
				TrainCar car = Cars[i];
				if (car.SpeedMpS != 0 || car.TotalForceN <= (car.FrictionForceN + car.BrakeForceN))
					continue;
				int j = i;
				float f = 0;
				float m = 0;
				for (; ; )
				{
                    if (car.IsDriveable)
                        f += car.TotalForceN - (car.FrictionForceN);
                    else
					    f += car.TotalForceN - (car.FrictionForceN + car.BrakeForceN);
					m += car.MassKG;
					if (j == Cars.Count - 1 || car.CouplerSlackM < car.GetMaximumCouplerSlack2M())
						break;
					j++;
					car = Cars[j];
				}
				if (f > 0)
				{
					for (int k = i; k <= j; k++)
						Cars[k].SpeedMpS = f / m * elapsedTime;
					n -= j - i + 1;
				}
			}
			if (n == 0)
				return;
			// start cars moving backward
			for (int i = Cars.Count - 1; i >= 0; i--)
			{
				TrainCar car = Cars[i];
                if (car.SpeedMpS != 0 || car.TotalForceN > (-1.0f * (car.FrictionForceN + car.BrakeForceN)))
					continue;
				int j = i;
				float f = 0;
				float m = 0;
				for (; ; )
				{
                    if (car.IsDriveable)
					    f += car.TotalForceN + car.FrictionForceN;
                    else
                        f += car.TotalForceN + car.FrictionForceN + car.BrakeForceN;
					m += car.MassKG;
					if (j == 0 || car.CouplerSlackM > -car.GetMaximumCouplerSlack2M())
						break;
					j--;
					car = Cars[j];
				}
				if (f < 0)
				{
					for (int k = j; k <= i; k++)
						Cars[k].SpeedMpS = f / m * elapsedTime;
				}
			}
		}
		void UpdateCouplerSlack(float elapsedTime)
		{
			TotalCouplerSlackM = 0;
			NPull = NPush = 0;
			for (int i = 0; i < Cars.Count - 1; i++)
			{
				TrainCar car = Cars[i];
				car.CouplerSlackM += (car.SpeedMpS - Cars[i + 1].SpeedMpS) * elapsedTime;
				float max = car.GetMaximumCouplerSlack2M();
				if (car.CouplerSlackM < -max)
					car.CouplerSlackM = -max;
				else if (car.CouplerSlackM > max)
					car.CouplerSlackM = max;
				TotalCouplerSlackM += car.CouplerSlackM;
				max = car.GetMaximumCouplerSlack1M();
				if (car.CouplerSlackM >= max)
					NPull++;
				else if (car.CouplerSlackM <= -max)
					NPush++;
			}
			foreach (TrainCar car in Cars)
				car.DistanceM += Math.Abs(car.SpeedMpS * elapsedTime);
		}





		/// <summary>
		/// Traverse the cars, occupying tracks underneath.
		/// </summary>
		public void UpdateTrackOccupation()
		{
			TrackNode tn = RearTDBTraveller.TN;

			Traveller traveller = new Traveller(RearTDBTraveller);
			// The traveller location represents the back of the train.

			// process the cars last to first
			for (int i = Cars.Count - 1; i >= 0; i--)
			{
				TrainCar car = Cars[i];

				if (i < Cars.Count - 1)
				{
					traveller.Move(car.CouplerSlackM + car.GetCouplerZeroLengthM());
				}

				TrackNode node = traveller.TN;

				if (node != null && Simulator.InterlockingSystem.Tracks.ContainsKey(node))
				{
					node.InterlockingTrack.Occupy();
				}

				// traveller is positioned at the back of the car

				// advance to the front of the car
				traveller.Move(car.Length);
			}
		}

		//used by remote train to update location based on message received
		public int expectedTileX, expectedTileZ, expectedTracIndex, expectedDIr, expectedTDir;
		public float expectedX, expectedZ, expectedTravelled;

		public void ToDoUpdate(int tni, int tX, int tZ, float x, float z, float eT, float speed, int dir, int tDir)
		{
			SpeedMpS = speed;
			expectedTileX = tX;
			expectedTileZ = tZ;
			expectedX = x;
			expectedZ = z;
			expectedTravelled = eT;
			expectedTracIndex = tni;
			expectedDIr = dir;
			expectedTDir = tDir;
			updateMSGReceived = true;
		}

        public static bool IsUnderObserving(int sigref)
        {
            int refc = 0;
            foreach (ObjectItemInfo oi in Program.Simulator.PlayerLocomotive.Train.SignalObjectItems)
            {
                if (oi.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                {
                    if (oi.ObjectDetails.thisRef == sigref)
                        return true;
                    refc++;
                    if (refc == 4)
                        break;
                }
            }

            foreach (AITrain ai in Program.Simulator.AI.AITrainDictionary.Values)
            {
                refc = 0;
                foreach (ObjectItemInfo oi in ai.SignalObjectItems)
                {
                    if (oi.ObjectType == ObjectItemInfo.ObjectItemType.SIGNAL)
                    {
                        if (oi.ObjectDetails.thisRef == sigref)
                            return true;
                        refc++;
                        if (refc == 4)
                            break;
                    }
                }
            }

            return false;
        }
	}// class Train
}
