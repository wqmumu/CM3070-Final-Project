using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class VolumePanelController : MonoBehaviour
{
    [Header("Audio Mixer (same one used in gameplay)")]
    [SerializeField] private AudioMixer masterMixer;

    // Exposed parameter names in the mixer
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

    private const string PP_MASTER = "pp_master";
    private const string PP_MUSIC = "pp_music";
    private const string PP_SFX = "pp_sfx";

    private void Awake()
    {
        // Auto-wire listeners so you don't have to hook them in the Inspector
        if (masterSlider) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (musicSlider) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    }

    private void OnEnable()
    {
        LoadPrefsToUI();
        ApplyAllToMixer();      // make sure mixer reflects saved values immediately
        UpdateAllLabels();

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // ---- slider callbacks ----
    public void OnMasterChanged(float v)
    {
        PlayerPrefs.SetFloat(PP_MASTER, v); PlayerPrefs.Save();
        SetMixerLinear(masterParam, v);
        UpdateLabel(masterLabel, "Master", v);
    }
    public void OnMusicChanged(float v)
    {
        PlayerPrefs.SetFloat(PP_MUSIC, v); PlayerPrefs.Save();
        SetMixerLinear(musicParam, v);
        UpdateLabel(musicLabel, "Music", v);
    }
    public void OnSfxChanged(float v)
    {
        PlayerPrefs.SetFloat(PP_SFX, v); PlayerPrefs.Save();
        SetMixerLinear(sfxParam, v);
        UpdateLabel(sfxLabel, "SFX", v);
    }

    // ---- internals ----
    private void LoadPrefsToUI()
    {
        float m = PlayerPrefs.GetFloat(PP_MASTER, 1f);
        float mu = PlayerPrefs.GetFloat(PP_MUSIC, 1f);
        float s = PlayerPrefs.GetFloat(PP_SFX, 1f);

        if (masterSlider) masterSlider.SetValueWithoutNotify(m);
        if (musicSlider) musicSlider.SetValueWithoutNotify(mu);
        if (sfxSlider) sfxSlider.SetValueWithoutNotify(s);
    }

    private void ApplyAllToMixer()
    {
        if (masterSlider) SetMixerLinear(masterParam, masterSlider.value);
        if (musicSlider) SetMixerLinear(musicParam, musicSlider.value);
        if (sfxSlider) SetMixerLinear(sfxParam, sfxSlider.value);
    }

    // Convert 0..1 slider to Mixer dB
    private void SetMixerLinear(string exposedParam, float linear01)
    {
        if (!masterMixer || string.IsNullOrEmpty(exposedParam)) return;

        if (Mathf.Approximately(linear01, 0f)) { masterMixer.SetFloat(exposedParam, -80f); return; }
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

    private void UpdateLabel(TextMeshProUGUI label, string name, float v)
    {
        if (!label) return;
        int pct = Mathf.RoundToInt(Mathf.Clamp01(v) * 100f);
        label.text = $"{name} ({pct}%)";
    }
}
