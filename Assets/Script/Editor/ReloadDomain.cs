using UnityEditor;
using UnityEngine;

public class ReloadDomain
{
    [MenuItem("Tools/Reload Domain")]
    public static void Reload()
    {
        EditorUtility.RequestScriptReload();
    }
}

