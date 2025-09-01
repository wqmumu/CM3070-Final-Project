using UnityEngine;
using TMPro;
using System.Collections;

public enum GateType { Add, Subtract, Multiply, Divide }

public class Gate : MonoBehaviour
{
    [Header("Gate Settings")]
    public GateType type;
    public int value = 1;
    public TMP_Text label;

    [Header("Visual Feedback")]
    public GameObject gateHitEffect;
    public Material positiveMaterial; // Blue for Add/Multiply
    public Material negativeMaterial; // Red for Subtract/Divide

    [Header("References")]
    public MeshRenderer bannerRenderer; // assign Banner MeshRenderer in Inspector
    public Transform popTarget;         // usually Banner transform (optional; auto-filled)

    [Header("Pop Animation")]
    [SerializeField] float popScale = 1.15f;   // peak scale relative to original
    [SerializeField] float popDuration = 0.12f; // half-cycle (up or down)

    bool triggered = false;
    Coroutine popRoutine;
    Vector3 originalScale;   // cached once and reused, prevents scale creep
    bool scaleCached = false;

    void Start()
    {
        if (popTarget == null && bannerRenderer != null)
            popTarget = bannerRenderer.transform;

        // Cache original scale once
        if (popTarget != null)
        {
            originalScale = popTarget.localScale;
            scaleCached = true;
        }

        UpdateLabel();
    }

    void UpdateLabel()
    {
        switch (type)
        {
            case GateType.Add: label.text = "+" + value; SetBannerMaterial(positiveMaterial); break;
            case GateType.Subtract: label.text = "-" + value; SetBannerMaterial(negativeMaterial); break;
            case GateType.Multiply: label.text = "x" + value; SetBannerMaterial(positiveMaterial); break;
            case GateType.Divide: label.text = "/" + value; SetBannerMaterial(negativeMaterial); break;
        }
    }

    void SetBannerMaterial(Material mat)
    {
        if (bannerRenderer != null && mat != null) bannerRenderer.material = mat;
    }

    public void OnBulletHit(Vector3 hitPosition)
    {
        if (triggered) return;

        // value/type changes
        if (type == GateType.Add || type == GateType.Multiply)
        {
            value++;
        }
        else // Subtract/Divide
        {
            value--;
            if (value <= 0)
            {
                type = (type == GateType.Subtract) ? GateType.Add : GateType.Multiply;
                value = 1;
            }
        }

        UpdateLabel();

        if (gateHitEffect != null)
            Instantiate(gateHitEffect, hitPosition, Quaternion.identity);

        // POP (always from originalScale)
        if (popTarget != null && scaleCached)
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popTarget.localScale = originalScale; // reset before replay -> no accumulation
            popRoutine = StartCoroutine(PopOnce());
        }
    }

    IEnumerator PopOnce()
    {
        // Up
        float t = 0f;
        Vector3 peak = originalScale * popScale;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            popTarget.localScale = Vector3.Lerp(originalScale, peak, t / popDuration);
            yield return null;
        }

        // Down
        t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            popTarget.localScale = Vector3.Lerp(peak, originalScale, t / popDuration);
            yield return null;
        }

        popTarget.localScale = originalScale;
        popRoutine = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        TroopManager manager = FindFirstObjectByType<TroopManager>();
        if (manager != null)
        {
            switch (type)
            {
                case GateType.Add: manager.AddTroops(value); break;
                case GateType.Subtract: manager.RemoveTroops(value); break;
                case GateType.Multiply: manager.MultiplyTroops(value); break;
                case GateType.Divide: manager.DivideTroops(value); break;
            }
        }
        triggered = true;
    }
}
