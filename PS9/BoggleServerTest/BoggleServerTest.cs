using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BoggleServer;
using System.Collections.Generic;
using CustomNetworking;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BoggleServerTest
{
    [TestClass]
    public class BoggleServerTest
    {
        [TestMethod]
        public void TestBasicWord()
        {
            TcpListener server = null;
            TcpClient client1 = null;
            TcpClient client2 = null;

            server = new TcpListener(IPAddress.Any, 4042);
            server.Start();
            client1 = new TcpClient("localhost", 4042);
            client2 = new TcpClient("localhost", 4042);

            Socket serverSocket = server.AcceptSocket();

            Socket client1Socket = client1.Client;
            Socket client2Socket = client2.Client;

            StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            StringSocket player1Socket = new StringSocket(client1Socket, new UTF8Encoding());
            StringSocket player2Socket = new StringSocket(client2Socket, new UTF8Encoding());

            BoggleServer.BoggleServer.Player player1 = new BoggleServer.BoggleServer.Player("ryan", player1Socket);
            BoggleServer.BoggleServer.Player player2 = new BoggleServer.BoggleServer.Player("ryan2", player2Socket);

            BoggleServer.BoggleServer.Match match = new BoggleServer.BoggleServer.Match(
                player1, player2, 60, new HashSet<string>(System.IO.File.ReadAllLines(@"..\..\..\Resources\Dictionary.txt")), "horstoaeaggdpple");

            match.ReceivedPlayer1Data("word horse", null, player1Socket);
            Assert.AreEqual(2, player1.Score);
            Assert.IsTrue(player1.UniqueLegalWords.Contains("HORSE"));
        }

        [TestMethod]
        public void TestOtherPlayerPlayingWord()
        {
            TcpListener server = null;
            TcpClient client1 = null;
            TcpClient client2 = null;

            server = new TcpListener(IPAddress.Any, 4043);
            server.Start();
            client1 = new TcpClient("localhost", 4043);
            client2 = new TcpClient("localhost", 4043);

            Socket serverSocket = server.AcceptSocket();

            Socket client1Socket = client1.Client;
            Socket client2Socket = client2.Client;

            StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            StringSocket player1Socket = new StringSocket(client1Socket, new UTF8Encoding());
            StringSocket player2Socket = new StringSocket(client2Socket, new UTF8Encoding());

            BoggleServer.BoggleServer.Player player1 = new BoggleServer.BoggleServer.Player("ryan", player1Socket);
            BoggleServer.BoggleServer.Player player2 = new BoggleServer.BoggleServer.Player("ryan2", player2Socket);

            BoggleServer.BoggleServer.Match match = new BoggleServer.BoggleServer.Match(
                player1, player2, 60, new HashSet<string>(System.IO.File.ReadAllLines(@"..\..\..\Resources\Dictionary.txt")), "horstoaeaggdpple");

            match.ReceivedPlayer1Data("word horse", null, player1Socket);
            match.ReceivedPlayer1Data("word apple", null, player1Socket);
            match.ReceivedPlayer2Data("word horse", null, player2Socket);

            Assert.AreEqual(2, player1.Score);
            Assert.AreEqual(0, player2.Score);
            Assert.IsTrue(player1.UniqueLegalWords.Contains("APPLE"));
            Assert.IsFalse(player1.UniqueLegalWords.Contains("HORSE"));
            Assert.IsTrue(match.LegalWordsInCommon.Contains("HORSE"));

            match.ReceivedPlayer2Data("word toggle", null, player2Socket);
            match.ReceivedPlayer2Data("word rage", null, player2Socket);
            match.ReceivedPlayer1Data("word toggle", null, player1Socket);

            Assert.AreEqual(1, player2.Score);
            Assert.AreEqual(2, player1.Score);
            Assert.IsTrue(player2.UniqueLegalWords.Contains("RAGE"));
            Assert.IsFalse(player2.UniqueLegalWords.Contains("TOGGLE"));
            Assert.IsTrue(match.LegalWordsInCommon.Contains("TOGGLE"));
        }

        [TestMethod]
        public void TestPositiveScoringPaths()
        {
            TcpListener server = null;
            TcpClient client1 = null;
            TcpClient client2 = null;

            server = new TcpListener(IPAddress.Any, 4044);
            server.Start();
            client1 = new TcpClient("localhost", 4044);
            client2 = new TcpClient("localhost", 4044);

            Socket serverSocket = server.AcceptSocket();

            Socket client1Socket = client1.Client;
            Socket client2Socket = client2.Client;

            StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            StringSocket player1Socket = new StringSocket(client1Socket, new UTF8Encoding());
            StringSocket player2Socket = new StringSocket(client2Socket, new UTF8Encoding());

            BoggleServer.BoggleServer.Player player1 = new BoggleServer.BoggleServer.Player("ryan", player1Socket);
            BoggleServer.BoggleServer.Player player2 = new BoggleServer.BoggleServer.Player("ryan2", player2Socket);

            BoggleServer.BoggleServer.Match match = new BoggleServer.BoggleServer.Match(
                player1, player2, 60, new HashSet<string>(System.IO.File.ReadAllLines(@"..\..\..\Resources\Dictionary.txt")), "horstoaeaggdpple");

            match.ReceivedPlayer1Data("word tap", null, player1Socket);
            match.ReceivedPlayer1Data("word rage", null, player1Socket);
            match.ReceivedPlayer1Data("word horse", null, player1Socket);
            match.ReceivedPlayer1Data("word toggle", null, player1Socket);
            Assert.AreEqual(7, player1.Score);
            Assert.IsTrue(player1.UniqueLegalWords.Contains("TAP"));
            Assert.IsTrue(player1.UniqueLegalWords.Contains("RAGE"));
            Assert.IsTrue(player1.UniqueLegalWords.Contains("HORSE"));
            Assert.IsTrue(player1.UniqueLegalWords.Contains("TOGGLE"));

            match.LegalWordsInCommon.Clear();
            player1.UniqueLegalWords.Clear();

            match.ReceivedPlayer2Data("word tap", null, player2Socket);
            match.ReceivedPlayer2Data("word rage", null, player2Socket);
            match.ReceivedPlayer2Data("word horse", null, player2Socket);
            match.ReceivedPlayer2Data("word toggle", null, player2Socket);
            Assert.AreEqual(7, player2.Score);
            Assert.IsTrue(player2.UniqueLegalWords.Contains("TAP"));
            Assert.IsTrue(player2.UniqueLegalWords.Contains("RAGE"));
            Assert.IsTrue(player2.UniqueLegalWords.Contains("HORSE"));
            Assert.IsTrue(player2.UniqueLegalWords.Contains("TOGGLE"));
        }

        [TestMethod]
        public void TestPlayingSameWordAgain()
        {
            TcpListener server = null;
            TcpClient client1 = null;
            TcpClient client2 = null;

            server = new TcpListener(IPAddress.Any, 4045);
            server.Start();
            client1 = new TcpClient("localhost", 4045);
            client2 = new TcpClient("localhost", 4045);

            Socket serverSocket = server.AcceptSocket();

            Socket client1Socket = client1.Client;
            Socket client2Socket = client2.Client;

            StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            StringSocket player1Socket = new StringSocket(client1Socket, new UTF8Encoding());
            StringSocket player2Socket = new StringSocket(client2Socket, new UTF8Encoding());

            BoggleServer.BoggleServer.Player player1 = new BoggleServer.BoggleServer.Player("ryan", player1Socket);
            BoggleServer.BoggleServer.Player player2 = new BoggleServer.BoggleServer.Player("ryan2", player2Socket);

            BoggleServer.BoggleServer.Match match = new BoggleServer.BoggleServer.Match(
                player1, player2, 60, new HashSet<string>(System.IO.File.ReadAllLines(@"..\..\..\Resources\Dictionary.txt")), "horstoaeaggdpple");

            match.ReceivedPlayer1Data("word tap", null, player1Socket);
            match.ReceivedPlayer1Data("word tap", null, player1Socket);

            Assert.AreEqual(1, player1.Score);
            Assert.IsTrue(player1.UniqueLegalWords.Contains("TAP"));

            match.ReceivedPlayer1Data("word tppr", null, player1Socket);
            match.ReceivedPlayer1Data("word tppr", null, player1Socket);

            Assert.AreEqual(0, player1.Score);
            Assert.IsTrue(player1.UniqueLegalWords.Contains("TAP"));
            Assert.IsTrue(player1.IllegalWords.Contains("TPPR"));

        }

        [TestMethod]
        public void TestShortIllegalWords()
        {
            TcpListener server = null;
            TcpClient client1 = null;
            TcpClient client2 = null;

            server = new TcpListener(IPAddress.Any, 4046);
            server.Start();
            client1 = new TcpClient("localhost", 4046);
            client2 = new TcpClient("localhost", 4046);

            Socket serverSocket = server.AcceptSocket();

            Socket client1Socket = client1.Client;
            Socket client2Socket = client2.Client;

            StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            StringSocket player1Socket = new StringSocket(client1Socket, new UTF8Encoding());
            StringSocket player2Socket = new StringSocket(client2Socket, new UTF8Encoding());

            BoggleServer.BoggleServer.Player player1 = new BoggleServer.BoggleServer.Player("ryan", player1Socket);
            BoggleServer.BoggleServer.Player player2 = new BoggleServer.BoggleServer.Player("ryan2", player2Socket);

            BoggleServer.BoggleServer.Match match = new BoggleServer.BoggleServer.Match(
                player1, player2, 60, new HashSet<string>(System.IO.File.ReadAllLines(@"..\..\..\Resources\Dictionary.txt")), "horstoaeaggdpple");

            match.ReceivedPlayer1Data("word tp", null, player1Socket);
            match.ReceivedPlayer1Data("word qz", null, player1Socket);

            Assert.AreEqual(0, player1.Score);
            Assert.IsTrue(player1.IllegalWords.Contains("TP"));
            Assert.IsTrue(player1.IllegalWords.Contains("QZ"));
        }

        [TestMethod]
        public void TestShortWords()
        {
            TcpListener server = null;
            TcpClient client1 = null;
            TcpClient client2 = null;

            server = new TcpListener(IPAddress.Any, 4047);
            server.Start();
            client1 = new TcpClient("localhost", 4047);
            client2 = new TcpClient("localhost", 4047);

            Socket serverSocket = server.AcceptSocket();

            Socket client1Socket = client1.Client;
            Socket client2Socket = client2.Client;

            StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            StringSocket player1Socket = new StringSocket(client1Socket, new UTF8Encoding());
            StringSocket player2Socket = new StringSocket(client2Socket, new UTF8Encoding());

            BoggleServer.BoggleServer.Player player1 = new BoggleServer.BoggleServer.Player("ryan", player1Socket);
            BoggleServer.BoggleServer.Player player2 = new BoggleServer.BoggleServer.Player("ryan2", player2Socket);

            BoggleServer.BoggleServer.Match match = new BoggleServer.BoggleServer.Match(
                player1, player2, 60, new HashSet<string>(System.IO.File.ReadAllLines(@"..\..\..\Resources\Dictionary.txt")), "horstoaeaggdpple");

            match.ReceivedPlayer1Data("word to", null, player1Socket);
            match.ReceivedPlayer1Data("word a", null, player1Socket);

            Assert.AreEqual(0, player1.Score);
            Assert.IsFalse(player1.UniqueLegalWords.Contains("TO"));
            Assert.IsFalse(player1.UniqueLegalWords.Contains("A"));
        }
    }
}
