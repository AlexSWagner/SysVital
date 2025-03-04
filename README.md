# SysVital - System Monitoring Tool (Prviously PCMonitor)

SysVital is a lightweight, modern system monitoring application for Windows that provides real-time information about your computer's hardware performance.

## Hardware Compatibility

**Important Note:** SysVital has been tested and optimized specifically for:
- Intel CPUs
- NVIDIA GPUs

While the application may work with AMD processors and graphics cards, full functionality cannot be guaranteed as testing was performed exclusively on Intel/NVIDIA hardware configurations.

## Features

- Real-time CPU, GPU, and RAM monitoring
- Process monitoring with resource usage tracking
- Dark and light theme support
- Compact mode for minimal desktop footprint
- System information details
- Performance logging capabilities

## Installation Options

### Option 1: ClickOnce Installation (Recommended)

1. Visit the [SysVital Releases](https://github.com/AlexSWagner/SysVital/releases) page
2. Download and run the latest `setup.exe` file
3. Follow the installation prompts

### Option 2: Standalone Download

1. Visit the [SysVital Releases](https://github.com/AlexSWagner/SysVital/releases) page
2. Download the latest `SysVital.zip` file
3. Extract the contents to a location of your choice
4. Run `SysVital.exe`

## Administrator Privileges

SysVital works best with administrator privileges to access hardware monitoring features. When running without administrator rights, some functionality may be limited.

- The application will prompt you to run as administrator on startup
- You can choose to continue with limited functionality if preferred
- A notification will appear at the bottom of the application when running with limited privileges

## Usage

### Main Interface

- **System Info**: View detailed information about your computer's hardware
- **Start Logging**: Begin recording performance metrics to a log file
- **Dark/Light Mode**: Toggle between dark and light themes
- **Compact Mode**: Switch to a smaller, more compact interface

### Process Monitoring

The "Top Processes" section displays the most resource-intensive processes currently running on your system, sorted by CPU or memory usage.

## Building from Source

### Prerequisites

- Visual Studio 2019 or newer
- .NET Framework 4.7.2 or higher
- Required NuGet packages:
  - LibreHardwareMonitorLib
  - HidSharp
  - System.CodeDom

### AMD Hardware Users

If you're using AMD processors or graphics cards:
- Some features may not work as expected
- Temperature and usage readings might be inaccurate
- Consider using alternative monitoring tools specifically designed for AMD hardware

## Technical Implementation

SysVital demonstrates hardware-software integration by:
- Accessing hardware sensors through LibreHardwareMonitor
- Using Windows Performance Counters for system metrics
- Implementing privileged operations for direct hardware access
- Providing a user-friendly interface for complex system information

## Acknowledgments

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) for hardware monitoring capabilities
- [OpenHardwareMonitor](https://openhardwaremonitor.org/) for additional hardware monitoring support 
