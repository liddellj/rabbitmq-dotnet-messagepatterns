using System.Configuration;

namespace RabbitMQ.Client.MessagePatterns.Configuration {
	/// <summary>
	/// Configuration section for configuring an AMQP broker connection.
	/// </summary>
	/// <example>
	/// Can be used in a form such as:
	///   <code>
	///     <config>
	///         <configSections>
	///             <section name="amqp" type="RabbitMQ.Client.MessagePatterns.Configuration.AmqpConnectionSettingsSection, RabbitMQ.Patterns" />
	///         </configSections>
	///         <amqp>
	///            <connection name="trades" server="localhost" />
	///            <connection name="curves" server="localhost" port="5672" />
	///         </amqp>
	///     </config>
	///   </code>
	/// </example>
	public class AmqpConnectionSettingsSection : ConfigurationSection {
		[ConfigurationProperty("connections", IsDefaultCollection = true)]
		[ConfigurationCollection(typeof(AmqpConnectionCollection), AddItemName = "connection")]
		public AmqpConnectionCollection Connections {
			get {
				return (AmqpConnectionCollection) this["connections"];
			}
		}
	}

	/// Provides a configuration collection of named services.
	/// </summary>
	public class AmqpConnectionCollection : ConfigurationElementCollection {
		protected override ConfigurationElement CreateNewElement() {
			return new AmqpConnection();
		}

		protected override object GetElementKey(ConfigurationElement element) {
			var service = (AmqpConnection) element;

			return GetKey(service);
		}

		/// <summary>
		/// Gets or sets the named service element for the given index.
		/// </summary>
		/// <param name="index">The index of the named service element to get or set.</param>
		/// <returns>The named service element.</returns>
		public AmqpConnection this[int index] {
			get {
				return (AmqpConnection) BaseGet(index);
			}
			set {
				if (BaseGet(index) != null) {
					BaseRemove(index);
				}
				BaseAdd(index, value);
			}
		}

		/// <summary>
		/// Gets or sets the named service element for the given name.
		/// </summary>
		/// <param name="name">The name of the named service element to get or set.</param>
		/// <returns>The named service element.</returns>
		public new AmqpConnection this[string name] {
			get {
				return (AmqpConnection) BaseGet(name);
			}
		}

		/// <summary>
		/// Gets the number of named service elements in this instance.
		/// </summary>
		public new int Count {
			get { return base.Count; }
		}

		public int IndexOf(AmqpConnection service) {
			return BaseIndexOf(service);
		}

		public void RemoveAt(int index) {
			BaseRemoveAt(index);
		}

		public void Add(AmqpConnection item) {
			BaseAdd(item);
		}

		public void Clear() {
			BaseClear();
		}

		public bool Contains(AmqpConnection item) {
			return BaseIndexOf(item) >= 0;
		}

		public void CopyTo(AmqpConnection[] array, int arrayIndex) {
			base.CopyTo(array, arrayIndex);
		}

		public new bool IsReadOnly {
			get { return false; }
		}

		public bool Remove(AmqpConnection item) {
			if (BaseIndexOf(item) >= 0) {
				BaseRemove(item);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Gets the key by which connection elements are mapped in the base class.
		/// </summary>
		/// <param name="service">The named service element to get the key from.</param>
		/// <returns>The key.</returns>
		private static string GetKey(AmqpConnection service) {
			return service.Name;
		}
	}

	/// <summary>
	/// Defines the details of an AmqpConnection to be used within client code.
	/// </summary>
	public class AmqpConnection : ConfigurationElement  {
		/// <summary>
		/// The name of this connection configuration.
		/// </summary>
		[ConfigurationProperty("name", IsRequired = true)]
		public string Name {
			get { return (string) this["name"]; }
			set { this["name"] = value; }
		}

		/// <summary>
		/// The server hostname to connect to.
		/// </summary>
		[ConfigurationProperty("server", IsRequired = true)]
		public string Server {
			get { return (string) this["server"]; }
			set { this["server"] = value; }
		}

		/// <summary>
		/// The port to connect to on the server.
		/// </summary>
		[ConfigurationProperty("port", IsRequired = false, DefaultValue = 5672)]
		public int Port {
			get { return (int) this["port"]; }
			set { this["port"] = value; }
		}

		/// <summary>
		/// Whether the underlying connection should be automatically closed.
		/// </summary>
		[ConfigurationProperty("autoclose", IsRequired = false, DefaultValue = true)]
		public bool AutoClose
		{
			get { return (bool)this["autoclose"]; }
			set { this["autoclose"] = value; }
		}
	}
}