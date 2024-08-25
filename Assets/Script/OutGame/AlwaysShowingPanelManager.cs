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

    // �÷��̾� ������ ������Ʈ�ϴ� �޼���
    public void UpdatePlayerInfo()
    {
        var data = ResourceHolder.Instance.playerOutgameData;

        playerNameText.text = data.playerName;
        playerRankText.text = $"Rank {data.playerRank}";
        expSlider.value = (float)data.expConsumed / GetCurrentRankRequiredExp(data);
        goldText.text = data.gold.ToString();
        gemText.text = data.gem.ToString();
    }

    // ���� ��ũ�� �ʿ��� ����ġ�� ��ȯ�ϴ� �޼���
    private int GetCurrentRankRequiredExp(PlayerOutgameData data)
    {
        var expRequiredList = ResourceHolder.Instance.gameVariables.expRequiredPerRank;
        if (data.playerRank - 1 < expRequiredList.Count)
        {
            return expRequiredList[data.playerRank - 1];
        }
        return expRequiredList[expRequiredList.Count - 1]; // ������ ��� �ִ밪 ��ȯ
    }

    // �����Ͱ� ���ŵ� ������ �� �޼��带 ȣ���Ͽ� UI�� ������ �� ����
    public void OnPlayerDataChanged()
    {
        UpdatePlayerInfo();
    }

    private IEnumerator UpdateRoutine()
    {
        while (true)
        {
            UpdatePlayerInfo();
            yield return new WaitForSeconds(5f); // 10�ʸ��� ������Ʈ
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
