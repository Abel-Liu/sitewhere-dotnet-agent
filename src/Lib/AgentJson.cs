using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Configuration;
using System.Threading;
using Newtonsoft.Json;

namespace sitewhere_dotnet_agent.Lib
{
    public class JsonAgent : IDisposable
    {
        /** Static LOGGER instance */
        private static Logger LOGGER = Logger.getLogger(typeof(Agent).ToString());

        /** Default MQTT port */
        private const int DEFAULT_MQTT_PORT = 1883;

        /** Command processor Java classname */
        private String commandProcessorClassname;

        /** Hardware id */
        private String hardwareId;

        /** Specification token */
        private String specificationToken;

        /** MQTT server hostname */
        private String mqttHostname;

        /** MQTT server port */
        private int mqttPort;

        /** Outbound SiteWhere MQTT topic */
        private String outboundSiteWhereTopic;

        /** Inbound SiteWhere MQTT topic */
        private String inboundSiteWhereTopic;

        /** Inbound specification command MQTT topic */
        private String inboundCommandTopic;

        /** MQTT client */
        private MqttClient mqtt;

        /** Outbound message processing */
        private MQTTOutbound outbound;

        private IAgentCommandProcessor _processor;

        public void Dispose()
        {
            if (mqtt != null)
            {
                try
                {
                    mqtt.Disconnect();
                }
                catch (Exception e)
                {
                    LOGGER.log("Exception disconnecting from MQTT broker.", e);
                }
            }

            if (_processor != null)
            {
                _processor.Dispose();
            }
        }

        public void start()
        {
            start(null);
        }

        /// <summary>
        /// Start the agent.
        /// </summary>
        /// <param name="processor"></param>
        public void start(IAgentCommandProcessor processor)
        {
            LOGGER.info("SiteWhere agent starting...");

            this.mqtt = new MqttClient(getMqttHostname(), getMqttPort(), false, null, null, MqttSslProtocols.None);

            LOGGER.info("Connecting to MQTT broker at '" + getMqttHostname() + ":" + getMqttPort() + "'...");

            try
            {
                mqtt.Connect(hardwareId);
                mqtt.MqttMsgPublishReceived += Mqtt_MqttMsgPublishReceived;
            }
            catch (Exception e)
            {
                throw new SiteWhereAgentException("Unable to establish MQTT connection.", e);
            }

            LOGGER.info("Connected to MQTT broker.");

            // Create outbound message processor.
            outbound = new MQTTOutbound(mqtt, getOutboundSiteWhereTopic());

            // Create an instance of the command processor.
            if (processor == null)
            {
                processor = createProcessor();
            }

            _processor = processor;

            processor.setHardwareId(hardwareId);
            processor.setSpecificationToken(specificationToken);
            processor.setEventDispatcher(outbound);

            RunInbound();

            // Executes any custom startup logic.
            processor.executeStartupLogic(getHardwareId(), getSpecificationToken(), outbound);

            LOGGER.info("SiteWhere agent started.");
        }

        private void Mqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (e.Topic == getInboundSiteWhereTopic())
            {
                _processor.processSiteWhereCommand(e.Message, outbound);
            }
            else if (e.Topic == getInboundCommandTopic())
            {
                _processor.processSpecificationCommand(e.Message, outbound);
            }
        }

        protected void RunInbound()
        {
            string[] topics = { getInboundSiteWhereTopic(), getInboundCommandTopic() };
            byte[] levels = { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE };

            mqtt.Subscribe(topics, levels);
        }

        /// <summary>
        /// Create an instance of the command processor.
        /// </summary>
        /// <returns></returns>
        protected IAgentCommandProcessor createProcessor()
        {
            try
            {
                var classname = getCommandProcessorClassname();

                IAgentCommandProcessor processor = (IAgentCommandProcessor)System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(classname);
                processor.processProto = ProcessProtocolEnum.Json;

                return processor;
            }
            catch (Exception e)
            {
                throw new SiteWhereAgentException(e);
            }
        }

