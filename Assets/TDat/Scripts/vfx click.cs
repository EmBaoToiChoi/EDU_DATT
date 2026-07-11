using UnityEngine;
using UnityEngine.EventSystems;

public class vfxclick : MonoBehaviour, IPointerClickHandler
{
    [Header("VFX Settings")]
    public GameObject vfxPrefab;
    public GameObject vfxChild;
    public Vector3 vfxScale = Vector3.one;
    public bool detachVfxOnPlay = true;
    public float vfxDestroyDelay = 2f;

    private void Awake()
    {
        if (vfxChild == null)
        {
            vfxChild = FindVfxChild(transform);
        }

        if (vfxChild != null)
        {
            vfxChild.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayVfxAndDestroyCarrot();
    }

    private void OnMouseDown()
    {
        PlayVfxAndDestroyCarrot();
    }

    private void PlayVfxAndDestroyCarrot()
    {
        GameObject vfxInstance = null;
        Vector3 targetScale = vfxScale;

        if (vfxPrefab != null)
        {
            vfxInstance = Instantiate(vfxPrefab, transform.parent, false);
        }
        else if (vfxChild != null)
        {
            bool isPrefabAsset = IsPrefabAsset(vfxChild);
            if (isPrefabAsset)
            {
                vfxInstance = Instantiate(vfxChild, transform.parent, false);
            }
            else
            {
                vfxInstance = Instantiate(vfxChild, transform.parent, false);
            }
        }

        if (vfxInstance != null)
        {
            if (detachVfxOnPlay)
            {
                vfxInstance.transform.SetParent(transform.parent, false);
            }

            if (vfxScale != Vector3.one)
            {
                vfxInstance.transform.localScale = vfxScale;
            }

            SetVfxPosition(vfxInstance);
            vfxInstance.SetActive(true);
            PlayParticleSystems(vfxInstance);
            Destroy(vfxInstance, vfxDestroyDelay);
        }
        else
        {
            Debug.LogWarning("vfxclick: No VFX prefab or child assigned.");
        }

        Destroy(gameObject);
    }

    private void SetVfxPosition(GameObject vfxInstance)
    {
        if (vfxInstance == null) return;

        var carrotRect = transform as RectTransform;
        var vfxRect = vfxInstance.transform as RectTransform;

        if (carrotRect != null && vfxRect != null)
        {
            vfxRect.anchoredPosition = carrotRect.anchoredPosition;
            vfxRect.localRotation = carrotRect.localRotation;
            return;
        }

        vfxInstance.transform.position = transform.position;
        vfxInstance.transform.rotation = transform.rotation;
    }

    private bool IsPrefabAsset(GameObject go)
    {
        return go != null && go.scene.rootCount == 0;
    }

    private void PlayParticleSystems(GameObject root)
    {
        if (root == null) return;

        var particles = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.gameObject.SetActive(true);
            ps.Play(true);
        }
    }

    private GameObject FindVfxChild(Transform root)
    {
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
}
