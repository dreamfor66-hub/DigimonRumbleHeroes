using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;


[InitializeOnLoad]
public static class ReloadDomainToolbar
{
    static ReloadDomainToolbar()
    {
        ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);
    }

    static void OnToolbarGUI()
    {
        if (GUILayout.Button("Reload Domain"))
            EditorUtility.RequestScriptReload();
    }
}

