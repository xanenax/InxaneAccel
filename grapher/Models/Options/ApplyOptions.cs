using grapher.Common;
using grapher.Models.Options.Directionality;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace grapher.Models.Options
{
    public class ApplyOptions
    {
        #region Constructors

        public ApplyOptions(
            CheckBox byComponentVectorXYLock,
            AccelOptionSet optionSetX,
            AccelOptionSet optionSetY,
            DirectionalityOptions directionalityOptions,
            Option sensitivity,
            LockableOption yxRatio,
            Option rotation,
            Label lockXYLabel,
            AccelCharts accelCharts)
        {
            Directionality = directionalityOptions;
            WholeVectorCheckBox = Directionality.WholeCheckBox;
            ByComponentVectorCheckBox = Directionality.ByComponentCheckBox;

            WholeVectorCheckBox.Click += new System.EventHandler(OnWholeClicked);
            ByComponentVectorCheckBox.Click += new System.EventHandler(OnByComponentClicked);

            WholeVectorCheckBox.CheckedChanged += new System.EventHandler(OnWholeCheckedChange);
            ByComponentVectorCheckBox.CheckedChanged += new System.EventHandler(OnByComponentCheckedChange);

            ByComponentVectorXYLock = byComponentVectorXYLock;
            OptionSetX = optionSetX;
            OptionSetY = optionSetY;
            Sensitivity = sensitivity;
            YToXRatio = yxRatio;
            Rotation = rotation;
            LockXYLabel = lockXYLabel;
            AccelCharts = accelCharts;

            LockXYLabel.AutoSize = false;
            LockXYLabel.Left = 140;
            LockXYLabel.Top = 112;
            LockXYLabel.Width = 90;
            LockXYLabel.Height = 20;
            LockXYLabel.TextAlign = ContentAlignment.MiddleLeft;

            if (OptionSetX?.ActiveValuesTitle != null)
            {
                OptionSetX.ActiveValuesTitle.Left = 242;
                OptionSetX.ActiveValuesTitle.Top = 12;
                OptionSetX.ActiveValuesTitle.Width = 76;
                OptionSetX.ActiveValuesTitle.Height = 18;
            }
            if (OptionSetY?.ActiveValuesTitle != null)
            {
                OptionSetY.ActiveValuesTitle.Left = 242;
                OptionSetY.ActiveValuesTitle.Width = 76;
                OptionSetY.ActiveValuesTitle.Height = 18;
            }

            ByComponentVectorXYLock.Text = "Lock X & Y";
            ByComponentVectorXYLock.AutoSize = false;
            ByComponentVectorXYLock.Location = new Point(144, 147);
            ByComponentVectorXYLock.Size = new Size(92, 20);
            ByComponentVectorXYLock.CheckedChanged += new System.EventHandler(OnByComponentXYLockChecked);
            ByComponentVectorXYLock.Checked = true;

            if (OptionSetX?.Options != null)
            {
                OptionSetX.Options.LayoutChangedCallback = ReflowInspectorLayout;
                if (OptionSetX.Options.ClassicCap?.CapTypeOptions?.OptionsDropdown != null)
                    OptionSetX.Options.ClassicCap.CapTypeOptions.OptionsDropdown.SelectedIndexChanged += (s, ev) => ReflowInspectorLayout();
                if (OptionSetX.Options.PowerCap?.CapTypeOptions?.OptionsDropdown != null)
                    OptionSetX.Options.PowerCap.CapTypeOptions.OptionsDropdown.SelectedIndexChanged += (s, ev) => ReflowInspectorLayout();
            }
            if (OptionSetY?.Options != null)
            {
                OptionSetY.Options.LayoutChangedCallback = ReflowInspectorLayout;
                OptionSetY.Options.HandleLUTOptionsOnResize();
                if (OptionSetY.Options.ClassicCap?.CapTypeOptions?.OptionsDropdown != null)
                    OptionSetY.Options.ClassicCap.CapTypeOptions.OptionsDropdown.SelectedIndexChanged += (s, ev) => ReflowInspectorLayout();
                if (OptionSetY.Options.PowerCap?.CapTypeOptions?.OptionsDropdown != null)
                    OptionSetY.Options.PowerCap.CapTypeOptions.OptionsDropdown.SelectedIndexChanged += (s, ev) => ReflowInspectorLayout();
            }

            YToXRatio.SnapTo(Sensitivity);
            Rotation.SnapTo(YToXRatio);

            Sensitivity.Top = 36;
            YToXRatio.Top = 62;
            Rotation.Top = 88;
            YToXRatio.LockBox.AutoSize = false;
            YToXRatio.LockBox.Size = new Size(18, 20);
            YToXRatio.LockBox.Left = 120;
            YToXRatio.LockBox.Top = 112;

            foreach (var opt in new[] { Sensitivity, YToXRatio.Option, Rotation })
            {
                opt.Label.Left = 16;
                opt.Label.Width = 96;
                opt.Field.Box.Left = 120;
                opt.Field.Box.Width = 112;
                opt.ActiveValueLabel.Left = 242;
                opt.ActiveValueLabel.Width = 76;
            }

            EnableWholeApplication();
        }

        #endregion Constructors

        #region Properties

        public CheckBox WholeVectorCheckBox { get; }

        public CheckBox ByComponentVectorCheckBox { get; }

        public CheckBox ByComponentVectorXYLock { get; }

        public AccelOptionSet OptionSetX { get; }

        public AccelOptionSet OptionSetY { get; }

        public DirectionalityOptions Directionality { get; }

        public Option Sensitivity { get; }

        public LockableOption YToXRatio { get; }

        public Option Rotation { get; }

        public AccelCharts AccelCharts { get; }

        public Label ActiveValueTitleY { get; }

        public Label LockXYLabel { get; }

        public bool IsWhole { get; private set; }

        private bool _isReflowing;

        public void ReflowInspectorLayout()
        {
            if (_isReflowing || OptionSetX?.Options == null || Directionality?.ContainingPanel == null)
            {
                return;
            }

            _isReflowing = true;
            try
            {
                ScrollableControl panel = Directionality.ContainingPanel.Parent as ScrollableControl;

                for (int pass = 0; pass < 3; pass++)
                {
                    int scrollOffset = panel?.AutoScrollPosition.Y ?? 0;

                    if (IsWhole)
                    {
                        OptionSetX.OptionsTitle.Left = 16;
                        OptionSetX.Options.Top = 172 + scrollOffset;
                        OptionSetX.Options.HandleLUTOptionsOnResize();
                        int xBottom = OptionSetX.Options.ComputeVisibleBottom();
                        Directionality.ContainingPanel.Top = Math.Max(370 + scrollOffset, xBottom + 12);
                    }
                    else if (ByComponentVectorXYLock.Checked)
                    {
                        OptionSetX.OptionsTitle.Left = 16;
                        OptionSetX.OptionsTitle.Top = 172 + scrollOffset;
                        OptionSetX.Options.Top = 192 + scrollOffset;
                        OptionSetX.Options.HandleLUTOptionsOnResize();
                        int xBottom = OptionSetX.Options.ComputeVisibleBottom();
                        Directionality.ContainingPanel.Top = Math.Max(370 + scrollOffset, xBottom + 12);
                    }
                    else
                    {
                        OptionSetX.OptionsTitle.Left = 16;
                        OptionSetX.OptionsTitle.Top = 172 + scrollOffset;
                        OptionSetX.Options.Top = 192 + scrollOffset;
                        OptionSetX.Options.HandleLUTOptionsOnResize();
                        int xBottom = OptionSetX.Options.ComputeVisibleBottom();

                        if (OptionSetY != null)
                        {
                            OptionSetY.OptionsTitle.Left = 16;
                            OptionSetY.OptionsTitle.Top = xBottom + 12;
                            OptionSetY.ActiveValuesTitle.Left = 242;
                            OptionSetY.ActiveValuesTitle.Width = 76;
                            OptionSetY.ActiveValuesTitle.Height = 18;
                            OptionSetY.ActiveValuesTitle.Top = xBottom + 12;
                            OptionSetY.Options.Top = OptionSetY.OptionsTitle.Bottom + 4;
                            OptionSetY.Options.HandleLUTOptionsOnResize();
                            int yBottom = OptionSetY.Options.ComputeVisibleBottom();
                            Directionality.ContainingPanel.Top = Math.Max(370 + scrollOffset, Math.Max(xBottom, yBottom) + 14);
                        }
                        else
                        {
                            Directionality.ContainingPanel.Top = Math.Max(370 + scrollOffset, xBottom + 14);
                        }
                    }

                    ByComponentVectorXYLock.Location = new Point(144, 147 + scrollOffset);

                    if (panel != null)
                    {
                        int currentScrollOffset = panel.AutoScrollPosition.Y;
                        int logicalBottom = (Directionality.ContainingPanel.Top - currentScrollOffset) + Directionality.ContainingPanel.Height + 4;
                        bool needsScroll = logicalBottom > panel.Height && panel.Height > 120;
                        if ((!needsScroll && (panel.AutoScroll || panel.AutoScrollPosition != Point.Empty)) || currentScrollOffset != scrollOffset)
                        {
                            if (!needsScroll)
                            {
                                panel.AutoScrollPosition = Point.Empty;
                                panel.AutoScroll = false;
                            }
                            continue;
                        }

                        if (panel.AutoScroll != needsScroll)
                        {
                            panel.AutoScroll = needsScroll;
                        }

                        grapher.Models.Theming.Theme.ApplyScrollBarTheme(panel);
                    }

                    break;
                }

                Directionality.ContainingPanel.Parent?.Invalidate();
            }
            finally
            {
                _isReflowing = false;
            }
        }

        #endregion Properties

        #region Methods

        public void SetArgsFromActiveValues(ref AccelArgs argsX, ref AccelArgs argsY)
        {
            OptionSetX.SetArgsFromActiveValues(ref argsX);

            if (ByComponentVectorXYLock.Checked)
            {
                OptionSetX.SetArgsFromActiveValues(ref argsY);
            }
            else
            {
                OptionSetY.SetArgsFromActiveValues(ref argsY);
            }
        }

        public void SetActiveValues(Profile settings)
        {
            Sensitivity.SetActiveValue(Helper.GetSensitivityFactor(settings));
            YToXRatio.SetActiveValue(settings.yxOutputDPIRatio);
            Rotation.SetActiveValue(settings.rotation);
            
            WholeVectorCheckBox.Checked = settings.inputSpeedArgs.combineMagnitudes;
            ByComponentVectorCheckBox.Checked = !settings.inputSpeedArgs.combineMagnitudes;
            ByComponentVectorXYLock.Checked = settings.argsX.IsEquivalentTo(settings.argsY);
            OptionSetX.SetActiveValues(ref settings.argsX);
            OptionSetY.SetActiveValues(ref settings.argsY);

            Directionality.SetActiveValues(settings);

            AccelCharts.SetLogarithmic(
                OptionSetX.Options.AccelerationType.LogarithmicCharts,
                OptionSetY.Options.AccelerationType.LogarithmicCharts);
        }

        public void OnWholeClicked(object sender, EventArgs e)
        {
            if (!WholeVectorCheckBox.Checked)
            {
                WholeVectorCheckBox.Checked = true;
                ByComponentVectorCheckBox.Checked = false;
                Directionality.ToWhole();
            }
        }

        public void OnByComponentClicked(object sender, EventArgs e)
        {
            if (!ByComponentVectorCheckBox.Checked)
            {
                WholeVectorCheckBox.Checked = false;
                ByComponentVectorCheckBox.Checked = true;
                Directionality.ToByComponent();
            }
        }

        public void OnWholeCheckedChange(object sender, EventArgs e)
        {
            if (WholeVectorCheckBox.Checked)
            {
                EnableWholeApplication();
            }
        }

        public void OnByComponentCheckedChange(object sender, EventArgs e)
        {
            if (ByComponentVectorCheckBox.Checked)
            {
                EnableByComponentApplication();
            }
        }

        public void ShowWholeSet()
        {
            OptionSetX.SetRegularMode();
            OptionSetY.Hide();
            ReflowInspectorLayout();
        }

        public void ShowByComponentAsOneSet()
        {
            OptionSetX.SetTitleMode("X = Y");
            OptionSetY.Hide();
            ReflowInspectorLayout();
        }

        public void ShowByComponentAsTwoSets()
        {
            OptionSetX.SetTitleMode("X");
            OptionSetY.SetTitleMode("Y");
            OptionSetY.Show();
            ReflowInspectorLayout();
        }

        public void ShowByComponentSet()
        {
            if (ByComponentVectorXYLock.Checked)
            {
                ShowByComponentAsOneSet();
            }
            else
            {
                ShowByComponentAsTwoSets();
            }
        }

        private void OnByComponentXYLockChecked(object sender, EventArgs e)
        {
            if (!IsWhole)
            {
                ShowByComponentSet();
            }
        }

        public void EnableWholeApplication()
        {
            IsWhole = true;
            ByComponentVectorXYLock.Hide();
            ShowWholeSet();
        }

        public void EnableByComponentApplication()
        {
            IsWhole = false;
            ByComponentVectorXYLock.Show();
            ShowByComponentSet();
        }

        private void SetActiveTitlesWhole()
        {
            OptionSetX.ActiveValuesTitle.Left = OptionSetX.Options.Left + OptionSetX.Options.Width;
            LockXYLabel.Width = (AccelCharts.Left - OptionSetX.ActiveValuesTitle.Left) / 2;
            OptionSetX.ActiveValuesTitle.Width = LockXYLabel.Width;
            LockXYLabel.Left = OptionSetX.ActiveValuesTitle.Left + OptionSetX.ActiveValuesTitle.Width;
            YToXRatio.LockBox.Left = LockXYLabel.Left + LockXYLabel.Width / 2 - YToXRatio.LockBox.Width / 2;
            ByComponentVectorXYLock.Left = YToXRatio.LockBox.Left;
            AlignActiveValues();
        }

        private void SetActiveTitlesByComponents()
        {
            OptionSetY.ActiveValuesTitle.Left = 242;
            OptionSetY.ActiveValuesTitle.Width = 76;
            AlignActiveValues();
        }

        private void AlignActiveValues()
        {
            OptionSetX.AlignActiveValues();
            OptionSetY.AlignActiveValues();
            Sensitivity.AlignActiveValues();
            Rotation.AlignActiveValues();
        }

        #endregion Methods
    }
}
