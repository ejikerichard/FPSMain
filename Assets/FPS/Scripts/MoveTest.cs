using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MoveTest : MonoBehaviour
{
    private float InputX;
    private float InputZ;

    [SerializeField] private float Speed = 5f;

    void Update()
    {
        InputX = Input.GetAxis("Horizontal");
        InputZ = Input.GetAxis("Vertical");
        Vector3 moveDir = new Vector3(InputX, 0, InputZ).normalized;
        transform.Translate(moveDir * Speed * Time.deltaTime, Space.World);
    }
}
