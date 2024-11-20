using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mirror;

public class DamageTextManager : SingletonBehaviour<DamageTextManager>
{
    private Canvas uiCanvas; // Canvas를 동적으로 참조
    private Queue<GameObject> textPool = new Queue<GameObject>();

    private void EnsureCanvasReference()
    {
        if (uiCanvas == null)
        {
            GameObject canvasObject = GameObject.FindWithTag("MainCanvas");
            if (canvasObject != null)
            {
                uiCanvas = canvasObject.GetComponent<Canvas>();
                if (uiCanvas == null)
                {
                    Debug.LogError("태그가 'MainCanvas'인 오브젝트에 Canvas 컴포넌트가 없습니다.");
                }
            }
            else
            {
                Debug.LogError("태그가 'MainCanvas'인 Canvas를 찾을 수 없습니다.");
            }
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

        for (int i = 0; i < 20; i++) // 풀 크기 기본값 20
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
            // Pool이 비어있으면 새 인스턴스를 생성
            instance = Instantiate(prefab, uiCanvas.transform);
        }

        instance.SetActive(true);

        // Convert world position to UI position
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        float offsetX = UnityEngine.Random.Range(-30f, 30f); // X축 오프셋 범위
        float offsetY = UnityEngine.Random.Range(-20f, 20f); // Y축 오프셋 범위
        screenPosition += new Vector3(offsetX, offsetY, 0);
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
