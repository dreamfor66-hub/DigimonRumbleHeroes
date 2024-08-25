using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "Data_PlayerOutgame", menuName = "Data/PlayerOutgameData", order = 1)]
public class PlayerOutgameData : ScriptableObject
{
    public int gold; // 골드
    public int gem; // 보석
    public int expStock; // 경험치 스톡

    public int playerRank; // 플레이어 랭크
    public string playerName; // 플레이어 닉네임
    public int expConsumed; // 현재 랭크에서 사용된 경험치


    private static string saveFilePath => Path.Combine(Application.persistentDataPath, "playerdata.json");

    // 데이터를 JSON 형식으로 저장
    public void SaveData()
    {
        // 이 오브젝트의 데이터를 JSON 형식으로 변환
        string jsonData = JsonUtility.ToJson(this, true);

        // 파일에 JSON 데이터를 저장
        File.WriteAllText(saveFilePath, jsonData);
        Debug.Log($"Data saved to {saveFilePath}");
    }

    // JSON 파일에서 데이터를 불러와 적용
    public void LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            // 파일에서 JSON 데이터를 불러옴
            string jsonData = File.ReadAllText(saveFilePath);

            // JSON 데이터를 현재 오브젝트에 적용
            JsonUtility.FromJsonOverwrite(jsonData, this);
            Debug.Log($"Data loaded from {saveFilePath}");
        }
        else
        {
            Debug.LogWarning("Save file not found, initializing with default values.");
        }
    }
}