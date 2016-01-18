using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpreadsheetUtilities;
using System.Xml;

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
    /// "25", "2x", and ".&" are not.  Cell names are case sensitive, so "x" and "X" are
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
        private bool isChanged;

        /// <summary>
        /// Constructor, creates empty spreadsheet, backed by a List of cells
        /// </summary>
        public Spreadsheet() 
            : base(s => true, s => s, "default")
        {
            allCells = new Dictionary<string, Cell>();
            graph = new DependencyGraph();
            isChanged = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isValid"></param>
        /// <param name="normalize"></param>
        /// <param name="version"></param>
        public Spreadsheet (Func<string, bool> isValid, Func<string, string> normalize, string version) 
            : base(isValid, normalize, version)
        {
            allCells = new Dictionary<string, Cell>();
            graph = new DependencyGraph();
            isChanged = false;
        }
        /// <summary>
        /// Constructs an abstract spreadsheet by recording its variable validity test,
        /// its normalization method, and its version information.  The variable validity
        /// test is used throughout to determine whether a string that consists of one or
        /// more letters followed by one or more digits is a valid cell name.  The variable
        /// equality test should be used thoughout to determine whether two variables are
        /// equal.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isValid"></param>
        /// <param name="normalize"></param>
        /// <param name="version"></param>
        public Spreadsheet (String filePath, Func<string, bool> isValid, Func<string, string> normalize, string version) 
            : base(isValid, normalize, version)
        {
            allCells = new Dictionary<string, Cell>();
            graph = new DependencyGraph();
            isChanged = false;

            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreWhitespace = true;
                using (XmlReader reader = XmlReader.Create(filePath, settings))
                {
                    char[] trimable = { ' ', '\n' };
                    string cellName = "";
                    string cellContents = "";
                    bool nameExists = false;
                    bool contentExists = false;
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "name":
                                    reader.Read();
                                    cellName = reader.ReadContentAsString();
                                    cellName = cellName.TrimStart();
                                    cellName = cellName.TrimEnd();
                                    nameExists = true;
                                    break;

                                case "contents":
                                    reader.Read();
                                    cellContents = reader.ReadContentAsString();
                                    cellContents = cellContents.TrimStart();
                                    cellContents = cellContents.TrimEnd();
                                    SetContentsOfCell(cellName, cellContents);
                                    contentExists = true;
                                    break;
                            }
                        }
                    }
                    if (!nameExists && !contentExists)
                    {
                        throw new Exception("Spreadsheet is empty, dummy!");
                    }
                }
            }
            catch (Exception e)
            {
                throw new SpreadsheetReadWriteException(e.Message);
            }
        }

        /// <summary>
        /// Enumerates the names of all the non-empty cells in the spreadsheet.
        /// </summary>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            return new List<string> (allCells.Keys);
        }

        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the contents (as opposed to the value) of the named cell.  The return
        /// value should be either a string, a double, or a Formula.
        /// </summary>
        public override object GetCellContents(string name)
        {
            if (!Cell.validName(name) || !IsValid(name))
            {
                throw new InvalidNameException();
            }

            Cell temp;
            if (allCells.TryGetValue(Normalize(name), out temp))
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
        protected override ISet<string> SetCellContents(string name, double number)
        {
            if (!Cell.validName(name) || !IsValid(name))
            {
                throw new InvalidNameException();
            }

            HashSet<string> dependents = new HashSet<string>();

            List<string> dependentCells = GetCellsToRecalculate(name).ToList();

            // If cell doesn't exist, add it
            if (!allCells.ContainsKey(name))
            {
                allCells.Add(name, new Cell(name, number));
                foreach (string str in dependentCells)
                {
                    allCells[str].eval(Lookup);
                    dependents.Add(str);
                }
                dependents.Add(name);
                isChanged = true;
                return dependents;
            }

            // If cell exists, overwrite it's content and value           
            Cell temp;
            if (allCells.TryGetValue(Normalize(name), out temp))
            {
                string content = temp.getContents().ToString();
                temp.setContents(number);
                temp.setValue(number);

                // Remove dependencies, if any
                Formula f = new Formula(content);
                foreach (string str in f.GetVariables())
                {
                    if (str.Equals(name))
                    {
                        graph.RemoveDependency(name, str);
                    }
                }
            }

            // Get all dependents, indirect and direct, and then add them to a List which is then
            // added to the returned HashSet
            dependents.Add(name);
            foreach (string str in dependentCells)
            {
                allCells[str].eval(Lookup);
                dependents.Add(str);
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
        protected override ISet<string> SetCellContents(string name, string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException();
            }

            if (!Cell.validName(name) || !IsValid(name))
            {
                throw new InvalidNameException();
            }

            // If cell doesn't exist, add it
            if (!allCells.ContainsKey(name))
            {
                allCells.Add(name, new Cell(name, text));
                isChanged = true;

                // Get all dependents, indirect and direct, and then add them to a List which is then
                // added to the returned HashSet
                HashSet<string> dependents = new HashSet<string>();
                List<string> dependentCells = GetCellsToRecalculate(name).ToList();

                dependents.Add(name);
                foreach (string str in dependentCells)
                {
                    allCells[str].eval(Lookup);
                    dependents.Add(str);
                }
                return dependents;
            }

            // If cell exists, overwrite it's content           
            Cell temp;
            if (allCells.TryGetValue(name, out temp))
            {
                string content = temp.getContents().ToString();
                temp.setContents(text);
                Formula f = new Formula(content);
                foreach (string str in f.GetVariables())
                {
                    if (str.Equals(name))
                    {
                        graph.RemoveDependency(name, str);
                    }
                }
            }

            // Get all dependents, indirect and direct, and then add them to a List which is then
            // added to the returned HashSet
            HashSet<string> dependent = new HashSet<string>();
            List<string> dependentCell = GetCellsToRecalculate(name).ToList();
            dependent.Add(name);
            foreach (string str in dependentCell)
            {
                allCells[str].eval(Lookup);
                dependent.Add(str);
            }
            return dependent;
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
        protected override ISet<string> SetCellContents(string name, Formula formula)
        {
            // If the formula parameter is null, throws an ArgumentNullException.
            if (formula.Equals(null))
            {
                throw new ArgumentNullException();
            }

            // Otherwise, if name is null or invalid, throws an InvalidNameException.
            if (!Cell.validName(name) || !IsValid(name))
            {
                throw new InvalidNameException();
            }

            // If cell doesn't exist, add it
            if (!allCells.ContainsKey(name))
            {
                // add new cell
                allCells.Add(name, new Cell(name, formula, Lookup));
                // if formula contained variables, setup new dependencies
                if (formula.GetVariables().Count() > 0)
                {
                    foreach (string str in formula.GetVariables())
                    {
                        graph.AddDependency(name, str);
                    }
                }
                HashSet<string> dependents = new HashSet<string>();
                List<string> dependentCells = GetCellsToRecalculate(name).ToList();

                dependents.Add(name);
                foreach (string str in dependentCells)
                {
                    allCells[str].eval(Lookup);
                    dependents.Add(str);
                }
                return dependents;

            }

            // Otherwise, if changing the contents of the named cell to be the formula would cause a 
            // circular dependency, throws a CircularException.  (No change is made to the spreadsheet.)
            List<string> dependentCell = GetCellsToRecalculate(name).ToList();

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
                foreach (string str in formula.GetVariables())
                {
                    variables.Add(str);
                }
                graph.ReplaceDependents(name, variables);
            }

            // Get all dependents, indirect and direct, and then add them to a List which is then
            // added to the returned HashSet
            HashSet<string> dependent = new HashSet<string>();
            dependent.Add(name);
            foreach (string str in dependentCell)
            {
                allCells[str].eval(Lookup);
                dependent.Add(str);
            }
            return dependent;
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
                if (kvp.Value.getContents() is Formula)
                {
                    string content = kvp.Value.getContents().ToString();
                    Formula f = new Formula(content, Normalize, IsValid);
                    foreach (string str in f.GetVariables())
                    {
                        if (str.Equals(name))
                        {
                            dependents.Add(kvp.Key);
                        }
                    }
                }
            }
            return dependents;
        }

        /// <summary>
        /// True if this spreadsheet has been modified since it was created or saved                  
        /// (whichever happened most recently); false otherwise.
        /// </summary>
        public override bool Changed
        {
            get
            {
                return isChanged;
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns the version information of the spreadsheet saved in the named file.
        /// If there are any problems opening, reading, or closing the file, the method
        /// should throw a SpreadsheetReadWriteException with an explanatory message.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public override string GetSavedVersion(string filename)
        {
            try
            {
                using (XmlReader reader = XmlReader.Create(filename))
                {
                    while (reader.Read())
                    {
                        if (reader.Name.Equals("spreadsheet"))
                        {
                            return reader.GetAttribute("version");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new SpreadsheetReadWriteException(e.Message);
            }
            throw new SpreadsheetReadWriteException("Version does not exist.");
        }

        /// <summary>
        /// Writes the contents of this spreadsheet to the named file using an XML format.
        /// The XML elements should be structured as follows:
        /// 
        /// <spreadsheet version="version information goes here">
        /// 
        /// <cell>
        ///     <name>
        ///         cell name goes here
        ///     </name>
        ///     <contents>
        ///         cell contents goes here
        ///     </contents>    
        /// </cell>
        /// 
        /// </spreadsheet>
        /// 
        /// There should be one cell element for each non-empty cell in the spreadsheet.  
        /// If the cell contains a string, it should be written as the contents.  
        /// If the cell contains a double d, d.ToString() should be written as the contents.  
        /// If the cell contains a Formula f, f.ToString() with "=" prepended should be written as the contents.
        /// 
        /// If there are any problems opening, writing, or closing the file, the method should throw a
        /// SpreadsheetReadWriteException with an explanatory message.
        /// </summary>
        public override void Save(string filename)
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineOnAttributes = true;

                XmlWriter writer = XmlWriter.Create(filename, settings);
                writer.WriteStartDocument();
                writer.WriteStartElement("spreadsheet");
                writer.WriteAttributeString("PS6", Version);

                foreach (KeyValuePair<string, Cell> kvp in allCells)
                {
                    writer.WriteStartElement("cell");
                    writer.WriteElementString("name", kvp.Key);
                    // Prepend equal sign if formula
                    if (kvp.Value.getContents() is Formula)
                    {
                        string formulaTemp = "=" + kvp.Value.getContents().ToString();
                        writer.WriteElementString("contents", formulaTemp);
                    }
                    else
                    {
                        writer.WriteElementString("contents", kvp.Value.getContents().ToString());
                    }
                    writer.WriteEndElement();
                }

                isChanged = false;

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close(); 
            }
            catch (Exception e)
            {
                throw new SpreadsheetReadWriteException(e.Message);
            }
        }

        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the value (as opposed to the contents) of the named cell.  The return
        /// value should be either a string, a double, or a SpreadsheetUtilities.FormulaError.
        /// </summary>
        public override object GetCellValue(string name)
        {
            if (!Cell.validName(name) || name == null) 
            {
                throw new InvalidNameException();
            }
            else
            {
                Cell temp;
                if (allCells.TryGetValue(Normalize(name), out temp))
                {
                    return temp.getValue();
                }
            }
            return "";
        }
        /// <summary>
        /// If content is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, if content parses as a double, the contents of the named
        /// cell becomes that double.
        /// 
        /// Otherwise, if content begins with the character '=', an attempt is made
        /// to parse the remainder of content into a Formula f using the Formula
        /// constructor.  There are then three possibilities:
        /// 
        ///   (1) If the remainder of content cannot be parsed into a Formula, a 
        ///       SpreadsheetUtilities.FormulaFormatException is thrown.
        ///       
        ///   (2) Otherwise, if changing the contents of the named cell to be f
        ///       would cause a circular dependency, a CircularException is thrown.
        ///       
        ///   (3) Otherwise, the contents of the named cell becomes f.
        /// 
        /// Otherwise, the contents of the named cell becomes content.
        /// 
        /// If an exception is not thrown, the method returns a set consisting of
        /// name plus the names of all other cells whose value depends, directly
        /// or indirectly, on the named cell.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public override ISet<string> SetContentsOfCell(string name, string content)
        {
            if (content == null)
            {
                throw new ArgumentNullException();
            }

            if (!Cell.validName(name) || !IsValid(name))
            {
                throw new InvalidNameException();
            }

            double myNum;
            bool isNumerical = Double.TryParse(content, out myNum);
            // If string is double
            if (isNumerical)
            {
                return SetCellContents(Normalize(name), myNum);
            }
            // If is not double, is string
            else
            {
                // If =, is formula
                if (content.StartsWith("="))
                {
                    // strip off equal sign to evaluate
                    string str = content.Substring(1);
                    Formula f = new Formula(str, Normalize, IsValid);
                    return SetCellContents(Normalize(name), f);
                }
                // Is not formula, is string
                else
                {
                    return SetCellContents(name, content);
                }
            }
        }
        /// <summary>
        /// Lookup function to be passed to cell creator, so that cell's value is computed immediately
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected double Lookup(String name)
        {
            name = Normalize(name);

            if (!(allCells[name].getValue() is Double))
            {
                throw new ArgumentException();
            }
            return (double)allCells[name].getValue();
        }
        
        /// <summary>
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
            /// <param name="_content"></param>
            /// <param name="lookup"></param>
            public Cell(string _name, Formula _content, Func<string, double> lookup)
            {
                name = _name;
                content = _content;
                value = _content.Evaluate(lookup);
            }

            /// <summary>
            /// Constructor for cell with string content
            /// Value of this cell is the same as the content
            /// </summary>
            /// <param name="_name"></param>
            /// <param name="_content"></param>
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

            public Object getValue()
            {
                return value;
            }

            public void setValue(double _value)
            {
                value = _value;
            }

            public void setName(string _name)
            {
                name = _name;
            }

            public void setContents(Object _content)
            {
                content = _content;
            }
            /// <summary>
            /// Helper method used to re-evaluate cells if they are formulas
            /// </summary>
            /// <param name="lookup"></param>
            public void eval(Func<string, double> lookup)
            {
                if (content is Formula)
                {
                    value = ((Formula)content).Evaluate(lookup);
                }
                else if (content is String)
                {
                    value = (string)content;
                }
                else if (content is Double)
                {
                    value = (double)content;
                }
            }

            /// <summary>
            /// Thus your code should consider a string to be a valid cell name if:
            /// The string consists of one or more letters followed by one or more digits, and
            /// The (application programmer's) IsValid function returns true for that string.
            /// </summary>
            /// <param name="token"></param>
            /// <returns></returns>
            public static bool validName(String token)
            {
                if (token == null)
                {
                    return false;
                }
                if (Char.IsLetter(token[0]))
                {
                    if (token.Length == 1)
                    {
                        return true;
                    }
                    for (int i = 1; i < token.Length; i++)
                    {
                        if (Char.IsLetter(token[i]) || Char.IsNumber(token[i]))
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
