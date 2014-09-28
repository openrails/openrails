// COPYRIGHT 2014 by the Open Rails project.
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


// This file is intended to show proof of concept to link ORTS itself to ORTS  (IPC is Inter Process Communication)
// The implementation has been chosen to have minimal impact to ORTS, making it easy to install.
//
// To be able to use this file to make a contact between ORTS and ORTS TrackViewer
// 
// Go to Solution Explorer
// Right-click on this file, select 'Exclude from project'
// Moved the file to RunActivity/Process
// Right click on 'Processes' (within RunActivity), select Add, existing item, and select TrackviewerIPC.cs
// Add a reference in RunActivity to System.ServiceModel;
// Add the following line to the end of PhysicsUpdate of train.cs:   ORTS.Processes.TrackviewerIPC.SendTrainStatusToTrackviewer(TrainType, FrontTDBTraveller);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Text.RegularExpressions;

using ORTS.Common;

namespace ORTS.Processes
{
    /// <summary>
    /// Interface definition so both ORTS and Trackviewer both know what to do
    /// </summary>
    [ServiceContract]
    public interface ITrainLocationUpdater
    {
        /// <summary>
        /// Return a string giving the location of the train
        /// </summary>
        [OperationContract]
        string TrainLocation();
    }

    /// <summary>
    /// Actual implementation of the communication routine between ORTS and Trackviewer
    /// </summary>
    class TrainLocationUpdater: ITrainLocationUpdater
    {
        public static Traveller traveller;      

        public string TrainLocation()
        {
            //this is the actual message sent from ORTS to trackviewer
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "FrontTDB:{0}:{1}:{2}:{3}", traveller.TileX, traveller.TileZ, traveller.Location.X, traveller.Location.Z);
        }
    }

    /// <summary>
    /// Class to take care of all comunication between ORTS and trackviewer, at least for now.
    /// </summary>
    public static class TrackviewerIpc {
        //
        // SERVER part
        //
        static ServiceHost host;
      
        /// <summary>
        /// Start the server. Will be called automatically
        /// </summary>
        static void StartServer()
        {
            host = new ServiceHost(
                typeof(TrainLocationUpdater),
                new Uri[]{
                    //new Uri("http://localhost:8000"),
                    new Uri("net.pipe://localhost")
            });

            //host.AddServiceEndpoint(typeof(ITrainLocationUpdater),
            //  new BasicHttpBinding(),
            //  "Reverse");

            host.AddServiceEndpoint(typeof(ITrainLocationUpdater),
              new NetNamedPipeBinding(),
              "PipeTraveller");

            host.Open();

        }

        ///// <summary>
        ///// stop the server. Currently not being called because only proof of concept.
        ///// </summary>
        //static void StopServer()
        //{
        //    host.Close();
        //}

        /// <summary>
        /// This routine needs to be called from ORTS to set the train status.
        /// </summary>
        /// <param name="trainType">Type of train (AI, player) ...</param>
        /// <param name="traveller">The traveller giving the location of the train</param>
        public static void SendTrainStatusToTrackviewer(ORTS.Train.TRAINTYPE trainType, Traveller traveller)
        {
            if (host == null) StartServer();
            if (trainType == ORTS.Train.TRAINTYPE.PLAYER)
            {
                TrainLocationUpdater.traveller = traveller;
            }
        }

        //
        // CLIENT part
        //
        static ITrainLocationUpdater pipeProxy;

        /// <summary>
        /// Start the client. This is being called automatically. It will only work (currently) if the host has started first.
        /// </summary>
        static void StartClient()
        {
            ChannelFactory<ITrainLocationUpdater> pipeFactory =
                new ChannelFactory<ITrainLocationUpdater>(
                    new NetNamedPipeBinding(),
                    new EndpointAddress(
                        "net.pipe://localhost/PipeTraveller"));

            pipeProxy = pipeFactory.CreateChannel();
        }

        /// <summary>
        /// Routine to be called from Trackviewer to determine location of player train
        /// </summary>
        /// <returns>Location of the player train</returns>
        public static WorldLocation PlayerTrainTraveller()
        {
            if (pipeProxy == null) StartClient();
            
            WorldLocation trainLocation;
            Regex findTravellerRegex = new Regex("FrontTDB:(?<tileX>[^:]*):(?<tileZ>[^:]*):(?<x>[^:]*):(?<z>[^:]*)");

            try
            {
                // the regular expression should not be an issue, since we create the string itself above.
                string locationString = pipeProxy.TrainLocation();
                Match match = findTravellerRegex.Match(locationString);
                if (match.Success)
                {
                    trainLocation = new WorldLocation(Convert.ToInt32(match.Groups["tileX"].Value, System.Globalization.CultureInfo.InvariantCulture),
                                                      Convert.ToInt32(match.Groups["tileZ"].Value, System.Globalization.CultureInfo.InvariantCulture),
                                                      Convert.ToSingle(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture), 0,
                                                      Convert.ToSingle(match.Groups["z"].Value, System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    trainLocation = WorldLocation.None;
                }
            }
            catch {
                trainLocation = WorldLocation.None;
            }
            return trainLocation;
        }
    }
}
