using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BoggleClient
{
    /// <summary>
    /// This class creates a Boggle-playing client that can connect to
    /// a Boggle server and play the game.
    /// </summary>
    public partial class BoggleClientController : Window
    {
        // The Model in the MVC format
        private BoggleClientModel _boggleClientModel;

        /// <summary>
        /// Creates a new BoggleClient.
        /// </summary>
        public BoggleClientController()
        {
            InitializeComponent();
            _boggleClientModel = new BoggleClientModel();
            _boggleClientModel.IncomingLineEvent += MessageReceived;
        }

        /// <summary>
        /// Called when "Connect" is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            // If already connected, inform the user and return
            if (!ReferenceEquals(_boggleClientModel._stringSocket, null))
            {
                PrintLine("You are already connected to a server.");
                return;
            }

            // If IP Address is left blank, inform user and return
            if (textBoxIPAddress.Text == "")
            {
                PrintLine("You must enter an IP Address.");
                return;
            }

            // If Port is left blank, inform user and return
            if (textBoxPort.Text == "")
            {
                PrintLine("You must enter a port number.");
                return;
            }

            // If Port is not a number, inform user and return
            int port;
            if (!int.TryParse(textBoxPort.Text, out port))
            {
                PrintLine("The port must be an integer.");
                return;
            }

            // Disable connect button while attempting connection
            buttonConnect.IsEnabled = false;

            // Attempt connection
            try
            {
                _boggleClientModel.Connect(textBoxIPAddress.Text, port);
            }
            catch
            {
                // Inform user connection failed and return
                PrintLine("Unable to connect to that server and port.");
                buttonConnect.IsEnabled = true;
                return;
            }

            // Inform user connection was successful
            PrintLine("Connected.");

            // Reset score to 0
            textBoxYourScore.Dispatcher.Invoke(new Action(() => { textBoxYourScore.Text = "0"; }));
            textBoxOpponentScore.Dispatcher.Invoke(new Action(() => { textBoxOpponentScore.Text = "0"; }));

            // Enable button now that connection is complete
            buttonConnect.IsEnabled = true;
        }

        /// <summary>
        /// Called when "Disconnect" is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            // If a connection hasn't been established, inform the user and return
            if (ReferenceEquals(_boggleClientModel._stringSocket, null))
            {
                PrintLine("You are not connected to a server.");
                return;
            }

            // Disconnect from server
            _boggleClientModel.Disconnect();

            // Print a message
            PrintLine("Disconnected.");
        }
        /// <summary>
        /// Called when "Send Command" is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSendCommand_Click(object sender, RoutedEventArgs e)
        {
            // If a connection hasn't been established, inform the user and return
            if (ReferenceEquals(_boggleClientModel._stringSocket, null))
            {
                PrintLine("You cannot send commands until you connect to a server.");
                return;
            }

            // If the message to send is left blank, inform the user and return
            if (textBoxSendCommand.Text == "")
            {
                PrintLine("You must enter a command before it can be sent.");
                return;
            }

            // Send a string to the server
            _boggleClientModel.SendCommand(textBoxSendCommand.Text);
            textBoxSendCommand.Text = "";
        }

        /// <summary>
        /// Called when "Send Word" is clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSendWord_Click(object sender, RoutedEventArgs e)
        {
            // If a connection hasn't been established, inform the user and return
            if (ReferenceEquals(_boggleClientModel._stringSocket, null))
            {
                PrintLine("You cannot send words until you connect to a server.");
                return;
            }

            // If the message to send is left blank, inform the user and return
            if (textBoxSendWord.Text == "")
            {
                PrintLine("You must enter a word before it can be sent.");
            }

            // Send a string to the server
            _boggleClientModel.SendCommand("WORD " + textBoxSendWord.Text);
            textBoxSendWord.Text = "";
        }

        /// <summary>
        /// Called when the enter key is pressed while entering a word.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxSendWord_KeyDown(object sender, KeyEventArgs e)
        {
            // Test for enter key
            if (e.Key == Key.Enter)
            {
                // If a connection hasn't been established, inform the user and return
                if (ReferenceEquals(_boggleClientModel._stringSocket, null))
                {
                    PrintLine("You cannot send words until you connect to a server.");
                    return;
                }

                // If the message to send is left blank, inform the user and return
                if (textBoxSendWord.Text == "")
                {
                    PrintLine("You must enter a word before it can be sent.");
                }

                // Send a string to the server
                _boggleClientModel.SendCommand("WORD " + textBoxSendWord.Text);
                textBoxSendWord.Text = "";
            }
        }

        /// <summary>
        /// Deals with incoming lines. The input sent with commands are routed where they
        /// need to go. Any bad commands sent from the server are printed to the main
        /// message box for diagnostic purposes.
        /// </summary>
        /// <param name="processedLine">The line that was received</param>
        private void MessageReceived(String line)
        {
            // Exit method if string is null
            if (ReferenceEquals(line, null))
            {
                return;
            }

            // Split input into individual strings (using whitespace to delimit them) and remove all whitespace
            string[] command = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);

            // Test for TERMINATED command
            if (command.Length == 1)
            {
                if (command[0].ToLower() == "terminated")
                {
                    PrintLine("The server has terminated the game.");
                    PrintLine("Disconnected.");
                    return; // Valid TERMINATED command: exit method
                }

                // Invalid TERMINATED command: exit method
                PrintLine("The Server Sent an Invalid Command: " + line);
                return;
            }

            // Test for TIME command
            if (command.Length == 2)
            {
                // Seconds remaining in the game, sent by server
                int gameTime;

                if (command[0].ToLower() == "time" && int.TryParse(command[1], out gameTime))
                {
                    // Refresh display of time remaining in game
                    textBoxTime.Dispatcher.Invoke(new Action(() => { textBoxTime.Text = gameTime.ToString(); }));
                    return; // Valid TIME command: exit method
                }

                // Invalid TIME command: exit method
                PrintLine("The Server Sent an Invalid Command: " + line);
                return;
            }

            // Test for SCORE command
            if (command.Length == 3)
            {
                if (command[0].ToLower() == "score")
                {
                    int yourScore;
                    int opponentScore;

                    if (int.TryParse(command[1], out yourScore) && int.TryParse(command[2], out opponentScore))
                    {
                        textBoxYourScore.Dispatcher.Invoke(new Action(() => { textBoxYourScore.Text = yourScore.ToString(); }));                        
                        textBoxOpponentScore.Dispatcher.Invoke(new Action(() => { textBoxOpponentScore.Text = opponentScore.ToString(); }));
                        return; // Valid SCORE command: exit method       
                    }                                        
                }

                // Invalid SCORE command: exit method
                PrintLine("The Server Sent an Invalid Command: " + line);
                return;
            }

            // Test for START command
            if (command.Length == 4)
            {
                // The initial game time
                int initialGameTime;
                
                // A valid START command is composed of:
                // command[0]: the string "start" (case-insensitive)
                // command[1]: a string of 16 letters (the game board, case-insensitive)
                // command[2]: a positive integer (initial game time)
                // command[3]: a string (opponent's name, case-SENSITIVE)
                if (command[0].ToLower() == "start" && Regex.IsMatch(command[1].ToLower(), @"^[a-z]{16}$") && int.TryParse(command[2], out initialGameTime))
                {
                    // Make clear the parts of the command
                    string gameBoard = command[1].ToUpper(); // Board should be uppercase
                    string opponentName = command[3];

                    // Set up display of the board
                    label01.Dispatcher.Invoke(new Action(() => { label01.Content = gameBoard[0]; }));
                    label02.Dispatcher.Invoke(new Action(() => { label02.Content = gameBoard[1]; }));
                    label03.Dispatcher.Invoke(new Action(() => { label03.Content = gameBoard[2]; }));
                    label04.Dispatcher.Invoke(new Action(() => { label04.Content = gameBoard[3]; }));
                    label05.Dispatcher.Invoke(new Action(() => { label05.Content = gameBoard[4]; }));
                    label06.Dispatcher.Invoke(new Action(() => { label06.Content = gameBoard[5]; }));
                    label07.Dispatcher.Invoke(new Action(() => { label07.Content = gameBoard[6]; }));
                    label08.Dispatcher.Invoke(new Action(() => { label08.Content = gameBoard[7]; }));
                    label09.Dispatcher.Invoke(new Action(() => { label09.Content = gameBoard[8]; }));
                    label10.Dispatcher.Invoke(new Action(() => { label10.Content = gameBoard[9]; }));
                    label11.Dispatcher.Invoke(new Action(() => { label11.Content = gameBoard[10]; }));
                    label12.Dispatcher.Invoke(new Action(() => { label12.Content = gameBoard[11]; }));
                    label13.Dispatcher.Invoke(new Action(() => { label13.Content = gameBoard[12]; }));
                    label14.Dispatcher.Invoke(new Action(() => { label14.Content = gameBoard[13]; }));
                    label15.Dispatcher.Invoke(new Action(() => { label15.Content = gameBoard[14]; }));
                    label16.Dispatcher.Invoke(new Action(() => { label16.Content = gameBoard[15]; }));

                    // Set display of opponent's name
                    textBoxOpponent.Dispatcher.Invoke(new Action(() => { textBoxOpponent.Text = opponentName; }));

                    // Set up display of initial time
                    textBoxTime.Dispatcher.Invoke(new Action(() => { textBoxTime.Text = initialGameTime.ToString(); }));

                    return; // Valid START command: exit method
                }

                // Invalid START command: exit method
                PrintLine("The Server Sent an Invalid Command: " + line);
                return;
            }

            // Test for STOP command
            if (command[0].ToLower() == "stop" && command.Length > 5)
            {
                // Recreate these variables that came from the server
                int countPlayerUniqueLegalWords = 0;
                int countOpponentUniqueLegalWords = 0;
                int countLegalWordsInCommon = 0;
                int countPlayerIllegalWords = 0;
                int countOpponentIllegalWords = 0;

                // Recreate these variables that came from the server
                // Don't use a HashSet because: if the server repeats words in the same set (it shouldn't),
                // the output will help diagnose that the server is using a list instead of a set
                Queue<string> playerUniqueLegalWords = new Queue<string>();
                Queue<string> opponentUniqueLegalWords = new Queue<string>();
                Queue<string> legalWordsInCommon = new Queue<string>();
                Queue<string> playerIllegalWords = new Queue<string>();
                Queue<string> opponentIllegalWords = new Queue<string>();
                
                // Keep track of this, there should be exactly 5 LEGITIMATE numbers in the stop command
                int amountOfNumbersInStopCommandSoFar = 0;

                // Get an enumerator for the "words" in the command
                IEnumerator enumerator = command.GetEnumerator();
                enumerator.MoveNext(); // We'll be testing for ints now, so skip the STOP string

                // Stores any one of the five numbers in the STOP command
                int number;

                // Manually iterate over the string array and count how many LEGITIMATE numbers are in the STOP command
                // REMINDER: words in between numbers must be skipped
                // Example of valid command: "STOP" "3" "hello" "apple" "dog" 1 "jam" 0 0 0
                // Example of invalid command: "STOP" "3" "hello" "apple" "1" "jam" 0 0 0
                // This is important because the invalid example cannot be put back together correctly
                while (enumerator.MoveNext())
                {
                    // Check this item in the string array and see if it parses as a number
                    if (int.TryParse((string)enumerator.Current, out number))
                    {
                        // Keep track of which number this is
                        amountOfNumbersInStopCommandSoFar++;

                        // Recreate the sets of strings
                        // Recreate the numbers that tells how many words are in each set of strings
                        // Exit method with error if not enough words after a number
                        switch (amountOfNumbersInStopCommandSoFar)
                        {
                            case 1:
                                countPlayerUniqueLegalWords = number;
                                for (int i = 0; i < number; i++)
                                {                                    
                                    if (enumerator.MoveNext() == false)
                                    {
                                        // Invalid STOP command: exit method
                                        PrintLine("The Server Sent an Invalid Command: " + line);
                                        return;
                                    }
                                    playerUniqueLegalWords.Enqueue((string)enumerator.Current);
                                }
                                break;
                            case 2:
                                countOpponentUniqueLegalWords = number;
                                for (int i = 0; i < number; i++)
                                {
                                    if (enumerator.MoveNext() == false)
                                    {
                                        // Invalid STOP command: exit method
                                        PrintLine("The Server Sent an Invalid Command: " + line);
                                        return;
                                    }
                                    opponentUniqueLegalWords.Enqueue((string)enumerator.Current);                                    
                                }                                    
                                break;
                            case 3:
                                countLegalWordsInCommon = number;
                                for (int i = 0; i < number; i++)
                                {
                                    if (enumerator.MoveNext() == false)
                                    {
                                        // Invalid STOP command: exit method
                                        PrintLine("The Server Sent an Invalid Command: " + line);
                                        return;
                                    }
                                    legalWordsInCommon.Enqueue((string)enumerator.Current);
                                }
                                break;
                            case 4:
                                countPlayerIllegalWords = number;
                                for (int i = 0; i < number; i++)
                                {
                                    if (enumerator.MoveNext() == false)
                                    {
                                        // Invalid STOP command: exit method
                                        PrintLine("The Server Sent an Invalid Command: " + line);
                                        return;
                                    }
                                    playerIllegalWords.Enqueue((string)enumerator.Current);
                                }
                                break;
                            case 5:
                                countOpponentIllegalWords = number;
                                for (int i = 0; i < number; i++)
                                {
                                    if (enumerator.MoveNext() == false)
                                    {
                                        // Invalid STOP command: exit method
                                        PrintLine("The Server Sent an Invalid Command: " + line);
                                        return;
                                    }
                                    opponentIllegalWords.Enqueue((string)enumerator.Current);
                                }
                                break;
                        }            
                    }
                    else
                    {
                        // Invalid STOP command (found a non-int while iterating through ints): exit method
                        PrintLine("The Server Sent an Invalid Command: " + line);
                        return;
                    }                    
                }

                PrintLine(string.Format("You found {0} unique words: {1}", countPlayerUniqueLegalWords.ToString(), string.Join(" ", playerUniqueLegalWords)));
                PrintLine(string.Format("Your opponent found {0} unique words: {1}", countOpponentUniqueLegalWords.ToString(), string.Join(" ", opponentUniqueLegalWords)));
                PrintLine(string.Format("{0} words in common: {1}", countLegalWordsInCommon.ToString(), string.Join(" ", legalWordsInCommon)));
                PrintLine(string.Format("You found {0} invalid words: {1}", countPlayerIllegalWords.ToString(), string.Join(" ", playerIllegalWords)));
                PrintLine(string.Format("Your opponent found {0} invalid words: {1}", countOpponentIllegalWords.ToString(), string.Join(" ", opponentIllegalWords)));
                // Disconnect from server
                _boggleClientModel.Disconnect();
                return; // Valid STOP command: exit method
            }

            // If input matches no commands, display it as a message from the server for diagnostic purposes            
            PrintLine("The Server Sent an Invalid Command: " + line);
            return;
        }

        /// <summary>
        /// Prints a message to the user in the Messages box.
        /// </summary>
        /// <param name="messageToPrint"></param>
        private void PrintLine(string messageToPrint)
        {
            textBoxMessages.Dispatcher.Invoke(new Action(() => { textBoxMessages.AppendText(messageToPrint + "\r\n"); textBoxMessages.ScrollToEnd(); }));
        }
    }
}
