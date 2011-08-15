﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web;
using System.Web.Caching;

namespace GameMotor
{
    public class RoundControl
    {
        private DateTime lastExecutionDate = DateTime.Now.Subtract(new TimeSpan(0, 1, 0, 0, 0));

        private int MinutesPerRound { get; set; }
        private string ExecutionKey { get; set; }
        private string CacheKey { get; set; }
        private XmlLogger xmlLogger;
        private bool isRunning = false;

        public RoundControl(string executionKey, int roundSpan)
        {
            MinutesPerRound = roundSpan <= 0 ? 60 : roundSpan;
            this.ExecutionKey = executionKey;
            this.CacheKey = string.Concat("RoundStatus.", this.ExecutionKey);
            xmlLogger = new XmlLogger(this.ExecutionKey);
        }

        public RoundControl(int roundSpan) : this("default", roundSpan){}

        public RoundControl() : this("default", 0){ }

        public List<string> roundLog = null;        

        public delegate void RoundExecutionDelegate();
        public event RoundExecutionDelegate OnRoundExecution;

        public void StartRoundControl()
        {
            xmlLogger.Initialize();
            roundLog = new List<string>();
            DateTime now = DateTime.Now;
            try { 
                CheckRoundAction();
                isRunning = true;
            }
            catch (Exception error) { 
                xmlLogger.Write(now, DateTime.Now, error.Message); 
            }
        }

        public void StopRoundControl()
        {
            isRunning = false;
            HttpRuntime.Cache.Remove(this.CacheKey);
        }

        public void CheckRoundAction()
        {
            DateTime now = DateTime.Now;            
            var cache = HttpRuntime.Cache;

            if (now.Minute % MinutesPerRound == 0 && !CurrentRoundHasRun(now))
            {
                //TODO create logic to allow rollback
                //TODO block actions during roung calculation

                //Execute round
                if (OnRoundExecution != null)
                    OnRoundExecution();

                //Save now on lastExecutionDate
                lastExecutionDate = now;

                now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0);
                xmlLogger.Write(lastExecutionDate, DateTime.Now, "");
             
                //Sleep minutesPerRound minutes                
                cache.Insert(CacheKey, "1", null, now.AddMinutes(MinutesPerRound), TimeSpan.Zero, CacheItemPriority.Normal, RoundCallback);		
            }
            else
            {
                xmlLogger.Write(now, DateTime.Now, "Not executed");
                cache.Insert(CacheKey, "1", null, now.AddMinutes(now.Minute % MinutesPerRound).AddSeconds(-1 * now.Second), TimeSpan.Zero, CacheItemPriority.Normal, RoundCallback);
            }
        }

        public void RoundCallback(String k, Object v, CacheItemRemovedReason r)
        {
            if (isRunning)
                CheckRoundAction();
        }

        private bool CurrentRoundHasRun(DateTime now)
        {
            return (now.Year == lastExecutionDate.Year && now.Month == lastExecutionDate.Month && now.Day == lastExecutionDate.Day && now.Hour == lastExecutionDate.Hour && now.Minute == lastExecutionDate.Minute);
        }
    }
}
