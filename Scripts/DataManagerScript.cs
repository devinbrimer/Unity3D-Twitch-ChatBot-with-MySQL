using UnityEngine;
using MySql.Data.MySqlClient;
using System.IO;
using System.Collections.Generic;
using System;

public class DataManagerScript : MonoBehaviour {

    [HideInInspector]
    public string csHostname;
    [HideInInspector]
    public int csPort;
    [HideInInspector]
    public string csUserID;
    [HideInInspector]
    public string csPassword;
    [HideInInspector]
    public string csDBName;
    [HideInInspector]
    public TextAsset mTextAssetPass;
    [HideInInspector]
    public string csCompleteStr;

    [HideInInspector]
    public MySqlConnection dbConn;

    [HideInInspector]
    public List<ViewerObj> mViewerList;

    void Start()
    {
        csCompleteStr = GetConnectionString();
    }

    public string GetConnectionString()
    {
        Security.PrefetchSocketPolicy("localhost", 21212);

        // set the connection string parameters
        csHostname = "localhost";
        csPort = 21212;
        csUserID = "overlayuser";
        //get the database user-password
        mTextAssetPass = (TextAsset)Resources.Load("dbpass");
        StringReader read = new StringReader(mTextAssetPass.text);
        if (read != null)
        {
            csPassword = read.ReadLine();
        }
        else
        {
            Debug.Log("Could not read the DB password");
        }
        csDBName = "streamoverlay";
        return string.Format("server={0};port={1};userid={2};password={3};database={4};", csHostname, csPort, csUserID, csPassword, csDBName);
    }
    
    public ViewerObj GetStoredViewer(string viewer)
    {
        try
        {
            dbConn = new MySqlConnection(csCompleteStr);
            dbConn.Open();

            string stm = "SELECT * FROM streamoverlay.viewers";
            MySqlCommand dbCOM = new MySqlCommand(stm, dbConn);
            MySqlDataReader dbRDR = dbCOM.ExecuteReader();

            while (dbRDR.Read())
            {
                if(dbRDR.GetString(1) == viewer)
                {
                    return new ViewerObj(dbRDR.GetInt32(0), dbRDR.GetString(1), dbRDR.GetBoolean(2), dbRDR.GetBoolean(3), dbRDR.GetInt32(4), DateTime.Now); //
                }
            }
            dbRDR.Close();
            return null;

        }
        catch (MySqlException ex)
        {
            Debug.Log(string.Format("Error: {0}", ex.ToString()));
            return null;

        }
        finally
        {
            if (dbConn != null)
            {
                dbConn.Close();
            }
        }
    }

    // save data from currentViewerList to Database
    public void SaveViewersToDatabase(List<ViewerObj> viewerList)
    {
        List<string> tempList = new List<string>();

        MySqlConnection dbConn = null;

        try
        {
            dbConn = new MySqlConnection(csCompleteStr);
            dbConn.Open();
            
            string stm = "SELECT * FROM streamoverlay.viewers";
            MySqlCommand dbCOM = new MySqlCommand(stm, dbConn);            
            
            MySqlDataReader dbRDR = dbCOM.ExecuteReader();

            while (dbRDR.Read())
            {
                tempList.Add(dbRDR.GetString(1));
            }

            dbRDR.Close();

            foreach (ViewerObj v in viewerList)
            {
                MySqlTransaction dbTrans = dbConn.BeginTransaction();
                dbCOM.Transaction = dbTrans;
                
                if (tempList.Contains(v.ViewerName))
                {
                    string query = @"UPDATE streamoverlay.viewers
                    SET Wallet=@vWallet, LastSeen=@vLastSeen
                    WHERE Name=@vViewerName;";
                    using (var command = new MySqlCommand(query, dbConn))
                    {
                        command.Parameters.Add("@vWallet", MySqlDbType.Int32).Value = v.Wallet;
                        command.Parameters.Add("@vLastSeen", MySqlDbType.DateTime).Value = v.LastSeen.ToString("yyyy-MM-dd H:mm:ss");
                        command.Parameters.Add("@vViewerName", MySqlDbType.VarChar).Value = v.ViewerName;
                        command.ExecuteNonQuery();
                    }
                    dbTrans.Commit();
                    dbTrans = null;
                }
                else
                {
                    string query = @"INSERT INTO streamoverlay.viewers(Name, Follower, Subscriber, Wallet, LastSeen)
                        VALUES(@vViewerName, 0, 0, @vWallet, @vLastSeen)";
                    using (var command = new MySqlCommand(query, dbConn))
                    {
                        command.Parameters.Add("@vViewerName", MySqlDbType.VarChar).Value = v.ViewerName;
                        command.Parameters.Add("@vWallet", MySqlDbType.Int32).Value = v.Wallet;
                        command.Parameters.Add("@vLastSeen", MySqlDbType.DateTime).Value = v.LastSeen.ToString("yyyy-MM-dd H:mm:ss");
                        command.ExecuteNonQuery();
                    }
                    dbTrans.Commit();
                    dbTrans = null;
                }
            }
        }
        catch (MySqlException ex)
        {
            Debug.Log(string.Format("Error: {0}", ex.ToString()));

        }
        finally
        {
            if (dbConn != null)
            {
                dbConn.Close();
            }
        }
    }
    
