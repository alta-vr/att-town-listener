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

TODO:
- Experiment with using the Windows SpeechRecognitionEngine class instead of Google's API
- Add an alias file which defines words to substitute for others, currently `me` => Your username and `everyone` => `*` (but theyre defined in code)
- Support phrase aliasing so that `go go danger chicken` => `spawn me spriggull`
- Support for inserting quotes, needed when spawning prefabs that contain spaces in their name
- Meta commands such as stop listening or a start word so that only phrases starting with the word are processed