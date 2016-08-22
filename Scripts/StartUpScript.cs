using UnityEngine;

public class StartUpScript : MonoBehaviour {
    
    void Start()
    {
        if(GameObject.Find("_BotInstance") == null)
        {
            BotInstance.AddBot();
        }
    }
        
}
