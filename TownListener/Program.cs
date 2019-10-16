using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Speech.V1;
using Google.Apis.Auth.OAuth2;
using Grpc.Core;
using Grpc.Auth;
using WebSocketSharp;
using Alta.WebApi.Utility;
using NAudio.Wave;
using Alta.WebApi.Client;
using Newtonsoft.Json;
using System.IO;

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

		public string GoogleCredentialsPath { get; set; }

		const string ConfigFilePath = "config.json";

		public static void Load()
		{
			Current = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigFilePath));
		}

		public void Save()
		{
			File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
		}
	}

	class Program
	{
		public static void Main(string[] args)
		{
			Run().GetAwaiter().GetResult();
		}

		static async Task Run()
		{
			await LogIntoAlta();

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

				TownListener listener = new TownListener();

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
			WaveInEvent voiceRecorder;
			SpeechClient.StreamingRecognizeStream streamingCall;
			Dictionary<string, Func<string, string>> aliases = new Dictionary<string, Func<string, string>>();

			public TownListener()
			{
				aliases.Add("me", _ => AltaAPI.Client.UserClient.LoggedInUserInfo.Username);
				aliases.Add("everyone", _ => "*");
			}

			public async Task ConnectAndListen(int serverIdentifier)
			{
				await ConnectToServer(serverIdentifier);

				await ConnectToGoogleAPI();

				StartHandlingVoiceRecognition();

				StartRecordingVoice();

				Console.WriteLine("Start Speaking, say quit to stop the application");
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

			public async Task Stop()
			{
				Console.WriteLine("Stopping...");

				StopRecordingVoice();

				await streamingCall.WriteCompleteAsync();
			}

			async Task ConnectToGoogleAPI()
			{
				GoogleCredential cred = GoogleCredential.FromFile(Config.Current.GoogleCredentialsPath);

				Channel channel = new Channel(
					SpeechClient.DefaultEndpoint.Host,
					SpeechClient.DefaultEndpoint.Port,
					cred.ToChannelCredentials());

				SpeechClient speech = SpeechClient.Create(channel);

				streamingCall = speech.StreamingRecognize();

				// Write the initial request with the config.
				await streamingCall.WriteAsync(
					new StreamingRecognizeRequest()
					{
						StreamingConfig = new StreamingRecognitionConfig()
						{
							Config = new RecognitionConfig()
							{
								Encoding =
								RecognitionConfig.Types.AudioEncoding.Linear16,
								SampleRateHertz = 16000,
								LanguageCode = "en",
							},
							InterimResults = true,
						}
					});
			}

			void StopRecordingVoice()
			{
				voiceRecorder.DataAvailable -= HandleAudioData;
				voiceRecorder.StopRecording();
			}

			void StartHandlingVoiceRecognition()
			{
				Task.Run(async () =>
				{
					while (await streamingCall.ResponseStream.MoveNext())
					{
						foreach (var result in streamingCall.ResponseStream.Current.Results)
						{
							if (result.IsFinal)
							{
								var bestMatch = result.Alternatives.FirstOrDefault();

								HandleRecognisedVoice(bestMatch.Transcript);
							}
							else
							{
								//Console.WriteLine("Not final");
							}
						}
					}
				});
			}

			void HandleRecognisedVoice(string text)
			{
				string lowered = text.ToLowerInvariant();

				if (lowered == "quit")
				{
					_ = Stop();
					return;
				}

				string processed = PreProcessVoice(lowered);

				string message = "{{\"id\":{0},\"content\":\"{1}\"}}";

				Console.WriteLine("Raw Speech: {0} converted: {1}", text, processed);

				string data = string.Format(message, count++, processed);

				webSocket.Send(data);
			}

			string PreProcessVoice(string text)
			{
				bool wasModified = false;

				string[] words = text.Split(' ');

				for (int i = 0; i < words.Length; i++)
				{
					if (aliases.TryGetValue(words[i], out Func<string, string> convert))
					{
						words[i] = convert(words[i]);

						wasModified = true;
					}
				}

				if (!wasModified)
				{
					return text;
				}

				return string.Join(" ", words);
			}

			void StartRecordingVoice()
			{
				voiceRecorder = new WaveInEvent
				{
					DeviceNumber = 0,
					WaveFormat = new WaveFormat(16000, 1)
				};

				voiceRecorder.DataAvailable += HandleAudioData;

				voiceRecorder.StartRecording();
			}

			void HandleAudioData(object sender, WaveInEventArgs args)
			{
				streamingCall.WriteAsync(
					new StreamingRecognizeRequest()
					{
						AudioContent = Google.Protobuf.ByteString
							.CopyFrom(args.Buffer, 0, args.BytesRecorded)
					}).Wait();
			}
		}
	}
}