using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // ���� ��� (�÷��̾�)
    public float followSpeed = 5f; // ī�޶� ���󰡴� �ӵ�
    public Vector3 offset = new Vector3(0, 5, -10); // ī�޶�� ��� ���� ������

    void Start()
    {
        // �ʱ� ������ Ÿ������ ����
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

    // ������ �ٲ� �� ȣ��Ǵ� �޼���
    public void UpdateTarget(Transform newTarget)
    {
        target = newTarget;
    }
}