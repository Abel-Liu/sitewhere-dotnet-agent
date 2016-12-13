using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sitewhere_dotnet_agent.Lib
{
    public enum ProcessProtocolEnum
    {
        Json = 0,
        ProtoBuf = 1
    }

    /// <summary>
    /// Interface for classes that process commands for an agent.
    /// </summary>
    public interface IAgentCommandProcessor : IDisposable
    {
        ProcessProtocolEnum processProto { get; set; }

        /// <summary>
        /// Executes logic that happens before the standard processing loop.
        /// </summary>
        /// <param name="hardwareId"></param>
        /// <param name="specificationToken"></param>
        /// <param name="dispatcher"></param>
        void executeStartupLogic(String hardwareId, String specificationToken, ISiteWhereEventDispatcher dispatcher);

        /// <summary>
        /// Process a SiteWhere system command.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="dispatcher"></param>
        void processSiteWhereCommand(byte[] message, ISiteWhereEventDispatcher dispatcher);

        /// <summary>
        /// Process a specification command.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="dispatcher"></param>
        void processSpecificationCommand(byte[] message, ISiteWhereEventDispatcher dispatcher);

        /// <summary>
        /// Set based on hardware id configured in agent.
        /// </summary>
        /// <param name="hardwareId"></param>
        void setHardwareId(String hardwareId);

        /// <summary>
        /// Set based on specification token configured in agent.
        /// </summary>
        /// <param name="specificationToken"></param>
        void setSpecificationToken(String specificationToken);

        /// <summary>
        /// Set the event dispatcher that allows data to be sent back to SiteWhere.
        /// </summary>
        /// <param name="dispatcher"></param>
        void setEventDispatcher(ISiteWhereEventDispatcher dispatcher);
    }
}
