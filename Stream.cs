// vim: ts=4:sw=4:noexpandtab
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Aseba
{
	// The deletage to process Aseba messages
	public delegate void ProcessMessage(ushort len, ushort source, ushort type, byte[] payload);
	
	// Implement a Dashel/Aseba client stream 
	public class Stream
	{
		// Socket this stream is connected to
		protected Socket socket;
		// The delegate to process message callback
		public ProcessMessage messageCallback;

		// Receive count bytes from a socket
		protected byte[] ReceiveAll(int count)
		{
			byte[] buffer = new byte[count];
			int done = 0;
			int left = count;
			while (left > 0)
			{
				int recvCount = socket.Receive(buffer,done,left,SocketFlags.None);
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

		// Create the stream
		public Stream(String host = "", ushort port = 33333)
		{
			// setup default delegates
			messageCallback = DefaultMessageCallback;

			// make sure we have a valid hostname
			if (String.IsNullOrEmpty(host))
				host = Dns.GetHostName();

			// Connect to a remote device.
			// Establish the remote endpoint for the socket.
			// This example uses port 33333 on the local computer.
			IPHostEntry ipHostInfo = Dns.GetHostEntry(host);
			IPAddress ipAddress = ipHostInfo.AddressList[0];
			IPEndPoint remoteEP = new IPEndPoint(ipAddress,port);

			// Create a TCP/IP  socket.
			socket = new Socket(AddressFamily.InterNetwork, 
					SocketType.Stream, ProtocolType.Tcp );

			// Connect the socket to the remote endpoint. Catch any errors.
			socket.Connect(remoteEP);
			Console.WriteLine("Socket connected to {0}", socket.RemoteEndPoint.ToString());
		}

		// Terminate connection
		public void Disconnect()
		{
			// Release the socket.
			socket.Shutdown(SocketShutdown.Both);
			socket.Close();
		}

		// Check for data on the network
		public void Step()
		{
			// note: the connected status is only updated on read, so most likely this program will not realise the server disconnected
			if (socket.Connected)
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

		// To be overridden by children
		public void DefaultMessageCallback(ushort len, ushort source, ushort type, byte[] payload)
		{
			Console.WriteLine(String.Format("Received message from {0} of type 0x{1:X4}, size {2} : {3}", source, type, len, String.Join(", ", Array.ConvertAll<byte, string>(payload, Convert.ToString))));
		}

	} // class Stream

} // namespace Aseba
