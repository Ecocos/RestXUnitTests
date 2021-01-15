using System.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RestXUnitTests.ExcelHelper;
using RestXUnitTests.Lib;

namespace RestXUnitTests.Helpers
{
    /// <summary>
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// 
    /// The main helper class for retrieving information from an excel spreadsheet
    /// </summary>
    public class SpreadsheetHelper : RestTestBase
    {

        /// <summary>
        /// This uses a specific row in the spreadsheet that contains the variable names used for the columns. This allow us to change columns without worrying about changing the code.
        /// </summary>
        /// <param name="varNames">a dictionary with varnames as keys. the value is an int (initially 0) for the column number</param>
        /// <param name="variableNameRow">The actual row which has the column names used in our dictionary</param>
        /// <returns>returns dictionary taht is passed in with the column numbers populated</returns>
        public static Dictionary<string, int> GetColsForVariables(Row variableNameRow, Dictionary<string, int> initialColumnNumberLookup = null) //load automatically if no param list given
        {

            Dictionary<string, int> varNames = initialColumnNumberLookup; //we have names for the columns in the spreadsheet to make code readability easier
            int colNum = 0;
            string colText = null;
            if (variableNameRow == null || varNames == null)
            {
                log.Error($"{MethodBase.GetCurrentMethod().Name} - null parameter passed.");
                throw new ArgumentException($"{MethodBase.GetCurrentMethod().Name} - null parameter passed.");
            }
            do
            {
                var cell = variableNameRow.Cells[colNum];
                if (cell == null)
                    break;
                if (string.IsNullOrEmpty(cell.Text) || !varNames.Keys.Contains(cell.Text))
                    continue;
                colText = cell.Text;
                varNames[colText] = colNum; //add which column number maps to this variable
            } while (!string.IsNullOrEmpty(colText) && (++colNum < variableNameRow.Cells.Length));
            return varNames;
        }


        /// <summary>
        /// loads rows from a spreadsheet/workbook into IRowObjects to be used by the xUnit theories
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="spreadsheetPath"></param>
        /// <param name="workbookName"></param>
        /// <param name="ColNamesRowIndex">a row in the spreadsheet with column names used for dictionary</param>
        /// <returns>List of IRowObjects from workbook</returns>
        /// 
        public static List<T> LoadRowDataFromSpreadsheet<T>(string spreadsheetPath, string workbookName, int ColNamesRowIndex = 1) where T : IRowObject, new()
        {
            if (!File.Exists(spreadsheetPath))
            {
                log.Error($"{MethodBase.GetCurrentMethod().Name} - file not found: '{spreadsheetPath}.");
                throw new FileNotFoundException(spreadsheetPath);
            }
                
            var workbook = new Workbook(spreadsheetPath);
            var worksheet = workbook[workbookName];
            //get the column numbers for the variables we are interested in
            // we have names for the columns in the spreadsheet to make code readability easier (altho' we could use headings if necessary)
            if (worksheet.Rows == null)
            {
                log.Error($"{MethodBase.GetCurrentMethod().Name} - file not found: '{spreadsheetPath}.");
                throw new ArgumentOutOfRangeException("No populated rows found in spreadsheet");
            }
                
            var maxRows = worksheet.Rows.Length - ColNamesRowIndex; //skip header rows
            var columnNames = GetColNameMapping(worksheet, ColNamesRowIndex); //gives us column name to column # mapping
            var rowsList = new List<T>();
            int currentRow = ColNamesRowIndex; //start on line after the column name row
            while (--maxRows > 0)
            {
                if (worksheet.Rows[currentRow] == null)
                    break;
                rowsList.Add(LoadRow<T>(++currentRow, worksheet.Rows[currentRow], columnNames)); //add row to our list
            }
            return rowsList;
        }


        /// <summary>
        /// maps the column name to the column #
        /// </summary>
        /// <param name="worksheet">the worksheet we are using as the row source</param>
        /// <param name="colNamesRowIndex">the index of the row to use for the column names</param>
        private static Dictionary<int, string> GetColNameMapping(worksheet worksheet, int colNamesRowIndex)
        {
            var columnLookup = new Dictionary<int, string>();
            var namedColRow = worksheet.Rows[colNamesRowIndex];
            int index = 0;
            foreach (var cell in namedColRow.Cells)
            {
                if (cell == null || (string.IsNullOrEmpty(cell.Text))) //only go until the cells are empty
                    break;
                columnLookup.Add(index++, cell.Text);
            }
            return columnLookup;
        }

        /// <summary>
        /// Loads an individual row from the spreadsheet
        /// </summary>
        /// <typeparam name="T">A type based on an IRowObject, used to describe a test case as a spreadsheet row</typeparam>
        /// <param name="rowNum">used to track rows for reports</param>
        /// <returns>IRowObject with the dictionary populated</returns>
        private static T LoadRow<T>(int rowNum, Row thisRow, Dictionary<int, string> colNames) where T : IRowObject, new()
        {
            if (thisRow.Cells[0] == null)
                return default;
            var cellValueDictionary = new Dictionary<string, string>();
            for (int i = 0; i < colNames.Count; i++)
            {
                if ((colNames[i] != null) && (thisRow.Cells[i] != null))
                {
                    cellValueDictionary.Add(colNames[i], thisRow.Cells[i].Text);
                }
            }
            var rowObj = new T()
            {
                RowNum = rowNum,
                CellValues = cellValueDictionary
            };
            return rowObj;

        }

    }
}
