using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SS;
using System.Collections.Generic;
using SpreadsheetUtilities;

namespace SpreadsheetTests
{
    [TestClass]
    public class MyTests
    {
        [TestMethod]
        public void TestConstructor()
        {
            AbstractSpreadsheet sheet = new Spreadsheet();
            Assert.IsTrue(sheet is Spreadsheet);
            Assert.IsTrue(sheet is AbstractSpreadsheet);
        }

        [TestMethod]
        public void TestEmptyGetNamesOfCells()
        {
            Spreadsheet sheet = new Spreadsheet();
            var nameEnum = sheet.GetNamesOfAllNonemptyCells().GetEnumerator();
            nameEnum.MoveNext();
            Assert.IsTrue(nameEnum.Current == null);
        }

        [TestMethod]
        public void TestGetNamesOfNonEmptyCells()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "10.5");
            sheet.SetContentsOfCell("B1", "horse");
            sheet.SetContentsOfCell("E1", "1.5");
            sheet.SetContentsOfCell("A", "dad");

            var nameEnum = sheet.GetNamesOfAllNonemptyCells().GetEnumerator();

            List<object> names = new List<object>();
            for (int i = 0; i < 4; i++)
            {
                nameEnum.MoveNext();
                names.Add(nameEnum.Current);
            }

            Assert.IsTrue(names.Count == 4);

