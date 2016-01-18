using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SS;
using System.IO;
using SpreadsheetUtilities;
using System.Text.RegularExpressions;

namespace SpreadsheetGUI
{
    public partial class Form1 : Form
    {
        private Spreadsheet model;
        public Form1()
        {
            InitializeComponent();
            model = new Spreadsheet(s => Regex.IsMatch(s, @"[A-Z][1-9][0-9]?"), s => s.ToUpper(), "PS6");
            spreadsheetPanel1.SetSelection(0, 0);
        }

        private void spreadsheetPanel1_SelectionChanged(SS.SpreadsheetPanel panel)
        {
            // Column A = 0, Row 1 = 0 thus A1 = 00
            // use ascii value for columns, 65 + column value to get char then call toString
            // for rows, 1 + row value
            int row, col;
            panel.GetSelection(out col, out row);
            label1.Text = "Current Cell: " + ((char)(65 + col)).ToString() + (1 + row);
            label2.Text = "Cell Value: " + model.GetCellValue(getCell(col, row)).ToString();
            if (model.GetCellContents(getCell(col, row)) is Formula)
            {
                string content = "=" + model.GetCellContents(getCell(col, row)).ToString();
                textBox3.Text = content;
            }
            else
            {
                textBox3.Text = model.GetCellContents(getCell(col, row)).ToString();
            }
        }
        /// <summary>
        /// Handle 'New' from File Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Tell the application context to run the form on the same
            // thread as the other forms.
            DemoApplicationContext.getAppContext().RunForm(new Form1());
        }
        /// <summary>
        /// Handle 'Open' from File Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "sprd files (*.sprd)|*.sprd|All files (*.*)|*.*";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // get hashset of old spreadsheet so that we can add to it and erase old and set new cells
                HashSet<string> oldSpread = new HashSet<string> (model.GetNamesOfAllNonemptyCells());

                model = new Spreadsheet(openFileDialog1.FileName, s => Regex.IsMatch(s, @"[A-Z][1-9][0-9]?"), s => s.ToUpper(), "PS6");
                foreach (string cell in model.GetNamesOfAllNonemptyCells())
                {
                    oldSpread.Add(cell);
                }
                refresh(oldSpread);
            }          
        }
        /// <summary>
        /// Handle 'Exit' from File Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (model.Changed)
            {
                DialogResult dialogResult = MessageBox.Show("Do you want to save before closing?", 
                    "**Warning - Unsaved changes**", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    saveFileDialog1.Filter = "sprd files (*.sprd)|*.sprd|All files (*.*)|*.*";
                    saveFileDialog1.FilterIndex = 1;
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        model.Save(saveFileDialog1.FileName);
                    }
                }
                else if (dialogResult == DialogResult.No)
                {
                    Close();
                }
            }
            Close();
        }
        /// <summary>
        /// Override 'X' to close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.CloseReason == CloseReason.WindowsShutDown) return;
            if (model.Changed)
            {
                DialogResult dialogResult = MessageBox.Show(this, "Do you want to save before closing?",
                    "**Warning - Unsaved changes**", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    saveFileDialog1.Filter = "sprd files (*.sprd)|*.sprd|All files (*.*)|*.*";
                    saveFileDialog1.FilterIndex = 1;
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        model.Save(saveFileDialog1.FileName);
                    }
                }
            }
        }
        /// <summary>
        /// Handle 'Save' from File Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "sprd files (*.sprd)|*.sprd|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                model.Save(saveFileDialog1.FileName);
            }
        }
        /// <summary>
        /// Handle 'About' from File Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Ryan Welling" +
                            "\n11/5/2014" +
                            "\n" + 
                            "\nLike this program?  Donate bitcoin to keep improvements coming:" +
                            "\n15nZNqEP16wu9hQcDJ3mf3rqq6Ct8kno12");
        }
        /// <summary>
        /// Handle 'Extra' from File Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void extraFeaturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("When you press Enter, you are moved down a cell like in Excel" +
                            "\n" + 
                            "\nI also added shortcuts to most of the menu items");
        }
        /// <summary>
        /// Handle 'Help' from File Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Here are some handy helpers to make your spreadsheeting easier:" +
                            "\n" +
                            "\n- When typing contents in the box, you need to press ENTER to save it." +
                            "\n- Formulas recalculate automagically." +
                            "\n- Formulas must begin with an equals sign." +
                            "\n- The only valid operators are + - * / and ()" +
                            "\n" +
                            "\n                           Enjoy ~");
        }
        /// <summary>
        /// Handle Text Box input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox3_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int col, row;
                string userInput = textBox3.Text;

                if (Regex.IsMatch(userInput.ToUpper(), @"[A-Z][1-9][0-9]?"))
                {
                    MessageBox.Show("Entered cell is out of range");
                }

                else
                {
                    label2.Text = "Cell Value: " + userInput;
                    spreadsheetPanel1.GetSelection(out col, out row);
                    string panelCell = getCell(col, row); // convert col and row into usable cell value and store it

                    // Input users input into backing spreadsheet
                    try
                    {
                        model.SetContentsOfCell(panelCell, userInput);
                        // Get value of cell and display it
                        object cellValue = model.GetCellValue(panelCell);
                        spreadsheetPanel1.SetValue(col, row, cellValue.ToString());
                    }
                    catch (Exception exp)
                    {
                        spreadsheetPanel1.SetValue(col, row, exp.Message);
                    }

                    // move down one cell when the enter key is pressed
                    if (row < 99)
                    {
                        spreadsheetPanel1.SetSelection(col, row + 1);
                    }
                    spreadsheetPanel1.GetSelection(out col, out row);
                    label1.Text = "Current Cell: " + ((char)(65 + col)).ToString() + (1 + row);
                    label2.Text = "Cell Value: " + model.GetCellValue(getCell(col, row)).ToString();
                    if (model.GetCellContents(getCell(col, row)) is Formula)
                    {
                        string content = "=" + model.GetCellContents(getCell(col, row)).ToString();
                        textBox3.Text = content;
                    }
                    else
                    {
                        textBox3.Text = model.GetCellContents(getCell(col, row)).ToString();
                    }
                    // update all cells with new information
                    refresh(model.GetNamesOfAllNonemptyCells());
                }
            }
        }

        /// <summary>
        /// Helper method that converts given column and row into string that can be used 
        /// elsewhere, or be passed into spreadsheet class for use.  
        /// 
        /// Eg. Given 0,0 will return A1
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        private string getCell(int col, int row)
        {
            return ((char)(65 + col)).ToString() + (1 + row).ToString();
        }


        /// <summary>
        /// Helper method to refresh values of cells
        /// </summary>
        /// <param name="cells"></param>
        private void refresh(IEnumerable<string> cells)
        {
            int col, row;
            foreach (string s in cells)
            {
                col = ((int)s[0] - 65);
                row = ((int)s[1] - 49);
              
                spreadsheetPanel1.SetValue(col, row, model.GetCellValue(s).ToString());                
            }
        }
    }
}
