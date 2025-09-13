using UnityEngine;

public class BossEnemy : EnemyBase
{
    [Header("Boss Stats")]
    [SerializeField] private int damagePerHit = 10;
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackInterval = 2.0f;
    [SerializeField] private float patrolSpeed = 5f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float chaseRange = 50f;
    [SerializeField] private float stopBuffer = 0.0f;

    [Header("Boss Animator Bindings")]
    [SerializeField] private string walkingBool = "Walking";
    [SerializeField] private string attackTrigger = "Attacking";
    [SerializeField] private string deadTrigger = "Dead";
    [SerializeField] private string attackSpeedParam = "AttackSpeed";
    [SerializeField] private string attackClipName = "BossAttack";

    [Header("Boss Audio")]
    [SerializeField] private SfxId attackSfx = SfxId.BossAttack;
    [SerializeField] private SfxId hitSfx = SfxId.BulletHitBoss;   // short impact
    [SerializeField] private SfxId dieSfx = SfxId.BossDie;

    // ---- overrides ----
    protected override int DamagePerHit => damagePerHit;
    protected override float AttackRange => attackRange;
    protected override float AttackInterval => attackInterval;
    protected override float PatrolSpeed => patrolSpeed;
    protected override float ChaseSpeed => chaseSpeed;
    protected override float ChaseRange => chaseRange;
    protected override float StopBuffer => stopBuffer;

    protected override string WalkingBool => walkingBool;
    protected override string AttackTrigger => attackTrigger;
    protected override string DeadTrigger => deadTrigger;
    protected override string AttackSpeedParam => attackSpeedParam;
    protected override string AttackClipName => attackClipName;

    protected override SfxId AttackSfx => attackSfx;
    protected override SfxId HitSfx => hitSfx;
    protected override SfxId DieSfx => dieSfx;

#if UNITY_EDITOR
    private void Reset()
    {
        damagePerHit = 10;
        attackRange = 2.5f;
        attackInterval = 2.0f;
        patrolSpeed = 5f;
        chaseSpeed = 5f;
        chaseRange = 50f;
        stopBuffer = 0f;

        walkingBool = "Walking";
        attackTrigger = "Attacking";
        deadTrigger = "Dead";
        attackSpeedParam = "AttackSpeed";
        attackClipName = "BossAttack";

        attackSfx = SfxId.BossAttack;
        hitSfx    = SfxId.BulletHitBoss;
        dieSfx    = SfxId.BossDie;
    }
#endif
}
