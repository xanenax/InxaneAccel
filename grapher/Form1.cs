using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using grapher.Models;
using grapher.Models.Options;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using grapher.Models.Serialized;
using grapher.Models.Theming;
using grapher.Common;

namespace grapher
{
    public partial class RawAcceleration : Form
    {

        #region Constructor


        public RawAcceleration()
        {
            InitializeComponent();

            Version driverVersion = VersionHelper.ValidOrThrow();

            var schemes = ColorSchemeManager.LoadSchemes().ToList();
            var themeMenuItem = new ToolStripMenuItem("&Themes");

            themeMenuItem.DropDownItemClicked += (s, e) =>
            {
                if (!(e.ClickedItem is ToolStripMenuItem clicked)) return;

                bool isThemeClick = Equals(clicked.Tag, "ThemeItem");
                foreach (var item in themeMenuItem.DropDownItems.OfType<ToolStripMenuItem>())
                {
                    bool itemIsTheme = Equals(item.Tag, "ThemeItem");
                    if (itemIsTheme == isThemeClick)
                    {
                        item.Checked = item == clicked;
                    }
                }
            };

            var settings = GUISettings.MaybeLoad();
            var selectedSchemeName = ColorSchemeManager.NormalizeSchemeName(settings?.CurrentColorScheme);
            var selectedAccentName = string.IsNullOrWhiteSpace(settings?.CurrentAccentColor)
                ? "Indigo"
                : settings.CurrentAccentColor;

            Theme.CurrentScheme = ColorSchemeManager.GetSelected(settings, schemes);

            var coreSchemes = new[]
            {
                ColorScheme.MacOSDarkTheme,
                ColorScheme.AppleLightTheme
            };

            foreach (var colorScheme in coreSchemes)
            {
                var menuItem = new ToolStripMenuItem(colorScheme.Name) { Tag = "ThemeItem" };

                menuItem.Checked = string.Equals(selectedSchemeName, colorScheme.Name, StringComparison.OrdinalIgnoreCase);
                themeMenuItem.DropDownItems.Add(menuItem);
            }

            themeMenuItem.DropDownItems.Add(new ToolStripSeparator());

            foreach (var accent in ColorScheme.AppleAccents)
            {
                var accentItem = new ToolStripMenuItem($"Accent: {accent.Name}") { Tag = accent.Name };
                accentItem.Checked = string.Equals(selectedAccentName, accent.Name, StringComparison.OrdinalIgnoreCase);
                themeMenuItem.DropDownItems.Add(accentItem);
            }

            menuStrip1.Items.Add(themeMenuItem);

            Theme.Apply(this, menuStrip1);

            AccelGUI = AccelGUIFactory.Construct(
                this,
                AccelerationChart,
                AccelerationChartY,
                VelocityChart,
                VelocityChartY,
                GainChart,
                GainChartY,
                chartContainer,
                accelTypeDropX,
                accelTypeDropY,
                XLutApplyDropdown,
                YLutApplyDropdown,
                CapTypeDropdownXClassic,
                CapTypeDropdownYClassic,
                CapTypeDropdownXPower,
                CapTypeDropdownYPower,
                writeButton,
                toggleButton,
                showVelocityGainToolStripMenuItem,
                showLastMouseMoveToolStripMenuItem,
                AutoWriteMenuItem,
                DeviceMenuItem,
                ScaleMenuItem,
                themeMenuItem,
                DPITextBox,
                PollRateTextBox,
                DirectionalityPanel,
                sensitivityBoxX,
                VertHorzRatioBox,
                rotationBox,
                inCapBoxXClassic,
                inCapBoxYClassic,
                outCapBoxXClassic,
                outCapBoxYClassic,
                inCapBoxXPower,
                inCapBoxYPower,
                outCapBoxXPower,
                outCapBoxYPower,
                inputJumpBoxX,
                inputJumpBoxY,
                outputJumpBoxX,
                outputJumpBoxY,
                inputOffsetBoxX,
                inputOffsetBoxY,
                outputOffsetBoxX,
                outputOffsetBoxY,
                accelerationBoxX,
                accelerationBoxY,
                decayRateBoxX,
                decayRateBoxY,
                gammaBoxX,
                gammaBoxY,
                smoothBoxX,
                smoothBoxY,
                scaleBoxX,
                scaleBoxY,
                limitBoxX,
                limitBoxY,
                powerBoxX,
                powerBoxY,
                expBoxX,
                expBoxY,
                syncSpeedBoxX,
                syncSpeedBoxY,
                DomainBoxX,
                DomainBoxY,
                RangeBoxX,
                RangeBoxY,
                LpNormBox,
                sensXYLock,
                ByComponentXYLock,
                FakeBox,
                WholeCheckBox,
                ByComponentCheckBox,
                gainSwitchX,
                gainSwitchY,
                XLutActiveValuesBox,
                YLutActiveValuesBox,
                XLutPointsBox,
                YLutPointsBox,
                LockXYLabel,
                sensitivityLabel,
                VertHorzRatioLabel,
                rotationLabel,
                inCapLabelXClassic,
                inCapLabelYClassic,
                outCapLabelXClassic,
                outCapLabelYClassic,
                CapTypeLabelXClassic,
                CapTypeLabelYClassic,
                inCapLabelXPower,
                inCapLabelYPower,
                outCapLabelXPower,
                outCapLabelYPower,
                CapTypeLabelXPower,
                CapTypeLabelYPower,
                inputJumpLabelX,
                inputJumpLabelY,
                outputJumpLabelX,
                outputJumpLabelY,
                inputOffsetLabelX,
                inputOffsetLabelY,
                outputOffsetLabelX,
                outputOffsetLabelY,
                constantOneLabelX,
                constantOneLabelY,
                decayRateLabelX,
                decayRateLabelY,
                gammaLabelX,
                gammaLabelY,
                smoothLabelX,
                smoothLabelY,
                scaleLabelX,
                scaleLabelY,
                limitLabelX,
                limitLabelY,
                powerLabelX,
                powerLabelY,
                expLabelX,
                expLabelY,
                LUTTextLabelX,
                LUTTextLabelY,
                constantThreeLabelX,
                constantThreeLabelY,
                ActiveValueTitle,
                ActiveValueTitleY,
                SensitivityMultiplierActiveLabel,
                VertHorzRatioActiveLabel,
                RotationActiveLabel,
                InCapActiveXLabelClassic,
                InCapActiveYLabelClassic,
                OutCapActiveXLabelClassic,
                OutCapActiveYLabelClassic,
                CapTypeActiveXLabelClassic,
                CapTypeActiveYLabelClassic,
                InCapActiveXLabelPower,
                InCapActiveYLabelPower,
                OutCapActiveXLabelPower,
                OutCapActiveYLabelPower,
                CapTypeActiveXLabelPower,
                CapTypeActiveYLabelPower,
                InputJumpActiveXLabel,
                InputJumpActiveYLabel,
                OutputJumpActiveXLabel,
                OutputJumpActiveYLabel,
                InputOffsetActiveXLabel,
                InputOffsetActiveYLabel,
                OutputOffsetActiveXLabel,
                OutputOffsetActiveYLabel,
                AccelerationActiveLabelX,
                AccelerationActiveLabelY,
                DecayRateActiveXLabel,
                DecayRateActiveYLabel,
                GammaActiveXLabel,
                GammaActiveYLabel,
                SmoothActiveXLabel,
                SmoothActiveYLabel,
                ScaleActiveXLabel,
                ScaleActiveYLabel,
                LimitActiveXLabel,
                LimitActiveYLabel,
                PowerClassicActiveXLabel,
                PowerClassicActiveYLabel,
                ExpActiveXLabel,
                ExpActiveYLabel,
                SyncSpeedActiveXLabel,
                SyncSpeedActiveYLabel,
                AccelTypeActiveLabelX,
                AccelTypeActiveLabelY,
                gainSwitchActiveLabelX,
                gainSwitchActiveLabelY,
                OptionSetXTitle,
                OptionSetYTitle,
                MouseLabel,
                DirectionalityLabel,
                DirectionalityX,
                DirectionalityY,
                DirectionalityActiveValueTitle,
                LPNormLabel,
                LpNormActiveValue,
                DirectionalDomainLabel,
                DomainActiveValueX,
                DomainActiveValueY,
                DirectionalityRangeLabel,
                RangeActiveValueX,
                RangeActiveValueY,
                XLutApplyLabel,
                YLutApplyLabel,
                LutApplyActiveXLabel,
                LutApplyActiveYLabel);

            CurvePresetManager.Initialize(XLutPointsBox.Text);
            InitializeDashboardControls();
        }

