using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sitewhere_dotnet_agent.Lib;

namespace sitewhere_dotnet_agent
{
    class Program
    {
        private static JsonAgent agent = new JsonAgent();

        static void Main(string[] args)
        {
            agent.load();
            agent.start();

            Console.ReadLine();
            agent.Dispose();
        }
    }
}
