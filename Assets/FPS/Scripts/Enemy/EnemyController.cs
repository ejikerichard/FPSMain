using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [SerializeField]
    NavMeshAgent agent;

    [SerializeField] Transform target;
    [SerializeField] float Dist;
    void Start(){
        agent = GetComponent<NavMeshAgent>();
        target = GameObject.FindWithTag("Player").transform;
    }

    void Update(){
        Chase();
    }

    void Chase() { 
        if (agent == null && target == null)
            return;

        Dist = Vector3.Distance(transform.position, target.position);

        if(Dist > agent.stoppingDistance){
            agent.SetDestination(target.position);
        }
        else if (Dist <= agent.stoppingDistance){
            Debug.Log("Attack");
        }

    }
}
