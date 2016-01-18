using CustomNetworking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BoggleClient
{
    public class BoggleClientModel
    {
        // The TcpClient used for setting up a StringSocket
        public TcpClient _client;

        // The StringSocket used to communicate with the server
        public StringSocket _stringSocket;

        // PAY ATTENTION: this is one of the most interesting features in the program!
        // Register for this event to be motified when a line of text arrives.
        public event Action<String> IncomingLineEvent;

        /// <summary>
        /// Creates a not yet connected client model.
        /// </summary>
        public BoggleClientModel()
        {
            _client = null;
            _stringSocket = null;
        }

        /// <summary>
        /// Connect to the server at the given hostname and port and with the given name.
        /// </summary>
        public void Connect(string hostname, int port)
        {
            // Connect or throw exception
            _client = new TcpClient(hostname, port);

            // Create a StringSocket for communicating with the server.
            _stringSocket = new StringSocket(_client.Client, UTF8Encoding.Default);           
        }

        /// <summary>
        /// Disconnect from any servers.
        /// </summary>
        public void Disconnect()
        {
            if (!ReferenceEquals(_stringSocket, null))
            {
                _stringSocket.Close();
                _stringSocket = null;
            }
        }

        /// <summary>
        /// Send a line of text to the server. Throws an exception if the socket
        /// is null.
        /// </summary>
        /// <param name="line"></param>
        public void SendCommand(String line)
        {
            // Send a command
            _stringSocket.BeginSend(line + "\n", (e, p) => { }, null);

            // Wait for a command from the server and return
            _stringSocket.BeginReceive(LineReceived, null);
        }

        /// <summary>
        /// Deal with an arriving line of text.
        /// </summary>
        private void LineReceived(String s, Exception e, object p)
        {
            // E and S will come back null when we close our socket
            if (ReferenceEquals(e, null) && ReferenceEquals(s, null) || !ReferenceEquals(e, null))
            {
                return;
            }

            // If we've been terminated, then reset everything and inform user
            if (s == "TERMINATED")
            {
                _stringSocket.Close();
                _stringSocket = null;
            }

            // Send the line onward if the connection was not terminated
            if (IncomingLineEvent != null)
            {
                IncomingLineEvent(s);
            }

            // Start listening on the socket again
            if (!ReferenceEquals(_stringSocket, null))
            {
                _stringSocket.BeginReceive(LineReceived, null);
            }

        }
    }
}
