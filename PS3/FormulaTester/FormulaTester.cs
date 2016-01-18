using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;
using System.Collections.Generic;

namespace FormulaTester
{
    [TestClass]
    public class FormulaTester
    {
        [TestMethod]
        public void TestConstructor()
        {
            Formula first = new Formula("x2+y3", Normalize, Validate);   
        }
        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructor2()
        {
            Formula third = new Formula("2x+y3", Normalize, Validate);
        }

        [TestMethod]
        public void TestConstructor3()
        {
            Formula second = new Formula("x+y3", Normalize, Validate);
        }

        [TestMethod]
        public void TestEquals()
        {
            Formula one = new Formula("x1+y2", Normalize, Validate);
            Assert.IsTrue(one.Equals(new Formula("X1+Y2")));
        }

        [TestMethod]
        public void TestEquals2()
        {
            Formula two = new Formula("x1+y2");
            Assert.IsFalse(two.Equals(new Formula("X1+Y2")));
        }

        [TestMethod]
        public void TestEquals3()
        {
            Formula three = new Formula("x1+y2");
            Assert.IsFalse(three.Equals(new Formula("y2+x1")));
        }

        [TestMethod]
        public void TestEquals4()
        {
            Formula four = new Formula("2.0 + x7");
            Assert.IsTrue(four.Equals(new Formula("2.000 + x7")));
        }

        [TestMethod]
        public void TestEquals5()
        {
            Formula f = new Formula("x7 + 7");
            Assert.IsTrue(f != new Formula("2.000 + x7"));
        }

        [TestMethod]
        public void TestEquals6()
        {
            Formula f = new Formula("x7 + 7");
            Assert.IsFalse(f != new Formula("x7 + 7"));
        }

        [TestMethod]
        public void TestEquals7()
        {
            Formula f = new Formula("x7 + 7", Normalize, s => true);
            Assert.IsTrue(f == new Formula("X7 + 7"));
        }

        [TestMethod]
        public void TestEquals8()
        {
            Formula f = new Formula("x7 + 7");
            Assert.IsFalse(f == new Formula("2.000 + x7"));
        }

        [TestMethod]
        public void TestEquals9()
        {
            Formula f = new Formula("x7 * 7");
            Assert.IsFalse(f == new Formula("x7 * (7+3)"));
        }

        [TestMethod]
        public void TestGetVariables()
        {
            string[] knownVariables = new string[3];
            knownVariables[0] = "X";
            knownVariables[1] = "Y";
            knownVariables[2] = "Z";

            List<string> variables = new List<string>();
            foreach (string s in new Formula("x+y*z", Normalize, s => true).GetVariables())
            {
                variables.Add(s);
            }

            bool exists = true;
            foreach (string s in knownVariables)
            {
                if (!variables.Contains(s))
                {
                    exists = false;
                }

            }

            Assert.IsTrue(exists);
            Assert.IsTrue(variables.Count == 3);
        }

        [TestMethod]
        public void TestGetVariables2()
        {
            string[] knownVariables = new string[2];
            knownVariables[0] = "X";
            knownVariables[1] = "Z";

            List<string> variable = new List<string>();
            foreach (string s in new Formula("x+X*z", Normalize, s => true).GetVariables())
            {
                variable.Add(s);
            }

            bool exists = true;
            foreach (string s in knownVariables)
            {
                if (!variable.Contains(s))
                {
                    exists = false;
                }

            }

            Assert.IsTrue(exists);
            Assert.IsTrue(variable.Count == 2);
        }

        [TestMethod]
        public void TestGetVariables3()
        {
            string[] knownVariables = new string[3];
            knownVariables[0] = "x";
            knownVariables[1] = "X";
            knownVariables[2] = "z";

            List<string> variable = new List<string>();
            foreach (string s in new Formula("x+X*z").GetVariables())
            {
                variable.Add(s);
            }

            bool exists = true;
            foreach (string s in knownVariables)
            {
                if (!variable.Contains(s))
                {
                    exists = false;
                }
            }

            Assert.IsTrue(exists);
            Assert.IsTrue(variable.Count == 3);
        }

        [TestMethod]
        public void TestToString()
        {
            Formula f = new Formula("x + y", Normalize, s => true);
            String temp = "X+Y";

            Assert.IsTrue(f.ToString().Equals(temp));
        }

        [TestMethod]
        public void TestToString2()
        {
            Formula f = new Formula("x + Y");
            String temp = "x+Y";

            Assert.IsTrue(f.ToString().Equals(temp));
        }

        [TestMethod]
        public void TestGetHashCode()
        {
            Formula f1 = new Formula("x1+y2", Normalize, Validate);
            Formula f2 = new Formula("X1+Y2");
            Assert.IsTrue(f1.GetHashCode() == f2.GetHashCode());
        }

