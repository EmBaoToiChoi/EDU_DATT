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

    [Header("Alignment Settings")]
    public Renderer backgroundRenderer;
    [Range(0.5f, 1.0f)]
    public float mazeFitPadding = 0.85f;

    [Header("Visual Prefabs")]
    public GameObject goalVisualPrefab;

    [Header("Maze Generator Settings")]
    public bool generateRandomMaze = true;
    public int mazeWidth = 19;
    public int mazeHeight = 13;
    public TileBase walkableTile;

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
        if (generateRandomMaze)
        {
            return IsDeadEndTerminal(cell);
        }
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
        if (generateRandomMaze)
        {
            var options = GetNeighbors(currentCell, previousCell, includePrevious: false);
            if (options.Count == 0) return currentCell;

            if (correct)
            {
                // Trả lời đúng -> tìm đường đi ngắn nhất đến Goal
                Vector3Int bestCell = options[0];
                int minDistance = GetGoalDistance(bestCell);

                for (int i = 1; i < options.Count; i++)
                {
                    int dist = GetGoalDistance(options[i]);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestCell = options[i];
                    }
                }
                return bestCell;
            }
            else
            {
                // Trả lời sai -> ưu tiên đi vào ngõ cụt gần nhất
                List<Vector3Int> deadEndOptions = new List<Vector3Int>();
                foreach (var opt in options)
                {
                    if (IsDeadEndOption(opt, currentCell))
                    {
                        deadEndOptions.Add(opt);
                    }
                }

                if (deadEndOptions.Count > 0)
                {
                    return deadEndOptions[UnityEngine.Random.Range(0, deadEndOptions.Count)];
                }
                else
                {
                    // Không có ngõ cụt -> đi đường dài nhất đến Goal
                    Vector3Int worstCell = options[0];
                    int maxDistance = GetGoalDistance(worstCell);

                    for (int i = 1; i < options.Count; i++)
                    {
                        int dist = GetGoalDistance(options[i]);
                        if (dist > maxDistance && dist != int.MaxValue)
                        {
                            maxDistance = dist;
                            worstCell = options[i];
                        }
                    }
                    return worstCell;
                }
            }
        }
        else
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

    public void GenerateMaze()
    {
#if UNITY_EDITOR
        if (goalVisualPrefab == null || !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(goalVisualPrefab))
        {
            string path = "Assets/Dung/Prefab/Goal.prefab";
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                goalVisualPrefab = prefab;
                Debug.Log($"Automatically assigned goalVisualPrefab: {path}");
            }
        }
