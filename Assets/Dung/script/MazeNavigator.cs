using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MazeNavigator : MonoBehaviour
{
    public Tilemap walkableTilemap;
    public float cellSize = 1f;
    public float cellTolerance = 0.2f;

    public Transform startPoint;
    public Transform goalPoint;
    public Transform shortRoot;
    public Transform longRoot;
    public Transform deadRoot;

    readonly Dictionary<Vector3Int, int> goalDistanceMap = new Dictionary<Vector3Int, int>();
    readonly Dictionary<Vector3Int, Vector3Int> nextStepToGoalMap = new Dictionary<Vector3Int, Vector3Int>();
    readonly HashSet<Vector3Int> shortCells = new HashSet<Vector3Int>();
    readonly HashSet<Vector3Int> longCells = new HashSet<Vector3Int>();
    readonly HashSet<Vector3Int> deadCells = new HashSet<Vector3Int>();

    readonly Vector3Int[] directions = new Vector3Int[]
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.left,
        Vector3Int.right
    };

    public void RefreshNodes()
    {
        if (walkableTilemap == null)
        {
            var tilemaps = FindObjectsOfType<Tilemap>();
            if (tilemaps != null && tilemaps.Length > 0)
                walkableTilemap = tilemaps[0];
        }

        if (startPoint == null)
        {
            var start = FindMarker<StartPoint>();
            if (start != null) startPoint = start.transform;
        }

        if (goalPoint == null)
        {
            var goal = FindMarker<GoalPoint>();
            if (goal != null) goalPoint = goal.transform;
        }

        if (shortRoot == null) shortRoot = GameObject.Find("Short")?.transform;
        if (longRoot == null) longRoot = GameObject.Find("Long")?.transform;

        if (deadRoot == null)
        {
            var deadObject = GameObject.Find("Dead");
            if (deadObject != null) deadRoot = deadObject.transform;
        }

        RebuildRouteCellSets();

        RebuildGoalDistanceMap();
    }

    void RebuildRouteCellSets()
    {
        shortCells.Clear();
        longCells.Clear();
        deadCells.Clear();

        if (walkableTilemap == null) return;

        CollectCells(shortRoot, shortCells);
        CollectCells(longRoot, longCells);
        CollectCells(deadRoot, deadCells);
    }

    void CollectCells(Transform root, HashSet<Vector3Int> set)
    {
        if (root == null || walkableTilemap == null) return;

        AddCellRecursive(root, set);
    }

    void AddCellRecursive(Transform node, HashSet<Vector3Int> set)
    {
        if (node == null) return;
        set.Add(walkableTilemap.WorldToCell(node.position));

        for (int i = 0; i < node.childCount; i++)
            AddCellRecursive(node.GetChild(i), set);
    }

    void RebuildGoalDistanceMap()
    {
        goalDistanceMap.Clear();
        nextStepToGoalMap.Clear();
        if (walkableTilemap == null) return;

        Vector3Int goalCell = GetGoalCell();
        if (!walkableTilemap.HasTile(goalCell)) return;

        var queue = new Queue<Vector3Int>();
        queue.Enqueue(goalCell);
        goalDistanceMap[goalCell] = 0;
        nextStepToGoalMap[goalCell] = goalCell;

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            int baseDistance = goalDistanceMap[cell];

            foreach (var dir in directions)
            {
                var neighbor = cell + dir;
                if (!walkableTilemap.HasTile(neighbor)) continue;
                if (goalDistanceMap.ContainsKey(neighbor)) continue;

                goalDistanceMap[neighbor] = baseDistance + 1;
                nextStepToGoalMap[neighbor] = cell;
                queue.Enqueue(neighbor);
            }
        }
    }

    public Vector3Int GetStartCell()
    {
        if (walkableTilemap == null) return Vector3Int.zero;

        if (startPoint != null)
            return walkableTilemap.WorldToCell(startPoint.position);

        return walkableTilemap.WorldToCell(transform.position);
    }

    public Vector3Int GetGoalCell()
    {
        if (walkableTilemap == null) return Vector3Int.zero;

        if (goalPoint != null)
            return walkableTilemap.WorldToCell(goalPoint.position);

        return walkableTilemap.WorldToCell(transform.position);
    }

    public List<Vector3Int> GetNeighbors(Vector3Int cell, Vector3Int previousCell, bool includePrevious = false)
    {
        var result = new List<Vector3Int>();
        if (walkableTilemap == null) return result;

        foreach (var dir in directions)
        {
            var neighbor = cell + dir;
            if (!includePrevious && neighbor == previousCell) continue;
            if (walkableTilemap.HasTile(neighbor))
                result.Add(neighbor);
        }
        return result;
    }

    public bool IsDeadEndCell(Vector3Int cell, Vector3Int fromCell)
    {
        if (walkableTilemap == null) return false;

        var options = GetNeighbors(cell, fromCell);
        return options.Count <= 1;
    }

    public bool IsDeadCell(Vector3Int cell)
    {
        return deadCells.Contains(cell);
    }

    public bool IsShortCell(Vector3Int cell)
    {
        return shortCells.Contains(cell);
    }

    public bool IsLongCell(Vector3Int cell)
    {
        return longCells.Contains(cell);
    }

    public Vector3Int ChooseNextCell(Vector3Int currentCell, Vector3Int previousCell, bool correct, int consecutiveWrong)
    {
        var options = GetNeighbors(currentCell, previousCell, includePrevious: !correct);
        if (options.Count == 0) return currentCell;

        if (correct)
        {
            return PickBestCorrectCell(options, currentCell, previousCell);
        }

        // Sai: ưu tiên đi vào Long trước, nếu không có thì mới vào Dead.
        for (int i = 0; i < options.Count; i++)
            if (longCells.Contains(options[i]))
                return options[i];

        for (int i = 0; i < options.Count; i++)
            if (deadCells.Contains(options[i]))
                return options[i];

        // Fallback: nếu không map được group nào thì đi tiếp theo lựa chọn gần goal nhất,
        // tuyệt đối không quay về Start.
        return PickClosestToGoal(options, GetGoalCell());
    }

    Vector3Int PickClosestToGoal(List<Vector3Int> options, Vector3Int goalCell)
    {
        Vector3Int best = options[0];
        float bestScore = ScoreToGoal(best, goalCell);
        for (int i = 1; i < options.Count; i++)
        {
            float score = ScoreToGoal(options[i], goalCell);
            if (score < bestScore)
            {
                bestScore = score;
                best = options[i];
            }
        }
        return best;
    }

    Vector3Int PickBestCorrectCell(List<Vector3Int> options, Vector3Int currentCell, Vector3Int previousCell)
    {
        // Đúng: ưu tiên tuyệt đối các ô thuộc Short
        for (int i = 0; i < options.Count; i++)
            if (shortCells.Contains(options[i]) && !deadCells.Contains(options[i]))
                return options[i];

        // Nếu không map được short, chọn ô tiến gần goal nhất nhưng không được vào Dead.
        if (nextStepToGoalMap.TryGetValue(currentCell, out var mappedNext) && options.Contains(mappedNext) && !deadCells.Contains(mappedNext))
            return mappedNext;

        Vector3Int best = options[0];
        int bestScore = GetGoalDistance(best);
        for (int i = 1; i < options.Count; i++)
        {
            if (deadCells.Contains(options[i]) || longCells.Contains(options[i]))
                continue;

            int score = GetGoalDistance(options[i]);
            if (score < bestScore)
            {
                bestScore = score;
                best = options[i];
            }
            else if (score == bestScore && options[i] != previousCell && best == previousCell)
            {
                best = options[i];
            }
        }

        return best;
    }

    Vector3Int PickMostForward(List<Vector3Int> options, Vector3Int currentCell, Vector3Int forwardDir, Vector3Int goalCell)
    {
        Vector3Int best = options[0];
        float bestScore = DirectionScore(options[0] - currentCell, forwardDir);
        float bestGoal = ScoreToGoal(best, goalCell);

        for (int i = 1; i < options.Count; i++)
        {
            Vector3Int step = options[i] - currentCell;
            float score = DirectionScore(step, forwardDir);
            float goalScore = ScoreToGoal(options[i], goalCell);

            if (score > bestScore || (Mathf.Approximately(score, bestScore) && goalScore < bestGoal))
            {
                best = options[i];
                bestScore = score;
                bestGoal = goalScore;
            }
        }

        return best;
    }

    Vector3Int PickFarthestFromGoal(List<Vector3Int> options, Vector3Int goalCell)
    {
        Vector3Int best = options[0];
        float bestScore = ScoreToGoal(best, goalCell);
        for (int i = 1; i < options.Count; i++)
        {
            float score = ScoreToGoal(options[i], goalCell);
            if (score > bestScore)
            {
                bestScore = score;
                best = options[i];
            }
        }
        return best;
    }

    Vector3Int PickDeadEndLike(List<Vector3Int> options, Vector3Int currentCell, Vector3Int goalCell, Vector3Int previousCell)
    {
        Vector3Int best = options[0];
        int bestBranchCount = GetNeighbors(options[0], previousCell).Count;
        float bestGoalScore = ScoreToGoal(best, goalCell);

        for (int i = 1; i < options.Count; i++)
        {
            int branchCount = GetNeighbors(options[i], currentCell).Count;
            float goalScore = ScoreToGoal(options[i], goalCell);

            if (branchCount < bestBranchCount || (branchCount == bestBranchCount && goalScore > bestGoalScore))
            {
                best = options[i];
                bestBranchCount = branchCount;
                bestGoalScore = goalScore;
            }
        }

        return best;
    }

    float DirectionScore(Vector3Int step, Vector3Int forwardDir)
    {
        if (step == Vector3Int.zero || forwardDir == Vector3Int.zero) return float.MinValue;

        int dot = step.x * forwardDir.x + step.y * forwardDir.y + step.z * forwardDir.z;
        return dot;
    }

    float ScoreToGoal(Vector3Int cell, Vector3Int goalCell)
    {
        if (goalPoint == null && goalCell == Vector3Int.zero) return 0f;
        Vector3Int delta = cell - goalCell;
        return delta.x * delta.x + delta.y * delta.y + delta.z * delta.z;
    }

    int GetGoalDistance(Vector3Int cell)
    {
        if (goalDistanceMap.Count == 0)
            RebuildGoalDistanceMap();

        if (goalDistanceMap.TryGetValue(cell, out int distance))
            return distance;

        return int.MaxValue;
    }

    T FindMarker<T>() where T : Component
    {
        var markers = FindObjectsOfType<T>();
        return markers != null && markers.Length > 0 ? markers[0] : null;
    }
}

