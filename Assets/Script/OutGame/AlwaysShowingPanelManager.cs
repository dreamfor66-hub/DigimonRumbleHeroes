using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AlwaysShowingPanel : MonoBehaviour
{
    public static AlwaysShowingPanel Instance { get; private set; }
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI playerRankText;
    public Slider expSlider;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI gemText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(UpdateRoutine());
        UpdatePlayerInfo();
    }

    // 플레이어 정보를 업데이트하는 메서드
    public void UpdatePlayerInfo()
    {
        var data = ResourceHolder.Instance.playerOutgameData;

        playerNameText.text = data.playerName;
        playerRankText.text = $"Rank {data.playerRank}";
        expSlider.value = (float)data.expConsumed / GetCurrentRankRequiredExp(data);
        goldText.text = data.gold.ToString();
        gemText.text = data.gem.ToString();
    }

    // 현재 랭크에 필요한 경험치를 반환하는 메서드
    private int GetCurrentRankRequiredExp(PlayerOutgameData data)
    {
        var expRequiredList = ResourceHolder.Instance.gameVariables.expRequiredPerRank;
        if (data.playerRank - 1 < expRequiredList.Count)
        {
            return expRequiredList[data.playerRank - 1];
        }
        return expRequiredList[expRequiredList.Count - 1]; // 만렙일 경우 최대값 반환
    }

    // 데이터가 갱신될 때마다 이 메서드를 호출하여 UI를 갱신할 수 있음
    public void OnPlayerDataChanged()
    {
        UpdatePlayerInfo();
    }

    private IEnumerator UpdateRoutine()
    {
        while (true)
        {
            UpdatePlayerInfo();
            yield return new WaitForSeconds(5f); // 10초마다 업데이트
        }
    }

    public void ChangeGold(int amount)
    {
        ResourceHolder.Instance.playerOutgameData.gold += amount;
        ResourceHolder.Instance.playerOutgameData.SaveData();
        AlwaysShowingPanel.Instance.OnPlayerDataChanged();
    }
    
    public void ChangeGem(int amount)
    {
        ResourceHolder.Instance.playerOutgameData.gem += amount;
        ResourceHolder.Instance.playerOutgameData.SaveData();
        AlwaysShowingPanel.Instance.OnPlayerDataChanged();
    }
}
