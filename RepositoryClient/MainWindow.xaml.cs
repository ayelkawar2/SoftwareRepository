//////////////////////////////////////////////////////////////////////////
// WPFClient.xaml.cs   - Provides WPF interface for the client          //
//                                                                      //
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
 * This class provides GUI for the client using WPF. It consists of 4 tabs with seperate funcionality being provided in 
 * each tab. First tab is login tab. It provided interface to connect to the server using server IP and port. It also 
 * allows logging in the repository using username and password. The second tab is insertion tab which provides interface
 * to checkin new files from client to repository as open or closed. It allows selection fo dependencies from list of
 * packages already present in repository. Third tab is extraction which allows extraction of packages as components or single
 * file from repository. The last tab is management which allows modification of packages already checked in the repository.
 * It allows changing dependencies of packages, changing status of packages etc.
 * 
 *  Maintenance History:
 * ====================
 * ver 1.0 : 25 November 2011
 * - first release
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Net;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RepositoryClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RepositoryClient.Receiver recvr;    //Receiver and sender for sending messages to server and retriving message
        //from the server
        RepositoryClient.Sender sndr;
        Message rcvdMsg;
        XDocument pkglist;      //for storing package list
        private enum windowtab
        {
            Insertion,
            Extraction,
            Management,
        }
        windowtab listPlace;    //used to determine which tab should be used to show the package list from repository

        bool connected = false;
        string ClientIp = "localhost";
        string ClientPort = "8040";
        string ClientEndpoint = " ";
        string username = "";

        Thread rcvThrd = null;
        delegate void NewMessage(Message msg);  // delegate for functions which are to act on receiving msg from server
        event NewMessage OnNewMessage;


        //------------function that runs in a thread and blocks until message from server is received---------------------
        void ThreadProc()
        {
            while (true)
            {
                // get message out of receive queue - will block if queue is empty
                rcvdMsg = recvr.GetMessage();

                // call window functions on UI thread
                this.Dispatcher.BeginInvoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  OnNewMessage,
                  rcvdMsg);
            }
        }

        //------------Function to process messages from server---------------------
        void OnNewMessageHandler(Message msg)
        {

            switch (msg.command)
            {
                case Message.Command.LogIn:
                    ProcessLogin(msg);
                    break;
                case Message.Command.Statusmsg:
                    ProcessStatusMsg(msg);
                    break;
                case Message.Command.Connect:
                    ProcessConnect();
                    break;
                case Message.Command.PackageList:
                    ProcessPackageList(msg);
                    break;
                default:
                    System.Windows.MessageBox.Show("unknown command");
                    break;
            }
        }

        //------------Function to show package list---------------------
        private void ProcessPackageList(Message msg)
        {
            try
            {
                switch (listPlace)
                {
                    case windowtab.Insertion:   //for insersion tab
                        insertionList(msg);
                        break;
                    case windowtab.Extraction:  //for extraction tab
                        extractionList(msg);
                        break;
                    case windowtab.Management:      //for management tab
                        managementList(msg);
                        break;
                }
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
        }

        //------------Function to show package list in insertion tab---------------------
        private void insertionList(Message msg)
        {
            try
            {
                pkglist = XDocument.Parse(msg.text);
                var pkgnames = from x in pkglist.Element("message").Elements("package") //select packages
                               select x;
                var names = from x in pkgnames.Elements("name") //get package names
                            select x;
                foreach (var pkgname in names)
                    listBox1.Items.Add(pkgname.Value);  //add to list box
            }
            catch (Exception ex) { }
        }

        //------------Function to show package list in extraction tab---------------------
        private void extractionList(Message msg)
        {
            try
            {


                pkglist = XDocument.Parse(msg.text);
                var pkgnames = from x in pkglist.Element("message").Elements("package") //get packages
                               select x;
                var names = from x in pkgnames.Elements("name") //get package names
                            select x;
                foreach (var pkgname in names)
                    listBox4.Items.Add(pkgname.Value);
            }
            catch (Exception ex) { }
        }

        //------------Function to show package list in management tab---------------------
        private void managementList(Message msg)
        {
            try
            {
                listBox7.Items.Clear();
                listBox8.Items.Clear();
                pkglist = XDocument.Parse(msg.text);
                var pkgnames = from x in pkglist.Element("message").Elements("package") //get packages
                               select x;
                var names = from x in pkgnames.Elements("name") //get package names
                            select x;
                foreach (var pkgname in names)
                    listBox7.Items.Add(pkgname.Value);
            }
            catch (Exception ex) { }
        }
        //------------Function to show status messages---------------------
        private void ProcessStatusMsg(Message msg)
        {
            System.Windows.Forms.MessageBox.Show(msg.text);
        }

        //------------Function to process login message from server---------------------
        private void ProcessLogin(Message msg)
        {
            try
            {
                System.Windows.Forms.MessageBox.Show(msg.text);

                tabItem2.IsEnabled = true;
                tabItem3.IsEnabled = true;
                tabItem4.IsEnabled = true;
                button2.IsEnabled = false;
                button3.IsEnabled = true;
                textBox3.IsEnabled = false;
                passwordBox1.IsEnabled = false;
                passwordBox1.Password = "";
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show("Error: " + ex.Message); }
        }

        //------------Function to process connect msg from server---------------------
        private void ProcessConnect()
        {
            try
            {
                LoginGroup.IsEnabled = true;
                textBox3.IsEnabled = true;
                passwordBox1.IsEnabled = true;
                button2.IsEnabled = true;
                button3.IsEnabled = false;
                textBox1.IsEnabled = false;
                textBox2.IsEnabled = false;
                button1.Content = "Disconnect";
                button3.IsEnabled = false;
                connected = true;
                System.Windows.MessageBox.Show("Connected to Server");
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show("Error: " + ex.Message); }
        }


        public MainWindow()
        {
            InitializeComponent();
            OnNewMessage += new NewMessage(OnNewMessageHandler);
            textBox1.Text = "localhost";
            textBox2.Text = "8347";
        }

        //------------Function to disconnect from server---------------------
        private void disconnect()
        {
            try
            {
                sndr.Close();       //close sender and receiver channel and service
                recvr.Close();
                textBox1.IsEnabled = true;
                textBox2.IsEnabled = true;
                button1.Content = "Connect";
                LoginGroup.IsEnabled = false;
                logoutcleanup();

                connected = false;
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show("Error: " + ex.Message); }
        }

        //------------Function to connect to server---------------------
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (connected)  //disconnect if already connected
            {
                disconnect();
                return;
            }
            try
            {
                Random randnum = new Random();
                int portnumber = randnum.Next(5000, 10000); //randomly select port number avoid clash
                ClientIp = getMyip();   //local ip
                ClientPort = portnumber.ToString();
                ClientEndpoint = "http://" + ClientIp + ":" + ClientPort + "/IClientService";
                recvr = new RepositoryClient.Receiver();
                recvr.CreateRecvChannel(ClientEndpoint);    //create service
                rcvThrd = new Thread(new ThreadStart(this.ThreadProc)); //start receiver thread
                rcvThrd.IsBackground = true;
                rcvThrd.Start();
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show("Error: Could not connect to the server\n" + ex.Message); }
            connectToServer();  //create channel to repository service

        }

        //------------Function to create new connection with the server--------------------
        private void connectToServer()
        {
            try
            {
                string endpoint = "http://" + textBox1.Text + ":" + textBox2.Text + "/IRepoService";
                sndr = new Sender(endpoint);
                Message connectMsg = new Message();
                connectMsg.Endpoint = ClientEndpoint;
                connectMsg.command = Message.Command.Connect;
                sndr.PostMessage(connectMsg);           //send connect message and wait for reply

            }
            catch (Exception ex)
            {
                Window temp = new Window();
                temp.Content = ex.Message;
                temp.Height = 100;
                temp.Width = 500;
            }
        }

        //------------Function to close sender and receiver when window is unloaded---------------------
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                sndr.Close();
                recvr.Close();
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show("Error: " + ex.Message); }
        }


        //------------Function to login to the repository---------------------
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Message msg = new Message();
            try
            {
                if (textBox3.Text.Length != 0 && passwordBox1.Password.Length != 0) //check if username and password are not blank
                {
                    username = textBox3.Text;
                    msg.RIname = textBox3.Text;
                    msg.Password = passwordBox1.Password;
                    msg.Endpoint = ClientEndpoint;
                    msg.command = Message.Command.LogIn;
                    sndr.PostMessage(msg);
                }
                else
                    System.Windows.Forms.MessageBox.Show("Please enter Username and Password");
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show("Error: " + ex.Message); }
        }

        //------------Function to get local machine ip---------------------
        private string getMyip()
        {
            IPHostEntry host;
            string localIP = "?";
            try
            {
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily.ToString() == "InterNetwork")
                    {
                        localIP = ip.ToString();
                    }
                }
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show("Error: " + ex.Message); }
            return localIP;
        }

        //------------Function to clean up window on logout---------------------
        private void logoutcleanup()
        {
            try
            {
                username = "";
                textBox3.IsEnabled = true;
                passwordBox1.IsEnabled = true;
                button3.IsEnabled = false;
                button2.IsEnabled = true;
                tabItem2.IsEnabled = false;
                tabItem3.IsEnabled = false;
                tabItem4.IsEnabled = false;
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
        }

        //------------Function to log out ---------------------
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                logoutcleanup();

                Message logoutMsg = new Message();
                logoutMsg.Endpoint = ClientEndpoint;
                logoutMsg.command = Message.Command.Logout;
                sndr.PostMessage(logoutMsg);
                System.Windows.Forms.MessageBox.Show("Logged out successfully");
            }
            catch (Exception ex) { }
        }



        //------------Function to browse files---------------------
        private void button4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Multiselect = false;
                System.Windows.Forms.DialogResult result = openFileDialog1.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string filename = openFileDialog1.FileName;
                    textBox4.Text = filename;
                }
            }
            catch (Exception ex) { }
        }

        //------------Function to browse directories---------------------
        private void button6_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.Multiselect = true;
                System.Windows.Forms.DialogResult result = openFileDialog1.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string[] filenames = openFileDialog1.SafeFileNames;
                    foreach (string filename in filenames)
                        listBox3.Items.Add(filename);
                }
            }
            catch (Exception ex) { }
        }

        //------------Function to send checkin info to the server---------------------
        private void button8_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (textBox4.Text.Length == 0)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a package");
                    return;
                }
                string checkinstatus = "open";
                if (radioButton2.IsChecked == true)
                    checkinstatus = "closed";
                XDocument doc = new XDocument();
                ClientProc processor = new ClientProc();
                string pkgpath = textBox4.Text;         //create metadata to send to server
                processor.createCheckinXML(processor.getPkgname(textBox4.Text), username, checkinstatus,
                    processor.getFileName(textBox4.Text), pkgpath, doc);
                foreach (string item in listBox3.Items) //add dependency info to meta data
                    processor.addDependencies(doc, item);
                Message msg = new Message();
                msg.Endpoint = ClientEndpoint;
                msg.RIname = username;
                msg.command = Message.Command.CheckIn;
                msg.text = doc.ToString();

                sndr.PostMessage(msg);      //send to server
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
        }

        //------------Function to send checking info to server without filename for management only---------------------
        private void button14_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (textBox6.Text.Count() == 0)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a package");
                    return;
                }
                string checkinstatus = "";
                if (radioButton3.IsChecked == true)
                    checkinstatus = "open";
                if (radioButton4.IsChecked == true)
                    checkinstatus = "closed";
                if (radioButton5.IsChecked == true)
                {
                    cancelCheckin(textBox6.Text);
                    return;
                }
                XDocument doc = new XDocument();
                ClientProc processor = new ClientProc();
                string pkgpath = textBox4.Text;         //send -1 as file name and file path to tell server not to download file
                processor.createCheckinXML(textBox6.Text, username, checkinstatus, "-1", "-1", doc);
                foreach (string item in listBox9.Items)
                    processor.addDependencies(doc, item);
                Message msg = new Message();
                msg.Endpoint = ClientEndpoint;
                msg.RIname = username;
                msg.command = Message.Command.CheckIn;
                msg.text = doc.ToString();

                sndr.PostMessage(msg);
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
        }


        //------------Function to cancel checkin---------------------
        private void cancelCheckin(string pkg)
        {
            Message msg = new Message();
            msg.Endpoint = ClientEndpoint;
            msg.RIname = username;
            msg.command = Message.Command.CancelCheckin;    //send cancel checkin command along with package name
            msg.text = pkg;
            sndr.PostMessage(msg);
        }

        //------------Function to show package list---------------------
        private void button9_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBox1.Items.Clear();
                listBox2.Items.Clear();
                listPlace = windowtab.Insertion;
                getPackageList();
            }
            catch (Exception ex) { }
        }

        //------------Function to get package list from server---------------------
        private void getPackageList()
        {
            try
            {
                Message msg = new Message();
                msg.Endpoint = ClientEndpoint;
                msg.RIname = username;
                msg.command = Message.Command.PackageList;
                sndr.PostMessage(msg);
            }
            catch (Exception ex) { }
        }


        //------------Function to show package list in extraction---------------------
        private void button12_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                listBox4.Items.Clear();
                listBox5.Items.Clear();
                listBox6.Items.Clear();
                listPlace = windowtab.Extraction;
                getPackageList();
            }
            catch (Exception ex) { }
        }


        //------------Function to show version number for a package---------------------
        private void listBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                listBox2.Items.Clear();
                string pkg = listBox1.SelectedValue as string;      //get selected package
                var package = from x in pkglist.Element("message").Elements("package")
                              where pkg == x.Element("name").Value
                              select x;

                IEnumerable<XElement> versions = from x in package.Elements("version")  //get all versions
                                                 select x;
                foreach (XElement ver in versions)
                    listBox2.Items.Add(ver.Attribute("number").Value);  //display version numbers
            }
            catch (Exception ex) { }
        }

        //------------Function to add dependency to dependnecy box---------------------
        private void image2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (listBox1.SelectedItem == null)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a package name");
                    return;
                }
                if (listBox2.SelectedItem == null)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a version");
                    return;
                }
                string dependency = listBox1.SelectedValue.ToString() + "." + listBox2.SelectedValue.ToString();
                listBox3.Items.Add(dependency);
            }
            catch (Exception ex) { }
        }

        //------------Function to remove selected dependency---------------------
        private void button7_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox3.SelectedItem == null)
                    return;
                listBox3.Items.Remove(listBox3.SelectedItem);
            }
            catch (Exception ex) { }
        }

        //------------Function to clear dependency listbox---------------------
        private void button5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox3.SelectedItem == null)
                    return;

                listBox3.Items.Clear();
            }
            catch (Exception ex) { }
        }

        //------------Function to show version for a package in extraction tab---------------------
        private void listBox4_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                listBox5.Items.Clear();
                string pkg = listBox4.SelectedValue as string;
                var package = from x in pkglist.Element("message").Elements("package")
                              where pkg == x.Element("name").Value
                              select x;

                IEnumerable<XElement> versions = from x in package.Elements("version")
                                                 select x;
                foreach (XElement ver in versions)
                    listBox5.Items.Add(ver.Attribute("number").Value);
            }
            catch (Exception ex) { }
        }

        //------------Function to show dependencies for a selected version in extraction---------------------
        private void listBox5_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                listBox6.Items.Clear();
                string pkg = listBox4.SelectedValue as string;
                var package = from x in pkglist.Element("message").Elements("package")
                              where pkg == x.Element("name").Value
                              select x;

                string selversion = listBox5.SelectedValue as string;   //get selected version number
                IEnumerable<XElement> version = from x in package.Elements("version")
                                                where selversion == x.Attribute("number").Value
                                                select x;
                IEnumerable<XElement> deps = from x in version.Elements("dependency")   //get dependencies for version
                                             select x;
                foreach (XElement dependency in deps)   //add dependencies to listbox
                    listBox6.Items.Add(dependency.Value);
                string status = "";
                foreach (XElement ver in version)
                    status = ver.Attribute("status").Value; //show status
                label18.Content = status;
            }
            catch (Exception ex) { }
        }

        //------------Function to extract files from server---------------------
        private void button10_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox4.SelectedItem == null)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a package name");
                    return;
                }
                if (listBox5.SelectedItem == null)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a Version");
                    return;
                }
                if (textBox5.Text.Count() == 0)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a Output directory");
                    return;
                }
                string pkgname = listBox4.SelectedValue.ToString() + "." + listBox5.SelectedValue.ToString();
                recvr.savePath = textBox5.Text; //get output path

                Message filemsg = new Message();
                filemsg.Endpoint = ClientEndpoint;
                filemsg.command = Message.Command.Extraction;
                filemsg.text = pkgname;
                sndr.PostMessage(filemsg);
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
        }

        //------------Function to browse directory---------------------
        private void button6_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    string path = dialog.SelectedPath;
                    textBox5.Text = path;
                }
            }
            catch (Exception ex) { }
        }

        //------------Function to extract single file from server--------------------
        private void button11_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox4.SelectedItem == null)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a package name");
                    return;
                }
                if (listBox5.SelectedItem == null)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a Version");
                    return;
                }
                if (textBox5.Text.Count() == 0)
                {
                    System.Windows.Forms.MessageBox.Show("Please select a Output directory");
                    return;
                }
                string pkgname = listBox4.SelectedValue.ToString() + "." + listBox5.SelectedValue.ToString();
                recvr.savePath = textBox5.Text;

                Message filemsg = new Message();
                filemsg.Endpoint = ClientEndpoint;
                filemsg.command = Message.Command.FileRequest;  //command for single file request
                filemsg.text = pkgname;
                sndr.PostMessage(filemsg);
            }
            catch (Exception ex) { System.Windows.Forms.MessageBox.Show(ex.Message); }
        }

        //------------Function to show package list---------------------
        private void button13_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                listBox7.Items.Clear();
                listBox8.Items.Clear();
                listPlace = windowtab.Management;
                getPackageList();
            }
            catch (Exception ex) { }
        }

        //------------Function to get versions---------------------
        private void listBox7_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                listBox8.Items.Clear();
                string pkg = listBox7.SelectedValue as string;
                var package = from x in pkglist.Element("message").Elements("package")  //get package with specifiedname
                              where pkg == x.Element("name").Value
                              select x;

                IEnumerable<XElement> versions = from x in package.Elements("version")  //get versions
                                                 select x;
                foreach (XElement ver in versions)  //add version numbers
                    listBox8.Items.Add(ver.Attribute("number").Value);
            }
            catch (Exception ex) { }
        }

        //------------Function to set status of a version---------------------
        private void listBox8_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {

                string pkg = listBox7.SelectedValue as string;
                var package = from x in pkglist.Element("message").Elements("package")
                              where pkg == x.Element("name").Value
                              select x;

                string selversion = listBox8.SelectedValue as string;
                IEnumerable<XElement> version = from x in package.Elements("version")
                                                where selversion == x.Attribute("number").Value
                                                select x;

                string status = "";
                foreach (XElement ver in version)
                    status = ver.Attribute("status").Value;
                label21.Content = status;
            }
            catch (Exception ex) { }
        }

        //------------Function to add package to dependency box---------------------
        private void image4_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                listBox9.Items.Clear();
                textBox6.Text = listBox7.SelectedValue.ToString();
                string pkg = listBox7.SelectedValue as string;
                var package = from x in pkglist.Element("message").Elements("package")  //get selected package
                              where pkg == x.Element("name").Value
                              select x;

                string selversion = listBox8.SelectedValue as string;   //get selected version
                IEnumerable<XElement> version = from x in package.Elements("version")
                                                where selversion == x.Attribute("number").Value
                                                select x;
                IEnumerable<XElement> deps = from x in version.Elements("dependency")   //get all dependencies
                                             select x;
                foreach (XElement dependency in deps)
                    listBox9.Items.Add(dependency.Value);
            }
            catch (Exception ex) { }
        }

        //------------Function to add package to package textbox---------------------
        private void image3_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (listBox7.SelectedValue == null)
                    System.Windows.Forms.MessageBox.Show("Please select a package");
                if (listBox8.SelectedValue == null)
                    System.Windows.Forms.MessageBox.Show("Please select a version");
                string dependency = listBox7.SelectedValue.ToString() + "." + listBox8.SelectedValue.ToString();
                listBox9.Items.Add(dependency);
            }
            catch (Exception ex) { }
        }

        //------------Function to clear package---------------------
        private void button15_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox9.SelectedItem == null)
                    return;
                listBox9.Items.Remove(listBox9.SelectedItem);
            }
            catch (Exception ex) { }
        }

        //------------Function to clear all packages---------------------
        private void button16_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox9.SelectedItem == null)
                    return;
                listBox9.Items.Clear();
            }
            catch (Exception ex) { }
        }

        //------------Function to clear textbox---------------------
        private void button17_Click(object sender, RoutedEventArgs e)
        {
            textBox6.Text = "";
        }



    }
}
