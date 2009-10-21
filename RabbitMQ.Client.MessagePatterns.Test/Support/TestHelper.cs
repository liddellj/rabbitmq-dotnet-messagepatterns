using RabbitMQ.Client.MessagePatterns.Unicast;
using RabbitMQ.Patterns.Unicast;

namespace RabbitMQ.Client.MessagePatterns.Test.Support {
    public class TestHelper {

        public static void Sent(IMessage m) {
            LogMessage("sent", m.From, m);
        }

        public static void LogMessage(string action,
                                      string actor,
                                      IMessage m) {
            System.Console.WriteLine("{0} {1} {2}",
                                     actor, action, Decode(m.Body));
        }

        public static void LogMessage(string action,
                                      IMessaging actor,
                                      IMessage m) {
            LogMessage(action, actor.Identity, m);
        }

        public static byte[] Encode(string s) {
            return System.Text.Encoding.UTF8.GetBytes(s);
        }

        public static string Decode(byte[] b) {
            return System.Text.Encoding.UTF8.GetString(b);
        }

    }
}
