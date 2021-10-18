﻿using System;
using System.Text;
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
        public string bookinfo;
        public int Queue;
        public string configFile = @"../ClientServerConfig.json";
        public string bookFile = @"./Books.json";
        public List<BookData> books;

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

                string bookContent = File.ReadAllText(bookFile);
                this.books = JsonSerializer.Deserialize<List<BookData>>(bookContent);

            } catch (Exception e){
                Console.Out.WriteLine("[Book Helper server Exception] {0}", e.Message);
            }
        }

        public void start()
        {
            Console.WriteLine("Connecting to server...");
            listeningSocket.Listen(Queue);
            bookSocket = listeningSocket.Accept();
            Console.WriteLine("Connected!");

            while (true)
            {
                var msg = new byte[1000];
                var buffer = new byte[1000];
                Console.WriteLine("Waiting for messages from the client...");
                bookSocket.Receive(buffer);
                var received = BytesToMessage(buffer);
                if(received.Type == MessageType.EndCommunication){
                    Console.WriteLine("Closing the socket...");
                    bookSocket.Close();
                    break;
                } else if(received.Type == MessageType.BookInquiry){
                    Console.WriteLine("Book Inquiry received!");

                }
            }
        }

//helper functions
        public byte[] messageToBytes(Message msg)
        {
            return Encoding.ASCII.GetBytes(messageToString(msg));
        }

        public string messageToString(Message msg)
        {
            switch (msg.Type)
            {
                case (MessageType.Hello):
                    return "Hello" + "," + msg.Content;
                case (MessageType.Welcome):
                    return "Welcome" + "," + msg.Content;
                case (MessageType.BookInquiry):
                    return "BookInquiry" + "," + msg.Content;
                case (MessageType.UserInquiry):
                    return "UserInquiry" + "," + msg.Content;
                case (MessageType.BookInquiryReply):
                    return "BookInquiryReply" + "," + msg.Content;
                case (MessageType.UserInquiryReply):
                    return "UserInquiryReply" + "," + msg.Content;
                case (MessageType.EndCommunication):
                    return "EndCommunication" + "," + msg.Content;
                case (MessageType.Error):
                    return "Error" + "," + msg.Content;
                case (MessageType.NotFound):
                    return "NotFound" + "," + msg.Content;
                default:
                    return "";
            }


        }

        public Message BytesToMessage(byte[] bytes)
        {
            var msg = new Message();
            string fullstring = Encoding.ASCII.GetString(bytes);
            string[] subs = fullstring.Split(",");
            string type = subs[0];
            string content = subs[1];
            switch (type)
            {
                case ("Hello"):
                    msg.Type = MessageType.Hello; break;
                case("Welcome"):
                    msg.Type = MessageType.Welcome; break;
                case("BookInquiry"):
                    msg.Type = MessageType.BookInquiry; break;
                case("UserInquiry"):
                    msg.Type = MessageType.UserInquiry; break;
                case("BookInquiryReply"):
                    msg.Type = MessageType.BookInquiryReply; break;
                case("UserInquiryReply"):
                    msg.Type = MessageType.UserInquiryReply; break;
                case("EndCommunication"):
                    msg.Type = MessageType.EndCommunication; break;
                case("Error"):
                    msg.Type = MessageType.Error; break;
                case("NotFound"):
                    msg.Type = MessageType.NotFound; break;
            }
            return msg;
        }
    }
}
