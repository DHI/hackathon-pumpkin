using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace DHI.Services.ARRWebPortal
{
    public static class Logging
    {
        public static void Log(string message)
        {
            _LogToFile(message);
        }
        
        public static void LogException(Exception exception)
        {
            _LogToFile("EXCEPTION " + DateTime.Now + " " + " Exception:" + exception.Message + (exception.InnerException != null ? " InnerException" + exception.InnerException.Message : string.Empty));
        }

        private static void _LogToFile(string message)
        {
            string path = HttpContext.Current.Server.MapPath(@"~\App_Data\Log.txt");;
            FileInfo fileInfo = new FileInfo(path);

            if (!Directory.Exists(fileInfo.Directory.FullName))
            {
                Directory.CreateDirectory(fileInfo.Directory.FullName);
            }

            File.AppendAllLines(path, new List<string> { DateTime.Now + " " + message });
        }
    }
}
