using UnityEngine;

public class Target : MonoBehaviour
{
    public static System.Action<int> onDestroyed;
    public int points = 10;

    void OnCollisionEnter(Collision collision)
    {
        bool hit = false;

        if (collision.gameObject.GetComponent<EnergyProjectile>() != null)
            hit = true;
        else if (collision.gameObject.GetComponent<ThrowableObject>() != null)
            hit = true;

        if (!hit) return;

        if (GameManager.Instance != null)
            GameManager.Instance.OnTargetDestroyed(points);

        onDestroyed?.Invoke(points);

        // Cube burst destruction effect
        for (int i = 0; i < 8; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = transform.position + Random.insideUnitSphere * 0.2f;
            cube.transform.localScale = Vector3.one * 0.08f;

            Renderer r = cube.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.SetColor("_BaseColor", new Color(1f, 0.3f, 0f));
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(2f, 0.5f, 0f));
            r.material = mat;

            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.linearVelocity = Random.insideUnitSphere * 3f;
            rb.mass = 0.1f;

            Destroy(cube, 1.5f);
        }

        gameObject.SetActive(false);
    }
}
