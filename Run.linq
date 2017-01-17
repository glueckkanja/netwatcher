<Query Kind="Program">
  <Reference Relative="bin\Debug\net452\netwatcher.dll">C:\Users\janberdel\Code\OSS\netwatcher\bin\Debug\net452\netwatcher.dll</Reference>
  <Namespace>NetWatcher</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

Stopwatch sw = Stopwatch.StartNew();

async Task Main()
{
	using (var watcher = new NetworkWatcher())
	{
		watcher.ConnectionsChanged += (sender, e) =>
		{
			watcher.Connections.Dump(1);
			watcher.Interfaces.Dump(1);
		};
		
		Time("Ctor");

		await watcher.InitAsync();
		Time("InitAsync");

		watcher.Connections.Dump(1);
		watcher.Interfaces.Dump(1);
		
		"waiting for input".Dump();
		Console.ReadLine();
		
		sw.Restart();
	}

	Time("Dispose");
}

// Define other methods and classes here
void Time(string msg)
{
	sw.ElapsedMilliseconds.Dump(msg);
	sw.Restart();
}
