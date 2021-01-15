using System.Xml.Schema;
using System.Xml.Serialization;

namespace RestXUnitTests.ExcelHelper
{
    /// <summary>
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// 
    /// 
    /// items for relating sheet names to worksheets
    /// </summary>
    [XmlType(Namespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships")]
    public class SheetInfoItem
    {
 
        /// <summary>
        /// Used for converting from Excel column/row to column index starting at 0
        /// </summary>
        [XmlAttribute("name")]
        public string Name
        {
            get;set;
        }

        [XmlAttribute("id", Namespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships", Form = XmlSchemaForm.Qualified)]
        public string Id
        {
            get;set;
        }

        [XmlIgnore]
        public string SheetName { get; set; }

    }
}
