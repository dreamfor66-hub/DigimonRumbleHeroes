using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "EvolutionData", menuName = "Data/EvolutionData", order = 1)]
public class EvolutionData : ScriptableObject
{
    [Title("��ȭ ����")]
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
    public string arrow = "����";

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
    public string arrowc = "���� ��ȭ";

    [Title("���־�")]
    [HideLabel]
    public ImageOffsetSimulator ImageSimulator;

    [Button("FindSprite")]
    private void FindSprite()
    {
        string spriteFullBasePath = "Assets/Art/Sprite/Full"; // ��������Ʈ ���

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

        // ���� ������ ����
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    [Title("�Ҹ� ��ȭ")]
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

    [Title("��ȭ �� ĳ���� ����")]
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

    [Title("��ȭ �� ĳ���� ����")]
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

    [Title("�߾��� ����")]
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

        // �̹��� ũ�� ���� ��� (1600x1600 ĵ������ �������� ������ ����)
        float imageARatio = ImageSelectedCharacterSprite != null ? 1600 / ImageSelectedCharacterSprite.rect.width : 1f;
        float imageBRatio = ImageEvolutionCharacterSprite != null ? 1600 / ImageEvolutionCharacterSprite.rect.width : 1f;

        // ������ ���� �� ũ�� ���
        float adjustedScaleA = ImageSelectedCharacterScale * imageARatio;
        float adjustedScaleB = ImageEvolutionCharacterScale * imageBRatio;

        // ������ ��� (������ ���� ��)
        Vector2 adjustedOffsetA = ImageSelectedCharacterOffset + centralOffset;
        Vector2 adjustedOffsetB = ImageEvolutionCharacterOffset + centralOffset;

        // Image B�� ���� �׸���, Image A�� ���߿� �׸��ϴ�.
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