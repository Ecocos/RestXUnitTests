using System.Collections.Generic;

namespace RestXUnitTests.Helpers

{

    /// <summary>
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// 
    /// This is used when loading Excel Spreadsheets for test data. This represents a single row. 
    /// </summary>
    public interface IRowObject
    {
        /// <summary>
        /// which row on the spreadsheet this came from
        /// </summary>
        int RowNum { get; set; }

        /// <summary>
        /// key/value of column variable with names as key, cell value as the value
        /// </summary>
        Dictionary<string, string> CellValues { get; set; }
    }
}
