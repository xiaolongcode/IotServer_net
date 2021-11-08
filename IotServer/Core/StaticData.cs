using Coldairarrow.DotNettySocket;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

namespace IotServer.Core
{
    public class StaticData
    {
        /// <summary>
        /// 设备list
        /// </summary>
        private static List<ControllersModel> IotList = new List<ControllersModel>();
        /// <summary>
        /// 设备连接记录
        /// </summary>
        private static Dictionary<string, DicIotModel> dicIot = new Dictionary<string, DicIotModel>();
        /// <summary>
        /// 客户端连接记录
        /// </summary>
        private static Dictionary<string, DicIotModel> dicClientIot = new Dictionary<string, DicIotModel>();
        /// <summary>
        /// udp消息队列
        /// </summary>
        private static Queue<QueueUdpModel> udpQueue = new Queue<QueueUdpModel>();
        /// <summary>
        /// tcp消息队列
        /// </summary>
        private static Queue<QueueTcpModel> tcpQueue = new Queue<QueueTcpModel>();
        /// <summary>
        /// 是否正在发送控制指令
        /// </summary>
        public static bool controlState = false;
        /// <summary>
        /// 控制指令发送之后不处理读指令时间间隔，单位分（控制指令发送之后默认两分钟之内不操作读指令）
        /// </summary>
        public static int ReadInterval = 2;
        private static IUdpSocket UdpServer;
        private static ITcpSocketServer TcpServer;
        public StaticData(IUdpSocket _UdpServer, ITcpSocketServer _TcpServer)
        {
            UdpServer = _UdpServer;
            TcpServer = _TcpServer;
#pragma warning disable CS4014 // 由于此调用不会等待，因此在此调用完成之前将会继续执行当前方法。请考虑将 "await" 运算符应用于调用结果。
            TimerStart(AppSetting.GetValueByInt("timer"), AppSetting.GetValueByInt("readtimer"));
#pragma warning restore CS4014 // 由于此调用不会等待，因此在此调用完成之前将会继续执行当前方法。请考虑将 "await" 运算符应用于调用结果。
        }
        /// <summary>
        /// 启动定时任务
        /// </summary>
        /// <param name="seconds">定时执行间隔 秒</param>
        /// <param name="type">0 全部  1 udp  2 tcp</param>
        /// <returns></returns>
        public static async System.Threading.Tasks.Task TimerStart(int seconds,int minutes)
        {
            try
            {

                // 1.创建scheduler的引用
                ISchedulerFactory schedFact = new StdSchedulerFactory();
                IScheduler sched = await schedFact.GetScheduler();

                //2.启动 scheduler
                await sched.Start();


                //启动TCP队列监听任务
                // 创建 job
                IJobDetail tcpjob = JobBuilder.Create<TimerTCPService>()
                        .WithIdentity("tcpjob", "tcpgroup")
                        .Build();

                // 创建 trigger
                ITrigger tcptrigger = TriggerBuilder.Create()
                    .WithIdentity("tcptrigger", "tcpgroup")
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(seconds).RepeatForever())
                    .Build();

                //使用trigger规划执行任务job
                await sched.ScheduleJob(tcpjob, tcptrigger);

                ConsoleWrite($"启动TCP队列监听任务,{seconds}秒执行一次");

                ////启动UDP队列监听任务
                ////创建 job
                //IJobDetail udpjob = JobBuilder.Create<TimerUDPService>()
                //    .WithIdentity("udpjob", "udpgroup")

                //    .Build();

                ////创建 trigger
                //ITrigger udptrigger = TriggerBuilder.Create()
                //    .WithIdentity("udptrigger", "udpgroup")
                //    .WithSimpleSchedule(x => x.WithIntervalInSeconds(seconds).RepeatForever())
                //    .Build();

                ////使用trigger规划执行任务job
                //await sched.ScheduleJob(udpjob, udptrigger);
                //ConsoleWrite($"启动UDP队列监听任务,{seconds}秒执行一次");


                //启动定时执行任务
                // 创建 job
                IJobDetail timerjob = JobBuilder.Create<TimerService>()
                        .WithIdentity("timerjob", "timergroup")

                        .Build();

                // 创建 trigger
                ITrigger timertrigger = TriggerBuilder.Create()
                    .WithIdentity("timertrigger", "timergroup")
                    .WithSimpleSchedule(x => x.WithIntervalInHours(20).RepeatForever())
                    .Build();

                //使用trigger规划执行任务job
                await sched.ScheduleJob(timerjob, timertrigger);
                ConsoleWrite($"启动定时执行任务,20小时执行一次");

                //启动定时执行任务
                //创建 job
                IJobDetail timerStatejob = JobBuilder.Create<IotReadStateService>()
                        .WithIdentity("timerStatejob", "timerStategroup")

                        .Build();

                //创建 trigger
                ITrigger timerStatetrigger = TriggerBuilder.Create()
                    .WithIdentity("timerStatetrigger", "timerStategroup")
                    .WithSimpleSchedule(x => x.WithIntervalInMinutes(minutes).RepeatForever())
                    .Build();

                //使用trigger规划执行任务job
                await sched.ScheduleJob(timerStatejob, timerStatetrigger);
                ConsoleWrite($"启动设备状态读取任务,{minutes}分钟执行一次");

                //启动定时执行任务
                //创建 job
                IJobDetail kgStatejob = JobBuilder.Create<IotOpenCloseService>()
                        .WithIdentity("kgStatejob", "kgStategroup")

                        .Build();

                //创建 trigger
                ITrigger kgStatetrigger = TriggerBuilder.Create()
                    .WithIdentity("kgStatetrigger", "kgStategroup")
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(seconds).RepeatForever())
                    .Build();

