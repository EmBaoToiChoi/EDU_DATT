using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class charclick : MonoBehaviour, IPointerClickHandler
{
    [Header("Spawn settings")]
    public GameObject[] spawnPrefabs;
    public Transform spawnParent;
    public RectTransform spawnAreaRect;
    public Vector2 spawnAreaMin = new Vector2(-150f, -250f);
    public Vector2 spawnAreaMax = new Vector2(150f, 150f);
    public int minSpawnCount = 1;
    public int maxSpawnCount = 5;
    public bool randomRotation = false;
    public float fallSpeed = 500f;
    public float fallSpeedVariance = 150f;

    [Header("Sounds")]
    public AudioClip clickSound;
    public AudioSource audioSource;
    public AudioClip spawnSound;

    [Header("VFX")]
    public GameObject vfxPrefab;

    [Header("Button support")]
    public Button button;
    public bool autoBindButtonClick = true;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null && autoBindButtonClick)
        {
            button.onClick.RemoveListener(OnButtonClick);
            button.onClick.AddListener(OnButtonClick);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickSound();
        Debug.Log("charclick: OnPointerClick triggered.");
        SpawnRandomObjects();
    }

    public void OnButtonClick()
    {
        PlayClickSound();
        Debug.Log("charclick: OnButtonClick triggered.");
        SpawnRandomObjects();
    }

    private void PlayClickSound()
    {
        if (clickSound == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clickSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    public void SpawnRandomObjects()
    {
        Debug.Log("charclick: SpawnRandomObjects start.");
        if (spawnPrefabs == null || spawnPrefabs.Length == 0)
        {
            Debug.LogWarning("charclick: Chưa gán spawnPrefabs.");
            return;
        }

        if (button != null && !button.interactable)
        {
            Debug.LogWarning("charclick: Button không thể tương tác.");
            return;
        }

        Debug.Log("charclick: SpawnRandomObjects start.");
        // remove any previously spawned objects and VFX so old effects don't persist
        ClearSpawnedObjects();
        int count = Random.Range(minSpawnCount, maxSpawnCount + 1);
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Length)];
            if (prefab == null) continue;

            Vector3 spawnPosition = GetRandomSpawnPosition();
            Transform parent = spawnParent != null ? spawnParent : transform;
            GameObject spawned = Instantiate(prefab, parent);

            if (spawned.transform is RectTransform)
            {
                RectTransform rt = spawned.transform as RectTransform;
                rt.anchoredPosition = spawnPosition;
            }
            else
            {
                spawned.transform.localPosition = spawnPosition;
            }

            if (randomRotation)
            {
                spawned.transform.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
            }

            var fall = spawned.GetComponent<FallingObject>();
            if (fall == null)
            {
                fall = spawned.AddComponent<FallingObject>();
            }
            fall.fallSpeed = fallSpeed + Random.Range(-fallSpeedVariance, fallSpeedVariance);
            fall.destroyY = spawnAreaMin.y - 100f;

            AddDestroyableComponent(spawned);
            spawnedObjects.Add(spawned);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnAreaRect != null)
        {
            float x = Random.Range(spawnAreaRect.rect.xMin, spawnAreaRect.rect.xMax);
            float y = Random.Range(spawnAreaRect.rect.yMin, spawnAreaRect.rect.yMax);
            return new Vector3(x, y, 0f);
        }

        float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float randomY = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        return new Vector3(randomX, randomY, 0f);
    }

    private void ClearSpawnedObjects()
    {
        // destroy spawned objects
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i]);
            }
        }
        spawnedObjects.Clear();

        // also destroy any VFX children under the spawn parent to avoid lingering effects
        Transform parent = spawnParent != null ? spawnParent : transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child == null) continue;
            if (child.GetComponentInChildren<ParticleSystem>(true) != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void PlaySpawnSound()
    {
        if (spawnSound == null) return;
        if (audioSource != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }
        else
        {
            AudioSource.PlayClipAtPoint(spawnSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
        }
    }

    private void AddDestroyableComponent(GameObject spawned)
    {
        var destroyable = spawned.GetComponent<SpawnedDestroyable>();
        if (destroyable == null)
        {
            destroyable = spawned.AddComponent<SpawnedDestroyable>();
        }

        destroyable.vfxPrefab = vfxPrefab;
        // pass audio info to the destroyable so it can play sound when clicked/destroyed
        destroyable.clickSound = clickSound;
        destroyable.audioSource = audioSource;
    }

    private class SpawnedDestroyable : MonoBehaviour, IPointerClickHandler
    {
        public GameObject vfxPrefab;
        private bool hasHandled;
        public AudioClip clickSound;
        public AudioSource audioSource;

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
            if (hasHandled) return;
            hasHandled = true;

            // play sound at click, then show VFX and destroy
            PlayClickSound();
            Debug.Log("charclick: Carrot clicked -> play sound, show VFX and destroy.");
            StartCoroutine(ShowVfxThenDestroy());
        }

        private void PlayClickSound()
        {
            if (clickSound == null) return;

            if (audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
            else
            {
                var camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(clickSound, camPos);
            }
        }

        private IEnumerator ShowVfxThenDestroy()
        {
            GameObject vfxInstance = null;

            if (vfxPrefab != null)
            {
                vfxInstance = Instantiate(vfxPrefab, transform.parent, false);
                PositionVfx(vfxInstance);
            }
            else
            {
                var vfxChild = FindVfxChild(transform);
                if (vfxChild != null)
                {
                    vfxInstance = Instantiate(vfxChild, transform.parent, false);
                    PositionVfx(vfxInstance);
                }
                else
                {
                    Debug.LogWarning("charclick: No VFX child/prefab found for clicked carrot.");
                }
            }

            if (vfxInstance != null)
            {
                vfxInstance.SetActive(true);
                PlayParticleSystem(vfxInstance);
                Debug.Log("charclick: VFX instantiated for clicked carrot.");
            }

            yield return null;
            Destroy(gameObject);
        }

        private GameObject FindVfxChild(Transform root)
        {
            if (root == null) return null;

            foreach (Transform child in root)
            {
                if (child == null) continue;

                if (child.name.ToLowerInvariant().Contains("vfx") || child.GetComponent<ParticleSystem>() != null)
                {
                    return child.gameObject;
                }

                var nested = FindVfxChild(child);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void PositionVfx(GameObject vfxInstance)
        {
            if (vfxInstance == null) return;

            if (vfxInstance.transform is RectTransform vfxRect && transform is RectTransform carrotRect)
            {
                vfxRect.SetParent(transform.parent, false);
                vfxRect.anchoredPosition = carrotRect.anchoredPosition;
                vfxRect.localRotation = Quaternion.identity;
                vfxRect.localScale = carrotRect.localScale;
                return;
            }

            vfxInstance.transform.SetParent(transform.parent, false);
            vfxInstance.transform.position = transform.position;
            vfxInstance.transform.rotation = transform.rotation;
            vfxInstance.transform.localScale = transform.localScale;
        }

        private void PlayParticleSystem(GameObject vfxInstance)
        {
            if (vfxInstance == null) return;

            var particles = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particles)
            {
                ps.gameObject.SetActive(true);
                ps.Play(true);
            }
        }
    }
}

public class FallingObject : MonoBehaviour
{
    public float fallSpeed = 500f;
    public float destroyY = -400f;

    private RectTransform rectTransform;
    private bool hasBeenDestroyed;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        float delta = fallSpeed * Time.deltaTime;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += new Vector2(0f, -delta);
            if (rectTransform.anchoredPosition.y <= destroyY)
            {
                if (!hasBeenDestroyed)
                {
                    hasBeenDestroyed = true;
                    Debug.Log("charclick: Carrot fell out of bounds and was destroyed.");
                }
                Destroy(gameObject);
            }
        }
        else
        {
            transform.localPosition += new Vector3(0f, -delta, 0f);
            if (transform.localPosition.y <= destroyY)
            {
                if (!hasBeenDestroyed)
                {
                    hasBeenDestroyed = true;
                    Debug.Log("charclick: Carrot fell out of bounds and was destroyed.");
                }
                Destroy(gameObject);
            }
        }
    }
}
