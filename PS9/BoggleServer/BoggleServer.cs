using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Threading;
using CustomNetworking;
using BoggleClient;

namespace BoggleServer
{
    /// <summary>
    /// 
    /// </summary>
    public class BoggleServer
    {
        // The timer.
        private static System.Timers.Timer timer;

        // The listener for incoming connections.
        private TcpListener _server;

        // The player waiting for a match.
        private Player _waitingPlayer;

        // The collection of all the matches.
        private HashSet<Match> _allMatches;

        // The port to be used.
        private readonly int _PORT;

        // The lock to protect local variables.
        private readonly object _lock;

        // The variables from the command line.    
        int _gameTime;
        private HashSet<string> _dictionary;
        private string _boardConfiguration;

        /// <summary>
        /// Creates a BoggleServer that listens for connections on port 2000.
        /// </summary>
        public BoggleServer(int gameTime, HashSet<string> dictionary, string boardConfiguration)
        {
            // Initialize fields.
            _PORT = 2000;
            _server = new TcpListener(IPAddress.Any, _PORT);
            _waitingPlayer = null;
            _allMatches = new HashSet<Match>();
            _lock = new object();
            _gameTime = gameTime;
            _dictionary = dictionary;
            _boardConfiguration = boardConfiguration;

            // Start the server.
            _server.Start();

            // Start listening for a client to connect.
            _server.BeginAcceptSocket(ConnectionReceived, null);

            // Create a timer with a one second interval.
            timer = new System.Timers.Timer(1000);

            // Hook up the Elapsed event for the timer. 
            timer.Elapsed += OneSecondHasPassed;
            timer.Enabled = true;

            // Keep this program alive
            Thread t = new Thread(stayAlive);
            t.Start();
        }

