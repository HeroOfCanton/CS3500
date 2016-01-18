using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            // Test multiplication
            if (FormulaEvaluator.Evaluator.Evaluate("1 * 0", look) != 0)
            {
                Console.WriteLine("TEST FAILED: 1 * 0 should be 0 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("1 * 0", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("1 * 2 + 4", look) != 6)
            {
                Console.WriteLine("TEST FAILED: 1 * 2 + 4 should be 6 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("1 * 2 + 4", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("1 * 2 * 3", look) != 6)
            {
                Console.WriteLine("TEST FAILED: 1 * 2 * 3 should be 6 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("1 * 2 * 3", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("(1 * 2)", look) != 2)
            {
                Console.WriteLine("TEST FAILED: (1 * 2) should be 2 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("(1 * 2)", look));
            }

            //Test addition
            if (FormulaEvaluator.Evaluator.Evaluate("1 + 2", look) != 3)
            {
                Console.WriteLine("TEST FAILED: 1 + 2 should be 3 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("1 + 2", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("1 + 2 + 3", look) != 6)
            {
                Console.WriteLine("TEST FAILED: 1 + 2 + 3 should be 6 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("1 + 2 + 3", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("1 + 2 * 3", look) != 7)
            {
                Console.WriteLine("TEST FAILED: 1 + 2 * 3 should be 7 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("1 + 2 * 3", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("(1 + 2)", look) != 3)
            {
                Console.WriteLine("TEST FAILED: (1 + 2) should be 3 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("(1 + 2)", look));
            }

            //Test subtraction
            if (FormulaEvaluator.Evaluator.Evaluate("10 - 2", look) != 8)
            {
                Console.WriteLine("TEST FAILED: 10 - 2 should be 8 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("10 - 2", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("10 - 2 - 4 / 4", look) != 7)
            {
                Console.WriteLine("TEST FAILED: 10 - 2 - 4 / 4 should be 7 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("10 - 2 - 4 / 4", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("(10 - 2)", look) != 8)
            {
                Console.WriteLine("TEST FAILED: (10 - 2) should be 8 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("(10 - 2)", look));
            }

            //Test Division
            if (FormulaEvaluator.Evaluator.Evaluate("8 / 4", look) != 2)
            {
                Console.WriteLine("TEST FAILED: 8 / 4 should be 2 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("8 / 4", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("2 + 8 / 4", look) != 4)
            {
                Console.WriteLine("TEST FAILED: 2 + 8 / 4 should be 4 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("2 + 8 / 4", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("(10 / 2)", look) != 5)
            {
                Console.WriteLine("TEST FAILED: (10 / 2) should be 5 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("(10 / 2)", look));
            }

            //Test variables
            if (FormulaEvaluator.Evaluator.Evaluate("a1 + 2 + 3", look) != 6)
            {
                Console.WriteLine("TEST FAILED: a1 + 2 + 3 should be 6 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("a1 + 2 + 3", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("1 + a2 + 3", look) != 6)
            {
                Console.WriteLine("TEST FAILED: 1 + a2 + 3 should be 6 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("1 + a2 + 3", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("a1 + a2 + a3", look) != 2)
            {
                Console.WriteLine("TEST FAILED: a1 + a2 + a3 should be 2 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("a1 + a2 + a3", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("1 + a2 * (a1 + 3)", look) != 9)
            {
                Console.WriteLine("TEST FAILED: 1 + a2 * (a1 + 3) should be 9 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("1 + a2 * (a1 + 3)", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("1 + a2 - (a1 + 3)", look) != -1)
            {
                Console.WriteLine("TEST FAILED: 1 + a2 - (a1 + 3) should be -1 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("1 + a2 - (a1 + 3)", look));
            }
            try
            {
                if (FormulaEvaluator.Evaluator.Evaluate("1a + 2 + 3", look) != 6)
                {
                    Console.WriteLine("TEST FAILED: a1 + 2 + 3 should be 6 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("a1 + 2 + 3", look));
                }
            }
            catch
            {
                ArgumentException e; 
                Console.WriteLine("Exception caught");
            }

            //Test OoO
            if (FormulaEvaluator.Evaluator.Evaluate("(10 / 2) * 4 + 9 / 3", look) != 23)
            {
                Console.WriteLine("TEST FAILED: (10 / 2) * 4 + 9 / 3 should be 23 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("(10 / 2) * 4 + 9 / 3", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("9 / 3 + (10 / 2) * 4", look) != 23)
            {
                Console.WriteLine("TEST FAILED: (10 / 2) * 4 + 9 / 3 should be 23 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("(10 / 2) * 4 + 9 / 3", look));
            }
            if (FormulaEvaluator.Evaluator.Evaluate("(10 / 2) * 4 + (9 / 3)", look) != 23)
            {
                Console.WriteLine("TEST FAILED: (10 / 2) * 4 + (9 / 3) should be 23 but resulted in: " + FormulaEvaluator.Evaluator.Evaluate("(10 / 2) * 4 + (9 / 3)", look));
            }

            Console.WriteLine("TESTS PASSED");
            Console.ReadLine();
        }
        public static int look(String s)
        {
            String t = s;
            if (t.Equals("a1"))
            {
                return 1;
            }
            else if (t.Equals("a2"))
            {
                return 2;
            }
            else if (t.Equals("a3"))
            {
                return -1;
            }
            else if (t.Equals("a4"))
            {
                return 0;
            }
            else if (t.Equals("a5"))
            {
                return 10;
            }
            else
            {
                return 0;
            }
        }
    }
}
