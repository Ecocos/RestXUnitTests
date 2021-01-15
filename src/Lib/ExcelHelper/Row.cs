﻿using System.Xml.Serialization;

namespace RestXUnitTests.ExcelHelper
{
    /// <summary>
    /// (c) 2014 Vienna, Dietmar Schoder
    /// 
    /// Code Project Open License (CPOL) 1.02
    /// 
    /// Deals with an Excel row
    /// </summary>
    public class Row
    {
        [XmlElement("c")]
        public Cell[] FilledCells;
        [XmlIgnore]
        public Cell[] Cells;

        public void ExpandCells(int NumberOfColumns)
        {
            Cells = new Cell[NumberOfColumns];
            if (FilledCells != null)
            {
                foreach (var cell in FilledCells)
                    Cells[cell.ColumnIndex] = cell;
            }
            FilledCells = null;
        }
    }
}