        [TestMethod()]
        public void Test1()
        {
            Formula f = new Formula("5");
            Assert.AreEqual(5.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test2()
        {
            Formula f = new Formula("X5");
            Assert.AreEqual(13.0, f.Evaluate(s => 13));
        }

        [TestMethod()]
        public void Test3()
        {
            Formula f = new Formula("5+3");
            Assert.AreEqual(8.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test4()
        {
            Formula f = new Formula("18-10");
            Assert.AreEqual(8.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test5()
        {
            Formula f = new Formula("2*4");
            Assert.AreEqual(8.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test6()
        {
            Formula f = new Formula("16/2");
            Assert.AreEqual(8.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test7()
        {
            Formula f = new Formula("2+X1");
            Assert.AreEqual(6.0, f.Evaluate(s => 4));
        }

        [TestMethod()]
        public void Test9()
        {
            Formula f = new Formula("2*6+3");
            Assert.AreEqual(15.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test10()
        {
            Formula f = new Formula("2+6*3");
            Assert.AreEqual(20.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test11()
        {
            Formula f = new Formula("(2+6)*3");
            Assert.AreEqual(24.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test12()
        {
            Formula f = new Formula("2*(3+5)");
            Assert.AreEqual(16.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test13()
        {
            Formula f = new Formula("2+(3+5)");
            Assert.AreEqual(10.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test14()
        {
            Formula f = new Formula("2+(3+5*9)");
            Assert.AreEqual(50.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test15()
        {
            Formula f = new Formula("2+3*(3+5)");
            Assert.AreEqual(26.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test16()
        {
            Formula f = new Formula("2+3*5+(3+4*8)*5+2");
            Assert.AreEqual(194.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test17()
        {
            Formula f = new Formula("5/0");
            Assert.IsTrue(f.Evaluate(s => 0) is FormulaError);
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void Test18()
        {
            Formula f = new Formula("+");
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void Test19()
        {
            Formula f = new Formula("2+5+");
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void Test20()
        {
            Formula f = new Formula("2+5*7)");
        }

        [TestMethod()]
        public void Test21()
        {
            Formula f = new Formula("xx");
            Assert.AreEqual(0.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        public void Test22()
        {
            Formula f = new Formula("5+xx");
            Assert.AreEqual(5.0, f.Evaluate(s => 0));
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void Test23()
        {
            Formula f = new Formula("5+7+(5)8");
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void Test24()
        {
            Formula f = new Formula("");
        }

        [TestMethod()]
        public void Test25()
        {
            Formula f = new Formula("y1*3-8/2+4*(8-9*2)/14*x7");
            Assert.AreEqual(5.142857142, (double)f.Evaluate(s => (s == "x7") ? 1 : 4), 1e-9);
        }

        [TestMethod()]
        public void Test26()
        {
            Formula f = new Formula("x1+(x2+(x3+(x4+(x5+x6))))");
            Assert.AreEqual(6.0, f.Evaluate(s => 1));
        }

        [TestMethod()]
        public void Test27()
        {
            Formula f = new Formula("((((x1+x2)+x3)+x4)+x5)+x6");
            Assert.AreEqual(12.0, f.Evaluate(s => 2));
        }

        [TestMethod()]
        public void Test28()
        {
            Formula f = new Formula("a4-a4*a4/a4");
            Assert.AreEqual(0.0, f.Evaluate(s => 3));
        }

        [TestMethod()]
        public void Test29()
        {
        
            Formula f = new Formula("x+7");
            Assert.AreEqual(9.0, f.Evaluate(s => 2));

            Formula f1 = new Formula("x+7", Normalize, Validate);
            Assert.AreEqual(11.0, f.Evaluate(s => 4));
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void Test30()
        {
            Formula f = new Formula("5+7(5)8");
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void Test31()
        {
            Formula f = new Formula("5+7(5()8");
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void Test32()
        {
            Formula f = new Formula("5+7^5X%5()8");
        }

        [TestMethod()]
        public void Test33()
        {
            Formula f = new Formula("4-3*1/a4");
            Assert.IsTrue(f.Evaluate(s => 0) is FormulaError);
        }

        [TestMethod()]
        [ExpectedException(typeof(FormulaFormatException))]
        public void Test34()
        {
            Formula f = new Formula("5+-7(5()8");
        }

        public static string Normalize(string formula)
        {
            return formula.ToUpper();
        }

        public static bool Validate(string formula)
        {
            if (formula[0].Equals("_") || Char.IsLetter(formula[0]))
            {
                if (formula.Length == 1)
                {
                    return true;
                }
                for (int i = 1; i < formula.Length; i++)
                {
                    if (Char.IsLetter(formula[i]) || Char.IsNumber(formula[i]) || formula[i].Equals("_"))
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