        #endregion Constructor

        #region Properties

        public AccelGUI AccelGUI { get; }

        public bool IsExpertDrawerOpen { get; private set; }

        public bool IsVelocityAndGainOpen { get; private set; }

        private Panel _titleBarPanel;
        private Panel _bottomDeckPanel;
        private NativeSurfaceButton _curvePresetSwitcher;
        private NativeSurfaceButton _expertDrawerButton;
        private int _hoveredPresetSegment = -1;
        private int _lockedSettingsButtonX = -1;
        private bool _presetSwitchedOnMouseDown;

        #endregion Properties

        #region Methods

        private void InitializeDashboardControls()
        {
            _titleBarPanel = Theme.CreateBorderlessTitleBar(this, "Inxane Accel", includeMinimize: true);
            Controls.Add(_titleBarPanel);
            // Highest index docks first in WinForms, so SendToBack lets the top bar span the full width.
            _titleBarPanel.SendToBack();
            chartsPanel.BringToFront();

            _bottomDeckPanel = new NativeSurfacePanel
            {
                Name = "bottomDeckPanel",
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = (Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme).Surface
            };
            _bottomDeckPanel.Paint += BottomDeckPanel_Paint;
            _bottomDeckPanel.Resize += (s, e) => LayoutBottomDeckControls();

            _curvePresetSwitcher = new NativeSurfaceButton
            {
                Name = "curvePresetSwitcher",
                Tag = "CustomDashboardControl",
                Text = "CurvePresetSwitcher",
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Location = new Point(16, 10),
                Size = new Size(240, 32),
                TabStop = false,
                Cursor = Cursors.Hand
            };
            _curvePresetSwitcher.CustomPaint += CurvePresetSwitcher_Paint;
            _curvePresetSwitcher.MouseMove += (s, e) =>
            {
                int seg = e.X < _curvePresetSwitcher.Width / 2 ? 0 : 1;
                if (seg != _hoveredPresetSegment)
                {
                    _hoveredPresetSegment = seg;
                    _curvePresetSwitcher.Invalidate();
                }
            };
            _curvePresetSwitcher.MouseLeave += (s, e) =>
            {
                _hoveredPresetSegment = -1;
                _presetSwitchedOnMouseDown = false;
                _curvePresetSwitcher.Invalidate();
            };
            _curvePresetSwitcher.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                _presetSwitchedOnMouseDown = true;
                string target = e.X < _curvePresetSwitcher.Width / 2
                    ? CurvePresetManager.PresetOriginal
                    : CurvePresetManager.PresetCleanBump;
                ApplyCurvePreset(target);
            };
            _curvePresetSwitcher.Click += (s, e) =>
            {
                if (_presetSwitchedOnMouseDown)
                {
                    _presetSwitchedOnMouseDown = false;
                    return;
                }

                string next = CurvePresetManager.ActivePreset == CurvePresetManager.PresetOriginal
                    ? CurvePresetManager.PresetCleanBump
                    : CurvePresetManager.PresetOriginal;
                ApplyCurvePreset(next);
            };

