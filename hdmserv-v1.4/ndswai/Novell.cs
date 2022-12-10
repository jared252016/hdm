using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace ndswai
{
	public class Novell
	{
		[DllImport("calwin32.dll")]
		public static extern int NWCallsInit(byte reserved1, byte reserved2);
		[DllImport("netwin32.dll", EntryPoint = "NWDSCreateContextHandle")]
		public static extern int NWDSCreateContextHandle(ref int context);
		[DllImport("netwin32.dll", EntryPoint = "NWDSWhoAmI")]
		public static extern int NWDSWhoAmI(int context, StringBuilder NovellUserId);
		[DllImport("netwin32.dll", EntryPoint = "NWDSFreeContext")]
		public static extern int NWDSFreeContext(int context);

		public static string getCurrentUser()
		{
			string user = "";
			try
			{
				int cCode = NWCallsInit(0, 0);
				if (cCode == 0)
				{
					int NovellContext = 0;
					cCode = NWDSCreateContextHandle(ref NovellContext);
					if (cCode == 0)
					{
						StringBuilder NovellUserId = new StringBuilder(256);
						cCode = NWDSWhoAmI(NovellContext, NovellUserId);
						if (cCode == 0)
						{
							user = NovellUserId.ToString();
						}
						cCode = NWDSFreeContext(NovellContext);
					}
				}
			}
			catch { }
			return user;
		}
	}
}
