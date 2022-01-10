using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MergeMeals
{
    public class FloorManager : MonoBehaviour
    {

        public GameObject floorTilePrefab;
        public GameObject [,] floorTiles;

        // Start is called before the first frame update
        void Awake()
        {
            //PositionVirtualCamera() moved to LevelManager -DS 09/04
        }

        void Start()
        {
            //MakeFloor() is now called by LevelManager.

        }
        public void MakeFloor(int width, int height)
        {
            int colorCount = 0;
            floorTiles = new GameObject[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Vector3 pos = new Vector3(i, 0, j);
                    floorTiles[i, j] = Instantiate(floorTilePrefab, pos, Quaternion.identity) as GameObject;
                    floorTiles[i, j].transform.SetParent(this.transform);
                    if (colorCount % 2 == 0)
                    {
                        floorTiles[i, j].GetComponentInChildren<MeshRenderer>().materials[0].color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
                    }
                    else
                    {
                        floorTiles[i, j].GetComponentInChildren<MeshRenderer>().materials[0].color = new Color(0.3f, 0.3f, 0.3f, 1.0f);
                    }
                    colorCount++;
                }
                if (height > 1 && width % 2 == 0) colorCount++;
            }
        }
    }
}
