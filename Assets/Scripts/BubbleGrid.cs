using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BubbleGrid : MonoBehaviour
{
    public GameObject BubblePrefab;
    public GameObject Holder;
    public GameObject EmptyCircle;
    public RectTransform GameAreaRectTransform;
    public LineRenderer AimLineRenderer;
    public Text TxtScore;
    public Text TxtMultiplier;
    public Text TxtMultiplierBig;
    public Animator MultiplierAnimator;


    private const int BubblePerLine = 6; //If you change this number, you should implement scaling bubbles.

    //We will add new line at zero index. So, list order will match the UI.
    private readonly List<Bubble[]> grid = new List<Bubble[]>();
    private bool isFirstRight = false;
    private float bubbleSize;
    private float bubbleVerticalDistance;
    private Vector3 shooterPos;
    Vector2Int _emptyCircleGridPos = new Vector2Int(-1, -1);
    private bool isMouseDown = false;
    Vector2? touchPositionWorld = null;
    Vector3 holderTargetPos;

    private Bubble shooterCurrentBubble;
    List<Bubble> waitingQueue = new List<Bubble>();

    private float _score = 0;
    private float _multiplier = 1;

    private void IncreaseMultiplier()
    {
        _multiplier++;
        TxtMultiplier.text = "x" + _multiplier;
        TxtMultiplierBig.text = TxtMultiplier.text;
        MultiplierAnimator.Play("MultiplierTextGrow", 0, 0);
    }
    private void ResetMultiplier()
    {
        _multiplier = 1;
        TxtMultiplier.text = "x" + _multiplier;
        TxtMultiplierBig.text = TxtMultiplier.text;
    }
    void AddScore(float value)
    {
        _score += value * _multiplier;
        string scoreText = "";
        if (_score >= 1000)
            scoreText = _score / 1000 + "K";
        else
            scoreText = _score.ToString();
        TxtScore.text = scoreText;

        //TODO: show new level value
        var level = GetLevel(_score);
        //Debug.Log("level -> " + level);
    }
    static float GetLevel(float score)
    {
        return (float)Math.Sqrt(score) / 20;
    }

    enum EnumAnimationStates
    {
        NoAnimation = 0,
        PuttingBubbleToShooter = 1,
        AddingNewLine = 2,
        Shooting = 4,
        WaitingToMerge = 8,
        Merging = 16
    }

    //Using binary state to keep more than one state at a time.
    //Do not send the state directly
    private int currentAnimationStateBinary;

    bool IsAnimating()
    {
        return !IsInAnimationStateOnly(EnumAnimationStates.NoAnimation);
    }
    bool IsInAnimationStateOnly(EnumAnimationStates state)
    {
        return currentAnimationStateBinary == (int)state;
    }
    bool IsInAnimationState(EnumAnimationStates state)
    {
        return (currentAnimationStateBinary & (int)state) == (int)state;
    }
    void AddAnimationState(EnumAnimationStates state)
    {
        currentAnimationStateBinary |= (int)state;
    }
    void RemoveAnimationState(EnumAnimationStates state)
    {
        currentAnimationStateBinary ^= (int)state;
    }

    // Start is called before the first frame update
    void Start()
    {
        shooterPos = new Vector2(0, GameAreaRectTransform.position.y - GameAreaRectTransform.rect.size.y * GameAreaRectTransform.parent.localScale.y / 2f);
        bubbleSize = BubblePrefab.GetComponent<CircleCollider2D>().radius * 2f;
        bubbleVerticalDistance = bubbleSize / 2 * 1.73205080757f; //r * sqrt(3)
        holderTargetPos = Holder.transform.localPosition;
        InitGrid();
        AddNewBubbleToWaitingQueue(GetRandomNumber());
        AddNewBubbleToWaitingQueue(GetRandomNumber());
        PutNextBubbleToShooter();
    }

    Vector3 GlobalPositionToScaledHolderPosition(Vector3 position)
    {
        return new Vector3(
            position.x / Holder.transform.lossyScale.x,
            position.y / Holder.transform.lossyScale.y,
            position.z / Holder.transform.lossyScale.z
            );
    }

    void PutNextBubbleToShooter()
    {
        if (waitingQueue.Count == 0)
        {
            AddNewBubbleToWaitingQueue(GetRandomNumber());
            Debug.LogError("Waiting Queue was empty! Be careful next time.");
        }
        shooterCurrentBubble = waitingQueue[0];
        waitingQueue.RemoveAt(0);
        shooterCurrentBubble.transform.position = new Vector3(shooterCurrentBubble.transform.position.x, shooterCurrentBubble.transform.position.y, BubblePrefab.transform.localPosition.z);
        shooterCurrentBubble.ScaleTo(new List<Vector2>() { shooterCurrentBubble.transform.localScale / 0.7f }, 0.6f);
        shooterCurrentBubble.MoveTo(new List<Vector3>() { GlobalPositionToScaledHolderPosition(shooterPos - Holder.transform.position) }, 1);
        AddAnimationState(EnumAnimationStates.PuttingBubbleToShooter);
        shooterCurrentBubble.Arrived += OnBubbleArrivedToShooter;
    }

    private void OnBubbleArrivedToShooter(Bubble bubble)
    {
        shooterCurrentBubble.Arrived -= OnBubbleArrivedToShooter;
        RemoveAnimationState(EnumAnimationStates.PuttingBubbleToShooter);
    }

    void AddNewBubbleToWaitingQueue(int number = -1)
    {
        var newBubble = Instantiate(BubblePrefab, Holder.transform, true).GetComponent<Bubble>();
        newBubble.transform.position = new Vector3(shooterPos.x - bubbleSize, shooterPos.y, waitingQueue.Count + 1);
        newBubble.transform.localScale = newBubble.transform.localScale * 0.7f;
        newBubble.Number = GetNumberForNextBubbleInTheQueue(GetNewVirtualGrid(grid), isFirstRight);
        newBubble.IgnoreRaycast = true;
        waitingQueue.Add(newBubble);
    }



    // Update is called once per frame
    void Update()
    {
        UpdateTouchPositionWorld();
        if (!IsAnimating())
        {
            if (Input.GetMouseButtonUp(0))
            {
                EmptyCircle.gameObject.SetActive(false);
                var shootDestinations = new List<Vector3>();
                if (AimLineRenderer.positionCount > 2)
                    shootDestinations.Add(GlobalPositionToScaledHolderPosition(AimLineRenderer.GetPosition(1) - Holder.transform.position));
                shootDestinations.Add(GlobalPositionToScaledHolderPosition(new Vector3(_emptyCircleGridPos[1] * bubbleSize + GetLineIndent(_emptyCircleGridPos[0]), transform.position.y - (_emptyCircleGridPos[0] + 1) * bubbleVerticalDistance - Holder.transform.position.y, BubblePrefab.transform.position.z)));
                ShootTheBubble(shootDestinations);
            }
            else
                RaycastAimLine();
        }
        else
        {
            if (IsInAnimationState(EnumAnimationStates.AddingNewLine))
            {
                Holder.transform.localPosition = Vector3.MoveTowards(Holder.transform.localPosition, holderTargetPos, 5 * Time.deltaTime);
                //We move the shooter bubble and queue bubbles so they stay where they were comparing to screen.
                shooterCurrentBubble.transform.localPosition =
                    GlobalPositionToScaledHolderPosition(shooterPos - Holder.transform.position);
                foreach (Bubble bubble in waitingQueue.ToArray())
                {
                    bubble.transform.localPosition =
                        GlobalPositionToScaledHolderPosition(shooterPos - Holder.transform.position + new Vector3(-bubbleSize, 0));
                }
                if (Vector3.Distance(Holder.transform.localPosition, holderTargetPos) < 0.001f)
                {
                    RemoveAnimationState(EnumAnimationStates.AddingNewLine);
                }
            }

            if (IsInAnimationState(EnumAnimationStates.WaitingToMerge))
            {
                startMergeIn -= Time.deltaTime;
                if (startMergeIn <= 0)
                {
                    RemoveAnimationState(EnumAnimationStates.WaitingToMerge);
                    AddAnimationState(EnumAnimationStates.Merging);
                    ResetMultiplier();
                    ApplyMergePath();
                }
            }
        }
    }

    void InitGrid()
    {
        AddNewLine(false);
        AddNewLine(false);
        AddNewLine(false);
        AddNewLine(false);
    }


    void RaycastAimLine()
    {
        AimLineRenderer.positionCount = 0;
        if (touchPositionWorld == null)
        {
            EmptyCircle.gameObject.SetActive(false);
            return;
        }

        //It raycasts until either it hits a bubble or topwall and sends the hit2D or wall for the second time and sends null.
        //And will draw the aim line.
        List<Vector3> positions = new List<Vector3> { shooterPos };
        var startPoint = shooterPos;
        var endPoint = (Vector3)touchPositionWorld;

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

        Vector2Int? emptyCircleGridPos = null;
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
            PlaceEmptyCircle((Vector2Int)emptyCircleGridPos);
            AimLineRenderer.positionCount = positions.Count;
            AimLineRenderer.SetPositions(positions.ToArray());
        }
        else
        {
            EmptyCircle.gameObject.SetActive(false);
        }
    }

    void UpdateTouchPositionWorld()
    {
        //We will get either mouse position or touch position
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
            return;
        }
        touchPositionWorld = null;
    }

    void ShootTheBubble(List<Vector3> shootDestinations)
    {
        if (_emptyCircleGridPos[0] > -1 && _emptyCircleGridPos[1] > -1)
        {
            if (_emptyCircleGridPos[0] == grid.Count)
                grid.Add(new Bubble[6]);
            grid[_emptyCircleGridPos[0]][_emptyCircleGridPos[1]] = shooterCurrentBubble;
            shooterCurrentBubble.IgnoreRaycast = false;
            shooterCurrentBubble.MoveTo(shootDestinations, 5);
            shooterCurrentBubble.Arrived += OnShootedBubbleArrived;
            AddAnimationState(EnumAnimationStates.Shooting);
        }
    }

    private void OnShootedBubbleArrived(Bubble bubble)
    {
        shooterCurrentBubble.Arrived -= OnShootedBubbleArrived;
        AddNewBubbleToWaitingQueue(GetRandomNumber());
        PutNextBubbleToShooter();
        DisturbOthers(_emptyCircleGridPos);
        RemoveAnimationState(EnumAnimationStates.Shooting);
        CalculateAndAnimatePops(_emptyCircleGridPos);
        //AddNewLine();
    }

    void DisturbOthers(Vector2Int gridPos)
    {
        //As this is not a blocking animation, we don't set animation state.
        bool isRight = IsLineRight(isFirstRight, gridPos[0]);
        Vector2Int[] surroundingGridPosDiffs = new[]
        {
            new Vector2Int(0, -1), //left
            new Vector2Int(-1, isRight ? 0 : -1), //left up
            new Vector2Int(-1, isRight ? 1 : 0), //right up
            new Vector2Int(0, 1), //right
            new Vector2Int(1, isRight ? 1 : 0), //right down
            new Vector2Int(1, isRight ? 0 : -1) //left down
        };
        float distance = 0.015f;
        for (int i = 0; i < 6; i++)
        {
            var bubblePos = gridPos + surroundingGridPosDiffs[i];
            if (bubblePos[1] > -1 && bubblePos[1] < 6 && bubblePos[0] > -1 && grid.Count > bubblePos[0] && grid[bubblePos[0]][bubblePos[1]] != null)
            {
                var bubble = grid[bubblePos[0]][bubblePos[1]];
                var angle = 2 * Math.PI / 6 * i + Math.PI; //As we start from left, we add some more radian to rotate it.
                Vector3 direction = new Vector3((float)Math.Cos(angle), -(float)Math.Sin(angle));
                bubble.MoveTo(new List<Vector3>()
                {
                    bubble.transform.localPosition + direction * distance,
                    bubble.transform.localPosition,
                }, 0.2f);
            }
        }
    }


    Vector2Int GetGridPosFromTopWallHit(RaycastHit2D topWallHit2D)
    {
        return new Vector2Int(0, (int)Math.Floor((topWallHit2D.point.x - Holder.transform.position.x - GetLineIndent(0) + bubbleSize / 2) / bubbleSize));
    }

    Vector2Int GetGridPosFromBubbleHit(RaycastHit2D bubbleHit2D)
    {
        //Put empty circle
        var hitBubble = bubbleHit2D.transform.GetComponent<Bubble>();
        Vector2Int hitBubbleGridPos = new Vector2Int(-1, -1);
        for (int l = 0; l < grid.Count; l++)
        {
            for (int i = 0; i < grid[l].Length; i++)
            {
                if (grid[l][i] != null && grid[l][i].Id == hitBubble.Id)
                {
                    hitBubbleGridPos = new Vector2Int(l, i);
                    break;
                }
            }
            if (hitBubbleGridPos[0] > -1)
                break;
        }
        Vector2Int emptyCircleGridPos = new Vector2Int();
        var hitDir = bubbleHit2D.point - (Vector2)bubbleHit2D.transform.position;
        var hitAngle = Math.Atan2(hitDir.y, hitDir.x);
        bool isHitLineRight = IsLineRight(isFirstRight, hitBubbleGridPos[0]);
        if (hitAngle <= -Math.PI / 2) //left bottom
        {
            emptyCircleGridPos = hitBubbleGridPos + new Vector2Int(1, -1 + (isHitLineRight ? 1 : 0));
            if (grid.Count > emptyCircleGridPos[0] && grid[emptyCircleGridPos[0]][emptyCircleGridPos[1]] != null)
            {
                if (hitAngle <= -Math.PI / 2 - Math.PI / 4) //left
                {
                    emptyCircleGridPos[0] = hitBubbleGridPos[0];
                    emptyCircleGridPos[1] = hitBubbleGridPos[1] - 1;
                }
                else //right bottom
                {
                    emptyCircleGridPos[1]++;
                }
            }
        }
        else if (hitAngle > -Math.PI / 2 && hitAngle < 0) //right bottom
        {
            emptyCircleGridPos = hitBubbleGridPos + new Vector2Int(1, 1 + (isHitLineRight ? 0 : -1));
            if (grid.Count > emptyCircleGridPos[0] && grid[emptyCircleGridPos[0]][emptyCircleGridPos[1]] != null)
            {

                if (hitAngle >= -Math.PI / 4) //right
                {
                    emptyCircleGridPos[0] = hitBubbleGridPos[0];
                    emptyCircleGridPos[1] = hitBubbleGridPos[1] + 1;
                }
                else //left bottom
                {
                    emptyCircleGridPos[1]--;
                }
            }
        }
        else if (hitAngle > Math.PI / 2) //left
        {
            emptyCircleGridPos = hitBubbleGridPos + new Vector2Int(0, -1);
        }
        else if (hitAngle < Math.PI / 2) //right
        {
            emptyCircleGridPos = hitBubbleGridPos + new Vector2Int(0, 1);
        }
        return emptyCircleGridPos;
    }

    void PlaceEmptyCircle(Vector2Int newGridPos)
    {
        if (grid.Count > newGridPos[0])
        {
            if (grid[newGridPos[0]][newGridPos[1]] != null)
            {
                Debug.LogError("Same place!");
                return;
            }
        }

        EmptyCircle.transform.position = new Vector3(Holder.transform.position.x + newGridPos[1] * bubbleSize + GetLineIndent(newGridPos[0]), transform.position.y - (newGridPos[0] + 1) * bubbleVerticalDistance, 0);

        EmptyCircle.gameObject.SetActive(true);
        if (_emptyCircleGridPos[0] != newGridPos[0] ||
            _emptyCircleGridPos[1] != newGridPos[1])
        {
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
    }

    void AddNewLine(bool animate = true)
    {
        var newLine = new Bubble[BubblePerLine];
        isFirstRight = !isFirstRight;
        for (int i = 0; i < BubblePerLine; i++)
        {
            newLine[i] = Instantiate(BubblePrefab, Holder.transform, true).GetComponent<Bubble>();
            newLine[i].transform.position = new Vector3(Holder.transform.position.x + i * bubbleSize + GetLineIndent(0), transform.position.y, 0);
            newLine[i].Number = GetRandomNumber();
        }

        grid.Insert(0, newLine);
        if (!animate)
            Holder.transform.localPosition = new Vector3(Holder.transform.localPosition.x, Holder.transform.localPosition.y - bubbleVerticalDistance, Holder.transform.localPosition.z);
        else
        {
            holderTargetPos = new Vector3(Holder.transform.localPosition.x, Holder.transform.localPosition.y - bubbleVerticalDistance, Holder.transform.localPosition.z);
            AddAnimationState(EnumAnimationStates.AddingNewLine);
        }

    }




    enum EnumDirections
    {
        None,
        Left,
        LeftUp,
        RightUp,
        Right,
        RightDown,
        LeftDown
    }

    static bool IsBubbleGridPositionValid(List<Bubble[]> grid, Vector2Int bubbleGridPos)
    {
        return !(grid.Count <= bubbleGridPos[0] || bubbleGridPos[0] < 0 || bubbleGridPos[1] < 0 || bubbleGridPos[1] >= 6);
    }

    static bool IsBubbleGridPositionValid(List<int[]> virtualGrid, Vector2Int bubbleGridPos)
    {
        return !(virtualGrid.Count <= bubbleGridPos[0] || bubbleGridPos[0] < 0 || bubbleGridPos[1] < 0 || bubbleGridPos[1] >= 6);
    }

    static Bubble GetBubble(List<Bubble[]> grid, Vector2Int bubbleGridPos)
    {
        if (IsBubbleGridPositionValid(grid, bubbleGridPos))
            return grid[bubbleGridPos[0]][bubbleGridPos[1]];
        return null;
    }


    static void GetConnectedBubblesWithSameNumberRecursive(List<int[]> virtualGrid, bool isFirstRight, Vector2Int bubbleGridPos, int number, EnumDirections ignoredDirection, BubbleConnection parentConnection)
    {
        //find if to right or left
        bool isRight = IsLineRight(isFirstRight, bubbleGridPos[0]);

        //calculate touching bubble indices starting from left up
        //iterate over them
        //if you find same number, check that number's area too but only the ones this one is not touching
        if (ignoredDirection != EnumDirections.LeftUp && ignoredDirection != EnumDirections.Left &&
            ignoredDirection != EnumDirections.LeftDown)
        {
            var leftBubblePos = new Vector2Int(bubbleGridPos[0], bubbleGridPos[1] - 1);
            var bubbleNumber = GetBubbleNumber(virtualGrid, leftBubblePos);
            if (bubbleNumber == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(leftBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = leftBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumberRecursive(virtualGrid, isFirstRight, leftBubblePos, number, EnumDirections.Right, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.Left && ignoredDirection != EnumDirections.LeftUp &&
            ignoredDirection != EnumDirections.RightUp)
        {
            var leftUpBubblePos = new Vector2Int(bubbleGridPos[0] - 1, bubbleGridPos[1] + (isRight ? 0 : -1));
            var bubbleNumber = GetBubbleNumber(virtualGrid, leftUpBubblePos);
            if (bubbleNumber == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(leftUpBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = leftUpBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumberRecursive(virtualGrid, isFirstRight, leftUpBubblePos, number, EnumDirections.RightDown, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.LeftUp && ignoredDirection != EnumDirections.RightUp &&
            ignoredDirection != EnumDirections.Right)
        {
            var rightUpBubblePos = new Vector2Int(bubbleGridPos[0] - 1, bubbleGridPos[1] + (isRight ? 1 : 0));
            var bubbleNumber = GetBubbleNumber(virtualGrid, rightUpBubblePos);
            if (bubbleNumber == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(rightUpBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = rightUpBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumberRecursive(virtualGrid, isFirstRight, rightUpBubblePos, number, EnumDirections.LeftDown, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.RightUp && ignoredDirection != EnumDirections.Right &&
            ignoredDirection != EnumDirections.RightDown)
        {
            var rightBubblePos = new Vector2Int(bubbleGridPos[0], bubbleGridPos[1] + 1);
            var bubbleNumber = GetBubbleNumber(virtualGrid, rightBubblePos);
            if (bubbleNumber == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(rightBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = rightBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumberRecursive(virtualGrid, isFirstRight, rightBubblePos, number, EnumDirections.Left, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.Right && ignoredDirection != EnumDirections.RightDown &&
            ignoredDirection != EnumDirections.LeftDown)
        {
            var rightDownBubblePos = new Vector2Int(bubbleGridPos[0] + 1, bubbleGridPos[1] + (isRight ? 1 : 0));
            var bubbleNumber = GetBubbleNumber(virtualGrid, rightDownBubblePos);
            if (bubbleNumber == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(rightDownBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = rightDownBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumberRecursive(virtualGrid, isFirstRight, rightDownBubblePos, number, EnumDirections.LeftUp, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.Left && ignoredDirection != EnumDirections.LeftDown &&
            ignoredDirection != EnumDirections.RightDown)
        {
            var leftDownBubblePos = new Vector2Int(bubbleGridPos[0] + 1, bubbleGridPos[1] + (isRight ? 0 : -1));
            var bubbleNumber = GetBubbleNumber(virtualGrid, leftDownBubblePos);
            if (bubbleNumber == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(leftDownBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = leftDownBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumberRecursive(virtualGrid, isFirstRight, leftDownBubblePos, number, EnumDirections.RightUp, parentConnection);
            }
        }

    }


    static void CalculateMergeDepthMap(List<int[]> virtualGrid, bool isFirstRight, Vector2Int bubbleGridPos, out BubbleConnection rootConnection, out List<List<int>> depthList)
    {
        //var virtualGrid = virtualGrid.ToList();

        rootConnection = new BubbleConnection
        {
            Number = GetBubbleNumber(virtualGrid, bubbleGridPos),
            GridPosition = bubbleGridPos,
            Connections = new List<BubbleConnection>()
        };

        //We add same object without reference to prevent infinite loop.
        rootConnection.Connections.Add(new BubbleConnection()
        {
            Number = rootConnection.Number,
            GridPosition = bubbleGridPos,
            Connections = new List<BubbleConnection>(),
            ParentConnection = rootConnection
        });

        //We add same object without reference to prevent infinite loop.

        GetConnectedBubblesWithSameNumberRecursive(virtualGrid, isFirstRight, bubbleGridPos, rootConnection.Number, EnumDirections.None, rootConnection);

        //As there is a exception on NextNumber for the first level, we call this once manually.
        foreach (var innerConnection in rootConnection.Connections)
        {
            GetConnectedBubblesWithSameNumberRecursive(virtualGrid, isFirstRight, innerConnection.GridPosition, GetMergeResultNumber(rootConnection.Number, rootConnection.Connections.Count), EnumDirections.None, innerConnection);
            if (innerConnection.Connections.Count > 0)
                FindDeepConnectionsRecursive(virtualGrid, isFirstRight, innerConnection);
        }

        //Create depth list
        //TODO: Eliminate ones that merge outside
        depthList = GetDepthListRecursive(rootConnection);
    }

    static void GetDesiredMergePath(List<int[]> virtualGrid, bool isFirstRight, Vector2Int bubbleGridPos, out BubbleConnection rootConnection, out List<int> desiredMergePath)
    {
        CalculateMergeDepthMap(virtualGrid, isFirstRight, bubbleGridPos, out rootConnection, out var depthList);

        desiredMergePath = null;
        //if (depthList.Count <= 1)
        //    return;

        //Find desired depth path
        depthList = depthList.OrderBy(d => d.Count).ToList();
        var minDepth = depthList.Min(d => d.Count);
        var maxDepth = depthList.Max(d => d.Count);
        var wantedDepth = Mathf.Lerp(minDepth, maxDepth, Globals.MergeDepthScale);
        float minDiff = Single.MaxValue;
        foreach (var depth in depthList)
        {
            var diff = Math.Abs(wantedDepth - depth.Count);
            if (diff < minDiff)
            {
                minDiff = diff;
                desiredMergePath = depth;
            }
        }
        if (desiredMergePath == null)
            Debug.LogError("Could select any path. Path count => " + depthList.Count);
    }


    void CalculateAndAnimatePops(Vector2Int bubbleGridPos)
    {
        GetDesiredMergePath(GetNewVirtualGrid(grid), isFirstRight, bubbleGridPos, out var rootConnection, out var desiredMergePath);

        if (desiredMergePath == null || rootConnection == null)
            return;

        //Animate merging and popping
        nextConnection = rootConnection;
        nextPath = desiredMergePath;

        startMergeIn = Globals.WaitSecondsBeforeMerge;
        AddAnimationState(EnumAnimationStates.WaitingToMerge);
    }



    private float startMergeIn = 0.5f; //seconds
    private BubbleConnection nextConnection = null;
    private List<int> nextPath = null;
    void ApplyMergePath()
    {
        if (!nextPath.Any())
        {
            RemoveAnimationState(EnumAnimationStates.Merging);
            if (grid.Count < 4 || grid.Sum(l => l.Count(b => b != null)) < 18 && grid.Count < 8)
                AddNewLine();
            return;
        }
        toBeArrivedCount = 0;
        var connection = this.nextConnection;
        if (!connection.Connections.Any())
        {
            Debug.LogError("There must be more connections! Something is wrong.");
            return;
        }
        if (connection.Connections.Count <= nextPath[0] || nextPath[0] < 0)
        {
            Debug.LogError("Connection count is " + connection.Connections.Count + " while index in the path is " + nextPath[0]);
            return;
        }
        nextConnection = connection.Connections[nextPath[0]];
        var targetPosition = new List<Vector3>()
        {
            grid[nextConnection.GridPosition[0]][nextConnection.GridPosition[1]].transform.localPosition
        };
        foreach (var childConnection in connection.Connections)
        {
            var bubble = grid[childConnection.GridPosition[0]][childConnection.GridPosition[1]];
            grid[childConnection.GridPosition[0]][childConnection.GridPosition[1]] = null;
            bubble.MoveTo(targetPosition, 1f);
            bubble.Arrived += OnBubbleArriveMergePoint;
            toBeArrivedCount++;
        }
        AddScore(connection.Number * 10);

        var newConnection = new BubbleConnection()
        {
            Number = GetMergeResultNumber(nextConnection.Number, connection.Connections.Count),
            GridPosition = nextConnection.GridPosition,
            Connections = new List<BubbleConnection>()
        };
        nextConnection.Connections.Add(newConnection);

        nextPath.RemoveAt(0);
        if (nextPath.Any())
            IncreaseMultiplier();
    }

    private int toBeArrivedCount;
    private void OnBubbleArriveMergePoint(Bubble bubble)
    {
        bubble.Pop();
        toBeArrivedCount--;
        if (toBeArrivedCount <= 0)
        {
            var newConnection = nextConnection.Connections[nextConnection.Connections.Count - 1];
            AddNewBubble(newConnection.GridPosition[0], newConnection.GridPosition[1], newConnection.Number);
            if (newConnection.Number == 2048)
            {
                //TODO: collect points from this
                PopAllBubblesAround(newConnection.GridPosition);
            }
            ApplyMergePath();
        }
    }

    static void PopAndRemoveBubble(List<Bubble[]> grid, Vector2Int bubblePos, float delay = 0)
    {
        var bubble = GetBubble(grid, bubblePos);
        if (bubble != null)
        {
            bubble.Pop(delay);
            grid[bubblePos[0]][bubblePos[1]] = null;
        }
    }

    private void PopAllBubblesAround(Vector2Int _2048GridPos)
    {
        var isLineRight = IsLineRight(isFirstRight, _2048GridPos[0]);
        var leftGridPos = GetGridPositionByDirection(_2048GridPos, EnumDirections.Left, isLineRight);
        PopAndRemoveBubble(grid, leftGridPos, 0.05f);

        var leftUpGridPos = GetGridPositionByDirection(_2048GridPos, EnumDirections.LeftUp, isLineRight);
        PopAndRemoveBubble(grid, leftUpGridPos, 0.1f);

        var rightUpGridPos = GetGridPositionByDirection(_2048GridPos, EnumDirections.RightUp, isLineRight);
        PopAndRemoveBubble(grid, rightUpGridPos, 0.15f);

        var rightGridPos = GetGridPositionByDirection(_2048GridPos, EnumDirections.Right, isLineRight);
        PopAndRemoveBubble(grid, rightGridPos, 0.2f);

        var rightDownGridPos = GetGridPositionByDirection(_2048GridPos, EnumDirections.RightDown, isLineRight);
        PopAndRemoveBubble(grid, rightDownGridPos, 0.25f);

        var leftDownGridPos = GetGridPositionByDirection(_2048GridPos, EnumDirections.LeftDown, isLineRight);
        PopAndRemoveBubble(grid, leftDownGridPos, 0.3f);

        PopAndRemoveBubble(grid, _2048GridPos, 0.35f);

    }

    static List<List<int>> GetDepthListRecursive(BubbleConnection connection)
    {
        var myList = new List<List<int>>();

        for (int i = 0; i < connection.Connections.Count; i++)
        {
            var childConnection = connection.Connections[i];
            var childList = GetDepthListRecursive(childConnection);
            if (childList.Any())
            {
                for (int j = 0; j < childList.Count; j++)
                {
                    var grandChildPath = childList[j];
                    var grandChildPathWithChild = grandChildPath.ToList();
                    grandChildPathWithChild.Insert(0, i);
                    myList.Add(grandChildPathWithChild);
                }
            }
            else
            {
                myList.Add(new List<int>() { i });
            }
        }

        return myList;
    }

    static void FindDeepConnectionsRecursive(List<int[]> virtualGrid, bool isFirstRight, BubbleConnection connection)
    {
        foreach (var innerConnection in connection.Connections)
        {
            GetConnectedBubblesWithSameNumberRecursive(virtualGrid, isFirstRight, innerConnection.GridPosition, innerConnection.NextNumber, EnumDirections.None, innerConnection);
            if (innerConnection.Connections.Count > 0)
                FindDeepConnectionsRecursive(virtualGrid, isFirstRight, innerConnection);
        }
    }

    static int GetMergeResultNumber(int number, int count)
    {
        count += Globals.NumberIndexDic[number];
        if (count >= 11)
            return 2048;
        if (count <= 1)
            return 2;
        return (int)Math.Pow(2, count);
    }

    class BubbleConnection
    {
        public int Number { get; set; }

        public int NextNumber => GetMergeResultNumber(Number, ParentConnection.Connections.Count + 1);

        public BubbleConnection ParentConnection { get; set; }
        public Vector2Int GridPosition { get; set; }
        public List<BubbleConnection> Connections { get; set; }
    }

    static int GetRandomNumber()
    {
        return (int)Math.Pow(2, Random.Range(1, 9));
    }
    static bool IsLineRight(bool isFirstRight, int lineIndex)
    {
        return isFirstRight ? lineIndex % 2 == 0 : lineIndex % 2 == 1;
    }
    float GetLineIndent(int lineIndex)
    {
        return bubbleSize / 2 * (IsLineRight(isFirstRight, lineIndex) ? 1 : 0);
    }

    static Vector2Int GetGridPositionByDirection(Vector2Int referenceGridPos, EnumDirections direction, bool isLineRight)
    {
        Vector2Int diff;

        switch (direction)
        {
            case EnumDirections.Left:
                diff = new Vector2Int(0, -1);
                break;
            case EnumDirections.LeftUp:
                diff = new Vector2Int(-1, isLineRight ? 0 : -1);
                break;
            case EnumDirections.RightUp:
                diff = new Vector2Int(-1, isLineRight ? 1 : 0);
                break;
            case EnumDirections.Right:
                diff = new Vector2Int(0, 1);
                break;
            case EnumDirections.RightDown:
                diff = new Vector2Int(1, isLineRight ? 1 : 0);
                break;
            case EnumDirections.LeftDown:
                diff = new Vector2Int(1, isLineRight ? 0 : -1);
                break;
            default:
                diff = new Vector2Int();
                break;
        }

        return referenceGridPos + diff;
    }


    static List<Vector2Int> GetEdgePositions(List<int[]> virtualGrid, bool isFirstRight)
    {
        var edgePointsDic = new Dictionary<Vector2Int, bool>();

        //We need a empty line at the end as a start point
        virtualGrid.Add(new int[6]);

        var swarmStartPoint = new Vector2Int(virtualGrid.Count - 1, 0);
        SwarmUntilBubbleRecursive(virtualGrid, isFirstRight, new List<Vector2Int>(), edgePointsDic, swarmStartPoint);
        virtualGrid.RemoveAt(virtualGrid.Count - 1);
        
        return edgePointsDic.Select(e => e.Key).ToList();
    }

    static void SwarmUntilBubbleRecursive(List<int[]> virtualGrid, bool isFirstRight, List<Vector2Int> visitedPoints, Dictionary<Vector2Int, bool> edgePointsDic, Vector2Int gridPos)
    {
        visitedPoints.Add(gridPos);

        //swarm to all 6 directions
        var directions = new EnumDirections[]
        {
            EnumDirections.Left,
            EnumDirections.LeftUp,
            EnumDirections.RightUp,
            EnumDirections.Right,
            EnumDirections.RightDown,
            EnumDirections.LeftDown
        };

        for (int i = 0; i < directions.Length; i++)
        {
            var dirGridPos = GetGridPositionByDirection(gridPos, directions[i], isFirstRight);
            if (!IsBubbleGridPositionValid(virtualGrid, dirGridPos))
                continue;
            var isVisited = visitedPoints.Any(v => v[0] == dirGridPos[0] && v[1] == dirGridPos[1]);
            if (virtualGrid[dirGridPos[0]][dirGridPos[1]] != 0)
                edgePointsDic[gridPos] = true;
            else if (!isVisited)
                SwarmUntilBubbleRecursive(virtualGrid, isFirstRight, visitedPoints, edgePointsDic, dirGridPos);
        }
    }


    int GetNumberForNextBubbleInTheQueue(List<int[]> virtualGrid, bool isFirstRight)
    {
        //return GetRandomNumber();

        var edgePositions = GetEdgePositions(virtualGrid, isFirstRight);


        ////REMOVE
        //foreach (var newGridPos in edgePositions)
        //{
        //    var bubble = Instantiate(BubblePrefab, Holder.transform, true).GetComponent<Bubble>();
        //    bubble.transform.position = new Vector3(Holder.transform.position.x + newGridPos[1] * bubbleSize + GetLineIndent(newGridPos[0]), transform.position.y - (newGridPos[0] + 1) * bubbleVerticalDistance, 0);
        //    bubble.Number = 2048;
        //}
        //return 2;
        ////REMOVE

        //we keep the number of possible merges of numbers and sum of possible merges for each point in the current grid state

        Vector2Int[] numberMergeDepthArray = new Vector2Int[11];

        //calculate depth for each position
        virtualGrid.Add(new int[6]); //we will be putting temporary bubbles on edges to test. so we need an extra line.
        foreach (var edgePos in edgePositions)
        {
            //get numbers of surrouding bubbles
            var surroundingBubblePositions = GetSurroundingBubbleGridPositions(virtualGrid, isFirstRight, edgePos);

            //get depth path for each numbers on the edgePos
            foreach (var surroundingBubblePosition in surroundingBubblePositions)
            {
                var surroundingNumber = virtualGrid[surroundingBubblePosition[0]][surroundingBubblePosition[1]];
                //We need to put a Bubble to the position temporarily to simulate as if this bubble is there. We will then remove it.
                virtualGrid[edgePos[0]][edgePos[1]] = surroundingNumber;
                GetDesiredMergePath(virtualGrid, isFirstRight, edgePos, out _, out var desiredMergePath);
                virtualGrid[edgePos[0]][edgePos[1]] = 0;
                numberMergeDepthArray[Globals.NumberIndexDic[surroundingNumber]][1] += desiredMergePath.Count; //sum of all merge points' merge count
                numberMergeDepthArray[Globals.NumberIndexDic[surroundingNumber]][0]++; //how many possible merge points for this number
            }
        }

        var countRatio = 1 - Globals.NextNumberDepthToCountScale;
        var sumRatio = Globals.NextNumberDepthToCountScale;

        //normalize data and calculate the score for each number
        var maxPositionCount = numberMergeDepthArray.Max(a => a[0]);
        var maxSum = numberMergeDepthArray.Max(a => a[1]);
        var scoreDic = new Dictionary<int, float>();

        for (var numberIndex = 0; numberIndex < numberMergeDepthArray.Length; numberIndex++)
        {
            var numberVecInt = numberMergeDepthArray[numberIndex];
            var count = (float) numberVecInt[0] / maxPositionCount;
            var sum = (float) numberVecInt[1] / maxSum;
            var score = countRatio * count + sumRatio * sum;
            scoreDic[numberIndex] = score;
        }

        //We choose the number with highest score
        return Globals.IndexNumberDic[scoreDic.OrderByDescending(s => s.Value).Select(s => s.Key).First()];
    }

    static List<Vector2Int> GetSurroundingBubbleGridPositions(List<int[]> virtualGrid, bool isFirstRight, Vector2Int gridPos)
    {
        var surroundingBubblePositions = new List<Vector2Int>();
        var directions = new EnumDirections[]
        {
            EnumDirections.Left,
            EnumDirections.LeftUp,
            EnumDirections.RightUp,
            EnumDirections.Right,
            EnumDirections.RightDown,
            EnumDirections.LeftDown
        };
        for (int i = 0; i < directions.Length; i++)
        {
            var dirGridPos = GetGridPositionByDirection(gridPos, directions[i], isFirstRight);
            var bubbleNumber = GetBubbleNumber(virtualGrid, dirGridPos);
            if (bubbleNumber != 0)
                surroundingBubblePositions.Add(dirGridPos);
        }

        return surroundingBubblePositions;
    }

    static int GetBubbleNumber(List<int[]> virtualGrid, Vector2Int gridPos)
    {
        if (IsBubbleGridPositionValid(virtualGrid, gridPos))
            return virtualGrid[gridPos[0]][gridPos[1]];
        return 0;
    }

    static List<int[]> GetNewVirtualGrid(List<Bubble[]> grid)
    {
        var virtualGrid = new List<int[]>();
        foreach (Bubble[] bubbles in grid)
        {
            var newLine = new int[6];
            for (int i = 0; i < bubbles.Length; i++)
            {
                newLine[i] = bubbles[i]?.Number ?? 0;
            }
            virtualGrid.Add(newLine);
        }
        return virtualGrid;
    }
}