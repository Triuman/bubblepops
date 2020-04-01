using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = Unity.Mathematics.Random;

public class LevelMeter : MonoBehaviour
{
    public SpriteRenderer LevelMeterSpriteRenderer;
    public SpriteRenderer CurrentLevelSpriteRenderer;
    public SpriteRenderer NextLevelSpriteRenderer;
    public Text TxtCurrentLevel;
    public Text TxtNextLevel;

    private float currentLevelFloat;
    private int currentLevelInt;
    public float Level
    {
        get => currentLevelFloat;
        set
        {
            var newLevelFloat = value + 1; //we dont want to start from zero
            var newLevelInt = (int)Mathf.Floor(newLevelFloat);
            currentLevelFloat = newLevelFloat;
            if (currentLevelInt != newLevelInt)
            {
                currentLevelInt = newLevelInt;
                TxtCurrentLevel.text = currentLevelInt.ToString();
                TxtNextLevel.text = (currentLevelInt + 1).ToString();
                var currentLevelColor = Globals.GetLevelColor(currentLevelInt);
                LevelMeterSpriteRenderer.material.SetColor("_FillColor", currentLevelColor);
                CurrentLevelSpriteRenderer.color = currentLevelColor;
                NextLevelSpriteRenderer.color = Globals.GetLevelColor(currentLevelInt+1);
            }
            LevelMeterSpriteRenderer.material.SetFloat("_Value", currentLevelFloat - currentLevelInt);
        }
    }
}
