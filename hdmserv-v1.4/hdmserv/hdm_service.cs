using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;

namespace hdmserv
{
    public partial class hdm_service : ServiceBase
    {
        private coreXP serv;

        public hdm_service()
        {
            InitializeComponent();

			this.CanHandlePowerEvent = true;
			this.CanHandleSessionChangeEvent = true;
			this.CanPauseAndContinue = false;
			this.CanShutdown = true;
			this.CanStop = true;
        }

        protected override void OnStart(string[] args)
        {
            this.serv = new coreXP();
            this.serv.Start();
        }

		protected override void OnShutdown()
		{
			this.serv.onShutdown();
			this.serv.Stop();
			base.OnShutdown();
		}

        protected override void OnStop()
        {
            this.serv.Stop();
			base.OnStop();
        }
    }
}
