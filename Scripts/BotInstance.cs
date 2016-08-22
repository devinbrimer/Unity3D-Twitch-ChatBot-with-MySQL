using UnityEngine;
using System.Collections.Generic;
using System;

public class BotInstance : MonoBehaviour
{
    public static BotInstance _instance;

    public static TwitchIRC IRC;

    bool botInit;

    private static string mBotName = "botname";
    private static string mChannelName = "twitchname";

    private static LinkedList<GameObject> messages = new LinkedList<GameObject>();
    public static int maxMessages = 100;

    private static LinkedList<GameObject> cmdMessages = new LinkedList<GameObject>();
    public static int maxCmdMessages = 100;

    public static DataManagerScript mDBScript;

    public static List<ViewerObj> currentViewersList;

    public static Queue<string> nameFlyByQueue = new Queue<string>();

    void Awake()
    {
        DontDestroyOnLoad(gameObject);     
    }


    public static BotInstance instance
    {
        get
        {
            if (_instance != null)
                return _instance;
            var obj = new GameObject("_BotInstance");
            obj.transform.localPosition = Vector3.zero;
            _instance = obj.AddComponent<BotInstance>();

            currentViewersList = new List<ViewerObj>();

            IRC = obj.AddComponent<TwitchIRC>();
            IRC.mBotName = mBotName;
            IRC.mChannelName = mChannelName;
            IRC.messageReceivedEvent.AddListener(OnChatMsgRecieved);
            IRC.commandReceivedEvent.AddListener(OnCommandMsgReceived);
            IRC.StartIRC();

            mDBScript = obj.AddComponent<DataManagerScript>();

            return _instance;
        }
    }

    public static void AddBot()
    {
        if (!instance.botInit)
        {
            instance.botInit = true;
        }        
    }

    private static void OnChatMsgRecieved(string msg)
    {
        //parse from buffer.
        int msgIndex = msg.IndexOf("PRIVMSG #");
        string msgString = msg.Substring(msgIndex + IRC.mChannelName.Length + 11);
        string user = msg.Substring(1, msg.IndexOf('!') - 1);

        //remove old messages for performance reasons.
        if (messages.Count > maxMessages)
        {
            Destroy(messages.First.Value);
            messages.RemoveFirst();
        }

        Debug.Log(string.Format("{0} said: {1}", user, msgString));

        CheckIfKnownCommand(user, msgString);
    }

    private static void OnCommandMsgReceived(string cmd)
    {
        if (cmd.Contains("JOIN #"))
        {
            Debug.Log(cmd);

            int iBang = cmd.IndexOf("!");
            if (iBang > 0)
            {
                string viewerName = cmd.Substring(1, iBang - 1);                                

                // check if the viewer is in the current list, if not, add them
                if (!currentViewersList.Exists(v => v.ViewerName == viewerName))
                {
                    // Check if the viewer exists in the DB
                    ViewerObj thisViewer = mDBScript.GetStoredViewer(viewerName);
                    if (thisViewer != null)
                    {
                        currentViewersList.Add(thisViewer);
                        ViewerObj viewerObj = GetViewerObjFromString(viewerName);
                        mDBScript.DBUpdateViewerLastSeen(viewerObj);
                    }
                    else // create the new viewer object, add to currentviewerList, save to DB
                    {
                        thisViewer = new ViewerObj(viewerName, false, false, 5, DateTime.Now);
                        currentViewersList.Add(thisViewer);
                        mDBScript.SaveViewersToDatabase(currentViewersList);
                    }
                }
            }
        }
        else if (cmd.Contains("PART #"))
        {
            Debug.Log(cmd);
            
            int iBang = cmd.IndexOf("!");
            if (iBang > 0)
            {
                string viewerName = cmd.Substring(1, iBang - 1);

                // check if the viewer is in the current list, if not, do nothing
                if (currentViewersList.Exists(v => v.ViewerName == viewerName))
                {
                    ViewerObj viewerObj = GetViewerObjFromString(viewerName);
                    mDBScript.DBUpdateViewerLastSeen(viewerObj);
                    currentViewersList.Remove(viewerObj);
                }
            }
        }
        else if (cmd.Contains(":brimerbot.tmi.twitch.tv 353 brimerbot = #" + mChannelName + ":"))
        {
            // Adjust the startIndex position to match the expected length when including the channel name
            // Example, with "devin_brimer" it is 56 places forward.

            int startIndex = 56;
            string allNames = cmd.Substring(startIndex, cmd.Length - startIndex);
            Debug.Log(allNames);
            string[] namesArray = allNames.Split(' ');
            foreach(string name in namesArray)
            {
                // check if the viewer is in the current list, if not, add them
                if (!currentViewersList.Exists(v => v.ViewerName == name))
                {
                    // Check if the viewer exists in the DB
                    ViewerObj thisViewer = mDBScript.GetStoredViewer(name);
                    if (thisViewer != null)
                    {
                        //thisViewer.LastSeen = DateTime.Now;
                        currentViewersList.Add(thisViewer);
                        mDBScript.SaveViewersToDatabase(currentViewersList);
                    }
                    else // create the new viewer object, add to currentviewerList, save to DB
                    {
                        thisViewer = new ViewerObj(name, false, false, 5, DateTime.Now);
                        currentViewersList.Add(thisViewer);
                        mDBScript.SaveViewersToDatabase(currentViewersList);
                    }
                }
            }
        }
        
        //remove old messages for performance reasons.
        if (cmdMessages.Count > maxCmdMessages)
        {
            Destroy(cmdMessages.First.Value);
            cmdMessages.RemoveFirst();
        }
    }


