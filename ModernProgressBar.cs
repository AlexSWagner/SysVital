using System;
using System.Drawing;
using System.Windows.Forms;

namespace SysVital
{
    public class ModernProgressBar : Control
    {
        private int _value;
        private int _maximum = 100;
        private Color _progressColor = Color.FromArgb(0, 120, 215); // Default to blue
        private bool _useTemperatureColors = false;
        private int _cornerRadius = 3;
        private bool _showValueText = false;
        private string _customFormat = "{0}%";
        private Color _textColor = Color.White;

        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Max(0, Math.Min(value, Maximum));
                Invalidate();
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = Math.Max(1, value);
                Invalidate();
            }
        }

        public Color ProgressColor
        {
            get => _progressColor;
            set
            {
                _progressColor = value;
                Invalidate();
            }
        }

        public bool UseTemperatureColors
        {
            get => _useTemperatureColors;
            set
            {
                _useTemperatureColors = value;
                Invalidate();
            }
        }

        public int CornerRadius
        {
            get => _cornerRadius;
            set
            {
                _cornerRadius = Math.Max(0, Math.Min(value, Height / 2));
                Invalidate();
            }
        }

        public bool ShowValueText
        {
            get => _showValueText;
            set
            {
                _showValueText = value;
                Invalidate();
            }
        }

        public string CustomFormat
        {
            get => _customFormat;
            set
            {
                _customFormat = string.IsNullOrEmpty(value) ? "{0}%" : value;
                Invalidate();
            }
        }

        public Color TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                Invalidate();
            }
        }

        public ModernProgressBar()
        {
            SetStyle(ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer, true);
            Height = 6; // Make all bars consistently thin
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw background with rounded corners
            using (var path = GetRoundedRectangle(ClientRectangle, CornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }

            // Calculate progress width
            int progressWidth = (int)((float)Value / Maximum * Width);

            // Determine color based on settings
            Color progressColorToUse = ProgressColor;
            if (UseTemperatureColors)
            {
                // Change color based on value (temperature)
                if (Value < 50)
                {
                    // Safe range - use blue to green gradient
                    progressColorToUse = Interpolate(Color.FromArgb(0, 120, 215), Color.FromArgb(40, 167, 69), Value / 50.0f);
                }
                else if (Value < 75)
                {
                    // Warning range - use green to orange gradient
                    progressColorToUse = Interpolate(Color.FromArgb(40, 167, 69), Color.FromArgb(255, 153, 0), (Value - 50) / 25.0f);
                }
                else
                {
                    // Critical range - use orange to red gradient
                    progressColorToUse = Interpolate(Color.FromArgb(255, 153, 0), Color.FromArgb(220, 53, 69), (Value - 75) / 25.0f);
                }
            }

            // Draw progress with rounded corners
            if (progressWidth > 0)
            {
                var progressRect = new Rectangle(0, 0, progressWidth, Height);
                using (var path = GetRoundedRectangle(progressRect, CornerRadius))
                using (var brush = new SolidBrush(progressColorToUse))
                {
                    e.Graphics.FillPath(brush, path);
                }
            }

            // Draw text if enabled
            if (ShowValueText && Height >= 15) // Only show text if there's enough space
            {
                string text = string.Format(CustomFormat, Value);
                using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
                using (var brush = new SolidBrush(TextColor))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    e.Graphics.DrawString(text, font, brush, ClientRectangle, format);
                }
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            // Top left arc
            path.AddArc(arc, 180, 90);

            // Top right arc
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Bottom right arc
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Bottom left arc
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        // Color interpolation helper
        private Color Interpolate(Color color1, Color color2, float factor)
        {
            factor = Math.Max(0, Math.Min(1, factor)); // Clamp factor to 0-1
            int r = (int)(color1.R + (color2.R - color1.R) * factor);
            int g = (int)(color1.G + (color2.G - color1.G) * factor);
            int b = (int)(color1.B + (color2.B - color1.B) * factor);
            return Color.FromArgb(r, g, b);
        }
    }
}