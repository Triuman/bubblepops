using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private Vector3 shooterPos;
    Vector2Int _emptyCircleGridPos = new Vector2Int(-1, -1);
    private bool isMouseDown = false;
    Vector2? touchPositionWorld = null;
    Vector3 holderTargetPos;

    private Bubble shooterCurrentBubble;
    List<Bubble> waitingQueue = new List<Bubble>();

    readonly Random random = new Random();

    enum EnumAnimationStates
    {
        NoAnimation = 0,
        PuttingBubbleToShooter = 1,
        AddingNewLine = 2,
        Shooting = 4,
        Merging = 8
    }

    //Using binary state to keep more than one state at a time.
    //Do not send the state directly
    private int currentAnimationStateBinary;

    bool IsAnimating()
    {
        return !IsInAnimationStateOnly(0);
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
        shooterCurrentBubble.MoveTo(new List<Vector3>() { GlobalPositionToScaledHolderPosition(shooterPos - Holder.transform.position) }, 4);
        AddAnimationState(EnumAnimationStates.PuttingBubbleToShooter);
        shooterCurrentBubble.Arrived += OnBubbleArrivedToShooter;
    }

    private void OnBubbleArrivedToShooter(Bubble bubble)
    {
        shooterCurrentBubble.Arrived -= OnBubbleArrivedToShooter;
        RemoveAnimationState(EnumAnimationStates.PuttingBubbleToShooter);
    }

    void AddNewBubbleToWaitingQueue(int number)
    {
        var newBubble = Instantiate(BubblePrefab, Holder.transform, true).GetComponent<Bubble>();
        newBubble.transform.position = new Vector3(shooterPos.x - bubbleSize, shooterPos.y, waitingQueue.Count + 1);
        newBubble.transform.localScale = newBubble.transform.localScale * 0.7f;
        newBubble.Number = number;
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
                shootDestinations.Add(GlobalPositionToScaledHolderPosition(new Vector3(_emptyCircleGridPos[1] * bubbleSize + GetLineIndent((int)_emptyCircleGridPos[0]), transform.position.y - (_emptyCircleGridPos[0] + 1) * bubbleVerticalDistance - Holder.transform.position.y, BubblePrefab.transform.position.z)));
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
        }
    }

    void InitGrid()
    {
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
            if ((int)_emptyCircleGridPos[0] == grid.Count)
                grid.Add(new Bubble[6]);
            grid[(int)_emptyCircleGridPos[0]][(int)_emptyCircleGridPos[1]] = shooterCurrentBubble;
            shooterCurrentBubble.IgnoreRaycast = false;
            shooterCurrentBubble.MoveTo(shootDestinations, 20);
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
        bool isRight = isFirstRight ? (int)gridPos[0] % 2 == 0 : (int)gridPos[0] % 2 == 1;
        Vector2Int[] surroundingGridPosDiffs = new[]
        {
            new Vector2Int(0, -1), //left
            new Vector2Int(-1, isRight ? 0 : -1), //left up
            new Vector2Int(-1, isRight ? 1 : 0), //right up
            new Vector2Int(0, 1), //right
            new Vector2Int(1, isRight ? 1 : 0), //right down
            new Vector2Int(1, isRight ? 0 : -1) //left down
        };
        float distance = 0.1f;
        for (int i = 0; i < 6; i++)
        {
            var bubblePos = gridPos + surroundingGridPosDiffs[i];
            if (bubblePos[1] > -1 && bubblePos[1] < 6 && bubblePos[0] > -1 && grid.Count > bubblePos[0] && grid[(int)bubblePos[0]][(int)bubblePos[1]] != null)
            {
                var bubble = grid[(int)bubblePos[0]][(int)bubblePos[1]];
                var angle = 2 * Math.PI / 6 * i + Math.PI; //As we start from left, we add some more radian to rotate it.
                Vector3 direction = new Vector3((float)Math.Cos(angle), -(float)Math.Sin(angle));
                bubble.MoveTo(new List<Vector3>()
                {
                    bubble.transform.localPosition + direction * distance,
                    bubble.transform.localPosition,
                }, 1);
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
        var hitBubble = bubbleHit2D.transform.parent.GetComponent<Bubble>();
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
        bool isHitLineRight = isFirstRight ? (int)hitBubbleGridPos[0] % 2 == 0 : (int)hitBubbleGridPos[0] % 2 == 1;
        if (hitAngle <= -Math.PI / 2) //left bottom
        {
            emptyCircleGridPos = hitBubbleGridPos + new Vector2Int(1, -1 + (isHitLineRight ? 1 : 0));
            if (grid.Count > (int)emptyCircleGridPos[0] && grid[(int)emptyCircleGridPos[0]][(int)emptyCircleGridPos[1]] != null)
            {
                if (hitAngle <= -Math.PI / 2 - Math.PI / 4) //left
                {
                    emptyCircleGridPos[0] = (int)hitBubbleGridPos[0];
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
            if (grid.Count > (int)emptyCircleGridPos[0] && grid[(int)emptyCircleGridPos[0]][(int)emptyCircleGridPos[1]] != null)
            {

                if (hitAngle >= -Math.PI / 4) //right
                {
                    emptyCircleGridPos[0] = (int)hitBubbleGridPos[0];
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
        if (grid.Count > (int)newGridPos[0])
        {
            if (grid[(int)newGridPos[0]][(int)newGridPos[1]] != null)
            {
                Debug.LogError("Same place!");
                return;
            }
        }

        EmptyCircle.transform.position = new Vector3(Holder.transform.position.x + newGridPos[1] * bubbleSize + GetLineIndent((int)newGridPos[0]), transform.position.y - (newGridPos[0] + 1) * bubbleVerticalDistance, 0);

        EmptyCircle.gameObject.SetActive(true);
        if ((int)_emptyCircleGridPos[0] != (int)newGridPos[0] ||
            (int)_emptyCircleGridPos[1] != (int)newGridPos[1])
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
        //TODO: Check effect of this bubble
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

    static Bubble GetBubble(List<Bubble[]> virtualGrid, int lineIndex, int columnIndex)
    {
        if (virtualGrid.Count <= lineIndex || lineIndex < 0 || columnIndex < 0 || columnIndex >= 6)
            return null;
        Debug.LogWarning(lineIndex);
        Debug.LogWarning(columnIndex);
        return virtualGrid[lineIndex][columnIndex];
    }
    static Bubble GetBubble(List<Bubble[]> virtualGrid, Vector2Int bubblePos)
    {
        return GetBubble(virtualGrid, bubblePos[0], bubblePos[1]);
    }


    static void GetConnectedBubblesWithSameNumber(List<Bubble[]> virtualGrid, bool isFirstRight, Vector2Int bubbleGridPos, int number, EnumDirections ignoredDirection, BubbleConnection parentConnection)
    {
        //find if to right or left
        bool isRight = isFirstRight ? bubbleGridPos[0] % 2 == 0 : bubbleGridPos[0] % 2 == 1;

        //calculate touching bubble indices starting from left up
        //iterate over them
        //if you find same number, check that number's area too but only the ones this one is not touching
        if (ignoredDirection != EnumDirections.LeftUp && ignoredDirection != EnumDirections.Left &&
            ignoredDirection != EnumDirections.LeftDown)
        {
            var leftBubblePos = new Vector2Int(bubbleGridPos[0], bubbleGridPos[1] - 1);
            var bubble = GetBubble(virtualGrid, leftBubblePos);
            if (bubble != null && bubble.Number == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(leftBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = leftBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumber(virtualGrid, isFirstRight, leftBubblePos, number, EnumDirections.Right, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.Left && ignoredDirection != EnumDirections.LeftUp &&
            ignoredDirection != EnumDirections.RightUp)
        {
            var leftUpBubblePos = new Vector2Int(bubbleGridPos[0] - 1, bubbleGridPos[1] + (isRight ? 0 : -1));
            var bubble = GetBubble(virtualGrid, leftUpBubblePos);
            if (bubble != null && bubble.Number == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(leftUpBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = leftUpBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumber(virtualGrid, isFirstRight, leftUpBubblePos, number, EnumDirections.RightDown, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.LeftUp && ignoredDirection != EnumDirections.RightUp &&
            ignoredDirection != EnumDirections.Right)
        {
            var rightUpBubblePos = new Vector2Int(bubbleGridPos[0] - 1, bubbleGridPos[1] + (isRight ? 1 : 0));
            var bubble = GetBubble(virtualGrid, rightUpBubblePos);
            if (bubble != null && bubble.Number == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(rightUpBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = rightUpBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumber(virtualGrid, isFirstRight, rightUpBubblePos, number, EnumDirections.LeftDown, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.RightUp && ignoredDirection != EnumDirections.Right &&
            ignoredDirection != EnumDirections.RightDown)
        {
            var rightBubblePos = new Vector2Int(bubbleGridPos[0], bubbleGridPos[1] + 1);
            var bubble = GetBubble(virtualGrid, rightBubblePos);
            if (bubble != null && bubble.Number == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(rightBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = rightBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumber(virtualGrid, isFirstRight, rightBubblePos, number, EnumDirections.Left, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.Right && ignoredDirection != EnumDirections.RightDown &&
            ignoredDirection != EnumDirections.LeftDown)
        {
            var rightDownBubblePos = new Vector2Int(bubbleGridPos[0] + 1, bubbleGridPos[1] + (isRight ? 1 : 0));
            var bubble = GetBubble(virtualGrid, rightDownBubblePos);
            if (bubble != null && bubble.Number == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(rightDownBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = rightDownBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumber(virtualGrid, isFirstRight, rightDownBubblePos, number, EnumDirections.LeftUp, parentConnection);
            }
        }

        if (ignoredDirection != EnumDirections.Left && ignoredDirection != EnumDirections.LeftDown &&
            ignoredDirection != EnumDirections.RightDown)
        {
            var leftDownBubblePos = new Vector2Int(bubbleGridPos[0] + 1, bubbleGridPos[1] + (isRight ? 0 : -1));
            var bubble = GetBubble(virtualGrid, leftDownBubblePos);
            if (bubble != null && bubble.Number == number && !parentConnection.Connections.Any(b => b.GridPosition.Equals(leftDownBubblePos)))
            {
                var newConnection = new BubbleConnection()
                {
                    Number = number,
                    GridPosition = leftDownBubblePos,
                    Connections = new List<BubbleConnection>(),
                    ParentConnection = parentConnection
                };
                parentConnection.Connections.Add(newConnection);
                GetConnectedBubblesWithSameNumber(virtualGrid, isFirstRight, leftDownBubblePos, number, EnumDirections.RightUp, parentConnection);
            }
        }
        
    }

    //TODO: Put a favourity scale. if 1, we will choose the deepest path. if 0, we will choose the shortest.

    void CalculateAndAnimatePops(Vector2Int bubbleGridPos)
    {
        var virtualGrid = grid.ToList();

        var rootConnection = new BubbleConnection()
        {
            Number = GetBubble(virtualGrid, bubbleGridPos).Number,
            GridPosition = bubbleGridPos,
            Connections = new List<BubbleConnection>()
        };
        //We add same object without reference to prevent infinite loop.
        rootConnection.Connections.Add(new BubbleConnection()
        {
            Number = GetBubble(virtualGrid, bubbleGridPos).Number,
            GridPosition = bubbleGridPos,
            Connections = new List<BubbleConnection>(),
            ParentConnection = rootConnection
        });
        
        GetConnectedBubblesWithSameNumber(virtualGrid, isFirstRight, bubbleGridPos, rootConnection.Number, EnumDirections.None, rootConnection);

        //As there is a exception on NextNumber for the first level, we call this once manually.
        foreach (var innerConnection in rootConnection.Connections)
        {
            GetConnectedBubblesWithSameNumber(virtualGrid, isFirstRight, innerConnection.GridPosition, GetMergeResultNumber(rootConnection.Number, rootConnection.Connections.Count), EnumDirections.None, innerConnection);
            if (innerConnection.Connections.Count > 0)
                FindDeepConnections(virtualGrid, isFirstRight, innerConnection);
        }
        

        
        //TODO: find the deepest connection and start animating

    }

    static void FindDeepConnections(List<Bubble[]> virtualGrid, bool isFirstRight, BubbleConnection connection)
    {
        foreach (var innerConnection in connection.Connections)
        {
            GetConnectedBubblesWithSameNumber(virtualGrid, isFirstRight, innerConnection.GridPosition, innerConnection.NextNumber, EnumDirections.None, innerConnection);
            if(innerConnection.Connections.Count > 0)
                FindDeepConnections(virtualGrid, isFirstRight, innerConnection);
        }
    }

    static int GetMergeResultNumber(int number, int count)
    {
        return (int)Math.Pow(2, Globals.NumberIndexDic[number] + count);
    }

    class BubbleConnection
    {
        public int Number { get; set; }

        public int NextNumber
        {
            get { return GetMergeResultNumber(Number, ParentConnection.Connections.Count + 1); }
        }

        public BubbleConnection ParentConnection { get; set; }
        public Vector2Int GridPosition { get; set; }
        public List<BubbleConnection> Connections { get; set; }
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


    //static void GetConnectedBubblesOld(List<Bubble[]> virtualGrid, Vector2Int bubbleGridPos, EnumDirections ignoredDirection, List<Vector2Int> sameBubbleList)
    //{
    //    sameBubbleList = sameBubbleList ?? new List<Vector2Int>();
    //    int currentBubbleValue = GetBubbleNumber(bubbleGridPos);

    //    //find if to right or left
    //    bool isRight = isFirstRight ? bubbleGridPos[0] % 2 == 0 : bubbleGridPos[0] % 2 == 1;

    //    //calculate touching bubble indices starting from left up
    //    //iterate over them
    //    //if you find same number, check that number's area too but only the ones this one is not touching
    //    if (ignoredDirection != EnumDirections.LeftUp && ignoredDirection != EnumDirections.Left &&
    //        ignoredDirection != EnumDirections.LeftDown)
    //    {
    //        var leftBubblePos = new Vector2Int(bubbleGridPos[0], bubbleGridPos[1] - 1);
    //        if (GetBubbleNumber(leftBubblePos) == currentBubbleValue)
    //        {
    //            sameBubbleList.Add(leftBubblePos);
    //            GetConnectedBubbles(virtualGrid, leftBubblePos, EnumDirections.Right, sameBubbleList);
    //        }
    //    }

    //    if (ignoredDirection != EnumDirections.Left && ignoredDirection != EnumDirections.LeftUp &&
    //        ignoredDirection != EnumDirections.RightUp)
    //    {
    //        var leftUpBubblePos = new Vector2Int(bubbleGridPos[0] + 1, bubbleGridPos[1] + (isRight ? 0 : -1));
    //        if (GetBubbleNumber(leftUpBubblePos) == currentBubbleValue)
    //        {
    //            sameBubbleList.Add(leftUpBubblePos);
    //            GetConnectedBubbles(virtualGrid, leftUpBubblePos, EnumDirections.RightDown, sameBubbleList);
    //        }
    //    }

    //    if (ignoredDirection != EnumDirections.LeftUp && ignoredDirection != EnumDirections.RightUp &&
    //        ignoredDirection != EnumDirections.Right)
    //    {
    //        var rightUpBubblePos = new Vector2Int(bubbleGridPos[0] + 1, bubbleGridPos[1] + (isRight ? 1 : 0));
    //        if (GetBubbleNumber(rightUpBubblePos) == currentBubbleValue)
    //        {
    //            sameBubbleList.Add(rightUpBubblePos);
    //            GetConnectedBubbles(virtualGrid, rightUpBubblePos, EnumDirections.LeftDown, sameBubbleList);
    //        }
    //    }

    //    if (ignoredDirection != EnumDirections.RightUp && ignoredDirection != EnumDirections.Right &&
    //        ignoredDirection != EnumDirections.RightDown)
    //    {
    //        var rightBubblePos = new Vector2Int(bubbleGridPos[0] + 1, bubbleGridPos[1] + 1);
    //        if (GetBubbleNumber(rightBubblePos) == currentBubbleValue)
    //        {
    //            sameBubbleList.Add(rightBubblePos);
    //            GetConnectedBubbles(virtualGrid, rightBubblePos, EnumDirections.Left, sameBubbleList);
    //        }
    //    }

    //    if (ignoredDirection != EnumDirections.Right && ignoredDirection != EnumDirections.RightDown &&
    //        ignoredDirection != EnumDirections.LeftDown)
    //    {
    //        var rightDownBubblePos = new Vector2Int(bubbleGridPos[0] - 1, bubbleGridPos[1] + (isRight ? 1 : 0));
    //        if (GetBubbleNumber(rightDownBubblePos) == currentBubbleValue)
    //        {
    //            sameBubbleList.Add(rightDownBubblePos);
    //            GetConnectedBubbles(virtualGrid, rightDownBubblePos, EnumDirections.LeftUp, sameBubbleList);
    //        }
    //    }

    //    if (ignoredDirection != EnumDirections.Left && ignoredDirection != EnumDirections.LeftDown &&
    //        ignoredDirection != EnumDirections.RightDown)
    //    {
    //        var leftDownBubblePos = new Vector2Int(bubbleGridPos[0] - 1, bubbleGridPos[1] + (isRight ? 0 : -1));
    //        if (GetBubbleNumber(leftDownBubblePos) == currentBubbleValue)
    //        {
    //            sameBubbleList.Add(leftDownBubblePos);
    //            GetConnectedBubbles(virtualGrid, leftDownBubblePos, EnumDirections.RightUp, sameBubbleList);
    //        }
    //    }


    //    //TODO: We also need to decide where to connect while doing this calculations.

    //}

}