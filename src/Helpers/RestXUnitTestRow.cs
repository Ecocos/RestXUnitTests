using RestXUnitTests.ExcelHelper;
using Xunit;
using Xunit.Abstractions;

namespace RestXUnitTests.Helpers
{
    public class RestXUnitTestRow : TheoryData<GenericSpreadsheetRow>
    {
             public RestXUnitTestRow()
            {
                //use Add to add a data row
                var spreadsheetRows = RestApiHelper.LoadSpreadsheet<GenericSpreadsheetRow>();
                foreach (var row in spreadsheetRows)
                {
                    Add(row);
                }
            }
        }
    }

