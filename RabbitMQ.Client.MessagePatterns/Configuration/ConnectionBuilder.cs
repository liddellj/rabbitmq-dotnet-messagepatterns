using System.Configuration;

namespace RabbitMQ.Client.MessagePatterns.Configuration {
    public class ConnectionBuilder {
        private readonly ConnectionFactory _factory;

        /// <summary>
        /// Creates a new ConnectionBuilder, loading configuration from the given named section/connection name pair.
        /// </summary>
        /// <param name="sectionName">the name of the section that contains amqp connection settings</param>
        /// <param name="connectionName">the name of the connection to use from the settings block</param>
        public ConnectionBuilder(string sectionName, string connectionName) {
            _factory = new ConnectionFactory();

            var protocol = Protocols.FromEnvironment();
            var settingsSection = (AmqpConnectionSettingsSection)
                ConfigurationManager.GetSection(sectionName);
            var connectionConfig = settingsSection.Connections[connectionName];

            _factory.Endpoint = new AmqpTcpEndpoint(protocol,
                                                    connectionConfig.Server,
                                                    connectionConfig.Port);
        }

        /// <summary>
        /// Creates a new ConnectionBuilder that will use the given connection factory.
        /// </summary>
        /// <param name="factory">the connection factory to use</param>
        public ConnectionBuilder(ConnectionFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Creates a new connection based on the builder's configuration.
        /// </summary>
        /// <returns>a new AMQP connection</returns>
        public IConnection CreateConnection() {
            return _factory.CreateConnection();
        }
    }
}
