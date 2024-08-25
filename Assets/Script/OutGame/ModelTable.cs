// ModelTable.cs
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ModelTable", menuName = "Data/ModelTable", order = 1)]
public class ModelTable : ScriptableObject
{
    [System.Serializable]
    public class ModelMapping
    {
        public CharacterData characterData; // ĳ���� ������
        public GameObject characterPrefab; // 3D �� ������
        public RuntimeAnimatorController animator; // �ִϸ��̼� ��Ʈ�ѷ�
    }
    [TableList]
    public List<ModelMapping> modelMappings; // ���� ����Ʈ

    // CharacterData�� ���� 3D �� ���� ã��
    public ModelMapping GetModelMapping(CharacterData characterData)
    {
        return modelMappings.Find(mapping => mapping.characterData == characterData); 
    }

    [Button("Populate Model Mappings")]
    private void PopulateModelMappings()
    {
        string characterDataPath = "Assets/Data/CharacterData"; // CharacterData ���ϵ��� ��ġ�� ���
        string modelBasePath = "Assets/Art/Model/"; // 3D �� ���
        string animatorBasePath = "Assets/AnimationController"; // �ִϸ��̼� ��Ʈ�ѷ� ���

        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { characterDataPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData characterData = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);

            // �̹� ����Ʈ�� �ִ��� Ȯ��
            bool exists = modelMappings.Exists(mapping => mapping.characterData == characterData);

            if (!exists)
            {
                // ĳ���� �̸� ����
                string characterName = characterData.name.Replace("Data_Character_", "");

                // 3D �� ������ ��� ����
                string modelPath = Path.Combine(modelBasePath, characterName+"/", "root.fbx");
                GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

                // �ִϸ��̼� ��Ʈ�ѷ� ��� ����
                string animatorPath = Path.Combine(animatorBasePath, $"AC_Character_{characterName}.controller");
                RuntimeAnimatorController animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorPath);

                // ����Ʈ�� ���ο� ���� �߰�
                ModelMapping newMapping = new ModelMapping
                {
                    characterData = characterData,
                    characterPrefab = characterPrefab,
                    animator = animatorController
                };
                modelMappings.Add(newMapping);
            }
        }

        // ���� ������ ����
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
}