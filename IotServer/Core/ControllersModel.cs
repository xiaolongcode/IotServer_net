using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotServer.Core
{
    public class ControllersModel
    {
        /// <summary>
        ///设备地址
        /// </summary>
        public string address { get; set; }

        /// <summary>
        ///控制指令发送时间
        /// </summary>
        public DateTime updatetime { get; set; }
        /// <summary>
        /// 回路数量
        /// </summary>
        public string waycount { get; set; }

        /// <summary>
        /// 设备ip
        /// </summary>
        public string serverip { get; set; }

        /// <summary>
        /// 设备端口
        /// </summary>
        public string port { get; set; }

        /// <summary>
        /// TCP连接id
        /// </summary>
        public string ConnectionId { get; set; }
    }
}