#endif

        if (walkableTilemap == null) return;

        if (mazeWidth % 2 == 0) mazeWidth++;
        if (mazeHeight % 2 == 0) mazeHeight++;

        if (walkableTile == null)
        {
            walkableTile = GetFirstAvailableTile();
        }

        walkableTilemap.ClearAllTiles();

        bool[,] maze = new bool[mazeWidth, mazeHeight];

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int start = new Vector2Int(1, 1);
        maze[start.x, start.y] = true;
        stack.Push(start);

        Vector2Int[] dirs = {
            new Vector2Int(0, 2),
            new Vector2Int(0, -2),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0)
        };

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> unvisitedNeighbors = new List<Vector2Int>();

            foreach (var d in dirs)
            {
                Vector2Int next = current + d;
                if (next.x > 0 && next.x < mazeWidth - 1 && next.y > 0 && next.y < mazeHeight - 1)
                {
                    if (!maze[next.x, next.y])
                    {
                        unvisitedNeighbors.Add(next);
                    }
                }
            }

            if (unvisitedNeighbors.Count > 0)
            {
                Vector2Int chosen = unvisitedNeighbors[UnityEngine.Random.Range(0, unvisitedNeighbors.Count)];
                Vector2Int wallBetween = current + (chosen - current) / 2;
                maze[wallBetween.x, wallBetween.y] = true;
                maze[chosen.x, chosen.y] = true;

                stack.Push(chosen);
            }
            else
            {
                stack.Pop();
            }
        }

        List<Vector2Int> brokenWalls = new List<Vector2Int>();
        for (int x = 1; x < mazeWidth - 1; x++)
        {
            for (int y = 1; y < mazeHeight - 1; y++)
            {
                // Chỉ phá tường ở các ô (chẵn, lẻ) hoặc (lẻ, chẵn) để tránh tạo thành ô 2x2 hoặc đường đi rộng 2 ô
                bool isWallCell = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);
                if (!isWallCell) continue;

                if (!maze[x, y])
                {
                    bool horizontalNeighbors = maze[x - 1, y] && maze[x + 1, y];
                    bool verticalNeighbors = maze[x, y - 1] && maze[x, y + 1];
                    if (horizontalNeighbors || verticalNeighbors)
                    {
                        if (UnityEngine.Random.value < 0.15f)
                        {
                            // Kiểm tra khoảng cách để tránh các vòng lặp quá gần nhau (ngăn tạo hình số 8)
                            bool tooClose = false;
                            foreach (var bw in brokenWalls)
                            {
                                int dist = Mathf.Abs(bw.x - x) + Mathf.Abs(bw.y - y);
                                if (dist < 4) // Khoảng cách tối thiểu giữa 2 lần phá tường là 4 ô
                                {
                                    tooClose = true;
                                    break;
                                }
                            }

                            if (!tooClose)
                            {
                                maze[x, y] = true;
                                brokenWalls.Add(new Vector2Int(x, y));
                            }
                        }
                    }
                }
            }
        }

        maze[1, 1] = true;

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                if (maze[x, y] && walkableTile != null)
                {
                    walkableTilemap.SetTile(new Vector3Int(x, y, 0), walkableTile);
                }
            }
        }

        AlignAndFitToBackground();

        // 1. Tính toán khoảng cách từ startCell (1, 1) đến tất cả các ô bằng BFS
        Vector3Int startCell = new Vector3Int(1, 1, 0);
        Dictionary<Vector3Int, int> startDistanceMap = new Dictionary<Vector3Int, int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(startCell);
        startDistanceMap[startCell] = 0;

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            int dist = startDistanceMap[cell];

            foreach (var dir in directions)
            {
                var neighbor = cell + dir;
                if (!walkableTilemap.HasTile(neighbor)) continue;
                if (startDistanceMap.ContainsKey(neighbor)) continue;

                startDistanceMap[neighbor] = dist + 1;
                queue.Enqueue(neighbor);
            }
        }

        // 2. Tìm tất cả các ô ngõ cụt (Dead-end terminal) trong mê cung
        List<Vector3Int> deadEndTerminals = new List<Vector3Int>();
        for (int x = 1; x < mazeWidth - 1; x++)
        {
            for (int y = 1; y < mazeHeight - 1; y++)
            {
                if (maze[x, y])
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    int neighborCount = 0;
                    foreach (var d in directions)
                    {
                        if (maze[x + d.x, y + d.y]) neighborCount++;
                    }
                    if (neighborCount <= 1 && cell != startCell)
                    {
                        deadEndTerminals.Add(cell);
                    }
                }
            }
        }

        // 3. Tìm ô ngõ cụt xa StartCell nhất
        Vector3Int bestGoalCell = new Vector3Int(mazeWidth - 2, mazeHeight - 2, 0); // Dự phòng
        int maxDist = -1;

        foreach (var cell in deadEndTerminals)
        {
            if (startDistanceMap.TryGetValue(cell, out int dist))
            {
                if (dist > maxDist)
                {
                    maxDist = dist;
                    bestGoalCell = cell;
                }
            }
        }

        // Nếu không có ngõ cụt nào, chọn ô xa nhất trong toàn bộ mê cung
        if (maxDist == -1)
        {
            foreach (var kvp in startDistanceMap)
            {
                if (kvp.Value > maxDist)
                {
                    maxDist = kvp.Value;
                    bestGoalCell = kvp.Key;
                }
            }
        }

        if (startPoint != null)
        {
            startPoint.position = walkableTilemap.GetCellCenterWorld(startCell);
        }
        if (goalPoint != null)
        {
            goalPoint.position = walkableTilemap.GetCellCenterWorld(bestGoalCell);

            // Xóa hình ảnh cũ của đích đến nếu có
            foreach (Transform child in goalPoint)
            {
                Destroy(child.gameObject);
            }

            // Tạo hình ảnh mới đại diện cho đích đến từ Prefab kéo thả
            if (goalVisualPrefab != null)
            {
                GameObject visual = Instantiate(goalVisualPrefab, goalPoint);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
            }
        }

        RebuildGoalDistanceMap();
    }

    void AlignAndFitToBackground()
    {
        if (walkableTilemap == null) return;

        if (backgroundRenderer == null)
        {
            var renderers = FindObjectsOfType<SpriteRenderer>();
            foreach (var r in renderers)
            {
                string nameLower = r.gameObject.name.ToLower();
                if (nameLower.Contains("background") || nameLower.Contains("bg") || nameLower.Contains("map"))
                {
                    backgroundRenderer = r;
                    break;
                }
            }
        }

        if (backgroundRenderer != null)
        {
            Bounds bgBounds = backgroundRenderer.bounds;
            float bgWidth = bgBounds.size.x;
            float bgHeight = bgBounds.size.y;
            Vector3 bgCenter = bgBounds.center;

            float cellX = walkableTilemap.layoutGrid != null ? walkableTilemap.layoutGrid.cellSize.x : cellSize;
            float cellY = walkableTilemap.layoutGrid != null ? walkableTilemap.layoutGrid.cellSize.y : cellSize;

            float mazeLocalWidth = mazeWidth * cellX;
            float mazeLocalHeight = mazeHeight * cellY;

            // Fit with customizable padding
            float scale = Mathf.Min((bgWidth * mazeFitPadding) / mazeLocalWidth, (bgHeight * mazeFitPadding) / mazeLocalHeight);

            walkableTilemap.transform.localScale = new Vector3(scale, scale, 1f);

            Vector3 localCenter = new Vector3(mazeLocalWidth / 2f, mazeLocalHeight / 2f, 0);
            Vector3 scaledCenter = Vector3.Scale(localCenter, walkableTilemap.transform.localScale);
            walkableTilemap.transform.position = bgCenter - scaledCenter;

            Debug.Log($"Fitted maze to background '{backgroundRenderer.gameObject.name}' with scale {scale}");
        }
    }

    TileBase GetFirstAvailableTile()
    {
        if (walkableTilemap == null) return null;
        
        var bounds = walkableTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                if (walkableTilemap.HasTile(pos))
                {
                    return walkableTilemap.GetTile(pos);
                }
            }
        }
        return null;
    }

    public bool IsDeadEndOption(Vector3Int option, Vector3Int currentCell)
    {
        return !CanReachGoalWithout(option, currentCell);
    }

    public bool CanReachGoalWithout(Vector3Int start, Vector3Int blockedCell)
    {
        if (walkableTilemap == null) return false;
        Vector3Int goalCell = GetGoalCell();
        if (start == goalCell) return true;

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        queue.Enqueue(start);
        visited.Add(start);
        visited.Add(blockedCell);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (cell == goalCell) return true;

            foreach (var dir in directions)
            {
                var neighbor = cell + dir;
                if (!walkableTilemap.HasTile(neighbor)) continue;
                if (visited.Contains(neighbor)) continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }

        return false;
    }

    public bool IsDeadEndTerminal(Vector3Int cell)
    {
        if (walkableTilemap == null) return false;
        if (cell == GetStartCell()) return false;
        if (cell == GetGoalCell()) return false;

        int neighborCount = 0;
        foreach (var dir in directions)
        {
            if (walkableTilemap.HasTile(cell + dir))
            {
                neighborCount++;
            }
        }
        return neighborCount <= 1;
    }

    T FindMarker<T>() where T : Component
    {
        var markers = FindObjectsOfType<T>();
        return markers != null && markers.Length > 0 ? markers[0] : null;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (goalVisualPrefab == null || !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(goalVisualPrefab))
        {
            string path = "Assets/Dung/Prefab/Goal.prefab";
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                goalVisualPrefab = prefab;
                Debug.Log($"Automatically assigned goalVisualPrefab from asset path: {path}");
            }
        }
    }
#endif
}

