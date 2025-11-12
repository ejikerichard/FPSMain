using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FPS
{
    public class InputManager : MonoBehaviour
    {
        PlayerInput playerInput;
        PlayerInput.OnFootActions onFootActions;
        PlayerController playerController;
        MouseLook mouseLook;

        private void Awake(){
            playerInput = new PlayerInput();
            mouseLook = GetComponent<MouseLook>();
            onFootActions = playerInput.OnFoot;

            onFootActions.Jump.performed += ctx => playerController.OnJumpPressed();

            onFootActions.Sprint.performed += ctx => playerController.OnSprintPressed();
            onFootActions.Sprint.canceled += ctx => playerController.OnSprintReleased();

            onFootActions.Crouch.performed += ctx => playerController.OnCrouchPressed();
            onFootActions.Crouch.canceled += ctx => playerController.OnCrouchReleased();

        }
        private void Start(){
            playerController = GetComponent<PlayerController>();
        }
        private void OnEnable(){
            onFootActions.Enable();
        }
        void OnDisable(){
            onFootActions.Disable();
        }

        void FixedUpdate(){
            playerController.HandleMovement(onFootActions.Movement.ReadValue<Vector2>());
        }
        private void LateUpdate(){
            mouseLook.ProcessLook(onFootActions.Look.ReadValue<Vector2>());
        }
    }
}

