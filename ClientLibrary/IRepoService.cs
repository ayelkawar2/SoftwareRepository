//////////////////////////////////////////////////////////////////////////
// IRepoService.cs   -  IRepoService contract for client to server      //
//                       communication                                  //
// ver 1.0                                                              //
// Language:    C#, Visual Studio 10.0, .Net Framework 4.0              //
// Platform:    Dell Inspiron N5010, Windows 7 Home Premium SP1         //
// Application: SoftwareRepository, CSE681, Fall 2011                   //
// Author:      Aditya Yelkawar, Syracuse University                    //
//              (703) 618-6101, ayelkawa@syr.edu                        //
//////////////////////////////////////////////////////////////////////////


/*
 * Summary:
 * ==================
 * This package provides signitures for Repository service. It has a service contract which has one operation contract
 * which clients will use to call the repository service function. It also has a data contract which defines what kind
 * of data will we transferred between client and server. It also provides signiture for getmessage function which the 
 * server will use to retrive the message sent by the clients. Client do not have access to this function
 * 
 *  Maintenance History:
 * ====================
 * ver 1.0 : 25 November 2011
 * - first release
 * */



using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;

namespace RepositoryClient
{
    [ServiceContract]
    public interface IRepoService
    {

        [OperationContract(IsOneWay = true)]
        void PostMessage(Message msg);

        // Not a service operation so only server can call

        Message GetMessage();
    }

    [DataContract(Namespace = "RepositoryServer")]
    public class Message
    {
        [DataMember]
        string endpoint = "";
        [DataMember]
        string RI = "Username";
        [DataMember]
        string password = "password";
        [DataMember]
        Command cmd = Command.LogIn;
        [DataMember]
        string body = "default message text";


        public enum Command
        {
            [EnumMember]
            Connect,
            [EnumMember]
            LogIn,
            [EnumMember]
            Logout,
            [EnumMember]
            CheckIn,
            [EnumMember]
            Extraction,
            [EnumMember]
            FileRequest,
            [EnumMember]
            Statusmsg,
            [EnumMember]
            PackageList,
            [EnumMember]
            CancelCheckin

        }

        [DataMember]
        public Command command
        {
            get { return cmd; }
            set { cmd = value; }
        }
        [DataMember]
        public string Endpoint
        {
            get { return endpoint; }
            set { endpoint = value; }
        }
        [DataMember]
        public string RIname
        {
            get { return RI; }
            set { RI = value; }
        }
        [DataMember]
        public string Password
        {
            get { return password; }
            set { password = value; }
        }
        [DataMember]
        public string text
        {
            get { return body; }
            set { body = value; }
        }

    }

}
