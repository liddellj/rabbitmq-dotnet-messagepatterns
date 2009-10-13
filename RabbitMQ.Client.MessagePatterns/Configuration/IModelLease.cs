using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace RabbitMQ.Patterns.Configuration
{
	/// <summary>
	/// A ModelLease provides a temporary exclusive handle on an AMQP model. Leases should
	/// be taken out from the AmqpFactorySupport library, then disposed as soon as the code
	/// is no longer required to do any more work. Leases should not be held onto for extended
	/// periods - pooling of models is managed by the underlying factory support.
	/// </summary>
	public interface IModelLease : IDisposable
	{
		/// <summary>
		/// Retrieves the underlying model being leased.
		/// </summary>
		IModel Model { get; }
	}
}
