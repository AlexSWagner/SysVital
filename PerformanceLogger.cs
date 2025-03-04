using System;
using System.IO;

namespace SysVital
{
    public class PerformanceLogger
    {
        private string logPath;
        private bool isLogging;
        private StreamWriter writer;
        private readonly object lockObject = new object();
        private DateTime lastLogTime;
        private const int LOG_INTERVAL_SECONDS = 5; // Only log every 5 seconds

        public bool IsLogging => isLogging;

        public PerformanceLogger(string path)
        {
            logPath = path;
            lastLogTime = DateTime.Now;
        }

        public void StartLogging()
        {
            if (!isLogging)
            {
                writer = new StreamWriter(logPath, true);
                writer.WriteLine("Timestamp,CPU Usage (%),CPU Temp (°C),GPU Usage (%),GPU Temp (°C),RAM Available (MB)");
                isLogging = true;
            }
        }

        public void LogData(float cpuUsage, float cpuTemp, float gpuUsage, float gpuTemp, float ramAvailable)
        {
            if (isLogging && (DateTime.Now - lastLogTime).TotalSeconds >= LOG_INTERVAL_SECONDS)
            {
                lock (lockObject)
                {
                    writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{cpuUsage:F1},{cpuTemp:F1},{gpuUsage:F1},{gpuTemp:F1},{ramAvailable:F0}");
                    writer.Flush();
                    lastLogTime = DateTime.Now;
                }
            }
        }

        public void StopLogging()
        {
            if (isLogging)
            {
                writer.Close();
                isLogging = false;
            }
        }
    }
}