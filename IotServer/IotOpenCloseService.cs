using IotServer.Core;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IotServer
{
    /// <summary>
    /// 定时开关
    /// </summary>
    public class IotOpenCloseService : IJob
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
                    StaticData.ConsoleWrite("开始执行定时开关控制任务",4);
                    IsRun = false;
                    try
                    {
                        #region 业务代码
                        TxtLogHelper.InfoLog("执行定时任务处理");
                        DingShiKaiGuan();
                        TxtLogHelper.InfoLog("结束定时任务处理");
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        TxtLogHelper.ErroLog("定时开关任务处理 Exception:" + ex.Message);
                    }
                    IsRun = true;
                    StaticData.ConsoleWrite("结束执行定时开关控制任务", 4);
                });
            }
            else
            {
                if (StaticData.controlState)
                    StaticData.ConsoleWrite("正在发送设备手动控制指令，下次调用", 4);
                else
                    StaticData.ConsoleWrite("正在执行定时开关控制任务，下次调用", 4);
            }
        }

        /// <summary>
        /// 定时开关
        /// </summary>
        private void DingShiKaiGuan()
        {
            int hour = DateTime.Now.Hour;
            int minte = DateTime.Now.Minute;
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            int week = Convert.ToInt32(DateTime.Now.DayOfWeek.ToString("d")) + 1;
            string s = string.Format(" select case	when (onh='{0}' and onm='{1}') then 1 " +
                                        " when (offh='{0}' and offm='{1}') then 0	end as type,deviceids from modeunite mu " +
                                        " inner join modedate md on mu.modedateid=md.id " +
                                        " inner join modetime mt on mu.modetimeid=mt.id " +
                                        " left join (select * from moderound where date='{2}') a on a.modedateid=mu.modedateid" +
                                        " left join (select * from moderoundweek where week ='{3}') b on b.modedateid=mu.modedateid" +
                                        " where (everyday=1 or date is not null or week is not null) " +
                                        " and ((onsj=1 and onh='{0}' and onm='{1}') or (offsj=1 and offh='{0}' and offm='{1}'))" +
                                        " and ((onh='{0}' and onm='{1}') or (offh='{0}' and offm='{1}'))" +
                                        " and (onh<>offh or onm<>offm)", hour, minte, date, week);
            DataTable dt = SQLiteHelper.ExecuteDataTable(s, null);
            Dictionary<string, DicIotModel> diciot = new Dictionary<string, DicIotModel>(StaticData.GetDicIot());
            if (dt.Rows.Count > 0&& diciot!=null&& diciot.Count>0)
            {
           
                TxtLogHelper.SQLLog("定时开关查询 SQL：" + s);

                StaticData.ConsoleWrite("模式服务-定时（共查询到" + dt.Rows.Count + "条记录）（" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "）开始......",4);
                for (int k = 0; k < dt.Rows.Count; k++)
                {
                    try
                    {
                        string type = dt.Rows[k]["type"].ToString();
                        string deviceids = dt.Rows[k]["deviceids"].ToString();
                        #region 如果只有一个回路
                        if (deviceids.Length == 36)
                        {
                            #region SQL
                            string sql = " select v.*,c.serverip,c.serverport,c.address,c.mdeptid,c.waycount from devices v left join controllers c on v.code = c.code " +
                                         " where v.id='" + deviceids + "'";
                            DataTable dtdv = SQLiteHelper.ExecuteDataTable(sql, null);
                            #endregion
                            if (dtdv.Rows.Count == 1)
                            {
                                string id = dtdv.Rows[0]["id"].ToString();
                                string address = dtdv.Rows[0]["address"].ToString();
                                string ip = dtdv.Rows[0]["serverip"].ToString();
                                string port = dtdv.Rows[0]["serverport"].ToString();
                                string mdeptid= dtdv.Rows[0]["mdeptid"].ToString();
                                string deptid = dtdv.Rows[0]["deptid"].ToString();
                                string count = dtdv.Rows[0]["waycount"].ToString();
                                int way = Convert.ToInt32(dtdv.Rows[0]["way"].ToString());
                                byte[] data = new byte[9];
                                //地址
                                data[0] = (byte)Convert.ToInt32(address);
                                //功能码
                                data[1] = 0x10;
                                //起始寄存器  单路和六路：00 0d
                                data[2] = 0x00;
                                data[3] = (byte)(15 + way);
                                try
                                {
                                    if (int.Parse(count) == 1 || int.Parse(count) == 6)
                                    {
                                        //起始寄存器  单路和六路：00 00
                                        data[2] = 0x00;
                                        data[3] = 0x0d;
                                    }
                                }
                                catch { }

                                //回路个数
                                data[4] = 0x00;
                                data[5] = 0x01;
                                //字节数
                                data[6] = 0x02;
                                //开或关
                                if (type == "1")
                                {
                                    data[7] = 0x01;
                                    data[8] = 0xaa;
                                }
                                else
                                {
                                    data[7] = 0x00;
                                    data[8] = 0x55;
                                }

                                byte[] crc = StaticData.CRC16_C(data);
                                byte[] send = StaticData.CopyByte(data, crc);
                                string ReStr = StaticData.ByteToString2(send);
                                ControllersModel model = StaticData.GetControllers(address);
                                if (model != null && diciot.ContainsKey(model.ConnectionId))
                                {
                                    #region 给指定ip发送指令
                                    Thread.Sleep(500);
                                    StaticData.ConsoleWrite($"设备{address}发送模式服务控制指令 data:{ReStr}", 1);
                                    StaticData.TCPSend(model.ConnectionId, send);
                                    #endregion
                                }
                                else
                                {
                                    #region 给所有ip发送指令
                                    foreach (var v in diciot)
                                    {
                                        Thread.Sleep(500);
                                        StaticData.ConsoleWrite($"设备{address}发送模式服务控制指令 data:{ReStr}", 1);
                                        StaticData.TCPSend(v.Key, send);
                                    }
                                    #endregion
                                }
                                #region 更新控制状态并添加操作日志
                                string str = string.Format("update devices set isopen='{0}',controltime='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',opencloseState='{1}' where id ='" + deviceids + "' ", type, Convert.ToInt32(type));
                                SQLiteHelper.ExecuteNonQuery(str, null);
                                id = "'" + id + "'";
                                AddLogs(id, type, "模式服务（时间）",deptid,mdeptid);
                                #endregion
                            }
                        }
                        #endregion

                        #region 如果包含多个回路
                        if (deviceids.Length > 36)
                        {
                            ControlKaiGuan(deviceids.Split(','), type, "模式服务（时间）", diciot);
                            Thread.Sleep(300);
                            ControlKaiGuan(deviceids.Split(','), type, "模式服务（时间）", diciot);
                        }
                        #endregion
                    }
                    catch(Exception ex)
                    {
                        TxtLogHelper.ErroLog("模式服务-定时-单回路 Exception" + ex.Message);
                    }

                }
                StaticData.ConsoleWrite("模式服务-定时（共查询到" + dt.Rows.Count + "条记录）（" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "）结束......",4);
            }
            else
            {
                StaticData.ConsoleWrite("未查询到需要执行的定时开关数据", 4);
            }
        }
        private void ControlKaiGuan(string[] deviceids, string type, string servername, Dictionary<string, DicIotModel> diciot)
        {
            try
            {
                #region 查询条件

                string deviceids_ = "";
                for (int i = 0; i < deviceids.Length; i++)
                {
                    deviceids_ += "'" + deviceids[i] + "',";
                }
                deviceids_ = deviceids_.Remove(deviceids_.Length - 1, 1);
                #endregion

                #region 遍历设备
                string sql1 = " select v.id,v.code,c.serverip,c.serverport,c.address,c.waycount,c.mdeptid,v.deptid  from devices v left join controllers c on v.code = c.code " +
                              " where v.id in(" + deviceids_ + ")" +
                              " group by v.code,c.serverport,c.serverip,c.address,c.waycount,c.mdeptid,v.deptid";
                DataTable dt1 = SQLiteHelper.ExecuteDataTable(sql1, null);
                for (int i = 0; i < dt1.Rows.Count; i++)
                {
                    try
                    {
                        string code = dt1.Rows[i]["code"].ToString();
                        string address = dt1.Rows[i]["address"].ToString();
                        string ip = dt1.Rows[i]["serverip"].ToString();
                        string port = dt1.Rows[i]["serverport"].ToString();
                        string count = dt1.Rows[i]["waycount"].ToString();
                        string mdeptid = dt1.Rows[0]["mdeptid"].ToString();
                        string deptid = dt1.Rows[0]["deptid"].ToString();
                        byte[] data = new byte[7 + Convert.ToInt32(count) * 2];
                        //地址
                        data[0] = (byte)Convert.ToInt32(address);
                        //功能码
                        data[1] = 0x10;
                        //起始寄存器
                        data[2] = 0x00;
                        data[3] = 0x10;
                        try
                        {
                            if (int.Parse(count) == 1 || int.Parse(count) == 6)
                            {
                                //起始寄存器  单路和六路：00 00
                                data[2] = 0x00;
                                data[3] = 0x0d;
                            }
                        }
                        catch { }

                        //寄存器个数
                        data[4] = 0x00;
                        data[5] = (byte)int.Parse(count);
                        //字节数
                        data[6] = (byte)(int.Parse(count) * 2);
                        //开或关
                        for (int n = 0; n < int.Parse(count); n++)
                        {
                            data[7 + n * 2] = 0xff;
                            data[8 + n * 2] = 0xff;
                        }
                        string sql2 = "select id,way from devices where id in (" + deviceids_ + ") and code='" + code + "'";
                        DataTable dt2 = SQLiteHelper.ExecuteDataTable(sql2, null);
                        for (int k = 0; k < dt2.Rows.Count; k++)
                        {
                            int way = Convert.ToInt32(dt2.Rows[k]["way"].ToString()) - 1;
                            if (type == "0")
                            {
                                data[7 + way * 2] = 0x00;
                                data[8 + way * 2] = 0x55;
                            }
                            if (type == "1")
                            {
                                data[7 + way * 2] = 0x01;
                                data[8 + way * 2] = 0xaa;
                            }
                        }
                        byte[] crc = StaticData.CRC16_C(data);
                        byte[] send = StaticData.CopyByte(data, crc);
                        string ReStr = StaticData.ByteToString2(send);
                        ControllersModel model= StaticData.GetControllers(address);
                        if (model != null && diciot.ContainsKey(model.ConnectionId))
                        {
                            #region 给指定ip发送指令
                            Thread.Sleep(500);
                            StaticData.ConsoleWrite($"设备{address}发送模式服务控制指令 data:{ReStr}", 1);
                            StaticData.TCPSend(model.ConnectionId, send);
                            #endregion
                        }
                        else
                        {
                            #region 给所有ip发送指令
                            foreach (var v in diciot)
                            {

                                Thread.Sleep(500);
                                StaticData.ConsoleWrite($"设备{address}发送模式服务控制指令 data:{ReStr}", 1);
                                StaticData.TCPSend(v.Key, send);
                            }
                            #endregion
                        }
                        for (int k = 0; k < dt2.Rows.Count; k++)
                        {
                            //添加操作日志
                            AddLogs(dt2.Rows[k]["id"].ToString(), type, servername, deptid, mdeptid);
                        }
                    }
                    catch(Exception ex)
                    {
                        TxtLogHelper.ErroLog("模式服务-定时-多回路 Exception" + ex.Message);
                    }
                }

                //更新控制状态
                string str = string.Format("update devices set isopen='{0}',controltime='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "',opencloseState='{1}' where id in(" + deviceids_ + "); ", type, Convert.ToInt32(type));
                str += string.Format("update controllers set updatetime='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where code in(select DISTINCT code from devices where id in(" + deviceids_ + ")); ");
                SQLiteHelper.ExecuteNonQuery(str, null);
              
                #endregion
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {

            }
        }
        /// <summary>
        /// 操作日志
        /// </summary>
        private void AddLogs(string deviceids_, string type, string servername,string deptid,string mdeptid)
        {
            string errosql = "";
            try
            {
                if (type == "1")
                {
                    type = "开";
                }
                else
                {
                    type = "关";
                }
                if (!deviceids_.Contains("'"))
                {
                    deviceids_ = "'" + deviceids_ + "'";
                }
                string sql = " insert into logs(id,userid,username,deviceid,datetime,descn,deptid,mdeptid) " +
                             " SELECT '" + Guid.NewGuid().ToString() + "' as 'id', '" + servername + "' AS 'userid','" + servername + "' AS 'username',id,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' as 'datetime','" + type + "' as 'descn','" +
                             deptid+"' ,'"+ mdeptid + "' from devices where " +
                             " id in(" + deviceids_ + ")";
                errosql = sql;
                SQLiteHelper.ExecuteNonQuery(sql, null);
            }
            catch (Exception ex)
            {
                TxtLogHelper.ErroLog("定时开关添加操作日志 Exception" + ex.Message+" sql:"+ errosql);
            }
        }

        /// <summary>
        /// 操作日志
        /// </summary>
        private void AddLogs(string type, string servername, string deptid, string mdeptid)
        {
            string errosql = "";
            try
            {
                if (type == "1")
                {
                    type = "开";
                }
                else
                {
                    type = "关";
                }
                string sql = " insert into logs(id,userid,username,datetime,descn,deptid,mdeptid) " +
                             " SELECT '" + Guid.NewGuid().ToString() + "' as 'id', '" + servername + "' AS 'userid','" + servername + "' AS 'username','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' as 'datetime','" + type + "' as 'descn','" +
                             deptid + "' ,'" + mdeptid +"'";
                errosql = sql;
                SQLiteHelper.ExecuteNonQuery(sql, null);
            }
            catch (Exception ex)
            {
                TxtLogHelper.ErroLog("定时开关添加操作日志 Exception" + ex.Message + " sql:" + errosql);
            }
        }
    }
}
