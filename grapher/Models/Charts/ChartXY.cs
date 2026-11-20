using grapher.Common;
using grapher.Models.Mouse;
using System;
using System.Collections;
using System.Windows.Forms.DataVisualization.Charting;

namespace grapher
{
    public class ChartXY
    {
        #region Fields

        public static ElementPosition DefaultLegendPosition = new ElementPosition(15, 8, 70, 8);
        public static ElementPosition GainLegendPosition = new ElementPosition(25, -1, 50, 25);

        #endregion Fields

        #region Constructors

        public ChartXY(Chart chartX, Chart chartY, string title)
        {
            ChartX = chartX;
            ChartY = chartY;
            Title = title;

            ChartY.Top = ChartX.Top;
            ChartY.Height = ChartX.Height;
            ChartY.Width = ChartX.Width;
            ChartY.Left = ChartX.Left + ChartX.Width + Constants.ChartSeparationHorizontal;

            SetupChart(ChartX);
            SetupChart(ChartY);

            Combined = false;
            SetCombined();
            Visible = true;
        }

        #endregion Constructors

        private const double VerticalMargin = 0.1;

        #region Properties

        public Chart ChartX { get; }

        public Chart ChartY { get; }

        public int Height { 
            get
            {
                return ChartX.Height;
            }
        }

        public int Width { 
            get
            {
                if (Combined)
                {
                    return ChartX.Width;
                }
                else
                {
                    return ChartY.Left + ChartY.Width - ChartX.Left;
                }
            }
        }

        public int Top { 
            get
            {
                return ChartX.Top;
            }
        }

        public int Left { 
            get
            {
                return ChartX.Left;
            }
        }

        public bool Combined { get; set; }

        public bool Visible { get; set; }

        public string Title { get; }

        private PointData CombinedPointData { get; set; }

        private PointData XPointData { get; set; }

        private PointData YPointData { get; set; }

        #endregion Properties

        #region Methods
        
        public static void SetupChart(Chart chart)
        {
            ChartArea area = chart.ChartAreas[0];
            Legend legend = chart.Legends[0];
            Title title = chart.Titles[0];

            area.AxisX.RoundAxisValues();

            area.AxisX.ScaleView.Zoomable = true;
            area.AxisY.ScaleView.Zoomable = true;

            area.AxisY.ScaleView.MinSize = 0.01;
            area.AxisY.ScaleView.SmallScrollSize = 0.001;
            
            area.AxisX.LabelStyle.Format = "0.##";
            area.AxisY.LabelStyle.Format = "0.00";
            area.AxisX.MajorTickMark.Enabled = true;
            area.AxisX.MajorTickMark.TickMarkStyle = TickMarkStyle.OutsideArea;
            area.AxisX.MajorTickMark.LineColor = System.Drawing.Color.Transparent;
            area.AxisX.MajorTickMark.Size = 2.2f;
            area.AxisY.MajorTickMark.Enabled = true;
            area.AxisY.MajorTickMark.TickMarkStyle = TickMarkStyle.OutsideArea;
            area.AxisY.MajorTickMark.LineColor = System.Drawing.Color.Transparent;
            area.AxisY.MajorTickMark.Size = 2.2f;
            area.AxisX.MinorTickMark.Enabled = false;
            area.AxisY.MinorTickMark.Enabled = false;

            area.CursorY.Interval = 0.001;

            area.CursorX.AutoScroll = true;
            area.CursorY.AutoScroll = true;

            area.CursorX.IsUserSelectionEnabled = true;
            area.CursorY.IsUserSelectionEnabled = true;

            area.CursorX.IsUserEnabled = true;
            area.CursorY.IsUserEnabled = true;

            chart.Dock = System.Windows.Forms.DockStyle.Fill;
            chart.Margin = new System.Windows.Forms.Padding(0);

            chart.Series[1].Points.Clear();
            int dummyIdx = chart.Series[1].Points.AddXY(0, 0);
            chart.Series[1].Points[dummyIdx].Color = System.Drawing.Color.Transparent;
            chart.Series[1].Points[dummyIdx].MarkerColor = System.Drawing.Color.Transparent;
            chart.Series[1].Points[dummyIdx].MarkerBorderColor = System.Drawing.Color.Transparent;
            
            area.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 8.0F, System.Drawing.FontStyle.Regular);
            area.AxisY.LabelStyle.Font = area.AxisX.LabelStyle.Font;
            area.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            area.AxisY.TitleFont = area.AxisX.TitleFont;

