using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using grapher.Models.Theming.Controls;
using Cursor = System.Windows.Forms.Cursor;

namespace grapher.Models.Theming
{
    internal class NativeSurfacePanel : Panel
    {
        public NativeSurfacePanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();
        }

        protected override bool ShowFocusCues => false;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color bg = BackColor != Color.Transparent ? BackColor : (Parent?.BackColor ?? (Theme.CurrentScheme?.Surface ?? Color.Black));
            e.Graphics.Clear(bg);
        }
    }

    internal class NativeSurfaceButton : Button
    {
        public NativeSurfaceButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            FlatAppearance.CheckedBackColor = Color.Transparent;
        }

        internal event PaintEventHandler CustomPaint;

        protected override bool ShowFocusCues => false;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color bg = Parent?.BackColor ?? BackColor;
            e.Graphics.Clear(bg);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (CustomPaint != null)
            {
                CustomPaint.Invoke(this, e);
                return;
            }

            base.OnPaint(e);
        }
    }

    public class Theme
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWCP_ROUND = 2;

        private const TextFormatFlags TextFormatFlags = System.Windows.Forms.TextFormatFlags.HorizontalCenter |
                                                        System.Windows.Forms.TextFormatFlags.VerticalCenter |
                                                        System.Windows.Forms.TextFormatFlags.WordBreak;

        private static readonly string SystemFontFamilyName = ResolveSystemFontFamilyName();
        private static readonly string NumericFontFamilyName = ResolveNumericFontFamilyName();
        internal static readonly Font UiRegularFont = ResolveSystemFont(8.5F, FontStyle.Regular);
        private static readonly Font UiHeaderFont = ResolveSystemFont(8.5F, FontStyle.Bold);
        private static readonly Font UiPrimaryButtonFont = ResolveSystemFont(9.0F, FontStyle.Bold);
        private static readonly Font UiNumericFont = new Font(NumericFontFamilyName, 8.25F, FontStyle.Regular);
        private static readonly Font UiRichNumericFont = new Font(NumericFontFamilyName, 7.75F, FontStyle.Regular);
        internal static readonly StyledMenuRenderer SharedMenuRenderer = new StyledMenuRenderer();

        internal static bool SupportsDwmRoundedCorners =>
            Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;

        private static string ResolveSystemFontFamilyName()
        {
            return PickInstalledFont(new[] { "SF Pro Text", "SF Pro Display", "Inter", "Segoe UI Variable Text", "Segoe UI" }, "Segoe UI");
        }

        private static string ResolveNumericFontFamilyName()
        {
            return PickInstalledFont(new[] { "JetBrains Mono", "Cascadia Mono", "SF Mono", "Consolas", "Segoe UI" }, "Segoe UI");
        }

        private static string PickInstalledFont(string[] preferred, string fallback)
        {
            using (var installed = new InstalledFontCollection())
            {
                foreach (var candidate in preferred)
                {
                    foreach (var family in installed.Families)
                    {
                        if (string.Equals(family.Name, candidate, StringComparison.OrdinalIgnoreCase))
                        {
                            return family.Name;
                        }
                    }
                }
            }

            return fallback;
        }

        private static Font ResolveSystemFont(float size, FontStyle style)
        {
            return new Font(SystemFontFamilyName, size, style);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ScrollableControl, ScrollPanelRepaintHook> ScrollRepaintHooks =
            new System.Runtime.CompilerServices.ConditionalWeakTable<ScrollableControl, ScrollPanelRepaintHook>();

        private sealed class ScrollPanelRepaintHook : NativeWindow
        {
            private const int WM_HSCROLL = 0x0114;
            private const int WM_VSCROLL = 0x0115;
            private const int WM_MOUSEWHEEL = 0x020A;

            private readonly ScrollableControl _panel;

            public ScrollPanelRepaintHook(ScrollableControl panel)
            {
                _panel = panel;
                panel.Disposed += (s, e) => ReleaseHandle();
                panel.HandleDestroyed += (s, e) => ReleaseHandle();
                panel.HandleCreated += (s, e) => AttachIfNeeded();
                AttachIfNeeded();
            }

            public void AttachIfNeeded()
            {
                if (_panel != null && !_panel.IsDisposed && _panel.IsHandleCreated && Handle != _panel.Handle)
                {
                    if (Handle != IntPtr.Zero)
                    {
                        ReleaseHandle();
                    }
                    AssignHandle(_panel.Handle);
                }
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if ((m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL || m.Msg == WM_MOUSEWHEEL) &&
                    _panel != null && !_panel.IsDisposed)
                {
                    _panel.Invalidate(false);
                }
            }
        }

        private static void Control_ApplyScrollBarOnHandleCreated(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                ApplyScrollBarTheme(control);
            }
        }

        internal static void ApplyScrollBarTheme(Control control)
        {
            if (control == null || control.IsDisposed) return;

            if (control is ScrollableControl scrollable)
            {
                if (!ScrollRepaintHooks.TryGetValue(scrollable, out var hook))
                {
                    hook = new ScrollPanelRepaintHook(scrollable);
                    ScrollRepaintHooks.Add(scrollable, hook);
                }
                else
                {
                    hook.AttachIfNeeded();
                }
            }

            try
            {
                if (!control.IsHandleCreated)
                {
                    control.HandleCreated -= Control_ApplyScrollBarOnHandleCreated;
                    control.HandleCreated += Control_ApplyScrollBarOnHandleCreated;
                    return;
                }
                bool isDark = SafeScheme.Background.GetBrightness() < 0.5f;
                SetWindowTheme(control.Handle, isDark ? "DarkMode_Explorer" : "Explorer", null);
            }
            catch
            {
                // Ignore on older OS builds.
            }
        }

        private static Icon _cachedLogoIcon;
        private static Color _cachedLogoColor;
        private static readonly PropertyInfo DoubleBufferedProp =
            typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void EnableDoubleBuffering(Control control)
        {
            try
            {
                DoubleBufferedProp?.SetValue(control, true, null);
            }
            catch
            {
                // Best-effort double buffering for built-in controls.
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        private sealed class DropDownPositionHook : NativeWindow
        {
            private const int WM_WINDOWPOSCHANGING = 0x0046;
            private const uint SWP_NOSIZE = 0x0001;
            private const uint SWP_NOMOVE = 0x0002;

            private readonly ToolStripDropDown _dropDown;

            public DropDownPositionHook(ToolStripDropDown dropDown)
            {
                _dropDown = dropDown;
                dropDown.Disposed += (s, e) => ReleaseHandle();
                dropDown.HandleDestroyed += (s, e) => ReleaseHandle();
                dropDown.HandleCreated += (s, e) => AttachIfNeeded();
                AttachIfNeeded();
            }

            public void AttachIfNeeded()
            {
                if (_dropDown != null && !_dropDown.IsDisposed && _dropDown.IsHandleCreated)
                {
                    if (Handle != _dropDown.Handle)
                    {
                        if (Handle != IntPtr.Zero)
                        {
                            ReleaseHandle();
                        }
                        AssignHandle(_dropDown.Handle);
                    }
                }
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_WINDOWPOSCHANGING &&
                    _dropDown != null &&
                    !_dropDown.IsDisposed &&
                    _dropDown.OwnerItem is ToolStripMenuItem owner &&
                    !owner.IsOnDropDown &&
                    owner.GetCurrentParent() is MenuStrip strip &&
                    strip.IsHandleCreated)
                {
                    var wp = Marshal.PtrToStructure<WINDOWPOS>(m.LParam);
                    if ((wp.flags & SWP_NOMOVE) == 0)
                    {
                        int desiredTop = strip.PointToScreen(new Point(0, owner.Bounds.Bottom)).Y + 4;
                        int h = (wp.flags & SWP_NOSIZE) != 0 ? _dropDown.Height : wp.cy;
                        if (h <= 0) h = _dropDown.PreferredSize.Height;
                        var workArea = Screen.FromControl(strip).WorkingArea;
                        if (desiredTop + h <= workArea.Bottom)
                        {
                            wp.y = desiredTop;
                            Marshal.StructureToPtr(wp, m.LParam, false);
                        }
                    }
                }

                base.WndProc(ref m);
            }
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ToolStripDropDown, DropDownPositionHook> DropDownHooks =
            new System.Runtime.CompilerServices.ConditionalWeakTable<ToolStripDropDown, DropDownPositionHook>();

        private static ColorScheme SafeScheme => CurrentScheme ?? ColorScheme.MacOSDarkTheme;

        private static Color GetMutedLabelColor(ColorScheme scheme, Color againstSurface)
        {
            return ColorScheme.Blend(scheme.OnField, againstSurface, 0.28f);
        }

        internal static Color ResolveChartCanvasColor(ColorScheme scheme)
        {
            var s = scheme ?? SafeScheme;
            return s.ChartBackground;
        }

        private static Color GetChartAxisColor(ColorScheme scheme)
        {
            return ColorScheme.Blend(scheme.ChartForeground, ResolveChartCanvasColor(scheme), 0.85f);
        }

        private static Color GetChartGridColor(ColorScheme scheme)
        {
            return ColorScheme.Blend(scheme.ChartForeground, ResolveChartCanvasColor(scheme), 0.92f);
        }

        internal static Panel CreateBorderlessTitleBar(Form form, string title, bool includeMinimize, bool showAppLogo = true)
        {
            var scheme = SafeScheme;
            var header = new NativeSurfacePanel
            {
                Name = "titleBarPanel",
                Dock = DockStyle.Top,
                Height = 40,
                Width = form.ClientSize.Width,
                BackColor = scheme.Surface
            };

            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var activeScheme = SafeScheme;
                Color bg = activeScheme.Surface;
                Color hairline = activeScheme.ControlBorder;
                g.Clear(bg);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                float titleX = 16f;
                if (showAppLogo)
                {
                    DrawInxaneLogo(g, new RectangleF(14f, 10f, 20f, 20f));
                    titleX = 42f;
                }

                using (var font = ResolveSystemFont(9.75f, FontStyle.Bold))
                using (var textBrush = new SolidBrush(activeScheme.OnBackground))
                using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(title, font, textBrush, new RectangleF(titleX, 0f, Math.Max(90f, header.Width - titleX - 96f), header.Height - 1f), sf);
                }

                if (showAppLogo && includeMinimize && header.Width > 260)
                {
                    using (var sepPen = new Pen(hairline, 1f))
                    {
                        g.DrawLine(sepPen, 136f, 12f, 136f, 28f);
                    }
                }

                using (var hairPen = new Pen(hairline, 1f))
                {
                    g.DrawLine(hairPen, 0f, header.Height - 0.5f, header.Width, header.Height - 0.5f);
                }
            };

            header.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(form.Handle, 0xA1, 0x2, 0);
                }
            };

            Button MakeWindowBtn(string name, bool isClose, int rightOffset, Action onClick)
            {
                var btn = new NativeSurfaceButton
                {
                    Name = name,
                    Tag = "CustomDashboardControl",
                    Text = string.Empty,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    Size = new Size(42, 39),
                    Location = new Point(header.Width - rightOffset, 0),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    TabStop = false,
                    Cursor = Cursors.Hand
                };
                btn.CustomPaint += (s, e) => PaintTitleWindowButton((Control)s, e, isClose);
                btn.MouseEnter += (s, e) => btn.Invalidate();
                btn.MouseLeave += (s, e) => btn.Invalidate();
                btn.MouseDown += (s, e) => btn.Invalidate();
                btn.MouseUp += (s, e) => btn.Invalidate();
                btn.Click += (s, e) => onClick();
                return btn;
            }

            Button minBtn = includeMinimize
                ? MakeWindowBtn("titleMinimizeButton", false, 84, () => form.WindowState = FormWindowState.Minimized)
                : null;
            Button closeBtn = MakeWindowBtn("titleCloseButton", true, 42, () => form.Close());

            if (minBtn != null) header.Controls.Add(minBtn);
            header.Controls.Add(closeBtn);

            header.Resize += (s, e) =>
            {
                header.Invalidate();
            };

            return header;
        }

        private static void PaintTitleWindowButton(Control btn, PaintEventArgs e, bool isClose)
        {
            var g = e.Graphics;
            var scheme = SafeScheme;
            g.Clear(scheme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var mousePos = btn.PointToClient(Cursor.Position);
            bool hovered = btn.ClientRectangle.Contains(mousePos);
            bool pressed = hovered && (Control.MouseButtons & MouseButtons.Left) != 0;

            // Highlight-only glyph hover: never draw background fill or border boxes behind window glyphs.
            Color idleGlyph = ColorScheme.Blend(scheme.OnBackground, scheme.Surface, 0.42f);
            Color glyphColor = pressed
                ? scheme.Primary
                : (hovered ? scheme.OnBackground : idleGlyph);

            float cx = btn.Width / 2f;
            float cy = btn.Height / 2f;
            using (var glyphPen = new Pen(glyphColor, 1.35f))
            {
                glyphPen.StartCap = LineCap.Round;
                glyphPen.EndCap = LineCap.Round;
                if (isClose)
                {
                    float r = 4.2f;
                    g.DrawLine(glyphPen, cx - r, cy - r, cx + r, cy + r);
                    g.DrawLine(glyphPen, cx - r, cy + r, cx + r, cy - r);
                }
                else
                {
                    g.DrawLine(glyphPen, cx - 4.5f, cy, cx + 4.5f, cy);
                }
            }
        }

        public static void DrawInxaneLogo(Graphics g, RectangleF rect)
        {
            var prevSmooth = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var scheme = SafeScheme;
            float radius = Math.Max(3.5f, rect.Width * 0.24f);
            Color fillCol = scheme.Control;
            Color borderCol = scheme.ControlBorder;
            Color slashPrimary = scheme.Primary;
            Color slashSecondary = ColorScheme.Blend(scheme.OnBackground, scheme.Surface, 0.35f);

            using (var bgPath = CreateRoundedRectPath(rect, radius))
            using (var bgBrush = new SolidBrush(fillCol))
            using (var borderPen = new Pen(borderCol, Math.Max(1f, rect.Width * 0.055f)))
            {
                g.FillPath(bgBrush, bgPath);
                g.DrawPath(borderPen, bgPath);
            }

            float padX = rect.Width * 0.28f;
            float padY = rect.Height * 0.28f;
            float xLeft = rect.X + padX;
            float xRight = rect.Right - padX;
            float yTop = rect.Y + padY;
            float yBot = rect.Bottom - padY;
            float stroke = Math.Max(1.8f, rect.Width * 0.12f);

            using (var primarySlashPen = new Pen(slashPrimary, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            using (var crossSlashPen = new Pen(slashSecondary, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(crossSlashPen, xLeft, yBot, xRight, yTop);
                g.DrawLine(primarySlashPen, xLeft, yTop, xRight, yBot);
            }

            g.SmoothingMode = prevSmooth;
        }

        public static Bitmap CreateInxaneLogoBitmap(int size)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                float pad = Math.Max(1f, size * 0.06f);
                DrawInxaneLogo(g, new RectangleF(pad, pad, size - pad * 2f, size - pad * 2f));
            }
            return bmp;
        }

        public static Icon CreateInxaneLogoIcon()
        {
            Color primary = SafeScheme.Primary;
            if (_cachedLogoIcon != null && _cachedLogoColor == primary)
            {
                return _cachedLogoIcon;
            }

            _cachedLogoIcon?.Dispose();
            using (var bmp = CreateInxaneLogoBitmap(32))
            {
                IntPtr hIcon = bmp.GetHicon();
                _cachedLogoIcon = (Icon)Icon.FromHandle(hIcon).Clone();
                DestroyIcon(hIcon);
                _cachedLogoColor = primary;
            }
            return _cachedLogoIcon;
        }

        public static ColorScheme CurrentScheme
        {
            get => _currentScheme;
            set
            {
                _previousScheme = _currentScheme;
                _currentScheme = value;
            }
        }

        private static ColorScheme _previousScheme = null;
        private static ColorScheme _currentScheme;

        public static void Apply(Form form, MenuStrip menu = null)
        {
            var scheme = SafeScheme;
            form.BackColor = form is RawAcceleration ? ResolveChartCanvasColor(scheme) : scheme.Background;
            form.ForeColor = scheme.OnBackground;
            form.Icon = CreateInxaneLogoIcon();
            ApplyDwm(form);

            if (form.HasChildren)
            {
                RemoveEvents(form.Controls);
                ApplyTheme(form.Controls);
            }

            if (menu == null)
            {
                form.Invalidate(true);
                return;
            }

            ApplyMenuTheme(menu);
            form.Invalidate(true);
        }

        private static void ApplyMenuTheme(MenuStrip menu)
        {
            var scheme = SafeScheme;
            menu.BackColor = scheme.Surface;
            menu.ForeColor = scheme.OnBackground;
            menu.Font = UiRegularFont;
            menu.Padding = new Padding(0);

            var menuRenderer = SharedMenuRenderer;
            menu.Renderer = menuRenderer;

            int count = menu.Items.Count;
            int totalWidth = 0;
            for (int i = 0; i < count; i++)
            {
                if (menu.Items[i] is ToolStripMenuItem item)
                {
                    string plain = (item.Text ?? string.Empty).Replace("&", string.Empty);
                    int textW = TextRenderer.MeasureText(plain, UiRegularFont).Width;
                    item.AutoSize = false;
                    item.TextAlign = ContentAlignment.MiddleCenter;
                    item.Padding = Padding.Empty;
                    item.Margin = new Padding(2, 0, 2, 0);
                    item.Size = new Size(textW + 24, 28);
                    totalWidth += item.Width + item.Margin.Horizontal;
                    ApplyTheme(item);
                    PreWarmDropDownHierarchy(item.DropDown, menuRenderer);
                }
            }

            menu.AutoSize = false;
            menu.Height = 28;
            if (totalWidth > 0)
            {
                menu.Width = totalWidth + 4;
            }
        }

        private static void PreWarmDropDownHierarchy(ToolStripDropDown dropDown, StyledMenuRenderer renderer)
        {
            if (dropDown == null || dropDown.IsDisposed)
            {
                return;
            }

            var scheme = SafeScheme;
            dropDown.Renderer = renderer ?? SharedMenuRenderer;
            dropDown.BackColor = scheme.MenuBackground;
            dropDown.ForeColor = scheme.OnField;
            dropDown.Padding = new Padding(0, 4, 0, 4);
            dropDown.DropShadowEnabled = !SupportsDwmRoundedCorners;
            EnableDoubleBuffering(dropDown);

            dropDown.HandleCreated -= DropDown_HandleCreated;
            dropDown.HandleCreated += DropDown_HandleCreated;
            dropDown.Opening -= DropDown_Opening;
            dropDown.Opening += DropDown_Opening;

            if (!DropDownHooks.TryGetValue(dropDown, out var hook))
            {
                hook = new DropDownPositionHook(dropDown);
                DropDownHooks.Add(dropDown, hook);
            }

            dropDown.PerformLayout();
            if (!dropDown.IsHandleCreated)
            {
                IntPtr unused = dropDown.Handle;
            }
            hook.AttachIfNeeded();
            ApplyDropDownDwm(dropDown);

            foreach (ToolStripItem child in dropDown.Items)
            {
                if (child is ToolStripDropDownItem subItem && subItem.HasDropDownItems)
                {
                    PreWarmDropDownHierarchy(subItem.DropDown, renderer);
                }
            }
        }

        private static void DropDown_HandleCreated(object sender, EventArgs e)
        {
            if (sender is ToolStripDropDown dropDown && !dropDown.IsDisposed)
            {
                EnableDoubleBuffering(dropDown);
                ApplyDropDownDwm(dropDown);
            }
        }

        private static void DropDown_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is ToolStripDropDown dropDown && !dropDown.IsDisposed)
            {
                var scheme = SafeScheme;
                dropDown.BackColor = scheme.MenuBackground;
                dropDown.ForeColor = scheme.OnField;
                dropDown.DropShadowEnabled = !SupportsDwmRoundedCorners;
                if (!dropDown.IsHandleCreated)
                {
                    IntPtr unused = dropDown.Handle;
                }
                ApplyDropDownDwm(dropDown);
            }
        }

        private static void ApplyDwm(Form form)
        {
            if (form.IsHandleCreated)
            {
                ApplyDwmAttributes(form);
            }
            else
            {
                form.HandleCreated -= Form_HandleCreated;
                form.HandleCreated += Form_HandleCreated;
            }
        }

        private static void Form_HandleCreated(object sender, EventArgs e)
        {
            if (sender is Form form)
            {
                ApplyDwmAttributes(form);
            }
        }

        private static void ApplyDwmAttributes(Form form)
        {
            try
            {
                var scheme = SafeScheme;
                bool isDark = scheme.Background.GetBrightness() < 0.5f;
                int useDarkMode = isDark ? 1 : 0;
                DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

                int cornerPref = DWMWCP_ROUND;
                DwmSetWindowAttribute(form.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

                int win32Border = ColorTranslator.ToWin32(scheme.ControlBorder);
                DwmSetWindowAttribute(form.Handle, DWMWA_BORDER_COLOR, ref win32Border, sizeof(int));

                int captionColor = ColorTranslator.ToWin32(scheme.Surface);
                DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
            }
            catch
            {
                // Ignore on older Windows versions that do not support these DWM attributes.
            }
        }

        internal static void ApplyDropDownDwm(ToolStripDropDown dropDown)
        {
            if (dropDown == null || !dropDown.IsHandleCreated)
            {
                return;
            }

            try
            {
                dropDown.Region = null;
                var scheme = SafeScheme;
                bool isDark = scheme.Background.GetBrightness() < 0.5f;
                int useDarkMode = isDark ? 1 : 0;
                DwmSetWindowAttribute(dropDown.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));

                int cornerPref = DWMWCP_ROUND;
                DwmSetWindowAttribute(dropDown.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

                int win32Border = ColorTranslator.ToWin32(scheme.ControlBorder);
                DwmSetWindowAttribute(dropDown.Handle, DWMWA_BORDER_COLOR, ref win32Border, sizeof(int));
            }
            catch
            {
                // Ignore DWM errors on older OS versions.
            }
        }

        private static void ApplyTheme(ToolStripDropDownItem item)
        {
            var scheme = SafeScheme;
            item.BackColor = scheme.Surface;
            item.ForeColor = scheme.OnBackground;
            item.Font = UiRegularFont;

            if (!item.HasDropDownItems) return;

            if (item.DropDown != null)
            {
                item.DropDown.BackColor = scheme.MenuBackground;
                item.DropDown.ForeColor = scheme.OnField;
                item.DropDown.Padding = new Padding(0, 4, 0, 4);
                item.DropDown.DropShadowEnabled = !SupportsDwmRoundedCorners;
                item.DropDown.Opened -= DropDown_Opened;
                item.DropDown.Opened += DropDown_Opened;
                item.DropDown.Closed -= DropDown_Closed;
                item.DropDown.Closed += DropDown_Closed;
            }

            foreach (ToolStripItem child in item.DropDownItems)
            {
                if (child is ToolStripMenuItem toolStripItem)
                {
                    ApplyTheme(toolStripItem);
                }
                else if (child is ToolStripTextBox textBoxItem)
                {
                    textBoxItem.BackColor = scheme.MenuBackground;
                    textBoxItem.ForeColor = scheme.OnBackground;
                    textBoxItem.Font = UiNumericFont;
                    textBoxItem.BorderStyle = BorderStyle.None;
                    if (textBoxItem.TextBoxTextAlign != HorizontalAlignment.Center)
                    {
                        textBoxItem.TextBoxTextAlign = HorizontalAlignment.Center;
                    }
                    if (textBoxItem.TextBox != null)
                    {
                        textBoxItem.TextBox.BackColor = scheme.MenuBackground;
                        textBoxItem.TextBox.ForeColor = scheme.OnBackground;
                        textBoxItem.TextBox.GotFocus -= ToolStripTextBox_InvalidateOwner;
                        textBoxItem.TextBox.GotFocus += ToolStripTextBox_InvalidateOwner;
                        textBoxItem.TextBox.LostFocus -= ToolStripTextBox_InvalidateOwner;
                        textBoxItem.TextBox.LostFocus += ToolStripTextBox_InvalidateOwner;
                        textBoxItem.TextBox.MouseEnter -= ToolStripTextBox_InvalidateOwner;
                        textBoxItem.TextBox.MouseEnter += ToolStripTextBox_InvalidateOwner;
                        textBoxItem.TextBox.MouseLeave -= ToolStripTextBox_InvalidateOwner;
                        textBoxItem.TextBox.MouseLeave += ToolStripTextBox_InvalidateOwner;
                        textBoxItem.TextBox.TextChanged -= ToolStripTextBox_InvalidateOwner;
                        textBoxItem.TextBox.TextChanged += ToolStripTextBox_InvalidateOwner;
                    }
                }
            }
        }

        private static void ToolStripTextBox_InvalidateOwner(object sender, EventArgs e)
        {
            if (sender is Control c)
            {
                c.Parent?.Invalidate();
            }
        }

        private static void DropDown_Opened(object sender, EventArgs e)
        {
            if (sender is ToolStripDropDown dropDown &&
                dropDown.OwnerItem is ToolStripMenuItem owner &&
                !owner.IsOnDropDown &&
                owner.GetCurrentParent() is MenuStrip strip)
            {
                strip.Invalidate();
            }
        }

        private static void DropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            if (sender is ToolStripDropDown dropDown &&
                dropDown.OwnerItem is ToolStripMenuItem owner &&
                owner.GetCurrentParent() is MenuStrip strip)
            {
                strip.Invalidate();
            }
        }

        public static void ApplyTheme(Control.ControlCollection container)
        {
            foreach (Control control in container)
            {
                switch (control)
                {
                    case MenuStrip menuStrip:
                    {
                        ApplyMenuTheme(menuStrip);
                        break;
                    }
                    case Chart chart:
                    {
                        var scheme = SafeScheme;
                        Color dotCore = scheme.Background.GetBrightness() < 0.5f ? Color.White : scheme.Surface;
                        SetChartColors(chart);
                        chart.PostPaint += Chart_PostPaint;
                        chart.Resize += Chart_ResizeInlineLayout;

                        chart.Series[0].Color = scheme.Primary;
                        chart.Series[1].MarkerStyle = MarkerStyle.Circle;
                        chart.Series[1].MarkerSize = 7;
                        chart.Series[1].Color = dotCore;
                        chart.Series[1].MarkerColor = dotCore;
                        chart.Series[1].MarkerBorderColor = scheme.Primary;
                        chart.Series[1].MarkerBorderWidth = 2;

                        chart.Series[2].Color = scheme.Secondary;
                        if (chart.Series.Count > 3)
                        {
                            chart.Series[3].MarkerStyle = MarkerStyle.Circle;
                            chart.Series[3].MarkerSize = 7;
                            chart.Series[3].Color = dotCore;
                            chart.Series[3].MarkerColor = dotCore;
                            chart.Series[3].MarkerBorderColor = scheme.Secondary;
                            chart.Series[3].MarkerBorderWidth = 2;
                        }

                        break;
                    }
                    case Label label:
                    {
                        var scheme = SafeScheme;
                        Color labelBack = ResolveLabelBackColor(label);
                        Color refBg = labelBack == Color.Transparent ? scheme.Surface : labelBack;
                        bool isHeader = IsHeaderLabel(label);
                        bool isActiveValue = label.Name.IndexOf("Active", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool isWordActive = label.Name.IndexOf("AccelTypeActive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            label.Name.IndexOf("LutApplyActive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            label.Name.IndexOf("CapTypeActive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            label.Name.IndexOf("gainSwitchActive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            label.Name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool isNumericLabel = (isActiveValue && !isWordActive) || label.Name == "MouseLabel";
                        bool isDescriptor = !isHeader && !isActiveValue && IsDescriptorLabel(label);

                        label.Font = isHeader ? UiHeaderFont : (isNumericLabel ? UiNumericFont : UiRegularFont);
                        if (isDescriptor)
                        {
                            label.ForeColor = GetMutedLabelColor(scheme, refBg);
                        }
                        else if (isActiveValue && !isHeader)
                        {
                            // Keep active readouts unboxed, clean, and uniform with surrounding text.
                            label.ForeColor = isNumericLabel
                                ? scheme.OnBackground
                                : ColorScheme.Blend(scheme.OnBackground, refBg, 0.14f);
                        }
                        else
                        {
                            label.ForeColor = scheme.OnBackground;
                        }
                        label.BackColor = labelBack;
                        label.Paint += Label_DisabledPaint;
                        label.EnabledChanged += Control_InvalidateOnMouse;
                        break;
                    }
                    case CheckBox checkBox:
                    {
                        var scheme = SafeScheme;
                        Color hostBack = ResolveContainerBackColor(checkBox.Parent);
                        checkBox.Appearance = Appearance.Button;
                        checkBox.FlatStyle = FlatStyle.Flat;
                        checkBox.TextAlign = ContentAlignment.MiddleLeft;
                        checkBox.FlatAppearance.BorderSize = 0;
                        checkBox.FlatAppearance.MouseOverBackColor = Color.Transparent;
                        checkBox.FlatAppearance.MouseDownBackColor = Color.Transparent;
                        checkBox.FlatAppearance.CheckedBackColor = Color.Transparent;
                        checkBox.AutoSize = false;
                        if (checkBox.Height < 20)
                        {
                            checkBox.Height = 20;
                        }
                        checkBox.Font = UiRegularFont;
                        if (!string.IsNullOrEmpty(checkBox.Text))
                        {
                            int neededWidth = TextRenderer.MeasureText(checkBox.Text, checkBox.Font).Width + 24;
                            if (checkBox.Width < neededWidth)
                            {
                                checkBox.Width = neededWidth;
                            }
                        }
                        checkBox.BackColor = hostBack;
                        checkBox.ForeColor = checkBox.Checked ? scheme.OnBackground : GetMutedLabelColor(scheme, hostBack);
                        checkBox.Paint += CheckBox_Paint;
                        checkBox.EnabledChanged += Control_InvalidateOnMouse;
                        checkBox.CheckedChanged += Control_InvalidateOnMouse;
                        checkBox.MouseEnter += Control_InvalidateOnMouse;
                        checkBox.MouseLeave += Control_InvalidateOnMouse;
                        checkBox.MouseDown += Control_InvalidateOnMouseButton;
                        checkBox.MouseUp += Control_InvalidateOnMouseButton;
                        break;
                    }
                    case FlowLayoutPanel flowPanel:
                    {
                        var scheme = SafeScheme;
                        flowPanel.BackColor = ResolveContainerBackColor(flowPanel.Parent);
                        flowPanel.ForeColor = scheme.OnBackground;
                        break;
                    }
                    case TableLayoutPanel tablePanel:
                    {
                        var scheme = SafeScheme;
                        EnableDoubleBuffering(tablePanel);
                        tablePanel.BackColor = ResolveContainerBackColor(tablePanel.Parent);
                        tablePanel.ForeColor = scheme.OnBackground;
                        break;
                    }
                    case Panel panel:
                    {
                        var scheme = SafeScheme;
                        EnableDoubleBuffering(panel);
                        panel.BackColor = ResolveContainerBackColor(panel);
                        if (panel.Name == "optionsPanel")
                        {
                            panel.Paint += OptionsPanel_Paint;
                            ApplyScrollBarTheme(panel);
                        }
                        else if (panel.Name == "chartsPanel")
                        {
                            panel.Paint += ChartsPanel_Paint;
                        }
                        panel.ForeColor = scheme.OnBackground;
                        break;
                    }
                    case Button button:
                    {
                        var scheme = SafeScheme;
                        EnableDoubleBuffering(button);
                        if (Equals(button.Tag, "CustomDashboardControl"))
                        {
                            button.BackColor = ResolveContainerBackColor(button.Parent);
                            button.FlatAppearance.BorderSize = 0;
                            button.FlatAppearance.MouseOverBackColor = Color.Transparent;
                            button.FlatAppearance.MouseDownBackColor = Color.Transparent;
                            button.FlatAppearance.CheckedBackColor = Color.Transparent;
                            button.Invalidate();
                            break;
                        }

                        if (button.FlatStyle == FlatStyle.Flat && !Equals(button.Tag, "ThemedButton"))
                        {
                            // Handle this button like a Label instead.
                            control.ForeColor = scheme.OnBackground;
                            control.BackColor = ResolveContainerBackColor(button.Parent);
                            control.Font = button.Name == "DirectionalityLabel" ? UiHeaderFont : UiRegularFont;
                            button.FlatAppearance.BorderSize = 0;
                            button.FlatAppearance.MouseOverBackColor = Color.Transparent;
                            button.FlatAppearance.MouseDownBackColor = Color.Transparent;
                            button.FlatAppearance.CheckedBackColor = Color.Transparent;
                            break;
                        }

                        button.Tag = "ThemedButton";
                        button.UseVisualStyleBackColor = false;
                        button.FlatStyle = FlatStyle.Flat;
                        button.FlatAppearance.BorderSize = 0;
                        button.TabStop = false;

                        bool isPrimary = IsPrimaryButton(button);
                        Color cardBack = button.Parent != null && button.Parent.BackColor != Color.Transparent
                            ? button.Parent.BackColor
                            : ResolveContainerBackColor(button.Parent);
                        button.Font = isPrimary ? UiPrimaryButtonFont : UiRegularFont;
                        button.ForeColor = isPrimary ? scheme.OnPrimary : scheme.OnControl;
                        button.BackColor = cardBack;
                        button.FlatAppearance.BorderColor = cardBack;
                        button.FlatAppearance.MouseOverBackColor = Color.Transparent;
                        button.FlatAppearance.MouseDownBackColor = Color.Transparent;
                        button.FlatAppearance.CheckedBackColor = Color.Transparent;
                        button.Paint += Button_Paint;
                        button.EnabledChanged += Button_EnabledChanged;
                        button.MouseEnter += Control_InvalidateOnMouse;
                        button.MouseLeave += Control_InvalidateOnMouse;
                        button.MouseDown += Control_InvalidateOnMouseButton;
                        button.MouseUp += Control_InvalidateOnMouseButton;

                        break;
                    }
                    case ListBox listBox:
                    {
                        var scheme = SafeScheme;
                        listBox.BackColor = ResolveContainerBackColor(listBox.Parent);
                        listBox.ForeColor = scheme.OnField;
                        listBox.Font = UiRegularFont;
                        break;
                    }
                    case NumericUpDown numUpDown:
                    {
                        var scheme = SafeScheme;
                        Color surface = ResolveContainerBackColor(numUpDown.Parent);
                        numUpDown.BackColor = surface;
                        numUpDown.ForeColor = numUpDown.Enabled
                            ? ColorScheme.Blend(scheme.OnBackground, surface, 0.10f)
                            : scheme.OnDisabledControl;
                        numUpDown.BorderStyle = BorderStyle.None;
                        numUpDown.Font = UiNumericFont;
                        if (numUpDown.Controls.Count > 0)
                        {
                            var upDown = numUpDown.Controls[0];
                            upDown.Paint += UpDownButtons_Paint;
                            upDown.MouseMove += Control_InvalidateOnMouse;
                            upDown.MouseEnter += Control_InvalidateOnMouse;
                            upDown.MouseLeave += Control_InvalidateOnMouse;
                            upDown.MouseDown += Control_InvalidateOnMouseButton;
                            upDown.MouseUp += Control_InvalidateOnMouseButton;
                        }
                        break;
                    }
                    case ThemeableRichTextBox richTextBox:
                    {
                        var scheme = SafeScheme;
                        Color surface = ResolveContainerBackColor(richTextBox.Parent);
                        control.BackColor = richTextBox.ReadOnly ? surface : scheme.Field;
                        control.ForeColor = richTextBox.ReadOnly
                            ? ColorScheme.Blend(scheme.OnBackground, surface, 0.14f)
                            : scheme.OnField;
                        control.Font = UiRichNumericFont;
                        richTextBox.BorderStyle = BorderStyle.Fixed3D;
                        richTextBox.BorderColor = richTextBox.ReadOnly ? surface : scheme.ControlBorder;
                        richTextBox.ReadOnlyBackColor = surface;
                        richTextBox.ApplyInnerMargins(6, 6);
                        break;
                    }
                    case ThemeableComboBox comboBox:
                    {
                        var scheme = SafeScheme;
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                        comboBox.DrawMode = DrawMode.OwnerDrawFixed;
                        comboBox.IntegralHeight = false;
                        comboBox.FlatStyle = FlatStyle.Flat;
                        comboBox.BackColor = scheme.Control;
                        comboBox.ForeColor = scheme.OnControl;
                        comboBox.Font = UiRegularFont;
                        comboBox.BorderColor = scheme.ControlBorder;
                        comboBox.ButtonColor = scheme.Control;
                        comboBox.LockClosedHeight();
                        comboBox.ClearEditSelection();
                        comboBox.InvalidatePopover();
                        comboBox.WarmUpPopover();
                        comboBox.Invalidate();
                        break;
                    }
                    case ThemeableTextBox textBox:
                    {
                        var scheme = SafeScheme;
                        Color surface = ResolveContainerBackColor(textBox.Parent ?? textBox);
                        bool isUnavailable = textBox.IsUnavailableVisual || (!textBox.ReadOnly && (!textBox.Enabled || (!textBox.Focused && string.IsNullOrEmpty(textBox.Text))));
                        if (isUnavailable)
                        {
                            textBox.BackColor = scheme.DisabledControl;
                            textBox.ForeColor = scheme.OnDisabledControl;
                        }
                        else if (textBox.IsEditedVisual)
                        {
                            textBox.BackColor = scheme.EditedField;
                            textBox.ForeColor = scheme.OnEditedField;
                        }
                        else
                        {
                            textBox.BackColor = scheme.Field;
                            textBox.ForeColor = ColorScheme.Blend(scheme.OnBackground, surface, 0.10f);
                        }
                        textBox.Font = UiNumericFont;
                        if (textBox.TextAlign != HorizontalAlignment.Center)
                        {
                            textBox.TextAlign = HorizontalAlignment.Center;
                        }
                        textBox.BorderStyle = BorderStyle.Fixed3D;
                        textBox.BorderColor = scheme.ControlBorder;
                        textBox.ApplyInnerMargins(4, 4);
                        textBox.SyncSurfaceAppearance();
                        break;
                    }
                    case ComboBox _:
                    {
                        Console.WriteLine(
                            "Please replace all ComboBoxes with the ThemeambleComboBox, so theming can be applied"
                        );
                        break;
                    }
                    case TextBox plainTextBox:
                    {
                        var scheme = SafeScheme;
                        plainTextBox.BackColor = plainTextBox.ReadOnly
                            ? ResolveContainerBackColor(plainTextBox.Parent)
                            : (plainTextBox.Enabled ? scheme.Field : scheme.DisabledControl);
                        plainTextBox.ForeColor = plainTextBox.ReadOnly
                            ? scheme.OnBackground
                            : (plainTextBox.Enabled ? scheme.OnField : scheme.OnDisabledControl);
                        plainTextBox.Font = plainTextBox.ReadOnly ? UiRegularFont : UiNumericFont;
                        break;
                    }
                    case RichTextBox _:
                    {
                        Console.WriteLine(
                            "Please replace all RichTextBoxes with the ThemeambleRichTextBox, so theming can be correctly applied"
                        );

                        break;
                    }
                }

                if (control.HasChildren)
                {
                    ApplyTheme(control.Controls);
                }
            }
        }

        private static bool IsHeaderLabel(Control label)
        {
            switch (label.Name)
            {
                case "ActiveValueTitle":
                case "ActiveValueTitleY":
                case "DirectionalityActiveValueTitle":
                case "OptionSetXTitle":
                case "OptionSetYTitle":
                case "DirectionalityX":
                case "DirectionalityY":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsDescriptorLabel(Control label)
        {
            if (label.Name == "MouseLabel")
            {
                return false;
            }

            string name = label.Name ?? string.Empty;
            return name.Contains("Label") && !name.Contains("Active");
        }

        internal static Color ResolveContainerBackColor(Control container)
        {
            var scheme = SafeScheme;
            Control cur = container;
            while (cur != null)
            {
                if (cur.Name == "chartsPanel")
                {
                    return ResolveChartCanvasColor(scheme);
                }

                if (cur.Name == "optionsPanel" ||
                    cur.Name == "titleBarPanel" ||
                    cur.Name == "bottomDeckPanel" ||
                    cur.Name == "DirectionalityPanel" ||
                    cur.Name == "deviceListCard" ||
                    cur.Name == "deviceDetailCard" ||
                    cur.Name == "deviceIdPanel")
                {
                    return scheme.Surface;
                }

                if (cur is RawAcceleration)
                {
                    return ResolveChartCanvasColor(scheme);
                }

                if (cur is Form)
                {
                    return scheme.Background;
                }

                cur = cur.Parent;
            }

            return scheme.Surface;
        }

        private static Color ResolveLabelBackColor(Control label)
        {
            if (label.Name == "DirectionalityX" ||
                label.Name == "DirectionalityY" ||
                label.Name == "DirectionalityActiveValueTitle")
            {
                return Color.Transparent;
            }

            return ResolveContainerBackColor(label.Parent);
        }

        private static void Control_InvalidateOnMouse(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                control.Invalidate();
            }
        }

        private static void Control_InvalidateOnMouseButton(object sender, MouseEventArgs e)
        {
            if (sender is Control control)
            {
                control.Invalidate();
            }
        }

        private static void UpDownButtons_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Control ctrl))
            {
                return;
            }

            var scheme = SafeScheme;
            Color hostSurface = ResolveContainerBackColor(ctrl.Parent?.Parent ?? ctrl.Parent);
            var g = e.Graphics;
            g.Clear(hostSurface);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = ctrl.Width / 2f;
            float topCy = ctrl.Height * 0.28f;
            float botCy = ctrl.Height * 0.72f;

            var mousePos = ctrl.PointToClient(Cursor.Position);
            bool overCtrl = ctrl.Enabled && ctrl.ClientRectangle.Contains(mousePos);
            bool topHovered = overCtrl && mousePos.Y < ctrl.Height / 2;
            bool botHovered = overCtrl && mousePos.Y >= ctrl.Height / 2;

            Color idleGlyph = ctrl.Enabled
                ? ColorScheme.Blend(scheme.OnField, hostSurface, 0.20f)
                : scheme.OnDisabledControl;
            Color hoverGlyph = scheme.OnBackground;

            using (var topPen = new Pen(topHovered ? hoverGlyph : idleGlyph, 1.35f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            })
            using (var botPen = new Pen(botHovered ? hoverGlyph : idleGlyph, 1.35f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            })
            {
                g.DrawLines(topPen, new[] { new PointF(cx - 2.5f, topCy + 1.2f), new PointF(cx, topCy - 1.3f), new PointF(cx + 2.5f, topCy + 1.2f) });
                g.DrawLines(botPen, new[] { new PointF(cx - 2.5f, botCy - 1.2f), new PointF(cx, botCy + 1.3f), new PointF(cx + 2.5f, botCy - 1.2f) });
            }
        }

        private static void ChartsPanel_Paint(object sender, PaintEventArgs e)
        {
            // Keep Zone 2 chart workspace flush on CurrentScheme.Background with no separator lines.
        }

        private static void OptionsPanel_Paint(object sender, PaintEventArgs e)
        {
            if (!(sender is Panel panel) || panel.Width < 100)
            {
                return;
            }

            var g = e.Graphics;
            var scheme = SafeScheme;
            g.Clear(scheme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            float scrollY = panel.AutoScrollPosition.Y;
            Color hairlineCol = scheme.ControlBorder;
            var dirPanel = panel.Controls.ContainsKey("DirectionalityPanel")
                ? panel.Controls["DirectionalityPanel"]
                : null;
            float div2Y = dirPanel != null ? dirPanel.Top - 8f : (358f + scrollY);

            using (var hairlinePen = new Pen(hairlineCol, 1f))
            {
                // Flush full-height 1px vertical hairline separating Inspector from Zone 2 canvas
                g.DrawLine(hairlinePen, 0.5f, 0f, 0.5f, panel.Height);
                // Section hairlines: Sensitivity | Acceleration & LUT | Anisotropy
                g.DrawLine(hairlinePen, 1f, 140f + scrollY, panel.Width, 140f + scrollY);
                g.DrawLine(hairlinePen, 1f, div2Y, panel.Width, div2Y);
            }

            using (var headerBrush = new SolidBrush(scheme.OnBackground))
            using (var leftSf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            using (var centerSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("Sensitivity", UiHeaderFont, headerBrush, new RectangleF(16f, 12f + scrollY, 140f, 18f), leftSf);
                g.DrawString("Acceleration & LUT", UiHeaderFont, headerBrush, new RectangleF(16f, 148f + scrollY, 126f, 18f), leftSf);
                g.DrawString("Current", UiHeaderFont, headerBrush, new RectangleF(242f, 148f + scrollY, 76f, 18f), centerSf);
            }
        }

        internal static void DrawBentoCard(Graphics g, RectangleF rect, float radius, bool isDark)
        {
            var scheme = SafeScheme;
            using (var path = CreateRoundedRectPath(rect, radius))
            {
                using (var fill = new SolidBrush(scheme.Surface))
                {
                    g.FillPath(fill, path);
                }

                using (var border = new Pen(scheme.ControlBorder, 1f))
                {
                    g.DrawPath(border, path);
                }
            }
        }

        private static void Chart_PostPaint(object sender, ChartPaintEventArgs e)
        {
            if (!(sender is Chart chart) || !(e.ChartElement is ChartArea area))
            {
                return;
            }

            try
            {
                float plotLeft = (float)area.AxisX.ValueToPixelPosition(area.AxisX.ScaleView.ViewMinimum);
                float plotRight = (float)area.AxisX.ValueToPixelPosition(area.AxisX.ScaleView.ViewMaximum);
                float plotTop = (float)area.AxisY.ValueToPixelPosition(area.AxisY.ScaleView.ViewMaximum);
                float plotBottom = (float)area.AxisY.ValueToPixelPosition(area.AxisY.ScaleView.ViewMinimum);

                if (float.IsNaN(plotLeft) || float.IsNaN(plotRight) || float.IsNaN(plotTop) || float.IsNaN(plotBottom))
                {
                    return;
                }

                float minX = Math.Min(plotLeft, plotRight);
                float maxX = Math.Max(plotLeft, plotRight);
                float minY = Math.Min(plotTop, plotBottom);
                float maxY = Math.Max(plotTop, plotBottom);
                if (maxX - minX < 4f || maxY - minY < 4f)
                {
                    return;
                }

                var g = e.ChartGraphics.Graphics;
                var prevClip = g.Clip;
                var prevSmooth = g.SmoothingMode;
                g.SetClip(RectangleF.FromLTRB(minX, minY, maxX, maxY));
                g.SmoothingMode = SmoothingMode.AntiAlias;

                foreach (int seriesIndex in new[] { 0, 2 })
                {
                    if (chart.Series.Count <= seriesIndex) continue;
                    var series = chart.Series[seriesIndex];
                    if (!series.Enabled || series.Points.Count < 2) continue;

                    var pts = new List<PointF>(series.Points.Count);
                    foreach (DataPoint dp in series.Points)
                    {
                        if (dp.IsEmpty || dp.YValues == null || dp.YValues.Length == 0) continue;
                        float px = (float)area.AxisX.ValueToPixelPosition(dp.XValue);
                        float py = (float)area.AxisY.ValueToPixelPosition(dp.YValues[0]);
                        if (!float.IsNaN(px) && !float.IsInfinity(px) && !float.IsNaN(py) && !float.IsInfinity(py))
                        {
                            pts.Add(new PointF(px, py));
                        }
                    }

                    if (pts.Count >= 2)
                    {
                        if ((chart.Name == "GainChart" || chart.Name == "GainChartY") && pts.Count >= 32)
                        {
                            SmoothVisualGainY(pts, halfWindow: 4, passes: 2);
                        }

                        if (!area.AxisX.ScaleView.IsZoomed)
                        {
                            if (pts[0].X > minX + 0.5f)
                            {
                                pts.Insert(0, new PointF(minX, pts[0].Y));
                            }
                            if (pts[pts.Count - 1].X < maxX - 0.5f)
                            {
                                pts.Add(new PointF(maxX, pts[pts.Count - 1].Y));
                            }
                        }

                        var pointArray = pts.ToArray();
                        Color color = series.Color;

                        if (seriesIndex == 0)
                        {
                            float minCurveY = pointArray[0].Y;
                            for (int p = 1; p < pointArray.Length; p++)
                            {
                                if (pointArray[p].Y < minCurveY)
                                {
                                    minCurveY = pointArray[p].Y;
                                }
                            }

                            if (minCurveY < maxY - 1f)
                            {
                                using (var areaPath = new GraphicsPath())
                                {
                                    areaPath.AddLines(pointArray);
                                    areaPath.AddLine(pointArray[pointArray.Length - 1].X, maxY, pointArray[0].X, maxY);
                                    areaPath.CloseFigure();

                                    using (var grad = new LinearGradientBrush(
                                        new PointF(0f, minCurveY),
                                        new PointF(0f, maxY),
                                        Color.FromArgb(56, color),
                                        Color.FromArgb(0, color)))
                                    {
                                        g.FillPath(grad, areaPath);
                                    }
                                }
                            }
                        }

                        using (var outerGlow = new Pen(Color.FromArgb(24, color), 9f))
                        using (var innerGlow = new Pen(Color.FromArgb(55, color), 4.5f))
                        using (var corePen = new Pen(color, 2f))
                        {
                            outerGlow.StartCap = LineCap.Round;
                            outerGlow.EndCap = LineCap.Round;
                            outerGlow.LineJoin = LineJoin.Round;
                            innerGlow.StartCap = LineCap.Round;
                            innerGlow.EndCap = LineCap.Round;
                            innerGlow.LineJoin = LineJoin.Round;
                            corePen.StartCap = LineCap.Round;
                            corePen.EndCap = LineCap.Round;
                            corePen.LineJoin = LineJoin.Round;

                            g.DrawLines(outerGlow, pointArray);
                            g.DrawLines(innerGlow, pointArray);
                            g.DrawLines(corePen, pointArray);
                        }
                    }
                }

                g.ResetClip();
                foreach (int dotIndex in new[] { 1, 3 })
                {
                    if (chart.Series.Count <= dotIndex) continue;
                    var dotSeries = chart.Series[dotIndex];
                    if (!dotSeries.Enabled || dotSeries.Points.Count == 0) continue;

                    foreach (DataPoint dp in dotSeries.Points)
                    {
                        if (dp.IsEmpty || dp.YValues == null || dp.YValues.Length == 0) continue;
                        if (dp.XValue <= 1e-6 ||
                            (Math.Abs(dp.XValue) < 1e-9 && Math.Abs(dp.YValues[0]) < 1e-9) ||
                            (Math.Abs(dp.XValue - 0.01) < 1e-6 && Math.Abs(dp.YValues[0] - 0.01) < 1e-6))
                        {
                            continue;
                        }

                        float px = (float)area.AxisX.ValueToPixelPosition(dp.XValue);
                        float py = (float)area.AxisY.ValueToPixelPosition(dp.YValues[0]);
                        if (!float.IsNaN(px) && !float.IsInfinity(px) && !float.IsNaN(py) && !float.IsInfinity(py))
                        {
                            var scheme = SafeScheme;
                            Color coreCol = scheme.Background.GetBrightness() < 0.5f ? Color.White : scheme.Surface;
                            Color ringColor = dotIndex == 1 ? scheme.Primary : scheme.Secondary;
                            using (var coreBrush = new SolidBrush(coreCol))
                            using (var ringPen = new Pen(ringColor, 2f))
                            {
                                g.FillEllipse(coreBrush, px - 3.0f, py - 3.0f, 6.0f, 6.0f);
                                g.DrawEllipse(ringPen, px - 3.5f, py - 3.5f, 7.0f, 7.0f);
                            }
                        }
                    }
                }

                g.Clip = prevClip;
                g.SmoothingMode = prevSmooth;
            }
            catch
            {
                // Ignore transient layout states when axis bounds are not yet computed.
            }
        }

        private static void SmoothVisualGainY(List<PointF> pts, int halfWindow, int passes)
        {
            int count = pts.Count;
            var scratch = new float[count];
            for (int pass = 0; pass < passes; pass++)
            {
                scratch[0] = pts[0].Y;
                scratch[count - 1] = pts[count - 1].Y;
                for (int i = 1; i < count - 1; i++)
                {
                    float sum = 0f;
                    int lo = Math.Max(0, i - halfWindow);
                    int hi = Math.Min(count - 1, i + halfWindow);
                    int n = hi - lo + 1;
                    for (int j = lo; j <= hi; j++)
                    {
                        sum += pts[j].Y;
                    }
                    scratch[i] = sum / n;
                }

                for (int i = 1; i < count - 1; i++)
                {
                    pts[i] = new PointF(pts[i].X, scratch[i]);
                }
            }
        }

        private static void Chart_ResizeInlineLayout(object sender, EventArgs e)
        {
            if (sender is Chart chart)
            {
                ConfigureBalancedPlotArea(chart);
            }
        }

        private static void ConfigureBalancedPlotArea(Chart chart)
        {
            if (chart == null || chart.ChartAreas.Count == 0 || chart.Height < 48)
            {
                return;
            }

            var area = chart.ChartAreas[0];
            area.Position.Auto = false;
            area.Position.X = 1.0f;
            area.Position.Y = 3.0f;
            area.Position.Width = 97.5f;
            area.Position.Height = 95.0f;
            area.InnerPlotPosition.Auto = true;
        }

        private static void Label_DisabledPaint(object sender, PaintEventArgs e)
        {
            if (!(sender is Label label) || label.Enabled)
            {
                return;
            }

            var scheme = SafeScheme;
            Color back = ResolveLabelBackColor(label);
            if (back == Color.Transparent)
            {
                back = scheme.Surface;
            }

            e.Graphics.Clear(back);
            Color disabledFg = ColorScheme.Blend(scheme.OnField, back, 0.35f);

            var flags = System.Windows.Forms.TextFormatFlags.NoPrefix | System.Windows.Forms.TextFormatFlags.EndEllipsis;
            switch (label.TextAlign)
            {
                case ContentAlignment.TopLeft:
                    flags |= System.Windows.Forms.TextFormatFlags.Top | System.Windows.Forms.TextFormatFlags.Left;
                    break;
                case ContentAlignment.TopCenter:
                    flags |= System.Windows.Forms.TextFormatFlags.Top | System.Windows.Forms.TextFormatFlags.HorizontalCenter;
                    break;
                case ContentAlignment.TopRight:
                    flags |= System.Windows.Forms.TextFormatFlags.Top | System.Windows.Forms.TextFormatFlags.Right;
                    break;
                case ContentAlignment.MiddleCenter:
                    flags |= System.Windows.Forms.TextFormatFlags.VerticalCenter | System.Windows.Forms.TextFormatFlags.HorizontalCenter;
                    break;
                case ContentAlignment.MiddleRight:
                    flags |= System.Windows.Forms.TextFormatFlags.VerticalCenter | System.Windows.Forms.TextFormatFlags.Right;
                    break;
                case ContentAlignment.BottomLeft:
                    flags |= System.Windows.Forms.TextFormatFlags.Bottom | System.Windows.Forms.TextFormatFlags.Left;
                    break;
                case ContentAlignment.BottomCenter:
                    flags |= System.Windows.Forms.TextFormatFlags.Bottom | System.Windows.Forms.TextFormatFlags.HorizontalCenter;
                    break;
                case ContentAlignment.BottomRight:
                    flags |= System.Windows.Forms.TextFormatFlags.Bottom | System.Windows.Forms.TextFormatFlags.Right;
                    break;
                default:
                    flags |= System.Windows.Forms.TextFormatFlags.VerticalCenter | System.Windows.Forms.TextFormatFlags.Left;
                    break;
            }

            TextRenderer.DrawText(e.Graphics, label.Text, label.Font, label.ClientRectangle, disabledFg, flags);
        }

        private static bool IsPrimaryButton(Button button)
        {
            return button.Name == "writeButton" || button.Text == "Apply" || button.Text == "&OK";
        }

        private static bool IsResetButton(Button button)
        {
            return button.Name == "toggleButton" || button.Text == "Reset";
        }

        private static void RemoveEvents(Control.ControlCollection container)
        {
            foreach (Control control in container)
            {
                switch (control)
                {
                    case Panel panel when panel.Name == "optionsPanel":
                    {
                        panel.Paint -= OptionsPanel_Paint;
                        break;
                    }
                    case Panel panel when panel.Name == "chartsPanel":
                    {
                        panel.Paint -= ChartsPanel_Paint;
                        break;
                    }
                    case Label label:
                    {
                        label.Paint -= Label_DisabledPaint;
                        label.EnabledChanged -= Control_InvalidateOnMouse;
                        break;
                    }
                    case Chart chart:
                    {
                        chart.PostPaint -= Chart_PostPaint;
                        chart.Resize -= Chart_ResizeInlineLayout;
                        break;
                    }
                    case CheckBox checkBox:
                    {
                        checkBox.Paint -= CheckBox_Paint;
                        checkBox.EnabledChanged -= Control_InvalidateOnMouse;
                        checkBox.CheckedChanged -= Control_InvalidateOnMouse;
                        checkBox.MouseEnter -= Control_InvalidateOnMouse;
                        checkBox.MouseLeave -= Control_InvalidateOnMouse;
                        checkBox.MouseDown -= Control_InvalidateOnMouseButton;
                        checkBox.MouseUp -= Control_InvalidateOnMouseButton;
                        break;
                    }
                    case Button button:
                    {
                        button.Paint -= Button_Paint;
                        button.EnabledChanged -= Button_EnabledChanged;
                        button.MouseEnter -= Control_InvalidateOnMouse;
                        button.MouseLeave -= Control_InvalidateOnMouse;
                        button.MouseDown -= Control_InvalidateOnMouseButton;
                        button.MouseUp -= Control_InvalidateOnMouseButton;

                        break;
                    }
                    case NumericUpDown numUpDown:
                    {
                        if (numUpDown.Controls.Count > 0)
                        {
                            var upDown = numUpDown.Controls[0];
                            upDown.Paint -= UpDownButtons_Paint;
                            upDown.MouseMove -= Control_InvalidateOnMouse;
                            upDown.MouseEnter -= Control_InvalidateOnMouse;
                            upDown.MouseLeave -= Control_InvalidateOnMouse;
                            upDown.MouseDown -= Control_InvalidateOnMouseButton;
                            upDown.MouseUp -= Control_InvalidateOnMouseButton;
                        }
                        break;
                    }
                }

                if (control.HasChildren)
                {
                    RemoveEvents(control.Controls);
                }
            }
        }

        private static void Button_EnabledChanged(object sender, EventArgs e)
        {
            var scheme = SafeScheme;
            var button = (Button)sender;
            Color cardBack = button.Parent != null && button.Parent.BackColor != Color.Transparent
                ? button.Parent.BackColor
                : ResolveContainerBackColor(button.Parent);
            button.BackColor = cardBack;
            button.ForeColor = !button.Enabled
                ? scheme.OnDisabledControl
                : (IsPrimaryButton(button) ? scheme.OnPrimary : scheme.OnControl);
            button.Invalidate();
        }

        private static void Button_Paint(object sender, PaintEventArgs e)
        {
            var scheme = SafeScheme;
            var button = (Button)sender;
            var parentBack = button.Parent != null && button.Parent.BackColor != Color.Transparent
                ? button.Parent.BackColor
                : ResolveContainerBackColor(button.Parent);

            e.Graphics.Clear(parentBack);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool isPrimary = IsPrimaryButton(button);
            bool isReset = IsResetButton(button);
            bool isDark = scheme.Background.GetBrightness() < 0.5f;
            var bounds = new RectangleF(0.5f, 0.5f, button.Width - 1.5f, button.Height - 1.5f);

            using (var path = CreateRoundedRectPath(bounds, 6f))
            {
                if (!button.Enabled)
                {
                    using (var brush = new SolidBrush(scheme.DisabledControl))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    if (!isPrimary)
                    {
                        using (var pen = new Pen(scheme.ControlBorder, 1f))
                        {
                            e.Graphics.DrawPath(pen, path);
                        }
                    }

                    using (var disabledBrush = new SolidBrush(scheme.OnDisabledControl))
                    using (var disabledSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString(button.Text, button.Font, disabledBrush, button.ClientRectangle, disabledSf);
                    }
                    return;
                }

                var mousePos = button.PointToClient(Cursor.Position);
                var isHovering = button.ClientRectangle.Contains(mousePos);
                bool pressed = isHovering && (Control.MouseButtons & MouseButtons.Left) != 0;

                if (isPrimary)
                {
                    Color fillCol = scheme.Primary;
                    if (pressed)
                    {
                        fillCol = ControlPaint.Dark(scheme.Primary, 0.08f);
                    }
                    else if (isHovering)
                    {
                        fillCol = LightenColor(scheme.Primary, isDark ? 0.14f : 0.10f);
                    }

                    using (var brush = new SolidBrush(fillCol))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    using (var textBrush = new SolidBrush(scheme.OnPrimary))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString(button.Text, button.Font, textBrush, button.ClientRectangle, sf);
                    }
                }
                else if (isReset)
                {
                    Color fillCol = scheme.ButtonFace;
                    if (pressed)
                    {
                        fillCol = ColorScheme.Blend(scheme.ButtonFace, scheme.Field, 0.65f);
                    }
                    else if (isHovering)
                    {
                        fillCol = isDark
                            ? ColorScheme.Blend(scheme.ButtonFace, scheme.OnControl, 0.08f)
                            : ColorScheme.Blend(scheme.ButtonFace, scheme.OnControl, 0.06f);
                    }

                    using (var brush = new SolidBrush(fillCol))
                    using (var pen = new Pen(scheme.ButtonBorder, 1f))
                    {
                        e.Graphics.FillPath(brush, path);
                        e.Graphics.DrawPath(pen, path);
                    }

                    using (var textBrush = new SolidBrush(scheme.OnControl))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString(button.Text, button.Font, textBrush, button.ClientRectangle, sf);
                    }
                }
                else
                {
                    // All buttons except Apply and Reset use highlight-only text hover with static button surfaces.
                    Color fillCol = pressed
                        ? ColorScheme.Blend(scheme.ButtonFace, scheme.Field, 0.65f)
                        : scheme.ButtonFace;
                    using (var brush = new SolidBrush(fillCol))
                    using (var pen = new Pen(scheme.ButtonBorder, 1f))
                    {
                        e.Graphics.FillPath(brush, path);
                        e.Graphics.DrawPath(pen, path);
                    }

                    Color idleText = ColorScheme.Blend(scheme.OnControl, scheme.ButtonFace, 0.22f);
                    Color textColor = (isHovering || pressed) ? scheme.OnBackground : idleText;
                    using (var textBrush = new SolidBrush(textColor))
                    using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    {
                        e.Graphics.DrawString(button.Text, button.Font, textBrush, button.ClientRectangle, sf);
                    }
                }
            }
        }

        private static void CheckBox_Paint(object sender, PaintEventArgs e)
        {
            var scheme = SafeScheme;
            var checkBox = (CheckBox)sender;
            var fillBack = checkBox.Parent != null && checkBox.Parent.BackColor != Color.Transparent
                ? checkBox.Parent.BackColor
                : ResolveContainerBackColor(checkBox.Parent);

            e.Graphics.Clear(fillBack);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int boxSize = Math.Min(14, Math.Max(10, Math.Min(checkBox.Width - 2, checkBox.Height - 2)));
            float drawSize = boxSize - 1f;
            float boxY = Math.Max(1f, (checkBox.Height - drawSize) / 2f + 0.5f);
            var rect = new RectangleF(1.5f, boxY, drawSize, drawSize);

            var mousePos = checkBox.PointToClient(Cursor.Position);
            var isHovering = checkBox.Enabled && checkBox.ClientRectangle.Contains(mousePos);

            const int textSpacing = 6;
            int textOffset = boxSize + textSpacing + 2;
            if (!string.IsNullOrEmpty(checkBox.Text) && checkBox.Width > textOffset)
            {
                var textBounds = new Rectangle(textOffset, 0, checkBox.Width - textOffset, checkBox.Height);
                Color uncheckedText = ColorScheme.Blend(scheme.OnField, fillBack, 0.22f);
                Color textCol = !checkBox.Enabled
                    ? scheme.OnDisabledControl
                    : ((checkBox.Checked || isHovering) ? scheme.OnBackground : uncheckedText);
                TextRenderer.DrawText(e.Graphics, checkBox.Text, checkBox.Font, textBounds, textCol,
                    System.Windows.Forms.TextFormatFlags.Left | System.Windows.Forms.TextFormatFlags.VerticalCenter | System.Windows.Forms.TextFormatFlags.NoPrefix);
            }

            using (var path = CreateRoundedRectPath(rect, 4f))
            {
                if (checkBox.Checked)
                {
                    Color fillColor = !checkBox.Enabled
                        ? Color.FromArgb(120, scheme.CheckBoxChecked)
                        : scheme.CheckBoxChecked;
                    using (var brush = new SolidBrush(fillColor))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    using (var checkPen = new Pen(checkBox.Enabled ? scheme.OnPrimary : Color.FromArgb(190, scheme.OnPrimary), 1.8f))
                    {
                        checkPen.StartCap = LineCap.Round;
                        checkPen.EndCap = LineCap.Round;
                        checkPen.LineJoin = LineJoin.Round;

                        float s = rect.Width;
                        var pts = new[]
                        {
                            new PointF(rect.X + s * 0.22f, rect.Y + s * 0.52f),
                            new PointF(rect.X + s * 0.43f, rect.Y + s * 0.74f),
                            new PointF(rect.X + s * 0.80f, rect.Y + s * 0.28f)
                        };
                        e.Graphics.DrawLines(checkPen, pts);
                    }
                }
                else
                {
                    Color bg = !checkBox.Enabled
                        ? scheme.DisabledControl
                        : scheme.Field;
                    using (var brush = new SolidBrush(bg))
                    {
                        e.Graphics.FillPath(brush, path);
                    }

                    using (var pen = new Pen(scheme.ControlBorder, 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }

        internal static GraphicsPath CreateRoundedRectPath(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float diameter = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0.5f)
            {
                path.AddRectangle(bounds);
                return path;
            }

            var arc = new RectangleF(bounds.Location, new SizeF(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color LightenColor(Color color, float amount)
        {
            int r = Math.Min(255, (int)(color.R + (255 - color.R) * amount));
            int g = Math.Min(255, (int)(color.G + (255 - color.G) * amount));
            int b = Math.Min(255, (int)(color.B + (255 - color.B) * amount));
            return Color.FromArgb(color.A, r, g, b);
        }

        private static void SetChartColors(Chart chart)
        {
            var scheme = SafeScheme;
            chart.AntiAliasing = AntiAliasingStyles.All;
            chart.TextAntiAliasingQuality = TextAntiAliasingQuality.High;
            EnableDoubleBuffering(chart);

            Color chartBg = ResolveChartCanvasColor(scheme);
            Color axisCol = GetChartAxisColor(scheme);
            Color mutedFg = ColorScheme.Blend(scheme.ChartForeground, chartBg, 0.25f);
            Color gridCol = GetChartGridColor(scheme);

            chart.ForeColor = mutedFg;
            chart.BackColor = chartBg;
            chart.BorderlineWidth = 0;
            chart.BorderlineColor = Color.Transparent;

            if (chart.Titles.Count > 0)
            {
                chart.Titles[0].Visible = false;
                chart.Titles[0].ForeColor = mutedFg;
            }
            if (chart.Legends.Count > 0)
            {
                chart.Legends[0].Enabled = false;
                chart.Legends[0].ForeColor = mutedFg;
            }
            ConfigureBalancedPlotArea(chart);

            var area = chart.ChartAreas[0];
            area.AxisX.LabelStyle.ForeColor = mutedFg;
            area.AxisY.LabelStyle.ForeColor = mutedFg;

            area.AxisX.LineColor = axisCol;
            area.AxisY.LineColor = axisCol;
            area.AxisX.LineWidth = 1;
            area.AxisY.LineWidth = 1;
            area.AxisX2.Enabled = AxisEnabled.False;
            area.AxisY2.Enabled = AxisEnabled.False;

            area.AxisX.MajorTickMark.Enabled = true;
            area.AxisX.MajorTickMark.TickMarkStyle = TickMarkStyle.OutsideArea;
            area.AxisX.MajorTickMark.LineColor = Color.Transparent;
            area.AxisX.MajorTickMark.Size = 2.2f;
            area.AxisY.MajorTickMark.Enabled = true;
            area.AxisY.MajorTickMark.TickMarkStyle = TickMarkStyle.OutsideArea;
            area.AxisY.MajorTickMark.LineColor = Color.Transparent;
            area.AxisY.MajorTickMark.Size = 2.2f;
            area.AxisX.MinorTickMark.Enabled = false;
            area.AxisY.MinorTickMark.Enabled = false;
            bool isVelocity = chart.Name == "VelocityChart" || chart.Name == "VelocityChartY";
            area.AxisY.LabelStyle.Format = isVelocity ? "0.##" : "0.00";

            area.AxisX.MajorGrid.LineColor = gridCol;
            area.AxisY.MajorGrid.LineColor = gridCol;

            area.AxisX.MinorGrid.Enabled = false;
            area.AxisY.MinorGrid.Enabled = false;

            area.AxisX.TitleForeColor = mutedFg;
            area.AxisY.TitleForeColor = mutedFg;

            area.BorderWidth = 0;
            area.BorderColor = Color.Transparent;
            area.BackColor = chartBg;

            bool isDark = chartBg.GetBrightness() < 0.5f;
            foreach (var axis in new[] { area.AxisX, area.AxisY })
            {
                axis.ScrollBar.Enabled = true;
                axis.ScrollBar.IsPositionedInside = false;
                axis.ScrollBar.Size = 10;
                axis.ScrollBar.ButtonStyle = ScrollBarButtonStyles.SmallScroll;
                axis.ScrollBar.BackColor = chartBg;
                axis.ScrollBar.ButtonColor = scheme.Control;
                axis.ScrollBar.LineColor = scheme.ControlBorder;
            }

            Color cursorLine = Color.FromArgb(140, scheme.Primary);
            Color cursorSel = Color.FromArgb(isDark ? 44 : 32, scheme.Primary);
            area.CursorX.LineColor = cursorLine;
            area.CursorY.LineColor = cursorLine;
            area.CursorX.SelectionColor = cursorSel;
            area.CursorY.SelectionColor = cursorSel;
        }
    }
}