using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;
using System;

[CreateAssetMenu(fileName = "Data_Bullet_New", menuName = "Data/Bullet Data", order = 1)]
public class BulletData : ScriptableObject
{
    [PropertyOrder(0)]
    public float LiftTime = 0.5f;
    [PropertyOrder(0)]
    public BulletTrigger DespawnBy = BulletTrigger.LifeTime;

    [Title("Move")]
    [PropertyOrder(1)]
    public float Speed = 1f;

    [Title("Hitboxes")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<HitboxData> HitboxList = new List<HitboxData>();

    [HideLabel]
    [PropertyOrder(1)]
    public List<HitData> HitIdList = new List<HitData>();

}

[Flags]
public enum BulletTrigger
{
    None = 0,
    Spawn = 1 <<  0,
    Hit = 1 << 1,
    Despawn = 1 << 2,
    LifeTime = 1 <<3,

    All = 1-3,
}

public enum BulletMoveType
{
    ConstantSpeed = 0,
}