using UnityEngine;

public class BossEnemy : EnemyBase
{
    [Header("Attack Settings")]
    public int damagePerHit = 10;
    public float attackRange = 2.5f;
    public float attackInterval = 2f;

    private float attackTimer = 0f;

    protected override void Update()
    {
        base.Update();

        if (isDead || !shouldChase || targetTroop == null)
            return;

        attackTimer += Time.deltaTime;

        float distance = Vector3.Distance(transform.position, targetTroop.position);
        if (distance <= attackRange && attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            Attack();
        }
    }

    private void Attack()
    {
        TroopManager manager = FindFirstObjectByType<TroopManager>();
        if (manager != null)
        {
            manager.RemoveTroops(damagePerHit);
        }

        if (anim != null)
        {
            anim.SetBool("Walking", false);
            anim.SetTrigger("Attacking");
        }
    }
}
