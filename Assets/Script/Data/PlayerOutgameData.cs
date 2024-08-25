using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "Data_PlayerOutgame", menuName = "Data/PlayerOutgameData", order = 1)]
public class PlayerOutgameData : ScriptableObject
{
    public int gold; // ���
    public int gem; // ����
    public int expStock; // ����ġ ����

    public int playerRank; // �÷��̾� ��ũ
    public string playerName; // �÷��̾� �г���
    public int expConsumed; // ���� ��ũ���� ���� ����ġ


    private static string saveFilePath => Path.Combine(Application.persistentDataPath, "playerdata.json");

    // �����͸� JSON �������� ����
    public void SaveData()
    {
        // �� ������Ʈ�� �����͸� JSON �������� ��ȯ
        string jsonData = JsonUtility.ToJson(this, true);

        // ���Ͽ� JSON �����͸� ����
        File.WriteAllText(saveFilePath, jsonData);
        Debug.Log($"Data saved to {saveFilePath}");
    }

    // JSON ���Ͽ��� �����͸� �ҷ��� ����
    public void LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            // ���Ͽ��� JSON �����͸� �ҷ���
            string jsonData = File.ReadAllText(saveFilePath);

            // JSON �����͸� ���� ������Ʈ�� ����
            JsonUtility.FromJsonOverwrite(jsonData, this);
            Debug.Log($"Data loaded from {saveFilePath}");
        }
        else
        {
            Debug.LogWarning("Save file not found, initializing with default values.");
        }
    }
}