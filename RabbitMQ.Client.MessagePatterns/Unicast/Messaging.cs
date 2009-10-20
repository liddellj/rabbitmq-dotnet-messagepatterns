using System;
using System.IO;
using RabbitMQ.Client.Framing.v0_8;
using RabbitMQ.Patterns.Unicast;

namespace RabbitMQ.Client.MessagePatterns.Unicast {
	class Validator
	{

		public static void CheckNotNull(Object thing, Object c, string prop)
		{
			if (thing == null)
			{
				string msg = String.Format("'{0}' property in {1} " +
				                           "must not be null",
				                           prop, c);
				throw new InvalidOperationException(msg);
			}
		}
	}

	internal class Sender : ISender
	{

		protected IConnector m_connector;
		protected SetupDelegate m_setup;
		protected String m_identity;
		protected String m_exchangeName = "";
		protected bool m_transactional = true;

		protected IModel m_channel;

		protected long m_msgIdPrefix;
		protected long m_msgIdSuffix;

		public event MessageEventHandler Sent;

		public IConnector Connector
		{
			get { return m_connector; }
			set { m_connector = value; }
		}

		public event SetupDelegate Setup;

		public String Identity
		{
			get { return m_identity; }
			set { m_identity = value; }
		}

		public String ExchangeName
		{
			get { return m_exchangeName; }
			set { m_exchangeName = value; }
		}

		public bool Transactional
		{
			get { return m_transactional; }
			set { m_transactional = value; }
		}

		public String CurrentId
		{
			get
			{
				return String.Format("{0:x8}{1:x8}",
				                     m_msgIdPrefix, m_msgIdSuffix);
			}
		}

		public Sender() { }

		public void Init()
		{
			Init(DateTime.UtcNow.Ticks);
		}

		public void Init(long msgIdPrefix)
		{
			CheckProps();
			m_msgIdPrefix = msgIdPrefix;
			m_msgIdSuffix = 0;

			Connector.Connect(Connect);
		}

		protected void CheckProps()
		{
			Validator.CheckNotNull(Connector, this, "Connector");
			Validator.CheckNotNull(ExchangeName, this, "ExchangeName");
		}

		protected void Connect(IConnection conn)
		{
			m_channel = conn.CreateModel();
			if (Transactional) m_channel.TxSelect();
			SetupDelegate setupHandler = Setup;
			if (setupHandler != null) Setup(m_channel);
		}

		protected String NextId()
		{
			String res = CurrentId;
			m_msgIdSuffix++;
			return res;
		}

		public IMessage CreateMessage()
		{
			IMessage m = new Message();
			m.Properties = m_channel.CreateBasicProperties();
			m.From = Identity;
			m.MessageId = NextId();
			return m;
		}

		public IMessage CreateReply(IMessage m)
		{
			IMessage r = m.CreateReply();
			r.MessageId = NextId();
			return r;
		}

		public void Send(IMessage m)
		{
			while (true)
			{
				if (Connector.Try(delegate()
				                  {
				                  	m_channel.BasicPublish(ExchangeName,
				                  	                       m.RoutingKey,
				                  	                       m.Properties, m.Body);
				                  	if (Transactional) m_channel.TxCommit();
				                  }, Connect)) break;
			}
			//TODO: if/when IModel supports 'sent' notifications then
			//we will translate those, rather than firing our own here
			if (Sent != null) Sent(m);
		}

	}

	internal class Receiver : IReceiver
	{

		protected IConnector m_connector;
		protected SetupDelegate m_setup;
		protected String m_identity;
		protected String m_queueName = "";

		protected IModel m_channel;

		protected QueueingMessageConsumer m_consumer;
		protected string m_consumerTag;
		
		public IConnector Connector
		{
			get { return m_connector; }
			set { m_connector = value; }
		}

		public event SetupDelegate Setup;

		public String Identity
		{
			get { return m_identity; }
			set { m_identity = value; }
		}

		public String QueueName
		{
			get { return ("".Equals(m_queueName) ? Identity : m_queueName); }
			set { m_queueName = value; }
		}

		public Receiver() { }

		public void Init()
		{
			CheckProps();
			Connector.Connect(Connect);
		}

		protected void CheckProps()
		{
			Validator.CheckNotNull(Connector, this, "Connector");
			Validator.CheckNotNull(QueueName, this, "QueueName");
		}

		protected void Connect(IConnection conn)
		{
			m_channel = conn.CreateModel();
			SetupDelegate setupHandler = Setup;
			if (setupHandler != null) Setup(m_channel);
			Consume();

			return;
		}

		protected void Consume()
		{
			m_consumer = new QueueingMessageConsumer(m_channel);
			m_consumerTag = m_channel.BasicConsume
				(QueueName, false, null, m_consumer);
		}

		public void Cancel()
		{
			Connector.Try(() => m_channel.BasicCancel(m_consumerTag), Connect);
		}

