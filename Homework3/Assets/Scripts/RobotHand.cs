using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class RobotHand : MonoBehaviour
{
    public Transform palmEmitter;
    public float GripValue { get; private set; }
    public float TriggerValue { get; private set; }
    public bool SecondaryButtonPressed { get; private set; }

    InputDevice device;
    bool isLeft;
    List<Collider> handColliders = new List<Collider>();

    void Start()
    {
        isLeft = gameObject.name.ToLower().Contains("left");
        SetupPalmEmitter();
        CollectChildColliders();
    }

    void SetupPalmEmitter()
    {
        GameObject emitterObj = new GameObject("PalmEmitter");
        emitterObj.transform.SetParent(transform);
        emitterObj.transform.localPosition = new Vector3(0f, -0.02f, 0.05f);
        emitterObj.transform.localRotation = Quaternion.identity;
        palmEmitter = emitterObj.transform;
    }

    void CollectChildColliders()
    {
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider col in cols)
        {
            if (col != null)
                handColliders.Add(col);
        }
    }

    void Update()
    {
        if (!device.isValid)
        {
            var characteristics = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand;
            characteristics |= isLeft ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right;

            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            if (devices.Count > 0)
                device = devices[0];
            else
                return;
        }

        device.TryGetFeatureValue(CommonUsages.grip, out float grip);
        device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
        device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryPressed);
        GripValue = grip;
        TriggerValue = trigger;
        SecondaryButtonPressed = secondaryPressed;
    }

    public List<Collider> GetHandColliders()
    {
        return handColliders;
    }
}