    public void DBUpdateViewerLastSeen(ViewerObj viewerObj)
    {
        MySqlConnection dbConn = null;

        try
        {
            dbConn = new MySqlConnection(csCompleteStr);
            dbConn.Open();

            string stm = "SELECT * FROM streamoverlay.viewers";
            MySqlCommand dbCOM = new MySqlCommand(stm, dbConn);

            MySqlDataReader dbRDR = dbCOM.ExecuteReader();

            bool foundViewer = false;

            while (dbRDR.Read())
            {
                if (dbRDR.GetString(1) == viewerObj.ViewerName.ToLower())
                {
                    foundViewer = true;
                }
            }

            dbRDR.Close();

            if (foundViewer)
            {
                MySqlTransaction dbTrans = dbConn.BeginTransaction();
                dbCOM.Transaction = dbTrans;

                string query = @"UPDATE streamoverlay.viewers
                    SET LastSeen=@vLastSeen
                    WHERE Name=@vViewerName;";
                using (var command = new MySqlCommand(query, dbConn))
                {
                    command.Parameters.Add("@vLastSeen", MySqlDbType.DateTime).Value = DateTime.Now.ToString("yyyy-MM-dd H:mm:ss");
                    command.Parameters.Add("@vViewerName", MySqlDbType.VarChar).Value = viewerObj.ViewerName.ToLower();
                    command.ExecuteNonQuery();
                }
                dbTrans.Commit();
                dbTrans = null;
                foundViewer = false;
            }
            


        }
        catch (MySqlException ex)
        {
            Debug.Log(string.Format("Error: {0}", ex.ToString()));

        }
        finally
        {
            if (dbConn != null)
            {
                dbConn.Close();
            }
        }
    }

    public void DBUpdateViewerWallet(ViewerObj viewerObj)
    {
        MySqlConnection dbConn = null;

        try
        {
            dbConn = new MySqlConnection(csCompleteStr);
            dbConn.Open();

            string stm = "SELECT * FROM streamoverlay.viewers";
            MySqlCommand dbCOM = new MySqlCommand(stm, dbConn);

            MySqlDataReader dbRDR = dbCOM.ExecuteReader();

            bool foundViewer = false;

            while (dbRDR.Read())
            {
                if (dbRDR.GetString(1) == viewerObj.ViewerName.ToLower())
                {
                    foundViewer = true;
                }
            }

            dbRDR.Close();

            if (foundViewer)
            {
                MySqlTransaction dbTrans = dbConn.BeginTransaction();
                dbCOM.Transaction = dbTrans;

                string query = @"UPDATE streamoverlay.viewers
                    SET Wallet=@vWallet
                    WHERE Name=@vViewerName;";
                using (var command = new MySqlCommand(query, dbConn))
                {
                    command.Parameters.Add("@vWallet", MySqlDbType.Int32).Value = viewerObj.Wallet;
                    command.Parameters.Add("@vViewerName", MySqlDbType.VarChar).Value = viewerObj.ViewerName.ToLower();
                    command.ExecuteNonQuery();
                }
                dbTrans.Commit();
                dbTrans = null;
                foundViewer = false;
            }
        }
        catch (MySqlException ex)
        {
            Debug.Log(string.Format("Error: {0}", ex.ToString()));

        }
        finally
        {
            if (dbConn != null)
            {
                dbConn.Close();
            }
        }
    }

}
