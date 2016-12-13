using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Configuration;
using System.Threading;

namespace sitewhere_dotnet_agent.Lib
{
    /// <summary>
    /// Not complete because of protobuf
    /// </summary>
    public class Agent : IDisposable
    {
        /** Static LOGGER instance */
        private static Logger LOGGER = Logger.getLogger(typeof(Agent).ToString());

        /** Default outbound SiteWhere MQTT topic */
        private const String DEFAULT_MQTT_OUTBOUND_SITEWHERE = "SiteWhere/input/protobuf";

        /** Default MQTT hostname */
        private const String DEFAULT_MQTT_HOSTNAME = "localhost";

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

        /** Inbound message processing */
        private MQTTInbound inbound;
        private Thread inboundThread;

        private IAgentCommandProcessor _processor;

        public void Dispose()
        {
            if (mqtt != null)
            {
                try
                {
                    mqtt.Disconnect();
                    if (inboundThread != null)
                    {
                        inboundThread.Abort();
                    }

                    if (_processor != null)
                    {
                        _processor.Dispose();
                    }
                }
                catch (Exception e)
                {
                    LOGGER.log("Exception disconnecting from MQTT broker.", e);
                }
            }
        }

        /**
         * Start the agent using the command processor specified by classname.
         * 
         * @throws SiteWhereAgentException
         */
        public void start()
        {
            start(null);
        }

        /**
         * Start the agent.
         */
        public void start(IAgentCommandProcessor processor)
        {
            LOGGER.info("SiteWhere agent starting...");

            this.mqtt = new MqttClient(getMqttHostname(), getMqttPort(), false, null, null, MqttSslProtocols.None);
            mqtt.MqttMsgPublishReceived += Mqtt_MqttMsgPublishReceived;

            LOGGER.info("Connecting to MQTT broker at '" + getMqttHostname() + ":" + getMqttPort() + "'...");

            try
            {
                mqtt.Connect(hardwareId);
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

            // Create inbound message processing thread.
            //inboundThread = new Thread(new ParameterizedThreadStart(RunInbound));
            //inboundThread.Start(processor);

            // Executes any custom startup logic.
            processor.executeStartupLogic(getHardwareId(), getSpecificationToken(), outbound);

            LOGGER.info("SiteWhere agent started.");
        }

        private void Mqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            if (e != null)
            {

            }
        }

        protected void RunInbound(object processor)
        {
            inbound = new MQTTInbound(mqtt, getInboundSiteWhereTopic(), getInboundCommandTopic(), (IAgentCommandProcessor)processor, outbound);
            inbound.run();
        }

        /**
         * Create an instance of the command processor. FOs * @return
         * 
         * @throws SiteWhereAgentException
         */
        protected IAgentCommandProcessor createProcessor()
        {
            try
            {
                var classname = getCommandProcessorClassname();

                IAgentCommandProcessor processor = (IAgentCommandProcessor)System.Reflection.Assembly.GetExecutingAssembly().CreateInstance(classname);
                processor.processProto = ProcessProtocolEnum.ProtoBuf;

                return processor;
            }
            catch (Exception e)
            {
                throw new SiteWhereAgentException(e);
            }
        }

        /**
         * Internal class for sending MQTT outbound messages.
         * 
         * @author Derek
         */
        public class MQTTOutbound : ISiteWhereEventDispatcher
        {

            /** MQTT outbound topic */
            private String topic;
            private MqttClient connection;

            public MQTTOutbound(MqttClient connection, String topic)
            {
                this.connection = connection;
                this.topic = topic;
                connection.MqttMsgPublishReceived += Connection_MqttMsgPublishReceived;
            }

            private void Connection_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
            {
                if (e != null)
                {

                }
            }

            /*
             * (non-Javadoc)
             * 
             * @see
             * com.sitewhere.agent.ISiteWhereEventDispatcher#registerDevice(com.sitewhere.
             * device.communication.protobuf.proto.Sitewhere.SiteWhere.RegisterDevice,
             * java.lang.String)
             */
            public void registerDevice(Lib.SiteWhere.SiteWhere.Types.RegisterDevice register, String originator)
            {
                sendMessage(Lib.SiteWhere.SiteWhere.Types.Command.SendRegistration, register, originator, "registration");
            }

