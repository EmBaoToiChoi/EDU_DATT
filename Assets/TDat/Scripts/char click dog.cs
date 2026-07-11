using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class charclickdog : MonoBehaviour, IPointerClickHandler
{
    [Header("Prefab objects")]
    public GameObject khoBaoObjectPrefab;

    [Header("Chest stage sprites")]
    public Sprite chestStage1Sprite;
    public Sprite chestStage2Sprite;
    public Sprite chestBombStage3Sprite;
    public Sprite chestTreasureStage3Sprite;

    [Header("Spawn settings")]
    public Transform spawnParent;
    public RectTransform spawnAreaRect;
    public Vector2 spawnPositionOffset = Vector2.zero;
    public float objectLifetime = 5f;
    public int clicksToOpen = 2;

    [Header("Sounds")]
    public AudioClip charClickSound;
    public AudioClip closedChestClickSound;
    public AudioClip treasureOpenSound;
    public AudioClip bombOpenSound;
    public AudioSource audioSource;

    [Header("VFX")]
    public GameObject chestBombVfxPrefab;
    public GameObject chestTreasureVfxPrefab;

    [Header("Button support")]
    public Button button;
    public bool autoBindButtonClick = true;

    private GameObject currentObject;
    private int dogClickCount;
    private bool objectOpened;
    private bool openedAsBomb;

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
        PlayClickSound(charClickSound);
        HandleDogClick();
    }

    public void OnButtonPressed()
    {
        PlayClickSound(charClickSound);
        HandleDogClick();
    }

    private void HandleDogClick()
    {
        if (currentObject == null)
        {
            currentObject = SpawnChestObject();
            dogClickCount = 0;
            objectOpened = false;
            openedAsBomb = false;
            SetChestStage(currentObject, chestStage1Sprite);
            return;
        }

        // Nếu đã có rương, click char dog chỉ dùng để hủy khi rương đã mở
        if (!objectOpened)
        {
            return;
        }

        DestroyCurrentObject();
    }

    private void HandleChestClick()
    {
        if (currentObject == null || objectOpened)
        {
            return;
        }

        dogClickCount++;
        PlaySound(closedChestClickSound);

        if (dogClickCount == 1)
        {
            SetChestStage(currentObject, chestStage2Sprite);
            return;
        }

        if (dogClickCount >= clicksToOpen)
        {
            OpenChest();
        }
    }

    private GameObject SpawnChestObject()
    {
        if (khoBaoObjectPrefab == null)
        {
            Debug.LogWarning("charclickdog: khoBaoObjectPrefab chưa gán.");
            return null;
        }

        GameObject spawned = Instantiate(khoBaoObjectPrefab, spawnParent != null ? spawnParent : transform);
        PositionSpawnedObject(spawned);
        var clickable = spawned.GetComponent<DogSpawnedObject>() ?? spawned.AddComponent<DogSpawnedObject>();
        clickable.lifeTime = objectLifetime;
        clickable.isOpened = false;
        clickable.clickSound = closedChestClickSound;
        clickable.onOpenedDestroy = DestroyCurrentObject;
        clickable.onChestClicked = HandleChestClick;
        clickable.audioSource = audioSource;
        return spawned;
    }

    private void OpenChest()
    {
        if (currentObject == null)
        {
            return;
        }

        openedAsBomb = Random.value < 0.5f;
        objectOpened = true;
        SetChestStage(currentObject, openedAsBomb ? chestBombStage3Sprite : chestTreasureStage3Sprite);

        var clickable = currentObject.GetComponent<DogSpawnedObject>();
        if (clickable != null)
        {
            clickable.isOpened = true;
            clickable.openSound = openedAsBomb ? bombOpenSound : treasureOpenSound;
            clickable.clickSound = null;
        }

        PlaySound(openedAsBomb ? bombOpenSound : treasureOpenSound);
        PlayOpenVfx(currentObject, openedAsBomb);
    }

    private void PlayOpenVfx(GameObject chestObject, bool bomb)
    {
        if (chestObject == null) return;
        GameObject vfxPrefab = bomb ? chestBombVfxPrefab : chestTreasureVfxPrefab;
        if (vfxPrefab == null) return;

        Transform parent = chestObject.transform.parent != null ? chestObject.transform.parent : null;
        GameObject vfx = Instantiate(vfxPrefab, parent);

        // make sure VFX is parented under the same parent and rendered on top
        vfx.transform.SetParent(parent, false);
        PositionVfx(vfx, chestObject);
        vfx.transform.SetAsLastSibling();

        // raise particle renderers' sorting order so they appear above UI/world objects
        var psRenderers = vfx.GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (var pr in psRenderers)
        {
            pr.sortingOrder = 1000;
        }

        // if there's a Canvas in the VFX prefab, force it to override sorting
        var vfxCanvas = vfx.GetComponentInChildren<Canvas>(true);
        if (vfxCanvas != null)
        {
            vfxCanvas.overrideSorting = true;
            vfxCanvas.sortingOrder = 1000;
        }

        vfx.SetActive(true);
    }

    private void PositionVfx(GameObject vfxObject, GameObject targetObject)
    {
        if (vfxObject == null || targetObject == null) return;

        if (vfxObject.transform is RectTransform vfxRect && targetObject.transform is RectTransform targetRect)
        {
            vfxRect.SetParent(targetObject.transform.parent, false);
            vfxRect.anchoredPosition = targetRect.anchoredPosition;
            vfxRect.localRotation = Quaternion.identity;
            return;
        }

        vfxObject.transform.SetParent(targetObject.transform.parent, false);
        vfxObject.transform.position = targetObject.transform.position;
        vfxObject.transform.rotation = targetObject.transform.rotation;
    }

    private void SetChestStage(GameObject obj, Sprite sprite)
    {
        if (obj == null || sprite == null) return;

        var image = obj.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            return;
        }

        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sprite = sprite;
        }
    }

    private void PositionSpawnedObject(GameObject spawned)
    {
        if (spawned == null) return;

        if (spawned.transform is RectTransform rt)
        {
            Vector2 position = spawnPositionOffset;
            if (spawnAreaRect != null)
            {
                position = new Vector2(
                    Random.Range(spawnAreaRect.rect.xMin, spawnAreaRect.rect.xMax),
                    Random.Range(spawnAreaRect.rect.yMin, spawnAreaRect.rect.yMax)
                );
            }
            rt.anchoredPosition = position;
        }
        else
        {
            spawned.transform.localPosition = transform.localPosition + (Vector3)spawnPositionOffset;
        }
    }

    private void DestroyCurrentObject()
    {
        if (currentObject != null)
        {
            Destroy(currentObject);
        }
        currentObject = null;
        dogClickCount = 0;
        objectOpened = false;
        openedAsBomb = false;
    }

    private void PlayClickSound(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        PlayClickSound(clip);
    }
}

public class DogSpawnedObject : MonoBehaviour, IPointerClickHandler
{
    public AudioClip clickSound;
    public AudioClip openSound;
    public AudioSource audioSource;
    public float lifeTime = 5f;
    public bool isOpened;
    public System.Action onOpenedDestroy;
    public System.Action onChestClicked;
    private bool destroyedByClick;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    private void OnMouseDown()
    {
        HandleClick();
    }

    private void HandleClick()
    {
        if (isOpened)
        {
            return;
        }

        PlaySound(clickSound);
        onChestClicked?.Invoke();
    }

    private void OnDestroy()
    {
        if (!destroyedByClick)
        {
            onOpenedDestroy?.Invoke();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }
}
