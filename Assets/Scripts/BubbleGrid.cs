using System;
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

    private Bubble shooterCurrentBubble;
    private Bubble shooterNextBubble;

    readonly Random random = new Random();

    // Start is called before the first frame update
    void Start()
    {
        shooterPos = new Vector2(0, GameAreaRectTransform.position.y - GameAreaRectTransform.rect.size.y * GameAreaRectTransform.parent.localScale.y / 2f);
        var bubble = Instantiate(BubblePrefab, shooterPos, Quaternion.identity);
        bubble.GetComponent<Bubble>().Number = 32;
        bubble.GetComponent<Bubble>().IgnoreRaycast = true;
        bubbleSize = BubblePrefab.GetComponent<CircleCollider2D>().radius * 2f;
        bubbleVerticalDistance = bubbleSize / 2 * 1.73205080757f; //r * sqrt(3)
        Holder.transform.localPosition = new Vector3(-bubbleSize * (BubblePerLine - 1) / 2f, Holder.transform.localPosition.y, 0);
        InitGrid();

    }

    private bool isMouseDown = false;
    Vector2 emptyCircleGridPos = new Vector2(-1, -1);
    // Update is called once per frame
    void Update()
    {
        //We will get either mouse position or touch position
        Vector2? touchPosition = null;
        if (Input.GetMouseButtonDown(0))
            isMouseDown = true;
        if (Input.GetMouseButtonUp(0))
        {
            isMouseDown = false;
            if (emptyCircleGridPos[0] > -1 && emptyCircleGridPos[1] > -1)
            {
                //TODO: shoot the ball to target area
                AddNewBubble((int)emptyCircleGridPos[0], (int)emptyCircleGridPos[1], GetRandomNumber());
            }
        }
        if (isMouseDown)
            touchPosition = Input.mousePosition;
        if (Input.touchCount > 0)
            touchPosition = Input.GetTouch(0).position;

        if (touchPosition != null)
        {
            Vector2 touchPositionWorld = Camera.main.ScreenToWorldPoint((Vector3)touchPosition);
            List<Vector3> positions = new List<Vector3>();
            positions.Add(shooterPos);
            var startPoint = shooterPos;
            var endPoint = touchPositionWorld;
            RaycastHit2D circleHit2D = new RaycastHit2D();
            GameObject wallToChangeLayer = null;
            while (true)
            {
                var hit2d = Physics2D.Raycast(startPoint, endPoint - startPoint);
                if (wallToChangeLayer != null)
                    wallToChangeLayer.layer = 0;
                if (hit2d.transform == null)
                    break;
                positions.Add(hit2d.point);
                if (hit2d.transform.gameObject.tag == "bubble")
                {
                    circleHit2D = hit2d;
                    break;
                }

                if (hit2d.transform.gameObject.tag == "topwall")
                {
                    circleHit2D = hit2d;
                    break;
                }
                if (positions.Count < 3 && hit2d.transform.gameObject.tag == "wall")
                {
                    hit2d.transform.gameObject.layer = 2;
                    wallToChangeLayer = hit2d.transform.gameObject;
                    endPoint = new Vector2(shooterPos.x, hit2d.point.y + hit2d.point.y - startPoint.y);
                    startPoint = hit2d.point;
                    continue;
                }
                break;
            }

            if (circleHit2D.transform != null)
            {
                AimLineRenderer.positionCount = positions.Count;
                AimLineRenderer.SetPositions(positions.ToArray());

                //Put empty circle
                var hitBubble = circleHit2D.transform.parent.GetComponent<Bubble>();
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
                var hitDir = circleHit2D.point - (Vector2)circleHit2D.transform.position;
                var hitAngle = Math.Atan2(hitDir.y, hitDir.x);
                bool isHitLineRight = isFirstRight ? (int)hitBubbleGridPos.x % 2 == 0 : (int)hitBubbleGridPos.x % 2 == 1;
                var prevEmptyCircleGridPos = new Vector2(emptyCircleGridPos[0], emptyCircleGridPos[1]);
                if (hitAngle <= -Math.PI / 2) //left bottom
                {
                    EmptyCircle.transform.position = new Vector2(circleHit2D.transform.position.x - bubbleSize / 2, circleHit2D.transform.position.y - bubbleVerticalDistance);
                    emptyCircleGridPos = hitBubbleGridPos + new Vector2(1, -1 + (isHitLineRight ? 1 : 0));
                }
                else if (hitAngle > -Math.PI / 2 && hitAngle < 0) //right bottom
                {
                    EmptyCircle.transform.position = new Vector2(circleHit2D.transform.position.x + bubbleSize / 2, circleHit2D.transform.position.y - bubbleVerticalDistance);
                    emptyCircleGridPos = hitBubbleGridPos + new Vector2(1, 1 + (isHitLineRight ? 0 : -1));
                }
                else if (hitAngle > Math.PI / 2) //left
                {
                    EmptyCircle.transform.position = new Vector2(circleHit2D.transform.position.x - bubbleSize, circleHit2D.transform.position.y);
                    emptyCircleGridPos = hitBubbleGridPos + new Vector2(0, -1);
                }
                else if (hitAngle < Math.PI / 2) //right
                {
                    EmptyCircle.transform.position = new Vector2(circleHit2D.transform.position.x + bubbleSize, circleHit2D.transform.position.y);
                    emptyCircleGridPos = hitBubbleGridPos + new Vector2(0, 1);
                }

                if ((int) prevEmptyCircleGridPos[0] != (int) emptyCircleGridPos[0] ||
                    (int) prevEmptyCircleGridPos[1] != (int) emptyCircleGridPos[1])
                {
                    //EmptyCircle.GetComponent<SpriteRenderer>().color = Globals.NumberColorDic[shooterCurrentBubble.Number];
                    EmptyCircle.GetComponent<Animator>().Play("EmptyCircleGrow");
                }
            }
            else
            {
                AimLineRenderer.positionCount = 0;
            }
        }
    }

    int GetRandomNumber()
    {
        return (int)Math.Pow(2, random.Next(1, 9));
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


    void InitGrid()
    {
        AddNewLine();
        AddNewLine();
        AddNewLine();
        AddNewLine();
    }

    bool IsLineRight(int lineIndex)
    {
        return isFirstRight ? lineIndex % 2 == 0 : lineIndex % 2 == 1;
    }

    float GetLineIndent(int lineIndex)
    {
        return bubbleSize / 4 * (IsLineRight(lineIndex) ? 1 : -1);
    }

    void AddNewBubble(int lineIndex, int columnIndex, int number)
    {
        if(lineIndex == grid.Count)
            grid.Add(new Bubble[6]);
        grid[lineIndex][columnIndex] = Instantiate(BubblePrefab, Holder.transform, true).GetComponent<Bubble>();
        grid[lineIndex][columnIndex].transform.position = new Vector3(Holder.transform.position.x + columnIndex * bubbleSize + GetLineIndent(lineIndex), transform.position.y - (lineIndex + 1) * bubbleVerticalDistance, 0);
        grid[lineIndex][columnIndex].Number = number;
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