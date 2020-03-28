using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This bubble will destroy itself when hit the bottom or popped and play an animation along with a sound.
/// </summary>
public class Bubble : MonoBehaviour
{
    public List<GameObject> CirclePrefabs;

    static Dictionary<int, int> numberIndexDic = new Dictionary<int, int>()
    {
        { 2, 0 },
        { 4, 1 },
        { 8, 2 },
        { 16, 3 },
        { 32, 4 },
        { 64, 5 },
        { 128, 6 },
        { 256, 7 },
        { 512, 8 },
        { 1024, 9 },
        { 2048, 10 },
    };

    public int Number;

    public Bubble(int number)
    {
        Number = number;
    }

    // Start is called before the first frame update
    void Start()
    {
        Instantiate(CirclePrefabs[numberIndexDic[Number]], transform.position, Quaternion.identity, transform);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
