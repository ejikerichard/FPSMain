using UnityEngine;
using System.Collections;

namespace FPS
{
    public class CameraWobble : MonoBehaviour
    {
        [Header("Shake Settings")]
        public float shakeAmount = 2f;
        public float shakeSpeed = 25f;
        public float shakeDecay = 5f;

        private float currentShake;
        private Vector3 shakeOffset;
        private Vector3 rotationOffset;

        private Vector3 baseLocalPos;
        private Quaternion baseLocalRot;

        public static CameraWobble Instance;

        private void Awake(){
            Instance = this;
        }

        void Start(){
            baseLocalPos = transform.localPosition;
            baseLocalRot = transform.localRotation;
        }

        void LateUpdate()
        {
            if (currentShake > 0)
            {
                float noiseX = Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f;
                float noiseY = Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f;

                shakeOffset = new Vector3(noiseX, noiseY, 0) * currentShake;
                rotationOffset = new Vector3(-noiseY, noiseX, noiseX) * currentShake;

                transform.localPosition = baseLocalPos + shakeOffset;
                transform.localRotation = baseLocalRot * Quaternion.Euler(rotationOffset);

                currentShake = Mathf.Lerp(currentShake, 0, Time.deltaTime * shakeDecay);
            }
            else
            {
                transform.localPosition = baseLocalPos;
                transform.localRotation = baseLocalRot;
            }
        }

        public void Shake(float intensity)
        {
            currentShake += intensity;
        }
    }
}