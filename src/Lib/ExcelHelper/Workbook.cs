using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace RestXUnitTests.ExcelHelper
{ 
    /// <summary>
    /// (c) 2014 Vienna, Dietmar Schoder
    /// 
    /// Code Project Open License (CPOL) 1.02
    /// 
    /// Deals with an Excel workbook in an xlsx-file and provides all worksheets in it.
    /// 
    /// UPDATED FROM ORIGINAL SOURCE CODE: Added code to reference worksheets by title. This requires an instance of this class (new it up) instead of the static accessors
    /// </summary>
    public class Workbook
    {
        public static sst SharedStrings;
        private const string WORKSHEETPREFIX = @"xl/";
        private readonly Dictionary<string, worksheet> _worksheetDict = null;

        public Workbook(string ExcelFileName)
        {
             IEnumerable<worksheet> workSheets = Worksheets(ExcelFileName);
             _worksheetDict = workSheets.ToDictionary(x => x.WorksheetTitle.ToLower(), y => y);
        }

        public worksheet this[string worksheetName]
        {
            get => _worksheetDict[worksheetName.ToLower()];
        }

        /// <summary>
        /// All worksheets in the Excel workbook deserialized
        /// </summary>
        /// <param name="ExcelFileName">Full path and filename of the Excel xlsx-file</param>
        /// <returns></returns>
        public static IEnumerable<worksheet> Worksheets(string ExcelFileName)
        {
            worksheet ws;
            var sheets = WorksheetNames.Sheets(ExcelFileName);

            using (ZipArchive zipArchive = ZipFile.Open(ExcelFileName, ZipArchiveMode.Read))
            {
                SharedStrings = DeserializedZipEntry<sst>(GetZipArchiveEntry(zipArchive, @"xl/sharedStrings.xml"));
                foreach (var worksheetEntry in (WorkSheetFileNames(zipArchive)).OrderBy(x => x.FullName))
                {
                    ws = DeserializedZipEntry<worksheet>(worksheetEntry);
                    //get rid of the "xl/" in the front of the SheetName
                    ws.WorksheetTitle = sheets.SheetInfoItems.FirstOrDefault(x => x.SheetName.Equals(worksheetEntry.FullName.Substring(WORKSHEETPREFIX.Length))).Name;
                    ws.NumberOfColumns = worksheet.MaxColumnIndex + 1;
                    ws.ExpandRows();
                    yield return ws;
                }
            }
        }

        /// <summary>
        /// Method converting an Excel cell value to a date
        /// </summary>
        /// <param name="ExcelCellValue"></param>
        /// <returns></returns>
        public static DateTime DateFromExcelFormat(string ExcelCellValue)
        {
            return DateTime.FromOADate(Convert.ToDouble(ExcelCellValue));
        }
        
        private static ZipArchiveEntry GetZipArchiveEntry(ZipArchive ZipArchive, string ZipEntryName)
        {
            return ZipArchive.Entries.First<ZipArchiveEntry>(n => n.FullName.Equals(ZipEntryName));
        }
        private static IEnumerable<ZipArchiveEntry> WorkSheetFileNames(ZipArchive ZipArchive)
        {
            foreach (var zipEntry in ZipArchive.Entries)
                if (zipEntry.FullName.StartsWith("xl/worksheets/sheet"))
                    yield return zipEntry;
        }
        private static T DeserializedZipEntry<T>(ZipArchiveEntry ZipArchiveEntry)
        {
            using (Stream stream = ZipArchiveEntry.Open())
                return (T)new XmlSerializer(typeof(T)).Deserialize(XmlReader.Create(stream));
        }
    }
}
