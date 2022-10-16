using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TransformData
{
    public float[] position;
    public float[] rotation;

    public TransformData(Transform transform)
    {
        position = new float[3];
        rotation = new float[3];

        position[0] = transform.position.x;
        position[1] = transform.position.y;
        position[2] = transform.position.z;

        rotation[0] = transform.rotation.eulerAngles.x;
        rotation[1] = transform.rotation.eulerAngles.y;
        rotation[2] = transform.rotation.eulerAngles.z;
    }
}
