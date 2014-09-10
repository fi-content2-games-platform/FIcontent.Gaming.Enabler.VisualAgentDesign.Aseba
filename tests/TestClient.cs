using Aseba;
using System;
using System.Threading;

public class TestClient
{
	public static int Main(String[] args)
	{
		Stream stream = new Stream();
		stream.Connect("localhost");
		while (true)
		{
			stream.Step();
			Thread.Sleep(100);
		}
		//return 0;
	}
}
