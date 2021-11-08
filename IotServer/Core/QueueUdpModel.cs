using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace IotServer.Core
{
    public class QueueUdpModel
    {
        public EndPoint Point { get; set; }

        public string Key { get; set; }

        public int ServerPort { get; set; }

        /// <summary>
        /// 0：照度上报  1：设备反馈(34)(6路)  2：8路设备(70)  3：[收]设备时间（39） 4：[收]设备定时（36） 5：[收]设置设备定时反馈（35） 6：[收]设备参数（31）
        /// 7：[收]设备变比（3d） 8：[收]设备额定电流(3b)  
        /// 9：更新33、37、87设备  10：更新50设备   11：更新71程序（电能表） 12：更新72程序（电能表+箱门报警） 13：更新71程序（电能表） 14：更新60设备
        /// </summary>
        public int Type { get; set; }

        public byte[] Data { get; set; }
    }
}
