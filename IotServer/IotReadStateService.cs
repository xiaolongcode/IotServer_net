using IotServer.Core;
using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IotServer
{
    /// <summary>
    /// 定时读取设备状态
    /// </summary>
    public class IotReadStateService : IJob
    {
        /// <summary>
        /// 是否执行定时任务
        /// </summary>
        private static bool IsRun = true;
        public async Task Execute(IJobExecutionContext context)
        {
            if (IsRun && !StaticData.controlState)
            {
                await Task.Run(() =>
                {
                    StaticData.ConsoleWrite("开始执行定时读取设备状态任务", 5);
                    IsRun = false;
                    try
                    {
                        #region 业务代码
                        TxtLogHelper.InfoLog("执行定时任务处理");
                        DingShiState();
                        TxtLogHelper.InfoLog("结束定时任务处理");
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        TxtLogHelper.ErroLog("定时读取设备状态任务处理 Exception:" + ex.Message);
                    }
                    IsRun = true;
                    StaticData.ConsoleWrite("结束执行定时读取设备状态任务", 5);
                });
            }
            else
            {
                if (StaticData.controlState)
                    StaticData.ConsoleWrite("正在发送设备手动控制指令，下次调用", 5);
                else
                    StaticData.ConsoleWrite("正在执行定时读取设备状态任务，下次调用", 5);
            }
        }
        /// <summary>
        /// 定时读取设备状态
        /// </summary>
        public void DingShiState()
        {
            List<ControllersModel> list = new List<ControllersModel>(StaticData.GetAllControllers().ToArray());
            Dictionary<string, DicIotModel> diciot =new Dictionary<string, DicIotModel>( StaticData.GetDicIot());
            if (list != null && list.Count > 0 && diciot != null && diciot.Count > 0)
            {
                foreach (ControllersModel model in list)
                {
                    DateTime newdate = DateTime.Now;
                    string address = model.address;
                    string count = model.waycount;
                    byte[] data = new byte[6];
                    //地址
                    data[0] = (byte)Convert.ToInt32(address);
                    //功能码
                    data[1] = 0x03;
                    //起始寄存器  4路和8路
                    data[2] = 0x00;
                    data[3] = 0x10;
                    //寄存器数量
                    data[4] = 0x00;
                    data[5] = (byte)int.Parse(count);

                    try
                    {
                        if (int.Parse(count) == 1 || int.Parse(count) == 6)
                        {
                            //起始寄存器  单路和六路：00 00
                            data[2] = 0x00;
                            data[3] = 0x00;

                            //寄存器数量
                            data[4] = 0x00;
                            data[5] = 0x0d;
                        }
                    }
                    catch { }

                    //CRC校验
                    byte[] crc = StaticData.CRC16_C(data);
                    byte[] send = StaticData.CopyByte(data, crc);//最终发送数据
                    string ReStr = StaticData.ByteToString2(send);
                    if (model.ConnectionId != null && diciot.ContainsKey(model.ConnectionId))
                    {
                        #region 给指定ip发送指令
                        double minutes = (newdate - model.updatetime).TotalMinutes;
                        if (minutes <= StaticData.ReadInterval)
                        {
                            StaticData.ConsoleWrite($"{count}路设备{address},{minutes}分钟内发送过控制指令,本次读状态指令不发送！", 1);
                            continue;
                        }
                        Thread.Sleep(600);
                        StaticData.ConsoleWrite($"{count}路设备{address}发送读状态指令 data:{ReStr}", 1);
                        StaticData.TCPSend(model.ConnectionId, send, true);
                        #endregion
                    }
                    else
                    {
                        #region 给所有ip发送指令
                        foreach (var v in diciot)
                        {
                            double minutes = (newdate - model.updatetime).TotalMinutes;
                            if (minutes <= StaticData.ReadInterval)
                            {
                                StaticData.ConsoleWrite($"{count}路设备{address},{minutes}分钟内发送过控制指令,本次读状态指令不发送！", 1);
                                continue;
                            }
                            Thread.Sleep(600);
                            StaticData.ConsoleWrite($"{count}路设备{address}发送读状态指令 data:{ReStr}", 1);
                            StaticData.TCPSend(v.Key, send, true);
                        }
                    #endregion
                }
            }
            }
            else
            {
                if (list == null || list.Count <= 0)
                    StaticData.ConsoleWrite("未查询到设备，读取设备状态失败！", 5);
                else
                    StaticData.ConsoleWrite("无客户端连接，读取设备状态失败！", 5);
            }
        }
    }
}
