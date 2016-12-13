using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sitewhere_dotnet_agent.Lib
{
    public interface IDeviceEventOriginator
    {
        string getEventId();
    }

    public class DeviceEventOriginator : IDeviceEventOriginator
    {
        private string origin;

        public DeviceEventOriginator(string originId)
        {
            origin = originId;
        }

        public string getEventId()
        {
            return origin;
        }
    }
}
