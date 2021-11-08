using IotServer.Core;
using Quartz;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IotServer
{
    public class TimerTCPService : IJob
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
                    QueueTcpModel TCP = new QueueTcpModel();
                    try
                    {
                        #region 业务代码
                        int count = StaticData.GetTcpQueueCount();
                        if (count > 0)
                            StaticData.ConsoleWrite("TCP消息处理 消息数量：" + count, 6);
                        DateTime newdate = DateTime.Now;
                        string updatetime = newdate.ToString("yyyy-MM-dd HH:mm:ss");
                        List<ControllersModel> list = new List<ControllersModel>(StaticData.GetAllControllers());
                       
                        while (StaticData.GetTcpQueueCount() > 0)
                        {
                            string address = "";
                            try
                            {
                                TCP = StaticData.GetTcpQueue();
                                IPEndPoint endpoint = TCP.Point as IPEndPoint;
                                string ip = endpoint.Address.MapToIPv4().ToString();
                                int port = endpoint.Port;
                                byte[] Data = TCP.Data;
                                address = ((int)Data[0]).ToString();
                                string sql = "";
                                string ReStr = "";
                                int sqlcount = 0;
                                ControllersModel iotmodel = list.Find(o=>o.address== address);
                                int waysum = 0;
                                if (iotmodel != null&& int.TryParse(iotmodel.waycount, out waysum))
                                {
                                    #region  根据回路数判断设备
                                    switch (waysum)
                                    {
                                        case 1:
                                        case 6:
                                            #region 一路和六路设备
                                            ReStr = StaticData.ByteToString2(Data);
                                            #region 数据库更新
                                            //更新终端表
                                            sql = "";// " update devices set  updatetime='" + updatetime + "' where code =( select code from controllers  where address='" + address + "');";
                                            for (int i = 1; i <= 6; i++)
                                            {
                                                int isopen = -1;// 1 aa 开  0 55 关
                                                if (Data[14 + i * 2] == 0xaa)
                                                    isopen = 1;
                                                else if (Data[14 + i * 2] == 0x55)
                                                    isopen = 0;
                                                sql += "update devices set  updatetime = '" + updatetime + "',isopen=" + isopen + " where code = (select code from controllers where address = '" + address + "') and way=" + i + "; ";
                                            }
                                            //更新控制器表
                                            sql += "update controllers set updatetime='" + updatetime + "', serverip='" + ip + "' ,serverport='" + port + "' where address='" + address + "'";

                                            if (iotmodel != null)
                                            {
                                                double minutes = (newdate - iotmodel.updatetime).TotalMinutes;
                                                if (minutes <= StaticData.ReadInterval)
                                                {
                                                    StaticData.ConsoleWrite($"{waysum}路设备{address},{minutes}分钟内发送过控制指令,本次读状态指令返回数据不更新！", 1);
                                                    continue;
                                                }
                                            }
                                            sqlcount = SQLiteHelper.ExecuteNonQuery(sql, null);
                                            #endregion
                                            #endregion
                                            break;
                                        case 4:
                                        case 8:
                                            #region 四路和八路设备
                                            int waycount = (int)Data[2] / 2;
                                            ReStr = StaticData.ByteToString2(Data);
                                            #region 数据库更新
                                            //更新终端表
                                            sql = "";// " update devices set  updatetime='" + updatetime + "' where code =( select code from controllers  where address='" + address + "');";
                                            for (int i = 1; i <= waycount; i++)
                                            {
                                                int isopen = -1;// 1 aa 开  0 55 关
                                                if (Data[2 + i * 2] == 0xaa)
                                                    isopen = 1;
                                                else if (Data[2 + i * 2] == 0x55)
                                                    isopen = 0;
                                                sql += "update devices set  updatetime = '" + updatetime + "',isopen=" + isopen + " where code = (select code from controllers where address = '" + address + "') and way=" + i + "; ";
                                            }
                                            //更新控制器表
                                            sql += "update controllers set updatetime='" + updatetime + "', serverip='" + ip + "' ,serverport='" + port + "' where address='" + address + "'";

                                            if (iotmodel != null)
                                            {
                                                double minutes = (newdate - iotmodel.updatetime).TotalMinutes;
                                                if (minutes <= StaticData.ReadInterval)
                                                {
                                                    StaticData.ConsoleWrite($"{waysum}路设备{address},{minutes}分钟内发送过控制指令,本次读状态指令返回数据不更新！", 1);
                                                    continue;
                                                }
                                            }
                                            sqlcount = SQLiteHelper.ExecuteNonQuery(sql, null);
                                            #endregion
                                            #endregion
                                            break;

                                    }
                                    #endregion
                                }
                                else
                                {
                                    #region 根据字节数判断
                                    if (Data.Length > 26)
                                    {
                                        #region 单路六路
                                        ReStr = StaticData.ByteToString2(Data);
                                        #region 数据库更新
                                        //更新终端表
                                        sql = "";// " update devices set  updatetime='" + updatetime + "' where code =( select code from controllers  where address='" + address + "');";
                                        for (int i = 1; i <= 6; i++)
                                        {
                                            int isopen = -1;// 1 aa 开  0 55 关
                                            if (Data[14 + i * 2] == 0xaa)
                                                isopen = 1;
                                            else if (Data[14 + i * 2] == 0x55)
                                                isopen = 0;
                                            sql += "update devices set  updatetime = '" + updatetime + "',isopen=" + isopen + " where code = (select code from controllers where address = '" + address + "') and way=" + i + "; ";
                                        }
                                        //更新控制器表
                                        sql += "update controllers set updatetime='" + updatetime + "', serverip='" + ip + "' ,serverport='" + port + "' where address='" + address + "'";

                                        if (iotmodel != null)
                                        {
                                            double minutes = (newdate - iotmodel.updatetime).TotalMinutes;
                                            if (minutes <= StaticData.ReadInterval)
                                            {
                                                StaticData.ConsoleWrite($"{iotmodel.waycount}路设备{address},{minutes}分钟内发送过控制指令,本次读状态指令返回数据不更新！", 1);
                                                continue;
                                            }
                                        }
                                        sqlcount = SQLiteHelper.ExecuteNonQuery(sql, null);
                                        #endregion
                                        #endregion
                                    }
                                    else
                                    {
                                        int waycount = (int)Data[2] / 2;
                                        ReStr = StaticData.ByteToString2(Data);
                                        #region 数据库更新
                                        //更新终端表
                                        sql = "";// " update devices set  updatetime='" + updatetime + "' where code =( select code from controllers  where address='" + address + "');";
                                        for (int i = 1; i <= waycount; i++)
                                        {
                                            int isopen = -1;// 1 aa 开  0 55 关
                                            if (Data[2 + i * 2] == 0xaa)
                                                isopen = 1;
                                            else if (Data[2 + i * 2] == 0x55)
                                                isopen = 0;
                                            sql += "update devices set  updatetime = '" + updatetime + "',isopen=" + isopen + " where code = (select code from controllers where address = '" + address + "') and way=" + i + "; ";
                                        }
                                        //更新控制器表
                                        sql += "update controllers set updatetime='" + updatetime + "', serverip='" + ip + "' ,serverport='" + port + "' where address='" + address + "'";

                                        if (iotmodel != null)
                                        {
                                            double minutes = (newdate - iotmodel.updatetime).TotalMinutes;
                                            if (minutes <= StaticData.ReadInterval)
                                            {
                                                StaticData.ConsoleWrite($"{iotmodel.waycount}路设备{address},{minutes}分钟内发送过控制指令,本次读状态指令返回数据不更新！", 1);
                                                continue;
                                            }
                                        }
                                        sqlcount = SQLiteHelper.ExecuteNonQuery(sql, null);
                                        #endregion
                                    }
                                    #endregion
                                }

                                StaticData.ConsoleWrite("TCP数据更新成功："+waysum+"路设备：" + address + ",更新" + sqlcount + "条数据", 6);
                                TxtLogHelper.SQLLog("设备" + address + "数据更新 sql：" + sql);
                            }
                            catch (Exception ex)
                            {
                                TxtLogHelper.ErroLog($"设备{address}数据更新失败 Exception:" + ex.Message);
                            }
                        }
                        if (count > 0)
                            StaticData.ConsoleWrite("TCP消息处理完毕，等待下次执行", 6);
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        TxtLogHelper.ErroLog("TCP消息处理 Exception:" + ex.Message);
                    }
                    IsRun = true;
                });
            }
            else
            {
                StaticData.ConsoleWrite("正在执行TCP消息处理任务，下次调用", 6);
            }
        }
    }
}
