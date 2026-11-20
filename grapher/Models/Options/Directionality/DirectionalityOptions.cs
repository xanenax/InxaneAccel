using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using grapher.Common;
using grapher.Models.Theming;

namespace grapher.Models.Options.Directionality
{
    public class DirectionalityOptions
    {
        public DirectionalityOptions(
            Panel containingPanel,
            Button directionalityLabel,
            Label directionalityX,
            Label directionalityY,
            Label directionalityActiveValueTitle,
            Option lpNorm,
            OptionXY domain,
            OptionXY range,
            CheckBox wholeCheckBox,
            CheckBox byComponentCheckBox,
            int top)
        {
            ContainingPanel = containingPanel;

            DirectionalityLabel = directionalityLabel;
            DirectionalityLabel.FlatStyle = FlatStyle.Flat;
            DirectionalityLabel.FlatAppearance.BorderSize = 0;
            DirectionalityLabel.FlatAppearance.MouseDownBackColor = Color.Transparent;
            DirectionalityLabel.FlatAppearance.CheckedBackColor = Color.Transparent;
            DirectionalityLabel.FlatAppearance.MouseOverBackColor = Color.Transparent;

            DirectionalityX = directionalityX;
            DirectionalityY = directionalityY;
            DirectionalityActiveValueTitle = directionalityActiveValueTitle;
            LpNorm = lpNorm;
            Domain = domain;
            Range = range;
            WholeCheckBox = wholeCheckBox;
            ByComponentCheckBox = byComponentCheckBox;

            ContainingPanel.Paint += Panel_Paint;
            DirectionalityLabel.Click += Title_click;
            DirectionalityLabel.Paint += DirectionalityLabel_Paint;
            ContainingPanel.Left = 1;
            ContainingPanel.Top = top;
            ContainingPanel.Width = 319;
            DirectionalityLabel.AutoSize = false;
            DirectionalityLabel.Left = 15;
            DirectionalityLabel.Top = 2;
            DirectionalityLabel.Width = 302;
            DirectionalityLabel.Height = 22;
            DirectionalityLabel.TextAlign = ContentAlignment.MiddleLeft;
            DirectionalityX.BackColor = Color.Transparent;
            DirectionalityY.BackColor = Color.Transparent;
            DirectionalityActiveValueTitle.BackColor = Color.Transparent;
            LayoutDirectionalityControls();
            IsHidden = true;
            ToWhole();
            Show();
        }

        public Panel ContainingPanel { get; }

        public Button DirectionalityLabel { get; }

        public Label DirectionalityX { get; }

        public Label DirectionalityY { get; }

        public Label DirectionalityActiveValueTitle { get; }

        public Option LpNorm { get; }

        public OptionXY Domain { get; }

        public OptionXY Range { get; }

        public CheckBox WholeCheckBox { get; }

        public CheckBox ByComponentCheckBox { get; }

        public int OpenHeight { get => WholeCheckBox.Bottom - DirectionalityLabel.Top + 2 * Constants.DirectionalityTitlePad; }

        public int ClosedHeight { get => DirectionalityLabel.Height + 2 * Constants.DirectionalityTitlePad; }

        private bool IsHidden { get; set; }

        public Tuple<Vec2<double>, double> GetDomainArgs()
        {
            var weights = new Vec2<double>
            {
                x = Domain.Fields.X,
                y = Domain.Fields.Y
            };
            double p = ByComponentCheckBox.Checked ? 2 : LpNorm.Field.Data;

            return new Tuple<Vec2<double>, double>(weights, p);
        }

        public Vec2<double> GetRangeXY()
        {
            return new Vec2<double>
            {
                x = Range.Fields.X,
                y = Range.Fields.Y,
            };
        }

        public void SetActiveValues(Profile settings)
        {
            Domain.SetActiveValues(settings.domainXY.x, settings.domainXY.y);
            Range.SetActiveValues(settings.rangeXY.x, settings.rangeXY.y);

            if (settings.inputSpeedArgs.combineMagnitudes)
            {
                LpNorm.SetActiveValue(settings.inputSpeedArgs.lpNorm);
            }
            else
            {
                LpNorm.SetToUnavailable();
            }

            LayoutDirectionalityControls();
            if (!IsHidden)
            {
                Domain.ActiveValueLabels.Show();
                Range.ActiveValueLabels.Show();
            }
        }

        private void LayoutDirectionalityControls()
        {
            DirectionalityX.Hide();
            DirectionalityY.Hide();
            DirectionalityActiveValueTitle.Hide();

            // ContainingPanel sits at X=1..320; local 15, 119..231, 241..317 align strictly to inspector 16, 120..232, 242..318.
            DirectionalityActiveValueTitle.Left = 241;
            DirectionalityActiveValueTitle.Width = 76;

            ConfigureRowLabel(Domain.Label, 28);
            ConfigureBox(Domain.Fields.XField.Box, 119, 28, 53);
            ConfigureBox(Domain.Fields.YField.Box, 178, 28, 53);
            ConfigureBadgeLabel(Domain.ActiveValueLabels.X.ValueLabel, 241, 28, 37);
            ConfigureBadgeLabel(Domain.ActiveValueLabels.Y.ValueLabel, 280, 28, 37);
            Domain.ActiveValueLabels.X.Prefix = string.Empty;
            Domain.ActiveValueLabels.Y.Prefix = string.Empty;

            ConfigureRowLabel(Range.Label, 52);
            ConfigureBox(Range.Fields.XField.Box, 119, 52, 53);
            ConfigureBox(Range.Fields.YField.Box, 178, 52, 53);
            ConfigureBadgeLabel(Range.ActiveValueLabels.X.ValueLabel, 241, 52, 37);
            ConfigureBadgeLabel(Range.ActiveValueLabels.Y.ValueLabel, 280, 52, 37);
            Range.ActiveValueLabels.X.Prefix = string.Empty;
            Range.ActiveValueLabels.Y.Prefix = string.Empty;

            ConfigureRowLabel(LpNorm.Label, 76);
            ConfigureBox(LpNorm.Field.Box, 119, 76, 112);
            ConfigureBadgeLabel(LpNorm.ActiveValueLabel.ValueLabel, 241, 76, 76);

            WholeCheckBox.Left = 119;
            WholeCheckBox.Top = 100;
            WholeCheckBox.Height = 20;

            ByComponentCheckBox.Left = 196;
            ByComponentCheckBox.Top = 100;
            ByComponentCheckBox.Height = 20;
        }

