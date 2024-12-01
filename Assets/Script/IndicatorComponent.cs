using UnityEngine;

public class IndicatorComponent : MonoBehaviour
{
    public Transform BaseTransform;
    public Transform FillTransform;
    [HideInInspector]
    public Vector3 MaxScale; // 최대 크기 (Base와 동일)
}