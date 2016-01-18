using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BoggleServer;
using BoggleClient;
using CustomNetworking;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace BoggleClientTest
{
    [TestClass]
    public class BoggleClientTest
    {
        public static string received = " ";
        public void messageReceived(string line)
        {
            received = line;
        }

        [TestMethod]
        public void TestMethod1()
        {
            BoggleServer.BoggleServer bServ = new BoggleServer.BoggleServer(60, new HashSet<string>(System.IO.File.ReadAllLines(@"..\..\..\Resources\Dictionary.txt")), "horstoaeaggdpple");
            
            BoggleClientModel model1 = new BoggleClientModel();
            BoggleClientModel model2 = new BoggleClientModel();
            model1.Connect("localhost", 2000);
            model2.Connect("localhost", 2000);

            Assert.IsFalse(model1._client == null);
            Assert.IsFalse(model1._stringSocket == null);

            model1.SendCommand("PLAY Frodo");
            model2.SendCommand("PLAY Sam");

            model2.IncomingLineEvent += messageReceived;

            Assert.AreEqual(" ", BoggleClientTest.received);

            model1.Disconnect();
            model2.Disconnect();

            Assert.IsTrue(model1._stringSocket == null);
            Assert.IsTrue(model2._stringSocket == null);

        }
    }
}
