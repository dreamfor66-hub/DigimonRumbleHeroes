using UnityEngine;
using UnityEngine.UI;

public class UIInputBubble : MonoBehaviour
{
    public RectTransform bubbleTransform;
    public Animator bubbleAnimator;

    private Vector2 initialTouchPosition;
    private bool isTouching = false;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchStart(touch.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnTouchMove(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnTouchEnd();
                    break;
            }
        }
    }

    private void OnTouchStart(Vector2 touchPosition)
    {
        isTouching = true;
        initialTouchPosition = touchPosition;
        bubbleTransform.position = touchPosition;
        bubbleAnimator.SetTrigger("TouchStart");
        bubbleTransform.gameObject.SetActive(true);
    }

    private void OnTouchMove(Vector2 touchPosition)
    {
        if (!isTouching) return;

        Vector2 direction = touchPosition - initialTouchPosition;
        float horizontal = direction.x / Screen.width; // Normalize based on screen width
        float vertical = direction.y / Screen.height;  // Normalize based on screen height

        bubbleAnimator.SetFloat("Horizontal", horizontal);
        bubbleAnimator.SetFloat("Vertical", vertical);

        // 비눗방울 위치 업데이트 (애니메이션 효과)
        bubbleTransform.position = initialTouchPosition + direction;
    }

    private void OnTouchEnd()
    {
        isTouching = false;
        bubbleAnimator.SetTrigger("TouchEnd");
        bubbleTransform.gameObject.SetActive(false);
    }
}
