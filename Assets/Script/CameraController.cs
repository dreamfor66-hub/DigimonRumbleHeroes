using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // 따라갈 대상 (플레이어)
    public float followSpeed = 5f; // 카메라가 따라가는 속도
    public Vector3 offset = new Vector3(0, 5, -10); // 카메라와 대상 간의 오프셋

    void Start()
    {

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

    // 카메라로 따라가야 할 캐릭터 대상이 바뀔 수 있도록 해주는 메서드
    public void UpdateTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}