using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public enum PlayerAnimalType
{
    Bo,
    Ho,
    Ran,
    Tho,
    Ga,
    Rong
}

public class GamePlay : MonoBehaviour
{
    public MazeNavigator navigator;
    public QuestionManager questionManager;
    public SimpleUI ui;
    public GameObject startScreen;
    public float speed = 2f;
    public float reachThreshold = 0.1f;

    [Header("Player Health Settings")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Enemy Settings")]
    public Enemy enemy;
    public float enemySpawnAfterMovementTime = 2f;
    private Coroutine enemySpawnCoroutine;
    private float basePlayerSpeed;
    private bool enemySpawnPending;
    private bool enemySpawnedThisLevel;
    private float playerMovementTime;

    [Header("Game Over Settings")]
    public GameObject winScreen;
    public GameObject loseScreen;

    [Header("Endless Level Settings")]
    public bool endlessMode = true;
    public int currentLevel = 1;
    public float enemySpeedIncreasePerLevel = 0.2f;
    public float levelQuestionTimeDecrease = 0.15f;
    public float baseQuestionTime = 8f;
    public float minQuestionTime = 2f;
    public float levelClearDelay = 0.6f;

    [Header("Score Settings")]
    public int currentScore;
    public int scoreFromPickup = 50;
    public int scoreFromLevel = 100;
    public int scorePerLevelIncrease = 100;
    public int scorePickupsPerMap = 2;
    public GameObject scorePickupPrefab;
    public float scorePickupSpawnOffset = 0.2f;

    [Header("Animal / Player Settings")]
    public GameObject boPlayer;
    public GameObject hoPlayer;
    public GameObject ranPlayer;
    public GameObject thoPlayer;
    public GameObject gaPlayer;
    public GameObject rongPlayer;
    public PlayerAnimalType currentAnimalType = PlayerAnimalType.Bo;
    public GameObject skillPickupPrefab;
    public int skillPickupsPerMap = 1;
    public float skillPickupSpawnOffset = 0.2f;
    public float activeSkillDuration = 6f;
    private bool hasActiveSkill;
    private float activeSkillTimer;

    [Header("UI Settings")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;

    [Header("Dead-End Trigger Settings")]
    public GameObject deadEndQuestionPrefab;
    public float deadEndTriggerOffset = 0.2f;
    public float deadEndTriggerRadius = 0.35f;

    private Animator animator;
    private string currentAnimState = "";
    private Vector3 originalLocalScale = Vector3.one;
    private float currentQuestionTimeLimit;
    private bool isTransitioningToNextLevel;

    Vector3Int currentCell;
    Vector3Int previousCell;
    readonly List<Vector3Int> moveQueue = new List<Vector3Int>();
    readonly List<Vector3Int> visitedCells = new List<Vector3Int>();

    int consecutiveWrong = 0;
    [HideInInspector]
    public bool waitingForAnswer = false;
    Tilemap walkableTilemap;

    void PlayAnim(string stateName)
    {
        if (animator != null && currentAnimState != stateName)
        {
            currentAnimState = stateName;
            
            bool isMoving = (stateName.ToLower() == "run");
            
            // Set the Animator parameter
            try
            {
                animator.SetBool("IsMove", isMoving);
            }
            catch (System.Exception) 
            {
                // Fallback if parameter doesn't exist
            }

            // Play the state directly as fallback (handling lowercase "run" state name)
            if (isMoving)
            {
                animator.Play("run");
            }
            else
            {
                animator.Play("Idle");
            }
        }
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        originalLocalScale = transform.localScale;
        basePlayerSpeed = speed;

        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        FindEnemyInScene();
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
        }

        SelectRandomAnimal();

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
            SpawnDeadEndQuestionTriggers();
            SpawnScorePickups();
            SpawnSkillPickups();
            currentCell = navigator.GetStartCell();
            if (walkableTilemap != null)
            {
                transform.position = walkableTilemap.GetCellCenterWorld(currentCell);
                transform.localScale = originalLocalScale;
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
            }
        }

        ApplyLevelSettings();
        UpdateUI();
    }

