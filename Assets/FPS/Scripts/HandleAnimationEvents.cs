using FPS;
using UnityEngine;

namespace FPS
{
    public class HandleAnimationEvents : MonoBehaviour
    {
        [SerializeField] GameObject player;
        void Start(){
            player = GameObject.FindGameObjectWithTag("Player");
        }

        public void PlayFootSound(){
            if (player == null)
                return;

            player.GetComponent<PlayerController>().HandleFootsteps();
        }
        public void ResetAttack(){
            //player.GetComponent<PlayerAttack>().HandleResetAttack();
        }
    }
}

