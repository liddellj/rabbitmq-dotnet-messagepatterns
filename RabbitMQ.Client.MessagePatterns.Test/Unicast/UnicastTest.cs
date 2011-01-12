using NUnit.Framework;
using RabbitMQ.Client.MessagePatterns.Configuration;
using RabbitMQ.Client.MessagePatterns.Test.Support;

namespace RabbitMQ.Client.MessagePatterns.Test.Unicast {
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using RabbitMQ.Client.MessagePatterns.Unicast;
    using EndOfStreamException = System.IO.EndOfStreamException;

    public delegate SetupDelegate MessagingClosure(IMessaging m);

    //NB: For testing we declare all resources as auto-delete and
    //non-durable, to avoid manual cleanup. More typically the
    //resources would be non-auto-delete/non-exclusive and durable, so
    //that they survives server and client restarts.
    [TestFixture]
    public class UnicastTest {
        private ConnectionFactory _factory;
        private AmqpTcpEndpoint _server;
        private ConnectionBuilder _builder;

        public UnicastTest() {
            _factory = new ConnectionFactory();
            _server = new AmqpTcpEndpoint();
            _factory.Endpoint = _server;
            _builder = new ConnectionBuilder(_factory);
        }

        protected void DeclareExchange(IModel m, string name, string type) {
            m.ExchangeDeclare(name, type, false, true, null);
        }

        protected void DeclareQueue(IModel m, string name) {
            m.QueueDeclare(name, false, false, true, null);
        }

        protected void BindQueue(IModel m, string q, string x, string rk) {
            m.QueueBind(q, x, rk, false, null);
        }

        [Test]
        public void TestDirect() {
            using (IConnector conn = Factory.CreateConnector(_builder)) {
                //create two parties
                IMessaging foo = Factory.CreateMessaging();
                foo.Connector = conn;
                foo.Identity = "foo";
                foo.Sent += TestHelper.Sent;
                foo.SetupReceiver += delegate(IModel channel) {
                    DeclareQueue(channel, foo.Identity);
                };
                foo.Init();
                IMessaging bar = Factory.CreateMessaging();
                bar.Connector = conn;
                bar.Identity = "bar";
                bar.Sent += TestHelper.Sent;
                bar.SetupReceiver += delegate(IModel channel) {
                    DeclareQueue(channel, bar.Identity);
                };
                bar.Init();

                //send message from foo to bar
                IMessage mf = foo.CreateMessage();
                mf.Body = TestHelper.Encode("message1");
                mf.To = "bar";
                foo.Send(mf);

                //receive message at bar and reply
                IReceivedMessage rb = bar.Receive();
                TestHelper.LogMessage("recv", bar, rb);
                IMessage mb = bar.CreateReply(rb);
                mb.Body = TestHelper.Encode("message2");
                bar.Send(mb);
                bar.Ack(rb);

                //receive reply at foo
                IReceivedMessage rf = foo.Receive();
                TestHelper.LogMessage("recv", foo, rf);
                foo.Ack(rf);
            }
        }

        [Test]
        public void TestRelayed() {
            MessagingClosure senderSetup = delegate(IMessaging m) {
                return delegate(IModel channel) {
                    DeclareExchange(channel, m.ExchangeName, "fanout");
                };
            };
            MessagingClosure receiverSetup = delegate(IMessaging m) {
                return delegate(IModel channel) {
                    DeclareExchange(channel, "out", "direct");
                    DeclareQueue(channel, m.QueueName);
                    BindQueue(channel, m.QueueName, "out", m.QueueName);
                };
            };
            TestRelayedHelper(senderSetup, receiverSetup, _builder);
        }

        protected void TestRelayedHelper(MessagingClosure senderSetup,
                                         MessagingClosure receiverSetup,
                                         ConnectionBuilder builder) {
            using (IConnector conn = Factory.CreateConnector(builder),
                   relayConn = Factory.CreateConnector(builder)) {

                //create relay 
                IMessaging relay = Factory.CreateMessaging();
                relay.Connector = relayConn;
                relay.Identity = "relay";
                relay.ExchangeName = "out";
                relay.SetupSender += delegate(IModel channel) {
                    DeclareExchange(channel, relay.ExchangeName, "direct");
                };
                relay.SetupReceiver += delegate(IModel channel) {
                    DeclareExchange(channel, "in", "fanout");
                    DeclareQueue(channel, relay.QueueName);
                    BindQueue(channel, relay.QueueName, "in", "");
                };
                relay.Init();

                //run relay
                new System.Threading.Thread
                    (delegate() {
                    //receive messages and pass it on
                    IReceivedMessage r;
                    try {
                        while (true) {
                            r = relay.Receive();
                            relay.Send(r);
                            relay.Ack(r);
                        }
                    }
                    catch (EndOfStreamException) {
                    }
                    catch (AlreadyClosedException) {
                    }
                    catch (OperationInterruptedException) {
                    }
                }).Start();

                //create two parties
                IMessaging foo = Factory.CreateMessaging();
                foo.Connector = conn;
                foo.Identity = "foo";
                foo.SetupSender += senderSetup(foo);
                foo.SetupReceiver += receiverSetup(foo);
                foo.ExchangeName = "in";
                foo.Sent += TestHelper.Sent;
                foo.Init();
                IMessaging bar = Factory.CreateMessaging();
                bar.Connector = conn;
                bar.Identity = "bar";
                bar.SetupSender += senderSetup(bar);
                bar.SetupReceiver += receiverSetup(bar);
                bar.ExchangeName = "in";
                bar.Sent += TestHelper.Sent;
                bar.Init();

                //send message from foo to bar
                IMessage mf = foo.CreateMessage();
                mf.Body = TestHelper.Encode("message1");
                mf.To = "bar";
                foo.Send(mf);

                //receive message at bar and reply
                IReceivedMessage rb = bar.Receive();
                TestHelper.LogMessage("recv", bar, rb);
                IMessage mb = bar.CreateReply(rb);
                mb.Body = TestHelper.Encode("message2");
                bar.Send(mb);
                bar.Ack(rb);

                //receive reply at foo
                IReceivedMessage rf = foo.Receive();
                TestHelper.LogMessage("recv", foo, rf);
                foo.Ack(rf);
            }

        }

        [Test]
        public void TestPreconfigured() {
            //The idea here is to simulate a setup where are the
            //resources are pre-declared outside the Unicast messaging
            //framework.
            using (IConnection conn = _builder.CreateConnection()) {
                IModel ch = conn.CreateModel();

                //declare exchanges
                DeclareExchange(ch, "in", "fanout");
                DeclareExchange(ch, "out", "direct");

                //declare queue and binding for relay
                DeclareQueue(ch, "relay");
                BindQueue(ch, "relay", "in", "");

                //declare queue and binding for two participants
                DeclareQueue(ch, "foo");
                BindQueue(ch, "foo", "out", "foo");
                DeclareQueue(ch, "bar");
                BindQueue(ch, "bar", "out", "bar");
            }
            //set up participants, send some messages
            MessagingClosure dummy = delegate(IMessaging m) {
                return delegate(IModel channel) { };
            };
            TestRelayedHelper(dummy, dummy, _builder);
        }

    }
}
