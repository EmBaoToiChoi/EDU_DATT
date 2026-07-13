using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class AIsound : MonoBehaviour, IPointerClickHandler
{
    [Header("Audio")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool playOnStart;
    [SerializeField] private AudioClip startSceneSound;

    [Header("Replay button")]
    [SerializeField] private Button replayButton;

    [Header("Completion")]
    [SerializeField] private bool hideWhenCompleted = true;

    [Header("Visual feedback")]
    [SerializeField] private GameObject speendObject;
    [SerializeField] private bool showSpeendWhilePlaying = true;

    private bool isCompleted;
    private Coroutine hideSpeendCoroutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (replayButton != null)
            replayButton.onClick.AddListener(Replay);
    }

    private void Start()
    {
        if (CardAudioManager.Instance != null)
            CardAudioManager.Instance.RegisterCard(this);

        SetSpeendVisible(false);

        if (playOnStart)
            PlayAudio();

        if (startSceneSound != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = startSceneSound;
            audioSource.Play();
        }
    }

    private void OnDestroy()
    {
        if (hideSpeendCoroutine != null)
        {
            StopCoroutine(hideSpeendCoroutine);
            hideSpeendCoroutine = null;
        }

        if (replayButton != null)
            replayButton.onClick.RemoveListener(Replay);
    }

    private void OnDisable()
    {
        if (hideSpeendCoroutine != null)
        {
            StopCoroutine(hideSpeendCoroutine);
            hideSpeendCoroutine = null;
        }

        SetSpeendVisible(false);
    }

    private void OnMouseDown()
    {
        if (!isCompleted && !IsPointerOverUI())
            PlayAudio();

        handclick tutorial = FindObjectOfType<handclick>();
        if (tutorial != null)
            tutorial.HideTutorial();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isCompleted)
            PlayAudio();

        handclick tutorial = FindObjectOfType<handclick>();
        if (tutorial != null)
            tutorial.HideTutorial();
    }

    public void Replay()
    {
        if (!isCompleted)
            PlayAudio();
    }

    public void PlayAudio()
    {
        if (audioClip == null)
        {
            Debug.LogWarning($"{name}: Chưa gán AudioClip cho AI sound.", this);
            return;
        }

        if (audioSource == null)
            return;

        if (CardAudioManager.Instance != null)
            CardAudioManager.Instance.StopAllCardAudio();

        audioSource.Stop();
        audioSource.clip = audioClip;
        audioSource.Play();

        if (showSpeendWhilePlaying)
        {
            SetSpeendVisible(true);

            if (hideSpeendCoroutine != null)
                StopCoroutine(hideSpeendCoroutine);

            float delay = audioSource.clip != null ? audioSource.clip.length : 0f;
            hideSpeendCoroutine = StartCoroutine(HideSpeendAfterDelay(delay));
        }

        if (CardAudioManager.Instance != null)
            CardAudioManager.Instance.NotifyCardPlayed(this);
    }

    public void MarkCompleted()
    {
        isCompleted = true;
        SetSpeendVisible(false);

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (hideWhenCompleted)
            gameObject.SetActive(false);
    }

    public void ResetCompletion()
    {
        isCompleted = false;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    private IEnumerator HideSpeendAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetSpeendVisible(false);
        hideSpeendCoroutine = null;
    }

    private void SetSpeendVisible(bool visible)
    {
        if (speendObject != null)
            speendObject.SetActive(visible);
    }

    public bool IsCompleted()
    {
        return isCompleted;
    }

    public void StopCurrentAudio()
    {
        if (audioSource != null)
            audioSource.Stop();

        SetSpeendVisible(false);
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
