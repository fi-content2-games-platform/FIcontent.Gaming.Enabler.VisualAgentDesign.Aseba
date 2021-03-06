// vim: ts=4:sw=4:noexpandtab
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Aseba
{
	// The deletage to process Aseba messages
	public delegate void ProcessMessage(ushort len, ushort source, ushort type, byte[] payload);
	// The delegate to notify a disconnection
	public delegate void Disconnected();
	
	// Implement a Dashel/Aseba client stream 
	public class Stream
	{
		// Socket this stream is connected to
		protected Socket socket = null;
		// The delegate to process message callback
		public ProcessMessage messageCallback;
		// The delegate to process disconnection
		public Disconnected disconnectionCallback;

		// low-level receive
		
		// Receive count bytes from a socket
		protected byte[] ReceiveAll(int count)
		{
			byte[] buffer = new byte[count];
			int done = 0;
			int left = count;
			while (left > 0)
			{
				int recvCount = socket.Receive(buffer,done,left,SocketFlags.None);
				if (recvCount == 0)
					throw new SocketException();
				done += recvCount;
				left -= recvCount;
			}
			return buffer;
		}

		// Receive a UInt16 from a socket
		protected ushort ReceiveUInt16LE()
		{
			byte[] buffer = ReceiveAll(2);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(buffer); 
			return BitConverter.ToUInt16(buffer, 0);
		}

		// Receive a UInt16 array from a socket
		protected ushort[] ReceiveUInt16ArrayLE(int len)
		{
			ushort[] array = new ushort[len];
			for (int i=0; i<len; ++i)
				array[i] = ReceiveUInt16LE();
			return array;
		}
		
		// low-level send
		
		// Send an array fully to the socket
		protected void SendAll(byte[] buffer)
		{
			int done = 0;
			int left = buffer.Length;
			while (left > 0)
			{
				int sentCount = socket.Send(buffer,done,left,SocketFlags.None);
				if (sentCount == 0)
					throw new SocketException();
				done += sentCount;
				left -= sentCount;
			}
		}
		
		// Send a UInt16 to the socket
		protected void SendUInt16LE(ushort value)
		{
			byte[] buffer = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(buffer); 
			SendAll(buffer);
		}

		// Create the stream
		public Stream()
		{
			// setup default delegates
			messageCallback = DefaultMessageCallback;
			disconnectionCallback = DefaultDisconnectionCallback;
		}
		
		// Attempt to connect to the target, throw a SocketException if connection fails
		public void Connect(String host = "", ushort port = 33333)
		{
			// Create a TCP/IP  socket.
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.ReceiveTimeout = 1000;
			
			// make sure we have a valid hostname
			if (String.IsNullOrEmpty(host))
				host = Dns.GetHostName();
			
			try
			{
				// Connect the socket to the remote endpoint. Through further any errors.
				socket.Connect(host, port);
				Console.WriteLine("Socket connected to {0}", socket.RemoteEndPoint.ToString());
			}
			catch (SocketException)
			{
				socket = null;
			}
		}
		
		// Return whether we are connected
		public bool Connected
		{
			get
			{
				if (socket != null)
					return socket.Connected;
				else
					return false;
			}
		}

		// Terminate connection
		public void Disconnect()
		{
			try
			{
				if (socket != null && socket.Connected)
				{
					// Release the socket.
					socket.Shutdown(SocketShutdown.Both);
					socket.Close();
				}
			}
			catch (SocketException)
			{
				socket = null;
			}
		}

		// Check for data on the network
		public void Step()
		{
			try
			{
				// note: the connected status is only updated on read, so most likely this program will not realise the server disconnected
				if (socket != null && socket.Connected)
				{
					// look if there is some pending data
					bool isData = socket.Poll(0,SelectMode.SelectRead);
					if (isData)
					{
						// receive an Aseba message
						ushort len = ReceiveUInt16LE();
						ushort source = ReceiveUInt16LE();
						ushort type = ReceiveUInt16LE();
						byte[] payload = ReceiveAll(len);
						messageCallback(len, source, type, payload);
					}
				}
			}
			catch (SocketException)
			{
				socket = null;
			}
		}
		
		// Send an Aseba message to the network
		public void SendAsebaMessage(ushort source, ushort type, ushort[] payload)
		{
			SendUInt16LE((ushort)(payload.Length * 2));
			SendUInt16LE(source);
			SendUInt16LE(type);
			for (var i = 0; i < payload.Length; ++i)
				SendUInt16LE(payload[i]);
		}

		// To be overridden by children
		public void DefaultMessageCallback(ushort len, ushort source, ushort type, byte[] payload)
		{
			Console.WriteLine(String.Format("Received message from {0} of type 0x{1:X4}, size {2} : {3}", source, type, len, String.Join(", ", Array.ConvertAll<byte, string>(payload, Convert.ToString))));
		}
		
		// To be overridden by children
		public void DefaultDisconnectionCallback()
		{
			Console.WriteLine("Network disconnected");
		}

	} // class Stream

} // namespace Aseba
