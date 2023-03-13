﻿using System.Net.Sockets;
using System.Net;
using System.Text;

namespace TCPIPSampleApp
{
    internal class Program
    {
        private static Socket client;
        //callback for timer object.
        private static void CloseSocket(object state)
        {
            Console.WriteLine("Socket client timeout elapsed");
            client.Close();
        }
        public static async Task UseSocketClass(string ipString, int port, string message, int sendTimeoutMs, int receiveTimeoutMs)
        {
            string response = "";
            Timer clientReceiveTimer = null;
            Timer clientReceiveTimerLoop = null;
            //Build ip endpoint 
            IPAddress ip = IPAddress.Parse(ipString);
            IPEndPoint ipEndPoint = new(ip, port);
            //Initialize socket client to use TCP protocol 
            client = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //initialize connection to remote endpoint 
                await client.ConnectAsync(ipEndPoint);
                //Set initial timeouts 
                client.SendTimeout = sendTimeoutMs;
                /*  
                 * Get message length, convert it into big endian notation and convert 
                 * that into a byte array and send it to the remote endpoint. 
                 */
                int messageLength = message.Length;
                var messageLengthBytes = BitConverter.GetBytes(message.Length);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(messageLengthBytes);

                await client.SendAsync(messageLengthBytes, SocketFlags.None);
                //Asynchronously send the actual message bytes to the remote endpoint 
                var messageBytes = Encoding.UTF8.GetBytes(message);
                Console.WriteLine($"Socket client sent message: {message}");
                await client.SendAsync(messageBytes, SocketFlags.None);
                /* 
                 * Get the framing bytes from T24 which describes the length of the expected 
                 * message. 
                */
                var framingBuffer = new byte[4];
                //Create timer object to act as a timeout since .Receive() method doensn't work for asynchronous calls
                clientReceiveTimer = new Timer(CloseSocket, null, receiveTimeoutMs, receiveTimeoutMs);
                await client.ReceiveAsync(framingBuffer, SocketFlags.None);
                await clientReceiveTimer.DisposeAsync();
                /* 
                 * Reverse the array (since it is in little endian notation) and then convert 
                 * to an int. 
                 */
                Array.Reverse(framingBuffer);
                int expectedResponseLength = BitConverter.ToInt32(framingBuffer);
                /* 
                 * Set the preffered message buffer size and loop through the stream until 
                 * the received message length equals the expected message length. 
                */
                var buffer = new byte[1024];
                int actualresponseLength = 0;
                while (actualresponseLength < expectedResponseLength)
                {
                    clientReceiveTimerLoop = new Timer(CloseSocket, null, receiveTimeoutMs, receiveTimeoutMs);
                    int received = await client.ReceiveAsync(buffer, SocketFlags.None);
                    string currentBatch = Encoding.UTF8.GetString(buffer, 0, received);
                    actualresponseLength += currentBatch.Length;
                    response += currentBatch;
                    await clientReceiveTimerLoop.DisposeAsync();

                }

                if (!string.IsNullOrEmpty(response))
                    Console.WriteLine($"Socket server response: length => {response.Length} message => {response}");
                else
                    Console.WriteLine("Socket server response is empty.");

                client.Close();
            }
            catch (Exception ex)
            {
                if(clientReceiveTimer!=null)
                    await clientReceiveTimer.DisposeAsync();
                if(clientReceiveTimerLoop!=null)
                    await clientReceiveTimerLoop.DisposeAsync();
                client.Close();
                Console.WriteLine(ex.GetType() + " => " + ex.Message);
            }
        }

        public static async Task Main(string[] args)
        {
            string ipString = "10.233.235.150";
            int port = 7096;
            var message = "ENQUIRY.SELECT,,ICONALERTS/Kenya123/KE0010001,API.ALERTS";
            int sendTimeoutMs = 5000;
            int receiveTimeoutMs = 5000;
            await UseSocketClass(ipString, port, message, sendTimeoutMs, receiveTimeoutMs);
            Console.ReadLine();
        }
    }
}