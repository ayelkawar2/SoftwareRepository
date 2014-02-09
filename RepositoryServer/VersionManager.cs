//////////////////////////////////////////////////////////////////////////
// VersionManager.cs   -  Provides functions to manipulate version      //
//                         information in package and filenames         //
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
 * This class provides functions to manipulate version information in a given file or packagename. It provides 
 * functions to get next version of package or file. It also has functions to get and add version to a string
 * 
 * Public Interface
 * ================
 * VersionManager vm = new VersionManager();    //creates new instance of version manager
 * string a = vm.getnextversionname("abc");     //creates new string with next version as 1
 * a = vm.getnextversionname("abc.1.txt");      //creates new string with next version
 * int ver = vm.getVersion("abc.1.txt");        //gets the version number as int
 * string versionedname = vm.addVersion("abc.txt",1);   //adds version info to filename
 * 
 * Build Process:
 * ==============
 * Required Files:
 * None
 * 
 * Compiler Command:
 *   csc /target:exe /define:TEST_VERSIONMANAGER VersionManager.cs
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

namespace RepositoryServer
{
    class VersionManager
    {
        //------------Function to get next version for a package string---------------------
        public string getNextPkgVersionName(string name)
        {
            string pkgname = "";
            try
            {
                if (name.IndexOf(".") == -1)    //check if string doesnt have an extension
                {
                    name += ".1";
                    return name;
                }

                pkgname = name.Substring(0, name.IndexOf(".")); //get package name
                string version = name.Substring(name.IndexOf(".") + 1, 1);  //get version
                int versionnum = Convert.ToInt32(version);
                versionnum++;
                version = versionnum.ToString();
                pkgname += "." + version;
            }
            catch (Exception ex) { }
            return pkgname;

        }

        //------------Function to get next version for a filename---------------------
        public string getnextversionname(string name)
        {
            string pkgname = "";
            try
            {
                if (name.IndexOf(".") == -1)    //check if now extension is present
                {
                    name += ".1";
                    return name;
                }
                if (name.IndexOf(".") == name.LastIndexOf("."))     //check if no version info is present, set version to 1
                {
                    name = name.Insert(name.LastIndexOf("."), ".1");
                    return name;
                }
                pkgname = name.Substring(0, name.IndexOf("."));
                string extenstion = name.Substring(name.LastIndexOf("."));
                string version = name.Substring(name.IndexOf(".") + 1, 1);
                int versionnum = Convert.ToInt32(version);
                versionnum++;
                version = versionnum.ToString();
                pkgname += "." + version + extenstion;
            }
            catch (Exception ex) { }
            return pkgname;
        }

        //------------Function to get version of a filename---------------------
        public int getVersion(string name)
        {
            int versionnum = 0;
            try
            {
                if (name.LastIndexOf("\\") != -1)
                    name = name.Substring(name.LastIndexOf("\\") + 1);  //remove filepath if present
                if (name.IndexOf(".") == -1)    //return 0 if no version information
                    return 0;
                if (name.IndexOf(".") == name.LastIndexOf("."))
                    return 0;
                string version = name.Substring(name.IndexOf(".") + 1, 1);
                versionnum = Convert.ToInt32(version);
            }
            catch (Exception ex) { }
            return versionnum;
        }

        //------------Function to add new version info to a filename---------------------
        public string addVersion(string name, int version)
        {
            try
            {
                if (name.IndexOf(".") == -1)    //if no . found, add the version info at the end
                {
                    name += ".1";
                    return name;
                }
                if (name.IndexOf(".") == name.LastIndexOf(".")) //if no version present, add the new version
                {
                    name = name.Insert(name.LastIndexOf("."), version.ToString());
                    return name;
                }
            }
            catch (Exception ex) { }
            return name;
        }
#if(TEST_VERSIONMANAGER)
        public static void Main(string[] args)
        {
            VersionManager vm = new VersionManager();
            string a = vm.getnextversionname("abc");
            Console.WriteLine(a);
            a = vm.getnextversionname("abc.1.txt");
            Console.WriteLine(a);
            int ver = vm.getVersion("abc.1.txt");
            Console.WriteLine(a);
            string versionedname = vm.addVersion("abc.txt",1);
            Console.WriteLine(versionedname);
        }
#endif
    }
}
