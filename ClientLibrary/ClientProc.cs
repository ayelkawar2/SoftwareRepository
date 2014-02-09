/////////////////////////////////////////////////////////////////////////
// ClientProc.cs   -  Provides processing for Client GUI               //
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
 * This class provides processing for client GUI. It has functions to create new XML metadata for checkin, and provides
 * some string operations like getting name and package name from filepath. It has function to add dependencies to the
 * metatdata created.
 * 
 * 
 * Public Interface
 * ================
 * ClientProc cp = new ClientProc()     //creates new instance of clientproc
 * XDocument doc = new XDocument();
 * cp.createCheckinXML("pkg","ri","open","filename","filepath", doc)    //creates new xml metadata
 * cp.addDependencies(doc,List<string> dep)  //adds new dependencies
 * string filename = cp.getFileName(string path);   //gets file name from path
 * string pkgname = cp.getPkgName(string path);         //get pkg name from path
 * 
 * Build Process:
 * ==============
 * Required Files:
 *   None
 * Compiler Command:
 *   csc /target:exe /define:TEST_CLIENTPROC ClientProc.cs
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
using System.Xml.Linq;

namespace RepositoryClient
{
    public class ClientProc
    {
        //------------Function to create new xml metadata file for checkin---------------------
        public void createCheckinXML(string pkgname, string RI, string status, string filename, string filepath, XDocument doc)
        {
            XElement pkgElem = new XElement("package");
            XElement nameElem = new XElement("name", pkgname);
            pkgElem.Add(nameElem);
            XElement riElem = new XElement("RI", RI);
            pkgElem.Add(riElem);
            XElement checkinstatus = new XElement("status", status);
            pkgElem.Add(checkinstatus);
            XElement file = new XElement("filename", filename);
            pkgElem.Add(file);
            XElement path = new XElement("filepath", filepath);
            pkgElem.Add(path);
            doc.Add(pkgElem);
        }

        //------------Function to add dependencies to metadata file--------------------
        public void addDependencies(XDocument doc, string dependency)
        {
            XElement dep = new XElement("dependency", dependency);
            doc.Element("package").Add(dep);
        }

        //------------Function to get file name from path---------------------
        public string getFileName(string fullpath)
        {
            int slashindex = fullpath.LastIndexOf('\\');
            if (slashindex == -1)
                return fullpath;
            string filename = fullpath.Substring(slashindex + 1);
            return filename;
        }

        //------------Function to get package name from path---------------------
        public string getPkgname(string fullpath)
        {
            int slashindex = fullpath.LastIndexOf('\\');
            string filename = "";
            if (slashindex != -1)
                filename = fullpath.Substring(slashindex + 1);
            if (filename.LastIndexOf(".") == -1)
                return filename;
            string pkgname = filename.Substring(0, filename.LastIndexOf("."));
            return pkgname;

        }
#if(TEST_CLIENTPROC)
        public static void Main(string[] args)
        {
            ClientProc cp = new ClientProc()     //creates new instance of clientproc
            XDocument doc = new XDocument();
            cp.createCheckinXML("pkg","ri","open","filename","filepath", doc)    //creates new xml metadata
            cp.addDependencies(doc,List<string> dep)  //adds new dependencies
            string filename = cp.getFileName(string path);   //gets file name from path
            string pkgname = cp.getPkgName(string path);         //get pkg name from path
        }
#endif
    }
}
