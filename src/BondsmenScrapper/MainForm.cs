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
                htmlDoc = web.Load(url, "GET", _proxy, null);
            }
            else
            {
                Log("Login failed");
                BeginInvoke(new Action(Stop));
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

            if (tbLog.Lines.Length > 10)
            {
                var file = Path.Combine(Application.StartupPath, $"{DateTime.Now:yyyy-MM-dd_HH-mm} .log");
                File.WriteAllText(file, tbLog.Text);

                BeginInvoke(new Action(() => tbLog.Text = string.Empty));
                Log("Log flushed. All data saved to " + file);
            }
        }
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
