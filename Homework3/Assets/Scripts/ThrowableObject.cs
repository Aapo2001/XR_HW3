using UnityEngine;

public class ThrowableObject : MonoBehaviour
{
    Rigidbody rb;
    public bool isHeld;
    public Vector3 grabOffset = Vector3.forward * 0.1f;
    public Vector3 grabRotationEuler = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
    }

    public void Grab(Transform hand)
    {
        isHeld = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        transform.SetParent(hand);
        transform.localPosition = grabOffset;
        transform.localRotation = Quaternion.Euler(grabRotationEuler);
    }

    public void Release(Vector3 throwVelocity)
    {
        isHeld = false;
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.linearVelocity = throwVelocity;
    }
}
