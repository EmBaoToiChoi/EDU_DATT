using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider vfxSlider;

    void Start()
    {
        // Đồng bộ slider với cài đặt hiện tại trong AudioManager
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null)
            {
                musicSlider.value = AudioManager.Instance.GetMusicVolume();
                musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (vfxSlider != null)
            {
                vfxSlider.value = AudioManager.Instance.GetSFXVolume();
                vfxSlider.onValueChanged.AddListener(OnVFXSliderChanged);
            }
        }
        else
        {
            Debug.LogWarning("SettingsManager: Không tìm thấy AudioManager.Instance!");
        }
    }

    private void OnMusicSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    private void OnVFXSliderChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
    }
}
