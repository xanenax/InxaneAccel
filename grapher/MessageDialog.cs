using grapher.Models.Theming;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace grapher
{
    public partial class MessageDialog : Form
    {
        public MessageDialog(string message, string title = "")
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            AutoSize = false;
            var header = Theme.CreateBorderlessTitleBar(this, string.IsNullOrWhiteSpace(title) ? "Notice" : title, includeMinimize: false);
            Controls.Add(header);
            header.SendToBack();

            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Padding = new Padding(16, 12, 16, 14);
            Text = title;
            messageLabel.MaximumSize = new Size(340, 0);
            messageLabel.Text = message;

            int desiredW = Math.Max(280, Math.Min(390, messageLabel.PreferredWidth + 48));
            int desiredH = Math.Max(140, header.Height + messageLabel.PreferredHeight + 76);
            ClientSize = new Size(desiredW, desiredH);
        }
    }
}
