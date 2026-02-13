using UnityEngine;
using System.Collections;

namespace FPS {
    public class WeaponInventory : MonoBehaviour {
        public enum WeaponState{
            None, Equp
        }

        [Header("References")]
        [SerializeField] private GameObject[] Activeweapons;
        [SerializeField] private GameObject[] weaponItems;
        [SerializeField] private GameObject[] Active_brokenWeapon;
        [SerializeField] private GameObject activeWeapon;
        [SerializeField] private GameObject broken_weapon;
        public WeaponState state = WeaponState.None;

        public WeaponData currentWeapon;

        [Header("Weapon Settings")]
        public float range = 7f;
        public float handRange = 0f;
        public bool isPicked = false;
        public bool pickPressed = false;
        private bool swappingWeapon = false;

        private void Start(){
            handRange = range;
        }
        public void SwitchWeapon(){
            if (currentWeapon == null)
                return;

            foreach (GameObject weaponObject in Activeweapons){
                if (weaponObject.GetComponent<Weapon>().WeaponData == null)
                    return;

                if (currentWeapon.weaponName == weaponObject.GetComponent<Weapon>().WeaponData.weaponName){
                    weaponObject.SetActive(true);
                    activeWeapon = weaponObject;
                    Debug.Log(weaponObject.name);
                }else{
                    weaponObject.SetActive(false);
                }
            }

            foreach(GameObject brokenWeaponObject in Active_brokenWeapon){
                if (brokenWeaponObject.GetComponent<Weapon>().WeaponData == null)
                    return;
                if (currentWeapon.weaponName == brokenWeaponObject.GetComponent<Weapon>().WeaponData.weaponName){
                    brokenWeaponObject.SetActive(false);
                    broken_weapon = brokenWeaponObject;
                }
                else{
                    brokenWeaponObject.SetActive(false);
                }
            }

        }
        public void DestroyWeapon(){
            if (currentWeapon == null && activeWeapon == null)
                return;

            activeWeapon.GetComponent<Weapon>().ReduceHealth(5);
            if (activeWeapon.GetComponent<Weapon>().weaponHealth <= 0){
                if (!broken_weapon.activeSelf){
                    broken_weapon.SetActive(true);
                    return;
                }
                else if(broken_weapon.activeSelf){
                    currentWeapon = null;
                    range = handRange;
                    state = WeaponState.None;
                    activeWeapon = null;
                    isPicked = false;
                    broken_weapon.SetActive(false);
                    broken_weapon = null;
                }
                //currentWeapon = null;
                //range = handRange;
                //state = WeaponState.None;
                //activeWeapon = null;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Weapon")) return;
            if (swappingWeapon) return;

            Weapon groundWeapon = other.GetComponent<Weapon>();
            if (groundWeapon == null || groundWeapon.WeaponData == null) return;

            bool groundBroken = groundWeapon.weaponHealth <= 0;
            bool currentBroken = activeWeapon != null &&
                                 activeWeapon.GetComponent<Weapon>() != null &&
                                 activeWeapon.GetComponent<Weapon>().weaponHealth <= 0;

            // PICK FIRST WEAPON
            if (!isPicked){
                swappingWeapon = true;

                currentWeapon = groundWeapon.WeaponData;
                range = currentWeapon.attacks.range;

                SwitchWeapon();
                state = WeaponState.Equp;
                isPicked = true;

                Destroy(other.gameObject);

                StartCoroutine(ResetSwapFlag());
                return;
            }

            // SWAP WEAPON
            if (isPicked && pickPressed){

                swappingWeapon = true;

                // DROP CURRENT WEAPON
                if(currentWeapon != null){

                    foreach (GameObject weaponObject in weaponItems){
                        Weapon w = weaponObject.GetComponent<Weapon>();

                        if (w != null &&
                            w.WeaponData != null &&
                            w.WeaponData.weaponName == currentWeapon.weaponName && !currentBroken){
                            Instantiate(weaponObject, other.transform.position, Quaternion.identity);
                            break;
                        }
                    }
                }

                if(currentBroken || groundBroken){
                    Debug.Log("Broken weapon swap triggered");
                }

                // PICK NEW WEAPON
                currentWeapon = groundWeapon.WeaponData;
                range = currentWeapon.attacks.range;

                SwitchWeapon();
                state = WeaponState.Equp;

                Destroy(other.gameObject);

                StartCoroutine(ResetSwapFlag());
            }
        }

        IEnumerator ResetSwapFlag(){
            yield return new WaitForSeconds(0.2f);
            swappingWeapon = false;
        }
    }
}
