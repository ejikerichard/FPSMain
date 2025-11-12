using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FPS
{
    public class MouseLook : MonoBehaviour
    {
        [Header("References")]
        public Camera cam;
        public Transform cameraPivot; // The pivot point near the head
        public Transform playerRoot;  // Assign your player root (used to get layer)

        [Header("Look Settings")]
        public float minPitch = -80f;
        public float maxPitch = 80f;
        public float xSensitivity = 30f;
        public float ySensitivity = 30f;

        [Header("Clipping Settings")]
        public float cameraDistance = 0.3f;      // Default distance from pivot
        public float cameraRadius = 0.15f;       // Sphere radius for checking walls
        public float cameraAdjustSpeed = 10f;    // How smoothly camera adjusts
        public LayerMask clipMask;               // Layers that block the camera

        private float xRotation = 0f;
        private float currentDistance;
        private int playerLayer;

        void Start(){
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            currentDistance = cameraDistance;

            // Get player layer so we can ignore it in raycasts
            if (playerRoot != null)
                playerLayer = playerRoot.gameObject.layer;
            else
                playerLayer = gameObject.layer; // fallback
        }

        public void ProcessLook(Vector2 input){
            float mouseX = input.x * xSensitivity * Time.deltaTime;
            float mouseY = input.y * ySensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

            // Apply pitch to camera
            cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            // Apply yaw to player
            transform.Rotate(Vector3.up * mouseX);

            HandleCameraClipping();
        }

        void HandleCameraClipping(){
            if(cameraPivot == null || cam == null)
                return;

            Vector3 pivotPos = cameraPivot.position;
            Vector3 backward = -cameraPivot.forward;

            // Default desired camera position
            Vector3 desiredPos = pivotPos + backward * cameraDistance;

            // Perform a spherecast to detect nearby obstacles
            if(Physics.SphereCast(pivotPos, cameraRadius, backward, out RaycastHit hit, cameraDistance, clipMask, QueryTriggerInteraction.Ignore)){
                // Ignore collisions with player or its children (hands, arms, weapon, etc.)
                if(hit.collider.transform.root.gameObject.layer == playerLayer){
                    // Do nothing, ignore player collisions
                }
                else{
                    float targetDist = hit.distance - 0.05f;
                    currentDistance = Mathf.Lerp(currentDistance, Mathf.Max(0.05f, targetDist), Time.deltaTime * cameraAdjustSpeed);
                }
            }else{
                // Smoothly return to normal distance
                currentDistance = Mathf.Lerp(currentDistance, cameraDistance, Time.deltaTime * cameraAdjustSpeed);
            }

            // Apply final camera position
            cam.transform.position = pivotPos + backward * currentDistance;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (cameraPivot != null)
            {
                Gizmos.color = Color.yellow;
                Vector3 testPos = cameraPivot.position - cameraPivot.forward * currentDistance;
                Gizmos.DrawWireSphere(testPos, cameraRadius);
            }
        }
#endif
    }
}
