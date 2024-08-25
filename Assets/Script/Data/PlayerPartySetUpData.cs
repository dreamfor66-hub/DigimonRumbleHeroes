using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPartySetUpData", menuName = "Data/PlayerPartySetUpData", order = 1)]

[System.Serializable]
public class PlayerPartySetUpData : ScriptableObject
{
    public List<PartySetup> partySetups; // 파티 리스트
    public int currentPartyIndex;


    [System.Serializable]
    public class PartySetup
    {
        public string[] characterSerialNumbers = new string[3]; // 각 파티의 캐릭터 슬롯에 시리얼 넘버 저장
    }

}