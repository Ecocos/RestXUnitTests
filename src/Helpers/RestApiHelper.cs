using Newtonsoft.Json.Linq;
using RestXUnitTests.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// (c) 2021 Erich Tinguely
/// GNU General Public License v3
/// 
/// 
/// helper class for managing rest test cases
/// </summary>
namespace RestXUnitTests.Helpers
{

    public enum RestCallTypes
    {
        GetSkus,
        CreateOrUpdate,
        Delete
    }

    public class RestApiHelper : RestTestBase
    {
        public static readonly string SPREADSHEETDIR = "src\\testfiles";
        public static readonly string SPREADSHEETNAME = "RestApi.xlsx";
        public static readonly string WORKSHEETNAME = "RestApiTests";
        //NOTE: The base URI should be contained in a per-environment config file, this is hard-coded here for simplification
        private static readonly string _baseUri = "https://1ryu4whyek.execute-api.us-west-2.amazonaws.com"; 


        public RestApiHelper()
        {
        }


        /// <summary>
        /// called before testing to get the test information from the spreadsheet
        /// </summary>
        /// <returns></returns>
        public static List<T> LoadSpreadsheet<T>() where T : IRowObject, new()
        {
            var spreadsheet = Path.Combine(Environment.CurrentDirectory, SPREADSHEETDIR, SPREADSHEETNAME);
            var rows = SpreadsheetHelper.LoadRowDataFromSpreadsheet<T>(spreadsheet, WORKSHEETNAME, 1);
            var filteredRows = rows.Where(r => bool.Parse(r.CellValues["isTestCase"])).ToList(); //remove non-test case rows
            return filteredRows;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="expectedStatusCode"></param>
        /// <returns>tuple with bool (true for success), string (json returned from call), string (error message if any)</returns>
        public async Task<Tuple<bool, JToken, string>> GetFromApi(string url, HttpStatusCode expectedStatusCode)
        {
            //log.Debug($"GET: {queryUrl}");
            HttpResponseMessage response;
            string responseString;
            using (var client = BuildHttpClient())
            {
                try
                {
                    using (response = await client.GetAsync(url))
                    {
                        responseString = response.Content.ReadAsStringAsync().Result;
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"{ex.Message}\r\nSTACK: {ex.StackTrace}");
                    throw;
                }

            }
            var ret = GetTheJson(responseString, out JToken json);
            if (!ret)
            {
                var mssg = $"FAILURE: could not parse response as JSON: '{responseString}'";
                log.Error(mssg);
                return new Tuple<bool, JToken, string>(false, null, mssg);
            }
            return new Tuple<bool, JToken, string>(true, json, null);

        }

        public static HttpClient BuildHttpClient(HttpClientHandler handler = null)
        {
            if (handler == null)
            {
                handler = new HttpClientHandler()
                {
                    UseCookies = true,
                    AllowAutoRedirect = true                   
                };
            }
            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(_baseUri);
            return client;

        }

        /// <summary>
        /// returns true if able to parse string for json
        /// </summary>
        /// <param name="responseString"></param>
        /// <param name="json"></param>
        /// <returns></returns>
        public static bool GetTheJson(string responseString, out JToken json)
        {
            json = null;
            if (String.IsNullOrEmpty(responseString))
            {   
                log.Error($"ERROR: {MethodBase.GetCurrentMethod().Name}: response null or empty");
                //throw new ArgumentNullException($"{MethodBase.GetCurrentMethod().Name}: response null or empty");
                return false;
            }
            log.Debug($"attempting to parse '{responseString}'");
            try
            {
                //var jx = JToken.Parse(responseString);
                //json = JArray.Parse(responseString);
                json = JToken.Parse(responseString);
                if (json == null)
                {
                    log.Error($"ERROR: {MethodBase.GetCurrentMethod().Name}: json null or empty");
                    //throw new ArgumentNullException($"{MethodBase.GetCurrentMethod().Name}: json null or empty");
                    return false;
                }
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                log.Error($"{MethodBase.GetCurrentMethod().Name} JObject parse error for response '{responseString.Replace("\n", "|").Replace("\r", "").Replace("\t", " ")}:\n\r message: {ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="expectedStatusCode"></param>
        /// <param name="jsonContent"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        public async Task<Tuple<bool, JToken, string>> PostToApi(string url, HttpStatusCode expectedStatusCode, string jsonContent)
        {
            HttpResponseMessage response;
            string responseString;
            using (HttpClient client = BuildHttpClient())
            {
 
                var mediaType = new MediaTypeHeaderValue("application/json");
                HttpContent content = new StringContent(jsonContent, Encoding.UTF8);
                log.Debug($"posting '{jsonContent}' to url '{url}");
                using (response = await client.PostAsync(url, content).ConfigureAwait(false))
                {
                    responseString = response.Content.ReadAsStringAsync().Result;
                    if (!response.StatusCode.Equals(expectedStatusCode))
                    {
                        var mesage = $"FAILURE: Api returned incorrect status code: {response.StatusCode}, response: '{response}";
                        log.Error(mesage);
                        return new Tuple<bool, JToken, string>(false, null, mesage);
                    }
                }
                var ret = GetTheJson(responseString, out JToken json);
                if (!ret)
                {
                    var mssg = $"FAILURE: Api did not parse correctly for {responseString}";
                    log.Error(mssg);
                    return new Tuple<bool, JToken, string>(false, null, mssg);
                }
                return new Tuple<bool, JToken, string>(true, json, null);
            }
        }

        public async Task<Tuple<bool, JToken, string>> DeleteItem(string url, HttpStatusCode expectedStatusCode)
        {
            HttpResponseMessage response;
            string responseString;
            using (HttpClient client = BuildHttpClient())
            {
                log.Debug($"Calling delete on '{url}");
                using (response = await client.DeleteAsync(url).ConfigureAwait(false))
                {
                    responseString = response.Content.ReadAsStringAsync().Result;
                    if (!response.StatusCode.Equals(expectedStatusCode))
                    {
                        var mesage = $"FAILURE: Delete Api did not return the expected status (OK), instead returned: {response.StatusCode}, response was: '{response}";
                        log.Error(mesage);
                        return new Tuple<bool, JToken, string>(false, null, mesage);
                    }
                }
                log.Debug($"delete returned {responseString}");
                var ret = GetTheJson(responseString, out JToken json);
                if (!ret)
                {
                    var mssg = $"FAILURE: could not parse JSON from string '{responseString}'";
                    log.Error(mssg);
                    return new Tuple<bool, JToken, string>(false, null, mssg);
                }
                return new Tuple<bool, JToken, string>(true, json, null);
            }
        }

        /// <summary>
        /// Runs the first (optional) REST API test phase from spreadsheet
        /// </summary>
        /// <param name="cRow">the GenericSpreadsheetRow from the spreadsheet</param>
        /// <returns>a Tuple with:<br/>
        /// bool = call successful true/false<br/>
        /// JObject = parsed JSON returned (null if not successful)<br/>
        /// string = error message if not successful</returns>
        public  async Task<Tuple<bool, JToken, string>> RunPhase(string testCase, string apiCall, string url, string json, HttpStatusCode status = HttpStatusCode.OK, string jsonShouldContain=null, string jsonShouldNotContain=null)
        {
            //Columns: isTestCase	testCase	testSetupApiCall	testSetupUrl	setupJson	testApiCallType	url	postBody	responseJsonContains	responseJsonBodyDoesNotContain	expectedStatus	verfiyApiCallType	verifyUrl	verifyJsonBodyContains	verifyJsonBodyDoesNotContain
            //Phase I columns to use: testCase testSetupApiCall testSetupUrl	setupJson
            //string newUrl;
            if (String.IsNullOrEmpty(apiCall))
            {
                return new Tuple<bool, JToken, string>(true,null,null); //no setup needed
            }
            StringBuilder setupStatusMsg = new StringBuilder();
            var isEnum = Enum.TryParse(typeof(RestCallTypes), apiCall, true, out object callTypeObj); //look at api type in spreadsheet and make sure it is one we are using
            if (isEnum)
            {
                var callTYpe = (RestCallTypes)callTypeObj;
                Tuple<bool, JToken, string> returnStatus = null;
                switch (callTYpe)
                {
                    case RestCallTypes.CreateOrUpdate: 
                        returnStatus = await new RestApiHelper().PostToApi(url, status, json).ConfigureAwait(false);
                        break;
                    case RestCallTypes.Delete:
                        returnStatus = await new RestApiHelper().DeleteItem(url, status).ConfigureAwait(false);
                        break;
                    case RestCallTypes.GetSkus:
                        returnStatus = await new RestApiHelper().GetFromApi( url, status).ConfigureAwait(false);
                        break;
                    default:
                        throw new NotImplementedException($"{callTYpe} has not been implemented");

                }
                //compare json with what we expected
                var newOutcome = returnStatus.Item1;
                if (!String.IsNullOrEmpty(jsonShouldContain))
                {
 
                    var errsFound = new StringBuilder();
                    var shouldContain = JArray.Parse(jsonShouldContain);
                    var doesContain = returnStatus.Item2;
                    foreach(var item in shouldContain)
                    {
                        var thisMatch = doesContain.FirstOrDefault(z => z["sku"].ToString().Equals(item["sku"].ToString(),StringComparison.InvariantCultureIgnoreCase));
                        if (thisMatch != null)
                        {
                            if (!thisMatch["description"].ToString().Equals(item["description"].ToString(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                setupStatusMsg.AppendLine($"SKU '{item["sku"]}' did not have the same description. Expected: '{shouldContain["description"]}', actual: '{doesContain["description"]}");
                            }
                            if (!thisMatch["price"].ToString().Equals(item["price"].ToString(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                setupStatusMsg.AppendLine($"SKU '{item["sku"]}' did not have the same price. Expected: '{shouldContain["price"]}', actual: '{doesContain["price"]}");
                            }
                        }
                        else
                        {
                            setupStatusMsg.AppendLine($"Unable to find SKU {item["sku"]} in result from REST API.");
                        }
                     }
                }
                if (!String.IsNullOrEmpty(jsonShouldNotContain))
                {
                    var stringToAvoid = jsonShouldNotContain.ToString();
                    if (returnStatus.Item2.ToString().Contains(stringToAvoid))
                    {
                        setupStatusMsg.AppendLine($"The JSON returned an item which should have been deleted");
                    }
                   
                }
                if (setupStatusMsg.Length > 1)
                {
                    returnStatus = new Tuple<bool, JToken, string>(false, null, setupStatusMsg.ToString());
                }
                return returnStatus;
            }
            else
            {
                var errMsg = $"Test case {testCase} contained {apiCall} in the 'Call Type' column, please change to null, 'GetSkus','CreateOrUpdate' or 'Delete' ";
                return new Tuple<bool, JToken, string>(false,null,errMsg);
            }

        }

        /// <summary>
        /// create a random sku and verify it is not already being used
        /// </summary>
        /// <param name="url">the Url which should contain "&lt;RND&gt;" as a replacement token</param>
        /// <returns></returns>
        public async Task<string> GetRandomSku(string url)
        {
            Tuple<bool, JToken, string> returnStatus;
            Random random = new Random();
            string rndStr;
            var numtries = 5;
            do
            {
                rndStr = random.Next().ToString();
                //rndStr = random.ToString();
                returnStatus = await new RestApiHelper().GetFromApi(url.Replace("<RND>",rndStr), HttpStatusCode.OK).ConfigureAwait(false);
            } while (returnStatus.Item2.Contains("\"Item\":") && numtries-- >0);
            //TODO: this should return error if unable to find unused sku
            return rndStr; 

        }
    }
}
