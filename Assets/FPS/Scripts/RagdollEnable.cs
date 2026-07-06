using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RagdollEnable : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private Transform RagdollRoot;

    public Rigidbody[] Rigidbodies;
    private CharacterJoint[] joints;
    private Collider[] Colliders;

    private void Start()
    {
        animator = transform.GetComponentInParent<Animator>();

        Rigidbodies = RagdollRoot.GetComponentsInChildren<Rigidbody>();
        joints = RagdollRoot.GetComponentsInChildren<CharacterJoint>();
        Colliders = RagdollRoot.GetComponentsInChildren<Collider>();

        EnableAnimator();
    }

    public void EnableRagdoll()
    {
        animator.enabled = false;

        foreach (CharacterJoint joint in joints)
        {

            joint.enableCollision = true;
        }
        foreach (Collider col in Colliders)
        {
            col.enabled = true;
        }
        foreach (Rigidbody rigidbody in Rigidbodies)
        {

            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.detectCollisions = true;
            rigidbody.useGravity = true;
        }
    }
    public void EnableAnimator()
    {
        animator.enabled = true;

        foreach (CharacterJoint joint in joints)
        {

            joint.enableCollision = false;
        }
        foreach (Collider col in Colliders)
        {
            col.enabled = false;
        }
        foreach (Rigidbody rigidbody in Rigidbodies)
        {

            rigidbody.detectCollisions = false;
            rigidbody.useGravity = false;
        }
    }
}

