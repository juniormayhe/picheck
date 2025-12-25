using System;
using System.Drawing;
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
            notifyIcon.Icon = CreateCircleIcon(Color.Gray);
            notifyIcon.Visible = true;
            notifyIcon.Text = "PiCheck - Starting...";

            contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Force Check Now", null, ForceCheck_Click);
            contextMenu.Items.Add("Configure...", null, Configure_Click);
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
            Color iconColor = isOnline ? Color.Green : Color.Red;
            string status = isOnline ? "online" : "offline";
            string timeUntilNext = GetTimeUntilNextCheck();
            
            notifyIcon.Icon = CreateCircleIcon(iconColor);
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

        private Icon CreateCircleIcon(Color color)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(color))
                {
                    g.FillEllipse(brush, 2, 2, 12, 12);
                }
                using (var pen = new Pen(Color.Black, 1))
                {
                    g.DrawEllipse(pen, 2, 2, 12, 12);
                }
            }
            
            IntPtr hIcon = bitmap.GetHicon();
            Icon icon = Icon.FromHandle(hIcon);
            return icon;
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
                    notifyIcon.Icon = CreateCircleIcon(Color.Orange);
                    
                    // Immediate check with new configuration
                    await CheckConnectivityAsync();
                }
            }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Configure_Click(sender, e);
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