using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Noesis.Javascript;
using MouseKeyboardLibrary;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Management;
using System.Management.Instrumentation;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Diagnostics;
namespace hdmclient
{
    class JSKeyboard
    {
        public void Type(string k)
        {
            KeyboardSimulator.PressString(k);
        }
        public void PressEnter()
        {
            KeyboardSimulator.KeyPress(Keys.Enter);
        }
    }
    class JSSystem
    {
        public bool FocusWindow(string title)
        {
            try
            {
                IntPtr x = WinLib.FindWindow(null, title);
                if (x == new IntPtr(0)) return false;
                WinLib.SetForegroundWindow(x);
                return true;
            }
            catch { return false; }
        }
        public void ShowMessage(string n)
        {
            MessageBox.Show(n);
        }
        public void Login(string user, string pass, string context)
        {
            WinLib.BlockInput(true);
            FocusWindow("Novell Client for Windows 4.91 SP2");
            IntPtr h = WinLib.FindWindow(null, "Novell Client for Windows 4.91 SP2");
            WinLib.RECT r;

            if (WinLib.GetWindowRect(h, out r))
            {
                MouseSimulator.X = r.Left + 360;
                MouseSimulator.Y = r.Top + 130;
                MouseSimulator.DoubleClick(MouseButton.Left);
                System.Threading.Thread.Sleep(200);
                KeyboardSimulator.KeyPress(Keys.Delete);
                System.Threading.Thread.Sleep(200);
                KeyboardSimulator.PressString(user);
                KeyboardSimulator.KeyPress(Keys.Tab);
                KeyboardSimulator.PressString(pass);

                KeyboardSimulator.KeyDown(Keys.Shift);
                System.Threading.Thread.Sleep(50);
                KeyboardSimulator.KeyPress(Keys.Tab);
                System.Threading.Thread.Sleep(50);
                KeyboardSimulator.KeyPress(Keys.Tab);
                System.Threading.Thread.Sleep(50);
                KeyboardSimulator.KeyUp(Keys.Shift);
                System.Threading.Thread.Sleep(50);
                KeyboardSimulator.KeyPress(Keys.Enter);
                for (int i = 0; i < 3; i++)
                {
                    KeyboardSimulator.KeyPress(Keys.Tab);
                    System.Threading.Thread.Sleep(50);
                }
                KeyboardSimulator.PressString(context);

                System.Threading.Thread.Sleep(500);
                KeyboardSimulator.KeyPress(Keys.Enter);
                System.Threading.Thread.Sleep(500);
                KeyboardSimulator.KeyPress(Keys.Enter);

            }
            // ---- 360, 130

            System.Threading.Thread.Sleep(2000);
            WinLib.BlockInput(false);
        }
        public void Logout()
        {
            ManagementBaseObject mboShutdown = null;
            ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();
            // You can't shutdown without security privileges
            mcWin32.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
            // Flag 1 means we want to shut down the system
            mboShutdownParams["Flags"] = "0";
            mboShutdownParams["Reserved"] = "0";
            foreach (ManagementObject manObj in mcWin32.GetInstances())
            {
                mboShutdown = manObj.InvokeMethod("Win32Shutdown", mboShutdownParams, null);
            }
        }
        public void Reboot()
        {
            ManagementBaseObject mboShutdown = null;
            ManagementClass mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();
            // You can't shutdown without security privileges
            mcWin32.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
            // Flag 1 means we want to shut down the system
            mboShutdownParams["Flags"] = "2";
            mboShutdownParams["Reserved"] = "0";
            foreach (ManagementObject manObj in mcWin32.GetInstances())
            {
                mboShutdown = manObj.InvokeMethod("Win32Shutdown", mboShutdownParams, null);
            }
        }
        public void Sleep(int x)
        {
            System.Threading.Thread.Sleep(x);
        }
    }
    class JSFile
    {
        public void Write(string path, string data)
        {
            File.WriteAllText(path, data);
        }
        public string Read(string path)
        {
            return File.ReadAllText(path);
        }
        public bool Copy(string path1, string path2)
        {
            try
            {
                File.Copy(path1, path2);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool Move(string path1, string path2)
        {
            try
            {
                File.Move(path1, path2);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool Delete(string path)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void MarkReadOnly(string path)
        {
            File.SetAttributes(path, FileAttributes.ReadOnly);
        }
        public void MarkHidden(string path)
        {
            File.SetAttributes(path, FileAttributes.Hidden);
        }
        public void MarkSystem(string path)
        {
            File.SetAttributes(path, FileAttributes.System);
        }
        public void MarkNormal(string path)
        {
            File.SetAttributes(path, FileAttributes.Normal);
        }
    }
    class JSAMMT
    {
        public string ScriptVersion()
        {
            return "1.2";
        }
        public void SetState(int state) {
            // create a writer and open the file
            File.WriteAllText(Path.GetDirectoryName(Application.ExecutablePath) + "/ammt_state", state.ToString());
        }
        public int GetState()
        {
            try
            {
                return int.Parse(File.ReadAllText(Path.GetDirectoryName(Application.ExecutablePath) + "/ammt_state"));
            }
            catch
            {
                return -1;
            } 
        }
        public void ClearState()
        {
            File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + "/ammt_state");
        }
        public void LoadOnBoot(string file)
        {
            File.WriteAllText(Path.GetDirectoryName(Application.ExecutablePath) + "/ammt", file);
        }
    }
    class JSMouse
    {
        public void SetPosition(int x, int y)
        {
            MouseSimulator.X = x;
            MouseSimulator.Y = y;
        }
        public void LeftClick()
        {
            MouseSimulator.Click(MouseButton.Left);
        }
        public void RightClick()
        {
            MouseSimulator.Click(MouseButton.Right);
        }
        public void MiddleClick()
        {
            MouseSimulator.Click(MouseButton.Middle);
        }
        public void LeftDoubleClick()
        {
            MouseSimulator.DoubleClick(MouseButton.Left);
        }
        public void RightDoubleClick()
        {
            MouseSimulator.DoubleClick(MouseButton.Right);
        }
        public void MiddleDoubleClick()
        {
            MouseSimulator.DoubleClick(MouseButton.Middle);
        }
        public void LeftDown()
        {
            MouseSimulator.MouseDown(MouseButton.Left);
        }
        public void LeftUp()
        {
            MouseSimulator.MouseUp(MouseButton.Left);
        }
        public void RightDown()
        {
            MouseSimulator.MouseDown(MouseButton.Right);
        }
        public void RightUp()
        {
            MouseSimulator.MouseUp(MouseButton.Right);
        }
        public int GetX()
        {
            return MouseSimulator.Position.X;
        }
        public int GetY()
        {
            return MouseSimulator.Position.Y;
        }
        public void MoveTo(int x, int y, int speed)
        {
            int cX = GetX(), cY = GetY();
            while (x != cX && y != cY)
            {
                if (Math.Abs(cX - x) < speed || Math.Abs(cY - y) < speed) speed = 1;
                if (cX < x)
                {
                    x += speed;
                }
                else
                {
                    x -= speed;
                }
            }
        }
    }
    class JSProcess
    {
        public void Start(string n, string a = "")
        {
            System.Diagnostics.Process.Start(n, a);
        }
        public void StartWait(string n, string a = "")
        {
            Process p = System.Diagnostics.Process.Start(n, a);
            p.WaitForExit();            
        }

    }
    class JSDFC
    {
        public bool isFrozen()
        {
            return DFController.isFrozen();
        }
        public void Lock()
        {
            DFController.Lock();
        }
        public void Unlock()
        {
            DFController.Unlock();
        }
        public int RebootThawed()
        {
            return DFController.RebootThawed();
        }
        public int RebootFrozen()
        {
            return DFController.RebootFrozen();
        }
        public int RebootThawedLocked()
        {
            return DFController.RebootThawedLocked();
        }
        public int RebootThawedNoInput()
        {
            return DFController.RebootThawedNoInput();
        }
        public int BootThawedNext()
        {
            return DFController.BootThawedNext();
        }
        public int BootFrozenNext()
        {
            return DFController.BootFrozenNext();
        }
    }
    class ammt
    {
        private string _file = "";
        private string _src = "";
        public JavascriptContext context;
        public ammt(string file)
        {
            _file = file;
            try
            {
                WebClient client = new WebClient();
                _src = client.DownloadString("http://console.hudsonisd.org/hdms/ammt/" + file);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            context = new JavascriptContext();
        }
        public void Run()
        {
            try
            {
                context.SetParameter("Keyboard", new JSKeyboard());
                context.SetParameter("Mouse", new JSMouse());
                context.SetParameter("Process", new JSProcess());
                context.SetParameter("System", new JSSystem());
                context.SetParameter("AMMT", new JSAMMT());
                context.SetParameter("DFC", new JSDFC());
                context.SetParameter("File", new JSFile());
                // Prefix the src with some of our own functions

                _src = _src + "\nfunction alert(text) { System.ShowMessage(text); }\n";

                context.Run(_src);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
