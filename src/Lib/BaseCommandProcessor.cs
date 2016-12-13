using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sitewhere_dotnet_agent.Lib.SiteWhere;
using System.IO;

namespace sitewhere_dotnet_agent.Lib
{
    /// <summary>
    /// Base class for command processing. Handles processing of inbound SiteWhere system  messages.
    /// Processing of specification commands is left up to subclasses.
    /// </summary>
    public abstract class BaseCommandProcessor : IAgentCommandProcessor
    {
        /** Static logger instance */
        private static Logger LOGGER = Logger.getLogger(typeof(BaseCommandProcessor).ToString());

        /** Hardware id */
        private String hardwareId;

        /** Specification token */
        private String specificationToken;

        /** SiteWhere event dispatcher */
        private ISiteWhereEventDispatcher eventDispatcher;

        protected double latitudeSettingByServer = 0;
        protected double longitudeSettingByServer = 0;
        protected double elevationSettingByServer = 0;


        public ProcessProtocolEnum processProto
        {
            get; set;
        }

        public virtual void executeStartupLogic(String hardwareId, String specificationToken, ISiteWhereEventDispatcher dispatcher)
        {
        }

        /// <summary>
        /// process SiteWhere command (SiteWhere/system/HardwareId)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="dispatcher"></param>
        public void processSiteWhereCommand(byte[] message, ISiteWhereEventDispatcher dispatcher)
        {
            if (this.processProto == ProcessProtocolEnum.Json)
            {
                try
                {
                    dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(message));

                    switch ((string)obj.systemCommand.type)
                    {
                        case "RegistrationAck":

                            try
                            {
                                longitudeSettingByServer = obj.nestingContext.gateway.metadata.longitude;
                                latitudeSettingByServer = obj.nestingContext.gateway.metadata.latitude;
                                elevationSettingByServer = obj.nestingContext.gateway.metadata.elevation;
                            }
                            catch
                            {
                            }

                            handleRegistrationAckJson((string)obj.systemCommand.reason);
                            break;
                    }
                }
                catch (Exception e)
                {
                    LOGGER.warning("Can not process message content. " + e.Message);
                }
            }
            //TODO: protobuf doesn't work
            else if (processProto == ProcessProtocolEnum.ProtoBuf)
            {
                var stream = new MemoryStream(message);
                try
                {
                    var header = Lib.SiteWhere.Device.Types.Header.Parser.ParseDelimitedFrom(stream);
                    switch (header.Command)
                    {
                        case Device.Types.Command.AckRegistration:
                            {
                                var ack = SiteWhere.Device.Types.RegistrationAck.Parser.ParseDelimitedFrom(stream);
                                handleRegistrationAck(header, ack);
                                break;
                            }
                        case Device.Types.Command.AckDeviceStream:
                            {
                                // TODO: Add device stream support.
                                break;
                            }
                        case Device.Types.Command.ReceiveDeviceStreamData:
                            {
                                // TODO: Add device stream support.
                                break;
                            }
                    }
                }
                catch (IOException e)
                {
                    throw new SiteWhereAgentException(e);
                }
            }
            else
            {
                LOGGER.info("unknown process proto");
            }
        }

