// CharacterVisualMap.cs
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterVisualMap", menuName = "Data/CharacterVisualMap", order = 1)]
public class CharacterVisualMap : ScriptableObject
{
    [System.Serializable]
    public class ModelMapping
    {
        [VerticalGroup("Data Map")]
        [LabelText("name")]
        [ReadOnly]
        [ShowInInspector]
        public string characterName => characterData.characterName; // ĳ���� ������

        [VerticalGroup("Data Map")]
        [LabelText("Data")]
        public CharacterData characterData; // ĳ���� ������

        [VerticalGroup("Data Map")]
        [LabelText("Sprite")]
        public Sprite characterSpriteFull; // ĳ������ ���� �Ϸ���Ʈ

        [VerticalGroup("Main")]
        [HideLabel]
        public PositionAndScale spriteMainOffset;
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
        string spriteFullBasePath = "Assets/Art/Sprite/Full"; // ��������Ʈ ���

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

                // ��������Ʈ ��� ����
                string spriteFullPath = Path.Combine(spriteFullBasePath, $"T_CharacterItem_{characterName}_Full.png");
                Sprite spriteFull = AssetDatabase.LoadAssetAtPath<Sprite>(spriteFullPath);

                // ����Ʈ�� ���ο� ���� �߰�
                ModelMapping newMapping = new ModelMapping
                {
                    characterData = characterData,
                    characterSpriteFull = spriteFull,
                    spriteMainOffset = new PositionAndScale { pos = new Vector2(0,500), scale = 1 }, // �⺻�� ����
                };

                modelMappings.Add(newMapping);
            }
        }

        // ���� ������ ����
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    [System.Serializable]
    public struct PositionAndScale
    {
        [VerticalGroup("Main")]
        public Vector2 pos;

        [VerticalGroup("Main")]
        public float scale;
    }
}
