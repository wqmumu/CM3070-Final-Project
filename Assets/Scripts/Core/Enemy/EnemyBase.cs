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
    [Tooltip("Begin chasing when troop is within this distance.")]
    public float chaseRange = 50f;
    [Tooltip("Chase speed while pursuing the troop")]
    public float chaseSpeed = 3f;
    [Tooltip("Stop a bit inside attackRange so hits connect reliably.")]
    public float stopBuffer = 0.25f;

    [Header("Attack Settings (per-enemy unique)")]
    [Tooltip("How many troops to remove each successful hit.")]
    public int damagePerHit = 1;
    [Tooltip("Distance at which this enemy is allowed to attack.")]
    public float attackRange = 1.5f;
    [Tooltip("How often this enemy can attack (seconds per attack).")]
    public float attackInterval = 1f;

    [Header("Animator Hookups (names must match your Animator)")]
    [SerializeField] private string walkingBool = "Walking";
    [SerializeField] private string attackTrigger = "Attacking";
    [SerializeField] private string deadTrigger = "Dead";
    [Tooltip("Float parameter used as Speed Multiplier on the Attack state.")]
    [SerializeField] private string attackSpeedParam = "AttackSpeed";
    [Tooltip("Exact name of the attack clip used in this animator (for length lookup).")]
    [SerializeField] private string attackClipName = "BossAttack";

    [Header("Audio (per-enemy unique)")]
    [SerializeField] private SfxId attackSfx = SfxId.ZombieAttack;
    [SerializeField] private SfxId dieSfx = SfxId.ZombieDie;

    [Header("Visual Feedback")]
    public SkinnedMeshRenderer meshRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;
    private Color originalColor;

    [Header("Hit Effect (spawned where bullets hit)")]
    [SerializeField] private GameObject hitEffectPrefab;

    protected Transform targetTroop;
    protected bool shouldChase = false;
    protected TroopManager manager;
    public Animator anim;

    private float attackTimer = 0f;
    private bool loggedMissingAttackClip = false;

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

        if (!anim)
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

        if (shouldChase)
        {
            Chase();
            HandleAttack();
        }
        else
        {
            Patrol();
        }
    }

    // ---------- Attack ----------
    private void HandleAttack()
    {
        if (targetTroop == null) return;

        attackTimer += Time.deltaTime;

        float distance = Vector3.Distance(transform.position, targetTroop.position);
        if (distance <= attackRange && attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            PerformAttack();
        }
    }

    protected virtual void PerformAttack()
    {
        // Apply damage
        if (manager != null)
            manager.RemoveTroops(damagePerHit);

        // Animate
        if (anim != null)
        {
            if (!string.IsNullOrEmpty(walkingBool))
                anim.SetBool(walkingBool, false);

            // Sync attack animation speed to attackInterval
            ApplyAttackSpeedMultiplier();

            if (!string.IsNullOrEmpty(attackTrigger))
                anim.SetTrigger(attackTrigger);
        }

        // SFX
        if (AudioManager.I != null)
            AudioManager.I.PlayAt(attackSfx, transform.position);
    }

    private void ApplyAttackSpeedMultiplier()
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;
        if (string.IsNullOrEmpty(attackSpeedParam) || string.IsNullOrEmpty(attackClipName)) return;

        var clips = anim.runtimeAnimatorController.animationClips;
        AnimationClip attackClip = null;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == attackClipName)
            {
                attackClip = clips[i];
                break;
            }
        }

        if (attackClip == null)
        {
            if (!loggedMissingAttackClip)
            {
                Debug.LogWarning($"[{name}] EnemyBase: Attack clip '{attackClipName}' not found on Animator '{anim.runtimeAnimatorController.name}'. " +
                                 $"Attack will still play, but speed won’t be synced.");
                loggedMissingAttackClip = true;
            }
            return;
        }

        float targetDur = Mathf.Max(0.01f, attackInterval);
        float speed = attackClip.length / targetDur;  // so state duration == attackInterval
        anim.SetFloat(attackSpeedParam, speed);
    }

    // ---------- Movement ----------
    private void Patrol()
    {
        if (anim != null && !string.IsNullOrEmpty(walkingBool))
            anim.SetBool(walkingBool, true);

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

    private void SetNewTarget()
    {
        float newX = Random.Range(patrolBounds.x, patrolBounds.y);
        targetPos = new Vector3(newX, transform.position.y, transform.position.z);
    }

    protected virtual void Chase()
    {
        if (TargetInvalid()) return;

        float distance = Vector3.Distance(transform.position, targetTroop.position);
        float stopDistance = Mathf.Max(0.05f, attackRange - stopBuffer);

        bool shouldAdvance = distance > stopDistance;

        if (anim != null && !string.IsNullOrEmpty(walkingBool))
            anim.SetBool(walkingBool, shouldAdvance);

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

    // ---------- Target ----------
    private void HandleLeaderChanged(Transform t) { targetTroop = t; }

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

    // ---------- Damage & VFX ----------
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

        if (AudioManager.I != null)
            AudioManager.I.PlayAt(dieSfx, transform.position);

        if (anim != null)
        {
            if (!string.IsNullOrEmpty(walkingBool))
                anim.SetBool(walkingBool, false);
            if (!string.IsNullOrEmpty(deadTrigger))
                anim.SetTrigger(deadTrigger);
        }

        shouldChase = false;

        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

        this.enabled = false;
        Destroy(gameObject, 3f);
    }

    // ---------- Editor Safety ----------
    private void OnValidate()
    {
        maxHP = Mathf.Max(1f, maxHP);
        patrolSpeed = Mathf.Max(0f, patrolSpeed);
        chaseSpeed = Mathf.Max(0f, chaseSpeed);
        stopBuffer = Mathf.Clamp(stopBuffer, 0f, 10f);

        damagePerHit = Mathf.Max(0, damagePerHit);
        attackRange = Mathf.Max(0.01f, attackRange);
        attackInterval = Mathf.Max(0.05f, attackInterval);
    }
}
