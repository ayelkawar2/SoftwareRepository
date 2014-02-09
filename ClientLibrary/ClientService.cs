//////////////////////////////////////////////////////////////////////////
// ClientService.cs:  Provides client service to the server             //
//                                                                      //
// ver 1.0                                                              //
// Language:    C#, Visual Studio 10.0, .Net Framework 4.0              //
// Platform:    Dell Inspiron N5010, Windows 7 Home Premium SP1         //
// Application: SoftwareRepository, CSE681, Fall 2011                   //
// Author:      Aditya Yelkawar, Syracuse University                    //
//              (703) 618-6101, ayelkawa@syr.edu                        //
// Source:      Jim Fawcett, Syracuse University                        //
//////////////////////////////////////////////////////////////////////////

/*
 * Module Operations:
 * ==================
 * Receiver implements IClientService to provide services to the server. These services include uploading and downloading
 * file as will as sending and receiving messages
 * Sender is used to send messages to the server using a blocking queue. All the messages are first send to the queue and the
 * tranfered to the server using the channel created by the server
 * 
 * Maintenance History:
 * ====================
 * ver 2.2 : 01 Nov 11
 * - Removed unintended local declaration of ServiceHost in Receiver's 
 *   CreateReceiveChannel function
 * ver 2.1 : 10 Oct 11
 * - removed [OperationContract] from GetMessage() so only local client
 *   can dequeue messages
 * - added send thread to keep clients from blocking on slow sends
 * - added retries when creating Communication channel proxy
 * - added comments to clarify what code is doing
 * ver 2.0 : 06 Nov 08
 * - added close functions that close the service and receive channel
 * ver 1.0 : 14 Jul 07
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using SWTools;
using System.Windows;
using System.IO;

namespace RepositoryClient
{
    /////////////////////////////////////////////////////////////
    // Receiver hosts client service used by repository server

    public class Receiver : IClientService
    {
        string ToSendPath = "./";
        static string SavePath = ".\\receivedfiles";
        static BlockingQueue<Message> rcvBlockingQ = null;
        ServiceHost service = null;
        //string filename;
        int BlockSize = 1024;
        byte[] block;

        public string toSendpath
        {
            get { return ToSendPath; }
            set { ToSendPath = value; }
        }

        public string savePath
        {
            get { return SavePath; }
            set { SavePath = value; }
        }

        //------------Constructor---------------------
        public Receiver()
        {
            block = new byte[BlockSize];
            if (rcvBlockingQ == null)
                rcvBlockingQ = new BlockingQueue<Message>();
        }

        //------------Function to send files from server to client---------------------
        public void upLoadFile(FileTransferMessage msg)
        {
            try
            {
                int totalBytes = 0;
                string rfilename = Path.Combine(SavePath, msg.filename);
                if (!Directory.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);
                using (var outputStream = new FileStream(rfilename, FileMode.Create))
                {
                    while (true)
                    {
                        int bytesRead = msg.transferStream.Read(block, 0, BlockSize);
                        totalBytes += bytesRead;
                        if (bytesRead > 0)
                            outputStream.Write(block, 0, bytesRead);
                        else
                            break;
                    }
                }
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
        }

        //------------Function to send files from client to server---------------------
        public byte[] downLoadFile(string sfilename)
        {

            if (File.Exists(sfilename))
            {
                return File.ReadAllBytes(sfilename);

            }
            else
                throw new Exception("open failed for \"" + sfilename + "\"");

        }

        //------------Function to close service--------------------
        public void Close()
        {
            service.Close();
        }

        //  Create ServiceHost for Communication service

        public void CreateRecvChannel(Object address)
        {
            try
            {
                BasicHttpBinding binding = new BasicHttpBinding();
                binding.TransferMode = TransferMode.Streamed;

                binding.MaxReceivedMessageSize = int.MaxValue;  //setting max values for quotas to send large files
                binding.ReaderQuotas.MaxArrayLength = int.MaxValue;
                binding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
                binding.ReaderQuotas.MaxDepth = int.MaxValue;
                binding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;
                binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
                Uri baseAddress = new Uri(address as string);
                service = new ServiceHost(typeof(Receiver), baseAddress);
                service.AddServiceEndpoint(typeof(IClientService), binding, baseAddress);
                service.Open();
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show("Error: " + ex.Message); }
        }

        // Implement service method to receive messages from the server

        public void PostMessage(Message msg)
        {
            rcvBlockingQ.enQ(msg);
        }

        // Implement service method to extract messages from other Peers.
        // This will often block on empty queue, so user should provide
        // read thread.

        public Message GetMessage()
        {
            return rcvBlockingQ.deQ();
        }
#if(TEST_RECEIVER)
        public static void Main(string[] args)
        {
            RepositoryClient.Receiver recvr = new Receiver();
            recvr.CreateRecvChannel(args[0]);
            Message msg  = recvr.GetMessage();

        }
#endif
    }
    ///////////////////////////////////////////////////
    // client for repository server 

    public class Sender
    {
        IRepoService channel;
        string lastError = "";
        BlockingQueue<Message> sndBlockingQ = null;
        Thread sndThrd = null;
        int tryCount = 0, MaxCount = 10;

        // Processing for sndThrd to pull msgs out of sndBlockingQ
        // and post them to another Peer's Communication service

        void ThreadProc()
        {
            while (true)
            {
                try
                {
                    Message msg = sndBlockingQ.deQ();
                    channel.PostMessage(msg);
                    //if (msg == "quit")
                    //    break;
                }
                catch (Exception ex) { System.Windows.Forms.MessageBox.Show("Error: " + ex.Message); }
            }
        }

        // Create Communication channel proxy, sndBlockingQ, and
        // start sndThrd to send messages that client enqueues

        public Sender(string url)
        {
            sndBlockingQ = new BlockingQueue<Message>();
            while (true)
            {
                try
                {
                    CreateSendChannel(url);
                    tryCount = 0;
                    break;
                }
                catch (Exception ex)
                {
                    if (++tryCount < MaxCount)
                        Thread.Sleep(100);
                    else
                    {
                        lastError = ex.Message;
                        break;
                    }
                }
            }
            sndThrd = new Thread(ThreadProc);
            sndThrd.IsBackground = true;
            sndThrd.Start();
        }

        // Create proxy to another Peer's Communicator

        public void CreateSendChannel(string address)
        {
            EndpointAddress baseAddress = new EndpointAddress(address);
            BasicHttpBinding binding = new BasicHttpBinding();
            ChannelFactory<IRepoService> factory
            = new ChannelFactory<IRepoService>(binding, address);
            channel = factory.CreateChannel();
        }

        // Sender posts message to another Peer's queue using
        // Communication service hosted by receipient via sndThrd

        public void PostMessage(Message msg)
        {
            sndBlockingQ.enQ(msg);
        }

        public string GetLastError()
        {
            string temp = lastError;
            lastError = "";
            return temp;
        }

        //------------Function to close channel---------------------
        public void Close()
        {
            ((ICommunicationObject)channel).Close();

        }
#if(TEST_SENDER)
        public static void Main(string[] args)
        {
            Sender sndr = new Sender();
            recvr.CreateSendChannel(args[0]);
            Message msg = new Message();
            msg.text = "text";
            sndr.PostMessage(msg);

        }
#endif
    }

}
