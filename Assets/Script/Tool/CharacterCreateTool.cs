using UnityEditor;
using UnityEngine;
using System.IO;
using Mirror;
using System.Linq;

public class CharacterCreateTool : EditorWindow
{
    private string characterName = "";
    private bool isPlayer = true;

    [MenuItem("Tools/Character Create Tool")]
    public static void ShowWindow()
    {
        GetWindow<CharacterCreateTool>("Character Create Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Character Create", EditorStyles.boldLabel);

        // Character Name Input
        characterName = EditorGUILayout.TextField("Character Name", characterName);

        // Player or Enemy Toggle
        isPlayer = EditorGUILayout.Toggle("Is Player", isPlayer);

        // Create Button
        if (GUILayout.Button("Create Character"))
        {
            CreateCharacter();
        }
    }

    private void CreateCharacter()
    {
        if (string.IsNullOrEmpty(characterName))
        {
            Debug.LogError("Character Name cannot be empty!");
            return;
        }

        CreateVisual();
        CreateData();
        CreatePrefab();
        Debug.Log($"Character '{characterName}' created successfully!");
    }

    private void CreateVisual()
    {
        string modelFolderPath = Path.Combine("Assets/Art/Model", characterName);

        if (!Directory.Exists(modelFolderPath))
        {
            Directory.CreateDirectory(modelFolderPath);
            Debug.LogError($"Model folder '{modelFolderPath}' does not exist. A new folder has been created.");
            return;
        }

        // Find FBX files
        var fbxFiles = Directory.GetFiles(modelFolderPath, "*.fbx");
        if (fbxFiles.Length == 0)
        {
            Debug.LogError($"No FBX files found in '{modelFolderPath}'");
            return;
        }

        foreach (var fbxPath in fbxFiles)
        {
            var model = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (model != null)
            {
                model.globalScale = 100f;
                model.SaveAndReimport();
                model.animationType = ModelImporterAnimationType.Generic;
            }
        }

        string controllerPath = $"Assets/AnimationController/AC_Character_{characterName}.controller";
        if (!File.Exists(controllerPath))
        {
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var rootStateMachine = controller.layers[0].stateMachine;

            rootStateMachine.AddState("Idle");
            rootStateMachine.AddState("Basic01");
            rootStateMachine.AddState("Knockback");
            rootStateMachine.AddState("Die");
        }
    }

    private void CreateData()
    {
        string characterDataPath = $"Assets/Data/CharacterData/Data_Character_{characterName}.asset";

        CharacterData characterData = default ;
        if (!File.Exists(characterDataPath))
        {
            characterData = ScriptableObject.CreateInstance<CharacterData>();
            characterData.name = characterName;
            AssetDatabase.CreateAsset(characterData, characterDataPath);
        }

        string actionDataFolder = $"Assets/Data/ActionData/{characterName}";
        if (!Directory.Exists(actionDataFolder))
        {
            Directory.CreateDirectory(actionDataFolder);
        }

        string actionDataPath = Path.Combine(actionDataFolder, $"Data_Action_{characterName}_Basic01.asset");
        if (!File.Exists(actionDataPath))
        {
            var actionData = ScriptableObject.CreateInstance<ActionData>();
            AssetDatabase.CreateAsset(actionData, actionDataPath);

            if (characterData != null && characterData.ActionTable.Count < 1)
            {
                characterData.ActionTable.Add(new ActionTableEntry());
                characterData.ActionTable.First().ActionKey = ActionKey.Basic01;
                characterData.ActionTable.First().ActionData = actionData;
            }
        }
        // Enemy인 경우 BotAIData 생성
        if (!isPlayer)
        {
            string botAIDataPath = $"Assets/Data/BotAIData/Data_BotAI_{characterName}.asset";
            BotAIData botAIData = default;

            if (!File.Exists(botAIDataPath))
            {
                botAIData = ScriptableObject.CreateInstance<BotAIData>();
                botAIData.name = characterName;
                AssetDatabase.CreateAsset(botAIData, botAIDataPath);
            }
            else
            {
                botAIData = AssetDatabase.LoadAssetAtPath<BotAIData>(botAIDataPath);
            }

            if (botAIData.botAIStates.Count < 1)
            {
                BotAIState firstState = new BotAIState { state = CharacterState.Init, distanceRange = new Vector2(0, 10), nextState = CharacterState.Idle, duration = 1, actionKey = ActionKey.None, cooldown = 0 };
                BotAIState secondState = new BotAIState { state = CharacterState.Idle, distanceRange = new Vector2(3, 10), nextState = CharacterState.Move, duration = 0.5f, actionKey = ActionKey.None, cooldown = 0 };
                BotAIState thirdState = new BotAIState { state = CharacterState.Idle, distanceRange = new Vector2(0, 3), nextState = CharacterState.Action, duration = 1, actionKey = ActionKey.Basic01, cooldown = 1 };
                BotAIState fourthState = new BotAIState { state = CharacterState.Move, distanceRange = new Vector2(0, 2), nextState = CharacterState.Idle, duration = 1, actionKey = ActionKey.None, cooldown = 0 };
                BotAIState fifthState = new BotAIState { state = CharacterState.Move, distanceRange = new Vector2(2, 5), nextState = CharacterState.Move, duration = 1, actionKey = ActionKey.None, cooldown = 0 };
                BotAIState sixthState = new BotAIState { state = CharacterState.Move, distanceRange = new Vector2(0, 3), nextState = CharacterState.Action, duration = 1, actionKey = ActionKey.Basic01, cooldown = 1 };

                botAIData.botAIStates.Add(firstState);
                botAIData.botAIStates.Add(secondState);
                botAIData.botAIStates.Add(thirdState);
                botAIData.botAIStates.Add(fourthState);
                botAIData.botAIStates.Add(fifthState);
                botAIData.botAIStates.Add(sixthState);
            }
        }
        else
        {
            if (characterData != null && characterData.Resources.Count < 1)
            {
                characterData.Resources.Add(new CharacterResourceData());
                characterData.Resources.First().Key = CharacterResourceKey.Skill_Cooldown;
                characterData.Resources.First().Min = 0;
                characterData.Resources.First().Max = 10;
                characterData.Resources.First().ResetBy = CharacterResourceResetBy.None;
            }
        }
    }

