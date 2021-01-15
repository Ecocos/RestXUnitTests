using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace RestXUnitTests.ExcelHelper
{
    /// <summary>
    /// (c) 2014 Vienna, Dietmar Schoder
    /// 
    /// Code Project Open License (CPOL) 1.02
    /// 
    /// Deals with an Excel worksheet in an xlsx-file
    /// Update: Added WorksheetTitle (Author: Erich Tinguely)
    /// </summary>
    [Serializable()]
    [XmlRoot("worksheet", Namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main")]
    public class worksheet
    {
        [XmlArray("sheetData")]
        [XmlArrayItem("row")]
        public Row[] Rows;
        [XmlIgnore]
        public int NumberOfColumns; // Total number of columns in this worksheet
        public static int MaxColumnIndex = 0; // Temporary variable for import
        public static string WorksheetName {get;set;}
        private List<Row> _populatedRows;
        public void ExpandRows()
        {
            foreach (var row in Rows)
                row.ExpandCells(NumberOfColumns);
        }

        public string WorksheetTitle {get;set;}

        /// <summary>
        /// returns only rows that have a non-null cell in the first column that also has text
        /// </summary>
        public Row[] PopulatedRows
        {
            get
            {
                if(_populatedRows == null)
                {
                    var tempRows = Rows.Where(x => x.Cells[0] != null).ToList();
                    _populatedRows = tempRows.Where(x => !string.IsNullOrEmpty(x.Cells[0].Text)).ToList();
                }
                return _populatedRows.ToArray();
            }
        }
    }
}
