using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SysVital
{
    /// <summary>
    /// Class to monitor Nvidia GPU performance using NVML API
    /// </summary>
    public class NvidiaGPUInfo : IDisposable
    {
        // Constants for NVML initialization and status
        private const int NVML_SUCCESS = 0;
        
        // Flag to track if NVML is initialized
        private bool nvmlInitialized = false;

        // Structure to store temperature thresholds
        public struct NVML_TEMPERATURE_THRESHOLDS
        {
            public uint Shutdown;
            public uint Slowdown;
        }

        // P/Invoke declarations for NVML
        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlInit();

        [DllImport("nvml.dll", EntryPoint = "nvmlShutdown", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlShutdown();

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCount_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetCount(ref uint deviceCount);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetHandleByIndex(uint index, ref IntPtr device);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetUtilizationRates", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetUtilizationRates(IntPtr device, ref NVML_UTILIZATION utilization);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetTemperature(IntPtr device, int sensorType, ref uint temperature);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperatureThresholds", CallingConvention = CallingConvention.Cdecl)]
        private static extern int NvmlDeviceGetTemperatureThresholds(IntPtr device, int thresholdType, ref uint temperature);

        // Structure to hold GPU utilization data
        [StructLayout(LayoutKind.Sequential)]
        private struct NVML_UTILIZATION
        {
            public uint GPU;
            public uint Memory;
        }

        // Temperature sensor types
        private enum NVML_TEMPERATURE_TYPE
        {
            NVML_TEMPERATURE_GPU = 0
        }

        // Temperature threshold types
        private enum NVML_TEMPERATURE_THRESHOLD_TYPE
        {
            NVML_TEMPERATURE_THRESHOLD_SHUTDOWN = 0,
            NVML_TEMPERATURE_THRESHOLD_SLOWDOWN = 1
        }

        /// <summary>
        /// Constructor - attempts to initialize NVML
        /// </summary>
        public NvidiaGPUInfo()
        {
            try
            {
                int result = NvmlInit();
                nvmlInitialized = (result == NVML_SUCCESS);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NVML initialization failed: {ex.Message}");
                nvmlInitialized = false;
            }
        }

        /// <summary>
        /// Clean up NVML resources
        /// </summary>
        public void Dispose()
        {
            if (nvmlInitialized)
            {
                try
                {
                    NvmlShutdown();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NVML shutdown failed: {ex.Message}");
                }
                nvmlInitialized = false;
            }
        }

        /// <summary>
        /// Get GPU utilization percentage
        /// </summary>
        /// <returns>GPU utilization percentage (0-100) or 0 if failed</returns>
        public float GetGPUUsage()
        {
            if (!nvmlInitialized)
                return 0;

            try
            {
                uint deviceCount = 0;
                int result = NvmlDeviceGetCount(ref deviceCount);
                
                if (result != NVML_SUCCESS || deviceCount == 0)
                    return 0;

                // Use first GPU for simplicity
                IntPtr deviceHandle = IntPtr.Zero;
                result = NvmlDeviceGetHandleByIndex(0, ref deviceHandle);
                
                if (result != NVML_SUCCESS)
                    return 0;

                NVML_UTILIZATION utilization = new NVML_UTILIZATION();
                result = NvmlDeviceGetUtilizationRates(deviceHandle, ref utilization);
                
                if (result != NVML_SUCCESS)
                    return 0;

                return utilization.GPU;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPU usage: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get GPU temperature
        /// </summary>
        /// <returns>GPU temperature in Celsius or 0 if failed</returns>
        public float GetGPUTemperature()
        {
            if (!nvmlInitialized)
                return 0;

            try
            {
                uint deviceCount = 0;
                int result = NvmlDeviceGetCount(ref deviceCount);
                
                if (result != NVML_SUCCESS || deviceCount == 0)
                    return 0;

                // Use first GPU for simplicity
                IntPtr deviceHandle = IntPtr.Zero;
                result = NvmlDeviceGetHandleByIndex(0, ref deviceHandle);
                
                if (result != NVML_SUCCESS)
                    return 0;

                uint temperature = 0;
                result = NvmlDeviceGetTemperature(deviceHandle, (int)NVML_TEMPERATURE_TYPE.NVML_TEMPERATURE_GPU, ref temperature);
                
                if (result != NVML_SUCCESS)
                    return 0;

                return temperature;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPU temperature: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get GPU temperature thresholds
        /// </summary>
        /// <returns>Temperature thresholds structure</returns>
        public NVML_TEMPERATURE_THRESHOLDS GetTemperatureThresholds()
        {
            NVML_TEMPERATURE_THRESHOLDS thresholds = new NVML_TEMPERATURE_THRESHOLDS();
            
            if (!nvmlInitialized)
                return thresholds;

            try
            {
                uint deviceCount = 0;
                int result = NvmlDeviceGetCount(ref deviceCount);
                
                if (result != NVML_SUCCESS || deviceCount == 0)
                    return thresholds;

                // Use first GPU for simplicity
                IntPtr deviceHandle = IntPtr.Zero;
                result = NvmlDeviceGetHandleByIndex(0, ref deviceHandle);
                
                if (result != NVML_SUCCESS)
                    return thresholds;

                // Get shutdown threshold
                uint shutdownTemp = 0;
                result = NvmlDeviceGetTemperatureThresholds(
                    deviceHandle, 
                    (int)NVML_TEMPERATURE_THRESHOLD_TYPE.NVML_TEMPERATURE_THRESHOLD_SHUTDOWN, 
                    ref shutdownTemp);
                
                if (result == NVML_SUCCESS)
                    thresholds.Shutdown = shutdownTemp;

                // Get slowdown threshold
                uint slowdownTemp = 0;
                result = NvmlDeviceGetTemperatureThresholds(
                    deviceHandle, 
                    (int)NVML_TEMPERATURE_THRESHOLD_TYPE.NVML_TEMPERATURE_THRESHOLD_SLOWDOWN, 
                    ref slowdownTemp);
                
                if (result == NVML_SUCCESS)
                    thresholds.Slowdown = slowdownTemp;

                return thresholds;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting GPU temperature thresholds: {ex.Message}");
                return thresholds;
            }
        }
    }
} 