// COPYRIGHT 2012, 2013 by the Open Rails project.
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

using Orts.Simulation;
using Orts.Simulation.AIs;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Scripting.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Orts.MultiPlayer
{
    public class OnlineTrains
    {
        public Dictionary<string, OnlinePlayer> Players;
        public OnlineTrains()
        {
            Players = new Dictionary<string, OnlinePlayer>();
        }

        public List<OnlineLocomotive> OnlineLocomotives = new List<OnlineLocomotive>();

        public static void Update()
        {

        }

        public Train findTrain(string name)
        {
            if (Players.ContainsKey(name))
                return Players[name].Train;
            else return null;
        }

        public bool findTrain(Train t)
        {

            foreach (OnlinePlayer o in Players.Values.ToList())
            {
                if (o.Train == t) return true;
            }
            return false;
        }

        public string MoveTrains(MSGMove move)
        {
            string tmp = "";
            if (move == null) move = new MSGMove();
            foreach (OnlinePlayer p in Players.Values)
            {
                if (p.Train != null && MPManager.Simulator.PlayerLocomotive != null && !(p.Train == MPManager.Simulator.PlayerLocomotive.Train && p.Train.TrainType != Train.TRAINTYPE.REMOTE))
                {
                    if (Math.Abs(p.Train.SpeedMpS) > 0.001 || Math.Abs(p.Train.LastReportedSpeed) > 0)
                    {
                        move.AddNewItem(p.Username, p.Train);
                    }
                }
            }
            foreach (Train t in MPManager.Simulator.Trains)
            {
                if (MPManager.Simulator.PlayerLocomotive != null && t == MPManager.Simulator.PlayerLocomotive.Train) continue;//player drived train
                if (t == null || findTrain(t)) continue;//is an online player controlled train
                if (Math.Abs(t.SpeedMpS) > 0.001 || Math.Abs(t.LastReportedSpeed) > 0)
                {
                    move.AddNewItem("0xAI" + t.Number, t);
                }
            }
            tmp += move.ToString();
            return tmp;

        }
        public string MoveAllPlayerTrain(MSGMove move)
        {
            string tmp = "";
            if (move == null) move = new MSGMove();
            foreach (OnlinePlayer p in Players.Values)
            {
                if (p.Train == null) continue;
                if (Math.Abs(p.Train.SpeedMpS) > 0.001 || Math.Abs(p.Train.LastReportedSpeed) > 0)
                {
                    move.AddNewItem(p.Username, p.Train);
                }
            }
            tmp += move.ToString();
            return tmp;
        }

        public static string MoveAllTrain(MSGMove move)
        {
            string tmp = "";
            if (move == null) move = new MSGMove();
            foreach (Train t in MPManager.Simulator.Trains)
            {
                if (t != null && (Math.Abs(t.SpeedMpS) > 0.001 || Math.Abs(t.LastReportedSpeed) > 0))
                {
                    move.AddNewItem("AI" + t.Number, t);
                }
            }
            tmp += move.ToString();
            return tmp;
        }

        public string AddAllPlayerTrain() //WARNING, need to change
        {
            string tmp = "";
            foreach (OnlinePlayer p in Players.Values)
            {
                if (p.Train != null)
                {
                    MSGPlayer player = new MSGPlayer(p.Username, "1234", p.con, p.path, p.Train, p.Train.Number, p.url);
                    tmp += player.ToString();
                }
            }
            return tmp;
        }
        public void AddPlayers(MSGPlayer player, OnlinePlayer p)
        {
            if (Players.ContainsKey(player.user)) return;
            if (MPManager.Client != null && player.user == MPManager.Client.UserName) return; //do not add self//WARNING: may need to worry about train number here
            if (p == null)
            {
                p = new OnlinePlayer(null, null);
            }
            p.url = player.url;
            p.LeadingLocomotiveID = player.leadingID;
            p.con = MPManager.Simulator.BasePath + "\\TRAINS\\CONSISTS\\" + player.con;
            p.path = MPManager.Simulator.RoutePath + "\\PATHS\\" + player.path;
            Train train = new Train(MPManager.Simulator);
            train.TrainType = Train.TRAINTYPE.REMOTE;
            if (MPManager.IsServer()) //server needs to worry about correct train number
            {
            }
            else
            {
                train.Number = player.num;
            }
            if (player.con.Contains("tilted")) train.IsTilting = true;
            int direction = player.dir;
            train.travelled = player.Travelled;
            train.TrainMaxSpeedMpS = player.trainmaxspeed;

            if (MPManager.IsServer())
            {
                try
                {
#if ACTIVITY_EDITOR
                    AIPath aiPath = new AIPath(MPManager.Simulator.TDB, MPManager.Simulator.TSectionDat, p.path, MPManager.Simulator.TimetableMode, MPManager.Simulator.orRouteConfig);
#else
                    AIPath aiPath = new AIPath(MPManager.Simulator.TDB, MPManager.Simulator.TSectionDat, p.path);
#endif
                }
                catch (Exception) { MPManager.BroadCast((new MSGMessage(player.user, "Warning", "Server does not have path file provided, signals may always be red for you.")).ToString()); }
            }

            try
            {
                train.RearTDBTraveller = new Traveller(MPManager.Simulator.TSectionDat, MPManager.Simulator.TDB.TrackDB.TrackNodes, player.TileX, player.TileZ, player.X, player.Z, direction == 1 ? Traveller.TravellerDirection.Forward : Traveller.TravellerDirection.Backward);
            }
            catch (Exception e)
            {
                if (MPManager.IsServer())
                {
                    MPManager.BroadCast((new MSGMessage(player.user, "Error", "MultiPlayer Error：" + e.Message)).ToString());
                }
                else throw new Exception();
            }
            for (var i = 0; i < player.cars.Length; i++)// cars.Length-1; i >= 0; i--) {
            {

                string wagonFilePath = MPManager.Simulator.BasePath + @"\trains\trainset\" + player.cars[i];
                TrainCar car = null;
                try
                {
                    car = RollingStock.Load(MPManager.Simulator, wagonFilePath);
                    car.CarLengthM = player.lengths[i] / 100.0f;
                }
                catch (Exception error)
                {
                    System.Console.WriteLine(error.Message);
                    car = MPManager.Instance().SubCar(wagonFilePath, player.lengths[i]);
                }
                if (car == null) continue;
                bool flip = true;
                if (player.flipped[i] == 0) flip = false;
                car.Flipped = flip;
                car.CarID = player.ids[i];
                train.Cars.Add(car);
                car.Train = train;
                MSTSWagon w = (MSTSWagon)car;
                if (w != null)
                {
                    w.SignalEvent((player.pantofirst == 1 ? PowerSupplyEvent.RaisePantograph : PowerSupplyEvent.LowerPantograph), 1);
                    w.SignalEvent((player.pantosecond == 1 ? PowerSupplyEvent.RaisePantograph : PowerSupplyEvent.LowerPantograph), 2);
                    w.SignalEvent((player.pantothird == 1 ? PowerSupplyEvent.RaisePantograph : PowerSupplyEvent.LowerPantograph), 3);
                    w.SignalEvent((player.pantofourth == 1 ? PowerSupplyEvent.RaisePantograph : PowerSupplyEvent.LowerPantograph), 4);
                }

            }// for each rail car

            if (train.Cars.Count == 0)
            {
                throw (new Exception("The train of player " + player.user + " is empty from "));
            }

            p.Username = player.user;
            train.ControlMode = Train.TRAIN_CONTROL.EXPLORER;
            train.CheckFreight();
            train.InitializeBrakes();
            bool canPlace = true;
            Train.TCSubpathRoute tempRoute = train.CalculateInitialTrainPosition(ref canPlace);
            if (tempRoute.Count == 0 || !canPlace)
            {
                MPManager.BroadCast((new MSGMessage(p.Username, "Error", "Cannot be placed into the game")).ToString());//server will broadcast this error
                throw new InvalidDataException("Remote train original position not clear");
            }

            train.SetInitialTrainRoute(tempRoute);
            train.CalculatePositionOfCars();
            train.ResetInitialTrainRoute(tempRoute);

            train.CalculatePositionOfCars();
            train.AITrainBrakePercent = 100;

            //if (MPManager.Instance().AllowedManualSwitch) train.InitializeSignals(false);
            for (int iCar = 0; iCar < train.Cars.Count; iCar++)
            {
                var car = train.Cars[iCar];
                if (car.CarID == p.LeadingLocomotiveID)
                {
                    train.LeadLocomotive = car;
                    (train.LeadLocomotive as MSTSLocomotive).Headlight = player.headlight;
                    (train.LeadLocomotive as MSTSLocomotive).UsingRearCab = player.frontorrearcab == "R" ? true : false;
                }
                if (car is MSTSLocomotive && MPManager.IsServer())
                    MPManager.Instance().AddOrRemoveLocomotive(player.user, train.Number, iCar, true);
            }
            if (train.LeadLocomotive == null)
            {
                train.LeadNextLocomotive();
                if (train.LeadLocomotive != null) p.LeadingLocomotiveID = train.LeadLocomotive.CarID;
                else p.LeadingLocomotiveID = "NA";
            }

            if (train.LeadLocomotive != null)
            {
                train.Name = train.GetTrainName(train.LeadLocomotive.CarID);
            }
            else if (train.Cars != null && train.Cars.Count > 0)
            {
                train.Name = train.GetTrainName(train.Cars[0].CarID);
            }
            else if (player !=null && player.user != null) train.Name = player.user;

            if (MPManager.IsServer())
            {
                train.InitializeSignals(false);
            }
            p.Train = train;

            Players.Add(player.user, p);
            MPManager.Instance().AddOrRemoveTrain(train, true);

        }

        public void SwitchPlayerTrain(MSGPlayerTrainSw player)
        {
            // find info about the new player train
            // look in all trains

            if (MPManager.Client != null && player.user == MPManager.Client.UserName) return; //do not add self//WARNING: may need to worry about train number here
            OnlinePlayer p;
            var doesPlayerExist = Players.TryGetValue(player.user, out p);
            if (!doesPlayerExist) return;
            if (player.oldTrainReverseFormation) p.Train.ReverseFormation(false);
            p.LeadingLocomotiveID = player.leadingID;
            Train train;

            if (MPManager.IsServer()) //server needs to worry about correct train number
            {
                train = MPManager.Simulator.Trains.Find(t => t.Number == player.num);
                train.TrainType = Train.TRAINTYPE.REMOTE;
            }
            else
            {
                train = MPManager.Simulator.Trains.Find(t => t.Number == player.num);
                train.TrainType = Train.TRAINTYPE.REMOTE;
            }
            p.Train = train;
            if (player.newTrainReverseFormation) p.Train.ReverseFormation(false);
        }

        public string ExhaustingLocos(MSGExhaust exhaust)
        {
            string tmp = "";
            if (exhaust == null) exhaust = new MSGExhaust();
            foreach (OnlineLocomotive l in OnlineLocomotives)
            {
                if (l.userName != MPManager.GetUserName())
                {
                    Train t = MPManager.FindPlayerTrain(l.userName);
                    if (t != null && l.trainCarPosition < t.Cars.Count && (Math.Abs(t.SpeedMpS) > 0.001 || Math.Abs(t.LastReportedSpeed) > 0))
                    {
                            if (t.Cars[l.trainCarPosition] is MSTSDieselLocomotive)
                            {
                                exhaust.AddNewItem(l.userName, t, l.trainCarPosition);
                            }
                    }
                }
            }
            if (exhaust != null) tmp += exhaust.ToString();
            return tmp;
        }

        // Save
        public void Save (BinaryWriter outf)
        {
            outf.Write(Players.Count);
            foreach (var onlinePlayer in Players.Values)
            {
                onlinePlayer.Save(outf);
            }
        }

        // Restore
        public void Restore (BinaryReader inf)
        {
            var onlinePlayersCount = inf.ReadInt32();
            if (onlinePlayersCount > 0)
            {
                while (onlinePlayersCount > 0)
                {
                    OnlinePlayer player = new OnlinePlayer(inf);
                    Players.Add(player.Username, player);
                    onlinePlayersCount -= 1;
                }
            }
        }
    }

    public struct OnlineLocomotive
    {
        public string userName;
        public int trainNumber;
        public int trainCarPosition;
    }
}
