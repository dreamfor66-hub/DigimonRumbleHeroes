using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data_BotAI_New", menuName = "Data/Bot AI Data", order = 1)]
public class BotAIData : ScriptableObject
{
    [Title("Bot AI States")]
    [TableList(AlwaysExpanded = true)]
    public List<BotAIState> botAIStates = new List<BotAIState>();
}