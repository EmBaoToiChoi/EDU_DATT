using UnityEngine;

public class AnimalSkillPickup : MonoBehaviour
{
    private GamePlay gamePlay;
    private bool collected;

    public void Initialize(GamePlay gp)
    {
        gamePlay = gp;
    }

    private void Update()
    {
        if (collected || gamePlay == null) return;

        if (Vector3.Distance(transform.position, gamePlay.transform.position) <= 0.35f)
        {
            TryCollect();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other.gameObject);
    }

    private void TryCollect(GameObject otherObject = null)
    {
        if (collected) return;

        if (gamePlay == null)
        {
            gamePlay = FindAnyObjectByType<GamePlay>();
        }

        if (gamePlay == null) return;

        if (otherObject != null)
        {
            bool isPlayer = otherObject == gamePlay.gameObject || otherObject.transform.IsChildOf(gamePlay.transform);
            if (!isPlayer && otherObject.transform.parent != null)
            {
                isPlayer = otherObject.transform.parent.gameObject == gamePlay.gameObject || otherObject.transform.parent.IsChildOf(gamePlay.transform);
            }

            if (!isPlayer) return;
        }

        collected = true;
        gamePlay.GrantSkill();
        Destroy(gameObject);
    }
}
