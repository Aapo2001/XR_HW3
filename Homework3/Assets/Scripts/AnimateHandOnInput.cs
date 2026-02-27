using UnityEngine;
using UnityEngine.InputSystem;

public class AnimateHandOnInput : MonoBehaviour
{
    [SerializeField] InputActionReference gripActionRef;
    [SerializeField] InputActionReference triggerActionRef;

    Animator animator;
    static readonly int GripHash = Animator.StringToHash("Grip");
    static readonly int TriggerHash = Animator.StringToHash("Trigger");

    void Awake()
    {
        animator = GetComponent<Animator>();

        // Auto-detect from XRI Default Input Actions if not assigned
        if (gripActionRef == null || triggerActionRef == null)
            FindXRIActions();
    }

    void FindXRIActions()
    {
        // Determine handedness from parent hierarchy name
        bool isLeft = transform.parent != null &&
                      transform.parent.name.ToLower().Contains("left");
        string mapName = isLeft ? "XRI Left Interaction" : "XRI Right Interaction";

        var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        foreach (var asset in assets)
        {
            var map = asset.FindActionMap(mapName);
            if (map == null) continue;

            if (gripActionRef == null)
            {
                var selectAction = map.FindAction("Select");
                if (selectAction != null)
                    gripActionRef = InputActionReference.Create(selectAction);
            }
            if (triggerActionRef == null)
            {
                var activateAction = map.FindAction("Activate");
                if (activateAction != null)
                    triggerActionRef = InputActionReference.Create(activateAction);
            }
            break;
        }
    }

    void Update()
    {
        if (animator == null) return;

        float grip = gripActionRef != null && gripActionRef.action != null
            ? gripActionRef.action.ReadValue<float>() : 0f;
        float trigger = triggerActionRef != null && triggerActionRef.action != null
            ? triggerActionRef.action.ReadValue<float>() : 0f;

        animator.SetFloat(GripHash, grip);
        animator.SetFloat(TriggerHash, trigger);
    }
}