            _expertDrawerButton = new NativeSurfaceButton
            {
                Name = "expertDrawerButton",
                Tag = "CustomDashboardControl",
                Text = "Settings",
                AutoSize = false,
                Dock = DockStyle.None,
                UseMnemonic = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                MinimumSize = new Size(92, 28),
                MaximumSize = new Size(92, 28),
                Size = new Size(92, 28),
                Location = new Point(392, 6),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                TabStop = false,
                Cursor = Cursors.Hand
            };
            _expertDrawerButton.CustomPaint += ExpertDrawerButton_Paint;
            _expertDrawerButton.MouseEnter += (s, e) => _expertDrawerButton.Invalidate();
            _expertDrawerButton.MouseLeave += (s, e) => _expertDrawerButton.Invalidate();
            _expertDrawerButton.MouseDown += (s, e) => _expertDrawerButton.Invalidate();
            _expertDrawerButton.MouseUp += (s, e) => _expertDrawerButton.Invalidate();
            _expertDrawerButton.Click += (s, e) => ToggleExpertDrawer();

            // Keep MouseLabel alive for Raw Accel loop while keeping the readout hidden from UI.
            MouseLabel.Visible = false;

            // Edge-to-edge footer directly in Zone 2 with balanced padding around charts.
            chartsPanel.Padding = new Padding(0);
            chartContainer.Padding = new Padding(14, 10, 14, 10);
            Theme.EnableDoubleBuffering(chartContainer);

