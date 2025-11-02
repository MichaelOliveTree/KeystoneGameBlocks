using System;
using System.Collections.Generic;
using Lidgren.Network;

    namespace Keystone.Network
    {
        public abstract class NetworkClientBase
        {
            public enum NetworkStatus
            {
                ConnectRequest,
                Authenticating,
                Authenticated,

                Connected,
                Connecting,
                Disconnected
            }

            protected Lidgren.Network.NetClient mGameClient;
            protected string mGameServerName;
            protected string mGameServerAddress = "localhost";
            protected int mGameServerPort = 0;

            protected Settings.Initialization mINI;
            protected int mUserID;
            protected string mUserName;
            protected string mPassword;

            protected NetworkStatus mStatus;
            protected int mMaxAuthenticationTries = 4;
            protected int mMaxConnectTries = 2;
            protected int mCurrentAuthenticationAttempts;
            protected int mCurrentConnectAttempts;

            // in a multi-edit environment could we potentially have a need to specify the sender of the message? 
            // let's not worry about that for now.  we'll assume single player is single player.
            public delegate void UserMessageReceived(int command, NetChannel channel, NetBuffer buffer);
            public delegate void UserMessageSending(KeyCommon.Messages.MessageBase message);
            public UserMessageReceived UserMessageReceivedHandler;
            public UserMessageSending UserMessageSendingHandler;

            public NetworkClientBase(Settings.Initialization ini)
            {
                mINI = ini;
                string sectionName = "network";
                mUserName = mINI.settingRead(sectionName, "username");
                mPassword = mINI.settingRead(sectionName, "password");

                mStatus = NetworkStatus.Disconnected;
                mCurrentAuthenticationAttempts = 0;
                mCurrentConnectAttempts = 0;
            }

            public NetworkStatus Status { get { return mStatus; } }

            public virtual void ConnectTo(string host, int port)
            {
                // TODO: maybe here is a better place to assign the connection.Tag
                System.Diagnostics.Debug.WriteLine("NetworkClientBase.ConnectTo() - Attempting connection to " + host + " at port " + port.ToString());
                mGameClient.Connect(host, port);
                if (mGameClient.ServerConnection.RemoteEndpoint.Address.ToString() == "127.0.0.1")
                    System.Diagnostics.Debug.WriteLine("NetworkClientBase.ConnectTo() - SUCCESS.");
            }

            public bool UsingLoopBack 
            { 
                get 
                {
                    if (mGameClient == null || mGameClient.ServerConnection == null ||
                        mGameClient.ServerConnection.RemoteEndpoint != null)
                        return mGameClient.ServerConnection.RemoteEndpoint.Address.ToString() == "127.0.0.1";

                    else return false;
                } 
            }
            /// <summary>
            /// 
            /// </summary>
            /// <remarks></remarks>
            public void Disconnect()
            {
                mStatus = NetworkStatus.Disconnected;
            }

            public abstract void Update();
            

            public void SendMessage(IRemotableType command)
            {
                KeyCommon.Messages.MessageBase msg = (KeyCommon.Messages.MessageBase)command;

                // for NotifyPlugin messages, we don't want to go over the wire.  Just send it straight to client side command handlers
                // NOTE: ProcessCommandCompleted() will handle the updates to the plugin so that we are guaranteed to have completed
                // the graphics loop prior to updating the plugin.  This ensures that temporary modifications to node appearances for example
                // during the graphics loop do not get read into the plugin.  Instead, only the final state after all viewport renders do we update the plugin.
                if (msg.CommandID == (int)KeyCommon.Messages.Enumerations.NotifyPlugin_NodeSelected)
                {
                    if (UserMessageReceivedHandler != null)
                    {
                        KeyCommon.Messages.NotifyPlugin_NodeSelected notify = (KeyCommon.Messages.NotifyPlugin_NodeSelected)msg;

                        int size = 4 + 8 + notify.NodeID.Length + notify.Typename.Length;
                        NetBuffer buffer = new NetBuffer(size);
                        buffer.Write((int)0);  // flags
                        buffer.Write((long)0); // UUID

                        buffer.Write(notify.NodeID);
                        buffer.Write(notify.Typename);
                        UserMessageReceivedHandler.Invoke((int)msg.CommandID, NetChannel.ReliableInOrder1, buffer);
                        return;
                    }
                }
                else if (msg.CommandID == (int)KeyCommon.Messages.Enumerations.NotifyPlugin_ProcessEventQueue)
                {
                    if (UserMessageReceivedHandler != null)
                    {
                        KeyCommon.Messages.NotifyPlugin_ProcessEventQueue notify = (KeyCommon.Messages.NotifyPlugin_ProcessEventQueue)msg;

                        int size = 4 + 8 + notify.NodeID.Length + notify.Typename.Length;
                        NetBuffer buffer = new NetBuffer(size);
                        buffer.Write((int)0);  // flags
                        buffer.Write((long)0); // UUID

                        UserMessageReceivedHandler.Invoke((int)msg.CommandID, NetChannel.ReliableInOrder1, buffer);
                        return;
                    }

                }
                // invoke callback prior to sending so this command can be placed
                // into undo/redo or unconfirmed list if applicable
                UserMessageSendingHandler(msg);

                mGameClient.SendMessage(command);
            }


            protected virtual void ProcessUserMessage(IncomingNetMessage message)
            {
                int command = message.Buffer.ReadInt32();
                
                // TODO: we should pass sequence channel and sequence number to
                // UserMessageReceivedHandler so that we can look up confirmation messages
                // by their sequence number and channel number.

                switch (command)
                {
                    // TODO: here instead of sending the message we just send command and buffer
                    //         is this ok?  Is there no direct peer to peer communications for instance where
                    //         we'd need to know a user's sender endpoint to verify?  or all coms just go thru server
                    //         is probably best because then we know the server verified the sender's authenticity
                    default:
                        if (UserMessageReceivedHandler != null)
                            UserMessageReceivedHandler.Invoke(command, message.Channel, message.Buffer);
                        else
                            System.Diagnostics.Debug.WriteLine("ProcessUserMessage() - Unsupported message type '" + command.ToString() + "'.");

                        break;
                }
            }
        }
    }

