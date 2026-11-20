using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace grapher.Models.Theming.Controls
{
    /// <summary>
    /// A TextBox which has a customizable BorderColor, making it more theme friendly.
    /// Code is copied over from this StackOverflow answer; https://stackoverflow.com/a/39420512/2721113
    /// and was then reworked a bit.
    /// </summary>
    public class ThemeableTextBox : TextBox
    {
        private const int WM_NCPAINT = 0x85;
        private const uint RDW_INVALIDATE = 0x1;
        private const uint RDW_IUPDATENOW = 0x100;
        private const uint RDW_FRAME = 0x400;

        private Color _borderColor = Color.Gray;
        private bool _isHovered;
        private bool _isEditedVisual;
        private bool _isUnavailableVisual;

        public ThemeableTextBox()
        {
            TextAlign = HorizontalAlignment.Center;
        }

        internal bool IsEditedVisual => _isEditedVisual;

        internal bool IsUnavailableVisual =>
            _isUnavailableVisual || !Enabled || (!ReadOnly && !Focused && string.IsNullOrEmpty(Text));

        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                var scheme = Theme.CurrentScheme;
                if (scheme != null)
                {
                    int argb = value.ToArgb();
                    if (argb == scheme.EditedField.ToArgb())
                    {
                        _isEditedVisual = true;
                        _isUnavailableVisual = false;
                    }
                    else if (argb == scheme.DisabledControl.ToArgb())
                    {
                        _isUnavailableVisual = true;
                        _isEditedVisual = false;
                    }
                    else if (argb == scheme.Field.ToArgb())
                    {
                        _isEditedVisual = false;
                        _isUnavailableVisual = false;
                    }
                }

                Color flushBack = Theme.ResolveContainerBackColor(Parent ?? this);
                if (base.BackColor.ToArgb() != flushBack.ToArgb())
                {
                    base.BackColor = flushBack;
                }
                RedrawFrame();
            }
        }

        [DefaultValue(typeof(Color), "Gray")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                if (_borderColor == value) return;
                _borderColor = value;
                RedrawFrame();
            }
        }

        public void SyncSurfaceAppearance()
        {
            Color flushBack = Theme.ResolveContainerBackColor(Parent ?? this);
            if (base.BackColor.ToArgb() != flushBack.ToArgb())
            {
                base.BackColor = flushBack;
            }
            if (!ReadOnly && (!Enabled || (!Focused && string.IsNullOrEmpty(Text))))
            {
                _isUnavailableVisual = true;
                _isEditedVisual = false;
            }
            RedrawFrame();
        }

        private void RedrawFrame()
        {
            Invalidate();
            if (IsHandleCreated)
            {
                RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
            }
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg != WM_NCPAINT || BorderColor == Color.Transparent || BorderStyle != BorderStyle.Fixed3D)
            {
                base.WndProc(ref message);
                return;
            }

            var hdc = GetWindowDC(Handle);
            using (var graphics = Graphics.FromHdcInternal(hdc))
            {
                graphics.SmoothingMode = SmoothingMode.Default;
                var scheme = Theme.CurrentScheme;
                Color surround = Theme.ResolveContainerBackColor(Parent ?? this);

                int insetX = Math.Max(1, (Width - ClientSize.Width) / 2);
                int insetY = Math.Max(1, (Height - ClientSize.Height) / 2);
                var clientRect = new Rectangle(insetX, insetY, Math.Max(0, ClientSize.Width), Math.Max(0, ClientSize.Height));

                using (var ringRegion = new Region(new Rectangle(0, 0, Width, Height)))
                {
                    ringRegion.Exclude(clientRect);
                    using (var surroundBrush = new SolidBrush(surround))
                    {
                        graphics.FillRegion(surroundBrush, ringRegion);
                    }
                }

                bool unavailable = IsUnavailableVisual;
                bool focused = Focused && Enabled && !ReadOnly && !unavailable;
                bool hovered = _isHovered && Enabled && !ReadOnly && !unavailable;

                Color lineColor;
                int lineHeight = 1;
                if (focused)
                {
                    lineColor = scheme?.Primary ?? Color.FromArgb(99, 102, 241);
                    lineHeight = 2;
                }
                else if (hovered)
                {
                    lineColor = scheme != null
                        ? ColorScheme.Blend(scheme.Primary, scheme.OnBackground, 0.35f)
                        : BorderColor;
                }
                else if (unavailable)
                {
                    lineColor = scheme != null
                        ? ColorScheme.Blend(scheme.ControlBorder, surround, 0.55f)
                        : BorderColor;
                }
                else if (_isEditedVisual)
                {
                    lineColor = scheme != null
                        ? ColorScheme.Blend(scheme.Primary, scheme.ControlBorder, 0.45f)
                        : BorderColor;
                }
                else
                {
                    lineColor = scheme != null
                        ? ColorScheme.Blend(scheme.ControlBorder, scheme.OnField, 0.25f)
                        : BorderColor;
                }

                int maxLineW = Math.Max(1, Width - 4);
                int textW = string.IsNullOrEmpty(Text)
                    ? 0
                    : TextRenderer.MeasureText(
                        Text,
                        Font,
                        new Size(int.MaxValue, Height),
                        TextFormatFlags.NoPadding).Width;
                int lineW = Math.Min(maxLineW, Math.Max(22, textW + 8));
                int lineX = (Width - lineW) / 2;
                int lineY = Math.Max(0, Height - lineHeight);
                using (var lineBrush = new SolidBrush(lineColor))
                {
                    graphics.FillRectangle(lineBrush, lineX, lineY, lineW, lineHeight);
                }
            }

            ReleaseDC(Handle, hdc);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            if (!_isHovered)
            {
                _isHovered = true;
                RedrawFrame();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_isHovered)
            {
                _isHovered = false;
                RedrawFrame();
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            RedrawFrame();
            Parent?.Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            RedrawFrame();
            Parent?.Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            RedrawFrame();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (!string.IsNullOrEmpty(Text) && Enabled)
            {
                _isUnavailableVisual = false;
            }
            RedrawFrame();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            SyncSurfaceAppearance();
        }

        const int EM_SETMARGINS = 0x00D3;
        const int EC_LEFTMARGIN = 0x0001;
        const int EC_RIGHTMARGIN = 0x0002;

        public void ApplyInnerMargins(int left = 4, int right = 4)
        {
            if (IsHandleCreated)
            {
                SendMessage(
                    Handle,
                    EM_SETMARGINS,
                    EC_LEFTMARGIN | EC_RIGHTMARGIN,
                    (IntPtr)((right << 16) | (left & 0xFFFF)));
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyInnerMargins();
            SyncSurfaceAppearance();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            ApplyInnerMargins();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            ApplyInnerMargins();
            RedrawFrame();
        }

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprc, IntPtr hrgn, uint flags);
    }
}