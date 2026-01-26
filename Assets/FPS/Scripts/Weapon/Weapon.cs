using Autodesk.Fbx;
using UnityEngine;

namespace FPS {
    public class Weapon : MonoBehaviour
    {
        public WeaponData WeaponData;
        public int weaponHealth;

        private void Start(){
            if (WeaponData != null){
                weaponHealth = WeaponData.attacks.weaponHealth;
            }
        }

        public int ReduceHealth(int health) {

            weaponHealth -= health;
            if (weaponHealth <= 0){
                gameObject.SetActive(false);
                weaponHealth = 0;
            }

            return weaponHealth;
        }
    }
}

