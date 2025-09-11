using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float speed = 10f;
    private float damage = 10f;

    public void SetSpeed(float newSpeed) => speed = newSpeed;
    public void SetDamage(float newDamage) => damage = newDamage;

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 hitPoint = other.ClosestPoint(transform.position);

        if (TroopManager.CombatEngaged)
        {
            if (other.CompareTag("Boss"))
            {
                var enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    AudioManager.I.PlayAt(SfxId.BulletHitBoss, hitPoint);
                    enemy.ShowHitEffectAt(hitPoint);
                    enemy.TakeDamage(damage);
                }
                Destroy(gameObject);
            }
            else if (other.CompareTag("Zombie"))
            {
                var enemy = other.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    AudioManager.I.PlayAt(SfxId.BulletHitZombie, hitPoint);
                    enemy.ShowHitEffectAt(hitPoint);
                    enemy.TakeDamage(damage);
                }
                Destroy(gameObject);
            }
            return;
        }

        if (other.CompareTag("Gate"))
        {
            var gate = other.GetComponent<Gate>();
            if (gate != null) gate.OnBulletHit(hitPoint);
            AudioManager.I.PlayAt(SfxId.BulletHitGate, hitPoint);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Boss"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                AudioManager.I.PlayAt(SfxId.BulletHitBoss, hitPoint);
                enemy.ShowHitEffectAt(hitPoint);
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Zombie"))
        {
            var enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                AudioManager.I.PlayAt(SfxId.BulletHitZombie, hitPoint);
                enemy.ShowHitEffectAt(hitPoint);
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
