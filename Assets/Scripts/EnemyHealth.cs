using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 30;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"{gameObject.name}  {damage} Hasar ald�.");

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
