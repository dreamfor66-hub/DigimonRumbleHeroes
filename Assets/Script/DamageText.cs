using System;
using UnityEngine;
using UnityEngine.UI;

public class DamageText : MonoBehaviour
{
    [SerializeField] private Text damageText;
    [SerializeField] private float lifetime = 1f;

    private Action onDespawn;

    public void Initialize(int damage, Action despawnCallback)
    {
        damageText.text = damage.ToString(); // Set the rounded damage value
        onDespawn = despawnCallback;

        // Start animation and despawn after lifetime
        Invoke(nameof(Despawn), lifetime);
    }

    private void Despawn()
    {
        onDespawn?.Invoke();
    }
}
