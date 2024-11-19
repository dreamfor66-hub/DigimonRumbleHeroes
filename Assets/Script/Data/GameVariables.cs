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

    //일반넉백과 스매시 넉백을 구분할 수 있는 넉백기준값
    public float knockBackPowerReference = 7f;
    public float collideReduceMultiplier = 0.5f;
    public float hitstopWhenCollide = 5.5f;
    public float collideAvoidTime = 0.015f;
    public float maxCollisionAngle = 90f;
}
