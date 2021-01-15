using System.Xml.Serialization;

namespace RestXUnitTests.ExcelHelper
{
    /// <summary>
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// 
    /// Individual relationships between the sheet ID and worksheet path
    /// </summary>
    public class Relationship
    {
        [XmlAttribute("Target")]
        public string Target { get; set; }
        [XmlAttribute("Id")]
        public string Id  { get;set;}
    }
}
