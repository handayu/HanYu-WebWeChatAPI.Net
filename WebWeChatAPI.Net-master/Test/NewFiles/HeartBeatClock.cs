using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leestar54.WeChat.WebAPI.NewFiles
{
    public class HeartBeatClock
    {
        public delegate void HeartBeatClockHandle();
        public event HeartBeatClockHandle HeartBeatClockEvent;


        private System.Threading.Timer m_timer = null;
        private int m_timeSpanNotify = 0;

        public HeartBeatClock()
        {
            string timeSpanNotify = ConfigurationManager.AppSettings["HeartBeatClock"];

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
            if (HeartBeatClockEvent != null)
            {
                HeartBeatClockEvent();
            }
        }

        public void Stop()
        {
            m_timer.Dispose();
        }
    }
}
