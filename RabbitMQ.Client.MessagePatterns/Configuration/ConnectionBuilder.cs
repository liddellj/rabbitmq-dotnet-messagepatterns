using System.Configuration;

namespace RabbitMQ.Client.MessagePatterns.Configuration {
    public class ConnectionBuilder {
        private readonly ConnectionFactory _factory;
        private readonly AmqpTcpEndpoint[] _servers;

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

            _servers = new[] { new AmqpTcpEndpoint(protocol,
                                                   connectionConfig.Server,
                                                   connectionConfig.Port) };
        }

        /// <summary>
        /// Creates a new ConnectionBuilder that will use the given connection factory and connect to the given endpoints.
        /// </summary>
        /// <param name="factory">the connection factory to use</param>
        /// <param name="servers">the AMQP TCP endpoints that connections should be created to</param>
        public ConnectionBuilder(ConnectionFactory factory,
                                 params AmqpTcpEndpoint[] servers) {
            _factory = factory;
            _servers = servers;
        }

        /// <summary>
        /// Creates a new connection based on the builder's configuration.
        /// </summary>
        /// <returns>a new AMQP connection</returns>
        public IConnection CreateConnection() {
            return _factory.CreateConnection(_servers);
        }
    }
}
