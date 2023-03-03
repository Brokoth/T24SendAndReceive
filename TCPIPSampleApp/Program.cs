using System.Net.Sockets;
using System.Net;
using System.Text;

namespace TCPIPSampleApp
{
    internal class Program
    {
        public static async Task UseSocketClass(string ipString, int port, string message)
        {
            string response = "";
            int received;
            try
            {
                //build ip endpoint
                IPAddress ip = IPAddress.Parse(ipString);
                IPEndPoint ipEndPoint = new(ip, port);
                //initialize socket client to use TCP protocol
                using Socket client = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {                  
                    //initialize connection to remote endpoint
                    await client.ConnectAsync(ipEndPoint);
                    /*get message length
                     * convert it into big endian notation and convert that into a byte array
                     * convert the message into a byte array
                     * concatenate the messagelength byte array with the message byte array in that order
                     */
                    int messageLength = message.Length;
                    var messageLenghtBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(messageLength));
                    var messageBytes = Encoding.UTF8.GetBytes(message);
                    byte[] finalMessageBytes = messageLenghtBytes.Concat(messageBytes).ToArray();
                    //set initial client timeouts
                    client.SendTimeout = 5000;
                    client.ReceiveTimeout = 70000;
                    //asynchronously send the bytes to the remote endpoint
                    _ = await client.SendAsync(finalMessageBytes, SocketFlags.None);
                    Console.WriteLine($"Socket client sent message: \"{message}\"");
                    var buffer = new byte[1024];
                    received = client.Receive(buffer, SocketFlags.None);
                    response = response + Encoding.UTF8.GetString(buffer, 0, received);
                    /*reset timeout to small value and loop though entire stream
                     * this will loop until the relevant stream is read and timeout expires
                    */
                    client.ReceiveTimeout = 1000;
                    while (received > 0)
                    {
                        received = client.Receive(buffer, SocketFlags.None);
                        response += Encoding.UTF8.GetString(buffer, 0, received);
                    }

                    client.Shutdown(SocketShutdown.Both);
                }
                catch (SocketException ex)
                {
                    client.Shutdown(SocketShutdown.Both);

                    if (!string.IsNullOrEmpty(response))
                        Console.WriteLine($"Socket response => {response}");
                    else
                        Console.WriteLine("Socket response is empty.");

                    Console.WriteLine($"Response So Far => {response}");
                    Console.WriteLine(ex.GetType() + " => " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType() + " => " + ex.Message);
            }
        }
        public static async Task Main(string[] args)
        {
            string ipString = "10.233.235.150";
            int port = 7096;
            var message = "ENQUIRY.SELECT,,ICONALERTS/Kenya123/KE0010001,API.ALERTS";
            await UseSocketClass(ipString, port, message);
            Console.ReadLine();
        }

    }
}