using IotServer.Core;
using Quartz;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IotServer
{
    /// <summary>
    /// 定时任务 20小时执行一次
    /// </summary>
    public class TimerService : IJob
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
                    try
                    {
                        #region 业务代码
                        TxtLogHelper.InfoLog("执行定时任务处理");
                        DeleteFile(AppSetting.GetValueByInt("fileday"));
                        ReadIotData();
                        DeleteLogs();
                        TxtLogHelper.InfoLog("结束定时任务处理");
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        TxtLogHelper.ErroLog("定时任务处理 Exception:" + ex.Message);
                    }
                    IsRun = true;
                });
            }
            else
            {
                StaticData.ConsoleWrite("正在定时任务处理任务，下次调用");
            }
        }
        /// <summary>
        /// 删除三个月以上的操作日志
        /// </summary>
        public void DeleteLogs()
        {
            try
            {
                string date = DateTime.Now.AddMonths(-3).ToString("yyyy-MM-dd");
                string sql = string.Format(" delete from logs where datetime<'" + date + "' ");
                int count = SQLiteHelper.ExecuteNonQuery(sql, null);
                TxtLogHelper.InfoLog("删除" + date + "日期之前的操作日志，删除条数：" + count);
                StaticData.ConsoleWrite("删除" + date + "日期之前的操作日志，数据条数：" + count);
            }
            catch (Exception ex)
            {
                TxtLogHelper.ErroLog("删除三个月以上的操作日志 Exception:" + ex.Message);
            }
        }
        /// <summary>
        /// 定时读取设备数据到缓存
        /// </summary>
        public void ReadIotData()
        {
            try
            {
                string sql = string.Format(" select address,waycount,serverip,serverport from controllers order by code ");
                DataTable dt = SQLiteHelper.ExecuteDataTable(sql, null);
                if (dt != null && dt.Rows.Count > 0)
                {
                    DateTime newdate = DateTime.Now.AddMinutes(-StaticData.ReadInterval);
                    foreach (DataRow row in dt.Rows)
                    {
                        string address = row["address"].ToString();
                        string port = row["serverport"].ToString();
                        string ip = row["serverip"].ToString();
                        string count = row["waycount"].ToString();
                        StaticData.SetControllers(new ControllersModel() { address = address, port = port, serverip = ip, waycount = count, updatetime = newdate,ConnectionId="" });
                    }
                    StaticData.ConsoleWrite("读取设备数据，设备总数：" + dt.Rows.Count);
                }
                else
                {
                    StaticData.ConsoleWrite("未查询到设备，设备数据读取失败！");
                }
            }
            catch (Exception ex)
            {
                TxtLogHelper.ErroLog("定时读取设备数据到缓存 Exception:" + ex.Message);
            }
        }
        /// <summary>
        /// 删除目录下超过指定天数的文件
        /// </summary>
        private void DeleteFile(int saveDay)
        {
            try
            {
                string fileDirect = System.Environment.CurrentDirectory + @"\\Log\\";
                TxtLogHelper.InfoLog("删除" + fileDirect + "目录下的所有超过" + saveDay + "天的log文件");
                if (Directory.Exists(fileDirect))
                {
                    int count = 0;
                    DateTime nowTime = DateTime.Now;
                    string[] files = Directory.GetFiles(fileDirect, "*.log", SearchOption.AllDirectories);  //获取该目录下所有 .txt文件
                    foreach (string file in files)
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        TimeSpan t = nowTime - fileInfo.CreationTime;  //当前时间  减去 文件创建时间
                        int day = t.Days;
                        if (day > saveDay)   //保存的时间，单位：天
                        {
                            try
                            {
                                System.IO.File.Delete(fileInfo.FullName); //删除文件
                                count++;
                            }
                            catch (Exception err)
                            {
                                TxtLogHelper.ErroLog("删除日志文件" + fileInfo.FullName + " 操作异常：" + err.Message);
                            }
                        }
                    }
                    StaticData.ConsoleWrite("执行了一次删除日志任务,删除了"+ count + "个日志文件");
                }
            }
            catch (Exception ex)
            {
                TxtLogHelper.ErroLog("删除目录下超过"+ saveDay + "天的文件 Exception:" + ex.Message);
            }
        }
    }
}
