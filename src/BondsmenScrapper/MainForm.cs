using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace BondsmenScrapper
{
    public partial class MainForm : Form
    {
        private volatile bool _started;
        private static readonly HttpClient client = new HttpClient();

        public MainForm()
        {
            InitializeComponent();
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
            var proxy = ConfigurationManager.AppSettings.Get("ProxyIP");
            int port = -1;
            int.TryParse(ConfigurationManager.AppSettings.Get("ProxyPort"), out port);

            WebProxy webProxy = null;

            if (!string.IsNullOrEmpty(proxy) && port != -1)
            {
                webProxy = new WebProxy(proxy, port);    
            }
            
            var web = new HtmlWeb();
            HtmlDocument htmlDoc;

            if (webProxy != null)
                htmlDoc = web.Load(loginUrl, "GET", webProxy, null);
            else
                htmlDoc = web.Load(loginUrl);

            var values = new Dictionary<string, string>
            {
                {"_EVENTTARGET", ""},
                {"_EVENTARGUMENT", ""},
                {"_VIEWSTATE", "wEPDwUKLTY0NDEwNzIzNw9kFgICAQ8WAh4GYWN0aW9uBVMvZURvY3MvU2VjdXJlL1dpZGVMb2dpbi5hc3B4P1JldHVyblVybD0lMmZFZG9jcyUyZlB1YmxpYyUyZnNlYXJjaC5hc3B4JTNmU2hvd0ZGJTNkMRYEAgEPZBYEAhMPDxYCHgdWaXNpYmxlaGRkAhUPDxYEHgtOYXZpZ2F0ZVVybAVTL2VEb2NzL1NlY3VyZS9XaWRlTG9naW4uYXNweD9SZXR1cm5Vcmw9JTJmRWRvY3MlMmZQdWJsaWMlMmZzZWFyY2guYXNweCUzZlNob3dGRiUzZDEfAWhkZAIDDw8WAh8BaGRkGAEFHl9fQ29udHJvbHNSZXF1aXJlUG9zdEJhY2tLZXlfXxYBBQZidG5GQVGkUD5djGE0z9dz99IDxlw5D1"},
                {"_VIEWSTATEGENERATOR", "2728C45E"},
                {"_EVENTVALIDATION", @"/wEdAAfeViMp48uSrjgWll44z7nVY3plgk0YBAefRz3MyBlTcHY2+Mc6SrnAqio3oCKbxYZXqn8G/9ILxNg5awJtLjbUlbQysvvHYo6UvZRsI/lgzil0qZQk33egat5tMnM3Tn7JNU05kLxUP1MfqDgOBl2VhEhbV4SgucMtGzWzShpD+lrIIWg="},
                {"txtUserName", "1"},
                {"txtPassword", "1"},
                {"btnLoginImageButton", "Login" }
            };

            //var content = new FormUrlEncodedContent(values);

            //var response = client.PostAsync(@"https://www.hcdistrictclerk.com/eDocs/Secure/WideLogin.aspx?ReturnUrl=%2fEdocs%2fPublic%2fsearch.aspx%3fShowFF%3d1", content);
           // var responseString = response.Content.ReadAsStringAsync();
        }


        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            btnStart.Enabled = true;

            _started = false;
            Log("Stopping...");
        }

        private void Log(string message)
        {
            tbLog.Text += $"{DateTime.Now:HH:mm:ss}: {message}\r\n";

            if (tbLog.Lines.Length > 10)
            {
                var file = Path.Combine(Application.StartupPath, $"{DateTime.Now:yyyy-MM-dd_HH-mm} .log");
                File.WriteAllText(file, tbLog.Text);

                tbLog.Text = string.Empty;
                Log("Log flushed. All data saved to " + file);
            }
        }
    }
}
