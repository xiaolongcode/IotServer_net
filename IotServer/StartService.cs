using IotServer.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace IotServer
{
    partial class StartService : ServiceBase
    {
        public StartService()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            try
            {
#pragma warning disable CS4014 // 由于此调用不会等待，因此在此调用完成之前将会继续执行当前方法。请考虑将 "await" 运算符应用于调用结果。
                TCPService.TCPStart(AppSetting.GetValueByInt("tcpport"), AppSetting.GetValueByInt("tcptimeout"));
#pragma warning restore CS4014 // 由于此调用不会等待，因此在此调用完成之前将会继续执行当前方法。请考虑将 "await" 运算符应用于调用结果。
                System.Threading.Thread.Sleep(100);
#pragma warning disable CS4014 // 由于此调用不会等待，因此在此调用完成之前将会继续执行当前方法。请考虑将 "await" 运算符应用于调用结果。
                UDPService.UDPStart(AppSetting.GetValueByInt("udpport"));
#pragma warning restore CS4014 // 由于此调用不会等待，因此在此调用完成之前将会继续执行当前方法。请考虑将 "await" 运算符应用于调用结果。
                System.Threading.Thread.Sleep(100);
                new StaticData(UDPService.UdpServer, TCPService.TcpServer);

            }
            catch (Exception ex)
            {
               StaticData.ConsoleWrite("执行异常" + ex.Message);
                TxtLogHelper.ErroLog($"服务启动异常:{ex.Message}");
            }
        }
        protected override void OnStop()
        {
            Console.ReadLine();
        }
        public void DebugStart(string[] args)
        {
            this.OnStart(args);
        }
        public void DebugStop()
        {
            this.OnStop();
        }
    }
}
