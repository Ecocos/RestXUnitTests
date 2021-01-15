using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace RestXUnitTests.ExcelHelper
{
    /// <summary>
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// 
    /// Top-level class to reference worksheets by name
    /// </summary>
    public class WorksheetNames
    {
        /// <summary>
        /// All worksheet names
        /// </summary>
        /// <param name="ExcelFileName">Full path and filename of the Excel xlsx-file</param>
        /// <returns></returns>
        public static SheetInfo Sheets(string ExcelFileName)
        {
            SheetInfo sheetInfo = null;
            Relationships relationships = null;
            if (!File.Exists(ExcelFileName))
                throw new FileNotFoundException($"Excel file {ExcelFileName} not found.");
            using (ZipArchive zipArchive = ZipFile.Open(ExcelFileName, ZipArchiveMode.Read))
            {
                sheetInfo = DeserializedZipEntry<SheetInfo>(GetZipArchiveEntry(zipArchive, @"xl/workbook.xml"));
                relationships = DeserializedZipEntry<Relationships>(GetZipArchiveEntryByTarget(zipArchive, @"xl/_rels/workbook.xml.rels"),"Relationships");
            }
            foreach(var sheetItem in sheetInfo.SheetInfoItems)
            {
                sheetItem.SheetName = relationships.RelationshipArray.FirstOrDefault(x => x.Id.Equals(sheetItem.Id)).Target;
            }
            return sheetInfo;
        }

        private static ZipArchiveEntry GetZipArchiveEntryByTarget(ZipArchive ZipArchive, string targetStartsWith)
        {
            return ZipArchive.Entries.First<ZipArchiveEntry>(n => n.FullName.StartsWith(targetStartsWith));
        }        

        private static ZipArchiveEntry GetZipArchiveEntry(ZipArchive ZipArchive, string ZipEntryName)
        {
            return ZipArchive.Entries.First<ZipArchiveEntry>(n => n.FullName.Equals(ZipEntryName));
        }

        private static T DeserializedZipEntry<T>(ZipArchiveEntry ZipArchiveEntry, string rootAttribute=null)
        {
            using (Stream stream = ZipArchiveEntry.Open())
            {
                if (rootAttribute == null)
                {
                    return (T)new XmlSerializer(typeof(T)).Deserialize(XmlReader.Create(stream)); 
                }
                return (T)new XmlSerializer(typeof(T)).Deserialize(XmlReader.Create(stream));
            }
        }
    }
}
