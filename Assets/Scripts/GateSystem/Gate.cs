using UnityEngine;
using TMPro;

public enum GateType { Add, Subtract, Multiply, Divide }

public class Gate : MonoBehaviour
{
    [Header("Gate Settings")]
    public GateType type;
    public int value = 1;
    public TMP_Text label;

    [Header("Visual Feedback")]
    public GameObject gateHitEffect;

    [Header("Movement Settings")]
    public bool shouldMove = false;
    public float moveSpeed = 2f;

    private bool triggered = false;
    private bool movingRight = true;

    private void Start()
    {
        UpdateLabel();
    }

    private void Update()
    {
        if (!shouldMove) return;

        float moveStep = moveSpeed * Time.deltaTime;
        Vector3 direction = movingRight ? Vector3.right : Vector3.left;

        transform.Translate(direction * moveStep);
    }

    private void UpdateLabel()
    {
        switch (type)
        {
            case GateType.Add:
                label.text = "+" + value;
                break;
            case GateType.Subtract:
                label.text = "-" + value;
                break;
            case GateType.Multiply:
                label.text = "x" + value;
                break;
            case GateType.Divide:
                label.text = "/" + value;
                break;
        }
    }

    // ✅ Flip direction when hitting a wall
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            movingRight = !movingRight;
        }
    }

    public void OnBulletHit(Vector3 hitPosition)
    {
        if (triggered) return;

        if (type == GateType.Add || type == GateType.Multiply)
        {
            value++;
        }
        else if (type == GateType.Subtract || type == GateType.Divide)
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
        {
            Instantiate(gateHitEffect, hitPosition, Quaternion.identity);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        TroopManager manager = FindFirstObjectByType<TroopManager>();
        if (manager != null)
        {
            switch (type)
            {
                case GateType.Add:
                    manager.AddTroops(value);
                    break;
                case GateType.Subtract:
                    manager.RemoveTroops(value);
                    break;
                case GateType.Multiply:
                    manager.MultiplyTroops(value);
                    break;
                case GateType.Divide:
                    manager.DivideTroops(value);
                    break;
            }
        }

        triggered = true;
    }
}
