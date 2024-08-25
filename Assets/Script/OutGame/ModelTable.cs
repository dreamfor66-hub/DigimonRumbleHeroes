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
        public CharacterData characterData; // 캐릭터 데이터
        public GameObject characterPrefab; // 3D 모델 프리팹
        public RuntimeAnimatorController animator; // 애니메이션 컨트롤러
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
        string modelBasePath = "Assets/Art/Model/"; // 3D 모델 경로
        string animatorBasePath = "Assets/AnimationController"; // 애니메이션 컨트롤러 경로

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

                // 3D 모델 프리팹 경로 설정
                string modelPath = Path.Combine(modelBasePath, characterName+"/", "root.fbx");
                GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

                // 애니메이션 컨트롤러 경로 설정
                string animatorPath = Path.Combine(animatorBasePath, $"AC_Character_{characterName}.controller");
                RuntimeAnimatorController animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorPath);

                // 리스트에 새로운 매핑 추가
                ModelMapping newMapping = new ModelMapping
                {
                    characterData = characterData,
                    characterPrefab = characterPrefab,
                    animator = animatorController
                };
                modelMappings.Add(newMapping);
            }
        }

        // 변경 사항을 저장
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
}