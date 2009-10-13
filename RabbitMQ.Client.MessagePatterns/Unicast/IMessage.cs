using RabbitMQ.Client;

namespace RabbitMQ.Patterns.Unicast
{
	using Address = System.String;
	using MessageId = System.String;
	using Name = System.String;

	/// <summary>
	/// High-level wrapper for a RabbitMQ.Client message.
	/// </summary>
	public interface IMessage
	{
		IBasicProperties Properties { get; set; }
		byte[] Body { get; set; }
		string RoutingKey { get; set; }

		Address From { get; set; }
		Address To { get; set; }
		Address ReplyTo { get; set; }
		MessageId MessageId { get; set; }
		MessageId CorrelationId { get; set; }

		IMessage CreateReply();
	}

	/// <summary>
	/// Extension of the IMessage interface, provided when the message has been received
	/// (as opposed to being created for dispatch).
	/// </summary>
	public interface IReceivedMessage : IMessage
	{
		bool Redelivered { get; }
	}
}
