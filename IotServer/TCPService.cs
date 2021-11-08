using Coldairarrow.DotNettySocket;
using IotServer.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IotServer
{
    public class TCPService
    {
        public static ITcpSocketServer TcpServer;
        /// <summary>
        /// 启动服务
        /// </summary>
        /// <returns></returns>
        public static async Task TCPStart(int Port, int timeout)
        {
            #region TCP 服务
            try
            {
                TcpServer = await SocketBuilderFactory.GetTcpSocketServerBuilder(Port)
               //.SetLengthFieldEncoder(2)
               //.SetLengthFieldDecoder(ushort.MaxValue, 0, 2, 0, 2)
               .SetTimeout(timeout, 0, 0)
               .OnConnectionClose((server, connection) =>
               {
                   StaticData.ConsoleWrite($"TCP连接关闭,连接名[{connection.ConnectionName}],当前连接数:{server.GetConnectionCount()}");
                   TxtLogHelper.ConnectionLog($"TCP连接关闭,连接名[{connection.ConnectionName}],当前连接数:{server.GetConnectionCount()}");
                   StaticData.RemoveDicIot(connection.ConnectionId);

               })
               .OnException(ex =>
               {
                   StaticData.ConsoleWrite($"TCP服务端异常:{ex.Message}");
                   TxtLogHelper.ErroLog($"TCP服务端异常:{ex.Message}");

               })
               .OnNewConnection((server, connection) =>
               {
                   connection.ConnectionKey = connection.ConnectionId;
                   StaticData.ConsoleWrite($"TCP新的连接[{connection.ClientAddress.Address.MapToIPv4().ToString()}:{connection.ClientAddress.Port}]:{connection.ConnectionName},当前连接数:{server.GetConnectionCount()}");
                   TxtLogHelper.ConnectionLog($"TCP新的连接[{ connection.ClientAddress.Address.MapToIPv4().ToString()}:{ connection.ClientAddress.Port}]:{ connection.ConnectionName},当前连接数: { server.GetConnectionCount()}");

                   StaticData.AddDicIot(connection.ConnectionId, new DicIotModel() { Point = connection.ClientAddress });
               })
               .OnRecieve((server, connection, bytes) =>
               {
                   //处理客户端上报的内容
                   ReceiveInfo(server, connection, bytes);
               })
               .OnSend((server, connection, bytes) =>
               {
                   //Console.WriteLine($"TCP服务端向连接名[{connection.ConnectionName}]发送数据:{Encoding.UTF8.GetString(bytes)}");
               })
               .OnServerStarted(server =>
               {
                   StaticData.ConsoleWrite($"TCP服务启动 监听端口：" + Port);
               }).BuildAsync();
            }
            catch (Exception ex)
            {
                StaticData.ConsoleWrite($"TCP服务端异常:{ex.Message}");
                TxtLogHelper.ErroLog($"TCP服务端异常:{ex.Message}");
            }
            #endregion
        }
        /// <summary>
        /// 分析TCP
        /// </summary>
        public static void ReceiveInfo(ITcpSocketServer server, ITcpSocketConnection connection, byte[] Data)
        {
            try
            {

                string Key = ((int)Data[0]).ToString();//设备地址
                string ReStr = StaticData.ByteToString2(Data);
                if (Data[1] == 0x03)//读状态指令返回
                {
                    StaticData.ConsoleWrite($"设备{Key}读状态指令返回数据  data:{ReStr}",1);
              
                    StaticData.AddTcpQueue(new QueueTcpModel { Data = Data,Id=connection.ConnectionId, Key = Key,Point=connection.ClientAddress });
                    StaticData.SetControllers(new ControllersModel { address = Key, ConnectionId = connection.ConnectionId },true);
                }
                else
                {
                    StaticData.ConsoleWrite($"设备{Key}控制指令回复  data:{ReStr}",1);
                    TxtLogHelper.RecieveLog($"设备{Key}控制指令回复：" + ReStr);
                }
            }
            catch (Exception ex)
            {
                TxtLogHelper.ErroLog("TCP消息解析 Exception:" + ex.Message);
            }
        }
    }
}