    static void CheckIfKnownCommand(string viewer, string chatText)
    {
        if (chatText.Equals("#botcheck"))
        {
            IRC.SendMsg("Im Here!");
        }
        else if (chatText.Equals("#coins"))
        {
            ViewerObj who = currentViewersList.Find(v => v.ViewerName == viewer);
            if (who != null)
            {
                IRC.SendMsg(string.Format("@{0} , you have {1} coins.", viewer, who.Wallet.ToString()));
            }
            else
            {
                IRC.SendMsg("BEEP BOOP, catching up (blame twitchchat api), try again soon...");
            }
        }
        else if (viewer == mChannelName && chatText.StartsWith("!addcoins")) // Admin only command
        {
            string[] words = chatText.Split(' ');
            string recipientName = words[1].ToLower();
            string amount = words[2];
            int amountInt = int.Parse(amount);

            AddCoinsToViewer(recipientName, amountInt);
            Debug.Log(string.Format("{0} coins have been given to {1}", amount, recipientName));
        }
        else if (viewer == mChannelName && chatText.StartsWith("!removecoins")) // Admin only command
        {
            string[] words = chatText.Split(' ');
            string recipientName = words[1].ToLower();
            string amount = words[2];
            int amountInt = int.Parse(amount);
            RemoveCoinsFromViewer(recipientName, amountInt);
            Debug.Log(string.Format("{0} coins have been removed from {1}", amount, recipientName));
        }
        else if (chatText.Equals("#mytime"))
        {
            ViewerObj viewerObj = GetViewerObjFromString(viewer);
            if(viewerObj != null)
            {
                int vTime = CheckViewTime(viewerObj);
                IRC.SendMsg(string.Format("@{0} , it has been {1} minutes since your viewtime was last updated.",viewer, vTime.ToString()));
            }
        }
    }

    public static void AddCoinsToViewer(string viewerName, int amount)
    {
        ViewerObj viewer = currentViewersList.Find(v => v.ViewerName == viewerName);
        if(viewer != null)
        {
            viewer.Wallet += amount;
            mDBScript.DBUpdateViewerWallet(viewer);
        }
    }

    public static void RemoveCoinsFromViewer(string viewerName, int amount)
    {
        ViewerObj viewer = currentViewersList.Find(v => v.ViewerName == viewerName);
        if (viewer != null)
        {
            if(viewer.Wallet > amount)
            {
                viewer.Wallet -= amount;
            }
            else
            {
                viewer.Wallet = 0;
            }
            mDBScript.DBUpdateViewerWallet(viewer);
        }
    }
    
    public static ViewerObj GetViewerObjFromString(string viewerStr)
    {
        ViewerObj viewerObj = currentViewersList.Find(v => v.ViewerName == viewerStr);
        if (viewerObj != null)
        {
            return viewerObj;
        }
        else
        {
            return null;
        }
    }


    // Not Yet Implemented
    public static int CheckViewTime(ViewerObj viewerObj)
    {
        // get the current time
        // compare the timespan, in minutes, from the viewerObj.LastSeen to current time
        // if the span is greater than 15 minutes, add 5 coins to the viewers wallet
        
        TimeSpan tSpan = DateTime.Now - viewerObj.LastSeen;
        double tSpanMinutes = tSpan.TotalMinutes;

        int minutes = Mathf.FloorToInt((float)tSpanMinutes);

        return minutes;
    }
    
}
