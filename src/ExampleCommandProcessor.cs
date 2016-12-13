using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using sitewhere_dotnet_agent.Lib;
using sitewhere_dotnet_agent.Lib.SiteWhere;

namespace sitewhere_dotnet_agent
{
    public class ExampleCommandProcessor : BaseCommandProcessor
    {
        /** Static logger instance */
        private static Logger LOGGER = Logger.getLogger(typeof(ExampleCommandProcessor).ToString());

        Thread thread;

        public override void executeStartupLogic(string hardwareId, string specificationToken, ISiteWhereEventDispatcher dispatcher)
        {
            sendRegistration(hardwareId, specificationToken);
            base.executeStartupLogic(hardwareId, specificationToken, dispatcher);
        }

        public override void handleRegistrationAck(Device.Types.Header header, Device.Types.RegistrationAck ack)
        {
            switch (ack.State)
            {
                case Device.Types.RegistrationAckState.NewRegistration:
                    {
                        LOGGER.info("SiteWhere indicated device was successfully registered.");
                        onRegistrationConfirmed();
                        break;
                    }
                case Device.Types.RegistrationAckState.AlreadyRegistered:
                    {
                        LOGGER.info("SiteWhere indicated device is using an existing registration.");
                        onRegistrationConfirmed();
                        break;
                    }
                case Device.Types.RegistrationAckState.RegistrationError:
                    {
                        LOGGER.warning("SiteWhere indicated a device registration error.");
                        break;
                    }
            }

            base.handleRegistrationAck(header, ack);
        }

        public override void handleRegistrationAckJson(string registrationAckState)
        {
            switch (registrationAckState.ToUpper())
            {
                case "NEWREGISTRATION":
                    onRegistrationConfirmed();
                    break;
            }

            base.handleRegistrationAckJson(registrationAckState);
        }

        public void onRegistrationConfirmed()
        {
            sendDataAtInterval();
        }

        /// <summary>
        /// This is an example of creating a thread that will send data to SiteWhere every so often, sleeping between cycles.
        /// </summary>
        public void sendDataAtInterval()
        {
            thread = new Thread(new ThreadStart(() =>
           {
               while (true)
               {
                   try
                   {
                       var m = new Model.Types.Measurement() { MeasurementId = "Time ticks", MeasurementValue = DateTime.Now.Ticks };
                       sendMeasurement(getHardwareId(), new Model.Types.Measurement[] { m }, null);

                       //sendLocation(getHardwareId(), latitudeSettingByServer, longitudeSettingByServer, elevationSettingByServer, null);

                       LOGGER.info("Sent a batch of statistics.");

                       Thread.Sleep(5000);
                   }
                   catch (Exception e)
                   {
                       LOGGER.log("Unable to send measurements to SiteWhere.", e);
                   }
               }
           }));

            thread.Start();
        }

        public override void Dispose()
        {
            try
            {
                if (thread != null)
                {
                    thread.Abort();
                }
            }
            catch (Exception e)
            {

            }

            base.Dispose();
        }

        /// <summary>
        /// Handler for 'helloWorld(String, boolean)' command.
        /// </summary>
        /// <param name="greeting"></param>
        /// <param name="loud"></param>
        /// <param name="originator"></param>
        public void helloWorld(String greeting, Boolean loud, IDeviceEventOriginator originator)
        {
            String response = greeting + " Yungoal!";
            if (loud)
            {
                response = response.ToUpper();
            }
            sendAck(getHardwareId(), response, originator);
            LOGGER.info("Sent reponse to 'helloWorld' command.");
        }

        /// <summary>
        /// Handler for 'ping()' command.
        /// </summary>
        /// <param name="originator"></param>
        public void ping(IDeviceEventOriginator originator)
        {
            sendAck(getHardwareId(), "Acknowledged.", originator);
            LOGGER.info("Sent reponse to 'ping' command.");
        }

        /// <summary>
        /// Handler for 'testEvents()' command.
        /// </summary>
        /// <param name="originator"></param>
        public void testEvents(IDeviceEventOriginator originator)
        {
            //sendMeasurement(getHardwareId(), "engine.temp", 170.0, originator);
            sendLocation(getHardwareId(), 33.7550, -84.3900, 0.0, originator);
            sendAlert(getHardwareId(), "engine.overheat", "Engine is overheating!", originator);
            LOGGER.info("Sent reponse to 'testEvents' command.");
        }
    }
}