        /// <summary>
        /// Load configuration
        /// </summary>
        /// <returns></returns>
        public bool load()
        {
            // Load command processor class name.
            setCommandProcessorClassname(ConfigurationManager.AppSettings[AgentConfiguration.COMMAND_PROCESSOR_CLASSNAME]);
            if (getCommandProcessorClassname() == null)
            {
                LOGGER.info("Command processor class name not specified.");
                return false;
            }

            // Validate hardware id.
            setHardwareId(ConfigurationManager.AppSettings[AgentConfiguration.DEVICE_HARDWARE_ID]);
            if (getHardwareId() == null)
            {
                LOGGER.info("Device hardware id not specified in configuration.");
                return false;
            }
            LOGGER.info("Using configured device hardware id: " + getHardwareId());

            // Validate specification token.
            setSpecificationToken(ConfigurationManager.AppSettings[AgentConfiguration.DEVICE_SPECIFICATION_TOKEN]);
            if (getSpecificationToken() == null)
            {
                LOGGER.info("Device specification token not specified in configuration.");
                return false;
            }
            LOGGER.info("Using configured device specification token: " + getSpecificationToken());

            // Validate MQTT hostname.
            setMqttHostname(ConfigurationManager.AppSettings[AgentConfiguration.MQTT_HOSTNAME]);
            if (getMqttHostname() == null)
            {
                LOGGER.info("MQTT hostname not specified.");
                return false;
            }

            // Validate MQTT port.
            String strPort = ConfigurationManager.AppSettings[AgentConfiguration.MQTT_PORT];
            if (strPort != null)
            {
                try
                {
                    setMqttPort(int.Parse(strPort));
                }
                catch (Exception e)
                {
                    LOGGER.warning("Non-numeric MQTT port specified, using: " + DEFAULT_MQTT_PORT);
                    setMqttPort(DEFAULT_MQTT_PORT);
                }
            }
            else
            {
                LOGGER.warning("No MQTT port specified, using: " + DEFAULT_MQTT_PORT);
                setMqttPort(DEFAULT_MQTT_PORT);
            }

            // Validate outbound SiteWhere topic.
            setOutboundSiteWhereTopic(ConfigurationManager.AppSettings[AgentConfiguration.MQTT_OUTBOUND_SITEWHERE_TOPIC]);
            if (getOutboundSiteWhereTopic() == null)
            {
                LOGGER.info("Outbound SiteWhere MQTT topic not specified.");
                return false;
            }

            // Validate inbound SiteWhere topic.
            setInboundSiteWhereTopic(ConfigurationManager.AppSettings[AgentConfiguration.MQTT_INBOUND_SITEWHERE_TOPIC]);
            if (getInboundSiteWhereTopic() == null)
            {
                String inn = calculateInboundSiteWhereTopic();
                LOGGER.warning("Using default inbound SiteWhere MQTT topic: " + inn);
                setInboundSiteWhereTopic(inn);
            }

            // Validate inbound command topic.
            setInboundCommandTopic(ConfigurationManager.AppSettings[AgentConfiguration.MQTT_INBOUND_COMMAND_TOPIC]);
            if (getInboundCommandTopic() == null)
            {
                String inn = calculateInboundCommandTopic();
                LOGGER.warning("Using default inbound command MQTT topic: " + inn);
                setInboundCommandTopic(inn);
            }
            return true;
        }

        protected String calculateInboundSiteWhereTopic()
        {
            return "SiteWhere/system/" + getHardwareId();
        }

        protected String calculateInboundCommandTopic()
        {
            return "SiteWhere/commands/" + getHardwareId();
        }

        public String getCommandProcessorClassname()
        {
            return commandProcessorClassname;
        }

        public void setCommandProcessorClassname(String commandProcessorClassname)
        {
            this.commandProcessorClassname = commandProcessorClassname;
        }

        public String getHardwareId()
        {
            return hardwareId;
        }

        public void setHardwareId(String hardwareId)
        {
            this.hardwareId = hardwareId;
        }

        public String getSpecificationToken()
        {
            return specificationToken;
        }

        public void setSpecificationToken(String specificationToken)
        {
            this.specificationToken = specificationToken;
        }

        public String getMqttHostname()
        {
            return mqttHostname;
        }

        public void setMqttHostname(String mqttHostname)
        {
            this.mqttHostname = mqttHostname;
        }

        public int getMqttPort()
        {
            return mqttPort;
        }

        public void setMqttPort(int mqttPort)
        {
            this.mqttPort = mqttPort;
        }

        public String getOutboundSiteWhereTopic()
        {
            return outboundSiteWhereTopic;
        }

        public void setOutboundSiteWhereTopic(String outboundSiteWhereTopic)
        {
            this.outboundSiteWhereTopic = outboundSiteWhereTopic;
        }

        public String getInboundSiteWhereTopic()
        {
            return inboundSiteWhereTopic;
        }

        public void setInboundSiteWhereTopic(String inboundSiteWhereTopic)
        {
            this.inboundSiteWhereTopic = inboundSiteWhereTopic;
        }

        public String getInboundCommandTopic()
        {
            return inboundCommandTopic;
        }

        public void setInboundCommandTopic(String inboundCommandTopic)
        {
            this.inboundCommandTopic = inboundCommandTopic;
        }

    }
}
