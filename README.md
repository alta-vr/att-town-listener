Work in progress to interface with MIU for the purpose of accepting Twitch chat command

# Town-Listener
Streaming software Bot commands via the Websocket console

Listen for commands from a text file (Controlled by another software like MixItUp, Firebot, streamdeck etc,) and foward to a Township Tale Server.

Written in C#. Use Visual Studio to build it or download the latest release

Create a config.json file in the same directory as the exe containing the following options
```json
{
	"Username" : "user",
	"Password" : "pw",
	"FilePath" : "C:/WATCHER_FOLDER",
}
```

**Options:**
- Username/Password: your Alta Login details, the password will be replaced with a hashed version on first run so it wont store the plain text in there
- FilePath: .txt file modified in this location will pass information to the software. Space need to be replaced with %20 only use foward slashes


**TODO:**
- Automatically connect to specific server


Message Pixelbeard in the ATT-Meta Discord for any questions (https://discord.gg/YABBUp)

Basis of the code by Timo
