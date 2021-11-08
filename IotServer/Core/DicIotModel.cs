using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace IotServer.Core
{
    public class DicIotModel
    {
        ///// <summary>
        ///// IP信息
        ///// </summary>
        //public string IP { get; set; }
        ///// <summary>
        ///// 端口
        ///// </summary>
        //public string Port { get; set; }
        ///// <summary>
        ///// 连接类型 1 UDP 2 TCP  
        ///// </summary>
        //public int type { get; set; }


        /// <summary>
        ///连接信息
        /// </summary>
        public EndPoint Point { get; set; }
        /// <summary>
        /// 回路数量
        /// </summary>
        public int WayCount { get; set; }

        /// <summary>
        /// 数据库最后更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }


    }
}