            title.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            legend.Font = new System.Drawing.Font("Segoe UI", 8.0F, System.Drawing.FontStyle.Regular);
            
            chart.Series[0].BorderWidth = 2;
            chart.Series[0].MarkerSize = 6;
            chart.Series[2].BorderWidth = 2;
            chart.Series[2].MarkerSize = 6;

            for (int i = 1; i < chart.Series.Count; i += 2)
            {
                chart.Series[i].MarkerStyle = MarkerStyle.Circle;
                chart.Series[i].MarkerSize = Constants.DotMarkerSize;
                chart.Series[i].Color = System.Drawing.Color.White;
                chart.Series[i].MarkerColor = System.Drawing.Color.White;
                chart.Series[i].MarkerBorderWidth = 2;
            }

            area.AxisX.MinorGrid.Enabled = false;
            area.AxisY.MinorGrid.Enabled = false;

            title.Visible = false;
            title.DockedToChartArea = string.Empty;
            title.IsDockedInsideChartArea = false;
            title.Position.Auto = false;

            legend.Enabled = false;
            legend.DockedToChartArea = string.Empty;
            legend.IsDockedInsideChartArea = false;
            legend.Position.Auto = false;

            area.Position.Auto = false;
            area.InnerPlotPosition.Auto = true;

            System.Drawing.Color bgTrans = System.Drawing.Color.Transparent;

            area.BackColor = grapher.Models.Theming.Theme.ResolveChartCanvasColor(grapher.Models.Theming.Theme.CurrentScheme);
            area.BorderWidth = 0;
            area.BorderColor = System.Drawing.Color.Transparent;
            chart.BorderlineWidth = 0;
            chart.BorderlineColor = bgTrans;
            legend.BackColor = bgTrans;
        }

        public static void DrawPoint(Chart chart, PointData pointOne, PointData pointTwo = null)
        {
            if (chart.Visible)
            {
                pointOne.Get(out var x, out var y);
                chart.Series[1].Points.DataBindXY(x, y);
                HidePointPrimitives(chart.Series[1]);
                if (pointTwo != null)
                {
                    pointTwo.Get(out x, out y);
                    chart.Series[3].Points.DataBindXY(x, y);
                    HidePointPrimitives(chart.Series[3]);
                }
            }
        }

        private static void HidePointPrimitives(Series series)
        {
            foreach (DataPoint pt in series.Points)
            {
                pt.Color = System.Drawing.Color.Transparent;
                pt.MarkerColor = System.Drawing.Color.Transparent;
                pt.MarkerBorderColor = System.Drawing.Color.Transparent;
            }
        }

        public static void SetLogarithmic(Chart chart)
        {
            /*
            chart.ChartAreas[0].AxisX.Minimum = 0.001;
            chart.ChartAreas[0].AxisX.Maximum = 3500;
            chart.ChartAreas[0].AxisY.Minimum = 0.001;
            chart.ChartAreas[0].AxisY.Maximum = 10;
            chart.ChartAreas[0].AxisX.IsLogarithmic = true;
            chart.ChartAreas[0].AxisY.IsLogarithmic = true;
            */
        }

        public static void SetStandard(Chart chart)
        {
            /*
            chart.ChartAreas[0].AxisX.IsLogarithmic = false;
            chart.ChartAreas[0].AxisY.IsLogarithmic = false;
            */
        }

        public void ClearSecondDots()
        {
            ChartX.Series[3].Points.Clear();
        }

        public void Update()
        {
            ChartX.Update();
            if (ChartY.Visible)
            {
                ChartY.Update();
            }
        }

        public void SetPointBinds(PointData combined, PointData x, PointData y)
        {
            CombinedPointData = combined;
            XPointData = x;
            YPointData = y;
        }

        public void DrawLastMovementValue(bool twoDotsPerGraph = false)
        {
            if(Combined)
            {
                if (twoDotsPerGraph)
                {
                    DrawPoint(ChartX, XPointData, YPointData);
                }
                else
                {
                    DrawPoint(ChartX, CombinedPointData);
                }
            }
            else
            {
                DrawPoint(ChartX, XPointData);
                DrawPoint(ChartY, YPointData);
            }
        }

