using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpreadsheetUtilities;

namespace SS
{
    /// <summary> 
    /// A spreadsheet consists of an infinite number of named cells.
    /// 
    /// A string is a valid cell name if and only if:
    ///   (1) its first character is an underscore or a letter
    ///   (2) its remaining characters (if any) are underscores and/or letters and/or digits
    /// Note that this is the same as the definition of valid variable from the PS3 Formula class.
    /// 
    /// For example, "x", "_", "x2", "y_15", and "___" are all valid cell  names, but
    /// "25", "2x", and "&" are not.  Cell names are case sensitive, so "x" and "X" are
    /// different cell names.
    /// 
    /// A spreadsheet contains a cell corresponding to every possible cell name.  (This
    /// means that a spreadsheet contains an infinite number of cells.)  In addition to 
    /// a name, each cell has a contents and a value.  The distinction is important.
    /// 
    /// The contents of a cell can be (1) a string, (2) a double, or (3) a Formula.  If the
    /// contents is an empty string, we say that the cell is empty.  (By analogy, the contents
    /// of a cell in Excel is what is displayed on the editing line when the cell is selected.)
    /// 
    /// In a new spreadsheet, the contents of every cell is the empty string.
    ///  
    /// The value of a cell can be (1) a string, (2) a double, or (3) a FormulaError.  
    /// (By analogy, the value of an Excel cell is what is displayed in that cell's position
    /// in the grid.)
    /// 
    /// If a cell's contents is a string, its value is that string.
    /// 
    /// If a cell's contents is a double, its value is that double.
    /// 
    /// If a cell's contents is a Formula, its value is either a double or a FormulaError,
    /// as reported by the Evaluate method of the Formula class.  The value of a Formula,
    /// of course, can depend on the values of variables.  The value of a variable is the 
    /// value of the spreadsheet cell it names (if that cell's value is a double) or 
    /// is undefined (otherwise).
    /// 
    /// Spreadsheets are never allowed to contain a combination of Formulas that establish
    /// a circular dependency.  A circular dependency exists when a cell depends on itself.
    /// For example, suppose that A1 contains B1*2, B1 contains C1*2, and C1 contains A1*2.
    /// A1 depends on B1, which depends on C1, which depends on A1.  That's a circular
    /// dependency.
    /// </summary>
    public class Spreadsheet : AbstractSpreadsheet
    {
        private Dictionary<string, Cell> allCells;
        private DependencyGraph graph;

        /// <summary>
        /// Constructor, creates empty spreadsheet, backed by a List of cells
        /// </summary>
        public Spreadsheet()
        {
            allCells = new Dictionary<string, Cell>();
            graph = new DependencyGraph();
        }

        /// <summary>
        /// Enumerates the names of all the non-empty cells in the spreadsheet.
        /// </summary>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            return new List<string>(allCells.Keys);
        }

        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the contents (as opposed to the value) of the named cell.  The return
        /// value should be either a string, a double, or a Formula.
        /// </summary>
        public override object GetCellContents(string name)
        {
            if (!Cell.validName(name))
            {
                throw new InvalidNameException();
            }

            Cell temp;
            if (allCells.TryGetValue(name, out temp))
            {
                return temp.getContents();
            }
            return "";
        }
        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, the contents of the named cell becomes number.  The method returns a
        /// set consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        /// </summary>
        public override ISet<string> SetCellContents(string name, double number)
        {
            if (!Cell.validName(name))
            {
                throw new InvalidNameException();
            }

            HashSet<string> dependents = new HashSet<string>();
            List<string> dependentCells = GetCellsToRecalculate(name).ToList();

            // If cell doesn't exist, add it
            if (!allCells.ContainsKey(name))
            {
                allCells.Add(name, new Cell(name, number));
                foreach (string s in dependentCells)
                {
                    dependents.Add(s);
                }
                dependents.Add(name);
                return dependents;
            }

            // If cell exists, overwrite it
            Cell temp;
            if (allCells.TryGetValue(name, out temp))
            {
                string content = temp.getContents().ToString();
                temp.setContents(number);

                // remove dependencies, if they exist
                Formula f = new Formula(content);
                foreach (string s in f.GetVariables())
                {
                    if (s.Equals(name))
                    {
                        graph.RemoveDependency(name, s);
                    }
                }
            }
            // Get all dependents, indirect and direct, and then add them to a List which is then
            // added to the returned HashSet
            dependents.Add(name);
            foreach (string s in dependentCells)
            {
                dependents.Add(s);
            }
            return dependents;
        }
        /// <summary>
        /// If text is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, the contents of the named cell becomes text.  The method returns a
        /// set consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        /// </summary>
        public override ISet<string> SetCellContents(string name, string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException();
            }

