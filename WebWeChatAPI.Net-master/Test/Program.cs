using Leestar54.WeChat.WebAPI;
using Leestar54.WeChat.WebAPI.Modal;
using Leestar54.WeChat.WebAPI.Modal.Response;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Leestar54.WeChat.WebAPI.NewFiles;
namespace Test
{
    class Program
    {
        private static bool m_isLogin = false;

        /// <summary>
        /// 主交互Client-Wechat Require/Response
        /// </summary>
        private static Client client;

        /// <summary>
        /// 群名-群用户集合
        /// </summary>
        private static Dictionary<string, Contact> contactDict = new Dictionary<string, Contact>();

        /// <summary>
        /// 二维码Form
        /// </summary>
        private static QrCodeForm qrForm;

        /// <summary>
        /// 登陆Coockie
        /// </summary>
        private static string cookiePath = AppDomain.CurrentDomain.BaseDirectory + "autoLoginCookie";

        /// <summary>
        /// 心跳和图片Clock
        /// </summary>
        private static HeartBeatClock m_heartClock = new HeartBeatClock();
        private static PicturesClock m_picClock = new PicturesClock();

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                //全局异常设定
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                //生成对象
                client = new Client();
                qrForm = new QrCodeForm();

                string cookie = null;
                //获取登陆之后记录的cookie，实现推送手机端登陆，取代扫码
                //若不需要，注释掉以下代码即可
                if (File.Exists(cookiePath))
                {
                    StreamReader sr = new StreamReader(cookiePath, Encoding.Default);
                    cookie = sr.ReadLine();
                    sr.Close();
                }

                client.ExceptionCatched += Client_ExceptionCatched; ;
                client.GetLoginQrCodeComplete += Client_GetLoginQrCodeComplete; ;
                client.CheckScanComplete += Client_CheckScanComplete; ;
                client.LoginComplete += Client_LoginComplete; ;
                client.BatchGetContactComplete += Client_BatchGetContactComplete; ;
                client.GetContactComplete += Client_GetContactComplete; ;
                client.MPSubscribeMsgListComplete += Client_MPSubscribeMsgListComplete; ;
                client.LogoutComplete += Client_LogoutComplete; ;
                client.ReceiveMsg += Client_ReceiveMsg; ;
                client.DelContactListComplete += Client_DelContactListComplete; ;
                client.ModContactListComplete += Client_ModContactListComplete;

                Console.WriteLine("扫描启动...");
                Log.Write("扫描启动...");

                client.Start(cookie);

                //qrForm.ShowDialog();

                //启动Pic和Heart的时钟
                m_heartClock.HeartBeatClockEvent += M_heartClock_HeartBeatClockEvent;
                m_picClock.PicturesClockEvent += M_picClock_PicturesClockEvent;
                m_heartClock.Start();
                m_picClock.Start();

                //
                while (true)
                {
                    var keyinfo = Console.ReadKey();
                    switch (keyinfo.Key)
                    {
                        case ConsoleKey.D1://1
                            client.SendMsgAsync("测试助手发送！", "filehelper");//测试成功，似乎文件助手这里username不是中文,而是所谓的这种专用ID-Name
                            break;
                        case ConsoleKey.D2://2
                            OpenFileDialog openImgFileDialog = new OpenFileDialog();
                            openImgFileDialog.Filter = "图片|*.jpg;*.png;*.gif";
                            if (openImgFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var file = new FileInfo(openImgFileDialog.FileName);
                                client.SendMsgAsync(file, "filehelper");
                            }
                            break;
                        case ConsoleKey.Escape:
                            client.Close();
                            client.Logout();
                            break;
                    }
                }

                //获取群成员详情，需要我们主动调用，一般用不到，因为群里已经包含Member基本信息。
                //Contact chatRoom = contactDict["群UserName"];
                //string listStr = string.Join(",", chatRoom.MemberList);
                //client.GetBatchGetContactAsync(listStr, chatRoom.UserName);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private static void M_picClock_PicturesClockEvent()
        {
            if (!m_isLogin) return;

            Console.WriteLine(DateTime.Now.ToString() + ": Pic时钟...");
            Log.Write(DateTime.Now.ToString() + ": Pic时钟...");

            string imagePath = CaptureScreen.CaptureImage();
            var file = new FileInfo(imagePath);
            client.SendMsgAsync(file, "filehelper");
        }

        private static void M_heartClock_HeartBeatClockEvent()
        {
            if (!m_isLogin) return;

            Console.WriteLine(DateTime.Now.ToString() + ": 服务器心跳Heart时钟...");
            Log.Write(DateTime.Now.ToString() + ": 服务器心跳Heart时钟...");

            client.SendMsgAsync(DateTime.Now.ToString() + ": 服务器心跳Heart时钟...", "filehelper");//测试成功，似乎文件助手这里username不是中文,而是所谓的这种专用ID-Name

        }

