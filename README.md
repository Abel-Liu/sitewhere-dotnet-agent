# SiteWhere DotNET Agent

A .Net version of [sitewhere-java-agent](https://github.com/sitewhere/sitewhere-tools/tree/master/sitewhere-java-agent).

This client interact with SiteWhere over the MQTT transport by sending and receiving messages encoded in `JSON` format.

##Agent Usage Example

This project includes an example that shows how round-trip processing is accomplished. The device registers itself as a Raspberry Pi based on the specification token provided in the SiteWhere sample data. Once registered, it starts an event loop that sends test data to SiteWhere, then waits a few seconds and sends another batch. The example also implements the list of commands declared for the Raspberry Pi device specification, so if commands come in from SiteWhere, the corresponding methods are invoked on the agent.

###SiteWhere Tenant Configuration

Edit configuration to transport data use JSON format. On SiteWhere management site, **Tenant Configuration/Device Communication/Device Command Routing/Specification Mapping Router/Specification Mapping(Raspberry Pi)**, change Destination id to `json`.

###Running the Example

Check App.config

```xml
<appSettings>
  <add key="mqtt.hostname" value="192.168.1.223"/>
  <add key="command.processor.classname" value="sitewhere_dotnet_agent.ExampleCommandProcessor"/>
  <add key="device.hardware.id" value="test123"/>
  <add key="device.specification.token" value="7dfd6d63-5e8d-4380-be04-fc5c73801dfb"/>
  <add key="mqtt.outbound.sitewhere.topic" value="SiteWhere/input/json"/>
  <add key="site.token" value="bb105f8d-3150-41f5-b9d1-db04965668d3"/>
</appSettings>
```
Edit hostname to your SiteWhere server address.

*There is some problems using [Google Protocol Buffers] (https://developers.google.com/protocol-buffers/) format, so use JsonAgent class to run the example.*
