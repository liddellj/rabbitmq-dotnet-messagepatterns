using System;
using RabbitMQ.Client.Events;

namespace RabbitMQ.Client.MessagePatterns.Unicast {
    using Address = String;
    using MessageId = String;

    /// <summary>
    /// Implementation of the IMessage wrapper.
    /// </summary>
    internal class Message : IMessage {

        protected IBasicProperties m_properties;
        protected byte[] m_body;
        protected string m_routingKey;

        public IBasicProperties Properties {
            get { return m_properties; }
            set { m_properties = value; }
        }
        public byte[] Body {
            get { return m_body; }
            set { m_body = value; }
        }
        public string RoutingKey {
            get { return m_routingKey; }
            set { m_routingKey = value; }
        }

        public Address From {
            get { return Properties.UserId; }
            set {
                Properties.UserId = value;
                if (value == null) Properties.ClearUserId();
            }
        }
        public Address To {
            get { return RoutingKey; }
            set { RoutingKey = value; }
        }
        public Address ReplyTo {
            get { return Properties.ReplyTo; }
            set {
                Properties.ReplyTo = value;
                if (value == null) Properties.ClearReplyTo();
            }
        }
        public MessageId MessageId {
            get { return Properties.MessageId; }
            set {
                Properties.MessageId = value;
                if (value == null) Properties.ClearMessageId();
            }
        }
        public MessageId CorrelationId {
            get { return Properties.CorrelationId; }
            set {
                Properties.CorrelationId = value;
                if (value == null) Properties.ClearCorrelationId();
            }
        }

        public Message() {
        }

        public Message(IBasicProperties props, byte[] body, string rk) {
            m_properties = props;
            m_body = body;
            m_routingKey = rk;
        }

        public IMessage CreateReply() {
            IMessage m = new Message(Properties.Clone() as IBasicProperties,
                                     Body,
                                     RoutingKey);
            m.From = To;
            m.To = ReplyTo ?? From;
            m.ReplyTo = null;
            m.CorrelationId = MessageId;
            m.MessageId = null;

            return m;
        }

    }

    internal class ReceivedMessage : Message, IReceivedMessage {

        protected IModel m_channel;
        protected BasicDeliverEventArgs m_delivery;

        public bool Redelivered {
            get { return m_delivery.Redelivered; }
        }

        public IModel Channel {
            get { return m_channel; }
        }

        public BasicDeliverEventArgs Delivery {
            get { return m_delivery; }
        }

        public ReceivedMessage(IModel channel, BasicDeliverEventArgs delivery) :
            base(delivery.BasicProperties,
                 delivery.Body,
                 delivery.RoutingKey) {
            m_channel = channel;
            m_delivery = delivery;
        }

    }
}
