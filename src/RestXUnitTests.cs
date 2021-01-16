using log4net;
using Newtonsoft.Json.Linq;
using RestXUnitTests.ExcelHelper;
using RestXUnitTests.Helpers;
using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace RestXUnitTests
{
    /// <summary>
    /// (c) 2021 Erich Tinguely
    /// GNU General Public License v3
    /// 
    /// xUnit tests for REST api
    /// </summary>
    public class RestXUnitTests
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ITestOutputHelper output;

        public RestXUnitTests(ITestOutputHelper output) : base()
        {
            this.output = output;
            
        }

        /// <summary>
        /// This  is executed once per row in the spreadsheet
        /// </summary>
        /// <param name="cRow"></param>
        [Trait("Category", "RestApiTests")]
        [Theory]
        [ClassData(typeof(RestXUnitTestRow))]
        public async void RestApiTest(GenericSpreadsheetRow cRow)
        {
            if (cRow == null)
                throw new ArgumentNullException(paramName: nameof(cRow));
            //Each row can have up to three phases and each phase can be of type GetSkus, CreateOrUpdate, or Delete
            //var url = cRow.CellValues[""];
            //Columns: isTestCase	testCase	testSetupApiCall	testSetupUrl	setupJson	testApiCallType	url	postBody	responseJsonContains	responseJsonBodyDoesNotContain	expectedStatus	verfiyApiCallType	verifyUrl	verifyJsonBodyContains	verifyJsonBodyDoesNotContain

            //we have three phases:
            //1. [optional] make a call to set up test, check status (200)
            //2. make api call, check status 
            //3. [optional] make a call to verify test data (200 status expected)
            var hdrMsg = $"\r\n\r\nTest: {cRow.CellValues["testCase"]}, Row: {cRow.RowNum}";
            output.WriteLine(hdrMsg);
            log.Debug(hdrMsg);

            //=============================================================
            //debugging:
            //File.AppendAllText("testoutput.txt", $"\r\nTest: {cRow.CellValues["testCase"]}\r\n");
            //=============================================================
            var rndSku = await GrabNewRandomSku(cRow);

            Tuple<bool, JToken, string> phaseI = new Tuple<bool, JToken, string>(true, null, null);
            Tuple<bool, JToken, string> phaseIII = new Tuple<bool, JToken, string>(true, null, null);
            //execute first phase if there is a call type filled in
            if (!String.IsNullOrEmpty(cRow.CellValues["testSetupApiCall"]))
            {
                phaseI = await new RestApiHelper().RunPhase(
                    testCase: cRow.CellValues["testCase"],
                    apiCall: cRow.CellValues["testSetupApiCall"],
                    url: cRow.CellValues["testSetupUrl"].Replace("<RND>", rndSku),
                    json: cRow.CellValues["setupJson"].Replace("<RND>", rndSku)
                );
            }
            if (!phaseI.Item1) //if test setup failed
            {
                var msg = $"Pre-test setup failed. {phaseI.Item3}";
                output.WriteLine(msg);
                Assert.True(false);
            }
            //always execute main phase
            var phaseII = await new RestApiHelper().RunPhase(
                testCase: cRow.CellValues["testCase"],
                apiCall: cRow.CellValues["testApiCallType"],
                url: String.IsNullOrEmpty(cRow.CellValues["url"]) ? null : cRow.CellValues["url"].Replace("<RND>", rndSku),
                json: String.IsNullOrEmpty(cRow.CellValues["postBody"]) ? null : cRow.CellValues["postBody"].Replace("<RND>", rndSku),
                status: String.IsNullOrEmpty(cRow.CellValues["expectedStatus"]) ? System.Net.HttpStatusCode.OK : (System.Net.HttpStatusCode)Enum.Parse(typeof(System.Net.HttpStatusCode), cRow.CellValues["expectedStatus"]),
                jsonShouldContain: String.IsNullOrEmpty(cRow.CellValues["responseJsonContains"]) ? null : cRow.CellValues["responseJsonContains"].Replace("<RND>", rndSku),
                jsonShouldNotContain: cRow.CellValues["responseJsonBodyDoesNotContain"]
            );
            if (!phaseII.Item1) //if test failed
            {
                var msg = $"Test failed. {phaseII.Item3}";
                output.WriteLine(msg);
                Assert.True(false);
            }
            //execute third phase if there is a call type filled in
            if (!String.IsNullOrEmpty(cRow.CellValues["verfiyApiCallType"]))
            {
                phaseIII = await new RestApiHelper().RunPhase(
                    testCase: cRow.CellValues["testCase"],
                    apiCall: cRow.CellValues["verfiyApiCallType"],
                    url: cRow.CellValues["verifyUrl"].Replace("<RND>", rndSku),
                    json: cRow.CellValues["postBody"].Replace("<RND>", rndSku),
                    status: System.Net.HttpStatusCode.OK,
                    jsonShouldContain: cRow.CellValues["verifyJsonBodyContains"].Replace("<RND>", rndSku),
                    jsonShouldNotContain: cRow.CellValues["verifyJsonBodyDoesNotContain"]
               );
                if (!phaseIII.Item1) //if verification failed
                {
                    var msg = $"Verification step failed. {phaseIII.Item3}";
                    output.WriteLine(msg);
                    Assert.True(false);
                }
            }
            //if all stages were successful
            var success = phaseI.Item1 && phaseII.Item1 && phaseIII.Item1;
            if (!String.IsNullOrEmpty(cRow.CellValues["deleteMe"])) //clean up if set in spreadsheet
            {
                var yesDelete = bool.Parse(cRow.CellValues["deleteMe"]);
                if(yesDelete)
                {
                    try
                    {
                        await new RestApiHelper().DeleteItem(cRow.CellValues["verifyUrl"].Replace("<RND>", rndSku), System.Net.HttpStatusCode.OK).ConfigureAwait(false);
                    }
                    catch
                    {
                        //we are just attempting to clean up -- do not fail test if this does not work.
                        log.Warn($"Unable to delete sku {rndSku} to clean up test.");
                    }
                }
            }
            Assert.True(success);
        }

        /// <summary>
        /// Creates a random string to use as a SKU if the "&lt;RND&gt;" is embedded in specified strings
        /// </summary>
        /// <param name="cRow"></param>
        /// <returns></returns>
        private static async System.Threading.Tasks.Task<string> GrabNewRandomSku(GenericSpreadsheetRow cRow)
        {
            string rnd = null;
            if (cRow.CellValues["testSetupUrl"].Contains("<RND>")
                || cRow.CellValues["setupJson"].Contains("<RND>")
                || cRow.CellValues["url"].Contains("<RND>")
                || cRow.CellValues["postBody"].Contains("<RND>")
                || cRow.CellValues["verifyUrl"].Contains("<RND>")
                )
            {
                rnd = await new RestApiHelper().GetRandomSku("dev/skus/<RND>").ConfigureAwait(false);
            }
            return rnd;
        }
    }
 
}
