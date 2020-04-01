using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopEffect : MonoBehaviour
{
    private float timeLeftToDie;

    // Start is called before the first frame update
    void Start()
    {
        timeLeftToDie = 2;
    }

    // Update is called once per frame
    void Update()
    {
        timeLeftToDie -= Time.deltaTime;
        if (timeLeftToDie <= 0)
        {
            Destroy(gameObject);
        }
    }
}
