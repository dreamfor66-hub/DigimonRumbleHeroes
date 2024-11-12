using UnityEngine;

public enum HitType
{
    DamageOnly,
    Weak,
    Strong,
    [HideInInspector] All = 0-10,
}