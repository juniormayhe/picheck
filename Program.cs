using System;
using System.Windows.Forms;

namespace PiCheck
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            
            // Ensure only one instance is running
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "PiCheckApplication", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("PiCheck is already running.", "PiCheck", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                Application.Run(new MainForm());
            }
        }
    }
}