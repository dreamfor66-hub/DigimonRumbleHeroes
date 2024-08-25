using UnityEngine;

[CreateAssetMenu(fileName = "Data_ResourceHolder", menuName = "Data/ResourceHolder", order = 1)]
public class ResourceHolder : ScriptableObject
{
    private static ResourceHolder instance;

    public static ResourceHolder Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ResourceHolder>("Data_ResourceHolder");
                if (instance == null)
                {
                    Debug.LogError("ResourceHolder asset not found in Resources folder.");
                }
            }
            return instance;
        }
    }

    public PlayerInventoryData playerInventoryData;
    public ModelTable modelTable;
    public PlayerOutgameData playerOutgameData;
    public PlayerPartySetUpData playerPartySetUpData;
    public GameVariables gameVariables;
}