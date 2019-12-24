// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
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
//
// ===========================================================================================
//      Open Rails Web Server
//      The following files have been modified to accomodate the WebServer
//          Game.cs
//          HUDWindow.cs
//          WebServerProcess.cs
//          search for "WebServer" to find all occurrences
//
//      djr - 20171221
// ===========================================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Orts.Simulation;
using Orts.Simulation.Physics;

namespace Orts.Viewer3D.WebServices
{
    // =================================================================
    // State object for reading client data asynchronously
    // =================================================================
    public class StateObject
    {
        public Socket WorkSocket = null;                    // Client  socket.
        public const int BufferSize = 1024;                 // Size of receive buffer.
        public byte[] Buffer = new byte[BufferSize];        // Receive buffer.
    }

    // ==================================================================
    // 		class for holding HTTP Request data
    // ==================================================================
    public class HttpRequest
    {
        public Socket ClientSocket = null;
        public string Method = "";
        public string URI = "";
        public string Parameters;
        public Dictionary<string, string> headers = new Dictionary<string, string>();
        public Dictionary<string, string> Headers { get => headers; set => headers = value; }
    }

    // ==================================================================
    // 		class for holding HTTP Resonse data
    // ==================================================================
    public class HttpResponse
    {
        public Socket ClientSocket = null;
        public string ResponseCode = "";
        public string ContentType = "";
        public string strContent = "";
        public byte[] byteContent;
    }

    // ====================================================================
    //  TCP/IP Sockets WebServer
    // ====================================================================
    public class WebServer
    {
        private bool Running = false;
        private int timeout = 10;
        public Socket ServerSocket = null;
        private static Encoding CharEncoder = Encoding.UTF8;
        private static string ContentPath = "";
        private IPAddress ipAddress = null;
        private int Port = 0;
        private int MaxConnections = 0;

        // ===========================================================================================
        // 		Thread signal.
        // ===========================================================================================
        private static ManualResetEvent allDone = new ManualResetEvent(false);

        // ===========================================================================================
        // File exstensions this server will handle - any other extensions are returns as not found
        // ===========================================================================================
        private static Dictionary<string, string> extensions = new Dictionary<string, string>()
        {
            { "htm",  "text/html" },
            { "html", "text/html" },
            { "txt",  "text/plain" },
            { "css",  "text/css" },
            { "xml",  "application/xml" },
            { "js",   "application/javascript" },
            { "json", "application/json" },
            { "ico",  "image/x-icon" },
            { "png",  "image/png" },
            { "gif",  "image/gif" },
            { "jpg",  "image/jpg" },
            { "jpeg", "image/jpeg" }
        };

        public Dictionary<string, string> Extensions { get => extensions; set => extensions = value; }

        // ===========================================================================================
        //      Viewer object from Viewer3D - needed for acces to Heads Up Display Data
        // ===========================================================================================
        public Viewer viewer;

        // ===========================================================================================
        //  	WebServer constructor
        // ===========================================================================================
        public WebServer(string ipAddr, int port, int maxConnections, string path)
        {
            ipAddress = IPAddress.Parse(ipAddr);
            Port = port;
            ContentPath = path;
            MaxConnections = maxConnections;
            ApiDict.Add("/API/HUD", ApiHUD);
            ApiDict.Add("/API/APISAMPLE", ApiSample);
            ApiDict.Add("/API/TRACKMONITOR", ApiTrackMonitor);
            ApiDict.Add("/API/TRAININFO", ApiTrainInfo);
            return;
        }

