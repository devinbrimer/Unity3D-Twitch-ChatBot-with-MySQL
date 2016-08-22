using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

public class TwitchIRC : MonoBehaviour
{
    private string oauth;
    private TextAsset mOAuthTextAsset;
    public string mBotName;
    public string mChannelName;
    private string server = "irc.twitch.tv";
    private int port = 6667;
    
    // Event(buffer).
    public class MsgEvent : UnityEngine.Events.UnityEvent<string> { }
    public MsgEvent messageReceivedEvent = new MsgEvent();
    public class CmdEvent : UnityEngine.Events.UnityEvent<string> { }
    public MsgEvent commandReceivedEvent = new MsgEvent();
    private TcpClient IRC;
    private Queue<string> commandQueue = new Queue<string>();

    private NetworkStream ircStream;
    private string buffer = string.Empty;
    private StreamReader input;
    private StreamWriter output;
    private float lastTimeSentCommand;
    public bool botEnabled;
    public bool connected = false;
    public bool canTrackJoinsParts;
    public bool canSendCommands;

    public void StartIRC()
    {
        botEnabled = true;

        lastTimeSentCommand = Time.realtimeSinceStartup - 1;

        mOAuthTextAsset = (TextAsset)Resources.Load("pass");
        StringReader read = new StringReader(mOAuthTextAsset.text);
        if (read != null)
        {
            oauth = read.ReadLine();
        }
        else
        {
            Debug.Log("Could not read the password");
        }


        IRC = new TcpClient(server, port);

        ircStream = IRC.GetStream();
        input = new StreamReader(ircStream);
        output = new StreamWriter(ircStream);

        Debug.Log("About to send credentials..");

        output.WriteLine("PASS " + oauth);
        output.Flush();
        output.WriteLine("NICK " + mBotName);
        output.Flush();
        output.WriteLine("USER " + mBotName + " 8 * :" + mBotName);
        output.Flush();
    }

    public void SendCommand(string cmd)
    {
        commandQueue.Enqueue(cmd);
    }

    public void SendMsg(string msg)
    {
        commandQueue.Enqueue("PRIVMSG #" + mChannelName + " :" + msg);
    }


    void Update()
    {
        // Receive Messages
        if (ircStream != null && ircStream.DataAvailable)
        {
            buffer = input.ReadLine();

            if (buffer != null)
            {
                if (buffer.Contains("PRIVMSG #"))
                {
                    messageReceivedEvent.Invoke(buffer);
                }
                else if (buffer.Contains("JOIN #") || buffer.Contains("PART #"))
                {
                    commandReceivedEvent.Invoke(buffer);
                }
                else if (buffer.Contains(":brimerbot.tmi.twitch.tv 353 brimerbot = #devin_brimer :"))
                {
                    commandReceivedEvent.Invoke(buffer);
                    Debug.Log(buffer);
                }
                else
                {
                    Debug.Log(buffer);

                    if (buffer.Contains("PING :tmi.twitch.tv"))  // buffer.StartsWith("PING ")
                    {
                        SendCommand("PONG :tmi.twitch.tv");
                        Debug.Log("PONG :tmi.twitch.tv");
                    }
                    if (buffer.Contains(":tmi.twitch.tv 001")) // buffer.Split(' ')[1] == "001"
                    {
                        SendCommand("CAP REQ :twitch.tv/membership");
                        connected = true;
                    }
                    if (buffer.Contains(":tmi.twitch.tv CAP * ACK :twitch.tv/membership"))
                    {
                        canTrackJoinsParts = true;
                        SendCommand("CAP REQ :twitch.tv/commands");
                    }
                    if (buffer.Contains(":tmi.twitch.tv CAP * ACK :twitch.tv/commands"))
                    {
                        canSendCommands = true;

                        SendCommand("MODE " + mBotName + " +B");
                        SendCommand("JOIN #" + mChannelName);
                    }
                }
                buffer = string.Empty;
            }
        }

        //Send messages
        if (commandQueue.Count > 0) //do we have any commands to send? 
        {
            // https://github.com/justintv/Twitch-API/blob/master/IRC.md#command--message-limit 
            //has enough time passed since we last sent a message/command?
            if (lastTimeSentCommand + 0.2f < Time.realtimeSinceStartup)
            {
                //send msg.
                output.WriteLine(commandQueue.Peek());
                output.Flush();
                //remove msg from queue.
                commandQueue.Dequeue();

                lastTimeSentCommand = Time.realtimeSinceStartup;
            }
        }
    }
}