using UnityEngine;
using System.Collections;

public abstract class EnemyBase : MonoBehaviour
{
    // ===== Shared core =====
    [Header("Health")]
    [SerializeField] private float maxHP = 100f;
    protected float currentHP;
    protected bool isDead;

    // Public API expected by other scripts
    public bool IsAlive => !isDead;

    [Header("Targeting / Movement")]
    [SerializeField] private Vector2 patrolBounds = new Vector2(-13f, 13f);
    protected Vector3 targetPos;
    protected Transform targetTroop;
    protected bool shouldChase;
    protected TroopManager manager;
    protected Animator anim;

    private float attackTimer;
    private bool loggedMissingAttackClip;

    // ===== Per-enemy overrides =====
    // Stats
    protected abstract int DamagePerHit { get; }
    protected abstract float AttackRange { get; }
    protected abstract float AttackInterval { get; }
    protected abstract float PatrolSpeed { get; }
    protected abstract float ChaseSpeed { get; }
    protected abstract float ChaseRange { get; }
    protected abstract float StopBuffer { get; }

    // Animator
    protected abstract string WalkingBool { get; }
    protected abstract string AttackTrigger { get; }
    protected abstract string DeadTrigger { get; }
    protected abstract string AttackSpeedParam { get; }
    protected abstract string AttackClipName { get; }

    // Audio
    protected abstract SfxId AttackSfx { get; }
    protected abstract SfxId HitSfx { get; }
    protected abstract SfxId DieSfx { get; }

    // VFX (shared fields)
    [Header("Hit VFX")]
    [SerializeField] protected SkinnedMeshRenderer meshRenderer;
    [SerializeField] protected Color flashColor = Color.red;
    [SerializeField] protected float flashDuration = 0.1f;
    [SerializeField] protected GameObject hitEffectPrefab;
    private Color _originalColor;

    // ===== Lifecycle =====
    protected virtual void Awake()
    {
        manager = FindFirstObjectByType<TroopManager>();
        anim = GetComponentInChildren<Animator>();
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
        currentHP = Mathf.Max(1f, maxHP);
        SetNewTarget();
        if (meshRenderer) _originalColor = meshRenderer.material.color;
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

        if (!shouldChase && targetTroop)
        {
            if (Vector3.Distance(transform.position, targetTroop.position) < ChaseRange)
                shouldChase = true;
        }

        if (shouldChase) { Chase(); HandleAttack(); }
        else { Patrol(); }
    }