        #region 全局异常捕获
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ToString());
            Log.Write(e.ToString());
        }
        #endregion

        #region 所有Client对象的事件
        private static void Client_ModContactListComplete(object sender, TEventArgs<List<Contact>> e)
        {
            Console.WriteLine("接收修改联系人信息");
            foreach (var item in e.Result)
            {
                contactDict[item.UserName] = item;
            }
        }

        private static void Client_DelContactListComplete(object sender, TEventArgs<List<DelContactItem>> e)
        {
            Console.WriteLine("接收删除联系人信息");
        }

        private static void Client_ReceiveMsg(object sender, TEventArgs<List<AddMsg>> e)
        {
            try
            {
                foreach (var item in e.Result)
                {
                    switch (item.MsgType)
                    {
                        case MsgType.MM_DATA_TEXT://文字消息
                            if (contactDict.Keys.Contains(item.FromUserName))
                            {
                                if (item.FromUserName.StartsWith("@@"))
                                {
                                    //群消息，内容格式为[群内username];<br/>[content]，例如Content=@ffda8da3471b87ff22a6a542c5581a6efd1b883698db082e529e8e877bef79b6:<br/>哈哈
                                    string[] content = item.Content.Split(new string[] { ":<br/>" }, StringSplitOptions.RemoveEmptyEntries);
                                    Console.WriteLine(contactDict[item.FromUserName].NickName + "：" + contactDict[item.FromUserName].MemberDict[content[0]].NickName + "：" + content[1]);
                                }
                                else
                                {
                                    Console.WriteLine(contactDict[item.FromUserName].NickName + "：" + item.Content);
                                }
                            }
                            else
                            {
                                //不包含（一般为群）则需要我们主动拉取信息
                                client.GetBatchGetContactAsync(item.FromUserName);
                            }

                            //自动回复
                            if (item.Content == "qq")
                            {
                                //client.SendMsg("测试回复", item.FromUserName);

                                //如果收到aaa,则自动给filehelper文件助手发送屏幕截图
                                string imagePath = CaptureScreen.CaptureImage();
                                var file = new FileInfo(imagePath);
                                client.SendMsgAsync(file, "filehelper");

                            }
                            break;
                        case MsgType.MM_DATA_HTML://HTML消息
                            break;
                        case MsgType.MM_DATA_IMG://图片消息
                            Console.WriteLine("收到图片消息");
                            break;
                        case MsgType.MM_DATA_PRIVATEMSG_TEXT:
                            break;
                        case MsgType.MM_DATA_PRIVATEMSG_HTML:
                            break;
                        case MsgType.MM_DATA_PRIVATEMSG_IMG:
                            break;
                        case MsgType.MM_DATA_VOICEMSG:
                            break;
                        case MsgType.MM_DATA_PUSHMAIL:
                            break;
                        case MsgType.MM_DATA_QMSG:
                            break;
                        case MsgType.MM_DATA_VERIFYMSG:
                            //自动加好友，日限额80个左右，请勿超限额多次调用，有封号风险
                            //client.VerifyUser(item.RecommendInfo);
                            break;
                        case MsgType.MM_DATA_PUSHSYSTEMMSG:
                            break;
                        case MsgType.MM_DATA_QQLIXIANMSG_IMG:
                            break;
                        case MsgType.MM_DATA_POSSIBLEFRIEND_MSG:
                            break;
                        case MsgType.MM_DATA_SHARECARD:
                            break;
                        case MsgType.MM_DATA_VIDEO:
                            break;
                        case MsgType.MM_DATA_VIDEO_IPHONE_EXPORT:
                            break;
                        case MsgType.MM_DATA_EMOJI:
                            break;
                        case MsgType.MM_DATA_LOCATION:
                            break;
                        case MsgType.MM_DATA_APPMSG:
                            break;
                        case MsgType.MM_DATA_VOIPMSG:
                            break;
                        case MsgType.MM_DATA_STATUSNOTIFY:
                            switch (item.StatusNotifyCode)
                            {
                                case StatusNotifyCode.StatusNotifyCode_READED:
                                    break;
                                case StatusNotifyCode.StatusNotifyCode_ENTER_SESSION:
                                    break;
                                case StatusNotifyCode.StatusNotifyCode_INITED:
                                    break;
                                case StatusNotifyCode.StatusNotifyCode_SYNC_CONV:
                                    //初始化的时候第一次sync会返回最近聊天的列表
                                    client.GetBatchGetContactAsync(item.StatusNotifyUserName);
                                    break;
                                case StatusNotifyCode.StatusNotifyCode_QUIT_SESSION:
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case MsgType.MM_DATA_VOIPNOTIFY:
                            break;
                        case MsgType.MM_DATA_VOIPINVITE:
                            break;
                        case MsgType.MM_DATA_MICROVIDEO:
                            break;
                        case MsgType.MM_DATA_SYSNOTICE:
                            break;
                        case MsgType.MM_DATA_SYS:
                            //系统消息提示，例如完成好友验证通过，建群等等，提示消息“以已经通过了***的朋友验证请求，现在可以开始聊天了”、“加入了群聊”
                            //不在字典，说明是新增，我们就主动拉取加入联系人字典
                            if (!contactDict.Keys.Contains(item.FromUserName))
                            {
                                client.GetBatchGetContactAsync(item.FromUserName);
                            }
                            break;
                        case MsgType.MM_DATA_RECALLED:
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("异常：" + err.Message);
            }
        }

        private static void Client_LogoutComplete(object sender, TEventArgs<User> e)
        {
            Console.WriteLine("已登出");
            Application.Exit();
            Log.Write("已登出");
            m_isLogin = false;
            
        }

        private static void Client_MPSubscribeMsgListComplete(object sender, TEventArgs<List<MPSubscribeMsg>> e)
        {
            Console.WriteLine("获取公众号文章，总数：" + e.Result.Count);
            Log.Write("获取公众号文章，总数：" + e.Result.Count);

        }

        private static void Client_GetContactComplete(object sender, TEventArgs<List<Contact>> e)
        {
            Console.WriteLine("获取联系人列表（包括公众号，联系人），总数：" + e.Result.Count);
            Log.Write("获取联系人列表（包括公众号，联系人），总数：" + e.Result.Count);

            foreach (var item in e.Result)
            {
                if (!contactDict.Keys.Contains(item.UserName))
                {
                    contactDict.Add(item.UserName, item);
                }

                //联系人列表中包含联系人，公众号，可以通过参数做区分
                if (item.VerifyFlag != 0)
                {
                    //个人号
                    Console.WriteLine("拉取到联系人(个人号)：" + item.UserName);
                    Log.Write("拉取到联系人(个人号)：" + item.UserName);

                }
                else
                {
                    //公众号
                    Console.WriteLine("拉取到联系人(公众号)：" + item.UserName);
                    Log.Write("拉取到联系人(个人号)：" + item.UserName);

                }
            }
            //如果获取完成
            if (client.IsFinishGetContactList)
            {
                Console.WriteLine("Client_GetContactComplete!");
                Log.Write("Client_GetContactComplete!");
            }
        }

        private static void Client_BatchGetContactComplete(object sender, TEventArgs<List<Contact>> e)
        {
            Console.WriteLine("拉取联系人信息，总数：" + e.Result.Count);
            Log.Write("拉取联系人信息，总数：" + e.Result.Count);
            foreach (var item in e.Result)
            {
                if (!contactDict.Keys.Contains(item.UserName))
                {
                    contactDict.Add(item.UserName, item);
                    Console.WriteLine("拉取到联系人：" + item.UserName);
                    Log.Write("拉取到联系人：" + item.UserName);
                }
            }

            Console.WriteLine("Client_BatchGetContactComplete!");
            Log.Write("Client_BatchGetContactComplete!");
        }

        private static void Client_LoginComplete(object sender, TEventArgs<User> e)
        {
            string cookie = client.GetLastCookie();
            Console.WriteLine("登陆成功：" + e.Result.NickName);
            Console.WriteLine("========已记录cookie，下次登陆将推送提醒至手机，取代扫码========");

            Log.Write("登陆成功：" + e.Result.NickName);
            Log.Write("========已记录cookie，下次登陆将推送提醒至手机，取代扫码========");

            m_isLogin = true;

            using (StreamWriter sw = new StreamWriter(cookiePath, false))
            {
                sw.WriteLine(cookie);
            }
            try
            {
                qrForm.Invoke(new Action(() =>
                {
                    qrForm.Close();
                }));
            }
            catch { }
        }

        private static void Client_CheckScanComplete(object sender, TEventArgs<System.Drawing.Image> e)
        {
            Console.WriteLine("用户已扫码");
            Log.Write("用户已扫码");

            qrForm.SetPic(e.Result);
        }

        private static void Client_GetLoginQrCodeComplete(object sender, TEventArgs<System.Drawing.Image> e)
        {
            Console.WriteLine("已获取登陆二维码");
            Log.Write("已获取登陆二维码");

            qrForm.SetPic(e.Result);
            qrForm.ShowDialog();
        }

        private static void Client_ExceptionCatched(object sender, TEventArgs<Exception> e)
        {
            if (e.Result is GetContactException)
            {
                Console.WriteLine("获取好友列表异常：" + e.Result.ToString());
                Log.Write("获取好友列表异常：" + e.Result.ToString());

                return;
            }

            if (e.Result is OperateFailException)
            {
                Console.WriteLine("异步操作异常：" + e.Result.ToString());
                Log.Write("异步操作异常：" + e.Result.ToString());

                return;
            }

            Console.WriteLine("异常：" + e.Result.ToString());
            Log.Write("异常：" + e.Result.ToString());
        }

        #endregion
    }
}
