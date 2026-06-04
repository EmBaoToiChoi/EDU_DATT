using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GamePlay : MonoBehaviour
{
    public MazeNavigator navigator;
    public QuestionManager questionManager;
    public SimpleUI ui;
    public GameObject startScreen;
    public float speed = 2f;
    public float reachThreshold = 0.1f;

    Vector3Int currentCell;
    Vector3Int previousCell;
    readonly List<Vector3Int> moveQueue = new List<Vector3Int>();
    readonly List<Vector3Int> visitedCells = new List<Vector3Int>();

    int consecutiveWrong = 0;
    bool waitingForAnswer = false;
    Tilemap walkableTilemap;

    void Start()
    {
        if (navigator != null)
        {
            if (navigator.generateRandomMaze)
            {
                navigator.GenerateMaze();
            }
            else
            {
                navigator.RefreshNodes();
            }
            walkableTilemap = navigator.walkableTilemap;
            currentCell = navigator.GetStartCell();
            if (walkableTilemap != null)
            {
                transform.position = walkableTilemap.GetCellCenterWorld(currentCell);
                transform.localScale = walkableTilemap.transform.localScale;
                if (navigator.startPoint != null) navigator.startPoint.localScale = walkableTilemap.transform.localScale;
                if (navigator.goalPoint != null) navigator.goalPoint.localScale = walkableTilemap.transform.localScale;
                previousCell = currentCell;
                visitedCells.Clear();
                moveQueue.Clear();
                visitedCells.Add(currentCell);
                
                if (startScreen != null)
                {
                    startScreen.SetActive(true);
                }
                else
                {
                    PrepareNextStep(currentCell);
                }
            }
        }
    }

    void Update()
    {
        if (waitingForAnswer || walkableTilemap == null) return;
        if (moveQueue.Count == 0) return;

        Vector3Int nextCell = moveQueue[0];
        Vector3 targetWorld = walkableTilemap.GetCellCenterWorld(nextCell);
        transform.position = Vector3.MoveTowards(transform.position, targetWorld, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorld) <= reachThreshold)
        {
            transform.position = targetWorld;
            moveQueue.RemoveAt(0);
            previousCell = currentCell;
            currentCell = nextCell;
            visitedCells.Add(currentCell);
            HandleArrival(currentCell);
        }
    }

    void HandleArrival(Vector3Int cell)
    {
        if (navigator == null || walkableTilemap == null) return;

        if (navigator.IsDeadCell(cell))
        {
            ResetToStart();
            return;
        }

        if (cell == navigator.GetGoalCell())
        {
            Debug.Log("Reached goal");
            return;
        }

        var options = navigator.GetNeighbors(cell, previousCell);
        if (options.Count == 0)
        {
            Debug.Log($"Dead end at cell {cell}");
            return;
        }

        // Chỉ hỏi khi ô hiện tại thật sự có hơn 1 hướng đi hợp lệ (không tính ô vừa đi qua)
        bool shouldAsk = options.Count > 1;

        if (shouldAsk)
        {
            waitingForAnswer = true;
            Debug.Log($"Question at cell {cell} with {options.Count} option(s)");
            questionManager.ShowRandomQuestion((correct) =>
            {
                if (correct) consecutiveWrong = 0;
                else consecutiveWrong++;

                SetQueueToSingleCell(navigator.ChooseNextCell(cell, previousCell, correct, consecutiveWrong));

                waitingForAnswer = false;
            });
            return;
        }

        SetQueueToSingleCell(navigator.ChooseNextCell(cell, previousCell, true, consecutiveWrong));
    }

    void PrepareNextStep(Vector3Int cell)
    {
        if (navigator == null || walkableTilemap == null) return;

        var options = navigator.GetNeighbors(cell, previousCell);
        if (options.Count > 0)
        {
            // Bắt đầu đi ngay từ ô kế tiếp, không hiện câu hỏi ở ô spawn đầu tiên
            SetQueueToSingleCell(navigator.ChooseNextCell(cell, previousCell, true, 0));
        }
    }

    void SetQueueToSingleCell(Vector3Int cell)
    {
        moveQueue.Clear();
        moveQueue.Add(cell);
    }

    public void StartGame()
    {
        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }

        if (navigator != null)
        {
            if (navigator.generateRandomMaze)
            {
                navigator.GenerateMaze();
            }
            else
            {
                navigator.RefreshNodes();
            }
            walkableTilemap = navigator.walkableTilemap;
            currentCell = navigator.GetStartCell();
            if (walkableTilemap != null)
            {
                transform.position = walkableTilemap.GetCellCenterWorld(currentCell);
                transform.localScale = walkableTilemap.transform.localScale;
                if (navigator.startPoint != null) navigator.startPoint.localScale = walkableTilemap.transform.localScale;
                if (navigator.goalPoint != null) navigator.goalPoint.localScale = walkableTilemap.transform.localScale;
                previousCell = currentCell;
                visitedCells.Clear();
                moveQueue.Clear();
                visitedCells.Add(currentCell);
                consecutiveWrong = 0;
                waitingForAnswer = false;

                PrepareNextStep(currentCell);
                Debug.Log("Game started/restarted via button");
            }
        }
    }

    void ResetToStart()
    {
        Vector3Int startCell = navigator.GetStartCell();
        transform.position = walkableTilemap.GetCellCenterWorld(startCell);
        transform.localScale = walkableTilemap.transform.localScale;
        if (navigator.startPoint != null) navigator.startPoint.localScale = walkableTilemap.transform.localScale;
        if (navigator.goalPoint != null) navigator.goalPoint.localScale = walkableTilemap.transform.localScale;
        currentCell = startCell;
        previousCell = startCell;
        moveQueue.Clear();
        visitedCells.Clear();
        visitedCells.Add(startCell);
        consecutiveWrong = 0;
        waitingForAnswer = false;

        if (startScreen != null)
        {
            startScreen.SetActive(true);
        }
        else
        {
            PrepareNextStep(startCell);
        }
        Debug.Log("Reset to start after dead end");
    }
}
