﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpreadsheetUtilities;
using System.Text.RegularExpressions;

namespace SS
{
    /// <summary>
    /// A Spreadsheet object represents the state of a simple spreadsheet.  A 
    /// spreadsheet consists of an infinite number of named cells.
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

        private DependencyGraph dependency_graph;
        private Dictionary<String, Cell> cell;
        private HashSet<String> dependents;

        /// <summary>
        /// Constructor for Spreadsheet with zero arguments
        /// </summary>
        public Spreadsheet()
        {
            dependency_graph = new DependencyGraph();
            cell = new Dictionary<String, Cell>();
        }

        /// <summary>
        /// Enumerates the names of all the non-empty cells in the spreadsheet.
        /// </summary>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            //Foreach loops for returning each nonempty cell
            foreach (KeyValuePair<String, Cell> entry in cell)
            {
                if (entry.Value.Contents != "")
                    yield return entry.Key;
            }
        }


        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the contents (as opposed to the value) of the named cell.  The return
        /// value should be either a string, a double, or a Formula.
        public override object GetCellContents(string name)
        {
            if (string.IsNullOrEmpty(name) || !Regex.IsMatch(name, @"^[a-zA-Z_](?: [a-zA-Z_]|\d)*"))
                throw new InvalidNameException();

            if (!cell.ContainsKey(name))
                return "";
            return cell[name].Contents;

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
        public override ISet<string> SetCellContents(string name, double number)
        {
            if (name == null || !Regex.IsMatch(name, @"^[a-zA-Z_](?: [a-zA-Z_]|\d)*"))
                throw new InvalidNameException();

            if (cell.ContainsKey(name))
            {
                cell[name].Contents = number;
            }
            else
            {
                cell.Add(name, new Cell(name, number));
            }

            dependents = new HashSet<string>(GetCellsToRecalculate(name));
            dependents.Add(name);
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
        public override ISet<string> SetCellContents(string name, string text)
        {
            if (text == null)
                throw new ArgumentNullException();

            if (name == null || !Regex.IsMatch(name, @"^[a-zA-Z_](?: [a-zA-Z_]|\d)*"))
                throw new InvalidNameException();

            if (cell.ContainsKey(name))
            {
                cell[name].Contents = text;
            }
            else
            {
                cell.Add(name, new Cell(name, text));
            }
            dependents = new HashSet<string>(GetCellsToRecalculate(name));
            dependents.Add(name);
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
            if (formula == null)
                throw new ArgumentNullException();

            if (name == null || !Regex.IsMatch(name, @"^[a-zA-Z_](?: [a-zA-Z_]|\d)*"))
                throw new InvalidNameException();
            if (cell.ContainsKey(name))
            {
                cell[name].Contents = formula;
            }
            else
            {
                cell.Add(name, new Cell(name, formula));
            }

            foreach (string var in formula.GetVariables())
            {
                try
                {
                    dependency_graph.AddDependency(var, name);
                }
                catch (InvalidOperationException)
                {
                    throw new CircularException();
                }
            }
            return new HashSet<string>(GetCellsToRecalculate(name));
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
                throw new ArgumentNullException();

            if (!Regex.IsMatch(name, @"^[a-zA-Z_](?: [a-zA-Z_]|\d)*"))
                throw new InvalidNameException();
            return dependency_graph.GetDependents(name);
        }

        /// <summary>
        /// Private class for representing a Cell and what a Cell contains.
        /// </summary>
        private class Cell
        {
            /// <summary>
            /// Getter/Setter for Cell name
            /// </summary>
            public String Name { get; set; }

            /// <summary>
            /// Getter/Setter for Cell contents
            /// </summary>
            public object Contents { get; set; }
            /// <summary>
            /// Cell constructor
            /// </summary>
            /// <param name="cell_name"> new cell name</param>
            /// <param name="cell_contents">new cell contents</param>
            public Cell(string cell_name, object cell_contents)
            {
                Name = cell_name;
                Contents = cell_contents;
            }


        }
    }
}
