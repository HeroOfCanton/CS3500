// Skeleton written by Joe Zachary for CS 3500, September 2013
// Read the entire skeleton carefully and completely before you
// do anything else!

// Version 1.1 (9/22/13 11:45 a.m.)

// Change log:
//  (Version 1.1) Repaired mistake in GetTokens
//  (Version 1.1) Changed specification of second constructor to
//                clarify description of how validation works

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using double-precision 
    /// floating-point syntax; variables that consist of a letter or underscore followed by 
    /// zero or more letters, underscores, or digits; parentheses; and the four operator 
    /// symbols +, -, *, and /.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
    /// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; 
    /// and "x 23" consists of a variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The
    /// normalizer is used to convert variables into a canonical form, and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement 
    /// that it consist of a letter or underscore followed by zero or more letters, underscores,
    /// or digits.)  Their use is described in detail in the constructor and method comments.
    /// 
    /// Author - Ryan Welling, Joe Zachary
    /// </summary>
    public class Formula
    {
        private string formulaString;       // used in constructor to collect all valid tokens into string
                                            // returned in toString and used in GetHashCode
        private HashSet<string> variables;  // used in constructor to collect all valid variables, returned in GetVariables

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        public Formula(String formula) :
            this(formula, s => s, s => true)
        {

        }

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        public Formula(String formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            string[] tokenString = new string[GetTokens(formula).Count()];
            var tokenEnum = GetTokens(formula).GetEnumerator();
            int openParenCount = 0;
            int closeParenCount = 0;

            for (int i = 0; i < tokenString.Length; i++)
            {
                tokenEnum.MoveNext();
                tokenString[i] = normalize(tokenEnum.Current);
            }

            for (int i = 0; i < tokenString.Length; i++)
            {
                // - The first token of an expression must be a number, a variable, or an opening parenthesis.
                if (i == 0)
                {
                    if (!nvop(tokenString[i]))
                    {
                        throw new FormulaFormatException("First token of expression must be number, variable or opening paren, " +
                                                            "not " + tokenString[i]);
                    }
                }
                // - The last token of an expression must be a number, a variable, or a closing parenthesis.
                if (i == tokenString.Length - 1)
                {
                    double lastToken;
                    bool isNumeric = Double.TryParse(tokenString[i], out lastToken);
                    if (isNumeric || validVariable(tokenString[i]) || tokenString[i].Equals(")"))
                    {
                        if (tokenString[i].Equals(")"))
                        {
                            closeParenCount++;
                        }
                        continue;
                    }
                    else
                    {
                        throw new FormulaFormatException("Last token of expression must be number, variable or closing paren, " +
                                                            "not " + tokenString[i]);
                    }
                }

                double myNum;
                bool isNumerical = Double.TryParse(tokenString[i], out myNum);
                // If token is numerical, add it to the string.
                if (isNumerical)
                {
                    // - Any token that immediately follows a number, a variable, or a closing parenthesis must be either 
                    //   an operator or a closing parenthesis.
                    if (i != tokenString.Length && tokenString.Length > 1 && !(ocp(tokenString[i + 1])))
                    {
                        throw new FormulaFormatException("Token that follows " + tokenString[i] + " must be an operator " +
                                                            "or closing paren, but is " + tokenString[i + 1]);
                    }
                }
                // If token is a valid variable, check to see that it is still valid after normalize and validate is called
                // and then add it to the string.
                else if (validVariable(tokenString[i]))
                {
                    string normalizedString = normalize(tokenString[i]);
                    if (!validVariable(normalizedString))
                    {
                        throw new FormulaFormatException("Your Normalized method returned an invalid variable");
                    }

                    if (!isValid(normalizedString))
                    {
                        throw new FormulaFormatException("Your Normalized method returned an invalid variable according to your isValid method");
                    }

                    // - Any token that immediately follows a number, a variable, or a closing parenthesis must be either 
                    //   an operator or a closing parenthesis.
                    if (i > tokenString.Length && tokenString.Length > 1)
                    {
                        if(!(ocp(tokenString[i + 1])))
                        {
                            throw new FormulaFormatException("Token that follows " + tokenString[i] + " must be an operator " +
                                                            "or closing paren, but is " + tokenString[i + 1]);
                        }
                    }
                }
                // If token is valid operator, add it to the string.
                else if (tokenString[i].Equals("+") || tokenString[i].Equals("-") || tokenString[i].Equals("*") || tokenString[i].Equals("/"))
                {
                    // - Any token that immediately follows an opening parenthesis or an operator must be either a number, 
                    //   a variable, or an opening parenthesis.
                    if (i != tokenString.Length && !nvop(tokenString[i + 1]))
                    {
                        throw new FormulaFormatException("Token that follows " + tokenString[i] + " must be number, variable " +
                                                            "or opening paren, but is " + tokenString[i + 1]);
                    }
                }
                // If token is open paren, increment open paren count, check next token and then add it to the string.
                else if (tokenString[i].Equals("("))
                {
                    openParenCount++;
                    // - Any token that immediately follows an opening parenthesis or an operator must be either a number, 
                    //   a variable, or an opening parenthesis.
                    if (i != tokenString.Length && tokenString.Length > 1 && !nvop(tokenString[i + 1]))
                    {
                        throw new FormulaFormatException("Token that follows " + tokenString[i] + " must be number, variable " +
                                                            "or opening paren, but is " + tokenString[i + 1]);
                    }
                }
                // If token is closing paren, increment closing paren count, check next token and then add it to the string.
                else if (tokenString[i].Equals(")"))
                {
                    closeParenCount++;
                    // - Any token that immediately follows a number, a variable, or a closing parenthesis must be either 
                    //   an operator or a closing parenthesis.
                    if (i != tokenString.Length && tokenString.Length > 1 && !(ocp(tokenString[i + 1])))
                    {
                        throw new FormulaFormatException("Token that follows " + tokenString[i] + " must be an operator " +
                                                            "or closing paren, but is " + tokenString[i + 1]);
                    }
                }
                // - When reading tokens from left to right, at no point should the number of closing parentheses
                //   seen so far be greater than the number of opening parentheses seen so far.
                if (closeParenCount > openParenCount)
                {
                    throw new FormulaFormatException("There are more close parens appearing before open parens.");
                }
            }
         
            // There must always be at least 1 token
            if (tokenString.Length < 1)
            {
                throw new FormulaFormatException("Formula must not be empty");
            }

            // - The total number of opening parentheses must equal the total number of closing parentheses.
            if (openParenCount != closeParenCount)
            {
                throw new FormulaFormatException("There must be an equal number of opening and closing parens");
            }

            // Take all tokens, throw them in a string builder and set that to the member variable to be used in the 
            // toString and GetHashCode methods later on.  If they are variables, add them to HashSet for GetVariables.
            StringBuilder formulaStr = new StringBuilder();
            variables = new HashSet<string>(); 

            foreach (string s in tokenString)
            {
                formulaStr.Append(s);
                if (validVariable(s))
                {
                    variables.Add(s);
                }
            }
            formulaString = formulaStr.ToString();
        }

        /// <summary>
        /// Private method to check for valid variable. Valid variables consist of a letter 
        /// or underscore followed by zero or more letters, underscores, or digits;
        /// </summary>
        /// <param name="token">
        /// string to be evaluated
        /// </param>
        /// <returns>
        /// boolean
        /// </returns>
        private bool validVariable(String token)
        {
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

        /// <summary>
        /// Private method that determines if token is an operator (* / + -) or a closing paren
        /// </summary>
        /// <param name="token">
        /// string to be checked
        /// </param>
        /// <returns>
        /// boolean
        /// </returns>
        private bool ocp(string token)
        {
            if (token.Equals("+") || token.Equals("-") || token.Equals("*") || token.Equals("/") || token.Equals(")"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Private method to determine if token is number, variable or open parenthesis
        /// </summary>
        /// <param name="token"></param>
        /// <returns>
        /// boolean true if number, variable or open paren
        /// </returns>
        private bool nvop(String token)
        {
            double myNum;
            bool isNumerical = Double.TryParse(token, out myNum);
            // If token is numeric, return true.
            if (isNumerical)
            {
                return true;
            }
            // If token is an open paren, return true.
            if (token.Equals("("))
            {
                return true;
            }
            // If token is a valid variable, return true.
            if (validVariable(token))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>
        public object Evaluate(Func<string, double> lookup)
        {
            Stack values = new Stack();
            Stack operations = new Stack();

            var tokenEnum = GetTokens(formulaString).GetEnumerator();
            string[] substrings = new string[GetTokens(formulaString).Count()];
            
            for(int i = 0; i < substrings.Length; i++) 
            {
                tokenEnum.MoveNext();
                substrings[i] = tokenEnum.Current;
            }

            for (int i = 0; i < substrings.Length; i++)
            {
                double myNum;
                bool isNumerical = Double.TryParse(substrings[i], out myNum);

                // If token is numeric, and there are more than one values in the stack, if operations stack has * or /
                // act on it with the token and popped value, else, add token to the stack.
                if (isNumerical)
                {
                    if (values.Count > 0)
                    {
                        if (operations.Peek().Equals("*"))
                        {
                            double valueTop = (double)values.Pop();
                            operations.Pop();
                            values.Push(myNum * valueTop);
                        }
                        else if (operations.Peek().Equals("/"))
                        {
                            if (myNum == 0)
                            {
                                return new FormulaError("Can't divide by zero");
                            }
                            else
                            {
                                double valueTop = (double)values.Pop();
                                operations.Pop();
                                values.Push(valueTop / myNum);
                            }
                        }
                        else
                        {
                            values.Push(myNum);
                        }
                    }
                    else
                    {
                        values.Push(myNum);
                    }
                }
                // If token is not numeric, check to see if it is whitespace, * /, ( ), or a variable
                else
                {
                    if (substrings[i] == " " || substrings[i] == "")
                    {
                        continue;
                    }
                    // If + -, and operations stack has a + or - on it,  act on top two values with operation.  Regardless, push token on stack
                    else if (substrings[i] == "+" || substrings[i] == "-")
                    {
                        if (operations.Count > 0 && (operations.Peek().Equals("+") || operations.Peek().Equals("-")))
                        {
                            double valueOne = (double)values.Pop();
                            double valueTwo = (double)values.Pop();
                            String op = (string)operations.Pop();
                            if (op.Equals("+"))
                            {
                                values.Push(valueOne + valueTwo);
                            }
                            else
                            {
                                values.Push(valueTwo - valueOne);
                            }
                        }
                        operations.Push(substrings[i]);
                    }
                    else if (substrings[i] == "*" || substrings[i] == "/")
                    {
                        operations.Push(substrings[i]);
                    }
                    else if (substrings[i] == "(")
                    {
                        operations.Push(substrings[i]);
                    }
                    // If closing paren, clear out everything in the paren and check to see if preceding operation is * or /
                    else if (substrings[i] == ")")
                    {                        
                        if (operations.Peek().Equals("+") || operations.Peek().Equals("-"))
                        {
                            if (operations.Peek().Equals("+"))
                            {
                                double valueOne = (double)values.Pop();
                                double valueTwo = (double)values.Pop();
                                operations.Pop();
                                values.Push(valueOne + valueTwo);
                                // If the next item in the operations stack is (, pop it off but then check to see if next operation after that is * or /
                                if (operations.Peek().Equals("("))
                                {
                                    operations.Pop();
                                    if (operations.Count > 0 && (operations.Peek().Equals("*") || operations.Peek().Equals("/")))
                                    {
                                        if (operations.Peek().Equals("*"))
                                        {
                                            double valueOnes = (double)values.Pop();
                                            double valueTwos = (double)values.Pop();
                                            operations.Pop();
                                            values.Push(valueOnes * valueTwos);
                                        }
                                        else if (operations.Peek().Equals("/"))
                                        {
                                            double valueOnes = (double)values.Pop();
                                            double valueTwos = (double)values.Pop();
                                            if (valueOnes == 0)
                                            {
                                                return new FormulaError("Can't divide by zero");
                                            }
                                            operations.Pop();
                                            values.Push(valueTwos / valueOnes);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                double valueOne = (double)values.Pop();
                                double valueTwo = (double)values.Pop();
                                operations.Pop();
                                values.Push(valueTwo - valueOne);
                                // If the next item in the operations stack is (, pop it off but then check to see if next operation after that is * or /
                                if (operations.Peek().Equals("("))
                                {
                                    operations.Pop();
                                    if (operations.Count > 0 && (operations.Peek().Equals("*") || operations.Peek().Equals("/")))
                                    { 
                                        if (operations.Peek().Equals("*"))
                                        {
                                            double valueOnes = (double)values.Pop();
                                            double valueTwos = (double)values.Pop();
                                            operations.Pop();
                                            values.Push(valueOnes * valueTwos);
                                        }
                                        else if (operations.Peek().Equals("/"))
                                        {
                                            double valueOnes = (double)values.Pop();
                                            double valueTwos = (double)values.Pop();
                                            if (valueOnes == 0)
                                            {
                                                return new FormulaError("Can't divide by zero");
                                            }
                                            operations.Pop();
                                            values.Push(valueTwos / valueOnes);
                                        }
                                    }
                                }
                            }
                        }
                        // If the next item in the operations stack is (, pop it off but then check to see if next operation after that is * or /
                        else if (operations.Peek().Equals("("))
                        {
                            operations.Pop();
                            if (operations.Count > 0 && (operations.Peek().Equals("*") || operations.Peek().Equals("/")))
                            { 
                                if (operations.Peek().Equals("*"))
                                {
                                    double valueOne = (double)values.Pop();
                                    double valueTwo = (double)values.Pop();
                                    operations.Pop();
                                    values.Push(valueOne * valueTwo);
                                }
                                else if (operations.Peek().Equals("/"))
                                {
                                    double valueOne = (double)values.Pop();
                                    double valueTwo = (double)values.Pop();
                                    if (valueOne == 0)
                                    {
                                        return new FormulaError("Can't divide by zero");
                                    }
                                    operations.Pop();
                                    values.Push(valueTwo / valueOne);
                                }
                            }
                        }
                    }
                    // Whatever is left, however improbable, must be a variable.  Look it up, return FormulaError if there is an exception
                    else
                    {
                        try
                        {
                            if (values.Count > 0)
                            {
                                if (operations.Peek().Equals("*"))
                                {
                                    double valueTop = (double)values.Pop();
                                    operations.Pop();
                                    values.Push(lookup(substrings[i]) * valueTop);
                                }
                                else if (operations.Peek().Equals("/"))
                                {
                                    if (lookup(substrings[i]) == 0)
                                    {
                                        return new FormulaError("Can't divide by zero");
                                    }
                                    else
                                    {
                                        double valueTop = (double)values.Pop();
                                        operations.Pop();
                                        values.Push(valueTop / lookup(substrings[i]));
                                    }
                                }
                                else
                                {
                                    values.Push(lookup(substrings[i]));
                                }
                            }
                            else
                            {
                                values.Push(lookup(substrings[i]));
                            }
                        }
                        catch(Exception e)
                        {
                            return new FormulaError("Lookup of variable threw an error, does it exist?");
                        }
                    }
                }
            }
            // Once all tokens have been processed, clear out the last values, operate on them if there is more than one and return final value
            if (operations.Count == 0 && values.Count == 1)
            {
                return (double)values.Pop();
            }
            else
            {
                if (operations.Peek().Equals("+"))
                {
                    double valueOne = (double)values.Pop();
                    double valueTwo = (double)values.Pop();
                    var operationsTop = operations.Pop();
                    return valueOne + valueTwo;
                }
                else
                {
                    double valueOne = (double)values.Pop();
                    double valueTwo = (double)values.Pop();
                    var operationsTop = operations.Pop();
                    return valueTwo - valueOne;
                }
            }
        }

        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears more than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            return variables;
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {
            return formulaString;
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens, which are compared as doubles, and variable tokens,
        /// whose normalized forms are compared as strings.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object obj)
        {
            // If obj is null, or is not formula, fail
            if (obj == null || !(obj is Formula))
            {
                return false;
            }
            
            string lhsString = this.ToString();
            string rhsString = ((Formula)obj).ToString();

            // Create string from lhs formula. Iterrate over it, taking tokens into a string array
            string[] lhsEnumArr = new string[GetTokens(lhsString).Count()];
            var lhsEnum = GetTokens(lhsString).GetEnumerator();
            for (int i = 0; i < lhsEnumArr.Length; i++)
            {
                lhsEnum.MoveNext();
                lhsEnumArr[i] = lhsEnum.Current;
            }

            // Create string from rhs formula.  Iterrate over it, taking tokens into string array
            string[] rhsEnumArr = new string[GetTokens(rhsString).Count()];
            var rhsEnum = GetTokens(rhsString).GetEnumerator();
            for (int i = 0; i < rhsEnumArr.Length; i++)
            {
                rhsEnum.MoveNext();
                rhsEnumArr[i] = rhsEnum.Current;
            }

            for (int i = 0; i < lhsEnumArr.Length; i++)
            {
                double lhsNum;
                bool isLhsNum = Double.TryParse(lhsEnumArr[i], out lhsNum);

                double rhsNum;
                bool isRhsNum = Double.TryParse(rhsEnumArr[i], out rhsNum);
                // If token is double, make sure they're equal
                if(isLhsNum || isRhsNum) 
                {
                    if(lhsNum == rhsNum) 
                    {
                        continue;
                    }
                    return false;
                }
                // If token is a variable, make sure they're equal
                else if (validVariable(lhsEnumArr[i]) || validVariable(rhsEnumArr[i]))
                {
                    if (lhsEnumArr[i].Equals(rhsEnumArr[i]))
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
                // If token is an operator or paren, make sure they're equal
                else if(lhsEnumArr[i].Equals("+") || lhsEnumArr[i].Equals("-") || lhsEnumArr[i].Equals("*") || lhsEnumArr[i].Equals("/")
                        || lhsEnumArr[i].Equals("(") || lhsEnumArr[i].Equals(")"))
                {
                    if (lhsEnumArr[i].Equals(rhsEnumArr[i]))
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return true.  If one is
        /// null and one is not, this method should return false.
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2)
        {
            if (f1.Equals(null) && f2.Equals(null))
            {
                return true;
            }

            if (f1.Equals(null) && !f2.Equals(null) || !f1.Equals(null) && f2.Equals(null))
            {
                return false;
            }
            
            if (f1.Equals(f2))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return false.  If one is
        /// null and one is not, this method should return true.
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2)
        {
            if (f1.Equals(null) && f2.Equals(null))
            {
                return false;
            }

            if (f1.Equals(null) && !f2.Equals(null) || !f1.Equals(null) && f2.Equals(null))
            {
                return true;
            }

            if (!(f1.Equals(f2)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            return formulaString.GetHashCode();
        }

        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<string> GetTokens(String formula)
        {
            // Patterns for individual tokens
            String lpPattern = @"\(";
            String rpPattern = @"\)";
            String opPattern = @"[\+\-*/]";
            String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
            String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
            String spacePattern = @"\s+";

            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                            lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

            // Enumerate matching tokens that don't consist solely of white space.
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
                {
                    yield return s;
                }
            }
        }
    }

    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }

}

