using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.MessagePatterns.Configuration;

namespace RabbitMQ.Client.MessagePatterns.Unicast {
    public static class Factory {
        public static IConnector CreateConnector(ConnectionBuilder builder) {
            return new Connector(builder);
        }

        public static ISender CreateSender() {
            return new Sender();
        }

        public static IReceiver CreateReceiver() {
            return new Receiver(); ;
        }

        public static IMessaging CreateMessaging() {
            return new Messaging();
        }
    }
}
