using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace SysVital
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

            // Check if running as administrator
            bool isAdmin = IsRunningAsAdministrator();
            
            if (!isAdmin)
            {
                // Ask user if they want to restart as administrator
                DialogResult result = MessageBox.Show(
                    "SysVital requires administrator privileges to access hardware information.\n\n" +
                    "Would you like to restart the application as administrator?",
                    "Administrator Privileges Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.Yes)
                {
                    // Restart as administrator
                    RestartAsAdministrator();
                    return;
                }
                
                // Warn user about limited functionality
                MessageBox.Show(
                    "Some features may not work correctly without administrator privileges.",
                    "Limited Functionality",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            
            Application.Run(new Form1());
        }
        
        /// <summary>
        /// Checks if the application is running with administrator privileges
        /// </summary>
        private static bool IsRunningAsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        
        /// <summary>
        /// Restarts the application with administrator privileges
        /// </summary>
        private static void RestartAsAdministrator()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;
            startInfo.Verb = "runas"; // This is what triggers the UAC prompt
            
            try
            {
                Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // User cancelled the UAC prompt
                MessageBox.Show(
                    "Administrator privileges are required to access all features.",
                    "Operation Cancelled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
