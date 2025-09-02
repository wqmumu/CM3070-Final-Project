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
    [Tooltip("Chase speed while pursuing the troop")]
    public float chaseSpeed = 3f;
    [Tooltip("Stop slightly inside attackRange so hits connect reliably")]
    public float stopBuffer = 0.25f; // stopDistance = attackRange - stopBuffer

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

    protected TroopManager manager;
    public bool IsAlive => !isDead;

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

        if (TargetInvalid())
        {
            AcquireTarget();
            if (TargetInvalid())
            {
                shouldChase = false;
                Patrol();
                return;
            }
        }

        if (!shouldChase && targetTroop != null)
        {
            float distance = Vector3.Distance(transform.position, targetTroop.position);
            if (distance < chaseRange) shouldChase = true;
        }

        if (shouldChase) Chase();
        else Patrol();
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

    // Resolve each enemy's attackRange without requiring overrides
    protected float ResolveAttackRange()
    {
        var n = GetComponent<NormalEnemy>();
        if (n != null) return n.attackRange;

        var b = GetComponent<BossEnemy>();
        if (b != null) return b.attackRange;

        return 1.5f; // default
    }

    protected virtual void Chase()
    {
        if (TargetInvalid()) return;

        float distance = Vector3.Distance(transform.position, targetTroop.position);

        float attackRange = ResolveAttackRange();
        float stopDistance = Mathf.Max(0.05f, attackRange - stopBuffer);

        bool shouldAdvance = distance > stopDistance;

        if (anim != null) anim.SetBool("Walking", shouldAdvance);

        if (shouldAdvance)
        {
            Vector3 dir = (targetTroop.position - transform.position).normalized;
            transform.position += new Vector3(dir.x, 0f, dir.z) * chaseSpeed * Time.deltaTime;
        }

        Vector3 lookDir = targetTroop.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
    }

    // ----- Target management -----

    void HandleLeaderChanged(Transform t) { targetTroop = t; }

    protected void AcquireTarget()
    {
        targetTroop = manager ? manager.GetLeadTroop() : null;

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

        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

        this.enabled = false;
        Destroy(gameObject, 3f);
    }
}
