using UnityEngine;

namespace FPS {
    public class WeaponInventory : MonoBehaviour {
        enum WeaponState{
            None, Equp
        }

        [SerializeField] private GameObject[] weapons;
        [SerializeField] private GameObject activeWeapon;
        [SerializeField] WeaponState state = WeaponState.None;

        public WeaponData currentWeapon;

        public float range = 7f;
        public float handRange = 0f;

        private void Start()
        {
            handRange = range;
        }
        public void SwitchWeapon(){
            if (currentWeapon == null)
                return;

            foreach (GameObject weaponObject in weapons){
                if (weaponObject.GetComponent<Weapon>().WeaponData == null)
                    return;

                if (currentWeapon.weaponName == weaponObject.GetComponent<Weapon>().WeaponData.weaponName){
                    weaponObject.SetActive(true);
                    activeWeapon = weaponObject;
                    Debug.Log(weaponObject.name);
                }
            }
        }
        public void DestroyWeapon(){
            if (currentWeapon == null && activeWeapon == null)
                return;

            activeWeapon.GetComponent<Weapon>().ReduceHealth(5);
            if (activeWeapon.GetComponent<Weapon>().weaponHealth <= 0){
                currentWeapon = null;
                range = handRange;
                state = WeaponState.None;
                activeWeapon = null;
            }
        }

        private void OnTriggerEnter(Collider other){
            if(currentWeapon != null) return;

            if(other.CompareTag("Weapon")){
                currentWeapon = other.gameObject.GetComponent<Weapon>().WeaponData;
                range = currentWeapon.attacks.range;
                SwitchWeapon();
                state = WeaponState.Equp;
                Destroy(other.gameObject, 0.01f);
            }
        }
    }
}
