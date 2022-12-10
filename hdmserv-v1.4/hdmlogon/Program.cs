using System;
using MouseKeyboardLibrary;
using System.Windows.Forms;
namespace hdmlogon
{
	partial class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 2 && args.Length != 3) Environment.Exit(0);
			DisableButtons();
			string username = args[0];
			string password = args[1];
			Console.Title = "";
			Console.WindowWidth = 30;
			Console.BufferWidth = 30;
			Console.WindowHeight = 5;
			Console.BufferHeight = 5;
			SetWindowPos(MyConsole, 0, 10, 10, 0, 0, 0x0001 | 0x0010);
			BlockInput(true);
			Console.WriteLine("Authenticating " + username + "...");

			// Process login here
			IntPtr h = FindWindow(null, "Novell Client for Windows 4.91 SP2");
			RECT r;
			if (GetWindowRect(h, out r))
			{
				MouseSimulator.X = r.Left + 360;
				MouseSimulator.Y = r.Top + 130;
				MouseSimulator.DoubleClick(MouseButton.Left);
				System.Threading.Thread.Sleep(200);
				KeyboardSimulator.KeyPress(Keys.Delete);
				System.Threading.Thread.Sleep(200);
				KeyboardSimulator.PressString(username);
				KeyboardSimulator.KeyPress(Keys.Tab);
				KeyboardSimulator.PressString(password);

				if (args.Length == 3)
				{
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
					KeyboardSimulator.PressString(args[2]);
				}
				System.Threading.Thread.Sleep(500);
				KeyboardSimulator.KeyPress(Keys.Enter);
				System.Threading.Thread.Sleep(500);
				KeyboardSimulator.KeyPress(Keys.Enter);

			}
			// ---- 360, 130

			System.Threading.Thread.Sleep(2000);
			BlockInput(false);

		}
		public static void DisableButtons()
		{
			IntPtr hSystemMenu = GetSystemMenu(MyConsole, false);
			EnableMenuItem(hSystemMenu, SC_CLOSE, MF_DISABLED);
		}
	}
}
