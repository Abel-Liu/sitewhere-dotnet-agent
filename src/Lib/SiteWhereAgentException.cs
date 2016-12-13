using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sitewhere_dotnet_agent.Lib
{
    public class SiteWhereAgentException : Exception
    {
        //private const long serialVersionUID = 3351303154000958250L;

        public SiteWhereAgentException()
        {
        }

        public SiteWhereAgentException(Exception e) : base(e.Message)
        {
        }

        public SiteWhereAgentException(String message) : base(message)
        {
        }

        public SiteWhereAgentException(String message, Exception error) : base(message, error)
        {
        }
    }
}
