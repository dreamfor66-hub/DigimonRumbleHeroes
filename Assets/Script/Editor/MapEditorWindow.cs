using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class MapEditorWindow : OdinEditorWindow
{
    [Title("Block Prefab Settings")]
    [InfoBox("Insert the prefab you want to use for creating blocks in the map.")]
    [InlineEditor(InlineEditorModes.LargePreview)]
    public GameObject blockPrefab;

    [Title("Boundary Settings")]
    [PropertyOrder(1)]
    public Material floorMaterial;
    [PropertyOrder(2)]
    public GameObject wallPrefab;
    [PropertyOrder(3)]
    public Vector2 mapSize = new Vector2(2, 3);

    [Button("Create Boundary")]
    [PropertyOrder(4)]
    private void CreateBoundary()
    {
        // Boundary 생성 로직
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
    [PropertyOrder(0)]
    public bool isEditing = false;
    public bool isErasing = false;

    private Vector3? previewPosition = null;
    private Vector3 lastMousePosition;
    private float gridSnapSize = 1f;

    [Button(ButtonSizes.Large)]
    [GUIColor(0.5f, 1f, 0.5f)]
    private void ToggleEditing()
    {
        isEditing = !isEditing;
        if (isEditing)
        {
            Selection.activeObject = null;
            SceneView.duringSceneGui += OnSceneGUI;
            Tools.current = Tool.None; // Transform 도구 비활성화
        }
        else
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Tools.current = Tool.Move; // Edit 모드 비활성화 시 MoveTool로 복구
        }
    }

    [Button(ButtonSizes.Large)]
    [GUIColor(1f, 0.5f, 0.5f)]
    [ShowIf("isEditing")]
    private void ToggleErasing()
    {
        isErasing = !isErasing;
    }

    private void OnEnable()
    {
        if (isEditing)
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Tools.current = Tool.None; // Transform 도구 비활성화
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Tools.current = Tool.Move; // Edit 모드 비활성화 시 MoveTool로 복구
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (isEditing)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                Vector3 currentMousePosition = e.mousePosition;

                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 targetPosition = hit.point;
                    targetPosition.y = 0;

                    targetPosition.x = Mathf.Round(targetPosition.x);
                    targetPosition.z = Mathf.Round(targetPosition.z);

                    previewPosition = targetPosition;

                    if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                    {
                        if (e.button == 0 && isEditing)
                        {
                            if (isErasing || e.control)
                            {
                                EraseBlock(targetPosition);
                            }
                            else
                            {
                                CreateBlock(targetPosition);
                            }
                            e.Use();
                        }
                    }
                }
                else
                {
                    previewPosition = null;
                }
                sceneView.Repaint();
            }

            if (previewPosition.HasValue)
            {
                DrawPreviewGizmo(previewPosition.Value);
            }
        }
    }

    private void DrawPreviewGizmo(Vector3 position)
    {
        Handles.color = Color.yellow;
        Handles.DrawWireCube(position + Vector3.up * 0.5f, Vector3.one);
    }

    private void CreateBlock(Vector3 position)
    {
        if (blockPrefab == null)
        {
            Debug.LogWarning("Block prefab is not assigned.");
            return;
        }

        GameObject mapObject = GameObject.Find("Map");
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
        GameObject mapObject = GameObject.Find("Map");
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
}
