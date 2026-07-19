using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace FPS
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {

        [Header("Animation")]
        public Animator anim;
        public float animationSmoothing = 8f;

        [Header("References")]
        public Camera playerCamera;
        public Transform orientation; 
        public PlayerInput playerInput; 
        public AudioSource footstepSource;
        public AudioClip footstepClips;
        private WeaponInventory weaponInventory;

        [Header("Movement")]
        public float walkSpeed = 5f;
        public float sprintSpeed = 9f;
        public float crouchSpeed = 2.5f;
        public float acceleration = 20f;
        public float deceleration = 20f;
        public float airControl = 0.5f;
        public float targetSpeed;
        private float move_ID;
        private float idle_ID;
        public float speedPercent;

        [Header("Jump & Gravity")]
        public float jumpForce = 7f;
        public float gravity = -24f;
        public float fallGravityMultiplier = 2.5f; 
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        public float jumpForwardBoost = 3.5f;
        public float sprintJumpMultiplier = 1.25f;

        [Header("Crouch & Slide")]
        public float standingHeight = 1.8f;
        public float crouchHeight = 1.0f;
        public float crouchTransitionSpeed = 8f;
        public float slideThresholdSpeed = 6f;
        public float slideDuration = 0.9f;
        public float slideFriction = 8f;

        [Header("Vaulting")]
        public float vaultMaxHeight = 1.1f;   
        public float vaultMaxDistance = 1.0f;  
        public float vaultSpeed = 5f;

        [Header("Head/Weapon Bob & Recoil")]
        public float headBobAmount = 0.03f;
        public float headBobFrequency = 8f;
        public float aimBobMultiplier = 0.5f;
        public float recoilAmount = 0.08f;
        public float recoilReturnSpeed = 6f;

        [Header("Stamina")]
        public float maxStamina = 5f;       
        public float staminaDrainRate = 1f;  
        public float staminaRecoverRate = 0.8f;
        public float staminaRecoverDelay = 1.0f;

        [Header("Ground & Fall")]
        public LayerMask groundMask;
        public float groundCheckDistance = 0.1f;
        public float stepOffset = 0.3f;
        public float slopeLimit = 45f;
        public float fallDamageMinHeight = 6f;
        public float fallDamageMultiplier = 5f;


        [SerializeField]
        Rigidbody myBody;
        [SerializeField]
        CapsuleCollider capsuleCollider;

        Vector2 moveInput;
        Vector2 lookInput;
        Vector3 velocity;
        float verticalVelocity;
        public bool jumpRequested;
        float lastGroundTime = -10f;
        float lastJumpRequestTime = -10f;

        // states
        bool sprintPressed;
        public bool crouchPressed;
        public bool isCrouching;
        bool isSliding;
        public bool attackPressed;
        float slideTimer;
        float currentStamina;
        float staminaRecoverTimer;

        // camera bob
        Vector3 initialCamLocalPos;
        float bobTimer;
        Vector3 recoilOffset;


       public AnimatorStateInfo stateInfo;

       public int baseLayer { get { return anim.GetLayerIndex("Base Layer"); } }

        void Start(){
            myBody = GetComponent<Rigidbody>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            weaponInventory = GetComponent<WeaponInventory>();
        }
        private void Update(){
            HandleStamina();
            HandleTimers();
            HandleJump();
            HandleVaultCheck();
            HandleCameraBob();
            LayerControl();
        }
        private void FixedUpdate()
        {
            HandleCrouch();
        }

        public void HandleMovement(Vector2 input){
            moveInput = input;

            if (moveInput.sqrMagnitude < 0.1f) return;

            Vector3 right = GetOrientationRight();
            Vector3 forward = GetOrientationForward();

            targetSpeed = walkSpeed;
            if (isCrouching) targetSpeed = crouchSpeed;
            if (sprintPressed && !isCrouching && currentStamina > 0 && Mathf.Abs(moveInput.y) > 0.1f || sprintPressed && !isCrouching && currentStamina > 0 && Mathf.Abs(moveInput.x) > 0.1f) targetSpeed = sprintSpeed;
            Vector3 targetVelocity = (forward * moveInput.y + right * moveInput.x) * targetSpeed;
            float acclelerationRate = IsGrounded() ? acceleration : acceleration * airControl;
            velocity = targetVelocity * acclelerationRate * Time.deltaTime;

            Vector3 move = velocity * Time.deltaTime;
            myBody.MovePosition(transform.position + move);

        }
        void HandleCrouch(){

            float targetHeight = isCrouching ? crouchHeight : standingHeight;
            if (Mathf.Abs(capsuleCollider.height - targetHeight) > 0.01f)
            {
                float h = Mathf.Lerp(capsuleCollider.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
                AdjustCharacterHeight(h);
            }
        }
        public void HandleAnimate(Vector2 moveInput){
            Vector2 dir = new Vector2(moveInput.x, moveInput.y);
            float moveAmount = Mathf.Clamp01(dir.magnitude);

            // ----------- 3 ZONE ANALOG LOCOMOTION ------------
            float targetSpeed = 0f;

            if(moveAmount < 0.1f){
                targetSpeed = 0f;  // IDLE
            }
            else if(moveAmount < 0.35f){
                targetSpeed = 0.5f;  // WALK
            }
            else if(moveAmount < 0.75f){
                targetSpeed = 0.85f; // JOG
            }
            else{
                targetSpeed = 1.2f;  // RUN
            }

            if (weaponInventory.state == WeaponInventory.WeaponState.None){
                idle_ID = 0;
                move_ID = 1;
            }
            else if (weaponInventory.state == WeaponInventory.WeaponState.Equp){
                idle_ID = 1;
                move_ID = 2;
            }

            // Smooth movement blending
            anim.SetFloat("Speed", targetSpeed, 0.1f, Time.deltaTime);

            // Optional: feed joystick strength into a secondary parameter
            anim.SetFloat("WalkBlend", moveAmount, 0.1f, Time.deltaTime);

            anim.SetFloat("Move_ID", move_ID, 0.1f, Time.deltaTime);

            anim.SetFloat("Idle_ID", idle_ID, 0.1f, Time.deltaTime);

            //Debug.Log($"MoveAmount={moveAmount}, Speed={targetSpeed}");

        }
        void HandleJump()
        {
            if(isCrouching) return;


            if (IsGrounded()){
                verticalVelocity = (verticalVelocity < 0f) ? -2f : verticalVelocity;

                if(Time.time - lastJumpRequestTime <= jumpBufferTime &&
                    Time.time - lastGroundTime <= coyoteTime){
                    // --- Vertical jump ---
                    verticalVelocity = jumpForce;

                    // --- Horizontal jump boost ---
                    Vector3 forward = GetOrientationForward();
                    Vector3 right = GetOrientationRight();

                    Vector3 inputDir =
                        (forward * moveInput.y + right * moveInput.x).normalized;

                    Vector3 horizontalVelocity = new Vector3(
                        myBody.linearVelocity.x,
                        0f,
                        myBody.linearVelocity.z
                    );

                    float boost =
                        jumpForwardBoost *
                        Mathf.Clamp01(moveInput.magnitude) *
                        (sprintPressed ? sprintJumpMultiplier : 1f);

                    horizontalVelocity += inputDir * boost;

                    myBody.linearVelocity =
                        horizontalVelocity + Vector3.up * verticalVelocity;

                    lastJumpRequestTime = -10f;
                }
            }else{
                bool falling = verticalVelocity < 0f;
                verticalVelocity += gravity *
                    (falling ? fallGravityMultiplier : 1f) *
                    Time.deltaTime;

                // Preserve forward speed in air
                Vector3 horizontalVelocity = new Vector3(
                    myBody.linearVelocity.x,
                    0f,
                    myBody.linearVelocity.z
                );

                myBody.linearVelocity =
                    horizontalVelocity + Vector3.up * verticalVelocity;
            }

            anim.SetBool("IsGrounded", IsGrounded());
        }


        void HandleCameraBob(){
            if (!IsGrounded()) return;
            float speed = new Vector3(myBody.linearVelocity.x, 0, myBody.linearVelocity.z).magnitude;
            if(speed > 0.1f){
                bobTimer += Time.deltaTime * (speed / walkSpeed) * headBobFrequency;
                float bobAmount = headBobAmount * (isCrouching ? 0.5f : 1f) * (sprintPressed ? 1.3f : 1f);
                float x = Mathf.Sin(bobTimer) * bobAmount;
                float y = Mathf.Cos(bobTimer * 2) * bobAmount * 0.5f;
                playerCamera.transform.localPosition = initialCamLocalPos + new Vector3(x, y, 0) + recoilOffset;
            }else{
                // return to rest
                //playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, initialCamLocalPos + recoilOffset, Time.deltaTime * 6f);
            }
        }

        public void OnJumpPressed() { jumpRequested = true; /*anim.CrossFadeInFixedTime("JumpStart", 0.1f); lastJumpRequestTime = Time.time; */}
        public void OnSprintPressed() { if (moveInput.sqrMagnitude < 0.1f) return; sprintPressed = true; Debug.Log("Sprint button pressed"); }
        public void OnSprintReleased() { sprintPressed = false; Debug.Log("Sprint button released"); }
        public void OnCrouchPressed() { crouchPressed = true; isCrouching = true; Debug.Log("CrouchButton pressed"); }
        public void OnCrouchReleased() { crouchPressed = false; isCrouching = false;  Debug.Log("CrouchButton releassed"); }
        public void OnAttackPressed() { attackPressed = true; Debug.Log("Attack Pressed"); }
        public void OnAttackReleased() { attackPressed =false; Debug.Log("Attack Released"); }
        public void OnPickPress() { weaponInventory.pickPressed = true; Debug.Log("Interact Press"); }
        public void OnPickRelease() { weaponInventory.pickPressed = false; Debug.Log("Interact Release"); }
        void HandleTimers(){
            if (IsGrounded())
                lastGroundTime = Time.time;

            if (jumpRequested)
                lastJumpRequestTime = Time.time;
        }
        void HandleStamina(){
            bool sprinting = sprintPressed && !isCrouching && IsGrounded() && moveInput.y > 0.1f && currentStamina > 0;
            if(sprinting){
                currentStamina = Mathf.Max(0, currentStamina - staminaDrainRate * Time.deltaTime);
                staminaRecoverTimer = 0;
            }else{
                if(staminaRecoverTimer >= staminaRecoverDelay)
                    currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRecoverRate * Time.deltaTime);
                else
                    staminaRecoverTimer += Time.deltaTime;
            }

            // start slide if sprint + crouch pressed and speed > threshold
            if (crouchPressed && sprintPressed && !isSliding && myBody.linearVelocity.magnitude > slideThresholdSpeed && IsGrounded() && currentStamina > 0)
                StartCoroutine(StartSlide());
        }

        void LayerControl(){
            stateInfo = anim.GetCurrentAnimatorStateInfo(baseLayer);
        }
        public bool IsAnimatorTag(string tag){

            if(anim == null) return false;
            if(stateInfo.IsTag(tag)) return true;

            return false;
           
        }
        IEnumerator StartSlide(){
            isSliding = true;
            slideTimer = 0f;
            float startHeight = capsuleCollider.height;
            // lower camera quickly for visual
            while(slideTimer < slideDuration){
                slideTimer += Time.deltaTime;
                // apply forward force by manipulating velocity
                Vector3 forward = GetOrientationForward();
                myBody.MovePosition(transform.position + forward * (sprintSpeed + 1f) * Time.deltaTime); // simple slide movement
                currentStamina = Mathf.Max(0, currentStamina - staminaDrainRate * Time.deltaTime);
                yield return null;
            }
            isSliding = false;
        }
        void HandleVaultCheck(){
            if (!IsGrounded() || isSliding) return;
            if (moveInput.magnitude < 0.1f) return;

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 dir = GetOrientationForward();
            // check for low obstacle
            if(Physics.Raycast(origin, dir, out RaycastHit hit, vaultMaxDistance)){
                float hitHeight = hit.point.y - transform.position.y;
                if(hitHeight < vaultMaxHeight && hitHeight > 0.2f){
                    // check space above obstacle
                    Vector3 topCheck = hit.point + Vector3.up * (vaultMaxHeight + 0.2f);
                    if(!Physics.CheckSphere(topCheck, 0.25f)){
                        // vault
                        StartCoroutine(VaultOver(hit.point + Vector3.up * 0.5f));
                    }
                }
            }
        }
        IEnumerator VaultOver(Vector3 vaultTarget){
            float t = 0;
            float duration = 0.28f;
            Vector3 start = transform.position;
            Vector3 end = vaultTarget + GetOrientationForward() * 0.5f;
            capsuleCollider.enabled = false; // temporarily disable controller for manual move
            while (t < duration){
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, t / duration);
                yield return null;
            }
            capsuleCollider.enabled = true;
        }
        public void HandleFootsteps(){
            if (!IsGrounded()) return;
           
            PlayFootstep();
              //  PlayFootstep();
            //float speed = new Vector3(myBody.linearVelocity.x, 0, myBody.linearVelocity.z).magnitude;
            //Debug.Log($"Footstep Speed: {(myBody.linearVelocity.magnitude )}");
            //if (myBody.linearVelocity.magnitude > 0){
            //    // simple timer-based footsteps
            //    float rate = 0.45f;
            //    Debug.Log($"Speed: {(speed)}");
            //    if (sprintPressed) rate = 0.25f;
            //    if (isCrouching) rate = 0.6f;
            //    // use bobTimer to schedule footsteps
            //    if(Mathf.Repeat(bobTimer, rate) < Time.deltaTime * 2f && !footstepSource.isPlaying){
            //        PlayFootstep();
            //    }
            //}
        }
        void PlayFootstep(){
            if (footstepClips == null || footstepSource == null) return;
            var clip = footstepClips;
            footstepSource.PlayOneShot(clip);
        }
        //void HandleRecoilReturn(){
        //    // simple recoil Lerp back to zero
        //    recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, Time.deltaTime * recoilReturnSpeed);
        //}

        //public void ApplyRecoil(Vector2 recoil){
        //    // recoil is in degrees: x = vertical, y = horizontal
        //    recoilOffset += new Vector3(-recoil.x * recoilAmount, recoil.y * recoilAmount, 0);
        //    // also rotate camera a bit (use MouseLook script to apply)
        //}
        Vector3 GetOrientationForward(){
            if (orientation != null) return (Vector3.ProjectOnPlane(orientation.forward, Vector3.up)).normalized;
            return (Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up)).normalized;
        }

        Vector3 GetOrientationRight(){
            if (orientation != null) return (Vector3.ProjectOnPlane(orientation.right, Vector3.up)).normalized;
            return (Vector3.ProjectOnPlane(playerCamera.transform.right, Vector3.up)).normalized;
        }

        bool IsGrounded(){

            Vector3 origin = transform.position + Vector3.up * 0.1f;
            return Physics.SphereCast(origin, 0.2f, Vector3.down, out _, groundCheckDistance + 0.05f, groundMask);
        }
        void AdjustCharacterHeight(float height){
            float diff = capsuleCollider.height - height;
            capsuleCollider.height = height;
            capsuleCollider.center += Vector3.up * (diff * 0.5f);

            // move camera down/up to match (so eyes move with character)
            float camY = Mathf.Clamp(playerCamera.transform.localPosition.y - diff, 0.1f, 2f);
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, camY, playerCamera.transform.localPosition.z);
        }
        public float lastYVel;
        void LateUpdate(){
            // simple fall damage detection on landing
            if(!IsGrounded()){
                lastYVel = verticalVelocity;
            }else{
                if(lastYVel < -fallDamageMinHeight){
                    float damage = (Mathf.Abs(lastYVel) - fallDamageMinHeight) * fallDamageMultiplier;
                    // apply damage to player here
                  //  Debug.Log($"Fall damage: {damage}");
                }
                lastYVel = 0;
            }

            // reset jump request
            jumpRequested = false;
        }
    }
}

