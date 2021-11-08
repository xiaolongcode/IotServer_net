using IotServer.Core;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IotServer
{
    public class TimerUDPService : IJob
    {
        /// <summary>
        /// 是否执行定时任务
        /// </summary>
        private static bool IsRun = true;
        public async Task Execute(IJobExecutionContext context)
        {
            if (IsRun)
            {
                await Task.Run(() =>
                {
                    IsRun = false;
                    QueueUdpModel UDP = new QueueUdpModel();
                    try
                    {
                        #region 业务代码
                        int msgcount = StaticData.GetUdpQueueCount();
                        if (msgcount > 0)
                            StaticData.ConsoleWrite("UDP消息处理 消息数量：" + msgcount, 2);
                        if (msgcount > 0)
                            StaticData.ConsoleWrite("UDP消息处理完毕，等待下次执行", 2);
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        TxtLogHelper.ErroLog("UDP消息处理 Exception:" + ex.Message);
                    }
                    IsRun = true;
                });
            }
            else
            {
                StaticData.ConsoleWrite("正在执行UDP消息处理任务，下次调用", 2);
            }
        }
    }
}
