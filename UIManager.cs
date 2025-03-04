using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;

namespace SysVital
{
    public class UIManager
    {
        // Window and control dimensions
        public const int WINDOW_WIDTH = 600;
        public const int WINDOW_HEIGHT = 850;
        public const int COMPACT_WIDTH = 350;
        public const int COMPACT_HEIGHT = 400;
        public const int CONTROL_HEIGHT = 25;
        public const int SECTION_SPACING = 25;
        public const int ITEM_SPACING = 15;
        public const int PROGRESS_BAR_HEIGHT = 6;
        public const int SIDE_PADDING = 20;

        // Theme colors
        public Color DarkBackground { get; private set; } = Color.FromArgb(32, 32, 32);
        public Color LightBackground { get; private set; } = Color.FromArgb(240, 240, 240);
        public Color DarkText { get; private set; } = Color.FromArgb(220, 220, 220);
        public Color LightText { get; private set; } = Color.FromArgb(40, 40, 40);
        public Color DarkAccent { get; private set; } = Color.FromArgb(0, 120, 215);
        public Color LightAccent { get; private set; } = Color.FromArgb(0, 99, 177);

        // Font definitions
        public Font TitleFont { get; private set; } = new Font("Segoe UI", 28, FontStyle.Bold);
        public Font SectionFont { get; private set; } = new Font("Segoe UI", 14, FontStyle.Bold);
        public Font RegularFont { get; private set; } = new Font("Segoe UI", 11, FontStyle.Bold);
        public Font SmallFont { get; private set; } = new Font("Segoe UI", 10, FontStyle.Regular);
        public Font DiskMetricsFont { get; private set; } = new Font("Segoe UI", 10, FontStyle.Bold);

        // Theme state
        public bool IsDarkMode { get; private set; } = true;
        public bool IsCompactMode { get; private set; } = false;

        // Required for window dragging
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        // Constructor
        public UIManager()
        {
            // Initialize with default settings
        }

        // Apply modern styling to the form
        public void ApplyModernStyle(Form form, Panel titleBar)
        {
            // Modern window style - borderless for custom title bar
            form.FormBorderStyle = FormBorderStyle.None;
            form.BackColor = IsDarkMode ? DarkBackground : LightBackground;
            form.Size = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);
            form.MinimumSize = new Size(WINDOW_WIDTH, WINDOW_HEIGHT);

            // Add resize grip to bottom-right corner
            form.SizeGripStyle = SizeGripStyle.Show;

            // Apply styles to all controls
            foreach (Control control in form.Controls)
            {
                ApplyStyleToControl(control);
            }
        }

        // Apply style to a specific control based on its type
        public void ApplyStyleToControl(Control control)
        {
            if (control is ProgressBar progressBar)
            {
                StyleProgressBar(progressBar);
            }
            else if (control is Label label)
            {
                // Check if this is a label with "System Performance" text but no tag
                // This is likely a leftover label that should be removed
                if (label.Text == "System Performance" && string.IsNullOrEmpty(label.Tag?.ToString()))
                {
                    // Instead of styling it, mark it for removal
                    label.Tag = "ToBeRemoved";
                    label.Visible = false;
                    return;
                }
                
                // Check if this is a stray "System" label
                if (label.Text == "System" && string.IsNullOrEmpty(label.Tag?.ToString()))
                {
                    // Instead of styling it, mark it for removal
                    label.Tag = "ToBeRemoved";
                    label.Visible = false;
                    return;
                }
                
                if (label.Text == "System Performance")
                {
                    // Title styling
                    label.Font = TitleFont;
                }
                else if (label.Text.Contains("Top Processes"))
                {
                    // Section styling
                    label.Font = SectionFont;
                }
                else
                {
                    // Regular label styling
                    label.Font = RegularFont;
                }
                label.ForeColor = IsDarkMode ? Color.White : Color.Black;
            }
            else if (control is Button button)
            {
                // Check if this is a stray "System" button
                if ((button.Text == "System" || button.Text.Contains("System")) && 
                    (string.IsNullOrEmpty(button.Tag?.ToString()) || button.Tag.ToString() != "btnSystemInfo"))
                {
                    // Instead of styling it, mark it for removal
                    button.Tag = "ToBeRemoved";
                    button.Visible = false;
                    return;
                }
                
                StyleButton(button);
            }
            else if (control is CheckBox checkbox)
            {
                checkbox.Font = RegularFont;
                checkbox.ForeColor = IsDarkMode ? DarkText : LightText;
            }
            else if (control is NumericUpDown numericUpDown)
            {
                numericUpDown.Font = RegularFont;
                numericUpDown.ForeColor = IsDarkMode ? DarkText : LightText;
                numericUpDown.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(230, 230, 230);
            }
            else if (control is ListView listView)
            {
                listView.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(230, 230, 230);
                listView.ForeColor = IsDarkMode ? DarkText : LightText;
                listView.Font = RegularFont;
            }
        }

