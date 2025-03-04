/*
 * PC Performance Monitor
 * A real-time system monitoring application that tracks CPU, GPU, RAM, and disk metrics
 * Author: Alex Wagner
 * Created: 10/20/2024
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;
using LibreHardwareMonitor.Hardware;
using System.Runtime.InteropServices;
using SysVital.Properties;
using System.Threading;
using System.IO;
using System.Security.Principal;

namespace SysVital
{
    // Restore 'partial' keyword since there may be a Form1.Designer.cs file
    public partial class Form1 : Form
    {
        // UI Manager
        private UIManager uiManager;
        
        // Form Layout Manager
        private FormLayoutManager layoutManager;

        // Performance counters
        private PerformanceCounter cpuCounter = null;
        private PerformanceCounter ramCounter = null;
        private PerformanceCounter diskReadCounter;
        private PerformanceCounter diskWriteCounter;
        
        // Controls not in the designer - only declare these
        // NOTE TO DEVELOPERS: The following UI controls are NOT defined in Form1.Designer.cs 
        // and must be maintained here. Other controls like lblCPU, lblRAM, lblDiskRead, 
        // and lblDiskWrite are defined in the designer file and should not be redeclared here
        // to avoid ambiguity errors.
        private Label lblCPUTemp;
        private Label lblGPUTemp;
        private Label lblGPUUsage;
        
        // System metrics
        private ulong totalMemory;
        private LibreHardwareMonitor.Hardware.Computer computer;
        private System.Windows.Forms.Timer updateTimer;

        // Hardware monitoring
        private readonly UpdateVisitor updateVisitor;
        private bool isUpdating = false;
        
        // Performance logging
        private PerformanceLogger logger = null;

        // Process monitoring
        private readonly ProcessMonitor processMonitor = new ProcessMonitor();
        private System.Windows.Forms.Timer processTimer;
        private ListView processListView;

        // Hardware monitoring visitor implementation
        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        // Initialize form and set background color
        public Form1()
        {
            InitializeComponent();
            
            // Create the hardware monitoring visitor
            updateVisitor = new UpdateVisitor();
            
            // Create missing controls
            InitializeMissingControls();
            
            // Create the UI Manager and Layout Manager
            uiManager = new UIManager();
            layoutManager = new FormLayoutManager(this, uiManager);
            
            // Create title bar - make sure we only create it once
            // First check if a title bar already exists
            var existingTitleBar = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "TitleBar");
            if (existingTitleBar != null)
            {
                // Remove existing title bar to avoid duplicates
                this.Controls.Remove(existingTitleBar);
                existingTitleBar.Dispose();
            }
            
            // Now create a new title bar
            Panel titleBar = uiManager.CreateCustomTitleBar(this, "SysVital", (s, e) => this.Close(), (s, e) => this.WindowState = FormWindowState.Minimized);
            titleBar.Tag = "TitleBar"; // Add tag for finding it later
            this.Controls.Add(titleBar);
            uiManager.ApplyModernStyle(this, titleBar);
        }
        
        // Initialize controls not defined in the designer
        private void InitializeMissingControls()
        {
            // Initialize only controls not in the designer
            lblCPUTemp = new Label { Tag = "lblCPUTemp" };
            lblGPUTemp = new Label { Tag = "lblGPUTemp" };
            lblGPUUsage = new Label { Tag = "lblGPUUsage" };
            
            // Add the labels to the form
            this.Controls.AddRange(new Control[] {
                lblCPUTemp, lblGPUTemp, lblGPUUsage
            });
        }

        // Initialize performance counters for system metrics
        private void InitializeCounters()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
        }

        // Set up UI elements and initialize monitoring systems
        private void Form1_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("Form1_Load starting");
            this.Text = "SysVital";
            this.Size = new Size(UIManager.WINDOW_WIDTH, UIManager.WINDOW_HEIGHT);
            
            // Clean up any stray labels that might exist
            CleanupStrayLabels();
            
            // Initialize hardware monitoring
            GetTotalMemory();
            InitializeCounters();
            InitializeHardwareMonitor();

            // Create update timer
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 2000; // Default 2 seconds
            updateTimer.Tick += UpdateTimer_Tick;
            
            // Initialize control references
            InitializeControlReferences();
            
            // Setup form layout and connect event handlers
            layoutManager.InitializeLayout(
                refreshRateControl_ValueChanged,
                AlwaysOnTopCheckBox_CheckedChanged,
                ShowSystemInfo,
                StartLogging_Click,
                ToggleTheme,
                ToggleCompactMode
            );
            
            // Explicitly set tags for the label controls from the designer
            if (lblCPU != null) lblCPU.Tag = "lblCPU";
            if (lblRAM != null) lblRAM.Tag = "lblRAM";
            if (lblDiskRead != null) lblDiskRead.Tag = "lblDiskRead";
            if (lblDiskWrite != null) lblDiskWrite.Tag = "lblDiskWrite";
            
            // Start update timer
            updateTimer.Start();
            Debug.WriteLine("Form1_Load completed");
        }
        
        // Clean up any stray labels that might exist
        private void CleanupStrayLabels()
        {
            // Find and remove any stray labels
            var strayLabels = this.Controls.OfType<Label>()
                .Where(l => (l.Text == "System" || l.Text == "System Performance") && 
                           string.IsNullOrEmpty(l.Tag?.ToString()))
                .ToList();
                
            foreach (var label in strayLabels)
            {
                this.Controls.Remove(label);
                label.Dispose();
                Debug.WriteLine($"Removed stray label with text: {label.Text}");
            }
            
            // Find and remove any stray buttons
            var strayButtons = this.Controls.OfType<Button>()
                .Where(b => (b.Text == "System" || b.Text.Contains("System")) && 
                           (string.IsNullOrEmpty(b.Tag?.ToString()) || b.Tag.ToString() != "btnSystemInfo"))
                .ToList();
                
            foreach (var button in strayButtons)
            {
                this.Controls.Remove(button);
                button.Dispose();
                Debug.WriteLine($"Removed stray button with text: {button.Text}");
            }
        }

        // Handler for system info button
        private void ShowSystemInfo(object sender, EventArgs e)
        {
            var sysInfoForm = new SystemInfoForm(computer);
            sysInfoForm.ShowDialog();
        }

        // Initialize control references for the layout manager
        private void InitializeControlReferences()
        {
            // Create dictionary of control references to pass to layout manager
            var controlMap = new Dictionary<string, Control>();
            
            // Add all label controls - some from designer, some we created
            controlMap.Add("lblCPU", lblCPU);
            controlMap.Add("lblCPUTemp", lblCPUTemp);
            controlMap.Add("lblGPUTemp", lblGPUTemp);
            controlMap.Add("lblGPUUsage", lblGPUUsage);
            controlMap.Add("lblRAM", lblRAM);
            controlMap.Add("lblDiskRead", lblDiskRead);
            controlMap.Add("lblDiskWrite", lblDiskWrite);
            
            // Check for existing controls before creating new ones
            // CPU Progress Bar
            ModernProgressBar cpuProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "cpuProgressBar");
            if (cpuProgressBar == null)
            {
                cpuProgressBar = new ModernProgressBar { Tag = "cpuProgressBar" };
                this.Controls.Add(cpuProgressBar);
            }
            
            // CPU Temperature Progress Bar
            ModernProgressBar cpuTempProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "cpuTempProgressBar");
            if (cpuTempProgressBar == null)
            {
                cpuTempProgressBar = new ModernProgressBar { UseTemperatureColors = true, Tag = "cpuTempProgressBar" };
                this.Controls.Add(cpuTempProgressBar);
            }
            
            // GPU Temperature Progress Bar
            ModernProgressBar gpuTempProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "gpuTempProgressBar");
            if (gpuTempProgressBar == null)
            {
                gpuTempProgressBar = new ModernProgressBar { UseTemperatureColors = true, Tag = "gpuTempProgressBar" };
                this.Controls.Add(gpuTempProgressBar);
            }
            
            // GPU Usage Progress Bar
            ModernProgressBar gpuUsageProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "gpuUsageProgressBar");
            if (gpuUsageProgressBar == null)
            {
                gpuUsageProgressBar = new ModernProgressBar { Tag = "gpuUsageProgressBar" };
                this.Controls.Add(gpuUsageProgressBar);
            }
            
            // RAM Progress Bar
            ModernProgressBar ramProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "ramProgressBar");
            if (ramProgressBar == null)
            {
                ramProgressBar = new ModernProgressBar { Tag = "ramProgressBar" };
                this.Controls.Add(ramProgressBar);
            }
            
            // Refresh Rate Control
            NumericUpDown refreshRateControl = this.Controls.OfType<NumericUpDown>().FirstOrDefault(c => c.Tag?.ToString() == "refreshRateControl");
            if (refreshRateControl == null)
            {
                refreshRateControl = new NumericUpDown { Tag = "refreshRateControl" };
                this.Controls.Add(refreshRateControl);
            }
            
            // Always On Top CheckBox
            CheckBox alwaysOnTopCheckBox = this.Controls.OfType<CheckBox>().FirstOrDefault(c => c.Tag?.ToString() == "alwaysOnTopCheckBox");
            if (alwaysOnTopCheckBox == null)
            {
                alwaysOnTopCheckBox = new CheckBox { Text = "Always on Top", Tag = "alwaysOnTopCheckBox" };
                this.Controls.Add(alwaysOnTopCheckBox);
            }
            
            // System Info Button
            Button btnSystemInfo = this.Controls.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "btnSystemInfo");
            if (btnSystemInfo == null)
            {
                btnSystemInfo = new Button { Text = "System Info", Tag = "btnSystemInfo" };
                this.Controls.Add(btnSystemInfo);
            }
            
            // Start Log Button
            Button btnStartLog = this.Controls.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "btnStartLog");
            if (btnStartLog == null)
            {
                btnStartLog = new Button { Text = "Start Logging", Tag = "btnStartLog" };
                this.Controls.Add(btnStartLog);
            }
            
            // Dark Mode Button
            Button btnDarkMode = this.Controls.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "btnDarkMode");
            if (btnDarkMode == null)
            {
                btnDarkMode = new Button { Tag = "btnDarkMode" };
                this.Controls.Add(btnDarkMode);
            }
            
            // Compact Mode Button
            Button btnCompactMode = this.Controls.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "btnCompactMode");
            if (btnCompactMode == null)
            {
                btnCompactMode = new Button { Text = "Compact Mode", Tag = "btnCompactMode" };
                this.Controls.Add(btnCompactMode);
            }
            
            // Add controls to the map
            controlMap.Add("cpuProgressBar", cpuProgressBar);
            controlMap.Add("cpuTempProgressBar", cpuTempProgressBar);
            controlMap.Add("gpuTempProgressBar", gpuTempProgressBar);
            controlMap.Add("gpuUsageProgressBar", gpuUsageProgressBar);
            controlMap.Add("ramProgressBar", ramProgressBar);
            controlMap.Add("refreshRateControl", refreshRateControl);
            controlMap.Add("alwaysOnTopCheckBox", alwaysOnTopCheckBox);
            controlMap.Add("btnSystemInfo", btnSystemInfo);
            controlMap.Add("btnStartLog", btnStartLog);
            controlMap.Add("btnDarkMode", btnDarkMode);
            controlMap.Add("btnCompactMode", btnCompactMode);
            
            // Find title bar
            var titleBar = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "TitleBar");
            if (titleBar != null)
            {
                controlMap.Add("titleBar", titleBar);
            }
            
            layoutManager.SetControlReferences(controlMap);
        }

        private void InitializeHardwareMonitor()
        {
            try
            {
                // Check if running as administrator
                bool isAdmin = IsRunningAsAdministrator();
                
                computer = new LibreHardwareMonitor.Hardware.Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = true
                };
                computer.Open();
                computer.Accept(updateVisitor);
                
                if (!isAdmin)
                {
                    // Display a non-blocking notification about limited functionality
                    ShowAdminRequiredNotification();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing hardware monitor: {ex.Message}");
                MessageBox.Show("Error initializing hardware monitoring. Some features may not work correctly.\n\n" +
                               "This may be due to insufficient privileges. Try running as administrator.",
                               "Initialization Error",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
            }
        }
        
        private bool IsRunningAsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
        
        private void ShowAdminRequiredNotification()
        {
            // Create a notification label
            Label adminNotice = new Label
            {
                Text = "Limited functionality: Not running as administrator",
                ForeColor = Color.Red,
                BackColor = Color.Transparent,
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Tag = "adminNotice"
            };
            
            // Position at the bottom of the form
            adminNotice.Location = new Point(
                (this.ClientSize.Width - adminNotice.Width) / 2,
                this.ClientSize.Height - adminNotice.Height - 5
            );
            
            // Add to form
            this.Controls.Add(adminNotice);
            adminNotice.BringToFront();
            
            // Make sure it stays visible
            this.SizeChanged += (sender, e) => 
            {
                adminNotice.Location = new Point(
                    (this.ClientSize.Width - adminNotice.Width) / 2,
                    this.ClientSize.Height - adminNotice.Height - 5
                );
            };
        }

        // Retrieve total system RAM using WMI
        private void GetTotalMemory()
        {
            try
            {
                ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
                ManagementObjectCollection results = searcher.Get();

                foreach (ManagementObject result in results)
                {
                    totalMemory = Convert.ToUInt64(result["TotalPhysicalMemory"]);
                    break; // Only need first result
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting total memory: " + ex.Message);
                totalMemory = 1; // Prevent division by zero
            }
        }

        private void StartLogging_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    logger = new PerformanceLogger(dialog.FileName);
                    logger.StartLogging();
                    ((Button)sender).Text = "Stop Logging";
                    ((Button)sender).Click -= StartLogging_Click;
                    ((Button)sender).Click += StopLogging_Click;
                }
            }
        }

        private void StopLogging_Click(object sender, EventArgs e)
        {
            if (logger != null)
            {
                logger.StopLogging();
                ((Button)sender).Text = "Start Logging";
                ((Button)sender).Click -= StopLogging_Click;
                ((Button)sender).Click += StartLogging_Click;
            }
        }

        private void ToggleTheme(object sender, EventArgs e)
        {
            Debug.WriteLine("ToggleTheme called");
            
            // Toggle theme in UI Manager
            uiManager.ToggleTheme();
            
            // First, remove the old title bar
            var titleBar = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "TitleBar");
            if (titleBar != null)
            {
                this.Controls.Remove(titleBar);
                titleBar.Dispose();
            }
            
            // Remove any stray "System" labels that might have been created
            var systemLabels = this.Controls.OfType<Label>().Where(l => l.Text == "System" || l.Text == "System Performance").ToList();
            foreach (var label in systemLabels)
            {
                this.Controls.Remove(label);
                label.Dispose();
            }
            
            // Remove any stray "Top Processes" labels that might have been created
            var processLabels = this.Controls.OfType<Label>().Where(l => 
                l.Text.Contains("Top Processes") || 
                l.Tag?.ToString() == "ProcessListTitle").ToList();
            foreach (var label in processLabels)
            {
                this.Controls.Remove(label);
                label.Dispose();
                Debug.WriteLine("Removed stray Top Processes label");
            }
            
            // Remove any stray "System" buttons that might have been created
            var systemButtons = this.Controls.OfType<Button>().Where(b => b.Text == "System" || b.Text.Contains("System")).ToList();
            foreach (var button in systemButtons)
            {
                // Only remove if it doesn't have a proper tag
                if (string.IsNullOrEmpty(button.Tag?.ToString()) || button.Tag.ToString() != "btnSystemInfo")
                {
                    this.Controls.Remove(button);
                    button.Dispose();
                    Debug.WriteLine("Removed stray System button");
                }
            }

            // Create new title bar with updated theme (fix parameter name collision)
            titleBar = uiManager.CreateCustomTitleBar(this, "SysVital", 
                (sender2, args) => this.Close(), 
                (sender2, args) => this.WindowState = FormWindowState.Minimized);
            titleBar.Tag = "TitleBar";
            this.Controls.Add(titleBar);

            // Apply updated theme to form and all controls
            this.BackColor = uiManager.IsDarkMode ? uiManager.DarkBackground : uiManager.LightBackground;

            // Apply style to all controls
            foreach (Control control in this.Controls)
            {
                uiManager.ApplyStyleToControl(control);
                
                // Ensure buttons have white text in dark mode
                if (control is Button button)
                {
                    button.ForeColor = uiManager.IsDarkMode ? Color.White : Color.Black;
                    Debug.WriteLine($"Set button '{button.Text}' text color to {(uiManager.IsDarkMode ? "White" : "Black")}");
                }
                
                // Ensure labels have white text in dark mode
                if (control is Label label)
                {
                    label.ForeColor = uiManager.IsDarkMode ? Color.White : Color.Black;
                    Debug.WriteLine($"Set label '{label.Text}' text color to {(uiManager.IsDarkMode ? "White" : "Black")}");
                }
            }
            
            // Remove any controls marked for removal
            var controlsToRemove = this.Controls.OfType<Control>().Where(c => c.Tag?.ToString() == "ToBeRemoved").ToList();
            foreach (var control in controlsToRemove)
            {
                this.Controls.Remove(control);
                control.Dispose();
            }
            
            // Update layout for theme changes
            layoutManager.ResetForThemeChange();
            
            // Reconnect event handlers for all buttons
            var btnSystemInfo = this.Controls.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "btnSystemInfo");
            if (btnSystemInfo != null)
            {
                btnSystemInfo.Click -= ShowSystemInfo;
                btnSystemInfo.Click += ShowSystemInfo;
                Debug.WriteLine("Reconnected ShowSystemInfo handler");
            }
            
            var btnStartLog = this.Controls.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "btnStartLog");
            if (btnStartLog != null)
            {
                btnStartLog.Click -= StartLogging_Click;
                btnStartLog.Click += StartLogging_Click;
                Debug.WriteLine("Reconnected StartLogging_Click handler");
            }
            
            var btnDarkMode = this.Controls.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "btnDarkMode");
            if (btnDarkMode != null)
            {
                btnDarkMode.Click -= ToggleTheme;
                btnDarkMode.Click += ToggleTheme;
                Debug.WriteLine("Reconnected ToggleTheme handler");
            }
            
            var btnCompactMode = this.Controls.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "btnCompactMode");
            if (btnCompactMode != null)
            {
                btnCompactMode.Click -= ToggleCompactMode;
                btnCompactMode.Click += ToggleCompactMode;
                Debug.WriteLine("Reconnected ToggleCompactMode handler");
            }
            
            Debug.WriteLine("ToggleTheme completed");
        }

        private void ToggleCompactMode(object sender, EventArgs e)
        {
            Debug.WriteLine("ToggleCompactMode called");
            this.SuspendLayout();

            uiManager.ToggleCompactMode();
            Debug.WriteLine($"Compact mode toggled. IsCompactMode: {uiManager.IsCompactMode}");
            
            // Ensure we have the correct button reference
            Button btnCompactMode = sender as Button;
            if (btnCompactMode == null)
            {
                btnCompactMode = this.Controls.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString() == "btnCompactMode");
                Debug.WriteLine($"Found compact mode button: {btnCompactMode != null}");
            }
            
            // Remove any existing "Top Processes" labels to prevent duplication
            var processLabels = this.Controls.OfType<Label>().Where(l => 
                l.Text.Contains("Top Processes") || 
                l.Tag?.ToString() == "ProcessListTitle").ToList();
            foreach (var label in processLabels)
            {
                this.Controls.Remove(label);
                label.Dispose();
                Debug.WriteLine("Removed Top Processes label before mode switch");
            }
            
            if (uiManager.IsCompactMode)
            {
                Debug.WriteLine("Switching to compact mode");
                // Create a list of essential controls to keep visible in compact mode
                var essentialControls = new List<Control>();
                
                // Find essential controls by tag
                var lblCPU = this.Controls.OfType<Label>().FirstOrDefault(l => l.Tag?.ToString() == "lblCPU" || l == this.lblCPU);
                var lblCPUTemp = this.Controls.OfType<Label>().FirstOrDefault(l => l.Tag?.ToString() == "lblCPUTemp" || l == this.lblCPUTemp);
                var lblGPUTemp = this.Controls.OfType<Label>().FirstOrDefault(l => l.Tag?.ToString() == "lblGPUTemp" || l == this.lblGPUTemp);
                var lblGPUUsage = this.Controls.OfType<Label>().FirstOrDefault(l => l.Tag?.ToString() == "lblGPUUsage" || l == this.lblGPUUsage);
                var lblRAM = this.Controls.OfType<Label>().FirstOrDefault(l => l.Tag?.ToString() == "lblRAM" || l == this.lblRAM);
                
                // Add found controls to essential list
                if (lblCPU != null) essentialControls.Add(lblCPU);
                if (lblCPUTemp != null) essentialControls.Add(lblCPUTemp);
                if (lblGPUTemp != null) essentialControls.Add(lblGPUTemp);
                if (lblGPUUsage != null) essentialControls.Add(lblGPUUsage);
                if (lblRAM != null) essentialControls.Add(lblRAM);
                
                Debug.WriteLine($"Found {essentialControls.Count} essential controls");
                
                // Find the controls by their tags
                var alwaysOnTopCheckBox = this.Controls.OfType<CheckBox>().FirstOrDefault(c => c.Tag?.ToString() == "alwaysOnTopCheckBox");
                var titleBar = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Tag?.ToString() == "TitleBar");
                
                if (btnCompactMode != null)
                {
                    btnCompactMode.Text = "Normal Mode"; // Update button text
                    // Ensure the button has the correct style
                    uiManager.StyleButton(btnCompactMode);
                }
                
                uiManager.SwitchToCompactMode(this, essentialControls, alwaysOnTopCheckBox, btnCompactMode, titleBar);
            }
            else
            {
                Debug.WriteLine("Switching to normal mode");
                if (btnCompactMode != null)
                {
                    btnCompactMode.Text = "Compact Mode"; // Update button text
                    // Ensure the button has the correct style
                    uiManager.StyleButton(btnCompactMode);
                }
                
                uiManager.SwitchToNormalMode(this);
                layoutManager.LayoutControls(
                    ShowSystemInfo,
                    StartLogging_Click,
                    ToggleTheme,
                    ToggleCompactMode
                );
                layoutManager.EnsureProgressBarsVisible();
            }

            this.ResumeLayout(true);
            Debug.WriteLine("ToggleCompactMode completed");
        }
        
        // Toggle always-on-top functionality
        private void AlwaysOnTopCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                var alwaysOnTopCheckBox = (CheckBox)sender;
                this.TopMost = alwaysOnTopCheckBox.Checked;
                Debug.WriteLine($"Always on top set to: {this.TopMost}");
                
                // Show a brief notification to confirm the change
                string statusMessage = alwaysOnTopCheckBox.Checked ? 
                    "Window will stay on top" : 
                    "Window will behave normally";
                    
                ToolTip tooltip = new ToolTip();
                tooltip.Show(statusMessage, alwaysOnTopCheckBox, 20, -20, 1000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting TopMost: {ex.Message}");
            }
        }

        // Retrieve CPU temperature using LibreHardwareMonitor
        private float GetCPUTemperature()
        {
            try
            {
                var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                if (cpu != null)
                {
                    var tempSensor = cpu.Sensors
                        .FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Value.HasValue);
                    
                    return tempSensor?.Value ?? 0;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCPUTemperature: {ex.Message}");
                return 0;
            }
        }

        // Retrieve GPU temperature and usage information
        private (float temperature, float usage) GetGPUInfo()
        {
            try
            {
                var gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia) ??
                         computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuAmd) ??
                         computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuIntel);

                if (gpu != null)
                {
                    float temperature = 0;
                    float usage = 0;

                    // Get core temperature
                    var tempSensor = gpu.Sensors.FirstOrDefault(s => 
                        s.SensorType == SensorType.Temperature && 
                        s.Name.Contains("GPU Core"));
                    
                    if (tempSensor != null)
                        temperature = tempSensor.Value ?? 0;
                    
                    // Get core usage
                    var usageSensor = gpu.Sensors.FirstOrDefault(s => 
                        s.SensorType == SensorType.Load && 
                        s.Name.Contains("GPU Core"));
                    
                    if (usageSensor != null)
                        usage = usageSensor.Value ?? 0;

                    return (temperature, usage);
                }
                return (0, 0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetGPUInfo: {ex.Message}");
                return (0, 0);
            }
        }

        // Timer event handler for updating display
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (isUpdating) return;

            try
            {
                isUpdating = true;

                // Use the existing visitor to update all hardware components at once
                computer.Accept(updateVisitor);

                // Get all needed metrics in one block
                float cpuUsage = cpuCounter.NextValue();
                float cpuTemp = GetCPUTemperature();
                var (gpuTemp, gpuUsage) = GetGPUInfo();
                float ramUsage = ramCounter.NextValue();
                float diskReadSpeed = diskReadCounter.NextValue() / 1024 / 1024; // Convert to MB/s
                float diskWriteSpeed = diskWriteCounter.NextValue() / 1024 / 1024; // Convert to MB/s
                float usedMemoryPercentage = (totalMemory - (ulong)ramUsage * 1024 * 1024) / (float)totalMemory * 100;

                // Update UI on the UI thread
                BeginInvoke((MethodInvoker)delegate
                {
                    // Update CPU metrics
                    lblCPU.Text = $"CPU Usage: {cpuUsage:F1}%";
                    lblCPUTemp.Text = cpuTemp > 0
                        ? $"CPU Temperature: {cpuTemp:F1}°C"
                        : "CPU Temperature: Not available";
                    
                    var cpuProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "cpuProgressBar");
                    var cpuTempProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "cpuTempProgressBar");
                    
                    if (cpuProgressBar != null) cpuProgressBar.Value = (int)cpuUsage;
                    if (cpuTempProgressBar != null) cpuTempProgressBar.Value = (int)cpuTemp;

                    // Update GPU metrics
                    lblGPUUsage.Text = $"GPU Usage: {gpuUsage:F1}%";
                    lblGPUTemp.Text = gpuTemp > 0
                        ? $"GPU Temperature: {gpuTemp:F1}°C"
                        : "GPU Temperature: Not available";
                    
                    var gpuUsageProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "gpuUsageProgressBar");
                    var gpuTempProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "gpuTempProgressBar");
                    
                    if (gpuUsageProgressBar != null) gpuUsageProgressBar.Value = (int)gpuUsage;
                    if (gpuTempProgressBar != null) gpuTempProgressBar.Value = (int)gpuTemp;

                    // Update memory metrics
                    lblRAM.Text = $"Available Memory: {ramUsage:F0} MB ({usedMemoryPercentage:F1}% Used)";
                    
                    var ramProgressBar = this.Controls.OfType<ModernProgressBar>().FirstOrDefault(p => p.Tag?.ToString() == "ramProgressBar");
                    if (ramProgressBar != null) ramProgressBar.Value = (int)usedMemoryPercentage;
                    
                    // Update disk metrics
                    lblDiskRead.Text = $"Disk Read: {diskReadSpeed:F2} MB/s";
                    lblDiskWrite.Text = $"Disk Write: {diskWriteSpeed:F2} MB/s";
                });

                // Add logging - make sure this happens after we have all the values
                if (logger != null && logger.IsLogging)
                {
                    logger.LogData(cpuUsage, cpuTemp, gpuUsage, gpuTemp, ramUsage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating stats: {ex.Message}");
            }
            finally
            {
                isUpdating = false;
            }
        }

        // Handle refresh rate changes
        private void refreshRateControl_ValueChanged(object sender, EventArgs e)
        {
            var refreshRateControl = (NumericUpDown)sender;
            // Ensure timer interval is never zero - minimum 1000ms (1 second)
            int interval = Math.Max(1000, (int)(refreshRateControl.Value * 1000));
            updateTimer.Interval = interval;
        }

        // Clean up resources on form close
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (computer != null)
                {
                    computer.Close();
                }
                
                // Stop process timer if running
                var processTimer = layoutManager.GetProcessTimer();
                if (processTimer != null)
                {
                    processTimer.Stop();
                    processTimer.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing hardware monitor: {ex.Message}");
            }
            base.OnFormClosed(e);
        }
    }
}
