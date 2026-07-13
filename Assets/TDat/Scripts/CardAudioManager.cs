using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CardAudioManager : MonoBehaviour
{
    public static CardAudioManager Instance;

    [Header("Replay target")]
    [SerializeField] private GameObject replayObject;

    [Header("Cards")]
    [SerializeField] private List<AIsound> cards = new List<AIsound>();

    private AIsound lastPlayedCard;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (replayObject == null)
            replayObject = gameObject;

        if (replayObject != null)
        {
            var clickReceiver = replayObject.GetComponent<ReplayCardClickReceiver>();
            if (clickReceiver == null)
                clickReceiver = replayObject.AddComponent<ReplayCardClickReceiver>();

            clickReceiver.SetManager(this);
        }
    }

    public void RegisterCard(AIsound card)
    {
        if (card == null) return;
        if (!cards.Contains(card))
            cards.Add(card);
    }

    public void NotifyCardPlayed(AIsound card)
    {
        if (card == null) return;
        lastPlayedCard = card;
    }

    public void ReplayLastCard()
    {
        if (lastPlayedCard != null && !lastPlayedCard.IsCompleted())
            lastPlayedCard.PlayAudio();
    }

    public void StopAllCardAudio()
    {
        foreach (var card in cards)
        {
            if (card != null && card != this)
            {
                card.StopCurrentAudio();
            }
        }
    }
}

public class ReplayCardClickReceiver : MonoBehaviour, IPointerClickHandler
{
    private CardAudioManager manager;

    public void SetManager(CardAudioManager targetManager)
    {
        manager = targetManager;
    }

    private void Awake()
    {
        EnsureCollider();
    }

    private void Start()
    {
        if (manager == null)
            manager = FindObjectOfType<CardAudioManager>();
    }

    private void OnMouseDown()
    {
        TriggerReplay();
    }

    private void OnMouseUpAsButton()
    {
        TriggerReplay();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TriggerReplay();
    }

    private void TriggerReplay()
    {
        if (manager == null)
            manager = FindObjectOfType<CardAudioManager>();

        if (manager != null)
            manager.ReplayLastCard();
    }

    private void EnsureCollider()
    {
        if (GetComponent<Collider>() != null || GetComponent<Collider2D>() != null)
            return;

        if (GetComponent<SpriteRenderer>() != null)
        {
            var collider = gameObject.GetComponent<BoxCollider2D>();
            if (collider == null)
                gameObject.AddComponent<BoxCollider2D>();
            return;
        }

        var boxCollider = gameObject.GetComponent<BoxCollider>();
        if (boxCollider == null)
            gameObject.AddComponent<BoxCollider>();
    }
}
