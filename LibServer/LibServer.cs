using System;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using LibData;


namespace LibServer
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
    public class SequentialServer
    {
        public Socket bookSocket;
        public Socket userSocket;
        public Socket listeningSocket;
        public Socket serverSocket;
        public IPEndPoint bookEndPoint;
        public IPEndPoint userEndPoint;
        public IPEndPoint localEndPoint;
        public IPAddress bookIP;
        public IPAddress userIP;
        public IPAddress localIP;
        public Setting settings;
        public int Queue;
        public string configFile = @"../ClientServerConfig.json";

        public SequentialServer()
        {
            try
            {
                string configContent = File.ReadAllText(configFile);
                this.settings = JsonSerializer.Deserialize<Setting>(configContent);
                this.bookIP = IPAddress.Parse(settings.BookHelperIPAddress);
                this.userIP = IPAddress.Parse(settings.UserHelperIPAddress);
                this.localIP = IPAddress.Parse(settings.ServerIPAddress);
                this.bookEndPoint = new IPEndPoint(bookIP, settings.BookHelperPortNumber);
                this.userEndPoint = new IPEndPoint(userIP, settings.UserHelperPortNumber);
                this.localEndPoint = new IPEndPoint(localIP, settings.ServerPortNumber);
                this.bookSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.userSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listeningSocket.Bind(localEndPoint);
                this.Queue = settings.ServerListeningQueue;
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Server Exception] {0}", e.Message);
            }
        }

        public void start()
        {
            byte[] buffer = new byte[1000];
            byte[] msg = new byte[1000];
            int b;

            Console.WriteLine("Connecting to Book helper server...");
            bookSocket.Connect(bookEndPoint);
            Console.WriteLine("Connected!");
            Console.WriteLine("Connecting to User helper server...");
            userSocket.Connect(userEndPoint);
            Console.WriteLine("Connected!");


            while (true)
            {
                buffer = new byte[1000];
                msg = null;

                listeningSocket.Listen(Queue);
                Console.WriteLine("\nConnecting to client...");
                serverSocket = listeningSocket.Accept();
                Console.WriteLine("Connected!");

                b = serverSocket.Receive(buffer);
                var hello = BytesToMessage(buffer);

                if (hello.Type == MessageType.EndCommunication)
                {
                    var endcomm = new Message();
                    endcomm.Type = MessageType.EndCommunication;
                    endcomm.Content = "";
                    msg = messageToBytes(endcomm);
                    Console.WriteLine("Closing sockets...");
                    serverSocket.Close();
                    listeningSocket.Close();
                    bookSocket.Send(msg);
                    bookSocket.Close();
                    userSocket.Send(msg);
                    userSocket.Close();
                    Console.WriteLine("Closed! Now quitting.");
                    break;
                }
                if (hello.Type != MessageType.Hello)
                {
                    var error = new Message();
                    error.Type = MessageType.Error;
                    error.Content = "Error: didnt receive hello message";
                    msg = messageToBytes(error);
                    serverSocket.Send(msg);
                    Console.WriteLine("Closing Connection...");
                    serverSocket.Close();
                    continue;
                }
                Console.WriteLine(hello.Content + " says hello. Sending back 'Welcome'.");
                //send back welcome
                var welcome = new Message();
                welcome.Type = MessageType.Welcome;
                welcome.Content = "";
                msg = messageToBytes(welcome);
                serverSocket.Send(msg);


                //get book inquiry and send it to the book helper server
                buffer = new byte[1000];
                b = serverSocket.Receive(buffer);
                Console.WriteLine("Book inquiry received, asking book from book helper");
                bookSocket.Send(buffer);
                //Send the message received from the book server to the client
                buffer = new byte[1000];
                b = bookSocket.Receive(buffer);
                Console.WriteLine("Reply from book helper received, sending it to the client");
                serverSocket.Send(buffer);
                Message bookinquiryreply = BytesToMessage(buffer);

                //stop and go to the next loop if the book is not found or the book is available. otherwise wait for the userinquiry
                if (bookinquiryreply.Type == MessageType.NotFound || bookinquiryreply.Type == MessageType.Error)
                {
                    Console.WriteLine("Book was not found/an error was found");
                    Console.WriteLine("Closing Connection...");
                    serverSocket.Close();
                    continue;
                }

                string a = bookinquiryreply.Content;
                Console.WriteLine(a);
                Console.WriteLine(a.Length);
                BookData myBook = JsonSerializer.Deserialize<BookData>(bookinquiryreply.Content);
                if (myBook.Status == "Available")
                {
                    Console.WriteLine("Book is available! No need to ask for an user.");
                    Console.WriteLine("Closing Connection...");
                    serverSocket.Close();
                    continue;
                }
                //Receive user inquiry and send to user helper
                b = serverSocket.Receive(buffer);
                userSocket.Send(buffer);

                //Receive user inquiry reply and send to client
                b = userSocket.Receive(buffer);
                serverSocket.Send(buffer);

                //Close socket connection with client
                Console.WriteLine("Closing Connection...");
                serverSocket.Close();
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
                    return "Hello" + "|" + msg.Content + ";";
                case (MessageType.Welcome):
                    return "Welcome" + "|" + msg.Content + ";";
                case (MessageType.BookInquiry):
                    return "BookInquiry" + "|" + msg.Content + ";";
                case (MessageType.UserInquiry):
                    return "UserInquiry" + "|" + msg.Content + ";";
                case (MessageType.BookInquiryReply):
                    return "BookInquiryReply" + "|" + msg.Content + ";";
                case (MessageType.UserInquiryReply):
                    return "UserInquiryReply" + "|" + msg.Content + ";";
                case (MessageType.EndCommunication):
                    return "EndCommunication" + "|" + msg.Content + ";";
                case (MessageType.Error):
                    return "Error" + "|" + msg.Content + ";";
                case (MessageType.NotFound):
                    return "NotFound" + "|" + msg.Content + ";";
                default:
                    return "";
            }
        }

        public Message BytesToMessage(byte[] bytes)
        {
            var msg = new Message();
            string fullstring = Encoding.ASCII.GetString(bytes);
            fullstring = fullstring.Substring(0, fullstring.IndexOf(";"));
            string[] subs = fullstring.Split("|");
            string type = subs[0];
            string content = "";
            if (subs.Length != 1)
            {
                content = subs[1];
            }
            msg.Content = content;
            switch (type)
            {
                case ("Hello"):
                    msg.Type = MessageType.Hello; break;
                case ("Welcome"):
                    msg.Type = MessageType.Welcome; break;
                case ("BookInquiry"):
                    msg.Type = MessageType.BookInquiry; break;
                case ("UserInquiry"):
                    msg.Type = MessageType.UserInquiry; break;
                case ("BookInquiryReply"):
                    msg.Type = MessageType.BookInquiryReply; break;
                case ("UserInquiryReply"):
                    msg.Type = MessageType.UserInquiryReply; break;
                case ("EndCommunication"):
                    msg.Type = MessageType.EndCommunication; break;
                case ("Error"):
                    msg.Type = MessageType.Error; break;
                case ("NotFound"):
                    msg.Type = MessageType.NotFound; break;
            }
            return msg;
        }
    }
}