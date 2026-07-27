using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    // SimpleUI reference removed to avoid compile-time dependency; UI interaction uses QuestionManager/SimpleUI at runtime.
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
    public GameObject boPlayerAnimation;
    public GameObject hoPlayerAnimation;
    public GameObject ranPlayerAnimation;
    public GameObject thoPlayerAnimation;
    public GameObject gaPlayerAnimation;
    public GameObject rongPlayerAnimation;
    public Sprite boSprite;
    public Sprite hoSprite;
    public Sprite ranSprite;
    public Sprite thoSprite;
    public Sprite gaSprite;
    public Sprite rongSprite;
    public Transform playerVisualRoot;
    public SpriteRenderer playerSpriteRenderer;
    public PlayerAnimalType currentAnimalType = PlayerAnimalType.Bo;
    public PlayerAnimalType currentGoalAnimalType = PlayerAnimalType.Ho;
    private GameObject activePlayerVisual;
    public Transform goalVisualRoot;
    public SpriteRenderer goalSpriteRenderer;
    public GameObject skillPickupPrefab;
    public int skillPickupsPerMap = 1;
    public float skillPickupSpawnOffset = 0.2f;
    public float skillSpawnInterval = 8f;
    public float initialSkillDelay = 5f;
    private float skillSpawnTimer;
    public float activeSkillDuration = 6f;
    private bool hasActiveSkill;
    private float activeSkillTimer;

    [Header("Animal Score Pickup Effects")]
    public float snakeScoreSlowDuration = 4f;
    public float rabbitDeadEndClearRadius = 1.5f;
    public float dragonFearDuration = 5f;
    private bool snakeSlowActive;
    private float snakeSlowTimer;
    private Vector3 playerVisualOriginalScale;
    private bool playerFacingRight = true;

    [Header("UI Settings")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI healthText;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("Stat Panel Settings")]
    public Vector2 levelPanelPosition = new Vector2(-680f, 455f);
    public Vector2 levelPanelSize = new Vector2(220f, 70f);
    public Vector3 levelPanelScale = Vector3.one;
    public Vector2 timePanelPosition = new Vector2(-220f, 455f);
    public Vector2 timePanelSize = new Vector2(220f, 70f);
    public Vector3 timePanelScale = Vector3.one;
    public Vector2 healthPanelPosition = new Vector2(220f, 455f);
    public Vector2 healthPanelSize = new Vector2(220f, 70f);
    public Vector3 healthPanelScale = Vector3.one;
    public Vector2 scorePanelPosition = new Vector2(680f, 455f);
    public Vector2 scorePanelSize = new Vector2(220f, 70f);
    public Vector3 scorePanelScale = Vector3.one;
    [Header("Screen Half Positioning")]
    public bool useHalfScreenPositions = false;
    public float screenHalfOffsetX = 0f;
    public float screenHalfOffsetY = 0f;
    public Vector3 levelTextScale = Vector3.one;
    public Vector3 timeTextScale = Vector3.one;
    public Vector3 healthTextScale = Vector3.one;
    public Vector3 scoreTextScale = Vector3.one;
    public Vector2 levelTextPosition = new Vector2(0f, 2f);
    public Vector2 timeTextPosition = new Vector2(0f, 2f);
    public Vector2 healthTextPosition = new Vector2(0f, 2f);
    public Vector2 scoreTextPosition = new Vector2(0f, 2f);
    [Header("Heart Icon Settings")]
    public Vector3 heartScale = Vector3.one;
    public float heartSize = 40f;
    public float heartSpacing = 10f;
    public Color statPanelColor = new Color(1f, 1f, 1f, 0.85f);
    public Sprite levelPanelSprite;
    public Sprite timePanelSprite;
    public Sprite healthPanelSprite;
    public Sprite scorePanelSprite;
    public bool showLegacyDebugHud = false;

    [Header("Dead-End Trigger Settings")]
    public GameObject deadEndQuestionPrefab;
    public float deadEndTriggerOffset = 0.2f;
    public float deadEndTriggerRadius = 0.35f;

    private Animator animator;
    private string currentAnimState = "";
    private Image levelPanelImage;
    private Image timePanelImage;
    private Image healthPanelImage;
    private Image scorePanelImage;
    private Transform healthDisplayContainer;
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

        playerVisualOriginalScale = playerVisualRoot != null ? playerVisualRoot.localScale : transform.localScale;
        InitializeAnimalCycle();

        if (navigator != null)
        {
            if (navigator.generateRandomMaze)
            {
                navigator.GenerateMaze();
                UpdateGoalVisual();
            }
            else
            {
                navigator.RefreshNodes();
                UpdateGoalVisual();
            }
            walkableTilemap = navigator.walkableTilemap;
            SpawnDeadEndQuestionTriggers();
            SpawnScorePickups();
            ResetSkillSpawnTimer(true);
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

        SetupStatPanels();
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

        UpdatePlayerEffects();
        UpdateSkillSpawnTimer();

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
                    UpdateFlip(direction);
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

        Vector3 movementDelta = targetWorld - transform.position;
        if (Mathf.Abs(movementDelta.x) > Mathf.Abs(movementDelta.y))
        {
            UpdateFlip(movementDelta);
        }

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

    void UpdateFlip(Vector3Int direction)
    {
        if (direction == Vector3Int.left)
        {
            FlipPlayer(false);
        }
        else if (direction == Vector3Int.right)
        {
            FlipPlayer(true);
        }
    }

    void UpdateFlip(Vector3 direction)
    {
        if (direction.x < 0f)
        {
            FlipPlayer(false);
        }
        else if (direction.x > 0f)
        {
            FlipPlayer(true);
        }
    }

    void FlipPlayer(bool faceRight)
    {
        if (playerVisualRoot == null)
            playerVisualRoot = transform;

        if (playerFacingRight == faceRight)
            return;

        playerFacingRight = faceRight;

        Vector3 scale = playerVisualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * (faceRight ? 1f : -1f);
        playerVisualRoot.localScale = scale;
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

        GameObject pickup = null;

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

        int spawnCount = 1;
        for (int i = 0; i < spawnCount; i++)
        {
            int index = UnityEngine.Random.Range(0, candidateCells.Count);
            Vector3Int cell = candidateCells[index];
            candidateCells.RemoveAt(index);

            Vector3 worldPos = walkableTilemap.GetCellCenterWorld(cell);
            worldPos += Vector3.back * skillPickupSpawnOffset;

            if (skillPickupPrefab != null)
            {
                pickup = Instantiate(skillPickupPrefab, worldPos, Quaternion.identity, pickupRoot);
            }
            else
            {
                pickup = CreateFallbackSkillPickup(worldPos, pickupRoot);
            }

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

    GameObject CreateFallbackSkillPickup(Vector3 worldPos, Transform pickupRoot)
    {
        GameObject pickup = new GameObject("SkillPickup_Fallback");
        pickup.transform.SetParent(pickupRoot, false);
        pickup.transform.position = worldPos;

        SpriteRenderer renderer = pickup.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateFallbackPickupSprite();
        renderer.color = Color.yellow;
        renderer.sortingOrder = 20;

        CircleCollider2D circleCollider = pickup.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.2f;

        Rigidbody2D rb = pickup.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        AnimalSkillPickup pickupComp = pickup.AddComponent<AnimalSkillPickup>();
        pickupComp.Initialize(this);

        return pickup;
    }

    Sprite CreateFallbackPickupSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dx = x - 32f;
                float dy = y - 32f;
                float distance = dx * dx + dy * dy;

                if (distance <= 24f * 24f)
                {
                    pixels[y * 64 + x] = Color.yellow;
                }
                else if (distance <= 32f * 32f)
                {
                    pixels[y * 64 + x] = new Color(1f, 0.9f, 0.2f, 0.5f);
                }
                else
                {
                    pixels[y * 64 + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateUI();
        Debug.Log($"Score: {currentScore}");
    }

    void ActivateDragonFear()
    {
        if (enemy != null)
        {
            enemy.FearFromPlayer(dragonFearDuration);
            Debug.Log("Dragon skill effect: enemy frightened and runs away.");
        }
    }

    void ClearNearbyDeadEnds()
    {
        if (navigator == null) return;

        Transform triggerRoot = navigator.transform.Find("DeadEndTriggers");
        if (triggerRoot == null) return;

        var children = new List<Transform>();
        foreach (Transform child in triggerRoot)
        {
            if (child.name.Contains("DeadEndTrigger"))
                children.Add(child);
        }

        foreach (Transform child in children)
        {
            if (Vector3.Distance(transform.position, child.position) <= rabbitDeadEndClearRadius)
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log("Rabbit score effect: nearby dead-end triggers removed.");
    }

    void UpdatePlayerEffects()
    {
        if (hasActiveSkill)
        {
            activeSkillTimer -= Time.deltaTime;
            if (activeSkillTimer <= 0f)
            {
                hasActiveSkill = false;
            }
        }

        if (hasActiveSkill && currentAnimalType == PlayerAnimalType.Ga)
        {
            speed = basePlayerSpeed * 1.5f;
        }
        else if (!hasActiveSkill)
        {
            speed = basePlayerSpeed;
        }
    }

    void ClearSkillPickups()
    {
        if (navigator == null) return;

        Transform pickupRoot = navigator.transform.Find("SkillPickups");
        if (pickupRoot == null) return;

        for (int i = pickupRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(pickupRoot.GetChild(i).gameObject);
        }
    }

    void ResetSkillSpawnTimer(bool initialDelay = false)
    {
        if (initialDelay)
        {
            ClearSkillPickups();
        }

        skillSpawnTimer = initialDelay ? initialSkillDelay : skillSpawnInterval;
    }

    void UpdateSkillSpawnTimer()
    {
        if (skillPickupPrefab == null || navigator == null || walkableTilemap == null)
            return;

        skillSpawnTimer -= Time.deltaTime;
        if (skillSpawnTimer > 0f)
            return;

        Transform pickupRoot = navigator.transform.Find("SkillPickups");
        bool hasPickup = pickupRoot != null && pickupRoot.childCount > 0;

        if (!hasPickup)
        {
            SpawnSkillPickups();
        }

        ResetSkillSpawnTimer(false);
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
                    enemy.ApplyShrinkAndSlow(0.5f, activeSkillDuration);
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

        ResetSkillSpawnTimer(false);
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

        if (closest != null)
        {
            Destroy(closest.gameObject);
            Debug.Log("Rabbit skill: removed closest dead-end trigger.");
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
            case PlayerAnimalType.Bo: return boPlayerAnimation != null ? boPlayerAnimation : boPlayer;
            case PlayerAnimalType.Ho: return hoPlayerAnimation != null ? hoPlayerAnimation : hoPlayer;
            case PlayerAnimalType.Ran: return ranPlayerAnimation != null ? ranPlayerAnimation : ranPlayer;
            case PlayerAnimalType.Tho: return thoPlayerAnimation != null ? thoPlayerAnimation : thoPlayer;
            case PlayerAnimalType.Ga: return gaPlayerAnimation != null ? gaPlayerAnimation : gaPlayer;
            case PlayerAnimalType.Rong: return rongPlayerAnimation != null ? rongPlayerAnimation : rongPlayer;
            default: return boPlayerAnimation != null ? boPlayerAnimation : boPlayer;
        }
    }

    GameObject GetAnimalAnimationPrefab(PlayerAnimalType type)
    {
        switch (type)
        {
            case PlayerAnimalType.Bo: return boPlayerAnimation;
            case PlayerAnimalType.Ho: return hoPlayerAnimation;
            case PlayerAnimalType.Ran: return ranPlayerAnimation;
            case PlayerAnimalType.Tho: return thoPlayerAnimation;
            case PlayerAnimalType.Ga: return gaPlayerAnimation;
            case PlayerAnimalType.Rong: return rongPlayerAnimation;
            default: return null;
        }
    }

    PlayerAnimalType GetRandomAnimal()
    {
        var values = Enum.GetValues(typeof(PlayerAnimalType));
        return (PlayerAnimalType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }

    PlayerAnimalType GetRandomAnimalExcept(PlayerAnimalType excluded)
    {
        var values = new List<PlayerAnimalType>((PlayerAnimalType[])Enum.GetValues(typeof(PlayerAnimalType)));
        values.Remove(excluded);
        if (values.Count == 0) return excluded;
        return values[UnityEngine.Random.Range(0, values.Count)];
    }

    void UpdateActivePlayerModel()
    {
        UpdatePlayerVisual();
    }

    void UpdatePlayerVisual()
    {
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.sprite = GetAnimalSprite(currentAnimalType);
        }

        if (playerVisualRoot == null)
            playerVisualRoot = transform;

        GameObject selectedModel = GetAnimalModel(currentAnimalType);
        GameObject animationPrefab = GetAnimalAnimationPrefab(currentAnimalType);

        // Deactivate any scene player models that are not the selected scene model.
        var animalModels = new GameObject[] { boPlayer, hoPlayer, ranPlayer, thoPlayer, gaPlayer, rongPlayer, boPlayerAnimation, hoPlayerAnimation, ranPlayerAnimation, thoPlayerAnimation, gaPlayerAnimation, rongPlayerAnimation };
        foreach (var model in animalModels)
        {
            if (model == null) continue;
            if (model.scene.IsValid())
            {
                model.SetActive(model == selectedModel);
            }
        }

        // Clear any previously spawned clone visuals under the root.
        for (int i = playerVisualRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(playerVisualRoot.GetChild(i).gameObject);
        }
        activePlayerVisual = null;

        if (selectedModel == null)
            return;

        if (animationPrefab != null)
        {
            activePlayerVisual = Instantiate(animationPrefab, playerVisualRoot);
            activePlayerVisual.transform.localPosition = Vector3.zero;
            activePlayerVisual.transform.localRotation = Quaternion.identity;
            activePlayerVisual.transform.localScale = Vector3.one;
            if (selectedModel != null && selectedModel.scene.IsValid())
            {
                selectedModel.SetActive(false);
            }
        }
        else if (!selectedModel.scene.IsValid())
        {
            activePlayerVisual = Instantiate(selectedModel, playerVisualRoot);
            activePlayerVisual.transform.localPosition = Vector3.zero;
            activePlayerVisual.transform.localRotation = Quaternion.identity;
            activePlayerVisual.transform.localScale = Vector3.one;
        }
        else
        {
            activePlayerVisual = selectedModel;
        }

        PlayVisualAnimation(activePlayerVisual);

        Animator foundAnimator = activePlayerVisual != null ? activePlayerVisual.GetComponentInChildren<Animator>(true) : null;
        Animation foundLegacy = activePlayerVisual != null ? activePlayerVisual.GetComponentInChildren<Animation>(true) : null;

        if (foundAnimator != null)
        {
            foundAnimator.enabled = true;
            if (foundAnimator.runtimeAnimatorController != null)
            {
                try { foundAnimator.Play("Idle", 0, 0f); } catch { }
            }
        }
        if (foundLegacy != null && foundLegacy.clip != null)
        {
            try { foundLegacy.Play(); } catch { }
        }

        if (playerSpriteRenderer != null)
        {
            bool visualIsChildOfRoot = playerVisualRoot != null && activePlayerVisual != null && activePlayerVisual.transform.IsChildOf(playerVisualRoot);
            bool visualHasAnimator = foundAnimator != null && foundAnimator.runtimeAnimatorController != null;
            try { playerSpriteRenderer.enabled = !(visualHasAnimator && visualIsChildOfRoot); } catch { }
        }

        if ((foundAnimator == null || foundAnimator.runtimeAnimatorController == null) && (foundLegacy == null || foundLegacy.clip == null))
        {
            Debug.LogWarning($"Player visual '{(activePlayerVisual != null ? activePlayerVisual.name : "null")}' for {currentAnimalType} has no Animator or Animation clip.");
        }
    }

    void UpdateGoalVisual()
    {
        if (navigator == null) return;

        GameObject goalModel = GetAnimalModel(currentGoalAnimalType);
        navigator.goalVisualPrefab = goalModel;

        if (goalSpriteRenderer != null)
        {
            goalSpriteRenderer.sprite = GetAnimalSprite(currentGoalAnimalType);
        }

        if (goalVisualRoot == null && navigator.goalPoint != null)
            goalVisualRoot = navigator.goalPoint;

        if (goalVisualRoot != null && goalModel != null)
        {
            foreach (Transform child in goalVisualRoot)
            {
                Destroy(child.gameObject);
            }

            GameObject visual = Instantiate(goalModel, goalVisualRoot);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            PlayVisualAnimation(visual);
        }
    }

    void PlayVisualAnimation(GameObject visual)
    {
        if (visual == null) return;

        visual.SetActive(true);

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.enabled = true;
            if (animator.runtimeAnimatorController != null)
            {
                try
                {
                    animator.Play("Idle", 0, 0f);
                }
                catch (System.Exception)
                {
                    animator.Play(0, -1, 0f);
                }
            }
        }

        Animation legacyAnimation = visual.GetComponentInChildren<Animation>(true);
        if (legacyAnimation != null && legacyAnimation.clip != null)
        {
            legacyAnimation.Play();
        }
    }

    void InitializeAnimalCycle()
    {
        currentAnimalType = GetRandomAnimal();
        currentGoalAnimalType = GetRandomAnimalExcept(currentAnimalType);
        UpdateActivePlayerModel();
    }

    void SelectRandomGoalAnimal(PlayerAnimalType exclude)
    {
        currentGoalAnimalType = GetRandomAnimalExcept(exclude);
    }

    Sprite GetAnimalSprite(PlayerAnimalType type)
    {
        switch (type)
        {
            case PlayerAnimalType.Bo: return boSprite;
            case PlayerAnimalType.Ho: return hoSprite;
            case PlayerAnimalType.Ran: return ranSprite;
            case PlayerAnimalType.Tho: return thoSprite;
            case PlayerAnimalType.Ga: return gaSprite;
            case PlayerAnimalType.Rong: return rongSprite;
            default: return null;
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
        currentAnimalType = currentGoalAnimalType;
        UpdateActivePlayerModel();
        SelectRandomGoalAnimal(currentAnimalType);
        UpdateGoalVisual();

        if (navigator != null)
        {
            navigator.GenerateMaze();
            walkableTilemap = navigator.walkableTilemap;
            SpawnDeadEndQuestionTriggers();
            SpawnScorePickups();
            ResetSkillSpawnTimer(true);
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

        if (playerVisualRoot == null)
            playerVisualRoot = transform;

        if (playerVisualRoot == null)
            playerVisualRoot = transform;

        InitializeAnimalCycle();

        if (navigator != null)
        {
            if (goalVisualRoot == null && navigator.goalPoint != null)
                goalVisualRoot = navigator.goalPoint;

            if (navigator.generateRandomMaze)
            {
                navigator.GenerateMaze();
                UpdateGoalVisual();
            }
            else
            {
                navigator.RefreshNodes();
                UpdateGoalVisual();
            }
            walkableTilemap = navigator.walkableTilemap;
            SpawnDeadEndQuestionTriggers();
            SpawnScorePickups();
            ResetSkillSpawnTimer(true);
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
        currentHealth = Mathf.Max(0, currentHealth);
        UpdateUI();
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

    void SetupStatPanels()
    {
        Canvas uiCanvas = FindAnyObjectByType<Canvas>();
        if (uiCanvas == null)
        {
            Debug.LogError("SetupStatPanels: No Canvas found in scene!");
            return;
        }

        // Ensure text components exist; create them if necessary
        if (levelText == null) levelText = CreateStatText(uiCanvas, "LevelText");
        if (timeText == null) timeText = CreateStatText(uiCanvas, "TimeText");
        if (healthText == null) healthText = CreateStatText(uiCanvas, "HealthText");
        if (scoreText == null) scoreText = CreateStatText(uiCanvas, "ScoreText");

        // Setup panels
        SetupStatPanel(ref levelPanelImage, levelText, levelPanelPosition, levelPanelSize, levelPanelScale, levelTextScale, levelTextPosition, levelPanelSprite, "LevelPanel", uiCanvas);
        SetupStatPanel(ref timePanelImage, timeText, timePanelPosition, timePanelSize, timePanelScale, timeTextScale, timeTextPosition, timePanelSprite, "TimePanel", uiCanvas);
        SetupStatPanel(ref healthPanelImage, healthText, healthPanelPosition, healthPanelSize, healthPanelScale, healthTextScale, healthTextPosition, healthPanelSprite, "HealthPanel", uiCanvas);
        SetupStatPanel(ref scorePanelImage, scoreText, scorePanelPosition, scorePanelSize, scorePanelScale, scoreTextScale, scoreTextPosition, scorePanelSprite, "ScorePanel", uiCanvas);
    }

    TextMeshProUGUI CreateStatText(Canvas canvas, string textName)
    {
        GameObject textObj = new GameObject(textName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(canvas.transform, false);
        
        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        var rect = textObj.GetComponent<RectTransform>();
        
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(300f, 60f);
        
        tmp.text = textName;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 26;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        
        return tmp;
    }

    void SetupStatPanel(ref Image panelImage, TextMeshProUGUI statText, Vector2 panelPosition, Vector2 panelSize, Vector3 panelScale, Vector3 textScale, Vector2 textPosition, Sprite panelSprite, string panelName, Canvas canvas)
    {
        if (statText == null || canvas == null) return;

        GameObject statRoot = null;

        // Create panel if doesn't exist
        if (panelImage == null)
        {
            GameObject panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            panelImage = panelObject.GetComponent<Image>();
        }

        if (panelImage != null)
        {
            statRoot = panelImage.transform.parent != null && panelImage.transform.parent.name == $"{panelName}Root"
                ? panelImage.transform.parent.gameObject
                : null;

            if (statRoot == null)
            {
                statRoot = new GameObject($"{panelName}Root", typeof(RectTransform));
                statRoot.transform.SetParent(canvas.transform, false);
                panelImage.transform.SetParent(statRoot.transform, false);
            }

            Vector2 finalPosition = panelPosition;
            if (useHalfScreenPositions)
            {
                float halfWidth = canvas.GetComponent<RectTransform>().rect.width * 0.5f;
                float halfHeight = canvas.GetComponent<RectTransform>().rect.height * 0.5f;
                finalPosition = new Vector2(
                    panelPosition.x < 0 ? -halfWidth + panelSize.x * 0.5f + screenHalfOffsetX : halfWidth - panelSize.x * 0.5f + screenHalfOffsetX,
                    panelPosition.y + screenHalfOffsetY
                );
            }

            panelImage.color = statPanelColor;
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.preserveAspect = false;
            panelImage.raycastTarget = false;

            var panelRect = panelImage.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = panelSize;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localScale = panelScale;

            var rootRect = statRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = panelSize;
            rootRect.anchoredPosition = finalPosition;
            rootRect.localScale = Vector3.one;
        }

        // Move text into its own container so it can scale independently from the panel.
        if (statText.transform.parent != statRoot.transform)
        {
            statText.transform.SetParent(statRoot.transform, false);
        }

        var textRect = statText.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = textPosition;
        textRect.sizeDelta = new Vector2(panelSize.x * 0.88f, panelSize.y * 0.8f);
        textRect.localScale = textScale;
        statText.alignment = TextAlignmentOptions.Center;
        statText.textWrappingMode = TextWrappingModes.NoWrap;
        statText.fontSize = 24;
        statText.enableAutoSizing = false;
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

        if (timeText != null)
        {
            timeText.text = $"Time: {currentQuestionTimeLimit:F1}s";
        }

        // Update health display with heart icons
        UpdateHealthDisplay();
    }

    void UpdateHealthDisplay()
    {
        if (healthText == null) return;

        // Create or find health container
        if (healthDisplayContainer == null)
        {
            GameObject containerObj = new GameObject("HealthDisplayContainer", typeof(RectTransform));
            containerObj.transform.SetParent(healthText.transform.parent, false);
            
            var rect = containerObj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = healthTextPosition;
            rect.sizeDelta = new Vector2(200f, 50f);
            rect.localScale = Vector3.one;
            
            healthDisplayContainer = containerObj.transform;
            
            // Hide the text component if it exists
            healthText.gameObject.SetActive(false);
        }

        if (healthDisplayContainer != null)
        {
            healthDisplayContainer.localScale = healthPanelScale;
            var heartContainerRect = healthDisplayContainer.GetComponent<RectTransform>();
            heartContainerRect.anchoredPosition = healthTextPosition;
        }

        // Clear existing hearts
        foreach (Transform child in healthDisplayContainer)
        {
            Destroy(child.gameObject);
        }

        // Create heart icons
        float size = heartSize;
        float spacing = heartSpacing;
        float startX = -(maxHealth * (size + spacing)) / 2f;

        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heartObj = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
            heartObj.transform.SetParent(healthDisplayContainer, false);
            
            var heartRect = heartObj.GetComponent<RectTransform>();
            heartRect.sizeDelta = new Vector2(size, size);
            heartRect.anchoredPosition = new Vector3(startX + i * (size + spacing), 0f, 0f);
            heartRect.localScale = heartScale;
            
            var heartImage = heartObj.GetComponent<Image>();
            heartImage.sprite = (i < currentHealth) ? fullHeartSprite : emptyHeartSprite;
            heartImage.color = Color.white;
            heartImage.preserveAspect = true;
            heartImage.raycastTarget = false;
        }
    }

    void OnGUI()
    {
        if (!showLegacyDebugHud) return;
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
