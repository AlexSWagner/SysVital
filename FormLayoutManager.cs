using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Diagnostics;

namespace SysVital
{
    /// <summary>
    /// Manages the layout of controls and UI components in the application
    /// Responsible for creating, positioning, and organizing UI elements
    /// </summary>
    public class FormLayoutManager
    {
        // Reference to UIManager for styling and dimensions
        private readonly UIManager uiManager;
        
        // Constants for layout
        private const int DISK_SECTION_SPACING = 18;
        private const int DISK_INDENT = 15;
        private const int PROCESS_LIST_HEIGHT = 130;
        
        // Main form reference
        private Form parentForm;
        
        // Control references for layout positioning
        private Label lblCPU;
        private Label lblCPUTemp;
        private Label lblGPUTemp;
        private Label lblGPUUsage;
        private Label lblRAM;
        private Label lblDiskRead;
        private Label lblDiskWrite;
        private ModernProgressBar cpuProgressBar;
        private ModernProgressBar cpuTempProgressBar;
        private ModernProgressBar gpuTempProgressBar;
        private ModernProgressBar gpuUsageProgressBar;
        private ModernProgressBar ramProgressBar;
        private NumericUpDown refreshRateControl;
        private CheckBox alwaysOnTopCheckBox;
        private Button btnSystemInfo;
        private Button btnStartLog;
        private Button btnDarkMode;
        private Button btnCompactMode;
        private Panel titleBar;
        private ListView processListView;
        private System.Windows.Forms.Timer processTimer;
        private ProcessMonitor processMonitor;
        
        /// <summary>
        /// Constructor - initializes with required dependencies
        /// </summary>
        /// <param name="form">The parent form</param>
        /// <param name="uiManager">The UI styling manager</param>
        public FormLayoutManager(Form form, UIManager uiManager)
        {
            this.parentForm = form;
            this.uiManager = uiManager;
            this.processMonitor = new ProcessMonitor();
        }
        
        /// <summary>
        /// Sets control references from the parent form
        /// </summary>
        /// <param name="controls">Dictionary of controls from the form</param>
        public void SetControlReferences(Dictionary<string, Control> controls)
        {
            // Map control references
            lblCPU = controls["lblCPU"] as Label;
            lblCPUTemp = controls["lblCPUTemp"] as Label;
            lblGPUTemp = controls["lblGPUTemp"] as Label;
            lblGPUUsage = controls["lblGPUUsage"] as Label;
            lblRAM = controls["lblRAM"] as Label;
            lblDiskRead = controls["lblDiskRead"] as Label;
            lblDiskWrite = controls["lblDiskWrite"] as Label;
            cpuProgressBar = controls["cpuProgressBar"] as ModernProgressBar;
            cpuTempProgressBar = controls["cpuTempProgressBar"] as ModernProgressBar;
            gpuTempProgressBar = controls["gpuTempProgressBar"] as ModernProgressBar;
            gpuUsageProgressBar = controls["gpuUsageProgressBar"] as ModernProgressBar;
            ramProgressBar = controls["ramProgressBar"] as ModernProgressBar;
            refreshRateControl = controls["refreshRateControl"] as NumericUpDown;
            alwaysOnTopCheckBox = controls["alwaysOnTopCheckBox"] as CheckBox;
            btnSystemInfo = controls["btnSystemInfo"] as Button;
            btnStartLog = controls["btnStartLog"] as Button;
            btnDarkMode = controls["btnDarkMode"] as Button;
            btnCompactMode = controls["btnCompactMode"] as Button;
            titleBar = controls["titleBar"] as Panel;
        }
        
