using System.Configuration;
using System.IO;

namespace IotServer
{
    public class AppSetting
    {
        /// <summary>
        /// 根据key获取配置文件
        /// </summary>
        /// <param name="Key">key</param>
        /// <returns></returns>
        public static string GetValue(string Key)
        {
            return  ConfigurationManager.AppSettings[Key].ToString();
        }
        /// <summary>
        /// 根据key获取配置文件
        /// </summary>
        /// <param name="Key">key</param>
        /// <returns></returns>
        public static int GetValueByInt(string Key)
        {
            return int.Parse(ConfigurationManager.AppSettings[Key].ToString());
        }
        /// <summary>
        /// 刷新配置文件
        /// </summary>
        public static void Refresh()
        {
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
