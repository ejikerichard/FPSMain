using FPS;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthControl : MonoBehaviour
{

    [SerializeField] float currentHealth = 0;
    [SerializeField] float MaxHealth = 100f;

    [SerializeField] CanvasGroup redSplatter = null;

    [SerializeField] float hurtTimer;

    public bool IsDead = false;


    void Start()
    {
        currentHealth = MaxHealth;
        
    }

    // Update is called once per frame
    void Update()
    {
        Heal(0.2f * Time.deltaTime);
    }

    void UpdateHealth(){
        redSplatter.alpha = 1.2f - (currentHealth / MaxHealth);
    }

    public void TakeDamage(float damage) {
        if (currentHealth > 0)
            currentHealth -= damage;
        else 
            if(currentHealth <= 0){
                currentHealth = 0;
                Dead();
            }

        UpdateHealth();
        CameraWobble.Instance.Shake(2.5f);
    }

    void Heal(float healAmount)
    {
        if (currentHealth <= 0)
            return;
        
        if(redSplatter.alpha > 0)
        {
            redSplatter.alpha -= healAmount;
            if(redSplatter.alpha < 0)
                redSplatter.alpha = 0;
        }
    }
    public void Dead()
    {

        IsDead = true;
        GetComponent<CapsuleCollider>().height = 0;
        GetComponent<PlayerController>().enabled = false;
        GetComponent<PlayerAttack>().enabled = false;
        Debug.Log("Player is dead");
    }
}