    void Update()
    {
        if ((startScreen != null && startScreen.activeSelf) || 
            (winScreen != null && winScreen.activeSelf) || 
            (loseScreen != null && loseScreen.activeSelf) || 
            waitingForAnswer || walkableTilemap == null)
        {
            PlayAnim("Idle");
            return;
        }

        if (hasActiveSkill)
        {
            activeSkillTimer -= Time.deltaTime;
            if (activeSkillTimer <= 0f)
            {
                hasActiveSkill = false;
                speed = basePlayerSpeed;
            }
        }

        if (moveQueue.Count > 0)
        {
            playerMovementTime += Time.deltaTime;
            if (enemySpawnPending && !enemySpawnedThisLevel && playerMovementTime >= enemySpawnAfterMovementTime)
            {
                SpawnEnemyNow();
            }
        }

        if (moveQueue.Count == 0 && Keyboard.current != null)
        {
            Vector3Int direction = Vector3Int.zero;
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
                direction = Vector3Int.up;
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
                direction = Vector3Int.down;
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                direction = Vector3Int.left;
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                direction = Vector3Int.right;

            if (direction != Vector3Int.zero)
            {
                Vector3Int targetCell = currentCell + direction;
                if (walkableTilemap.HasTile(targetCell))
                {
                    previousCell = currentCell;
                    SetQueueToSingleCell(targetCell);
                }
            }
        }

        CheckDeadEndTriggersProximity();

        if (moveQueue.Count == 0)
        {
            PlayAnim("Idle");
            return;
        }

        PlayAnim("Run");

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
            return;
        }

        if (cell == navigator.GetGoalCell())
        {
            AdvanceToNextLevel();
            return;
        }

        var options = navigator.GetNeighbors(cell, previousCell);
        if (options.Count == 0)
        {
            Debug.Log($"Dead end at cell {cell}");
            return;
        }

