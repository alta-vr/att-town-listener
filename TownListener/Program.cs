using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSocketSharp;
using Alta.WebApi.Utility;
using Alta.WebApi.Client;
using Newtonsoft.Json;
using System.IO;
using System.Threading;


namespace TownListener
{
	public class Config
	{
		public static Config Current
		{
			get
			{
				if (current == null)
				{
					Load();
				}

				return current;
			}

			private set
			{
				current = value;
			}
		}

		static Config current;

		public string Username { get; set; }

		public string Password { get; set; }

		public string FilePath { get; set; }


		const string ConfigFilePath = "config.json";

		public static void Load()
		{
			Current = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFilePath));
		}

		public void Save()
		{
			File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented));
		}
	}

	class Program
	{
		public static System.Timers.Timer timer;

		public static TownListener listener;

		public static string lastCommand = "";


		public static void Main(string[] args)
		{			
			Run().GetAwaiter().GetResult();
		}

		private static void OnChanged(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType != WatcherChangeTypes.Changed)
			{
				return;
			}


			Thread.Sleep(400);

			string fullFile = File.ReadAllText(e.FullPath);

			if (lastCommand != fullFile)
			{
				lastCommand = fullFile;

				Console.WriteLine($"Changed: {e.FullPath}");

				listener.HandleServerCommand(fullFile);
			}


		}
		private static void OnError(object sender, System.IO.ErrorEventArgs e) => PrintException(e.GetException());

		private static void PrintException(Exception ex)
		{
			if (ex != null)
			{
				Console.WriteLine($"Message: {ex.Message}");
				Console.WriteLine("Stacktrace:");
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine();
				PrintException(ex.InnerException);
			}
		}

		private static void onTimer(Object Source, System.Timers.ElapsedEventArgs e)
        {
			listener.HandleServerCommand("player message pixelbeard \'Server is Still Alive\' 6");

		}
		static async Task Run()
		{
			await LogIntoAlta();

			timer = new System.Timers.Timer(1200000);

			timer.Elapsed += onTimer;
			timer.AutoReset = true;
			timer.Enabled = true;


			string path = Config.Current.FilePath.Replace("%20", " ");
			FileSystemWatcher watcher = new FileSystemWatcher(path);

			//watcher.NotifyFilter = NotifyFilters.Attributes
			//					 | NotifyFilters.CreationTime
			//					 | NotifyFilters.DirectoryName
			//					 | NotifyFilters.FileName
			//					 | NotifyFilters.LastAccess
			//					 | NotifyFilters.LastWrite
			//					 | NotifyFilters.Security
			//					 | NotifyFilters.Size;

			watcher.NotifyFilter = NotifyFilters.LastWrite;

				watcher.Changed += OnChanged;
				watcher.Error += OnError;

				watcher.Filter = "*.txt";
				watcher.IncludeSubdirectories = true;
				watcher.EnableRaisingEvents = true;
			
			while (true)
			{
				var servers = (await AltaAPI.Client.ServerClient.GetOnlineServersAsync()).ToArray();

				for (int i = 0; i < servers.Length; i++)
				{
					Alta.WebApi.Models.GameServerInfo server = servers[i];
					Console.WriteLine("{0}: {1} - {2}", i, server.Identifier, server.Name);
				}

				Console.WriteLine("Which server do you want to connect to?");

				var serverNumber = int.Parse(Console.ReadLine());

				if (serverNumber < 0)
				{
					serverNumber = -1;
				}
				else
				{
					serverNumber = servers[serverNumber].Identifier;
				}

				listener = new TownListener();

				try
				{
					await listener.ConnectAndListen(serverNumber);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
		}

		static async Task LogIntoAlta()
		{
			string username = Config.Current.Username;
			string pw = Config.Current.Password;

			Console.WriteLine("Logging in as: {0}", username);

			if (pw.Length < 64)
			{
				pw = LoginUtilities.HashString(pw);
			}

			await AltaAPI.Client.LoginAsync(username, pw);

			Console.WriteLine("Logged in");

			Config.Current.Password = pw;

			Config.Current.Save();
		}

		public static class AltaAPI
		{
			public static IHighLevelApiClient Client { get; } = HighLevelApiClientFactory.CreateHighLevelClient();
		}

		public class TownListener
		{
			static int count;

			WebSocket webSocket;


			CancellationTokenSource cancellation = new CancellationTokenSource();


			public async Task ConnectAndListen(int serverIdentifier)
			{
				await ConnectToServer(serverIdentifier);

				Console.WriteLine("Listening to file change for commands");

				await Task.Delay(-1, cancellation.Token);
			}

			async Task ConnectToServer(int serverIdentifier)
			{
				var joinResult = await AltaAPI.Client.ServerClient.ConnectConsole(serverIdentifier, false, false);

				Console.WriteLine("Got Connection details, Allowed: {0}", joinResult.IsAllowed);

				if (!joinResult.IsAllowed)
				{
					throw new Exception("NOT ALLOWED");
				}

				string url = $"ws://{joinResult.ConnectionInfo.Address.ToString()}:{joinResult.ConnectionInfo.WebSocketPort}";

				//string url = $"ws://{joinResult.ConnectionInfo.Address.ToString()}:{7767}";

				webSocket = new WebSocket(url);

				//webSocket.OnMessage += (sender, e) => Console.WriteLine("Server: " + e.Data);

				webSocket.Connect();

				webSocket.Send(joinResult.Token.Write());
			}


			public void HandleServerCommand(string text)
			{
				string lowered = text.ToLowerInvariant();

				if (lowered == "quit")
				{
					Stop();

					return;
				}

				string processed = lowered;

				Console.WriteLine("Sent Command: {0}", processed);

				string message = "{{\"id\":{0},\"content\":\"{1}\"}}";

				string data = string.Format(message, count++, processed);

				webSocket.Send(data);
			}

			public void Stop()
			{
				Console.WriteLine("Stopping...");


				cancellation.Cancel();
			}
		}
	}
}