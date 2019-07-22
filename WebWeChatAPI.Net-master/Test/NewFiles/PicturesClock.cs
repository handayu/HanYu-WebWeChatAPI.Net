using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leestar54.WeChat.WebAPI.NewFiles
{
    public class PicturesClock
    {
        public delegate void PicturesClockHandle();
        public event PicturesClockHandle PicturesClockEvent;


        private System.Threading.Timer m_timer = null;
        private int m_timeSpanNotify = 0;

        public PicturesClock()
        {
            string timeSpanNotify = ConfigurationManager.AppSettings["PicturesClock"];

            int timeSpanSec = 0;
            int.TryParse(timeSpanNotify, out timeSpanSec);
            m_timeSpanNotify = timeSpanSec;
        }

        public void Start()
        {
            m_timer = new System.Threading.Timer(Timer, null, 10000, m_timeSpanNotify);
        }

        public void Timer(object o)
        {
            if (PicturesClockEvent != null)
            {
                PicturesClockEvent();
            }
        }

        public void Stop()
        {
            m_timer.Dispose();
        }
    }
}
