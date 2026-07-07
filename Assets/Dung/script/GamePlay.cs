using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

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
    private Coroutine enemySpawnCoroutine;

    [Header("Game Over Settings")]
    public GameObject winScreen;
    public GameObject loseScreen;

    private Animator animator;
    private string currentAnimState = "";
    private Vector3 originalLocalScale = Vector3.one;

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

        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        FindEnemyInScene();
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
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
            LoseGame();
            return;
        }

        if (cell == navigator.GetGoalCell())
        {
            WinGame();
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

    public void StartGame()
    {
        if (startScreen != null)
        {
            startScreen.SetActive(false);
        }
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);

        currentHealth = maxHealth;
        waitingForAnswer = false;
        consecutiveWrong = 0;

        if (enemySpawnCoroutine != null) StopCoroutine(enemySpawnCoroutine);
        enemySpawnCoroutine = StartCoroutine(SpawnEnemyAfterDelay(5f));

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
        FindEnemyInScene();
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(delay);

        FindEnemyInScene();
        if (enemy != null && navigator != null && walkableTilemap != null)
        {
            enemy.gameObject.SetActive(true);
            enemy.Spawn(navigator.GetStartCell(), this);
        }
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
        Debug.Log("Game Lost!");
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
