using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class VolumePanelController : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string musicParam = "MusicVol";
    [SerializeField] private string sfxParam = "SFXVol";

    [Header("Sliders (0..1)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Labels (optional)")]
    [SerializeField] private TextMeshProUGUI masterLabel;
    [SerializeField] private TextMeshProUGUI musicLabel;
    [SerializeField] private TextMeshProUGUI sfxLabel;

    private void OnEnable()
    {
        // Only sync UI from PlayerPrefs; do NOT write to the mixer here.
        LoadPrefsToUI();
        UpdateAllLabels();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ApplySavedToMixer()
    {
        // Always refresh from PlayerPrefs first to avoid stale UI values
        LoadPrefsToUI();
        ApplyAll();
        UpdateAllLabels();
    }


    // Slider events
    public void OnMasterChanged(float v)
    {
        PlayerPrefs.SetFloat("pp_master", v);
        PlayerPrefs.Save();
        SetMixerLinear(masterParam, v);
        UpdateLabel(masterLabel, "Master", v);
    }

    public void OnMusicChanged(float v)
    {
        PlayerPrefs.SetFloat("pp_music", v);
        PlayerPrefs.Save();
        SetMixerLinear(musicParam, v);
        UpdateLabel(musicLabel, "Music", v);
    }

    public void OnSfxChanged(float v)
    {
        PlayerPrefs.SetFloat("pp_sfx", v);
        PlayerPrefs.Save();
        SetMixerLinear(sfxParam, v);
        UpdateLabel(sfxLabel, "SFX", v);
    }

    // Internals
    private void ApplyAll()
    {
        if (masterSlider) SetMixerLinear(masterParam, masterSlider.value);
        if (musicSlider) SetMixerLinear(musicParam, musicSlider.value);
        if (sfxSlider) SetMixerLinear(sfxParam, sfxSlider.value);
    }

    private void LoadPrefsToUI()
    {
        float m = PlayerPrefs.GetFloat("pp_master", 1f);
        float mu = PlayerPrefs.GetFloat("pp_music", 1f);
        float s = PlayerPrefs.GetFloat("pp_sfx", 1f);

        if (masterSlider) masterSlider.SetValueWithoutNotify(m);
        if (musicSlider) musicSlider.SetValueWithoutNotify(mu);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(s);
    }

    private void SetMixerLinear(string exposedParam, float linear01)
    {
        if (!masterMixer || string.IsNullOrEmpty(exposedParam)) return;

        if (Mathf.Approximately(linear01, 0f))
        {
            masterMixer.SetFloat(exposedParam, -80f);
            return;
        }

        float l = Mathf.Clamp(linear01, 0.0001f, 1f);
        float dB = Mathf.Log10(l) * 20f;
        masterMixer.SetFloat(exposedParam, dB);
    }

    private void UpdateAllLabels()
    {
        if (masterSlider) UpdateLabel(masterLabel, "Master", masterSlider.value);
        if (musicSlider) UpdateLabel(musicLabel, "Music", musicSlider.value);
        if (sfxSlider) UpdateLabel(sfxLabel, "SFX", sfxSlider.value);
    }

    private void UpdateLabel(TextMeshProUGUI label, string text, float value01)
    {
        if (!label) return;
        int pct = Mathf.RoundToInt(Mathf.Clamp01(value01) * 100f);
        label.text = $"{text} ({pct}%)";
    }
}
