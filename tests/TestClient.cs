using Aseba;
using System;
using System.Threading;

public class TestClient
{
	public static int Main(String[] args)
	{
		Stream stream = new Stream("localhost");
		while (true)
		{
			stream.Step();
			Thread.Sleep(100);
		}
		stream.Disconnect();
		return 0;
	}
}
