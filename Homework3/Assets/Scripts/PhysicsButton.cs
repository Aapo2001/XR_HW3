using UnityEngine;
using UnityEngine.Events;

public class PhysicsButton : MonoBehaviour
{
    public UnityEvent onPressed = new UnityEvent();
    public Color buttonColor = Color.red;
    public string label = "BUTTON";

    Transform buttonTop;
    Vector3 topRestLocal;
    float pressDepth = 0.015f;
    float lastPressTime;
    float debounce = 0.5f;
    bool isPressed;

    public void Build()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        // Base cylinder
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "ButtonBase";
        baseObj.transform.SetParent(transform);
        baseObj.transform.localPosition = Vector3.zero;
        baseObj.transform.localScale = new Vector3(0.08f, 0.01f, 0.08f);
        Material baseMat = new Material(shader);
        baseMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f));
        baseMat.SetFloat("_Metallic", 0.7f);
        baseObj.GetComponent<Renderer>().material = baseMat;

        // Pushable top
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        top.name = "ButtonTop";
        top.transform.SetParent(transform);
        top.transform.localPosition = new Vector3(0f, 0.015f, 0f);
        top.transform.localScale = new Vector3(0.06f, 0.008f, 0.06f);
        Material topMat = new Material(shader);
        topMat.SetColor("_BaseColor", buttonColor);
        topMat.EnableKeyword("_EMISSION");
        topMat.SetColor("_EmissionColor", buttonColor * 0.5f);
        top.GetComponent<Renderer>().material = topMat;

        buttonTop = top.transform;
        topRestLocal = buttonTop.localPosition;

        // Trigger zone detector
        GameObject triggerZone = new GameObject("ButtonTriggerDetector");
        triggerZone.transform.SetParent(transform);
        triggerZone.transform.localPosition = new Vector3(0f, 0.025f, 0f);
        Rigidbody triggerRb = triggerZone.AddComponent<Rigidbody>();
        triggerRb.isKinematic = true;
        triggerRb.useGravity = false;
        BoxCollider triggerCol = triggerZone.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(0.07f, 0.03f, 0.07f);
        ButtonTriggerDetector detector = triggerZone.AddComponent<ButtonTriggerDetector>();
        detector.button = this;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0f, 0.005f, 0.05f);
        labelObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 24;
        tm.characterSize = 0.01f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
    }

    public void Press()
    {
        if (Time.time - lastPressTime < debounce) return;
        lastPressTime = Time.time;
        onPressed.Invoke();
    }

    public void SetPressed(bool pressed)
    {
        isPressed = pressed;
        if (buttonTop != null)
        {
            Vector3 target = pressed ? topRestLocal - new Vector3(0f, pressDepth, 0f) : topRestLocal;
            buttonTop.localPosition = Vector3.Lerp(buttonTop.localPosition, target, Time.deltaTime * 20f);
        }
    }
}

public class ButtonTriggerDetector : MonoBehaviour
{
    public PhysicsButton button;
    int contactCount;

    bool IsInteractor(Collider other)
    {
        if (other.GetComponentInParent<RobotHand>() != null) return true;
        if (other.attachedRigidbody != null) return true;
        return false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsInteractor(other))
        {
            contactCount++;
            if (contactCount == 1)
            {
                button.SetPressed(true);
                button.Press();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsInteractor(other))
        {
            contactCount = Mathf.Max(0, contactCount - 1);
            if (contactCount == 0)
                button.SetPressed(false);
        }
    }
}
