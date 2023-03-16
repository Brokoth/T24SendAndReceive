using System.Net.Sockets;
using System.Net;
using System.Text;

namespace T24SendAndReceiveLibrary
{
    public class T24
    {
        private static bool toContinue = true;
        private static Socket client;

        private static void CloseSocket(object state)
        {
            toContinue = false;
            Console.WriteLine("Socket client timeout elapsed");
            client.Close();
        }
        public static async Task<string[]> Send(string ipString, int port, string message, int sendTimeoutMs, int receiveTimeoutMs)
        {
            string response = "";
            Timer clientReceiveTimer = null;
            Timer clientReceiveTimerLoop = null;
            IPAddress ip = IPAddress.Parse(ipString);
            IPEndPoint ipEndPoint = new(ip, port);
            client = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                await client.ConnectAsync(ipEndPoint);
                client.SendTimeout = sendTimeoutMs;
                int messageLength = message.Length;
                var messageLengthBytes = BitConverter.GetBytes(message.Length);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(messageLengthBytes);

                await client.SendAsync(messageLengthBytes, SocketFlags.None);
                var messageBytes = Encoding.UTF8.GetBytes(message);
                await client.SendAsync(messageBytes, SocketFlags.None);
                var framingBuffer = new byte[4];
                clientReceiveTimer = new Timer(CloseSocket, null, receiveTimeoutMs, receiveTimeoutMs);
                await client.ReceiveAsync(framingBuffer, SocketFlags.None);
                await clientReceiveTimer.DisposeAsync();

                if (toContinue)
                {

                    Array.Reverse(framingBuffer);
                    int expectedResponseLength = BitConverter.ToInt32(framingBuffer);
                    var buffer = new byte[1024];
                    int actualresponseLength = 0;

                    while (actualresponseLength < expectedResponseLength && toContinue)
                    {

                        clientReceiveTimerLoop = new Timer(CloseSocket, null, receiveTimeoutMs, receiveTimeoutMs);
                        int received = await client.ReceiveAsync(buffer, SocketFlags.None);
                        string currentBatch = Encoding.UTF8.GetString(buffer, 0, received);
                        actualresponseLength += currentBatch.Length;
                        response += currentBatch;
                        await clientReceiveTimerLoop.DisposeAsync();

                    }

                }
                client.Close();
                if (toContinue)
                    return new string[] { "OK", response };
                else
                    return new string[] { "FAIL", "Socket client timeout elapsed" };
            }
            catch (Exception ex)
            {

                if (clientReceiveTimer != null)
                    await clientReceiveTimer.DisposeAsync();

                if (clientReceiveTimerLoop != null)
                    await clientReceiveTimerLoop.DisposeAsync();

                client.Close();

                if (toContinue)
                    return new string[] { "FAIL", ex.GetType() + " => " + ex.Message };
                else
                    return new string[] { "FAIL", "Socket client timeout elapsed" };

            }
        }
    }
}