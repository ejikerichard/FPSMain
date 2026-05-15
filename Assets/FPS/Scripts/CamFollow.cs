using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CamFollow : MonoBehaviour
{
    public Transform target;

    public Vector3 offset;

    public float speed;
    public float dampTime = 0.3f;
    public float smoothTime = 0.3f;

    private Vector3 VelocityF = Vector3.zero;

    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }
    void FixedUpdate()
    {
        Vector3 targetCamPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetCamPos, ref VelocityF, smoothTime);
    }
}
