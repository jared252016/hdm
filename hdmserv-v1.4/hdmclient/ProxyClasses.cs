using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace hdmclient
{
    internal class RegUtils
    {
        internal enum RegKeyType : int
        {
            CurrentUser = 1,
            LocalMachine = 2
        }

        internal RegUtils() { }

        
        // byte[]
        internal void GetKeyValue(RegKeyType KeyType, string RegKey,
            string Name, out object Value)
        {
            RegistryKey oRegKey = null;

            switch ((int)KeyType)
            {
                case 1:
                    oRegKey = Registry.CurrentUser;
                    break;
                case 2:
                    oRegKey = Registry.LocalMachine;
                    break;
            }
            oRegKey = oRegKey.OpenSubKey(RegKey);

            Value = oRegKey.GetValue(Name);

            oRegKey.Close();
        }

        // byte[]
        internal void SetKeyValue(RegKeyType KeyType, string RegKey,
            string Name, object Value)
        {
            RegistryKey oRegKey = null;

            switch ((int)KeyType)
            {
                case 1:
                    oRegKey = Registry.CurrentUser;
                    break;
                case 2:
                    oRegKey = Registry.LocalMachine;
                    break;
            }

            oRegKey = oRegKey.OpenSubKey(RegKey, true);
            oRegKey.SetValue(Name, Value);
            oRegKey.Close();
            User32Utils.Notify_SettingChange();
            WinINetUtils.Notify_OptionSettingChanges();
        }
    }
    internal class WinINetUtils
    {
        #region WININET Options
        private const uint INTERNET_PER_CONN_PROXY_SERVER = 2;
        private const uint INTERNET_PER_CONN_PROXY_BYPASS = 3;
        private const uint INTERNET_PER_CONN_FLAGS = 1;

        private const uint INTERNET_OPTION_REFRESH = 37;
        private const uint INTERNET_OPTION_PROXY = 38;
        private const uint INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const uint INTERNET_OPTION_END_BROWSER_SESSION = 42;
        private const uint INTERNET_OPTION_PER_CONNECTION_OPTION = 75;

        private const uint PROXY_TYPE_DIRECT = 0x1;
        private const uint PROXY_TYPE_PROXY = 0x2;

        private const uint INTERNET_OPEN_TYPE_PROXY = 3;
        #endregion

        #region STRUCT


        [StructLayout(LayoutKind.Sequential)]
        struct INTERNET_PER_CONN_OPTION_LIST
        {
            uint dwSize;
            [MarshalAs(UnmanagedType.LPStr, SizeConst = 256)]
            string pszConnection;
            uint dwOptionCount;
            uint dwOptionError;
            IntPtr pOptions;

        };

        [StructLayout(LayoutKind.Sequential)]
        struct INTERNET_CONNECTED_INFO
        {
            int dwConnectedState;
            int dwFlags;
        };
        #endregion

        #region Interop
        [DllImport("wininet.dll", EntryPoint = "InternetSetOptionA",
                  CharSet = CharSet.Ansi, SetLastError = true, PreserveSig = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, uint dwOption,
                                                     IntPtr pBuffer, int dwReserved);
        #endregion

        internal WinINetUtils() { }


        internal static void Notify_OptionSettingChanges()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED,
                    IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
    }
    internal class User32Utils
    {
        #region USER32 Options
        static IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        static IntPtr WM_SETTINGCHANGE = new IntPtr(0x001A);
        #endregion

        #region STRUCT
        enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0000,
            SMTO_BLOCK = 0x0001,
            SMTO_ABORTIFHUNG = 0x0002,
            SMTO_NOTIMEOUTIFNOTHUNG = 0x0008
        }
        #endregion

        #region Interop
        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern int SendMessage
        //(int hWnd, int msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr SendMessageTimeout(IntPtr hWnd,
                                                uint Msg,
                                                UIntPtr wParam,
                                                UIntPtr lParam,
                                                SendMessageTimeoutFlags fuFlags,
                                                uint uTimeout,
                                                out UIntPtr lpdwResult);
        #endregion


        internal User32Utils() { }

        internal static void Notify_SettingChange()
        {
            UIntPtr result;
            SendMessageTimeout(HWND_BROADCAST, (uint)WM_SETTINGCHANGE,
                               UIntPtr.Zero, UIntPtr.Zero,
                                SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);
        }
    }
    public class CEngine
    {
        RegUtils _regUtils = new RegUtils();

        public CEngine() { }

        public void SetProxyName(string ProxyAddress)
        {
            string szRegKey =
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\";
            string szName = "ProxyServer";

            _regUtils.SetKeyValue(RegUtils.RegKeyType.CurrentUser, szRegKey, szName, ProxyAddress);
        }

        public void EnableProxy(string ProxyAddress)
        {
            string szRegKey =
                 @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\";
            string szName = "ProxyEnable";
            Object szValue = string.Empty;

            SetProxyName(ProxyAddress);

            RegistryKey oRegKey = null;
            oRegKey = Registry.CurrentUser.OpenSubKey(szRegKey, true);
            oRegKey.SetValue(szName, 1);
            oRegKey.Close();
            User32Utils.Notify_SettingChange();
            WinINetUtils.Notify_OptionSettingChanges();

        }

        public void DisableProxy(string ProxyAddress)
        {
            string szRegKey =
                @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\";
            string szName = "ProxyEnable";
            _regUtils.SetKeyValue(RegUtils.RegKeyType.CurrentUser,
                                   szRegKey, szName, 0);

            SetProxyName("");
        }

        public string GetProxyName()
        {
            string szRegKey =
                 @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\";
            string szName = "ProxyServer";

            RegistryKey oRegKey = null;

            oRegKey = Registry.CurrentUser.OpenSubKey(szRegKey);

            string szProxyAddress = (string)oRegKey.GetValue(szName);

            oRegKey.Close();

            return szProxyAddress;
        }

        public int GetProxyStatus()
        {
            string szRegKey =
                 @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\";
            string szName = "ProxyEnable";
            object iProxyStatus = 0;

            RegistryKey oRegKey = null;

            oRegKey = Registry.CurrentUser.OpenSubKey(szRegKey);

            int status = (int)oRegKey.GetValue(szName);

            oRegKey.Close();

            return status;
        }
    }
    }
