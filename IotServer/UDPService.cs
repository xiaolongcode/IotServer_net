using Coldairarrow.DotNettySocket;
using IotServer.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IotServer
{
    public class UDPService
    {
        public static IUdpSocket UdpServer;


        /// <summary>
        /// 启动服务
        /// </summary>
        public static async Task UDPStart(int Port)
        {
            #region UDP 服务
            try
            {
                UdpServer = await SocketBuilderFactory.GetUdpSocketBuilder(Port)
                  .OnClose(server =>
                  {
                      StaticData.ConsoleWrite($"UDP服务端关闭");
                  })
                  .OnException(ex =>
                  {
                      StaticData.ConsoleWrite($"UDP服务端异常:{ex.Message}");
                      TxtLogHelper.ErroLog($"UDP服务端异常:{ex.Message}");
                  })
                  .OnRecieve((server, point, data) =>
                  {
                      ReceiveInfo(server, point, data);

                  })
                  .OnSend((server, point, bytes) =>
                  {
                      StaticData.ConsoleWrite($"UDP服务端发送数据:目标[{point.ToString()}]数据:{StaticData.ByteToString2(bytes)}");
                  })
                  .OnStarted(server =>
                  {
                      StaticData.ConsoleWrite($"UDP服务端启动 监听端口：" + Port);
                  }).BuildAsync();
            }
            catch (Exception ex)
            {
                StaticData.ConsoleWrite($"UDP服务端异常:{ex.Message}");
                TxtLogHelper.ErroLog($"UDP服务端执行异常:{ex.Message}");
            }
            #endregion
        }

        /// <summary>
        /// 分析UDP
        /// </summary>
        private static void ReceiveInfo(IUdpSocket server, EndPoint point, byte[] data)
        {
            try
            {
                if (data[0] == 0x66 && data[1] == 0x88)//自定义 初始化数据，并重新读取设备状态
                {
                    string ReStr = StaticData.ByteToString2(data);
                    StaticData.ConsoleWrite("UDP接收数据刷新命令 指令：" + ReStr, 2);
                    TimerService time = new TimerService();
                    time.ReadIotData();
                    System.Threading.Thread.Sleep(1000);
                    IotReadStateService iotstate = new IotReadStateService();
                    iotstate.DingShiState();
                }
                else //功能码 0x10 写  0x03读 
                {
                    string address = ((int)data[0]).ToString();//设备地址
                    string ReStr = StaticData.ByteToString2(data);
                    StaticData.ConsoleWrite("UDP接收数据目标设备" + address + " 指令：" + ReStr, 2);
                    TxtLogHelper.SendLog("UDP接收数据目标设备" + address + " 指令：" + ReStr);
                    Dictionary<string, DicIotModel> diciot =new Dictionary<string, DicIotModel>( StaticData.GetDicIot());
                    if (diciot != null && diciot.Count > 0)
                    {
                        ControllersModel model = StaticData.GetControllers(address);
                        if (model != null && diciot.ContainsKey(model.ConnectionId))
                        {
                            #region 给指定ip发送指令
                            System.Threading.Thread.Sleep(300);
                            bool b = StaticData.TCPSend(model.ConnectionId, data);
                            if (b)
                                StaticData.ConsoleWrite($"设备{address}发送控制指令成功 data:{ReStr}", 1);
                            else
                                StaticData.ConsoleWrite($"设备{address}发送控制指令失败 data:{ReStr}", 1);
                            #endregion
                        }
                        else
                        {
                            #region 给所有ip发送指令
                            foreach (var v in diciot)
                            {
                                System.Threading.Thread.Sleep(300);
                                //StaticData.ConsoleWrite($"设备{address}发送控制指令 data:{ReStr}", 1);
                                bool b = StaticData.TCPSend(v.Key, data);
                                if (b)
                                    StaticData.ConsoleWrite($"设备{address}发送控制指令成功 data:{ReStr}", 1);
                                else
                                    StaticData.ConsoleWrite($"设备{address}发送控制指令失败 data:{ReStr}", 1);
                            }
                            #endregion
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TxtLogHelper.ErroLog("UDP消息解析 Exception:" + ex.Message);
            }
        }
    }
}
