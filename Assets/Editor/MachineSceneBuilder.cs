using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MachineSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MicroPrototype.unity";
    private const string GeneratedRootName = "MachineMicroPrototype_Generated";

    public static void BuildOrUpdateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureFolder("Assets/Scenes");
        EnsureBallTag();

        Scene scene;
        if (File.Exists(ToAbsolutePath(ScenePath)))
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        GameObject root = GameObject.Find(GeneratedRootName);
        if (root == null)
        {
            root = new GameObject(GeneratedRootName);
        }

        ConfigureCamera(root.transform);

        GameObject managerObject = GetOrCreateChild(root.transform, "GameManager");
        GameManager gameManager = EnsureComponent<GameManager>(managerObject);

        ConfigureFloor(root.transform);
        ConfigureRamp(root.transform);
        ConfigureBox(root.transform);
        ConfigureBall(root.transform);
        ConfigureTarget(root.transform);

        GameObject winPanel = ConfigureUI(root.transform, gameManager);
        AssignWinPanel(gameManager, winPanel);
        ConfigureEventSystem(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("[Machine Tools] Scene built/updated: " + ScenePath);
        EditorUtility.DisplayDialog("Machine Tools", "Сцена создана/обновлена:\n" + ScenePath, "OK");
    }

    private static string ToAbsolutePath(string assetPath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string name = Path.GetFileName(folder);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    private static void EnsureBallTag()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(assets[0]);
        SerializedProperty tags = serializedObject.FindProperty("tags");
        if (tags == null)
        {
            return;
        }

        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == "Ball")
            {
                return;
            }
        }

        int index = tags.arraySize;
        tags.InsertArrayElementAtIndex(index);
        tags.GetArrayElementAtIndex(index).stringValue = "Ball";
        serializedObject.ApplyModifiedProperties();
    }

    private static void ConfigureCamera(Transform root)
    {
        GameObject cameraObject = GetOrCreateChild(root, "Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.transform.rotation = Quaternion.identity;

        Camera camera = EnsureComponent<Camera>(cameraObject);
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void ConfigureFloor(Transform root)
    {
        GameObject floor = GetOrCreateChild(root, "Floor");
        floor.transform.position = new Vector3(0f, -4.25f, 0f);
        floor.transform.rotation = Quaternion.identity;

        ConfigureRenderer(floor, new Vector2(16f, 0.75f), new Color(0.22f, 0.24f, 0.28f, 1f), 0);

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(floor);
        collider.isTrigger = false;
        collider.size = new Vector2(16f, 0.75f);
    }

    private static void ConfigureRamp(Transform root)
    {
        GameObject ramp = GetOrCreateChild(root, "Ramp");
        ramp.transform.position = new Vector3(-2.1f, -0.9f, 0f);
        ramp.transform.rotation = Quaternion.Euler(0f, 0f, -14f);

        ConfigureRenderer(ramp, new Vector2(5.5f, 0.35f), new Color(0.65f, 0.42f, 0.20f, 1f), 2);

        Rigidbody2D rb = EnsureComponent<Rigidbody2D>(ramp);
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(ramp);
        collider.isTrigger = false;
        collider.size = new Vector2(5.5f, 0.35f);

        Part part = EnsureComponent<Part>(ramp);
        part.draggable = true;
        part.runBodyType = RigidbodyType2D.Kinematic;
    }

    private static void ConfigureBox(Transform root)
    {
        GameObject box = GetOrCreateChild(root, "Box");
        box.transform.position = new Vector3(0.6f, 1.2f, 0f);
        box.transform.rotation = Quaternion.identity;

        ConfigureRenderer(box, new Vector2(1.25f, 1.25f), new Color(0.58f, 0.34f, 0.16f, 1f), 3);

        Rigidbody2D rb = EnsureComponent<Rigidbody2D>(box);
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 1f;
        rb.mass = 1.5f;

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(box);
        collider.isTrigger = false;
        collider.size = new Vector2(1.25f, 1.25f);

        Part part = EnsureComponent<Part>(box);
        part.draggable = true;
        part.runBodyType = RigidbodyType2D.Dynamic;
    }

    private static void ConfigureBall(Transform root)
    {
        GameObject ball = GetOrCreateChild(root, "Ball");
        ball.tag = "Ball";
        ball.transform.position = new Vector3(-5f, 2.9f, 0f);
        ball.transform.rotation = Quaternion.identity;

        ConfigureRenderer(ball, new Vector2(1f, 1f), new Color(0.82f, 0.86f, 0.92f, 1f), 4);

        Rigidbody2D rb = EnsureComponent<Rigidbody2D>(ball);
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 1f;
        rb.mass = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D oldBox = ball.GetComponent<BoxCollider2D>();
        if (oldBox != null)
        {
            Object.DestroyImmediate(oldBox);
        }

        CircleCollider2D collider = EnsureComponent<CircleCollider2D>(ball);
        collider.isTrigger = false;
        collider.radius = 0.5f;

        Part part = EnsureComponent<Part>(ball);
        part.draggable = true;
        part.runBodyType = RigidbodyType2D.Dynamic;
    }

    private static void ConfigureTarget(Transform root)
    {
        GameObject target = GetOrCreateChild(root, "Target");
        target.transform.position = new Vector3(4.8f, -3.15f, 0f);
        target.transform.rotation = Quaternion.identity;

        ConfigureRenderer(target, new Vector2(2.1f, 1.4f), new Color(0.15f, 0.75f, 0.35f, 0.35f), 1);

        BoxCollider2D collider = EnsureComponent<BoxCollider2D>(target);
        collider.isTrigger = true;
        collider.size = new Vector2(2.1f, 1.4f);

        TargetZone targetZone = EnsureComponent<TargetZone>(target);
        targetZone.targetTag = "Ball";
    }

    private static void ConfigureRenderer(GameObject target, Vector2 size, Color color, int sortingOrder)
    {
        SpriteRenderer renderer = EnsureComponent<SpriteRenderer>(target);
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private static GameObject ConfigureUI(Transform root, GameManager gameManager)
    {
        GameObject canvasObject = GetOrCreateUIChild(root, "Canvas");
        Canvas canvas = EnsureComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = EnsureComponent<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureComponent<GraphicRaycaster>(canvasObject);

        Button startButton = ConfigureButton(canvasObject.transform, "StartButton", "START", new Vector2(170f, 70f));
        RectTransform startRect = (RectTransform)startButton.transform;
        startRect.anchorMin = Vector2.zero;
        startRect.anchorMax = Vector2.zero;
        startRect.pivot = Vector2.zero;
        startRect.anchoredPosition = new Vector2(40f, 40f);

        Button resetButton = ConfigureButton(canvasObject.transform, "ResetButton", "RESET", new Vector2(170f, 70f));
        RectTransform resetRect = (RectTransform)resetButton.transform;
        resetRect.anchorMin = Vector2.zero;
        resetRect.anchorMax = Vector2.zero;
        resetRect.pivot = Vector2.zero;
        resetRect.anchoredPosition = new Vector2(230f, 40f);

        ClearPersistentListeners(startButton);
        ClearPersistentListeners(resetButton);
        UnityEventTools.AddPersistentListener(startButton.onClick, gameManager.StartRun);
        UnityEventTools.AddPersistentListener(resetButton.onClick, gameManager.ResetToEdit);

        GameObject winPanel = GetOrCreateUIChild(canvasObject.transform, "WinPanel");
        RectTransform panelRect = (RectTransform)winPanel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = EnsureComponent<Image>(winPanel);
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject labelObject = GetOrCreateUIChild(winPanel.transform, "Label");
        RectTransform labelRect = (RectTransform)labelObject.transform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(900f, 160f);
        labelRect.anchoredPosition = Vector2.zero;

        Text label = EnsureComponent<Text>(labelObject);
        label.text = "LEVEL COMPLETE";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 64;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;

        winPanel.SetActive(false);
        return winPanel;
    }

    private static Button ConfigureButton(Transform parent, string name, string labelText, Vector2 size)
    {
        GameObject buttonObject = GetOrCreateUIChild(parent, name);
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.sizeDelta = size;

        Image image = EnsureComponent<Image>(buttonObject);
        image.color = new Color(0.14f, 0.42f, 0.78f, 0.96f);

        Button button = EnsureComponent<Button>(buttonObject);
        button.targetGraphic = image;

        GameObject textObject = GetOrCreateUIChild(buttonObject.transform, "Text");
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = EnsureComponent<Text>(textObject);
        text.text = labelText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        return button;
    }

    private static void ClearPersistentListeners(Button button)
    {
        while (button.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        }
    }

    private static void AssignWinPanel(GameManager gameManager, GameObject winPanel)
    {
        SerializedObject serializedObject = new SerializedObject(gameManager);
        SerializedProperty property = serializedObject.FindProperty("winPanel");
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = winPanel;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureEventSystem(Transform root)
    {
        GameObject eventSystemObject = GetOrCreateChild(root, "EventSystem");
        EnsureComponent<EventSystem>(eventSystemObject);

        StandaloneInputModule legacyModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
        {
            Object.DestroyImmediate(legacyModule);
        }

        EnsureComponent<InputSystemUIInputModule>(eventSystemObject);
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject GetOrCreateUIChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child is RectTransform)
        {
            return child.gameObject;
        }

        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }

        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }
}