        public void ClearLastValue()
        {
            ChartX.Series[1].Points.Clear();
            ChartY.Series[1].Points.Clear();
        }

        public void Bind(IDictionary data)
        {
            ChartX.Series[0].Points.DataBindXY(data.Keys, data.Values);
            HidePointPrimitives(ChartX.Series[0]);
            ChartX.Series[2].IsVisibleInLegend = false;
            ChartX.Series[2].Points.Clear();
            ConfigureXAxis(ChartX.ChartAreas[0], GetMaxDictKey(data));
            if (Title == Constants.VelocityChartTitle)
            {
                ConfigureVelocityAxis(ChartX.ChartAreas[0], GetMaxDictValue(data));
            }
        }

        public void BindXY(IDictionary dataX, IDictionary dataY)
        {
            ChartX.Series[0].Points.DataBindXY(dataX.Keys, dataX.Values);
            HidePointPrimitives(ChartX.Series[0]);
            ChartY.Series[0].Points.DataBindXY(dataY.Keys, dataY.Values);
            HidePointPrimitives(ChartY.Series[0]);
            ChartX.Series[2].IsVisibleInLegend = false;
            ChartX.Series[2].Points.Clear();
            ConfigureXAxis(ChartX.ChartAreas[0], GetMaxDictKey(dataX));
            ConfigureXAxis(ChartY.ChartAreas[0], GetMaxDictKey(dataY));
            if (Title == Constants.VelocityChartTitle)
            {
                ConfigureVelocityAxis(ChartX.ChartAreas[0], GetMaxDictValue(dataX));
                ConfigureVelocityAxis(ChartY.ChartAreas[0], GetMaxDictValue(dataY));
            }
        }

        public void BindXYCombined(IDictionary dataX, IDictionary dataY)
        {
            ChartX.Series[0].Points.DataBindXY(dataX.Keys, dataX.Values);
            HidePointPrimitives(ChartX.Series[0]);
            ChartX.Series[2].Points.DataBindXY(dataY.Keys, dataY.Values);
            HidePointPrimitives(ChartX.Series[2]);
            ChartX.Series[2].IsVisibleInLegend = true;
            ConfigureXAxis(ChartX.ChartAreas[0], Math.Max(GetMaxDictKey(dataX), GetMaxDictKey(dataY)));
            if (Title == Constants.VelocityChartTitle)
            {
                ConfigureVelocityAxis(ChartX.ChartAreas[0], Math.Max(GetMaxDictValue(dataX), GetMaxDictValue(dataY)));
            }
        }

        private static double GetMaxDictKey(IDictionary data)
        {
            double maxKey = 0.0;
            foreach (var k in data.Keys)
            {
                if (k is double d && !double.IsNaN(d) && !double.IsInfinity(d) && d > maxKey)
                {
                    maxKey = d;
                }
            }
            return maxKey;
        }

        private static double GetMaxDictValue(IDictionary data)
        {
            double maxVal = 0.0;
            foreach (var v in data.Values)
            {
                if (v is double d && !double.IsNaN(d) && !double.IsInfinity(d) && d > maxVal)
                {
                    maxVal = d;
                }
            }
            return maxVal;
        }

        private static double PickCleanStep(double rawStep)
        {
            if (rawStep <= 0)
            {
                return 1.0;
            }

            if (rawStep >= 10)
            {
                double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
                double norm = rawStep / magnitude;
                if (norm <= 1.0) return 1.0 * magnitude;
                if (norm <= 2.0) return 2.0 * magnitude;
                if (norm <= 2.5) return 2.5 * magnitude;
                if (norm <= 5.0) return 5.0 * magnitude;
                return 10.0 * magnitude;
            }

            return Math.Max(0.5, Math.Ceiling(rawStep * 2.0) / 2.0);
        }

        private static void ConfigureXAxis(ChartArea area, double maxX)
        {
            if (maxX <= 0)
            {
                maxX = 80.0;
            }

            double step = PickCleanStep(maxX / 4.0);
            double axisEnd = Math.Ceiling(maxX / step) * step;
            if (axisEnd < step * 3) axisEnd = step * 4;

            area.AxisX.IsMarginVisible = false;
            area.AxisX.Minimum = 0.0;
            area.AxisX.Maximum = axisEnd;
            area.AxisX.Interval = step;
            area.AxisX.MajorGrid.Interval = step;
            area.AxisX.MajorGrid.IntervalOffset = 0.0;
            area.AxisX.LabelStyle.Interval = step;
            area.AxisX.LabelStyle.IntervalOffset = 0.0;
            area.AxisX.LabelStyle.IsEndLabelVisible = true;
            area.AxisX.MajorTickMark.Interval = step;
            area.AxisX.MajorTickMark.IntervalOffset = 0.0;
        }

