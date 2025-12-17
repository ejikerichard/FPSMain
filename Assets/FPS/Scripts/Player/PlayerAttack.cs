using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

namespace FPS
{
    public class PlayerAttack : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Camera armCam;
        [SerializeField] LayerMask hitLayers;
        [SerializeField] PlayerController controller;
        [SerializeField] float range = 100f;

        [Header("AttackSettings")]
        [SerializeField] bool isAttacking = false;
        [SerializeField] int attackCount = 0;
        [SerializeField] float timeBetweenAttacks = 0.5f;
        [SerializeField] float attackTimer = 0f;

        [Header("AnimationPrematers")]
        [SerializeField] string attackID_Params = "AttackID";
        [SerializeField] string noHit_Params = "NoHit_ID";
        [SerializeField] string isAttack_Params = "IsAttacking";


        void Start(){
            controller = GetComponent<PlayerController>();
            armCam = GameObject.FindGameObjectWithTag("ArmCamera").GetComponent<Camera>();
        }

        void Update(){
            FirRay();

            if (attackTimer > 0)
                attackTimer -= Time.deltaTime;
        }

        void FirRay(){
            RaycastHit hit;
            if(Physics.Raycast(armCam.transform.position, armCam.transform.forward, out hit, range, hitLayers)){
                if(controller.attackPressed){
                    if(attackTimer <= 0){
                        if(attackCount <= 0 && !isAttacking){
                            attackCount += 1;
                            isAttacking = true;
                            controller.anim.SetInteger(attackID_Params, attackCount);
                            controller.anim.SetBool(isAttack_Params, isAttacking);
                        }
                        
                        else if(attackCount == 1 && !isAttacking){
                            attackCount += 1;
                            isAttacking = true;
                            controller.anim.SetInteger(attackID_Params, attackCount);
                            controller.anim.SetBool(isAttack_Params, isAttacking);
                        }
                        
                        if(attackCount == 2 && !isAttacking){
                            attackCount += 1;
                            isAttacking = true;
                            controller.anim.SetInteger(attackID_Params, attackCount);
                            controller.anim.SetBool(isAttack_Params, isAttacking);
                        }
                            attackTimer = timeBetweenAttacks;
                    }
                }
                Debug.Log(hit.transform.name);
            }
            else
            {
                if(controller.attackPressed){
                    if(attackTimer <= 0){
                        if(attackCount <= 0 && !isAttacking){
                            attackCount += 1;
                            isAttacking = true;
                            controller.anim.SetInteger(attackID_Params, attackCount);
                            controller.anim.SetBool(isAttack_Params, isAttacking);
                        }
                        
                        else if(attackCount == 1 && !isAttacking){
                            attackCount += 1;
                            isAttacking = true;
                            controller.anim.SetInteger(attackID_Params, attackCount);
                            controller.anim.SetBool(isAttack_Params, isAttacking);
                        }
                        if(attackCount == 2 && !isAttacking){
                            attackCount += 1;
                            isAttacking = true;
                            controller.anim.SetInteger(attackID_Params, attackCount);
                            controller.anim.SetBool(isAttack_Params, isAttacking);
                        }

                        attackTimer = timeBetweenAttacks;
                    }
                }
            }
        }
        public void HandleResetAttack(){
            isAttacking = false;
            if(attackCount >=3)
                { attackCount = 0; }
            controller.anim.SetBool(isAttack_Params, isAttacking);
        }
    }
}

