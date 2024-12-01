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
    public GameVariables gameVariables;

    public GameObject IndicatorLinePrefab;
    public GameObject IndicatorCirclePrefab;

    public GameObject GetIndicatorPrefab(IndicatorType type)
    {
        switch (type)
        {
            case IndicatorType.Line:
                return IndicatorLinePrefab;
            case IndicatorType.Circle:
                return IndicatorCirclePrefab;
            default:
                Debug.LogError($"Invalid IndicatorType: {type}");
                return null;
        }
    }
}