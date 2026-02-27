using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingCubes : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0, Time.deltaTime*5, 0);
        for(int i=0; i<transform.childCount; i++)
        {
            transform.GetChild(i).Rotate(Time.deltaTime * 10, Time.deltaTime * 20, Time.deltaTime * 30, Space.World);
            transform.GetChild(i).localPosition *= 1.0f + Mathf.Sin(Time.time*0.2f) * 0.001f;
        }
    }
}