        private static void ConfigureRowLabel(Label label, int top)
        {
            if (label == null) return;
            label.AutoSize = false;
            label.Left = 15;
            label.Top = top;
            label.Width = 96;
            label.Height = 20;
            label.TextAlign = ContentAlignment.MiddleRight;
        }

        private static void ConfigureBox(Control box, int left, int top, int width)
        {
            if (box == null) return;
            box.Left = left;
            box.Top = top;
            box.Width = width;
            box.Height = 20;
        }

        private static void ConfigureBadgeLabel(Label badge, int left, int top, int width)
        {
            if (badge == null) return;
            badge.AutoSize = false;
            badge.Left = left;
            badge.Top = top;
            badge.Width = width;
            badge.Height = 20;
        }

        public void Hide()
        {
            if (!IsHidden)
            {
                IsHidden = true;
                DirectionalityX.Hide();
                DirectionalityY.Hide();
                DirectionalityActiveValueTitle.Hide();
                LpNorm.Hide();
                Domain.Hide();
                Range.Hide();
                WholeCheckBox.Hide();
                ByComponentCheckBox.Hide();
                DirectionalityLabel.Text = Constants.DirectionalityTitleClosed;
                DrawHidden();
            }
        }

        public void Show()
        {
            if (IsHidden)
            {
                IsHidden = false;
                LpNorm.Show();
                Domain.Show();
                Range.Show();
                Range.Fields.LockCheckBox.Hide();
                WholeCheckBox.Show();
                ByComponentCheckBox.Show();
                LayoutDirectionalityControls();
                Domain.ActiveValueLabels.Show();
                Range.ActiveValueLabels.Show();
                DirectionalityLabel.Text = Constants.DirectionalityTitleOpen;
                DrawShown();
            }
        }

        public void ToByComponent()
        {
            LpNorm.SetToUnavailable();
        }

        public void ToWhole()
        {
            LpNorm.SetToAvailable();
        }

        private void DrawHidden()
        {
            ContainingPanel.Height = ClosedHeight;
            SyncActionButtonsAndWindow();
            ContainingPanel.Invalidate();
            DirectionalityLabel.Invalidate();
        }

        private void DrawShown()
        {
            ContainingPanel.Height = OpenHeight;
            SyncActionButtonsAndWindow();
            ContainingPanel.Invalidate();
            DirectionalityLabel.Invalidate();
        }

        private void SyncActionButtonsAndWindow()
        {
            ContainingPanel.Parent?.Invalidate();
        }

        private void DirectionalityLabel_Paint(object sender, PaintEventArgs e)
        {
            var btn = DirectionalityLabel;
            var g = e.Graphics;
            var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
            Color bg = btn.Parent != null && btn.Parent.BackColor != Color.Transparent
                ? btn.Parent.BackColor
                : scheme.Surface;
            g.Clear(bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var textRect = new RectangleF(0f, 0f, 92f, btn.Height);
            using (var textBrush = new SolidBrush(scheme.OnBackground))
            using (var sf = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(btn.Text, btn.Font, textBrush, textRect, sf);
            }

            if (!IsHidden)
            {
                float offset = btn.Left;
                // Center X over DomainBoxX (119..172), Y over DomainBoxY (178..231), Current over badges (241..323)
                var xRect = new RectangleF(119f - offset, 0f, 53f, btn.Height);
                var yRect = new RectangleF(178f - offset, 0f, 53f, btn.Height);
                var curRect = new RectangleF(241f - offset, 0f, 76f, btn.Height);

                using (var headerBrush = new SolidBrush(scheme.OnBackground))
                using (var centerSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString("X", btn.Font, headerBrush, xRect, centerSf);
                    g.DrawString("Y", btn.Font, headerBrush, yRect, centerSf);
                    g.DrawString("Current", btn.Font, headerBrush, curRect, centerSf);
                }
            }

            float cx = btn.Width - 5f;
            float cy = btn.Height / 2f;
            PointF[] chevron = IsHidden
                ? new[] { new PointF(cx - 4f, cy - 1.5f), new PointF(cx, cy + 2.5f), new PointF(cx + 4f, cy - 1.5f) }
                : new[] { new PointF(cx - 4f, cy + 2.0f), new PointF(cx, cy - 2.0f), new PointF(cx + 4f, cy + 2.0f) };

            Color chevCol = ColorScheme.Blend(scheme.OnBackground, bg, 0.35f);
            using (var pen = new Pen(chevCol, 1.6f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, chevron);
            }
        }

        private void Panel_Paint(object sender, PaintEventArgs e)
        {
            var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
            e.Graphics.Clear(Theme.ResolveContainerBackColor(ContainingPanel));
        }

        private void Title_click(object sender, EventArgs e)
        {
            if (IsHidden)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
    }
}
