using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.UI;
using Tobii.Gaming;

public class PlayerControler : NetworkBehaviour
{
    //character control
    private float speed;
    private float gravity;
    private Vector3 moveDirection;
    private CharacterController controller;

    //camera mouse control
    private float yaw = 0.0f;
    private float pitch = 0.0f;

    //player carrying an object
    private bool carrying;
    private GameObject carriedObject;
    private GameObject lastObj;    
    
    //screen point determined by the eye tracker
    private GazePoint gazePoint;

    //rotation of the prefab's eyes based on the gazePoint
    private Vector3 eyeRotation;
    private bool rotatedToNeutralPosition;

    //UI crosshair texture change
    private Texture2D neutral;
    private Texture2D ok;
    private Texture2D fail;

    //knowledge about the rotation of the carried object
    private float a;
    private float b;
    private float c;
    private bool done;

    //gazePoint smoothing, separate array for each screen axis
    private float[] array_x = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private float[] array_y = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    private int position;

    //rendering of the line of gaze on a Q key press
    private LineRenderer lineRenderer;
    public Material red;

    //showing the look of another player in the UI
    private GameObject lastPlayer;
    private GameObject lookedAtUI;
    [SyncVar]
    public bool lookedAt;

    //saving a reference to the baseplate with Game Control script
    public GameObject baseplate;

    //saving network manager settings in case of a reconnect
    public string address;
    public int port;

    //saving references
    private Lobby lobby;

    //creating color array
    public Color playerColor;
    private Color[] playerColors = {
        UnityEngine.Color.blue,
        UnityEngine.Color.green,
        new Color32(216,130,0,255), //orange
        UnityEngine.Color.magenta,
        UnityEngine.Color.yellow,
        UnityEngine.Color.cyan,
        new Color32(255,195,255,255), //pink
        new Color32(134,81,0,255), //brown
        UnityEngine.Color.black,
        UnityEngine.Color.gray, 
        new Color32(128,0,0,255), //maroon red
        new Color32(128,128,0,255), //olive green
        new Color32(0,128,128,255), //teal 
        new Color32(255,127,80,255), //skin
        new Color32(184,134,11,255), //dark golden
        new Color32(173,255,47,255), //yellow green
        new Color32(47,79,79,255), //dark blue
        new Color32(75,0,130,255), //indigo
        new Color32(176,196,222,255), //dark steel blue
        new Color32(160,82,45,255), //light brown
        UnityEngine.Color.blue,
        UnityEngine.Color.green,
        new Color32(216,130,0,255), //orange
        UnityEngine.Color.magenta,
        UnityEngine.Color.yellow,
        UnityEngine.Color.cyan,
        new Color32(255,195,255,255), //pink
        new Color32(134,81,0,255), //brown
        UnityEngine.Color.black,
        UnityEngine.Color.gray,
        new Color32(128,0,0,255), //maroon red
        new Color32(128,128,0,255), //olive green
        new Color32(0,128,128,255), //teal 
        new Color32(255,127,80,255), //skin
        new Color32(184,134,11,255), //dark golden
        new Color32(173,255,47,255), //yellow green
        new Color32(47,79,79,255), //dark blue
        new Color32(75,0,130,255), //indigo
        new Color32(176,196,222,255), //dark steel blue
        new Color32(160,82,45,255), //light brown
    };

    //assigning colors to players
    [SyncVar(hook = "OnPlayerColorChanged")] public Color myPlayerColor;

    //assigning names to players
    [SyncVar(hook = "OnPlayerNameChanged")] public string myPlayerName;

    void Start()
    {
        //variables initialization
        speed = 5.0f;
        gravity = 20.0f;
        moveDirection = Vector3.zero;
        controller = GetComponent<CharacterController>();
        lookedAt = false;
        lookedAtUI = GameObject.FindGameObjectsWithTag("LOOKEDAT")[0];
        GameObject.FindGameObjectsWithTag("GUITEXTURE")[0].GetComponent<RawImage>().enabled = true;
        lobby = GameObject.FindGameObjectsWithTag("NM")[0].GetComponent<Lobby>();
        eyeRotation = new Vector3(0.0f, 0.0f, 0.0f);
        baseplate = GameObject.FindGameObjectsWithTag("BS")[0];
        a = 0;
        b = 0;
        c = 0;
        position = 0;
        done = false;
        neutral = Resources.Load("crosshair") as Texture2D;
        ok = Resources.Load("crosshair_green") as Texture2D;
        fail = Resources.Load("crosshair_red") as Texture2D;
        rotatedToNeutralPosition = false;
        Cursor.visible = false;
        controller.enableOverlapRecovery = false;
        //if no player name has been entered in the lobby, pick one from the list
        Random.InitState(System.DateTime.Now.Millisecond);
        if (KeepDataOnReload.playerName == "") KeepDataOnReload.playerName = KeepDataOnReload.names[Random.Range(0, 37)];
        myPlayerName = KeepDataOnReload.playerName; 

        //set player rotation according to the spawn position
        //round robin position 1 is already rotated correctly
        //round robin position 3 needs to rotate by 90 degrees
        if (transform.position.x < 0) yaw = 90;
        //round robin position 4 needs to rotate by 90 degrees
        else if (transform.position.x > 0) yaw = 270;
        //round robin position 2 needs to rotate to the opposite direction
        else if (transform.position.z > 6) yaw = 180;

        //make sure there is no notification about reconnecting
        if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";

        //line renderer initialization
        lineRenderer = transform.GetChild(6).GetComponent<LineRenderer>();
        lineRenderer.material = red;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.positionCount = 2;        

        //saving the ip address and port in case of a reconnect
        address = GameObject.FindGameObjectsWithTag("NM")[0].GetComponent<NetworkManager>().networkAddress;
        port = GameObject.FindGameObjectsWithTag("NM")[0].GetComponent<NetworkManager>().networkPort;
    }    