        /// <summary>
        /// Keeps the main thread alive.
        /// </summary>
        private void stayAlive()
        {
            while (true)
            {
                // Wait 1 second so as to not tax CPU
                System.Threading.Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// This method will execute every 1 second, when the timer pulses.
        /// It updates the time in each match and then, if the match is over,
        /// ends the match and removes the match from the collection of matches.
        /// </summary>
        /// <param name="source">Unused.</param>
        /// <param name="e">Unused.</param>
        private void OneSecondHasPassed(Object source, ElapsedEventArgs e)
        {
            // Make a copy of all matches for removing matches soon
            HashSet<Match> copyOfAllMatches = new HashSet<Match>(_allMatches);

            // Iterate through all matches and tell them to update their times.
            foreach (Match match in copyOfAllMatches)
            {
                // Decriment each match's timer by 1 second.                
                match.GameTimeLeft -= 1;

                // Inform each player of the new time.
                match.TransmitTime();

                // If match has prematurely terminated, remove it
                if (match._isTerminated)
                {
                    _allMatches.Remove(match);
                }

                // If time has expired, end the match.
                if (match.HasTimeExpired())
                {
                    match.EndMatch();
                    _allMatches.Remove(match); // Remove match from all matches
                }
            }
        }

        /// <summary>
        /// Handles connection requests.
        /// </summary>
        private void ConnectionReceived(IAsyncResult ar)
        {
            // The client connected. Create a socket to communicate with it.
            Socket socket = _server.EndAcceptSocket(ar);

            // Wrap the socket into a StringSocket to communicate with it.
            StringSocket ss = new StringSocket(socket, UTF8Encoding.Default);

            // Wait for the client to send us a string.
            ss.BeginReceive(ReadyToPlay, ss);

            // Start listening for another client to connect.
            _server.BeginAcceptSocket(ConnectionReceived, null);
        }

        /// <summary>
        /// Handles claims from players that they are ready to play.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="e"></param>
        /// <param name="payload"></param>
        private void ReadyToPlay(string line, Exception e, object payload)
        {
            // Make sure socket hasn't closed or thrown exception
            if (ReferenceEquals(line, null) && ReferenceEquals(e, null) || !ReferenceEquals(e, null))
            {
                return;
            }

            // The socket to talk to this client.
            StringSocket ss = (StringSocket)payload;

            // For storing soon.
            string playerName;

            // If client's message doesn't start with "play " (case insensitive) and then at least one character,
            // tell them the command is being ignored and wait for another message from them.
            if (line.Length < 6 || !Regex.IsMatch(line, @"^[Pp][Ll][Aa][Yy] "))
            {
                // Inform client we're ignoring command.
                ss.BeginSend("IGNORING: " + line + "\n", (ee, pp) => { }, null);

                // Start listening on that socket again and return.
                ss.BeginReceive(ReadyToPlay, ss);
                return;
            }
            else
            {
                // Extract the player's name from the input and remove any /r from the end.
                playerName = line.Substring(5);

                if (playerName.EndsWith("\r"))
                {
                    playerName = playerName.Substring(0, line.Length - 1);
                }

                // remove all whitespace from words that come in
                playerName = new string(playerName.Where(c => !Char.IsWhiteSpace(c)).ToArray());
            }

            // Lock class variables.
            lock (_lock)
            {
                // If no waiting opponent is present, store this player.
                if (_waitingPlayer == null)
                {
                    _waitingPlayer = new Player(playerName, ss);
                }
                // Otherwise, create a match and add it to the match collection.
                else
                {
                    Match match = new Match(_waitingPlayer, new Player(playerName, ss), _gameTime, _dictionary, _boardConfiguration);
                    _allMatches.Add(match);
                    _waitingPlayer = null; // There is no longer a waiting player.
                }
            }
        }

        /// <summary>
        /// Encapsulates everything needed to generate a new Player
        /// </summary>
        public class Player
        {
            /// <summary>
            /// The player's name.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// The StringSocket for communicating with the player.
            /// </summary>
            public StringSocket Socket { get; private set; }

            /// <summary>
            /// Player's score
            /// </summary>
            public int Score { get; set; }

            /// <summary>
            /// HashSet of client played legal words that weren't played by the opponent
            /// Passed back to opponent as opponent's list of legal words
            /// </summary>
            public HashSet<string> UniqueLegalWords { get; set; }

            /// <summary>
            /// HashSet of client played illegal words
            /// Passed back to opponent as opponent's list of illegal words
            /// </summary>
            public HashSet<string> IllegalWords { get; set; }

            /// <summary>
            /// Creates a new Client object.
            /// </summary>
            /// <param name="name">The player's name.</param>
            /// <param name="ss">The StringSocket, with which to communicate with the player.</param>
            public Player(string name, StringSocket socket)
            {
                // Initialize properties.
                Name = name;
                Socket = socket;
                Score = 0;
                UniqueLegalWords = new HashSet<string>();
                IllegalWords = new HashSet<string>();
            }
        }

        /// <summary>
        /// Represents a boggle game match between two players.
        /// </summary>
        public class Match
        {
            // The dictionary of words.
            public HashSet<string> _dictionary;

            // Boolean that gets tripped if the match is prematurely terminated
            public bool _isTerminated = false;

            /// <summary>
            /// The board for the match.
            /// </summary>
            public BoggleBoard Board { get; private set; }

            /// <summary>
            /// Player 1 object
            /// </summary>
            public Player Player1 { get; private set; }

            /// <summary>
            /// Player 2 object
            /// </summary>
            public Player Player2 { get; private set; }

            /// <summary>
            /// Server specified time of game length
            /// </summary>
            public int GameTimeLeft { get; set; }

            /// <summary>
            /// HashSet of legal words in common between the two players
            /// </summary>
            public HashSet<string> LegalWordsInCommon { get; set; }

            /// <summary>
            /// HashSet of all words found by both players
            /// </summary>
            public HashSet<string> AllFoundWords { get; set; }

            /// <summary>
            /// Creates a new Match object that represents a boggle game match between two players.
            /// </summary>
            /// <param name="player1"></param>
            /// <param name="player2"></param>
            /// <param name="startTime"></param>
            /// <param name="boardSetup"></param>
            public Match(Player player1, Player player2, int startTime, HashSet<string> dictionary, string boardConfiguration)
            {
                // Initialize board.                
                if (boardConfiguration.Length == 16)
                {
                    Board = new BoggleBoard(boardConfiguration);
                }
                else
                {
                    Board = new BoggleBoard();
                }

                // Initialize variables.
                _dictionary = dictionary;
                Player1 = player1;
                Player2 = player2;
                GameTimeLeft = startTime;
                LegalWordsInCommon = new HashSet<string>();
                AllFoundWords = new HashSet<string>();

                // Send game-starting command to each player.
                Player1.Socket.BeginSend(string.Format("START {0} {1} {2}\n", Board.ToString(), GameTimeLeft, Player2.Name), (ee, pp) => { }, null);
                Player2.Socket.BeginSend(string.Format("START {0} {1} {2}\n", Board.ToString(), GameTimeLeft, Player1.Name), (ee, pp) => { }, null);

                // Start listening for either player to send a message.
                Player1.Socket.BeginReceive(ReceivedPlayer1Data, Player1.Socket);
                Player2.Socket.BeginReceive(ReceivedPlayer2Data, Player2.Socket);
            }

            /// <summary>
            /// At any time, a client may play a word by sending the command "WORD $", where $ is the word being played.
            /// Each time the score changes, the server should send the command "SCORE #1 #2", 
            /// where #1 is the client's current score and #2 is the opponent's current score.
            /// </summary>
            /// <param name="word"></param>
            /// <param name="e"></param>
            /// <param name="payload"></param>
            public void ReceivedPlayer1Data(string line, Exception e, object payload)
            {
                // If returned string and exception are both null, the underlying socket has been closed
                // Call terminate method and break out of method.
                if (ReferenceEquals(line, null) && ReferenceEquals(e, null) || !ReferenceEquals(e, null))
                {
                    TerminateMatch(1);
                    return;
                }

                // Will soon extract this from incoming line
                string word;

                // If client's message doesn't start with "word " (case insensitive) and then at least one character,
                // tell them the command is being ignored and wait for another message from them.
                if (line.Length < 6 || !Regex.IsMatch(line, @"^[Ww][Oo][Rr][Dd] "))
                {
                    Player1.Socket.BeginSend(string.Format("IGNORING: {0}\n", line), (ee, pp) => { }, null);
                    Player1.Socket.BeginReceive(ReceivedPlayer1Data, Player1.Socket);
                    return;
                }
                else
                {
                    // Extract the player's word from the line we received and remove any /r from the end.
                    word = line.Substring(5);

                    if (word.EndsWith("\r"))
                    {
                        word = word.Substring(0, word.Length - 1);
                    }

                    // remove all whitespace from words that come in
                    word = new string(word.Where(c => !Char.IsWhiteSpace(c)).ToArray());
                }

                // convert line to Upper Case so that we can check against dictionary
                word = word.ToUpper();

                // Don't allow player to play same word over and over for points
                if (Player1.UniqueLegalWords.Contains(word) || Player1.IllegalWords.Contains(word) || LegalWordsInCommon.Contains(word))
                {
                    Player1.Socket.BeginReceive(ReceivedPlayer1Data, Player1.Socket);
                    return;
                }
                else if (_dictionary.Contains(word))
                {
                    // Check to see if word can be formed
                    if (Board.CanBeFormed(word))
                    {
                        int score = 0;
                        switch (word.Length)
                        {
                            // Words less than 3 chars in length get discarded    
                            case 0:
                            case 1:
                            case 2:
                                break;
                            // Words 3-4 chars in length get added to the legal word list and a score of 1
                            case 3:
                            case 4:
                                Player1.UniqueLegalWords.Add(word);
                                Player1.Score += 1;
                                score = 1;
                                break;
                            // Words of 5 chars in length get added to the legal word list, and get a score of 2
                            case 5:
                                Player1.UniqueLegalWords.Add(word);
                                Player1.Score += 2;
                                score = 2;
                                break;
                            // Words of 6 chars in length get added to the legal word list, and get a score of 3
                            case 6:
                                Player1.UniqueLegalWords.Add(word);
                                Player1.Score += 3;
                                score = 3;
                                break;
                            // Words of 7 chars in length get added to the legal word list, and get a score of 5
                            case 7:
                                Player1.UniqueLegalWords.Add(word);
                                Player1.Score += 5;
                                score = 5;
                                break;
                            // Words longer than 7 chars get added to the legal word list, and get a score of 11
                            default:
                                Player1.UniqueLegalWords.Add(word);
                                Player1.Score += 11;
                                score = 11;
                                break;
                        }

                        // Check to see if player 2 has the same word, if they do, add it to the list of words in common,
                        // decriment each players score, and remove it from player2's list of legal words
                        if (Player2.UniqueLegalWords.Contains(word))
                        {
                            LegalWordsInCommon.Add(word);
                            Player2.UniqueLegalWords.Remove(word);
                            Player1.UniqueLegalWords.Remove(word);
                            Player1.Score -= score;
                            Player2.Score -= score;
                        }
                        TransmitScore();
                    }
                    // Word could not be formed from the board, add it to the illegal word list, and adjust score by -1
                    else
                    {
                        Player1.IllegalWords.Add(word);
                        Player1.Score -= 1;
                        if (word.Length < 3)
                        {
                            Player1.Score += 1;
                            Player1.Socket.BeginReceive(ReceivedPlayer1Data, Player1.Socket);
                            return;
                        }
                        TransmitScore();
                    }
                }
                // Word was not in the dictionary, add it to the illegal word list, 
                // add word to set of all found words and adjust score by -1            
                else
                {
                    Player1.IllegalWords.Add(word);
                    Player1.Score -= 1;
                    if (word.Length < 3)
                    {
                        Player1.Score += 1;
                        Player1.Socket.BeginReceive(ReceivedPlayer1Data, Player1.Socket);
                        return;
                    }
                    TransmitScore();
                }
                Player1.Socket.BeginReceive(ReceivedPlayer1Data, Player1.Socket);
            }

            /// <summary>
            /// At any time, a client may play a word by sending the command "WORD $", where $ is the word being played.
            /// Each time the score changes, the server should send the command "SCORE #1 #2", 
            /// where #1 is the client's current score and #2 is the opponent's current score.
            /// </summary>
            /// <param name="data"></param>
            public void ReceivedPlayer2Data(string line, Exception e, object payload)
            {
                // If returned string and exception are both null, the underlying socket has been closed
                // Call terminate method and break out of method.
                if (ReferenceEquals(line, null) && ReferenceEquals(e, null) || !ReferenceEquals(e, null))
                {
                    TerminateMatch(2);
                    return;
                }

                // Will soon extract this from incoming line
                string word;

                // If client's message doesn't start with "word " (case insensitive) and then at least one character,
                // tell them the command is being ignored and wait for another message from them.
                if (line.Length < 6 || !Regex.IsMatch(line, @"^[Ww][Oo][Rr][Dd] "))
                {
                    Player2.Socket.BeginSend(string.Format("IGNORING: {0}\n", line), (ee, pp) => { }, null);
                    Player2.Socket.BeginReceive(ReceivedPlayer2Data, Player2.Socket);
                    return;
                }
                else
                {
                    // Extract the player's word from the line we received and remove any /r from the end.
                    word = line.Substring(5);

                    if (word.EndsWith("\r"))
                    {
                        word = word.Substring(0, word.Length - 1);
                    }

                    // remove all whitespace from words that come in
                    word = new string(word.Where(c => !Char.IsWhiteSpace(c)).ToArray());
                }

                // convert line to Upper Case so that we can check against dictionary
                word = word.ToUpper();

                // Don't allow player to play same word over and over for points
                if (Player2.UniqueLegalWords.Contains(word) || Player2.IllegalWords.Contains(word) || LegalWordsInCommon.Contains(word))
                {
                    Player2.Socket.BeginReceive(ReceivedPlayer2Data, Player2.Socket);
                    return;
                }
                else if (_dictionary.Contains(word))
                {
                    // Check to see if word can be formed
                    if (Board.CanBeFormed(word))
                    {
                        int score = 0;
                        switch (word.Length)
                        {
                            // Words less than 3 chars in length get discarded    
                            case 0:
                            case 1:
                            case 2:
                                break;
                            // Words 3-4 chars in length get added to the legal word list and a score of 1
                            case 3:
                            case 4:
                                Player2.UniqueLegalWords.Add(word);
                                Player2.Score += 1;
                                score = 1;
                                break;
                            // Words of 5 chars in length get added to the legal word list, and get a score of 2
                            case 5:
                                Player2.UniqueLegalWords.Add(word);
                                Player2.Score += 2;
                                score = 2;
                                break;
                            // Words of 6 chars in length get added to the legal word list, and get a score of 3
                            case 6:
                                Player2.UniqueLegalWords.Add(word);
                                Player2.Score += 3;
                                score = 3;
                                break;
                            // Words of 7 chars in length get added to the legal word list, and get a score of 5
                            case 7:
                                Player2.UniqueLegalWords.Add(word);
                                Player2.Score += 5;
                                score = 5;
                                break;
                            // Words longer than 7 chars get added to the legal word list, and get a score of 11
                            default:
                                Player2.UniqueLegalWords.Add(word);
                                Player2.Score += 11;
                                score = 11;
                                break;
                        }

                        // Check to see if player 2 has the same word, if they do, add it to the list of words in common,
                        // decriment each players score, and remove it from player2's list of legal words
                        if (Player1.UniqueLegalWords.Contains(word))
                        {
                            LegalWordsInCommon.Add(word);
                            Player2.UniqueLegalWords.Remove(word);
                            Player1.UniqueLegalWords.Remove(word);
                            Player1.Score -= score;
                            Player2.Score -= score;
                        }
                        TransmitScore();
                    }
                    // Word could not be formed from the board, add it to the illegal word list, and adjust score by -1
                    else
                    {
                        Player2.IllegalWords.Add(word);
                        Player2.Score -= 1;
                        if (word.Length < 3)
                        {
                            Player2.Score += 1;
                            Player2.Socket.BeginReceive(ReceivedPlayer2Data, Player2.Socket);
                            return;
                        }
                        TransmitScore();
                    }
                }
                // Word was not in the dictionary, add it to the illegal word list, 
                // add word to set of all found words and adjust score by -1              
                else
                {
                    Player2.IllegalWords.Add(word);
                    Player2.Score -= 1;
                    if (word.Length < 3)
                    {
                        Player2.Score += 1;
                        Player2.Socket.BeginReceive(ReceivedPlayer2Data, Player2.Socket);
                        return;
                    }
                    TransmitScore();
                }
                Player2.Socket.BeginReceive(ReceivedPlayer2Data, Player2.Socket);
            }

            /// <summary>
            /// Helper method that Terminates the game if one or the other players disconnects or the underlying socket is closed
            /// </summary>
            /// <param name="playerThatIsStillThere"></param>
            private void TerminateMatch(int playerThatIsStillThere)
            {
                if (playerThatIsStillThere == 2)
                {
                    Player1.Socket.BeginSend("TERMINATED\n", (ee, pp) => { }, null);
                    Player1.Socket.Close();
                }
                else
                {
                    Player2.Socket.BeginSend("TERMINATED\n", (ee, pp) => { }, null);
                    Player2.Socket.Close();
                }
                _isTerminated = true;
            }

            /// <summary>
            /// Transmits remaining time to each player
            /// </summary>
            public void TransmitTime()
            {
                Player1.Socket.BeginSend(string.Format("TIME {0}\n", GameTimeLeft), (ee, pp) => { }, null);
                Player2.Socket.BeginSend(string.Format("TIME {0}\n", GameTimeLeft), (ee, pp) => { }, null);
            }

            /// <summary>
            /// Transmits score to each player
            /// </summary>
            private void TransmitScore()
            {
                Player1.Socket.BeginSend(string.Format("SCORE {0} {1}\n", Player1.Score, Player2.Score), (ee, pp) => { }, null);
                Player2.Socket.BeginSend(string.Format("SCORE {0} {1}\n", Player2.Score, Player1.Score), (ee, pp) => { }, null);
            }

            /// <summary>
            /// Returns true if the game timer expires.
            /// </summary>
            /// <returns>Boolean that determines whether the match is over</returns>
            public bool HasTimeExpired()
            {
                if (GameTimeLeft < 1)
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            /// When time has expired, the server ignores any further communication from the clients and shuts down the game. 
            /// - First, it transmits the final score to both clients as described above. 
            /// - Next, it transmits a game summary line to both clients. 
            /// Suppose that during the game the client played A legal words that weren't played by the opponent, 
            /// the opponent played B legal words that weren't played by the client, both players played C legal words in common, 
            /// the client played D illegal words, and the opponent played E illegal words. 
            /// The game summary command should be "STOP a #1 b #2 c #3 d #4 e #5", 
            /// where a, b, c, d, and e are the counts described above and #1, #2, #3, #4, and #5 are the corresponding space-separated lists of words.
            /// 
            /// After this final transmission, the server should close the two client sockets.
            /// </summary>
            /// <param name="match"></param>
            public void EndMatch()
            {
                // Transmit score to both players
                TransmitScore();

                // Transmit game summary to each player
                Player1.Socket.BeginSend(string.Format("STOP {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}\n",
                                                        Player1.UniqueLegalWords.Count,             // {0}
                                                        string.Join(" ", Player1.UniqueLegalWords), // {1}                                                        
                                                        Player2.UniqueLegalWords.Count,             // {2}
                                                        string.Join(" ", Player2.UniqueLegalWords), // {3}
                                                        LegalWordsInCommon.Count,                   // {4}
                                                        string.Join(" ", LegalWordsInCommon),       // {5}
                                                        Player1.IllegalWords.Count,                 // {6}
                                                        string.Join(" ", Player1.IllegalWords),     // {7}
                                                        Player2.IllegalWords.Count,                 // {8}
                                                        string.Join(" ", Player2.IllegalWords)      // {9}
                                                      ), (ee, pp) => { }, null);                    // Finish BeginSend

                Player2.Socket.BeginSend(string.Format("STOP {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}\n",
                                                        Player2.UniqueLegalWords.Count,             // {0}
                                                        string.Join(" ", Player2.UniqueLegalWords), // {1}                                                        
                                                        Player1.UniqueLegalWords.Count,             // {2}
                                                        string.Join(" ", Player1.UniqueLegalWords), // {3}
                                                        LegalWordsInCommon.Count,                   // {4}
                                                        string.Join(" ", LegalWordsInCommon),       // {5}
                                                        Player2.IllegalWords.Count,                 // {6}
                                                        string.Join(" ", Player2.IllegalWords),     // {7}
                                                        Player1.IllegalWords.Count,                 // {8}
                                                        string.Join(" ", Player1.IllegalWords)      // {9}
                                                      ), (ee, pp) => { }, null);                    // Finish BeginSend                            

                // Close both sockets.
                Player1.Socket.Close();
                Player2.Socket.Close();
            }
        }

        /// <summary>
        /// The main program.
        /// </summary>
        /// <param name="args">Game time in seconds, path to a text file dictionary, optional board configuration (string of 16 characters).</param>
        public static void Main(string[] args)
        {
            // The max time limit for each game
            int GameTime;

            // The set of legal words for each game
            HashSet<string> Dictionary;

            // A preset board configuration for each game (optional)
            string BoardConfiguration;

            // Handle border case of missing either of required parameters or sending in invalid or too-many parameters.
            if (args.Length > 3 || args.Length < 2)
            {
                Console.WriteLine("You must provide 2 or 3 parameters.");
                return;
            }

            int time;
            // Get initial values that were passed in.
            if (int.TryParse(args[0], out time))
            {
                if (time < 0)
                {
                    Console.WriteLine("Game time error: the first argument must be a positive integer.");
                    return;
                }
                GameTime = time;
            }
            else
            {
                Console.WriteLine("Game time error: the first argument must be a positive integer.");
                return;
            }

            // Parse and store input file as the dictionary
            try
            {
                Dictionary = new HashSet<string>(System.IO.File.ReadAllLines(args[1]));
            }
            catch
            {
                Console.WriteLine("Dictionary error: the second parameter must be a path to a text file storing one word per line.");
                return;
            }

            // Store the optional board.
            BoardConfiguration = "";
            if (args.Length == 3)
            {
                if (Regex.IsMatch(args[2], @"^[A-Za-z]{16}$"))
                {
                    BoardConfiguration = args[2];
                }
                else
                {
                    Console.WriteLine("Optional parameter error: if a third parameter is included, it must be contain exactly 16 letters.");
                }
            }

            // Start a Boggle server.
            new BoggleServer(GameTime, Dictionary, BoardConfiguration);

            // Start 1st Boggle client
            System.Diagnostics.Process client1 = new System.Diagnostics.Process();
            client1.StartInfo.FileName = @"..\..\..\BoggleClient\bin\Debug\BoggleClient.exe";
            client1.Start();

            // Start 2nd Boggle client
            System.Diagnostics.Process client2 = new System.Diagnostics.Process();
            client2.StartInfo.FileName = @"..\..\..\BoggleClient\bin\Debug\BoggleClient.exe";
            client2.Start();          
        }
    }
}
