using System;
using System.Runtime.InteropServices;
using System.Management;
using System.Security.Principal;
using System.Diagnostics;

namespace SysVital
{
    public class CpuTemperature
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpFileName);

        private const uint CPUID_0 = 0;
        private const uint CPUID_EXT = 0x80000000;

        public static float GetCpuTemperature()
        {
            try
            {
                // Check if running with admin privileges
                if (!IsAdministrator())
                {
                    Debug.WriteLine("Application is not running with administrator privileges");
                    return 0;
                }

                var options = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate,
                    Authentication = AuthenticationLevel.PacketPrivacy,
                    EnablePrivileges = true
                };

                var scope = new ManagementScope(@"root\WMI", options);
                scope.Connect();

                if (!scope.IsConnected)
                {
                    Debug.WriteLine("Failed to connect to WMI");
                    return 0;
                }

                using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM MSAcpi_ThermalZoneTemperature")))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var temp = Convert.ToDouble(obj["CurrentTemperature"].ToString());
                        // Convert tenth degrees Kelvin to Celsius
                        temp = (temp - 2732) / 10.0;
                        Debug.WriteLine($"Found CPU temperature: {temp}°C");
                        return (float)temp;
                    }
                }

                // Try alternative method
                scope = new ManagementScope(@"root\CIMV2", options);
                scope.Connect();

                using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery("SELECT * FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation")))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var temp = Convert.ToDouble(obj["Temperature"].ToString());
                        Debug.WriteLine($"Found CPU temperature (alt method): {temp}°C");
                        return (float)temp;
                    }
                }

                Debug.WriteLine("No temperature data found through WMI");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting CPU temperature: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return 0;
            }
        }

        private static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking admin rights: {ex.Message}");
                return false;
            }
        }
    }
} 