using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EntityContainer : SingletonBehaviour<EntityContainer>
{
    public List<PlayerController> PlayerCharacters = new List<PlayerController>();
    public PlayerController LeaderPlayer;

    private CameraController cameraController;
    public List<CharacterBehaviour> CharacterList { get; private set; } = new List<CharacterBehaviour>();

    public override void Init()
    {
        // 초기화 코드가 필요하면 여기에 작성
        cameraController = FindObjectOfType<CameraController>();
        if (cameraController != null && LeaderPlayer != null)
        {
            cameraController.UpdateTarget(LeaderPlayer.transform);
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        player.playerNumber = PlayerCharacters.Count + 1;  // 고유 넘버 부여
        PlayerCharacters.Add(player);

        if (player.isLeader)
        {
            LeaderPlayer = player;
        }
    }

    public PlayerController GetNextLeader(PlayerController currentLeader)
    {
        // 현재 리더의 넘버를 기준으로 다음 플레이어를 찾는다
        var nextLeader = PlayerCharacters
            .Where(p => p != currentLeader && p != null)
            .OrderBy(p => (p.playerNumber > currentLeader.playerNumber) ? p.playerNumber : int.MaxValue)
            .FirstOrDefault();

        // 만약 다음 리더가 없다면 첫 번째 플레이어로 돌아간다
        if (nextLeader == null)
        {
            nextLeader = PlayerCharacters
                .Where(p => p != currentLeader && p != null)
                .OrderBy(p => p.playerNumber)
                .FirstOrDefault();
        }

        return nextLeader;
    }

    public void RegisterCharacter(CharacterBehaviour character)
    {
        if (!CharacterList.Contains(character))
        {
            CharacterList.Add(character);
        }
    }

    public void ChangeLeader(PlayerController newLeader)
    {
        foreach (var player in PlayerCharacters)
        {
            player.isLeader = false;
        }

        newLeader.isLeader = true;
        LeaderPlayer = newLeader;

        // 리더가 바뀔 때 카메라 타겟 업데이트
        if (cameraController != null)
        {
            cameraController.UpdateTarget(newLeader.transform);
        }
    }

    public void UnregisterCharacter(CharacterBehaviour character)
    {
        if (character is PlayerController player)
        {
            PlayerCharacters.Remove(player);
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