            // Host navigation menus and Settings toggle cleanly inside the 40px top header bar,
            // and place the Curve Preset + Reset / Apply toolbar cleanly in the flush bottom footer.
            optionsPanel.Controls.Remove(writeButton);
            optionsPanel.Controls.Remove(toggleButton);
            optionsPanel.Controls.Remove(menuStrip1);

            menuStrip1.Dock = DockStyle.None;
            menuStrip1.AutoSize = false;
            menuStrip1.Height = 28;
            menuStrip1.Location = new Point(146, 6);
            MainMenuStrip = menuStrip1;

            _titleBarPanel.Controls.Add(menuStrip1);
            _titleBarPanel.Controls.Add(_expertDrawerButton);
            _titleBarPanel.Resize += (s, e) => LayoutHeaderControls();

            _bottomDeckPanel.Controls.Add(_curvePresetSwitcher);
            _bottomDeckPanel.Controls.Add(writeButton);
            _bottomDeckPanel.Controls.Add(toggleButton);

            chartsPanel.Controls.Add(_bottomDeckPanel);
            _bottomDeckPanel.SendToBack();
            chartContainer.BringToFront();

            Theme.Apply(this, menuStrip1);
            optionsPanel.AutoScroll = false;
            optionsPanel.AutoScrollMargin = new Size(0, 6);
            optionsPanel.HorizontalScroll.Enabled = false;
            optionsPanel.HorizontalScroll.Visible = false;
            optionsPanel.Scroll += (s, ev) => optionsPanel.Invalidate();
            optionsPanel.MouseWheel += (s, ev) => optionsPanel.Invalidate();
            Theme.ApplyScrollBarTheme(optionsPanel);
            AccelGUI?.ApplyOptions?.OptionSetX?.Options?.RefreshLayout();
            AccelGUI?.ApplyOptions?.OptionSetY?.Options?.RefreshLayout();
            AccelGUI?.ApplyOptions?.ReflowInspectorLayout();
            LayoutHeaderControls();
            LayoutBottomDeckControls();
        }

        private void LayoutHeaderControls()
        {
            if (_titleBarPanel == null || _expertDrawerButton == null)
            {
                return;
            }

            menuStrip1.Location = new Point(146, 6);
            const int closedBaseWidth = 620;
            const int maxSafeClosedLeft = closedBaseWidth - 84 - 92 - 8; // 436
            int desiredLeft = Math.Min(maxSafeClosedLeft, Math.Max(menuStrip1.Right + 12, 392));
            if (_lockedSettingsButtonX < 0)
            {
                _lockedSettingsButtonX = desiredLeft;
            }
            _expertDrawerButton.Location = new Point(_lockedSettingsButtonX, 6);
            _expertDrawerButton.Size = new Size(92, 28);
        }

