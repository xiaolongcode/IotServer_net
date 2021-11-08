using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace IotServer
{
    class Program
    {
        #region 设置控制台标题 禁用关闭按钮

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        extern static IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);
        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        extern static IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        static void DisbleClosebtn()
        {
            IntPtr windowHandle = FindWindow(null, "IotServer");
            IntPtr closeMenu = GetSystemMenu(windowHandle, IntPtr.Zero);
            uint SC_CLOSE = 0xF060;
            RemoveMenu(closeMenu, SC_CLOSE, 0x0);
        }
        static void CloseConsole(object sender, ConsoleCancelEventArgs e)
        {
            Environment.Exit(0);
        }
        #endregion

        #region 关闭控制台 快速编辑模式、插入模式
        const int STD_INPUT_HANDLE = -10;
        const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        const uint ENABLE_INSERT_MODE = 0x0020;
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int hConsoleHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint mode);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint mode);

        public static void DisbleQuickEditMode()
        {
            IntPtr hStdin = GetStdHandle(STD_INPUT_HANDLE);
            uint mode;
            GetConsoleMode(hStdin, out mode);
            mode &= ~ENABLE_QUICK_EDIT_MODE;//移除快速编辑模式
            mode &= ~ENABLE_INSERT_MODE;      //移除插入模式
            SetConsoleMode(hStdin, mode);
        }
        #endregion
        static void Main(string[] args)
        {
            //try
            //{
            //    //TCPService.TCPStart(AppSetting.GetValueByInt("tcpport"), AppSetting.GetValueByInt("tcptimeout"));
            //    //System.Threading.Thread.Sleep(100);

            //     UDPService.UDPStart(AppSetting.GetValueByInt("udpport"));
            //    System.Threading.Thread.Sleep(100);
            //    new StaticData(UDPService.UdpServer);

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("执行异常" + ex.Message);
            //    TxtLogHelper.ErroLog($"服务启动异常:{ex.Message}");
            //}
            //Console.ReadKey();

            //Console.Title = "智能控制系统 V1.2.1";
            StartService s = new StartService();
            if (Environment.UserInteractive)
            {
                try
                {
                    s.DebugStart(args);
                    s.DebugStop();
                }
#pragma warning disable CS0168 // 声明了变量“ex”，但从未使用过
                catch (System.Exception ex)
#pragma warning restore CS0168 // 声明了变量“ex”，但从未使用过
                {

                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new StartService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
    
}
