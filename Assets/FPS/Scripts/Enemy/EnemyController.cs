using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

namespace FPS {
    public class EnemyController : MonoBehaviour
    {
        [SerializeField]
        NavMeshAgent agent;
        [SerializeField]
        Animator animator;
        [SerializeField]
        EnemyType enemyType;

        [SerializeField] Transform target;
        [SerializeField] float Dist;
        [SerializeField] bool isRun = false;

        void Start(){
            agent = GetComponent<NavMeshAgent>();
            animator = transform.GetChild(0).GetComponent<Animator>();   
            target = GameObject.FindWithTag("Player").transform;


        }

        void Update(){
            Chase();
        }

        void Chase(){
            if(agent == null && target == null)
                return;

            Dist = Vector3.Distance(transform.position, target.position);

            if (Dist > agent.stoppingDistance)
            {
                agent.SetDestination(target.position);
                isRun = true;
                animator.SetBool("BearRun", true);
            }

            if (Dist <= agent.stoppingDistance)
            {
                Debug.Log("Attack");
                isRun = false;
                animator.SetBool("BearRun", false);
            }

        }

        void LookAtTarget(Transform target){

        }
    }
}

