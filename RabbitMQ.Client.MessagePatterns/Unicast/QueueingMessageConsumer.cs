using RabbitMQ.Client.Events;
using RabbitMQ.Util;

namespace RabbitMQ.Client.MessagePatterns.Unicast {
    /// <summary>
    /// Variant on the RabbitMQ.Client.QueueingBasicConsumer that will construct
    /// ReceivedMessage instance objects (which bundle the model too) instead of simply providing
    /// the BasicDeliverEventArgs.
    /// </summary>
    internal class QueueingMessageConsumer : DefaultBasicConsumer {

        protected SharedQueue m_queue;

        public QueueingMessageConsumer(IModel model)
            : base(model) {
            m_queue = new SharedQueue();
        }

        public SharedQueue Queue {
            get { return m_queue; }
        }

        public override void OnCancel() {
            m_queue.Close();
            base.OnCancel();
        }

        public override void HandleBasicDeliver(string consumerTag,
                                                ulong deliveryTag,
                                                bool redelivered,
                                                string exchange,
                                                string routingKey,
                                                IBasicProperties properties,
                                                byte[] body) {
            BasicDeliverEventArgs e = new BasicDeliverEventArgs();
            e.ConsumerTag = consumerTag;
            e.DeliveryTag = deliveryTag;
            e.Redelivered = redelivered;
            e.Exchange = exchange;
            e.RoutingKey = routingKey;
            e.BasicProperties = properties;
            e.Body = body;
            m_queue.Enqueue(new ReceivedMessage(Model, e));
        }

    }
}
