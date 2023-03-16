using System.Net.Sockets;
using System.Net;
using System.Text;

namespace TCPIPSampleApp
{
    internal class Program
    {
        private static bool toContinue = true;
        private static Socket client;

        //callback for timer object.
        private static void CloseSocket(object state)
        {
            toContinue = false;
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
                if (toContinue)
                {
                    Array.Reverse(framingBuffer);
                    int expectedResponseLength = BitConverter.ToInt32(framingBuffer);
                    /* 
                     * Set the preffered message buffer size and loop through the stream until 
                     * the received message length equals the expected message length. 
                    */
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
                    if (toContinue)
                    {
                        if (!string.IsNullOrEmpty(response))
                            Console.WriteLine($"Socket server response: length => {response.Length} message => {response}");
                        else
                            Console.WriteLine("Socket server response is empty.");
                    }
                }
                client.Close();
            }
            catch (Exception ex)
            {
                if (clientReceiveTimer != null)
                    await clientReceiveTimer.DisposeAsync();
                if (clientReceiveTimerLoop != null)
                    await clientReceiveTimerLoop.DisposeAsync();
                client.Close();
                if (toContinue)
                    Console.WriteLine(ex.GetType() + " => " + ex.ToString());
            }
        }

        public static async Task Main(string[] args)
        {
            string ipString = "10.233.235.150";
            int port = 7096;
            var message = "NCBA.IBPS.PROCESS.COLLATERAL.RIGHT,,ibpsuser/Kenya123/KE0010001,,IBPS*COLRIGHT*100118|COLLATERAL.CODE=300,COMPANY=BNK,PERCENTAGE.COVER=100//COLLATERAL.CODE=300,COLLATERAL.TYPE=306,DESCRIPTION=KCJ169X,APPLICATION.ID=,CURRENCY=KES,COUNTRY=KE,NOMINAL.VALUE=2800000.00,MAXIMUM.VALUE=,CHARGED.AMT=,EXECUTION.VALUE=,VALUE.DATE=20230302,REVIEW.DATE.FQU=20240302M1231,EXPIRY.DATE=,ADDRESS=,NOTES=,APPLICATION=,MD.NAME.DOC=,MD.DATE.DOC=,MD.DTE.RECEIPT=,MD.REVIEW.DTE=,MD.RECEIVED=,MD.COMMENTS=,VDNAME.VALUER=,VDDTE.VALUAT=,VDNEXT.VALDTE=,VDVALUE.AMT=2800000.00,OPEN.MKT.VALUE=,FORCED.SALVALUE=,INSURANCE.VALUE=,INS.ISS.DATE=,VAL.RCPT.DATE=,VDRECEIVED.YN=,VD.ADD.COMMENTS=,AR.ANN.RETURN=,AR.DATE.FILE=,AR.REVIEW.DTE=,AR.RECEIVED=,COM.ADD.COMMENT=,ID.INSU.COMPANY=CIC,ID.POLICY.NO=22/050/1/000197/2019/11,ID.AMT.INSURED=2800000.00,ID.INSU.COVER=MOTOR COMMERCIAL GENERAL CARTAGE,ID.RENEWAL.DATE=20231114,ID.RECEIVED=YES,SEC.DETAILS=KCJ169X,LD.NAME.LAWYER=NULL//COLLATERAL.CODE=300,COLLATERAL.TYPE=306,DESCRIPTION=KCJ169X,APPLICATION.ID=,CURRENCY=KES,COUNTRY=KE,NOMINAL.VALUE=2800000.00,MAXIMUM.VALUE=,CHARGED.AMT=,EXECUTION.VALUE=,VALUE.DATE=20230302,REVIEW.DATE.FQU=20240302M1231,EXPIRY.DATE=,ADDRESS=,NOTES=,APPLICATION=,MD.NAME.DOC=,MD.DATE.DOC=,MD.DTE.RECEIPT=,MD.REVIEW.DTE=,MD.RECEIVED=,MD.COMMENTS=,VDNAME.VALUER=,VDDTE.VALUAT=,VDNEXT.VALDTE=,VDVALUE.AMT=2800000.00,OPEN.MKT.VALUE=,FORCED.SALVALUE=,INSURANCE.VALUE=,INS.ISS.DATE=,VAL.RCPT.DATE=,VDRECEIVED.YN=,VD.ADD.COMMENTS=,AR.ANN.RETURN=,AR.DATE.FILE=,AR.REVIEW.DTE=,AR.RECEIVED=,COM.ADD.COMMENT=,ID.INSU.COMPANY=CIC,ID.POLICY.NO=22/050/1/000197/2019/11,ID.AMT.INSURED=2800000.00,ID.INSU.COVER=MOTOR COMMERCIAL GENERAL CARTAGE,ID.RENEWAL.DATE=20231114,ID.RECEIVED=YES,SEC.DETAILS=KCJ169X,LD.NAME.LAWYER=NULL//COLLATERAL.CODE=300,COLLATERAL.TYPE=306,DESCRIPTION=KCJ169X,APPLICATION.ID=,CURRENCY=KES,COUNTRY=KE,NOMINAL.VALUE=2800000.00,MAXIMUM.VALUE=,CHARGED.AMT=,EXECUTION.VALUE=,VALUE.DATE=20230302,REVIEW.DATE.FQU=20240302M1231,EXPIRY.DATE=,ADDRESS=,NOTES=,APPLICATION=,MD.NAME.DOC=,MD.DATE.DOC=,MD.DTE.RECEIPT=,MD.REVIEW.DTE=,MD.RECEIVED=,MD.COMMENTS=,VDNAME.VALUER=,VDDTE.VALUAT=,VDNEXT.VALDTE=,VDVALUE.AMT=2800000.00,OPEN.MKT.VALUE=,FORCED.SALVALUE=,INSURANCE.VALUE=,INS.ISS.DATE=,VAL.RCPT.DATE=,VDRECEIVED.YN=,VD.ADD.COMMENTS=,AR.ANN.RETURN=,AR.DATE.FILE=,AR.REVIEW.DTE=,AR.RECEIVED=,COM.ADD.COMMENT=,ID.INSU.COMPANY=CIC,ID.POLICY.NO=22/050/1/000197/2019/11,ID.AMT.INSURED=2800000.00,ID.INSU.COVER=MOTOR COMMERCIAL GENERAL CARTAGE,ID.RENEWAL.DATE=20231114,ID.RECEIVED=YES,SEC.DETAILS=KCJ169X,LD.NAME.LAWYER=NULL//COLLATERAL.CODE=300,COLLATERAL.TYPE=306,DESCRIPTION=KCJ169X,APPLICATION.ID=,CURRENCY=KES,COUNTRY=KE,NOMINAL.VALUE=2800000.00,MAXIMUM.VALUE=,CHARGED.AMT=,EXECUTION.VALUE=,VALUE.DATE=20230302,REVIEW.DATE.FQU=20240302M1231,EXPIRY.DATE=,ADDRESS=,NOTES=,APPLICATION=,MD.NAME.DOC=,MD.DATE.DOC=,MD.DTE.RECEIPT=,MD.REVIEW.DTE=,MD.RECEIVED=,MD.COMMENTS=,VDNAME.VALUER=,VDDTE.VALUAT=,VDNEXT.VALDTE=,VDVALUE.AMT=2800000.00,OPEN.MKT.VALUE=,FORCED.SALVALUE=,INSURANCE.VALUE=,INS.ISS.DATE=,VAL.RCPT.DATE=,VDRECEIVED.YN=,VD.ADD.COMMENTS=,AR.ANN.RETURN=,AR.DATE.FILE=,AR.REVIEW.DTE=,AR.RECEIVED=,COM.ADD.COMMENT=,ID.INSU.COMPANY=CIC,ID.POLICY.NO=22/050/1/000197/2019/11,ID.AMT.INSURED=2800000.00,ID.INSU.COVER=MOTOR COMMERCIAL GENERAL CARTAGE,ID.RENEWAL.DATE=20231114,ID.RECEIVED=YES,SEC.DETAILS=KCJ169X,LD.NAME.LAWYER=NULL";
            int sendTimeoutMs = 5000;
            int receiveTimeoutMs = 15000;
            await UseSocketClass(ipString, port, message, sendTimeoutMs, receiveTimeoutMs);
            Console.ReadLine();
        }
    }
}