using System;
using System.Collections.Generic;
using System.Text;

namespace jjw
{
    class Program
    {
        static string _pname = "socks=10.1.9.127:1080";
        static void Main(string[] args)
        {
            CEngine c = new CEngine();
            if (c.GetProxyName() == _pname && c.GetProxyStatus() == 1)
            {
                c.DisableProxy(_pname);
            }
            else if (c.GetProxyStatus() == 0)
            {
                c.EnableProxy(_pname);
            }
        }
    }
}
