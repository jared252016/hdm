using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace hdmclient
{
    public static class WinLib
    {


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GetConsoleWindow();

        public static IntPtr MyConsole = GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        public static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        public static extern bool BlockInput(bool fBlockIt);

        public const UInt32 SC_CLOSE = 0xF060;
        public const UInt32 SC_MOVE = 0xF010;
        public const UInt32 SC_SIZE = 0xF000;
        public const UInt32 MF_ENABLED = 0x00000000;
        public const UInt32 MF_GRAYED = 0x00000001;
        public const UInt32 MF_DISABLED = 0x00000002;
        public const int GWL_STYLE = (-16);
        public const uint MF_BYCOMMAND = 0x00000000;
        public const int WS_CAPTION = 0xc00000;
        public const int DS_MODALFRAME = 0x80;
        public const int WS_MINIMIZEBOX = 0x20000;
        public const int WS_SYSMENU = 0x80000;
    }
}
