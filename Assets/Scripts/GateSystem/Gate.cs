using UnityEngine;
using TMPro;
using System.Collections;
using System;

public enum GateType { Add, Subtract, Multiply, Divide }

public class Gate : MonoBehaviour
{
    // Raised when a gate in a pair is triggered. Carries the pairId set by LevelGenerator.
    public static Action<int> GateTriggered;

    [Header("Gate Settings")]
    public GateType type;
    public int value = 1;           // For Subtract, this is the ABS magnitude shown as "-value"
    public TMP_Text label;

    [Header("Visual Feedback")]
    public GameObject gateHitEffect;
    public Material positiveMaterial; // Blue for Add/Multiply
    public Material negativeMaterial; // Red for Subtract/Divide
    public Material disabledMaterial; // Optional: to gray-out when disabled

    [Header("References")]
    public MeshRenderer bannerRenderer; // assign Banner MeshRenderer in Inspector
    public Transform popTarget;         // usually Banner transform (optional; auto-filled)

    [Header("Pop Animation")]
    [SerializeField] float popScale = 1.15f;
    [SerializeField] float popDuration = 0.12f;

    // sibling link so only ONE gate in the pair can trigger
    private Gate siblingGate;

    // Assigned by LevelGenerator so it knows which pair was passed
    [NonSerialized] public int pairId = -1;

    bool triggered = false;
    bool disabledGate = false;
    Coroutine popRoutine;
    Vector3 originalScale;
    bool scaleCached = false;

    void Start()
    {
        if (popTarget == null && bannerRenderer != null)
            popTarget = bannerRenderer.transform;

        if (popTarget != null)
        {
            originalScale = popTarget.localScale;
            scaleCached = true;
        }

        UpdateLabel();
    }

    // Link two gates as siblings (called by LevelGenerator after spawn)
    public void LinkSibling(Gate sibling)
    {
        siblingGate = sibling;
    }

    // External/config helper
    public void Configure(GateType newType, int newValue)
    {
        type = newType;
        value = Mathf.Max(1, newValue); // keep it sane
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (disabledGate)
        {
            if (bannerRenderer != null && disabledMaterial != null)
                bannerRenderer.material = disabledMaterial;
            return;
        }

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

    // Bullet impact rules:
    // - Add: value++
    // - Subtract: count up towards positive, skipping 0 (−3→−2→−1→+1→+2…)
    // - Multiply / Divide: NO change
    public void OnBulletHit(Vector3 hitPosition)
    {
        if (triggered || disabledGate) return;

        if (type == GateType.Add)
        {
            value++;
        }
        else if (type == GateType.Subtract)
        {
            if (value > 1)
            {
                // e.g., -3 -> -2
                value--;
            }
            else
            {
                // value == 1 (i.e., "-1") -> becomes "+1"
                type = GateType.Add;
                value = 1;
            }
        }
        // Multiply / Divide: do nothing on bullet

        UpdateLabel();

        if (gateHitEffect != null)
            Instantiate(gateHitEffect, hitPosition, Quaternion.identity);

        if (popTarget != null && scaleCached)
        {
            if (popRoutine != null) StopCoroutine(popRoutine);
            popTarget.localScale = originalScale;
            popRoutine = StartCoroutine(PopOnce());
        }
    }

    IEnumerator PopOnce()
    {
        float t = 0f;
        Vector3 peak = originalScale * popScale;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            popTarget.localScale = Vector3.Lerp(originalScale, peak, t / popDuration);
            yield return null;
        }

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
        if (triggered || disabledGate || !other.CompareTag("Player")) return;

        // Immediately block re-entry while we process
        triggered = true;

        // Apply effect
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

        // Disable sibling so only ONE gate of the pair can ever activate
        if (siblingGate != null)
            siblingGate.Deactivate();

        // Tell LevelGenerator this pair was passed so it can spawn the next pair
        GateTriggered?.Invoke(pairId);

        // Disable self collider to avoid duplicate triggers on the same pass
        DisableColliderOnly();
    }

    public void Deactivate()
    {
        disabledGate = true;
        DisableColliderOnly();
        UpdateLabel(); // refresh to disabled material if assigned
    }

    void DisableColliderOnly()
    {
        foreach (var col in GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }
}
