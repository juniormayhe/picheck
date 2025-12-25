using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace PiCheck
{
    public class ConfigDialog : Form
    {
        private TextBox textBoxSshTarget;
        private Button buttonOk;
        private Button buttonCancel;
        private Label labelInstruction;
        private CheckBox checkBoxStartup;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SshTarget { get; set; }

        public ConfigDialog(string currentTarget)
        {
            InitializeComponent();
            SshTarget = currentTarget;
            textBoxSshTarget.Text = currentTarget;
            checkBoxStartup.Checked = StartupManager.IsStartupEnabled();
        }

        private void InitializeComponent()
        {
            this.textBoxSshTarget = new TextBox();
            this.buttonOk = new Button();
            this.buttonCancel = new Button();
            this.labelInstruction = new Label();
            this.checkBoxStartup = new CheckBox();
            this.SuspendLayout();

            // labelInstruction
            this.labelInstruction.AutoSize = true;
            this.labelInstruction.Location = new System.Drawing.Point(12, 15);
            this.labelInstruction.Size = new System.Drawing.Size(200, 15);
            this.labelInstruction.Text = "Enter SSH target (user@hostname):";

            // textBoxSshTarget
            this.textBoxSshTarget.Location = new System.Drawing.Point(15, 35);
            this.textBoxSshTarget.Size = new System.Drawing.Size(250, 23);
            this.textBoxSshTarget.TabIndex = 0;

            // checkBoxStartup
            this.checkBoxStartup.AutoSize = true;
            this.checkBoxStartup.Location = new System.Drawing.Point(15, 70);
            this.checkBoxStartup.Size = new System.Drawing.Size(120, 19);
            this.checkBoxStartup.TabIndex = 1;
            this.checkBoxStartup.Text = "Start with Windows";
            this.checkBoxStartup.UseVisualStyleBackColor = true;

            // buttonOk
            this.buttonOk.Location = new System.Drawing.Point(110, 105);
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.ButtonOk_Click);
            this.buttonOk.DialogResult = DialogResult.OK;

            // buttonCancel
            this.buttonCancel.Location = new System.Drawing.Point(190, 105);
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.DialogResult = DialogResult.Cancel;

            // ConfigDialog
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 145);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.checkBoxStartup);
            this.Controls.Add(this.textBoxSshTarget);
            this.Controls.Add(this.labelInstruction);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "SSH Configuration";
            this.AcceptButton = this.buttonOk;
            this.CancelButton = this.buttonCancel;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxSshTarget.Text))
            {
                MessageBox.Show("Please enter a valid SSH target.", "Invalid Input", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SshTarget = textBoxSshTarget.Text.Trim();
            
            // Handle startup setting change
            bool currentStartupEnabled = StartupManager.IsStartupEnabled();
            if (checkBoxStartup.Checked != currentStartupEnabled)
            {
                StartupManager.SetStartupEnabled(checkBoxStartup.Checked);
            }
        }
    }
}