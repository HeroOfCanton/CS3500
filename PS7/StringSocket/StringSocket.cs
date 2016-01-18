using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;

namespace CustomNetworking
{
    /// <summary> 
    /// A StringSocket is a wrapper around a Socket.  It provides methods that
    /// asynchronously read lines of text (strings terminated by newlines) and 
    /// write strings. (As opposed to Sockets, which read and write raw bytes.)  
    ///
    /// StringSockets are thread safe.  This means that two or more threads may
    /// invoke methods on a shared StringSocket without restriction.  The
    /// StringSocket takes care of the synchonization.
    /// 
    /// Each StringSocket contains a Socket object that is provided by the client.  
    /// A StringSocket will work properly only if the client refrains from calling
    /// the contained Socket's read and write methods.
    /// 
    /// If we have an open Socket s, we can create a StringSocket by doing
    /// 
    ///    StringSocket ss = new StringSocket(s, new UTF8Encoding());
    /// 
    /// We can write a string to the StringSocket by doing
    /// 
    ///    ss.BeginSend("Hello world", callback, payload);
    ///    
    /// where callback is a SendCallback (see below) and payload is an arbitrary object.
    /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
    /// successfully written the string to the underlying Socket, or failed in the 
    /// attempt, it invokes the callback.  The parameters to the callback are a
    /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
    /// the Exception that caused the send attempt to fail.
    /// 
    /// We can read a string from the StringSocket by doing
    /// 
    ///     ss.BeginReceive(callback, payload)
    ///     
    /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
    /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
    /// string of text terminated by a newline character from the underlying Socket, or
    /// failed in the attempt, it invokes the callback.  The parameters to the callback are
    /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
    /// string or the Exception will be non-null, but nor both.  If the string is non-null, 
    /// it is the requested string (with the newline removed).  If the Exception is non-null, 
    /// it is the Exception that caused the send attempt to fail.
    /// 
    /// Authors - Ryan Welling and Jared Jensen, except where noted.
    /// </summary>

    public class StringSocket
    {
        // These delegates describe the callbacks that are used for sending and receiving strings.
        public delegate void SendCallback(Exception e, object payload);
        public delegate void ReceiveCallback(String s, Exception e, object payload);        

        // Backing structures.
        Queue<Message> _messagesToSend;
        Queue<Message> _messagesReceived;
        private Socket _socket;
        private Encoding _encoding;

        /// <summary>
        ///  Records whether an asynchronous send receive attempt is ongoing
        /// </summary>
        private bool _sendIsOngoing = false;
        private bool _receiveParseIsOngoing = false;

        /// <summary>
        /// For use with the BeginReceive method.
        /// </summary>
        private string _textToReturn;
        private string _textReceivedSoFar;
       
        /// <summary>
        /// Text that needs to be sent to the client but has not yet gone
        /// </summary>
        private string _textToSend;

        /// <summary>
        /// Message that needs to be sent to the client but hasn't gone yet
        /// </summary>
        private Message _messageToSend;

        /// <summary>
        /// Message that has been received but not dealt with yet
        /// </summary>
        private Message _messageReceived;

        /// <summary>
        ///  For synchronizing sends and receives
        /// </summary>
        private object lockSend = new object();
        private object lockReceive = new object();

        /// <summary>
        /// Creates a StringSocket from a regular Socket, which should already be connected.  
        /// The read and write methods of the regular Socket must not be called after the
        /// LineSocket is created.  Otherwise, the StringSocket will not behave properly.  
        /// The encoding to use to convert between raw bytes and strings is also provided.
        /// </summary>
        public StringSocket(Socket s, Encoding e)
        {
            _socket = s;
            _encoding = e;
            _textToSend = "";
            _textReceivedSoFar = "";
            _messagesToSend = new Queue<Message>();
            _messagesReceived = new Queue<Message>();
        }

        /// <summary>
        /// We can write a string to a StringSocket ss by doing
        /// 
        ///    ss.BeginSend("Hello world", callback, payload);
        ///    
        /// where callback is a SendCallback (see below) and payload is an arbitrary object.
        /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
        /// successfully written the string to the underlying Socket, or failed in the 
        /// attempt, it invokes the callback.  The parameters to the callback are a
        /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
        /// the Exception that caused the send attempt to fail. 
        /// 
        /// This method is non-blocking.  This means that it does not wait until the string
        /// has been sent before returning.  Instead, it arranges for the string to be sent
        /// and then returns.  When the send is completed (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginSend
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginSend must take care of synchronization instead.  On a given StringSocket, each
        /// string arriving via a BeginSend method call must be sent (in its entirety) before
        /// a later arriving string can be sent.
        /// 
        /// **reminder: payload never goes out over the internet, only between string socket and original program / server**
        /// </summary>
        public void BeginSend(String s, SendCallback callback, object payload)
        {
            // Enqueue the incoming message as a message object, lock so we don't mess up the queue
            lock (lockSend)
            {
                _messagesToSend.Enqueue(new Message(s, callback, payload));

                if (!_sendIsOngoing)
                {
                    _sendIsOngoing = true;
                    SendMessage();
                }
            }
        }

