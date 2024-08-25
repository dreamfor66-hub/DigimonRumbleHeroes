using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MapEditorWindow : OdinEditorWindow
{
    private bool wasEditingBeforePlay = false;
    private int rotate = 0;

    [MenuItem("Window/Map Editor")]
    private static void OpenWindow()
    {
        GetWindow<MapEditorWindow>("Map Editor").Show();
    }

    [Title("Map Settings")]
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [Required]
    [PropertyOrder(0)]
    public Map currentMap;

    [PropertyOrder(4)]
    [Button("Save Changes")]
    private void SaveChanges()
    {
        if (currentMap != null)
        {
            EditorUtility.SetDirty(currentMap);
            AssetDatabase.SaveAssets();
            Debug.Log("Map settings saved.");
        }
        else
        {
            Debug.LogWarning("No map selected.");
        }
    }

    [PropertyOrder(0)]
    [Button("Create / Set Map")]
    void MapSetting()
    {
        if (GameObject.FindFirstObjectByType<Map>() == null)
        {
            var newMap = new GameObject("Map").AddComponent<Map>();
            currentMap = newMap;
        }
        else
        {
            currentMap = GameObject.FindFirstObjectByType<Map>();
        }
    }

    public enum ObjectType { Block, Character }

    [Title("Object Type Settings")]
    [PropertyOrder(0)]
    [EnumToggleButtons]
    public ObjectType objectType = ObjectType.Block;

    [ShowIf("objectType", ObjectType.Block)]
    [Title("Block Prefab")]
    [InfoBox("Insert the prefab you want to use for creating blocks in the map.")]
    [InlineEditor(InlineEditorModes.LargePreview)]
    public GameObject blockPrefab;

    [ShowIf("objectType", ObjectType.Character)]
    [HorizontalGroup("Character", Width = 0.4f, MarginRight = 0.05f)]
    [VerticalGroup("Character/a")]
    [AssetsOnly]
    [Title("Character Prefab"), PreviewField(220, Sirenix.OdinInspector.ObjectFieldAlignment.Center), HideLabel]
    public GameObject selectedCharacter;

    private string searchString = "";

    [ShowIf("objectType", ObjectType.Character)]
    [Title("Character Selection")]
    [VerticalGroup("Character/b")]
    //[HorizontalGroup("Character", Width = 0.5f, MarginRight = 0.05f)]
    [OnInspectorGUI]
    public void CharacterSelectionGUI()
    {
        ShowPrefabSelectButtons("Assets/Prefabs/Character/");
    }

    private void ShowPrefabSelectButtons(string path)
    {
        //searchString = EditorGUILayout.TextField(searchString);
        var assets = AssetDatabase.FindAssets("t:Prefab", new[] { path });
        foreach (var guid in assets)
        {
            var filePath = AssetDatabase.GUIDToAssetPath(guid);
            var name = Path.GetFileNameWithoutExtension(filePath);
            var style = SirenixGUIStyles.Button;
            style.alignment = TextAnchor.MiddleLeft;
            if (!string.IsNullOrEmpty(searchString))
            {
                if (!name.ToLower().Contains(searchString.ToLower()))
                    continue;
            }
            if (GUILayout.Button(name, style))
            {
                selectedCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);
            }
        }
    }


    [Title("Boundary Settings")]
    [PropertyOrder(1)]
    public Material floorMaterial;

    [PropertyOrder(2)]
    public GameObject wallPrefab;

    [PropertyOrder(3)]
    [OnValueChanged("UpdateMapSize")]
    public Vector2 mapSize = new Vector2(2, 3);

    [PropertyOrder(4)]
    [ShowInInspector, OnValueChanged("UpdateMapStartPoint")]
    [PropertyTooltip("The start point for the player on the map.")]
    public Vector2 PlayerStartPoint
    {
        get => currentMap != null ? currentMap.PlayerStartPoint : Vector2.zero;
        set
        {
            if (currentMap != null)
            {
                currentMap.PlayerStartPoint = value;
                MarkSceneDirty();
            }
        }
    }

    [Button("Create Boundary")]
    [PropertyOrder(5)]
    private void CreateBoundary()
    {
        if (currentMap != null)
        {
            currentMap.MapSize = mapSize;
        }

        GameObject floorParent = GameObject.Find("Map/Floor");
        if (floorParent != null)
        {
            Undo.DestroyObjectImmediate(floorParent);
        }
        GameObject boundaryParent = GameObject.Find("Map/Boundary");
        if (boundaryParent != null)
        {
            Undo.DestroyObjectImmediate(boundaryParent);
        }

        floorParent = new GameObject("Floor");
        boundaryParent = new GameObject("Boundary");

        floorParent.transform.SetParent(GameObject.Find("Map").transform);
        boundaryParent.transform.SetParent(GameObject.Find("Map").transform);

        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                Vector3 position = new Vector3(x, 0, y);
                GameObject floorBlock = CreateFloorBlock(position);
                floorBlock.transform.SetParent(floorParent.transform);
                Undo.RegisterCreatedObjectUndo(floorBlock, "Create Floor Block");
            }
        }

        for (int x = -1; x <= mapSize.x; x++)
        {
            for (int y = -1; y <= mapSize.y; y++)
            {
                if (x == -1 || x == mapSize.x || y == -1 || y == mapSize.y)
                {
                    Vector3 position = new Vector3(x, 0, y);
                    GameObject wallBlock = PrefabUtility.InstantiatePrefab(wallPrefab) as GameObject;
                    wallBlock.transform.position = position;
                    wallBlock.transform.SetParent(boundaryParent.transform);
                    wallBlock.GetComponentInChildren<Collider>().isTrigger = true;
                    Undo.RegisterCreatedObjectUndo(wallBlock, "Create Boundary Wall");
                }
            }
        }
    }

    private GameObject CreateFloorBlock(Vector3 position)
    {
        GameObject floorBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floorBlock.transform.position = position;
        floorBlock.transform.localScale = new Vector3(1f, 0.1f, 1f);
        if (floorMaterial != null)
        {
            floorBlock.GetComponent<Renderer>().material = floorMaterial;
        }
        return floorBlock;
    }

    [Title("Edit Mode")]
    public bool isEditing = false;
    public bool isErasing = false;

    private Vector3? previewPosition = null;

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;

        if (GameObject.FindFirstObjectByType<Map>() == null)
        {
            MapSetting();  // Initialize currentMap if it doesn't exist.
        }
        else
        {
            currentMap = GameObject.FindFirstObjectByType<Map>();
        }

        if (wasEditingBeforePlay)
        {
            ToggleEditing();
        }

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            wasEditingBeforePlay = isEditing;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (currentMap == null)
            {
                MapSetting();
            }
            if (wasEditingBeforePlay)
            {
                ToggleEditing();
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (currentMap == null)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            return;
        }

        DrawHandle();

        if (isEditing && (e.button == 0 || e.button == 1 || e.button == 2))
        {
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 targetPosition = hit.point;
                    targetPosition.y = 0;
                    targetPosition.x = Mathf.Round(targetPosition.x);
                    targetPosition.z = Mathf.Round(targetPosition.z);

                    previewPosition = targetPosition;

                    if (e.button == 0 && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
                    {
                        if (!e.control)
                        {
                            CreateObject(targetPosition);
                        }
                        else
                        {
                            if (objectType == ObjectType.Block)
                            {
                                EraseBlock(targetPosition);
                            }
                            else if (objectType == ObjectType.Character)
                            {
                                EraseCharacter(targetPosition);
                            }
                        }
                        e.Use();
                    }
                }
                else
                {
                    previewPosition = null;
                }
            }
            sceneView.Repaint();
        }

        if (previewPosition.HasValue)
        {
            DrawPreviewGizmo(previewPosition.Value);
        }

        // Handle rotation input
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
        {
            rotate = (rotate + 1) % 8; // Rotate by 45 degrees
        }
    }

    [Button(ButtonSizes.Large)]
    [GUIColor(0.5f, 1f, 0.5f)]
    private void ToggleEditing()
    {
        isEditing = !isEditing;

        if (isEditing)
        {
            Selection.activeObject = null;
            Tools.current = Tool.None;
            SceneView.RepaintAll();
        }
        else
        {
            Tools.current = Tool.Move;
        }
    }

    private void DrawHandle()
    {
        if (currentMap == null) return;

        Vector3 startPosition = new Vector3(PlayerStartPoint.x, 0, PlayerStartPoint.y);

        EditorGUI.BeginChangeCheck();
        var fmh_253_74_638595741349808741 = Quaternion.identity;
        Vector3 newStartPosition = Handles.FreeMoveHandle(startPosition, 0.75f, Vector3.one * 0.5f, Handles.SphereHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            newStartPosition.x = Mathf.Round(newStartPosition.x);
            newStartPosition.z = Mathf.Round(newStartPosition.z);

            PlayerStartPoint = new Vector2(newStartPosition.x, newStartPosition.z);
            UpdateMapStartPoint();
        }

        Handles.color = Color.yellow;
        Handles.SphereHandleCap(0, startPosition, Quaternion.identity, 0.75f, EventType.Repaint);
        Handles.Label(startPosition + Vector3.up * 1.5f, "Player Start Point");
    }

    private void DrawPreviewGizmo(Vector3 position)
    {
        Handles.color = Color.yellow;
        Handles.DrawWireCube(position + Vector3.up * 0.5f, Vector3.one);

        if (objectType == ObjectType.Character)
        {
            var rotation = Quaternion.Euler(0, rotate * 45f, 0);
            var direction = rotation * Vector3.forward;
            Handles.DrawLine(position, position + direction);
        }
    }

    private void UpdateMapStartPoint()
    {
        if (currentMap != null)
        {
            currentMap.PlayerStartPoint = PlayerStartPoint;
            MarkSceneDirty();
        }
    }

    private void UpdateMapSize()
    {
        if (currentMap != null)
        {
            currentMap.MapSize = mapSize;
            MarkSceneDirty();
        }
    }

    private void MarkSceneDirty()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }

    private void CreateObject(Vector3 position)
    {
        if (objectType == ObjectType.Block)
        {
            CreateBlock(position);
        }
        else if (objectType == ObjectType.Character)
        {
            CreateCharacter(position);
        }
    }

    private void CreateBlock(Vector3 position)
    {
        if (blockPrefab == null)
        {
            Debug.LogWarning("Block prefab is not assigned.");
            return;
        }

        GameObject mapObject = currentMap.gameObject;
        if (mapObject == null)
        {
            Debug.LogWarning("Map object not found.");
            return;
        }

        Transform blocksTransform = mapObject.transform.Find("Blocks");
        if (blocksTransform == null)
        {
            GameObject blocksObject = new GameObject("Blocks");
            blocksTransform = blocksObject.transform;
            blocksTransform.SetParent(mapObject.transform);
        }

        foreach (Transform child in blocksTransform)
        {
            if (child.position == position)
            {
                return;
            }
        }

        GameObject newBlock = PrefabUtility.InstantiatePrefab(blockPrefab) as GameObject;
        newBlock.transform.position = position;
        newBlock.transform.SetParent(blocksTransform);
        newBlock.GetComponentInChildren<Collider>().isTrigger = true;

        Undo.RegisterCreatedObjectUndo(newBlock, "Create Block");
    }

    private void EraseBlock(Vector3 position)
    {
        GameObject mapObject = currentMap.gameObject;
        if (mapObject == null)
        {
            Debug.LogWarning("Map object not found.");
            return;
        }

        Transform blocksTransform = mapObject.transform.Find("Blocks");
        if (blocksTransform == null)
        {
            Debug.LogWarning("Blocks object not found.");
            return;
        }

        foreach (Transform child in blocksTransform)
        {
            if (child.position == position)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
                return;
            }
        }
    }

    private void CreateCharacter(Vector3 position)
    {
        if (selectedCharacter == null)
        {
            Debug.LogWarning("No Character prefab selected.");
            return;
        }

        GameObject mapObject = currentMap.gameObject;
        if (mapObject == null)
        {
            Debug.LogWarning("Map object not found.");
            return;
        }

        Transform enemiesTransform = mapObject.transform.Find("Enemies");
        if (enemiesTransform == null)
        {
            GameObject enemiesObject = new GameObject("Enemies");
            enemiesTransform = enemiesObject.transform;
            enemiesTransform.SetParent(mapObject.transform);
        }

        foreach (Transform child in enemiesTransform)
        {
            if (child.position == position)
            {
                return;
            }
        }

        GameObject newCharacter = PrefabUtility.InstantiatePrefab(selectedCharacter) as GameObject;
        newCharacter.transform.position = position;
        newCharacter.transform.rotation = Quaternion.Euler(0, rotate * 45f, 0); // Apply rotation
        newCharacter.transform.SetParent(enemiesTransform);

        Undo.RegisterCreatedObjectUndo(newCharacter, "Create Character");
    }

    private void EraseCharacter(Vector3 position)
    {
        GameObject mapObject = currentMap.gameObject;
        if (mapObject == null)
        {
            Debug.LogWarning("Map object not found.");
            return;
        }

        Transform enemiesTransform = mapObject.transform.Find("Enemies");
        if (enemiesTransform == null)
        {
            Debug.LogWarning("Enemies object not found.");
            return;
        }

        // 큐브 기즈모의 크기 및 중심 좌표
        Vector3 cubeCenter = position + Vector3.up * 0.5f;
        Vector3 cubeSize = Vector3.one;

        List<Transform> charactersToRemove = new List<Transform>();

        foreach (Transform child in enemiesTransform)
        {
            // 캐릭터가 큐브 기즈모 내부에 있는지 확인
            if (IsInsideCube(cubeCenter, cubeSize, child.position))
            {
                charactersToRemove.Add(child);
            }
        }

        foreach (Transform character in charactersToRemove)
        {
            Undo.DestroyObjectImmediate(character.gameObject);
        }

        if (charactersToRemove.Count > 0)
        {
            EditorUtility.SetDirty(mapObject);
        }
    }

    private bool IsInsideCube(Vector3 cubeCenter, Vector3 cubeSize, Vector3 point)
    {
        return (point.x >= cubeCenter.x - cubeSize.x / 2 && point.x <= cubeCenter.x + cubeSize.x / 2) &&
               (point.y >= cubeCenter.y - cubeSize.y / 2 && point.y <= cubeCenter.y + cubeSize.y / 2) &&
               (point.z >= cubeCenter.z - cubeSize.z / 2 && point.z <= cubeCenter.z + cubeSize.z / 2);
    }
}
