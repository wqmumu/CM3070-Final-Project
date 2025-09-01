using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthUI : MonoBehaviour
{
    public Slider healthSlider;
    public Canvas hpCanvas; // <-- add this
    private EnemyBase enemy;
    private Camera cam;

    public TextMeshProUGUI hpText;

    void Start()
    {
        enemy = GetComponentInParent<EnemyBase>();
        cam = Camera.main;

        if (hpCanvas == null)
            hpCanvas = GetComponent<Canvas>();
    }

    void Update()
    {
        if (enemy == null || healthSlider == null || hpCanvas == null) return;

        float normalized = enemy.GetHealthNormalized();
        healthSlider.value = Mathf.Lerp(healthSlider.value, normalized, Time.deltaTime * 10f);

        if (hpText != null)
            hpText.text = Mathf.CeilToInt(normalized * 100f) + "%";

        // Hide if full or dead
        hpCanvas.enabled = (normalized > 0f && normalized < 1f);

        // Face camera
        if (cam != null)
            transform.LookAt(transform.position + cam.transform.forward);
    }
}