        /// <summary>
        /// Initialize the form layout - should be called from Form1_Load
        /// </summary>
        /// <param name="refreshRateChangedHandler">Event handler for refresh rate control</param>
        /// <param name="alwaysOnTopChangedHandler">Event handler for always on top checkbox</param>
        /// <param name="systemInfoClickHandler">Event handler for system info button</param>
        /// <param name="startLoggingClickHandler">Event handler for start logging button</param>
        /// <param name="toggleThemeHandler">Event handler for toggle theme button</param>
        /// <param name="toggleCompactModeHandler">Event handler for toggle compact mode button</param>
        public void InitializeLayout(
            EventHandler refreshRateChangedHandler,
            EventHandler alwaysOnTopChangedHandler,
            EventHandler systemInfoClickHandler,
            EventHandler startLoggingClickHandler,
            EventHandler toggleThemeHandler,
            EventHandler toggleCompactModeHandler)
        {
            Debug.WriteLine("FormLayoutManager.InitializeLayout called");
            
            // Clean up any leftover controls from previous sessions
            CleanupLeftoverControls();
            
            // Initialize all controls
            InitializeControls(alwaysOnTopChangedHandler);
            
            // Connect event handlers
            refreshRateControl.ValueChanged -= refreshRateChangedHandler; // Remove first to avoid duplicates
            refreshRateControl.ValueChanged += refreshRateChangedHandler;
            
            // Ensure the compact mode button has the correct event handler
            if (btnCompactMode != null)
            {
                // Remove any existing handlers to avoid duplicates
                btnCompactMode.Click -= toggleCompactModeHandler;
                
                // Add the new handler
                btnCompactMode.Click += toggleCompactModeHandler;
                Debug.WriteLine("Connected toggleCompactModeHandler to btnCompactMode");
            }
            else
            {
                Debug.WriteLine("WARNING: btnCompactMode is null in InitializeLayout");
            }
            
            // Ensure the system info button has the correct event handler
            if (btnSystemInfo != null)
            {
                btnSystemInfo.Click -= systemInfoClickHandler;
                btnSystemInfo.Click += systemInfoClickHandler;
                Debug.WriteLine("Connected systemInfoClickHandler to btnSystemInfo");
            }
            
            // Ensure the start logging button has the correct event handler
            if (btnStartLog != null)
            {
                btnStartLog.Click -= startLoggingClickHandler;
                btnStartLog.Click += startLoggingClickHandler;
                Debug.WriteLine("Connected startLoggingClickHandler to btnStartLog");
            }
            
            // Ensure the dark mode button has the correct event handler
            if (btnDarkMode != null)
            {
                btnDarkMode.Click -= toggleThemeHandler;
                btnDarkMode.Click += toggleThemeHandler;
                Debug.WriteLine("Connected toggleThemeHandler to btnDarkMode");
            }
            
            // Layout all controls
            LayoutControls(
                systemInfoClickHandler,
                startLoggingClickHandler,
                toggleThemeHandler,
                toggleCompactModeHandler);
        }
        
        /// <summary>
        /// Remove any lingering controls from previous sessions
        /// </summary>
        public void CleanupLeftoverControls()
        {
            var controlsToRemove = new List<Control>();
            
            // Find any controls that might be leftover
            foreach (Control control in parentForm.Controls)
            {
                if (control is Label label)
                {
                    // Remove any standalone "Disk" label or any label at default position (0,0)
                    if (label.Text == "Disk" || 
                        (label.Text.Contains("Disk") && !label.Text.Contains("Read") && !label.Text.Contains("Write")) ||
                        (label.Location.X == 0 && label.Location.Y == 0 && label.Text.Contains("Disk")) ||
                        label.Text == "System") // Remove any stray "System" labels
                    {
                        controlsToRemove.Add(label);
                    }
                }
                // Check for duplicate buttons with the same tag
                else if (control is Button button && button.Tag != null)
                {
                    var duplicateButtons = parentForm.Controls.OfType<Button>()
                        .Where(b => b != button && b.Tag?.ToString() == button.Tag?.ToString())
                        .ToList();
                    
                    // Keep the first one, mark others for removal
                    controlsToRemove.AddRange(duplicateButtons);
                }
                // Check for duplicate progress bars with the same tag
                else if (control is ProgressBar progressBar && progressBar.Tag != null)
                {
                    var duplicateProgressBars = parentForm.Controls.OfType<ProgressBar>()
                        .Where(p => p != progressBar && p.Tag?.ToString() == progressBar.Tag?.ToString())
                        .ToList();
                    
                    // Keep the first one, mark others for removal
                    controlsToRemove.AddRange(duplicateProgressBars);
                }
            }
            
            // Now remove them
            foreach (Control control in controlsToRemove)
            {
                parentForm.Controls.Remove(control);
                control.Dispose();
            }
        }
        