    //remove displayed text after 2 seconds
    IEnumerator DeleteText()
    {
        yield return new WaitForSeconds(2f);
        GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
    }

    //assigning the colors and names to players
    public override void OnStartClient()
    {
        OnPlayerColorChanged(myPlayerColor);
        OnPlayerNameChanged(myPlayerName);
    }
    void OnPlayerColorChanged(Color newValue)
    {
        transform.GetChild(0).GetComponent<MeshRenderer>().material.color = newValue;
    }
    void OnPlayerNameChanged(string newValue)
    {
        transform.GetChild(7).GetChild(0).GetComponent<Text>().text = newValue;
    }
    //runs on the local pc after starting the player script
    public override void OnStartLocalPlayer()
    {
        CmdSend();        
    }
    //runs on the server
    [Command]
    public void CmdSend()
    {
        KeepDataOnReload.numberPlayers++;
        int loopVariable = 0;
        while (KeepDataOnReload.colorsUsed[loopVariable] == true)
        {
            loopVariable++;
        }
        KeepDataOnReload.colorsUsed[loopVariable] = true;
        TargetSend(connectionToClient, loopVariable);
    }
    //runs on the local pc after executed by the server
    [TargetRpc]
    public void TargetSend(NetworkConnection target, int order)
    {
        CmdSetPlayerName(myPlayerName);
        playerColor = playerColors[order];
        CmdSetPlayerColor(playerColor);
    }
    [Command]
    void CmdSetPlayerColor(Color newColor)
    {
        myPlayerColor = newColor;
    }
    [Command]
    void CmdSetPlayerName(string playerName)
    {
        myPlayerName = playerName;
    }