            if (!Cell.validName(name))
            {
                throw new InvalidNameException();
            }

            // If cell doesn't exist, add it
            if (!allCells.ContainsKey(name))
            {
                allCells.Add(name, new Cell(name, text));
            }

            // If cell exists, overwrite it's content           
            Cell temp;
            if (allCells.TryGetValue(name, out temp))
            {
                string content = temp.getContents().ToString();
                temp.setContents(text);
                Formula f = new Formula(content);
                foreach (string s in f.GetVariables())
                {
                    if (s.Equals(name))
                    {
                        graph.RemoveDependency(name, s);
                    }
                }
            }

            // Get all dependents, indirect and direct, and then add them to a List which is then
            // added to the returned HashSet
            HashSet<string> dependents = new HashSet<string>();
            List<string> dependentCells = GetCellsToRecalculate(name).ToList();
            dependents.Add(name);
            foreach (string s in dependentCells)
            {
                dependents.Add(s);
            }
            return dependents;
        }
        /// <summary>
        /// If the formula parameter is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, if changing the contents of the named cell to be the formula would cause a 
        /// circular dependency, throws a CircularException.  (No change is made to the spreadsheet.)
        /// 
        /// Otherwise, the contents of the named cell becomes formula.  The method returns a
        /// Set consisting of name plus the names of all other cells whose value depends,
        /// directly or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        /// </summary>
        public override ISet<string> SetCellContents(string name, Formula formula)
        {
            // If the formula parameter is null, throws an ArgumentNullException.
            if (formula.Equals(null))
            {
                throw new ArgumentNullException();
            }

            // Otherwise, if name is null or invalid, throws an InvalidNameException.
            if (!Cell.validName(name))
            {
                throw new InvalidNameException();
            }

            // If cell doesn't exist, add it
            if (!allCells.ContainsKey(name))
            {
                // Check for circular exception
                GetCellsToRecalculate(name);
                // add new cell
                allCells.Add(name, new Cell(name, formula));
                // if formula contained variables, setup new dependencies
                if (formula.GetVariables().Count() > 0)
                {
                    foreach (string s in formula.GetVariables())
                    {
                        graph.AddDependency(name, s);
                    }
                }
            }

            // Otherwise, if changing the contents of the named cell to be the formula would cause a 
            // circular dependency, throws a CircularException.  (No change is made to the spreadsheet.)
            GetCellsToRecalculate(name);

            // Otherwise, the contents of the named cell becomes formula.
            // If cell exists, overwrite it's content
            Cell temp;
            if (allCells.TryGetValue(name, out temp))
            {
                temp.setContents(formula);
            }

            // If the replacement formula has variables, replace the dependency of the old cell with 
            // new ones from the new formula.
            if (formula.GetVariables().Count() > 0)
            {
                List<string> variables = new List<string>();
                foreach (string s in formula.GetVariables())
                {
                    variables.Add(s);
                }

                graph.ReplaceDependents(name, variables);
            }

            // Get all dependents, indirect and direct, and then add them to a List which is then
            // added to the returned HashSet
            HashSet<string> dependents = new HashSet<string>();
            List<string> dependentCells = GetCellsToRecalculate(name).ToList();

            dependents.Add(name);
            foreach (string s in dependentCells)
            {
                dependents.Add(s);
            }
            return dependents;
        }
        /// <summary>
        /// If name is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name isn't a valid cell name, throws an InvalidNameException.
        /// 
        /// Otherwise, returns an enumeration, without duplicates, of the names of all cells whose
        /// values depend directly on the value of the named cell.  In other words, returns
        /// an enumeration, without duplicates, of the names of all cells that contain
        /// formulas containing name.
        /// 
        /// For example, suppose that
        /// A1 contains 3
        /// B1 contains the formula A1 * A1
        /// C1 contains the formula B1 + A1
        /// D1 contains the formula B1 - C1
        /// The direct dependents of A1 are B1 and C1
        /// </summary>
        protected override IEnumerable<string> GetDirectDependents(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException();
            }

            if (!Cell.validName(name))
            {
                throw new InvalidNameException();
            }

            // Loop over every cell in spreadsheet, checking for dependents of name by looking for
            // cells that contain name
            HashSet<string> dependents = new HashSet<string>();
            foreach (KeyValuePair<string, Cell> kvp in allCells)
            {
                // Turn name into a string, convert to a formula and call get variables on the formula
                // if there are variables in the formula (the contents of the cell) and one of those
                // variables is name, add the name of the cell that contains name to the dependents list
                string content = kvp.Value.getContents().ToString();
                Formula f = new Formula(content);
                foreach (string s in f.GetVariables())
                {
                    if (s.Equals(name))
                    {
                        dependents.Add(kvp.Key);
                    }
                }
            }
            return dependents;
        }
        
        /// <summary>
        /// A string is a valid cell name if and only if:
        ///   (1) its first character is an underscore or a letter
        ///   (2) its remaining characters (if any) are underscores and/or letters and/or digits
        /// Note that this is the same as the definition of valid variable from the PS3 Formula class.
        /// 
        /// For example, "x", "_", "x2", "y_15", and "___" are all valid cell  names, but
        /// "25", "2x", and "&" are not.  Cell names are case sensitive, so "x" and "X" are
        /// different cell names.
        /// 
        /// The contents of a cell can be (1) a string, (2) a double, or (3) a Formula.  If the
        /// contents is an empty string, we say that the cell is empty.  (By analogy, the contents
        /// of a cell in Excel is what is displayed on the editing line when the cell is selected.)
        /// 
        /// In a new spreadsheet, the contents of every cell is the empty string.
        ///  
        /// The value of a cell can be (1) a string, (2) a double, or (3) a FormulaError.  
        /// (By analogy, the value of an Excel cell is what is displayed in that cell's position
        /// in the grid.)
        /// 
        /// If a cell's contents is a string, its value is that string.
        /// 
        /// If a cell's contents is a double, its value is that double.
        /// 
        /// If a cell's contents is a Formula, its value is either a double or a FormulaError,
        /// as reported by the Evaluate method of the Formula class.  The value of a Formula,
        /// of course, can depend on the values of variables.  The value of a variable is the 
        /// value of the spreadsheet cell it names (if that cell's value is a double) or 
        /// is undefined (otherwise).
        /// </summary>
        private class Cell
        {
            private string name;
            private Object content;
            private Object value;
            public Cell() 
            {
                name = "";
                content = "";
            }
            /// <summary>
            /// Constructor for cell with double content
            /// Value of this cell is the same as the content
            /// </summary>
            /// <param name="_name"></param>
            /// <param name="_content"></param>
            public Cell(string _name, double _content)
            {
                name = _name;
                content = _content;
                value = _content;
            }
            /// <summary>
            /// Constructor for cell with Formula content
            /// Value of this cell is the evaluated content
            /// </summary>
            /// <param name="_name"></param>
            /// <param name="content"></param>
            public Cell(string _name, Formula _content)
            {
                name = _name;
                content = _content;
            }
            /// <summary>
            /// Constructor for cell with string content
            /// Value of this cell is the same as the content
            /// </summary>
            /// <param name="_name"></param>
            /// <param name="content"></param>
            public Cell(string _name, string _content)
            {
                name = _name;
                content = _content;
                value = _content;
            }

            public string getName() {
                return name;
            }

            public Object getContents()
            {
                return content;
            }

            public void setName(string _name)
            {
                name = _name;
            }

            public void setContents(Object _content)
            {
                content = _content;
            }

            public static bool validName(String token)
            {
                if (token == null)
                {
                    return false;
                }
                if (token[0].Equals("_") || Char.IsLetter(token[0]))
                {
                    if (token.Length == 1)
                    {
                        return true;
                    }
                    for (int i = 1; i < token.Length; i++)
                    {
                        if (Char.IsLetter(token[i]) || Char.IsNumber(token[i]) || token[i].Equals("_"))
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
        }
    }
}
