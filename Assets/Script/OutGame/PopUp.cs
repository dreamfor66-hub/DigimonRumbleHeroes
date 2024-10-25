using UnityEngine;
using UnityEngine.UI;

public class PopUp : MonoBehaviour
{
    public Button closeButton;

    private void Start()
    {
        closeButton.onClick.AddListener(ClosePopUp);
    }

    private void ClosePopUp()
    {
        PopUpManager.Instance.ClosePopUp(gameObject);
    }
}
