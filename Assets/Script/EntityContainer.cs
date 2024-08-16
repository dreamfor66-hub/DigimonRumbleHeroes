using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityContainer : SingletonBehaviour<EntityContainer>
{
    public PlayerController PlayerCharacter { get; private set; }
    public List<CharacterBehaviour> CharacterList { get; private set; } = new List<CharacterBehaviour>();

    public override void Init()
    {
        // �ʱ�ȭ �ڵ尡 �ʿ��ϸ� ���⿡ �ۼ�
    }

    public void RegisterCharacter(CharacterBehaviour character)
    {
        if (character is PlayerController player)
        {
            PlayerCharacter = player;
        }

        if (!CharacterList.Contains(character))
        {
            CharacterList.Add(character);
        }
    }

    public void UnregisterCharacter(CharacterBehaviour character)
    {
        if (character is PlayerController player && PlayerCharacter == player)
        {
            PlayerCharacter = null;
        }

        if (CharacterList.Contains(character))
        {
            CharacterList.Remove(character);
        }
    }

    public CharacterBehaviour GetCharacterNearestTo(Vector3 position)
    {
        return CharacterList.OrderBy(c => Vector3.Distance(c.transform.position, position)).FirstOrDefault();
    }
}