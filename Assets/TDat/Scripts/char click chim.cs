using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class charclickchim : MonoBehaviour, IPointerClickHandler
{
    [Header("Spawn settings")]
    public GameObject chickenPrefab;
    public GameObject cowPrefab;
    public GameObject rabbitPrefab;
    public Transform spawnParent;
    public RectTransform spawnAreaRect;
    public Vector2 spawnPositionOffset = Vector2.zero;
    public float objectLifetime = 2f;

    [Header("Sound")]
    public AudioClip charClickSound;
    public AudioClip chickenSound;
    public AudioClip cowSound;
    public AudioClip rabbitSound;
    public AudioSource audioSource;

    [Header("VFX")]
    public GameObject spawnVfxPrefab;
    public float vfxDestroyDelay = 2f;

    [Header("Button support")]
    public Button button;
    public bool autoBindButtonClick = true;

    private GameObject currentSpawnedObject;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null && autoBindButtonClick)
        {
            button.onClick.RemoveListener(OnButtonPressed);
            button.onClick.AddListener(OnButtonPressed);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button != null)
        {
            return;
        }
        PlayCharClickSound();
        SpawnStaticObject();
    }

    public void OnButtonPressed()
    {
        PlayCharClickSound();
        SpawnStaticObject();
    }

    private void PlayCharClickSound()
    {
        if (charClickSound == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(charClickSound);
        }
        else
        {
            AudioSource.PlayClipAtPoint(charClickSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    private void SpawnStaticObject()
    {
        var selected = GetRandomAnimalPrefab();
        if (selected.prefab == null)
        {
            Debug.LogWarning("charclickchim: chưa gán prefab gà/bò/thỏ.");
            return;
        }

        if (currentSpawnedObject != null)
        {
            Destroy(currentSpawnedObject);
        }

        Transform parent = spawnParent != null ? spawnParent : transform;
        GameObject spawned = Instantiate(selected.prefab, parent);
        currentSpawnedObject = spawned;

        if (spawned.transform is RectTransform spawnedRect)
        {
            Vector2 anchoredPosition = spawnPositionOffset;
            if (spawnAreaRect != null)
            {
                anchoredPosition = new Vector2(
                    Random.Range(spawnAreaRect.rect.xMin, spawnAreaRect.rect.xMax),
                    Random.Range(spawnAreaRect.rect.yMin, spawnAreaRect.rect.yMax)
                );
            }
            spawnedRect.anchoredPosition = anchoredPosition;
        }
        else
        {
            spawned.transform.localPosition = transform.localPosition + (Vector3)spawnPositionOffset;
        }

        AddTimedDestroyable(spawned);
        AddClickableDestroyable(spawned, selected.sound);
    }

    private (GameObject prefab, AudioClip sound) GetRandomAnimalPrefab()
    {
        var prefabs = new[] { chickenPrefab, cowPrefab, rabbitPrefab };
        var sounds = new[] { chickenSound, cowSound, rabbitSound };

        int count = 0;
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                count++;
            }
        }

        if (count == 0)
        {
            return (null, null);
        }

        int index;
        do
        {
            index = Random.Range(0, prefabs.Length);
        }
        while (prefabs[index] == null && count > 0);

        return (prefabs[index], sounds[index]);
    }

    private void AddTimedDestroyable(GameObject spawned)
    {
        var timed = spawned.GetComponent<TimedDestroyable>();
        if (timed == null)
        {
            timed = spawned.AddComponent<TimedDestroyable>();
        }
        timed.lifeTime = objectLifetime;
    }

    private void AddClickableDestroyable(GameObject spawned, AudioClip sound)
    {
        var clickable = spawned.GetComponent<ClickableSpawnedObject>();
        if (clickable == null)
        {
            clickable = spawned.AddComponent<ClickableSpawnedObject>();
        }
        clickable.clickSound = sound;
        clickable.audioSource = audioSource;
        clickable.vfxPrefab = spawnVfxPrefab;
        clickable.vfxDestroyDelay = vfxDestroyDelay;
    }
}

public class TimedDestroyable : MonoBehaviour
{
    public float lifeTime = 2f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}

public class ClickableSpawnedObject : MonoBehaviour, IPointerClickHandler
{
    public AudioClip clickSound;
    public AudioSource audioSource;
    public GameObject vfxPrefab;
    public float vfxDestroyDelay = 2f;

    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySoundAndDestroy();
    }

    private void OnMouseDown()
    {
        PlaySoundAndDestroy();
    }

    private void PlaySoundAndDestroy()
    {
        if (clickSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clickSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            }
        }
        // spawn VFX if assigned
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, transform.parent);

            // position and bring to front in hierarchy
            if (vfx.transform is RectTransform && transform is RectTransform)
            {
                var vfxRect = vfx.transform as RectTransform;
                var srcRect = transform as RectTransform;
                vfxRect.SetParent(transform.parent, false);
                vfxRect.anchoredPosition = srcRect.anchoredPosition;
                vfxRect.SetAsLastSibling();
            }
            else
            {
                vfx.transform.SetParent(transform.parent, false);
                vfx.transform.position = transform.position;
                vfx.transform.SetAsLastSibling();
            }

            // raise particle renderers' sorting order so they appear above UI/world objects
            var psRenderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var pr in psRenderers)
            {
                try { pr.sortingOrder = 1000; } catch { }
            }

            // if there's a Canvas in the VFX prefab, force it to override sorting
            var vfxCanvas = vfx.GetComponentInChildren<Canvas>(true);
            if (vfxCanvas != null)
            {
                vfxCanvas.overrideSorting = true;
                vfxCanvas.sortingOrder = 1000;
            }

            // play particles
            var parts = vfx.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in parts) { ps.gameObject.SetActive(true); ps.Play(true); }
            Destroy(vfx, vfxDestroyDelay);
        }
        Destroy(gameObject);
    }
}
