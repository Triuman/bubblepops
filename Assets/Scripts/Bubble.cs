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

    public event Action Arrived;

    //We will use this Id to quickly find a hit bubble in the grid.
    public int Id { get; private set; }
    public int Number = 2;
    public float ShootSpeed;
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
        ShootSpeed = 10;
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

    private List<Vector2> shootDestinations = new List<Vector2>();
    public void Shoot(List<Vector2> shootDestinations)
    {
        this.shootDestinations = shootDestinations;
    }

    // Update is called once per frame
    void Update()
    {
        if (shootDestinations.Count > 0)
        {
            transform.position = Vector3.MoveTowards(transform.position, shootDestinations[0], ShootSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, shootDestinations[0]) < 0.001f)
            {
                shootDestinations.RemoveAt(0);
                if(shootDestinations.Count == 0)
                    Arrived?.Invoke();
            }
        }
    }
}
