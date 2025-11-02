using System;
using System.Collections.Generic;
using System.Diagnostics;
using Authentication;
using Keystone;
using Keystone.IO;
using Keystone.Scene;
using Keystone.Simulation;
using Lidgren.Network;
using Settings;
using Keystone.Elements;
using KeyCommon.Messages;
using Keystone.Traversers;
using Keystone.Resource;
using Keystone.Entities;
using Keystone.Appearance;


namespace Keystone.Network
{
    public class LoopbackServer
    {
        public delegate void UserMessageReceivedLoopback(Lidgren.Network.NetConnectionBase connection, int command, NetChannel channel, NetBuffer buffer);

        public UserMessageReceivedLoopback UserMessageReceivedLoopbackHandler;


        // Unlike master servers that are usually just 1 (at least in a region of the world) there can be
        // many game servers so the "username" is not the service name, but is like the password
        // the user's registered name.  Only localhost (loopback) game servers do not need a username or password since
        // they dont have to authenticate

        private NetTCPServer m_server;
        private Algorithm m_hashAlgorithm = Algorithm.SHA256;
        private string m_serverName = "Loopback Server 1"; // unique to every game server
        private const string m_serviceName = "KeyGameServerSvc";
        private const string m_servicePassword = "KeyGameServerSvc_Passw0rd"; //  same password for user created game servers as their client registration password
        
        private string m_localAddress; 
        private int m_localPort;


        private Keystone.Core _core;
        private SceneManagerBase _sceneManager;
        private static string _configFile, _configPath;

        public static string DATA_PATH;
        public static string COMPANY_NAME = "SciFiCommand";
        public static string CONFIG_FILENAME = "KGBServer.config";


        // TODO: i think we should make the loopback server a seperate dll?
        /// <summary>
        /// No authentication validation of clients required unless our loopback will also accept external connections for 
        /// user hosted multiplayer matches. 
        /// TODO: verify we can set up our listening for both scenarios
        /// </summary>
        public LoopbackServer(string hostAddress, int hostPort, string configBasePath)
        {
            DATA_PATH = configBasePath + "data\\";
        	m_localAddress = hostAddress ;
        	m_localPort = hostPort;

            // loopback server does not have to authenticate 
            // or connect to a MasterServer.  We can immediately start the actual game server and begin accepting connections

            // then we can start the game server
            // and can start to accept client connections
            NetConfiguration config = new NetConfiguration(m_serviceName);

            // todo: are the buffer sizes being set according to the KGBServer.config file?
            m_server = new NetTCPServer(config); // use TCP for loopback
            //m_server.SetMessageTypeEnabled(NetMessageType.ConnectionApproval, false);
            m_server.Bind(m_localAddress, m_localPort);
        }

        /// <summary>
        /// Scene is shared by the client for loopback server and so is Core, Repository, Pager, etc.
        /// </summary>
        /// <param name="scene"></param>
        public void SetScene(Scene.Scene scene)
        {
            //_scene = scene;
        }

       public NetTCPServer Server { get { return m_server; } }
        /// <summary>
		/// 
		/// </summary>
        public void Update()
        {

            NetMessage[] messages;
            
            // pump netserver until shutdown.  
            m_server.Heartbeat(); 
            messages = m_server.GetMessages();

            if (messages != null && messages.Length > 0)
            {
                foreach (NetMessage msg in messages)
                {
                    switch (msg.Type)
                    {
                        // our game server processes received messages as they arrive
                        // but it runs the simulation at a fixed frequency
                        // approval for when clients are trying to connect with this GameServer
                        case NetMessageType.ConnectionApproval:
                            m_server.ApproveConnection(((IncomingNetMessage)msg).m_sender);
                            Debug.WriteLine("LoopbackServer:Update() - Client Approved.");
                            break;
                        case NetMessageType.Data:
                            
                            // loopback only runs a single simulation.  For consistancy though we will use a SimHost
                            //Debug.WriteLine("LoopbackServer:Update() - Data received...");
                            //mSimHost.MessageProc(messages);
                            // TODO: for now we dont need to implement any extra classes.  We'll just call a MessageProc
                            // that is in LoopbackServer
                            ProcessUserMessage((IncomingNetMessage)msg);
                            break;
                        case NetMessageType.BadMessageReceived :
                            // one type of bad message is if the app ident when first connecting is incorrect
                            Debug.WriteLine("LoopbackServer:Update() - BAD DATA RECVD." + msg.Buffer.ReadString ());
                            break;
                        case NetMessageType.DebugMessage:
                        case NetMessageType.VerboseDebugMessage:
                            Debug.WriteLine("LoopbackServer:Update() - DEBUG: " + msg.Buffer.ReadString());
                            break;
                    }
                }
            }

        }

        private NetConnectionBase mLoopbackClientConnection;
        // TODO: Simulation.cs should have a Loopback Server implementation that emulates independant server
        //       that can know for example, when to spawn Entities for the client side.
        // TODO: loopback message processing should be in App.Exe
        //       else we cannot properly handle GameObjects which are only
        //       EXE and Script accessible.
        private void ProcessUserMessage(IncomingNetMessage message)
        {
            // all messages sent here are data messages, but read first 4 bytes to get the type
            mLoopbackClientConnection = message.m_sender;
            //Player player = (Player)message.m_sender.Tag;
            //System.Diagnostics.Debug.Assert(player != null);

            KeyCommon.Messages.Enumerations command  = (KeyCommon.Messages.Enumerations)message.Buffer.ReadInt32();
            Lidgren.Network.NetConnectionBase connection = message.m_sender;

            UserMessageReceivedLoopbackHandler(connection, (int)command, message.Channel, message.Buffer);

            
        }

        
    }
}
