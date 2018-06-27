using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BondsmenScrapper.Data;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace BondsmenScrapper
{
    public partial class MainForm : Form
    {
        private volatile bool _started;
        private WebClient _client;
        private WebProxy _proxy;
        private string _cookie;

        public MainForm()
        {
            InitializeComponent();
            _client = new GZipWebClient();

            if (Debugger.IsAttached)
            {
                tbUsername.Text = "nbaca@walden.ly";
                tbPassword.Text = "1.Upwork";
                tbBondsman.Text = "74517";
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = true;
            btnStart.Enabled = false;

            tbBondsman.Enabled = false;
            tbUsername.Enabled = false;
            tbPassword.Enabled = false;

            Task.Factory.StartNew(BeginScrapping);
            //Test();
            _started = true;
            Log("Starting...");
        }

        void Test()
        {
            var web = new HtmlWeb();
            HtmlDocument htmlDoc;

            htmlDoc = web.Load("c:\\temp\\d.htm");
            try
            {
                var caseRow = htmlDoc.DocumentNode.SelectSingleNode("//table").ChildNodes[5];
                var cause =
                    $"{new string(caseRow.ChildNodes[3].InnerText.Where(char.IsDigit).ToArray())}-{new string(caseRow.ChildNodes[5].InnerText.Where(char.IsDigit).ToArray())}";

                var connectionString = ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString;

                using (var connection = new MySqlConnection(connectionString))
                {

                    // DbConnection that is already opened
                    using (var context = new DataContext(connection, false))
                    {
                        // Interception/SQL logging
                        //context.Database.Log = Console.WriteLine;

                        // *********************    CASE DETAILS **************************
                        var caseDetailsTable = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='tblCaseDetails']");

                        // No case - returning
                        if (caseDetailsTable == null) return;

                        var existingCase = context.CaseSummaries.FirstOrDefault(c => c.CaseNumber == cause);

                        var isUpdate = existingCase != null;

                        var caseSummary = existingCase ?? new CaseSummary();

                        caseSummary.CaseNumber = cause;
                        caseSummary.FileDate = DateTime.Parse(caseDetailsTable.ChildNodes[3].ChildNodes[3].InnerText);
                        caseSummary.CaseStatus = caseDetailsTable.ChildNodes[7].ChildNodes[3].InnerText;
                        caseSummary.Offense = caseDetailsTable.ChildNodes[11].ChildNodes[3].InnerText;
                        caseSummary.LastInstrumentFiled = caseDetailsTable.ChildNodes[15].ChildNodes[3].InnerText;
                        caseSummary.Disposition = caseDetailsTable.ChildNodes[19].ChildNodes[3].InnerText;
                        caseSummary.CompletionDate =
                            caseDetailsTable.ChildNodes[23].ChildNodes[3].InnerText.ToDateTimeNullable();
                        caseSummary.DefendantStatus = caseDetailsTable.ChildNodes[27].ChildNodes[3].InnerText;
                        caseSummary.BondAmount = caseDetailsTable.ChildNodes[31].ChildNodes[3].InnerText;
                        caseSummary.SettingDate =
                            caseDetailsTable.ChildNodes[35].ChildNodes[3].InnerText.ToDateTimeNullable();
                        caseSummary.CaseGuid = Guid.NewGuid().ToString();

                        // *********************** DEFENDANT DETAILS  *************************
                        var defDetailsTable =
                            htmlDoc.DocumentNode.SelectSingleNode("//table[@id='tblDefendantDetails']");
                        if (defDetailsTable != null)
                        {
                            var defTablePart1 = defDetailsTable.ChildNodes[3].ChildNodes[1].ChildNodes[1];
                            caseSummary.DefendantRaceSex =
                                defTablePart1.ChildNodes[1].ChildNodes[3].InnerText;
                            caseSummary.DefendantEyes =
                                defTablePart1.ChildNodes[5].ChildNodes[3].InnerText;
                            caseSummary.DefendantSkin =
                                defTablePart1.ChildNodes[9].ChildNodes[3].InnerText;
                            caseSummary.DefendantDob =
                                defTablePart1.ChildNodes[13].ChildNodes[3].InnerText.ToDateTimeNullable();
                            caseSummary.DefendantUsCitizen =
                                defTablePart1.ChildNodes[17].ChildNodes[3].InnerText;

                            var defTablePart2 = defDetailsTable.ChildNodes[3].ChildNodes[3].ChildNodes[1];
                            caseSummary.DefendantHeightWeight =
                                defTablePart2.ChildNodes[1].ChildNodes[3].InnerText;
                            caseSummary.DefendantHair =
                                defTablePart2.ChildNodes[5].ChildNodes[3].InnerText;
                            caseSummary.DefendantBuild =
                                defTablePart2.ChildNodes[9].ChildNodes[3].InnerText;
                            caseSummary.DefendantInCustody =
                                defTablePart2.ChildNodes[13].ChildNodes[3].InnerText;
                            caseSummary.DefendantPlaceOfBirth =
                                defTablePart2.ChildNodes[17].ChildNodes[3].InnerText;

                            caseSummary.DefendantAddress = defDetailsTable.ChildNodes[5].ChildNodes[3].InnerText;
                            caseSummary.DefendantMarkings = defDetailsTable.ChildNodes[9].ChildNodes[3].InnerText;
                        }

                        // ****************************  CURRENT PRESIDING JUDGE ***********************
                        var courtDetailsTable =
                            htmlDoc.DocumentNode.SelectSingleNode("//table[@id='tblCourtDetails']");
                        if (courtDetailsTable != null)
                        {
                            caseSummary.CpjCurrentCourt = courtDetailsTable.ChildNodes[3].ChildNodes[3].InnerText;
                            caseSummary.CpjAddress = courtDetailsTable.ChildNodes[7].ChildNodes[3].InnerText;
                            caseSummary.CpjJudgeName = courtDetailsTable.ChildNodes[11].ChildNodes[3].InnerText;
                            caseSummary.CpjCourtType = courtDetailsTable.ChildNodes[15].ChildNodes[3].InnerText;
                        }

                        if (!isUpdate)
                            context.CaseSummaries.Add(caseSummary);

                        context.SaveChanges();

                        // ***********************************   BONDS  ***********************************
                        var bondsRows = htmlDoc.DocumentNode.SelectNodes
                            ("//table[@id='tblBonds']/tr[@style='font-size:12px; ']");

                        if (isUpdate)
                        {
                            var existingBonds = context.Bonds.Where(b => b.CaseId == caseSummary.Id).ToList();
                            existingBonds.ForEach(b => context.Bonds.Remove(b));
                        }

                        if (bondsRows != null)
                        {
                            foreach (var bondsRow in bondsRows)
                            {
                                var bond = new Bond();

                                bond.Date = DateTime.Parse(bondsRow.ChildNodes[1].InnerText);
                                bond.Type = bondsRow.ChildNodes[3].InnerText;
                                bond.Description = bondsRow.ChildNodes[5].InnerText;
                                bond.Snu = bondsRow.ChildNodes[7].InnerText;
                                bond.CaseId = caseSummary.Id;

                                context.Bonds.Add(bond);
                            }
                        }

                        // ***********************************   ACTIVITIES  ***********************************
                        var activitiesRows = htmlDoc.DocumentNode.SelectNodes(
                            "//table[@id='tblActivities']/tr[@style='font-size:12px; ']");

                        if (isUpdate)
                        {
                            var existingActivities = context.Activities.Where(a => a.CaseId == caseSummary.Id).ToList();
                            existingActivities.ForEach(a => context.Activities.Remove(a));
                        }

                        if (activitiesRows != null)
                        {
                            foreach (var activitiesRow in activitiesRows)
                            {
                                var activity = new Activity();

                                activity.Date = DateTime.Parse(activitiesRow.ChildNodes[1].InnerText);
                                activity.Type = activitiesRow.ChildNodes[3].InnerText;
                                activity.Description = activitiesRow.ChildNodes[5].InnerText;
                                activity.SnuCfi = activitiesRow.ChildNodes[7].InnerText;
                                activity.CaseId = caseSummary.Id;

                                context.Activities.Add(activity);
                            }
                        }

                        // ***********************************   BOOKINGS  ***********************************
                        var bookingsRows = htmlDoc.DocumentNode.SelectNodes(
                            "//table[@id='tblBookings']/tr[@style='font-size:12px; ']");

                        if (isUpdate)
                        {
                            var existingBookings = context.Bookings.Where(b => b.CaseId == caseSummary.Id).ToList();
                            existingBookings.ForEach(b => context.Bookings.Remove(b));
                        }

                        if (bookingsRows != null)
                        {
                            foreach (var bookingsRow in bookingsRows)
                            {
                                var booking = new Booking();

                                booking.ArrestDate = bookingsRow.ChildNodes[1].InnerText.ToDateTimeNullable();
                                booking.ArrestLocation = bookingsRow.ChildNodes[3].InnerText;
                                booking.BookingDate = bookingsRow.ChildNodes[5].InnerText.ToDateTimeNullable();
                                booking.CaseId = caseSummary.Id;

                                context.Bookings.Add(booking);
                            }
                        }

                        // ***********************************   HOLDS  ***********************************
                        var holdsRows =
                            htmlDoc.DocumentNode.SelectNodes("//table[@id='tblHolds']/tr[@style='font-size:12px; ']");

                        if (isUpdate)
                        {
                            var existingHolds = context.Holds.Where(h => h.CaseId == caseSummary.Id).ToList();
                            existingHolds.ForEach(h => context.Holds.Remove(h));
                        }

                        if (holdsRows != null)
                        {
                            foreach (var holdsRow in holdsRows)
                            {
                                var hold = new Hold();

                                hold.AgencyPlacingHold = holdsRow.ChildNodes[1].InnerText;
                                hold.AgencyName = holdsRow.ChildNodes[3].InnerText;
                                hold.WarrantNumber = holdsRow.ChildNodes[5].InnerText;
                                hold.BondAmount = holdsRow.ChildNodes[7].InnerText;
                                hold.Offense = holdsRow.ChildNodes[9].InnerText;
                                hold.PlacedDate = holdsRow.ChildNodes[11].InnerText.ToDateTimeNullable();
                                hold.LiftedDate = holdsRow.ChildNodes[13].InnerText.ToDateTimeNullable();
                                hold.CaseId = caseSummary.Id;

                                context.Holds.Add(hold);
                            }
                        }

                        // ***********************************   CRIMINAL HISTORY  ***********************************
                        var crimHistRows = htmlDoc.DocumentNode.SelectNodes(
                            "//table[@id='tblCrimHist']/tr[@style='font-size:11px; vertical-align:top; ']");

                        if (isUpdate)
                        {
                            var existingHistories =
                                context.CriminalHistories.Where(h => h.CaseId == caseSummary.Id).ToList();
                            existingHistories.ForEach(h => context.CriminalHistories.Remove(h));
                        }

                        if (crimHistRows != null)
                        {
                            foreach (var crimHistRow in crimHistRows)
                            {
                                var criminalHistory = new CriminalHistory();

                                criminalHistory.CaseNumStatus = crimHistRow.ChildNodes[1].InnerText.Trim();
                                criminalHistory.Offense = crimHistRow.ChildNodes[3].InnerText.Trim();
                                criminalHistory.DateFiledBooked = crimHistRow.ChildNodes[5].InnerText.Trim();
                                criminalHistory.Court = crimHistRow.ChildNodes[7].InnerText.Trim();
                                criminalHistory.DefendantStatus = crimHistRow.ChildNodes[9].InnerText.Trim();
                                criminalHistory.Disposition = crimHistRow.ChildNodes[11].InnerText.Trim();
                                criminalHistory.BondAmount = crimHistRow.ChildNodes[13].InnerText.Trim();
                                criminalHistory.Offense = crimHistRow.ChildNodes[15].InnerText.Trim();
                                criminalHistory.NextSetting =
                                    crimHistRow.ChildNodes[17].InnerText.Trim().ToDateTimeNullable();
                                criminalHistory.CaseId = caseSummary.Id;

                                context.CriminalHistories.Add(criminalHistory);
                            }
                        }

                        // ***********************************   SETTINGS  ***********************************
                        var settingsRows = htmlDoc.DocumentNode.SelectNodes(
                            "//table[@id='tblSettings']/tr[@style='font-size:12px; ']");

                        if (isUpdate)
                        {
                            var existingSettings = context.Settings.Where(s => s.CaseId == caseSummary.Id).ToList();
                            existingSettings.ForEach(s => context.Settings.Remove(s));
                        }

                        if (settingsRows != null)
                        {
                            foreach (var settingsRow in settingsRows)
                            {
                                var setting = new Setting();

                                setting.Date = DateTime.Parse(settingsRow.ChildNodes[1].InnerText.Trim(),
                                    new CultureInfo("en-US"));
                                setting.Court = settingsRow.ChildNodes[3].InnerText.Trim();
                                setting.PostJdgm = settingsRow.ChildNodes[5].InnerText.Trim();
                                setting.DocketType = settingsRow.ChildNodes[7].InnerText.Trim();
                                setting.Reason = settingsRow.ChildNodes[9].InnerText.Trim();
                                setting.Results = settingsRow.ChildNodes[11].InnerText.Trim();
                                setting.Defendant = settingsRow.ChildNodes[13].InnerText.Trim();
                                setting.FutureDate = settingsRow.ChildNodes[15].InnerText.Trim().ToDateTimeNullable();
                                setting.Comments = settingsRow.ChildNodes[17].InnerText.Trim();
                                setting.AttorneyAppearanceIndicator = settingsRow.ChildNodes[19].InnerText.Trim();
                                setting.CaseId = caseSummary.Id;

                                context.Settings.Add(setting);

                            }
                        }

                        context.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                var error = "Error processing row. " + e.Message;
                Log(error);
                Trace.TraceError(error + e.StackTrace);
            }
        }

        private void BeginScrapping()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US", false);

            try
            {
                var loginUrl = ConfigurationManager.AppSettings.Get("LoginUrl");
                var url = ConfigurationManager.AppSettings.Get("Url");
                var proxy = ConfigurationManager.AppSettings.Get("ProxyIP");
                var port = 0;

                if(!string.IsNullOrEmpty(proxy))
                    port = int.Parse(ConfigurationManager.AppSettings.Get("ProxyPort"));

                if (!string.IsNullOrEmpty(proxy) && port > 0)
                {
                    _proxy = new WebProxy(proxy, port);
                    _client.Proxy = _proxy;
                    Log($"Using proxy {proxy}:{port}");
                }

                var web = new HtmlWeb();
                HtmlDocument htmlDoc = null;
                
                web.PostResponse = delegate(HttpWebRequest request, HttpWebResponse response)
                {
                    var cookies = response.Headers["Set-Cookie"];
                    if (cookies != null)
                    {
                        var sessionId = cookies.Split(';')[0];
                        _client.Headers.Add(HttpRequestHeader.Cookie, sessionId + "; Login=");
                    }
                };

                Log("Connecting to the website");

                new Action(() =>
                {
                    if(!_started) return;

                    if (_proxy != null)
                        htmlDoc = web.Load(loginUrl, "GET", _proxy, null);
                    else
                        htmlDoc = web.Load(loginUrl);
                    //todo:check if page is ok
                }).ExecuteWithAttempts(1000, (exception, i) => i > 5, (exception, i) => Log($"Error: {exception.Message}. Retrying..."));

                Thread.Sleep(2000);

                if (!_started) return;
                Log("Trying to login");

                var login = false;
                new Action(() =>
                {
                    if (!_started) return;

                    login = Login(htmlDoc, loginUrl);
                }).ExecuteWithAttempts(1000, (exception, i) => i > 5, (exception, i) => Log($"Error: {exception.Message}. Retrying..."));


                if (login)
                {
                    Log("Login successful");

                    web = new HtmlWeb();
                    web.PreRequest = delegate(HttpWebRequest request)
                    {
                        request.Headers["Cookie"] = _cookie;
                        return true;
                    };
                    Log("Loading search form");

                    new Action(() =>
                    {
                        if (!_started) return;

                        if (_proxy != null)
                            htmlDoc = web.Load(url, "GET", _proxy, null);
                        else
                            htmlDoc = web.Load(url);
                        //todo:check if page is ok
                    }).ExecuteWithAttempts(1000, (exception, i) => i > 5, (exception, i) => Log($"Error: {exception.Message}. Retrying..."));
                    
                    Log("Making a request");

                    var result = string.Empty;
                    new Action(() =>
                    {
                        result = Search(htmlDoc);
                        //todo:check if page is ok
                    }).ExecuteWithAttempts(1000, (exception, i) => i > 5, (exception, i) => Log($"Error: {exception.Message}. Retrying..."));


                    if (!string.IsNullOrEmpty(result))
                    {
                        var lastPage = GetLastPage(result);
                        Log($"Total pages count = {lastPage}");

                        var page = 1;
                        do
                        {
                            Log($"Processing page = {page}");

                            if (page > 1)
                                new Action(() =>
                                {
                                    result = GetNextResultsPage(htmlDoc, page);
                                    //todo:check if page is ok
                                }).ExecuteWithAttempts(1000, (exception, i) => i > 5,
                                    (exception, i) => Log($"Error: {exception.Message}. Retrying..."));

                            if (!_started)
                                break;

                            htmlDoc = new HtmlDocument();
                            htmlDoc.LoadHtml(result);

                            var rows =
                                htmlDoc.DocumentNode.SelectNodes(
                                    "//table[@class='resultHeader contentwidth']/tr[not(@class='trResultHeader')]");
                            if (rows != null)
                            {
                                // 'Type of action / Offense' field is not empty
                                var filledRows =
                                    rows.Where(row => !string.IsNullOrEmpty(row.ChildNodes[11].InnerText.Trim()))
                                        .ToList();

                                var processedRows = 0;
                                foreach (var row in filledRows)
                                {
                                    if (ProcessTableRow(web, row))
                                        processedRows++;
                                    File.WriteAllText("c://temp//res.htm", htmlDoc.DocumentNode.InnerHtml);
                                    if (!_started)
                                        break;

                                    Thread.Sleep(1000);
                                }
                                Log($"Processed {processedRows} rows");
                            }
                            else
                            {
                                Log("No result rows found");
                            }
                            page++;

                            if (!_started)
                                break;

                            Thread.Sleep((page%4 + 1)*1000);

                        } while (page <= lastPage);
                    }
                    else
                    {
                        Log("No result found");
                    }
                }
                else
                {
                    Log("Login failed");
                }
            }
            catch (Exception e)
            {
                Log("An error occured. " + e.Message);
                Trace.TraceError(e.Message + e.StackTrace);
            }
            finally
            {
                BeginInvoke(new Action(Stop));
            }
        }

        private static int GetLastPage(string result)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(result);
                var lastPageLink = doc.DocumentNode.SelectSingleNode("//a[@title=' to Last Page ']").Attributes["href"];
                var lastPage = int.Parse(lastPageLink.Value.Split('\'')[3]);
                return lastPage;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message + e.StackTrace);
                return 1;
            }
        }

        private bool ProcessTableRow(HtmlWeb web, HtmlNode row)
        {
            try
            {
                var link1 = row.ChildNodes[3].ChildNodes[1];
                var link = link1.GetAttributeValue("onclick", "");
                if (!string.IsNullOrEmpty(link))
                {
                    var query = link.Split('\'')[1];
                    var url = ConfigurationManager.AppSettings["CaseUrl"] + query;

                    var htmlDoc = new HtmlDocument();
                    new Action(() =>
                    {
                        if (_proxy != null)
                            htmlDoc = web.Load(url, "GET", _proxy, null);
                        else
                            htmlDoc = web.Load(url);

                        //todo:check if page is ok
                    }).ExecuteWithAttempts(1000, (exception, i) => i > 5,
                        (exception, i) => Log($"Error: {exception.Message}. Retrying..."));

                    File.WriteAllText($"{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}.htm", htmlDoc.DocumentNode.InnerHtml);
                    var res = _client.DownloadString(url);
                    File.WriteAllText($"{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}.htm", res);
                    var caseRow = htmlDoc.DocumentNode.SelectSingleNode("//table").ChildNodes[5];
                    var cause =
                        $"{new string(caseRow.ChildNodes[3].InnerText.Where(char.IsDigit).ToArray())}-{new string(caseRow.ChildNodes[5].InnerText.Where(char.IsDigit).ToArray())}";

                    var connectionString = ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString;

                    using (var connection = new MySqlConnection(connectionString))
                    {

                        using (var context = new DataContext(connection, false))
                        {
                            // Interception/SQL logging
                            //context.Database.Log = Console.WriteLine;

                            // *********************    CASE DETAILS **************************
                            var caseDetailsTable = htmlDoc.DocumentNode.SelectSingleNode("//table[@id='tblCaseDetails']");

                            // No case - returning
                            if (caseDetailsTable == null) return false;

                            var existingCase = context.CaseSummaries.FirstOrDefault(c => c.CaseNumber == cause);

                            var isUpdate = existingCase != null;

                            // If case exists and we shouldn't skip it then updating all linked entities
                            if (isUpdate && bool.Parse(ConfigurationManager.AppSettings["SkipIfDuplicate"]))
                            {
                                Log($"Case {cause} is duplicated, skipping");
                                return false;
                            }

                            var caseSummary = existingCase ?? new CaseSummary();

                            caseSummary.CaseNumber = cause;
                            caseSummary.FileDate = DateTime.Parse(caseDetailsTable.ChildNodes[3].ChildNodes[3].InnerText);
                            caseSummary.CaseStatus = caseDetailsTable.ChildNodes[7].ChildNodes[3].InnerText;
                            caseSummary.Offense = caseDetailsTable.ChildNodes[11].ChildNodes[3].InnerText;
                            caseSummary.LastInstrumentFiled = caseDetailsTable.ChildNodes[15].ChildNodes[3].InnerText;
                            caseSummary.Disposition = caseDetailsTable.ChildNodes[19].ChildNodes[3].InnerText;
                            caseSummary.CompletionDate =
                                caseDetailsTable.ChildNodes[23].ChildNodes[3].InnerText.ToDateTimeNullable();
                            caseSummary.DefendantStatus = caseDetailsTable.ChildNodes[27].ChildNodes[3].InnerText;
                            caseSummary.BondAmount = caseDetailsTable.ChildNodes[31].ChildNodes[3].InnerText;
                            caseSummary.SettingDate =
                                caseDetailsTable.ChildNodes[35].ChildNodes[3].InnerText.ToDateTimeNullable();
                            caseSummary.CaseGuid = Guid.NewGuid().ToString();

                            // *********************** DEFENDANT DETAILS  *************************
                            var defDetailsTable =
                                htmlDoc.DocumentNode.SelectSingleNode("//table[@id='tblDefendantDetails']");
                            if (defDetailsTable != null)
                            {
                                var defTablePart1 = defDetailsTable.ChildNodes[3].ChildNodes[1].ChildNodes[1];
                                caseSummary.DefendantRaceSex =
                                    defTablePart1.ChildNodes[1].ChildNodes[3].InnerText;
                                caseSummary.DefendantEyes =
                                    defTablePart1.ChildNodes[5].ChildNodes[3].InnerText;
                                caseSummary.DefendantSkin =
                                    defTablePart1.ChildNodes[9].ChildNodes[3].InnerText;
                                caseSummary.DefendantDob =
                                    defTablePart1.ChildNodes[13].ChildNodes[3].InnerText.ToDateTimeNullable();
                                caseSummary.DefendantUsCitizen =
                                    defTablePart1.ChildNodes[17].ChildNodes[3].InnerText;

                                var defTablePart2 = defDetailsTable.ChildNodes[3].ChildNodes[3].ChildNodes[1];
                                caseSummary.DefendantHeightWeight =
                                    defTablePart2.ChildNodes[1].ChildNodes[3].InnerText;
                                caseSummary.DefendantHair =
                                    defTablePart2.ChildNodes[5].ChildNodes[3].InnerText;
                                caseSummary.DefendantBuild =
                                    defTablePart2.ChildNodes[9].ChildNodes[3].InnerText;
                                caseSummary.DefendantInCustody =
                                    defTablePart2.ChildNodes[13].ChildNodes[3].InnerText;
                                caseSummary.DefendantPlaceOfBirth =
                                    defTablePart2.ChildNodes[17].ChildNodes[3].InnerText;

                                caseSummary.DefendantAddress = defDetailsTable.ChildNodes[5].ChildNodes[3].InnerText;
                                caseSummary.DefendantMarkings = defDetailsTable.ChildNodes[9].ChildNodes[3].InnerText;
                            }

                            // ****************************  CURRENT PRESIDING JUDGE ***********************
                            var courtDetailsTable =
                                htmlDoc.DocumentNode.SelectSingleNode("//table[@id='tblCourtDetails']");
                            if (courtDetailsTable != null)
                            {
                                caseSummary.CpjCurrentCourt = courtDetailsTable.ChildNodes[3].ChildNodes[3].InnerText;
                                caseSummary.CpjAddress = courtDetailsTable.ChildNodes[7].ChildNodes[3].InnerText;
                                caseSummary.CpjJudgeName = courtDetailsTable.ChildNodes[11].ChildNodes[3].InnerText;
                                caseSummary.CpjCourtType = courtDetailsTable.ChildNodes[15].ChildNodes[3].InnerText;
                            }

                            if (!isUpdate)
                                context.CaseSummaries.Add(caseSummary);

                            context.SaveChanges();

                            // ***********************************   BONDS  ***********************************
                            var bondsRows = htmlDoc.DocumentNode.SelectNodes
                                ("//table[@id='tblBonds']/tr[@style='font-size:12px; ']");

                            if (isUpdate)
                            {
                                var existingBonds = context.Bonds.Where(b => b.CaseId == caseSummary.Id).ToList();
                                existingBonds.ForEach(b => context.Bonds.Remove(b));
                            }

                            if (bondsRows != null)
                            {
                                foreach (var bondsRow in bondsRows)
                                {
                                    var bond = new Bond();

                                    bond.Date = DateTime.Parse(bondsRow.ChildNodes[1].InnerText);
                                    bond.Type = bondsRow.ChildNodes[3].InnerText;
                                    bond.Description = bondsRow.ChildNodes[5].InnerText;
                                    bond.Snu = bondsRow.ChildNodes[7].InnerText;
                                    bond.CaseId = caseSummary.Id;

                                    context.Bonds.Add(bond);
                                }
                            }

                            // ***********************************   ACTIVITIES  ***********************************
                            var activitiesRows = htmlDoc.DocumentNode.SelectNodes(
                                "//table[@id='tblActivities']/tr[@style='font-size:12px; ']");

                            if (isUpdate)
                            {
                                var existingActivities =
                                    context.Activities.Where(a => a.CaseId == caseSummary.Id).ToList();
                                existingActivities.ForEach(a => context.Activities.Remove(a));
                            }

                            if (activitiesRows != null)
                            {
                                foreach (var activitiesRow in activitiesRows)
                                {
                                    var activity = new Activity();

                                    activity.Date = DateTime.Parse(activitiesRow.ChildNodes[1].InnerText);
                                    activity.Type = activitiesRow.ChildNodes[3].InnerText;
                                    activity.Description = activitiesRow.ChildNodes[5].InnerText;
                                    activity.SnuCfi = activitiesRow.ChildNodes[7].InnerText;
                                    activity.CaseId = caseSummary.Id;

                                    context.Activities.Add(activity);
                                }
                            }

                            // ***********************************   BOOKINGS  ***********************************
                            var bookingsRows = htmlDoc.DocumentNode.SelectNodes(
                                "//table[@id='tblBookings']/tr[@style='font-size:12px; ']");

                            if (isUpdate)
                            {
                                var existingBookings = context.Bookings.Where(b => b.CaseId == caseSummary.Id).ToList();
                                existingBookings.ForEach(b => context.Bookings.Remove(b));
                            }

                            if (bookingsRows != null)
                            {
                                foreach (var bookingsRow in bookingsRows)
                                {
                                    var booking = new Booking();

                                    booking.ArrestDate = bookingsRow.ChildNodes[1].InnerText.ToDateTimeNullable();
                                    booking.ArrestLocation = bookingsRow.ChildNodes[3].InnerText;
                                    booking.BookingDate = bookingsRow.ChildNodes[5].InnerText.ToDateTimeNullable();
                                    booking.CaseId = caseSummary.Id;

                                    context.Bookings.Add(booking);
                                }
                            }

                            // ***********************************   HOLDS  ***********************************
                            var holdsRows =
                                htmlDoc.DocumentNode.SelectNodes("//table[@id='tblHolds']/tr[@style='font-size:12px; ']");

                            if (isUpdate)
                            {
                                var existingHolds = context.Holds.Where(h => h.CaseId == caseSummary.Id).ToList();
                                existingHolds.ForEach(h => context.Holds.Remove(h));
                            }

                            if (holdsRows != null)
                            {
                                foreach (var holdsRow in holdsRows)
                                {
                                    var hold = new Hold();

                                    hold.AgencyPlacingHold = holdsRow.ChildNodes[1].InnerText;
                                    hold.AgencyName = holdsRow.ChildNodes[3].InnerText;
                                    hold.WarrantNumber = holdsRow.ChildNodes[5].InnerText;
                                    hold.BondAmount = holdsRow.ChildNodes[7].InnerText;
                                    hold.Offense = holdsRow.ChildNodes[9].InnerText;
                                    hold.PlacedDate = holdsRow.ChildNodes[11].InnerText.ToDateTimeNullable();
                                    hold.LiftedDate = holdsRow.ChildNodes[13].InnerText.ToDateTimeNullable();
                                    hold.CaseId = caseSummary.Id;

                                    context.Holds.Add(hold);
                                }
                            }

                            // ***********************************   CRIMINAL HISTORY  ***********************************
                            var crimHistRows = htmlDoc.DocumentNode.SelectNodes(
                                "//table[@id='tblCrimHist']/tr[@style='font-size:11px; vertical-align:top; ']");

                            if (isUpdate)
                            {
                                var existingHistories =
                                    context.CriminalHistories.Where(h => h.CaseId == caseSummary.Id).ToList();
                                existingHistories.ForEach(h => context.CriminalHistories.Remove(h));
                            }

                            if (crimHistRows != null)
                            {
                                foreach (var crimHistRow in crimHistRows)
                                {
                                    var criminalHistory = new CriminalHistory();

                                    criminalHistory.CaseNumStatus = crimHistRow.ChildNodes[1].InnerText.Trim();
                                    criminalHistory.Offense = crimHistRow.ChildNodes[3].InnerText.Trim();
                                    criminalHistory.DateFiledBooked = crimHistRow.ChildNodes[5].InnerText.Trim();
                                    criminalHistory.Court = crimHistRow.ChildNodes[7].InnerText.Trim();
                                    criminalHistory.DefendantStatus = crimHistRow.ChildNodes[9].InnerText.Trim();
                                    criminalHistory.Disposition = crimHistRow.ChildNodes[11].InnerText.Trim();
                                    criminalHistory.BondAmount = crimHistRow.ChildNodes[13].InnerText.Trim();
                                    criminalHistory.Offense = crimHistRow.ChildNodes[15].InnerText.Trim();
                                    criminalHistory.NextSetting =
                                        crimHistRow.ChildNodes[17].InnerText.Trim().ToDateTimeNullable();
                                    criminalHistory.CaseId = caseSummary.Id;

                                    context.CriminalHistories.Add(criminalHistory);
                                }
                            }

                            // ***********************************   SETTINGS  ***********************************
                            var settingsRows = htmlDoc.DocumentNode.SelectNodes(
                                "//table[@id='tblSettings']/tr[@style='font-size:12px; ']");

                            if (isUpdate)
                            {
                                var existingSettings = context.Settings.Where(s => s.CaseId == caseSummary.Id).ToList();
                                existingSettings.ForEach(s => context.Settings.Remove(s));
                            }

                            if (settingsRows != null)
                            {
                                foreach (var settingsRow in settingsRows)
                                {
                                    var setting = new Setting();

                                    setting.Date = DateTime.Parse(settingsRow.ChildNodes[1].InnerText.Trim(),
                                        new CultureInfo("en-US"));
                                    setting.Court = settingsRow.ChildNodes[3].InnerText.Trim();
                                    setting.PostJdgm = settingsRow.ChildNodes[5].InnerText.Trim();
                                    setting.DocketType = settingsRow.ChildNodes[7].InnerText.Trim();
                                    setting.Reason = settingsRow.ChildNodes[9].InnerText.Trim();
                                    setting.Results = settingsRow.ChildNodes[11].InnerText.Trim();
                                    setting.Defendant = settingsRow.ChildNodes[13].InnerText.Trim();
                                    setting.FutureDate =
                                        settingsRow.ChildNodes[15].InnerText.Trim().ToDateTimeNullable();
                                    setting.Comments = settingsRow.ChildNodes[17].InnerText.Trim();
                                    setting.AttorneyAppearanceIndicator = settingsRow.ChildNodes[19].InnerText.Trim();
                                    setting.CaseId = caseSummary.Id;

                                    context.Settings.Add(setting);
                                }
                            }

                            context.SaveChanges();
                        }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                var error = "Error processing row. " + e.Message;
                Log(error);
                Trace.TraceError(error + e.StackTrace);
                File.WriteAllText($"{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}.htm", row.InnerHtml);
            }
            return false;
        }

        private string Search(HtmlDocument htmlDoc)
        {
            var eventTarget =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTTARGET']")?.GetAttributeValue("value", "");

            var eventArgument =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTARGUMENT']")?.GetAttributeValue("value", "");
            var viewState =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATE']")?.GetAttributeValue("value", "");
            var viewStateGenerator =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATEGENERATOR']")?.GetAttributeValue("value", "");
            var viewStateEncrypted =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATEENCRYPTED']")?.GetAttributeValue("value", "");
            var eventValidation =
                htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTVALIDATION']")?.GetAttributeValue("value", "");

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

            _client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            _client.Headers[HttpRequestHeader.Host] = "www.hcdistrictclerk.com";
            _client.Headers[HttpRequestHeader.Cookie] = _cookie;

            var result = Encoding.ASCII.GetString(_client.UploadValues(url, values));
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

            var result = Encoding.ASCII.GetString(_client.UploadValues(url, values));
            return result;
        }


        private void AddSearchFormValues(NameValueCollection values)
        {
            // insert license #
            var formattedPairs = string.Format(searchFormValues, tbBondsman.Text);
            // insert required search form values
            var pairs = formattedPairs.Split('&');
            foreach (var pair in pairs)
            {
                try
                {
                    var key = pair.Split('=')[0];
                    var value = pair.Split('=')[1];
                    values.Add(key, value);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.Message + e.StackTrace);
                }
            }
        }

        private void AddNextPageFormValues(NameValueCollection values)
        {
            // insert required form values to get the next page
            var pairs = nextPageFormValues.Split('&');
            foreach (var pair in pairs)
            {
                try
                {
                    var key = pair.Split('=')[0];
                    var value = pair.Split('=')[1];
                    values.Add(key, value);
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.Message + e.StackTrace);
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

            _client.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
            _client.Headers.Add(HttpRequestHeader.Host, "www.hcdistrictclerk.com");
            _client.Headers.Add("DNT", "1");
            _client.Headers.Add(HttpRequestHeader.Referer, loginUrl);
            _client.Headers.Add("Upgrade-Insecure-Requests", "1");
            _client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 6.3; Win64; x64; rv:60.0) Gecko/20100101 Firefox/60.0");
            _client.Headers.Add(HttpRequestHeader.KeepAlive,"");
            _client.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _client.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
            _client.Headers.Add(HttpRequestHeader.AcceptLanguage, "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");
            
            _client.UploadValues(loginUrl, values);
            _cookie = _client.ResponseHeaders["Set-Cookie"];

            // if login is successful, cookie length should be more than 500 symbols
            return _cookie.Length > 500;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            _started = false;
            btnStop.Enabled = false;
            Log("Stopping...");
        }

        private void Stop()
        {
            btnStop.Enabled = false;
            btnStart.Enabled = true;
            tbBondsman.Enabled = true;
            tbUsername.Enabled = true;
            tbPassword.Enabled = true;

            Log("Stopped");
        }

        private void Log(string message)
        {
            BeginInvoke(new Action(() =>
            {
                tbLog.AppendText($"{DateTime.Now:HH:mm:ss}: {message}\r\n");
                tbLog.ScrollToCaret();
            }));

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
