using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace BondsmenScrapper
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // Initialize and start our tracer
            Tracer.StartTracing();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception)
            {
                Trace.TraceError("UnhandledException: {0}", (Exception) e.ExceptionObject);
            }
            else
            {
                Trace.TraceError("UnhandledException: '{0}'", e.ExceptionObject);
            }
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
    }
}