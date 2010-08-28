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

namespace ORTS
{
    public class AI
    {
        Heap<AITrain> StartQueue = new Heap<AITrain>();
        public Simulator Simulator;
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
            //Console.WriteLine("AI {0} {1} {2} {3}", ClockTime, st.Hour, st.Minute, st.Second);
            if (simulator.Activity != null && simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition != null)
                foreach (Service_Definition sd in simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition)
                {
                    AITrain train = CreateAITrain(sd);
                    if (train == null)
                        continue;
                    AITrainDictionary.Add(sd.UiD, train);
                    //Console.WriteLine("AIQ {0} {1} {2} {3}", sd.Service, sd.Time, sd.UiD, Simulator.ClockTime);
                }
            Dispatcher = new Dispatcher(this);
            foreach (KeyValuePair<int, AITrain> kvp in AITrainDictionary)
                StartQueue.Add(kvp.Value.StartTime, kvp.Value);
        }

        // restore game state
        public AI(Simulator simulator, BinaryReader inf)
        {
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
                AITrain train = new AITrain(inf);
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
                if (Dispatcher.RequestAuth(train,false) == false)
                    StartQueue.Add(Simulator.ClockTime + 10, train);
                else
                {
                    AITrains.Add(train);
                    Simulator.Trains.Add(train);
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
            SRVFile srvFile = new SRVFile(Simulator.RoutePath + @"\SERVICES\" + sd.Service + ".SRV");
            string consistFileName = srvFile.Train_Config;
            CONFile conFile = new CONFile(Simulator.BasePath + @"\TRAINS\CONSISTS\" + consistFileName + ".CON");
            string pathFileName = Simulator.RoutePath + @"\PATHS\" + srvFile.PathID + ".PAT";

            PATFile patFile = new PATFile(pathFileName);
            AITrain train = new AITrain(sd.UiD, this, new AIPath(patFile, Simulator.TDB, Simulator.TSectionDat, pathFileName), sd.Time);

            if (conFile.Train.TrainCfg.MaxVelocity.A > 0 && srvFile.Efficiency > 0)
                train.MaxSpeedMpS = conFile.Train.TrainCfg.MaxVelocity.A * srvFile.Efficiency;

            train.Path.AlignAllSwitches();
            // This is the position of the back end of the train in the database.
            //PATTraveller patTraveller = new PATTraveller(Simulator.RoutePath + @"\PATHS\" + pathFileName + ".PAT");
            WorldLocation wl = train.Path.FirstNode.Location;
            train.RearTDBTraveller = new TDBTraveller(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Z, 0, Simulator.TDB, Simulator.TSectionDat);
            // figure out if the next waypoint is forward or back
            //patTraveller.NextWaypoint();
            wl = train.GetNextNode(train.Path.FirstNode).Location;
            if (train.RearTDBTraveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z) < 0)
                train.RearTDBTraveller.ReverseDirection();
            //train.PATTraveller = patTraveller;
            if (sd.Time < Simulator.ClockTime)
            {
                float dtS = (float)(Simulator.ClockTime - sd.Time);
                if (train.RearTDBTraveller.Move(dtS * train.MaxSpeedMpS) > 0.01 || train.RearTDBTraveller.TN.TrEndNode != null)
                    return null;
                //Console.WriteLine("initial move {0} {1}", dtS * train.MaxSpeedMpS, train.MaxSpeedMpS);
                AIPathNode node = train.Path.FirstNode;
                while (node != null && node.NextMainTVNIndex != train.RearTDBTraveller.TrackNodeIndex)
                    node = node.NextMainNode;
                if (node != null)
                    train.Path.FirstNode = node;
            }

            // add wagons
            TrainCar previousCar = null;
            foreach (Wagon wagon in conFile.Train.TrainCfg.Wagons)
            {

                string wagonFolder = Simulator.BasePath + @"\trains\trainset\" + wagon.Folder;
                string wagonFilePath = wagonFolder + @"\" + wagon.Name + ".wag"; ;
                if (wagon.IsEngine)
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                try
                {
                    TrainCar car = RollingStock.Load(wagonFilePath, previousCar);
                    car.Flipped = wagon.Flip;
                    train.Cars.Add(car);
                    car.Train = train;
                    car.SignalEvent(EventID.PantographUp);
                    previousCar = car;
                }
                catch (Exception error)
                {
					Trace.WriteLine(wagonFilePath);
					Trace.WriteLine(error);
                }

            }// for each rail car

            train.CalculatePositionOfCars(0);
            for (int i = 0; i < train.Cars.Count; i++)
                train.Cars[i].WorldPosition.XNAMatrix.M42 -= 1000;
            if (train.FrontTDBTraveller.TN.TrEndNode != null)
                return null;

            train.AITrainDirectionForward = true;
            train.BrakeLine3PressurePSI = 0;

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
            List<AITrain> removeList = new List<AITrain>();
            foreach (AITrain train in AITrains)
                if (train.NextStopNode == null || train.TrackAuthority.StartNode == null || train.Cars.Count == 0 || train.Cars[0].Train != train)
                    removeList.Add(train);
            foreach (AITrain train in removeList)
            {
                AITrains.Remove(train);
                Simulator.Trains.Remove(train);
                Dispatcher.Release(train);
                if (train.Cars.Count > 0 && train.Cars[0].Train == train)
                    foreach (TrainCar car in train.Cars)
                        car.Train = null; // WorldPosition.XNAMatrix.M42 -= 1000;
            }
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