    // ===== Movement =====
    private void Patrol()
    {
        if (anim && !string.IsNullOrEmpty(WalkingBool))
            anim.SetBool(WalkingBool, true);

        transform.position = Vector3.MoveTowards(transform.position, targetPos, PatrolSpeed * Time.deltaTime);

        var dir = targetPos - transform.position;
        if (Mathf.Abs(dir.x) > 0.0001f)
        {
            var look = new Vector3(dir.x, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), 5f * Time.deltaTime);
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
        float stopDistance = Mathf.Max(0.05f, AttackRange - StopBuffer);
        bool shouldAdvance = distance > stopDistance;

        if (anim && !string.IsNullOrEmpty(WalkingBool))
            anim.SetBool(WalkingBool, shouldAdvance);

        if (shouldAdvance)
        {
            Vector3 dir = (targetTroop.position - transform.position).normalized;
            transform.position += new Vector3(dir.x, 0f, dir.z) * ChaseSpeed * Time.deltaTime;
        }

        Vector3 lookDir = targetTroop.position - transform.position; lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 5f * Time.deltaTime);
    }

    // ===== Targeting =====
    private void HandleLeaderChanged(Transform t) => targetTroop = t;

    protected void AcquireTarget()
    {
        targetTroop = manager ? manager.GetLeadTroop() : null;
        if (targetTroop) return;

        // fallback: nearest Player
        var troops = GameObject.FindGameObjectsWithTag("Player");
        float closest = Mathf.Infinity; Transform best = null;
        foreach (var go in troops)
        {
            if (!go || !go.activeInHierarchy) continue;
            var unit = go.GetComponent<TroopUnit>();
            if (unit != null && unit.IsDying) continue;

            float d = Vector3.Distance(transform.position, go.transform.position);
            if (d < closest) { closest = d; best = go.transform; }
        }
        targetTroop = best;
    }

    protected bool TargetInvalid()
    {
        if (!targetTroop || !targetTroop.gameObject.activeInHierarchy) return true;
        var u = targetTroop.GetComponent<TroopUnit>();
        return (u != null && u.IsDying);
    }

    // ===== Attacking =====
    private void HandleAttack()
    {
        if (!targetTroop) return;
        attackTimer += Time.deltaTime;

        if (Vector3.Distance(transform.position, targetTroop.position) <= AttackRange
            && attackTimer >= AttackInterval)
        {
            attackTimer = 0f;
            PerformAttack();
        }
    }

    protected virtual void PerformAttack()
    {
        if (manager) manager.RemoveTroops(DamagePerHit);

        if (anim)
        {
            if (!string.IsNullOrEmpty(WalkingBool)) anim.SetBool(WalkingBool, false);
            ApplyAttackSpeedMultiplier();
            if (!string.IsNullOrEmpty(AttackTrigger)) anim.SetTrigger(AttackTrigger);
        }

        if (AudioManager.I != null)
            AudioManager.I.PlayAt(AttackSfx, transform.position);
    }

    private void ApplyAttackSpeedMultiplier()
    {
        if (!anim || anim.runtimeAnimatorController == null) return;
        if (string.IsNullOrEmpty(AttackSpeedParam) || string.IsNullOrEmpty(AttackClipName)) return;

        var clips = anim.runtimeAnimatorController.animationClips;
        AnimationClip attackClip = null;
        for (int i = 0; i < clips.Length; i++)
            if (clips[i] && clips[i].name == AttackClipName) { attackClip = clips[i]; break; }

        if (!attackClip)
        {
            if (!loggedMissingAttackClip)
            {
                Debug.LogWarning($"[{name}] Attack clip '{AttackClipName}' not found on '{anim.runtimeAnimatorController.name}'.");
                loggedMissingAttackClip = true;
            }
            return;
        }

        float targetDur = Mathf.Max(0.01f, AttackInterval);
        float speed = attackClip.length / targetDur;
        anim.SetFloat(AttackSpeedParam, speed);
    }

    // ===== Damage / VFX =====
    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHP -= amount;

        if (AudioManager.I != null)
            AudioManager.I.PlayAt(HitSfx, transform.position);

        if (meshRenderer) StartCoroutine(FlashEffect());
        if (currentHP <= 0f) Die();
    }

    public void TakeDamage(float amount, Vector3 hitPosition)
    {
        if (isDead) return;
        currentHP -= amount;

        if (AudioManager.I != null)
            AudioManager.I.PlayAt(HitSfx, hitPosition);

        ShowHitEffectAt(hitPosition); 
        if (meshRenderer) StartCoroutine(FlashEffect());
        if (currentHP <= 0f) Die();
    }

    protected IEnumerator FlashEffect()
    {
        if (!meshRenderer) yield break;
        var mat = meshRenderer.material;
        if (!mat.HasProperty("_Color")) yield break;

        mat.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        mat.color = _originalColor;
    }

    // RESTORED for Projectile.cs and others
    public void ShowHitEffectAt(Vector3 hitPosition) 
    {
        if (hitEffectPrefab) Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
    }

    public float GetHealthNormalized() => currentHP / Mathf.Max(1f, maxHP);

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        if (AudioManager.I != null)
        {
            // Use the important path so death can’t be masked by hit spam
            AudioManager.I.PlayAtImportant(DieSfx, transform.position, reinforce2D: true);
        }

        if (anim)
        {
            if (!string.IsNullOrEmpty(WalkingBool)) anim.SetBool(WalkingBool, false);
            if (!string.IsNullOrEmpty(DeadTrigger)) anim.SetTrigger(DeadTrigger);
        }

        shouldChase = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        enabled = false;
        Destroy(gameObject, 3f);
    }

}
