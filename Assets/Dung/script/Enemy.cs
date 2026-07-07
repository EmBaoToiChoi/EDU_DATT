using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Speed Settings")]
    public float speed = 1.5f;

    [Header("Stun Settings")]
    public float stunDuration = 2f;

    private Vector3Int currentCell;
    private Vector3Int targetCell;
    private bool isStunned = false;
    private float stunTimer = 0f;

    private GamePlay gamePlay;
    private Tilemap walkableTilemap;
    private MazeNavigator navigator;

    private Vector3Int startCell;
    private float defaultSpeed;
    private bool hasSavedDefaultSpeed = false;
    private Vector3 originalLocalScale = Vector3.one;

    private void Awake()
    {
        originalLocalScale = transform.localScale;
    }

    public void Spawn(Vector3Int spawnCell, GamePlay gp)
    {
        if (!hasSavedDefaultSpeed)
        {
            defaultSpeed = speed;
            hasSavedDefaultSpeed = true;
        }
        else
        {
            speed = defaultSpeed;
        }

        startCell = spawnCell;
        gamePlay = gp;
        navigator = gp.navigator;
        walkableTilemap = gp.navigator.walkableTilemap;

        transform.position = walkableTilemap.GetCellCenterWorld(spawnCell);
        transform.localScale = originalLocalScale;

        currentCell = spawnCell;
        targetCell = spawnCell;
        isStunned = false;
        stunTimer = 0f;
    }

    public void ResetToStart()
    {
        if (walkableTilemap == null) return;
        transform.position = walkableTilemap.GetCellCenterWorld(startCell);
        currentCell = startCell;
        targetCell = startCell;
        isStunned = false;
        stunTimer = 0f;
    }

    void Update()
    {
        if (gamePlay == null || walkableTilemap == null || navigator == null) return;

        // If startScreen is active, do not move/act
        if (gamePlay.startScreen != null && gamePlay.startScreen.activeSelf)
        {
            return;
        }

        // If stunned, tick down timer and stand still
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
            }
            return;
        }

        // If game is waiting for a question to be answered, stand still
        if (gamePlay.waitingForAnswer)
        {
            return;
        }

        // Move towards target cell
        Vector3 targetWorld = walkableTilemap.GetCellCenterWorld(targetCell);
        transform.position = Vector3.MoveTowards(transform.position, targetWorld, speed * Time.deltaTime);

        // When reaching target cell, calculate the next cell towards player
        if (Vector3.Distance(transform.position, targetWorld) <= 0.05f)
        {
            transform.position = targetWorld;
            currentCell = targetCell;

            Vector3Int playerCell = walkableTilemap.WorldToCell(gamePlay.transform.position);
            if (currentCell != playerCell)
            {
                targetCell = GetNextStep(currentCell, playerCell);
            }
        }

        // Detect touch/proximity with Player
        float playerDist = Vector3.Distance(transform.position, gamePlay.transform.position);
        Vector3Int enemyCell = walkableTilemap.WorldToCell(transform.position);
        Vector3Int playerGridCell = walkableTilemap.WorldToCell(gamePlay.transform.position);

        if (enemyCell == playerGridCell || playerDist < 0.4f)
        {
            TriggerQuestionPrompt();
        }
    }

    void TriggerQuestionPrompt()
    {
        // Safety check to ensure we only trigger when game is not already asking a question
        if (gamePlay == null || gamePlay.waitingForAnswer || isStunned) return;

        gamePlay.waitingForAnswer = true;
        gamePlay.StopMovement();

        if (gamePlay.questionManager != null)
        {
            gamePlay.questionManager.ShowRandomQuestion((correct) =>
            {
                if (correct)
                {
                    Debug.Log("Enemy touched player: Answered CORRECT. Enemy resets to start.");
                    ResetToStart();
                }
                else
                {
                    Debug.Log("Enemy touched player: Answered INCORRECT. Player loses 1 HP, Enemy resets to start and moves 2% faster.");
                    gamePlay.TakeDamage(1);
                    speed *= 1.02f;
                    ResetToStart();
                }

                // If player is still alive, resume movement
                if (gamePlay.currentHealth > 0)
                {
                    gamePlay.waitingForAnswer = false;
                }
            });
        }
        else
        {
            // Fallback if no question manager is configured
            Debug.LogWarning("No QuestionManager found on GamePlay!");
            ResetToStart();
            gamePlay.waitingForAnswer = false;
        }
    }

    // BFS Pathfinding from start to target on the walkable tilemap
    private Vector3Int GetNextStep(Vector3Int start, Vector3Int target)
    {
        if (start == target) return start;

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> parentMap = new Dictionary<Vector3Int, Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector3Int[] dirs = new Vector3Int[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        bool found = false;

        while (queue.Count > 0)
        {
            Vector3Int curr = queue.Dequeue();
            if (curr == target)
            {
                found = true;
                break;
            }

            foreach (var d in dirs)
            {
                Vector3Int next = curr + d;
                if (walkableTilemap.HasTile(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    parentMap[next] = curr;
                    queue.Enqueue(next);
                }
            }
        }

        if (!found) return start; // No path found, stay in place

        // Reconstruct path to find the first step from start towards target
        Vector3Int step = target;
        while (parentMap.ContainsKey(step) && parentMap[step] != start)
        {
            step = parentMap[step];
        }

        return step;
    }

    // Also support Unity Physics 2D collisions in case they are configured
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (gamePlay != null && collision.gameObject == gamePlay.gameObject)
        {
            TriggerQuestionPrompt();
        }
        else if (collision.gameObject.GetComponent<GamePlay>() != null)
        {
            TriggerQuestionPrompt();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (gamePlay != null && collision.gameObject == gamePlay.gameObject)
        {
            TriggerQuestionPrompt();
        }
        else if (collision.gameObject.GetComponent<GamePlay>() != null)
        {
            TriggerQuestionPrompt();
        }
    }
}
