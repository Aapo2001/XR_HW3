using UnityEngine;

public class GrabThrow : MonoBehaviour
{
    public float grabRadius = 0.15f;
    public float gripThreshold = 0.7f;
    public float triggerThreshold = 0.7f;

    RobotHand hand;
    ThrowableObject heldObject;
    bool wasGripDown;
    bool wasTriggerDown;

    // Velocity tracking circular buffer
    Vector3[] positionSamples = new Vector3[10];
    float[] timeSamples = new float[10];
    int sampleIndex;

    void Start()
    {
        hand = GetComponent<RobotHand>();
    }

    void Update()
    {
        if (hand == null) return;

        bool gripDown = hand.GripValue > gripThreshold;

        if (gripDown && !wasGripDown)
            TryGrab();
        else if (!gripDown && wasGripDown && heldObject != null)
            Release();

        wasGripDown = gripDown;

        if (heldObject != null)
        {
            // Track position for throw velocity
            positionSamples[sampleIndex % positionSamples.Length] = transform.position;
            timeSamples[sampleIndex % timeSamples.Length] = Time.time;
            sampleIndex++;

            // Fire weapon with trigger while holding
            bool triggerDown = hand.TriggerValue > triggerThreshold;
            if (triggerDown && !wasTriggerDown)
            {
                Pistol pistol = heldObject.GetComponent<Pistol>();
                if (pistol != null)
                    pistol.Fire();
            }
            wasTriggerDown = triggerDown;
        }
        else
        {
            wasTriggerDown = false;
        }
    }

    void TryGrab()
    {
        if (hand.palmEmitter == null) return;

        Collider[] hits = Physics.OverlapSphere(hand.palmEmitter.position, grabRadius);
        ThrowableObject closest = null;
        float closestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            ThrowableObject obj = hit.GetComponent<ThrowableObject>();
            if (obj == null)
                obj = hit.GetComponentInParent<ThrowableObject>();
            if (obj != null && !obj.isHeld)
            {
                float dist = Vector3.Distance(hand.palmEmitter.position, obj.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = obj;
                }
            }
        }

        if (closest != null)
        {
            heldObject = closest;
            heldObject.Grab(transform);
            sampleIndex = 0;
        }
    }

    void Release()
    {
        Vector3 velocity = CalculateThrowVelocity();
        heldObject.Release(velocity);
        heldObject = null;
    }

    Vector3 CalculateThrowVelocity()
    {
        int count = Mathf.Min(sampleIndex, positionSamples.Length);
        if (count < 2) return Vector3.zero;

        int newest = (sampleIndex - 1) % positionSamples.Length;
        int oldest = (sampleIndex - count) % positionSamples.Length;
        if (oldest < 0) oldest += positionSamples.Length;

        float dt = timeSamples[newest] - timeSamples[oldest];
        if (dt <= 0f) return Vector3.zero;

        return (positionSamples[newest] - positionSamples[oldest]) / dt;
    }
}
