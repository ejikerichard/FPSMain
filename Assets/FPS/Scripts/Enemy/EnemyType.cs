using UnityEngine;

namespace FPS
{
    [CreateAssetMenu(fileName = "New Enemy Type", menuName = "FPS/Enemy Type")]
    public class EnemyType : ScriptableObject{

        public enum EnemyCategory
        {
            WaveEnemy,
            NonWaveEnemy
        }

        public string enemyName;
        public float health;
        public float speed;
        public float damage;
        public float attackRange;

        public EnemyCategory category;

    }

}
