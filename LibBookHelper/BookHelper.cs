using System;
using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections.Generic;
using LibData;

namespace BookHelper
{
    // Note: Do not change this class.
    public class Setting
    {
        public int ServerPortNumber { get; set; }
        public int BookHelperPortNumber { get; set; }
        public int UserHelperPortNumber { get; set; }
        public string ServerIPAddress { get; set; }
        public string BookHelperIPAddress { get; set; }
        public string UserHelperIPAddress { get; set; }
        public int ServerListeningQueue { get; set; }
    }

    // Note: Complete the implementation of this class. You can adjust the structure of this class.
    public class SequentialHelper
    {
        public Socket bookSocket;
        public Socket listeningSocket;
        public IPEndPoint localEndPoint;
        public IPEndPoint serverEndPoint;
        public IPAddress localIP;
        public IPAddress serverIP;
        public Setting settings;
        public int Queue;
        public string configFile = @"../ClientServerConfig.json";

        public SequentialHelper()
        {
            try
            {
                string configContent = File.ReadAllText(configFile);
                this.settings = JsonSerializer.Deserialize<Setting>(configContent);
                this.serverIP = IPAddress.Parse(settings.ServerIPAddress);
                this.localIP = IPAddress.Parse(settings.BookHelperIPAddress);
                this.localEndPoint = new IPEndPoint(localIP, settings.BookHelperPortNumber);
                this.serverEndPoint = new IPEndPoint(serverIP, settings.ServerPortNumber);
                this.listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listeningSocket.Bind(localEndPoint);
                this.Queue = settings.ServerListeningQueue;
            } catch (Exception e){
                Console.Out.WriteLine("[Book Helper server Exception] {0}", e.Message);
            }
        }

        public void start()
        {
            //todo: implement the body. Add extra fields and methods to the class if needed
        }
    }
}
