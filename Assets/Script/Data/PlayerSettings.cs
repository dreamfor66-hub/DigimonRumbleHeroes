using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Settings/PlayerSettings", order = 1)]
public class PlayerSettings : ScriptableObject
{
    private static PlayerSettings instance;

    public static PlayerSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<PlayerSettings>("Data_PlayerSettings");

                if (instance == null)
                {
                    Debug.LogError("PlayerSettings asset not found in Resources folder.");
                }
            }
            return instance;
        }
    }

    public float dragThreshold = 12f;
    public float tapThreshold = 0.2f;
    public float maxDistance = 200f;
}
