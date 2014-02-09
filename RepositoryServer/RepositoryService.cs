//////////////////////////////////////////////////////////////////////////
// RepositoryService.cs:  Provides repository services to multiple      //
//                        clients                                       //
// ver 1.0                                                              //
// Language:    C#, Visual Studio 10.0, .Net Framework 4.0              //
// Platform:    Dell Inspiron N5010, Windows 7 Home Premium SP1         //
// Application: SoftwareRepository, CSE681, Fall 2011                   //
// Author:      Aditya Yelkawar, Syracuse University                    //
//              (703) 618-6101, ayelkawa@syr.edu                        //
//////////////////////////////////////////////////////////////////////////


/*
 * Module Operations:
 * ==================
 * This class implements IReposervice to provide repository checking and extraction services to multiple clients.
 * It uses a blocking queue to receive messages from clients and processes them. It connects to those clients through the
 * services provided by the clients by creating channels to client with the endpoint information provided in client
 * messages. 
 * This class uses a thread which blocks on the queue until a message from client arrives. All the messages are retrived
 * and processes using ServerProc object which is used to connect to the client.
 * 
 * Public Interface
 * ================
 * RepositoryService service = new RepositoryService();                 //creates new instance of Repository service
 * service.host = new ServiceHost(typeof(RepositoryService), address1)      //creates a new service host of the type RepositoryService
 * service.host.AddServiceEndpoint(typeof(IRepoService), binding1, address1);   //Adds new endpoint
 * service.host.Open();                                                  //Starts the service
 * string serverIP = service.getMyip();                                  //gets the machines local IP address
 * 
 * Build Process:
 * ==============
 * Required Files:
 * BlockingQueue.cs IRepoService.cs ServerProc.cs
 * 
 * Compiler Command:
 *   csc /target:exe RepositoryService.cs
 * 
 * Maintenance History:
* ====================
 * ver 1.0 : 25 November 2011
 * - first release
 * */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using SWTools;
using System.Threading;
using System.ServiceModel.Channels;
using System.Net;
namespace RepositoryServer
{
    class RepositoryService : IRepoService
    {
        static BlockingQueue<Message> BlockingQ = null;
        ServiceHost host = null;
        //List<ServerProc> clientlist;

        //------------Constructor---------------------
        RepositoryService()
        {
            if (BlockingQ == null)
                BlockingQ = new BlockingQueue<Message>();   //create new blocking queue
            //clientlist = new List<ServerProc>();
        }

        //------------Function used by client to post message to blocking queue---------------------
        public void PostMessage(Message msg)
        {

            BlockingQ.enQ(msg);
        }

        //------------Function to get the next message from blocking queue---------------------
        public Message GetMessage()
        {
            return BlockingQ.deQ();
        }

        //------------Function that runs in a differnt thread and blocks on blockingqueue for client msg---------------------
        protected virtual void ThreadProc()
        {
            while (true)
            {
                Message msg = this.GetMessage();
                switch (msg.command)        //Chooses different functions based on command from client
                {
                    case Message.Command.LogIn:
                        Thread loginThrd = new Thread(ProcessLogin);
                        loginThrd.IsBackground = true;
                        loginThrd.Start(msg);
                        break;
                    case Message.Command.FileRequest:
                        ProcessFileRequest(msg);
                        break;
                    case Message.Command.CheckIn:
                        ProcessCheckin(msg);
                        break;
                    case Message.Command.PackageList:
                        ProcessPackageList(msg);
                        break;
                    case Message.Command.Extraction:
                        ProcessExtraction(msg);
                        break;
                    case Message.Command.CancelCheckin:
                        ProcessCancelCheckin(msg);
                        break;
                    case Message.Command.Connect:
                        Thread conThrd = new Thread(ProcessConnect);
                        conThrd.IsBackground = true;
                        conThrd.Start(msg);
                        break;
                }
            }
        }