        /// <summary>
        /// Initialize all controls used in the form
        /// </summary>
        /// <param name="alwaysOnTopChangedHandler">Event handler for always on top checkbox</param>
        public void InitializeControls(EventHandler alwaysOnTopChangedHandler)
        {
            // Only initialize controls if they don't already exist
            if (lblCPU == null) lblCPU = new Label();
            if (lblCPUTemp == null) lblCPUTemp = new Label();
            if (lblGPUTemp == null) lblGPUTemp = new Label();
            if (lblGPUUsage == null) lblGPUUsage = new Label();
            if (lblRAM == null) lblRAM = new Label();
            if (lblDiskRead == null) lblDiskRead = new Label();
            if (lblDiskWrite == null) lblDiskWrite = new Label();

            // Initialize progress bars
            if (cpuProgressBar == null) cpuProgressBar = new ModernProgressBar();
            if (cpuTempProgressBar == null) cpuTempProgressBar = new ModernProgressBar { UseTemperatureColors = true };
            if (gpuTempProgressBar == null) gpuTempProgressBar = new ModernProgressBar { UseTemperatureColors = true };
            if (gpuUsageProgressBar == null) gpuUsageProgressBar = new ModernProgressBar();
            if (ramProgressBar == null) ramProgressBar = new ModernProgressBar();

            // Initialize other controls
            if (refreshRateControl == null) refreshRateControl = new NumericUpDown();
            // Set default value for refresh rate control
            refreshRateControl.Minimum = 1;
            refreshRateControl.Value = 2;
            
            if (alwaysOnTopCheckBox == null) alwaysOnTopCheckBox = new CheckBox();
            alwaysOnTopCheckBox.Text = "Always on Top";
            alwaysOnTopCheckBox.AutoSize = true; // Allow text to display fully
            alwaysOnTopCheckBox.CheckedChanged += alwaysOnTopChangedHandler; // Connect event handler

            // Add all controls to the form
            parentForm.Controls.AddRange(new Control[] {
                lblCPU, lblCPUTemp, lblGPUTemp, lblGPUUsage, lblRAM, lblDiskRead, lblDiskWrite,
                cpuProgressBar, cpuTempProgressBar, gpuTempProgressBar, gpuUsageProgressBar, ramProgressBar,
                refreshRateControl, alwaysOnTopCheckBox
            });
        }
        
        /// <summary>
        /// Layout all controls on the form
        /// </summary>
        /// <param name="systemInfoClickHandler">Event handler for system info button</param>
        /// <param name="startLoggingClickHandler">Event handler for start logging button</param>
        /// <param name="toggleThemeHandler">Event handler for toggle theme button</param>
        /// <param name="toggleCompactModeHandler">Event handler for toggle compact mode button</param>
        public void LayoutControls(
            EventHandler systemInfoClickHandler,
            EventHandler startLoggingClickHandler,
            EventHandler toggleThemeHandler,
            EventHandler toggleCompactModeHandler)
        {
            Debug.WriteLine("FormLayoutManager.LayoutControls called");
            int currentY = 70;  // Start after title bar

            // Convert all progress bars to ModernProgressBar if needed
            EnsureModernProgressBars();

            // CPU Usage
            uiManager.AddMetricSection(parentForm, ref currentY, lblCPU, cpuProgressBar, "CPU Usage");
            currentY += UIManager.SECTION_SPACING;

            // CPU Temperature
            uiManager.AddMetricSection(parentForm, ref currentY, lblCPUTemp, cpuTempProgressBar, "CPU Temperature");
            currentY += UIManager.SECTION_SPACING;

            // GPU Temperature
            uiManager.AddMetricSection(parentForm, ref currentY, lblGPUTemp, gpuTempProgressBar, "GPU Temperature");
            currentY += UIManager.SECTION_SPACING;

            // GPU Usage
            uiManager.AddMetricSection(parentForm, ref currentY, lblGPUUsage, gpuUsageProgressBar, "GPU Usage");
            currentY += UIManager.SECTION_SPACING;

            // RAM Usage
            uiManager.AddMetricSection(parentForm, ref currentY, lblRAM, ramProgressBar, "Memory Usage");
            currentY += UIManager.SECTION_SPACING;

            // Add disk metrics section
            AddDiskMetricsSection(ref currentY);

            // Add controls section (refresh rate selector, etc.)
            AddControlsSection(ref currentY, 
                systemInfoClickHandler, 
                startLoggingClickHandler, 
                toggleThemeHandler, 
                toggleCompactModeHandler);

            // Process section
            currentY += UIManager.SECTION_SPACING;
            CreateProcessMonitor();
            
            // Add tooltips
            var controls = new List<Control> {
                lblCPU, lblCPUTemp, lblGPUTemp, lblGPUUsage, lblRAM, lblDiskRead, lblDiskWrite,
                cpuProgressBar, cpuTempProgressBar, gpuTempProgressBar, gpuUsageProgressBar, ramProgressBar,
                refreshRateControl, alwaysOnTopCheckBox, btnSystemInfo, btnStartLog, btnDarkMode, btnCompactMode
            };
            uiManager.AddMetricTooltips(controls);
            
            // Ensure all controls have proper tags
            EnsureControlTags();
            
            Debug.WriteLine("FormLayoutManager.LayoutControls completed");
        }
        
