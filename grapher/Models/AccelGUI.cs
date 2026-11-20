using grapher.Common;
using grapher.Models.Calculations;
using grapher.Models.Devices;
using grapher.Models.Mouse;
using grapher.Models.Options;
using grapher.Models.Serialized;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace grapher
{
    public class AccelGUI
    {

        #region Constructors

        public AccelGUI(
            RawAcceleration accelForm,
            AccelCalculator accelCalculator,
            AccelCharts accelCharts,
            SettingsManager settings,
            ApplyOptions applyOptions,
            Button writeButton,
            Button resetButton,
            MouseWatcher mouseWatcher,
            ToolStripMenuItem scaleMenuItem,
            ToolStripMenuItem deviceMenuItem)
        {
            AccelForm = accelForm;
            AccelCalculator = accelCalculator;
            AccelCharts = accelCharts;
            ApplyOptions = applyOptions;
            WriteButton = writeButton;
            ResetButton = resetButton;
            ScaleMenuItem = scaleMenuItem;
            DeviceMenuItem = deviceMenuItem;
            Settings = settings;
            DefaultButtonFont = WriteButton.Font;
            SmallButtonFont = new Font(WriteButton.Font.Name, WriteButton.Font.Size * Constants.SmallButtonSizeFactor);
            MouseWatcher = mouseWatcher;

            DeviceMenuItem.Click += DeviceMenuItemClick;
            ScaleMenuItem.Click += new System.EventHandler(OnScaleMenuItemClick);
            WriteButton.Click += new System.EventHandler(OnWriteButtonClick);
            ResetButton.Click += new System.EventHandler(ResetDriverEventHandler);
            AccelForm.FormClosing += new FormClosingEventHandler(SaveGUISettingsOnClose);

            ButtonTimerInterval = Convert.ToInt32(DriverConfig.WriteDelayMs);
            ButtonTimer = new Timer();
            ButtonTimer.Tick += new System.EventHandler(OnButtonTimerTick);

            ChartRefresh = SetupChartTimer();

            RefreshUser();
            RefreshActive();
            SetupButtons();

            ApplyOptions.Directionality.Show();
        }

        #endregion Constructors

        #region Properties

        public RawAcceleration AccelForm { get; }

        public AccelCalculator AccelCalculator { get; }

        public AccelCharts AccelCharts { get; }

        public SettingsManager Settings { get; }

        public ApplyOptions ApplyOptions { get; }

        public Button WriteButton { get; }

        public Button ResetButton { get; }

        public Timer ButtonTimer { get; }

        public MouseWatcher MouseWatcher { get; }

        public ToolStripMenuItem ScaleMenuItem { get; }

        public ToolStripMenuItem DeviceMenuItem { get; }

        private Timer ChartRefresh { get; }

        private Font SmallButtonFont { get; }

        private Font DefaultButtonFont { get; }

        private bool SettingsNotDefault { get; set; }

        private bool LastToggleChecked { get; set; }

        private int ButtonTimerInterval { get; }

        #endregion Properties

        #region Methods

        private void SaveGUISettingsOnClose(Object sender, FormClosingEventArgs e)
        {
            var guiSettings = Settings.MakeGUISettingsFromFields();
            if (!Settings.GuiSettings.Equals(guiSettings))
            {
                guiSettings.Save();
            }
        }

        public Profile MakeSettingsFromFields()
        {
            var settings = new Profile();

            settings.rotation = ApplyOptions.Rotation.Field.Data;
            settings.outputDPI = Helper.CalculatOutputDPI(ApplyOptions.Sensitivity.Field.Data);

            settings.yxOutputDPIRatio = ApplyOptions.YToXRatio.Value;
            settings.inputSpeedArgs.combineMagnitudes = ApplyOptions.IsWhole;
            ApplyOptions.SetArgsFromActiveValues(ref settings.argsX, ref settings.argsY);

            var (domWeights, lpNorm) = ApplyOptions.Directionality.GetDomainArgs();
            settings.domainXY = domWeights;
            settings.inputSpeedArgs.lpNorm = lpNorm;

            settings.rangeXY = ApplyOptions.Directionality.GetRangeXY();

            Settings.SetHiddenOptions(settings);

            return settings;
        }

        public void UpdateActiveSettingsFromFields()
        {
            string error_message;

            try
            {
                ButtonDelay(WriteButton);

                if (!Settings.TryActivate(MakeSettingsFromFields(), out string errors))
                {
                    error_message = errors.ToString();
                }
                else
                {
                    if (Settings.ActiveProfile.argsX.mode == AccelMode.lut &&
                        !string.IsNullOrWhiteSpace(ApplyOptions?.OptionSetX?.Options?.LutPanel?.PointsTextBox?.Text))
                    {
                        try
                        {
                            string baseDir = System.IO.Path.GetDirectoryName(typeof(CurvePresetManager).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
                        string lutPointsPath = System.IO.Path.Combine(baseDir, CurvePresetManager.LutPointsFileName);
                            System.IO.File.WriteAllText(
                                lutPointsPath,
                                ApplyOptions.OptionSetX.Options.LutPanel.PointsTextBox.Text.Trim(),
                                new System.Text.UTF8Encoding(false));
                        }
                        catch
                        {
                            // Ignore disk write failure if directory is read-only.
                        }
                    }

                    RefreshActive();
                    Settings.SetActiveHandles();
                    return;
                }
            }
            catch (ApplicationException e)
            {
                error_message = e.Message;
            }

            using (var form = new MessageDialog(error_message, "bad input"))
            {
                grapher.Models.Theming.Theme.Apply(form);
                form.ShowDialog();
            }
        }

        public void RefreshActive()
        {
            UpdateShownActiveValues(Settings.ActiveProfile);
            UpdateGraph();
        }

        public void RefreshUser()
        {
            UpdateShownActiveValues(Settings.UserProfile);
        }

        public void UpdateGraph()
        {
            AccelCharts.Calculate(Settings.ActiveAccelForGraphing, Settings.ActiveProfile);
            AccelCharts.Bind();
        }

        public void UpdateShownActiveValues(Profile args)
        {
            AccelForm.ResetAutoScroll();
            AccelCharts.ShowActive(args);
            ApplyOptions.SetActiveValues(args);
        }

        private Timer SetupChartTimer()
        {
            Timer chartTimer = new Timer();
            chartTimer.Enabled = true;
            chartTimer.Interval = 10;
            chartTimer.Tick += new System.EventHandler(OnChartTimerTick);
            return chartTimer;
        }

        public void SelectCurvePreset(
            string presetName,
            RichTextBox xLutPointsBox,
            RichTextBox yLutPointsBox,
            ComboBox accelDropX,
            ComboBox accelDropY)
        {
            string targetLut = string.Equals(presetName, CurvePresetManager.PresetCleanBump, StringComparison.OrdinalIgnoreCase)
                ? CurvePresetManager.CleanBumpLutText
                : CurvePresetManager.OriginalLutText;

            if (string.IsNullOrWhiteSpace(targetLut))
            {
                return;
            }

            CurvePresetManager.SetActivePreset(presetName);

            if (accelDropX != null && !(accelDropX.SelectedItem is Layouts.LUTLayout))
            {
                accelDropX.SelectedItem = AccelTypeOptions.LUT;
            }

            if (xLutPointsBox != null)
            {
                xLutPointsBox.Text = targetLut;
            }

            if (!ApplyOptions.IsWhole && !ApplyOptions.ByComponentVectorXYLock.Checked && yLutPointsBox != null)
            {
                if (accelDropY != null && !(accelDropY.SelectedItem is Layouts.LUTLayout))
                {
                    accelDropY.SelectedItem = AccelTypeOptions.LUT;
                }
                yLutPointsBox.Text = targetLut;
            }

            // Preview curve purely in-memory without mutating Apply as, saved files, active readouts, or the driver
            // until Apply is explicitly pressed.
            try
            {
                var previewProfile = MakeSettingsFromFields();
                previewProfile.inputSpeedArgs.inputSmoothHalflife = 0;
                previewProfile.inputSpeedArgs.scaleSmoothHalflife = 0;
                previewProfile.inputSpeedArgs.outputSmoothHalflife = 0;
                using (var previewAccel = new ManagedAccel(previewProfile))
                {
                    AccelCharts.Calculate(previewAccel, previewProfile);
                    AccelCharts.Bind();
                }
            }
            catch
            {
                // Ignore transient parse states.
            }
        }

        private void SetupButtons()
        {
            int pw = WriteButton.Parent != null && WriteButton.Parent.ClientSize.Width > 320
                ? WriteButton.Parent.ClientSize.Width
                : 620;

            const int marginX = 16;
            const int ctrlY = 10;
            const int ctrlH = 32;
            const int btnGap = 8;
            const int applyW = 104;
            const int resetW = 88;

            int applyX = pw - marginX - applyW;
            int resetX = applyX - btnGap - resetW;

            WriteButton.Left = applyX;
            WriteButton.Top = ctrlY;
            WriteButton.Width = applyW;
            WriteButton.Height = ctrlH;

            ResetButton.TextAlign = ContentAlignment.MiddleCenter;
            ResetButton.Left = resetX;
            ResetButton.Top = ctrlY;
            ResetButton.Width = resetW;
            ResetButton.Height = ctrlH;

            SetButtonDefaults();
        }

        private void SetButtonDefaults()
        {
            ResetButton.Font = DefaultButtonFont;
            ResetButton.Text = Constants.ResetButtonText;
            ResetButton.Enabled = true;
            ResetButton.Update();

            WriteButton.Font = DefaultButtonFont;
            WriteButton.Text = Constants.WriteButtonDefaultText;
            WriteButton.Enabled = true;
            WriteButton.Update();
        }

        private void OnScaleMenuItemClick(object sender, EventArgs e)
        {
            AccelCalculator.DPI.NewInputToData();
            AccelCalculator.PollRate.NewInputToData();
            UpdateGraph();
        }

        private void OnWriteButtonClick(object sender, EventArgs e)
        {
            UpdateActiveSettingsFromFields();
        }

        private void ResetDriverEventHandler(object sender, EventArgs e)
        {
            ButtonDelay(ResetButton);
            Settings.ResetDriver();
            RefreshActive();
        }

        private void OnButtonTimerTick(object sender, EventArgs e)
        {
            ButtonTimer.Stop();
            SetButtonDefaults();
            DeviceMenuItem.Enabled = true;
        }

        private void StartButtonTimer()
        {
            DeviceMenuItem.Enabled = false;
            ButtonTimer.Interval = ButtonTimerInterval;
            ButtonTimer.Start();
        }

        private void ButtonDelay(ButtonBase btn)
        {
            ResetButton.Enabled = false;
            WriteButton.Enabled = false;

            btn.Font = SmallButtonFont;
            btn.Text = $"{Constants.ButtonDelayText} : {ButtonTimerInterval} ms";

            ResetButton.Update();
            WriteButton.Update();

            StartButtonTimer();
        }

        private void OnChartTimerTick(object sender, EventArgs e)
        {
            AccelCharts.DrawLastMovement();
            MouseWatcher.UpdateLastMove();
        }

        private void DeviceMenuItemClick(object sender, EventArgs e)
        {
            using (var devMenu = new DeviceMenuForm(Settings))
            {
                grapher.Models.Theming.Theme.Apply(devMenu);
                if (devMenu.ShowDialog() == DialogResult.OK)
                {
                    Settings.Submit(devMenu.defaultConfig, devMenu.Items);
                    UpdateActiveSettingsFromFields();
                }
            }
        }

        #endregion Methods
    }

}
