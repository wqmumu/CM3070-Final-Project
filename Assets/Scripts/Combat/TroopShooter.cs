using UnityEngine;

public class TroopShooter : MonoBehaviour
{
    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1f;
    public float bulletSpeed = 10f;
    public float bulletDamage = 10f;

    private float fireTimer;
    private TroopUnit unit;
    private bool canShoot = true;

    void Awake()
    {
        unit = GetComponent<TroopUnit>();
    }

    void OnEnable()
    {
        fireTimer = 0f;
        canShoot = true;
    }

    void Update()
    {
        // stop all shooting if disabled or troop is dying
        if (!canShoot) return;
        if (unit != null && unit.IsDying)
        {
            OnOwnerDied();
            return;
        }

        fireTimer += Time.deltaTime;
        if (fireTimer >= 1f / Mathf.Max(0.0001f, fireRate))
        {
            FireBullet();
            fireTimer = 0f;
        }
    }

    private void FireBullet()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        Projectile projectile = bullet.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.SetSpeed(bulletSpeed);
            projectile.SetDamage(bulletDamage);
        }
    }

    // called when the owning troop dies
    public void OnOwnerDied()
    {
        canShoot = false;
        StopAllCoroutines();
        CancelInvoke();
        enabled = false; // belt & suspenders
    }
}
