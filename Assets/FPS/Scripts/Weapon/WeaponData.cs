using UnityEngine;

namespace FPS
{
    [CreateAssetMenu(menuName = "FPS/Weapon Attack Data")]
    public class WeaponData : ScriptableObject
    {
        public string weaponName;

        public AttackData[] attacks;
    }
    [System.Serializable]
    public class AttackData
    {
        public float damage = 10f;
        public float range = 2f;
        public float hitRadius = 0.3f;

        public GameObject hitVFX;
        public AudioClip hitSFX;
    }
}

