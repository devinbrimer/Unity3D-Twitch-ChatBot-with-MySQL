using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class ViewerObj
{
    private int _viewerID;
    private string _viewerName;
    private bool _follower;
    private bool _subscriber;
    private int _wallet;
    private DateTime _lastSeen;
    
    public ViewerObj(string viewerName, int wallet, DateTime lastSeen)
    {
        _viewerName = viewerName;
        _wallet = wallet;
        _lastSeen = lastSeen;
    }

    public ViewerObj(string viewerName, bool follower, bool subscriber, int wallet, DateTime lastSeen)
    {
        _viewerName = viewerName;
        _follower = follower;
        _subscriber = subscriber;
        _wallet = wallet;
        _lastSeen = lastSeen;
    }

    public ViewerObj(int viewerID, string viewerName, bool follower, bool subscriber, int wallet, DateTime lastSeen)
    {
        _viewerID = viewerID;
        _viewerName = viewerName;
        _follower = follower;
        _subscriber = subscriber;
        _wallet = wallet;
        _lastSeen = lastSeen;
    }

    public int ViewerID { get { return _viewerID; } }
    public string ViewerName { get { return _viewerName; } }
    public bool Follower { get { return _follower; } }
    public bool Subscriber { get { return _subscriber; } }
    public int Wallet { get { return _wallet; } set { _wallet = value; } }
    public DateTime LastSeen { get { return _lastSeen; } set { _lastSeen = value; } }
    
}
