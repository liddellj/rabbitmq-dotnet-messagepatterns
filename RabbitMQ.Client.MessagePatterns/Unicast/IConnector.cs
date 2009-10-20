namespace RabbitMQ.Client.MessagePatterns.Unicast {
	/// <summary>
	/// Enumeration of the possible states that a connector could be in.
	/// </summary>
	public enum ConnectorState
	{
		/// <summary>
		/// The endpoint is connected to an AMQP server.
		/// </summary>
		Connected,

		/// <summary>
		/// The endpoint is not connected to any AMQP server, but is attempting to reconnect.
		/// </summary>
		Reconnecting,

		/// <summary>
		/// The endpoint is not connected to any AMQP server, and is not currently attempting
		/// to reconnect.
		/// </summary>
		Disconnected
	}

	/// <summary>
	/// Delegate used to allow a consumer of a connector to re-establish their own resources on top
	/// of a connection.
	/// </summary>
	/// <param name="conn">the connection that has been established</param>
	public delegate void ConnectionDelegate(IConnection conn);

	/// <summary>
	/// No-args delegate to be provided in re-retryable operations.
	/// </summary>
	public delegate void Thunk();

	/// <summary>
	/// Delegate that can be provided to the messaging infrastructure to receive notifications
	/// about endpoint state.
	/// </summary>
	/// <param name="sender">the connector that is sending the state</param>
	/// <param name="state">the new state the connector has entered</param>
	public delegate void ConnectorStateHandler(IConnector sender, ConnectorState state);

	/// <summary>
	/// Underlying interface provided by services that support executing reliable operations
	/// on a channel.
	/// </summary>
	public interface IConnector : System.IDisposable
	{
		/// <summary>
		/// Event issued when the state of the messaging handler changes.
		/// </summary>
		event ConnectorStateHandler StateChanged;

		/// <summary>
		/// Retrieves the state of the connector.
		/// </summary>
		ConnectorState State { get;  }

		/// <summary>
		/// The number of milliseconds to pause between reconnection attempts.
		/// </summary>
		int Pause { get; set; }

		/// <summary>
		/// The number of reconnection attempts to make in a row before entering a disconnected state.
		/// </summary>
		int Attempts { get; set; }

		/// <summary>
		/// Requests that the connector open a connection, and invoke the given delegate with the connection
		/// details.
		/// </summary>
		/// <param name="d">the delegate to inform of the connection</param>
		void Connect(ConnectionDelegate d);

		/// <summary>
		/// Requests that the connector attempt the given operation. If the connection fails whilst attempting
		/// to perform this operation, the connection delegate will be informed of any newly created connections.
		/// </summary>
		/// <param name="t">the thunk to attempt</param>
		/// <param name="d">the connection delegate to inform of any new connections</param>
		/// <returns>true - the operation succeeded; false - it did not</returns>
		bool Try(Thunk t, ConnectionDelegate d);

		/// <summary>
		/// Closes the connector and frees up any underlying resources it is consuming.
		/// </summary>
		void Close();
	}
}