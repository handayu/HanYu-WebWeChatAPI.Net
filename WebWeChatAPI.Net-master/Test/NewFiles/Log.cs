using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leestar54.WeChat.WebAPI.NewFiles
{
    public class Log
    {
        /// <summary>
        /// <add key="ImagePath" value="C:\Strategy\"/>
        /// </summary>
        private static string m_logPath = ConfigurationManager.AppSettings["LogPath"];

        public static void Write(string str)
        {
            try
            {
                string strInfo = DateTime.Now.ToString() + ":" + str;
                using (StreamWriter sw = new StreamWriter(m_logPath, true))
                {
                    sw.WriteLine(strInfo);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(DateTime.Now.ToString() + "Write()写入文本异常:" + ex.Message);
            }
        }
    }
}
