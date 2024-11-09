using System;
using Mirror;
using Sirenix.OdinInspector;
using UnityEngine;

    public enum VfxPlayType
    {
        [Tooltip("소환 후 일정시간이 지나면 자동으로 사라집니다.")]
        Time,

        [Tooltip("캐릭터의 상태에 따라 제어되는 Vfx 입니다. 주로 Action 에서 사용됩니다.")]
        Manual,

        [Tooltip("계속 반복되며 코드에 의해 제어되는 Vfx 입니다. 유지되는 버프 등에 사용됩니다.")]
        Loop,
    }

    public enum VfxFollowType
    {
        None,
        Position,
        PositionAndRotation,
        Rotation,
    }

[RequireComponent(typeof(ParticleSystem))]
public class VfxObject : NetworkBehaviour
{
    [SerializeField] public ParticleSystem particle;

    public VfxPlayType PlayType;
    public VfxFollowType StartPivot = VfxFollowType.PositionAndRotation;
    public VfxFollowType FollowType;

    public float spawnFrame;

    [ShowIf(nameof(PlayType), VfxPlayType.Time)]
    public float DespawnTime = 1f;

    [HideIf(nameof(PlayType), VfxPlayType.Manual)]
    [Tooltip("Vfx의 재생 및 Despawn 시간계산이 실제 시간이 아닌 게임상 시간으로 제어됩니다.")]
    public bool UseGameTime;
    public bool UseAutoRandomSeed = true;

    private Transform followTm;
    private Vector3 localPosition;
    private Quaternion localRotation;

    public float elapsedTime = 0f;
    private float elapsedGameTime;

    //private void OnEnable()
    //{
    //    if (PlayType != VfxPlayType.Manual)
    //        UpdateManager.Register(this);
    //}

    //private void OnDisable()
    //{
    //    if (PlayType != VfxPlayType.Manual)
    //        UpdateManager.Release(this);
    //}

    private void Update()
        {
            if (PlayType == VfxPlayType.Time && !UseGameTime)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime >= DespawnTime)
                    Destroy(gameObject);
            }
        }

        private void LateUpdate()
        {
            FollowTransform();
        }

        public void UpdateState(float deltaTime)
        {
            if (PlayType != VfxPlayType.Manual && UseGameTime)
            {
                elapsedGameTime += deltaTime;
                if (elapsedGameTime >= DespawnTime && PlayType != VfxPlayType.Loop)
                    Destroy(gameObject);
        }
        }

        public void UpdateVisual(float deltaTime)
        {
            if (PlayType != VfxPlayType.Manual && UseGameTime)
            {
                particle.Simulate(elapsedGameTime, true, true, false);
            }
        }

        public void SetTransform(Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale)
        {
            followTm = parent;
            this.localPosition = localPos;
            this.localRotation = localRot;

            var position = StartPivot != VfxFollowType.None ? parent.rotation * localPos + parent.position : localPos;
            var rotation = StartPivot == VfxFollowType.PositionAndRotation ? parent.rotation * localRot : localRot;

            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = localScale;
        }

    public void SetTime(float time)
    {
        elapsedTime = time; // HitStop 해제 시 이어서 재개하기 위해 시간을 저장
        particle.Simulate(time, true, true, false);
    }

    public void Stop()
    {
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void FollowTransform()
        {
            if (FollowType == VfxFollowType.None)
                return;
            if (followTm == null)
                return;

            if (FollowType == VfxFollowType.Position)
            {
                var position = followTm.rotation * localPosition + followTm.position;
                transform.position = position;
            }
            else if (FollowType == VfxFollowType.Rotation)
            {
                var rotation = followTm.rotation * localRotation;
                transform.rotation = rotation;
            }
            else if (FollowType == VfxFollowType.PositionAndRotation)
            {
                var position = followTm.TransformPoint(localPosition);
                var rotation = followTm.rotation * localRotation;
                transform.SetPositionAndRotation(position, rotation);
            }
        }
    public void OnSpawn(float spawnFrame)
    {
        this.spawnFrame = spawnFrame;
        elapsedTime = 0f;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (UseAutoRandomSeed)
        {
            var seed = BitConverter.ToUInt32(BitConverter.GetBytes(UnityEngine.Random.Range(0, Int32.MaxValue)));
            particle.useAutoRandomSeed = false;
            particle.randomSeed = seed;
        }

        if (PlayType == VfxPlayType.Manual || UseGameTime)
        {
            particle.Pause();
        }
        else
        {
            particle.Play();
        }
    }

    public void OnDespawn()
    {
        followTm = null;
        Stop();
        Destroy(gameObject);
    }
}

