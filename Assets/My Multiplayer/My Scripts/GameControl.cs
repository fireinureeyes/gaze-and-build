using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameControl : NetworkBehaviour
{
    //boolean variables controling the enabling/disabling game features
    [SyncVar]
    public bool canRotateEyes = true;
    [SyncVar]
    public bool canNotifyWatched = true;
    [SyncVar]
    public bool canOutlineObjects = true;
    [SyncVar]
    public bool canOutlineObjectsShared = true;
    [SyncVar]
    public bool canCastPointingLine = true;
    [SyncVar]
    public bool assymetricMode = false;
    [SyncVar]
    public bool instructions = false;
    [SyncVar]
    public int instructionsNumber = 0;
    [SyncVar]
    public int statueNumber = 0;
    [SyncVar]
    public int numberOfPlayers = 0;

    NetworkManager nm;

    void Start () {
        nm = FindObjectOfType<NetworkManager>();
    }

    void Update()
    {
        if (!isServer) { return; }
        else {
            foreach (NetworkConnection conn in NetworkServer.connections)
            {
                if (conn == null) KeepDataOnReload.colorsUsed[NetworkServer.connections.IndexOf(conn)] = false;
            }
            numberOfPlayers = nm.numPlayers;
        }
    }
}
