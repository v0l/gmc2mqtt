using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using shared;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO.Ports;
using System.Text.Json;
using System.Threading.Tasks;

namespace gmc2mqtt
{
    class Program
    {
        static Task Main(string[] args)
        {
            var cmd = new RootCommand("GQ - Geiger Muller Counter to MQTT")
            {
                new Option<Uri>("--mqtt", "Mqtt server address") {
                    IsRequired = true 
                },
                new Option<string>("--serial-port", "Serial port to open for GMC device")
                {
                    IsRequired = true
                }
            };

            cmd.Handler = CommandHandler.Create(async (Uri mqtt, string serialPort) =>
            {
                var factory = new MqttFactory();
                var mqttClient = factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithClientId($"gmc2mqtt-{Guid.NewGuid()}")
                    .WithTcpServer(mqtt.DnsSafeHost, mqtt.IsDefaultPort ? null : mqtt.Port)
                    .Build();

                await mqttClient.ConnectAsync(options);

                while (true)
                {
                    try
                    {
                        await ReadCPM(serialPort, mqttClient);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                }
            });

            return cmd.InvokeAsync(args);
        }

        static async Task ReadCPM(string port, IMqttClient mqttClient)
        {
            var sp = new SerialPort(port);
            sp.BaudRate = 57600;
            sp.DataBits = 8;
            sp.Parity = Parity.None;
            sp.StopBits = StopBits.One;

            sp.Open();

            //get serial
            sp.WriteLine("<GETSERIAL>>");

            var serial = ReadExactly(sp, 7);

            var serialHex = BitConverter.ToString(serial)
                .Replace("-", string.Empty).ToLowerInvariant();

            Console.WriteLine($"Serial: {serialHex}");

            while (true)
            {
                sp.WriteLine("<GETCPM>>");

                var msb = (byte)sp.ReadByte();
                var lsb = (byte)sp.ReadByte();

                var cpm = GMCTool.GetCPM(msb, lsb);
                Console.WriteLine($"CPM: [{msb:X2} {lsb:X2}] {cpm}");
                if (cpm.Type == CountType.CountsPerMinute)
                {
                    var payload = new Reading(cpm.Value);
                    await mqttClient.PublishAsync($"gmc2mqtt/{serialHex}", JsonSerializer.Serialize(payload));
                }
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        static byte[] ReadExactly(SerialPort sp, int n)
        {
            var offset = 0;
            var result = new byte[n];
            read_again:
            offset += sp.Read(result, offset, n - offset);
            if(offset < n)
            {
                goto read_again;
            }
            return result;
        }
    }

    internal record Reading(int CPM);
}
