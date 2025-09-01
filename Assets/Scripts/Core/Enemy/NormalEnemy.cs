using UnityEngine;

public class NormalEnemy : EnemyBase
{
    [Header("Attack Settings")]
    public int damagePerHit = 1;
    public float attackRange = 1.5f;
    public float attackInterval = 1f;

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
