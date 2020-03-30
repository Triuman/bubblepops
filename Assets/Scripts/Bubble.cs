using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// This bubble will destroy itself when hit the bottom or popped and play an animation along with a sound.
/// </summary>
public class Bubble : MonoBehaviour
{
    public List<GameObject> CirclePrefabs;
    public GameObject PopEffectPrefab;

    public event Action<Bubble> Arrived;

    //We will use this Id to quickly find a hit bubble in the grid.
    public int Id { get; private set; }
    public int Number = 2;
    private GameObject circle;
    
    private int defaultLayer = 0;


    private bool ignoreRaycast = false;
    public bool IgnoreRaycast
    {
        get => gameObject.layer == 2 && circle.layer == 2;
        set
        {
            ignoreRaycast = value;
            if (circle != null)
            {
                gameObject.layer = ignoreRaycast ? 2 : defaultLayer;
                circle.layer = ignoreRaycast ? 2 : defaultLayer;
            }
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
        Debug.Log(Number);
        circle = Instantiate(CirclePrefabs[Globals.NumberIndexDic[Number]], transform.position, Quaternion.identity, transform);
        defaultLayer = gameObject.layer;
        gameObject.layer = ignoreRaycast ? 2 : defaultLayer;
        circle.layer = ignoreRaycast ? 2 : defaultLayer;
    }


    private List<Vector3> targetPositions = new List<Vector3>();
    private float moveSpeed = 0;
    public void MoveTo(List<Vector3> targetPositions, float speed)
    {
        this.targetPositions = targetPositions.ToList();
        moveSpeed = speed;
    }

    private List<Vector2> targetScales = new List<Vector2>();
    private float scaleSpeed = 0;
    public void ScaleTo(List<Vector2> targetScales, float speed)
    {
        this.targetScales = targetScales.ToList();
        scaleSpeed = speed;
    }

    private float secondsToPop;
    private bool isGoingToPop = false;
    public void Pop(float delay = 0)
    {
        secondsToPop = delay;
        isGoingToPop = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isGoingToPop)
        {
            secondsToPop -= Time.deltaTime;
            if (secondsToPop <= 0)
            {
                isGoingToPop = false;
                ParticleSystem.MainModule settings = PopEffectPrefab.GetComponent<ParticleSystem>().main;
                settings.startColor = new Color(Globals.NumberColorDic[Number].r, Globals.NumberColorDic[Number].g, Globals.NumberColorDic[Number].b, 1);
                Instantiate(PopEffectPrefab, transform.parent.transform.parent.transform.parent, true).transform.position = transform.position;
                Destroy(gameObject);
            }
        }

        if (targetScales.Count > 0)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetScales[0], scaleSpeed * Time.deltaTime * Globals.AnimationSpeedScale);
            if (Vector3.Distance(transform.localScale, targetScales[0]) < 0.001f)
            {
                targetScales.RemoveAt(0);
                if (targetScales.Count == 0)
                    Arrived?.Invoke(this);
            }
        }
        if (targetPositions.Count > 0)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPositions[0], moveSpeed * Time.deltaTime * Globals.AnimationSpeedScale);
            if (Vector3.Distance(transform.localPosition, targetPositions[0]) < 0.001f)
            {
                targetPositions.RemoveAt(0);
                if(targetPositions.Count == 0)
                    Arrived?.Invoke(this);
            }
        }
    }
}
