using UnityEngine;

public class EnergyProjectile : MonoBehaviour
{
    public float lifetime = 5f;
    float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (Time.time - spawnTime > lifetime)
            Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Spawn impact flash
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        flash.transform.localScale = Vector3.one * 0.1f;
        Destroy(flash.GetComponent<Collider>());

        Renderer r = flash.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = new Color(0f, 1f, 0.5f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0f, 2f, 1f));
        mat.SetColor("_BaseColor", new Color(0f, 1f, 0.5f));
        r.material = mat;

        flash.AddComponent<ImpactFlash>();

        Destroy(gameObject);
    }
}

public class ImpactFlash : MonoBehaviour
{
    float timer;
    float duration = 0.3f;
    Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = timer / duration;
        transform.localScale = startScale * (1f + t * 5f);

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            Color c = r.material.color;
            c.a = 1f - t;
            r.material.color = c;
        }

        if (timer >= duration)
            Destroy(gameObject);
    }
}
