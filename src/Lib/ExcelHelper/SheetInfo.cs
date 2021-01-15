using System;
using System.Xml.Serialization;

namespace RestXUnitTests.ExcelHelper
{
    /// <summary>
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// 
    /// retrieves data for worksheets
    /// </summary>
    /// <remarks>
    /// In the xml files like '/worksheets/sheet1.xml
    /// </remarks>
    [Serializable()]
    [XmlRoot("workbook", Namespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main")]
    public class SheetInfo
    {
        [XmlArray("sheets")]
        [XmlArrayItem("sheet")]
        public SheetInfoItem[] SheetInfoItems;
    }
}
