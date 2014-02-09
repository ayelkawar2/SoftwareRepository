//////////////////////////////////////////////////////////////////////////
// IClientService.cs   -  IClientService contract for server to client  //
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
 * This package provides signitures for Client service. It has a service contract which has 3 operation contracts
 * which server will use to call the client service functions. It also has a Message contract which defines what kind
 * of data will we transferred between client and server when file is being transferred.
 * It also provides signiture for getmessage function which the server will use to retrive the message sent by the server. 
 * Server does not have access to this function
 * 
 *  Maintenance History:
 * ====================
 * ver 1.0 : 25 November 2011
 * - first release
 * */

using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.IO;

namespace RepositoryServer
{
    [ServiceContract(Namespace = "RepositoryClient")]
    public interface IClientService
    {
        [OperationContract(IsOneWay = true)]
        void PostMessage(Message msg);

        [OperationContract(IsOneWay = true)]
        void upLoadFile(FileTransferMessage msg);

        [OperationContract]
        byte[] downLoadFile(string filename);
        // used only locally so not exposed as service method

        Message GetMessage();
    }
    [MessageContract]
    public class FileTransferMessage
    {
        [MessageHeader(MustUnderstand = true)]
        public string filename { get; set; }

        [MessageBodyMember(Order = 1)]
        public Stream transferStream { get; set; }
    }
}
