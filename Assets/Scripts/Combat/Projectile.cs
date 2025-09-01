using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float speed = 10f;
    private float damage = 10f;

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 hitPoint = transform.position; // Approximate hit point

        if (other.CompareTag("Gate"))
        {
            Gate gate = other.GetComponent<Gate>();
            if (gate != null)
                gate.OnBulletHit(hitPoint);

            Destroy(gameObject);
        }
        else if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
                enemy.ShowHitEffectAt(hitPoint);
            enemy.TakeDamage(damage);

            Destroy(gameObject);
        }
    }

}
