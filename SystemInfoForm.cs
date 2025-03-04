using System;
using System.Drawing;
using System.Windows.Forms;
using System.Management;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SysVital
{
    public class SystemInfoForm : Form
    {
        private const int padding = 10;
        private readonly IComputer computer;
        private TabControl tabControl;
        
        // Font definitions to match main form
        private readonly Font titleFont = new Font("Segoe UI", 20, FontStyle.Bold);
        private readonly Font sectionFont = new Font("Segoe UI", 14, FontStyle.Bold);
        private readonly Font regularFont = new Font("Segoe UI", 11, FontStyle.Bold);
        private readonly Font contentFont = new Font("Segoe UI", 11, FontStyle.Regular);

        // Custom title bar elements
        private Panel titleBar;
        private Label titleLabel;
        private Button closeButton;
        
        // Colors matching the main form
        private Color darkBackground = Color.FromArgb(32, 32, 32);
        private Color darkText = Color.FromArgb(220, 220, 220);

        // Required for window dragging
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public SystemInfoForm(IComputer computer)
        {
            this.computer = computer;
            InitializeComponents();
            LoadSystemInfo();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(400, 480);
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = darkBackground;
            this.ForeColor = darkText;
            
            // Create custom title bar
            CreateCustomTitleBar();
            
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = regularFont;
            tabControl.Padding = new Point(10, 6);
            
            TabPage cpuTab = CreateTabPage("CPU");
            TabPage gpuTab = CreateTabPage("GPU");
            TabPage ramTab = CreateTabPage("Memory");
            TabPage storageTab = CreateTabPage("Storage");
            
            tabControl.TabPages.AddRange(new TabPage[] { cpuTab, gpuTab, ramTab, storageTab });
            this.Controls.Add(tabControl);
            
            // Add padding around the tab control to match the main form's padding
            tabControl.Location = new Point(padding, titleBar.Bottom + padding);
            tabControl.Size = new Size(
                this.ClientSize.Width - (padding * 2),
                this.ClientSize.Height - titleBar.Height - (padding * 2)
            );
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        }

        private void CreateCustomTitleBar()
        {
            // Create title bar panel
            titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(45, 45, 48)
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
                Debug.WriteLine("SystemInfoForm: Attempting to load logo...");
                
                // Try loading from Resources folder (relative path)
                string logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "SysVitalLogo.png");
                Debug.WriteLine($"SystemInfoForm: Trying path: {logoPath}");
                
                if (System.IO.File.Exists(logoPath))
                {
                    logoPictureBox.Image = Image.FromFile(logoPath);
                    Debug.WriteLine("SystemInfoForm: Logo loaded successfully from BaseDirectory path");
                }
                else
                {
                    // Try project directory path
                    logoPath = "Resources\\SysVitalLogo.png";
                    Debug.WriteLine($"SystemInfoForm: Trying path: {logoPath}");
                    
                    if (System.IO.File.Exists(logoPath))
                    {
                        logoPictureBox.Image = Image.FromFile(logoPath);
                        Debug.WriteLine("SystemInfoForm: Logo loaded successfully from relative path");
                    }
                    else
                    {
                        // Try one more path - the executable directory
                        logoPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.ExecutablePath), "Resources", "SysVitalLogo.png");
                        Debug.WriteLine($"SystemInfoForm: Trying path: {logoPath}");
                        
                        if (System.IO.File.Exists(logoPath))
                        {
                            logoPictureBox.Image = Image.FromFile(logoPath);
                            Debug.WriteLine("SystemInfoForm: Logo loaded successfully from executable path");
                        }
                        else
                        {
                            Debug.WriteLine("SystemInfoForm: Could not find logo file in any location");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SystemInfoForm: Error loading logo: {ex.Message}");
            }
            
            titleBar.Controls.Add(logoPictureBox);

            // Add title
            titleLabel = new Label
            {
                Text = "System Information",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = darkText,
                Location = new Point(logoPictureBox.Right + 8, 8),
                AutoSize = true
            };
            titleBar.Controls.Add(titleLabel);

            // Close button
            closeButton = new Button
            {
                Text = "×",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = darkText,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Location = new Point(titleBar.Width - 40, 5),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                TabStop = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => this.Close();

            // Add hover effects
            closeButton.MouseEnter += (s, e) => {
                closeButton.BackColor = Color.FromArgb(232, 17, 35);
                closeButton.ForeColor = Color.White;
            };
            closeButton.MouseLeave += (s, e) => {
                closeButton.BackColor = Color.Transparent;
                closeButton.ForeColor = darkText;
            };

            // Add controls to title bar
            titleBar.Controls.Add(closeButton);
            
            // Enable dragging the form by the title bar
            titleBar.MouseDown += TitleBar_MouseDown;
            titleLabel.MouseDown += TitleBar_MouseDown;
            logoPictureBox.MouseDown += TitleBar_MouseDown;

            // Add title bar to form
            this.Controls.Add(titleBar);
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private TabPage CreateTabPage(string title)
        {
            TabPage page = new TabPage(title);
            page.BackColor = darkBackground;
            
            RichTextBox textBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = darkBackground,
                ForeColor = darkText,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Padding = new Padding(10)
            };
            
            page.Controls.Add(textBox);
            return page;
        }

        private void LoadSystemInfo()
        {
            try
            {
                // CPU Info
                var cpuText = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        cpuText.AppendLine($"CPU: {obj["Name"]}");
                        cpuText.AppendLine($"Cores: {obj["NumberOfCores"]}");
                        cpuText.AppendLine($"Threads: {obj["NumberOfLogicalProcessors"]}");
                        cpuText.AppendLine($"Base Speed: {obj["MaxClockSpeed"]} MHz");
                    }
                }
                ((RichTextBox)tabControl.TabPages[0].Controls[0]).Text = cpuText.ToString();

                // GPU Info
                var gpuText = new StringBuilder();
                var gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia) ??
                         computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuAmd) ??
                         computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuIntel);
                
                if (gpu != null)
                {
                    gpuText.AppendLine($"GPU: {gpu.Name}");
                    gpu.Update();
                    foreach (var sensor in gpu.Sensors)
                    {
                        string unit = "";
                        switch (sensor.SensorType)
                        {
                            case SensorType.Temperature:
                                unit = "°C";
                                break;
                            case SensorType.Load:
                                unit = "%";
                                break;
                            case SensorType.Clock:
                                unit = "MHz";
                                break;
                            case SensorType.Power:
                                unit = "W";
                                break;
                            case SensorType.Fan:
                                unit = "RPM";
                                break;
                        }
                        
                        if (sensor.Value.HasValue)
                        {
                            gpuText.AppendLine($"{sensor.Name}: {sensor.Value:F1} {unit}");
                        }
                    }
                }
                ((RichTextBox)tabControl.TabPages[1].Controls[0]).Text = gpuText.ToString();

                // RAM Info
                var ramText = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        ramText.AppendLine($"Capacity: {Convert.ToInt64(obj["Capacity"]) / (1024 * 1024 * 1024)} GB");
                        ramText.AppendLine($"Speed: {obj["Speed"]} MHz");
                        ramText.AppendLine($"Manufacturer: {obj["Manufacturer"]}");
                        ramText.AppendLine();
                    }
                }
                ((RichTextBox)tabControl.TabPages[2].Controls[0]).Text = ramText.ToString();

                // Storage Info
                var storageText = new StringBuilder();
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        storageText.AppendLine($"Drive: {obj["Caption"]}");
                        storageText.AppendLine($"Size: {Convert.ToInt64(obj["Size"]) / (1024 * 1024 * 1024)} GB");
                        storageText.AppendLine($"Interface: {obj["InterfaceType"]}");
                        storageText.AppendLine();
                    }
                }
                ((RichTextBox)tabControl.TabPages[3].Controls[0]).Text = storageText.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading system information: {ex.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 