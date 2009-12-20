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
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using MSTS;

namespace ORTS
{
    public class AI
    {
        StartQueue StartQueue = new StartQueue();
        public Simulator Simulator;
        public List<AITrain> AITrains = new List<AITrain>();// active AI trains
        Dictionary<int, AITrain> AITrainDictionary = new Dictionary<int, AITrain>();
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
            if( simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition != null )
                foreach (Service_Definition sd in simulator.Activity.Tr_Activity.Tr_Activity_File.Traffic_Definition)
                {
                    StartQueue.Add(sd);
                    AITrainDictionary.Add(sd.UiD, CreateAITrain(sd));
                    //Console.WriteLine("AIQ {0} {1} {2}", sd.Service, sd.Time, sd.UiD);
                }
            Dispatcher = new Dispatcher(this);
        }

        /// <summary>
        /// Updates AI train information.
        /// Creates any AI trains that are scheduled to appear.
        /// Moves all active AI trains by calling their Update method.
        /// And finally, removes any AI trains that have reached the end of their path.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (FirstUpdate)
            {
                foreach (KeyValuePair<int, AITrain> kvp in AITrainDictionary)
                    Simulator.Trains.Remove(kvp.Value);
                FirstUpdate = false;
            }
            Dispatcher.Update(Simulator.ClockTime);
            for (Service_Definition sd = StartQueue.GetNext(Simulator.ClockTime); sd!=null; sd=StartQueue.GetNext(Simulator.ClockTime))
            {
                AITrain train = AITrainDictionary[sd.UiD];
                if (Dispatcher.RequestAuth(train) == false)
                {
                    sd.Time += 60;
                    StartQueue.Add(sd);
                }
                else
                {
                    AITrains.Add(train);
                    Simulator.Trains.Add(train);
                }
            }
            bool remove = false;
            foreach (AITrain train in AITrains)
                if (train.NextStopNode == null || train.RearNode == null || train.Cars[0].Train != train)
                    remove = true;
                else
                    train.AIUpdate(gameTime, Simulator.ClockTime);
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
            string pathFileName = srvFile.PathID;

            PATFile patFile = new PATFile(Simulator.RoutePath + @"\PATHS\" + pathFileName + ".PAT");
            AITrain train = new AITrain(sd.UiD, this, new AIPath(patFile, Simulator.TDB, Simulator.TSectionDat));

            // This is the position of the back end of the train in the database.
            //PATTraveller patTraveller = new PATTraveller(Simulator.RoutePath + @"\PATHS\" + pathFileName + ".PAT");
            WorldLocation wl = train.RearNode.Location;
            train.RearTDBTraveller = new TDBTraveller(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Z, 0, Simulator.TDB, Simulator.TSectionDat);
            // figure out if the next waypoint is forward or back
            //patTraveller.NextWaypoint();
            wl = train.GetNextNode(train.RearNode).Location;
            if (train.RearTDBTraveller.DistanceTo(wl.TileX, wl.TileZ, wl.Location.X, wl.Location.Y, wl.Location.Z) < 0)
                train.RearTDBTraveller.ReverseDirection();
            //train.PATTraveller = patTraveller;

            // add wagons
            foreach (ConsistTrainset wagon in conFile)
            {

                string wagonFolder = Simulator.BasePath + @"\trains\trainset\" + wagon.Folder;
                string wagonFilePath = wagonFolder + @"\" + wagon.File + ".wag"; ;
                if (wagon.IsEngine)
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                try
                {
                    TrainCarSimulator car = TrainCarSimulator.Create(wagonFilePath);
                    train.Cars.Add(car);
                    car.Train = train;
                }
                catch (System.Exception error)
                {
                    Console.Error.WriteLine("Couldn't open " + wagonFilePath + "\n" + error.Message);
                }

            }// for each rail car

            train.CalculatePositionOfCars(0);
            for (int i = 0; i < train.Cars.Count; i++)
                train.Cars[i].WorldPosition.XNAMatrix.M42 -= 1000;

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
                if (train.NextStopNode == null || train.RearNode == null || train.Cars[0].Train != train)
                    removeList.Add(train);
            foreach (AITrain train in removeList)
            {
                AITrains.Remove(train);
                Simulator.Trains.Remove(train);
                Dispatcher.Release(train);
                if (train.Cars[0].Train == train)
                    foreach (TrainCarSimulator car in train.Cars)
                        car.WorldPosition.XNAMatrix.M42 -= 1000;
            }
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
            QueueSize++;
        }
        public Service_Definition GetNext(double time)
        {
            for (int i=0; i<QueueSize; i++)
            {
                if (List[i] != null && List[i].Time < time)
                {
                    Service_Definition sd = List[i];
                    List[i] = List[--QueueSize];
                    return sd;
                }
            }
            return null;
        }
    }
}
