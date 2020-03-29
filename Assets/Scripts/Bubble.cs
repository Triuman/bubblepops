using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// This bubble will destroy itself when hit the bottom or popped and play an animation along with a sound.
/// </summary>
public class Bubble : MonoBehaviour
{
    public List<GameObject> CirclePrefabs;


    //We will use this Id to quickly find a hit bubble in the grid.
    public int Id { get; private set; }
    public int Number;
    private GameObject circle;
    
    private int defaultLayer = 0;
    public bool IgnoreRaycast
    {
        get => gameObject.layer == 2 && circle.layer == 2;
        set
        {
            InitCircle();
            gameObject.layer = value ? 2 : defaultLayer;
            circle.layer = value ? 2 : defaultLayer;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        Id = Random.Range(1, 10000000);
        InitCircle();
    }

    void InitCircle()
    {
        if (circle != null)
            return;
        circle = Instantiate(CirclePrefabs[Globals.NumberIndexDic[Number]], transform.position, Quaternion.identity, transform);
        defaultLayer = gameObject.layer;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
