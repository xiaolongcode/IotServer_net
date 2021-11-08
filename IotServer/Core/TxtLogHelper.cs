using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IotServer.Core
{
    public class TxtLogHelper
    {


        /// <summary>
        /// 写普通日志，存放到指定路径，使用默认日志类型
        /// </summary>
        /// <param name="msg">日志内容</param>
        /// <param name="_logtype">0 信息 1 数据上报 2 数据下发 3 设备连接  4 sql执行  5 执行异常</param>
        private static bool WriteLog(string msg, int _logtype)
        {
            try
            {
                string islog = AppSetting.GetValue("printlog");//日志是否打印 0不打印 1打印
                if (islog != "1")
                {
                    return false;
                }
                string fileName = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\\Log\\";
                switch (_logtype)
                {
                    case 0:
                        fileName += "Info\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                        break;
                    case 1:
                        fileName += "Recieve\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                        break;
                    case 2:
                        fileName += "Send\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                        break;
                    case 3:
                        fileName += "Connection\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                        break;
                    case 4:
                        fileName += "SQL\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                        break;
                    case 5:
                        fileName += "Exception\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                        break;
                    default:
                        fileName += "Info\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";
                        break;
                }
                string logContext = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + msg;

                return WriteFile(logContext, fileName);
            }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
            catch (Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
            {
                return false;
            }
        }
        /// <summary>
        /// 常规日志
        /// </summary>
        /// <param name="msg">日志内容</param>
        /// <returns></returns>
        public static bool InfoLog(string msg)
        {
            return WriteLog(msg, 0);
        }
        /// <summary>
        /// 设备数据上报
        /// </summary>
        /// <param name="msg">日志内容</param>
        /// <returns></returns>
        public static bool RecieveLog(string msg)
        {
            return WriteLog(msg, 1);
        }
        /// <summary>
        /// 平台数据下发
        /// </summary>
        /// <param name="msg">日志内容</param>
        /// <returns></returns>
        public static bool SendLog(string msg)
        {
            return WriteLog(msg, 2);
        }
        /// <summary>
        /// 设备连接情况
        /// </summary>
        /// <param name="msg">日志内容</param>
        /// <returns></returns>
        public static bool ConnectionLog(string msg)
        {
            return WriteLog(msg, 3);
        }
        /// <summary>
        /// SQL数据执行
        /// </summary>
        /// <param name="msg">日志内容</param>
        /// <returns></returns>
        public static bool SQLLog(string msg)
        {
            return WriteLog(msg, 4);
        }

        /// <summary>
        /// 执行异常
        /// </summary>
        /// <param name="msg">日志内容</param>
        /// <returns></returns>
        public static bool ErroLog(string msg)
        {
            return WriteLog(msg,5);
        }
     
        /// <summary>
        /// 写日志到文件
        /// </summary>
        /// <param name="logContext">日志内容</param>
        /// <param name="fullName">文件名</param>
        private static bool WriteFile(string logContext, string fullName)
        {
            bool b = false;
            FileStream fs = null;
            StreamWriter sw = null;

            int splitIndex = fullName.LastIndexOf('\\');
            if (splitIndex == -1)
                return b;
            string path = fullName.Substring(0, splitIndex);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            try
            {
                if (!File.Exists(fullName)) fs = new FileStream(fullName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
                else fs = new FileStream(fullName, FileMode.Append);

                sw = new StreamWriter(fs);
                logContext += "\r\n";
                sw.WriteLine(logContext);
                b = true;

            }
            catch
            {
                b = false;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                }
                if (fs != null)
                {
                    fs.Close();
                    fs.Dispose();
                }
            }
            return b;
        }
    }
}