        private static void ConfigureVelocityAxis(ChartArea area, double maxVal)
        {
            if (maxVal <= 0)
            {
                maxVal = 100.0;
            }

            double step = PickCleanStep((maxVal * 1.05) / 4.0);
            double padBottom = step * 0.22;
            double padTop = step * 0.20;

            area.AxisY.Minimum = -padBottom;
            area.AxisY.Maximum = step * 4.0 + padTop;
            area.AxisY.Interval = step;
            area.AxisY.MajorGrid.Interval = step;
            area.AxisY.MajorGrid.IntervalOffset = padBottom;
            area.AxisY.LabelStyle.Interval = step;
            area.AxisY.LabelStyle.IntervalOffset = padBottom;
            area.AxisY.MajorTickMark.Interval = step;
            area.AxisY.MajorTickMark.IntervalOffset = padBottom;
            area.AxisY.LabelStyle.Format = "0.##";
        }

        public void SetLegendPosition(ElementPosition position)
        {
            ChartX.Legends[0].Position = position;
            ChartY.Legends[0].Position = position;
        }

        private void VerifyRange(double min, double max)
        {
            if (min > max)
            {
                throw new ArgumentException($"invalid chart range: ({min}, {max})");
            }
        }

        public void SetMinMax(double min, double max)
        {
            VerifyRange(min, max);
            if (Title == Constants.SensitivityChartTitle &&
                grapher.Models.Options.CurvePresetManager.OriginalMinY > 0 &&
                grapher.Models.Options.CurvePresetManager.OriginalMaxY > grapher.Models.Options.CurvePresetManager.OriginalMinY)
            {
                min = Math.Min(min, grapher.Models.Options.CurvePresetManager.OriginalMinY);
                max = Math.Max(max, grapher.Models.Options.CurvePresetManager.OriginalMaxY);
            }

            ApplySpanYRange(ChartX.ChartAreas[0], min, max);
        }

        public void SetMinMaxXY(double minX, double maxX, double minY, double maxY)
        {
            VerifyRange(minX, maxX);
            ApplySpanYRange(ChartX.ChartAreas[0], minX, maxX);

            VerifyRange(minY, maxY);
            ApplySpanYRange(ChartY.ChartAreas[0], minY, maxY);
        }

        private static void ApplySpanYRange(ChartArea area, double min, double max)
        {
            double span = max - min;
            double pad = Math.Max(span * 0.15, Math.Max(0.08, Math.Abs(min) * VerticalMargin));
            double axisMin = min - pad;
            double axisMax = max + pad;
            area.AxisY.Minimum = axisMin;
            area.AxisY.Maximum = axisMax + 1e-4;
            area.AxisY.Interval = (axisMax - axisMin) / 4.0;
        }

        public void SetCombined()
        {
            if (!Combined)
            {
                ChartY.Hide();
                Combined = true;
                ChartX.Titles[0].Text = Title;
            }
        }

        public void SetSeparate()
        {
            if (Combined)
            {
                if (Visible)
                {
                    ChartY.Show();
                }

                ChartX.Titles[0].Text = SetComponentTitle(Constants.XComponent);
                ChartY.Titles[0].Text = SetComponentTitle(Constants.YComponent);

                Combined = false;
            }
        }


        public void Hide()
        {

            if (Visible)
            {
                ChartX.Hide();
                ChartY.Hide();
                Visible = false;
            }
        }

        public void Show()
        {
            if (!Visible)
            {
                ChartX.Show();

                if (!Combined)
                {
                    ChartY.Show();
                }

                Visible = true;
            }
        }

        public void SetTop(int top)
        {
            ChartX.Top = top;
            ChartY.Top = top;
        }

        public void SetHeight(int height)
        {
            ChartX.Height = height;
            ChartY.Height = height;
        }

        private string SetComponentTitle(string component)
        {
            return $"{Title} : {component}";
        }
        #endregion Methods
    }
}