        // Style a button with modern flat appearance
        public void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = IsDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(180, 180, 180);
            button.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(230, 230, 230);
            button.ForeColor = IsDarkMode ? Color.White : Color.Black;
            button.Font = RegularFont;
            button.Cursor = Cursors.Hand;
        }

        // Style a progress bar with modern appearance
        public void StyleProgressBar(ProgressBar progressBar)
        {
            progressBar.Height = PROGRESS_BAR_HEIGHT;
            progressBar.ForeColor = IsDarkMode ? DarkAccent : LightAccent;
            progressBar.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(220, 220, 220);
        }

        // Create and style the custom title bar
        public Panel CreateCustomTitleBar(Form form, string appTitle, EventHandler closeHandler, EventHandler minimizeHandler)
        {
            // Create title bar panel
            Panel titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(230, 230, 230)
            };

            // Add logo
            PictureBox logoPictureBox = new PictureBox
            {
                Size = new Size(32, 32),
                Location = new Point(8, 4),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Tag = "TitleBarLogo" // Add tag for identification
            };
            
            try
            {
                Debug.WriteLine("Attempting to load logo...");
                
                // Try loading from Resources folder (relative path)
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "SysVitalLogo.png");
                Debug.WriteLine($"Trying path: {logoPath}");
                
                if (System.IO.File.Exists(logoPath))
                {
                    logoPictureBox.Image = Image.FromFile(logoPath);
                    Debug.WriteLine("Logo loaded successfully from BaseDirectory path");
                }
                else
                {
                    // Try project directory path
                    logoPath = "Resources\\SysVitalLogo.png";
                    Debug.WriteLine($"Trying path: {logoPath}");
                    
                    if (System.IO.File.Exists(logoPath))
                    {
                        logoPictureBox.Image = Image.FromFile(logoPath);
                        Debug.WriteLine("Logo loaded successfully from relative path");
                    }
                    else
                    {
                        // Try one more path - the executable directory
                        logoPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), "Resources", "SysVitalLogo.png");
                        Debug.WriteLine($"Trying path: {logoPath}");
                        
                        if (System.IO.File.Exists(logoPath))
                        {
                            logoPictureBox.Image = Image.FromFile(logoPath);
                            Debug.WriteLine("Logo loaded successfully from executable path");
                        }
                        else
                        {
                            Debug.WriteLine("Could not find logo file in any location");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading logo: {ex.Message}");
            }
            
            titleBar.Controls.Add(logoPictureBox);

            // Add app title - positioned after the logo
            Label titleLabel = new Label
            {
                Text = appTitle,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = IsDarkMode ? DarkText : LightText,
                Location = new Point(logoPictureBox.Right + 8, 8),
                AutoSize = true,
                Tag = "TitleBarLabel" // Add tag for identification
            };
            titleBar.Controls.Add(titleLabel);

            // Close button
            Button closeButton = new Button
            {
                Text = "×",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = IsDarkMode ? DarkText : LightText,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Location = new Point(titleBar.Width - 40, 5),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                TabStop = false,
                Tag = "TitleBarCloseButton" // Add tag for identification
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += closeHandler;

            // Minimize button
            Button minimizeButton = new Button
            {
                Text = "−",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = IsDarkMode ? DarkText : LightText,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Location = new Point(titleBar.Width - 75, 5),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                TabStop = false,
                Tag = "TitleBarMinimizeButton" // Add tag for identification
            };
            minimizeButton.FlatAppearance.BorderSize = 0;
            minimizeButton.Click += minimizeHandler;

            // Add hover effects
            closeButton.MouseEnter += (s, e) => {
                closeButton.BackColor = Color.FromArgb(232, 17, 35);
                closeButton.ForeColor = Color.White;
            };
            closeButton.MouseLeave += (s, e) => {
                closeButton.BackColor = Color.Transparent;
                closeButton.ForeColor = IsDarkMode ? DarkText : LightText;
            };

            minimizeButton.MouseEnter += (s, e) => {
                minimizeButton.BackColor = IsDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(210, 210, 210);
            };
            minimizeButton.MouseLeave += (s, e) => {
                minimizeButton.BackColor = Color.Transparent;
            };

            // Add controls to title bar
            titleBar.Controls.AddRange(new Control[] { titleLabel, closeButton, minimizeButton });
            
            // Enable dragging the form by the title bar
            titleBar.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(form.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            titleLabel.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(form.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };
            logoPictureBox.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(form.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            return titleBar;
        }

        // Method to add a metric section with a label and progress bar
        public void AddMetricSection(Form form, ref int currentY, Label label, Control progressBar, string name)
        {
            // Update label font based on metric type
            if (name.Contains("Disk"))
            {
                label.Font = DiskMetricsFont;
            }
            else
            {
                label.Font = RegularFont;
            }
            
            label.ForeColor = IsDarkMode ? DarkText : LightText;
            label.Location = new Point(SIDE_PADDING, currentY);
            label.AutoSize = true;
            currentY += label.Height + 5;

            if (progressBar != null)
            {
                progressBar.Location = new Point(SIDE_PADDING, currentY);
                progressBar.Size = new Size(form.ClientSize.Width - (SIDE_PADDING * 2), PROGRESS_BAR_HEIGHT);

                if (progressBar is ModernProgressBar modernBar)
                {
                    modernBar.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(220, 220, 220);
                    
                    // Apply different settings based on metric type
                    if (name.Contains("Temperature"))
                    {
                        modernBar.UseTemperatureColors = true;
                        modernBar.Maximum = 100; // Max temperature in Celsius
                    }
                    else if (name.Contains("CPU") || name.Contains("GPU"))
                    {
                        modernBar.ProgressColor = IsDarkMode ? Color.FromArgb(0, 120, 215) : Color.FromArgb(0, 99, 177); // Blue
                        modernBar.Maximum = 100; // Percentage
                    }
                    else if (name.Contains("Memory"))
                    {
                        modernBar.ProgressColor = IsDarkMode ? Color.FromArgb(170, 77, 219) : Color.FromArgb(153, 51, 204); // Purple
                        modernBar.Maximum = 100; // Percentage
                    }
                    else
                    {
                        modernBar.ProgressColor = IsDarkMode ? DarkAccent : LightAccent;
                    }
                }
                else if (progressBar is ProgressBar standardBar)
                {
                    standardBar.Height = PROGRESS_BAR_HEIGHT;
                    standardBar.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(220, 220, 220);
                    standardBar.ForeColor = IsDarkMode ? DarkAccent : LightAccent;
                }

                currentY += progressBar.Height + ITEM_SPACING;
            }
            else
            {
                currentY += ITEM_SPACING;
            }
        }

        // Toggle between dark and light mode
        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }

        // Toggle between normal and compact mode
        public void ToggleCompactMode()
        {
            IsCompactMode = !IsCompactMode;
        }

        // Switch to compact mode
        public void SwitchToCompactMode(Form form, IEnumerable<Control> essentialControls, 
            CheckBox alwaysOnTopCheckBox, Button btnCompactMode, Panel titleBar)
        {
            // Make sure we have valid controls before proceeding
            if (essentialControls == null || !essentialControls.Any())
            {
                Debug.WriteLine("Warning: No essential controls provided for compact mode");
                return;
            }

            Debug.WriteLine($"SwitchToCompactMode: Essential controls count: {essentialControls.Count()}");
            Debug.WriteLine($"SwitchToCompactMode: titleBar is null? {titleBar == null}");
            Debug.WriteLine($"SwitchToCompactMode: btnCompactMode is null? {btnCompactMode == null}");
            Debug.WriteLine($"SwitchToCompactMode: alwaysOnTopCheckBox is null? {alwaysOnTopCheckBox == null}");

            // Hide all non-essential controls
            foreach (Control control in form.Controls)
            {
                bool isEssential = control == titleBar || 
                                   essentialControls.Contains(control) ||
                                   control == alwaysOnTopCheckBox || 
                                   control == btnCompactMode;
                                   
                control.Visible = isEssential;
                
                if (isEssential)
                {
                    Debug.WriteLine($"Keeping visible: {control.GetType().Name} - {control.Tag}");
                }
                else
                {
                    Debug.WriteLine($"Hiding: {control.GetType().Name} - {control.Tag}");
                }
            }

            // Ensure compact mode button is visible and properly styled
            if (btnCompactMode != null)
            {
                btnCompactMode.Visible = true;
                btnCompactMode.Text = "Normal Mode";
                StyleButton(btnCompactMode);
                Debug.WriteLine("Styled compact mode button");
            }
            
            // Ensure always on top checkbox is visible
            if (alwaysOnTopCheckBox != null)
            {
                alwaysOnTopCheckBox.Visible = true;
                Debug.WriteLine("Made always on top checkbox visible");
            }
            
            // Ensure title bar is visible
            if (titleBar != null)
            {
                titleBar.Visible = true;
                Debug.WriteLine("Made title bar visible");
            }

            // Resize window to compact size
            ResizeFormSafely(form, COMPACT_WIDTH, COMPACT_HEIGHT);
            Debug.WriteLine($"Resized form to {COMPACT_WIDTH}x{COMPACT_HEIGHT}");
            
            // Re-arrange essential controls for compact view
            if (essentialControls.Any())
            {
                int y = titleBar?.Height ?? 40;
                y += 20; // Add some spacing after title bar
                
                // Arrange the essential controls vertically
                foreach (Control control in essentialControls)
                {
                    if (control != null)
                    {
                        control.Location = new Point(SIDE_PADDING, y);
                        y += control.Height + 10;
                        Debug.WriteLine($"Positioned {control.GetType().Name} - {control.Tag} at Y={y}");
                    }
                }
                
                // Position always on top checkbox and compact mode button
                if (alwaysOnTopCheckBox != null)
                {
                    alwaysOnTopCheckBox.Location = new Point(SIDE_PADDING, y);
                    y += alwaysOnTopCheckBox.Height + 10;
                    Debug.WriteLine($"Positioned always on top checkbox at Y={y}");
                }
                
                if (btnCompactMode != null)
                {
                    btnCompactMode.Location = new Point(SIDE_PADDING, COMPACT_HEIGHT - btnCompactMode.Height - 20);
                    btnCompactMode.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                    Debug.WriteLine($"Positioned compact mode button at Y={COMPACT_HEIGHT - btnCompactMode.Height - 20}");
                }
            }
        }

        // Switch back to normal mode
        public void SwitchToNormalMode(Form form)
        {
            Debug.WriteLine("SwitchToNormalMode called");
            
            // Remove any existing "Top Processes" labels to prevent duplication
            var processLabels = form.Controls.OfType<Label>().Where(l => 
                l.Text.Contains("Top Processes") || 
                l.Tag?.ToString() == "ProcessListTitle").ToList();
            foreach (var label in processLabels)
            {
                form.Controls.Remove(label);
                label.Dispose();
                Debug.WriteLine("Removed Top Processes label before switching to normal mode");
            }
            
            // Resize form to normal size
            ResizeFormSafely(form, WINDOW_WIDTH, WINDOW_HEIGHT);
            Debug.WriteLine($"Resized form to {WINDOW_WIDTH}x{WINDOW_HEIGHT}");
            
            // Reset Anchor for controls that might have been changed
            foreach (Control control in form.Controls)
            {
                if (control is Button button && button.Tag?.ToString() == "btnCompactMode")
                {
                    button.Anchor = AnchorStyles.None;
                    button.Text = "Compact Mode";
                    StyleButton(button);
                    Debug.WriteLine("Reset compact mode button");
                }
            }
            
            // Make all controls visible
            foreach (Control control in form.Controls)
            {
                control.Visible = true;
            }
            Debug.WriteLine("Made all controls visible");
        }

        // Safely resize the form
        public void ResizeFormSafely(Form form, int width, int height)
        {
            // Temporarily remove size constraints
            form.FormBorderStyle = FormBorderStyle.None;
            form.MinimumSize = new Size(0, 0);
            
            // Apply new size
            form.ClientSize = new Size(width, height);
            
            // Restore minimum size
            form.MinimumSize = new Size(width, height);
        }

        // Add tooltips for metric controls
        public void AddMetricTooltips(IEnumerable<Control> controls)
        {
            var tooltip = new ToolTip();
            
            foreach (var control in controls)
            {
                if (control is Label label)
                {
                    if (label.Text.Contains("CPU Usage"))
                        tooltip.SetToolTip(label, "CPU utilization percentage");
                    else if (label.Text.Contains("CPU Temperature"))
                        tooltip.SetToolTip(label, "Current CPU temperature in Celsius");
                    else if (label.Text.Contains("GPU Usage"))
                        tooltip.SetToolTip(label, "GPU utilization percentage");
                    else if (label.Text.Contains("GPU Temperature"))
                        tooltip.SetToolTip(label, "Current GPU temperature in Celsius");
                    else if (label.Text.Contains("Memory"))
                        tooltip.SetToolTip(label, "Available system memory");
                    else if (label.Text.Contains("Disk Read"))
                        tooltip.SetToolTip(label, "Current disk read speed in MB/s");
                    else if (label.Text.Contains("Disk Write"))
                        tooltip.SetToolTip(label, "Current disk write speed in MB/s");
                }
                else if (control is ProgressBar progressBar)
                {
                    // Set tooltips for progress bars based on their names or tags
                    if (control.Name.Contains("cpu") && !control.Name.Contains("Temp"))
                        tooltip.SetToolTip(progressBar, "CPU utilization percentage");
                    else if (control.Name.Contains("cpuTemp"))
                        tooltip.SetToolTip(progressBar, "Current CPU temperature in Celsius");
                    else if (control.Name.Contains("gpu") && !control.Name.Contains("Temp"))
                        tooltip.SetToolTip(progressBar, "GPU utilization percentage");
                    else if (control.Name.Contains("gpuTemp"))
                        tooltip.SetToolTip(progressBar, "Current GPU temperature in Celsius");
                    else if (control.Name.Contains("ram"))
                        tooltip.SetToolTip(progressBar, "Memory usage percentage");
                }
                else if (control is NumericUpDown refreshRateControl)
                {
                    tooltip.SetToolTip(refreshRateControl, "Refresh rate in seconds (higher values reduce CPU usage)");
                }
                else if (control is CheckBox checkBox && checkBox.Text.Contains("Always on Top"))
                {
                    tooltip.SetToolTip(checkBox, "Keep this window on top of other windows");
                }
                else if (control is Button button)
                {
                    if (button.Text.Contains("System Info"))
                        tooltip.SetToolTip(button, "Show detailed system information");
                    else if (button.Text.Contains("Logging"))
                        tooltip.SetToolTip(button, "Log performance data to a CSV file");
                    else if (button.Text.Contains("Mode") && button.Text.Contains("Dark") || button.Text.Contains("Light"))
                        tooltip.SetToolTip(button, "Toggle between dark and light themes");
                    else if (button.Text.Contains("Compact"))
                        tooltip.SetToolTip(button, "Toggle between normal and compact view");
                }
            }
        }
    }
}
