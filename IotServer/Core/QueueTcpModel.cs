using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace IotServer.Core
{
    public class QueueTcpModel
    {
        /// <summary>
        /// tcp连接信息
        /// </summary>
        public EndPoint Point { get; set; }

        /// <summary>
        /// 设备地址
        /// </summary>

        public string Key { get; set; }

        /// <summary>
        /// tcp连接id
        /// </summary>
        public string Id { get; set; }

       /// <summary>
       /// tcp接收的数据
       /// </summary>
        public byte[] Data { get; set; }
    }
}
