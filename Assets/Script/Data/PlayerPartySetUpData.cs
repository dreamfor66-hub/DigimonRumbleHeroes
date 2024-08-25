using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPartySetUpData", menuName = "Data/PlayerPartySetUpData", order = 1)]

[System.Serializable]
public class PlayerPartySetUpData : ScriptableObject
{
    public List<PartySetup> partySetups; // ��Ƽ ����Ʈ
    public int currentPartyIndex;


    [System.Serializable]
    public class PartySetup
    {
        public string[] characterSerialNumbers = new string[3]; // �� ��Ƽ�� ĳ���� ���Կ� �ø��� �ѹ� ����
    }

}