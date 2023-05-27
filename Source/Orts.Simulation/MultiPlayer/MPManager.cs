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

/* MPManager
 * 
 * Contains code to manager multiplayer sessions, especially to hide server/client mode from other code.
 * For example, the Notify method will check if it is server (then broadcast) or client (then send msg to server)
 * but the caller does not need to care.
 * 
 * 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using GNU.Gettext;
using Orts.Common;
using Orts.Parsers.Msts;
using Orts.Simulation;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks;
using ORTS.Common;

namespace Orts.MultiPlayer
{
    //a singleton class handles communication, update and stop etc.
    public class MPManager
    {
        public static GettextResourceManager Catalog { get; private set; }
        public static Random Random { get; private set; }
        public static Simulator Simulator { get; internal set; }

        public static Server Server;
        public static ClientComm Client;

        public int version = 15;
        double lastMoveTime;
        public double lastSwitchTime;
        public double lastSyncTime = 0;
        double lastSendTime;
        string metric = "";
        double metricbase = 1.0f;
        public static OnlineTrains OnlineTrains = new OnlineTrains();
        private static MPManager localUser;

        public List<Train> removedTrains;
        private List<Train> addedTrains;
        public List<OnlineLocomotive> removedLocomotives;
        private List<OnlineLocomotive> addedLocomotives;

        private List<Train> uncoupledTrains;

        public bool weatherChanged = false;
        public int weather = -1;
        public float fogDistance = -1f;
        public float pricipitationIntensity = -1f;
        public float overcastFactor = -1f;
        public double serverTimeDifference = 0;

        public double lastPlayerAddedTime;
        public int MPUpdateInterval = 10;
        static public bool AllowedManualSwitch = true;
        public bool TrySwitch = true;
        public bool AllowNewPlayer = true;
        public bool ComposingText = false;
        public string lastSender = ""; //who last sends me a message
        public bool AmAider = false; //am I aiding the dispatcher?
        public List<string> aiderList;
        public Dictionary<string, OnlinePlayer> lostPlayer = new Dictionary<string, OnlinePlayer>();
        public bool NotServer = true;
        public bool CheckSpad = true;
        public static bool PreferGreen = true;
        public string MD5Check = "";

        public class ServerChangedEventArgs : EventArgs
        {
            public readonly bool WeAreTheServer;

            public ServerChangedEventArgs(bool weAreTheServer)
            {
                WeAreTheServer = weAreTheServer;
            }
        }

        public class AvatarUpdatedEventArgs : EventArgs
        {
            public readonly string User;
            public readonly string URL;

            public AvatarUpdatedEventArgs(string user, string url)
            {
                User = user;
                URL = url;
            }
        }

        public class MessageReceivedEventArgs : EventArgs
        {
            public readonly double Time;
            public readonly string Message;

            public MessageReceivedEventArgs(double time, string message)
            {
                Time = time;
                Message = message;
            }
        }

        public event EventHandler<ServerChangedEventArgs> ServerChanged;
        public event EventHandler<AvatarUpdatedEventArgs> AvatarUpdated;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public void AddUncoupledTrains(Train t)
        {
            lock (uncoupledTrains)
            {
                uncoupledTrains.Add(t);
            }
        }

        public void RemoveUncoupledTrains(Train t)
        {
            lock (uncoupledTrains)
            {
                uncoupledTrains.Remove(t);
            }
        }

        public string OriginalSwitchState = "";

        public void RememberOriginalSwitchState()
        {
            MSGSwitchStatus msg = new MSGSwitchStatus();
            var str = msg.ToString();
            var index = str.IndexOf("SWITCHSTATES ");
            OriginalSwitchState = str.Remove(0, index + 13);
        }
        //handles singleton
        private MPManager()
        {
            playersRemoved = new List<OnlinePlayer>();
            uncoupledTrains = new List<Train>();
            addedTrains = new List<Train>();
            removedTrains = new List<Train>();
            addedLocomotives = new List<OnlineLocomotive>();
            removedLocomotives = new List<OnlineLocomotive>();
            aiderList = new List<string>();
            if (Server != null) NotServer = false;
            users = new SortedList<double, string>();
            GetMD5HashFromTDBFile();
        }
        public static MPManager Instance()
        {
            if (localUser == null)
            {
                Catalog = new GettextResourceManager("Orts.Simulation");
                Random = new Random();
                localUser = new MPManager();
            }
            return localUser;
        }

        public static void RequestControl()
        {
            try
            {
                Train train = Simulator.PlayerLocomotive.Train;

                MSGControl msgctl;
                //I am the server, I have control
                if (IsServer())
                {
                    train.TrainType = Train.TRAINTYPE.PLAYER; train.LeadLocomotive = Simulator.PlayerLocomotive;
                    InitializeBrakesCommand.Receiver = MPManager.Simulator.PlayerLocomotive.Train;
                    train.InitializeSignals(false);
                    if (Simulator.Confirmer != null)
                        Simulator.Confirmer.Information(MPManager.Catalog.GetString("You gained back the control of your train"));
                    msgctl = new MSGControl(GetUserName(), "Confirm", train);
                    BroadCast(msgctl.ToString());
                }
                else //client, send request
                {
                    msgctl = new MSGControl(GetUserName(), "Request", train);
                    SendToServer(msgctl.ToString());
                }
            }
            catch (Exception)
            { }
        }

        public void PreUpdate()
        {
            if (NotServer == true && Server != null) //I was a server, but no longer
            {
                Server = null;
            }
            else if (NotServer == false && Server == null) //I am declared the server
            {
            }
        }

        double previousSpeed;
        double begineZeroTime;

        /// <summary>
        /// Update. Determines what messages to send every some seconds
        /// 1. every one second will send train location
        /// 2. by defaulr, every 10 seconds will send switch/signal status, this can be changed by in the menu of setting MPUpdateInterval
        /// 3. housekeeping (remove/add trains, remove players)
        /// 4. it will also capture key stroke of horn, panto, wiper, bell, headlight etc.
        /// </summary>
        public void Update(double newtime)
        {
            if (begineZeroTime == 0) begineZeroTime = newtime - 10;

            CheckPlayerTrainSpad();//over speed or pass a red light

            //server update train location of all
            if (Server != null && newtime - lastMoveTime >= 1f)
            {
                MSGMove move = new MSGMove();
                if (Simulator.PlayerLocomotive.Train.TrainType != Train.TRAINTYPE.REMOTE)
                    move.AddNewItem(GetUserName(), Simulator.PlayerLocomotive.Train);
                Server.BroadCast(OnlineTrains.MoveTrains(move));
                MSGExhaust exhaust = new MSGExhaust(); // Also updating loco exhaust
                Train t = Simulator.PlayerLocomotive.Train;
                for (int iCar = 0; iCar < t.Cars.Count; iCar++)
                {
                    if (t.Cars[iCar] is MSTSDieselLocomotive)
                    {
                        exhaust.AddNewItem(GetUserName(), t, iCar);
                    }
                }
                // Broadcast also exhaust
                var exhaustMessage = OnlineTrains.ExhaustingLocos(exhaust);
                if (exhaustMessage != "") Server.BroadCast(exhaustMessage);

                lastMoveTime = lastSendTime = newtime;

#if INDIVIDUAL_CONTROL
                if (Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE)
                {
                    Server.BroadCast((new MSGLocoInfo(Simulator.PlayerLocomotive, GetUserName())).ToString());
                }
#endif
            }

            //server updates switch
            if (Server != null && newtime - lastSwitchTime >= MPUpdateInterval)
            {
                lastSwitchTime = lastSendTime = newtime;
                var switchStatus = new MSGSwitchStatus();

                if (switchStatus.OKtoSend) BroadCast(switchStatus.ToString());
                var signalStatus = new MSGSignalStatus();
                if (signalStatus.OKtoSend) BroadCast(signalStatus.ToString());

            }

            //client updates itself
            if (Client != null && Server == null && newtime - lastMoveTime >= 1f)
            {
                Train t = Simulator.PlayerLocomotive.Train;
                MSGMove move = new MSGMove();
                MSGExhaust exhaust = new MSGExhaust(); // Also updating loco exhaust
                //if I am still controlling the train
                if (t.TrainType != Train.TRAINTYPE.REMOTE)
                {
                    if (Math.Abs(t.SpeedMpS) > 0.001 || newtime - begineZeroTime < 5f || Math.Abs(t.LastReportedSpeed) > 0)
                    {
                        move.AddNewItem(GetUserName(), t);
                        for (int iCar = 0; iCar < t.Cars.Count; iCar++)
                        {
                            if (t.Cars[iCar] is MSTSDieselLocomotive)
                            {
                                exhaust.AddNewItem(GetUserName(), t, iCar);
                            }
                        }
                    }
                    else
                    {
                        if (Math.Abs(previousSpeed) > 0.001)
                        {
                            begineZeroTime = newtime;
                        }
                    }

                }
                //MoveUncoupledTrains(move); //if there are uncoupled trains
                //if there are messages to send
                if (move.OKtoSend())
                {
                    Client.Send(move.ToString());
                    if (exhaust.OKtoSend()) Client.Send(exhaust.ToString());
                    lastMoveTime = lastSendTime = newtime;
                }
                previousSpeed = t.SpeedMpS;

#if INDIVIDUAL_CONTROL

                if (Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE)
                {
                    Client.Send((new MSGLocoInfo(Simulator.PlayerLocomotive, GetUserName())).ToString());
                }
#endif
            }


            //need to send a keep-alive message if have not sent one to the server for the last 30 seconds
            if (Client != null && Server == null && newtime - lastSendTime >= 30f)
            {
                Notify((new MSGAlive(GetUserName())).ToString());
                lastSendTime = newtime;
            }

            //some players are removed
            //need to send a keep-alive message if have not sent one to the server for the last 30 seconds
            if (IsServer() && newtime - lastSyncTime >= 60f)
            {
                Notify((new MSGMessage("All", "TimeCheck", Simulator.ClockTime.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToString());
                lastSyncTime = newtime;
            }
            RemovePlayer();

            //some players are disconnected more than 1 minute ago, will not care if they come back later
            CleanLostPlayers();

            //some trains are added/removed
            HandleTrainList();

            //some locos are added/removed
            if (IsServer()) HandleLocoList();

            AddPlayer(); //a new player joined? handle it

            /* will have this in the future so that helpers can also control
            //I am a helper, will see if I need to update throttle and dynamic brake
            if (Simulator.PlayerLocomotive.Train != null && Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE) 
            {

            }
             * */
        }


        void CheckPlayerTrainSpad()
        {
            if (CheckSpad == false) return;
            var Locomotive = (MSTSLocomotive)Simulator.PlayerLocomotive;
            if (Locomotive == null) return;
            var train = Locomotive.Train;
            if (train == null || train.TrainType == Train.TRAINTYPE.REMOTE) return;//no train or is remotely controlled

            //var spad = false;
            var maxSpeed = Math.Abs(train.AllowedMaxSpeedMpS) + 3;//allow some margin of error (about 10km/h)
            var speed = Math.Abs(Locomotive.SpeedMpS);
            //if (speed > maxSpeed) spad = true;
            //if (train.TMaspect == ORTS.Popups.TrackMonitorSignalAspect.Stop && Math.Abs(train.distanceToSignal) < 2*speed && speed > 5) spad = true; //red light and cannot stop within 2 seconds, if the speed is large

        }
        //check if it is in the server mode
        public static bool IsServer()
        {
            if (Server != null) return true;
            else return false;
        }

        //check if it is in the client mode
        public static bool IsClient()
        {
            if (!MPManager.IsMultiPlayer() || MPManager.IsServer()) return false;
            return true;
        }
        //check if it is in the server mode && they are players && not allow autoswitch
        public static bool NoAutoSwitch()
        {
            if (!MPManager.IsMultiPlayer() || MPManager.IsServer()) return false;
            //if (MPManager.IsClient()) return true;
            return !MPManager.AllowedManualSwitch; //aloow manual switch or not
        }
        //user name
        static public string GetUserName()
        {
            if (Server != null) return Server.UserName;
            else if (Client != null) return Client.UserName;
            else return "";
        }

        //check if it is in the multiplayer session
        static public bool IsMultiPlayer()
        {
            if (Server != null || Client != null) return true;
            else return false;
        }

        static public void BroadCast(string m)
        {
            if (m == null) return;
            if (Server != null) Server.BroadCast(m);
        }

        //notify others (server will broadcast, client will send msg to server)
        static public void Notify(string m)
        {
            if (m == null) return;
            if (Client != null && Server == null) Client.Send(m); //client notify server
            if (Server != null) Server.BroadCast(m); //server notify everybody else
        }

        static public void SendToServer(string m)
        {
            if (m != null && Client != null) Client.Send(m);
        }

        //nicely shutdown listening threads, and notify the server/other player
        static public void Stop()
        {
            if (Client != null && Server == null)
            {
                Client.Send((new MSGQuit(GetUserName())).ToString()); //client notify server
                Thread.Sleep(1000);
                Client.Stop();
            }
            if (Server != null)
            {
                Server.BroadCast((new MSGQuit("ServerHasToQuit\t" + GetUserName())).ToString()); //server notify everybody else
                Thread.Sleep(1000);
                if (Server.ServerComm != null) Server.Stop();
                if (Client != null) Client.Stop();
            }
        }

        //when two player trains connected, require decouple at speed 0.
        public static bool TrainOK2Decouple(Confirmer confirmer, Train t)
        {
            if (t == null) return false;
            if (Math.Abs(t.SpeedMpS) < 0.001) return true;
            try
            {
                var count = 0;
                foreach (var p in OnlineTrains.Players.Keys)
                {
                    string p1 = p + " ";
                    foreach (var car in t.Cars)
                    {
                        if (car.CarID.Contains(p1)) count++;
                    }
                }
                if (count >= 2)
                {
                    if (confirmer != null)
                        confirmer.Information(MPManager.Catalog.GetPluralStringFmt("Cannot decouple: train has {0} player, need to completely stop.", "Cannot decouple: train has {0} players, need to completely stop.", count));
                    return false;
                }
            }
            catch { return false; }
            return true;
        }

        public bool PlayerAdded = false;

        public void AddPlayer()
        {
            if (!MPManager.IsServer()) return;
            if (PlayerAdded == true)
            {
                PlayerAdded = false;
                MPManager.Instance().lastPlayerAddedTime = Simulator.GameTime;
                MPManager.Instance().lastSwitchTime = Simulator.GameTime;

                MSGPlayer host = new MSGPlayer(MPManager.GetUserName(), "1234", Simulator.conFileName, Simulator.patFileName, Simulator.PlayerLocomotive.Train,
                    Simulator.PlayerLocomotive.Train.Number, Simulator.Settings.AvatarURL);
                MPManager.BroadCast(host.ToString() + MPManager.OnlineTrains.AddAllPlayerTrain());
                foreach (Train t in Simulator.Trains)
                {
                    if (Simulator.PlayerLocomotive != null && t == Simulator.PlayerLocomotive.Train) continue; //avoid broadcast player train
                    if (MPManager.FindPlayerTrain(t)) continue;
                    if (removedTrains.Contains(t)) continue;//this train is going to be removed, should avoid it.
                    MPManager.BroadCast((new MSGTrain(t, t.Number)).ToString());
                }
                if (CheckSpad == false) { MultiPlayer.MPManager.BroadCast((new MultiPlayer.MSGMessage("All", "OverSpeedOK", "OK to go overspeed and pass stop light")).ToString()); }
                else { MultiPlayer.MPManager.BroadCast((new MultiPlayer.MSGMessage("All", "NoOverSpeed", "Penalty for overspeed and passing stop light")).ToString()); }
                MPManager.BroadCast(GetEnvInfo());
            }
        }

        //create weather message
        public string GetEnvInfo()
        {
            return (new MSGWeather(-1, overcastFactor, pricipitationIntensity, fogDistance)).ToString();//update weather

        }

        //set weather message
        public void SetEnvInfo(float o, float f)
        {
            fogDistance = f;
            overcastFactor = o;

        }

        //this will be used in the server, in Simulator.cs
        public static bool TrainOK2Couple(Simulator simulator, Train t1, Train t2)
        {
            //if (Math.Abs(t1.SpeedMpS) > 10 || Math.Abs(t2.SpeedMpS) > 10) return false; //we do not like high speed punch in MP, will mess up a lot.

            if (t1.TrainType != Train.TRAINTYPE.REMOTE && t2.TrainType != Train.TRAINTYPE.REMOTE) return true;

            bool result = true;
            try
            {
                foreach (var p in OnlineTrains.Players)
                {
                    if (p.Value.Train == t1 && simulator.GameTime - p.Value.CreatedTime < 120) { result = false; break; }
                    if (p.Value.Train == t2 && simulator.GameTime - p.Value.CreatedTime < 120) { result = false; break; }
                }
            }
            catch (Exception)
            {
            }
            return result;
        }
        /// <summary>
        /// Return a string of information of how many players online and those users who are close
        /// </summary>

        SortedList<double, string> users;

        public string GetOnlineUsersInfo()
        {

            string info = "";
            if (Simulator.PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.REMOTE) info = "Your locomotive is a helper\t";
            info += ("" + (OnlineTrains.Players.Count + 1) + (OnlineTrains.Players.Count <= 0 ? " player " : "  players "));
            info += ("" + Simulator.Trains.Count + (Simulator.Trains.Count <= 1 ? " train" : "  trains"));
            TrainCar mine = Simulator.PlayerLocomotive;
            users.Clear();
            try//the list of players may be changed during the following process
            {
                //foreach (var train in Simulator.Trains) info += "\t" + train.Number + " " + train.Cars.Count;
                //info += "\t" + MPManager.OnlineTrains.Players.Count;
                //foreach (var p in MPManager.OnlineTrains.Players) info += "\t" + p.Value.Train.Number + " " + p.Key;
                foreach (OnlinePlayer p in OnlineTrains.Players.Values)
                {
                    if (p.Train == null) continue;
                    if (p.Train.Cars.Count <= 0) continue;
                    var d = WorldLocation.GetDistanceSquared(p.Train.RearTDBTraveller.WorldLocation, mine.Train.RearTDBTraveller.WorldLocation);
                    users.Add(Math.Sqrt(d) + MPManager.Random.NextDouble(), p.Username);
                }
            }
            catch (Exception)
            {
            }
            if (metric == "")
            {
                metric = Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric == true ? " m" : " yd";
                metricbase = Simulator.TRK.Tr_RouteFile.MilepostUnitsMetric == true ? 1.0f : 1.0936133f;
            }

            int count = 0;
            foreach (var pair in users)
            {
                if (count >= 10) break;
                info += "\t" + pair.Value + ": distance of " + (int)(pair.Key / metricbase) + metric;
                count++;
            }
            if (count < OnlineTrains.Players.Count) { info += "\t ..."; }
            return info;
        }

        private List<OnlinePlayer> playersRemoved;
        public void AddRemovedPlayer(OnlinePlayer p)
        {
            lock (playersRemoved)
            {
                if (playersRemoved.Contains(p)) return;
                playersRemoved.Add(p);
            }
        }

        private void CleanLostPlayers()
        {
            //check if any of the lost player list has been lost for more than 600 seconds. If so, remove it and will not worry about it anymore
            if (lostPlayer.Count > 0)
            {
                List<string> removeLost = null;
                foreach (var x in lostPlayer)
                {
                    if (Simulator.GameTime - x.Value.quitTime > 600) //within 10 minutes it will be held
                    {
                        if (removeLost == null) removeLost = new List<string>();
                        removeLost.Add(x.Key);
                    }
                }
                if (removeLost != null)
                {
                    foreach (var name in removeLost)
                    {
                        lostPlayer.Remove(name);
                    }
                }
            }
        }
        //only can be called by Update
        private void RemovePlayer()
        {
            //if (Server == null) return; //client will do it by decoding message
            if (playersRemoved.Count == 0) return;

            try //do it with lock, but may still have exception
            {
                lock (playersRemoved)
                {
                    foreach (OnlinePlayer p in playersRemoved)
                    {
                        if (Server != null) Server.Players.Remove(p);
                        //player is not in this train
                        if (p.Train != null && p.Train != Simulator.PlayerLocomotive.Train)
                        {
                            //make sure this train has no other player on it
                            bool hasOtherPlayer = false;
                            foreach (var p1 in OnlineTrains.Players)
                            {
                                if (p == p1.Value) continue;
                                if (p1.Value.Train == p.Train) { hasOtherPlayer = true; break; }//other player has the same train
                            }
                            if (hasOtherPlayer == false)
                            {
                                AddOrRemoveLocomotives(p.Username, p.Train, false);
                                if (p.Train.Cars.Count > 0)
                                {
                                    foreach (TrainCar car in p.Train.Cars)
                                    {
                                        car.Train = null; // WorldPosition.XNAMatrix.M42 -= 1000;
                                        car.IsPartOfActiveTrain = false;  // to stop sounds
                                                                          // remove containers if any
                                        if (car.FreightAnimations?.Animations != null)
                                            car.FreightAnimations?.HideDiscreteFreightAnimations();
                                    }
                                }
                                p.Train.RemoveFromTrack();
                                Simulator.Trains.Remove(p.Train);
                            }
                        }
                        OnlineTrains.Players.Remove(p.Username);
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e + e.StackTrace); return;
            }
            playersRemoved.Clear();

        }

        public bool AddOrRemoveTrain(Train t, bool add)
        {
            if (add)
            {
                lock (addedTrains)
                {
                    foreach (var t1 in addedTrains)
                    {
                        if (t1.Number == t.Number) return false;
                    }
                    addedTrains.Add(t); return true;
                }
            }
            else
            {
                lock (removedTrains)
                {
                    removedTrains.Add(t); return true;
                }
            }
        }

        public bool AddOrRemoveLocomotive(string userName, int tNumber, int trainCarPosition, bool add)
        {
            if (add)
            {
                lock (addedLocomotives)
                {
                    foreach (var l1 in addedLocomotives)
                    {
                        if (l1.trainNumber == tNumber && l1.trainCarPosition == trainCarPosition) return false;
                    }
                    OnlineLocomotive newLoco;
                    newLoco.userName = userName;
                    newLoco.trainNumber = tNumber;
                    newLoco.trainCarPosition = trainCarPosition;
                    addedLocomotives.Add(newLoco);
                    return true;
                }
            }
            else
            {
                lock (removedLocomotives)
                {
                    OnlineLocomotive removeLoco;
                    removeLoco.userName = userName;
                    removeLoco.trainNumber = tNumber;
                    removeLoco.trainCarPosition = trainCarPosition;
                    removedLocomotives.Add(removeLoco);
                    return true;
                }
            }
        }

        public bool AddOrRemoveLocomotives(string userName, Train t, bool add)
        {
            for (int iCar = 0; iCar < t.Cars.Count; iCar++)
            {
                if (t.Cars[iCar] is MSTSLocomotive)
                {
                    AddOrRemoveLocomotive(userName, t.Number, iCar, add);
                }

            }
            return true;
        }

        //only can be called by Update
        private void HandleTrainList()
        {
            if (addedTrains.Count != 0)
            {

                try //do it without lock, so may have exception
                {
                    foreach (var t in addedTrains)
                    {
                        var hasIt = false;
                        foreach (var t1 in Simulator.Trains)
                        {
                            if (t1.Number == t.Number) { hasIt = true; break; }
                        }
                        if (!hasIt) Simulator.Trains.Add(t);
                    }
                    addedTrains.Clear();
                }
                catch (Exception) { }
            }
            if (removedTrains.Count != 0)
            {

                try //do it without lock, so may have exception
                {
                    foreach (var t in removedTrains)
                    {
                        t.RemoveFromTrack();
                        Simulator.Trains.Remove(t);
                    }
                    removedTrains.Clear();
                }
                catch (Exception) { }
            }
        }

        //only can be called by Update
        private void HandleLocoList()
        {
            if (removedLocomotives.Count != 0)
            {

                try //do it without lock, so may have exception
                {
                    foreach (var l in removedLocomotives)
                    {
                        for (int index = 0; index < OnlineTrains.OnlineLocomotives.Count; index++)
                        {
                            var thisOnlineLocomotive = OnlineTrains.OnlineLocomotives[index];
                            if (l.trainNumber == thisOnlineLocomotive.trainNumber && l.trainCarPosition == thisOnlineLocomotive.trainCarPosition)
                            {
                                OnlineTrains.OnlineLocomotives.RemoveAt(index);
                                break;
                            }
                        }
                    }
                    removedLocomotives.Clear();
                }
                catch (Exception) { }
            }
            if (addedLocomotives.Count != 0)
            {

                try //do it without lock, so may have exception
                {
                    foreach (var l in addedLocomotives)
                    {
                        var hasIt = false;
                        foreach (var l1 in OnlineTrains.OnlineLocomotives)
                        {
                            if (l1.trainNumber == l.trainNumber && l1.trainCarPosition == l.trainCarPosition) { hasIt = true; break; }
                        }
                        if (!hasIt) OnlineTrains.OnlineLocomotives.Add(l);
                    }
                    addedLocomotives.Clear();
                }
                catch (Exception) { }
            }
        }

        public static Train FindPlayerTrain(string user)
        {
            return OnlineTrains.findTrain(user);
        }

        public static bool FindPlayerTrain(Train t)
        {
            return OnlineTrains.findTrain(t);
        }

        public static void LocoChange(Train t, TrainCar lead)
        {
            var frontOrRearCab = (lead as MSTSLocomotive).UsingRearCab ? "R" : "F";
            Notify((new MSGLocoChange(GetUserName(), lead.CarID, frontOrRearCab, t)).ToString());
        }

        public TrainCar SubCar(Train train, string wagonFilePath, int length)
        {
            Console.WriteLine("Will substitute with your existing stocks\n.");

            try
            {
                char type = 'w';
                if (wagonFilePath.ToLower().Contains(".eng")) type = 'e';
                string newWagonFilePath = SubMissingCar(length, type);

                TrainCar car = RollingStock.Load(Simulator, train, newWagonFilePath);
                car.CarLengthM = length;
                car.RealWagFilePath = wagonFilePath;

                Simulator.Confirmer?.Information(MPManager.Catalog.GetString("Missing car, have substituted with other one."));

                return car;
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message + "Substitution failed, will ignore it\n.");
                return null;
            }
        }

        SortedList<double, string> coachList;
        SortedList<double, string> engList;

        public string SubMissingCar(int length, char type)
        {
            type = char.ToLower(type);
            SortedList<double, string> copyList;
            if (type == 'w')
            {
                if (coachList == null)
                    coachList = GetList(Simulator, type);
                copyList = coachList;
            }
            else
            {
                if (engList == null)
                    engList = GetList(Simulator, type);
                copyList = engList;
            }
            string bestName = "Default\\default.wag"; double bestDist = 1000;

            foreach (var item in copyList)
            {
                var dist = Math.Abs(item.Key - length);
                if (dist < bestDist) { bestDist = dist; bestName = item.Value; }
            }
            return Simulator.BasePath + "\\trains\\trainset\\" + bestName;
        }

        static SortedList<double, string> GetList(Simulator simulator, char type)
        {
            string ending = "*.eng";
            if (type == 'w') ending = "*.wag";
            string[] filePaths = Directory.GetFiles(simulator.BasePath + "\\trains\\trainset", ending, SearchOption.AllDirectories);
            string temp;
            List<string> allEngines = new List<string>();
            SortedList<double, string> carList = new SortedList<double, string>();
            for (var i = 0; i < filePaths.Length; i++)
            {
                int index = filePaths[i].LastIndexOf("\\trains\\trainset\\");
                temp = filePaths[i].Substring(index + 17);
                if (!temp.Contains("\\")) continue;
                allEngines.Add(temp);
            }
            foreach (string name in allEngines)
            {
                double len = 0.0f;
                Microsoft.Xna.Framework.Vector3 def = new Microsoft.Xna.Framework.Vector3();

                try
                {
                    using (var stf = new STFReader(simulator.BasePath + "\\trains\\trainset\\" + name, false))
                        stf.ParseFile(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("wagon", ()=>{
                                stf.ReadString();
                                stf.ParseBlock(new STFReader.TokenProcessor[] {
                                    new STFReader.TokenProcessor("size", ()=>{ def = stf.ReadVector3Block(STFReader.UNITS.Distance, def); }),
                                });
                            }),
                        });

                    len = def.Z;
                    carList.Add(len + MPManager.Random.NextDouble() / 10.0f, name);
                }
                catch { }
            }
            return carList;
        }

        //md5 check of TDB file
        public void GetMD5HashFromTDBFile()
        {
            try
            {
                string fileName = Simulator.RoutePath + @"\" + Simulator.TRK.Tr_RouteFile.FileName + ".tdb";
                FileStream file = new FileStream(fileName, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                MD5Check = Encoding.Unicode.GetString(retVal, 0, retVal.Length);
            }
            catch (Exception e)
            {
                Trace.TraceWarning("{0} Cannot get MD5 check of TDB file, use NA instead but server may not connect you.", e.Message);
                MD5Check = "NA";
            }
        }

        internal void OnServerChanged(bool weAreTheServer)
        {
            var e = new ServerChangedEventArgs(weAreTheServer);
            var serverChanged = ServerChanged;
            if (serverChanged != null)
                serverChanged(this, e);
        }

        internal void OnMessageReceived(double time, string message)
        {
            var e = new MessageReceivedEventArgs(time, message);
            var messageReceived = MessageReceived;
            if (messageReceived != null)
                messageReceived(this, e);
        }

        internal void OnAvatarUpdated(string user, string url)
        {
            var e = new AvatarUpdatedEventArgs(user, url);
            var avatarUpdated = AvatarUpdated;
            if (avatarUpdated != null)
                avatarUpdated(this, e);
        }
    }
}
