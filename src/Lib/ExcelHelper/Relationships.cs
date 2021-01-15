using System;
using System.Xml.Serialization;

namespace RestXUnitTests.ExcelHelper
{
    /// <summary>
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// 
    /// Relationships information to map worksheet name to worksheet
    /// </summary>
    [Serializable()]
    [XmlRoot("Relationships", Namespace = "http://schemas.openxmlformats.org/package/2006/relationships")] 
    public class Relationships
    {
        [XmlElement("Relationship")]
        public Relationship[] RelationshipArray;
    }
}
