using Autodesk.Fbx;
using UnityEngine;

namespace FPS {
    public class Weapon : MonoBehaviour
    {
        public enum WeaponState{
            NON_BROKEN, BROKEN
        }

        public WeaponData WeaponData;
        public WeaponState weaponState;
        public int weaponHealth;

        private void Start(){
            if (WeaponData != null){
                if(weaponState  == WeaponState.NON_BROKEN)
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

