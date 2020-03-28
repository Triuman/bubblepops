using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class BubbleGrid : MonoBehaviour
{
    public GameObject BubblePrefab;
    public GameObject Holder;

    private const int BubblePerLine = 6; //If you change this number, you should implement scaling bubbles.

    //We will add new line at zero index. So, list order will match the UI.
    private readonly List<Bubble[]> grid = new List<Bubble[]>();
    private bool isFirstRight = false;
    private float bubbleSize;
    private float bubbleDistance;

    readonly Random random = new Random();

    // Start is called before the first frame update
    void Start()
    {
        bubbleSize = BubblePrefab.GetComponent<CircleCollider2D>().radius * 2f;
        bubbleDistance = bubbleSize / 2 * 1.73205080757f; //r * sqrt(3)
        Holder.transform.localPosition = new Vector3(-bubbleSize * (BubblePerLine - 1) / 2f, 0, 0);
        InitGrid();

    }

    // Update is called once per frame
    void Update()
    {

    }

    int GetRandomNumber()
    {
        return (int)Math.Pow(2, random.Next(1, 9));
    }

    void AddNewLine()
    {
        var newLine = new Bubble[BubblePerLine];
        isFirstRight = !isFirstRight;
        float indent = bubbleSize / 4 * (isFirstRight ? 1 : -1);
        for (int i = 0; i < BubblePerLine; i++)
        {
            newLine[i] = Instantiate(BubblePrefab, new Vector3(Holder.transform.localPosition.x + i * bubbleSize + indent, transform.localPosition.y, 0), Quaternion.identity, Holder.transform).GetComponent<Bubble>();
            newLine[i].Number = GetRandomNumber();
        }

        grid.Insert(0, newLine);
        Holder.transform.position = new Vector3(Holder.transform.position.x, Holder.transform.position.y - bubbleDistance, Holder.transform.position.z);
    }


    void InitGrid()
    {
        AddNewLine();
        AddNewLine();
        AddNewLine();
        AddNewLine();
        AddNewLine();
        AddNewLine();
        AddNewLine();
        AddNewLine();
        AddNewLine();
    }

    void AddNewBubble(int lineIndex, int columnIndex, int number)
    {
        grid[lineIndex][columnIndex] = new Bubble(number);
        //TODO: Check effect of this bubble
    }

    enum EnumDirections
    {
        Left,
        LeftUp,
        RightUp,
        Right,
        RightDown,
        LeftDown
    }

    Bubble GetBubble(int lineIndex, int columnIndex)
    {
        return grid[lineIndex][columnIndex];
    }
    Bubble GetBubble(int[] bubblePos)
    {
        return GetBubble(bubblePos[0], bubblePos[1]);
    }
    int GetBubbleNumber(int lineIndex, int columnIndex)
    {
        return grid[lineIndex][columnIndex].Number;
    }

    int GetBubbleNumber(int[] bubblePos)
    {
        return GetBubbleNumber(bubblePos[0], bubblePos[1]);
    }

    void CheckNeighbors(int[] bubble, EnumDirections ignoredDirection, List<int[]> sameBubbleList)
    {
        CheckNeighbors(bubble[0], bubble[1], ignoredDirection, sameBubbleList);
    }


    void CheckNeighbors(int lineIndex, int columnIndex, EnumDirections ignoredDirection, List<int[]> sameBubbleList)
    {
        sameBubbleList = sameBubbleList ?? new List<int[]>();
        int currentBubbleValue = GetBubbleNumber(lineIndex, columnIndex);

        //find if to right or left
        bool isRight = isFirstRight ? lineIndex % 2 == 0 : lineIndex % 2 == 1;

        //calculate touching bubble indices starting from left up
        //iterate over them
        //if you find same number, check that number's area too but only the ones this one is not touching
        if (ignoredDirection == EnumDirections.RightUp || ignoredDirection == EnumDirections.Right ||
            ignoredDirection == EnumDirections.RightDown)
        {
            var leftBubblePos = new[] { lineIndex, columnIndex - 1 };
            if (GetBubbleNumber(leftBubblePos) == currentBubbleValue)
            {
                sameBubbleList.Add(leftBubblePos);
                CheckNeighbors(leftBubblePos, EnumDirections.Right, sameBubbleList);
            }
        }

        if (ignoredDirection == EnumDirections.Right || ignoredDirection == EnumDirections.RightDown ||
            ignoredDirection == EnumDirections.LeftDown)
        {
            var leftUpBubblePos = new[] { lineIndex + 1, columnIndex + (isRight ? 0 : -1) };
            if (GetBubbleNumber(leftUpBubblePos) == currentBubbleValue)
            {
                sameBubbleList.Add(leftUpBubblePos);
                CheckNeighbors(leftUpBubblePos, EnumDirections.RightDown, sameBubbleList);
            }
        }

        if (ignoredDirection == EnumDirections.Left || ignoredDirection == EnumDirections.LeftDown ||
            ignoredDirection == EnumDirections.RightDown)
        {
            var rightUpBubblePos = new[] { lineIndex + 1, columnIndex + (isRight ? 1 : 0) };
            if (GetBubbleNumber(rightUpBubblePos) == currentBubbleValue)
            {
                sameBubbleList.Add(rightUpBubblePos);
                CheckNeighbors(rightUpBubblePos, EnumDirections.LeftDown, sameBubbleList);
            }
        }

        if (ignoredDirection == EnumDirections.LeftUp || ignoredDirection == EnumDirections.Left ||
            ignoredDirection == EnumDirections.LeftDown)
        {
            var rightBubblePos = new[] { lineIndex + 1, columnIndex + 1 };
            if (GetBubbleNumber(rightBubblePos) == currentBubbleValue)
            {
                sameBubbleList.Add(rightBubblePos);
                CheckNeighbors(rightBubblePos, EnumDirections.Left, sameBubbleList);
            }
        }

        if (ignoredDirection == EnumDirections.Left || ignoredDirection == EnumDirections.LeftUp ||
            ignoredDirection == EnumDirections.RightUp)
        {
            var rightDownBubblePos = new[] { lineIndex - 1, columnIndex + (isRight ? 1 : 0) };
            if (GetBubbleNumber(rightDownBubblePos) == currentBubbleValue)
            {
                sameBubbleList.Add(rightDownBubblePos);
                CheckNeighbors(rightDownBubblePos, EnumDirections.LeftUp, sameBubbleList);
            }
        }

        if (ignoredDirection == EnumDirections.LeftUp || ignoredDirection == EnumDirections.RightUp ||
            ignoredDirection == EnumDirections.Right)
        {
            var leftDownBubblePos = new[] { lineIndex - 1, columnIndex + (isRight ? 0 : -1) };
            if (GetBubbleNumber(leftDownBubblePos) == currentBubbleValue)
            {
                sameBubbleList.Add(leftDownBubblePos);
                CheckNeighbors(leftDownBubblePos, EnumDirections.RightUp, sameBubbleList);
            }
        }


        //TODO: We also need to decide where to connect while doing this calculations.

    }




}