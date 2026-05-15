using UnityEngine;
using TMPro;

using UnityEngine;
using TMPro;

public class HealthTest : MonoBehaviour
{
    
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    [SerializeField] private TMP_Text m_Text;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthText();
    }

    void Reduces(float health)
    {
        if(currentHealth < health) { currentHealth = 0;
        }
        else
        {
            currentHealth -= health;
        }
    }

    void UpdateHealthText()
    {
        m_Text.text = "Health: " + currentHealth;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Reduces(10f);
            UpdateHealthText();
        }
    }

}