        /// <summary>
        /// Ensure all progress bars are of the ModernProgressBar type
        /// </summary>
        private void EnsureModernProgressBars()
        {
            if (!(cpuProgressBar is ModernProgressBar))
            {
                var newCpuBar = new ModernProgressBar
                {
                    Location = cpuProgressBar.Location,
                    Size = cpuProgressBar.Size,
                    Maximum = 100,
                    BackColor = Color.FromArgb(45, 45, 48),
                    ProgressColor = Color.FromArgb(0, 120, 215)
                };
                parentForm.Controls.Remove(cpuProgressBar);
                cpuProgressBar = newCpuBar;
                parentForm.Controls.Add(cpuProgressBar);
            }

            if (!(gpuTempProgressBar is ModernProgressBar))
            {
                var newGpuTempBar = new ModernProgressBar
                {
                    Location = gpuTempProgressBar.Location,
                    Size = gpuTempProgressBar.Size,
                    Maximum = 100,
                    BackColor = Color.FromArgb(45, 45, 48),
                    ProgressColor = Color.FromArgb(0, 120, 215)
                };
                parentForm.Controls.Remove(gpuTempProgressBar);
                gpuTempProgressBar = newGpuTempBar;
                parentForm.Controls.Add(gpuTempProgressBar);
            }

            if (!(gpuUsageProgressBar is ModernProgressBar))
            {
                var newGpuUsageBar = new ModernProgressBar
                {
                    Location = gpuUsageProgressBar.Location,
                    Size = gpuUsageProgressBar.Size,
                    Maximum = 100,
                    BackColor = Color.FromArgb(45, 45, 48),
                    ProgressColor = Color.FromArgb(0, 120, 215)
                };
                parentForm.Controls.Remove(gpuUsageProgressBar);
                gpuUsageProgressBar = newGpuUsageBar;
                parentForm.Controls.Add(gpuUsageProgressBar);
            }

            // Add CPU Temperature progress bar if needed
            if (cpuTempProgressBar == null)
            {
                cpuTempProgressBar = new ModernProgressBar
                {
                    Maximum = 100,
                    BackColor = Color.FromArgb(45, 45, 48),
                    ProgressColor = Color.FromArgb(0, 120, 215)
                };
                parentForm.Controls.Add(cpuTempProgressBar);
            }
        }
        
        /// <summary>
        /// Add the disk metrics section to the form
        /// </summary>
        private void AddDiskMetricsSection(ref int currentY)
        {
            // Add a disk metrics section header
            Label diskSectionLabel = new Label
            {
                Text = "Disk Activity",
                Font = uiManager.DiskMetricsFont,
                ForeColor = uiManager.IsDarkMode ? uiManager.DarkText : uiManager.LightText,
                Location = new Point(UIManager.SIDE_PADDING, currentY),
                AutoSize = true
            };
            parentForm.Controls.Add(diskSectionLabel);
            currentY += diskSectionLabel.Height + 8; // Slightly more spacing after section header
            
            // Disk Read - indented
            lblDiskRead.Font = uiManager.DiskMetricsFont;
            lblDiskRead.Location = new Point(UIManager.SIDE_PADDING + DISK_INDENT, currentY);
            lblDiskRead.AutoSize = true;
            parentForm.Controls.Add(lblDiskRead);
            currentY += lblDiskRead.Height + DISK_SECTION_SPACING;

            // Disk Write - indented
            lblDiskWrite.Font = uiManager.DiskMetricsFont;
            lblDiskWrite.Location = new Point(UIManager.SIDE_PADDING + DISK_INDENT, currentY);
            lblDiskWrite.AutoSize = true;
            parentForm.Controls.Add(lblDiskWrite);
            currentY += DISK_SECTION_SPACING + 25; // Added significant spacing after disk metrics section
        }
        