        // ===========================================================================================
        // ===========================================================================================
        public void Run()
        {
            if (Running)
                return;

            // Viewer is not yet initialized in the GameState object - wait until it is
            while ((viewer = Program.Viewer) == null)
                Thread.Sleep(1000);

            try
            {
                ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
                ServerSocket.Listen(MaxConnections);
                ServerSocket.ReceiveTimeout = timeout;
                ServerSocket.SendTimeout = timeout;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Bind Socket: " + e.Message);
                return;
            }
            while (true)
            {
                Running = true;
                // Set the event to nonsignaled state.
                allDone.Reset();

                try
                {
                    // Start an asynchronous socket to listen for connections.
                    ServerSocket.BeginAccept(new AsyncCallback(acceptCallback), ServerSocket);
                }
                catch (Exception e)
                {
                    Console.WriteLine("100 Exception calling BeginAccept: " + e.Message);
                }
                // TODO:
                // Break out of any waiting states
                // Break out of any async states
                // Close down any open sockets !!!!
                if (!Running)
                {
                    break;
                }
                // Wait until a connection is made before continuing.
                //Trace.WriteLine("WebServer is waiting for a connection");
                allDone.WaitOne();
            }
        }

        // ===========================================================================================
        // ===========================================================================================
        public void acceptCallback(IAsyncResult ar)
        {
            // wjc if we stopped the thread just leave
            if (Running)
            {
                // Signal the main thread to continue.
                allDone.Set();
                // Get the socket that handles the client request.
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);
                // Create the state object.
                StateObject state = new StateObject();
                state.WorkSocket = handler;
                handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(receiveCallback),
                    state);
            }
        }

        // ===========================================================================================
        // 		Main processing loop - read request and call  response functions
        // ===========================================================================================
        public static void receiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            StreamReader streamReader;
            HttpRequest request = new HttpRequest();
            HttpResponse response = new HttpResponse();
            request.ClientSocket = state.WorkSocket;
            response.ClientSocket = state.WorkSocket;
            try
            {
                int bytesReceived = request.ClientSocket.EndReceive(ar);
                streamReader = new StreamReader(new MemoryStream(state.Buffer, 0, bytesReceived));
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception instantiate StreamReader: " + e.Message);
                return;
            }
            while (streamReader.Peek() > -1)
            {
                string lineRead = streamReader.ReadLine();
                if (lineRead.Length == 0)
                {
                    if (request.Method.Equals("POST"))
                    {
                        request.Parameters = streamReader.ReadToEnd();
                        ProcessPost(request, response);
                    }
                    else if (request.Method.Equals("GET"))
                    {
                        ProcessGet(request, response);
                    }
                    else
                        sendNotImplemented(response);
                    return;
                }
                else if (request.Method.Equals(""))
                {
                    try
                    {
                        request.Method = lineRead.Substring(0, lineRead.IndexOf(" "));
                        request.Method.Trim();
                        int start = lineRead.IndexOf('/');
                        int length = lineRead.LastIndexOf(" ") - start;
                        request.URI = lineRead.Substring(start, length);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("httpMethod: " + e.Message);
                    }

                    if (!request.Method.Equals("GET") && !request.Method.Equals("POST"))
                    {
                        sendNotImplemented(response);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        int seperator = lineRead.IndexOf(':');
                        string heading = lineRead.Substring(0, seperator);
                        heading = heading.Trim();
                        ++seperator;
                        string value = lineRead.Substring(seperator);
                        value = value.Trim();
                        request.Headers.Add(heading, value);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                if (streamReader.EndOfStream)
                {
                    break;
                }
            }
            sendServerError(response);
        }

        // ===========================================================================================
        // ===========================================================================================
        public void stop()
        {
            if (Running)
            {
                Running = false;
                // TODO:
                // Will Shutdown and close break out of any async waiting states??
                try
                {
                    // wjc we are just streaming for now, so make sure the socket closes at end of game
                    // so that the socket is not hung when opening again
                    //ServerSocket.Shutdown(SocketShutdown.Both);
                    //tcpListener.Stop();
                    ServerSocket.Close();
                    
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.Message);
                    Trace.WriteLine("WebServer", e.Message);
                }
                ServerSocket = null;
            }
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void ProcessPost(HttpRequest request, HttpResponse response)
        {
            request.URI = request.URI.Replace('\\', '/');
            request.URI = request.URI.ToUpper();
            if (!request.URI.StartsWith("/API/"))
            {
                Console.WriteLine("Post Method - API Not Implemented [{0}]", request.URI);
                sendNotImplemented(response);
                return;
            }
            response.strContent = ExecuteApi(request.URI, request.Parameters);
            response.ContentType = "application/json";
            sendOkResponse(response);
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void ProcessGetAPI(HttpRequest request, HttpResponse response)
        {
            sendNotImplemented(response);
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void ProcessGet(HttpRequest request, HttpResponse response)
        {
            request.URI = request.URI.Replace("/", "\\").Replace("\\..", "");
            if (request.URI.StartsWith("/API/"))
            {
                ProcessGetAPI(request, response);
                return;
            }
            int length = request.URI.Length;
            int start = request.URI.LastIndexOf('.');
            if (start == -1)
            {
                if (request.URI.Substring(length - 1, 1) != "\\")
                    request.URI += "\\";
                request.URI += "index.html";
            }
            start = request.URI.LastIndexOf('.');
            length = request.URI.Length - start - 1;
            string extension = request.URI.Substring(start + 1, length);
            if (extensions.ContainsKey(extension))
            {
                if (File.Exists(ContentPath + request.URI))
                {
                    byte[] bytes = File.ReadAllBytes(ContentPath + request.URI);
                    response.byteContent = new byte[bytes.Length];
                    response.byteContent = bytes;
                    response.ContentType = extensions[extension];
                    sendOkResponse(response);
                }
                else
                    sendNotFound(response); // We don't support this extension. We are assuming that it doesn't exist.
            }
            else
                sendNotImplemented(response);
            return;
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void HTMLContent(HttpResponse response)
        {
            response.strContent = "<!doctype HTML>" +
                                  "<html>" +
                                  "<head>" +
                                  "<meta http-equiv=\"Content-Type\" content=\"text/html;" +
                                  "charset=utf-8\">" +
                                  "</head>" +
                                  "<body>" +
                                  "<h1>OpenRails WebServer</h1>" +
                                  "<div>" + response.ResponseCode + "</div>" + "" +
                                  "</body></html>";

            return;
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void sendNotImplemented(HttpResponse response)
        {
            response.ResponseCode = "501 Not Implemented";
            HTMLContent(response);
            SendHttp(response);
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void sendNotFound(HttpResponse response)
        {
            response.ResponseCode = "404 Not Found";
            HTMLContent(response);
            SendHttp(response);
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void sendServerError(HttpResponse response)
        {
            response.ResponseCode = "500 Internal Server Error";
            HTMLContent(response);
            SendHttp(response);
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void sendOkResponse(HttpResponse response)
        {
            response.ResponseCode = "200 OK";
            SendHttp(response);
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void SendHttp(HttpResponse response)
        {
            // Convert the string data to byte data using ASCII encoding.
            if (response.strContent.Length > 0)
            {
                response.byteContent = Encoding.ASCII.GetBytes(response.strContent);
            }
            byte[] byteData = CharEncoder.GetBytes(
                              "HTTP/1.1 " + response.ResponseCode + "\r\n"
                            + "Server: OpenRails WebServer\r\n"
                            + "Content-Length: " + response.byteContent.Length.ToString() + "\r\n"
                            + "Connection: close\r\n"
                            + "Content-Type: " + response.ContentType + "\r\n"
                            + "Cache-Control: no-cache \r\n\r\n"
                            + System.Text.Encoding.UTF8.GetString(response.byteContent));

            // Begin sending the data to the remote device.
            response.ClientSocket.BeginSend(byteData, 0,
                                             byteData.Length, 0,
                                             new AsyncCallback(SendHttpCallback),
                                             response.ClientSocket);
        }

        // ===========================================================================================
        // ===========================================================================================
        private static void SendHttpCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket clientSocket = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = clientSocket.EndSend(ar);

                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Send CallBack: " + e.ToString());
            }
        }

        // ===========================================================================================
        // 		API routing classes & functions
        // ===========================================================================================
        public static Dictionary<string, Func<string, object>> ApiDict = new Dictionary<string, Func<string, object>>();

        public static string ExecuteApi(string apiName, string Parameters)
        {
            Func<string, object> apiMethod;
            if (!ApiDict.TryGetValue(apiName, out apiMethod))
            {
                Console.WriteLine("Not Found"); //TODO
            }
            object result = apiMethod(Parameters);
            string json = JsonConvert.SerializeObject(result, Formatting.Indented);
            return json;
        }

        // =======================================================================================
        // 		API for Sample Data
        // =======================================================================================


        public class Embedded
        {
            public string Str;
            public int Numb;
        }
        public class ApiSampleData
        {
            public int intData;
            public string strData;
            public DateTime dateData;
            public Embedded embedded;
            public string[] strArrayData;
        }

        // -------------------------------------------------------------------------------------------
        public object ApiSample(string Parameters)
        {
            ApiSampleData sampleData = new ApiSampleData();

            sampleData.intData = 576;
            sampleData.strData = "Sample String";
            sampleData.dateData = new DateTime(2018, 1, 1);

            sampleData.embedded = new Embedded();
            sampleData.embedded.Str = "Embeddded String";
            sampleData.embedded.Numb = 123;

            sampleData.strArrayData = new string[5];

            sampleData.strArrayData[0] = "First member";
            sampleData.strArrayData[1] = "Second member";
            sampleData.strArrayData[2] = "Third Member";
            sampleData.strArrayData[3] = "Forth member";
            sampleData.strArrayData[4] = "Fifth member";

            return (sampleData);
        }

        // =======================================================================================
        // 		API to display the HUD Windows
        // ======================================================================================= 
        public class HudApiTable
        {
            public int nRows;
            public int nCols;
            public string[] values;
        }

        // -------------------------------------------------------------------------------------------
        public class HudApiArray
        {
            public int nTables;
            public HudApiTable commonTable;
            public HudApiTable extraTable;
        }


        // -------------------------------------------------------------------------------------------
        public object ApiHUD(string Parameters)
        {
            int index = Parameters.IndexOf('=');
            if (index == -1)
                return (null);
            string strPageno = Parameters.Substring(index + 1, Parameters.Length - index - 1);
            strPageno = strPageno.Trim();
            int pageNo = Int32.Parse(strPageno);

            HudApiArray hudApiArray = new HudApiArray();
            hudApiArray.nTables = 1;

            hudApiArray.commonTable = ApiHUD_ProcessTable(0);
            if (pageNo > 0)
            {
                hudApiArray.nTables = 2;
                hudApiArray.extraTable = ApiHUD_ProcessTable(pageNo);
            }
            return hudApiArray;
        }

        // -------------------------------------------------------------------------------------------
        public HudApiTable ApiHUD_ProcessTable(int pageNo)
        {
            int nRows = 0;
            int nCols = 0;
            int nextCell = 0;

            Viewer3D.Popups.HUDWindow.TableData hudTable = viewer.HUDWindow.PrepareTable(pageNo);

            HudApiTable apiTable = new HudApiTable();

            apiTable.nRows = hudTable.Cells.GetLength(0);
            nRows = apiTable.nRows;
            apiTable.nCols = hudTable.Cells.GetLength(1);
            nCols = apiTable.nCols;
            apiTable.values = new string[nRows * nCols];

            try
            {
                for (int i = 0; i < nRows; ++i)
                {
                    for (int j = 0; j < nCols; ++j)
                    {
                        apiTable.values[nextCell++] = hudTable.Cells[i, j];
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
            return (apiTable);
        }

        // =======================================================================================
        // 		API for Track Monitor Data
        // =======================================================================================

        // -------------------------------------------------------------------------------------------
        public object ApiTrackMonitor(string Parameters)
        {
            Train.TrainInfo trainInfo = viewer.PlayerTrain.GetTrainInfo();

            return (trainInfo);

        }

        // =======================================================================================
        // 		API for Train Info
        // =======================================================================================

        // -------------------------------------------------------------------------------------------
        public object ApiTrainInfo(string Parameters)
        {
            Train.TrainInfo trainInfo = viewer.PlayerTrain.GetTrainInfo();

            return (trainInfo);
        }
    }
}