    private void CreatePrefab()
    {
        string prefabFolder = isPlayer ? "Assets/Prefabs/Character/Player" : "Assets/Prefabs/Character/Enemy";
        string prefabPath = Path.Combine(prefabFolder, isPlayer ? $"PF_PlayerCharacter_{characterName}.prefab" : $"PF_EnemyCharacter_{characterName}.prefab");

        if (File.Exists(prefabPath))
        {
            Debug.Log($"Prefab '{prefabPath}' already exists.");
            return;
        }

        // 프리팹 GameObject 생성
        GameObject prefabObject = new GameObject($"PF_{(isPlayer ? "PlayerCharacter" : "EnemyCharacter")}_{characterName}");

        // Load the root object from the Visual creation logic
        string modelFolderPath = Path.Combine("Assets/Art/Model", characterName);
        if (!Directory.Exists(modelFolderPath))
        {
            Debug.LogError($"Model folder '{modelFolderPath}' not found. Make sure Visual creation logic ran correctly.");
            return;
        }

        var fbxFiles = Directory.GetFiles(modelFolderPath, "*.fbx");
        if (fbxFiles.Length == 0)
        {
            Debug.LogError($"No FBX files found in '{modelFolderPath}'");
            return;
        }

        // 첫 번째 FBX를 프리팹의 자식으로 설정
        string rootFbxPath = fbxFiles[0];
        GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rootFbxPath);
        if (rootPrefab == null)
        {
            Debug.LogError($"Failed to load FBX asset at path '{rootFbxPath}'");
            return;
        }

        // FBX를 프리팹의 자식으로 추가 (PrefabInstance 유지)
        GameObject rootInstance = PrefabUtility.InstantiatePrefab(rootPrefab) as GameObject;
        if (rootInstance != null)
        {
            rootInstance.name = "root"; // 자식 이름 설정
            rootInstance.transform.SetParent(prefabObject.transform, false); // 계층 구조에 추가
        }

        // Add components
        var animator = rootInstance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = rootInstance.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"Assets/AnimationController/AC_Character_{characterName}.controller");

        if (isPlayer)
        {
            prefabObject.AddComponent<PlayerController>();
            prefabObject.AddComponent<NetworkIdentity>();
            prefabObject.AddComponent<NetworkRoomPlayer>();
            prefabObject.AddComponent<NetworkAnimator>();
            prefabObject.AddComponent<NetworkTransformUnreliable>();
        }
        else
        {
           var enemyController = prefabObject.AddComponent<EnemyController>();
            prefabObject.AddComponent<NetworkIdentity>();
            prefabObject.AddComponent<NetworkAnimator>();
            prefabObject.AddComponent<NetworkTransformUnreliable>();

            string botAIDataPath = $"Assets/Data/BotAIData/Data_BotAI_{characterName}.asset";
            var botAIData = AssetDatabase.LoadAssetAtPath<BotAIData>(botAIDataPath);
            if (botAIData != null)
            {
                enemyController.botAIData = botAIData;
            }
            else
            {
                Debug.LogError($"BotAIData not found at '{botAIDataPath}'. Make sure the Data creation step ran correctly.");
            }
        }

        prefabObject.GetComponent<NetworkAnimator>().animator = prefabObject.GetComponentInChildren<Animator>();

        var characterData = AssetDatabase.LoadAssetAtPath<CharacterData>($"Assets/Data/CharacterData/Data_Character_{characterName}.asset");
        if (characterData != null)
        {
            var behaviour = prefabObject.GetComponent<CharacterBehaviour>();

            behaviour.characterData = characterData;
            behaviour.teamType = isPlayer ? TeamType.Player : TeamType.Enemy;
        }

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(prefabObject, prefabPath);
        DestroyImmediate(prefabObject);
    }
}