        /// <summary>
        /// Add the controls section (buttons, refresh rate selector, etc.)
        /// </summary>
        private void AddControlsSection(ref int currentY, 
            EventHandler systemInfoClickHandler, 
            EventHandler startLoggingClickHandler, 
            EventHandler toggleThemeHandler, 
            EventHandler toggleCompactModeHandler)
        {
            Debug.WriteLine("AddControlsSection called");
            
            // Refresh rate control
            Label refreshRateLabel = new Label
            {
                Text = "Refresh Rate (seconds):",
                AutoSize = true,
                Location = new Point(UIManager.SIDE_PADDING, currentY),
                Font = uiManager.RegularFont,
                ForeColor = uiManager.IsDarkMode ? Color.White : Color.Black,
                Tag = "refreshRateLabel"
            };
            parentForm.Controls.Add(refreshRateLabel);
            
            refreshRateControl.Location = new Point(refreshRateLabel.Right + 10, currentY);
            refreshRateControl.Size = new Size(60, UIManager.CONTROL_HEIGHT);
            refreshRateControl.BackColor = uiManager.IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(230, 230, 230);
            refreshRateControl.ForeColor = uiManager.IsDarkMode ? Color.White : Color.Black;
            currentY += UIManager.CONTROL_HEIGHT + UIManager.ITEM_SPACING;

            // Always on top checkbox
            alwaysOnTopCheckBox.Location = new Point(UIManager.SIDE_PADDING, currentY);
            alwaysOnTopCheckBox.Font = uiManager.RegularFont;
            alwaysOnTopCheckBox.ForeColor = uiManager.IsDarkMode ? Color.White : Color.Black;
            currentY += UIManager.CONTROL_HEIGHT + UIManager.ITEM_SPACING;

            // Calculate the button width to fit all 4 buttons in a row with spacing
            int availableWidth = parentForm.ClientSize.Width - (UIManager.SIDE_PADDING * 2);
            int buttonSpacing = 10;
            int buttonWidth = (availableWidth - (buttonSpacing * 3)) / 4; // 4 buttons with 3 spaces between them

            // System Info Button - check if it already exists
            if (btnSystemInfo == null)
            {
                btnSystemInfo = new Button
                {
                    Text = "System Info",
                    Size = new Size(buttonWidth, 35),
                    Location = new Point(UIManager.SIDE_PADDING, currentY),
                    Tag = "btnSystemInfo"
                };
                uiManager.StyleButton(btnSystemInfo);
                parentForm.Controls.Add(btnSystemInfo);
            }
            else
            {
                // If the button already exists, just update its position and size
                btnSystemInfo.Size = new Size(buttonWidth, 35);
                btnSystemInfo.Location = new Point(UIManager.SIDE_PADDING, currentY);
                btnSystemInfo.Tag = "btnSystemInfo"; // Ensure it has the correct tag
                
                // Remove from controls and re-add to ensure event handlers are fresh
                parentForm.Controls.Remove(btnSystemInfo);
                parentForm.Controls.Add(btnSystemInfo);
                
                // Make sure the button is properly styled
                uiManager.StyleButton(btnSystemInfo);
            }
            
            // Add the event handler
            btnSystemInfo.Click -= systemInfoClickHandler; // Remove first to avoid duplicates
            btnSystemInfo.Click += systemInfoClickHandler;
            Debug.WriteLine("Connected systemInfoClickHandler to btnSystemInfo");

            // Start Log Button - check if it already exists
            if (btnStartLog == null)
            {
                btnStartLog = new Button
                {
                    Text = "Start Logging",
                    Size = new Size(buttonWidth, 35),
                    Location = new Point(btnSystemInfo.Right + buttonSpacing, currentY),
                    Tag = "btnStartLog"
                };
                uiManager.StyleButton(btnStartLog);
                parentForm.Controls.Add(btnStartLog);
            }
            else
            {
                // If the button already exists, just update its position and size
                btnStartLog.Size = new Size(buttonWidth, 35);
                btnStartLog.Location = new Point(btnSystemInfo.Right + buttonSpacing, currentY);
                btnStartLog.Tag = "btnStartLog"; // Ensure it has the correct tag
                
                // Remove from controls and re-add to ensure event handlers are fresh
                parentForm.Controls.Remove(btnStartLog);
                parentForm.Controls.Add(btnStartLog);
                
                // Make sure the button is properly styled
                uiManager.StyleButton(btnStartLog);
            }
            
            // Add the event handler
            btnStartLog.Click -= startLoggingClickHandler; // Remove first to avoid duplicates
            btnStartLog.Click += startLoggingClickHandler;
            Debug.WriteLine("Connected startLoggingClickHandler to btnStartLog");

            // Dark Mode Button - check if it already exists
            if (btnDarkMode == null)
            {
                btnDarkMode = new Button
                {
                    Text = uiManager.IsDarkMode ? "Light Mode" : "Dark Mode",
                    Size = new Size(buttonWidth, 35),
                    Location = new Point(btnStartLog.Right + buttonSpacing, currentY),
                    Tag = "btnDarkMode"
                };
                uiManager.StyleButton(btnDarkMode);
                parentForm.Controls.Add(btnDarkMode);
            }
            else
            {
                // If the button already exists, just update its position and size
                btnDarkMode.Size = new Size(buttonWidth, 35);
                btnDarkMode.Location = new Point(btnStartLog.Right + buttonSpacing, currentY);
                btnDarkMode.Text = uiManager.IsDarkMode ? "Light Mode" : "Dark Mode";
                btnDarkMode.Tag = "btnDarkMode"; // Ensure it has the correct tag
                
                // Remove from controls and re-add to ensure event handlers are fresh
                parentForm.Controls.Remove(btnDarkMode);
                parentForm.Controls.Add(btnDarkMode);
                
                // Make sure the button is properly styled
                uiManager.StyleButton(btnDarkMode);
            }
            
            // Add the event handler
            btnDarkMode.Click -= toggleThemeHandler; // Remove first to avoid duplicates
            btnDarkMode.Click += toggleThemeHandler;
            Debug.WriteLine("Connected toggleThemeHandler to btnDarkMode");

            // Compact Mode Button - check if it already exists
            if (btnCompactMode == null)
            {
                btnCompactMode = new Button
                {
                    Text = "Compact Mode",
                    Size = new Size(buttonWidth, 35),
                    Location = new Point(btnDarkMode.Right + buttonSpacing, currentY),
                    Tag = "btnCompactMode"
                };
                uiManager.StyleButton(btnCompactMode);
                parentForm.Controls.Add(btnCompactMode);
            }
            else
            {
                // If the button already exists, just update its position and size
                btnCompactMode.Size = new Size(buttonWidth, 35);
                btnCompactMode.Location = new Point(btnDarkMode.Right + buttonSpacing, currentY);
                btnCompactMode.Text = uiManager.IsCompactMode ? "Normal Mode" : "Compact Mode";
                btnCompactMode.Tag = "btnCompactMode"; // Ensure it has the correct tag
                
                // Remove from controls and re-add to ensure event handlers are fresh
                parentForm.Controls.Remove(btnCompactMode);
                parentForm.Controls.Add(btnCompactMode);
                
                // Make sure the button is properly styled
                uiManager.StyleButton(btnCompactMode);
            }
            
            // Add the event handler
            btnCompactMode.Click -= toggleCompactModeHandler; // Remove first to avoid duplicates
            btnCompactMode.Click += toggleCompactModeHandler;
            Debug.WriteLine("Connected toggleCompactModeHandler to btnCompactMode");

            currentY += btnSystemInfo.Height + UIManager.ITEM_SPACING;
            
            // Add tooltips for buttons
            var buttonTooltip = new ToolTip();
            buttonTooltip.SetToolTip(btnStartLog, "Log performance data to a CSV file");
            buttonTooltip.SetToolTip(btnSystemInfo, "Show detailed system information");
            buttonTooltip.SetToolTip(btnDarkMode, "Toggle between dark and light themes");
            buttonTooltip.SetToolTip(btnCompactMode, "Toggle between normal and compact view");
            
            Debug.WriteLine("AddControlsSection completed");
        }
        