        /// <summary>
        /// process custom command (SiteWhere/commands/HardwareId)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="dispatcher"></param>
        public void processSpecificationCommand(byte[] message, ISiteWhereEventDispatcher dispatcher)
        {
            try
            {
                dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(message));
                string commandName = (string)obj.command.command.name;

                var method = this.GetType().GetMethod(commandName);
                if (method != null)
                {
                    var inputParas = (Newtonsoft.Json.Linq.JObject)obj.command.parameters;
                    Dictionary<string, object> paralist = new Dictionary<string, object>();

                    foreach (var p in inputParas)
                    {
                        paralist.Add(p.Key, ((Newtonsoft.Json.Linq.JValue)p.Value).Value);
                    }

                    List<object> sortedParas = new List<object>();

                    foreach (var p in method.GetParameters())
                    {
                        var find = paralist.Where(i => i.Key.ToUpper() == p.Name.ToUpper());
                        if (find.Any())
                            sortedParas.Add(find.First().Value);
                    }

                    var originid = (string)obj.command.invocation.id;
                    sortedParas.Add(new DeviceEventOriginator(originid));

                    method.Invoke(this, sortedParas.ToArray());
                }
            }
            catch (Exception e)
            {
                LOGGER.info(e.Message);
            }
        }

        public void setHardwareId(String hardwareId)
        {
            this.hardwareId = hardwareId;
        }

        public String getHardwareId()
        {
            return hardwareId;
        }

        public void setSpecificationToken(String specificationToken)
        {
            this.specificationToken = specificationToken;
        }

        public String getSpecificationToken()
        {
            return specificationToken;
        }

        public void setEventDispatcher(ISiteWhereEventDispatcher eventDispatcher)
        {
            this.eventDispatcher = eventDispatcher;
        }

        public ISiteWhereEventDispatcher getEventDispatcher()
        {
            return eventDispatcher;
        }

        public virtual void handleRegistrationAck(Lib.SiteWhere.Device.Types.Header header, Lib.SiteWhere.Device.Types.RegistrationAck ack)
        {
        }

        public virtual void handleRegistrationAckJson(string registrationAckState)
        {
        }

        /// <summary>
        /// Convenience method for sending device registration information to SiteWhere.
        /// </summary>
        /// <param name="hardwareId"></param>
        /// <param name="specificationToken"></param>
        public void sendRegistration(String hardwareId, String specificationToken)
        {
            Lib.SiteWhere.SiteWhere.Types.RegisterDevice register = new SiteWhere.SiteWhere.Types.RegisterDevice();
            register.HardwareId = hardwareId;
            register.SpecificationToken = specificationToken;
            register.SiteToken = System.Configuration.ConfigurationManager.AppSettings[AgentConfiguration.SITE_TOKEN];

            getEventDispatcher().registerDevice(register, null);
        }

        /// <summary>
        /// Convenience method for sending an acknowledgement event to SiteWhere.
        /// </summary>
        /// <param name="hardwareId"></param>
        /// <param name="message"></param>
        /// <param name="originator"></param>
        public void sendAck(String hardwareId, String message, IDeviceEventOriginator originator)
        {
            SiteWhere.SiteWhere.Types.Acknowledge ack = new SiteWhere.SiteWhere.Types.Acknowledge();
            ack.HardwareId = hardwareId;
            ack.Message = message;
            getEventDispatcher().acknowledge(ack, getOriginatorEventId(originator));
        }

        /// <summary>
        /// Convenience method for sending a measurement event to SiteWhere.
        /// </summary>
        /// <param name="hardwareId"></param>
        /// <param name="measurements"></param>
        /// <param name="originator"></param>
        public void sendMeasurement(String hardwareId, Model.Types.Measurement[] measurements, IDeviceEventOriginator originator)
        {
            Lib.SiteWhere.Model.Types.DeviceMeasurements mb = new Model.Types.DeviceMeasurements();

            mb.HardwareId = hardwareId;

            mb.Measurement.AddRange(measurements);

            getEventDispatcher().sendMeasurement(mb, getOriginatorEventId(originator));
        }

        /// <summary>
        /// Convenience method for sending a location event to SiteWhere.
        /// </summary>
        /// <param name="hardwareId"></param>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <param name="elevation"></param>
        /// <param name="originator"></param>
        public void sendLocation(String hardwareId, double latitude, double longitude, double elevation, IDeviceEventOriginator originator)
        {
            Lib.SiteWhere.Model.Types.DeviceLocation lb = new Model.Types.DeviceLocation();
            lb.HardwareId = hardwareId;
            lb.Longitude = longitude;
            lb.Elevation = elevation;
            lb.Latitude = latitude;

            getEventDispatcher().sendLocation(lb, getOriginatorEventId(originator));
        }

        /// <summary>
        /// Convenience method for sending an alert event to SiteWhere.
        /// </summary>
        /// <param name="hardwareId"></param>
        /// <param name="alertType"></param>
        /// <param name="message"></param>
        /// <param name="originator"></param>
        public void sendAlert(String hardwareId, String alertType, String message, IDeviceEventOriginator originator)
        {
            Lib.SiteWhere.Model.Types.DeviceAlert ab = new Model.Types.DeviceAlert();
            ab.HardwareId = hardwareId;
            ab.AlertType = alertType;
            ab.AlertMessage = message;

            getEventDispatcher().sendAlert(ab, getOriginatorEventId(originator));
        }

        /// <summary>
        /// Gets event id of the originating command if available.
        /// </summary>
        /// <param name="originator"></param>
        /// <returns></returns>
        protected String getOriginatorEventId(IDeviceEventOriginator originator)
        {
            if (originator == null)
            {
                return null;
            }

            return originator.getEventId();
        }

        public virtual void Dispose()
        {

        }
    }
}
