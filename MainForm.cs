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
        private NotificationManager notificationManager;
        private bool isFirstCheck = true;

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
            notificationManager = new NotificationManager();
            notificationManager.ConfigureRequested += OnNotificationConfigureRequested;
            notificationManager.ForceCheckRequested += OnNotificationForceCheckRequested;
            
            // Load SSH target from isolated storage
            LoadUserSettings();
            
            if (string.IsNullOrEmpty(sshTarget))
            {
                sshTarget = "junior@100.117.1.121";
                SaveUserSettings();
                System.Diagnostics.Debug.WriteLine($"Initialized default SSH target: {sshTarget}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Loaded SSH target from settings: {sshTarget}");
            }

            SetupTrayIcon();
            SetupTimer();
            
            // Initial check
            _ = CheckConnectivityAsync();
            
            // Clear any lingering notifications on startup
            notificationManager.ClearAllNotifications();
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
                
                // Handle notifications on status change or first check
                if (wasOnline != isOnline || (isFirstCheck && !isOnline))
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => {
                            HandleNotificationForStatusChange(wasOnline, isOnline, isFirstCheck);
                        }));
                    }
                    else
                    {
                        HandleNotificationForStatusChange(wasOnline, isOnline, isFirstCheck);
                    }
                }
                
                // Mark that we've completed the first check
                isFirstCheck = false;
            }
            catch (Exception ex)
            {
                isOnline = false;
                
                if (InvokeRequired)
                {
                    Invoke(new Action(() => {
                        UpdateTrayIcon();
                        // Show persistent notification for connectivity errors (treated as offline)
                        notificationManager.ShowOfflineNotification(sshTarget);
                        notifyIcon.ShowBalloonTip(3000, "PiCheck", 
                            $"Error checking connectivity: {ex.Message}", ToolTipIcon.Error);
                    }));
                }
                else
                {
                    UpdateTrayIcon();
                    // Show persistent notification for connectivity errors (treated as offline)
                    notificationManager.ShowOfflineNotification(sshTarget);
                    notifyIcon.ShowBalloonTip(3000, "PiCheck", 
                        $"Error checking connectivity: {ex.Message}", ToolTipIcon.Error);
                }
                
                // Mark that we've completed the first check even on error
                isFirstCheck = false;
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

        private void HandleNotificationForStatusChange(bool wasOnline, bool isNowOnline, bool isFirstCheck = false)
        {
            if (isFirstCheck)
            {
                System.Diagnostics.Debug.WriteLine($"First connectivity check: {sshTarget} is {(isNowOnline ? "online" : "offline")}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Status change: {wasOnline} -> {isNowOnline} for {sshTarget}");
            }
            
            if (isNowOnline)
            {
                // Host is/came online - clear persistent notification and show positive feedback
                System.Diagnostics.Debug.WriteLine($"Clearing offline notifications for {sshTarget}");
                notificationManager.ClearOfflineNotification(sshTarget);
                
                // Only show balloon tip for status changes (not first check if online)
                if (!isFirstCheck)
                {
                    notifyIcon.ShowBalloonTip(3000, "PiCheck", 
                        $"{sshTarget} is now online", ToolTipIcon.Info);
                }
            }
            else
            {
                // Host is/went offline - show persistent notification
                System.Diagnostics.Debug.WriteLine($"Showing offline notification for {sshTarget}");
                notificationManager.ShowOfflineNotification(sshTarget);
                
                // Show balloon tip for both status changes and first check if offline
                string message = isFirstCheck ? 
                    $"{sshTarget} is offline" : 
                    $"{sshTarget} is now offline";
                notifyIcon.ShowBalloonTip(3000, "PiCheck", message, ToolTipIcon.Warning);
            }
        }

        private async void OnNotificationConfigureRequested(object sender, EventArgs e)
        {
            // Handle configure request from notification
            Configure_Click(sender, e);
        }

        private async void OnNotificationForceCheckRequested(object sender, EventArgs e)
        {
            // Handle force check request from notification
            ForceCheck_Click(sender, e);
        }

        private async void ForceCheck_Click(object sender, EventArgs e)
        {
            notifyIcon.Text = "PiCheck - Checking...";
            isFirstCheck = false; // Ensure force checks are not treated as first checks
            await CheckConnectivityAsync();
        }

        private async void Configure_Click(object sender, EventArgs e)
        {
            string oldTarget = sshTarget;
            using (var configDialog = new ConfigDialog(sshTarget))
            {
                if (configDialog.ShowDialog() == DialogResult.OK)
                {
                    string newTarget = configDialog.SshTarget;
                    
                    // If target changed, clear any existing notifications for old target
                    if (oldTarget != newTarget)
                    {
                        System.Diagnostics.Debug.WriteLine($"SSH target changed from {oldTarget} to {newTarget}");
                        notificationManager.ClearOfflineNotification(oldTarget);
                    }
                    
                    sshTarget = newTarget;
                    SaveUserSettings();
                    
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

        private void LoadUserSettings()
        {
            try
            {
                // Upgrade settings from previous version if needed
                if (Properties.Settings.Default.UpgradeRequired)
                {
                    System.Diagnostics.Debug.WriteLine("Upgrading user settings from previous version");
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.UpgradeRequired = false;
                    Properties.Settings.Default.Save();
                }

                sshTarget = Properties.Settings.Default.SshTarget;
                System.Diagnostics.Debug.WriteLine($"Settings loaded from: {GetSettingsFilePath()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading user settings: {ex.Message}");
                sshTarget = string.Empty; // Will trigger default initialization
            }
        }

        private void SaveUserSettings()
        {
            try
            {
                Properties.Settings.Default.SshTarget = sshTarget;
                Properties.Settings.Default.Save();
                System.Diagnostics.Debug.WriteLine($"Settings saved. SSH Target: {sshTarget}");
                System.Diagnostics.Debug.WriteLine($"Settings stored at: {GetSettingsFilePath()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving user settings: {ex.Message}");
            }
        }

        private string GetSettingsFilePath()
        {
            try
            {
                // Get the settings file path for debugging purposes
                var config = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                    System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal);
                return config.FilePath;
            }
            catch
            {
                return "Unable to determine settings path";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Save settings one final time on disposal
                SaveUserSettings();
                
                // Clean up notification event handlers
                if (notificationManager != null)
                {
                    notificationManager.ConfigureRequested -= OnNotificationConfigureRequested;
                    notificationManager.ForceCheckRequested -= OnNotificationForceCheckRequested;
                    notificationManager.Dispose();
                }
                
                notifyIcon?.Dispose();
                contextMenu?.Dispose();
                checkTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.ComponentModel.IContainer components = null;
    }
}