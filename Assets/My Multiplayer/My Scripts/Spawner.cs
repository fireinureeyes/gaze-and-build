using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    public GameObject prefab4x2;
    public GameObject prefab3x2;
    public GameObject prefab2x2;
    public GameObject prefab6x1;
    public GameObject prefab1x2;
    public GameObject prefab1x1;
    public GameObject prefab1x1r;
    public GameObject prefab3x1;
    public GameObject prefab4x1;
    public GameObject prefab6x2;
    private GameObject legoPrefab;
    public int numberOfLegos;
    public float coveredAreaSquared;
    public float uncoveredArea = 9.0f;

    private float xCoor;
    private float zCoor;

    public override void OnStartServer()
    {
        for (int i = 0; i < numberOfLegos; i++)
        {
            xCoor = Random.Range(-coveredAreaSquared, coveredAreaSquared);
            zCoor = Random.Range(-coveredAreaSquared, coveredAreaSquared);

            while (InInterval(xCoor, -uncoveredArea, uncoveredArea) && InInterval(zCoor, -uncoveredArea, uncoveredArea)) {
                xCoor = Random.Range(-coveredAreaSquared, coveredAreaSquared);
                zCoor = Random.Range(-coveredAreaSquared, coveredAreaSquared);
            };

            Vector3 spawnPosition = new Vector3(xCoor, 0.0f, zCoor);
            Quaternion spawnRotation = Quaternion.Euler(0.0f, Random.Range(0, 180), 0.0f);
            int randomize = Random.Range(0, 10);
            switch (randomize)
            {
                case 0: legoPrefab = prefab4x2; break;
                case 1: legoPrefab = prefab3x2; break;
                case 2: legoPrefab = prefab2x2; break;
                case 3: legoPrefab = prefab6x1; break;
                case 4: legoPrefab = prefab1x2; break;
                case 5: legoPrefab = prefab1x1; break;
                case 6: legoPrefab = prefab1x1r; break;
                case 7: legoPrefab = prefab3x1; break;
                case 8: legoPrefab = prefab4x1; break;
                case 9: legoPrefab = prefab6x2; break;
                default: legoPrefab = prefab4x2; break;
            }
            GameObject obj = Instantiate(legoPrefab, spawnPosition, spawnRotation);
            Color color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            obj.GetComponent<Pickupable>().myColor = color;
            
            NetworkServer.Spawn(obj);
        }
    }

    public bool InInterval(float x, float min, float max)
    {
        return x >= min && x <= max;
    }
}