    //called once per frame
    void Update()
    {        
        if (!isLocalPlayer)
        {
            return;
        }

        if (!lobby.userInputEnabled)
        {
            return;
        }

        //toggle the game control settings
        if (Input.GetKeyUp(KeyCode.F1))
        {
            //toggle test condition 1 (only object outlining)
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";

            if (isServer) { RpcToggle("canRotateEyes", false); } else { CmdToggle("canRotateEyes", false); }
            if (isServer) { RpcToggle("canNotifyWatched", false); } else { CmdToggle("canNotifyWatched", false); }
            if (isServer) { RpcToggle("canOutlineObjects", true); } else { CmdToggle("canOutlineObjects", true); }
            if (isServer) { RpcToggle("canOutlineObjectsShared", false); } else { CmdToggle("canOutlineObjectsShared", false); }
            if (isServer) { RpcToggle("canCastPointingLine", false); } else { CmdToggle("canCastPointingLine", false); }

            ColorText(false);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Condition 1 (gaze) is now on";
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F2))
        {
            //toggle test condition 2 (object outlining, object outlining shared, eye rotation, notification about being watched, pointing by pressing Q)
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";

            if (isServer) { RpcToggle("canRotateEyes", true); } else { CmdToggle("canRotateEyes", true); }
            if (isServer) { RpcToggle("canNotifyWatched", true); } else { CmdToggle("canNotifyWatched", true); }
            if (isServer) { RpcToggle("canOutlineObjects", true); } else { CmdToggle("canOutlineObjects", true); }
            if (isServer) { RpcToggle("canOutlineObjectsShared", true); } else { CmdToggle("canOutlineObjectsShared", true); }
            if (isServer) { RpcToggle("canCastPointingLine", true); } else { CmdToggle("canCastPointingLine", true); }

            ColorText(false);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Condition 2 (social gaze) is now on";
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F3))
        {
            //toggle test condition 3 (control condition - everything off)
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";

            if (isServer) { RpcToggle("canRotateEyes", false); } else { CmdToggle("canRotateEyes", false); }
            if (isServer) { RpcToggle("canNotifyWatched", false); } else { CmdToggle("canNotifyWatched", false); }
            if (isServer) { RpcToggle("canOutlineObjects", false); } else { CmdToggle("canOutlineObjects", false); }
            if (isServer) { RpcToggle("canOutlineObjectsShared", false); } else { CmdToggle("canOutlineObjectsShared", false); }
            if (isServer) { RpcToggle("canCastPointingLine", false); } else { CmdToggle("canCastPointingLine", false); }

            ColorText(false);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Condition 3 (no gaze) is now on";
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F4))
        {
            //toggle object outlining
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
            if (isServer) { RpcToggle("canOutlineObjectsShared", !baseplate.GetComponent<GameControl>().canOutlineObjectsShared); } else { CmdToggle("canOutlineObjectsShared", !baseplate.GetComponent<GameControl>().canOutlineObjectsShared); }
            ColorText(baseplate.GetComponent<GameControl>().canOutlineObjectsShared);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Mutual gaze object outlining is now " + StatusToString(baseplate.GetComponent<GameControl>().canOutlineObjectsShared);
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F5))
        {
            //toggle eye rotation
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
            if (isServer) { RpcToggle("canRotateEyes", !baseplate.GetComponent<GameControl>().canRotateEyes); } else { CmdToggle("canRotateEyes", !baseplate.GetComponent<GameControl>().canRotateEyes); }
            ColorText(baseplate.GetComponent<GameControl>().canRotateEyes);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Eye rotation is now " + StatusToString(baseplate.GetComponent<GameControl>().canRotateEyes);
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F6))
        {
            //toggle notification about being watched
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
            if (isServer) { RpcToggle("canNotifyWatched", !baseplate.GetComponent<GameControl>().canNotifyWatched); } else { CmdToggle("canNotifyWatched", !baseplate.GetComponent<GameControl>().canNotifyWatched); }
            ColorText(baseplate.GetComponent<GameControl>().canNotifyWatched);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Notification about being watched is now " + StatusToString(baseplate.GetComponent<GameControl>().canNotifyWatched);
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F7))
        {
            //toggle object outlining
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
            if (isServer) { RpcToggle("canOutlineObjects", !baseplate.GetComponent<GameControl>().canOutlineObjects); } else { CmdToggle("canOutlineObjects", !baseplate.GetComponent<GameControl>().canOutlineObjects); }
            ColorText(baseplate.GetComponent<GameControl>().canOutlineObjects);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Object outlining is now " + StatusToString(baseplate.GetComponent<GameControl>().canOutlineObjects);
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F8))
        {
            //toggle pointing by pressing Q
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
            if (isServer) { RpcToggle("canCastPointingLine", !baseplate.GetComponent<GameControl>().canCastPointingLine); } else { CmdToggle("canCastPointingLine", !baseplate.GetComponent<GameControl>().canCastPointingLine); }
            ColorText(baseplate.GetComponent<GameControl>().canCastPointingLine);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Pointing by pressing Q is now " + StatusToString(baseplate.GetComponent<GameControl>().canCastPointingLine);
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F9))
        {
            //toggle assymetric mode
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
            if (isServer) { RpcToggle("assymetricMode", !baseplate.GetComponent<GameControl>().assymetricMode); } else { CmdToggle("assymetricMode", !baseplate.GetComponent<GameControl>().assymetricMode); }
            ColorText(baseplate.GetComponent<GameControl>().assymetricMode);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Assymetric playing mode is now " + StatusToString(baseplate.GetComponent<GameControl>().assymetricMode);
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F10))
        {
            //toggle instructions
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
            if (isServer) { RpcToggle("instructions", !baseplate.GetComponent<GameControl>().instructions); } else { CmdToggle("instructions", !baseplate.GetComponent<GameControl>().instructions); }
            ColorText(baseplate.GetComponent<GameControl>().instructions);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Instructions are now " + StatusToString(baseplate.GetComponent<GameControl>().instructions);
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F11))
        {
            //switch instructions
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
            if (isServer) { RpcSetInstructions(); } else { CmdSetInstructions(); }
            ColorText(false);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Instructions changed to " + InstructionsToString(baseplate.GetComponent<GameControl>().instructionsNumber+1);
            StartCoroutine("DeleteText");
        }
        if (Input.GetKeyUp(KeyCode.F12))
        {
            //switch statues
            StopCoroutine("DeleteText");
            if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "";
            if (isServer) { RpcSetStatue(); } else { CmdSetStatue(); }
            ColorText(false);
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Statue changed to " + StatuesToString(baseplate.GetComponent<GameControl>().statueNumber+1);
            StartCoroutine("DeleteText");
        }

        //changing color of the lookedAt ui component
        if (baseplate.GetComponent<GameControl>().canNotifyWatched)
        {
            if (lookedAt)
            {
                lookedAtUI.GetComponent<Image>().color = new Color(255, 0, 0, 1f);
            } else
            {
                StartCoroutine("Color");
            }
        }

        //loading the most recent gazepoint
        GazePoint gazePoint = TobiiAPI.GetGazePoint();
        //object highlighting and eye rotation
        if (gazePoint.IsRecent())
        {
            //filtering the raw gazePoint data for smoother visualisation
            Vector2 point = Smoothify(new Vector2(gazePoint.Screen.x, gazePoint.Screen.y));

            if (baseplate.GetComponent<GameControl>().canCastPointingLine)
            {
                if (Input.GetButton("Fire1"))
                {
                    //drawing of a line and a 2D target texture from the player to the gazed point in space on a key press
                    if (isServer)
                    {
                        RpcDrawLine(Camera.main.transform.position, Camera.main.ScreenToWorldPoint(new Vector3(point.x, point.y, 10)), true);
                        transform.GetChild(6).GetComponent<LineRenderer>().enabled = false;
                    }
                    else
                    {
                        //drawing the target of the line (projected crosshair)
                        transform.GetChild(3).GetComponent<Projector>().enabled = true;
                        transform.GetChild(3).transform.LookAt(Camera.main.ScreenToWorldPoint(new Vector3(point.x, point.y, 10)));
                        CmdDrawLine(Camera.main.transform.position, Camera.main.ScreenToWorldPoint(new Vector3(point.x, point.y, 10)), true);
                        transform.GetChild(6).GetComponent<LineRenderer>().enabled = false;
                    }
                }
            }
            if (Input.GetKeyUp(KeyCode.Q))
            {
                if (isServer)
                {
                    RpcDrawLine(new Vector3(0, 0, 0), new Vector3(0, 0, 0), false);
                }
                else
                {
                    CmdDrawLine(new Vector3(0, 0, 0), new Vector3(0, 0, 0), false);
                }
            }

            //re-mapping the eye data to the rotation of eye objects in the scene
            eyeRotation = new Vector3(-Remap(Mathf.Clamp(point.y, 0, Screen.height), 0, Screen.height, -35, 35), Remap(Mathf.Clamp(point.x, 0, Screen.width), 0, Screen.width, -35, 35), 0.0f);

            //saving the current object at which the player is looking
            GameObject o = TobiiAPI.GetFocusedObject();              

            //finding out if the player is looking at another player
            if ((o != null) && (lastPlayer == null) && ((o.GetComponent<PlayerControler>() != null) || (o.GetComponent<Detect>() != null)))
            {
                if (!isServer)
                {
                    if (o.GetComponent<PlayerControler>() != null) { CmdLookedAt(o.GetComponent<PlayerControler>().gameObject, true); }
                    if (o.GetComponent<Detect>() != null) { CmdLookedAt(o.transform.parent.GetComponent<PlayerControler>().gameObject, true); }

                }
                if (o.GetComponent<PlayerControler>() != null) { o.GetComponent<PlayerControler>().lookedAt = true; }
                if (o.GetComponent<Detect>() != null) { o.transform.parent.GetComponent<PlayerControler>().lookedAt = true; }
                lastPlayer = o;
            }
            else if ((lastPlayer != null) && (o != lastPlayer))
            {
                if (!isServer)
                {
                    if (lastPlayer.GetComponent<PlayerControler>() != null) { CmdLookedAt(lastPlayer.GetComponent<PlayerControler>().gameObject, false); }
                    if (lastPlayer.GetComponent<Detect>() != null) { CmdLookedAt(lastPlayer.transform.parent.GetComponent<PlayerControler>().gameObject, false); }
                } 
                if (lastPlayer.GetComponent<PlayerControler>() != null) { lastPlayer.GetComponent<PlayerControler>().lookedAt = false; }
                if (lastPlayer.GetComponent<Detect>() != null) { lastPlayer.transform.parent.GetComponent<PlayerControler>().lookedAt = false; }
                lastPlayer = null;
            }

            //switching the highlighting on the object, in case on server, the data is sent to the clients, in case on client, the data is sent to the server
            if ((o != null) && (lastObj == null) && (o.tag == "PICKUPABLE"))
            {
                if (isServer)
                {
                    RpcHighlightOn(o, playerColor);
                }
                else
                {
                    CmdHighlightOn(o, playerColor);
                }
                lastObj = o;
            }
            else if ((lastObj != null) && (o != lastObj))
            {
                if (isServer)
                {
                    RpcHighlightOff(lastObj);
                }
                else
                {
                    CmdHighlightOff(lastObj);
                }
                lastObj = null;
            }

        }
        else
        {
            //if no gaze is detected, hide the drawn line and the target
            if (isServer)
            {
                RpcDrawLine(new Vector3(0, 0, 0), new Vector3(0, 0, 0), false);
            }
            else
            {
                transform.GetChild(3).GetComponent<Projector>().enabled = false;
                CmdDrawLine(new Vector3(0, 0, 0), new Vector3(0, 0, 0), false);
            }
            
            //if no gaze is detected, the rotation of the eye objects in the scene is set to neutral
            eyeRotation = new Vector3(0.0f, 0.0f, 0.0f);
        }

        //rotating of the eye objects in the scene, in case on server, the data is sent to the clients, in case on client, the data is sent to the server
        //has to be outside of the main eye-tracking loop, otherwise wouldnt rotate the eye objects back after loosing of the eye position data
        if (baseplate.GetComponent<GameControl>().canRotateEyes)
        {
            rotatedToNeutralPosition = false;
            if (isServer)
            {
                RpcEyeRotate(eyeRotation);
            }
            else
            {
                EyeRotate(eyeRotation);
                CmdEyeRotate(eyeRotation);
            }
        }
        else {
            if (!rotatedToNeutralPosition) { 
                eyeRotation = new Vector3(0.0f, 0.0f, 0.0f);
                if (isServer)
                {
                    RpcEyeRotate(eyeRotation);
                }
                else
                {
                    EyeRotate(eyeRotation);
                    CmdEyeRotate(eyeRotation);
                }
                rotatedToNeutralPosition = true;
            }            
        }

        //determining if an object is being carried by the player
        if (carrying)
        {
            //set the current position of the carried object in front of the 1st person camera and adjust its properties
            carriedObject.GetComponent<Rigidbody>().isKinematic = true;
            carriedObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            carriedObject.transform.position = (transform.position + transform.forward * 2.15f + transform.up * 0.25f);

            //if the player presses the E key, stop carrying the object             
            if (Input.GetKeyDown(KeyCode.E))
            {
                DropObject();
            }

            //if the player presses a mouse button or scrolls the wheel, adjust the rotation of the carried object accordingly
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetAxis("Mouse ScrollWheel") != 0f)
            {                
                //rotate the carried object up
                if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                {
                    if (a == 0)
                    {
                        if (b == 0)
                        {
                            if ((b == 0) && (c == 0) && (done == false)) { a += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; b = 0; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b = 0; c = 270; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; b = 0; c = 180; done = true; }
                        }
                        if (b == 90)
                        {
                            if ((c == 0) && (done == false)) { c += 90; done = true; }
                            else if ((c == 90) && (done == false)) { c += 90; done = true; }
                            else if ((c == 180) && (done == false)) { c += 90; done = true; }
                            else if ((c == 270) && (done == false)) { c += 90; done = true; }
                        }
                        if (b == 180)
                        {
                            if ((c == 0) && (done == false)) { a -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; done = true; }
                        }
                        if (b == 270)
                        {
                            if ((c == 0) && (done == false)) { c -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { c -= 90; done = true; }
                            else if ((c == 180) && (done == false)) { c -= 90; done = true; }
                            else if ((c == 270) && (done == false)) { c -= 90; done = true; }
                        }
                    }
                    else if (a == 90)
                    {
                        if (b == 0)
                        {
                            if ((b == 0) && (c == 0) && (done == false)) { a += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; b = 0; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b = 0; c = 270; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; b = 0; c = 180; done = true; }
                        }
                        if (b == 90)
                        {
                            if ((c == 0) && (done == false)) { a += 90; b -= 90; c += 270; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b += 90; c += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; b -= 90; c -= 90; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; b -= 90; c -= 90; done = true; }
                        }
                        if (b == 180)
                        {
                            if ((c == 0) && (done == false)) { a -= 90; b = 180; c = 0; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; b = 180; c = 180; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b -= 180; c -= 180; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b = 180; c = 90; done = true; }
                        }
                        if (b == 270)
                        {
                            if ((c == 0) && (done == false)) { a += 90; b += 90; c += 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b += 90; c += 270; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b -= 90; c -= 90; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; b -= 90; c -= 90; done = true; }
                        }
                    }
                    else if (a == 180)
                    {
                        if (b == 0)
                        {
                            if ((c == 0) && (done == false)) { a += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; b = 0; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b = 0; c = 270; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; b = 0; c = 180; done = true; }
                        }
                        if (b == 90)
                        {
                            if ((c == 0) && (done == false)) { a -= 180; b += 180; c += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a = 180; b = 90; c -= 90; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 180; b += 180; c += 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 180; b += 180; c += 90; done = true; }
                        }
                        if (b == 180)
                        {
                            if ((c == 0) && (done == false)) { a -= 90; b = 180; c = 0; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; b = 180; c = 180; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b = 180; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b -= 180; c -= 180; done = true; }
                        }
                        if (b == 270)
                        {
                            if ((c == 0) && (done == false)) { c += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 180; b -= 180; c += 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 180; b -= 18; c -= 90; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 180; b -= 180; c -= 90; done = true; }
                        }
                    }
                    else if (a == 270)
                    {
                        if (b == 0)
                        {
                            if ((c == 0) && (done == false)) { a += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; b = 0; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b = 0; c = 270; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; b = 0; c = 180; done = true; }
                        }
                        if (b == 90)
                        {
                            if ((c == 0) && (done == false)) { a -= 270; b -= 90; c += 90; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 270; b -= 90; c += 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 270; b -= 90; c += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 270; b -= 90; c += 90; done = true; }
                        }
                        if (b == 180)
                        {
                            if ((c == 0) && (done == false)) { a -= 90; b = 180; c = 0; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; b -= 180; c += 180; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; b = 180; c = 180; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b -= 180; c -= 180; done = true; };
                        }
                        if (b == 270)
                        {
                            if ((c == 0) && (done == false)) { a += 90; b += 90; c += 270; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b += 90; c -= 90; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 270; b -= 270; c -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 270; b -= 270; c -= 90; done = true; }
                        }
                    }
                }
                //rotate the carried object down
                if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                {
                    if (a == 0)
                    {
                        if (b == 0)
                        {
                            if ((b == 0) && (c == 0) && (done == false)) { a -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b = 0; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b = 0; c = 270; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; b = 0; c = 180; done = true; }
                        }
                        if (b == 90)
                        {
                            if ((c == 0) && (done == false)) { c -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { c -= 90; done = true; }
                            else if ((c == 180) && (done == false)) { c -= 90; done = true; }
                            else if ((c == 270) && (done == false)) { c -= 90; done = true; }
                        }
                        if (b == 180)
                        {
                            if ((c == 0) && (done == false)) { a += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; done = true; }
                        }
                        if (b == 270)
                        {
                            if ((c == 0) && (done == false)) { c += 90; done = true; }
                            else if ((c == 90) && (done == false)) { c += 90; done = true; }
                            else if ((c == 180) && (done == false)) { c += 90; done = true; }
                            else if ((c == 270) && (done == false)) { c += 90; done = true; }
                        }
                    }
                    else if (a == 90)
                    {
                        if (b == 0)
                        {
                            if ((b == 0) && (c == 0) && (done == false)) { a -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b = 0; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b = 0; c = 270; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; b = 0; c = 180; done = true; }
                        }
                        if (b == 90)
                        {
                            if ((c == 0) && (done == false)) { a -= 90; b += 90; c -= 270; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b -= 90; c -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b += 90; c += 90; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; b += 90; c += 90; done = true; }
                        }
                        if (b == 180)
                        {
                            if ((c == 0) && (done == false)) { a += 90; b = 180; c = 0; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; b = 180; c = 180; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b += 180; c += 180; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; b = 180; c = 90; done = true; }
                        }
                        if (b == 270)
                        {
                            if ((c == 0) && (done == false)) { a -= 90; b -= 90; c -= 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b -= 90; c -= 270; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; b += 90; c += 90; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; b += 90; c += 90; done = true; }
                        }
                    }
                    else if (a == 180)
                    {
                        if (b == 0)
                        {
                            if ((c == 0) && (done == false)) { a -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b = 0; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b = 0; c = 270; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; b = 0; c = 180; done = true; }
                        }
                        if (b == 90)
                        {
                            if ((c == 0) && (done == false)) { a += 180; b -= 180; c -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a = 180; b = 90; c += 90; done = true; }
                            else if ((c == 180) && (done == false)) { a += 180; b -= 180; c -= 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 180; b -= 180; c -= 90; done = true; }
                        }
                        if (b == 180)
                        {
                            if ((c == 0) && (done == false)) { a += 90; b = 180; c = 0; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; b = 180; c = 180; done = true; }
                            else if ((c == 90) && (done == false)) { a += 90; b = 180; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 90; b += 180; c += 180; done = true; }
                        }
                        if (b == 270)
                        {
                            if ((c == 0) && (done == false)) { c -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a += 180; b += 180; c -= 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 180; b += 18; c += 90; done = true; }
                            else if ((c == 180) && (done == false)) { a += 180; b += 180; c += 90; done = true; }
                        }
                    }
                    else if (a == 270)
                    {
                        if (b == 0)
                        {
                            if ((c == 0) && (done == false)) { a -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b = 0; c = 90; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b = 0; c = 270; done = true; }
                            else if ((c == 180) && (done == false)) { a -= 90; b = 0; c = 180; done = true; }
                        }
                        if (b == 90)
                        {
                            if ((c == 0) && (done == false)) { a += 270; b += 90; c -= 90; done = true; }
                            else if ((c == 180) && (done == false)) { a += 270; b += 90; c -= 90; done = true; }
                            else if ((c == 270) && (done == false)) { a += 270; b += 90; c -= 90; done = true; }
                            else if ((c == 90) && (done == false)) { a += 270; b += 90; c -= 90; done = true; }
                        }
                        if (b == 180)
                        {
                            if ((c == 0) && (done == false)) { a += 90; b = 180; c = 0; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b += 180; c -= 180; done = true; }
                            else if ((c == 180) && (done == false)) { a += 90; b = 180; c = 180; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b += 180; c += 180; done = true; };
                        }
                        if (b == 270)
                        {
                            if ((c == 0) && (done == false)) { a -= 90; b -= 90; c -= 270; done = true; }
                            else if ((c == 270) && (done == false)) { a -= 90; b -= 90; c += 90; done = true; }
                            else if ((c == 180) && (done == false)) { a += 270; b += 270; c += 90; done = true; }
                            else if ((c == 90) && (done == false)) { a -= 90; b += 90; c -= 90; done = true; }
                        }
                    }
                }
                //rotate the carried object left
                if (Input.GetMouseButtonDown(0))
                {

                    if (a == 90 || a == 0 || a == 270)
                    {
                        b += 90;
                    }
                    if (a == 180)
                    {
                        b -= 90;
                    }

                }
                //rotate the carried object right
                if (Input.GetMouseButtonDown(1))
                {

                    if (a == 90 || a == 0 || a == 270)
                    {
                        b -= 90;
                    }
                    if (a == 180)
                    {
                        b += 90;
                    }
                }

                //normalize the rotation values
                if (a == 360 || a == -360) { a = 0; }
                else if (a == -90) { a = 270; }
                else if (a == -180) { a = 180; }
                else if (a == -270) { a = 90; };
                if (b == 360 || b == -360) { b = 0; }
                else if (b == -90) { b = 270; }
                else if (b == -180) { b = 180; }
                else if (b == -270) { b = 90; };
                if (c == 360 || c == -360) { c = 0; }
                else if (c == -90) { c = 270; }
                else if (c == -180) { c = 180; }
                else if (c == -270) { c = 90; };                
            }
            //set the current rotation of the carried object leveled with the ground, counting in the rotation caused by the player
            CarriedObjRotation();
            CmdCarriedObjRotation();
            done = false;
        }
        else //if not carrying
        {
            //if the player presses the E key, try to start carrying the object
            if (Input.GetKeyDown(KeyCode.E)) Pickup();    
        }

        
        //update the coordinates for the rotation of the player by mouse
        yaw += Input.GetAxis("Mouse X") * 6.0f;
        pitch -= Input.GetAxis("Mouse Y") * 6.0f;
        //height view constraints
        if (Mathf.Min(pitch, 30) == 30)
        {
            pitch = 30;
        }
        if (Mathf.Max(pitch, -60) == -60)
        {
            pitch = -60;
        }
        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
        
        //check if is on the ground
        if (controller.isGrounded)
        {
            //fill moveDirection with input.
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection.y = 0;
            //multiply by speed.
            moveDirection *= speed;
        }
        //ppplying gravity to the controller
        moveDirection.y -= gravity * Time.deltaTime;
        //making the character move
        controller.Move(moveDirection * Time.deltaTime);

        /* old movement algorithm
        //update the coordinates for the movement of the player by keys
        float x = Input.GetAxis("Horizontal") * Time.deltaTime * 6.0f;
        float z = Input.GetAxis("Vertical") * Time.deltaTime * 6.0f;
        //translate the player
        transform.Translate(x, 0.0f, z);
        //keep the player kinematic while at the same time not moving on the Y axis
        x = transform.position.x;
        z = transform.position.z;
        transform.position = new Vector3(x, 0.0f, z);
        //GetComponent<Rigidbody>().isKinematic = true;
        */

        //1st person camera
        Camera.main.transform.position = (this.transform.position + this.transform.forward * 0.3f + transform.up * 0.5f);
        Camera.main.transform.LookAt(this.transform.position + this.transform.forward * 10 - transform.up * 1f);
        
        //if no Q press is detected, hide the drawn line and the target texture
        if (!Input.GetButton("Fire1"))
        {
            transform.GetChild(3).GetComponent<Projector>().enabled = false;
            if (lineRenderer) { 
                lineRenderer.startColor = new Color(00, 00, 00, 00);
                lineRenderer.endColor = new Color(00, 00, 00, 00);
            }
        }
    }

    //try to start carrying an object
    void Pickup()
    {
        //determine the screen center and save it as a new ray
        int x = Screen.width / 2;
        int y = Screen.height / 2;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y));
        //cast the ray and save the object's Pickupable script's reference in case of a hit        
        RaycastHit hit;
        //limit maximum picking distance to 3 meters
        if (Physics.Raycast(ray, out hit, 3))
        {            
            Pickupable pickupable = hit.collider.GetComponent<Pickupable>();
            if (pickupable != null)
            {
                if (!pickupable.carried)
                {
                    //change UI crosshair texture to green color
                    GameObject.Find("RawImage").GetComponent<RawImage>().texture = ok;
                    StartCoroutine("Wait");

                    //set the carry flag and the reference to the object
                    carrying = true;
                    carriedObject = pickupable.gameObject;
                    if (isServer)
                    {
                        RpcCarryOn(carriedObject);
                    } else
                    {
                        CmdCarryOn(carriedObject);
                    }
                    //set the player as the objects parent
                    carriedObject.transform.parent = transform;
                }
                else
                {
                    //change UI crosshair texture to red color
                    GameObject.Find("RawImage").GetComponent<RawImage>().texture = fail;
                    StartCoroutine("Wait");
                }
            }
        }
        else
        {
            //change UI crosshair texture to red color
            GameObject.Find("RawImage").GetComponent<RawImage>().texture = fail;
            StartCoroutine("Wait");
        }
    }

    IEnumerator Wait()
    {
        //reset UI crosshair texture to white color after 0.2 seconds
        yield return new WaitForSeconds(0.2f);
        GameObject.Find("RawImage").GetComponent<RawImage>().texture = neutral;
    }

    //drop the carried object
    void DropObject()
    {
        //change UI crosshair texture to green color
        GameObject.Find("RawImage").GetComponent<RawImage>().texture = ok;
        StartCoroutine("Wait");

        //reset the knowledge about the object rotation
        a = 0;
        b = 0;
        c = 0;

        //set the carry flag and forget the references to the object
        carrying = false;
        if (isServer)
        {
            RpcCarryOff(carriedObject);
        }
        else
        {
            CmdCarryOff(carriedObject);
        }
        carriedObject.transform.parent = null;
        carriedObject = null;
    }

    //using networking functions finding out if the player is looking at another player
    [ClientRpc]
    void RpcLookedAt(GameObject o, bool lookedAt) { o.GetComponent<PlayerControler>().lookedAt = lookedAt; }
    [Command]
    void CmdLookedAt(GameObject o, bool lookedAt) { o.GetComponent<PlayerControler>().lookedAt = lookedAt; }

    //using networking functions drawing lines between two supplied points and the target texture
    void DrawLine(Vector3 point1, Vector3 point2, bool enabled)
    {
        transform.GetChild(3).GetComponent<Projector>().enabled = enabled;        
        transform.GetChild(3).transform.LookAt(point2);
        if ((lineRenderer != null) && (point1 != null)&&(point2 != null)) { 
            lineRenderer.SetPosition(0, point1);
            lineRenderer.SetPosition(1, point2);
        }
    }
    [ClientRpc]
    void RpcDrawLine(Vector3 point1, Vector3 point2, bool enabled) { DrawLine(point1, point2, enabled); }
    [Command]
    void CmdDrawLine(Vector3 point1, Vector3 point2, bool enabled) {
        DrawLine(point1, point2, enabled);
        RpcDrawLine(point1, point2, enabled);
    }

    //using networking functions rotating of the eye objects in the scene
    void EyeRotate(Vector3 eyeRotation)
    {        
        transform.Find("Left eye").transform.localEulerAngles = eyeRotation;
        transform.Find("Right eye").transform.localEulerAngles = eyeRotation;
    }
    [ClientRpc] void RpcEyeRotate(Vector3 eyeRotation) { EyeRotate(eyeRotation); }
    [Command] void CmdEyeRotate(Vector3 eyeRotation) {
        EyeRotate(eyeRotation);
        RpcEyeRotate(eyeRotation);
    }

    //using networking functions turning on and off the object highlight 
    void HighlightOn(GameObject o, Color hlColor)
    {
        o.GetComponent<Pickupable>().gazeCount += 1;
        o.GetComponent<Pickupable>().highlightColor = hlColor;
    }
    void HighlightOff(GameObject o)
    {
        o.GetComponent<Pickupable>().gazeCount -= 1;
    }
    [ClientRpc] void RpcHighlightOn(GameObject o, Color playerColor) { HighlightOn(o, playerColor); }
    [Command] void CmdHighlightOn(GameObject o, Color playerColor) { HighlightOn(o, playerColor); }
    [ClientRpc] void RpcHighlightOff(GameObject o) { HighlightOff(o); }
    [Command] void CmdHighlightOff(GameObject o) { HighlightOff(o); }

    //using networking functions setting the object's carry flags and changing the authority over it
    void CarryOn(GameObject obj)
    {
        obj.GetComponent<Pickupable>().carried = true;
        obj.GetComponent<Rigidbody>().isKinematic = true;
    }
    void CarryOff(GameObject obj)
    {
        obj.GetComponent<Pickupable>().carried = false;
        obj.GetComponent<Rigidbody>().isKinematic = false;
        if (obj.GetComponent<NetworkIdentity>().clientAuthorityOwner != null)
        {
            obj.GetComponent<NetworkIdentity>().RemoveClientAuthority(connectionToClient);
        }
    }
    [ClientRpc] void RpcCarryOn(GameObject obj) { CarryOn(obj); }
    [Command] void CmdCarryOn(GameObject obj)
    {
        obj.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);
        CarryOn(obj);
        RpcCarryOn(obj);
    }

    [ClientRpc] void RpcCarryOff(GameObject obj) { CarryOff(obj); }
    [Command] void CmdCarryOff(GameObject obj) {
        CarryOff(obj);
        RpcCarryOff(obj);
    }

    //enabling or disabling eyetracking features
    void Toggle(string what, bool state)
    {        
        switch (what)
        {
            case "canRotateEyes":
                baseplate.GetComponent<GameControl>().canRotateEyes = state;
                break;
            case "canNotifyWatched":
                baseplate.GetComponent<GameControl>().canNotifyWatched = state;
                break;
            case "canOutlineObjects":
                baseplate.GetComponent<GameControl>().canOutlineObjects = state;
                break;
            case "canOutlineObjectsShared":
                baseplate.GetComponent<GameControl>().canOutlineObjectsShared = state;
                break;
            case "canCastPointingLine":
                baseplate.GetComponent<GameControl>().canCastPointingLine = state;
                break;
            case "assymetricMode":
                baseplate.GetComponent<GameControl>().assymetricMode = state;
                break;
            case "instructions":
                baseplate.GetComponent<GameControl>().instructions = state;
                break;
            default:              
                break;
        }
    }
    [ClientRpc]
    void RpcToggle(string what, bool state) { Toggle(what,state); }
    [Command]
    void CmdToggle(string what, bool state) {
        Toggle(what,state);
        RpcToggle(what, state);
    }

    void SetInstructions() {
        if (baseplate.GetComponent<GameControl>().instructionsNumber > 1)
        {
            baseplate.GetComponent<GameControl>().instructionsNumber = 0;
        }
        else
        {
            baseplate.GetComponent<GameControl>().instructionsNumber++;
        }
    }
    [ClientRpc]
    void RpcSetInstructions() { SetInstructions(); }
    [Command]
    void CmdSetInstructions() { SetInstructions(); }

    void SetStatue()
    {
        if (baseplate.GetComponent<GameControl>().statueNumber > 2)
        {
            baseplate.GetComponent<GameControl>().statueNumber = 0;
        }
        else {
            baseplate.GetComponent<GameControl>().statueNumber++;
        }        
    }
    [ClientRpc]
    void RpcSetStatue() { SetStatue(); }
    [Command]
    void CmdSetStatue() { SetStatue(); }

    //using networking functions seting the current rotation of the carried object leveled with the ground, counting in the rotation caused by the player 
    void CarriedObjRotation()
    {
        if (carriedObject != null)
        {
            carriedObject.transform.rotation = Quaternion.Slerp(
                                                   Quaternion.Euler(carriedObject.transform.eulerAngles.x, carriedObject.transform.eulerAngles.y, carriedObject.transform.eulerAngles.z),
                                                   //Quaternion.Euler(a, b, c),
                                                   Quaternion.Euler(a, transform.eulerAngles.y + b, c),
                                                   0.3f
                                               );
        }
    }
    [Command] void CmdCarriedObjRotation() { CarriedObjRotation(); }
    

    IEnumerator Color()
    {
        //return UI lookedAt color to transparent after 1 second
        yield return new WaitForSeconds(1f);
        if (!lookedAt) lookedAtUI.GetComponent<Image>().color = new Color(255, 255, 255, 0);
    }

    //making the mouse cursor visible again, after the game stops
    void OnDestroy()
    {
        Cursor.visible = true;
    }

    //takes a 2D screen point position and returnes an average from the last 10 2D screen positions
    private Vector2 Smoothify(Vector2 point)
    {
        array_x[position] = point.x;
        array_y[position] = point.y;

        if (position == array_x.Length - 1)
        {
            position = 0;
        }
        else
        {
            position += 1;
        }

        float sum_x = 0;
        float sum_y = 0;
        for (int i = 0; i < array_x.Length; i++)
        {
            sum_x += array_x[i];
        }
        for (int j = 0; j < array_y.Length; j++)
        {
            sum_y += array_y[j];
        }

        Vector2 smoothPoint = new Vector2(sum_x / array_x.Length, sum_y / array_y.Length);

        return smoothPoint;
    }

    public static string StatusToString(bool status)
    {
        if (status) { return "off"; } else { return "on"; };
    }

    public static string InstructionsToString(int number)
    {
        switch (number) {
            case 0: return "car";
            case 1: return "pet";
            case 2: return "boat";
            default: return "car";
        }
    }

    public static string StatuesToString(int number)
    {
        switch (number)
        {
            case 0: return "off";
            case 1: return "car";
            case 2: return "pet";
            case 3: return "boat";
            default: return "off";
        }
    }

    public static void ColorText(bool status) {
        if (status) {
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().color = new Color32(229, 115, 115, 255);
        } else {
            GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().color = new Color32(129, 199, 132, 255);
        }
    }

    //re-maps a number from one range to another (Remap(2, 1, 3, 0, 10) returns 5)
    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }    
}
 
 