        private void LayoutBottomDeckControls()
        {
            if (_bottomDeckPanel == null)
            {
                return;
            }

            int w = _bottomDeckPanel.ClientSize.Width;
            if (w <= 320)
            {
                w = 620;
            }

            const int marginX = 16;
            const int ctrlY = 10;
            const int ctrlH = 32;
            const int btnGap = 8;
            const int applyW = 104;
            const int resetW = 88;
            const int switcherW = 240;

            _curvePresetSwitcher.Location = new Point(marginX, ctrlY);
            _curvePresetSwitcher.Size = new Size(Math.Min(switcherW, Math.Max(180, w - (marginX * 2) - applyW - resetW - btnGap - 16)), ctrlH);

            int applyX = w - marginX - applyW;
            int resetX = applyX - btnGap - resetW;
            toggleButton.Location = new Point(resetX, ctrlY);
            toggleButton.Size = new Size(resetW, ctrlH);
            writeButton.Location = new Point(applyX, ctrlY);
            writeButton.Size = new Size(applyW, ctrlH);
        }

        private void BottomDeckPanel_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Panel panel) || panel.Width < 40 || panel.Height < 20)
            {
                return;
            }

            var g = e.Graphics;
            var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
            g.Clear(scheme.Surface);

            using (var pen = new Pen(scheme.ControlBorder, 1f))
            {
                g.DrawLine(pen, 0f, 0.5f, panel.Width, 0.5f);
            }
        }

        private void ClearAllComboSelections()
        {
            accelTypeDropX?.ClearEditSelection();
            accelTypeDropY?.ClearEditSelection();
            XLutApplyDropdown?.ClearEditSelection();
            YLutApplyDropdown?.ClearEditSelection();
            CapTypeDropdownXClassic?.ClearEditSelection();
            CapTypeDropdownYClassic?.ClearEditSelection();
            CapTypeDropdownXPower?.ClearEditSelection();
            CapTypeDropdownYPower?.ClearEditSelection();
        }

        public void ApplyCurvePreset(string presetName)
        {
            AccelGUI?.SelectCurvePreset(
                presetName,
                XLutPointsBox,
                YLutPointsBox,
                accelTypeDropX,
                accelTypeDropY);
            ClearAllComboSelections();
            _curvePresetSwitcher?.Invalidate();
            optionsPanel?.Invalidate();
            _bottomDeckPanel?.Invalidate();
        }

        public void SetVelocityAndGainMode(bool showVelocityAndGain)
        {
            IsVelocityAndGainOpen = showVelocityAndGain;
            if (_bottomDeckPanel == null)
            {
                return;
            }

            var workingArea = Screen.FromControl(this).WorkingArea;
            int targetClientHeight = GetTargetClientHeight(workingArea);
            SuspendLayout();
            ClientSize = new Size(ClientSize.Width, targetClientHeight);
            ResumeLayout(true);

            if (Bottom > workingArea.Bottom)
            {
                Top = Math.Max(workingArea.Top, workingArea.Bottom - Height);
            }
            LayoutBottomDeckControls();
            chartsPanel.Invalidate(true);
            optionsPanel.Invalidate(true);
        }

        public void ToggleExpertDrawer()
        {
            IsExpertDrawerOpen = !IsExpertDrawerOpen;
            var workingArea = Screen.FromControl(this).WorkingArea;
            int targetOptionsWidth = IsExpertDrawerOpen ? 340 : 0;
            int targetClientWidth = GetTargetClientWidth(workingArea);

            _titleBarPanel?.SuspendLayout();
            SuspendLayout();
            optionsPanel.Width = targetOptionsWidth;
            ClientSize = new Size(targetClientWidth, ClientSize.Height);
            LayoutHeaderControls();
            ResumeLayout(true);
            _titleBarPanel?.ResumeLayout(true);

            if (Right > workingArea.Right)
            {
                Left = Math.Max(workingArea.Left, workingArea.Right - Width);
            }

            LayoutBottomDeckControls();
            AccelGUI?.ApplyOptions?.OptionSetX?.Options?.RefreshLayout();
            AccelGUI?.ApplyOptions?.OptionSetY?.Options?.RefreshLayout();
            AccelGUI?.ApplyOptions?.ReflowInspectorLayout();
            ClearAllComboSelections();
            WarmUpComboPopovers(optionsPanel);
            _expertDrawerButton?.Invalidate();
            optionsPanel.Invalidate(true);
            chartsPanel.Invalidate(true);
        }

        private int GetTargetClientWidth(Rectangle workingArea)
        {
            int targetWidth = IsExpertDrawerOpen ? 960 : 620;
            return Math.Min(workingArea.Width, targetWidth);
        }

        private int GetTargetClientHeight(Rectangle workingArea)
        {
            int targetHeight = IsVelocityAndGainOpen ? 740 : 556;
            return Math.Min(workingArea.Height - 24, targetHeight);
        }

        private void CurvePresetSwitcher_Paint(object sender, PaintEventArgs e)
        {
            var ctrl = (Control)sender;
            var g = e.Graphics;
            var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
            Color parentBack = ctrl.Parent?.BackColor ?? scheme.Surface;
            g.Clear(parentBack);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool isDark = scheme.Background.GetBrightness() < 0.5f;
            var outerRect = new RectangleF(0.5f, 0.5f, ctrl.Width - 1.5f, ctrl.Height - 1.5f);
            Color trackFill = scheme.Field;
            Color trackBorderCol = scheme.ControlBorder;

            using (var trackPath = Theme.CreateRoundedRectPath(outerRect, 8f))
            using (var trackBrush = new SolidBrush(trackFill))
            using (var trackBorder = new Pen(trackBorderCol, 1f))
            {
                g.FillPath(trackBrush, trackPath);
                g.DrawPath(trackBorder, trackPath);
            }

            var leftRect = new RectangleF(3f, 3f, (ctrl.Width / 2f) - 4f, ctrl.Height - 6f);
            var rightRect = new RectangleF((ctrl.Width / 2f) + 1f, 3f, (ctrl.Width / 2f) - 4f, ctrl.Height - 6f);

            var mousePos = ctrl.PointToClient(Cursor.Position);
            int activeHoverSeg = ctrl.ClientRectangle.Contains(mousePos)
                ? (mousePos.X < ctrl.Width / 2 ? 0 : 1)
                : _hoveredPresetSegment;

            bool origActive = CurvePresetManager.ActivePreset == CurvePresetManager.PresetOriginal;
            DrawPresetSegment(g, leftRect, "Original Curve", origActive, activeHoverSeg == 0, isDark, scheme, trackFill);
            DrawPresetSegment(g, rightRect, "Clean Curve", !origActive, activeHoverSeg == 1, isDark, scheme, trackFill);
        }

        private static void DrawPresetSegment(
            Graphics g,
            RectangleF rect,
            string title,
            bool active,
            bool hovered,
            bool isDark,
            ColorScheme scheme,
            Color trackFill)
        {
            if (active)
            {
                Color pillFill = scheme.Control.ToArgb() == trackFill.ToArgb()
                    ? (isDark
                        ? ColorScheme.Blend(scheme.OnBackground, trackFill, 0.14f)
                        : scheme.Surface)
                    : scheme.Control;
                using (var segPath = Theme.CreateRoundedRectPath(rect, 5f))
                using (var fill = new SolidBrush(pillFill))
                using (var border = new Pen(Color.FromArgb(isDark ? 70 : 95, scheme.ControlBorder), 1f))
                {
                    g.FillPath(fill, segPath);
                    g.DrawPath(border, segPath);
                }
            }

            // Highlight-only text hover for inactive segment: zero hover background box or border.
            Color mutedText = ColorScheme.Blend(scheme.OnField, trackFill, 0.32f);
            Color textColor = (active || hovered) ? scheme.OnBackground : mutedText;
            using (var brush = new SolidBrush(textColor))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(title, Theme.UiRegularFont, brush, rect, sf);
            }
        }

        private void ExpertDrawerButton_Paint(object sender, PaintEventArgs e)
        {
            var btn = (Control)sender;
            var g = e.Graphics;
            var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
            Color parentBack = btn.Parent?.BackColor ?? scheme.Surface;
            g.Clear(parentBack);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool isDark = scheme.Background.GetBrightness() < 0.5f;
            var mousePos = btn.PointToClient(Cursor.Position);
            bool hovered = btn.ClientRectangle.Contains(mousePos);

            // Highlight-only hover when closed: never draw a hover box or dot; draw pill only when drawer is open.
            if (IsExpertDrawerOpen)
            {
                var pillRect = new RectangleF(0.5f, 0.5f, btn.Width - 1.5f, btn.Height - 1.5f);
                Color pillFill = Color.FromArgb(isDark ? 36 : 24, scheme.Primary);
                Color pillBorder = Color.FromArgb(isDark ? 95 : 85, scheme.Primary);

                using (var path = Theme.CreateRoundedRectPath(pillRect, 5f))
                using (var fillBrush = new SolidBrush(pillFill))
                using (var borderPen = new Pen(pillBorder, 1f))
                {
                    g.FillPath(fillBrush, path);
                    g.DrawPath(borderPen, path);
                }
            }

            // Static coordinates and constant font across all states prevent 1px text/icon jump on click.
            Color idleContent = ColorScheme.Blend(scheme.OnBackground, parentBack, 0.36f);
            Color contentCol = IsExpertDrawerOpen
                ? scheme.OnBackground
                : (hovered ? scheme.OnBackground : idleContent);

            const float sx = 15.0f;
            const float cy = 14.0f;
            const float sy1 = cy - 2.8f;
            const float sy2 = cy + 2.8f;
            Color knobColor = IsExpertDrawerOpen ? scheme.Primary : contentCol;
            using (var iconPen = new Pen(contentCol, 1.35f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var knobBrush = new SolidBrush(knobColor))
            {
                g.DrawLine(iconPen, sx, sy1, sx + 11f, sy1);
                g.FillEllipse(knobBrush, sx + 2.0f, sy1 - 1.8f, 3.6f, 3.6f);
                g.DrawLine(iconPen, sx, sy2, sx + 11f, sy2);
                g.FillEllipse(knobBrush, sx + 6.4f, sy2 - 1.8f, 3.6f, 3.6f);
            }

            var textRect = new RectangleF(32.0f, 0f, Math.Max(1f, btn.Width - 36.0f), btn.Height);
            using (var textBrush = new SolidBrush(contentCol))
            using (var sf = new StringFormat(StringFormatFlags.NoWrap)
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                HotkeyPrefix = HotkeyPrefix.None
            })
            {
                g.DrawString("Settings", Theme.UiRegularFont, textBrush, textRect, sf);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (!(AccelGUI is null))
            {
                if (m.Msg == 0x00ff) // WM_INPUT
                {
                    AccelGUI.MouseWatcher.ReadMouseMove(m);
                }
                else if (m.Msg == 0x00fe) // WM_INPUT_DEVICE_CHANGE
                {
                    AccelGUI.Settings.OnDeviceChangeMessage();
                }
            }

            base.WndProc(ref m);
        }

        public void ResetAutoScroll()
        {
            chartsPanel.AutoScrollPosition = Constants.Origin;
        }

        public void ResizeAndCenter()
        {
            ResetAutoScroll();

            var workingArea = Screen.FromControl(this).WorkingArea;

            optionsPanel.Width = IsExpertDrawerOpen ? 340 : 0;
            ClientSize = new Size
            {
                Width = GetTargetClientWidth(workingArea),
                Height = GetTargetClientHeight(workingArea)
            };

            Location = new Point
            {
                X = workingArea.X + (workingArea.Width - Size.Width) / 2,
                Y = workingArea.Y + (workingArea.Height - Size.Height) / 2
            };

            LayoutBottomDeckControls();
        }

        #endregion Method

        static void MakeStartupShortcut(bool gui)
        {
            var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            if (string.IsNullOrEmpty(startupFolder))
            {
                throw new Exception("Startup folder does not exist");
            }

            //Windows Script Host Shell Object
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));
            dynamic shell = Activator.CreateInstance(t);

            try
            {
                // Delete any other RA / Inxane Accel related startup shortcuts
                var candidates = new[] { "rawaccel", "raw accel", "inxaneaccel", "inxane accel", "writer" };

                foreach (string path in Directory.EnumerateFiles(startupFolder, "*.lnk")
                    .Where(f => candidates.Any(f.Substring(startupFolder.Length).ToLower().Contains)))
                {
                    var link = shell.CreateShortcut(path);
                    try
                    {
                        string targetPath = link.TargetPath;

                        if (!(targetPath is null) &&
                            (targetPath.EndsWith("InxaneAccel.exe", StringComparison.OrdinalIgnoreCase) ||
                             targetPath.EndsWith("rawaccel.exe", StringComparison.OrdinalIgnoreCase) ||
                             (targetPath.EndsWith("writer.exe", StringComparison.OrdinalIgnoreCase) &&
                              new FileInfo(targetPath).Directory.GetFiles("*.exe")
                                  .Any(exe => exe.Name.Equals("InxaneAccel.exe", StringComparison.OrdinalIgnoreCase) ||
                                              exe.Name.Equals("rawaccel.exe", StringComparison.OrdinalIgnoreCase)))))
                        {
                            File.Delete(path);
                        }
                    }
                    finally
                    {
                        Marshal.FinalReleaseComObject(link);
                    }
                }

                var name = gui ? "InxaneAccel" : "writer";

                var lnk = shell.CreateShortcut($@"{startupFolder}\{name}.lnk");

                try
                {
                    if (!gui) lnk.Arguments = Constants.DefaultSettingsFileName;
                    lnk.TargetPath = $@"{Application.StartupPath}\{name}.exe";
                    lnk.Save();
                }
                finally
                {
                    Marshal.FinalReleaseComObject(lnk);
                }

            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }

        private void RawAcceleration_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                Properties.Settings.Default.Size = Size;
                Properties.Settings.Default.Location = Location;
                Properties.Settings.Default.Save();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            Application.Exit();
        }

        private void RawAcceleration_Shown(object sender, EventArgs e)
        {
            ResizeAndCenter();
            sensitivityBoxX.SelectionLength = 0;
            ClearAllComboSelections();
            WarmUpComboPopovers(optionsPanel);
            ActiveControl = _bottomDeckPanel;
            if (!Properties.Settings.Default.HasRunBefore)
            {
                Properties.Settings.Default.HasRunBefore = true;
                Properties.Settings.Default.Save();
            }
        }

        private static void WarmUpComboPopovers(Control container)
        {
            if (container == null) return;
            foreach (Control ctrl in container.Controls)
            {
                if (ctrl is grapher.Models.Theming.Controls.ThemeableComboBox cb)
                {
                    if (!cb.IsHandleCreated)
                    {
                        IntPtr unused = cb.Handle;
                    }
                    cb.WarmUpPopover();
                }
                else if (ctrl.HasChildren)
                {
                    WarmUpComboPopovers(ctrl);
                }
            }
        }
    }
}
