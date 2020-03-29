﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class BubbleGrid : MonoBehaviour
{
    public GameObject BubblePrefab;
    public GameObject Holder;
    public GameObject EmptyCircle;
    public RectTransform GameAreaRectTransform;
    public LineRenderer AimLineRenderer;

    private const int BubblePerLine = 6; //If you change this number, you should implement scaling bubbles.

    //We will add new line at zero index. So, list order will match the UI.
    private readonly List<Bubble[]> grid = new List<Bubble[]>();
    private bool isFirstRight = false;
    private float bubbleSize;
    private float bubbleVerticalDistance;
    private Vector2 shooterPos;
    Vector2 _emptyCircleGridPos = new Vector2(-1, -1);
    private bool isMouseDown = false;
    Vector2? touchPositionWorld = null;

    private Bubble shooterCurrentBubble;
    private Bubble shooterNextBubble;

    readonly Random random = new Random();


    // Start is called before the first frame update
    void Start()
    {
        shooterPos = new Vector2(0, GameAreaRectTransform.position.y - GameAreaRectTransform.rect.size.y * GameAreaRectTransform.parent.localScale.y / 2f);
        bubbleSize = BubblePrefab.GetComponent<CircleCollider2D>().radius * 2f;
        bubbleVerticalDistance = bubbleSize / 2 * 1.73205080757f; //r * sqrt(3)
        //Holder.transform.localPosition = new Vector3(-bubbleSize * (BubblePerLine - 1) / 2f, Holder.transform.localPosition.y, 0);
        InitGrid();
        CreateNewCurrentBubble();

    }

    void CreateNewCurrentBubble()
    {
        shooterCurrentBubble = Instantiate(BubblePrefab, Holder.transform, true).GetComponent<Bubble>();
        shooterCurrentBubble.transform.position = shooterPos;
        shooterCurrentBubble.Number = GetRandomNumber();
        shooterCurrentBubble.IgnoreRaycast = true;
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            var shootDestinations = new List<Vector2>();
            if (AimLineRenderer.positionCount > 2)
                shootDestinations.Add(AimLineRenderer.GetPosition(1));
            shootDestinations.Add(new Vector2(Holder.transform.position.x + _emptyCircleGridPos[1] * bubbleSize + GetLineIndent((int)_emptyCircleGridPos[0]), transform.position.y - (_emptyCircleGridPos[0] + 1) * bubbleVerticalDistance));

            ShootTheBubble(shootDestinations);
        }
        RaycastAimLine();

    }

    void InitGrid()
    {
        //AddNewLine();
    }


    void RaycastAimLine()
    {
        AimLineRenderer.positionCount = 0;
        GetTouchPositionWorld();
        if (touchPositionWorld == null)
        {
            EmptyCircle.gameObject.SetActive(false);
            return;
        }

        //It raycasts until either it hits a bubble or topwall and sends the hit2D or wall for the second time and sends null.
        //And will draw the aim line.
        List<Vector3> positions = new List<Vector3> { shooterPos };
        var startPoint = shooterPos;
        var endPoint = (Vector2)touchPositionWorld;

        var hit1 = Physics2D.Raycast(startPoint, endPoint - startPoint);
        RaycastHit2D hit2;
        if (hit1.transform == null)
            return;
        if (hit1.transform.gameObject.tag == "wall")
        {
            endPoint = new Vector2(shooterPos.x, hit1.point.y + hit1.point.y - startPoint.y);
            startPoint = hit1.point;
            positions.Add(hit1.point);

            //We change layer of the wall to 2(ignoreRaycast) we just hit so that when we raycast from that point, we dont hit it again.
            hit1.transform.gameObject.layer = 2;
            hit2 = Physics2D.Raycast(startPoint, endPoint - startPoint);
            hit1.transform.gameObject.layer = 0;
            if (hit2.transform.gameObject.tag == "wall")
            {
                //We do not want to draw an aim line in this case
                //EmptyCircle.gameObject.SetActive(false);
                return;
            }
        }
        else
        {
            hit2 = hit1;
        }

        Vector2? emptyCircleGridPos = null;
        if (hit2.transform.gameObject.tag == "bubble")
        {
            if (hit2.transform == null)
            {
                //We do not want to draw an aim line in this case
                //EmptyCircle.gameObject.SetActive(false);
                return;
            }
            positions.Add(hit2.point);
            emptyCircleGridPos = GetGridPosFromBubbleHit(hit2);
        }

        if (hit2.transform.gameObject.tag == "topwall")
        {
            positions.Add(hit2.point);
            emptyCircleGridPos = GetGridPosFromTopWallHit(hit2);
        }

        if (emptyCircleGridPos != null)
        {
            PlaceEmptyCircle((Vector2)emptyCircleGridPos);
            AimLineRenderer.positionCount = positions.Count;
            AimLineRenderer.SetPositions(positions.ToArray());
        }
        else
        {
            EmptyCircle.gameObject.SetActive(false);
        }
    }

    Vector2? GetTouchPositionWorld()
    {
        //We will get either mouse position or touch position

        touchPositionWorld = null;
        Vector2? touchPosition = null;
        if (Input.GetMouseButtonDown(0))
            isMouseDown = true;
        if (Input.GetMouseButtonUp(0))
            isMouseDown = false;
        if (isMouseDown)
            touchPosition = Input.mousePosition;
        if (Input.touchCount > 0)
            touchPosition = Input.GetTouch(0).position;

        if (touchPosition != null)
        {
            touchPositionWorld = Camera.main.ScreenToWorldPoint((Vector3)touchPosition);
            //If touch goes under shoot spot, we need to mirror its location on y axis.
            if (touchPositionWorld != null && touchPositionWorld.Value[1] < shooterPos.y)
                touchPositionWorld = new Vector2(-touchPositionWorld.Value[0], 2 * shooterPos.y - touchPositionWorld.Value[1]);
        }
        return touchPositionWorld;
    }

    void ShootTheBubble(List<Vector2> shootDestinations)
    {
        if (_emptyCircleGridPos[0] > -1 && _emptyCircleGridPos[1] > -1)
        {
            //TODO: shoot the ball to target area with animation
            if ((int)_emptyCircleGridPos[0] == grid.Count)
                grid.Add(new Bubble[6]);
            grid[(int)_emptyCircleGridPos[0]][(int)_emptyCircleGridPos[1]] = shooterCurrentBubble;
            shooterCurrentBubble.IgnoreRaycast = false;
            shooterCurrentBubble.Shoot(shootDestinations);
            shooterCurrentBubble.Arrived += () =>
            {
                CreateNewCurrentBubble();

            }; //TODO: Remove this event handler after call.
            //AddNewBubble((int)_emptyCircleGridPos[0], (int)_emptyCircleGridPos[1], GetRandomNumber());
        }
    }


    Vector2 GetGridPosFromTopWallHit(RaycastHit2D topWallHit2D)
    {
        return new Vector2(0, (float)Math.Floor((topWallHit2D.point.x - Holder.transform.position.x - GetLineIndent(0) + bubbleSize / 2) / bubbleSize));
    }

    Vector2 GetGridPosFromBubbleHit(RaycastHit2D bubbleHit2D)
    {
        //Put empty circle
        var hitBubble = bubbleHit2D.transform.parent.GetComponent<Bubble>();
        Vector2 hitBubbleGridPos = new Vector2(-1, -1);
        for (int l = 0; l < grid.Count; l++)
        {
            for (int i = 0; i < grid[l].Length; i++)
            {
                if (grid[l][i] != null && grid[l][i].Id == hitBubble.Id)
                {
                    hitBubbleGridPos = new Vector2(l, i);
                    break;
                }
            }
            if (hitBubbleGridPos[0] > -1)
                break;
        }
        Vector2 emptyCircleGridPos = new Vector2();
        var hitDir = bubbleHit2D.point - (Vector2)bubbleHit2D.transform.position;
        var hitAngle = Math.Atan2(hitDir.y, hitDir.x);
        bool isHitLineRight = isFirstRight ? (int)hitBubbleGridPos.x % 2 == 0 : (int)hitBubbleGridPos.x % 2 == 1;
        if (hitAngle <= -Math.PI / 2) //left bottom
        {
            emptyCircleGridPos = hitBubbleGridPos + new Vector2(1, -1 + (isHitLineRight ? 1 : 0));
            if (grid.Count > (int)emptyCircleGridPos[0] && grid[(int)emptyCircleGridPos[0]][(int)emptyCircleGridPos[1]] != null)
            {
                emptyCircleGridPos[1] = emptyCircleGridPos[1] + 1;
            }
        }
        else if (hitAngle > -Math.PI / 2 && hitAngle < 0) //right bottom
        {
            emptyCircleGridPos = hitBubbleGridPos + new Vector2(1, 1 + (isHitLineRight ? 0 : -1));
            if (grid.Count > (int)emptyCircleGridPos[0] && grid[(int)emptyCircleGridPos[0]][(int)emptyCircleGridPos[1]] != null)
            {
                emptyCircleGridPos[1] = emptyCircleGridPos[1] - 1;
            }
        }
        else if (hitAngle > Math.PI / 2) //left
        {
            emptyCircleGridPos = hitBubbleGridPos + new Vector2(0, -1);
        }
        else if (hitAngle < Math.PI / 2) //right
        {
            emptyCircleGridPos = hitBubbleGridPos + new Vector2(0, 1);
        }
        return emptyCircleGridPos;
    }

    void PlaceEmptyCircle(Vector2 newGridPos)
    {
        EmptyCircle.transform.position = new Vector3(Holder.transform.position.x + newGridPos[1] * bubbleSize + GetLineIndent((int)newGridPos[0]), transform.position.y - (newGridPos[0] + 1) * bubbleVerticalDistance, 0);

        EmptyCircle.gameObject.SetActive(true);
        if ((int)_emptyCircleGridPos[0] != (int)newGridPos[0] ||
            (int)_emptyCircleGridPos[1] != (int)newGridPos[1])
        {
            Debug.Log(_emptyCircleGridPos);
            Debug.Log(newGridPos);
            Debug.Log("");
            EmptyCircle.GetComponent<SpriteRenderer>().color = Globals.NumberColorDic[shooterCurrentBubble.Number];
            EmptyCircle.GetComponent<Animator>().Play("EmptyCircleGrow", 0, 0);
        }
        _emptyCircleGridPos = newGridPos;
    }



    void AddNewBubble(int lineIndex, int columnIndex, int number)
    {
        if (lineIndex == grid.Count)
            grid.Add(new Bubble[6]);
        grid[lineIndex][columnIndex] = Instantiate(BubblePrefab, Holder.transform, true).GetComponent<Bubble>();
        grid[lineIndex][columnIndex].transform.position = new Vector3(Holder.transform.position.x + columnIndex * bubbleSize + GetLineIndent(lineIndex), transform.position.y - (lineIndex + 1) * bubbleVerticalDistance, 0);
        grid[lineIndex][columnIndex].Number = number;
        //TODO: Check effect of this bubble
    }

    void AddNewLine()
    {
        var newLine = new Bubble[BubblePerLine];
        isFirstRight = !isFirstRight;
        for (int i = 0; i < BubblePerLine; i += 2)
        {
            newLine[i] = Instantiate(BubblePrefab, Holder.transform, true).GetComponent<Bubble>();
            newLine[i].transform.position = new Vector3(Holder.transform.position.x + i * bubbleSize + GetLineIndent(0), transform.position.y, 0);
            newLine[i].Number = GetRandomNumber();
        }

        grid.Insert(0, newLine);
        Holder.transform.localPosition = new Vector3(Holder.transform.localPosition.x, Holder.transform.localPosition.y - bubbleVerticalDistance, Holder.transform.localPosition.z);
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


    int GetRandomNumber()
    {
        return (int)Math.Pow(2, random.Next(1, 9));
    }
    bool IsLineRight(int lineIndex)
    {
        return isFirstRight ? lineIndex % 2 == 0 : lineIndex % 2 == 1;
    }
    float GetLineIndent(int lineIndex)
    {
        return bubbleSize / 2 * (IsLineRight(lineIndex) ? 1 : 0);
    }
}