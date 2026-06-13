using Unity.VisualScripting;
using UnityEngine;

public class EnemyAnimatorSync : MonoBehaviour
{
    private Animator anim;
    private Rigidbody rb;

    EnemyMotor motor;

    public bool useRootMotion;

    public string[] attackAnimations = new string[]
    {
        "Attack_0",
        "Attack_1",
        "Attack_2"
    };


    int lastAttack = -1;


    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        motor = GetComponent<EnemyMotor>();
    }

    bool allowLocomotion = true;

    public void SetLocomotion(bool enabled)
    {
        allowLocomotion = enabled;
    }

    public void PlayIdle(int index = 0)
    {
        useRootMotion = false;
        anim.SetInteger("RandIdle", index);
        anim.SetBool("Run", false);
    }

    public void PlayAttack(){
        useRootMotion = true;
        int index;

        do
        {
            index = Random.Range(0, attackAnimations.Length);
        }
        while (index == lastAttack && attackAnimations.Length > 1);

        lastAttack = index;

        anim.CrossFade(attackAnimations[index], 0.1f);
       
    }

    public void PlayRecover(){
        useRootMotion = true;
        anim.CrossFade("Recover", 0.1f);
    }

    public void PlayDodge(){
        anim.CrossFade("Dodge", 0.1f);
    }

    public void PlayDashBack(){
        anim.CrossFade("DashBack", 0.1f);
    }

    public void PlayStunned(){
        anim.CrossFade("Stunned", 0.1f);
    }
    public void PlayDashForward(){
        anim.CrossFade("DashFwd", 0.1f);
    }
    public void PlayTaunt(int index){
        if (index <= 0)
            anim.CrossFade("Taunt1", 0.1f);
        else if (index == 1)
            anim.CrossFade("Taunt2", 0.1f);
        if (index == 2)
            anim.CrossFade("Taunt3", 0.1f);
    }
    public void PlayDamage()
    {
        anim.CrossFade("hitReact", 0.1f);
    }

    public void UpdateMovement(Vector3 velocity, Transform target)
    {
        if (!allowLocomotion)
        {
            anim.SetBool("Run", false);
            anim.SetFloat("MoveX", 0f);
            anim.SetFloat("MoveY", 0f);
            return;
        }


        Vector3 localInput = transform.InverseTransformDirection(motor.LastMoveInput);

        float moveX = localInput.x;
        float moveY = localInput.z;

        Vector2 input = new Vector2(moveX, moveY);
        input = Vector2.ClampMagnitude(input, 1f);

        anim.SetFloat("MoveX", input.x, 0.1f, Time.deltaTime);
        anim.SetFloat("MoveY", input.y, 0.1f, Time.deltaTime);

        float inputMag = new Vector2(moveX, moveY).magnitude;
        anim.SetBool("Run", inputMag > 0.1f);
    }

    public void ForceStopMovement()
    {
        anim.SetBool("Run", false);
        anim.SetFloat("MoveX", 0f);
        anim.SetFloat("MoveY", 0f);
    }
    public bool IsAnimationFinished()
    {
        return anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
    }

    void OnAnimatorMove()
    {
        if (!useRootMotion)
        {
            // Never let the animator push velocity when we're not in a root motion state
            return;
        }

        Vector3 delta = anim.deltaPosition;
        delta.y = 0f;  // don't let root motion fight gravity
        rb.linearVelocity = delta / Time.deltaTime;
    }
}