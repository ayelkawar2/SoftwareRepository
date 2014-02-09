//////////////////////////////////////////////////////////////////////////
// Manifest.cs   - Represents a manifest file and gives functions to    //
//                  extract information from the manifest               //
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
 * This class provides functions to extract information from a manifest. The information provided is Package name, RI,
 * Filename, checkin status and dependencies. Manifest can be loaded from a file or a XDocument. All information is retrived
 * in the form of string except dependencies which is returned in the form of List of strings.
 * 
 * Public Interface
 * ================
 * Manifest man = new Manifest(string filepath);    //creates manifest from file
 * Manifest man = new Manifest(Xdocument doc);      //creates manifest from Xdocument
 * string pkg = man.getPkgName()            //return package name
 * string file = man.getFileName()      //return file name
 * string ri = man.getRI()          //return RI name
 * string status = man.getStatus()  //return status
 * List<string> deps = man.getDependencies();       //return dependency list
 * 
 * Build Process:
 * ==============
 * Required Files:
 * None
 * 
 * Compiler Command:
 *   csc /target:exe /define:TEST_MANIFEST Manifest.cs
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
using System.Xml.Linq;

namespace RepositoryServer
{
    class Manifest
    {
        XDocument doc;

        //------------Constructor for loading from file---------------------
        public Manifest(string filepath)
        {
            doc = XDocument.Load(filepath);

        }
        //------------Constructor for loading from XDocument--------------------
        public Manifest(XDocument document)
        {
            doc = new XDocument(document);
        }

        //------------Function to get package name---------------------
        public string getPackageName()
        {

            string pkgname = doc.Element("package").Element("name").Value;
            return pkgname;
        }

        //------------Function to get filename---------------------
        public string getFileName()
        {
            string filename = doc.Element("package").Element("filename").Value;
            return filename;
        }

        //------------Function to get RI name---------------------
        public string getRI()
        {
            string RI = doc.Element("package").Element("RI").Value;
            return RI;
        }

        //------------Function to get status---------------------
        public string getStatus()
        {
            string status = doc.Element("package").Element("status").Value;
            return status;
        }

        //------------Function to get dependencies--------------------
        public List<string> getDependencies()
        {
            var dependencies = from s in doc.Element("package").Elements("dependency")
                               select s;
            List<string> deps = new List<string>();
            foreach (var dependency in dependencies)    //add dependencies to list
                deps.Add(dependency.Value);
            return deps;
        }
#if(TEST_MANIFEST)
        public static void Main(string[] args)
        {
            string filepath = arg[0];
            Manifest man = new Manifest(string filepath);    //creates manifest from file
            Xdocument doc = new Xdocument();
            Manifest man = new Manifest(Xdocument doc);      //creates manifest from Xdocument
            string pkg = man.getPkgName()            //return package name
            string file = man.getFileName()      //return file name
            string ri = man.getRI()          //return RI name
            string status = man.getStatus()  //return status
            List<string> deps = man.getDependencies();       //return dependency list
        }
#endif
    }
}
