using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace grapher.Models.Theming.Controls
{
    /// <summary>
    /// A ComboBox which has properties for the Border and Button color, making it look far better when applying a theme.
    /// Code is largely copied over from this StackOverflow Answer; https://stackoverflow.com/a/65976649/2721113
    /// and was then reworked a bit.
    /// </summary>
    public class ThemeableComboBox : ComboBox
    {
        private const int WM_PAINT = 0xF;
        private const int WM_ERASEBKGND = 0x14;
        private const int WM_CTLCOLOREDIT = 0x0133;
        private const int WM_CTLCOLORLISTBOX = 0x0134;
        private const int WM_CTLCOLORSTATIC = 0x0138;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;
        private const int OCM_DRAWITEM = 0x202B;
        private const uint ODS_COMBOBOXEDIT = 0x1000;
        private const int CB_SETITEMHEIGHT = 0x0153;
        private const int CB_SETEDITSEL = 0x0142;
        private const int NULL_BRUSH = 5;

        private static readonly ConditionalWeakTable<Control, ParentCtlColorHook> ParentHooks =
            new ConditionalWeakTable<Control, ParentCtlColorHook>();

        private bool _isHovered;
        private bool _isPopoverShowing;
        private bool _closedByClickOnSelf;
        private ToolStripDropDownMenu _activePopover;
        private ToolStripDropDownMenu _cachedPopover;
        private int _lastPopoverClosedTick;

        private sealed class ParentCtlColorHook : NativeWindow
        {
            private readonly Control _parent;

            public ParentCtlColorHook(Control parent)
            {
                _parent = parent;
                parent.Disposed += (s, e) => ReleaseHandle();
                parent.HandleDestroyed += (s, e) => ReleaseHandle();
                parent.HandleCreated += (s, e) => AttachIfNeeded();
                AttachIfNeeded();
            }

            public void AttachIfNeeded()
            {
                if (_parent != null && !_parent.IsDisposed && _parent.IsHandleCreated && Handle != _parent.Handle)
                {
                    if (Handle != IntPtr.Zero)
                    {
                        ReleaseHandle();
                    }
                    AssignHandle(_parent.Handle);
                }
            }

            protected override void WndProc(ref Message m)
            {
                if ((m.Msg == WM_CTLCOLOREDIT || m.Msg == WM_CTLCOLORLISTBOX || m.Msg == WM_CTLCOLORSTATIC) &&
                    m.LParam != IntPtr.Zero &&
                    Control.FromHandle(m.LParam) is ThemeableComboBox combo &&
                    combo.DropDownStyle == ComboBoxStyle.DropDownList)
                {
                    // Prevent native Win32 COMBOBOX focus/killfocus handlers from erasing the live closed DC.
                    m.Result = GetStockObject(NULL_BRUSH);
                    return;
                }

                base.WndProc(ref m);
            }
        }

        private void EnsureParentCtlColorHook()
        {
            var parent = Parent;
            if (parent == null || parent.IsDisposed)
            {
                return;
            }

            if (!ParentHooks.TryGetValue(parent, out var hook))
            {
                hook = new ParentCtlColorHook(parent);
                ParentHooks.Add(parent, hook);
            }
            else
            {
                hook.AttachIfNeeded();
            }
        }

        public ThemeableComboBox()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            DrawMode = DrawMode.OwnerDrawFixed;
            IntegralHeight = false;
            FlatStyle = FlatStyle.Flat;
        }

        private Color _borderColor = ColorScheme.MacOSDarkTheme.ControlBorder;

        [DefaultValue(typeof(Color), "#262833")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor == value) return;
                _borderColor = value;
                Invalidate();
            }
        }

        private Color _buttonColor = ColorScheme.MacOSDarkTheme.Control;

        [DefaultValue(typeof(Color), "#1A1B21")]
        public Color ButtonColor
        {
            get => _buttonColor;
            set
            {
                if (_buttonColor == value) return;
                _buttonColor = value;
                Invalidate();
            }
        }

        internal bool IsPopoverOpen =>
            _isPopoverShowing || (_activePopover != null && !_activePopover.IsDisposed && _activePopover.Visible);

        public void ClearEditSelection()
        {
            if (DropDownStyle == ComboBoxStyle.DropDownList)
            {
                return;
            }
            if (IsHandleCreated)
            {
                SendMessage(Handle, CB_SETEDITSEL, 0, (IntPtr)(-1));
            }
            SelectionLength = 0;
        }

        public void LockClosedHeight()
        {
            if (IsDisposed || Disposing) return;
            if (!IsHandleCreated)
            {
                IntPtr unused = Handle;
            }
            // Lock closed control height to 30px (24 + 6) once without touching dropdown sizing.
            SendMessage(Handle, CB_SETITEMHEIGHT, -1, (IntPtr)24);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            EnsureParentCtlColorHook();
            LockClosedHeight();
            ClearEditSelection();
            if (Items.Count > 0 && Width > 10)
            {
                WarmUpPopover();
            }
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            EnsureParentCtlColorHook();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && IsHandleCreated && Items.Count > 0 && Width > 10)
            {
                WarmUpPopover();
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            LockClosedHeight();
            InvalidatePopover();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClearEditSelection();
            if (IsHandleCreated && Items.Count > 0 && Width > 10)
            {
                WarmUpPopover();
            }
            else
            {
                InvalidatePopover();
            }
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            ClearEditSelection();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            ClearEditSelection();
            PaintClosedToWindowDc();
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            ClearEditSelection();
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            ClearEditSelection();
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Enabled &&
                DropDownStyle != ComboBoxStyle.Simple &&
                (e.KeyCode == Keys.Space ||
                 e.KeyCode == Keys.F4 ||
                 (e.Alt && (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up))))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                TogglePopover();
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnDropDown(EventArgs e)
        {
            base.OnDropDown(e);
            ApplyRoundedDropDownList();
            Invalidate();
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            ClearEditSelection();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs h)
            {
                h.Handled = true;
            }
        }

        public void TogglePopover()
        {
            if (IsPopoverOpen)
            {
                _activePopover.Close();
                return;
            }

            ShowCustomPopover();
        }

        public void InvalidatePopover()
        {
            if (_cachedPopover != null && !_cachedPopover.Visible)
            {
                _cachedPopover.Dispose();
                _cachedPopover = null;
            }
        }

        public void WarmUpPopover()
        {
            if (IsDisposed || Disposing || !IsHandleCreated || Items.Count == 0 || Width <= 10)
            {
                return;
            }

            EnsurePopoverReady();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _activePopover = null;
                if (_cachedPopover != null)
                {
                    _cachedPopover.Dispose();
                    _cachedPopover = null;
                }
            }
            base.Dispose(disposing);
        }

        private bool MatchesCachedPopover(ToolStripDropDownMenu pop, int expectedW, int expectedH)
        {
            if (pop == null || pop.IsDisposed || pop.Items.Count != Items.Count)
            {
                return false;
            }

            if (pop.Width != expectedW || pop.Height != expectedH)
            {
                return false;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                if (!string.Equals(pop.Items[i].Text, GetItemText(Items[i]), StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private sealed class StyledComboPopover : ToolStripDropDownMenu
        {
            private const int CS_DROPSHADOW = 0x00020000;

            public StyledComboPopover()
            {
                SetStyle(
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint,
                    true);
                UpdateStyles();
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    if (Theme.SupportsDwmRoundedCorners)
                    {
                        cp.ClassStyle &= ~CS_DROPSHADOW;
                    }
                    return cp;
                }
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                Region = null;
                Theme.ApplyDropDownDwm(this);
            }

            protected override void OnOpening(CancelEventArgs e)
            {
                base.OnOpening(e);
                Region = null;
                Theme.ApplyDropDownDwm(this);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_MOUSEWHEEL || m.Msg == WM_MOUSEHWHEEL)
                {
                    m.Result = IntPtr.Zero;
                    return;
                }

                base.WndProc(ref m);
            }
        }

        private ToolStripDropDownMenu EnsurePopoverReady()
        {
            var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
            const int rowH = 28;
            int popW = Math.Max(80, Width);
            int padV = 8;
            int popH = Math.Min(360, (Items.Count * rowH) + padV);

            if (MatchesCachedPopover(_cachedPopover, popW, popH))
            {
                _cachedPopover.Renderer = Theme.SharedMenuRenderer;
                _cachedPopover.BackColor = scheme.MenuBackground;
                _cachedPopover.ForeColor = scheme.OnField;
                _cachedPopover.Font = Font;
                _cachedPopover.DropShadowEnabled = !Theme.SupportsDwmRoundedCorners;
                for (int i = 0; i < _cachedPopover.Items.Count; i++)
                {
                    if (_cachedPopover.Items[i] is ToolStripMenuItem mi)
                    {
                        mi.Checked = i == SelectedIndex;
                        mi.Font = Font;
                        mi.Size = new Size(popW, rowH);
                    }
                }
                if (SelectedIndex >= 0 && SelectedIndex < _cachedPopover.Items.Count)
                {
                    _cachedPopover.Items[SelectedIndex].Select();
                }
                Theme.ApplyDropDownDwm(_cachedPopover);
                return _cachedPopover;
            }

            if (_cachedPopover != null && !_cachedPopover.Visible)
            {
                _cachedPopover.Dispose();
                _cachedPopover = null;
            }

            var pop = new StyledComboPopover
            {
                Tag = "ComboPopover",
                ShowImageMargin = false,
                ShowCheckMargin = false,
                AutoClose = true,
                DropShadowEnabled = !Theme.SupportsDwmRoundedCorners,
                AutoSize = false,
                Padding = new Padding(0, 4, 0, 4),
                Margin = Padding.Empty,
                BackColor = scheme.MenuBackground,
                ForeColor = scheme.OnField,
                Font = Font,
                Renderer = Theme.SharedMenuRenderer
            };

            pop.SuspendLayout();
            pop.MinimumSize = new Size(popW, popH);
            pop.MaximumSize = new Size(popW, popH);
            pop.Size = new Size(popW, popH);

            for (int i = 0; i < Items.Count; i++)
            {
                int index = i;
                bool isChecked = i == SelectedIndex;
                var mi = new ToolStripMenuItem(GetItemText(Items[i]))
                {
                    AutoSize = false,
                    Size = new Size(popW, rowH),
                    Margin = Padding.Empty,
                    Padding = Padding.Empty,
                    Checked = isChecked,
                    Font = Font
                };
                mi.Click += (s, ev) =>
                {
                    if (SelectedIndex != index)
                    {
                        SelectedIndex = index;
                    }
                    Invalidate();
                };
                pop.Items.Add(mi);
            }

            pop.Opening += (s, ev) =>
            {
                for (int i = 0; i < pop.Items.Count; i++)
                {
                    if (pop.Items[i] is ToolStripMenuItem mi)
                    {
                        mi.Checked = i == SelectedIndex;
                    }
                }
                if (SelectedIndex >= 0 && SelectedIndex < pop.Items.Count)
                {
                    pop.Items[SelectedIndex].Select();
                }
            };

            pop.Opened += (s, ev) =>
            {
                PaintClosedToWindowDc();
                Invalidate();
            };

            pop.Closed += (s, ev) =>
            {
                _closedByClickOnSelf = ev.CloseReason == ToolStripDropDownCloseReason.AppClicked &&
                                       IsHandleCreated &&
                                       ClientRectangle.Contains(PointToClient(Cursor.Position));
                _lastPopoverClosedTick = Environment.TickCount;
                _isPopoverShowing = false;
                if (ReferenceEquals(_activePopover, pop))
                {
                    _activePopover = null;
                }
                PaintClosedToWindowDc();
                Invalidate();
            };

            pop.ResumeLayout(true);
            pop.PerformLayout();
            if (!pop.IsHandleCreated)
            {
                IntPtr unused = pop.Handle;
            }
            if (SelectedIndex >= 0 && SelectedIndex < pop.Items.Count)
            {
                pop.Items[SelectedIndex].Select();
            }
            Theme.ApplyDropDownDwm(pop);

            _cachedPopover = pop;
            return pop;
        }

        public ToolStripDropDownMenu ShowCustomPopover()
        {
            if (!Enabled || Items.Count == 0 || !IsHandleCreated)
            {
                return null;
            }

            var pop = EnsurePopoverReady();
            var workArea = Screen.FromControl(this).WorkingArea;
            Point below = PointToScreen(new Point(0, Height + 4));
            Point showPt = (below.Y + pop.Height <= workArea.Bottom)
                ? below
                : PointToScreen(new Point(0, -pop.Height - 4));

            _activePopover = pop;
            _isPopoverShowing = true;
            PaintClosedToWindowDc();
            pop.Show(showPt);
            if (!pop.Visible)
            {
                _isPopoverShowing = false;
                if (ReferenceEquals(_activePopover, pop))
                {
                    _activePopover = null;
                }
                PaintClosedToWindowDc();
            }
            return pop;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            // Fallback custom item painting strictly using the active ColorScheme tokens (never #0078D7).
            if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0)
            {
                return;
            }

            var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
            Color closedFill = Enabled ? scheme.Control : scheme.DisabledControl;
            Color listFill = scheme.MenuBackground;
            Color textMuted = ColorScheme.Blend(scheme.OnField, scheme.MenuBackground, 0.36f);
            Color textPrimary = scheme.OnBackground;
            bool isDark = scheme.Background.GetBrightness() < 0.5f;
            Color activePill = Color.FromArgb(isDark ? 34 : 24, scheme.Primary);

            int w = e.Bounds.Width;
            int h = e.Bounds.Height;
            bool isEditArea = (e.State & DrawItemState.ComboBoxEdit) != 0;

            using (var bmp = new Bitmap(w, h))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(isEditArea ? closedFill : listFill);

                if (e.Index >= 0 && e.Index < Items.Count)
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    string text = GetItemText(Items[e.Index]);

                    if (isEditArea)
                    {
                        Color baseText = ColorScheme.Blend(scheme.OnControl, scheme.Control, 0.14f);
                        Color editTextCol = Enabled
                            ? ((_isHovered || IsPopoverOpen) ? scheme.OnBackground : baseText)
                            : scheme.OnDisabledControl;
                        var editRect = new RectangleF(10f, 0f, Math.Max(1f, w - 12f), h);
                        using (var sf = new StringFormat(StringFormatFlags.NoWrap)
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter,
                            HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None
                        })
                        using (var textBrush = new SolidBrush(editTextCol))
                        {
                            g.DrawString(text, Font, textBrush, editRect, sf);
                        }
                    }
                    else
                    {
                        bool isSelected = (e.State & DrawItemState.Selected) != 0;
                        bool isChosen = e.Index == SelectedIndex;

                        if (isSelected)
                        {
                            var pillRect = new RectangleF(4f, 1.5f, w - 8f, h - 3f);
                            using (var pillPath = Theme.CreateRoundedRectPath(pillRect, 5f))
                            using (var pillBrush = new SolidBrush(activePill))
                            {
                                g.FillPath(pillBrush, pillPath);
                            }
                        }

                        if (isChosen)
                        {
                            float cx = w - 14f;
                            float cy = h / 2f;
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

                        Color itemTextColor = (isSelected || isChosen) ? textPrimary : textMuted;
                        float rightReserve = isChosen ? 24f : 10f;
                        var itemTextRect = new RectangleF(12f, 0.5f, Math.Max(1f, w - 12f - rightReserve), h - 1f);
                        using (var sf = new StringFormat(StringFormatFlags.NoWrap)
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Center,
                            Trimming = StringTrimming.EllipsisCharacter,
                            HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None
                        })
                        using (var textBrush = new SolidBrush(itemTextColor))
                        {
                            g.DrawString(text, Font, textBrush, itemTextRect, sf);
                        }
                    }
                }

                e.Graphics.DrawImageUnscaled(bmp, e.Bounds.X, e.Bounds.Y);
            }
        }

        private void ApplyRoundedDropDownList()
        {
            if (!IsHandleCreated) return;
            try
            {
                var info = new COMBOBOXINFO();
                info.cbSize = Marshal.SizeOf(info);
                if (GetComboBoxInfo(Handle, ref info) && info.hwndList != IntPtr.Zero)
                {
                    var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
                    int useDark = scheme.Background.GetBrightness() < 0.5f ? 1 : 0;
                    DwmSetWindowAttribute(info.hwndList, 20, ref useDark, sizeof(int));
                    int cornerPref = 2; // DWMWCP_ROUND
                    DwmSetWindowAttribute(info.hwndList, 33, ref cornerPref, sizeof(int));
                    int win32Border = ColorTranslator.ToWin32(scheme.ControlBorder);
                    DwmSetWindowAttribute(info.hwndList, 34, ref win32Border, sizeof(int));
                }
            }
            catch
            {
                // Ignore on older OS versions.
            }
        }

        private void PaintClosedToWindowDc()
        {
            if (!IsHandleCreated || !Visible || IsDisposed || Disposing || Width <= 0 || Height <= 0 || DropDownStyle == ComboBoxStyle.Simple)
            {
                return;
            }

            IntPtr dc = GetDC(Handle);
            if (dc == IntPtr.Zero)
            {
                return;
            }

            try
            {
                PaintClosedToHdc(dc);
            }
            finally
            {
                ReleaseDC(Handle, dc);
            }
        }

        private void PaintClosedToHdc(IntPtr dc)
        {
            if (dc == IntPtr.Zero)
            {
                return;
            }

            int w = Math.Max(1, Width);
            int h = Math.Max(1, Height);
            using (var buffer = new Bitmap(w, h))
            using (var g = Graphics.FromImage(buffer))
            {
                var scheme = Theme.CurrentScheme ?? ColorScheme.MacOSDarkTheme;
                Color parentBack = Parent != null && Parent.BackColor != Color.Transparent
                    ? Parent.BackColor
                    : Theme.ResolveContainerBackColor(this);

                g.Clear(parentBack);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // Keep closed fill & border static on mouse-over (highlight-only text/glyph hover).
                // Highlight border with accent Primary only while the popover itself is open.
                Color fillCol = Enabled ? scheme.Control : scheme.DisabledControl;
                Color borderCol = !Enabled
                    ? scheme.DisabledControl
                    : (IsPopoverOpen ? scheme.Primary : scheme.ControlBorder);

                var boxRect = new RectangleF(0.5f, 0.5f, w - 1.5f, h - 1.5f);
                using (var path = Theme.CreateRoundedRectPath(boxRect, 6f))
                using (var fillBrush = new SolidBrush(fillCol))
                using (var borderPen = new Pen(borderCol, 1f))
                {
                    g.FillPath(fillBrush, path);
                    g.DrawPath(borderPen, path);
                }

                int chevronAreaW = 22;
                string displayText = SelectedIndex >= 0 && SelectedIndex < Items.Count
                    ? GetItemText(Items[SelectedIndex])
                    : Text;
                Color idleTextCol = ColorScheme.Blend(scheme.OnControl, scheme.Control, 0.16f);
                Color textCol = !Enabled
                    ? scheme.OnDisabledControl
                    : ((_isHovered || IsPopoverOpen) ? scheme.OnBackground : idleTextCol);

                var textRect = new RectangleF(10f, 0.5f, Math.Max(1f, w - chevronAreaW - 12f), h - 1f);
                using (var sf = new StringFormat(StringFormatFlags.NoWrap)
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None
                })
                using (var textBrush = new SolidBrush(textCol))
                {
                    g.DrawString(displayText, Font, textBrush, textRect, sf);
                }

                float cx = w - 12f;
                float cy = h / 2f;
                var chevron = new[]
                {
                    new PointF(cx - 3.2f, cy - 1.5f),
                    new PointF(cx, cy + 1.8f),
                    new PointF(cx + 3.2f, cy - 1.5f)
                };
                Color idleChevCol = ColorScheme.Blend(scheme.OnControl, scheme.Control, 0.42f);
                Color chevCol = !Enabled
                    ? scheme.OnDisabledControl
                    : (IsPopoverOpen ? scheme.Primary : (_isHovered ? scheme.OnBackground : idleChevCol));
                using (var p = new Pen(chevCol, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    g.DrawLines(p, chevron);
                }

                using (var dst = Graphics.FromHdc(dc))
                {
                    dst.DrawImageUnscaled(buffer, 0, 0);
                }
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == WM_MOUSEWHEEL || message.Msg == WM_MOUSEHWHEEL)
            {
                if (message.Msg == WM_MOUSEWHEEL && !IsPopoverOpen)
                {
                    Control ancestor = Parent;
                    while (ancestor != null)
                    {
                        if (ancestor is ScrollableControl sc && sc.AutoScroll && sc.IsHandleCreated)
                        {
                            SendMessage(sc.Handle, message.Msg, message.WParam, message.LParam);
                            break;
                        }
                        ancestor = ancestor.Parent;
                    }
                }

                message.Result = IntPtr.Zero;
                return;
            }

            if (message.Msg == WM_ERASEBKGND)
            {
                message.Result = (IntPtr)1;
                return;
            }

            if (message.Msg == OCM_DRAWITEM &&
                DropDownStyle == ComboBoxStyle.DropDownList &&
                message.LParam != IntPtr.Zero)
            {
                var dis = Marshal.PtrToStructure<DRAWITEMSTRUCT>(message.LParam);
                if ((dis.itemState & ODS_COMBOBOXEDIT) != 0)
                {
                    PaintClosedToWindowDc();
                    message.Result = (IntPtr)1;
                    return;
                }
            }

            if ((message.Msg == WM_LBUTTONDOWN || message.Msg == WM_LBUTTONDBLCLK) &&
                DropDownStyle != ComboBoxStyle.Simple &&
                Enabled)
            {
                bool justClosedByTriggerClick = _closedByClickOnSelf &&
                                                unchecked(Environment.TickCount - _lastPopoverClosedTick) >= 0 &&
                                                unchecked(Environment.TickCount - _lastPopoverClosedTick) < 180;
                _closedByClickOnSelf = false;
                Focus();
                ClearEditSelection();
                if (!justClosedByTriggerClick)
                {
                    TogglePopover();
                }
                else
                {
                    PaintClosedToWindowDc();
                }

                message.Result = IntPtr.Zero;
                return;
            }

            if (message.Msg != WM_PAINT || DropDownStyle == ComboBoxStyle.Simple)
            {
                base.WndProc(ref message);
                return;
            }

            var ps = new PAINTSTRUCT();
            IntPtr dc = BeginPaint(Handle, ref ps);
            try
            {
                PaintClosedToHdc(dc);
            }
            finally
            {
                EndPaint(Handle, ref ps);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DRAWITEMSTRUCT
        {
            public uint CtlType;
            public uint CtlID;
            public uint itemID;
            public uint itemAction;
            public uint itemState;
            public IntPtr hwndItem;
            public IntPtr hDC;
            public RECT rcItem;
            public IntPtr itemData;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int L, T, R, B;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public int rcPaint_left;
            public int rcPaint_top;
            public int rcPaint_right;
            public int rcPaint_bottom;
            public bool fRestore;
            public bool fIncUpdate;
            public int reserved1;
            public int reserved2;
            public int reserved3;
            public int reserved4;
            public int reserved5;
            public int reserved6;
            public int reserved7;
            public int reserved8;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd,
            [In, Out] ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int fnObject);

        [DllImport("gdi32.dll")]
        public static extern int SelectClipRgn(IntPtr hDC, IntPtr hRgn);

        [DllImport("user32.dll")]
        public static extern int GetUpdateRgn(IntPtr hwnd, IntPtr hrgn, bool fErase);

        public enum RegionFlags
        {
            ERROR = 0,
            NULLREGION = 1,
            SIMPLEREGION = 2,
            COMPLEXREGION = 3,
        }

        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct COMBOBOXINFO
        {
            public int cbSize;
            public RECT rcItem;
            public RECT rcButton;
            public int stateButton;
            public IntPtr hwndCombo;
            public IntPtr hwndEdit;
            public IntPtr hwndList;
        }

        [DllImport("user32.dll")]
        private static extern bool GetComboBoxInfo(IntPtr hwndCombo, ref COMBOBOXINFO info);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);
    }
}