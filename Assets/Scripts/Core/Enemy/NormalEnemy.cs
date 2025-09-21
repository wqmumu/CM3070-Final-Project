using UnityEngine;

public class NormalEnemy : EnemyBase
{
    [Header("Zombie Stats")]
    [SerializeField] private int damagePerHit = 1;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackInterval = 1.0f;
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float chaseRange = 50f;
    [SerializeField] private float stopBuffer = 0.25f;

    [Header("Zombie Animator Bindings")]
    [SerializeField] private string walkingBool = "Walking";
    [SerializeField] private string attackTrigger = "Attacking";
    [SerializeField] private string deadTrigger = "Dead";
    [SerializeField] private string attackSpeedParam = "AttackSpeed";
    [SerializeField] private string attackClipName = "ZombieAttack";

    [Header("Zombie Audio")]
    [SerializeField] private SfxId attackSfx = SfxId.ZombieAttack;
    [SerializeField] private SfxId hitSfx = SfxId.BulletHitZombie;
    [SerializeField] private SfxId dieSfx = SfxId.ZombieDie;

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
        damagePerHit = 1;
        attackRange = 1.5f;
        attackInterval = 1.0f;
        patrolSpeed = 3f;
        chaseSpeed = 3f;
        chaseRange = 50f;
        stopBuffer = 0.25f;

        walkingBool = "Walking";
        attackTrigger = "Attacking";
        deadTrigger = "Dead";
        attackSpeedParam = "AttackSpeed";
        attackClipName = "ZombieAttack";

        attackSfx = SfxId.ZombieAttack;
        hitSfx    = SfxId.BulletHitZombie;
        dieSfx    = SfxId.ZombieDie;
    }
#endif
}
