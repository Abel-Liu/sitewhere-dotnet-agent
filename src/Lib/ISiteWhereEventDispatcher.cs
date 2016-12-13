using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sitewhere_dotnet_agent.Lib
{
    /// <summary>
    /// Interface for events that can be dispatched to SiteWhere server.
    /// </summary>
    public interface ISiteWhereEventDispatcher
    {
        /// <summary>
        /// Register a device.
        /// </summary>
        /// <param name="register"></param>
        /// <param name="originator"></param>
        void registerDevice(Lib.SiteWhere.SiteWhere.Types.RegisterDevice register, String originator);

        /// <summary>
        /// Send an acknowledgement message.
        /// </summary>
        /// <param name="ack"></param>
        /// <param name="originator"></param>
        void acknowledge(Lib.SiteWhere.SiteWhere.Types.Acknowledge ack, String originator);

        /// <summary>
        /// Send a measurement event.
        /// </summary>
        /// <param name="measurement"></param>
        /// <param name="originator"></param>
        void sendMeasurement(Lib.SiteWhere.Model.Types.DeviceMeasurements measurement, String originator);

        /// <summary>
        /// Send a location event.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="originator"></param>
        void sendLocation(Lib.SiteWhere.Model.Types.DeviceLocation location, String originator);

        /// <summary>
        /// Send an alert event.
        /// </summary>
        /// <param name="alert"></param>
        /// <param name="originator"></param>
        void sendAlert(Lib.SiteWhere.Model.Types.DeviceAlert alert, String originator);
    }
}
