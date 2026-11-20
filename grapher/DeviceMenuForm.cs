using grapher.Models.Devices;
using grapher.Models.Serialized;
using grapher.Models.Theming;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace grapher
{
    public class DeviceMenuForm : Form
    {
        #region Constructors
        public DeviceMenuForm(SettingsManager sm)
        {
            Manager = sm;
            defaultConfig = Manager.UserConfig.defaultDeviceConfig;

            SuspendLayout();

            Name = "DeviceMenuForm";
            Text = "Devices";
            MaximizeBox = false;
            MinimizeBox = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(580, 364);

            var titleBar = Theme.CreateBorderlessTitleBar(this, "Devices", includeMinimize: false, showAppLogo: false);

            DefaultDisableCheck = new CheckBox
            {
                Text = "Disable all devices by default",
                AutoSize = false,
                Location = new Point(14, 48),
                Size = new Size(260, 22),
                Checked = defaultConfig.disable,
            };

            var listCard = new Panel
            {
                Name = "deviceListCard",
                Location = new Point(14, 76),
                Size = new Size(256, 228),
            };
            listCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool isDark = scheme.Background.GetBrightness() < 0.5f;
                g.Clear(scheme.Background);
                Theme.DrawBentoCard(g, new RectangleF(0.5f, 0.5f, listCard.Width - 1.5f, listCard.Height - 1.5f), 10f, isDark);
            };

            DeviceSelect = new ListBox
            {
                Location = new Point(6, 6),
                Size = new Size(244, 216),
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28,
            };
            DeviceSelect.DrawItem += DeviceSelect_DrawItem;
            listCard.Controls.Add(DeviceSelect);

            var detailCard = new Panel
            {
                Name = "deviceDetailCard",
                Location = new Point(282, 76),
                Size = new Size(284, 228),
            };
            detailCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool isDark = scheme.Background.GetBrightness() < 0.5f;
                g.Clear(scheme.Background);
                Theme.DrawBentoCard(g, new RectangleF(0.5f, 0.5f, detailCard.Width - 1.5f, detailCard.Height - 1.5f), 10f, isDark);

                if (AnySelected)
                {
                    using (var divPen = new Pen(scheme.ControlBorder, 1f))
                    {
                        g.DrawLine(divPen, 14f, 58f, detailCard.Width - 14f, 58f);
                    }

                    var prevSmooth = g.SmoothingMode;
                    g.SmoothingMode = SmoothingMode.Default;
                    foreach (Control input in new Control[] { DpiInput, RateInput })
                    {
                        if (input == null || !input.Visible) continue;
                        bool unavailable = !input.Enabled;
                        bool isFocused = !unavailable && input.ContainsFocus;
                        bool isHovered = !unavailable && input.Visible &&
                            input.RectangleToScreen(input.ClientRectangle).Contains(Cursor.Position);
                        Color lineCol;
                        int lineH = 1;
                        if (isFocused)
                        {
                            lineCol = scheme.Primary;
                            lineH = 2;
                        }
                        else if (isHovered)
                        {
                            lineCol = ColorScheme.Blend(scheme.Primary, scheme.OnBackground, 0.35f);
                        }
                        else if (unavailable)
                        {
                            lineCol = ColorScheme.Blend(scheme.ControlBorder, scheme.Surface, 0.55f);
                        }
                        else
                        {
                            lineCol = ColorScheme.Blend(scheme.ControlBorder, scheme.OnField, 0.25f);
                        }

                        int lineX = input.Left;
                        int lineW = input.Width;
                        int lineY = input.Bottom + 3 - (lineH - 1);
                        using (var lineBrush = new SolidBrush(lineCol))
                        {
                            g.FillRectangle(lineBrush, lineX, lineY, lineW, lineH);
                        }
                    }
                    g.SmoothingMode = prevSmooth;
                }
            };

            EmptyHintLabel = new Label
            {
                Name = "DeviceEmptyHintLabel",
                Text = "Select a device on the left to inspect its hardware ID or override defaults.",
                AutoSize = false,
                Location = new Point(20, 20),
                Size = new Size(244, 188),
                TextAlign = ContentAlignment.MiddleCenter,
            };

            IdPanel = new Panel
            {
                Name = "deviceIdPanel",
                Location = new Point(14, 14),
                Size = new Size(256, 36),
            };

            IdText = new TextBox
            {
                Name = "deviceIdText",
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                TabStop = false,
                TextAlign = HorizontalAlignment.Left,
                Multiline = true,
                WordWrap = true,
                Location = new Point(0, 2),
                Size = new Size(256, 32),
            };
            IdPanel.Controls.Add(IdText);

            Label MakeConfigLabel(string text, int y)
            {
                return new Label
                {
                    Text = text,
                    AutoSize = false,
                    Location = new Point(14, y),
                    Size = new Size(132, 24),
                    TextAlign = ContentAlignment.MiddleLeft,
                };
            }

            OverrideLabel = MakeConfigLabel("Override defaults", 70);
            DisableLabel = MakeConfigLabel("Disable device", 104);
            DpiLabel = MakeConfigLabel("DPI", 140);
            RateLabel = MakeConfigLabel("Polling rate", 176);

            CheckBox MakeCheck(int y)
            {
                return new CheckBox
                {
                    Text = string.Empty,
                    AutoSize = false,
                    Location = new Point(153, y + 2),
                    Size = new Size(22, 20),
                };
            }

            NumericUpDown MakeNumericInput(int y, int val = 0, int min = 0, int max = 999999)
            {
                var num = new NumericUpDown
                {
                    Value = val,
                    Minimum = min,
                    Maximum = max,
                    TextAlign = HorizontalAlignment.Center,
                    BorderStyle = BorderStyle.None,
                    Location = new Point(158, y + 4),
                    Size = new Size(62, 16),
                };
                EventHandler syncVisual = (s, ev) =>
                {
                    var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
                    num.BackColor = scheme.Surface;
                    num.ForeColor = num.Enabled
                        ? ColorScheme.Blend(scheme.OnBackground, scheme.Surface, 0.10f)
                        : scheme.OnDisabledControl;
                    if (num.Controls.Count > 0)
                    {
                        num.Controls[0].Invalidate();
                    }
                    detailCard.Invalidate();
                };
                num.EnabledChanged += syncVisual;
                num.Enter += syncVisual;
                num.Leave += syncVisual;
                num.MouseEnter += syncVisual;
                num.MouseLeave += syncVisual;
                foreach (Control child in num.Controls)
                {
                    child.Enter += syncVisual;
                    child.Leave += syncVisual;
                    child.MouseEnter += syncVisual;
                    child.MouseLeave += syncVisual;
                }
                return num;
            }

            OverrideCheck = MakeCheck(70);
            DisableCheck = MakeCheck(104);
            DpiInput = MakeNumericInput(140);
            RateInput = MakeNumericInput(176);

            detailCard.Controls.AddRange(new Control[]
            {
                EmptyHintLabel,
                IdPanel,
                OverrideLabel,
                OverrideCheck,
                DisableLabel,
                DisableCheck,
                DpiLabel,
                DpiInput,
                RateLabel,
                RateInput,
            });

            var applyButton = new Button
            {
                Text = "Apply",
                DialogResult = DialogResult.OK,
                Location = new Point(374, 318),
                Size = new Size(92, 32),
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(474, 318),
                Size = new Size(92, 32),
            };

            Controls.AddRange(new Control[]
            {
                DefaultDisableCheck,
                listCard,
                detailCard,
                applyButton,
                cancelButton,
                titleBar,
            });
            titleBar.SendToBack();

            ResetDataAndSelection();
            SetEnabled(false);
            SetVisible(false);

            applyButton.Click += ApplyButton_Click;
            OverrideCheck.Click += OverrideCheck_Click;
            OverrideCheck.CheckedChanged += OverrideCheck_Checked;
            DefaultDisableCheck.CheckedChanged += DefaultDisableCheck_Checked;
            IdText.DoubleClick += SelectAllText;
            DeviceSelect.SelectedIndexChanged += DeviceSelect_SelectedIndexChanged;
            Manager.DeviceChange += OnDeviceChange;
            Disposed += OnDispose;

            var toolTip = new ToolTip();
            toolTip.SetToolTip(IdText, "Device ID");

            var rateTip = "Keep at 0 for automatic adjustment";
            toolTip.SetToolTip(RateInput, rateTip);
            toolTip.SetToolTip(RateLabel, rateTip);

            var dpiTip = "Normalizes sensitivity and input speed to 1000 DPI";
            toolTip.SetToolTip(DpiInput, dpiTip);
            toolTip.SetToolTip(DpiLabel, dpiTip);

            AcceptButton = applyButton;
            CancelButton = cancelButton;
            ResumeLayout(false);
        }

        private void DeviceSelect_DrawItem(object sender, DrawItemEventArgs e)
        {
            var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
            using (var bg = new SolidBrush(scheme.Surface))
            {
                e.Graphics.FillRectangle(bg, e.Bounds);
            }

            if (e.Index < 0 || e.Index >= DeviceSelect.Items.Count)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool isDark = scheme.Background.GetBrightness() < 0.5f;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            if (selected)
            {
                var pillRect = new RectangleF(e.Bounds.X + 2f, e.Bounds.Y + 1.5f, e.Bounds.Width - 4f, e.Bounds.Height - 3f);
                Color fill = Color.FromArgb(isDark ? 36 : 26, scheme.Primary);
                Color borderCol = Color.FromArgb(isDark ? 95 : 85, scheme.Primary);
                using (var path = Theme.CreateRoundedRectPath(pillRect, 5f))
                using (var brush = new SolidBrush(fill))
                using (var border = new Pen(borderCol, 1f))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(border, path);
                }
            }

            string text = DeviceSelect.Items[e.Index]?.ToString() ?? string.Empty;
            var textRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, Math.Max(10, e.Bounds.Width - 18), e.Bounds.Height);
            Color idleColor = ColorScheme.Blend(scheme.OnField, scheme.Surface, 0.25f);
            Color textColor = selected ? scheme.OnBackground : idleColor;
            TextRenderer.DrawText(
                e.Graphics,
                text,
                DeviceSelect.Font,
                textRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        #endregion Constructors

        #region Fields

        public DeviceConfig defaultConfig;

        #endregion Fields

        #region Properties

        public DeviceDialogItem[] Items { get; private set; }

        private DeviceDialogItem Selected
        {
            get => (DeviceDialogItem)DeviceSelect.SelectedItem;
        }

        private bool AnySelected
        {
            get => DeviceSelect.SelectedIndex != -1;
        }

        private int LastSelectedIndex { get; set; }

        private SettingsManager Manager { get; }

        private ListBox DeviceSelect { get; }

        private CheckBox DefaultDisableCheck { get; }

        private Label EmptyHintLabel { get; }

        private TextBox IdText { get; }
        private Panel IdPanel { get; }

        private Label OverrideLabel { get; }
        private CheckBox OverrideCheck { get; }

        private Label DisableLabel { get; }
        private CheckBox DisableCheck { get; }

        private Label DpiLabel { get; }
        private NumericUpDown DpiInput { get; }

        private Label RateLabel { get; }
        private NumericUpDown RateInput { get; }

        #endregion Properties

        #region Methods

        private void ResetDataAndSelection()
        {
            var count = Manager.SystemDevices.Count;
            Items = new DeviceDialogItem[count];

            for (int i = 0; i < count; i++)
            {
                var sysDev = Manager.SystemDevices[i];
                var settings = Manager.UserConfig.devices.Find(s => s.id == sysDev.id);
                bool found = !(settings is null);

                Items[i] = new DeviceDialogItem
                {
                    device = sysDev,
                    overrideDefaultConfig = found,
                    oldSettings = settings,
                    newConfig = found ?
                        settings.config :
                        Manager.UserConfig.defaultDeviceConfig,
                    newProfile = found ?
                        settings.profile :
                        Manager.UserConfig.profiles[0].name
                };
            }

            LastSelectedIndex = -1;
            DeviceSelect.ClearSelected();
            DeviceSelect.Items.Clear();
            DeviceSelect.Items.AddRange(Items);
        }

        private void SetVisible(bool visible)
        {
            if (EmptyHintLabel != null) EmptyHintLabel.Visible = !visible;
            IdPanel.Visible = visible;
            OverrideLabel.Visible = visible;
            OverrideCheck.Visible = visible;
            DisableLabel.Visible = visible;
            DisableCheck.Visible = visible;
            DpiInput.Visible = visible;
            DpiLabel.Visible = visible;
            RateInput.Visible = visible;
            RateLabel.Visible = visible;
        }

        private void SetEnabled(bool enable)
        {
            DisableLabel.Enabled = enable;
            DisableCheck.Enabled = enable;
            DpiInput.Enabled = enable;
            DpiLabel.Enabled = enable;
            RateInput.Enabled = enable;
            RateLabel.Enabled = enable;
        }

        private void SetInputsFromNewSelection()
        {
            IdText.Text = Selected.device.id;
            OverrideCheck.Checked = Selected.overrideDefaultConfig;

            SetOverrideDependentInputs();
        }

        private void SetOverrideDependentInputs()
        {
            var item = Selected;
            bool oride = item.overrideDefaultConfig;
            DisableCheck.Checked = oride ? item.newConfig.disable : defaultConfig.disable;
            DpiInput.Value = oride ? item.newConfig.dpi : defaultConfig.dpi;
            RateInput.Value = oride ? item.newConfig.pollingRate : defaultConfig.pollingRate;
        }

        private void UpdateLastSelected()
        {
            var item = Items[LastSelectedIndex];
            bool oride = OverrideCheck.Checked;
            item.overrideDefaultConfig = oride;
            item.newConfig.disable = oride ? DisableCheck.Checked : defaultConfig.disable;
            item.newConfig.dpi = oride ? (int)DpiInput.Value : defaultConfig.dpi;
            item.newConfig.pollingRate = oride ? (int)RateInput.Value : defaultConfig.pollingRate;
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            if (AnySelected) UpdateLastSelected();
        }

        private void OverrideCheck_Checked(object sender, EventArgs e)
        {
            SetEnabled(OverrideCheck.Checked);
        }

        private void OverrideCheck_Click(object sender, EventArgs e)
        {
            UpdateLastSelected();
            SetOverrideDependentInputs();
        }

        private void DefaultDisableCheck_Checked(object sender, EventArgs e)
        {
            defaultConfig.disable = DefaultDisableCheck.Checked;

            if (AnySelected && !Selected.overrideDefaultConfig)
            {
                DisableCheck.Checked = DefaultDisableCheck.Checked;
            }
        }

        private void DeviceSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AnySelected)
            {
                if (LastSelectedIndex != -1)
                {
                    UpdateLastSelected();
                }

                SetInputsFromNewSelection();
            }

            LastSelectedIndex = DeviceSelect.SelectedIndex;

            SetVisible(AnySelected);
            DpiInput?.Parent?.Invalidate();
        }

        private void OnDeviceChange(object sender, EventArgs e)
        {
            ResetDataAndSelection();
        }

        private void OnDispose(object sender, EventArgs e)
        {
            Manager.DeviceChange -= OnDeviceChange;
        }

        private static void SelectAllText(object sender, EventArgs e)
        {
            ((TextBoxBase)sender).SelectAll();
        }

        #endregion Methods
    }
}
