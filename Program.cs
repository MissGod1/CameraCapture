using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace CameraCapture
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var mutex = new Mutex(false, "Global\\CameraCapture"))
            {
                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show("ÔËÐÐÖÐ");
                    Environment.Exit(0);
                }
                Directory.SetCurrentDirectory(Application.StartupPath);

                if (!Directory.Exists("video"))
                {
                    Directory.CreateDirectory("video");
                }

                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());

            }

        }
    }
}
