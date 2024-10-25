using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "EvolutionData", menuName = "Data/EvolutionData", order = 1)]
public class EvolutionData : ScriptableObject
{
    [Title("진화 정보")]
    [DisplayAsString]
    [HideLabel]
    public string arrowz = "";

    [VerticalGroup("Evolution Path/A")]
    [OnValueChanged("FindSprite")]
    [HideLabel]
    public CharacterData prevCharacterData;

    [ShowInInspector]
    [HorizontalGroup("Evolution Path", Width = 0.3f)]
    [VerticalGroup("Evolution Path/A")]
    [PreviewField(150, ObjectFieldAlignment.Center)]
    [HideLabel]
    [ReadOnly]
    public Sprite prevCharacterDataImage => prevCharacterData.characterSprite;

    [Space(70)]
    [HorizontalGroup("Evolution Path", Width = 0.1f)]
    [VerticalGroup("Evolution Path/C")]
    [DisplayAsString(TextAlignment.Center, EnableRichText = true, FontSize = 20)]
    [HideLabel]
    public string arrow = "에서";

    [VerticalGroup("Evolution Path/B")]
    [HideLabel]
    [OnValueChanged("FindSprite")]
    public CharacterData nextCharacterData;

    [ShowInInspector]
    [HorizontalGroup("Evolution Path", Width = 0.3f)]
    [VerticalGroup("Evolution Path/B")]
    [PreviewField(150, ObjectFieldAlignment.Center)]
    [HideLabel]
    [ReadOnly]
    public Sprite nextCharacterDataImage => nextCharacterData.characterSprite;

    [Space(70)]
    [HorizontalGroup("Evolution Path", Width = 0.3f)]
    [VerticalGroup("Evolution Path/D")]
    [DisplayAsString(TextAlignment.Left, EnableRichText = true, FontSize = 20)]
    [HideLabel]
    public string arrowc = "으로 진화";

    [Title("비주얼")]
    [HideLabel]
    public ImageOffsetSimulator ImageSimulator;

    [Button("FindSprite")]
    private void FindSprite()
    {
        string spriteFullBasePath = "Assets/Art/Sprite/Full"; // 스프라이트 경로

        if (prevCharacterData != null)
        {
            string characterName = prevCharacterData.name.Replace("Data_Character_", "");
            string spriteFullPath = Path.Combine(spriteFullBasePath, $"T_CharacterItem_{characterName}_Full.png");
            Sprite spriteFull = AssetDatabase.LoadAssetAtPath<Sprite>(spriteFullPath);

            ImageSimulator.ImageSelectedCharacterSprite = spriteFull;
        }

        if (nextCharacterData != null)
        {
            string characterName = nextCharacterData.name.Replace("Data_Character_", "");
            string spriteFullPath = Path.Combine(spriteFullBasePath, $"T_CharacterItem_{characterName}_Full.png");
            Sprite spriteFull = AssetDatabase.LoadAssetAtPath<Sprite>(spriteFullPath);

            ImageSimulator.ImageEvolutionCharacterSprite = spriteFull;
        }

        // 변경 사항을 저장
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    [Title("소모 재화")]
    [TableList]
    public List<ResourceItem> requiredResources;

    private void OnValidate()
    {
        if (ImageSimulator != null)
        {
            ImageSimulator.SetParent(this);
            ImageSimulator.UpdatePreview();
        }
    }
}

[System.Serializable]
public class ResourceItem
{
    public int itemId;
    public int itemCount;
}

[System.Serializable]
public class ImageOffsetSimulator
{
    private EvolutionData parent;

    [Title("진화 전 캐릭터 설정")]
    [HorizontalGroup("Image Settings")]
    [VerticalGroup("Image Settings/A")]
    [LabelText("Sprite")]
    public Sprite ImageSelectedCharacterSprite;

    [OnValueChanged("UpdatePreview")]
    [VerticalGroup("Image Settings/A")]
    [LabelText("Offset")]
    public Vector2 ImageSelectedCharacterOffset;

    [OnValueChanged("UpdatePreview")]
    [VerticalGroup("Image Settings/A")]
    [LabelText("Scale")]
    public float ImageSelectedCharacterScale = 1f;

    [Title("진화 후 캐릭터 설정")]
    [HorizontalGroup("Image Settings")]
    [VerticalGroup("Image Settings/B")]
    [LabelText("Sprite")]
    public Sprite ImageEvolutionCharacterSprite;

    [OnValueChanged("UpdatePreview")]
    [VerticalGroup("Image Settings/B")]
    [LabelText("Offset")]
    public Vector2 ImageEvolutionCharacterOffset;

    [OnValueChanged("UpdatePreview")]
    [VerticalGroup("Image Settings/B")]
    [LabelText("Scale")]
    public float ImageEvolutionCharacterScale = 1f;

    [Title("중앙점 조정")]
    [OnValueChanged("UpdatePreview")]
    public Vector2 centralOffset = Vector2.zero;

    [OnValueChanged("UpdatePreview")]
    public float centralScale = 1f;

    private const float canvasSize = 400f;

    public void SetParent(EvolutionData parent)
    {
        this.parent = parent;
    }

    [Button("Preview Images")]
    private void PreviewImages()
    {
        UpdatePreview();
    }

    [OnInspectorGUI]
    private void DrawPreviews()
    {
        GUILayout.Label("Combined Image Preview", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(canvasSize, canvasSize);

        EditorGUI.DrawRect(rect, Color.gray);

        // 이미지 크기 비율 계산 (1600x1600 캔버스를 기준으로 스케일 조정)
        float imageARatio = ImageSelectedCharacterSprite != null ? 1600 / ImageSelectedCharacterSprite.rect.width : 1f;
        float imageBRatio = ImageEvolutionCharacterSprite != null ? 1600 / ImageEvolutionCharacterSprite.rect.width : 1f;

        // 스케일 적용 후 크기 계산
        float adjustedScaleA = ImageSelectedCharacterScale * imageARatio;
        float adjustedScaleB = ImageEvolutionCharacterScale * imageBRatio;

        // 오프셋 계산 (스케일 적용 전)
        Vector2 adjustedOffsetA = ImageSelectedCharacterOffset + centralOffset;
        Vector2 adjustedOffsetB = ImageEvolutionCharacterOffset + centralOffset;

        // Image B를 먼저 그리고, Image A를 나중에 그립니다.
        if (ImageEvolutionCharacterSprite != null)
        {
            DrawSprite(ImageEvolutionCharacterSprite, rect, adjustedOffsetB * centralScale, adjustedScaleB * centralScale);
        }

        if (ImageSelectedCharacterSprite != null)
        {
            DrawSprite(ImageSelectedCharacterSprite, rect, adjustedOffsetA * centralScale, adjustedScaleA * centralScale);
        }
    }

    private void DrawSprite(Sprite sprite, Rect rect, Vector2 offset, float scale)
    {
        Texture2D texture = sprite.texture;
        Rect textureRect = sprite.textureRect;

        float width = textureRect.width * scale;
        float height = textureRect.height * scale;

        Rect position = new Rect(
            rect.x + offset.x + (rect.width - width) / 2,
            rect.y - offset.y + (rect.height - height) / 2,
            width,
            height
        );

        GUI.DrawTextureWithTexCoords(position, texture, new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height
        ));
    }

    public void UpdatePreview()
    {
        if (parent != null)
        {
            EditorUtility.SetDirty(parent);
        }
    }
}