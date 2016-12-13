using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sitewhere_dotnet_agent.Lib
{
    public class Logger
    {
        string Name { get; set; }

        private Logger()
        {

        }

        public static Logger getLogger(string name)
        {
            return new Logger() { Name = name };
        }

        public void info(string msg)
        {
            Console.WriteLine(DateTime.Now.ToString() + "  " + msg);
        }

        public void warning(string msg)
        {
            info(msg);
        }

        public void log(string msg, Exception e)
        {
            info(msg + ", " + e.Message + e.StackTrace);
        }
    }
}
