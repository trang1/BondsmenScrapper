using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BondsmenScrapper
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public static class Extensions
    {
        public static DateTime? ToDateTimeNullable(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            DateTime dt;
            if (DateTime.TryParse(str, out dt))
                return dt;
            return null;
        }

        public static decimal? ToDecimalNullable(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            decimal dt;
            if (decimal.TryParse(str, out dt))
                return dt;
            return null;
        }
    }
}
