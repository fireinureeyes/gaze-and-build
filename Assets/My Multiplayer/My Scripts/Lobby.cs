using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class Lobby : MonoBehaviour
{
    //references to the lobby GUI elements
    public Button playButton;
    public Toggle toggle;
    public GameObject input;
    public GameObject nameInput;
    public GameObject cancel;
    public GameObject pause;
    public GameObject quit;
    public GameObject continueButton;
    public GameObject menu;
    public GameObject message;
    public GameObject canvas;
    public GameObject es;
    public GameObject disconnect;
    public GameObject warning;
    public Sprite error;
    public Sprite ok;

    //disables the player controls during the pause screen
    public bool userInputEnabled;

    //helper variable describing if the user entered a valid connection configuration 
    private bool canWeConnect;

    //delay of the cancel button display
    private bool timeHasPassed;

    //count repeat connection trials
    private int count;

    //press button only once
    private bool playPressed;
    private bool cancelPressed;
    private bool disconnectPressed;

    private int numberOfPlayers;

    private void Start()
    {        
        toggle.GetComponent<Toggle>().isOn = KeepDataOnReload.toggleState;
        input.GetComponent<InputField>().text = KeepDataOnReload.inputText;
        nameInput.GetComponent<InputField>().text = KeepDataOnReload.playerName;
        if (KeepDataOnReload.warning == true)
        {
            KeepDataOnReload.warning = false;
            toggle.GetComponent<Toggle>().isOn = false;
            warning.SetActive(true);
        }

        //hiding the network manager hud
        GameObject.FindGameObjectsWithTag("NM")[0].GetComponent<NetworkManagerHUD>().enabled = false;

        //variables initialization
        userInputEnabled = true;
        canWeConnect = false;
        timeHasPassed = false;
        count = 0;
        playPressed = false;
        cancelPressed = false;
        disconnectPressed = false;
        numberOfPlayers = 0;

        //setting button callbacks and visibility in GUI
        playButton.onClick.AddListener(Play);
        quit.GetComponent<Button>().onClick.AddListener(Quit);
        continueButton.GetComponent<Button>().onClick.AddListener(Continue);
        cancel.GetComponent<Button>().onClick.AddListener(Cancel);
        disconnect.GetComponent<Button>().onClick.AddListener(Disconnect);
        cancel.SetActive(false);
        pause.SetActive(false);
        if (toggle.GetComponent<Toggle>().isOn)
        {
            input.SetActive(true);
        } else
        {
            input.SetActive(false);
        }

        //setting toggle element callback
        toggle.GetComponent<Toggle>().onValueChanged.AddListener(delegate {
            Change(toggle);
        });
    }

    // Update is called once per frame
    void Update()
    {
        //react on tab press by switching the toggle
        if (Input.GetKeyDown("tab"))
        {
            if (playButton.gameObject.activeSelf == true)
            {
                toggle.GetComponent<Toggle>().isOn = !toggle.GetComponent<Toggle>().isOn;
            }            
        }
        //react on esc press by opening or closing the pause screen
        if (Input.GetKeyDown("escape")) {
            if (pause.activeSelf == false) { 
                if (menu.activeSelf == false)
                {
                    disconnect.SetActive(true);
                    disconnectPressed = false;
                    pause.transform.GetChild(0).gameObject.SetActive(false); //background
                    GameObject.FindGameObjectsWithTag("GUITEXTURE")[0].GetComponent<RawImage>().enabled = false;
                }
                else
                {
                    disconnect.SetActive(false);
                    pause.transform.GetChild(0).gameObject.SetActive(true); //background
                }
                pause.SetActive(true);
                //making the eyetracking status message appear on the top
                canvas.SetActive(false);
                canvas.SetActive(true);
                Cursor.visible = true;
                userInputEnabled = false;
            } else
            {
                Continue();
            }
        }
        if (Input.GetKeyDown("d"))
        {
            if (pause.activeSelf == true)
            {
                Disconnect();
            }
        }
        if (Input.GetKeyDown("return")) {
            if (pause.activeSelf == true)
            {
                Quit();
            }
            else { 
                if (menu.activeSelf == true)
                {
                    es.GetComponent<EventSystem>().SetSelectedGameObject(null);
                    if (playButton.gameObject.activeSelf == true)
                    {                    
                        Play();
                    }
                    else
                    {
                        if (timeHasPassed) { 
                            Cancel();
                        }
                    }
                }
            }
        }
    }

    //controling the toggle button
    void Change(Toggle toggle)
    {
        if (toggle.GetComponent<Toggle>().isOn == true)
        {
            if (input.activeSelf == false)
            {
                warning.SetActive(false);
                input.SetActive(true);
                es.GetComponent<EventSystem>().SetSelectedGameObject(input);
            }
        }
        else {
            if (input.activeSelf == true)
            {
                input.SetActive(false);
            }
        }
        es.GetComponent<EventSystem>().SetSelectedGameObject(null);
    }

    //close the game window after pressing the quit button
    void Quit()
    {
        Application.Quit();
    }
     
    //close the connection
    void Disconnect()
    {
        if (!disconnectPressed) {
            disconnectPressed = true;
            GetComponent<Reconnect>().onPurpose = true;
            GetComponent<NetworkManager>().StopClient();
            GetComponent<NetworkManager>().StopServer();
            //Application.LoadLevel(Application.loadedLevel); --obsolete
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    //hide the pause screen after pressing the continue button
    void Continue()
    {
        if (menu.activeSelf == false) GameObject.FindGameObjectsWithTag("GUITEXTURE")[0].GetComponent<RawImage>().enabled = true;
        if (GameObject.FindGameObjectsWithTag("Player").Length != 0) { 
            Cursor.visible = false;
        }
        pause.SetActive(false);
        userInputEnabled = true;
    }

    //start the game after pressing the play button
    void Play()
    {
        if (!playPressed)
        {
            playPressed = true;
            GetComponent<Reconnect>().onPurpose = false;
            if (toggle.GetComponent<Toggle>().isOn == false)
            {
                //is host 
                KeepDataOnReload.playerName = nameInput.GetComponent<InputField>().text;
                KeepDataOnReload.toggleState = toggle.GetComponent<Toggle>().isOn;
                KeepDataOnReload.inputText = input.GetComponent<InputField>().text;
                NetworkManager.singleton.StartHost();
                StartCoroutine("Wait");
                StartCoroutine("Check");
            }
            else
            {
                //is client
                //if no ip address is entered, try to connect to localhost
                if (input.GetComponent<InputField>().text == "")
                {
                    NetworkManager.singleton.networkAddress = "localhost";
                    canWeConnect = true;
                }
                else
                {
                    //check if the entered ip is valid and try to connect to it
                    if ((input.GetComponent<InputField>().text == "localhost") || (ValidateIP(input.GetComponent<InputField>().text))) {
                        NetworkManager.singleton.networkAddress = input.GetComponent<InputField>().text;
                        canWeConnect = true;
                    }
                }                
                if (canWeConnect == true)
                {
                    KeepDataOnReload.playerName = nameInput.GetComponent<InputField>().text;
                    KeepDataOnReload.toggleState = toggle.GetComponent<Toggle>().isOn;
                    KeepDataOnReload.inputText = input.GetComponent<InputField>().text;
                    input.GetComponent<Image>().sprite = ok;
                    playButton.gameObject.SetActive(false);
                    toggle.GetComponent<Toggle>().interactable = false;
                    input.GetComponent<InputField>().interactable = false;
                    nameInput.GetComponent<InputField>().interactable = false;
                    StartCoroutine("ShowButton");
                    message.GetComponent<Text>().enabled = true;
                    NetworkManager.singleton.StartClient();
                    StartCoroutine("Wait");
                    StartCoroutine("Repeat");
                }
                else {
                    //if the entered adress is incorrect, display error in GUI for the user
                    input.GetComponent<Image>().sprite = error;
                    es.GetComponent<EventSystem>().SetSelectedGameObject(input);
                    playPressed = false;
                }
            }
        }
    }

    //stops the trials to connect
    void Cancel() {
        if (!cancelPressed) {
            cancelPressed = true;
            StopAllCoroutines();
            NetworkManager.singleton.StopClient();
            NetworkManager.singleton.StopServer();
            StartCoroutine("Wait");
            canWeConnect = false;
            cancel.SetActive(false);
            playButton.gameObject.SetActive(true);
            playPressed = false;
            toggle.GetComponent<Toggle>().interactable = true;
            input.GetComponent<InputField>().interactable = true;
            nameInput.GetComponent<InputField>().interactable = true;
            message.GetComponent<Text>().enabled = false;
            es.GetComponent<EventSystem>().SetSelectedGameObject(input);
            timeHasPassed = false;
            count = 0;
        }
    }

    //wait until the scene loads and hide the lobby screen
    IEnumerator Wait()
    {
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Player").Length != 0);
        StopAllCoroutines();
        menu.SetActive(false);
    }

    //wait until the scene loads and hide the lobby screen
    IEnumerator Check()
    {
        yield return new WaitForSeconds(1f);
        if (GameObject.FindGameObjectsWithTag("Player").Length == 0) {
            KeepDataOnReload.warning = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    //wait until the scene loads and hide the lobby screen
    IEnumerator ShowButton()
    {
        yield return new WaitForSeconds(5f);
        cancel.SetActive(true);
        cancelPressed = false;
        timeHasPassed = true;
    }

    //try to connect again if the previous trial failed
    IEnumerator Repeat()
    {
        yield return new WaitForSeconds(10f);
        if (GameObject.FindGameObjectsWithTag("Player").Length == 0)
        {
            if (count < 4) {
                count += 1;
                NetworkManager.singleton.StartClient();
                StartCoroutine("Repeat");
            } else
            {
                Cancel();
                input.GetComponent<Image>().sprite = error;
            }
        }
    }

    //parses the given string and returns true if it is a valit IP address
    bool ValidateIP(string strIP) {
        //  Split string by ".", check that array length is 3
        char chrFullStop = '.';
        string[] arrOctets = strIP.Split(chrFullStop);
        if (arrOctets.Length != 4)
        {
            return false;
        }
        //  Check each substring checking that the int value is less than 255 and that is char[] length is !> 2
        int MAXVALUE = 255;
        int temp; // Parse returns Int32
        foreach (string strOctet in arrOctets)
        {
            if (strOctet.Length > 3)
            {
                return false;
            }

            temp = int.Parse(strOctet);
            if (temp > MAXVALUE)
            {
                return false;
            }
        }
        return true;
    }
}