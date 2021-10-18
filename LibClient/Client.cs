using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using LibData;


namespace LibClient
{
    // Note: Do not change this class 
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

    // Note: Do not change this class 
    public class Output
    {
        public string Client_id { get; set; } // the id of the client that requests the book
        public string BookName { get; set; } // the name of the book to be reqyested
        public string Status { get; set; } // final status received from the server
        public string BorrowerName { get; set; } // the name of the borrower in case the status is borrowed, otherwise null
        public string BorrowerEmail { get; set; } // the email of the borrower in case the status is borrowed, otherwise null
    }

    // Note: Complete the implementation of this class. You can adjust the structure of this class.
    public class SimpleClient
    {
        // some of the fields are defined. 
        public Output result;
        public Socket clientSocket;
        public IPEndPoint serverEndPoint;
        public IPAddress ipAddress;
        public Setting settings;
        public string client_id;
        private string bookName;
        // all the required settings are provided in this file
        public string configFile = @"../ClientServerConfig.json";
        //public string configFile = @"../../../../ClientServerConfig.json"; // for debugging

        // todo: add extra fields here in case needed 

        /// <summary>
        /// Initializes the client based on the given parameters and seeting file.
        /// </summary>
        /// <param name="id">id of the clients provided by the simulator</param>
        /// <param name="bookName">name of the book to be requested from the server, provided by the simulator</param>
        public SimpleClient(int id, string bookName)
        {

            this.bookName = bookName;
            this.client_id = "Client " + id.ToString();
            this.result = new Output();
            result.BookName = bookName;
            result.Client_id = this.client_id;
            // read JSON directly from a file
            try
            {
                string configContent = File.ReadAllText(configFile);
                this.settings = JsonSerializer.Deserialize<Setting>(configContent);
                this.ipAddress = IPAddress.Parse(settings.ServerIPAddress);
                this.serverEndPoint = new IPEndPoint(ipAddress, settings.ServerPortNumber);
                this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("[Client Exception] {0}", e.Message);
            }
        }

        /// <summary>
        /// Establishes the connection with the server and requests the book according to the specified protocol.
        /// Note: The signature of this method must not change.
        /// </summary>
        /// <returns>The result of the request</returns>
        public Output start()
        {
            Console.WriteLine(client_id + " has started");
            //initialize variables, create connection
            byte[] buffer = new byte[1000];
            byte[] msg = new byte[1000];

            //if this is the first client, create a connection
            if (client_id == "Client 0")
            {
                Console.WriteLine("Connecting to server...");
                clientSocket.Connect(serverEndPoint);
                Console.WriteLine("Connected!");
            }

            //if this is client -1, send endcommunication message and close the socket.
            if (client_id == "Client -1"){
                var endcomm = new Message();
                endcomm.Type = MessageType.EndCommunication;
                endcomm.Content = "";
                msg = messageToBytes(endcomm);
                clientSocket.Send(msg);
                clientSocket.Close();
            }

            //Client starts with hello message
            var hello = new Message();
            hello.Type = MessageType.Hello;
            hello.Content = client_id;
            msg =   messageToBytes(hello);
            clientSocket.Send(msg);

            //Then the client waits until he receives 'welcome' message
            int b = clientSocket.Receive(buffer);
            var welcome = BytesToMessage(buffer);
            Console.WriteLine("Welcome message was received.");
            //if the received message is error this function will return an error.
            if (welcome.Type == MessageType.Error)
            {
                result.Status = welcome.Content;
                result.BorrowerEmail = null;
                result.BorrowerName = null;
                return result;
            }

            //the client will send the BookInquiry message, asking for a book by sending bookname
            Console.WriteLine("Sending book inquiry");
            var bookinquiry = new Message();
            bookinquiry.Type = MessageType.BookInquiry;
            bookinquiry.Content = this.bookName;
            msg = messageToBytes(bookinquiry);
            clientSocket.Send(msg);

            //the client will wait unitl he receives the status of the book
            buffer = new byte[1000];
            b = clientSocket.Receive(buffer);
            var bookinquiryreply = BytesToMessage(buffer);
            if (bookinquiryreply.Type == MessageType.NotFound)
            {
                result.Status = "BookNotFound";
                result.BorrowerEmail = null;
                result.BorrowerName = null;
                return result;
            } else if (bookinquiryreply.Type == MessageType.Error)
            {
                result.Status = bookinquiryreply.Content;
                result.BorrowerEmail = null;
                result.BorrowerName = null;
                return result;
            }
            //change content of the message from jsonstring to an BookData object
            string jsonstring = bookinquiryreply.Content;
            BookData myBook = JsonSerializer.Deserialize<BookData>(jsonstring);
            

            //if the book is available, status will be "Available", borrower information will be null
            if (myBook.Status == "Available")
            {
                result.Status = "Available";
                result.BorrowerEmail = null;
                result.BorrowerName = null;
                return result;
            //if the book is borrowed, the client will request the user information, using the user_id in the status of myBook
            } else 
            {
                var userinquiry = new Message();
                userinquiry.Type = MessageType.UserInquiry;
                userinquiry.Content = myBook.BorrowedBy;
                msg = messageToBytes(userinquiry);
                clientSocket.Send(msg);
                buffer = null;
                b = clientSocket.Receive(buffer);
                var userinquiryreply = BytesToMessage(buffer);
                if (userinquiryreply.Type == MessageType.Error){
                    result.Status = userinquiryreply.Content;
                    result.BorrowerEmail = null;
                    result.BorrowerName = null;
                    return result;
                }
                if (userinquiryreply.Type == MessageType.NotFound){
                    result.Status = "Borrowed";
                    result.BorrowerEmail = null;
                    result.BorrowerName = "NotFound";
                    return result;
                }
                jsonstring = userinquiryreply.Content;
                UserData myUser = JsonSerializer.Deserialize<UserData>(jsonstring);

                //build output
                result.BorrowerName = myUser.Name;
                result.BorrowerEmail = myUser.Email;
                result.Status = "Borrowed";
                return result;
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
                    return "Hello" + "|" + msg.Content;
                case (MessageType.Welcome):
                    return "Welcome" + "|" + msg.Content;
                case (MessageType.BookInquiry):
                    return "BookInquiry" + "|" + msg.Content;
                case (MessageType.UserInquiry):
                    return "UserInquiry" + "|" + msg.Content;
                case (MessageType.BookInquiryReply):
                    return "BookInquiryReply" + "|" + msg.Content;
                case (MessageType.UserInquiryReply):
                    return "UserInquiryReply" + "|" + msg.Content;
                case (MessageType.EndCommunication):
                    return "EndCommunication" + "|" + msg.Content;
                case (MessageType.Error):
                    return "Error" + "|" + msg.Content;
                case (MessageType.NotFound):
                    return "NotFound" + "|" + msg.Content;
                default:
                    return "";
            }


        }

        public Message BytesToMessage(byte[] bytes)
        {
            var msg = new Message();
            string fullstring = Encoding.ASCII.GetString(bytes);
            string[] subs = fullstring.Split("|");
            string content = subs[1];
            string type = subs[0];
            msg.Content = content;
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