            /*
             * (non-Javadoc)
             * 
             * @see
             * com.sitewhere.agent.ISiteWhereEventDispatcher#acknowledge(com.sitewhere.device
             * .communication.protobuf.proto.Sitewhere.SiteWhere.Acknowledge,
             * java.lang.String)
             */
            public void acknowledge(Lib.SiteWhere.SiteWhere.Types.Acknowledge ack, String originator)
            {
                sendMessage(Lib.SiteWhere.SiteWhere.Types.Command.SendAcknowledgement, ack, originator, "ack");
            }

            /*
             * (non-Javadoc)
             * 
             * @see
             * com.sitewhere.agent.ISiteWhereEventDispatcher#sendMeasurement(com.sitewhere
             * .device.communication.protobuf.proto.Sitewhere.Model.DeviceMeasurements,
             * java.lang.String)
             */
            public void sendMeasurement(Lib.SiteWhere.Model.Types.DeviceMeasurements measurement, String originator)
            {
                sendMessage(Lib.SiteWhere.SiteWhere.Types.Command.SendDeviceMeasurements, measurement, originator, "measurement");
            }

            /*
             * (non-Javadoc)
             * 
             * @see
             * com.sitewhere.agent.ISiteWhereEventDispatcher#sendLocation(com.sitewhere.device
             * .communication.protobuf.proto.Sitewhere.Model.DeviceLocation, java.lang.String)
             */
            public void sendLocation(Lib.SiteWhere.Model.Types.DeviceLocation location, String originator)
            {
                sendMessage(Lib.SiteWhere.SiteWhere.Types.Command.SendDeviceLocation, location, originator, "location");
            }

            /*
             * (non-Javadoc)
             * 
             * @see
             * com.sitewhere.agent.ISiteWhereEventDispatcher#sendAlert(com.sitewhere.device
             * .communication.protobuf.proto.Sitewhere.Model.DeviceAlert, java.lang.String)
             */
            public void sendAlert(Lib.SiteWhere.Model.Types.DeviceAlert alert, String originator)
            {
                sendMessage(Lib.SiteWhere.SiteWhere.Types.Command.SendDeviceAlert, alert, originator, "alert");
            }

            /**
             * Common logic for sending messages via protocol buffers.
             * 
             * @param command
             * @param message
             * @param originator
             * @param label
             * @throws SiteWhereAgentException
             */
            protected void sendMessage(Lib.SiteWhere.SiteWhere.Types.Command command, Google.Protobuf.IMessage message, String originator, String label)
            {
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                Google.Protobuf.CodedOutputStream output = new Google.Protobuf.CodedOutputStream(stream);
                try
                {
                    Lib.SiteWhere.SiteWhere.Types.Header h = new SiteWhere.SiteWhere.Types.Header();
                    h.Command = command;

                    if (originator != null)
                    {
                        h.Originator = originator;
                    }
                    
                    h.WriteTo(output);
                    message.WriteTo(output);

                    output.Flush();

                    //string s = "2,8,1,50,10,10,121,117,110,103,111,97,108,50,50,50,18,36,55,100,102,100,54,100,54,51,45,53,101,56,100,45,52,51,56,48,45,98,101,48,52,45,102,99,53,99,55,51,56,48,49,100,102,98";
                    //List<byte> bytes = new List<byte>();
                    //foreach(var b in s.Split(','))
                    //{
                    //    byte br;
                    //    if (int.Parse(b) < 0)
                    //        br = (byte)(0xff & int.Parse(b));
                    //    else
                    //        br = byte.Parse(b);
                    //    bytes.Add(br);
                    //}

                    //connection.Publish(getTopic(), bytes.ToArray(), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    connection.Publish(getTopic(), stream.ToArray(), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

                    output.Dispose();
                }
                catch (Exception e)
                {
                    LOGGER.info(e.Message);
                }
            }

