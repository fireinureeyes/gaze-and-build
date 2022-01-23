using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class Pickupable : NetworkBehaviour
{
    [SyncVar]
    public bool carried;
    [SyncVar]
    public Color myColor;
    [SyncVar]
    public Color highlightColor;
    [SyncVar]
    public int gazeCount;

    private bool done;
    private float grid;
    private float noise;
    private float correction1;
    private float correction2;
    private bool once;
    private int _gazeCount;
    public Renderer rend;
    public Material[] mat;

    //saving a reference to the baseplate with Game Control script
    public GameObject baseplate;

    void Start()
    {
        //variables initialization 
        done = false;
        highlightColor = UnityEngine.Color.red;
        gazeCount = 0;
        once = true;
        rend = GetComponent<Renderer>();
        mat = rend.materials; 
        mat[1].shader = Shader.Find("3y3net/GlowHidden");

        //setting up the grid for specific lego object blocks
        grid = 0.251f;
        correction1 = 0;
        correction2 = 0;
        switch (gameObject.name)
        {
            case "1x2(Clone)": correction1 = 0.125f;  break;
            case "2x2(Clone)": correction1 = 0.125f; correction2 = 0.125f; break;
            case "4x2(Clone)": correction1 = 0.125f; correction2 = 0.125f; break;
            case "3x2(Clone)": correction1 = 0; correction2 = 0.125f; break;
            case "6x1(Clone)": correction1 = 0.125f; break;
            case "6x2(Clone)": correction1 = 0.125f; correction2 = 0.125f; break;
            case "3x1(Clone)": correction1 = 0; break;
            case "4x1(Clone)": correction1 = 0.125f; break;
            default: correction1 = 0; break;
        }       
        carried = false;
        GetComponent<Renderer>().material.color = myColor;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<MeshRenderer>() != null) child.GetComponent<Renderer>().material.color = myColor;
        }
    }

    //preventing jumping of the object after a fall
    void FixedUpdate()
    {
        var currentVelocity = GetComponent<Rigidbody>().velocity;
        if (currentVelocity.y <= 0f)
            return;
        currentVelocity.y = 0f;
        GetComponent<Rigidbody>().velocity = currentVelocity;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        //make the object not fall over (on Y axis) once it touched the ground or other object
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY;

        //make the object ignore the collisions with player prefab once it touched the ground or other object
        if (collision.gameObject.tag == "Player")
        {
            Physics.IgnoreCollision(collision.collider, GetComponent<BoxCollider>());
        }
    }

    void Update()
    {
        //change the highlight color according to who gazes at it
        if (baseplate.GetComponent<GameControl>().canOutlineObjects)
        {
            _gazeCount = (gazeCount > 2) ? 2 : gazeCount;
            switch (_gazeCount)
                {
                //nobody = no highlight
                case 0:
                    mat[1].SetFloat("_Outline", 0f);
                    break;
                //one player = the player specific color
                case 1:
                    mat[1].SetColor("_GlowColor", highlightColor);
                    mat[1].SetFloat("_Outline", 0.15f);
                    break;
                //two players = highlight with a distinct
                case 2:                        
                    if (baseplate.GetComponent<GameControl>().canOutlineObjectsShared)
                    {
                        mat[1].SetColor("_GlowColor", Color.red);
                        mat[1].SetFloat("_Outline", 0.4f);
                    }
                    else {
                        mat[1].SetColor("_GlowColor", highlightColor);
                        mat[1].SetFloat("_Outline", 0.15f);
                    }
                    break;
                default:
                    mat[1].SetFloat("_Outline", 0f);
                    break;
                }
        } else
        {
            mat[1].SetFloat("_Outline", 0f);
        }
        if (carried == true)
        {
            //make the object ignore the collisions once it is carried by a player
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            GetComponent<BoxCollider>().enabled = false;
            once = true;
        }
        else
        {
            //make the object completely static (appart from falling downwards) while its not being carried
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            GetComponent<BoxCollider>().enabled = true;
        }

        //adjust the object's rotation and position adjusted to a grid once it's dropped
        if (carried == false && done == false)
        {            
            done = true;
            transform.rotation = Quaternion.Euler(Mathf.Round(transform.eulerAngles.x / 90) * 90, Mathf.Round(transform.eulerAngles.y / 90) * 90, Mathf.Round(transform.eulerAngles.z / 90) * 90);

            if (once == true)
            {
                once = false;
                if ((Mathf.Round(transform.eulerAngles.y) == 90) || (Mathf.Round(transform.eulerAngles.y) == 270) || (Mathf.Round(transform.eulerAngles.y) == -270) || (Mathf.Round(transform.eulerAngles.y) == -90))
                {
                    transform.position = new Vector3(Mathf.Round(transform.position.x / grid) * grid - correction2, transform.position.y, Mathf.Round(transform.position.z / grid) * grid - correction1);
                } else
                {
                    transform.position = new Vector3(Mathf.Round(transform.position.x / grid) * grid - correction1, transform.position.y, Mathf.Round(transform.position.z / grid) * grid - correction2);
                }
            }
        }
        else
        {
            done = false;
        }
    }
}
