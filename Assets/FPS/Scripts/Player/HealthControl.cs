using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthControl : MonoBehaviour
{

    [SerializeField] float currentHealth = 0;
    [SerializeField] float MaxHealth = 100f;

    [SerializeField] CanvasGroup redSplatter = null;

    [SerializeField] float hurtTimer;


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
            if(currentHealth <= 0)
            currentHealth = 0;

        UpdateHealth();
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
}
