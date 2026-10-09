using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Trucardo.Core.BotDialogue
{
    // Windows closes this handle on an Editor/player crash too: no orphaned model process.
    internal sealed class OwnedProcessJob : IDisposable
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [StructLayout(LayoutKind.Sequential)] private struct BasicLimits
        {
            public long processTime, jobTime;
            public uint flags;
            public UIntPtr minimumWorkingSet, maximumWorkingSet;
            public uint activeProcesses;
            public UIntPtr affinity;
            public uint priorityClass, schedulingClass;
        }
        [StructLayout(LayoutKind.Sequential)] private struct IoCounters { public ulong a, b, c, d, e, f; }
        [StructLayout(LayoutKind.Sequential)] private struct ExtendedLimits
        {
            public BasicLimits basic;
            public IoCounters io;
            public UIntPtr processMemory, jobMemory, peakProcessMemory, peakJobMemory;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr job, int infoClass, ref ExtendedLimits info, uint size);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
        private IntPtr handle;

        public OwnedProcessJob(IntPtr process)
        {
            handle = CreateJobObject(IntPtr.Zero, null);
            var limits = new ExtendedLimits { basic = new BasicLimits { flags = 0x2000 } }; // KILL_ON_JOB_CLOSE
            if (handle == IntPtr.Zero || !SetInformationJobObject(handle, 9, ref limits, (uint)Marshal.SizeOf<ExtendedLimits>())
                || !AssignProcessToJobObject(handle, process))
            {
                int error = Marshal.GetLastWin32Error();
                Dispose();
                throw new Win32Exception(error, "No se pudo asegurar el cierre de llama-server.");
            }
        }
        public void Dispose()
        {
            if (handle == IntPtr.Zero) return;
            CloseHandle(handle);
            handle = IntPtr.Zero;
        }
#else
        public OwnedProcessJob(IntPtr process) { }
        public void Dispose() { }
#endif
    }
}
