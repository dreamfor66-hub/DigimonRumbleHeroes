using UnityEngine;

public class IndicatorComponent : MonoBehaviour
{
    public Transform BaseTransform;
    public Transform FillTransform;
    [HideInInspector]
    public Vector3 MaxScale; // �ִ� ũ�� (Base�� ����)
}