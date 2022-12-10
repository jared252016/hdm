using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace hdmserv
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
			List<string> argz = new List<string>();
			argz.AddRange(args);
            if (argz.Contains("-console"))
            {
                var app = new coreXP();
                app.Start(true); // Arg is to let it know we're in debug mode
                while (Console.ReadKey().KeyChar != 'q')
                {
                }
                app.Stop();
            }
            else
            {
                 ServiceBase[] ServicesToRun;
                 ServicesToRun = new ServiceBase[]
                {
                    new hdm_service()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
