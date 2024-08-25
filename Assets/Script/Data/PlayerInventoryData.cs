using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System;
using System.Text;

[CreateAssetMenu(fileName = "PlayerInventoryData", menuName = "Data/PlayerInventoryData", order = 1)]
public class PlayerInventoryData : ScriptableObject
{
    [TableList(AlwaysExpanded = true)]
    public List<CharacterItemInfo> characterItems = new List<CharacterItemInfo>();

    // 시리얼 넘버 중복 체크 메서드
    public bool HasSerialNumber(string serialNumber)
    {
        foreach (var item in characterItems)
        {
            if (item.serialNumber == serialNumber)
                return true;
        }
        return false;
    }

    // 새로운 캐릭터 아이템을 추가하는 메서드
    public void AddCharacterItem(CharacterItemInfo info)
    {
        CharacterItemInfo newItem = new CharacterItemInfo(info.characterData, info.level, info.maxLevel, this);
        characterItems.Add(newItem);
        Debug.Log($"Added new character: {newItem.characterData.name} with serial number {newItem.serialNumber}");
    }

    // 시리얼 넘버를 이용해 캐릭터를 검색하는 메서드 추가
    public CharacterItemInfo GetCharacterBySerialNumber(string serialNumber)
    {
        foreach (var character in characterItems)
        {
            if (character.serialNumber == serialNumber)
            {
                return character;
            }
        }

        Debug.LogWarning($"Character with serial number {serialNumber} not found.");
        return null; // 해당 시리얼 넘버를 가진 캐릭터가 없을 경우 null 반환
    }
}


[System.Serializable]
public class CharacterItemInfo
{
    [ReadOnly]
    public string serialNumber;
    public CharacterData characterData;  // 캐릭터의 고유 데이터
    public int level;  // 캐릭터의 현재 레벨
    public int maxLevel;  // 캐릭터의 최대 레벨
    public int expStockConsumed; // 먹은 경험치 스톡의 양

    // 추가적인 정보가 필요할 때 여기에 추가 가능

    public CharacterItemInfo(CharacterData data, int level, int maxLevel, PlayerInventoryData inventory)
    {
        this.characterData = data;
        this.level = level;
        this.maxLevel = maxLevel;
        this.serialNumber = GenerateUniqueSerialNumber(inventory);
        this.expStockConsumed = 0; // 초기값 0
    }
    // 시리얼 넘버를 생성하는 메서드
    private string GenerateUniqueSerialNumber(PlayerInventoryData inventory)
    {
        string newSerial;
        do
        {
            newSerial = GenerateRandomSerialNumber(16);
        } while (inventory.HasSerialNumber(newSerial)); // 중복 체크

        return newSerial;
    }

    // 16자리 랜덤 시리얼 넘버 생성
    private string GenerateRandomSerialNumber(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StringBuilder result = new StringBuilder(length);
        System.Random random = new System.Random();

        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }

        return result.ToString();
    }
}