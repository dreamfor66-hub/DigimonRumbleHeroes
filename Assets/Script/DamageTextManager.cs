using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class DamageTextManager : SingletonBehaviour<DamageTextManager>
{
    private Canvas uiCanvas; // Canvas�� �������� ����
    private Queue<GameObject> textPool = new Queue<GameObject>();

    private void EnsureCanvasReference()
    {
        if (uiCanvas == null)
        {
            uiCanvas = FindObjectOfType<Canvas>();
        }
    }

    public override void Init()
    {
        InitializePool();
    }

    private GameObject GetDamageTextPrefab()
    {
        var prefab = ResourceHolder.Instance?.gameVariables?.DamageTextPrefab;
        if (prefab == null)
        {
            Debug.LogError("DamageText Prefab is not assigned in GameVariables.");
        }
        return prefab;
    }

    private void InitializePool()
    {
        EnsureCanvasReference();
        GameObject prefab = GetDamageTextPrefab();
        //if (prefab == null || uiCanvas == null) return;

        for (int i = 0; i < 20; i++) // Ǯ ũ�� �⺻�� 20
        {
            var instance = Instantiate(prefab, uiCanvas.transform);
            instance.SetActive(false);
            textPool.Enqueue(instance);
        }
    }

    public void ShowDamageText(Vector3 worldPosition, int damage)
    {
        EnsureCanvasReference();
        GameObject prefab = GetDamageTextPrefab();

        GameObject instance;
        if (textPool.Count > 0)
        {
            instance = textPool.Dequeue();
        }
        else
        {
            // Pool�� ��������� �� �ν��Ͻ��� ����
            instance = Instantiate(prefab, uiCanvas.transform);
        }

        instance.SetActive(true);

        // Convert world position to UI position
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        instance.transform.position = screenPosition;

        // Update text
        var damageText = instance.GetComponentInChildren<DamageText>();
        damageText.Initialize(damage, () => ReturnToPool(instance));
    }

    private void ReturnToPool(GameObject instance)
    {
        instance.SetActive(false);
        textPool.Enqueue(instance);
    }
}
