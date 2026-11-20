using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Newtonsoft.Json.Serialization;

namespace grapher.Models.Theming.Controls
{
    public class ThemeableRichTextBox : RichTextBox
    {
        private const int WM_PAINT = 0x0F;
        private const int WM_NCPAINT = 0x85;
        private const uint RDW_INVALIDATE = 0x1;
        private const uint RDW_IUPDATENOW = 0x100;
        private const uint RDW_FRAME = 0x400;

        public ThemeableRichTextBox()
        {
            ScrollBars = RichTextBoxScrollBars.None;
        }

        private Color _borderColor = Color.Gray;

        [DefaultValue(typeof(Color), "Gray")]
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

        private Color _readOnlyBackColor = Color.DarkSlateGray;

        [DefaultValue(typeof(Color), "DarkSlateGray")]
        public Color ReadOnlyBackColor
        {
            get => _readOnlyBackColor;
            set
            {
                if (_readOnlyBackColor == value) return;
                _readOnlyBackColor = value;
                Invalidate();
            }
        }

        private const int BorderWidth = 1;

        protected override void WndProc(ref Message message)
        {
            if ((message.Msg != WM_NCPAINT && message.Msg != WM_PAINT) || BorderColor == Color.Transparent || BorderStyle != BorderStyle.Fixed3D)
            {
                base.WndProc(ref message);
                return;
            }

            if (message.Msg == WM_PAINT)
            {
                base.WndProc(ref message);
            }

            var hdc = GetWindowDC(Handle);
            using (var graphics = Graphics.FromHdcInternal(hdc))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                Color effectiveBorder = (Focused && !ReadOnly && Theme.CurrentScheme != null)
                    ? Theme.CurrentScheme.Primary
                    : BorderColor;
                Color surround = Theme.ResolveContainerBackColor(Parent ?? this);
                Color subtleSeparator = ReadOnly && Theme.CurrentScheme != null
                    ? ColorScheme.Blend(Theme.CurrentScheme.ControlBorder, surround, 0.55f)
                    : effectiveBorder;

                if (ReadOnly)
                {
                    BackColor = ReadOnlyBackColor;
                }
                Color effectiveBack = ReadOnly ? ReadOnlyBackColor : BackColor;

                var rect = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
                int insetX = Math.Max(1, (Width - ClientSize.Width) / 2);
                int insetY = Math.Max(1, (Height - ClientSize.Height) / 2);
                var clientRect = new Rectangle(insetX, insetY, Math.Max(0, ClientSize.Width), Math.Max(0, ClientSize.Height));

                using (var path = Theme.CreateRoundedRectPath(rect, 5f))
                {
                    using (var outerRgn = new Region(new Rectangle(0, 0, Width, Height)))
                    {
                        outerRgn.Exclude(path);
                        using (var surroundBrush = new SolidBrush(surround))
                        {
                            graphics.FillRegion(surroundBrush, outerRgn);
                        }
                    }

                    using (var innerRing = new Region(path))
                    {
                        innerRing.Exclude(clientRect);
                        using (var fillBrush = new SolidBrush(effectiveBack))
                        {
                            graphics.FillRegion(fillBrush, innerRing);
                        }
                    }

                    if (ReadOnly)
                    {
                        using (var sepPen = new Pen(subtleSeparator, 1f))
                        {
                            graphics.DrawLine(sepPen, 0.5f, 4f, 0.5f, Height - 4f);
                        }
                    }
                    else
                    {
                        using (var pen = new Pen(effectiveBorder, 1f))
                        {
                            graphics.DrawPath(pen, path);
                        }
                    }
                }
            }

            ReleaseDC(Handle, hdc);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
        }

        const int EM_SETMARGINS = 0x00D3;
        const int EC_LEFTMARGIN = 0x0001;
        const int EC_RIGHTMARGIN = 0x0002;

        public void ApplyInnerMargins(int left = 5, int right = 5)
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
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_IUPDATENOW | RDW_INVALIDATE);
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