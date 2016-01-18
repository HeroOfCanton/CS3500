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
using MySql.Data.MySqlClient;

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
        private TcpListener _gameServer;
        private TcpListener _webServer;

        // The player waiting for a match.
        private Player _waitingPlayer;

        // The collection of all the matches.
        private HashSet<Match> _allMatches;

        // The port to be used.
        private readonly int _GAMEPORT;
        private readonly int _WEBPORT;

        // The lock to protect local variables.
        private readonly object _lock;

        // The variables from the command line.    
        int _gameTime;
        private HashSet<string> _dictionary;
        private string _boardConfiguration;

        /// <summary>
        /// The database connection string.
        /// </summary>
        public const string connectionString = "server=atr.eng.utah.edu;database=cs3500_welling;uid=cs3500_welling;password=865963469";

        /// <summary>
        /// Creates a BoggleServer that listens for connections on port 2000.
        /// </summary>
        public BoggleServer(int gameTime, HashSet<string> dictionary, string boardConfiguration)
        {
            // Initialize fields.
            _GAMEPORT = 2000;
            _WEBPORT = 2500;
            _gameServer = new TcpListener(IPAddress.Any, _GAMEPORT);
            _webServer = new TcpListener(IPAddress.Any, _WEBPORT);
            _waitingPlayer = null;
            _allMatches = new HashSet<Match>();
            _lock = new object();
            _gameTime = gameTime;
            _dictionary = dictionary;
            _boardConfiguration = boardConfiguration;

            // Start the servers
            _gameServer.Start();
            _webServer.Start();

            // Start listening for clients to connect
            _gameServer.BeginAcceptSocket(ConnectionReceived, null);
            _webServer.BeginAcceptSocket(WebpageRequested, null);

            // Create a timer with a one second interval
            timer = new System.Timers.Timer(1000);

            // Hook up the Elapsed event for the timer
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
        /// Handles requests for web pages.
        /// </summary>
        private void WebpageRequested(IAsyncResult ar)
        {
            // A client connected. Create a socket to communicate with it
            Socket socket = _webServer.EndAcceptSocket(ar);

            // Wrap the socket into a Stringsocket to communicate with it
            StringSocket ss = new StringSocket(socket, UTF8Encoding.Default);

            // Wait for the client to send us a string
            ss.BeginReceive(ProcessRequest, ss);

            // Start listening for another client to connect
            _webServer.BeginAcceptSocket(WebpageRequested, null);
        }

        /// <summary>
        /// Handles connection requests.
        /// </summary>
        private void ConnectionReceived(IAsyncResult ar)
        {
            // A client connected. Create a socket to communicate with it
            Socket socket = _gameServer.EndAcceptSocket(ar);

            // Wrap the socket into a StringSocket to communicate with it
            StringSocket ss = new StringSocket(socket, UTF8Encoding.Default);

            // Wait for the client to send us a string
            ss.BeginReceive(ReadyToPlay, ss);

            // Start listening for another client to connect
            _gameServer.BeginAcceptSocket(ConnectionReceived, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="e"></param>
        /// <param name="payload"></param>
        private void ProcessRequest(string line, Exception e, object payload)
        {
            // Make sure socket hasn't closed or thrown an exception
            if (ReferenceEquals(line, null) && ReferenceEquals(e, null) || !ReferenceEquals(e, null))
            {
                return;
            }

            // The StringSocket to talk to this client.
            StringSocket ss = (StringSocket)payload;

            // Lowercase the line so we can process requests case-insensitively
            line = line.ToLower();

            // Remove whitespace from the end of the string that browsers seem to tack on and I can't seem to do a match for
            line = line.Trim();

            // Send valid HTTP connection message
            ss.BeginSend("HTTP/1.1 200 OK\r\n", (ee, pp) => { }, null);
            ss.BeginSend("Connection: close\r\n", (ee, pp) => { }, null);
            ss.BeginSend("Content-Type: text/html; charset=UTF-8\r\n", (ee, pp) => { }, null);
            ss.BeginSend("\r\n", (ee, pp) => { }, null);

            // Upon receiving "GET /players HTTP/1.1" the server will send back an HTML web page containing a table of information 
            // reporting all games played. The table should have one row for each player in the database and four columns. 
            // Each row should consist of the player's name, the number of games won by the player, the number of games lost by the player, 
            // and the number of games tied by the player. 
            if (line == "get /players http/1.1" || line == "get /players/ http/1.1")
            {
                PlayersTable pt = new PlayersTable();

                // Connect to the DB
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        // Open a connection
                        conn.Open();

                        // Create a command
                        MySqlCommand command = conn.CreateCommand();
                        command.CommandText = "SELECT * from Players";

                        // Execute the command and cycle through the DataReader object
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                pt.name.Add(reader["Name"].ToString());
                                pt.won.Add((int)reader["Won"]);
                                pt.tied.Add((int)reader["Tied"]);
                                pt.lost.Add((int)reader["Lost"]);
                            }
                        }
                    }
                    catch (Exception f)
                    {
                        Console.WriteLine(f.Message);
                    }
                }
                // Start sending data back to web page as HTML table
                ss.BeginSend("All Games Played: " +
                            "<table width=\"100%\" border=\"2\">" +
                            "<tr>" +
                            "<td>Player Name</td>" +
                            "<td>Games Won</td>" +
                            "<td>Games Tied</td>" +
                            "<td>Games Lost</td>" +
                            "</tr>" +

                            "<tr>", (ee, pp) => { }, null);

                for (int i = 0; i < pt.name.Count; i++)
                {
                    ss.BeginSend("<tr>" +
                        string.Format("<td><a href=\"/games?player={0}\">{0}</a></td>", pt.name[i]) +
                        string.Format("<td>{0}</td>", pt.won[i]) +
                        string.Format("<td>{0}</td>", pt.tied[i]) +
                        string.Format("<td>{0}</td>", pt.lost[i]) +
                        "</tr>", (ee, pp) => { }, null);
                }
                // Link to all Players and all Games
                ss.BeginSend("</table>" +
                    "<p>" +
                    "<a href=\"/players\">List of all Players</a>" +
                    "</p>" +
                    "<p>" +
                    "<a href=\"/games\">List of all Games</a>" +
                    "</p>", (ee, pp) => { }, null); 
            }

            // Upon receiving "GET /games?player=Joe HTTP/1.1" the server will send back an HTML web page containing 
            // a table of information reporting all games by the player "Joe". There should be one row for each 
            // game played by the player named in the line of text (e.g., "Joe" in the example above) and six columns. 
            // Each row should consist of a number that uniquely identifies the game (see the next paragraph for how that number will be used), 
            // the date and time when the game was played, the name of the opponent, the score for the named player, and the score for the opponent.
            else if (Regex.IsMatch(line, @"^get /games\?player=[a-z0-9_]{1,20} http/1\.1")) // Player names are capped at 20 max (see Player class)
            {
                GamesTable gt = new GamesTable();
                bool player1 = false;
              
                // Extract the player's name from the line
                string playerName = line.Substring(18, line.Length - 27);

                // Connect to the DB
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        // Open a connection
                        conn.Open();

                        // Create a command
                        MySqlCommand command = conn.CreateCommand();

                        // Check to see if player exists in Player1 field or Player2 field
                        command.CommandText = "SELECT Player1 from Games WHERE Player1 = '" + playerName + "'";
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                player1 = true;
                            }
                        }

                        // If player exists in Player1 field, get opponents data
                        if (player1)
                        {
                            command.CommandText = "SELECT GameID, DateTime, Player2, Player1Score, Player2Score from Games WHERE Player1 = '" + playerName + "'";
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    gt.gameID.Add((int)reader["GameID"]);
                                    gt.dateTime.Add(reader["DateTime"].ToString());
                                    gt.player2.Add(reader["Player2"].ToString());
                                    gt.player1Score.Add((int)reader["Player1Score"]);
                                    gt.player2Score.Add((int)reader["Player2Score"]);
                                }
                            }
                        }
                        // Player must be in Player2 field
                        else
                        {
                            command.CommandText = "SELECT GameID, DateTime, Player1, Player1Score, Player2Score from Games WHERE Player2 = '" + playerName + "'";
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    gt.gameID.Add((int)reader["GameID"]);
                                    gt.dateTime.Add(reader["DateTime"].ToString());
                                    gt.player1.Add(reader["Player1"].ToString());
                                    gt.player1Score.Add((int)reader["Player1Score"]);
                                    gt.player2Score.Add((int)reader["Player2Score"]);
                                }
                            }
                        }
                    }
                    catch (Exception f)
                    {
                        Console.WriteLine(f.Message);
                    }
                }

                ss.BeginSend(string.Format("<p>Player {0} Stats:</p>", playerName) +
                        "<table width=\"100%\" border=\"2\">" +
                            "<tr>"+
                            "<td>Game ID</td>" +
                            "<td>Date & Time</td>" +
                            "<td>Opponent Name</td>" +
                            "<td>Your Score</td>" +
                            "<td>Opponent Score</td>" +
                            "</tr>" , (ee, pp) => { }, null);

                // If the player is in Player1 field, post data on Player2 as opponent
                if (player1)
                {
                    for (int i = 0; i < gt.gameID.Count; i++)
                    {
                        ss.BeginSend("<tr>" +
                            string.Format("<td><a href=\"/game?id={0}\">{0}</a></td>", gt.gameID[i]) +
                            string.Format("<td>{0}</td>", gt.dateTime[i]) +
                            string.Format("<td><a href=\"/games?player={0}\">{0}</a></td>", gt.player2[i]) +
                            string.Format("<td>{0}</td>", gt.player1Score[i]) +
                            string.Format("<td>{0}</td>", gt.player2Score[i]) +
                            "</tr>", (ee, pp) => { }, null);
                    }
                }
                // Player is in Player2 field, post data on Player1 as opponent
                else
                {
                    for (int i = 0; i < gt.gameID.Count; i++)
                    {
                        ss.BeginSend("<tr>" +
                            string.Format("<td><a href=\"/game?id={0}\">{0}</a></td>", gt.gameID[i]) +
                            string.Format("<td>{0}</td>", gt.dateTime[i]) +
                            string.Format("<td><a href=\"/games?player={0}\">{0}</a></td>", gt.player1[i]) +
                            string.Format("<td>{0}</td>", gt.player2Score[i]) +
                            string.Format("<td>{0}</td>", gt.player1Score[i]) +
                            "</tr>", (ee, pp) => { }, null);
                    }
                }
                // Link to all Players and all Games
                ss.BeginSend("</table>" +
                    "<p>" +
                    "<a href=\"/players\">List of all Players</a>" +
                    "</p>" +
                    "<p>" +
                    "<a href=\"/games\">List of all Games</a>" +
                    "</p>", (ee, pp) => { }, null);  

            }
            // Upon receiving "GET /game?id=35 HTTP/1.1" the server should send back an HTML page containing information 
            // about the specified game (e.g., 35 in this example). The page should contain the names and scores of 
            // the two players involved, the date and time when the game was played, a 4x4 table containing the Boggle board that was used, 
            // the time limit that was used for the game, and the five-part word summary.
            else if (Regex.IsMatch(line, @"get /game\?id=[1-9][0-9]{0,11} http/1\.1")) // Put a cap on game numbers (11 integers long)
            {                
                GamesTable gt = new GamesTable();
                WordsTable wt = new WordsTable();

                // Extract the gameID from the line
                string gameIDAsString = line.Substring(13, line.Length-22);
                int gameID;
                int.TryParse(gameIDAsString, out gameID); // This cannot fail because of the Regex above (0-11 ints long)

                // Connect to the DB
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        // Open a connection
                        conn.Open();

                        // Create a command
                        MySqlCommand command = conn.CreateCommand();
                        command.CommandText = "SELECT Player1, Player2, Player1Score, Player2Score, DateTime, Board, TimeLimit, Word, Status " +
                                              "FROM Games, Words WHERE Games.GameID = " + gameID + " AND Words.Game = " + gameID;

                        gt.gameID.Add(gameID);

                        // Execute the command and cycle through the DataReader object
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                gt.player1.Add(reader["Player1"].ToString());
                                gt.player2.Add(reader["Player2"].ToString());
                                gt.player1Score.Add((int)reader["Player1Score"]);
                                gt.player2Score.Add((int)reader["Player2Score"]);
                                gt.dateTime.Add(reader["DateTime"].ToString());
                                gt.board = reader["Board"].ToString();
                                gt.timeLimit = (int)reader["TimeLimit"];
                                wt.word.Add(reader["Word"].ToString());
                                wt.status.Add(reader["Status"].ToString());
                            }
                        }
                    }
                    catch (Exception f)
                    {
                        Console.WriteLine(f.Message);
                    }
                }

                // Send game stats
                ss.BeginSend(string.Format("<p>Game {0} Stats:</p>", gameID) +
                        "<table width=\"100%\" border=\"2\">" +
                            "<tr>" +
                            "<td>Player 1 Name</td>" +
                            "<td>Player 2 Name</td>" +
                            "<td>Player 1 Score</td>" +
                            "<td>Player 2 Score</td>" +
                            "<td>Date & Time</td>" +
                            "<td>Time Limit</td>" +
                            "</tr>", (ee, pp) => { }, null);

                ss.BeginSend("<tr>" +
                        string.Format("<td><a href=\"/games?player={0}\">{0}</a></td>", gt.player1[0]) +
                        string.Format("<td><a href=\"/games?player={0}\">{0}</a></td>", gt.player2[0]) + 
                        string.Format("<td>{0}</td>", gt.player1Score[0]) +
                        string.Format("<td>{0}</td>", gt.player2Score[0]) +
                        string.Format("<td>{0}</td>", gt.dateTime[0]) +
                        string.Format("<td>{0}</td>", gt.timeLimit) +
                        "</tr>" +
                        "</table>", (ee, pp) => { }, null);


                // Send board configuration as a 4x4 table
                ss.BeginSend("<p>Board Configuration:</p>" +
                        "<table width=\"10%\" border=\"2\">", (ee, pp) => { }, null);
                // first row
                string board1 = gt.board.Substring(0, 4);
                ss.BeginSend("<tr>", (ee, pp) => { }, null);
                for(int i = 0; i < 4; i++) 
                {
                    ss.BeginSend(string.Format("<td>{0}</td>", board1[i]), (ee, pp) => { }, null);
                }
                ss.BeginSend("</tr>", (ee, pp) => { }, null);
                
                // second row
                string board2 = gt.board.Substring(4, 4);
                ss.BeginSend("<tr>", (ee, pp) => { }, null);
                for (int i = 0; i < 4; i++)
                {
                    ss.BeginSend(string.Format("<td>{0}</td>", board2[i]), (ee, pp) => { }, null);
                }
                ss.BeginSend("</tr>", (ee, pp) => { }, null);

                // third row
                string board3 = gt.board.Substring(8, 4);
                ss.BeginSend("<tr>", (ee, pp) => { }, null);
                for (int i = 0; i < 4; i++)
                {
                    ss.BeginSend(string.Format("<td>{0}</td>", board3[i]), (ee, pp) => { }, null);
                }
                ss.BeginSend("</tr>", (ee, pp) => { }, null);

                // fourth row
                string board4 = gt.board.Substring(12, 4);
                ss.BeginSend("<tr>", (ee, pp) => { }, null);
                for (int i = 0; i < 4; i++)
                {
                    ss.BeginSend(string.Format("<td>{0}</td>", board4[i]), (ee, pp) => { }, null);
                }
                ss.BeginSend("</tr>", (ee, pp) => { }, null); 
                ss.BeginSend("</table>", (ee, pp) => { }, null);

                // Send word summary
                ss.BeginSend("<p>Word Summary</p>" +
                        "<table width=\"15%\" border=\"2\">", (ee, pp) => { }, null);

                for (int i = 0; i < wt.word.Count; i++)
                {
                    ss.BeginSend("<tr>"+
                            string.Format("<td>{0}</td>", wt.word[i]) +
                            string.Format("<td>{0}</td>", wt.status[i])
                            , (ee, pp) => { }, null);
                }
                // Link to all Players and all Games
                ss.BeginSend("</table>" +
                    "<p>" +
                    "<a href=\"/players\">List of all Players</a>" +
                    "</p>"+
                    "<p>" +
                    "<a href=\"/games\">List of all Games</a>" +
                    "</p>", (ee, pp) => { }, null);                  
            }
            else if (line == "get /games http/1.1" || line == "get /games/ http/1.1") 
            {
                GamesTable gt = new GamesTable();

                // Connect to the DB
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        // Open a connection
                        conn.Open();

                        // Create a command
                        MySqlCommand command = conn.CreateCommand();
                        command.CommandText = "SELECT GameID, Player1, Player2, DateTime FROM Games";

                        // Execute the command and cycle through the DataReader object
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                gt.gameID.Add((int)reader["GameID"]);
                                gt.player1.Add(reader["Player1"].ToString());
                                gt.player2.Add(reader["Player2"].ToString());
                                gt.dateTime.Add(reader["DateTime"].ToString());
                            }
                        }
                    }
                    catch (Exception f)
                    {
                        Console.WriteLine(f.Message);
                    }
                }

                // Send game stats
                ss.BeginSend("<p>List of all Games in Database:</p>" +
                        "<table width=\"50%\" border=\"2\">" +
                            "<tr>" +
                            "<td>Game ID</td>" +
                            "<td>Player 1 Name</td>" +
                            "<td>Player 2 Name</td>" +
                            "<td>Date & Time</td>" +
                            "</tr>", (ee, pp) => { }, null);

                for (int i = 0; i < gt.player1.Count; i++)
                {
                    ss.BeginSend("<tr>" +
                        string.Format("<td><a href=\"/game?id={0}\">{0}</a></td>", gt.gameID[i]) +
                        string.Format("<td><a href=\"/games?player={0}\">{0}</a></td>", gt.player1[i]) +
                        string.Format("<td><a href=\"/games?player={0}\">{0}</a></td>", gt.player2[i]) +
                        string.Format("<td>{0}</td>", gt.dateTime[i]) +
                        "</tr>", (ee, pp) => { }, null);
                }
                // Link to all Players and all Games
                ss.BeginSend("</table>" +
                    "<p>" +
                    "<a href=\"/players\">List of all Players</a>" +
                    "</p>", (ee, pp) => { }, null); 
            }
            // If the first line of text sent by the browser to the server is anything else, the server should send back an HTML page 
            // containing an error message. The error message should be meaningful and contain a summary of valid options.
            else
            {
                // The line below this one is a generic placeholder for what needs to be sent
                ss.BeginSend(string.Format("<p>ERROR, you sent: [{0}]</p><p>The only valid commands are /players, /games?player=[player name], and /game?id=[integer]</p>", line), (ee, pp) => { }, null);
            }

            // Close the StringSocket
            ss.Close();
        }

        /// <summary>
        /// Handles claims from players that they are ready to play.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="e"></param>
        /// <param name="payload"></param>
        private void ReadyToPlay(string line, Exception e, object payload)
        {
            // Make sure socket hasn't closed or thrown an exception
            if (ReferenceEquals(line, null) && ReferenceEquals(e, null) || !ReferenceEquals(e, null))
            {
                return;
            }

            // The StringSocket to talk to this client.
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

                // Remove any /r from the end of the player's name
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
                // Cap player names at 20 characters
                if (name.Length > 20)
                {
                    name = name.Substring(0, 20);
                }

                // Initialize variables
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
            // A flag for hack-fixing a bug: Players' scores are being recorded multiple times by multiple threads
            // Trip this boolean to prevent it from happening multiple times
            private bool _endMatchMethodHasExecuted = false;
            private object _lock = new Object(); // A lock for the hack-around

            // The dictionary of words.
            public HashSet<string> _dictionary;

            // Boolean that gets tripped if the match is prematurely terminated
            public bool _isTerminated = false;

            // The game's start time (for writing to database)
            public int _timeLimit;

            // The game's board (for writing to database)
            public string _board;

            // The game's date and time (for writing to database)
            public string _dateTime;

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
                _timeLimit = startTime;
                _board = Board.ToString();
                _dateTime = (DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss");
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
            /// Increments a player's wins, ties, or losses in the database. If the player isn't in the 
            /// database, this method instead adds the player and records one win, loss or draw.
            /// Also records all statistics of the match. Also records all words used in the match.
            /// </summary>
            private void RecordMatchResults()
            {
                // Connect to database
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    try
                    {
                        // Open a connection
                        connection.Open();

                        // Create a command
                        MySqlCommand command = connection.CreateCommand();

                        // Write each player's results (Won, Lost, Tied) to the database
                        RecordScore(command);

                        // Write the match's details to the database                        
                        command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Games` (`Player1`, `Player2`, `Player1Score`, `Player2Score`, `DateTime`, `Board`, `TimeLimit`) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}');", Player1.Name, Player2.Name, Player1.Score, Player2.Score, _dateTime, _board, _timeLimit);
                        command.ExecuteNonQuery();
                        
                        // Get this match's ID that was just created by the database during the last insert
                        command.CommandText = "SELECT LAST_INSERT_ID();";
                        string GameID = command.ExecuteScalar().ToString(); // The ID of the match that was just inserted

                        // Record the words played during this match
                        RecordWords(command, GameID);
                    }
                    catch
                    {
                        // Should only reach this code if something goes wrong with accessing the database
                        // All MySQL commands should succeed in writing to the database,
                        // either in the TRY or the CATCH portion of a try/catch block
                    }
                }
            }
           
            /// <summary>
            /// Adds the players to the database if they're not there yet and increments whether
            /// they Won, Lost, or Tied.
            /// </summary>
            /// <param name="command"></param>
            private void RecordScore(MySqlCommand command)
            {
                // Each player's outcome
                string player1Outcome;
                string player2Outcome;

                // Determine the outcome of each player based on their scores
                if (Player1.Score > Player2.Score)
                {
                    player1Outcome = "Won";
                    player2Outcome = "Lost";                   
                }
                else if (Player1.Score < Player2.Score)
                {
                    player1Outcome = "Lost";
                    player2Outcome = "Won";                   
                }
                else
                {
                    player1Outcome = "Tied";
                    player2Outcome = "Tied";   
                }

                // Set command's text to add Player1's entry
                switch (player1Outcome)
                {
                    case "Won":
                        command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Players` (`Name`, `Won`, `Tied`, `Lost`) VALUES ('{0}', '1', '0', '0');", Player1.Name);
                        break;
                    case "Tied":
                        command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Players` (`Name`, `Won`, `Tied`, `Lost`) VALUES ('{0}', '0', '1', '0');", Player1.Name);
                        break;
                    case "Lost":
                        command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Players` (`Name`, `Won`, `Tied`, `Lost`) VALUES ('{0}', '0', '0', '1');", Player1.Name);
                        break;
                }

                // Attempt to execute the command to add Player1's entry
                try
                {
                    command.ExecuteNonQuery();
                }
                // If adding Player1's entry fails, then increment the entry's wins, ties, or losses instead
                catch
                {
                    command.CommandText = string.Format("UPDATE `cs3500_welling`.`Players` SET `{0}` = `{0}` + 1 WHERE `Name`='{1}';", player1Outcome, Player1.Name);
                    command.ExecuteNonQuery();
                }

                // Set command's text to add Player2's entry
                switch (player2Outcome)
                {
                    case "Won":
                        command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Players` (`Name`, `Won`, `Tied`, `Lost`) VALUES ('{0}', '1', '0', '0');", Player2.Name);
                        break;
                    case "Tied":
                        command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Players` (`Name`, `Won`, `Tied`, `Lost`) VALUES ('{0}', '0', '1', '0');", Player2.Name);
                        break;
                    case "Lost":
                        command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Players` (`Name`, `Won`, `Tied`, `Lost`) VALUES ('{0}', '0', '0', '1');", Player2.Name);
                        break;
                }

                // Attempt to execute the command to add Player2's entry
                try
                {
                    command.ExecuteNonQuery();
                }
                // If adding Player2's entry fails, then increment the entry's wins, ties, or losses instead
                catch
                {
                    command.CommandText = string.Format("UPDATE `cs3500_welling`.`Players` SET `{0}` = `{0}` + 1 WHERE `Name`='{1}';", player2Outcome, Player2.Name);
                    command.ExecuteNonQuery();
                }
            }
            
            /// <summary>
            /// Writes each word from the match into the database along with its legality and
            /// who played it.
            /// </summary>
            private void RecordWords(MySqlCommand command, string GameID)
            {
                foreach (string word in Player1.UniqueLegalWords)
                {
                    // Add the word to the database
                    command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Words` (`Word`, `Player`, `Game`, `Status`) VALUES ('{0}', '{1}', '{2}', 'unique');", word, Player1.Name, GameID);
                    command.ExecuteNonQuery();
                }
                foreach (string word in Player1.IllegalWords)
                {
                    // Add the word to the database
                    command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Words` (`Word`, `Player`, `Game`, `Status`) VALUES ('{0}', '{1}', '{2}', 'illegal');", word, Player1.Name, GameID);
                    command.ExecuteNonQuery();
                }
                foreach (string word in LegalWordsInCommon)
                {
                    // Add the word to the database
                    command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Words` (`Word`, `Player`, `Game`, `Status`) VALUES ('{0}', '{1}', '{2}', 'shared');", word, Player1.Name, GameID);
                    command.ExecuteNonQuery();
                    command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Words` (`Word`, `Player`, `Game`, `Status`) VALUES ('{0}', '{1}', '{2}', 'shared');", word, Player2.Name, GameID);
                    command.ExecuteNonQuery();
                }
                foreach (string word in Player2.UniqueLegalWords)
                {
                    // Add the word to the database
                    command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Words` (`Word`, `Player`, `Game`, `Status`) VALUES ('{0}', '{1}', '{2}', 'unique');", word, Player2.Name, GameID);
                    command.ExecuteNonQuery();
                }
                foreach (string word in Player2.IllegalWords)
                {
                    // Add the word to the database
                    command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Words` (`Word`, `Player`, `Game`, `Status`) VALUES ('{0}', '{1}', '{2}', 'illegal');", word, Player2.Name, GameID);
                    command.ExecuteNonQuery();
                }
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
                // A hack-around solution to a bug that somehow causes EndMatch() to execute multiple times by multiple threads
                // Remove this locked code once the bug is found and squashed
                lock(_lock)
                {
                    if (_endMatchMethodHasExecuted)
                    {
                        return;
                    }

                    _endMatchMethodHasExecuted = true;
                }                

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

                // Write data from this match to the database
                RecordMatchResults();

                // Close both sockets
                Player1.Socket.Close();
                Player2.Socket.Close();
            }
        }

        /// <summary>
        /// Class for holding data from DB Games table
        /// </summary>
        public class GamesTable
        {
            public List<int> gameID { get; set; }
            public List<string> player1 { get; set; }
            public List<string> player2 { get; set; }
            public List<int> player1Score { get; set; }
            public List<int> player2Score { get; set; }
            public List<string> dateTime { get; set; }
            public int timeLimit { get; set; }
            public string board { get; set; }

            public GamesTable()
            {
                gameID = new List<int>();
                player1 = new List<string>();
                player2 = new List<string>();
                player1Score = new List<int>();
                player2Score = new List<int>();
                dateTime = new List<string>();
                timeLimit = 0;
                board = "";
            }
        }

        /// <summary>
        /// Class for holding data from DB Players table
        /// </summary>
        public class PlayersTable
        {
            public List<string> name { get; set; }
            public List<int> won { get; set; }
            public List<int> tied { get; set; }
            public List<int> lost { get; set; }

            public PlayersTable()
            {
                name = new List<string>();
                won = new List<int>();
                tied = new List<int>();
                lost = new List<int>();
            }
        }

        /// <summary>
        /// Class for holding data from DB Words table
        /// </summary>
        public class WordsTable
        {
            public List<string> word { get; set; }
            public List<string> player { get; set; }
            public List<int> game { get; set; }
            public List<string> status { get; set; }

            public WordsTable()
            {
                word = new List<string>();
                player = new List<string>();
                game = new List<int>();
                status = new List<string>();
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
