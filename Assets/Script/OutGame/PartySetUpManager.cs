using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class PartySetUpManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PlayerPartySetUpData playerPartySetUpData; // 파티 설정 데이터
    public PlayerInventoryData playerInventoryData; // 플레이어 인벤토리 데이터
    public Image[] characterImages; // 파티 캐릭터 이미지 (3개)
    public TextMeshProUGUI[] characterLevelTexts; // 파티 캐릭터 레벨 텍스트 (3개)
    public Button[] characterSlotButtons; // 캐릭터 슬롯 버튼 (3개)
    public GameObject partySetUpInventoryPanel; // 인벤토리 패널
    public GameObject[] partySelectionDots; // 파티 선택을 위한 점(인디케이터)

    public RectTransform prevPartyTransform; // PrevPartyIndex 오브젝트의 RectTransform
    public RectTransform currentPartyTransform; // CurrentPartyIndex 오브젝트의 RectTransform
    public RectTransform nextPartyTransform; // NextPartyIndex 오브젝트의 RectTransform

    private Vector2 prevInitialPos;
    private Vector2 currentInitialPos;
    private Vector2 nextInitialPos;
    public float snapSpeed = 10f; // 페이지 스냅 속도
    public float snapThreshold = 0.2f; // 스크린의 20% 이상을 드래그해야 스냅 성공

    private int currentPartyIndex = 0; // 현재 선택된 파티 인덱스
    private int nextPartyIndex = 0; // 다음 파티 인덱스
    private int prevPartyIndex = 0; // 이전 파티 인덱스

    private Vector2 dragStartPos;
    private bool isDragging = false;
    private bool isMoving = false; // SmoothMove가 실행 중인지 확인하는 플래그

    private int selectedSlotIndex = -1; // 현재 선택된 캐릭터 슬롯 인덱스

    private void Start()
    {
        InitializePartySetUpPanel();
        UpdatePartyIndexes();
        UpdatePartyPages();
        UpdatePartySelectionDots();

        // 슬롯 버튼에 클릭 리스너 추가
        for (int i = 0; i < characterSlotButtons.Length; i++)
        {
            int index = i;
            characterSlotButtons[i].onClick.AddListener(() => OnCharacterSlotClicked(index));
        }

        // 초기 위치 저장
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

    // 파티 패널 초기화
    private void InitializePartySetUpPanel()
    {
        var currentParty = playerPartySetUpData.partySetups[currentPartyIndex];

        for (int i = 0; i < characterImages.Length; i++)
        {
            string characterSerialNumber = currentParty.characterSerialNumbers[i];

            if (!string.IsNullOrEmpty(characterSerialNumber))
            {
                // 시리얼 넘버를 사용하여 인벤토리에서 해당 캐릭터를 찾음
                CharacterItemInfo characterItem = playerInventoryData.characterItems
                    .Find(item => item.serialNumber == characterSerialNumber);

                if (characterItem != null)
                {
                    characterImages[i].sprite = characterItem.characterData.characterSprite;
                    characterLevelTexts[i].text = "Lv. " + characterItem.level;
                }
                else
                {
                    // 만약 시리얼 넘버가 있지만 해당 캐릭터를 찾을 수 없는 경우 (삭제되었을 가능성)
                    characterImages[i].sprite = null; // 빈 슬롯
                    characterLevelTexts[i].text = "";
                }
            }
            else
            {
                // 시리얼 넘버가 없는 경우 빈 슬롯 처리
                characterImages[i].sprite = null; // 빈 슬롯
                characterLevelTexts[i].text = "";
            }
        }
    }

    // 인디케이터 업데이트
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

    // 파티 슬롯 클릭 이벤트 처리
    public void OnCharacterSlotClicked(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        OpenInventoryForSlot(); // 인벤토리 패널을 엽니다.
        partySetUpInventoryPanel.GetComponent<InventoryManager>().OpenInventoryForSlot(slotIndex); // 인벤토리 패널에 슬롯 인덱스를 전달
    }

    // 인벤토리를 열어 캐릭터를 선택할 수 있도록 함
    private void OpenInventoryForSlot()
    {
        partySetUpInventoryPanel.SetActive(true);
    }

    // 선택된 캐릭터를 슬롯에 배치
    public void SetCharacterInSlot(int slotIndex, string characterSerialNumber)
    {
        if (slotIndex < 0 || slotIndex >= 3)
            return;

        var currentParty = playerPartySetUpData.partySetups[currentPartyIndex];
        currentParty.characterSerialNumbers[slotIndex] = characterSerialNumber;

        InitializePartySetUpPanel(); // 파티 패널을 갱신하여 UI에 반영
        CloseInventoryPanel(); // 인벤토리 패널을 닫음Cha
    }
    // 인벤토리 패널 닫기
    public void CloseInventoryPanel()
    {
        partySetUpInventoryPanel.SetActive(false);
    }

    // 뒤로가기 버튼 클릭 시 인벤토리 패널이 열려있으면 닫음
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

    // 캐릭터 버튼이 눌렸을 때 초기화
    public void OnCharacterButtonClicked()
    {
        CloseInventoryPanel();
    }

    // 드래그 시작 시 호출되는 메서드
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isMoving) return; // 이동 중일 때 드래그를 차단

        isDragging = true;
        dragStartPos = eventData.position;
        StopAllCoroutines(); // 드래그 시작 시 모든 코루틴 중지
    }

    // 드래그 중일 때 호출되는 메서드
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        float deltaX = eventData.position.x - dragStartPos.x;
        MovePanels(deltaX);
    }

    // 드래그 종료 시 호출되는 메서드
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
        // 예시: currentPartyTransform의 위치를 기반으로 현재 인덱스 결정
        // 예제: 스크롤 위치나 다른 논리를 기반으로 currentPartyIndex 업데이트
        int newIndex = DetermineCurrentPartyIndex();
        ResourceHolder.Instance.playerPartySetUpData.currentPartyIndex = newIndex;
    }

    // 예시: 현재 파티 인덱스를 결정하는 메서드
    private int DetermineCurrentPartyIndex()
    {
        // 현재 파티 인덱스를 계산하는 논리 구현 (예: currentPartyTransform의 anchoredPosition 기반)
        // 이 예제에서는 단순히 현재 인덱스를 반환하는 로직으로 설정합니다.
        // 실제로는 SnapToPage나 다른 메서드에서 계산된 값을 반환해야 합니다.
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
        isMoving = true; // 이동 시작

        int completedCount = 0; // 완료된 코루틴의 수를 추적

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

        // 모든 코루틴이 끝난 후에 ResetPanelsAfterSnap 호출
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
        // 패널들을 초기 위치로 리셋
        prevPartyTransform.anchoredPosition = prevInitialPos;
        currentPartyTransform.anchoredPosition = currentInitialPos;
        nextPartyTransform.anchoredPosition = nextInitialPos;

        UpdatePartySelectionDots();
        isMoving = false; // 이동 완료
    }

    private void SnapToCurrentPage()
    {
        isMoving = true; // 이동 시작

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
        onComplete?.Invoke(); // 코루틴 종료 시 콜백 실행
    }

    private IEnumerator WaitForAllSmoothMoves(System.Func<bool> allSmoothMovesCompleted, bool toPreviousPage)
    {
        // 모든 코루틴이 완료될 때까지 대기
        while (!allSmoothMovesCompleted())
        {
            yield return null; // 모든 코루틴이 완료될 때까지 대기
        }

        // 모든 코루틴 완료 후 패널 초기화 및 리셋
        StartCoroutine(ResetPanelsAfterSnap(toPreviousPage));
    }

    private IEnumerator EndMoveAfterSnap()
    {
        yield return new WaitForSeconds(1 / snapSpeed);
        isMoving = false; // 이동 완료
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

                characterImage.sprite = null; // 빈 슬롯
                characterLevelText.text = "";
            }
        }
    }
}
