using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace MyCR_StationSchedule.Common
{
    public class Logging
    {
        protected bool loggingActive = Convert.ToBoolean(ConfigurationManager.AppSettings["logging_active"]);
        protected string logFileName = ConfigurationManager.AppSettings["logging_file"];

        public void LogString(string message)
        {
            if (loggingActive)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFileName, true))
                {
                    file.WriteLine(message);
                }
            }
        }


    }

}