                //使用trigger规划执行任务job
                await sched.ScheduleJob(kgStatejob, kgStatetrigger);
                ConsoleWrite($"启动设备定时开关任务,{seconds}秒执行一次");
            }
            catch (Exception ex)
            {
                ConsoleWrite($"Quartz任务异常:{ex.Message}");
                TxtLogHelper.ErroLog($"Quartz任务异常:{ex.Message}");
            }
        }
        /// <summary>
        /// 控制台打印（默认白色）
        /// </summary>
        /// <param name="Msg">消息</param>
        /// <param name="type">type 0 系统信息 1 TCP数据  2 UDP数据 3 异常信息 4 定时开关  5 定时读状态 6 消息队列</param>
        public static void ConsoleWrite(string Msg,int type=0)
        {
            switch (type)
            {
                case 0:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case 1:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case 3:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case 4:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 5:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case 6:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            string logdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            TxtLogHelper.InfoLog(Msg);
            Console.WriteLine(logdate + "："+Msg);
        }
        #region 发送数据
        /// <summary>
        /// tcp发送消息
        /// </summary>
        /// <param name="Key">指定TCP链接Key</param>
        /// <param name="data">数据</param>
        /// <param name="isRead">是否读数据指令</param>
        public static bool TCPSend(string Key, byte[] data, bool isRead = false)
        {
            if (TcpServer.GetAllConnectionKeys().Contains(Key))
            {
                Thread.Sleep(200);
                if (!isRead)
                {
                    ControllersModel m = GetControllers(((int)data[0]).ToString());
                    if (m != null)
                    {
                        m.updatetime = DateTime.Now;
                        SetControllers(m);
                    }
                }
                TcpServer.GetConnectionByKey(Key).Send(data);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取指定key对应的ip
        /// </summary>
        /// <param name="Key">Key</param>
        public static string GetTCPIP(string Key)
        {
            if (TcpServer.GetAllConnectionKeys().Contains(Key))
            {
               return TcpServer.GetConnectionByKey(Key).ClientAddress.Address.MapToIPv4().ToString()+":"+ TcpServer.GetConnectionByKey(Key).ClientAddress.Port;
            }
            return "";
        }
        /// <summary>
        /// udp发送消息
        /// </summary>
        /// <param name="ip">客户端ip</param>
        /// <param name="port">客户端端口</param>
        /// <param name="data">数据</param>
        public static void UDPSend(string ip, string port, byte[] data)
        {
            byte[] bip = new byte[4];
            string[] sip = ip.Split('.');
            for (int i = 0; i < 4; i++)
            {
                bip[i] = byte.Parse(sip[i]);
            }
            Thread.Sleep(300);
            UdpServer.Send(data, new IPEndPoint(new IPAddress(bip), Convert.ToInt32(port)));
        }
        /// <summary>
        /// udp发送消息
        /// </summary>
        /// <param name="ip">客户端ip</param>
        /// <param name="port">客户端端口</param>
        /// <param name="data">数据</param>
        public static void UDPSend(EndPoint point, byte[] data)
        {
            Thread.Sleep(300);
            UdpServer.Send(data, point);
        }
        #endregion
        #region 设备连接情况

        /// <summary>
        /// 添加或更新设备连接信息
        /// </summary>
        /// <param name="Key">连接标识</param>
        /// <param name="IotModel">连接信息</param>
        public static void AddDicIot(string Key, DicIotModel IotModel)
        {
            if (dicIot.ContainsKey(Key))
            {
                dicIot[Key] = IotModel;
            }
            else
            {
                IotModel.UpdateTime = DateTime.Now;
                dicIot.Add(Key, IotModel);
            }
        }
        /// <summary>
        /// 根据key获取设备对象
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static DicIotModel GetDicIot(string Key)
        {
            if (dicIot.ContainsKey(Key))
                return dicIot[Key];
            return null;
        }
        /// <summary>
        /// 根据key获取设备对象
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static Dictionary<string, DicIotModel> GetDicIot()
        {
            return dicIot;
        }
        /// <summary>
        /// 是否包含指定Key数据
        /// </summary>
        /// <param name="Key">连接标识</param>
        /// <returns></returns>
        public static bool IsDicIot(string Key)
        {
            return dicIot.ContainsKey(Key);
        }
        /// <summary>
        /// 删除指定Key数据
        /// </summary>
        /// <param name="Key">连接标识</param>
        /// <returns></returns>
        public static bool RemoveDicIot(string Key)
        {
            if (dicIot.ContainsKey(Key))
                return dicIot.Remove(Key);
            return true;
        }
        /// <summary>
        /// 清空连接对象
        /// </summary>
        /// <returns></returns>
        public static void ClearDicIot()
        {
            dicIot.Clear();
        }
        #endregion
        #region 客户端连接情况

        /// <summary>
        /// 添加或更新客户端连接信息
        /// </summary>
        /// <param name="Key">连接标识</param>
        /// <param name="IotModel">连接信息</param>
        public static void AddDicClientIot(string Key, DicIotModel IotModel)
        {
            if (dicClientIot.ContainsKey(Key))
            {
                dicClientIot[Key] = IotModel;
            }
            else
            {
                dicClientIot.Add(Key, IotModel);
            }
        }
        /// <summary>
        /// 根据key获取客户端对象
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static DicIotModel GetDicClientIot(string Key)
        {
            if (dicClientIot.ContainsKey(Key))
                return dicClientIot[Key];
            return null;
        }
        /// <summary>
        /// 根据key获取客户端对象
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static Dictionary<string, DicIotModel> GetDicClientIot()
        {
            return dicClientIot;
        }
        /// <summary>
        /// 是否包含指定Key数据
        /// </summary>
        /// <param name="Key">连接标识</param>
        /// <returns></returns>
        public static bool IsDicClientIot(string Key)
        {
            return dicClientIot.ContainsKey(Key);
        }
        /// <summary>
        /// 删除指定Key数据
        /// </summary>
        /// <param name="Key">连接标识</param>
        /// <returns></returns>
        public static bool RemoveDicClientIot(string Key)
        {
            if (dicClientIot.ContainsKey(Key))
                return dicClientIot.Remove(Key);
            return true;
        }
        /// <summary>
        /// 清空连接对象
        /// </summary>
        /// <returns></returns>
        public static void ClearDicClientIot()
        {
            dicClientIot.Clear();
        }
        #endregion
        #region 队列操作
        #region UDP
        /// <summary>
        /// 移除并返回在 UDP Queue 的开头的对象。
        /// </summary>
        public static QueueUdpModel GetUdpQueue()
        {
            return udpQueue.Dequeue();
        }
        /// <summary>
        /// 返回在 UDP Queue 的开头的对象但是不删除。
        /// </summary>
        public static QueueUdpModel GetUdpQueuePeek()
        {
            return udpQueue.Peek();
        }
        /// <summary>
        /// 向UDP Queue 的末尾添加一个对象。
        /// </summary>
        /// <param name="model"></param>
        public static void AddUdpQueue(QueueUdpModel model)
        {
            udpQueue.Enqueue(model);
        }
        /// <summary>
        /// 判断某个元素是否在UDP Queue 中。
        /// </summary>
        /// <param name="model"></param>
        public static bool IsUdpQueue(QueueUdpModel model)
        {
            return udpQueue.Contains(model);
        }
        /// <summary>
        /// 清除UDP Queue数据
        /// </summary>
        public static void ClearUdpQueue()
        {
            udpQueue.Clear();
        }
        /// <summary>
        /// 获取UDP总元素数量
        /// </summary>
        public static int GetUdpQueueCount()
        {
            return udpQueue.Count;
        }
        #endregion
        #region TCP
        /// <summary>
        /// 移除并返回在 Tcp Queue 的开头的对象。
        /// </summary>
        public static QueueTcpModel GetTcpQueue()
        {
            return tcpQueue.Dequeue();
        }
        /// <summary>
        /// 返回在 Tcp Queue 的开头的对象但是不删除。
        /// </summary>
        public static QueueTcpModel GetTcpQueuePeek()
        {
            return tcpQueue.Peek();
        }
        /// <summary>
        /// 向Tcp Queue 的末尾添加一个对象。
        /// </summary>
        /// <param name="model"></param>
        public static void AddTcpQueue(QueueTcpModel model)
        {
            tcpQueue.Enqueue(model);
        }
        /// <summary>
        /// 判断某个元素是否在Tcp Queue 中。
        /// </summary>
        /// <param name="model"></param>
        public static bool IsTcpQueue(QueueTcpModel model)
        {
            return tcpQueue.Contains(model);
        }
        /// <summary>
        /// 清除Tcp Queue数据
        /// </summary>
        public static void ClearTcpQueue()
        {
            tcpQueue.Clear();
        }
        /// <summary>
        /// 获取Tcp总元素数量
        /// </summary>
        public static int GetTcpQueueCount()
        {
            return tcpQueue.Count;
        }
        #endregion
        #endregion
        #region 设备数据更新
        /// <summary>
        /// 添加或者更新设备数据
        /// </summary>
        /// <param name="model">设备对象</param>
        public static void SetControllers(ControllersModel model,bool isSetIP=false)
        {
            if (isSetIP) 
            {
                if (IotList.Exists(o => o.address == model.address))
                {
                    IotList.Find(o => o.address == model.address).ConnectionId = model.ConnectionId;
                }
            }
            else
            {
                if (IotList.Exists(o => o.address == model.address))
                {
                    IotList.RemoveAll(o => o.address == model.address);
                    IotList.Add(model);
                }
                else
                {
                    model.updatetime = DateTime.Now.AddMinutes(-StaticData.ReadInterval);
                    IotList.Add(model);
                }
            }
        }
        /// <summary>
        /// 查询设备对象list
        /// </summary>
        /// <returns></returns>
        public static List<ControllersModel> GetAllControllers()
        {
            if (IotList == null || IotList.Count == 0)
                return new List<ControllersModel>();
            return IotList;
        }
        /// <summary>
        /// 设备对象是否存在数据
        /// </summary>
        /// <returns></returns>
        public static bool IsControllers()
        {
            if (IotList == null || IotList.Count == 0)
                return false;
            return true;
        }
        /// <summary>
        /// 根据设备地址查询设备对象
        /// </summary>
        /// <param name="key">设备地址</param>
        /// <returns></returns>
        public static ControllersModel GetControllers(string key)
        {
            if (IotList == null || IotList.Count == 0)
                return null;
            if (string.IsNullOrEmpty(key))
                return null;
            if (IotList.Exists(o => o.address == key.Trim()))
                return IotList.Find(o => o.address == key.Trim());
            return null;
        }
        #endregion
        #region CRC
        public static byte XOR(byte[] SRC)
        {
            byte a = SRC[0];
            for (int i = 1; i < SRC.Length; i++)
            {
                a = byte.Parse(Convert.ToString(a ^ SRC[i]));
            }
            return a;

        }
        public static byte XOR(byte[] SRC, int Length)
        {
            byte a = SRC[0];
            for (int i = 1; i < Length; i++)
            {
                a = byte.Parse(Convert.ToString(a ^ SRC[i]));
            }
            return a;

        }
        /// <summary>
        /// CRC32校验
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int CRC32(byte[] data)
        {
            int count = data.Length;
            byte[] buf = new byte[data.Length + 2];
            data.CopyTo(buf, 0);
            int ptr = 0;
            int i = 0;
            int crc = 0;
            byte crc1, crc2, crc3;
            crc1 = buf[ptr++];
            crc2 = buf[ptr++];
            buf[count] = 0;
            buf[count + 1] = 0;
            while (--count >= 0)
            {
                crc3 = buf[ptr++];
                for (i = 0; i < 8; i++)
                {
                    if (((crc1 & 0x80) >> 7) == 1)//判断crc1高位是否为1
                    {
                        crc1 = (byte)(crc1 << 1); //移出高位
                        if (((crc2 & 0x80) >> 7) == 1)//判断crc2高位是否为1
                        {
                            crc1 = (byte)(crc1 | 0x01);//crc1低位由0变1
                        }
                        crc2 = (byte)(crc2 << 1);//crc2移出高位
                        if (((crc3 & 0x80) >> 7) == 1) //判断crc3高位是否为1
                        {
                            crc2 = (byte)(crc2 | 0x01); //crc2低位由0变1
                        }
                        crc3 = (byte)(crc3 << 1);//crc3移出高位
                        crc1 = (byte)(crc1 ^ 0x10);
                        crc2 = (byte)(crc2 ^ 0x21);
                    }
                    else
                    {
                        crc1 = (byte)(crc1 << 1); //移出高位
                        if (((crc2 & 0x80) >> 7) == 1)//判断crc2高位是否为1
                        {
                            crc1 = (byte)(crc1 | 0x01);//crc1低位由0变1
                        }
                        crc2 = (byte)(crc2 << 1);//crc2移出高位
                        if (((crc3 & 0x80) >> 7) == 1) //判断crc3高位是否为1
                        {
                            crc2 = (byte)(crc2 | 0x01); //crc2低位由0变1
                        }
                        crc3 = (byte)(crc3 << 1);//crc3移出高位
                    }
                }
            }
            crc = (int)((crc1 << 8) + crc2);
            return crc;
        }

        /// <summary>
        /// CRC高位校验码checkCRCHigh
        /// </summary>
        static byte[] ArrayCRCHigh =
        {
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
        0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
        0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
        0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
        0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
        0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
        0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
        0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
        0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
        0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
        0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
        0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
        0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
        0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
        0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
        0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
        0x80, 0x41, 0x00, 0xC1, 0x81, 0x40
        };

        /// <summary>
        /// CRC地位校验码checkCRCLow
        /// </summary>
        static byte[] checkCRCLow =
        {
        0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06,
        0x07, 0xC7, 0x05, 0xC5, 0xC4, 0x04, 0xCC, 0x0C, 0x0D, 0xCD,
        0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
        0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A,
        0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC, 0x14, 0xD4,
        0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
        0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3,
        0xF2, 0x32, 0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4,
        0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
        0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29,
        0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF, 0x2D, 0xED,
        0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
        0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60,
        0x61, 0xA1, 0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67,
        0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
        0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68,
        0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA, 0xBE, 0x7E,
        0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
        0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71,
        0x70, 0xB0, 0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92,
        0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
        0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B,
        0x99, 0x59, 0x58, 0x98, 0x88, 0x48, 0x49, 0x89, 0x4B, 0x8B,
        0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
        0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42,
        0x43, 0x83, 0x41, 0x81, 0x80, 0x40
        };
        /// <summary>
        /// CRC校验
        /// </summary>
        /// <param name="data">校验的字节数组</param>
        /// <param name="length">校验的数组长度</param>
        /// <returns>该字节数组的奇偶校验字节</returns>
        public static UInt16 CRC16(byte[] data, int arrayLength)
        {
            byte CRCHigh = 0xFF;
            byte CRCLow = 0xFF;
            byte index;
            int i = 0;
            while (arrayLength-- > 0)
            {
                index = (System.Byte)(CRCHigh ^ data[i++]);
                CRCHigh = (System.Byte)(CRCLow ^ ArrayCRCHigh[index]);
                CRCLow = checkCRCLow[index];
            }

            return (UInt16)(CRCHigh << 8 | CRCLow);
        }
        public static byte[] CRC16_C(byte[] data)
        {
            byte CRC16Lo;
            byte CRC16Hi;   //CRC寄存器 
            byte CL; byte CH;       //多项式码&HA001 
            byte SaveHi; byte SaveLo;
            byte[] tmpData;
            int Flag;
            CRC16Lo = 0xFF;
            CRC16Hi = 0xFF;
            CL = 0x01;
            CH = 0xA0;
            tmpData = data;
            for (int i = 0; i < tmpData.Length; i++)
            {
                CRC16Lo = (byte)(CRC16Lo ^ tmpData[i]); //每一个数据与CRC寄存器进行异或 
                for (Flag = 0; Flag <= 7; Flag++)
                {
                    SaveHi = CRC16Hi;
                    SaveLo = CRC16Lo;
                    CRC16Hi = (byte)(CRC16Hi >> 1);      //高位右移一位 
                    CRC16Lo = (byte)(CRC16Lo >> 1);      //低位右移一位 
                    if ((SaveHi & 0x01) == 0x01) //如果高位字节最后一位为1 
                    {
                        CRC16Lo = (byte)(CRC16Lo | 0x80);   //则低位字节右移后前面补1 
                    }             //否则自动补0 
                    if ((SaveLo & 0x01) == 0x01) //如果LSB为1，则与多项式码进行异或 
                    {
                        CRC16Hi = (byte)(CRC16Hi ^ CH);
                        CRC16Lo = (byte)(CRC16Lo ^ CL);
                    }
                }
            }
            byte[] ReturnData = new byte[2];
            ReturnData[1] = CRC16Hi;       //CRC高位 
            ReturnData[0] = CRC16Lo;       //CRC低位 
            return ReturnData;
        }
        public static byte[] CopyByte(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            a.CopyTo(c, 0);
            b.CopyTo(c, a.Length);
            return c;
        }
        #endregion
        #region Byte
        /// <summary>
        /// 16进制的数字的字符串表示形式转换为等效的byte数组
        /// </summary>
        /// <param name="strValues">16进制的数字的字符串</param>
        /// <returns></returns>
        public static byte[] strToToHexByte(string strValues)
        {
            int len = strValues.Length / 2;
            int remainder = strValues.Length % 2;
            byte[] hexValues = new Byte[len + remainder];

            for (int i = 0; i < len; i++)
            {
                hexValues[i] = Convert.ToByte(strValues.Substring(i * 2, 2), 16);
            }

            if (remainder != 0)
            {
                hexValues[len + remainder - 1] = Convert.ToByte(strValues.Substring(strValues.Length - 1, 1), 16);
            }

            return hexValues;
        }
        /// <summary>
        /// 十六进制转换为十进制
        /// </summary>
        /// <param name="G16">十六进制字符串</param>
        /// <returns></returns>
        public static int Convert16t10(string G16)
        {
            return Convert.ToInt32(G16, 16);
        }
        /// <summary>
        /// 十进制转十六进制
        /// </summary>
        /// <param name="G10">十进制数字</param>
        /// <returns></returns>
        public static string Convert10t16(int G10)
        {
            return Convert.ToString(G10, 16);
        }
        //内容转换(带异或)
        public static byte[] DContent(string send_data)
        {
            try
            {
                int len = send_data.Length / 2;
                byte[] b_write = new byte[len + 1];
                byte[] check = new byte[len];
                for (int i = 0; i < len; i++)
                {
                    b_write[i] = byte.Parse(Convert.ToInt32(send_data.Substring(i * 2, 2), 16).ToString());
                    check[i] = b_write[i];
                }
                b_write[len] = XOR(check, check.Length);
                return b_write;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //内容转换(不带异或)
        public static byte[] DContentNoXOR(string send_data)
        {
            int len = send_data.Length / 2;
            byte[] b_write = new byte[len + 1];
            byte[] check = new byte[len];
            for (int i = 0; i < len; i++)
            {
                b_write[i] = byte.Parse(Convert.ToInt32(send_data.Substring(i * 2, 2), 16).ToString());
                check[i] = b_write[i];
            }
            b_write[len] = XOR(check, check.Length);
            return check;
        }
        //格式转换(带空格)
        public static string ByteToString1(byte[] Data)
        {
            try
            {
                string ReStr = " ";
                for (int i = 0; i < Data.Length; i++)
                {
                    ReStr += Convert.ToString(Data[i], 16).PadLeft(2, '0') + " ";
                }
                return ReStr;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        //格式转换(不带空格)
        public static string ByteToString2(byte[] Data)
        {
            try
            {
                string ReStr = "";
                for (int i = 0; i < Data.Length; i++)
                {
                    ReStr += Convert.ToString(Data[i], 16).PadLeft(2, '0') + "";
                }
                return ReStr;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
