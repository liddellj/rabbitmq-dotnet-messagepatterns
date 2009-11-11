using System;

namespace RabbitMQ.Client.MessagePatterns.Unicast {
    /// <summary>
    /// Delegate used to subscribe to message related events that can be emitted from
    /// the IMessaging infrastructure.
    /// </summary>
    /// <param name="m"></param>
    public delegate void MessageEventHandler(IMessage m);

    /// <summary>
    /// Delegate that can be provided to the messaging infrastructure to handle setting
    /// up a freshly created connection.
    /// </summary>
    /// <param name="channel">the model that is being setup</param>
    public delegate void SetupDelegate(IModel channel);

    /// <summary>
    /// Interface of common functionality supported by both senders and receivers.
    /// </summary>
    public interface IMessagingCommon {
        /// <summary>
        /// The connector that the messaging component will use to execute operations.
        /// </summary>
        IConnector Connector { get; set; }

        /// <summary>
        /// Identification details for the messaging component.
        /// </summary>
        String Identity { get; set; }
    }

    /// <summary>
    /// Reliable sender interface.
    /// </summary>
    public interface ISender : IMessagingCommon {
        /// <summary>
        /// Event issued when a connection requires setup. This will be fired upon initial connection, and
        /// then whenever a failure requires the connection to be re-established.
        /// </summary>
        event SetupDelegate Setup;

        String ExchangeName { get; set; }
        bool Transactional { get; set; }

        String CurrentId { get; }

        event MessageEventHandler Sent;

        void Init();
        void Init(long msgIdPrefix);

        IMessage CreateMessage();
        IMessage CreateReply(IMessage m);
        void Send(IMessage m);
    }

    /// <summary>
    /// Reliable receiver interface.
    /// </summary>
    public interface IReceiver : IMessagingCommon {
        /// <summary>
        /// Event issued when a connection requires setup. This will be fired upon initial connection, and
        /// then whenever a failure requires the connection to be re-established.
        /// </summary>
        event SetupDelegate Setup;

        String QueueName { get; set; }
        bool Exclusive { get; set; }

        void Init();

        void Cancel();

        IReceivedMessage Receive();
        IReceivedMessage ReceiveNoWait();
        void Ack(IReceivedMessage m);
    }

    /// <summary>
    /// Combined sender/receiver interface.
    /// </summary>
    public interface IMessaging : ISender, IReceiver {
        /// <summary>
        /// Event issued when a connection requires setup. This will be fired upon initial connection, and
        /// then whenever a failure requires the connection to be re-established.
        /// </summary>
        new event SetupDelegate Setup;

        event SetupDelegate SetupSender;
        event SetupDelegate SetupReceiver;

        new void Init();
        new void Init(long msgIdPrefix);
    }
}
