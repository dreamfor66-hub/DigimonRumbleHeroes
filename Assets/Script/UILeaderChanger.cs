using System.Linq;
using UnityEngine;

public class UILeaderChanger : MonoBehaviour
{
    public void OnSwitchLeaderButton()
    {
        var currentLeader = EntityContainer.Instance.LeaderPlayer;
        var nextLeader = EntityContainer.Instance.GetNextLeader(currentLeader);

        if (nextLeader != null)
        {
            EntityContainer.Instance.ChangeLeader(nextLeader);
        }
    }
}