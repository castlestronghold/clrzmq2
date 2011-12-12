using System;
using System.Text;
using System.Diagnostics;
using System.Threading;
using ZMQ;

namespace local_lat {
    class Program {
        static int Main(string[] args) {
            if (args.Length != 3) {
                Console.Out.WriteLine("usage: local_lat <address> " +
                    "<message-size> <roundtrip-count>\n");
                return 1;
            }

            String address = args[0];
            uint messageSize = Convert.ToUInt32(args[1]);
            int roundtripCount = Convert.ToInt32(args[2]);

            //  Initialise 0MQ infrastructure
            using (Context ctx = new Context(1)) {
                using (Socket skt = ctx.Socket(SocketType.REP)) {
                    skt.Bind(address);

                    //  Bounce the messages.
                    for (int i = 0; i < roundtripCount; i++) {
                        byte[] msg;
                        msg = skt.Recv();
                        Debug.Assert(msg.Length == messageSize);
                        skt.Send(msg);
                    }
                    Thread.Sleep(1000);
                }
            }
            return 0;
        }
    }
}
