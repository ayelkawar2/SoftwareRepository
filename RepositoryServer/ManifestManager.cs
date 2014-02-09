////////////////////////////////////////////////////////////////////////////
// ManifestManager.cs   -  Provides functions to create manifest          //
//                         and search through the database for manifests  //
// ver 1.0                                                                //
// Language:    C#, Visual Studio 10.0, .Net Framework 4.0                //
// Platform:    Dell Inspiron N5010, Windows 7 Home Premium SP1           //
// Application: SoftwareRepository, CSE681, Fall 2011                     //
// Author:      Aditya Yelkawar, Syracuse University                      //
//              (703) 618-6101, ayelkawa@syr.edu                          //
////////////////////////////////////////////////////////////////////////////


/*
 * Module Operations:
 * ==================
 * This class provides functions to create new manifest and write it to specified path. It also has functions to
 * get the name of last version manifest of a specified package name. It also has function to check if a manifest
 * exists in the repository regardless of the version
 * 
 * Public Interface
 * ================
 * ManifestManager mn = new ManifestManager(".\\database");    //creates new instance of Manifest manager
 * bool exists = mn.checkIfPackageExists("Display")            //checks if a package exists with any version of that package
 * string pkgname = mn.GetLastPackageName("Display");           //get the last version name for a package
 * manager.createNewManifest("package1", "user", "open", "package1.cs", deps);  //creates a new manifest with specified info
 * 
 * 
 * Build Process:
 * ==============
 * Required Files:
 * None
 * 
 * Compiler Command:
 *   csc /target:exe /define:TEST_MANIFESTMANAGER ManifestManager.cs
 * 
 * Maintenance History:
* ====================
 * ver 1.0 : 25 November 2011
 * - first release
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace RepositoryServer
{
    public class ManifestManager
    {
        string savePath;
        public ManifestManager(string path) //accepts repository database path 
        {
            savePath = path;
        }

        //------------Function to check if a package exists in the repository---------------------
        public bool checkIfPackageExists(string pkgname)
        {
            try
            {
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                    return false;
                }
                if (pkgname.IndexOf(".") != -1)
                    pkgname = pkgname.Substring(0, pkgname.IndexOf("."));   //remove version information
                string searchstring = pkgname + ".*";
                string[] files = Directory.GetFiles(savePath, searchstring);
                if (files.Count() > 0)
                    return true;
            }
            catch (Exception ex) { }
            return false;
        }


        //------------Function to create a new manifest --------------------
        public void createNewManifest(string pkgname, string RI, string status, string filename, List<string> dependencies)
        {
            try
            {
                XDocument doc = new XDocument();
                XElement pkgElem = new XElement("package"); //all elements are added to this package element
                XElement nameElem = new XElement("name", pkgname);
                pkgElem.Add(nameElem);
                XElement riElem = new XElement("RI", RI);
                pkgElem.Add(riElem);
                XElement checkinstatus = new XElement("status", status);
                pkgElem.Add(checkinstatus);
                XElement fileElem = new XElement("filename", filename);
                pkgElem.Add(fileElem);
                doc.Add(pkgElem);
                foreach (string dependency in dependencies) //add dependencies from list of dependencies
                {
                    XElement dep = new XElement("dependency", dependency);
                    doc.Element("package").Add(dep);
                }
                doc.Save(savePath + "\\" + pkgname + ".xml");
            }
            catch (Exception ex) { }
        }

        //------------Function to get package name with last version number---------------------
        public string GetLastPackageName(string pkgname)
        {
            string lastpkg = "";
            try
            {
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                    return "-1";
                }
                if (pkgname.IndexOf(".") != -1)
                    pkgname = pkgname.Substring(0, pkgname.IndexOf(".") + 1);   //remove version info
                string searchstring = pkgname + ".*";
                string[] files = Directory.GetFiles(savePath, searchstring);
                if (files.Count() == 0)
                    return "-1";
                List<string> filelist = new List<string>(); //get all file with matching pattern
                foreach (string file in files)
                    filelist.Add(file);
                filelist.Sort();        //sort all files
                lastpkg = filelist[filelist.Count - 1];  //get the last package with highest version
                lastpkg = lastpkg.Substring(lastpkg.LastIndexOf("\\") + 1);
            }
            catch (Exception ex) { }
            return lastpkg;
        }

#if(TEST_MANIFESTMANAGER)
        public static void Main(string[] args)
        {
            ManifestManager manager = new ManifestManager(".\\database");
            if (manager.checkIfPackageExists("Display"))
                Console.WriteLine("Package Exists");
            string pkgname = manager.GetLastPackageName("Display");
            Console.WriteLine(pkgname);
            List<string> deps = new List<string>();
            deps.Add("abc.1");
            deps.Add("xyz.3");
            manager.createNewManifest("package1", "user", "open", "package1.cs", deps);
        }
#endif
    }
}
