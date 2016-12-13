using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace sitewhere_dotnet_agent.Lib
{
    /// <summary>
    /// Send MQTT outbound messages.
    /// </summary>
    public class MQTTOutbound : ISiteWhereEventDispatcher
    {

        /** MQTT outbound topic */
        private String topic;
        private MqttClient connection;

        public MQTTOutbound(MqttClient connection, String topic)
        {
            this.connection = connection;
            this.topic = topic;
        }

        public void registerDevice(Lib.SiteWhere.SiteWhere.Types.RegisterDevice register, String originator)
        {
            string json = @"{
                'hardwareId': '" + register.HardwareId + @"',
	            'type': 'RegisterDevice',
	            'request': {
                            'hardwareId': '" + register.HardwareId + @"',
		                    'specificationToken': '" + register.SpecificationToken + @"',
                            'siteToken':'" + register.SiteToken + @"'
                           }
                } ";

            sendMessage(json.Replace("'", "\""), "registration");
        }

        public void acknowledge(Lib.SiteWhere.SiteWhere.Types.Acknowledge ack, String originator)
        {
            var json = @"{
                    'hardwareId': '" + ack.HardwareId + @"',
                    'type': 'Acknowledge',
                    'request': {
                        'response': '" + ack.Message + @"',
                        'originatingEventId': '" + originator + @"'
                     }
                }";

            sendMessage(json.Replace("'", "\""), "ack");
        }

        public void sendMeasurement(Lib.SiteWhere.Model.Types.DeviceMeasurements measurement, String originator)
        {
            string values = "";

            foreach (var m in measurement.Measurement)
            {
                values += " '" + m.MeasurementId + "':'" + m.MeasurementValue + "', ";
            }

            var json = @"{
                    'hardwareId': '" + measurement.HardwareId + @"',
                    'type': 'DeviceMeasurements',
                    'request': {
                        'measurements': { " + values.Trim().TrimEnd(',') + @" },
                        'updateState': true
                     }
                }";

            sendMessage(json.Replace("'", "\""), "measurement");
        }

        public void sendLocation(Lib.SiteWhere.Model.Types.DeviceLocation location, String originator)
        {
            var json = @" {
                    'hardwareId': '" + location.HardwareId + @"',
                    'type':'DeviceLocation',
                    'request': {
                        'latitude': '" + location.Latitude.ToString() + @"',
                        'longitude': '" + location.Longitude.ToString() + @"',
                        'elevation': '" + location.Elevation.ToString() + @"',
                        'updateState': true
                    }
                }";

            sendMessage(json.Replace("'", "\""), "location");
        }

        public void sendAlert(Lib.SiteWhere.Model.Types.DeviceAlert alert, String originator)
        {
            var json = @"  {
                    'hardwareId': '" + alert.HardwareId + @"',
                    'type':'DeviceAlert',
                    'request': {
                        'type': '" + alert.AlertType + @"',
                        'level': 'Warning',
                        'message': '" + alert.AlertMessage + @"',
                        'updateState': false,
                        'metadata': {
                            'name1': 'value1',
                            'name2': 'value2'
                        }
                    }
                }";

            sendMessage(json.Replace("'", "\""), "alert");
        }

        public void sendMessage(string jsonMsg, string label)
        {
            connection.Publish(getTopic(), Encoding.UTF8.GetBytes(jsonMsg), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
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

}
