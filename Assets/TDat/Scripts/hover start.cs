using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class hoverstart : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Tham chiếu đến object cần hiển thị/ẩn khi hover
    public GameObject hoverObject;

    private void Start()
    {
        // Ẩn object khi bắt đầu
        if (hoverObject != null)
        {
            hoverObject.SetActive(false);
        }
    }

    // Khi chuột vào button
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverObject != null)
        {
            hoverObject.SetActive(true);
        }
    }

    // Khi chuột rời khỏi button
    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverObject != null)
        {
            hoverObject.SetActive(false);
        }
    }
}