        /// <summary>
        /// Ensure all controls have proper tags for identification
        /// </summary>
        private void EnsureControlTags()
        {
            if (lblCPU != null && string.IsNullOrEmpty(lblCPU.Tag?.ToString())) lblCPU.Tag = "lblCPU";
            if (lblCPUTemp != null && string.IsNullOrEmpty(lblCPUTemp.Tag?.ToString())) lblCPUTemp.Tag = "lblCPUTemp";
            if (lblGPUTemp != null && string.IsNullOrEmpty(lblGPUTemp.Tag?.ToString())) lblGPUTemp.Tag = "lblGPUTemp";
            if (lblGPUUsage != null && string.IsNullOrEmpty(lblGPUUsage.Tag?.ToString())) lblGPUUsage.Tag = "lblGPUUsage";
            if (lblRAM != null && string.IsNullOrEmpty(lblRAM.Tag?.ToString())) lblRAM.Tag = "lblRAM";
            if (lblDiskRead != null && string.IsNullOrEmpty(lblDiskRead.Tag?.ToString())) lblDiskRead.Tag = "lblDiskRead";
            if (lblDiskWrite != null && string.IsNullOrEmpty(lblDiskWrite.Tag?.ToString())) lblDiskWrite.Tag = "lblDiskWrite";
            
            if (cpuProgressBar != null && string.IsNullOrEmpty(cpuProgressBar.Tag?.ToString())) cpuProgressBar.Tag = "cpuProgressBar";
            if (cpuTempProgressBar != null && string.IsNullOrEmpty(cpuTempProgressBar.Tag?.ToString())) cpuTempProgressBar.Tag = "cpuTempProgressBar";
            if (gpuTempProgressBar != null && string.IsNullOrEmpty(gpuTempProgressBar.Tag?.ToString())) gpuTempProgressBar.Tag = "gpuTempProgressBar";
            if (gpuUsageProgressBar != null && string.IsNullOrEmpty(gpuUsageProgressBar.Tag?.ToString())) gpuUsageProgressBar.Tag = "gpuUsageProgressBar";
            if (ramProgressBar != null && string.IsNullOrEmpty(ramProgressBar.Tag?.ToString())) ramProgressBar.Tag = "ramProgressBar";
            
            if (refreshRateControl != null && string.IsNullOrEmpty(refreshRateControl.Tag?.ToString())) refreshRateControl.Tag = "refreshRateControl";
            if (alwaysOnTopCheckBox != null && string.IsNullOrEmpty(alwaysOnTopCheckBox.Tag?.ToString())) alwaysOnTopCheckBox.Tag = "alwaysOnTopCheckBox";
            if (btnSystemInfo != null && string.IsNullOrEmpty(btnSystemInfo.Tag?.ToString())) btnSystemInfo.Tag = "btnSystemInfo";
            if (btnStartLog != null && string.IsNullOrEmpty(btnStartLog.Tag?.ToString())) btnStartLog.Tag = "btnStartLog";
            if (btnDarkMode != null && string.IsNullOrEmpty(btnDarkMode.Tag?.ToString())) btnDarkMode.Tag = "btnDarkMode";
            if (btnCompactMode != null && string.IsNullOrEmpty(btnCompactMode.Tag?.ToString())) btnCompactMode.Tag = "btnCompactMode";
        }
        