            public String getTopic()
            {
                return topic;
            }

            public void setTopic(String topic)
            {
                this.topic = topic;
            }
        }

        /**
         * Handles inbound commands. Monitors two topics for messages. One contains SiteWhere
         * system messages and the other contains messages defined in the device
         * specification.
         * 
         * @author Derek
         */
        public class MQTTInbound
        {

            /** MQTT connection */
            private uPLibrary.Networking.M2Mqtt.MqttClient connection;

            /** SiteWhere inbound MQTT topic */
            private String sitewhereTopic;

            /** Command inbound MQTT topic */
            private String commandTopic;

            /** Command processor */
            private IAgentCommandProcessor processor;

            /** Event dispatcher */
            private ISiteWhereEventDispatcher dispatcher;

            public MQTTInbound(MqttClient connection, String sitewhereTopic, String commandTopic, IAgentCommandProcessor processor, ISiteWhereEventDispatcher dispatcher)
            {
                this.connection = connection;
                this.sitewhereTopic = sitewhereTopic;
                this.commandTopic = commandTopic;
                this.processor = processor;
                this.dispatcher = dispatcher;
            }

            public void run()
            {
                // Subscribe to chosen topic.
                string[] topics = { getSitewhereTopic(), getCommandTopic() };
                byte[] levels = { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE };
                try
                {
                    //connection.Subscribe(topics, levels);
                    //// LOGGER.info("Started MQTT inbound processing thread.");
                    //while (true)
                    //{
                    //    try
                    //    {
                    //        //connection.MqttMsgSubscribed += Connection_MqttMsgSubscribed;
                    //        Message message = connection();
                    //        message.ack();
                    //        if (getSitewhereTopic().equals(message.getTopic()))
                    //        {
                    //            getProcessor().processSiteWhereCommand(message.getPayload(), getDispatcher());
                    //        }
                    //        else if (getCommandTopic().equals(message.getTopic()))
                    //        {
                    //            getProcessor().processSpecificationCommand(message.getPayload(), getDispatcher());
                    //        }
                    //        else
                    //        {
                    //            LOGGER.warning("Message for unknown topic received: " + message.getTopic());
                    //        }
                    //    }
                    //catch (Exception e)
                    //{
                    //    LOGGER.warning("Device event processor interrupted.");
                    //    return;
                    //}

                    //}
                }
                catch (Exception e)
                {
                    LOGGER.log("Exception while attempting to subscribe to inbound topics.", e);
                }
            }

            public String getSitewhereTopic()
            {
                return sitewhereTopic;
            }

            public void setSitewhereTopic(String sitewhereTopic)
            {
                this.sitewhereTopic = sitewhereTopic;
            }

            public String getCommandTopic()
            {
                return commandTopic;
            }

            public void setCommandTopic(String commandTopic)
            {
                this.commandTopic = commandTopic;
            }

            public IAgentCommandProcessor getProcessor()
            {
                return processor;
            }

            public void setProcessor(IAgentCommandProcessor processor)
            {
                this.processor = processor;
            }

            public ISiteWhereEventDispatcher getDispatcher()
            {
                return dispatcher;
            }

            public void setDispatcher(ISiteWhereEventDispatcher dispatcher)
            {
                this.dispatcher = dispatcher;
            }
        }

        /**
         * Validates the agent configuration.
         * 
         * @return
         */
        public bool load()
        {
            // LOGGER.info("Validating configuration...");

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
                LOGGER.warning("Using default MQTT hostname: " + DEFAULT_MQTT_HOSTNAME);
                setMqttHostname(DEFAULT_MQTT_HOSTNAME);
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
                LOGGER.warning("Using default outbound SiteWhere MQTT topic: " + DEFAULT_MQTT_OUTBOUND_SITEWHERE);
                setOutboundSiteWhereTopic(DEFAULT_MQTT_OUTBOUND_SITEWHERE);
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
