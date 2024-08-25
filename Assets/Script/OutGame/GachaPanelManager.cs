using System.Collections.Generic;
using UnityEngine;

public class GachaPanelManager : MonoBehaviour
{
    public List<CharacterItemInfo> gachaPool;
    public PlayerInventoryData playerInventory;

    public void OnGachaButtonClicked()
    {
        // 랜덤으로 하나 선택
        int randomIndex = Random.Range(0, gachaPool.Count);
        CharacterItemInfo selectedCharacter = gachaPool[randomIndex];

        // Player Inventory에 추가
        playerInventory.AddCharacterItem(selectedCharacter);
    }
}
