using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SS;
using System.Collections.Generic;
using SpreadsheetUtilities;

namespace SpreadsheetTests
{
    [TestClass]
    public class UnitTest1
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
            sheet.SetCellContents("A1", 10.5);
            sheet.SetCellContents("B1", "horse");
            sheet.SetCellContents("E1", 1.5);
            sheet.SetCellContents("A", "dad");

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
            sheet.SetCellContents("%", "cat");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestSetCellContentsStringInvalidName2()
        {
            Spreadsheet sheet = new Spreadsheet();
            string s = null;
            sheet.SetCellContents("A1", s);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetCellContentsDoubleInvalidName()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("%", 10.0);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestSetCellContentsFormulaInvalidName()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("%", new Formula("9+9"));
        }

        [TestMethod]
        public void TestSetCellContentsDouble()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("A1", 10.5);

            Assert.IsTrue(sheet.GetCellContents("A1").Equals(10.5));
        }

        [TestMethod]
        public void TestSetCellContentsString()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("B1", "horse");

            Assert.IsTrue(sheet.GetCellContents("B1").Equals("horse"));
        }

        [TestMethod]
        public void TestSetCellContentsFormula()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("C1", new Formula("9 + 9 / 2"));

            Assert.IsTrue(sheet.GetCellContents("C1").ToString().Equals("9+9/2"));
        }

        [TestMethod]
        public void TestSetCellContentsDoubleReplace()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("A1", 10.5);
            sheet.SetCellContents("A1", -4.6);

            Assert.IsTrue(sheet.GetCellContents("A1").Equals(-4.6));
        }

        [TestMethod]
        public void TestSetCellContentsStringReplace()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("B1", "horse");
            sheet.SetCellContents("B1", "cow");

            Assert.IsTrue(sheet.GetCellContents("B1").Equals("cow"));
        }

        [TestMethod]
        public void TestSetCellContentsFormulaReplace()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("C1", new Formula("9 + 9 / 2"));
            sheet.SetCellContents("C1", new Formula("A1 + 9 / B2"));

            Assert.IsTrue(sheet.GetCellContents("C1").ToString().Equals("A1+9/B2"));
        }

        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// set {A1, B1, C1} is returned.
        [TestMethod]
        public void TestDependents()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("B1", new Formula("A1 * 2"));
            sheet.SetCellContents("C1", new Formula("B1 + A1"));
            HashSet<string> dependents = (HashSet<string>)sheet.SetCellContents("A1", 3);

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
            sheet.SetCellContents("B1", new Formula("A1"));
            sheet.SetCellContents("A1", new Formula("B1"));
        }

        [TestMethod]
        [ExpectedException(typeof(CircularException))]
        public void TestCircleException2()
        {
            Spreadsheet sheet = new Spreadsheet();
            sheet.SetCellContents("A1", new Formula("B1"));
            sheet.SetCellContents("A1", new Formula("B1 + C1"));
            sheet.SetCellContents("C1", new Formula("D2"));
            sheet.SetCellContents("D2", new Formula("A1"));
        }
    }
}
