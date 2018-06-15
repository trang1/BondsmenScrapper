using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace BondsmenScrapper
{
    public partial class MainForm : Form
    {
        private volatile bool _started;
        private WebClient _client;
        private WebProxy _proxy;
        public string _cookie;

        public MainForm()
        {
            InitializeComponent();
            _client = new GZipWebClient();
            tbUsername.Text = "nbaca@walden.ly";
            tbPassword.Text = "1.Upwork";
            tbBondsman.Text = "74517";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = true;
            btnStart.Enabled = false;

            Task.Factory.StartNew(BeginScrapping);
            _started = true;
            Log("Starting...");
        }

        private void BeginScrapping()
        {
            var loginUrl = ConfigurationManager.AppSettings.Get("LoginUrl");
            var url = ConfigurationManager.AppSettings.Get("Url");
            var proxy = ConfigurationManager.AppSettings.Get("ProxyIP");
            int port = -1;
            int.TryParse(ConfigurationManager.AppSettings.Get("ProxyPort"), out port);
            
            if (!string.IsNullOrEmpty(proxy) && port != -1)
            {
                _proxy = new WebProxy(proxy, port);
                _client.Proxy = _proxy;
                Log($"Using proxy {proxy}:{port}");
            }
            
            var web = new HtmlWeb();
            HtmlDocument htmlDoc;
            web.PostResponse = delegate(HttpWebRequest request, HttpWebResponse response)
            {
                var cookies = response.Headers["Set-Cookie"];
                var sessionId = cookies.Split(';')[0];
                _client.Headers.Add(HttpRequestHeader.Cookie, sessionId + "; Login=");
            };
            Log("Connecting to the website");
            if (_proxy != null)
                htmlDoc = web.Load(loginUrl, "GET", _proxy, null);
            else
                htmlDoc = web.Load(loginUrl);

            Thread.Sleep(2000);

            Log("Trying to login");
            if (Login(htmlDoc, loginUrl))
            {
                Log("Login successful");

                web = new HtmlWeb();
                web.PreRequest = delegate (HttpWebRequest request)
                {
                    request.Headers["Cookie"] = _cookie;
                    return true;
                };
                Log("Loading search form");
                if (_proxy != null)
                    htmlDoc = web.Load(url, "GET", _proxy, null);
                else
                    htmlDoc = web.Load(url);

                Log("Making a request");
                var result = Search(htmlDoc);


                if (!string.IsNullOrEmpty(result))
                {
                    var lastPage = GetLastPage(result);

                    var page = 1;
                    do
                    {
                        Log($"Processing page = {page}");

                        if (page > 1)
                            result = GetNextResultsPage(htmlDoc, page);

                        htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(result);

                        var rows =
                            htmlDoc.DocumentNode.SelectNodes(
                                "//table[@class='resultHeader contentwidth']/tr[not(@class='trResultHeader')]");
                        if (rows != null)
                        {
                            var filledRows = rows.Where(row => !string.IsNullOrEmpty(row.ChildNodes[11].InnerText.Trim())).ToList();
                            foreach (var row in filledRows)
                            {
                                ProcessTableRow(web, row);
                                Thread.Sleep(1000);
                            }
                            Log($"Processed {filledRows.Count} rows");
                        }
                        page++;

                        if (!_started)
                            break;

                        Thread.Sleep((page % 4 + 1)*1000);

                    } while (page <= lastPage );
                }
            }
            else
            {
                Log("Login failed");
                BeginInvoke(new Action(Stop));
            }
        }

        private static int GetLastPage(string result)
        {
            try
            {
                var docc = new HtmlDocument();
                docc.LoadHtml(result);
                var lastPageLink = docc.DocumentNode.SelectSingleNode("//a[@title=' to Last Page ']").Attributes["href"];
                var lastPage = int.Parse(lastPageLink.Value.Split('\'')[3]);
                return lastPage;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        private void ProcessTableRow(HtmlWeb web, HtmlNode row)
        {
            var link1 = row.ChildNodes[3].ChildNodes[1];
            var link= link1.GetAttributeValue("onclick", "");
            if (!string.IsNullOrEmpty(link))
            {
                var query = link.Split('\'')[1];
                var url = "http://www.hcdistrictclerk.com/edocs/public/CaseDetailsPrinting.aspx?" + query;

                HtmlDocument htmlDoc;
                if (_proxy != null)
                    htmlDoc = web.Load(url, "GET", _proxy, null);
                else
                    htmlDoc = web.Load(url);

                //File.WriteAllText("C:\\temp\\d1.htm", htmlDoc.Text);

                var caseRow = htmlDoc.DocumentNode.SelectSingleNode("//table").ChildNodes[5];
                var cause = caseRow.ChildNodes[3].InnerText.Trim() + ", " + caseRow.ChildNodes[5].InnerText.Trim();

                // *********************    CASE DETAILS **************************
                var caseDetailsTable = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='tblCaseDetails']");

                if (caseDetailsTable != null)
                {
                    var fileDate = caseDetailsTable.ChildNodes[3].ChildNodes[3].InnerText;
                    var status = caseDetailsTable.ChildNodes[7].ChildNodes[3].InnerText;
                    var offense = caseDetailsTable.ChildNodes[11].ChildNodes[3].InnerText;
                    var lastInstr = caseDetailsTable.ChildNodes[15].ChildNodes[3].InnerText;
                    var disposition = caseDetailsTable.ChildNodes[19].ChildNodes[3].InnerText;
                    var complDate = caseDetailsTable.ChildNodes[23].ChildNodes[3].InnerText;
                    var defStatus = caseDetailsTable.ChildNodes[27].ChildNodes[3].InnerText;
                    var bondAmt = caseDetailsTable.ChildNodes[31].ChildNodes[3].InnerText;
                    var settDate = caseDetailsTable.ChildNodes[35].ChildNodes[3].InnerText;
                }

                // *********************** DEFENDANT DETAILS  *************************
                var defDetailsTable = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='tblDefendantDetails']");
                if (defDetailsTable != null)
                {
                    var raceSex = defDetailsTable.ChildNodes[3].ChildNodes[1].ChildNodes[1].ChildNodes[1].ChildNodes[3].InnerText;
                    var eyes = defDetailsTable.ChildNodes[3].ChildNodes[1].ChildNodes[1].ChildNodes[5].ChildNodes[3].InnerText;
                    var skin = defDetailsTable.ChildNodes[3].ChildNodes[1].ChildNodes[1].ChildNodes[9].ChildNodes[3].InnerText;
                    var dob = defDetailsTable.ChildNodes[3].ChildNodes[1].ChildNodes[1].ChildNodes[13].ChildNodes[3].InnerText;
                    var usCitizen = defDetailsTable.ChildNodes[3].ChildNodes[1].ChildNodes[1].ChildNodes[17].ChildNodes[3].InnerText;
                    var hWeight = defDetailsTable.ChildNodes[3].ChildNodes[3].ChildNodes[1].ChildNodes[1].ChildNodes[3].InnerText;
                    var hair = defDetailsTable.ChildNodes[3].ChildNodes[3].ChildNodes[1].ChildNodes[5].ChildNodes[3].InnerText;
                    var build = defDetailsTable.ChildNodes[3].ChildNodes[3].ChildNodes[1].ChildNodes[9].ChildNodes[3].InnerText;
                    var inCustody = defDetailsTable.ChildNodes[3].ChildNodes[3].ChildNodes[1].ChildNodes[13].ChildNodes[3].InnerText;
                    var plOB = defDetailsTable.ChildNodes[3].ChildNodes[3].ChildNodes[1].ChildNodes[17].ChildNodes[3].InnerText;
                    var address = defDetailsTable.ChildNodes[5].ChildNodes[3].InnerText;
                    var markings = defDetailsTable.ChildNodes[9].ChildNodes[3].InnerText;
                }

                // ****************************  CURRENT PRESIDING JUDGE ***********************
                var courtDetailsTable = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='tblCourtDetails']");
                if (courtDetailsTable != null)
                {
                    var court = courtDetailsTable.ChildNodes[3].ChildNodes[3].InnerText;
                    var address = courtDetailsTable.ChildNodes[7].ChildNodes[3].InnerText;
                    var judgeName = courtDetailsTable.ChildNodes[11].ChildNodes[3].InnerText;
                    var courtType = courtDetailsTable.ChildNodes[15].ChildNodes[3].InnerText;
                }

                // ***********************************   BONDS  ***********************************
                var bondsRows = htmlDoc.DocumentNode.SelectNodes("//table[@id='tblBonds']/tr[@style='font-size:12px; ']");
                if (bondsRows != null)
                {
                    foreach (var bondsRow in bondsRows)
                    {
                        var date = bondsRow.ChildNodes[1].InnerText;
                        var type = bondsRow.ChildNodes[3].InnerText;
                        var desc = bondsRow.ChildNodes[5].InnerText;
                        var snu = bondsRow.ChildNodes[7].InnerText;
                    }
                }
                
                // ***********************************   ACTIVITIES  ***********************************
                var activitiesRows = htmlDoc.DocumentNode.SelectNodes("//table[@id='tblActivities']/tr[@style='font-size:12px; ']");
                if (activitiesRows != null)
                {
                    foreach (var activitiesRow in activitiesRows)
                    {
                        var date = activitiesRow.ChildNodes[1].InnerText;
                        var type = activitiesRow.ChildNodes[3].InnerText;
                        var desc = activitiesRow.ChildNodes[5].InnerText;
                        var snu = activitiesRow.ChildNodes[7].InnerText;
                    }
                }
                
                // ***********************************   BOOKINGS  ***********************************
                var bookingsRows = htmlDoc.DocumentNode.SelectNodes("//table[@id='tblBookings']/tr[@style='font-size:12px; ']");
                if (bookingsRows != null)
                {
                    foreach (var bookingsRow in bookingsRows)
                    {
                        var arrDate = bookingsRow.ChildNodes[1].InnerText;
                        var arrLocation = bookingsRow.ChildNodes[3].InnerText;
                        var date = bookingsRow.ChildNodes[5].InnerText;
                    }
                }

                // ***********************************   HOLDS  ***********************************
                var holdsRows = htmlDoc.DocumentNode.SelectNodes("//table[@id='tblHolds']/tr[@style='font-size:12px; ']");
                if (holdsRows != null)
                {
                    foreach (var holdsRow in holdsRows)
                    {
                        var agPlHold = holdsRow.ChildNodes[1].InnerText;
                        var agName = holdsRow.ChildNodes[3].InnerText;
                        var warrantNumber = holdsRow.ChildNodes[5].InnerText;
                        var bondAmount = holdsRow.ChildNodes[7].InnerText;
                        var offense = holdsRow.ChildNodes[9].InnerText;
                        var plDate = holdsRow.ChildNodes[11].InnerText;
                        var liftDate = holdsRow.ChildNodes[13].InnerText;
                    }
                }
                
                // ***********************************   CRIMINAL HISTORY  ***********************************
                var crimHistRows = htmlDoc.DocumentNode.SelectNodes("//table[@id='tblCrimHist']/tr[@style='font-size:11px; vertical-align:top; ']");
                if (crimHistRows != null)
                {
                    foreach (var crimHistRow in crimHistRows)
                    {
                        var caseNum = crimHistRow.ChildNodes[1].InnerText.Trim();
                        var defendant = crimHistRow.ChildNodes[3].InnerText.Trim();
                        var bookedFiled = crimHistRow.ChildNodes[5].InnerText.Trim();
                        var court = crimHistRow.ChildNodes[7].InnerText.Trim();
                        var defStatus = crimHistRow.ChildNodes[9].InnerText.Trim();
                        var disposition = crimHistRow.ChildNodes[11].InnerText.Trim();
                        var bondAmt = crimHistRow.ChildNodes[13].InnerText.Trim();
                        var typeOfAction = crimHistRow.ChildNodes[15].InnerText.Trim();
                        var nextSetting = crimHistRow.ChildNodes[17].InnerText.Trim();
                    }
                }

                // ***********************************   SETTINGS  ***********************************
                var settingsRows = htmlDoc.DocumentNode.SelectNodes("//table[@id='tblSettings']/tr[@style='font-size:12px; ']");
                if (settingsRows != null)
                {
                    foreach (var settingsRow in settingsRows)
                    {
                        var date = settingsRow.ChildNodes[1].InnerText.Trim();
                        var court = settingsRow.ChildNodes[3].InnerText.Trim();
                        var postJdgm = settingsRow.ChildNodes[5].InnerText.Trim();
                        var docketType = settingsRow.ChildNodes[7].InnerText.Trim();
                        var reason = settingsRow.ChildNodes[9].InnerText.Trim();
                        var results = settingsRow.ChildNodes[11].InnerText.Trim();
                        var defendant = settingsRow.ChildNodes[13].InnerText.Trim();
                        var futureDate = settingsRow.ChildNodes[15].InnerText.Trim();
                        var comments = settingsRow.ChildNodes[17].InnerText.Trim();
                        var atApIndicator = settingsRow.ChildNodes[19].InnerText.Trim();
                    }
                }
            }
        }

        private string Search(HtmlDocument htmlDoc)
        {
            var eventTarget =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTTARGET']").GetAttributeValue("value", "");

            var eventArgument =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTARGUMENT']").GetAttributeValue("value", "");
            var viewState =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATE']").GetAttributeValue("value", "");
            var viewStateGenerator =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATEGENERATOR']").GetAttributeValue("value", "");
            var viewStateEncrypted =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATEENCRYPTED']").GetAttributeValue("value", "");
            var eventValidation =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTVALIDATION']").GetAttributeValue("value", "");

            var url = ConfigurationManager.AppSettings["Url"];

            var values = new NameValueCollection()
            {
                {"ctl00_ctl00_ctl00_ContentPlaceHolder1_ContentPlaceHolder2_ContentPlaceHolder2_tabSearch_ClientState", "{\"ActiveTabIndex\":1,\"TabState\":[true,true,true,true,true,true,true,true]}"},
                {"__EVENTTARGET", eventTarget},
                {"__EVENTARGUMENT", eventArgument},
                {"__LASTFOCUS", ""},
                {"__VIEWSTATE", viewState},
                {"__VIEWSTATEGENERATOR", viewStateGenerator},
                {"__VIEWSTATEENCRYPTED", viewStateEncrypted},
                {"__EVENTVALIDATION", eventValidation}
            };

            AddSearchFormValues(values);

            //_client.Headers.Add(HttpRequestHeader.Connection,"keep-alive");
            _client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            _client.Headers[HttpRequestHeader.Host] = "www.hcdistrictclerk.com";
            _client.Headers[HttpRequestHeader.Cookie] = _cookie;
            //_client.Headers.Add("DNT", "1");
            //_client.Headers.Add(HttpRequestHeader.Referer, url);
            //_client.Headers.Add("Upgrade-Insecure-Requests", "1");
            //_client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.3; Win64; x64; rv:60.0) Gecko/20100101 Firefox/60.0");
            //_client.Headers.Add(HttpRequestHeader.KeepAlive, "");
            //_client.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            //_client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
            //_client.Headers.Add(HttpRequestHeader.AcceptLanguage, "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
            // var content = new FormUrlEncodedContent(values);

            var result = Encoding.ASCII.GetString(_client.UploadValues(url, values));
            //File.WriteAllText("C:\\temp\\2.htm", result);
            return result;
        }

        private string GetNextResultsPage(HtmlDocument htmlDoc, int page)
        {
            var eventTarget = "ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$pager1";
            var eventArgument = page.ToString();

            var viewState =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATE']").GetAttributeValue("value", "");
            var viewStateGenerator =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATEGENERATOR']").GetAttributeValue("value", "");
            var viewStateEncrypted =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATEENCRYPTED']").GetAttributeValue("value", "");
            var eventValidation =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTVALIDATION']").GetAttributeValue("value", "");

            var url = ConfigurationManager.AppSettings["Url"];

            var values = new NameValueCollection()
            {
                {"__EVENTTARGET", eventTarget},
                {"__EVENTARGUMENT", eventArgument},
                {"__VIEWSTATE", viewState},
                {"__VIEWSTATEGENERATOR", viewStateGenerator},
                {"__VIEWSTATEENCRYPTED", viewStateEncrypted},
                {"__EVENTVALIDATION", eventValidation}
            };

            AddNextPageFormValues(values);

            //_client.Headers.Add(HttpRequestHeader.Connection,"keep-alive");
            _client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            _client.Headers[HttpRequestHeader.Host] = "www.hcdistrictclerk.com";
            _client.Headers[HttpRequestHeader.Cookie] = _cookie;
            //_client.Headers.Add("DNT", "1");
            //_client.Headers.Add(HttpRequestHeader.Referer, url);
            //_client.Headers.Add("Upgrade-Insecure-Requests", "1");
            //_client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.3; Win64; x64; rv:60.0) Gecko/20100101 Firefox/60.0");
            //_client.Headers.Add(HttpRequestHeader.KeepAlive, "");
            //_client.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            //_client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
            //_client.Headers.Add(HttpRequestHeader.AcceptLanguage, "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
            // var content = new FormUrlEncodedContent(values);

            var result = Encoding.ASCII.GetString(_client.UploadValues(url, values));
            //File.WriteAllText($"C:\\temp\\{page}p.htm", result);
            return result;
        }


        private void AddSearchFormValues(NameValueCollection values)
        {
            var formattedPairs = string.Format(searchFormValues, tbBondsman.Text);
            var pairs = formattedPairs.Split('&');
            foreach (var pair in pairs)
            {
                try
                {
                    var key = pair.Split('=')[0];
                    var value = pair.Split('=')[1];
                    values.Add(key, value);
                }
                catch (Exception)
                {
                    
                }
            }
        }

        private void AddNextPageFormValues(NameValueCollection values)
        {
            var pairs = nextPageFormValues.Split('&');
            foreach (var pair in pairs)
            {
                try
                {
                    var key = pair.Split('=')[0];
                    var value = pair.Split('=')[1];
                    values.Add(key, value);
                }
                catch (Exception)
                {

                }
            }
        }

        private bool Login(HtmlDocument htmlDoc, string loginUrl)
        {
            var eventTarget =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTTARGET']").GetAttributeValue("value", "");

            var eventArgument =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTARGUMENT']").GetAttributeValue("value", "");
            var viewState =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATE']").GetAttributeValue("value", "");
            var viewStateGenerator =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATEGENERATOR']").GetAttributeValue("value", "");
            var eventValidation =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTVALIDATION']").GetAttributeValue("value", "");
            var btnLoginValue =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='btnLoginImageButton']").GetAttributeValue("value", "");

            var values = new NameValueCollection()
            {
                {"__EVENTTARGET", eventTarget},
                {"__EVENTARGUMENT", eventArgument},
                {"__VIEWSTATE", viewState},
                {"__VIEWSTATEGENERATOR", viewStateGenerator},
                {"__EVENTVALIDATION", eventValidation},
                {"txtUserName", tbUsername.Text},
                {"txtPassword", tbPassword.Text},
                {"btnLoginImageButton", btnLoginValue}
            };

            //_client.Headers.Add(HttpRequestHeader.Connection,"keep-alive");
            _client.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
            _client.Headers.Add(HttpRequestHeader.Host, "www.hcdistrictclerk.com");
           // _client.Headers.Add("Connection", "keep-alive");
            _client.Headers.Add("DNT", "1");
            _client.Headers.Add(HttpRequestHeader.Referer, loginUrl);
            _client.Headers.Add("Upgrade-Insecure-Requests", "1");
            _client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.3; Win64; x64; rv:60.0) Gecko/20100101 Firefox/60.0");
            _client.Headers.Add(HttpRequestHeader.KeepAlive,"");
            _client.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
            _client.Headers.Add(HttpRequestHeader.AcceptLanguage, "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
            // var content = new FormUrlEncodedContent(values);

            var result = Encoding.ASCII.GetString(_client.UploadValues(loginUrl, values));
            _cookie = _client.ResponseHeaders["Set-Cookie"];

            return _cookie.Length > 500;
            //var response = _client.PostAsync(loginUrl, content).GetAwaiter().GetResult();
            //var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }


        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            btnStop.Enabled = false;
            btnStart.Enabled = true;

            _started = false;
            Log("Stopping...");
        }

        private void Log(string message)
        {
            BeginInvoke(new Action(() => tbLog.Text += $"{DateTime.Now:HH:mm:ss}: {message}\r\n"));

            if (tbLog.Lines.Length > 100)
            {
                var file = Path.Combine(Application.StartupPath, $"{DateTime.Now:yyyy-MM-dd_HH-mm} .log");
                File.WriteAllText(file, tbLog.Text);

                BeginInvoke(new Action(() => tbLog.Text = string.Empty));
                Log("Log flushed. All data saved to " + file);
            }
        }


        string searchFormValues =
            "ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$HfCaseNbr=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$HfCaseCdi=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtCaseNumber=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$ddlPlaintiffSearchType=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtPlaintiff=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$wtrPlaintiff_ClientState=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$ddlDefendantSearchType=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtDefendant=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$TextBoxWatermarkExtender1_ClientState=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtCivilStartDate=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtCivilEndDate=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$ddlCourtID=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$ddlCaseTypeID=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$ddlCaseStatusID=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtBarNumber=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtTransactionNum=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtAgCaseNbr=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtCRSAccountID=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtPubImgNbrCiv=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtTaxId=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCivil$txtEnvelopeNbr=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtCrimCaseNumber=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$ddlCrimDefendantSearchType=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtCrimDefendant=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$TextBoxWatermarkExtender3_ClientState=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$chkSearchAlias=on&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtDOB=__/__/____&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$medDOB_ClientState=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtDOBEnd=__/__/____&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$medDOBEnd_ClientState=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$ddlRace=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$ddlGender=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtCrimStartDate=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtCrimEndDate=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$ddlCrimCourtID=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtCrimBarNumber=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtCrimBondNumber={0}&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtCrimTransactionNum=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtCrimCRSAccountID=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtCrimDefSPN=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$txtPubImgNbrCrim=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$ddlCriminalCaseStatus=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabCriminal$ddlDefendantStatus=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabPartySearch$ddlPartyNameSearchType=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabPartySearch$txtPartyName=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabPartySearch$wtrPartyName_ClientState=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabPartySearch$ddlPartyConnection=A" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabPartySearch$txtPartyStartDate=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabPartySearch$txtPartyEndDate=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabPartySearch$ddlPartyCaseStatusID=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabPartySearch$ddlCourtType2=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabBkgrndChk$txtSPN2=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabBkgrndChk$txtCaseCrim2=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabBkgrndChk$ddlReturnedCDI2=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabBkgrndChk$txtDefFirst2=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabBkgrndChk$txtDefLast2=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabBkgrndChk$txtDefDOB2=__/__/____&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabBkgrndChk$meeDOB2_ClientState=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabBkgrndChk$txtbkgrndimgID=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabHistoricalDocuments$historicalCtrl$txtSearch=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabHistoricalDocuments$historicalCtrl$txtNatName=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabHistoricalDocuments$historicalCtrl$extWaterMark_ClientState=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabHistoricalDocuments$historicalCtrl$ddlNatCountry=NCYUN&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabHistoricalDocuments$historicalCtrl$txtNatYear=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabHistoricalDocuments$historicalCtrl$extWaterMarkYear_ClientState=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabVerdicts$txtCaseNumberverdicts=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabVerdicts$ddlCourtIDverdicts=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabVerdicts$txtVerdictStartDate=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabVerdicts$txtVerdictEndDate=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabVerdicts$ddlJudgmentType=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabVerdicts$txtPubImgNbrVerdicts=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$caseNumberTextbox=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$codeDescDropdownlist=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$monthFromDropdownlist=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$dayFromDropdownlist=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$yearFromDropdownlist=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$monthToDropdownlist=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$dayToDropdownlist=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$yearToDropdownlist=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$courtDropdownlist=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$keywordTextbox=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$barNumberTextbox=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$spnTextbox=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabSpecialMinutes$attorneyNameTextbox=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$txtDocketStartDateMobile=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$ddlCourtMobile=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$txtDocketStartDate=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$txtDocketEndDate=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$txtCaseNbr=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$ddlCDI=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$ddlDocketType=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$txtSPNDock=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$txtLicNum=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$ddlCourtType=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$ddlCourt=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$tabSearch$tabDocket$ddlSortBy=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$btnSearch=Search&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnPin=0" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnCaselnk=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnLogout=1&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnPinNumber=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnCDI=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnAttyLogInWithPin=false&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$txtPin=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$txtImageVC=&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$txtMPETDL=";

        string nextPageFormValues =
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$HfCaseNbr=" +
            "& ctl00 % 24ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$HfCaseCdi=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnPin=vCJST" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnCaselnk=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnLogout=1" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnPinNumber=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnCDI=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$hdnAttyLogInWithPin=false" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$txtPin=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$txtImageVC=" +
            "&ctl00$ctl00$ctl00$ContentPlaceHolder1$ContentPlaceHolder2$ContentPlaceHolder2$txtMPETDL=";
    }

    public class GZipWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            var webResponse = base.GetWebResponse(request);
            return webResponse;
        }
    }
}
