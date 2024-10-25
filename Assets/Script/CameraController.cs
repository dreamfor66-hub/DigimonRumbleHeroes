using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // 따라갈 대상 (플레이어)
    public float followSpeed = 5f; // 카메라가 따라가는 속도
    public Vector3 offset = new Vector3(0, 5, -10); // 카메라와 대상 간의 오프셋

    void Start()
    {
        // 초기 리더를 타겟으로 설정
        if (EntityContainer.Instance.LeaderPlayer != null)
        {
            target = EntityContainer.Instance.LeaderPlayer.transform;
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }

    // 리더가 바뀔 때 호출되는 메서드
    public void UpdateTarget(Transform newTarget)
    {
        target = newTarget;
    }
}