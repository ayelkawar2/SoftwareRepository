/////////////////////////////////////////////////////////////////////////
// ServerProc.cs   -  Provides processing for Repository server        //
// ver 1.0                                                             //
// Language:    C#, Visual Studio 10.0, .Net Framework 4.0             //
// Platform:    Dell Inspiron N5010, Windows 7 Home Premium SP1        //
// Application: SoftwareRepository, CSE681, Fall 2011                  //
// Author:      Aditya Yelkawar, Syracuse University                   //
//              (703) 618-6101, ayelkawa@syr.edu                       //
/////////////////////////////////////////////////////////////////////////

/*
 * Module Operations:
 * ==================
 * This class provides processing for the functionality provided by the repository. It provides functionality for 
 * connecting to the client through a channel created with BasicHTTP binding. It provides functions for sending 
 * messages to the client. It also has functions for downloading and uploading files from client.
 * This class provides functions for checking in files with status open, close or pending (can not be specified by user),
 * extracting files from repository as a component by recursively scanning through dependencies or as a single file
 * It also provides functionality to cancel open checkin in which case it removes the manifest from the repository.
 * Checkin of a package is restricted to 1 user only. If any other user tries to checkin a package with the same name
 * the checkin is rejected. Similarly an open checkin can only be cancelled by the its Responsible Individual (RI).
 * 
 * 
 * Public Interface
 * ================
 * ServerProc serviceproc = new ServerProc();   //Creates new instance of ServerProc
 * serviceproc.Connect(string clientep);        //Connects to service provided at the endpoint clientep
 * serviceproc.sendMessage(Message msg);        //Sends a message to the client service
 * serviceProc.disconnect();                    //Disconnects from the service
 * serviceproc.cancelcheckin(Message msg);      //cancels checkin based on the package name provided in msg
 * serviceproc.ComponentExtraction(Message msg); //Recursively extracts all the files and their dependencies and sends to client
 * serviceproc.filerequest(Message msg);        //Sends the specified package file to the client
 * serviceproc.getPkgList();                    //Sends the list of the packages to the client service
 * serviceproc.doCheckin(Message msg);          //Performs checkin of one package at a time based on the metadata in msg
 * 
 * 
 * Build Process:
 * ==============
 * Required Files:
 *   IRepoService.cs , ManifestManager.cs, VersionManager.cs, IClientService.cs, Manifest.cs
 * Compiler Command:
 *   csc /target:exe /define:TEST_SERVERPROC ServerProc.cs
 * 
 *
 *  Maintenance History:
 * ====================
 * ver 1.0 : 25 November 2011
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.IO;
using System.Xml.Linq;

namespace RepositoryServer
{
    public class ServerProc
    {
        IClientService channel;
        string endpoint = "";

        string SavePath = ".\\Database";    //path for manifest and source files

        public string savePath
        {
            get { return SavePath; }
            set { SavePath = value; }
        }
        public string Endpoint
        {
            get { return endpoint; }
            set { endpoint = value; }
        }

        //------------Creates a BasicHttp channel with the endpoint specifed by url---------------------
        public void CreateBasicHttpChannel(string url)
        {
            try
            {
                BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
                EndpointAddress address = new EndpointAddress(url);
                BasicHttpBinding binding = new BasicHttpBinding(securityMode);
                binding.TransferMode = TransferMode.Streamed;

                //setting the binding quotas to maximum to allow transfer of large files
                binding.MaxReceivedMessageSize = int.MaxValue;
                binding.ReaderQuotas.MaxArrayLength = int.MaxValue;
                binding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
                binding.ReaderQuotas.MaxDepth = int.MaxValue;
                binding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;
                binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;

                channel = ChannelFactory<IClientService>.CreateChannel(binding, address);   //creating channel using channel factory
            }
            catch (Exception ex) { }
        }

        //------------Sends message to the service through channgel---------------------
        public void sendMessage(Message msg)
        {
            try
            {
                channel.PostMessage(msg);
            }
            catch (Exception ex) { }
        }

        //------------Performs checking operations based on metadata recieved in msg---------------------
        public void doCheckin(Message msg)
        {
            try
            {
                XDocument doc = XDocument.Parse(msg.text);

                //retriving information from metadata
                string RIname = doc.Element("package").Element("RI").Value;
                string checkinstatus = doc.Element("package").Element("status").Value;
                string file = doc.Element("package").Element("filename").Value;
                string packagename = doc.Element("package").Element("name").Value;
                string filepath = doc.Element("package").Element("filepath").Value;
                var dependencies = from s in doc.Element("package").Elements("dependency")
                                   select s;
                List<string> deps = new List<string>();
                foreach (var dependency in dependencies)    //generating list of dependencies
                    deps.Add(dependency.Value);

                ManifestManager manager = new ManifestManager(savePath);
                if (manager.checkIfPackageExists(packagename))          //check if package name already exists
                    checkinExitingPkg(packagename, RIname, file, filepath, checkinstatus, deps); //if yes go to checkin for existing pkgs
                else
                    checkinNewPkg(packagename, RIname, file, filepath, checkinstatus, deps); // checkin for new packages
            }
            catch (Exception ex) { }

        }

        //------------Checkin for new packages---------------------
        private void checkinNewPkg(string packagename, string RIname, string file, string filepath, string checkinstatus, List<string> deps)
        {
            try
            {
                ManifestManager manager = new ManifestManager(savePath);
                VersionManager vman = new VersionManager();
                packagename = vman.getNextPkgVersionName(packagename);  //get a version name for the package name, version 1 for new packages
                file = vman.getnextversionname(file);   //get file with versioned name
                manager.createNewManifest(packagename, RIname, "open", file, deps); // create new manifest with open status
                download(filepath, file);   //download file from client
                if (checkinstatus == "closed")
                {
                    CloseCheckin(packagename);  //if checkin specified is close the close checkin
                    return;
                }
                getPkgList();   //send new package list to client
                sendStatusmsg("Package checked in as open");
            }
            catch (Exception ex) { }
        }

        //------------Checkin for already existing packages---------------------------
        private void checkinExitingPkg(string packagename, string RIname, string file, string filepath, string checkinstatus, List<string> deps)
        {
            try
            {
                ManifestManager manager = new ManifestManager(savePath);
                VersionManager vman = new VersionManager();
                string lastpackage = manager.GetLastPackageName(packagename);   //get the name of the last package for the given pkg
                string lastpkgpath = savePath + "\\" + lastpackage;
                Manifest lastmanifest = new Manifest(lastpkgpath);  //open the last package
                if (lastmanifest.getRI() != RIname) //dont allow check in if RI name is different
                {
                    sendStatusmsg("Package already exists with a different username. \nPlease use a different package name");
                    return;
                }
                if (lastmanifest.getStatus() == "open") //if last package is open, overwrite the manifest and file
                {
                    manager.createNewManifest(lastmanifest.getPackageName(), lastmanifest.getRI(), "open", lastmanifest.getFileName(), deps);
                    if (file != "-1")   //download file if path is specified
                        download(filepath, lastmanifest.getFileName());
                    if (checkinstatus == "closed")
                        CloseCheckin(lastmanifest.getPackageName());
                    else
                    {
                        getPkgList();
                        sendStatusmsg("Package checked in as open");
                    }
                }   //create new version if last package is closed or pending
                else if (lastmanifest.getStatus() == "closed" || lastmanifest.getStatus() == "pending")
                {
                    string npkgname = vman.getNextPkgVersionName(lastmanifest.getPackageName());
                    string nfilename = vman.getnextversionname(lastmanifest.getFileName());
                    if (file != "-1")   //download file if path is specified
                    {
                        download(filepath, nfilename);
                        manager.createNewManifest(npkgname, RIname, "open", nfilename, deps);   //write new version of filename
                    }
                    else
                        manager.createNewManifest(npkgname, RIname, "open", lastmanifest.getFileName(), deps);
                    if (checkinstatus == "closed")
                        CloseCheckin(npkgname);
                    else
                        sendStatusmsg("Package checked in as open");
                }
            }
            catch (Exception ex) { }
        }

        //------------Checkin for already existing packages---------------------------
        private void CloseCheckin(string packagename)
        {
            try
            {
                ManifestManager manager = new ManifestManager(savePath);
                string manifestpath = Path.GetFullPath(savePath) + "\\" + packagename + ".xml";
                Manifest closeManifest = new Manifest(manifestpath);    //get the manifest of package to be closed
                List<string> deps = closeManifest.getDependencies();
                if (deps.Count == 0)
                {   //create new manifest with closed status if no dependencies are present
                    manager.createNewManifest(closeManifest.getPackageName(), closeManifest.getRI(), "closed",
                        closeManifest.getFileName(), closeManifest.getDependencies());

                    sendStatusmsg("Package checked in as closed");

                }
                if (deps.Count > 0)     //if dependencies are present, check for pending dependencies
                    CloseCheckinDep(manager, closeManifest, deps, packagename);
                CloseAllPending(packagename);
            }
            catch (Exception ex) { }

        }

        //------------Send a status message to client---------------------------
        private void sendStatusmsg(string msg)
        {
            try
            {
                Message statusmsg = new Message();
                statusmsg.command = Message.Command.Statusmsg;
                statusmsg.text = msg;
                channel.PostMessage(statusmsg);
            }
            catch (Exception ex) { }
        }

        //------------Sends a list of all packages along with version and dependency information---------------------------
        public void getPkgList()
        {
            try
            {
                if (!Directory.Exists(savePath))
                    return;
                string[] filearray = Directory.GetFiles(savePath, "*.xml");
                if (filearray.Count() == 0)
                    return;
                List<string> filelist = new List<string>(filearray);
                VersionManager vman = new VersionManager();
                XDocument doc = new XDocument();
                XElement msgElem = new XElement("message"); //create a root element called message
                while (filelist.Count != 0)
                {
                    string firstpkg = filelist[0];  //get the first package in the list of all packages
                    string pattern = getSearchPattern(firstpkg);
                    string[] oneTypearray = Directory.GetFiles(savePath, pattern);  //search the data base with packagename without version
                    XElement pkgElem = new XElement("package");
                    string pkgname = getPkgNameFromFilepath(oneTypearray[0]);   //the package name without version
                    XElement nameElem = new XElement("name", pkgname);
                    pkgElem.Add(nameElem);  //add name to the package element   
                    foreach (string pkg in oneTypearray)    //go through all the versions of package
                    {
                        filelist.Remove(pkg);   //remove package from main file list 
                        int version = vman.getVersion(pkg);
                        Manifest pkgmanifest = new Manifest(pkg);
                        XElement versionElem = new XElement("version"); //new version element 
                        versionElem.SetAttributeValue("number", version.ToString());
                        versionElem.SetAttributeValue("status", pkgmanifest.getStatus());
                        List<string> deps = pkgmanifest.getDependencies();
                        foreach (string dependency in deps) //add all dependencies to version
                        {
                            XElement depElem = new XElement("dependency", dependency);
                            versionElem.Add(depElem);
                        }
                        pkgElem.Add(versionElem);   //add all versions to package element
                    }
                    msgElem.Add(pkgElem);   //add all packages to message element
                }
                doc.Add(msgElem);   //add message to xml document
                Message listmsg = new Message();
                listmsg.command = Message.Command.PackageList;
                listmsg.text = doc.ToString();
                sendMessage(listmsg);   //send package list to client
            }
            catch (Exception ex) { }
        }

        //------------gets package name from filepath---------------------------
        private string getPkgNameFromFilepath(string filepath)
        {
            string pkgname = "";
            try
            {

                if (filepath.LastIndexOf("\\") != -1)
                    pkgname = filepath.Substring(filepath.LastIndexOf("\\") + 1);
                if (pkgname.IndexOf(".") != -1)
                    pkgname = pkgname.Substring(0, pkgname.IndexOf("."));

            }
            catch (Exception ex) { }
            return pkgname;
        }

        //------------Gets search pattern without path , version and extensionfor searching files---------------------------
        private string getSearchPattern(string name)
        {
            string pattern = "";
            try
            {
                if (name.LastIndexOf("\\") != -1)
                    name = name.Substring(name.LastIndexOf("\\") + 1);
                string pkgname = name.Substring(0, name.IndexOf("."));
                string extenstion = name.Substring(name.LastIndexOf("."));
                pattern = pkgname + ".*" + extenstion;


            }
            catch (Exception ex) { }
            return pattern;
        }

        //------------Close all the packages pending on current package---------------------------
        private void CloseAllPending(string packagename)
        {
            try
            {
                string[] files = Directory.GetFiles(savePath, "*.xml");
                if (files.Count() <= 1) //return if only 1 package is present in database
                    return;
                List<string> allfiles = new List<string>(files);

                foreach (string file in allfiles)
                {
                    string filename = file.Substring(file.LastIndexOf("\\") + 1);
                    ManifestManager manager = new ManifestManager(savePath);
                    Manifest manfile = new Manifest(file);
                    if (manfile.getStatus() == "pending")   //check only pending packages
                    {
                        List<string> deps = manfile.getDependencies();
                        if (deps.Count != 0)
                        {
                            if (deps.Contains(packagename)) //if the package contains package name as dependency, close the package
                            {
                                manager.createNewManifest(manfile.getPackageName(), manfile.getRI(), "closed",
                                    manfile.getFileName(), manfile.getDependencies());
                            }
                        }
                    }
                }
                getPkgList();
            }
            catch (Exception ex) { }
        }

        //------------Close checkin for packages with dependencies by checking the dependencies ---------------------------
        private void CloseCheckinDep(ManifestManager manager, Manifest closeManifest, List<string> deps, string packagename)
        {
            try
            {
                string newStatus = "";
                foreach (string dependnecy in deps)
                {
                    string dependencyPath = savePath + "\\" + dependnecy + ".xml";
                    Manifest depManifest = new Manifest(dependencyPath);    //open the dependency manifest
                    if (depManifest.getStatus() == "closed")
                        continue;
                    if (depManifest.getStatus() == "pending")   //if a dependency is pending check its dependencies
                    {
                        List<string> depsOfdeps = depManifest.getDependencies();
                        if (depsOfdeps.Contains(packagename))   //if its pending on current package, close it
                            manager.createNewManifest(depManifest.getPackageName(), depManifest.getRI(), "closed", depManifest.getFileName(), depManifest.getDependencies());
                        else
                            newStatus = "pending";  //if dependency not pending on current package, change status to pending
                    }
                    if (depManifest.getStatus() == "open")  //if dependency is open, change status to pending
                        newStatus = "pending";
                }
                if (newStatus != "pending") //if status is not pending, close package
                    newStatus = "closed";   //create new manifest with new status
                manager.createNewManifest(closeManifest.getPackageName(), closeManifest.getRI(), newStatus, closeManifest.getFileName(), closeManifest.getDependencies());
                getPkgList();
                sendStatusmsg("Package checked in as " + newStatus);
            }
            catch (Exception ex) { }
        }

        //------------upload file to client---------------------------
        public void uploadFile(string filepath, string filename)
        {
            try
            {
                //read input file as a filestream
                using (var inputStream = new FileStream(filepath, FileMode.Open))
                {
                    FileTransferMessage msg = new FileTransferMessage();
                    msg.filename = filename;
                    msg.transferStream = inputStream;
                    channel.upLoadFile(msg);    //send to client using channel in streaming mode
                }

            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }


        //------------download file from client---------------------------
        public void download(string filepath, string saveas)
        {
            try
            {
                byte[] strm = channel.downLoadFile(filepath);   //get file from client as a byte array
                VersionManager vm = new VersionManager();
                string rfilename = Path.Combine(SavePath, saveas);
                if (!Directory.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);
                File.WriteAllBytes(rfilename, strm);    //save the file to repository

                //Console.Write("\n  Received file \"{0}\"", filepath);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}\n", ex.Message);
            }
        }


        //------------Sends single file to the client---------------------------
        public void filerequest(Message msg)
        {
            try
            {
                string manifestpath = savePath + "\\" + msg.text + ".xml";
                if (File.Exists(manifestpath))
                {
                    Manifest fileman = new Manifest(manifestpath);  //open package manifest
                    string filename = fileman.getFileName();
                    string filepath = savePath + "\\" + filename;
                    filename = removeVersion(filename);
                    uploadFile(filepath, filename); //send file
                    sendStatusmsg("File Received");
                }
            }
            catch (Exception ex) { }
        }


        //------------Recursively extract a package and its dependencies---------------------------
        public void ComponentExtraction(Message msg)
        {
            try
            {
                string manifestpath = savePath + "\\" + msg.text + ".xml";
                if (File.Exists(manifestpath))
                {
                    List<string> todo = new List<string>(); //list of file yet to be sent
                    List<string> sent = new List<string>(); //list of files already sent
                    todo.Add(msg.text); //add the package as the first file to send

                    while (todo.Count != 0)
                    {
                        string pkgname = todo[0];
                        string manpath = savePath + "\\" + pkgname + ".xml";
                        if (!File.Exists(manpath))
                            continue;
                        Manifest fileman = new Manifest(manpath);   //open the manifest
                        string filename = fileman.getFileName();
                        string filepath = savePath + "\\" + filename;
                        filename = removeVersion(filename);
                        uploadFile(filepath, filename);     //send file to client
                        sent.Add(pkgname);  //add to sent list
                        todo.Remove(pkgname);   // remove from to do list
                        List<string> deps = fileman.getDependencies();  //get dependencies
                        foreach (string dependency in deps)  //add dependencies not in sent list to the todo list
                            if (!sent.Contains(dependency))
                                todo.Add(dependency);
                    }
                }
            }
            catch (Exception ex) { }
        }


        //------------remove the version from a filename with version---------------------------
        private string removeVersion(string filename)
        {
            try
            {
                string pkgname = filename.Substring(0, filename.IndexOf("."));
                string extension = filename.Substring(filename.LastIndexOf("."));
                filename = pkgname + extension;

            }
            catch (Exception ex) { }
            return filename;
        }

        //------------Cancel checkin for open packages---------------------------
        public void cancelcheckin(Message msg)
        {
            try
            {
                string pkgname = msg.text;
                ManifestManager manager = new ManifestManager(savePath);
                VersionManager vman = new VersionManager();
                if (!manager.checkIfPackageExists(msg.text))     //check if package exists
                {
                    sendStatusmsg("Package not found");
                    return;
                }
                string lastpkg = manager.GetLastPackageName(msg.text);  //get the last package with sent package name
                string pkgpath = savePath + "\\" + lastpkg;
                int ver = vman.getVersion(lastpkg);
                Manifest pkgman = new Manifest(pkgpath);

                if (pkgman.getStatus() != "open")   //check if the package is open, if not return error
                {
                    sendStatusmsg("Error: Only packages which have been checked in as open can be cancelled");
                    return;
                }
                if (pkgman.getRI() != msg.RIname)   //check if the RI is same, if not return error
                {
                    sendStatusmsg("Error: Only the RI of package can cancel check-in");
                    return;
                }
                string filename = pkgman.getFileName();
                string filepath = savePath + "\\" + filename;
                if (File.Exists(pkgpath))   //remove manifest
                    File.Delete(pkgpath);
                removeFromDependencies(pkgname + "." + ver.ToString());
            }
            catch (Exception ex) { }
        }

        //------------Remove the specified package from dependencies of all packages---------------------------
        private void removeFromDependencies(string pkgname)
        {
            try
            {
                if (!Directory.Exists(savePath))
                    return;
                string[] filearray = Directory.GetFiles(savePath, "*.xml");
                if (filearray.Count() == 0)
                    return;
                List<string> filelist = new List<string>(filearray);    //get all manifest file paths in list
                ManifestManager manager = new ManifestManager(savePath);
                foreach (string file in filelist)
                {
                    Manifest filemanifiest = new Manifest(file);
                    if (filemanifiest.getDependencies().Contains(pkgname))  //check if the package dependencies have the package
                    {
                        List<string> deps = filemanifiest.getDependencies();    //create new manifest if without the package as dependency
                        deps.Remove(pkgname);
                        manager.createNewManifest(filemanifiest.getPackageName(), filemanifiest.getRI(), filemanifiest.getStatus(),
                            filemanifiest.getFileName(), deps);
                    }
                }

                getPkgList();
                sendStatusmsg("Package removed from repository");
            }
            catch (Exception ex) { }
        }

        //------------Fucntion to close channel with the client---------------------------
        public void disconnect()
        {
            try
            {
                ((ICommunicationObject)channel).Close();
            }
            catch (Exception ex) { }
        }

        //------------Connect to client with endpoint url---------------------------
        public void Connect(string url)
        {
            endpoint = url;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    CreateBasicHttpChannel(url);
                    //string msg = "hi";
                    //client.channel.PostMessage("hi");
                    //Console.Write("\n  sending: {0}", msg);
                    //((ICommunicationObject)client.channel).Close();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\nConnection failed\nReason: " + ex.Message);

                }
            }
        }
#if(TEST_SERVERPROC)
        static void Main(string[] args)
        {
            if (args.GetLength(0) < 1)
            {
                Console.Write("\n  Please enter an endpoint for the service \n\n");
                return;
            }
            string clientep = args[0];
            ServerProc serviceproc = new ServerProc();
            serviceproc.Connect(clientep);
            Message msg = new Message();
            msg.command = Message.Command.CheckIn;
            msg.text = "<package><name>package1</name><RI>abc</RI><status>open</status><filename>file1.cs</filename><dependency>package2</dependency>";
            serviceproc.doCheckin(msg);
            msg.command = Message.Command.Extraction;
            msg.text = "package1";
            serviceproc.ComponentExtraction(msg);
            msg.command = Message.Command.FileRequest;
            serviceproc.filerequest(msg);
            msg.command = Message.Command.CancelCheckin;
            serviceproc.cancelcheckin(msg);
            serviceproc.getPkgList();
        }
#endif
    }
}
