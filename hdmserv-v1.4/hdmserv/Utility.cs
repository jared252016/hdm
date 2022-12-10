using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace hdmserv
{
    class Utility
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessID;
            public Int32 dwThreadID;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }
        [Flags]
        enum CreationFlags
        {
            CREATE_SUSPENDED = 0x00000004,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct StartupInfo
        {
            public int cb;
            public String reserved;
            public String desktop;
            public String title;
            public int x;
            public int y;
            public int xSize;
            public int ySize;
            public int xCountChars;
            public int yCountChars;
            public int fillAttribute;
            public int flags;
            public UInt16 showWindow;
            public UInt16 reserved2;
            public byte reserved3;
            public IntPtr stdInput;
            public IntPtr stdOutput;
            public IntPtr stdError;
        }

        internal struct ProcessInformation
        {
            public IntPtr process;
            public IntPtr thread;
            public int processId;
            public int threadId;
        }
        [Flags]
        enum LogonFlags
        {
            LOGON_WITH_PROFILE = 0x00000001,
            LOGON_NETCREDENTIALS_ONLY = 0x00000002
        }
        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        public const int GENERIC_ALL_ACCESS = 0x10000000;

        [
            DllImport("kernel32.dll",
                EntryPoint = "CloseHandle", SetLastError = true,
                CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)
        ]
        public static extern bool CloseHandle(IntPtr handle);

        [
            DllImport("advapi32.dll",
                EntryPoint = "CreateProcessAsUser", SetLastError = true,
                CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)
        ]
        public static extern bool
            CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine,
                                ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes,
                                bool bInheritHandle, Int32 dwCreationFlags, IntPtr lpEnvrionment,
                                string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo,
                                ref PROCESS_INFORMATION lpProcessInformation);

        [
            DllImport("advapi32.dll",
                EntryPoint = "DuplicateTokenEx")
        ]
        public static extern bool
            DuplicateTokenEx(IntPtr hExistingToken, Int32 dwDesiredAccess,
                            ref SECURITY_ATTRIBUTES lpThreadAttributes,
                            Int32 ImpersonationLevel, Int32 dwTokenType,
                            ref IntPtr phNewToken);
    }
}
