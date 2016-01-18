using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FormulaEvaluator
{
    /// <summary>
    /// This is an in-fix calculator.  It will respect basic orders of operations, meaning, Parens -> Multiply / Divide -> Add / Subtract.
    /// 
    /// Will throw exceptions under the following circumstances:
    /// - If the value stack has less than 2 items and there is an operation attempted on the values
    /// - If the closing or opening parenthesis is missing
    /// - If variables start with numerics
    /// - If, after all tokens have been processed, there are too many values left in the stack 
    ///   or the last operation in the stack is not + or -
    ///   
    /// The only legal tokens are the four operator symbols (+ - * /), left parentheses, right parentheses, 
    /// non-negative integers, whitespace, and variables consisting of one or more letters followed by one or more digits. 
    /// 
    /// </summary>

    public static class Evaluator
    {
        public delegate int Lookup(String v);
        public static int Evaluate(String exp, Lookup variableEvaluator)
        {
            Stack values = new Stack ();
            Stack operations = new Stack ();
            bool openParen = false;

            string[] substrings = Regex.Split(exp, "(\\()|(\\))|(-)|(\\+)|(\\*)|(/)");
            
            for (int i = 0; i < substrings.Length; i++ )
            {
                int myNum;
                bool isNumerical = int.TryParse(substrings[i], out myNum);
            
                    // If token is numeric, and there are more than one values in the stack, if operations stack has * or /
                    // act on it with the token and popped value, else, add token to the stack.
                if (isNumerical)
                {
                    if(values.Count > 0) {
                        if(operations.Peek().Equals("*")) 
                        {
                            int valueTop = (int)values.Pop();
                            operations.Pop();
                            values.Push(myNum * valueTop);
                        }
                        else if(operations.Peek().Equals("/")) 
                        {
                            if (myNum == 0)
                            {
                                throw new ArgumentException("Can't divide by zero");
                            }
                            else
                            {
                                int valueTop = (int)values.Pop();
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
                        if (operations.Count == 0 && values.Count == 0)
                        {
                            throw new ArgumentException("Not enough values to compute");
                        }

                        if (operations.Count > 0 && (operations.Peek().Equals("+") || operations.Peek().Equals("-")))
                        {
                            if (values.Count < 2)
                            {
                                throw new ArgumentException("Not enough values to compute");
                            }
                            int valueOne = (int)values.Pop();
                            int valueTwo = (int)values.Pop();
                            String op = (string) operations.Pop();
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
                        if (operations.Count == 0 && values.Count == 0)
                        {
                            throw new ArgumentException("Not enough values to compute");
                        }

                        operations.Push(substrings[i]);
                    }
                    else if (substrings[i] == "(")
                    {
                        operations.Push(substrings[i]);
                        openParen = true;
                    }
                        // If closing paren, clear out everything in the paren and check to see if preceding operation is * or /
                    else if (substrings[i] == ")")
                    {
                        int myNum2;
                        bool isNumerical2 = int.TryParse(substrings[i + 1], out myNum2);
                        if(isNumerical2) 
                        {
                            throw new ArgumentException("need operator after closing paren, dummy");
                        }
                        
                        if (!openParen)
                        {
                            throw new ArgumentException("There was no opening paren, can not be a closeing paren");
                        }
                        if (operations.Peek().Equals("+") || operations.Peek().Equals("-"))
                        {
                            if (values.Count < 2)
                            {
                                throw new ArgumentException("Not enough values to compute");
                            }
                            else
                            {
                                if (operations.Peek().Equals("+"))
                                {
                                    int valueOne = (int)values.Pop();
                                    int valueTwo = (int)values.Pop();
                                    operations.Pop();
                                    values.Push(valueOne + valueTwo);
                                    // If the next item in the operations stack is (, pop it off but then check to see if next operation after that is * or /
                                    if (operations.Peek().Equals("("))
                                    {
                                        operations.Pop();
                                        if (operations.Count > 0 && (operations.Peek().Equals("*") || operations.Peek().Equals("/")))
                                        {
                                            if (values.Count < 2)
                                            {
                                                throw new ArgumentException("Not enough values to compute");
                                            }
                                            else
                                            {
                                                if (operations.Peek().Equals("*"))
                                                {
                                                    int valueOnes = (int)values.Pop();
                                                    int valueTwos = (int)values.Pop();
                                                    operations.Pop();
                                                    values.Push(valueOnes * valueTwos);
                                                }
                                                else if (operations.Peek().Equals("/"))
                                                {
                                                    int valueOnes = (int)values.Pop();
                                                    int valueTwos = (int)values.Pop();
                                                    operations.Pop();
                                                    values.Push(valueTwos / valueOnes);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Missing closing parenthesis");
                                    }
                                }
                                else
                                {
                                    int valueOne = (int)values.Pop();
                                    int valueTwo = (int)values.Pop();
                                    operations.Pop();
                                    values.Push(valueTwo - valueOne);
                                    // If the next item in the operations stack is (, pop it off but then check to see if next operation after that is * or /
                                    if (operations.Peek().Equals("("))
                                    {
                                        operations.Pop();
                                        if (operations.Count > 0 && (operations.Peek().Equals("*") || operations.Peek().Equals("/")))
                                        {
                                            if (values.Count < 2)
                                            {
                                                throw new ArgumentException("Not enough values to compute");
                                            }
                                            else
                                            {
                                                if (operations.Peek().Equals("*"))
                                                {
                                                    int valueOnes = (int)values.Pop();
                                                    int valueTwos = (int)values.Pop();
                                                    operations.Pop();
                                                    values.Push(valueOnes * valueTwos);
                                                }
                                                else if (operations.Peek().Equals("/"))
                                                {
                                                    int valueOnes = (int)values.Pop();
                                                    int valueTwos = (int)values.Pop();
                                                    operations.Pop();
                                                    values.Push(valueTwos / valueOnes);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Missing closing parenthesis");
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
                                if (values.Count < 2)
                                {
                                    throw new ArgumentException("Not enough values to compute");
                                }
                                else
                                {
                                    if (operations.Peek().Equals("*"))
                                    {
                                        int valueOne = (int)values.Pop();
                                        int valueTwo = (int)values.Pop();
                                        operations.Pop();
                                        values.Push(valueOne * valueTwo);
                                    }
                                    else if (operations.Peek().Equals("/"))
                                    {
                                        int valueOne = (int)values.Pop();
                                        int valueTwo = (int)values.Pop();
                                        operations.Pop();
                                        values.Push(valueTwo / valueOne);
                                    }
                                }
                            }
                        }
                            // If token is * or /, pop two values and act on it
                        else if (operations.Peek().Equals("*") || operations.Peek().Equals("/"))
                        {
                            if (values.Count < 2)
                            {
                                throw new ArgumentException("Not enough values to compute");
                            }
                            else
                            {
                                if (operations.Peek().Equals("*"))
                                {
                                    int valueOne = (int)values.Pop();
                                    int valueTwo = (int)values.Pop();
                                    operations.Pop();
                                    values.Push(valueOne * valueTwo);
                                }
                                else if (operations.Peek().Equals("/"))
                                {
                                    int valueOne = (int)values.Pop();
                                    int valueTwo = (int)values.Pop();
                                    operations.Pop();
                                    values.Push(valueTwo / valueOne);
                                }
                            }
                        }
                        else
                        {
                            throw new ArgumentException("Missing operations between parenthesis");
                        }
                    }
                        // Whatever is left, however improbable, must be a variable.  Look it up
                    else
                    {
                        String s = substrings[i];

                        if (Char.IsWhiteSpace(s[s.Length - 1]))
                        {
                            s = s.Remove(s.Length - 1);
                        }
                        if (Char.IsWhiteSpace(s[0]))
                        {
                            s = s.Substring(1);
                        }
                        if(Char.IsNumber(s[0]))
                        {
                            throw new ArgumentException("Variable can't start with a number, started with: " + s[0]);
                        }
                        if (!Char.IsNumber(s[1]))
                        {
                            throw new ArgumentException("Variable must be of the form char + number");
                        }
                        else 
                        {
                            if (values.Count > 0)
                            {
                                if (operations.Peek().Equals("*"))
                                {
                                    int valueTop = (int)values.Pop();
                                    operations.Pop();
                                    values.Push(variableEvaluator(s) * valueTop);
                                }
                                else if (operations.Peek().Equals("/"))
                                {
                                    if (variableEvaluator(s) == 0)
                                    {
                                        throw new ArgumentException("Can't divide by zero");
                                    }
                                    else
                                    {
                                        int valueTop = (int)values.Pop();
                                        operations.Pop();
                                        values.Push(valueTop / variableEvaluator(s));
                                    }
                                }
                                else
                                {
                                    values.Push(variableEvaluator(s));
                                }
                            }
                            else
                            {
                                values.Push(variableEvaluator(s));
                            }
                        }
                    }
                }
            }
                // Once all tokens have been processed, clear out the last values, operate on them if there is more than one and return final value
            if (operations.Count == 0 && values.Count == 1)
            {
                return (int)values.Pop();
            }
            if (operations.Count == 1 && values.Count == 1)
            {
                throw new ArgumentException("There are not enough values left to compute");
            }
            if (operations.Count == 0 && values.Count != 1)
            {
                throw new ArgumentException("There should only be one value left but there are " + values.Count + " left.");
            }
            else 
            {
                if (operations.Peek().Equals("*") || operations.Peek().Equals("/"))
                {
                    throw new ArgumentException("The last value in the operations stack should be + or - but is " + operations.Peek());
                }
                else
                {
                    if (operations.Peek().Equals("+"))
                    {
                        int valueOne = (int)values.Pop();
                        int valueTwo = (int)values.Pop();
                        var operationsTop = operations.Pop();
                        return valueOne + valueTwo;
                    }
                    else
                    {
                        int valueOne = (int)values.Pop();
                        int valueTwo = (int)values.Pop();
                        var operationsTop = operations.Pop();
                        return valueTwo - valueOne;
                    } 
                }
            }
        }
    }
}
