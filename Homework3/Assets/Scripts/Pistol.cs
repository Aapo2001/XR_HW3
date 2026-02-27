using UnityEngine;

public class Pistol : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 100f;
    public float bulletLifeTime = 5f;

    public AudioClip bulletSound;
    private AudioSource _audioSource;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();

        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0f, 0.02f, 0.15f);
            fp.transform.localRotation = Quaternion.identity;
            firePoint = fp.transform;
        }

        if (bulletPrefab == null)
        {
            bulletPrefab = CreateBulletTemplate();
        }
    }

    GameObject CreateBulletTemplate()
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "BulletTemplate";
        bullet.transform.localScale = Vector3.one * 0.03f;

        Rigidbody rb = bullet.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Renderer r = bullet.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.SetColor("_BaseColor", new Color(1f, 0.8f, 0f));
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(3f, 2f, 0f));
        r.material = mat;

        bullet.SetActive(false);
        return bullet;
    }

    public void Fire()
    {
        if (bulletPrefab == null || firePoint == null) return;

        var bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        bullet.SetActive(true);

        var rb = bullet.GetComponent<Rigidbody>();

        // Add EnergyProjectile so targets detect bullet collisions
        var proj = bullet.AddComponent<EnergyProjectile>();
        proj.lifetime = bulletLifeTime;

        if (_audioSource != null && bulletSound != null)
            _audioSource.PlayOneShot(bulletSound);

        if (rb != null)
        {
            rb.linearVelocity = firePoint.forward * bulletSpeed;
        }

        // Track shot for accuracy
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterShot();
    }
}
