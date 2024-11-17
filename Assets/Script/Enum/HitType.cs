using Sirenix.OdinInspector;
using System;
using UnityEngine;

public enum HitType
{
    DamageOnly,
    Weak,
    Strong,
}

[Flags]
public enum HitTypeFilter
{
    None = 0,
    DamageOnly = 1 << 0,
    Weak = 1 << 1,
    Strong = 1 << 2,
    All = 1-2,
}