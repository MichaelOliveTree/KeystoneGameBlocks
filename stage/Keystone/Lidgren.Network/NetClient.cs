/* Copyright (c) 2008 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace Lidgren.Network
{
	/// <summary>
	/// A client which can connect to a single NetServer
	/// </summary>
	public abstract class NetClient : NetBase
	{
		protected NetConnectionBase m_serverConnection;
        internal NetNATIntro m_NATIntro;
        protected byte[] m_localHailData; // temporary container until NetConnectionBase has been created
        protected IPEndPoint m_connectEndpoint;
        protected object m_startLock;


		/// <summary>
		/// Creates a new NetClient
		/// </summary>
		public NetClient(NetConfiguration config, ProtocolType protocol)
            : base(config, protocol)
		{
			m_startLock = new object();
            m_NATIntro = new NetNATIntro(m_messageManager , m_bufferManager);
		}

        /// <summary>
        /// Gets the connection to the server
        /// </summary>
        public NetConnectionBase ServerConnection { get { return m_serverConnection; } }

        /// <summary>
        /// Gets the status of the connection to the server
        /// </summary>
        public NetConnectionStatus Status
        {
            get
            {
                if (m_serverConnection == null)
                    return NetConnectionStatus.Disconnected;
                return m_serverConnection.Status;
            }
        }

		/// <summary>
		/// Connects to the specified host on the specified port; passing no hail data
		/// </summary>
		public void Connect(string host, int port)
		{
		    byte[] data = null;
			Connect(host, port, data);
		}

		/// <summary>
		/// Connects to the specified host on the specified port; passing hailData to the server
		/// </summary>
		public void Connect(string host, int port, byte[] hailData)
		{
			IPAddress ip = NetUtility.Resolve(host);
			if (ip == null)
				throw new NetException("Unable to resolve host");
			Connect(new IPEndPoint(ip, port), hailData);
		}

        public void Connect (string host, int port, string hailData)
        {
            NetBuffer buf = new NetBuffer(hailData); // TODO: use new NetBuffer() until after we fix m_bufferManager.CreateBuffer(hailData);
            Connect(host, port, buf.Data);
        }

		/// <summary>
		/// Connects to the specified remove endpoint
		/// </summary>
		public void Connect(IPEndPoint remoteEndpoint)
		{
		    byte[] data = null;
			Connect(remoteEndpoint, data);
		}

        public void Connect (IPEndPoint remoteEndpoint, string hailData)
        {
            NetBuffer buf = new NetBuffer(hailData);  // TODO: use new NetBuffer() until after we fix m_bufferManager.CreateBuffer(hailData); 
            Connect(remoteEndpoint, buf.Data);
        }

	    /// <summary>
	    /// Connects to the specified remote endpoint; passing hailData to the server
	    /// </summary>
	    public abstract void Connect(IPEndPoint remoteEndpoint, byte[] hailData);
		
		/// <summary>
		/// Initiate explicit disconnect
		/// </summary>
		public virtual void Disconnect(string message)
		{
			if (m_serverConnection == null || m_serverConnection.Status == NetConnectionStatus.Disconnected)
			{
				m_messageManager.LogWrite("Disconnect - Not connected!");
				return;
			}
			m_serverConnection.Disconnect(message, 1.0f, true, false);
		}

		/// <summary>
		/// Sends unsent messages and reads new messages from the wire
        /// NOTE: This method is override by NetTCPClient and is never called 
        /// whereas NetUDPClient overrides but then calls base.Heartbeat()
		/// </summary>
		public override void Heartbeat()
		{
            base.Heartbeat();
            
			if (m_serverConnection == null) return;

	        m_serverConnection.Heartbeat(m_heartbeatCounter.LastCount); // will send unsend messages etc.
            m_messageManager.SendOutgoing(m_heartbeatCounter.LastCount);

            NetBuffer buffer = new NetBuffer(m_config.ReceiveBufferSize);  //m_bufferManager.CreateBuffer(m_config.ReceiveBufferSize);;
		    EndPoint remoteEndpoint = m_serverConnection.m_remoteEndPoint;
            IPEndPoint ipsender = (IPEndPoint)remoteEndpoint;
            
            try
            {
                // read all data on the socket
                while (true) // TODO: i dont think this needs to be a loop at all... it forces it to throw an exception after all bytes are read to get out of the loop!!
                {
                    if (m_socket == null || m_socket.Socket == null || m_socket.Socket.Available < 1)
                        return;

                    buffer.Reset();
                    int bytesReceived = 0;
                    try
                    {
                        // TODO: Hypno - i dont like how this buffer is being used to ReceiveFrom, that represents a copy from a socket
                        // buffer when that socket already has a buffer that we should be able to use directly and simply replace the socket's
                        // existing buffer for it's next read
                        bytesReceived = m_socket.Socket.ReceiveFrom(buffer.Data, 0, buffer.Data.Length, SocketFlags.None, ref remoteEndpoint);
                        buffer.LengthBits = bytesReceived * 8;
                        if (bytesReceived > 0)
                            m_serverConnection.m_statistics.CountPacketReceived(bytesReceived, m_heartbeatCounter.LastCount);
                    }
                    catch (SocketException sex)
                    {
                               
                        if (sex.ErrorCode ==  10054)
                        {
                            // forcibly closed; but m_senderRemote is unreliable, we can't trust it!
                            //NetConnectionBase conn = GetConnection((IPEndPoint)m_senderRemote);
                            //HandleConnectionForciblyClosed(conn, sex);
                            return;
                        }
            
                        // remarks from msdn
                        // Socket.ReceiveFrom Method (Byte[], Int32, Int32, SocketFlags, EndPoint)
                        // http://msdn.microsoft.com/en-us/library/kbfwcz73.aspx
                         //socketFlags is not a valid combination of values.
                        //-or-
                        //The LocalEndPoint property was not set.
                        //-or-
                        // With connectionless protocols, ReceiveFrom will read the first enqueued datagram received into the
                        // local network buffer. If the datagram you receive is larger than the size of buffer, the ReceiveFrom 
                        // method will fill buffer with as much of the message as is possible, and throw a SocketException.
                        // If you are using an unreliable protocol, the excess data will be lost. 
                        // If no data is available for reading, the ReceiveFrom method will block until data is available.
                        // If you are in non-blocking mode, and there is no data available in the in the protocol stack buffer, 
                        // the ReceiveFrom method will complete immediately and throw a SocketException. You can use 
                        // the Available property to determine if data is available for reading. When Available is non-zero,
                        // retry the receive operation.
                        // no good response to this yet
                        return;
                    }
                    DataReceived(buffer, bytesReceived , ipsender);
                }
            }

            catch (Exception ex)
            {
                throw new NetException("ReadPacket() exception", ex);
            }
		}


        public void SendMessage(byte[] data, NetChannel channel)
        {
            NetBuffer buffer = NetBuffer.FromData(data); // TODO: problem hwere is if bufferrecycling is enabled, this .FromData() call bypasses it

            m_serverConnection.SendMessage(buffer, channel);
        }

        public void SendMessage(IRemotableType remoteable)
        {
            SendMessage(remoteable, remoteable.Channel);
        }

        public void SendMessage(IRemotableType remoteable, NetChannel channel)
        {
            m_serverConnection.SendMessage(remoteable, channel);
        }

        public void SendMessage(byte[] data)
        {
            SendMessage(data, NetChannel.Unreliable);
        }

        public void SendMessage(string[] messages, NetChannel channel)
        {
            NetBuffer buffer = new NetBuffer(); // TODO: use new NetBuffer() until after we fix .CreateBuffer() managemer m_bufferManager.CreateBuffer(message);
            for (int i = 0; i < messages.Length; i++)
                buffer.Write(messages[i]);

            m_serverConnection.SendMessage(buffer, channel);
        }

        public void SendMessage(string message, NetChannel channel)
        {
            NetBuffer buffer =  new NetBuffer(message);  // TODO: use new NetBuffer() until after we fix .CreateBuffer() managemer m_bufferManager.CreateBuffer(message);
            m_serverConnection.SendMessage(buffer, channel);
        }

        public void SendMessage(string[] messages)
        {
            SendMessage(messages, NetChannel.Unreliable);
        }

        public void SendMessage(string message)
        {
            SendMessage(message, NetChannel.Unreliable);
        }

		/// <summary>
		/// Returns ServerConnection if passed the correct endpoint
		/// </summary>
        public override NetConnectionBase GetConnection(IPEndPoint remoteEndpoint)
		{
			if (m_serverConnection != null && m_serverConnection.RemoteEndpoint.Equals(remoteEndpoint))
				return m_serverConnection;
			return null;
		}

		/// <summary>
		/// Emit a discovery signal to your subnet
		/// </summary>
		public void DiscoverLocalServers(int serverPort)
		{
			m_discovery.SendDiscoveryRequest(new IPEndPoint(IPAddress.Broadcast, serverPort), true);
		}

		/// <summary>
		/// Emit a discovery signal to your subnet; polling every 'interval' second until 'timeout' seconds is reached
		/// </summary>
		public void DiscoverLocalServers(int serverPort, float interval, float timeout)
		{
			m_discovery.SendDiscoveryRequest(new IPEndPoint(IPAddress.Broadcast, serverPort), true, interval, timeout);
		}
		
		/// <summary>
		/// Emit a discovery signal to a single host
		/// </summary>
		public void DiscoverKnownServer(string host, int serverPort)
		{
			IPAddress address = NetUtility.Resolve(host);
			IPEndPoint ep = new IPEndPoint(address, serverPort);

			m_discovery.SendDiscoveryRequest(ep, false);
		}

		/// <summary>
		/// Emit a discovery signal to a host or subnet
		/// </summary>
		public void DiscoverKnownServer(IPEndPoint address, bool useBroadcast)
		{
			m_discovery.SendDiscoveryRequest(address, useBroadcast);
		}

		internal override void HandleConnectionForciblyClosed(NetConnectionBase connection, SocketException sex)
		{
			if (m_serverConnection == null)
				return;

			if (m_serverConnection.Status == NetConnectionStatus.Connecting)
			{
				// failed to connect; server is not listening
				m_serverConnection.Disconnect("Failed to connect; server is not listening", 0, false, true);
				return;
			}

			m_serverConnection.Disconnect("Connection forcibly closed by server", 0, false, true);
			return;
		}

		/// <summary>
		/// Disconnects from server and closes socket
		/// </summary>
		protected override void PerformShutdown(string reason)
		{
			if (m_serverConnection != null)
				m_serverConnection.Disconnect(reason, 0, true, true);

			base.PerformShutdown(reason);
		}
	}
}
