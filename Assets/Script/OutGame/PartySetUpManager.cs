using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class PartySetUpManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PlayerPartySetUpData playerPartySetUpData; // ��Ƽ ���� ������
    public PlayerInventoryData playerInventoryData; // �÷��̾� �κ��丮 ������
    public Image[] characterImages; // ��Ƽ ĳ���� �̹��� (3��)
    public TextMeshProUGUI[] characterLevelTexts; // ��Ƽ ĳ���� ���� �ؽ�Ʈ (3��)
    public Button[] characterSlotButtons; // ĳ���� ���� ��ư (3��)
    public GameObject partySetUpInventoryPanel; // �κ��丮 �г�
    public GameObject[] partySelectionDots; // ��Ƽ ������ ���� ��(�ε�������)

    public RectTransform prevPartyTransform; // PrevPartyIndex ������Ʈ�� RectTransform
    public RectTransform currentPartyTransform; // CurrentPartyIndex ������Ʈ�� RectTransform
    public RectTransform nextPartyTransform; // NextPartyIndex ������Ʈ�� RectTransform

    private Vector2 prevInitialPos;
    private Vector2 currentInitialPos;
    private Vector2 nextInitialPos;
    public float snapSpeed = 10f; // ������ ���� �ӵ�
    public float snapThreshold = 0.2f; // ��ũ���� 20% �̻��� �巡���ؾ� ���� ����

    private int currentPartyIndex = 0; // ���� ���õ� ��Ƽ �ε���
    private int nextPartyIndex = 0; // ���� ��Ƽ �ε���
    private int prevPartyIndex = 0; // ���� ��Ƽ �ε���

    private Vector2 dragStartPos;
    private bool isDragging = false;
    private bool isMoving = false; // SmoothMove�� ���� ������ Ȯ���ϴ� �÷���

    private int selectedSlotIndex = -1; // ���� ���õ� ĳ���� ���� �ε���

    private void Start()
    {
        InitializePartySetUpPanel();
        UpdatePartyIndexes();
        UpdatePartyPages();
        UpdatePartySelectionDots();

        // ���� ��ư�� Ŭ�� ������ �߰�
        for (int i = 0; i < characterSlotButtons.Length; i++)
        {
            int index = i;
            characterSlotButtons[i].onClick.AddListener(() => OnCharacterSlotClicked(index));
        }

        // �ʱ� ��ġ ����
        prevInitialPos = prevPartyTransform.anchoredPosition;
        currentInitialPos = currentPartyTransform.anchoredPosition;
        nextInitialPos = nextPartyTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        InitializePartySetUpPanel();
        UpdatePartySelectionDots();
    }

    void OnDisable()
    {
        UpdateCurrentPartyIndex();
    }

    // ��Ƽ �г� �ʱ�ȭ
    private void InitializePartySetUpPanel()
    {
        var currentParty = playerPartySetUpData.partySetups[currentPartyIndex];

        for (int i = 0; i < characterImages.Length; i++)
        {
            string characterSerialNumber = currentParty.characterSerialNumbers[i];

            if (!string.IsNullOrEmpty(characterSerialNumber))
            {
                // �ø��� �ѹ��� ����Ͽ� �κ��丮���� �ش� ĳ���͸� ã��
                CharacterItemInfo characterItem = playerInventoryData.characterItems
                    .Find(item => item.serialNumber == characterSerialNumber);

                if (characterItem != null)
                {
                    characterImages[i].sprite = characterItem.characterData.characterSprite;
                    characterLevelTexts[i].text = "Lv. " + characterItem.level;
                }
                else
                {
                    // ���� �ø��� �ѹ��� ������ �ش� ĳ���͸� ã�� �� ���� ��� (�����Ǿ��� ���ɼ�)
                    characterImages[i].sprite = null; // �� ����
                    characterLevelTexts[i].text = "";
                }
            }
            else
            {
                // �ø��� �ѹ��� ���� ��� �� ���� ó��
                characterImages[i].sprite = null; // �� ����
                characterLevelTexts[i].text = "";
            }
        }
    }

    // �ε������� ������Ʈ
    private void UpdatePartySelectionDots()
    {
        for (int i = 0; i < partySelectionDots.Length; i++)
        {
            partySelectionDots[i].SetActive(i == currentPartyIndex);
        }
    }

    private void UpdatePartyIndexes()
    {
        prevPartyIndex = (currentPartyIndex - 1 + playerPartySetUpData.partySetups.Count) % playerPartySetUpData.partySetups.Count;
        nextPartyIndex = (currentPartyIndex + 1) % playerPartySetUpData.partySetups.Count;
    }

    // ��Ƽ ���� Ŭ�� �̺�Ʈ ó��
    public void OnCharacterSlotClicked(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        OpenInventoryForSlot(); // �κ��丮 �г��� ���ϴ�.
        partySetUpInventoryPanel.GetComponent<InventoryManager>().OpenInventoryForSlot(slotIndex); // �κ��丮 �гο� ���� �ε����� ����
    }

    // �κ��丮�� ���� ĳ���͸� ������ �� �ֵ��� ��
    private void OpenInventoryForSlot()
    {
        partySetUpInventoryPanel.SetActive(true);
    }

    // ���õ� ĳ���͸� ���Կ� ��ġ
    public void SetCharacterInSlot(int slotIndex, string characterSerialNumber)
    {
        if (slotIndex < 0 || slotIndex >= 3)
            return;

        var currentParty = playerPartySetUpData.partySetups[currentPartyIndex];
        currentParty.characterSerialNumbers[slotIndex] = characterSerialNumber;

        InitializePartySetUpPanel(); // ��Ƽ �г��� �����Ͽ� UI�� �ݿ�
        CloseInventoryPanel(); // �κ��丮 �г��� ����Cha
    }
    // �κ��丮 �г� �ݱ�
    public void CloseInventoryPanel()
    {
        partySetUpInventoryPanel.SetActive(false);
    }

    // �ڷΰ��� ��ư Ŭ�� �� �κ��丮 �г��� ���������� ����
    public void OnBackButtonClicked()
    {
        if (partySetUpInventoryPanel.activeSelf)
        {
            CloseInventoryPanel();
        }
        else
        {
            partySetUpInventoryPanel.SetActive(false);
        }
    }

    // ĳ���� ��ư�� ������ �� �ʱ�ȭ
    public void OnCharacterButtonClicked()
    {
        CloseInventoryPanel();
    }

    // �巡�� ���� �� ȣ��Ǵ� �޼���
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isMoving) return; // �̵� ���� �� �巡�׸� ����

        isDragging = true;
        dragStartPos = eventData.position;
        StopAllCoroutines(); // �巡�� ���� �� ��� �ڷ�ƾ ����
    }

    // �巡�� ���� �� ȣ��Ǵ� �޼���
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        float deltaX = eventData.position.x - dragStartPos.x;
        MovePanels(deltaX);
    }

    // �巡�� ���� �� ȣ��Ǵ� �޼���
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        float dragDistance = eventData.position.x - dragStartPos.x;

        if (Mathf.Abs(dragDistance) > Screen.width * snapThreshold)
        {
            bool toPreviousPage = dragDistance > 0;
            SnapToPage(toPreviousPage);
        }
        else
        {
            SnapToCurrentPage();
        }

        isDragging = false;

        UpdateCurrentPartyIndex();
    }

    private void UpdateCurrentPartyIndex()
    {
        // ����: currentPartyTransform�� ��ġ�� ������� ���� �ε��� ����
        // ����: ��ũ�� ��ġ�� �ٸ� ���� ������� currentPartyIndex ������Ʈ
        int newIndex = DetermineCurrentPartyIndex();
        ResourceHolder.Instance.playerPartySetUpData.currentPartyIndex = newIndex;
    }

    // ����: ���� ��Ƽ �ε����� �����ϴ� �޼���
    private int DetermineCurrentPartyIndex()
    {
        // ���� ��Ƽ �ε����� ����ϴ� �� ���� (��: currentPartyTransform�� anchoredPosition ���)
        // �� ���������� �ܼ��� ���� �ε����� ��ȯ�ϴ� �������� �����մϴ�.
        // �����δ� SnapToPage�� �ٸ� �޼��忡�� ���� ���� ��ȯ�ؾ� �մϴ�.
        return currentPartyIndex;
    }

    private void MovePanels(float deltaX)
    {
        currentPartyTransform.anchoredPosition = currentInitialPos + new Vector2(deltaX, 0);
        prevPartyTransform.anchoredPosition = prevInitialPos + new Vector2(deltaX, 0);
        nextPartyTransform.anchoredPosition = nextInitialPos + new Vector2(deltaX, 0);
    }

    private void SnapToPage(bool toPreviousPage)
    {
        isMoving = true; // �̵� ����

        int completedCount = 0; // �Ϸ�� �ڷ�ƾ�� ���� ����

        System.Action onSmoothMoveComplete = () =>
        {
            completedCount++;
        };

        if (toPreviousPage)
        {
            StartCoroutine(SmoothMove(currentPartyTransform, nextInitialPos, snapSpeed, onSmoothMoveComplete));
            StartCoroutine(SmoothMove(prevPartyTransform, currentInitialPos, snapSpeed, onSmoothMoveComplete));
            StartCoroutine(SmoothMove(nextPartyTransform, nextInitialPos + new Vector2(Screen.width, 0), snapSpeed, onSmoothMoveComplete));
        }
        else
        {
            StartCoroutine(SmoothMove(currentPartyTransform, prevInitialPos, snapSpeed, onSmoothMoveComplete));
            StartCoroutine(SmoothMove(prevPartyTransform, prevInitialPos - new Vector2(Screen.width, 0), snapSpeed, onSmoothMoveComplete));
            StartCoroutine(SmoothMove(nextPartyTransform, currentInitialPos, snapSpeed, onSmoothMoveComplete));
        }

        // ��� �ڷ�ƾ�� ���� �Ŀ� ResetPanelsAfterSnap ȣ��
        StartCoroutine(WaitForAllSmoothMoves(() => completedCount >= 3, toPreviousPage));
    }

    private IEnumerator ResetPanelsAfterSnap(bool toPreviousPage)
    {
        yield return new WaitForSeconds(1 / snapSpeed);

        if (toPreviousPage)
        {
            currentPartyIndex = (currentPartyIndex - 1 + playerPartySetUpData.partySetups.Count) % playerPartySetUpData.partySetups.Count;
        }
        else
        {
            currentPartyIndex = (currentPartyIndex + 1) % playerPartySetUpData.partySetups.Count;
        }

        UpdatePartyIndexes();
        UpdatePartyPages();
        // �гε��� �ʱ� ��ġ�� ����
        prevPartyTransform.anchoredPosition = prevInitialPos;
        currentPartyTransform.anchoredPosition = currentInitialPos;
        nextPartyTransform.anchoredPosition = nextInitialPos;

        UpdatePartySelectionDots();
        isMoving = false; // �̵� �Ϸ�
    }

    private void SnapToCurrentPage()
    {
        isMoving = true; // �̵� ����

        StartCoroutine(SmoothMove(currentPartyTransform, currentInitialPos, snapSpeed));
        StartCoroutine(SmoothMove(prevPartyTransform, prevInitialPos, snapSpeed));
        StartCoroutine(SmoothMove(nextPartyTransform, nextInitialPos, snapSpeed));

        StartCoroutine(EndMoveAfterSnap());
    }

    private IEnumerator SmoothMove(RectTransform panel, Vector2 targetPosition, float speed, System.Action onComplete = null)
    {
        Vector2 startPosition = panel.anchoredPosition;
        float elapsedTime = 0f;
        float journeyLength = Vector2.Distance(startPosition, targetPosition);

        while (elapsedTime < journeyLength / speed)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime * speed / journeyLength);
            panel.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);

            yield return null;
        }

        panel.anchoredPosition = targetPosition;
        onComplete?.Invoke(); // �ڷ�ƾ ���� �� �ݹ� ����
    }

    private IEnumerator WaitForAllSmoothMoves(System.Func<bool> allSmoothMovesCompleted, bool toPreviousPage)
    {
        // ��� �ڷ�ƾ�� �Ϸ�� ������ ���
        while (!allSmoothMovesCompleted())
        {
            yield return null; // ��� �ڷ�ƾ�� �Ϸ�� ������ ���
        }

        // ��� �ڷ�ƾ �Ϸ� �� �г� �ʱ�ȭ �� ����
        StartCoroutine(ResetPanelsAfterSnap(toPreviousPage));
    }

    private IEnumerator EndMoveAfterSnap()
    {
        yield return new WaitForSeconds(1 / snapSpeed);
        isMoving = false; // �̵� �Ϸ�
    }

    private void UpdatePartyPages()
    {
        SetPartyData(prevPartyTransform, playerPartySetUpData.partySetups[prevPartyIndex]);
        SetPartyData(currentPartyTransform, playerPartySetUpData.partySetups[currentPartyIndex]);
        SetPartyData(nextPartyTransform, playerPartySetUpData.partySetups[nextPartyIndex]);
    }

    private void SetPartyData(RectTransform partyTransform, PlayerPartySetUpData.PartySetup partySetup)
    {
        for (int i = 0; i < characterSlotButtons.Length; i++)
        {
            string serialNumber = partySetup.characterSerialNumbers[i];
            CharacterItemInfo characterItem = playerInventoryData.characterItems
                .Find(item => item.serialNumber == serialNumber);

            if (characterItem != null)
            {
                var characterImage = partyTransform.GetChild(i).GetComponent<Image>();
                var characterLevelText = partyTransform.GetChild(i).GetComponentInChildren<TextMeshProUGUI>();

                characterImage.sprite = characterItem.characterData.characterSprite;
                characterLevelText.text = "Lv. " + characterItem.level;
            }
            else
            {
                var characterImage = partyTransform.GetChild(i).GetComponent<Image>();
                var characterLevelText = partyTransform.GetChild(i).GetComponentInChildren<TextMeshProUGUI>();

                characterImage.sprite = null; // �� ����
                characterLevelText.text = "";
            }
        }
    }
}
