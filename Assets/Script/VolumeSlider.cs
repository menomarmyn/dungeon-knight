using UnityEngine;
using UnityEngine.UI;

// Ползунок громкости в окне Settings. param — имя exposed-параметра микшера (MusicVol / SFXVol).
public class VolumeSlider : MonoBehaviour
{
    public string param = AudioPrefs.Music;
    public Slider slider;                     // Min Value 0.0001, Max Value 1.
    public TMPro.TextMeshProUGUI valueText;   // Показывает значение в dB.

    void Start()                              // Не Awake: SetFloat работает со Start.
    {
        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.value = AudioPrefs.Get(param);
        slider.onValueChanged.AddListener(Set);
        Set(slider.value);
    }

    void Set(float value)
    {
        AudioPrefs.Set(param, value);
        if (valueText != null)
        {
            float db = AudioPrefs.ToDb(value);
            valueText.text = db <= -79.5f ? "OFF" : Mathf.RoundToInt(db) + " dB";
        }
    }

    void OnDisable()
    {
        AudioPrefs.Save(); // Окно закрыли — громкость записана на диск.
    }
}
