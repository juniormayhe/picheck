using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PiCheck
{
    public partial class MainForm : Form
    {
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private Timer checkTimer;
        private SshChecker sshChecker;
        private string sshTarget;
        private DateTime nextCheckTime;
        private bool isOnline = false;
        private ToolStripMenuItem startupMenuItem;

        public MainForm()
        {
            InitializeComponent();
            InitializeApplication();
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.Text = "PiCheck";
            this.Size = new Size(0, 0);
        }

        private void InitializeApplication()
        {
            sshChecker = new SshChecker();
            sshTarget = Properties.Settings.Default.SshTarget;
            
            if (string.IsNullOrEmpty(sshTarget))
            {
                sshTarget = "junior@100.117.1.121";
                Properties.Settings.Default.SshTarget = sshTarget;
                Properties.Settings.Default.Save();
            }

            SetupTrayIcon();
            SetupTimer();
            
            // Initial check
            _ = CheckConnectivityAsync();
        }

        private void SetupTrayIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = LoadEmbeddedIcon("picheck-connecting.ico");
            notifyIcon.Visible = true;
            notifyIcon.Text = "PiCheck - Starting...";

            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Force Check Now", null, ForceCheck_Click);
            contextMenu.Items.Add("Configure...", null, Configure_Click);
            contextMenu.Items.Add("-");
            startupMenuItem = new ToolStripMenuItem("Start with Windows", null, StartupToggle_Click);
            startupMenuItem.CheckOnClick = true;
            startupMenuItem.Checked = StartupManager.IsStartupEnabled();
            contextMenu.Items.Add(startupMenuItem);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, Exit_Click);

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        private void SetupTimer()
        {
            checkTimer = new Timer();
            checkTimer.Interval = 3600000; // 1 hour
            checkTimer.Tick += async (s, e) => await CheckConnectivityAsync();
            checkTimer.Start();
            
            nextCheckTime = DateTime.Now.AddHours(1);
        }

        private async Task CheckConnectivityAsync()
        {
            try
            {
                bool wasOnline = isOnline;
                isOnline = await sshChecker.CheckSshConnectivityAsync(sshTarget);
                
                // Ensure UI updates happen on UI thread
                if (InvokeRequired)
                {
                    Invoke(new Action(() => {
                        UpdateTrayIcon();
                    }));
                }
                else
                {
                    UpdateTrayIcon();
                }
                
                nextCheckTime = DateTime.Now.AddHours(1);
                
                // Show balloon tip on status change
                if (wasOnline != isOnline)
                {
                    string message = isOnline ? 
                        $"{sshTarget} is now online" : 
                        $"{sshTarget} is now offline";
                    
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => {
                            notifyIcon.ShowBalloonTip(3000, "PiCheck", message, 
                                isOnline ? ToolTipIcon.Info : ToolTipIcon.Warning);
                        }));
                    }
                    else
                    {
                        notifyIcon.ShowBalloonTip(3000, "PiCheck", message, 
                            isOnline ? ToolTipIcon.Info : ToolTipIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                isOnline = false;
                
                if (InvokeRequired)
                {
                    Invoke(new Action(() => {
                        UpdateTrayIcon();
                        notifyIcon.ShowBalloonTip(3000, "PiCheck", 
                            $"Error checking connectivity: {ex.Message}", ToolTipIcon.Error);
                    }));
                }
                else
                {
                    UpdateTrayIcon();
                    notifyIcon.ShowBalloonTip(3000, "PiCheck", 
                        $"Error checking connectivity: {ex.Message}", ToolTipIcon.Error);
                }
            }
        }

        private void UpdateTrayIcon()
        {
            string iconFile = isOnline ? "picheck.ico" : "picheck-offline.ico";
            string status = isOnline ? "online" : "offline";
            string timeUntilNext = GetTimeUntilNextCheck();
            
            notifyIcon.Icon = LoadEmbeddedIcon(iconFile);
            notifyIcon.Text = $"{sshTarget} is {status}\nNext check: {timeUntilNext}";
        }

        private string GetTimeUntilNextCheck()
        {
            var timeSpan = nextCheckTime - DateTime.Now;
            if (timeSpan.TotalMinutes < 1)
                return "in less than 1 minute";
            else if (timeSpan.TotalHours < 1)
                return $"in {(int)timeSpan.TotalMinutes} minutes";
            else
                return $"in {(int)timeSpan.TotalHours} hours {timeSpan.Minutes} minutes";
        }

        private Icon LoadEmbeddedIcon(string iconFileName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                
                // Try different resource name formats
                string[] possibleResourceNames = {
                    $"picheck.{iconFileName}",           // Based on build output format
                    $"PiCheck.{iconFileName}",           // Original format
                    iconFileName,                        // Just the filename
                    $"picheck.Resources.{iconFileName}", // Common pattern
                };
                
                foreach (var resourceName in possibleResourceNames)
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            return new Icon(stream);
                        }
                    }
                }
                
                // Debug: List all available resources
                var resourceNames = assembly.GetManifestResourceNames();
                System.Diagnostics.Debug.WriteLine($"Available resources: {string.Join(", ", resourceNames)}");
                System.Diagnostics.Debug.WriteLine($"Looking for: {iconFileName}");
                
                // Fallback: try loading from file system if embedded resource not found
                if (File.Exists(iconFileName))
                {
                    return new Icon(iconFileName);
                }
                
                throw new FileNotFoundException($"Icon file not found: {iconFileName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Icon loading failed: {ex.Message}");
                
                // Create a simple fallback icon if loading fails
                var bitmap = new Bitmap(16, 16);
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Gray);
                    g.FillEllipse(Brushes.DarkGray, 2, 2, 12, 12);
                }
                
                IntPtr hIcon = bitmap.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        private async void ForceCheck_Click(object sender, EventArgs e)
        {
            notifyIcon.Text = "PiCheck - Checking...";
            await CheckConnectivityAsync();
        }

        private async void Configure_Click(object sender, EventArgs e)
        {
            using (var configDialog = new ConfigDialog(sshTarget))
            {
                if (configDialog.ShowDialog() == DialogResult.OK)
                {
                    sshTarget = configDialog.SshTarget;
                    Properties.Settings.Default.SshTarget = sshTarget;
                    Properties.Settings.Default.Save();
                    
                    // Show immediate feedback
                    notifyIcon.Text = "PiCheck - Checking new configuration...";
                    notifyIcon.Icon = LoadEmbeddedIcon("picheck-connecting.ico");
                    
                    // Immediate check with new configuration
                    await CheckConnectivityAsync();
                    
                    // Update startup menu item state in case it changed
                    startupMenuItem.Checked = StartupManager.IsStartupEnabled();
                }
            }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Configure_Click(sender, e);
        }

        private void StartupToggle_Click(object sender, EventArgs e)
        {
            bool newStartupState = startupMenuItem.Checked;
            
            if (StartupManager.SetStartupEnabled(newStartupState))
            {
                // Success - the menu item state is already updated due to CheckOnClick
                string message = newStartupState ? 
                    "PiCheck will now start with Windows" : 
                    "PiCheck will no longer start with Windows";
                notifyIcon.ShowBalloonTip(2000, "Startup Setting", message, ToolTipIcon.Info);
            }
            else
            {
                // Failed - revert the menu item state
                startupMenuItem.Checked = !newStartupState;
            }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            Application.Exit();
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                notifyIcon?.Dispose();
                contextMenu?.Dispose();
                checkTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.ComponentModel.IContainer components = null;
    }
}