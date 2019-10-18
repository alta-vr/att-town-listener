# Town-Listener
Voice Commands via the Websocket console

Uses the Windows Speech to Text API to send voice commands to a Township Tale Server.

Written in C#. Use Visual Studio to build it or download the latest release

If you want to improve your voice recognition be sure to train windows. Options for that are found in Control Panel > Ease of Access

Create a config.json file in the same directory as the exe containing the following options
```json
{
	"Username" : "user",
	"Password" : "pw",
	"GrammarFilePath" : "grammar.xml",
	"ConsoleMode" : false,
	"Language" : "en-US",
	"OverrideConfidence" : 1,
	"AliasFilePath" : "aliases.txt"
}
```

**Options:**
- Username/Password: your Alta Login details, the password will be replaced with a hashed version on first run so it wont store the plain text in there
- GrammarFilePath: The file path of the grammar file. This defines all possible phrases that can be recognized
- ConsoleMode: Type your commands in the console instead of using speech to text (useful for debugging)
- Language: The language to use when recognizing speech. This needs to be installed on your computer first. Go to Windows Settings > Time & Language > Language and "Add a preferred Language" after its added make sure it has the voice voice recognition symbol
- OverrideConfidence: Leave out or null if you want to leave it at confidence 1. Confidence is the value the speech recognition assigns a given phrase from 0 to 1. 1 being very sure its the phrase, 0 being it has no clue. If you voice detection is a bit buggy set this to a low number. Just be aware it will start picking up things incorrectly more often.
- AliasFilePath: An option file of aliases, 1 per line comma separated see below (it already has built in support for aliasing `me` => [Your username] and `everyone` => `*`)
```
give,spawn
arrows,arrow 1000
platform,woodenPlatform_1x1
down,down 0.5
move,select move
rotate,select rotate
```
NOTE: All words that appear on the left side of the alias need to also occur in the grammar file otherwise they wont be recognized

**TODO:**
- Automatically generate the grammar file from the command structure returned by the server
- Accept variables and numbers in the grammar
- Add an alias file which defines words to substitute for others, currently `me` => Your username and `everyone` => `*` (but theyre defined in code)
- Support phrase aliasing so that `go go danger chicken` => `spawn me spriggull`
- Support for inserting quotes, needed when spawning prefabs that contain spaces in their name
- Meta commands such as stop listening or a start word so that only phrases starting with the word are processed

Message Timo in the ATT-Meta Discord for any questions (https://discord.gg/YABBUp)