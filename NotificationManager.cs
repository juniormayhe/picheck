using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PiCheck
{
    public class NotificationManager
    {
        private List<PersistentNotificationForm> activeNotifications = new List<PersistentNotificationForm>();
        private HashSet<string> activeNotificationTargets = new HashSet<string>();
        private const int NotificationSpacing = 10;

        public event EventHandler ConfigureRequested;
        public event EventHandler ForceCheckRequested;

        public void ShowOfflineNotification(string sshTarget)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to show offline notification for: {sshTarget}");
                
                // Enhanced duplicate prevention - check multiple criteria
                if (activeNotificationTargets.Contains(sshTarget))
                {
                    System.Diagnostics.Debug.WriteLine($"Target {sshTarget} already tracked in active targets set");
                    return; // Already showing notification for this target
                }

                // Double-check with visible notifications list
                foreach (var existing in activeNotifications)
                {
                    if (existing.Visible && existing.SshTarget == sshTarget)
                    {
                        System.Diagnostics.Debug.WriteLine($"Notification already exists and visible for target: {sshTarget}");
                        // Ensure tracking set is in sync
                        activeNotificationTargets.Add(sshTarget);
                        return; // Already showing notification for this target
                    }
                }

                // Track this target as having an active notification
                activeNotificationTargets.Add(sshTarget);

                // Create new persistent notification
                var notification = new PersistentNotificationForm(
                    "PiCheck - Host Offline",
                    $"{sshTarget} is currently offline",
                    sshTarget);

                // Position the notification
                PositionNotification(notification);

                // Wire up notification events
                notification.ConfigureRequested += (s, e) => 
                {
                    ConfigureRequested?.Invoke(this, EventArgs.Empty);
                };
                
                notification.ForceCheckRequested += (s, e) => 
                {
                    ForceCheckRequested?.Invoke(this, EventArgs.Empty);
                };

                // Show the notification
                notification.Show();
                activeNotifications.Add(notification);
                
                System.Diagnostics.Debug.WriteLine($"Created and showed notification for: {sshTarget}. Total active: {activeNotifications.Count}");

                // Handle when notification is closed
                notification.FormClosed += (s, e) =>
                {
                    activeNotifications.Remove(notification);
                    activeNotificationTargets.Remove(sshTarget);
                    System.Diagnostics.Debug.WriteLine($"Notification closed for: {sshTarget}. Remaining active: {activeNotifications.Count}");
                    RepositionNotifications();
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show notification: {ex.Message}");
            }
        }

        public void ClearOfflineNotification(string sshTarget = null)
        {
            try
            {
                var notificationsToClose = new List<PersistentNotificationForm>();
                
                System.Diagnostics.Debug.WriteLine($"Looking for notifications to clear for target: {sshTarget ?? "all"}");
                System.Diagnostics.Debug.WriteLine($"Active notifications count: {activeNotifications.Count}");
                
                foreach (var notification in activeNotifications)
                {
                    System.Diagnostics.Debug.WriteLine($"Checking notification for target: {notification.SshTarget}, visible: {notification.Visible}");
                    
                    if (sshTarget == null || notification.SshTarget == sshTarget)
                    {
                        notificationsToClose.Add(notification);
                        System.Diagnostics.Debug.WriteLine($"Marking notification for closure: {notification.SshTarget}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Closing {notificationsToClose.Count} notifications");
                
                foreach (var notification in notificationsToClose)
                {
                    if (notification.Visible)
                    {
                        notification.Close();
                        activeNotificationTargets.Remove(notification.SshTarget);
                        System.Diagnostics.Debug.WriteLine($"Closed notification for target: {notification.SshTarget}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear notification: {ex.Message}");
            }
        }

        public void ClearAllNotifications()
        {
            ClearOfflineNotification();
            // Ensure tracking set is fully cleared
            activeNotificationTargets.Clear();
            System.Diagnostics.Debug.WriteLine("Cleared all notification targets from tracking set");
        }

        private void PositionNotification(PersistentNotificationForm notification)
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int yPosition = workingArea.Bottom - notification.Height - 10;

            // Stack notifications from bottom up
            foreach (var existing in activeNotifications)
            {
                if (existing.Visible)
                    yPosition -= (existing.Height + NotificationSpacing);
            }

            notification.Location = new Point(
                workingArea.Right - notification.Width - 10,
                Math.Max(workingArea.Top + 10, yPosition)
            );
        }

        private void RepositionNotifications()
        {
            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;
            int yPosition = workingArea.Bottom - 10;

            // Reposition all visible notifications from bottom up
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                var notification = activeNotifications[i];
                if (notification.Visible)
                {
                    yPosition -= notification.Height;
                    notification.Location = new Point(
                        notification.Location.X,
                        Math.Max(workingArea.Top + 10, yPosition)
                    );
                    yPosition -= NotificationSpacing;
                }
            }
        }

        public void Dispose()
        {
            try
            {
                ClearAllNotifications();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dispose error: {ex.Message}");
            }
        }
    }

    public partial class PersistentNotificationForm : Form
    {
        private Label titleLabel;
        private Label messageLabel;
        private Button dismissButton;
        private Button configureButton;
        private Button forceCheckButton;
        
        public string SshTarget { get; private set; }
        public event EventHandler ConfigureRequested;
        public event EventHandler ForceCheckRequested;

        public PersistentNotificationForm(string title, string message, string sshTarget)
        {
            SshTarget = sshTarget;
            InitializeComponent();
            SetupNotification(title, message);
        }

        private void InitializeComponent()
        {
            this.titleLabel = new Label();
            this.messageLabel = new Label();
            this.dismissButton = new Button();
            this.configureButton = new Button();
            this.forceCheckButton = new Button();
            this.SuspendLayout();

            // Form properties
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(420, 200);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(75, 0, 130); // Dark purple/indigo
            this.Text = "PiCheck Notification";

            // Title label
            this.titleLabel.Location = new Point(15, 15);
            this.titleLabel.Size = new Size(390, 24);
            this.titleLabel.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold);
            this.titleLabel.ForeColor = Color.White;

            // Message label
            this.messageLabel.Location = new Point(15, 42);
            this.messageLabel.Size = new Size(390, 50);
            this.messageLabel.Font = new Font("Microsoft Sans Serif", 9F);
            this.messageLabel.ForeColor = Color.White;

            // Configure button
            this.configureButton.Location = new Point(20, 120);
            this.configureButton.Size = new Size(90, 28);
            this.configureButton.Text = "Configure";
            this.configureButton.Font = new Font("Microsoft Sans Serif", 8.5F);
            this.configureButton.BackColor = Color.FromArgb(100, 20, 150); // Slightly lighter purple
            this.configureButton.ForeColor = Color.White;
            this.configureButton.FlatStyle = FlatStyle.Flat;
            this.configureButton.FlatAppearance.BorderColor = Color.White;
            this.configureButton.FlatAppearance.BorderSize = 1;
            this.configureButton.UseVisualStyleBackColor = false;
            this.configureButton.Click += new EventHandler(this.ConfigureButton_Click);

            // Force Check button
            this.forceCheckButton.Location = new Point(125, 120);
            this.forceCheckButton.Size = new Size(90, 28);
            this.forceCheckButton.Text = "Check Now";
            this.forceCheckButton.Font = new Font("Microsoft Sans Serif", 8.5F);
            this.forceCheckButton.BackColor = Color.FromArgb(100, 20, 150); // Slightly lighter purple
            this.forceCheckButton.ForeColor = Color.White;
            this.forceCheckButton.FlatStyle = FlatStyle.Flat;
            this.forceCheckButton.FlatAppearance.BorderColor = Color.White;
            this.forceCheckButton.FlatAppearance.BorderSize = 1;
            this.forceCheckButton.UseVisualStyleBackColor = false;
            this.forceCheckButton.Click += new EventHandler(this.ForceCheckButton_Click);

            // Dismiss button
            this.dismissButton.Location = new Point(310, 120);
            this.dismissButton.Size = new Size(90, 28);
            this.dismissButton.Text = "Dismiss";
            this.dismissButton.Font = new Font("Microsoft Sans Serif", 8.5F);
            this.dismissButton.BackColor = Color.FromArgb(100, 20, 150); // Slightly lighter purple
            this.dismissButton.ForeColor = Color.White;
            this.dismissButton.FlatStyle = FlatStyle.Flat;
            this.dismissButton.FlatAppearance.BorderColor = Color.White;
            this.dismissButton.FlatAppearance.BorderSize = 1;
            this.dismissButton.UseVisualStyleBackColor = false;
            this.dismissButton.Click += new EventHandler(this.DismissButton_Click);

            // Add controls to form
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.messageLabel);
            this.Controls.Add(this.configureButton);
            this.Controls.Add(this.forceCheckButton);
            this.Controls.Add(this.dismissButton);

            this.ResumeLayout(false);
        }

        private void SetupNotification(string title, string message)
        {
            this.titleLabel.Text = title;
            this.messageLabel.Text = message;
        }

        private void ConfigureButton_Click(object sender, EventArgs e)
        {
            ConfigureRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ForceCheckButton_Click(object sender, EventArgs e)
        {
            ForceCheckRequested?.Invoke(this, EventArgs.Empty);
        }

        private void DismissButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected override bool ShowWithoutActivation => true;

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value && !this.DesignMode);
        }
    }
}