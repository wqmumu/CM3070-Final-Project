using UnityEngine;
using System.Collections;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP = 100f;
    protected float currentHP;
    protected bool isDead = false;

    [Header("Patrol Settings")]
    public float patrolSpeed = 3f;
    public Vector2 patrolBounds = new Vector2(-13f, 13f);
    protected Vector3 targetPos;

    [Header("Chase Settings")]
    public float chaseRange = 50f;
    protected Transform targetTroop;
    protected bool shouldChase = false;

    [Header("Visual Feedback")]
    public SkinnedMeshRenderer meshRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;
    private Color originalColor;

    [Header("Animation")]
    public Animator anim;

    [Header("Hit Effect")]
    [SerializeField] private GameObject hitEffectPrefab;

    // --- New: leader awareness
    protected TroopManager manager;

    protected virtual void Awake()
    {
        manager = FindFirstObjectByType<TroopManager>();
    }

    protected virtual void OnEnable()
    {
        TroopManager.OnLeaderChanged += HandleLeaderChanged;
        AcquireTarget();
    }

    protected virtual void OnDisable()
    {
        TroopManager.OnLeaderChanged -= HandleLeaderChanged;
    }

    protected virtual void Start()
    {
        currentHP = maxHP;
        SetNewTarget();

        if (meshRenderer != null)
            originalColor = meshRenderer.material.color;

        if (anim == null)
            anim = GetComponentInChildren<Animator>();
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // Ensure target is valid or reacquire
        if (TargetInvalid())
        {
            AcquireTarget();
            if (TargetInvalid())
            {
                // No valid target -> patrol only
                shouldChase = false;
                Patrol();
                return;
            }
        }

        if (!shouldChase && targetTroop != null)
        {
            float distance = Vector3.Distance(transform.position, targetTroop.position);
            if (distance < chaseRange)
                shouldChase = true;
        }

        if (shouldChase)
            Chase();
        else
            Patrol();
    }

    // ----- Movement -----

    void Patrol()
    {
        if (anim != null) anim.SetBool("Walking", true);

        transform.position = Vector3.MoveTowards(transform.position, targetPos, patrolSpeed * Time.deltaTime);

        Vector3 direction = targetPos - transform.position;
        if (direction.x != 0)
        {
            Vector3 lookDir = new Vector3(direction.x, 0f, 0f);
            if (lookDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
        }

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            SetNewTarget();
    }

    void SetNewTarget()
    {
        float newX = Random.Range(patrolBounds.x, patrolBounds.y);
        targetPos = new Vector3(newX, transform.position.y, transform.position.z);
    }

    protected virtual void Chase()
    {
        float chaseSpeed = 3f;

        if (TargetInvalid()) return;

        if (anim != null) anim.SetBool("Walking", true);

        Vector3 dir = (targetTroop.position - transform.position).normalized;
        transform.position += new Vector3(dir.x, 0f, dir.z) * chaseSpeed * Time.deltaTime;

        Vector3 lookDir = targetTroop.position - transform.position;
        lookDir.y = 0f;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
    }

    // ----- Target management -----

    void HandleLeaderChanged(Transform t)
    {
        targetTroop = t; // switch immediately to the new leader (can be null)
    }

    protected void AcquireTarget()
    {
        targetTroop = manager ? manager.GetLeadTroop() : null;

        // Fallback: closest alive troop if leader is null
        if (targetTroop == null)
        {
            GameObject[] troops = GameObject.FindGameObjectsWithTag("Player");
            float closest = Mathf.Infinity;
            Transform best = null;

            foreach (GameObject troop in troops)
            {
                if (!troop || !troop.activeInHierarchy) continue;
                var unit = troop.GetComponent<TroopUnit>();
                if (unit != null && unit.IsDying) continue;

                float d = Vector3.Distance(transform.position, troop.transform.position);
                if (d < closest) { closest = d; best = troop.transform; }
            }
            targetTroop = best;
        }
    }

    protected bool TargetInvalid()
    {
        if (targetTroop == null) return true;
        if (!targetTroop.gameObject.activeInHierarchy) return true;

        var u = targetTroop.GetComponent<TroopUnit>();
        if (u != null && u.IsDying) return true;

        return false;
    }

    // ----- Damage & VFX -----

    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHP -= amount;

        if (meshRenderer != null)
            StartCoroutine(FlashEffect());

        if (currentHP <= 0) Die();
    }

    protected IEnumerator FlashEffect()
    {
        if (meshRenderer == null) yield break;

        Material mat = meshRenderer.material;
        if (!mat.HasProperty("_Color")) yield break;

        mat.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        mat.color = originalColor;
    }

    public void ShowHitEffectAt(Vector3 hitPosition)
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
    }

    public float GetHealthNormalized() => currentHP / maxHP;
    public void ActivateChase() => shouldChase = true;
    public void SetTarget(Transform target) => targetTroop = target;

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        if (anim != null)
        {
            anim.SetBool("Walking", false);
            anim.SetTrigger("Dead");
        }

        shouldChase = false;
        this.enabled = false;
        Destroy(gameObject, 3f);
    }
}
