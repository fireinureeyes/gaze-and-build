using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssymetricMode : MonoBehaviour {

    public GameObject baseplate;
    private bool lastStateMode;
    private bool lastStateInstructions;
    private int lastInstructions;
    private int lastStatue;

    public Material[] car;
    public Material[] pet;
    public Material[] boat;

    // Use this for initialization
    void Start () {
        lastStateMode = baseplate.GetComponent<GameControl>().assymetricMode;
        lastStateInstructions = baseplate.GetComponent<GameControl>().instructions;
        lastInstructions = baseplate.GetComponent<GameControl>().instructionsNumber;
        lastStatue = baseplate.GetComponent<GameControl>().statueNumber;
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
	
	// Update is called once per frame
	void Update () {
        if (gameObject.name == "Fence")
        {
            if (lastStateMode != baseplate.GetComponent<GameControl>().assymetricMode)
            {
                lastStateMode = baseplate.GetComponent<GameControl>().assymetricMode;
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(lastStateMode);
                }            
            }
        } else {
            if (gameObject.name == "Instructions")
            {
                if (lastStateInstructions != baseplate.GetComponent<GameControl>().instructions)
                {
                    lastStateInstructions = baseplate.GetComponent<GameControl>().instructions;
                    foreach (Transform child in transform)
                    {
                        child.gameObject.SetActive(lastStateInstructions);
                    }
                }
                if (lastInstructions != baseplate.GetComponent<GameControl>().instructionsNumber)
                {
                    lastInstructions = baseplate.GetComponent<GameControl>().instructionsNumber;
                    switch (lastInstructions)
                    {
                        case 0:
                            AssignMaterials(car);
                            break;
                        case 1:
                            AssignMaterials(pet);
                            break;
                        case 2:
                            AssignMaterials(boat);
                            break;
                        default:
                            AssignMaterials(car);
                            break;
                    }
                }
            }
            else {
                if (lastStatue != baseplate.GetComponent<GameControl>().statueNumber)
                {
                    lastStatue = baseplate.GetComponent<GameControl>().statueNumber;
                    switch (lastStatue)
                    {
                        case 0:
                            foreach (Transform child in transform)
                            {
                                child.gameObject.SetActive(false);
                            }
                            break;
                        case 1:
                            if (gameObject.name == "Car")
                            {
                                foreach (Transform child in transform)
                                {
                                    child.gameObject.SetActive(true);
                                }
                            }
                            else
                            {
                                foreach (Transform child in transform)
                                {
                                    child.gameObject.SetActive(false);
                                }
                            }
                            break;
                        case 2:
                            if (gameObject.name == "Pet")
                            {
                                foreach (Transform child in transform)
                                {
                                    child.gameObject.SetActive(true);
                                }
                            }
                            else
                            {
                                foreach (Transform child in transform)
                                {
                                    child.gameObject.SetActive(false);
                                }
                            }
                            break;
                        case 3:
                            if (gameObject.name == "Boat")
                            {
                                foreach (Transform child in transform)
                                {
                                    child.gameObject.SetActive(true);
                                }
                            }
                            else
                            {
                                foreach (Transform child in transform)
                                {
                                    child.gameObject.SetActive(false);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }
                
    }

    private void AssignMaterials(Material[] materials) {
        for (int i = 0; i < 8; i++)
        {
            transform.GetChild(i).gameObject.GetComponent<MeshRenderer>().material = materials[i];
        }        
    }
}
