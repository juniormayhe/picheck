using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PiCheck
{
    public static class StartupManager
    {
        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ApplicationName = "PiCheck";

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false))
                {
                    if (key == null) return false;
                    
                    string value = key.GetValue(ApplicationName) as string;
                    return !string.IsNullOrEmpty(value);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool SetStartupEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true))
                {
                    if (key == null) return false;

                    if (enabled)
                    {
                        string executablePath = Application.ExecutablePath;
                        if (File.Exists(executablePath))
                        {
                            key.SetValue(ApplicationName, executablePath);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        key.DeleteValue(ApplicationName, false);
                    }

                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Unable to modify startup settings. You may need administrator privileges.", 
                    "Startup Configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error modifying startup settings: {ex.Message}", 
                    "Startup Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static string GetRegisteredExecutablePath()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false))
                {
                    if (key == null) return string.Empty;
                    
                    return key.GetValue(ApplicationName) as string ?? string.Empty;
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}