        //------------Function to cancel a open checkin---------------------
        private void ProcessCancelCheckin(Message msg)
        {
            try
            {
                ServerProc serviceproc = new ServerProc();
                string clientep = msg.Endpoint;
                serviceproc.Connect(clientep);
                serviceproc.cancelcheckin(msg);
                serviceproc.disconnect();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //------------Function to recursively extract packages from repository---------------------
        private void ProcessExtraction(Message msg)
        {
            try
            {
                ServerProc serviceproc = new ServerProc();
                string clientep = msg.Endpoint;
                serviceproc.Connect(clientep);
                serviceproc.ComponentExtraction(msg);
                serviceproc.disconnect();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //------------Function to get a single file from repository---------------------
        private void ProcessFileRequest(Message msg)
        {
            try
            {
                ServerProc serviceproc = new ServerProc();
                string clientep = msg.Endpoint;
                serviceproc.Connect(clientep);
                serviceproc.filerequest(msg);
                serviceproc.disconnect();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //------------Function to get the list of all packages---------------------
        private void ProcessPackageList(Message msg)
        {
            try
            {
                ServerProc serviceproc = new ServerProc();
                string clientep = msg.Endpoint;
                serviceproc.Connect(clientep);
                serviceproc.getPkgList();
                serviceproc.disconnect();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //------------Function to checkin a new package or modify an existing one---------------------
        private void ProcessCheckin(Object msg)
        {
            try
            {
                Message clientmsg = msg as Message;
                ServerProc serviceproc = new ServerProc();
                string clientep = clientmsg.Endpoint;
                serviceproc.Connect(clientep);
                serviceproc.doCheckin(clientmsg);
                serviceproc.disconnect();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //------------Function to log in to repository---------------------
        private void ProcessLogin(Object msg)
        {
            try
            {
                Message clientmsg = msg as Message;
                string clientep = clientmsg.Endpoint;
                clientmsg.text = "Login successful";
                ServerProc serviceProc = new ServerProc();
                serviceProc.Connect(clientep);
                serviceProc.sendMessage(clientmsg);
                //serviceProc.uploadFile("./", "abc.txt");
                serviceProc.disconnect();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //------------Function to tell client that server is available---------------------
        private void ProcessConnect(Object msg)
        {
            try
            {
                Message clientmsg = msg as Message;
                string clientep = clientmsg.Endpoint;
                //clientlist.Add(serviceProc);
                Message connectMsg = new Message();
                connectMsg.command = clientmsg.command;

                ServerProc serviceProc = new ServerProc();
                serviceProc.Connect(clientep);
                serviceProc.sendMessage(connectMsg);
                serviceProc.disconnect();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //------------Function to get local machine IP---------------------
        public string getMyip()
        {
            IPHostEntry host;
            string localIP = "?";
            try
            {
                host = Dns.GetHostEntry(Dns.GetHostName()); //get host entry
                foreach (IPAddress ip in host.AddressList)  //check ip for each address in host
                {
                    if (ip.AddressFamily.ToString() == "InterNetwork")  //save local ip
                    {
                        localIP = ip.ToString();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            return localIP;
        }


        public static void Main(string[] args)
        {
            try
            {
                RepositoryService service = new RepositoryService();    //create new service
                BasicHttpBinding binding1 = new BasicHttpBinding();     //create binding with streaming enabled
                binding1.TransferMode = TransferMode.Streamed;
                binding1.MaxReceivedMessageSize = 50000000;     //increase max message size
                string serverIP = service.getMyip();
                string serverPort = "8347";
                string serverEndpoint = "http://" + serverIP + ":" + serverPort + "/IRepoService";
                Uri address1 = new Uri(serverEndpoint);
                using (service.host = new ServiceHost(typeof(RepositoryService), address1)) //create new host with specified binding
                {
                    service.host.AddServiceEndpoint(typeof(IRepoService), binding1, address1);   //add new service endpoint for clients
                    service.host.Open();
                    Console.Write("\n  Server started....");
                    Thread child = new Thread(new ThreadStart(service.ThreadProc));  //start a new listening thread
                    child.Start();
                    child.Join();
                }

            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}\n\n", ex.Message);
            }
        }
    }
}
