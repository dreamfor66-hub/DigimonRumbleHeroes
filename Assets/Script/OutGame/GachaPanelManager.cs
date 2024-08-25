using System.Collections.Generic;
using UnityEngine;

public class GachaPanelManager : MonoBehaviour
{
    public List<CharacterItemInfo> gachaPool;
    public PlayerInventoryData playerInventory;

    public void OnGachaButtonClicked()
    {
        // �������� �ϳ� ����
        int randomIndex = Random.Range(0, gachaPool.Count);
        CharacterItemInfo selectedCharacter = gachaPool[randomIndex];

        // Player Inventory�� �߰�
        playerInventory.AddCharacterItem(selectedCharacter);
    }
}
