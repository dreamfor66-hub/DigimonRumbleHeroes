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

    // �ø��� �ѹ� �ߺ� üũ �޼���
    public bool HasSerialNumber(string serialNumber)
    {
        foreach (var item in characterItems)
        {
            if (item.serialNumber == serialNumber)
                return true;
        }
        return false;
    }

    // ���ο� ĳ���� �������� �߰��ϴ� �޼���
    public void AddCharacterItem(CharacterItemInfo info)
    {
        CharacterItemInfo newItem = new CharacterItemInfo(info.characterData, info.level, info.maxLevel, this);
        characterItems.Add(newItem);
        Debug.Log($"Added new character: {newItem.characterData.name} with serial number {newItem.serialNumber}");
    }

    // �ø��� �ѹ��� �̿��� ĳ���͸� �˻��ϴ� �޼��� �߰�
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
        return null; // �ش� �ø��� �ѹ��� ���� ĳ���Ͱ� ���� ��� null ��ȯ
    }
}


[System.Serializable]
public class CharacterItemInfo
{
    [ReadOnly]
    public string serialNumber;
    public CharacterData characterData;  // ĳ������ ���� ������
    public int level;  // ĳ������ ���� ����
    public int maxLevel;  // ĳ������ �ִ� ����
    public int expStockConsumed; // ���� ����ġ ������ ��

    // �߰����� ������ �ʿ��� �� ���⿡ �߰� ����

    public CharacterItemInfo(CharacterData data, int level, int maxLevel, PlayerInventoryData inventory)
    {
        this.characterData = data;
        this.level = level;
        this.maxLevel = maxLevel;
        this.serialNumber = GenerateUniqueSerialNumber(inventory);
        this.expStockConsumed = 0; // �ʱⰪ 0
    }
    // �ø��� �ѹ��� �����ϴ� �޼���
    private string GenerateUniqueSerialNumber(PlayerInventoryData inventory)
    {
        string newSerial;
        do
        {
            newSerial = GenerateRandomSerialNumber(16);
        } while (inventory.HasSerialNumber(newSerial)); // �ߺ� üũ

        return newSerial;
    }

    // 16�ڸ� ���� �ø��� �ѹ� ����
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