        /// <summary>
        /// Helper method that dequeues the next message, pulls out it's string
        /// and then passes it along to be sent
        /// </summary>
        private void SendMessage()
        {
            // Get the message from the queue
            _messageToSend = _messagesToSend.Dequeue();

            // Get string out of message
            _textToSend = _messageToSend.Text;

            // try sending the message, catch exceptions to send to callback
            try
            {
                SendBytes();
            }
            catch (Exception e)
            {
                ThreadPool.QueueUserWorkItem(x => { _messageToSend.Callback(null, _messageToSend.Payload); });
            }
        }

        /// <summary>
        /// Helper method that attempts to send the entire outgoing string.
        /// 
        /// Some of this code was written by Prof. de St. Germain - Lecture 17 Examples
        /// </summary>
        private void SendBytes()
        {
            // if string is empty, message is sent, invoke callback and check queue for more
            if (_textToSend == "")
            {
                // Thread here in case User's callback takes too long
                ThreadPool.QueueUserWorkItem(x => { _messageToSend.Callback(null, _messageToSend.Payload); });
                
                // This is how we empty the queue
                if (_messagesToSend.Count > 0)
                    SendMessage();
                else
                    _sendIsOngoing = false;
            }
            // if string isn't empty, let's keep going and send the rest
            else
            {
                byte[] outgoingBuffer = _encoding.GetBytes(_textToSend);
                _textToSend = "";
                _socket.BeginSend(outgoingBuffer, 0, outgoingBuffer.Length, SocketFlags.None, OnDataSent, outgoingBuffer);
            }
        }

        /// <summary>
        /// This method is executed by the operating system when some data gets sent.
        /// 
        /// Author: Prof. de St. Germain - Lecture 17 Examples
        /// </summary>
        /// <param name="ar">The result of the send: used to tell whether everything has been sent.</param>
        private void OnDataSent(IAsyncResult ar)
        {
            // Find out how many bytes were actually sent
            int bytes = _socket.EndSend(ar);

            // Get exclusive access to send mechanism
            lock (lockSend)
            {
                // Get the bytes that we attempted to send
                byte[] outgoingBuffer = (byte[])ar.AsyncState;

                // The socket has been closed
                if (bytes == 0)
                {
                    _socket.Close();
                }

                // Prepend the unsent bytes and try sending again.
                else
                {
                    _textToSend = _encoding.GetString(outgoingBuffer, bytes, outgoingBuffer.Length - bytes) + _textToSend;
                    SendBytes();
                }
            }            
        }

        /// <summary>
        /// 
        /// <para>
        /// We can read a string from the StringSocket by doing
        /// </para>
        /// 
        /// <para>
        ///     ss.BeginReceive(callback, payload)
        /// </para>
        /// 
        /// <para>
        /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
        /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
        /// string of text terminated by a newline character from the underlying Socket, or
        /// failed in the attempt, it invokes the callback.  The parameters to the callback are
        /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
        /// string or the Exception will be non-null, but nor both.  If the string is non-null, 
        /// it is the requested string (with the newline removed).  If the Exception is non-null, 
        /// it is the Exception that caused the send attempt to fail.
        /// </para>
        /// 
        /// <para>
        /// This method is non-blocking.  This means that it does not wait until a line of text
        /// has been received before returning.  Instead, it arranges for a line to be received
        /// and then returns.  When the line is actually received (at some time in the future), the
        /// callback is called on another thread.
        /// </para>
        /// 
        /// <para>
        /// This method is thread safe.  This means that multiple threads can call BeginReceive
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginReceive must take care of synchronization instead.  On a given StringSocket, each
        /// arriving line of text must be passed to callbacks in the order in which the corresponding
        /// BeginReceive call arrived.
        /// </para>
        /// 
        /// <para>
        /// Note that it is possible for there to be incoming bytes arriving at the underlying Socket
        /// even when there are no pending callbacks.  StringSocket implementations should refrain
        /// from buffering an unbounded number of incoming bytes beyond what is required to service
        /// the pending callbacks.        
        /// </para>
        /// 
        /// <param name="callback"> The function to call upon receiving the data</param>
        /// <param name="payload"> 
        /// The payload is "remembered" so that when the callback is invoked, it can be associated
        /// with a specific Begin Receiver....
        /// </param>  
        /// 
        /// <example>
        ///   Here is how you might use this code:
        ///   <code>
        ///                    client = new TcpClient("localhost", port);
        ///                    Socket       clientSocket = client.Client;
        ///                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());
        ///                    receiveSocket.BeginReceive(CompletedReceive1, 1);
        /// 
        ///   </code>
        /// </example>
        /// </summary>
        public void BeginReceive(ReceiveCallback callback, object payload)
        {
            lock (lockReceive)
            {
                // Store it as a message.
                _messagesReceived.Enqueue(new Message(callback, payload));

                if (!_receiveParseIsOngoing)
                {
                    _receiveParseIsOngoing = true;
                    try
                    {
                        MessageReceived();
                    }
                    catch (Exception e)
                    {
                        _messageReceived.ReCallback(null, e, _messageReceived.Payload);
                    }
                }
            }
        }

