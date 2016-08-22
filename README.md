# Unity3D-Twitch-ChatBot-with-MySQL
A Twitch ChatBot for Unity3d, with a MySQL Database link. Designed for my use, but source open to all.

Most responses from the Twitch IRC Server are relayed to Unity3D’s console window. Be sure to monitor it for responses/errors.  

Prerequisites: 
1: Passwords, Bot Name, Channel - Two text files (1 for TwitchBot OAuth password, 1 for Database password) are expected to be present in the Unity3D path \Assets\Resources\ .  
1a: TwitchBot OAuth – filename: pass.txt  
1b: Database – filename: dbpass.txt  
1c: Bot Name – Within BotInstance class, change string variable mBotName to your Bot name.  
1d: Channel – Within BotInstance class, change string variable mChannelName to your Twitch name.  


2: MySQL database with the following schema:  
2a: Name – streamoverlay  
2b: Table – viewers  
2c: Columns – ID (int11, Auto-Increment, Primary Key), Name (varchar45), Follower (bool), 	Subscriber (bool), Wallet (int11), LastSeen (datetime).  

3: MySQL database User. Add a user of name “overlayuser”, and grant this user read/write privileges to “streamoverlay”.  

4: DLL’s – MySQL.Data.dll and System.Data.dll , must be present in the Unity3D path \Assets\Plugins\ .  

Notables: 
BotInstance class is a Singleton model. It is designed to be created/instantiated by the following method: “BotInstance.Add()”. Once this method is called upon, the singleton gameobject/class will persist through all scene changes unless removed manually.  
TwitchIRC class is intended to only be used by the BotInstance. When making references to the TwitchIRC class, do so from the BotInstance instead of the TwitchIRC class itself.

ViewerObj class: This is the model that the platform uses to store a realtime collection of current viewers of the stream. The model is a near match to the DB schema, although, the C# DateTime does not match the DB datetime models, therefor you will see a string conversion in the DataManagerScript class regarding datetime.

v.0.1 Logic Flow:  
Start in BotLobby scene.  
StartupScript creates BotInstance.  
BotInstance uses TwitchIRC to initialize the connection to the Twitch Chat Server, request membership and command flags, then join the streams chatroom.  
Twitch’s chat server provides current viewers names in one of two ways (no apparent consistency): All at once, or progressively over 30 seconds to 1 minute. BotInstance listens for both ways, and adds current viewers to the “currentViewersList” List<ViewerObj> collection.  
As each viewer is added, it is checked against the database for “known” viewers. If viewer is known, a ViewerObj object is created for that viewer, and has its realtime data associated from the database (LastSeen updated to DateTime.Now). If the viewer is not known, a new ViewerObj object is created for that viewer, given an initial 5 coins to their Wallet, LastSeen as DateTime.Now, then saved to the database.  
BotInstance has 2 registered Event Handlers to catch known prefixes from the TwitchIRC output (stream-buffer). One to catch “PRIVMSG” strings for public visible chat, one to catch “JOIN” or “PART” strings as command strings.  
“PRIVMSG” strings then gets sent to a method “CheckIfKnownCommand(string user, string msgString)”, to check for known viewer commands: #coins (reports viewers wallet/coincount), #mytime (reports how much time in minutes has passed since the last time the LastSeen value was updated).

Other:   
For my “in-use” version, there are multiple Unity3D specific variable assignments the classes will expect. If I have missed commenting out any of these, you will get a “Null Object Reference” error. Please report any of these to the Issues section of this repository so I may resolve them as soon as possible.  
