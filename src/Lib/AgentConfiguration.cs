using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sitewhere_dotnet_agent.Lib
{
    /// <summary>
    /// Constants for agent configuration properties.
    /// </summary>
    public class AgentConfiguration
    {
        /** Property for command processor classname */
        public const String COMMAND_PROCESSOR_CLASSNAME = "command.processor.classname";

        /** Property for device unique hardware id */
        public const String DEVICE_HARDWARE_ID = "device.hardware.id";

        /** Property for device specification token */
        public const String DEVICE_SPECIFICATION_TOKEN = "device.specification.token";

        /** Property for MQTT hostname */
        public const String MQTT_HOSTNAME = "mqtt.hostname";

        /** Property for MQTT port */
        public const String MQTT_PORT = "mqtt.port";

        /** Property for outbound SiteWhere MQTT topic */
        public const String MQTT_OUTBOUND_SITEWHERE_TOPIC = "mqtt.outbound.sitewhere.topic";

        /** Property for inbound SiteWhere MQTT topic */
        public const String MQTT_INBOUND_SITEWHERE_TOPIC = "mqtt.inbound.sitewhere.topic";

        /** Property for inbound command MQTT topic */
        public const String MQTT_INBOUND_COMMAND_TOPIC = "mqtt.inbound.command.topic";

        public const String SITE_TOKEN = "site.token";
    }
}
