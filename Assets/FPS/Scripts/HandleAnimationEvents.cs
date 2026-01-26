using FPS;
using UnityEngine;

namespace FPS
{
    public class HandleAnimationEvents : MonoBehaviour
    {
        [SerializeField] PlayerAttack player;
        void Start(){
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerAttack>();
        }

        public void PlayFootSound(){
            if (player == null)
                return;

            player.GetComponent<PlayerController>().HandleFootsteps();
        }
        public void SpawnDecal(){
            if(player == null) return;
            player.GetComponent<PlayerAttack>().HandleSpawnDecal();
        }
    }
}

