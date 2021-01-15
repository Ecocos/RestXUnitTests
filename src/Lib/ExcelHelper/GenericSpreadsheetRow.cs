using RestXUnitTests.Helpers;
using System.Collections.Generic;

namespace RestXUnitTests.ExcelHelper
{

    /// <summary>
    /// Represents a row (i.e. test case) from the Excel Spreadsheet
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// </summary>
    public class GenericSpreadsheetRow : IRowObject
    {

        public GenericSpreadsheetRow() { }

        /// <summary>
        /// The row number of this row in the spreadsheet
        /// </summary>
        public int RowNum { get; set; }

#pragma warning disable CA2227 // Collection properties should be read only

        public Dictionary<string, string> CellValues { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
 
    }
}