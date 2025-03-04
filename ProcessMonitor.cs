using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SysVital
{
    /// <summary>
    /// Handles monitoring of system processes including CPU and memory usage
    /// </summary>
    public class ProcessMonitor
    {
        // Store previous processor time measurements for CPU usage calculation
        private Dictionary<int, (DateTime time, TimeSpan totalProcessorTime)> processPreviousValues = 
            new Dictionary<int, (DateTime, TimeSpan)>();

        // Process info class to model process data
        public class ProcessInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double CpuUsage { get; set; }
            public long MemoryUsageMB { get; set; }
            public string FormattedCpuUsage => $"{CpuUsage:F1}%";
            public string FormattedMemoryUsage => $"{MemoryUsageMB} MB";
        }

        // Constructor
        public ProcessMonitor()
        {
            // Check for elevated privileges
            try
            {
                // Try to get a process that will fail without admin rights
                Process.GetProcessById(4); // System process
                Debug.WriteLine("ProcessMonitor: Elevated privileges detected");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ProcessMonitor: Note - may have limited access to process information: {ex.Message}");
            }
            
            // Initial population of process dictionary
            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        processPreviousValues[process.Id] = (DateTime.Now, process.TotalProcessorTime);
                    }
                    catch
                    {
                        // Skip processes we can't access
                    }
                }
                Debug.WriteLine($"ProcessMonitor: Initialized with {processPreviousValues.Count} processes");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ProcessMonitor: Error initializing process list: {ex.Message}");
            }
        }

        /// <summary>
        /// Get list of top processes sorted by CPU usage
        /// </summary>
        /// <param name="count">Number of processes to return</param>
        /// <returns>List of top processes</returns>
        public List<ProcessInfo> GetTopProcessesByCpu(int count = 3)
        {
            var result = new List<ProcessInfo>();
            
            try
            {
                var processes = Process.GetProcesses();
                var currentTime = DateTime.Now;
                var currentIds = new HashSet<int>();
                
                foreach (var p in processes)
                {
                    try
                    {
                        currentIds.Add(p.Id);
                        
                        TimeSpan currentTotalProcessorTime;
                        try
                        {
                            currentTotalProcessorTime = p.TotalProcessorTime;
                        }
                        catch
                        {
                            continue; // Skip if we can't get processor time
                        }
                        
                        // Calculate CPU usage if we have previous measurements
                        if (processPreviousValues.TryGetValue(p.Id, out var previous))
                        {
                            TimeSpan timeDiff = currentTime - previous.time;
                            TimeSpan cpuUsageDiff = currentTotalProcessorTime - previous.totalProcessorTime;
                            
                            double cpuUsagePercent = (cpuUsageDiff.TotalMilliseconds / 
                                                    (timeDiff.TotalMilliseconds * Environment.ProcessorCount)) * 100;
                            
                            if (cpuUsagePercent < 0) cpuUsagePercent = 0;
                            
                            result.Add(new ProcessInfo
                            {
                                Id = p.Id,
                                Name = p.ProcessName,
                                CpuUsage = cpuUsagePercent,
                                MemoryUsageMB = p.WorkingSet64 / (1024 * 1024)
                            });
                        }
                        else
                        {
                            // On first run, add the process with 0% CPU usage
                            // We'll sort by memory usage instead for the first cycle
                            result.Add(new ProcessInfo
                            {
                                Id = p.Id,
                                Name = p.ProcessName,
                                CpuUsage = 0,
                                MemoryUsageMB = p.WorkingSet64 / (1024 * 1024)
                            });
                        }
                        
                        // Store current values for next calculation
                        processPreviousValues[p.Id] = (currentTime, currentTotalProcessorTime);
                    }
                    catch
                    {
                        // Skip processes that throw exceptions (usually system processes)
                    }
                }
                
                // Clean up old processes
                CleanupOldProcesses(currentIds);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Process monitoring error: {ex.Message}");
                // Handle any exceptions during process monitoring
                result.Add(new ProcessInfo 
                { 
                    Name = "Error retrieving processes", 
                    CpuUsage = 0, 
                    MemoryUsageMB = 0 
                });
            }
            
            // If we have CPU usage data, sort by that; otherwise sort by memory
            if (result.Any(p => p.CpuUsage > 0))
            {
                return result.OrderByDescending(p => p.CpuUsage).Take(count).ToList();
            }
            else
            {
                // On first run, sort by memory usage instead
                return result.OrderByDescending(p => p.MemoryUsageMB).Take(count).ToList();
            }
        }
        
        /// <summary>
        /// Get the total number of running processes
        /// </summary>
        /// <returns>Process count</returns>
        public int GetProcessCount()
        {
            try
            {
                return Process.GetProcesses().Length;
            }
            catch
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Clean up processes that no longer exist
        /// </summary>
        private void CleanupOldProcesses(HashSet<int> currentIds)
        {
            // Find keys to remove (processes that no longer exist)
            var keysToRemove = processPreviousValues.Keys
                .Where(id => !currentIds.Contains(id))
                .ToList();
            
            // Remove old processes
            foreach (var key in keysToRemove)
            {
                processPreviousValues.Remove(key);
            }
        }
        
        /// <summary>
        /// Helper methods to expose the dictionary for Form1
        /// </summary>
        public bool TryGetValue(int id, out (DateTime time, TimeSpan totalProcessorTime) value)
        {
            return processPreviousValues.TryGetValue(id, out value);
        }
        
        public (DateTime time, TimeSpan totalProcessorTime) this[int id]
        {
            get { return processPreviousValues[id]; }
            set { processPreviousValues[id] = value; }
        }
        
        public IEnumerable<int> Keys => processPreviousValues.Keys;
        
        public bool Remove(int key)
        {
            return processPreviousValues.Remove(key);
        }
        
        /// <summary>
        /// Create a ListViewItem for a process
        /// </summary>
        /// <param name="process">Process information</param>
        /// <param name="isDarkMode">Whether dark mode is enabled</param>
        /// <returns>Formatted ListViewItem</returns>
        public ListViewItem CreateProcessListViewItem(ProcessInfo process, bool isDarkMode)
        {
            var item = new ListViewItem(process.Name);
            item.SubItems.Add(process.FormattedCpuUsage);
            item.SubItems.Add(process.FormattedMemoryUsage);
            
            if (isDarkMode)
            {
                item.ForeColor = Color.White;
                item.BackColor = Color.FromArgb(40, 40, 40);
            }
            
            return item;
        }
        
        /// <summary>
        /// Create an error ListViewItem
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="color">Text color</param>
        /// <returns>Formatted error ListViewItem</returns>
        public ListViewItem CreateErrorListViewItem(string message, Color color)
        {
            var item = new ListViewItem(message);
            item.ForeColor = color;
            return item;
        }
    }
} 