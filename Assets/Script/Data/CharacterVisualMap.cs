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
        public string characterName => characterData.characterName; // 캐릭터 데이터

        [VerticalGroup("Data Map")]
        [LabelText("Data")]
        public CharacterData characterData; // 캐릭터 데이터

        [VerticalGroup("Data Map")]
        [LabelText("Sprite")]
        public Sprite characterSpriteFull; // 캐릭터의 전신 일러스트

        [VerticalGroup("Main")]
        [HideLabel]
        public PositionAndScale spriteMainOffset;
    }

    [TableList]
    public List<ModelMapping> modelMappings; // 매핑 리스트

    // CharacterData를 통해 3D 모델 정보 찾기
    public ModelMapping GetModelMapping(CharacterData characterData)
    {
        return modelMappings.Find(mapping => mapping.characterData == characterData);
    }

    [Button("Populate Model Mappings")]
    private void PopulateModelMappings()
    {
        string characterDataPath = "Assets/Data/CharacterData"; // CharacterData 파일들이 위치한 경로
        string spriteFullBasePath = "Assets/Art/Sprite/Full"; // 스프라이트 경로

        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { characterDataPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData characterData = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);

            // 이미 리스트에 있는지 확인
            bool exists = modelMappings.Exists(mapping => mapping.characterData == characterData);

            if (!exists)
            {
                // 캐릭터 이름 추출
                string characterName = characterData.name.Replace("Data_Character_", "");

                // 스프라이트 경로 설정
                string spriteFullPath = Path.Combine(spriteFullBasePath, $"T_CharacterItem_{characterName}_Full.png");
                Sprite spriteFull = AssetDatabase.LoadAssetAtPath<Sprite>(spriteFullPath);

                // 리스트에 새로운 매핑 추가
                ModelMapping newMapping = new ModelMapping
                {
                    characterData = characterData,
                    characterSpriteFull = spriteFull,
                    spriteMainOffset = new PositionAndScale { pos = new Vector2(0,500), scale = 1 }, // 기본값 설정
                };

                modelMappings.Add(newMapping);
            }
        }

        // 변경 사항을 저장
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
