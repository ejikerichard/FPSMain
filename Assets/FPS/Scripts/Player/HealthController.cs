using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthController : MonoBehaviour
{
    [SerializeField] float currentHealth = 0f;
    [SerializeField] float maxHealth = 100f;

    [SerializeField] CanvasGroup redSplatterImage = null;

    [SerializeField] float hurtTimer = 0f;
   // [SerializeField] Image hurtImage = null;

    private void Start() {
        currentHealth = maxHealth;
    }

    void Update(){
        Heal(0.2f * Time.deltaTime);
    }


    public void UpdateHealth(){
        redSplatterImage.alpha = 1.2f - (currentHealth / maxHealth);
    }
    IEnumerator HurtFlash(){
        //hurtImage.enabled = true;
        yield return new WaitForSeconds(hurtTimer);
        //hurtImage.enabled = false;
    }

    public void TakeDamage(float damage){
        if(currentHealth > 0)
            currentHealth -= damage;
        else 
            if(currentHealth <= 0)
                currentHealth = 0;

        UpdateHealth();
        StartCoroutine(HurtFlash());
    }
    void Heal(float healAmount){
        if (currentHealth <= 0)
            return;

        if(redSplatterImage.alpha > 0){
            redSplatterImage.alpha -= healAmount;

            if(redSplatterImage.alpha < 0){
                redSplatterImage.alpha = 0;
            }
        }
    }
}
