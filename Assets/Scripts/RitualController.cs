using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class RitualController : MonoBehaviour
{
    // Singleton for global access
    public static RitualController Instance { get; private set; }

    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Persist across scenes
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Si el panell no existeix, el creem primer
            //if (xrayPanel == null)
            if(infoWindow.xrayPanel == null)
            {
                infoWindow.CreateXrayWindow();
                OpenXrayWindow();
            }
            else
            {
                // Si ja existeix, mirem si està actiu per decidir si obrir o tancar
                //if (xrayPanel.activeSelf)
                if(infoWindow.xrayPanel.activeSelf)
                {
                    CloseXrayWindow();
                }
                else
                {
                    OpenXrayWindow();
                }
            }
        }
    }


    // Fields & references
    private RitualData ritualData;              // Loaded JSON ritual info
    private AudioManager audioManager;          // Handles music & dialogues
    private Camera mainCamera;                  // Main camera for scene

    public Avatar characterAvatar;              // Default avatar for characters
    public bool smoothCameraTransition;         // Smooth camera movement toggle

    private int currentIndex = 0;               // Index of current line in play
    private Vector3 newOrigin = Vector3.zero;   // Scene origin offset for positioning

    private Dictionary<string, Button> choiceButtons = new Dictionary<string, Button>(); // Buttons for each choice

    private GameObject loadingScreen;           // UI loading screen panel
    private Text loadingText;                   // Text for loading screen

    // Branches
    private bool waitForBranch = false;         // Flag per pausar esperant la tria de l'usuari
    private string selectedBranch = null;       // ID de la branca seleccionada
    private int branchEnd = -1;                 // Índex on acaba la branca actual
    private int afterBranchResumeIndex;         // Índex on reprendre el ritual després de la branca
    private GameObject choicePanel;             // Panell UI per a les opcions

    [Header("Current Line Info")]
    [TextArea(2, 5)]
    public string currentDescription;           // Description of the current line
    public string currentCharacters;            // Characters involved in current line
    public Vector3 currentCameraPos;            // Camera position for current line
    public string currentCameraTarget;          // Target object/character for camera to look at

    private bool isAutoPlaying = true; // Controls pause/resume

    private InfoWindow infoWindow;

    // Unity Start
    void Start()
    {
        EnsureEventSystemExists();              // Make sure input system exists
        CreateMainCamera();                     // Set up main camera
        CreateLoadingScreen();                  // Show loading UI
        AddSunLight();                          // Add directional sunlight

        LoadJson();                             // Load ritual data from JSON
        InstantiateModels();                    // Spawn environments, objects, characters
        PlaceObjects();                         // Apply position, rotation, scale
        AddAnimations();                        // Load character animations
        PlayBackgroundMusic();                  // Start music playback

        // Si ja el tens posat a l'escena:
        infoWindow = FindFirstObjectByType<InfoWindow>();

        // O si el vols crear com a l'Opció A:
        infoWindow = new GameObject("InfoWindow").AddComponent<InfoWindow>();

        StartCoroutine(AutoplayCoroutine());    // Begin automatic playback of the ritual
    }

    // Environment
    void SetOrigin(Environment env)
        => newOrigin = new Vector3(env.Origin[0], env.Origin[1], env.Origin[2]); // Set scene offset

    // Camera & Lighting
    void CreateMainCamera()
    {
        GameObject cameraObj = new GameObject("MainCamera");
        mainCamera = cameraObj.AddComponent<Camera>();
        cameraObj.tag = "MainCamera";

        cameraObj.transform.position = newOrigin; // Start at origin
        cameraObj.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

        mainCamera.clearFlags = CameraClearFlags.Skybox; // Default skybox
        mainCamera.fieldOfView = 60f;
    }

    void AddSunLight()
    {
        GameObject sunGO = new GameObject("SunLight");
        Light sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;       // Simulate sunlight
        sun.color = Color.white;
        sun.intensity = 1f;
        sun.shadows = LightShadows.Soft;        // Soft shadows
        sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f); // Directional angle
    }

    // Loading Screen UI
    void CreateLoadingScreen()
    {
        GameObject canvasGO = new GameObject("LoadingCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>(); canvasGO.AddComponent<GraphicRaycaster>();

        // Panel background
        loadingScreen = new GameObject("LoadingPanel");
        loadingScreen.transform.SetParent(canvasGO.transform, false);
        Image bg = loadingScreen.AddComponent<Image>(); bg.color = Color.black;
        RectTransform bgRect = loadingScreen.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        // Loading text
        GameObject textGO = new GameObject("LoadingText");
        textGO.transform.SetParent(loadingScreen.transform, false);
        loadingText = textGO.AddComponent<Text>();
        loadingText.text = "Loading";
        loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        loadingText.alignment = TextAnchor.MiddleCenter; loadingText.color = Color.white; loadingText.fontSize = 48;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

        StartCoroutine(AnimateLoadingDots()); // Animate dots in "Loading..."
    }

    IEnumerator AnimateLoadingDots()
    {
        string baseText = "Loading"; int dotCount = 0;
        while (loadingScreen != null && loadingScreen.activeSelf)
        {
            dotCount = (dotCount + 1) % 4; // Cycle dots 0..3
            string dots = new string('.', dotCount);
            loadingText.text = baseText + dots.PadRight(3, ' '); // Keep width consistent
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator HideLoadingScreenAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (loadingScreen != null) loadingScreen.SetActive(false);
    }

    // Audio
    private void PlayBackgroundMusic()
    {
        GameObject go = new GameObject("AudioManager"); audioManager = go.AddComponent<AudioManager>();
        AudioSource musicSrc = new GameObject("MusicSource").AddComponent<AudioSource>(); musicSrc.transform.SetParent(go.transform);
        AudioSource sfxSrc = new GameObject("SFXSource").AddComponent<AudioSource>(); sfxSrc.transform.SetParent(go.transform);
        audioManager.musicSource = musicSrc; audioManager.sfxSource = sfxSrc;

        if (ritualData.Music == null || ritualData.Music.Count == 0) { Debug.LogWarning("No background music."); return; }

        List<AudioClip> clips = new List<AudioClip>();
        foreach (var music in ritualData.Music)
        {
            if (!string.IsNullOrEmpty(music.Id))
            {
                AudioClip clip = Resources.Load<AudioClip>($"Audios/{music.Id}"); // Load clip from Resources
                if (clip != null) clips.Add(clip);
                else Debug.LogError($"Music clip '{music.Id}' not found!");
            }
        }
        if (clips.Count > 0) audioManager.PlayMusicPlaylist(clips); // Play all loaded clips
    }

    // Event System & Choice UI
    void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>(); // Default input handling
        }
    }

    // Data Loading & Object Instantiation
    void LoadJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("ritual");
        if (jsonFile != null)
        {
            ritualData = JsonUtility.FromJson<RitualData>(jsonFile.text);
            Debug.Log("JSON Loaded Successfully");
        }
        else { Debug.LogError("JSON file not found in Resources folder"); }

        // Initialize environment
        SetOrigin(ritualData.FindEnvironment(ritualData.Play[0].EnvironmentID));

        if (ritualData.Branches != null && ritualData.Branches.Count > 0)
        {
            afterBranchResumeIndex = ritualData.Branches.Last().EndingLine - 1;
        }
    }

    void InstantiateModels()
    {
        if (ritualData == null) return;

        // Environments
        foreach (var env in ritualData.Environments)
        {
            env.ModelObject = ModelLoader.InstantiateModel(env);
            env?.InitiatePhysics();
        }

        // Objects
        foreach (var obj in ritualData.Objects)
        {
            obj.ModelObject = ModelLoader.InstantiateModel(obj);
            obj?.InitiatePhysics();
        }

        // Characters
        foreach (var character in ritualData.Characters)
        {
            character.ModelObject = ModelLoader.InstantiateModel(character);
            character.CharacterAvatar = characterAvatar;
            character.AddAnimator();
            character.ModelObject.AddComponent<CharacterAnimEvents>();
        }
    }

    void PlaceObjects()
    {
        foreach (var obj in ritualData.Objects)
        {
            if (obj == null) continue;

            Vector3 position = obj.Location != null && obj.Location.Length >= 3
                ? ToLocal(new Vector3(obj.Location[0], obj.Location[1], obj.Location[2]))
                : Vector3.one;

            Quaternion rotation = obj.Rotation != null && obj.Rotation.Length >= 3
                ? Quaternion.Euler(obj.Rotation[0], obj.Rotation[1], obj.Rotation[2])
                : Quaternion.identity;

            Vector3 scale = obj.Scale != null && obj.Scale.Length >= 3
                ? new Vector3(obj.Scale[0], obj.Scale[1], obj.Scale[2])
                : Vector3.one;

            obj.UpdateTransform(position, rotation, scale);
        }
    }

    // Animation Handling
    void AddAnimations()
    {
        // Load modified clips (custom events)
        AnimationClip[] modifiedClips = Resources.LoadAll<AnimationClip>("Animations/Modified");
        foreach (var clip in modifiedClips)
        {
            int idx = clip.name.IndexOf('_');
            string characterName = clip.name.Substring(0, idx);
            string animationName = clip.name.Substring(idx + 1);

            Character character = ritualData.FindCharacter(characterName);
            if (character == null) continue;

            AnimationClip newClip = new AnimationClip();
            EditorUtility.CopySerialized(clip, newClip);
            AnimationUtility.SetAnimationEvents(newClip, AnimationUtility.GetAnimationEvents(clip));
            newClip.name = animationName;

            character.AddAnimation(newClip);
            Debug.Log($"Added modified animation '{newClip.name}' to '{characterName}'");
        }

        // Load standard animations
        foreach (var anim in ritualData.Animations)
        {
            if (anim.CharacterID == null || anim.Id == null) continue;
            Character character = ritualData.FindCharacter(anim.CharacterID);
            if (character == null) continue;
            if (character.HasAnimation(anim.Id)) continue;

            string animationName = $"{anim.CharacterID.ToLower()}_{anim.Id}";
            AnimationClip clip = Resources.Load<AnimationClip>($"Animations/Accelerated/{animationName}");
            if (clip == null) { Debug.LogError($"Animation {animationName} not found."); continue; }

            clip.name = anim.Id;
            character.AddAnimation(clip);
        }
    }

    // Yield
    private IEnumerator WaitWhilePaused(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (isAutoPlaying)
                //elapsed += Time.deltaTime;
                elapsed += Time.unscaledDeltaTime; // ⬅️ IMPORTANT
            yield return null;
        }
    }


    // Core Gameplay Flow
    IEnumerator AutoplayCoroutine()
    {
        loadingScreen?.SetActive(true);
        yield return new WaitForSeconds(5f);
        StartCoroutine(HideLoadingScreenAfterDelay(10f));

        while (currentIndex < ritualData.Play.Count)
        {
            // LÒGICA DE BRANCHES
            // 1. Comprovar si toca triar branca
            if (selectedBranch == null && ritualData.Branches.Any(b => b.StartingLine - 2 == currentIndex))
            {
                waitForBranch = true;
                if (choicePanel == null) CreateChoicePanel();
                else choicePanel.SetActive(true);

                yield return new WaitWhile(() => waitForBranch);
            }

            // 2. Comprovar si hem acabat una branca per saltar al final
            if (selectedBranch != null && currentIndex > branchEnd)
            {
                currentIndex = afterBranchResumeIndex;
                selectedBranch = null; // Reset per permetre futures branques si n'hi hagués
            }

            //var currentLine = ritualData.Play[currentIndex];
            UpdateLineInfo();     // Update current description, characters, camera info
            ExecuteNextAction();  // Move characters, play animations, audio, camera

            var currentLine = ritualData.Play[currentIndex - 1];
            float waitTime = 5f;

            // Adjust wait time based on animation length
            if (currentLine.Duration == -1 && !string.IsNullOrEmpty(currentLine.Animation))
            {
                string animName = $"{currentLine.CharacterID.ToLower()}_{currentLine.Animation}";
                AnimationClip clip = Resources.Load<AnimationClip>($"Animations/{animName}");
                if (clip != null) waitTime = clip.length;
            }
            else if (currentLine.Duration > 0) waitTime = currentLine.Duration;

            yield return WaitWhilePaused(waitTime); //This ensures that if isAutoPlaying is false (paused), the coroutine stops counting time until resumed.

            //yield return new WaitForSeconds(waitTime);
        }

        Debug.Log("Ritual playback finished.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void PauseAutoplay() => isAutoPlaying = false;

    public void ResumeAutoplay()
    {
        // col·locar cam a la posició pre-xray
        isAutoPlaying = true;
    }
    

    public void OpenXrayWindow()
    {
        if (infoWindow.xrayPanel == null)
            infoWindow.CreateXrayWindow();

        // Si ja està obert, no fem res (opcional, per seguretat)
        if (infoWindow.xrayPanel.activeSelf) return;

        PauseAutoplay();

        Time.timeScale = 0f;          // Freeze animations, camera, physics
        audioManager?.PauseDialogue(); // ⬅️ ONLY dialogue

        infoWindow.PopulateXrayData();

        infoWindow.xrayPanel.SetActive(true);
    }
    public void CloseXrayWindow()
    {
        infoWindow.xrayPanel.SetActive(false);

        Time.timeScale = 1f;   // ⬅️ RESUME TIME
        audioManager?.ResumeDialogue();  // ⬅️ Resume dialogue

        ResumeAutoplay();
    }

    void UpdateLineInfo()
    {
        if (ritualData.Play == null || currentIndex >= ritualData.Play.Count) return;

        var line = ritualData.Play[currentIndex];
        currentDescription = line.Description;
        currentCharacters = line.CharacterID;

        if (line.CameraPos?.Length >= 3)
            currentCameraPos = new Vector3(line.CameraPos[0], line.CameraPos[1], line.CameraPos[2]);

        currentCameraTarget = line.CameraTarget;
    }

    // Execute Actions for Current Line
    void ExecuteNextAction()
    {
        if (currentIndex >= ritualData.Play.Count) return;

        Play currentLine = ritualData.Play[currentIndex];
        Debug.Log($"Executing line {currentIndex}: {currentLine.Description}");

        // Environment change
        if (!string.IsNullOrEmpty(currentLine.EnvironmentID))
        {
            Environment env = ritualData.FindEnvironment(currentLine.EnvironmentID);
            if (env != null) SetOrigin(env);
        }

        // Character placement and rotation
        Character character = ritualData.FindCharacter(currentLine.CharacterID);
        Character animationTarget = !string.IsNullOrEmpty(currentLine.Target)
            ? ritualData.FindCharacter(currentLine.Target)
            : null;

        if (currentLine.StartPos != null && character != null)
        {
            Place place = ritualData.FindPlace(currentLine.StartPos);
            if (place != null)
            {
                Vector3 position = ToLocal(new Vector3(place.Location[0], place.Location[1], place.Location[2]));
                Vector3 scale = new Vector3(character.Scale[0], character.Scale[1], character.Scale[2]);

                Quaternion rotation = character.ModelObject.transform.rotation;
                if (animationTarget?.ModelObject != null)
                {
                    Vector3 direction = (animationTarget.ModelObject.transform.position - position).normalized;
                    rotation = Quaternion.LookRotation(direction);
                }

                character.UpdateTransform(position, rotation, scale);
            }
        }

        // Camera
        Vector3 cameraPosition = mainCamera.transform.position;
        Transform lookAtTarget = null;

        if (currentLine.CameraPos?.Length >= 3)
            cameraPosition = ToLocal(new Vector3(currentLine.CameraPos[0], currentLine.CameraPos[1], currentLine.CameraPos[2]));

        if (!string.IsNullOrEmpty(currentLine.CameraTarget))
            lookAtTarget = ritualData.FindCharacter(currentLine.CameraTarget)?.ModelObject?.transform
                ?? ritualData.FindObject(currentLine.CameraTarget)?.ModelObject?.transform;

        if (smoothCameraTransition)
            SmoothCameraTransition(cameraPosition, true, lookAtTarget, 5f);
        else
        {
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.LookAt(lookAtTarget);
        }

        // Animations & Audio
        if (!string.IsNullOrEmpty(currentLine.Animation) && character != null)
            character.PlayAnimation(currentLine.Animation);

        if (!string.IsNullOrEmpty(currentLine.Audio))
        {
            AudioClip audioClip = Resources.Load<AudioClip>($"Audios/{currentLine.Audio}");
            audioManager.PlaySFX(audioClip);
        }

        currentIndex++;
    }

    // Smooth Camera
    void SmoothCameraTransition(Vector3 targetPosition, bool hasNewPosition, Transform lookAtTarget, float duration)
        => StartCoroutine(CameraTransitionCoroutine(targetPosition, hasNewPosition, lookAtTarget, duration));

    IEnumerator CameraTransitionCoroutine(Vector3 targetPosition, bool hasNewPosition, Transform lookAtTarget, float duration)
    {
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        Vector3 endPos = hasNewPosition ? targetPosition : startPos;
        Quaternion endRot = startRot;

        if (lookAtTarget != null)
        {
            Vector3 dir = lookAtTarget.position - endPos;
            if (dir != Vector3.zero) endRot = Quaternion.LookRotation(dir);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = endPos;
        mainCamera.transform.rotation = endRot;
    }

    void CreateChoicePanel()
    {
        // Canvas
        GameObject canvasGO = new GameObject("ChoiceCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>(); canvasGO.AddComponent<GraphicRaycaster>();

        canvas.sortingOrder = 10; // Prioritat mitjana

        // Panel background
        choicePanel = new GameObject("ChoicePanel");
        choicePanel.transform.SetParent(canvasGO.transform, false);
        Image panelImage = choicePanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        RectTransform panelRect = choicePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.3f);
        panelRect.anchorMax = new Vector2(0.9f, 0.7f);
        panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;

        // Local helper to create buttons
        Button CreateButton(string label, string id)
        {
            GameObject buttonGO = new GameObject("Button" + id);
            buttonGO.transform.SetParent(choicePanel.transform, false);

            Button button = buttonGO.AddComponent<Button>();
            Image btnImage = buttonGO.AddComponent<Image>(); btnImage.color = Color.white;

            RectTransform btnRect = buttonGO.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(600, 60); btnRect.anchorMin = btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);

            // Button label
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            Text text = textGO.AddComponent<Text>();
            text.text = label; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter; text.color = Color.black; text.fontSize = 20;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;

            button.onClick.AddListener(() => SelectBranch(id));
            return button;
        }

        // Create buttons dynamically
        List<Branch> branches = ritualData.Branches;
        float step = 70f;
        for (int i = 0; i < branches.Count; i++)
        {
            string buttonText = branches[i].ButtonText;
            string branchID = branches[i].Id;
            Button newButton = CreateButton(buttonText, branchID);
            RectTransform btnRect = newButton.GetComponent<RectTransform>();

            float totalHeight = branches.Count * step;
            float startY = totalHeight / 2f - step / 2f;
            btnRect.anchoredPosition = new Vector2(0, startY - i * step);
        }
    }

    public void SelectBranch(string branchID)
    {
        selectedBranch = branchID;
        waitForBranch = false;

        var branch = ritualData.Branches.FirstOrDefault(b => b.Id == branchID);
        if (branch == null) { Debug.LogError($"Branch {branchID} not found."); return; }

        int branchStartIndex = branch.StartingLine - 2; // Convert to 0-based
        branchEnd = branch.EndingLine - 2;
        currentIndex = branchStartIndex;

        choicePanel?.SetActive(false);

        Debug.Log($"Branch selected: {branchID}, from {branchStartIndex} to {branchEnd}");
    }


    // Utilities
    Vector3 ToLocal(Vector3 worldPosition) => worldPosition + newOrigin;

    public RitualData GetRitualData() => ritualData;
}