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

    /// <summary>
    /// Types of calls supported by this test framework
    /// </summary>
    public enum RestCallTypes
    {
        GetSkus, // GETs from URL
        CreateOrUpdate, //POSTS to URL
        Delete //DELETES from URL
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
        /// Does a get against the given url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="expectedStatusCode">HTTP status to expect</param>
        /// <returns>tuple with 1. bool (true for success), 2. JToken (json returned from call), and 3. string (error message, null if none)</returns>
        public async Task<Tuple<bool, JToken, string>> GetFromApi(string url, HttpStatusCode expectedStatusCode)
        {
            HttpResponseMessage response;
            string responseString;
            using (var client = BuildHttpClient())
            {
                try
                {
                    //=============================================================
                    //debugging:
                    //File.AppendAllText("testoutput.txt", $"GET URL:\r\n{url}\r\n");
                    //=============================================================
                    using (response = await client.GetAsync(url))
                    {
                        if (!response.StatusCode.Equals(expectedStatusCode))
                        {
                            var msg = $"The REST API returned a status code of '{response.StatusCode}' when '{expectedStatusCode}' was expected.";
                            log.Error(msg);
                            return new Tuple<bool, JToken, string>(false, null, msg);
                        }
                        responseString = response.Content.ReadAsStringAsync().Result;
                        //=============================================================
                        //debugging:
                        //File.AppendAllText("testoutput.txt",$"RESPONSE:\r\n{responseString}\r\n\r\n");
                        //=============================================================
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"{ex.Message}\r\nSTACK: {ex.StackTrace}");
                    throw;
                }

            }
            JToken json = null;
            if (response.StatusCode.Equals(HttpStatusCode.OK)) //if we are expecting non-200 response we are already done.
            {
                var ret = GetTheJson(responseString, out json);
                if (!ret)
                {
                    var mssg = $"FAILURE: could not parse response as JSON: '{responseString}'";
                    log.Error(mssg);
                    return new Tuple<bool, JToken, string>(false, null, mssg);
                }
            }
            return new Tuple<bool, JToken, string>(true, json, null);

        }

        /// <summary>
        /// creates the HttpClient to use to communicate with the REST API
        /// </summary>
        /// <param name="handler">optional external handler to use</param>
        /// <returns>An HTTP client</returns>
        private static HttpClient BuildHttpClient(HttpClientHandler handler = null)
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
        /// <param name="responseString">Raw response string</param>
        /// <param name="json">the JToken that parsed from the response string</param>
        /// <returns>True if successful</returns>
        public static bool GetTheJson(string responseString, out JToken json)
        {
            json = null;
            if (String.IsNullOrEmpty(responseString))
            {
                log.Warn($"null or empty response string");
                return true;
            }
            log.Debug($"attempting to parse '{responseString}'");
            try
            {
                json = JToken.Parse(responseString);
                if (json == null)
                {
                    log.Error($"ERROR: {MethodBase.GetCurrentMethod().Name}: json null or empty");
                    return false;
                }
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                log.Error($"{MethodBase.GetCurrentMethod().Name} JToken parse error for response '{responseString.Replace("\n", " ").Replace("\r", "").Replace("\t", " ")}'\n\r message: {ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// posts the given jsonContent to the given url
        /// </summary>
        /// <param name="url">url to post to</param>
        /// <param name="expectedStatusCode">what http status to expect</param>
        /// <param name="jsonContent">content to post</param>
        /// <returns>a tuple with 1. bool (true for success), 2. JToken (parsed JSON from reponse) and 3. string (error message if any, null if none.)</returns>
        public async Task<Tuple<bool, JToken, string>> PostToApi(string url, HttpStatusCode expectedStatusCode, string jsonContent)
        {
            HttpResponseMessage response;
            string responseString;
            using (HttpClient client = BuildHttpClient())
            {

                //=============================================================
                //debugging:
                //File.AppendAllText("testoutput.txt", $"POST URL:\r\n{url}\r\nCONTENT:\r\n{jsonContent}\r\n");
                //=============================================================
                var mediaType = new MediaTypeHeaderValue("application/json");
                HttpContent content = new StringContent(jsonContent, Encoding.UTF8);
                log.Debug($"posting '{jsonContent}' to url '{url}");
                using (response = await client.PostAsync(url, content).ConfigureAwait(false))
                {
                    responseString = response.Content.ReadAsStringAsync().Result;
                    //=============================================================
                    //debugging:
                    //File.AppendAllText("testoutput.txt", $"RESPONSE:\r\n{responseString}\r\n\r\n");
                    //=============================================================
                    if (!response.StatusCode.Equals(expectedStatusCode))
                    {
                        var mesage = $"FAILURE: Api returned incorrect status code: {response.StatusCode}, response: '{response}";
                        log.Error(mesage);
                        return new Tuple<bool, JToken, string>(false, null, mesage);
                    }
                }
                JToken json = null;
                if (response.StatusCode.Equals(HttpStatusCode.OK)) //if we are expecting non-200 response we are already done.
                {
                    var ret = GetTheJson(responseString, out json);
                    if (!ret)
                    {
                        var mssg = $"FAILURE: Api did not parse correctly for {responseString}";
                        log.Error(mssg);
                        return new Tuple<bool, JToken, string>(false, null, mssg);
                    }
                }
                return new Tuple<bool, JToken, string>(true, json, null);
            }
        }

        /// <summary>
        /// Activates a DELETE HTTP verb against the given url.
        /// </summary>
        /// <param name="url">Url to send DELETE to</param>
        /// <param name="expectedStatusCode">The status to expect back</param>
        /// <returns></returns>
        public async Task<Tuple<bool, JToken, string>> DeleteItem(string url, HttpStatusCode expectedStatusCode)
        {
            HttpResponseMessage response;
            string responseString;
            using (HttpClient client = BuildHttpClient())
            {
                log.Debug($"Calling delete on '{url}");
                //=============================================================
                //debugging:
                //File.AppendAllText("testoutput.txt", $"DELTEE URL:\r\n{url}\r\n");
                //=============================================================
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
                //=============================================================
                //debugging:
                //File.AppendAllText("testoutput.txt", $"RESPONSE:\r\n{responseString}\r\n");
                //=============================================================
                log.Debug($"delete returned {responseString}");
                JToken json = null;
                if (response.StatusCode.Equals(HttpStatusCode.OK)) //if we are expecting non-200 response we are already done.
                {
                    var ret = GetTheJson(responseString, out json);
                    if (!ret)
                    {
                        var mssg = $"FAILURE: could not parse JSON from string '{responseString}'";
                        log.Error(mssg);
                        return new Tuple<bool, JToken, string>(false, null, mssg);
                    }
                }
                return new Tuple<bool, JToken, string>(true, json, null);
            }
        }

        /// <summary>
        /// Runs a REST API test phase from information provided in the spreadsheet
        /// </summary>
        /// <param name="testCase">the name of the testcase</param>
        ///<param name="apiCall">The type of call to execute</param>
        /// <param name="url">the URL to use</param>
        /// <param name="json">The JSON to send to the REST service</param>
        /// <param name="status">The HTTP status to expect</param>
        /// <param name="jsonShouldContain">fully-formed JSON with the variables expected to be returned. Additional variables in the response are ignored</param>
        /// <param name="jsonShouldNotContain">a string that is searched for in the contents of the returned body, if found the test will fail.</param>
        /// <returns>a Tuple with:<br/>
        /// bool = call successful true/false<br/>
        /// JToken = parsed JSON returned (null if not successful)<br/>
        /// string = error message if not successful</returns>
        public  async Task<Tuple<bool, JToken, string>> RunPhase(string testCase, string apiCall, string url, string json, HttpStatusCode status = HttpStatusCode.OK, string jsonShouldContain=null, string jsonShouldNotContain=null)
        {
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
                        returnStatus = await new RestApiHelper().GetFromApi(url, status).ConfigureAwait(false);
                        break;
                    default:
                        throw new NotImplementedException($"{callTYpe} has not been implemented");

                }
                //=============================================================
                //debugging:
                //File.AppendAllText("testoutput.txt", $"{testCase} Call Type {apiCall} success ={returnStatus.Item1}, returned json {returnStatus.Item2}, err msg '{returnStatus.Item3}'\r\n");
                //=============================================================
                if (returnStatus.Item1.Equals(false))
                {
                    log.Debug($"{testCase} REST API returned {returnStatus.Item3}, failing.");
                    return returnStatus;

                }
                VerifyJsonItems(jsonShouldContain, setupStatusMsg, returnStatus);
                CheckForExclusionsInJson(jsonShouldNotContain, setupStatusMsg, returnStatus.Item2);
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
        /// checks for a forbidden string in the response
        /// </summary>
        /// <param name="jsonShouldNotContain">A String fragement that should not be in returned JSON</param>
        /// <param name="setupStatusMsg">A StringBuilder for errors found</param>
        /// <param name="returnedJson">The json returned from the call</param>
        private static void CheckForExclusionsInJson(string jsonShouldNotContain, StringBuilder setupStatusMsg, JToken returnedJson)
        {
            
            if (!String.IsNullOrEmpty(jsonShouldNotContain))
            {
                var stringToAvoid = jsonShouldNotContain.ToString();
                if (returnedJson.ToString().Contains(stringToAvoid))
                {
                    setupStatusMsg.AppendLine($"The JSON returned an item which should have been deleted");
                }

            }
        }

        /// <summary>
        /// Compares returned JSON with what was expected. Checks 'sku', 'description' and 'price'.
        /// </summary>
        /// <param name="jsonShouldContain">well-formed expected JSON to compare against</param>
        /// <param name="setupStatusMsg">Error messages if any</param>
        /// <param name="returnStatus">true if comparision worked, false otherwise</param>
        private static void VerifyJsonItems(string jsonShouldContain, StringBuilder setupStatusMsg, Tuple<bool, JToken, string> returnStatus)
        {
            //compare json with what we expected
            var newOutcome = returnStatus.Item1;
            if (!String.IsNullOrEmpty(jsonShouldContain))
            {

                var errsFound = new StringBuilder();
                try
                {
                    var shouldContain = JToken.Parse(jsonShouldContain);
                    var doesContain = returnStatus.Item2;
                    foreach (var item in shouldContain)
                    {
                        var thisItem = item;
                        if (item["Item"] != null)
                            thisItem = item["Item"];

                        JToken thisMatch = null;
                        if(doesContain.GetType() == typeof(JObject))
                        {
                            thisMatch = doesContain;
                            if (doesContain["Item"] != null) //if we just have a single item
                                thisMatch = doesContain["Item"];
                        }
                        else
                        {
                            //thisMatch = doesContain;
                            thisMatch = doesContain.FirstOrDefault(z => z["sku"].ToString().Equals(thisItem["sku"].ToString(), StringComparison.InvariantCultureIgnoreCase));
                        }
                        if (thisMatch != null)
                        {
                            if (thisItem["description"] != null)
                            {
                                if (!thisMatch["description"].ToString().Equals(thisItem["description"].ToString(), StringComparison.InvariantCultureIgnoreCase))
                                {
                                    setupStatusMsg.AppendLine($"SKU '{thisItem["sku"]}' did not have the same description. Expected: '{shouldContain["description"]}', actual: '{doesContain["description"]}");
                                }
                            }
                            if (thisItem["price"] != null)
                            {
                                if (!thisMatch["price"].ToString().Equals(thisItem["price"].ToString(), StringComparison.InvariantCultureIgnoreCase))
                                {
                                    setupStatusMsg.AppendLine($"SKU '{thisItem["sku"]}' did not have the same price. Expected: '{shouldContain["price"]}', actual: '{doesContain["price"]}");
                                }
                            }
                        }
                        else
                        {
                            setupStatusMsg.AppendLine($"Unable to find SKU {thisItem["sku"]} in result from REST API.");
                        }
                    }
                }
                catch (Newtonsoft.Json.JsonReaderException ex)
                {
                    var msg = $"{MethodBase.GetCurrentMethod().Name} JToken parse error for response '{jsonShouldContain.Replace("\n", " ").Replace("\r", "").Replace("\t", " ")}'\n\r message: {ex.Message}";
                    setupStatusMsg.AppendLine(msg);
                }
            }
        }

        /// <summary>
        /// create a random sku that has been verified as not being in used already
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
                returnStatus = await new RestApiHelper().GetFromApi(url.Replace("<RND>",rndStr), HttpStatusCode.OK).ConfigureAwait(false);
            } while (returnStatus.Item2.Contains("\"Item\":") && numtries-- >0);
            //TODO: this should return error if unable to find an unused sku
            log.Debug($"returning a random sku: {rndStr}\r\n");
            return rndStr; 
        }
    }
}