        /// <summary>
        /// Creates and configures the process monitor section
        /// </summary>
        public void CreateProcessMonitor()
        {
            try
            {
                // Remove any existing process list
                if (processListView != null)
                {
                    parentForm.Controls.Remove(processListView);
                    processListView.Dispose();
                    processListView = null;
                }
                
                // Remove any existing process title label
                var existingProcessTitle = parentForm.Controls.OfType<Label>()
                    .FirstOrDefault(l => l.Tag?.ToString() == "ProcessListTitle");
                if (existingProcessTitle != null)
                {
                    parentForm.Controls.Remove(existingProcessTitle);
                    existingProcessTitle.Dispose();
                    Debug.WriteLine("Removed existing process title label");
                }

                // Calculate position that's always visible
                int processY;
                if (uiManager.IsCompactMode)
                {
                    // In compact mode, position near the bottom but ensure it's visible
                    processY = parentForm.ClientSize.Height - PROCESS_LIST_HEIGHT - 70;
                }
                else
                {
                    // In normal mode, position after other controls
                    // Find the bottom-most control that isn't part of the process monitoring
                    int bottomMostControl = parentForm.Controls.OfType<Control>()
                        .Where(c => !(c is ListView) && c != titleBar)
                        .Select(c => c.Bottom)
                        .DefaultIfEmpty(titleBar.Height + 20)
                        .Max();
                    
                    processY = bottomMostControl + 20;
                }

                // Create a Label for the processes section
                Label processTitle = new Label
                {
                    Text = "Top Processes",
                    Font = uiManager.SectionFont,
                    ForeColor = uiManager.IsDarkMode ? Color.White : Color.Black,
                    AutoSize = true,
                    Location = new Point(UIManager.SIDE_PADDING, processY),
                    Tag = "ProcessListTitle"
                };
                parentForm.Controls.Add(processTitle);
                Debug.WriteLine("Created new process title label");

                // Create the list view with a smaller height
                processListView = new ListView
                {
                    Location = new Point(UIManager.SIDE_PADDING, processTitle.Bottom + 10),
                    Size = new Size(parentForm.ClientSize.Width - (UIManager.SIDE_PADDING * 2), PROCESS_LIST_HEIGHT),
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = false,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = uiManager.IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.White,
                    ForeColor = uiManager.IsDarkMode ? Color.White : Color.Black,
                    Font = uiManager.RegularFont, // Use the same font as other elements in the app
                };

                // Configure columns
                processListView.Columns.Add("Process", (int)(processListView.Width * 0.5));
                processListView.Columns.Add("CPU", (int)(processListView.Width * 0.25));
                processListView.Columns.Add("Memory", (int)(processListView.Width * 0.25));
                parentForm.Controls.Add(processListView);
                Debug.WriteLine($"Process list view created at Y={processListView.Top}, height={processListView.Height}");

                // Create and configure the timer for process monitoring
                if (processTimer != null)
                {
                    processTimer.Stop();
                    processTimer.Dispose();
                }
                
                processTimer = new System.Windows.Forms.Timer();
                processTimer.Interval = 2000;
                processTimer.Tick += (s, e) => 
                {
                    try
                    {
                        Debug.WriteLine("Process timer tick starting");
                        processListView.BeginUpdate();
                        processListView.Items.Clear();

                        // Get top processes by CPU usage
                        var topProcesses = processMonitor.GetTopProcessesByCpu(3);
                        Debug.WriteLine($"Retrieved {topProcesses.Count} top processes");
                        
                        // Add each process to the ListView
                        foreach (var process in topProcesses)
                        {
                            Debug.WriteLine($"Process: {process.Name}, CPU: {process.FormattedCpuUsage}, Memory: {process.FormattedMemoryUsage}");
                            var item = processMonitor.CreateProcessListViewItem(process, uiManager.IsDarkMode);
                            processListView.Items.Add(item);
                        }

                        // Update the process count
                        int totalProcesses = processMonitor.GetProcessCount();
                        Debug.WriteLine($"Total processes: {totalProcesses}");
                        if (processTitle != null)
                        {
                            processTitle.Text = $"Top Processes ({totalProcesses} running)";
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating process list: {ex.Message}");
                        
                        processListView.Items.Clear();
                        var errorItem = processMonitor.CreateErrorListViewItem(
                            "Error retrieving process data", 
                            Color.Red);
                        processListView.Items.Add(errorItem);
                    }
                    finally
                    {
                        processListView.EndUpdate();
                        Debug.WriteLine("Process timer tick completed");
                    }
                };

                processTimer.Start();
                Debug.WriteLine("Process timer started");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating process monitor: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Ensure all progress bars are visible
        /// </summary>
        public void EnsureProgressBarsVisible()
        {
            var progressBars = new[] {
                cpuProgressBar, cpuTempProgressBar, gpuTempProgressBar, 
                gpuUsageProgressBar, ramProgressBar
            };
            
            foreach (var bar in progressBars)
            {
                if (bar != null) bar.Visible = true;
            }
        }
        
        /// <summary>
        /// Get the current process timer
        /// </summary>
        public System.Windows.Forms.Timer GetProcessTimer()
        {
            return processTimer;
        }
        
        /// <summary>
        /// Reset layout for theme change
        /// </summary>
        public void ResetForThemeChange()
        {
            Debug.WriteLine("ResetForThemeChange called");
            
            // Clean up and recreate the process monitor with the updated theme
            CreateProcessMonitor();
            
            // Update button text
            if (btnDarkMode != null)
            {
                btnDarkMode.Text = uiManager.IsDarkMode ? "Light Mode" : "Dark Mode";
            }
            
            // Make sure all buttons are properly styled
            if (btnSystemInfo != null) 
            {
                uiManager.StyleButton(btnSystemInfo);
                Debug.WriteLine("Styled btnSystemInfo");
            }
            
            if (btnStartLog != null) 
            {
                uiManager.StyleButton(btnStartLog);
                Debug.WriteLine("Styled btnStartLog");
            }
            
            if (btnDarkMode != null) 
            {
                uiManager.StyleButton(btnDarkMode);
                Debug.WriteLine("Styled btnDarkMode");
            }
            
            if (btnCompactMode != null) 
            {
                uiManager.StyleButton(btnCompactMode);
                Debug.WriteLine("Styled btnCompactMode");
            }
            
            // Update refresh rate control styling
            if (refreshRateControl != null)
            {
                refreshRateControl.BackColor = uiManager.IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(230, 230, 230);
                refreshRateControl.ForeColor = uiManager.IsDarkMode ? Color.White : Color.Black;
                Debug.WriteLine("Styled refreshRateControl");
            }
            
            // Update checkbox styling
            if (alwaysOnTopCheckBox != null)
            {
                alwaysOnTopCheckBox.ForeColor = uiManager.IsDarkMode ? Color.White : Color.Black;
                Debug.WriteLine("Styled alwaysOnTopCheckBox");
            }
            
            Debug.WriteLine("ResetForThemeChange completed");
        }
    }
}
