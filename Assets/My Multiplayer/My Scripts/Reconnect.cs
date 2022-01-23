using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Reconnect : MonoBehaviour
{
    //saving network manager settings in case of a reconnect
    public string address = "";
    public int port = 0;
    private bool once = false;
    public bool onPurpose = false;

    // Update is called once per frame
    void Update() {
        //saving the ip address and port in case needed for a reconnect
        if ((address == "") && (port == 0) && (GameObject.FindGameObjectsWithTag("Player").Length != 0))
        {
            address = GameObject.FindGameObjectsWithTag("Player")[0].GetComponent<PlayerControler>().address;
            port = GameObject.FindGameObjectsWithTag("Player")[0].GetComponent<PlayerControler>().port;            
        }
        //if disconnected due to the failure, try to connect again in 20 second intervals
        if (!onPurpose) { 
            if ((address != "") && (GameObject.FindGameObjectsWithTag("Player").Length == 0))
            {
                if (!once)
                {
                    NetworkManager.singleton.StartClient();
                    once = true;
                    StartCoroutine(Repeat());
                    if (GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text != "") GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = ""; 
                    GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().color = new Color32(229, 115, 115, 255);
                    GameObject.FindGameObjectsWithTag("STATUS")[0].GetComponent<Text>().text = "Reconnecting";
                }
            }
        }
    }

    //coroutine controlling the connection retrying
    IEnumerator Repeat()
    {
        yield return new WaitForSeconds(10f);
        once = false;
    }
}
