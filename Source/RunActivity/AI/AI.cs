/* AI
 * 
 * Contains code to initialize and control AI trains.
 * Currently, AI trains are created at startup and moved down 1000 meters to make them
 * invisible.  This is done so the rendering code can discover the model it needs to draw.
 * 
 * 
/// COPYRIGHT 2009 by the Open Rails project.
/// This code is provided to enable you to contribute improvements to the open rails program.  
/// Use of the code for any other purpose or distribution of the code to anyone else
/// is prohibited without specific written permission from admin@openrails.org.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MSTS;
using ORTS.MultiPlayer;

namespace ORTS
{
    public class AI
    {
        Heap<AITrain> StartQueue = new Heap<AITrain>();
        public readonly Simulator Simulator;
        public List<AITrain> AITrains = new List<AITrain>();// active AI trains
        public Dictionary<int, AITrain> AITrainDictionary = new Dictionary<int, AITrain>();
        bool FirstUpdate = true; // flag for special processing if first call to Update
        public Dispatcher Dispatcher;

        /// <summary>
        /// Loads AI train information from activity file.
        /// Creates a queue of AI trains in the order they should appear.
        /// At the moment AI trains are also created off scene so the rendering code will know about them.
        /// </summary>
        public AI(Simulator simulator)
        {
            Simulator = simulator;
            Dispatcher = new Dispatcher(this);
            if (simulator.Activity != null && simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition != null)
                foreach (var sd in simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition.ServiceDefinitionList)
                {
                    AITrain train = CreateAITrain(sd);
                    if (train == null)
                        continue;
                    AITrainDictionary.Add(sd.UiD, train);
                }
            foreach (KeyValuePair<int, AITrain> kvp in AITrainDictionary)
                StartQueue.Add(kvp.Value.StartTime, kvp.Value);

            float distance = simulator.Activity != null ? Simulator.Trains[0].Length + 20 : float.MaxValue;
            Simulator.PlayerPath.AlignInitSwitches(Simulator.Trains[0].dRearTDBTraveller, 0, distance);
        }

        // restore game state
        public AI(Simulator simulator, BinaryReader inf)
        {
            Debug.Assert(simulator.Trains != null, "Cannot restore AI without Simulator.Trains.");
            Simulator = simulator;
            FirstUpdate = false;
            foreach (Train train in Simulator.Trains)
            {
                if (train.GetType() == typeof(AITrain))
                {
                    AITrain aiTrain = (AITrain)train;
                    AITrainDictionary.Add(aiTrain.UiD, aiTrain);
                    aiTrain.AI = this;
                    AITrains.Add(aiTrain);
                    aiTrain.Path.TrackDB = Simulator.TDB.TrackDB;
                    aiTrain.Path.TSectionDat = Simulator.TSectionDat;
                    for (; ; )
                    {
                        AIPathNode node = aiTrain.Path.ReadNode(inf);
                        if (node == null)
                            break;
                        AISwitchInfo sw = new AISwitchInfo(aiTrain.Path, node);
                        sw.SelectedRoute = inf.ReadInt32();
                        sw.DistanceM = inf.ReadSingle();
                        aiTrain.SwitchList.Add(sw);
                    }
                }
            }
            int n = inf.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                double time = inf.ReadDouble();
                AITrain train = new AITrain(Simulator, inf);
                StartQueue.Add(time, train);
                AITrainDictionary.Add(train.UiD, train);
                train.AI = this;
                train.Path.TrackDB = Simulator.TDB.TrackDB;
                train.Path.TSectionDat = Simulator.TSectionDat;
            }
            Dispatcher = new Dispatcher(this, inf);
        }

        // save game state
        public void Save(BinaryWriter outf)
        {
            foreach (Train train in Simulator.Trains)
            {
                if (train.GetType() == typeof(AITrain))
                {
                    AITrain aiTrain = (AITrain)train;
                    foreach (AISwitchInfo sw in aiTrain.SwitchList)
                    {
                        aiTrain.Path.WriteNode(outf, sw.PathNode);
                        outf.Write(sw.SelectedRoute);
                        outf.Write(sw.DistanceM);
                    }
                    aiTrain.Path.WriteNode(outf, null);
                }
            }
            outf.Write(StartQueue.GetSize());
            for (int i = 0; i < StartQueue.GetSize(); i++)
            {
                outf.Write(StartQueue.getKey(i));
                StartQueue.getValue(i).Save(outf);
            }
            Dispatcher.Save(outf);
        }

        /// <summary>
        /// Updates AI train information.
        /// Creates any AI trains that are scheduled to appear.
        /// Moves all active AI trains by calling their Update method.
        /// And finally, removes any AI trains that have reached the end of their path.
        /// </summary>
        public void Update( float elapsedClockSeconds )
        {
            if (FirstUpdate)
            {
                foreach (KeyValuePair<int, AITrain> kvp in AITrainDictionary)
                    Simulator.Trains.Remove(kvp.Value);
                FirstUpdate = false;
            }
            Dispatcher.Update(Simulator.ClockTime, elapsedClockSeconds);
            while (StartQueue.GetMinKey() < Simulator.ClockTime)
            {
                AITrain train = StartQueue.GetMinValue();
                StartQueue.DeleteMin();
                // Added By GeorgeS
                if (Dispatcher.RequestAuth(train, false) == false)
                {
                    StartQueue.Add(Simulator.ClockTime + 10, train);
                }
                else
                {
                    MSTSElectricLocomotive el = train.FirstCar as MSTSElectricLocomotive;
                    if (el != null)
                    {
                        el.SetPantographFirst(true);
                    }
                    AITrains.Add(train);
                    Simulator.Trains.Add(train);
					//For Multiplayer: Server BroadCast to others of AITrains being added
					if (MPManager.IsMultiPlayer()) MPManager.BroadCast((new MSGTrain(train, train.Number)).ToString());
                    train.spad = true;
                    train.Update(0);
                    train.spad = false;
                    //train.InitializeSignals(false);
                }
            }
            bool remove = false;
            foreach (AITrain train in AITrains)
                if (train.NextStopNode == null || train.TrackAuthority.StartNode == null || train.Cars.Count == 0 || train.Cars[0].Train != train)
                    remove = true;
                else
                    train.AIUpdate( elapsedClockSeconds, Simulator.ClockTime);
            if (remove)
                RemoveTrains();
        }

        /// <summary>
        /// Creates an AI train
        /// Moves the models down 1000M to make them invisible.
        /// </summary>
        private AITrain CreateAITrain(Service_Definition sd)
        {
            // set up a new AI train
            // first extract the service definition from the activity file
            // this gives the consist and path
            // TODO combine this with similar player train code
            SRVFile srvFile = new SRVFile(Simulator.RoutePath + @"\SERVICES\" + sd.Name + ".SRV");
            string consistFileName = srvFile.Train_Config;
            CONFile conFile = new CONFile(Simulator.BasePath + @"\TRAINS\CONSISTS\" + consistFileName + ".CON");
            string pathFileName = Simulator.RoutePath + @"\PATHS\" + srvFile.PathID + ".PAT";

            PATFile patFile = new PATFile(pathFileName);
            AITrain train = new AITrain(Simulator, sd.UiD, this, new AIPath(patFile, Simulator.TDB, Simulator.TSectionDat, pathFileName), sd.Time);

            if (conFile.Train.TrainCfg.MaxVelocity.A > 0 && srvFile.Efficiency > 0)
                train.RouteMaxSpeedMpS = train.MaxSpeedMpS = conFile.Train.TrainCfg.MaxVelocity.A * srvFile.Efficiency;
	    		// also set Route max speed for speedpost-processing in train.cs [R.Roeterdink]

            // By GeorgeS
            float locoMaxSpeedMpS = (float)Simulator.TRK.Tr_RouteFile.SpeedLimit * srvFile.Efficiency;
            foreach (Wagon wagon in conFile.Train.TrainCfg.WagonList)
            {
                string wagonFolder = Simulator.BasePath + @"\trains\trainset\" + wagon.Folder;
                string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
                if (wagon.IsEngine)
                {
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");
                    TrainCar car = RollingStock.Load(Simulator, wagonFilePath, null);
                    MSTSLocomotive loco = car as MSTSLocomotive;
                    locoMaxSpeedMpS = Math.Min(loco.MaxSpeedMpS * srvFile.Efficiency, locoMaxSpeedMpS);
                }
            }

            if (locoMaxSpeedMpS < train.MaxSpeedMpS)
                train.MaxSpeedMpS = locoMaxSpeedMpS;

            WorldLocation wl = train.Path.FirstNode.Location;
            train.RearTDBTraveller = new Traveller(Simulator.TSectionDat, Simulator.TDB.TrackDB.TrackNodes, wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Z);
            //train.Path.AlignAllSwitches();
            train.Path.AlignInitSwitches(train.RearTDBTraveller, -1 , 500);
            // This is the position of the back end of the train in the database.
            //PATTraveller patTraveller = new PATTraveller(Simulator.RoutePath + @"\PATHS\" + pathFileName + ".PAT");
            // figure out if the next waypoint is forward or back
            //patTraveller.NextWaypoint();
            wl = train.GetNextNode(train.Path.FirstNode).Location;
            if (train.RearTDBTraveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z) < 0)
                train.RearTDBTraveller.ReverseDirection();
            float nodelen = train.RearTDBTraveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z);
            //train.PATTraveller = patTraveller;
            if (sd.Time < Simulator.ClockTime)
            {
                float dtS = (float)(Simulator.ClockTime - sd.Time);

                AIPathNode tnode = train.Path.FirstNode;
                float rdtS = dtS;
                float disttotravel = 0;
                while (tnode != null && tnode.NextMainTVNIndex != -1)
                {
                    if (tnode.Type == AIPathNodeType.Stop)
                    {
                        rdtS -= tnode.WaitTimeS;
                        if (rdtS < 0)
                        {
                            tnode.WaitTimeS = -(int)rdtS;
                            break;
                        }
                    }
                    rdtS -= nodelen / train.MaxSpeedMpS;
                    if (rdtS < 0)
                        break;
                    disttotravel += nodelen;
                    tnode = tnode.NextMainNode;
                    if (tnode != null && tnode.NextMainTVNIndex != -1)
                        nodelen = Dispatcher.TrackLength[tnode.NextMainTVNIndex];
                }

                float sttime = train.MaxSpeedMpS / train.MaxAccelMpSS;

                float dist = Math.Min(dtS, sttime) * train.MaxSpeedMpS / 2;
                dist += Math.Max(dtS - sttime, 0) * train.MaxSpeedMpS;

                dist = dist < disttotravel ? dist : disttotravel;

                train.Path.AlignInitSwitches(train.RearTDBTraveller, -1 , dist);

                // By GeorgeS
                if (tnode == null || tnode.Type != AIPathNodeType.Stop)
                    train.SpeedMpS = Math.Min(dtS, sttime) * train.MaxAccelMpSS;
                
                if (train.RearTDBTraveller.Move(dist) > 0.01 || train.RearTDBTraveller.TN.TrEndNode)
                    return null;
                AIPathNode node = train.Path.FirstNode;
                while (node != null && node.NextMainTVNIndex != train.RearTDBTraveller.TrackNodeIndex)
                    node = node.NextMainNode;
                if (node != null)
                    train.Path.FirstNode = node;
            }

            // add wagons
            TrainCar previousCar = null;
			var id = 0;
            foreach (Wagon wagon in conFile.Train.TrainCfg.WagonList)
            {

                string wagonFolder = Simulator.BasePath + @"\trains\trainset\" + wagon.Folder;
                string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
                if (wagon.IsEngine)
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                try
                {
                    TrainCar car = RollingStock.Load(Simulator, wagonFilePath, previousCar);
                    car.Flipped = wagon.Flip;
					car.UiD = id++;
					car.CarID = "AI" + train.UiD + " - " + car.UiD;
                    train.Cars.Add(car);
                    car.Train = train;
                    car.SignalEvent(EventID.Pantograph1Up);
                    previousCar = car;
                    car.SpeedMpS = car.Flipped ? -train.SpeedMpS : train.SpeedMpS;
                }
                catch (Exception error)
                {
					Trace.TraceInformation(wagonFilePath);
					Trace.WriteLine(error);
                }

            }// for each rail car

			train.Cars[0].Headlight = 2;//AI train always has light on
            train.CalculatePositionOfCars(0);
            for (int i = 0; i < train.Cars.Count; i++)
                train.Cars[i].WorldPosition.XNAMatrix.M42 -= 1000;
            if (train.FrontTDBTraveller.IsEnd)
                return null;

			train.CheckFreight(); // check if train is freight or passenger [R.Roeterdink]
            train.AITrainDirectionForward = true;
            train.BrakeLine3PressurePSI = 0;
            train.InitializeSignals(false);  // Initialize Signals and Speedlimits without active speed information [R.Roeterdink]

            // By GeorgeS
            //train.InitializeSignals();

            //AITrains.Add(train);
            Simulator.Trains.Add(train);
            return train;
        }

        /// <summary>
        /// Removes AI trains that have reached the end of their path or
        /// have been coupled onto by the player train.
        /// Moves the models down 1000M to make them invisible.
        /// </summary>
        private void RemoveTrains()
        {
            List<Train> removeList = new List<Train>();
            foreach (AITrain train in AITrains)
                if (train.NextStopNode == null || train.TrackAuthority.StartNode == null || train.Cars.Count == 0 || train.Cars[0].Train != train)
                    removeList.Add(train);
            foreach (AITrain train in removeList)
            {
                AITrains.Remove(train);
                Simulator.Trains.Remove(train);
                Dispatcher.Release(train);
                train.Release();
                if (train.Cars.Count > 0 && train.Cars[0].Train == train)
                    foreach (TrainCar car in train.Cars)
                        car.Train = null; // WorldPosition.XNAMatrix.M42 -= 1000;
            }
			//server broadcast to others about removing this train
			if (MPManager.IsServer()) MPManager.BroadCast((new MSGRemoveTrain(removeList).ToString()));

        }

        public string GetStatus()
        {
            return Dispatcher.PlayerStatus();
        }
    }

    public class StartQueue
    {
        List<Service_Definition> List = new List<Service_Definition>();
        int QueueSize = 0;
        public void Add(Service_Definition sd)
        {
            if (QueueSize < List.Count)
                List[QueueSize]= sd;
            else
                List.Add(sd);
            int i = QueueSize++;
            while (i > 0)
            {
                int j = (i - 1) / 2;
                if (List[j].Time <= List[i].Time)
                    break;
                Service_Definition t = List[j];
                List[j] = List[i];
                List[i] = t;
                i = j;
            }
        }
        public void Print()
        {
            Console.WriteLine("StartQueue {0}",QueueSize);
            for (int i=0; i<QueueSize; i++)
                Console.WriteLine(" {0} {1}", i, List[i].Time);
        }
        public Service_Definition GetNext(double time)
        {
            if (QueueSize <= 0 || List[0].Time > time)
                return null;
            Service_Definition result = List[0];
            List[0] = List[--QueueSize];
            int i = 0;
            while (true)
            {
                int j = 2 * i + 1;
                if (j >= QueueSize)
                    break;
                if (j < QueueSize-1 && List[j + 1].Time < List[j].Time)
                    j++;
                if (List[i].Time <= List[j].Time)
                    break;
                Service_Definition t = List[j];
                List[j] = List[i];
                List[i] = t;
                i = j;
            }
            return result;
        }
    }
}