        /// <summary>
        /// Helper method that parses received data, looking for newline characters
        /// Keeps going until queue of received callbacks and payloads, as well as data
        /// has been sent.
        /// </summary>
        private void MessageReceived()
        {           
            int index;
            // If there are callbacks / payloads pending, check data for new line chars
            if (_messagesReceived.Count > 0)
            {
                // get the index of the first new line char and use that to construct a new string
                // that will be returned.
                if ((index = _textReceivedSoFar.IndexOf('\n')) >= 0)
                {
                    _textToReturn = _textReceivedSoFar.Substring(0, index);
                    if (_textToReturn.EndsWith("\r"))
                    {
                        _textToReturn = _textToReturn.Substring(0, index - 1);
                    }

                    _textReceivedSoFar = _textReceivedSoFar.Substring(index + 1);
                    _messageReceived = _messagesReceived.Dequeue();
                    ThreadPool.QueueUserWorkItem(x => { _messageReceived.ReCallback(_textToReturn, null, _messageReceived.Payload); });
                }
                // If there is still a request... then call BeginReceive
                byte[] buffer = new byte[1024];
                _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, MessageReceivedCallback, buffer);
            }
            else
            {
                _receiveParseIsOngoing = false;
            }
        }

        /// <summary>
        /// Helper callback when some data has been received.
        /// </summary>
        private void MessageReceivedCallback(IAsyncResult result)
        {
            // Get the buffer to which the data was written.
            byte[] writeBuffer = (byte[])(result.AsyncState);

            // Figure out how many bytes have come in
            int bytes = _socket.EndReceive(result);

            // If no bytes were received, it means the client closed its side of the socket.
            if (bytes == 0)
            {
                _socket.Close();
            }

            // Otherwise, decode and store the incoming bytes.
            else
            {
                lock (lockReceive)
                {
                    // Convert the bytes into a string
                    _textReceivedSoFar += _encoding.GetString(writeBuffer, 0, bytes);
                    try
                    {
                        MessageReceived();
                    }
                    catch (Exception e)
                    {
                        _messageReceived.ReCallback(null, e, _messageReceived.Payload);
                    }
                }
            }
        }

        /// <summary>
        /// Calling the close method will close the String Socket (and the underlying
        /// standard socket).  The close method  should make sure all 
        ///
        /// Note: ideally the close method should make sure all pending data is sent
        ///       
        /// Note: closing the socket should discard any remaining messages and       
        ///       disable receiving new messages
        /// 
        /// Note: Make sure to shutdown the socket before closing it.
        ///
        /// Note: the socket should not be used after closing.
        /// </summary>
        public void Close()
        {
            // ideally the close method should make sure all pending data is sent

            _socket.Shutdown(SocketShutdown.Both);

            // closing the socket should discard any remaining messages and disable receiving new messages
            _messagesToSend.Clear();
            _textReceivedSoFar = "";

            // Make sure to shutdown the socket before closing it.
            _socket.Close();
        }

        /// <summary>
        /// Private class that contains all of the portions of the message that comes in or needs to go out
        /// </summary>
        private class Message
        {
            /// <summary>
            /// Constructs a new message object with the given parameters
            /// </summary>
            /// <param name="text"></param>
            /// <param name="callback"></param>
            /// <param name="payload"></param>
            public Message(string text, StringSocket.SendCallback callback, object payload)
            {
                Text = text;
                Callback = callback;
                Payload = payload;
            }
            /// <summary>
            /// Constructs a new message object with the given parameters
            /// </summary>
            /// <param name="callback"></param>
            /// <param name="payload"></param>
            public Message(StringSocket.ReceiveCallback callback, object payload)
            {
                ReCallback = callback;
                Payload = payload;
            }

            /// <summary>
            /// Property to setup string that is either received or needing to be sent
            /// </summary>
            public string Text { get; set; }

            /// <summary>
            /// Property to setup SendCallback
            /// </summary>
            public StringSocket.SendCallback Callback { get; set; }

            /// <summary>
            /// Property to setup ReceiveCallback
            /// </summary>
            public StringSocket.ReceiveCallback ReCallback { get; set; }

            /// <summary>
            /// Property to setup Payload
            /// </summary>
            public object Payload { get; set; }
        }
    }
}
