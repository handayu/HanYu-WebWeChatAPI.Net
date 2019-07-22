using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Leestar54.WeChat.WebAPI.NewFiles
{
    public class CaptureScreen
    {

        /// <summary>
        /// <add key="ImagePath" value="C:\Strategy\"/>
        /// </summary>
        private static string m_imagePath = ConfigurationManager.AppSettings["ImagePath"];

        public static string CaptureImage()
        {
            string path = "";

            Thread th = new Thread(new ThreadStart(delegate ()
            {
                try
                {
                    //1-----------------------------------------
                    //屏幕宽
                    int iWidth = Screen.PrimaryScreen.Bounds.Width;
                    //屏幕高
                    int iHeight = Screen.PrimaryScreen.Bounds.Height;
                    //按照屏幕宽高创建位图
                    Image img = new Bitmap(iWidth, iHeight);
                    //从一个继承自Image类的对象中创建Graphics对象
                    Graphics gc = Graphics.FromImage(img);
                    //抓屏并拷贝到myimage里
                    gc.CopyFromScreen(new Point(0, 0), new Point(0, 0), new Size(iWidth, iHeight));

                    Clipboard.Clear();
                    Clipboard.SetDataObject(img);
                    //保存位图
                    string fileInfos = m_imagePath + Guid.NewGuid().ToString() + ".jpg";
                    img.Save(fileInfos);

                    path = fileInfos;
                }
                catch (Exception exc)
                {
                    MessageBox.Show("图片插入失败。" + exc.Message, "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }));
            th.TrySetApartmentState(ApartmentState.STA);
            th.Start();
            th.Join();

            return path;
        }
    }
}
