using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace grapher.Models.Theming
{
    public class StyledMenuRenderer : ToolStripProfessionalRenderer
    {
        public StyledMenuRenderer() : base(new StyledMenuColors())
        {
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (Theme.CurrentScheme == null)
            {
                base.OnRenderToolStripBackground(e);
                return;
            }

            Color fillCol = e.ToolStrip is MenuStrip
                ? Theme.CurrentScheme.Surface
                : Theme.CurrentScheme.MenuBackground;
            using (var bg = new SolidBrush(fillCol))
            {
                e.Graphics.FillRectangle(bg, e.ToolStrip.ClientRectangle);
            }

            if (!(e.ToolStrip is MenuStrip))
            {
                var scheme = Theme.CurrentScheme;
                foreach (ToolStripItem item in e.ToolStrip.Items)
                {
                    if (item is ToolStripTextBox tb && tb.Visible)
                    {
                        bool focused = tb.Focused || (tb.TextBox != null && tb.TextBox.Focused);
                        bool hovered = tb.Selected || (tb.TextBox != null && tb.TextBox.IsHandleCreated && tb.TextBox.ClientRectangle.Contains(tb.TextBox.PointToClient(Cursor.Position)));
                        Color lineCol = focused
                            ? scheme.Primary
                            : (hovered
                                ? ColorScheme.Blend(scheme.Primary, scheme.OnBackground, 0.35f)
                                : ColorScheme.Blend(scheme.ControlBorder, scheme.OnField, 0.25f));
                        int lineH = focused ? 2 : 1;
                        var b = tb.Bounds;
                        int textW = string.IsNullOrEmpty(tb.Text)
                            ? 0
                            : TextRenderer.MeasureText(
                                tb.Text,
                                tb.Font ?? SystemFonts.MenuFont,
                                new Size(int.MaxValue, b.Height),
                                TextFormatFlags.NoPadding).Width;
                        int lineW = Math.Min(b.Width, Math.Max(26, textW + 8));
                        int lineX = b.Left + (b.Width - lineW) / 2;
                        int lineY = Math.Min(e.ToolStrip.ClientRectangle.Bottom - lineH, b.Bottom);
                        using (var lineBrush = new SolidBrush(lineCol))
                        {
                            e.Graphics.FillRectangle(lineBrush, lineX, lineY, lineW, lineH);
                        }
                    }
                }
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Rely on Windows 11 DWM native rounded corners and DWMWA_BORDER_COLOR
            // rather than drawing fake GDI+ rounded inset borders with dark corners.
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            if (Theme.CurrentScheme == null)
            {
                base.OnRenderImageMargin(e);
                return;
            }

            using (var bg = new SolidBrush(Theme.CurrentScheme.MenuBackground))
            {
                e.Graphics.FillRectangle(bg, e.AffectedBounds);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            if (Theme.CurrentScheme == null)
            {
                base.OnRenderSeparator(e);
                return;
            }

            int y = e.Item.Height / 2;
            using (var pen = new Pen(Theme.CurrentScheme.ControlBorder, 1f))
            {
                e.Graphics.DrawLine(pen, 8, y, Math.Max(9, e.Item.Width - 8), y);
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            if (Theme.CurrentScheme == null)
            {
                base.OnRenderItemCheck(e);
                return;
            }

            if (e.Item is ToolStripMenuItem menuItem &&
                menuItem.Tag is string tag &&
                !string.Equals(tag, "ThemeItem", StringComparison.Ordinal))
            {
                // Checked state for accent items is drawn alongside the color swatch in OnRenderMenuItemBackground.
                return;
            }

            var g = e.Graphics;
            var prevSmooth = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = 14f;
            float cy = e.Item.Height / 2f;
            var pts = new[]
            {
                new PointF(cx - 4.2f, cy + 0.2f),
                new PointF(cx - 1.2f, cy + 3.2f),
                new PointF(cx + 4.5f, cy - 3.0f)
            };

            using (var pen = new Pen(Theme.CurrentScheme.Primary, 1.8f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, pts);
            }

            g.SmoothingMode = prevSmooth;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (Theme.CurrentScheme == null)
            {
                return;
            }

            var scheme = Theme.CurrentScheme;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            bool isDark = scheme.Background.GetBrightness() < 0.5f;

            if (e.Item.IsOnDropDown)
            {
                Color dropBg = scheme.MenuBackground;
                using (var fillBg = new SolidBrush(dropBg))
                {
                    g.FillRectangle(fillBg, new Rectangle(Point.Empty, e.Item.Size));
                }

                bool isComboPopover = Equals(e.ToolStrip?.Tag, "ComboPopover");
                bool isChecked = e.Item is ToolStripMenuItem mi && mi.Checked;
                if (e.Item.Selected)
                {
                    var dropRect = new RectangleF(4f, 2f, e.Item.Width - 8f, e.Item.Height - (isComboPopover ? 4f : 3f));
                    Color pillFill = isComboPopover
                        ? Color.FromArgb(isDark ? 34 : 24, scheme.Primary)
                        : ColorScheme.Blend(scheme.OnBackground, scheme.MenuBackground, isDark ? 0.91f : 0.93f);
                    using (var path = Theme.CreateRoundedRectPath(dropRect, 5f))
                    using (var brush = new SolidBrush(pillFill))
                    {
                        g.FillPath(brush, path);
                    }
                }

                if (isComboPopover && isChecked)
                {
                    float cx = e.Item.Width - 14f;
                    float cy = e.Item.Height / 2f;
                    var pts = new[]
                    {
                        new PointF(cx - 4.0f, cy + 0.1f),
                        new PointF(cx - 1.2f, cy + 3.0f),
                        new PointF(cx + 4.2f, cy - 2.8f)
                    };
                    using (var checkPen = new Pen(scheme.Primary, 1.7f)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round,
                        LineJoin = LineJoin.Round
                    })
                    {
                        g.DrawLines(checkPen, pts);
                    }
                }

                if (!isComboPopover &&
                    e.Item is ToolStripMenuItem menuItem &&
                    menuItem.Tag is string accentName &&
                    !string.Equals(accentName, "ThemeItem", StringComparison.Ordinal))
                {
                    Color? swatchColor = null;
                    foreach (var entry in ColorScheme.AppleAccents)
                    {
                        if (string.Equals(entry.Name, accentName, StringComparison.OrdinalIgnoreCase))
                        {
                            swatchColor = entry.Color;
                            break;
                        }
                    }

                    if (swatchColor.HasValue)
                    {
                        float cx = 14f;
                        float cy = e.Item.Height / 2f;
                        using (var swatchBrush = new SolidBrush(swatchColor.Value))
                        {
                            g.FillEllipse(swatchBrush, cx - 4f, cy - 4f, 8f, 8f);
                        }

                        if (menuItem.Checked)
                        {
                            using (var ringPen = new Pen(scheme.OnControl, 1.5f))
                            using (var coreBrush = new SolidBrush(Color.White))
                            {
                                g.DrawEllipse(ringPen, cx - 6f, cy - 6f, 12f, 12f);
                                g.FillEllipse(coreBrush, cx - 1.5f, cy - 1.5f, 3f, 3f);
                            }
                        }
                    }
                }
                return;
            }

            // Highlight-only text hover/open state on top bar items (Charts, Advanced, Themes): no theme accent fill box.
            return;
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            if (Theme.CurrentScheme != null)
            {
                bool active = e.Item != null && (e.Item.Selected || e.Item.Pressed);
                e.ArrowColor = active ? Theme.CurrentScheme.OnBackground : Theme.CurrentScheme.OnField;
            }
            base.OnRenderArrow(e);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (Theme.CurrentScheme == null)
            {
                base.OnRenderItemText(e);
                return;
            }

            var scheme = Theme.CurrentScheme;
            if (!e.Item.IsOnDropDown)
            {
                bool active = e.Item.Selected || e.Item.Pressed;
                e.TextColor = active ? scheme.OnBackground : scheme.OnField;
            }
            else if (!e.Item.Enabled)
            {
                e.TextColor = scheme.OnDisabledControl;
            }
            else if (Equals(e.ToolStrip?.Tag, "ComboPopover"))
            {
                bool isChecked = e.Item is ToolStripMenuItem mi && mi.Checked;
                e.TextColor = (e.Item.Selected || isChecked) ? scheme.OnBackground : scheme.OnField;
                var rect = new Rectangle(12, 0, Math.Max(1, e.Item.Width - 34), e.Item.Height);
                TextRenderer.DrawText(
                    e.Graphics,
                    e.Text,
                    e.TextFont,
                    rect,
                    e.TextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                return;
            }
            else
            {
                bool active = e.Item.Selected || (e.Item is ToolStripMenuItem mi && mi.Checked);
                e.TextColor = active ? scheme.OnBackground : scheme.OnField;
            }

            base.OnRenderItemText(e);
        }
    }

    public class StyledMenuColors : ProfessionalColorTable
    {
        private static ColorScheme S => Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;

        public override Color ButtonCheckedHighlight => S.ButtonFace;

        public override Color CheckBackground => S.CheckBoxBackground;

        public override Color CheckPressedBackground => S.MenuSelected;

        public override Color CheckSelectedBackground => S.CheckBoxHover;

        public override Color ButtonSelectedBorder => S.CheckBoxBorder;

        public override Color MenuItemSelectedGradientBegin => S.MenuSelected;

        public override Color MenuItemSelectedGradientEnd => S.MenuSelected;

        public override Color MenuItemPressedGradientBegin => S.Control;

        public override Color MenuItemPressedGradientEnd => S.Control;

        public override Color MenuItemBorder => S.MenuSelectedBorder;

        public override Color MenuItemSelected => S.MenuSelected;

        public override Color ToolStripDropDownBackground => S.MenuBackground;

        public override Color ImageMarginGradientBegin => S.MenuBackground;

        public override Color ImageMarginGradientMiddle => S.MenuBackground;

        public override Color ImageMarginGradientEnd => S.MenuBackground;

        public override Color SeparatorDark => S.ControlBorder;

        public override Color SeparatorLight => S.Surface;

        public override Color MenuBorder => S.ControlBorder;
    }
}