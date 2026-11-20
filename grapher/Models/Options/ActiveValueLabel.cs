using System.Drawing;
using System.Windows.Forms;
using grapher.Common;
using grapher.Models.Theming;

namespace grapher.Models.Options
{
    public class ActiveValueLabel
    {
        #region Fields

        private string _prefix;
        private string _value;

        #endregion Fields

        #region Constructors

        public ActiveValueLabel(Label valueLabel, Label centeringLabel)
        {
            ValueLabel = valueLabel;
            ValueLabel.AutoSize = false;
            ValueLabel.TextAlign = ContentAlignment.MiddleCenter;
            ValueLabel.ForeColor = Theme.CurrentScheme.OnFocusedField;

            CenteringLabel = centeringLabel;
            Align();

            FormatString = Constants.DefaultActiveValueFormatString;
            Prefix = string.Empty;
        }

        #endregion Constructors

        #region Properties

        public Label ValueLabel { get; }

        public string FormatString { get; set; }

        public string Prefix 
        { 
            get { return _prefix; }
            set 
            {
                _prefix = value;
                RefreshText();
            } 
        }

        private string Value
        { 
            get { return _value; }
            set 
            {
                _value = value;
                RefreshText();
            } 
        }

        public int Left
        {
            get 
            {
                return ValueLabel.Left;
            }

            set
            {
                ValueLabel.Left = value;
            }
        }

        public int Width
        {
            get 
            {
                return ValueLabel.Width;
            }

            set
            {
                ValueLabel.Width = value;
            }
        }

        public int Top
        {
            get
            {
                return ValueLabel.Top;
            }
            set
            {
                ValueLabel.Top = value;
            }
        }

        public int Height
        {
            get 
            {
                return ValueLabel.Height;
            }

            set
            {
                ValueLabel.Height = value;
            }
        }

        public Label CenteringLabel { get; }

        #endregion Properties

        #region Methods

        public void Hide()
        {
            ValueLabel.Hide();
        }

        public void Show()
        {
            ValueLabel.Show();
        }

        public void SetValue(double value)
        {
            SetValue(value.ToString(FormatString));
        }

        public void SetValue(string value)
        {
            Value = value;
        }

        public void RefreshText()
        {
            ValueLabel.Text = string.IsNullOrWhiteSpace(Prefix) ? Value: $"{Prefix}: {Value}";
        }

        public void Align()
        {
            CenteringLabel.AutoSize = false;
            if (CenteringLabel.Width < 64) CenteringLabel.Width = 64;
            if (CenteringLabel.Height < 18) CenteringLabel.Height = 18;
            Left = CenteringLabel.Left;
            Width = CenteringLabel.Width;
            if (ValueLabel.Height < 18) ValueLabel.Height = 18;
        }

        #endregion Methods
    }
}