        // If it's a corridor/turn (only 1 option), auto-move forward.
        // If it's a junction (more than 1 option), we stop and wait for player's WASD input.
        if (options.Count == 1)
        {
            SetQueueToSingleCell(options[0]);
        }
    }



    void SetQueueToSingleCell(Vector3Int cell)
    {
        moveQueue.Clear();
        moveQueue.Add(cell);
    }

    public void StopMovement()
    {
        if (walkableTilemap != null)
        {
            Vector3Int cellPosition = walkableTilemap.WorldToCell(transform.position);
            transform.position = walkableTilemap.GetCellCenterWorld(cellPosition);
            currentCell = cellPosition;
            previousCell = cellPosition;
        }
        moveQueue.Clear();
    }

    void SpawnDeadEndQuestionTriggers()
    {
        if (navigator == null || walkableTilemap == null) return;

        Transform triggerRoot = navigator.transform.Find("DeadEndTriggers");
        if (triggerRoot == null)
        {
            GameObject root = new GameObject("DeadEndTriggers");
            root.transform.SetParent(navigator.transform, false);
            triggerRoot = root.transform;
        }

        for (int i = triggerRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = triggerRoot.GetChild(i);
            if (child.name.Contains("DeadEndTrigger"))
            {
                Destroy(child.gameObject);
            }
        }

        foreach (var cell in navigator.GetAllDeadEndCells())
        {
            Vector3 worldPos = walkableTilemap.GetCellCenterWorld(cell);
            worldPos += Vector3.back * deadEndTriggerOffset;

            GameObject trigger;
            if (deadEndQuestionPrefab != null)
            {
                trigger = Instantiate(deadEndQuestionPrefab, worldPos, Quaternion.identity, triggerRoot);
            }
            else
            {
                trigger = new GameObject("DeadEndTrigger_" + cell.x + "_" + cell.y);
                trigger.transform.SetParent(triggerRoot, false);
                trigger.transform.position = worldPos;

                var renderer = trigger.AddComponent<SpriteRenderer>();
                var texture = Texture2D.whiteTexture;
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                renderer.sprite = sprite;
                renderer.color = Color.yellow;
                renderer.transform.localScale = Vector3.one * 0.25f;

                var collider = trigger.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.2f;
            }

            trigger.name = "DeadEndTrigger_" + cell.x + "_" + cell.y;
            trigger.transform.position = worldPos;
            var triggerComp = trigger.GetComponent<DeadEndQuestionTrigger>();
            if (triggerComp == null)
            {
                triggerComp = trigger.AddComponent<DeadEndQuestionTrigger>();
            }
            triggerComp.Initialize(this);
        }
    }

    void SpawnScorePickups()
    {
        if (navigator == null || walkableTilemap == null) return;

        Transform pickupRoot = navigator.transform.Find("ScorePickups");
        if (pickupRoot == null)
        {
            GameObject root = new GameObject("ScorePickups");
            root.transform.SetParent(navigator.transform, false);
            pickupRoot = root.transform;
        }

        for (int i = pickupRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(pickupRoot.GetChild(i).gameObject);
        }

        if (scorePickupPrefab == null) return;

        List<Vector3Int> candidateCells = new List<Vector3Int>();
        var bounds = walkableTilemap.cellBounds;
        Vector3Int startCell = navigator.GetStartCell();
        Vector3Int goalCell = navigator.GetGoalCell();

        for (int x = bounds.xMin; x <= bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!walkableTilemap.HasTile(cell)) continue;
                if (cell == startCell || cell == goalCell) continue;
                candidateCells.Add(cell);
            }
        }

        if (candidateCells.Count == 0) return;

        int spawnCount = Mathf.Min(Mathf.Max(1, scorePickupsPerMap), candidateCells.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            int index = UnityEngine.Random.Range(0, candidateCells.Count);
            Vector3Int cell = candidateCells[index];
            candidateCells.RemoveAt(index);

            Vector3 worldPos = walkableTilemap.GetCellCenterWorld(cell);
            worldPos += Vector3.back * scorePickupSpawnOffset;
            GameObject pickup = Instantiate(scorePickupPrefab, worldPos, Quaternion.identity, pickupRoot);
            pickup.name = "ScorePickup_" + cell.x + "_" + cell.y;

            var collider = pickup.GetComponent<Collider2D>();
            if (collider == null)
            {
                var circle = pickup.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.2f;
            }

            var rb = pickup.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = pickup.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;
            }

            var pickupComp = pickup.GetComponent<ScorePickup>();
            if (pickupComp == null)
            {
                pickupComp = pickup.AddComponent<ScorePickup>();
            }
            pickupComp.Initialize(this);
            pickupComp.scoreValue = scoreFromPickup;
        }
    }

    void SpawnSkillPickups()
    {
        if (navigator == null || walkableTilemap == null) return;

        Transform pickupRoot = navigator.transform.Find("SkillPickups");
        if (pickupRoot == null)
        {
            GameObject root = new GameObject("SkillPickups");
            root.transform.SetParent(navigator.transform, false);
            pickupRoot = root.transform;
        }

        for (int i = pickupRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(pickupRoot.GetChild(i).gameObject);
        }

        if (skillPickupPrefab == null) return;

        List<Vector3Int> candidateCells = new List<Vector3Int>();
        var bounds = walkableTilemap.cellBounds;
        Vector3Int startCell = navigator.GetStartCell();
        Vector3Int goalCell = navigator.GetGoalCell();

        for (int x = bounds.xMin; x <= bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!walkableTilemap.HasTile(cell)) continue;
                if (cell == startCell || cell == goalCell) continue;
                candidateCells.Add(cell);
            }
        }

        if (candidateCells.Count == 0) return;

        int spawnCount = Mathf.Min(Mathf.Max(1, skillPickupsPerMap), candidateCells.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            int index = UnityEngine.Random.Range(0, candidateCells.Count);
            Vector3Int cell = candidateCells[index];
            candidateCells.RemoveAt(index);

            Vector3 worldPos = walkableTilemap.GetCellCenterWorld(cell);
            worldPos += Vector3.back * skillPickupSpawnOffset;
            GameObject pickup = Instantiate(skillPickupPrefab, worldPos, Quaternion.identity, pickupRoot);
            pickup.name = "SkillPickup_" + cell.x + "_" + cell.y;

            var collider = pickup.GetComponent<Collider2D>();
            if (collider == null)
            {
                var circle = pickup.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 0.2f;
            }

            var rb = pickup.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = pickup.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;
            }

            var pickupComp = pickup.GetComponent<AnimalSkillPickup>();
            if (pickupComp == null)
            {
                pickupComp = pickup.AddComponent<AnimalSkillPickup>();
            }
            pickupComp.Initialize(this);
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateUI();
        Debug.Log($"Score: {currentScore}");
    }

    public void GrantSkill()
    {
        hasActiveSkill = true;
        activeSkillTimer = activeSkillDuration;

        switch (currentAnimalType)
        {
            case PlayerAnimalType.Bo:
                if (enemy != null)
                {
                    enemy.Stun(2f);
                }
                break;
            case PlayerAnimalType.Ho:
                if (enemy != null)
                {
                    enemy.ResetToStart();
                }
                break;
            case PlayerAnimalType.Ran:
                if (enemy != null)
                {
                    enemy.ApplySlow(0.6f, 5f);
                }
                break;
            case PlayerAnimalType.Tho:
                ConvertNearestDeadEnd();
                break;
            case PlayerAnimalType.Ga:
                speed = basePlayerSpeed * 1.5f;
                break;
            case PlayerAnimalType.Rong:
                if (enemy != null)
                {
                    enemy.FearFromPlayer(5f);
                }
                break;
        }
    }

    void ConvertNearestDeadEnd()
    {
        if (navigator == null) return;

        Transform triggerRoot = navigator.transform.Find("DeadEndTriggers");
        if (triggerRoot == null) return;

        Transform closest = null;
        float closestDistance = float.MaxValue;
        foreach (Transform child in triggerRoot)
        {
            if (!child.name.Contains("DeadEndTrigger")) continue;
            float dist = Vector3.Distance(transform.position, child.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = child;
            }
        }

        if (closest != null && closestDistance <= 1.2f)
        {
            Destroy(closest.gameObject);
        }
    }

    void CheckDeadEndTriggersProximity()
    {
        if (waitingForAnswer) return;

        Transform triggerRoot = navigator != null ? navigator.transform.Find("DeadEndTriggers") : null;
        if (triggerRoot == null) return;

        foreach (Transform child in triggerRoot)
        {
            if (!child.name.Contains("DeadEndTrigger")) continue;
            var triggerComp = child.GetComponent<DeadEndQuestionTrigger>();
            if (triggerComp == null) continue;

            if (Vector3.Distance(transform.position, child.position) <= deadEndTriggerRadius)
            {
                triggerComp.TriggerQuestion();
            }
        }
    }

    void ResetEnemySpawnState()
    {
        enemySpawnPending = true;
        enemySpawnedThisLevel = false;
        playerMovementTime = 0f;

        FindEnemyInScene();
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
        }
    }

    void SpawnEnemyNow()
    {
        FindEnemyInScene();
        if (enemy != null && navigator != null && walkableTilemap != null)
        {
            enemy.gameObject.SetActive(true);
            enemy.Spawn(navigator.GetStartCell(), this);
            enemySpawnedThisLevel = true;
            enemySpawnPending = false;
        }
    }

    GameObject GetAnimalModel(PlayerAnimalType type)
    {
        switch (type)
        {
            case PlayerAnimalType.Bo: return boPlayer;
            case PlayerAnimalType.Ho: return hoPlayer;
            case PlayerAnimalType.Ran: return ranPlayer;
            case PlayerAnimalType.Tho: return thoPlayer;
            case PlayerAnimalType.Ga: return gaPlayer;
            case PlayerAnimalType.Rong: return rongPlayer;
            default: return boPlayer;
        }
    }

    string GetAnimalName(PlayerAnimalType type)
    {
        switch (type)
        {
            case PlayerAnimalType.Bo: return "Bò";
            case PlayerAnimalType.Ho: return "Hổ";
            case PlayerAnimalType.Ran: return "Rắn";
            case PlayerAnimalType.Tho: return "Thỏ";
            case PlayerAnimalType.Ga: return "Gà";
            case PlayerAnimalType.Rong: return "Rồng";
            default: return "Không rõ";
        }
    }

    void SelectRandomAnimal()
    {
        currentAnimalType = (PlayerAnimalType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(PlayerAnimalType)).Length);
        if (navigator != null)
        {
            var model = GetAnimalModel(currentAnimalType);
            if (model != null)
            {
                navigator.goalVisualPrefab = model;
            }
        }
        Debug.Log($"Selected animal: {GetAnimalName(currentAnimalType)}");
    }

    void ApplyLevelSettings()
    {
        if (!endlessMode) return;

        currentQuestionTimeLimit = Mathf.Max(minQuestionTime, baseQuestionTime - (currentLevel - 1) * levelQuestionTimeDecrease);

        if (questionManager != null)
        {
            questionManager.questionTimeLimit = currentQuestionTimeLimit;
        }

        if (enemy != null)
        {
            enemy.speed = Mathf.Max(0.8f, 1.2f + (currentLevel - 1) * enemySpeedIncreasePerLevel);
        }

        UpdateUI();
        float enemySpeed = enemy != null ? enemy.speed : 0f;
        Debug.Log($"Level {currentLevel}: enemySpeed={enemySpeed}, questionTime={currentQuestionTimeLimit}");
    }

    void AdvanceToNextLevel()
    {
        if (!endlessMode || isTransitioningToNextLevel) return;

        isTransitioningToNextLevel = true;
        ResetEnemySpawnState();
        int levelScore = scoreFromLevel + (currentLevel - 1) * scorePerLevelIncrease;
        AddScore(levelScore);
        currentLevel++;
        ApplyLevelSettings();

        Invoke(nameof(GenerateNextLevel), levelClearDelay);
    }

    void GenerateNextLevel()
    {
        SelectRandomAnimal();

        if (navigator != null)
        {
            navigator.GenerateMaze();
            walkableTilemap = navigator.walkableTilemap;
            SpawnDeadEndQuestionTriggers();
            SpawnScorePickups();
            SpawnSkillPickups();
            currentCell = navigator.GetStartCell();
            if (walkableTilemap != null)
            {
                transform.position = walkableTilemap.GetCellCenterWorld(currentCell);
                previousCell = currentCell;
                visitedCells.Clear();
                moveQueue.Clear();
                visitedCells.Add(currentCell);
            }
        }

        isTransitioningToNextLevel = false;
        waitingForAnswer = false;
    }

    public void StartGame()
    {
        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        currentHealth = maxHealth;
        currentScore = 0;
        waitingForAnswer = false;
        consecutiveWrong = 0;
        currentLevel = 1;
        hasActiveSkill = false;
        activeSkillTimer = 0f;
        speed = basePlayerSpeed;
        UpdateUI();
        isTransitioningToNextLevel = false;

        if (enemySpawnCoroutine != null) StopCoroutine(enemySpawnCoroutine);
        ResetEnemySpawnState();

        SelectRandomAnimal();

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
            SpawnDeadEndQuestionTriggers();
            SpawnScorePickups();
            SpawnSkillPickups();
            currentCell = navigator.GetStartCell();
            if (walkableTilemap != null)
            {
                transform.position = walkableTilemap.GetCellCenterWorld(currentCell);
                transform.localScale = originalLocalScale;
                if (navigator.startPoint != null) navigator.startPoint.localScale = walkableTilemap.transform.localScale;
                if (navigator.goalPoint != null) navigator.goalPoint.localScale = walkableTilemap.transform.localScale;
                previousCell = currentCell;
                visitedCells.Clear();
                moveQueue.Clear();
                visitedCells.Add(currentCell);

                Debug.Log("Game started/restarted via button");
            }
        }
    }

    System.Collections.IEnumerator SpawnEnemyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnEnemyNow();
    }

    void FindEnemyInScene()
    {
        if (enemy == null)
        {
            Enemy[] allEnemies = Resources.FindObjectsOfTypeAll<Enemy>();
            foreach (var e in allEnemies)
            {
                if (e.gameObject.scene.name != null)
                {
                    enemy = e;
                    break;
                }
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"Player took damage! Health: {currentHealth}/{maxHealth}");
        if (currentHealth <= 0)
        {
            LoseGame();
        }
    }

    public void WinGame()
    {
        if (enemySpawnCoroutine != null) StopCoroutine(enemySpawnCoroutine);
        FindEnemyInScene();
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
        }
        StopMovement();

        if (winScreen != null)
        {
            winScreen.SetActive(true);
        }
        Debug.Log("Game Won!");
    }

    public void LoseGame()
    {
        if (enemySpawnCoroutine != null) StopCoroutine(enemySpawnCoroutine);
        FindEnemyInScene();
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
        }
        StopMovement();

        if (loseScreen != null)
        {
            loseScreen.SetActive(true);
        }
        endlessMode = false;
        Debug.Log("Game Lost!");
    }

    void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Điểm: {currentScore}";
        }

        if (levelText != null)
        {
            levelText.text = $"Lv: {currentLevel}";
        }
    }

    void OnGUI()
    {
        if (startScreen != null && startScreen.activeSelf) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.red;

        string hpText = "Máu: ";
        for (int i = 0; i < maxHealth; i++)
        {
            if (i < currentHealth)
                hpText += "❤️";
            else
                hpText += "🖤";
        }

        GUI.Label(new Rect(20, 20, 300, 40), hpText, style);

        if (endlessMode)
        {
            GUI.Label(new Rect(20, 60, 300, 40), $"Level: {currentLevel}", style);
            GUI.Label(new Rect(20, 95, 400, 40), $"Điểm: {currentScore}", style);
            GUI.Label(new Rect(20, 130, 400, 40), $"Thời gian câu hỏi: {currentQuestionTimeLimit:F1}s", style);
        }
    }

    void ResetToStart()
    {
        currentHealth = maxHealth;
        if (enemySpawnCoroutine != null) StopCoroutine(enemySpawnCoroutine);
        FindEnemyInScene();
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
        }

        Vector3Int startCell = navigator.GetStartCell();
        transform.position = walkableTilemap.GetCellCenterWorld(startCell);
        transform.localScale = originalLocalScale;
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
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
        Debug.Log("Reset to start after dead end or death");
    }
}
