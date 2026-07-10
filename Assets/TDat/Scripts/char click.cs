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
        Debug.Log("charclick: OnPointerClick triggered.");
        SpawnRandomObjects();
    }

    public void OnButtonClick()
    {
        Debug.Log("charclick: OnButtonClick triggered.");
        SpawnRandomObjects();
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
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();
    }

    private void AddDestroyableComponent(GameObject spawned)
    {
        if (spawned.GetComponent<SpawnedDestroyable>() == null)
        {
            spawned.AddComponent<SpawnedDestroyable>();
        }
    }

    private class SpawnedDestroyable : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            Destroy(gameObject);
        }

        private void OnMouseDown()
        {
            Destroy(gameObject);
        }
    }
}

public class FallingObject : MonoBehaviour
{
    public float fallSpeed = 500f;
    public float destroyY = -400f;

    private RectTransform rectTransform;

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
                Destroy(gameObject);
            }
        }
        else
        {
            transform.localPosition += new Vector3(0f, -delta, 0f);
            if (transform.localPosition.y <= destroyY)
            {
                Destroy(gameObject);
            }
        }
    }
}