            Assert.IsTrue(names.Contains("A1"));
            Assert.IsTrue(names.Contains("B1"));
            Assert.IsTrue(names.Contains("E1"));
            Assert.IsTrue(names.Contains("A"));
        }

        [TestMethod]
        public void TestEmptyGetCellContents()
        {
            Spreadsheet sheet = new Spreadsheet();
            object content = sheet.GetCellContents("A1");

            Assert.IsTrue(content.Equals(""));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestEmptyGetCellContents2()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.GetCellContents(null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestGetCellContentsInvalidName()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.GetCellContents("1");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetCellContentsStringInvalidName()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("%", "cat");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestSetCellContentsStringInvalidName2()
        {
            Spreadsheet sheet = new Spreadsheet();
            string s = null;
            sheet.SetContentsOfCell("A1", s);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetCellContentsDoubleInvalidName()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("%", "10.0");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetCellContentsFormulaInvalidName()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("%", "=9+9");
        }

        [TestMethod]
        public void TestSetCellContentsDouble()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "10.5");

            Assert.IsTrue(sheet.GetCellContents("A1").Equals(10.5));
        }

        [TestMethod]
        public void TestSetCellContentsString()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("B1", "horse");

            Assert.IsTrue(sheet.GetCellContents("B1").Equals("horse"));
        }

        [TestMethod]
        public void TestSetCellContentsFormula()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("C1", "=9 + 9 / 2");

            Assert.IsTrue(sheet.GetCellContents("C1").ToString().Equals("9+9/2"));
        }

        [TestMethod]
        public void TestSetCellContentsDoubleReplace()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "10.5");
            sheet.SetContentsOfCell("A1", "-4.6");

            Assert.IsTrue(sheet.GetCellContents("A1").Equals(-4.6));
        }

        [TestMethod]
        public void TestSetCellContentsStringReplace()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("B1", "horse");
            sheet.SetContentsOfCell("B1", "cow");

            Assert.IsTrue(sheet.GetCellContents("B1").Equals("cow"));
        }

        [TestMethod]
        public void TestSetCellContentsFormulaReplace()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("C1", "=9 + 9 / 2");
            sheet.SetContentsOfCell("C1", "=A1 + 9 / B2");

            Assert.IsTrue(sheet.GetCellContents("C1").ToString().Equals("A1+9/B2"));
        }

        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        [TestMethod]
        public void TestDependents()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("B1", "=A1 * 2");
            sheet.SetContentsOfCell("C1", "=B1 + A1");
            HashSet<string> dependents = (HashSet<string>)sheet.SetContentsOfCell("A1", "3");

            Assert.IsTrue(dependents.Count == 3);
            Assert.IsTrue(dependents.Contains("A1"));
            Assert.IsTrue(dependents.Contains("C1"));
            Assert.IsTrue(dependents.Contains("B1"));
        }

        [TestMethod]
        [ExpectedException(typeof(CircularException))]
        public void TestCircleException()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("B1", "=A1");
            sheet.SetContentsOfCell("A1", "=B1");
        }

        [TestMethod]
        [ExpectedException(typeof(CircularException))]
        public void TestCircleException2()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetContentsOfCell("A1", "=B1");
            sheet.SetContentsOfCell("A1", "=B1 + C1");
            sheet.SetContentsOfCell("C1", "=D2");
            sheet.SetContentsOfCell("D2", "=A1");
        }

        // EMPTY SPREADSHEETS
        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void Test1()
        {
            Spreadsheet s = new Spreadsheet();
            s.GetCellContents(null);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void Test2()
        {
            Spreadsheet s = new Spreadsheet();
            s.GetCellContents("1AA");
        }

        [TestMethod()]
        public void Test3()
        {
            Spreadsheet s = new Spreadsheet();
            Assert.AreEqual("", s.GetCellContents("A2"));
        }

        // SETTING CELL TO A DOUBLE
        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void Test4()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell(null, "1.5");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void Test5()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("1A1A", "1.5");
        }

        [TestMethod()]
        public void Test6()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("Z7", "1.5");
            Assert.AreEqual(1.5, (double)s.GetCellContents("Z7"), 1e-9);
        }

        // SETTING CELL TO A STRING
        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test7()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A8", (string)null);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void Test8()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell(null, "hello");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void Test9()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("1AZ", "hello");
        }

        [TestMethod()]
        public void Test10()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("Z7", "hello");
            Assert.AreEqual("hello", s.GetCellContents("Z7"));
        }

        // SETTING CELL TO A FORMULA
        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Test11()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A8", null);
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void Test12()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell(null, "=2");
        }

        [TestMethod()]
        [ExpectedException(typeof(InvalidNameException))]
        public void Test13()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("1AZ", "=2");
        }

        [TestMethod()]
        public void Test14()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("Z7", "=3");
            Formula f = (Formula)s.GetCellContents("Z7");
            Assert.AreEqual(new Formula("3"), f);
            Assert.AreNotEqual(new Formula("2"), f);
        }

        // CIRCULAR FORMULA DETECTION
        [TestMethod()]
        [ExpectedException(typeof(CircularException))]
        public void Test15()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "=A2");
            s.SetContentsOfCell("A2", "=A1");
        }

        [TestMethod()]
        [ExpectedException(typeof(CircularException))]
        public void Test16()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "=A2+A3");
            s.SetContentsOfCell("A3", "=A4+A5");
            s.SetContentsOfCell("A5", "=A6+A7");
            s.SetContentsOfCell("A7", "=A1+A1");
        }

        [TestMethod()]
        [ExpectedException(typeof(CircularException))]
        public void Test17()
        {
            Spreadsheet s = new Spreadsheet();
            try
            {
                s.SetContentsOfCell("A1", "=A2+A3");
                s.SetContentsOfCell("A2", "15");
                s.SetContentsOfCell("A3", "30");
                s.SetContentsOfCell("A2", "=A3*A1");
            }
            catch (CircularException e)
            {
                Assert.AreEqual(15, (double)s.GetCellContents("A2"), 1e-9);
                throw e;
            }
        }

        // NONEMPTY CELLS
        [TestMethod()]
        public void Test18()
        {
            Spreadsheet s = new Spreadsheet();
            Assert.IsFalse(s.GetNamesOfAllNonemptyCells().GetEnumerator().MoveNext());
        }

        [TestMethod()]
        public void Test19()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("B1", "");
            Assert.IsFalse(s.GetNamesOfAllNonemptyCells().GetEnumerator().MoveNext());
        }

        [TestMethod()]
        public void Test20()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("B1", "hello");
            Assert.IsTrue(new HashSet<string>(s.GetNamesOfAllNonemptyCells()).SetEquals(new HashSet<string>() { "B1" }));
        }

        [TestMethod()]
        public void Test21()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("B1", "52.25");
            Assert.IsTrue(new HashSet<string>(s.GetNamesOfAllNonemptyCells()).SetEquals(new HashSet<string>() { "B1" }));
        }

        [TestMethod()]
        public void Test22()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("B1", "=3.5");
            Assert.IsTrue(new HashSet<string>(s.GetNamesOfAllNonemptyCells()).SetEquals(new HashSet<string>() { "B1" }));
        }

        [TestMethod()]
        public void Test23()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "17.2");
            s.SetContentsOfCell("C1", "hello");
            s.SetContentsOfCell("B1", "=3.5");
            Assert.IsTrue(new HashSet<string>(s.GetNamesOfAllNonemptyCells()).SetEquals(new HashSet<string>() { "A1", "B1", "C1" }));
        }

        // RETURN VALUE OF SET CELL CONTENTS
        [TestMethod()]
        public void Test24()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("B1", "hello");
            s.SetContentsOfCell("C1", "=5");
            Assert.IsTrue(s.SetContentsOfCell("A1", "17.2").SetEquals(new HashSet<string>() { "A1" }));
        }

        [TestMethod()]
        public void Test25()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "17.2");
            s.SetContentsOfCell("C1", "=5");
            Assert.IsTrue(s.SetContentsOfCell("B1", "hello").SetEquals(new HashSet<string>() { "B1" }));
        }

        [TestMethod()]
        public void Test26()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "17.2");
            s.SetContentsOfCell("B1", "hello");
            Assert.IsTrue(s.SetContentsOfCell("C1", "=5").SetEquals(new HashSet<string>() { "C1" }));
        }

        [TestMethod()]
        public void Test27()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "=A2+A3");
            s.SetContentsOfCell("A2", "6");
            s.SetContentsOfCell("A3", "=A2+A4");
            s.SetContentsOfCell("A4", "=A2+A5");
            Assert.IsTrue(s.SetContentsOfCell("A5", "82.5").SetEquals(new HashSet<string>() { "A5", "A4", "A3", "A1" }));
        }

        // CHANGING CELLS
        [TestMethod()]
        public void Test28()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "=A2+A3");
            s.SetContentsOfCell("A1", "2.5");
            Assert.AreEqual(2.5, (double)s.GetCellContents("A1"), 1e-9);
        }

        [TestMethod()]
        public void Test29()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "=A2+A3");
            s.SetContentsOfCell("A1", "Hello");
            Assert.AreEqual("Hello", (string)s.GetCellContents("A1"));
        }

        [TestMethod()]
        public void Test30()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "Hello");
            s.SetContentsOfCell("A1", "=23");
            Assert.AreEqual(new Formula("23"), (Formula)s.GetCellContents("A1"));
            Assert.AreNotEqual(new Formula("24"), (Formula)s.GetCellContents("A1"));
        }

        // STRESS TESTS
        [TestMethod()]
        public void Test31()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "=B1+B2");
            s.SetContentsOfCell("B1", "=C1-C2");
            s.SetContentsOfCell("B2", "=C3*C4");
            s.SetContentsOfCell("C1", "=D1*D2");
            s.SetContentsOfCell("C2", "=D3*D4");
            s.SetContentsOfCell("C3", "=D5*D6");
            s.SetContentsOfCell("C4", "=D7*D8");
            s.SetContentsOfCell("D1", "=E1");
            s.SetContentsOfCell("D2", "=E1");
            s.SetContentsOfCell("D3", "=E1");
            s.SetContentsOfCell("D4", "=E1");
            s.SetContentsOfCell("D5", "=E1");
            s.SetContentsOfCell("D6", "=E1");
            s.SetContentsOfCell("D7", "=E1");
            s.SetContentsOfCell("D8", "=E1");
            ISet<String> cells = s.SetContentsOfCell("E1", "0");
            Assert.IsTrue(new HashSet<string>() { "A1", "B1", "B2", "C1", "C2", "C3", "C4", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "E1" }.SetEquals(cells));
        }

        [TestMethod()]
        public void TestXMLSave()
        {
            Spreadsheet saveSheet = new Spreadsheet();
            saveSheet.SetContentsOfCell("A1", "=A2+A3");
            saveSheet.SetContentsOfCell("A2", "6");
            saveSheet.SetContentsOfCell("A3", "=A2+A4");
            saveSheet.SetContentsOfCell("A4", "=A2+A5");
            saveSheet.Save("testSave.xml");

            Spreadsheet loadSheet = new Spreadsheet("testSave.xml", s => true, s => s, "default");
            Assert.IsTrue(loadSheet.GetCellContents("A1").Equals(new Formula("A2+A3")));
            Assert.IsTrue(loadSheet.GetCellContents("A2").Equals(6.0));
            Assert.IsTrue(loadSheet.GetCellContents("A3").Equals(new Formula("A2+A4")));
            Assert.IsTrue(loadSheet.GetCellContents("A4").Equals(new Formula("A2+A5")));

        }
       
        private String randomName(Random rand)
        {
            return "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(rand.Next(26), 1) + (rand.Next(99) + 1);
        }

        private String randomFormula(Random rand)
        {
            String f = randomName(rand);
            for (int i = 0; i < 10; i++)
            {
                switch (rand.Next(4))
                {
                    case 0:
                        f += "+";
                        break;
                    case 1:
                        f += "-";
                        break;
                    case 2:
                        f += "*";
                        break;
                    case 3:
                        f += "/";
                        break;
                }
                switch (rand.Next(2))
                {
                    case 0:
                        f += 7.2;
                        break;
                    case 1:
                        f += randomName(rand);
                        break;
                }
            }
            return f;
        }
    }
}
