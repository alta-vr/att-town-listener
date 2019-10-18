# Town-Listener
Voice Commands via the Websocket console

Uses the Windows Speech to Text API to send voice commands to a Township Tale Server.

Written in C#. Use Visual Studio to build it or download the latest release

Create config.json file in the same directory as the exe containing
```json
{
	"Username":"user",
	"Password":"pw",
	"GrammarFilePath": "grammar.xml",
    "ConsoleMode": false,
    "Language": "en-US"
}
```

**Options:**
GrammarFilePath: The file path of the grammar file. This defines all possible phrases that can be recognized
ConsoleMode: Type your commands in the console instead of using speech to text (useful for debugging)
Language: The language to use when recognizing speech. This needs to be installed on your computer first. Go to Windows Settings > Time & Language > Language and "Add a preferred Language" after its added make sure it has the voice voice recognition symbol

Message Timo in the ATT-Meta Discord for any questions

**TODO:**
- Automatically generate the grammar file from the command structure returned by the server
- Accept variables and numbers in the grammar
- Add an alias file which defines words to substitute for others, currently `me` => Your username and `everyone` => `*` (but theyre defined in code)
- Support phrase aliasing so that `go go danger chicken` => `spawn me spriggull`
- Support for inserting quotes, needed when spawning prefabs that contain spaces in their name
- Meta commands such as stop listening or a start word so that only phrases starting with the word are processed