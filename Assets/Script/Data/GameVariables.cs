using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameVariables", menuName = "Settings/GameVariables", order = 1)]
public class GameVariables : ScriptableObject
{
    public GameObject HPStaminaBarPrefab; 
    public GameObject DamageTextPrefab;

    public float dragThreshold = 12f;
    public float tapThreshold = 0.2f;
    public float maxDistance = 200f;

    //�Ϲݳ˹�� ���Ž� �˹��� ������ �� �ִ� �˹���ذ�
    public float knockBackPowerReference = 7f;
    public float collideReduceMultiplier = 0.5f;
    public float hitstopWhenCollide = 5.5f;
    public float collideAvoidTime = 0.015f;
    public float maxCollisionAngle = 90f;
}
