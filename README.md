# Town-Listener
Voice Commands via the Websocket console

Uses Google Voice Speech to Text API to send voice commands to a Township Tale Server.

Create config.json file in the same directory as the exe containing
```json
{
	"Username":"user",
	"Password":"pw",
	"GoogleCredentialsPath":"C:\\Path\\to\\google-credentials.json"
}
```

Follow steps 3 and 4 in this guide to get your GCP credentials
https://codelabs.developers.google.com/codelabs/cloud-speech-text-csharp/index.html?index=..%2F..index#3

Message Timo in the ATT-Meta Discord for any questions