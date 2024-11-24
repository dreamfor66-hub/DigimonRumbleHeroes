using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Animations;
using System;


[CreateAssetMenu(fileName = "Data_Bullet_New", menuName = "Data/Bullet Data", order = 1)]
[InlineEditor]
public class BulletData : ScriptableObject
{
    [PropertyOrder(0)]
    public float LiftTime = 0.5f;
    [PropertyOrder(0)]
    public BulletTrigger DespawnBy = BulletTrigger.LifeTime;
    [PropertyOrder(0)]
    public float BulletRadius = 0.3f;
    [PropertyOrder(0)]
    public bool ReflectOnWall = false;

    [Title("Move")]
    [PropertyOrder(1)]
    public BulletMoveType MoveType = BulletMoveType.ConstantSpeed;
    [PropertyOrder(1)]
    public float Speed = 1f;

    [Title("Hitboxes")]
    [PropertyOrder(1)]
    [TableList(AlwaysExpanded = true)]
    public List<HitboxData> HitboxList = new List<HitboxData>();

    [HideLabel]
    [PropertyOrder(1)]
    public List<HitData> HitIdList = new List<HitData>();

    [Title("SpawnBullet")]
    [PropertyOrder(2)]
    [TableList(AlwaysExpanded = true)]
    public List<BulletSpawnBulletData> BulletSpawnBulletList = new List<BulletSpawnBulletData>();
    
    [Title("SpawnVfx")]
    [PropertyOrder(3)]
    [TableList(AlwaysExpanded = true)]
    public List<BulletSpawnVfxData> BulletSpawnVfxList = new List<BulletSpawnVfxData>();

    public BulletData Clone()
    {
        // 얕은 복사 수행
        BulletData clonedBullet = (BulletData)MemberwiseClone();

        // HitIdList 복사
        clonedBullet.HitIdList = new List<HitData>();
        foreach (var hit in HitIdList)
        {
            clonedBullet.HitIdList.Add(hit.Clone()); // HitData에 Clone() 메서드가 있다고 가정
        }

        clonedBullet.HitboxList = new List<HitboxData>(HitboxList);
        clonedBullet.BulletSpawnBulletList = new List<BulletSpawnBulletData>(BulletSpawnBulletList);
        clonedBullet.BulletSpawnVfxList = new List<BulletSpawnVfxData>(BulletSpawnVfxList);

        return clonedBullet;
    }
}

[Flags]
public enum BulletTrigger
{
    None = 0,
    Spawn = 1 <<  0,
    Hit = 1 << 1,
    Despawn = 1 << 2,
    LifeTime = 1 <<3,
    CollideMap = 1 << 4,

    Custom1 = 1<<11,
    Custom2 = 1<<12,
    Custom3 = 1<<13,

    All = 1-4,
}

public enum BulletMoveType
{
    ConstantSpeed = 0,
}

[System.Serializable]
public class BulletSpawnBulletData
{
    public BulletTrigger SpawnTrigger;
    public BulletBehaviour BulletPrefab;
    public Vector2 Offset;
    public float Angle;
}

[System.Serializable]
public class BulletSpawnVfxData
{
    public BulletTrigger SpawnTrigger;
    public VfxObject VfxPrefab;
    public Vector2 Offset;
    public float Angle;
}

////////////
///
[System.Serializable]
public class SerializedBulletData
{
    public float lifeTime;
    public float speed;
    public List<HitboxData> hitboxList = new List<HitboxData>();
    public List<HitData> hitIdList = new List<HitData>();

    public SerializedBulletData() { }

    public SerializedBulletData(float lifeTime, float speed, List<HitboxData> hitboxes, List<HitData> hits)
    {
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.hitboxList = hitboxes;
        this.hitIdList = hits;
    }
}