		public IReceivedMessage Receive()
		{
			IReceivedMessage res = null;
			while (true)
			{
				if (Connector.Try(delegate()
				                  {
									  try
									  {
										  res = m_consumer.Queue.Dequeue()
												as IReceivedMessage;
									  }
									  catch (EndOfStreamException) 
									  {
										if (m_consumer.ShutdownReason == null) 
										{
											// EndOfStream with a missing ShutdownReason indicates that 
											// a CancelOk came in.
											res = null;
									  	}
									    else 
										{
											throw;	
										}
									  }
				                  }, Connect)) break;
			}

			if (res == null) 
			{
				throw new EndOfStreamException();	
			}

			return res;
		}

		public IReceivedMessage ReceiveNoWait()
		{
			IReceivedMessage res = null;
			while (true)
			{
				if (Connector.Try(delegate()
				                  {
				                  	try 
									{
										res = m_consumer.Queue.DequeueNoWait(null)
											  as IReceivedMessage;
				                  	}
				                  	catch (EndOfStreamException) 
									{
										if (m_consumer.ShutdownReason == null)
										{
											// EndOfStream with a missing ShutdownReason indicates that 
											// a CancelOk came in.
											res = null;
										}
										else
										{
											throw;
										}
				                  	}
				                  		
				                  }, Connect)) break;
			}

			if (m_consumer != null && m_consumer.ShutdownReason == null) 
			{
				throw new EndOfStreamException();	
			}

			return res;
		}

		public void Ack(IReceivedMessage m)
		{
			ReceivedMessage r = m as ReceivedMessage;
			if (r == null || r.Channel != m_channel)
			{
				//must have been reconnected; drop ack since there is
				//no place for it to go
				return;
			}
			//Acks must not be retried since they are tied to the
			//channel on which the message was delivered
			Connector.Try(delegate()
			              {
			              	m_channel.BasicAck(r.Delivery.DeliveryTag, false);
			              }, Connect);
		}
	}

	internal class Messaging : IMessaging
	{

		protected ISender m_sender = new Sender();
		protected IReceiver m_receiver = new Receiver();

		public IConnector Connector
		{
			get { return m_sender.Connector; }
			set { m_sender.Connector = value; m_receiver.Connector = value; }
		}

		event SetupDelegate IMessaging.Setup
		{
			add { m_sender.Setup += value; m_receiver.Setup += value; }
			remove { m_sender.Setup -= value; m_receiver.Setup -= value; }
		}

		event SetupDelegate ISender.Setup 
		{
			add { m_sender.Setup += value; }
			remove { m_sender.Setup -= value; }
		}
		public event SetupDelegate SetupSender 
		{
			add { m_sender.Setup += value; }
			remove { m_sender.Setup -= value; }
		}
		event SetupDelegate IReceiver.Setup 
		{
			add { m_receiver.Setup += value; }
			remove { m_receiver.Setup -= value; }
		}
		public event SetupDelegate SetupReceiver 
		{
			add { m_receiver.Setup += value; }
			remove { m_receiver.Setup -= value; }
		}

		public String Identity
		{
			get { return m_sender.Identity; }
			set { m_sender.Identity = value; m_receiver.Identity = value; }
		}

		public String ExchangeName
		{
			get { return m_sender.ExchangeName; }
			set { m_sender.ExchangeName = value; }
		}

		public bool Transactional
		{
			get { return m_sender.Transactional; }
			set { m_sender.Transactional = value; }
		}

		public String CurrentId
		{
			get { return m_sender.CurrentId; }
		}

		public String QueueName
		{
			get { return m_receiver.QueueName; }
			set { m_receiver.QueueName = value; }
		}

		public event MessageEventHandler Sent
		{
			add { m_sender.Sent += value; }
			remove { m_sender.Sent -= value; }
		}

		void ISender.Init()
		{
			m_sender.Init();
		}

		void ISender.Init(long msgIdPrefix)
		{
			m_sender.Init(msgIdPrefix);
		}

		void IReceiver.Init()
		{
			m_receiver.Init();
		}

		public void Init()
		{
			(this as ISender).Init();
			(this as IReceiver).Init();
		}

		public void Init(long msgIdPrefix)
		{
			(this as ISender).Init(msgIdPrefix);
			(this as IReceiver).Init();
		}

		public IMessage CreateMessage()
		{
			return m_sender.CreateMessage();
		}

		public IMessage CreateReply(IMessage m)
		{
			return m_sender.CreateReply(m);
		}

		public void Send(IMessage m)
		{
			m_sender.Send(m);
		}

		public IReceivedMessage Receive()
		{
			return m_receiver.Receive();
		}

		public IReceivedMessage ReceiveNoWait()
		{
			return m_receiver.ReceiveNoWait();
		}

		public void Ack(IReceivedMessage m)
		{
			m_receiver.Ack(m);
		}

		public void Cancel() 
		{
			m_receiver.Cancel();	
		}

		public Messaging()
		{
		}

	}
}