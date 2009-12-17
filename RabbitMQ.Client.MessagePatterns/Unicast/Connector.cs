using System;
using System.IO;
using System.Threading;
using RabbitMQ.Client.Framing.v0_8;
using RabbitMQ.Client.MessagePatterns.Configuration;

namespace RabbitMQ.Client.MessagePatterns.Unicast {
    class Connector : IConnector {

        protected int m_pause = 1000; //ms
        protected int m_attempts = 60;

        protected ConnectorState m_state = ConnectorState.Disconnected;
        protected ConnectionFactory m_factory;
        protected IConnection m_connection;
        protected ManualResetEvent m_closed = new ManualResetEvent(false);

        public int Pause {
            get { return m_pause; }
            set { m_pause = value; }
        }
        public int Attempts {
            get { return m_attempts; }
            set { m_attempts = value; }
        }

        public ConnectionFactory ConnectionFactory {
            get { return m_factory; }
        }

        public ConnectorState State {
            get { return m_state; }
        }

        public event ConnectorStateHandler StateChanged;

        public Connector(ConnectionFactory factory) {
            m_factory = factory;
        }

        public void Connect(ConnectionDelegate d) {
            IConnection conn = Connect();
            Exception e = Try(() => d(conn));
            if (e == null) return;
            if (!Reconnect(d))
                // TODO: This exception should probably be wrapped to
                // preserve the initial stack trace
                throw e;
        }

        public bool Try(Thunk t, ConnectionDelegate d) {
            Exception e = Try(t);
            if (e == null) return true;
            if (!Reconnect(d))
                // TODO: This exception should probably be wrapped to
                // preserve the initial stack trace
                throw e;
            return false;
        }

        public void Close() {
            m_closed.Set();
            lock (this) {
                if (m_connection != null) m_connection.Abort();
            }
        }

        void IDisposable.Dispose() {
            Close();
        }

        protected IConnection Connect() {
            lock (this) {
                if (m_connection != null) {
                    ShutdownEventArgs closeReason = m_connection.CloseReason;
                    if (closeReason == null) return m_connection;
                    if (!IsShutdownRecoverable(closeReason))
                        throw new Client.Exceptions.AlreadyClosedException
                            (closeReason);
                }
                OnStateChange(ConnectorState.Reconnecting);
                Exception e = null;
                for (int i = 0; i < Attempts; i++) {
                    e = Try(delegate {
                            m_connection = ConnectionFactory.CreateConnection();
                        });
                    if (e == null){
                        OnStateChange(ConnectorState.Connected);
                        return m_connection;
                    }
                    if (m_closed.WaitOne(Pause)) break;
                }
                throw (e);
            }
        }

        protected bool Reconnect(ConnectionDelegate d) {
            try {
                while (true) {
                    IConnection conn = Connect();
                    Exception e = Try(() => d(conn));
                    if (e == null) return true;
                }
            } catch (Exception) {
                // Ignore
            }

            OnStateChange(ConnectorState.Disconnected);
            return false;
        }

        protected static bool IsShutdownRecoverable(ShutdownEventArgs s) {
            return (s != null && s.Initiator != ShutdownInitiator.Application &&
                    ((s.ReplyCode == Constants.ConnectionForced) ||
                     (s.ReplyCode == Constants.InternalError) ||
                     (s.Cause is EndOfStreamException)));
        }

        protected static Exception Try(Thunk t) {
            try {
                t();
                return null;
            }
            catch (Client.Exceptions.AlreadyClosedException e) {
                if (IsShutdownRecoverable(e.ShutdownReason)) {
                    return e;
                }
                else {
                    throw;
                }
            }
            catch (Client.Exceptions.OperationInterruptedException e) {
                if (IsShutdownRecoverable(e.ShutdownReason)) {
                    return e;
                }
                else {
                    throw;
                }
            }
            catch (Client.Exceptions.BrokerUnreachableException e) {
                //TODO: we may want to be more specific here
                return e;
            }
            catch (System.IO.IOException e) {
                //TODO: we may want to be more specific here
                return e;
            }
        }

        protected void OnStateChange(ConnectorState newState) {
            m_state = newState;

            ConnectorStateHandler handler = StateChanged;
            if (handler != null) handler(this, m_state);
        }

    }
}
