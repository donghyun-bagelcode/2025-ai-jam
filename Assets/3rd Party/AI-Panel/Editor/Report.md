## 파일 구조 트리

└── Editor/
    ├── AI/
    │   ├── Interfaces/
    │   │   └── IAIModel.cs
    │   ├── Managers/
    │   │   ├── AIManager.cs
    │   │   └── PromptManager.cs
    │   ├── Models/
    │   │   ├── AIResponse.cs
    │   │   └── ChatGPTAssistant/
    │   │       ├── ChatGPTAPIAsync.cs
    │   │       └── ChatGPTAssistant.cs
    │   ├── Services/
    │   │   └── ChatMessageService.cs
    │   └── prompts/
    ├── CustomUI/
    │   ├── ChatGPTAssistantSetting.cs
    │   ├── ChatMessage.cs
    │   ├── ClaudeSonnetSetting.cs
    │   ├── CustomAIPanel.cs
    │   ├── DirectingCommentaryWindow.cs
    │   └── FileSelectionWindow.cs
    └── Utils/
        ├── CsvToJsonConverter.cs
        ├── JsonStatisticsCalculator.cs
        ├── JsonToCsvConverter.cs
        ├── SecurePlayerPrefs.cs
        └── UnityWebRequestExtensions.cs


## 파일 내용

CustomUI/ChatGPTAssistantSetting.cs
```csharp
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public enum ThreadType
{
    Dialog,
    Directing
}

[Serializable]
public struct ThreadSettings
{
    public string assistantId;
    public string threadId;
    public float temperature;
    public string model;
}

public class ChatGPTAssistantSetting : EditorWindow
{
    private const string modelName = "ChatGPTAssistant";
    private string apiKey = "";
    private string[] GPTmodels = new string[] { "gpt-4o", "gpt-4o-mini"};

    private ThreadSettings dialogThread = new ThreadSettings();
    private ThreadSettings directingThread = new ThreadSettings();

    private bool addNodeToEnd = false;
    private bool directingCommentMode = false;
    private GUIStyle labelStyle;

    [MenuItem("Leman/Settings/ChatGPT Assistant Settings", false, 101)]
    public static void OpenWindow()
    {
        var window = GetWindow<ChatGPTAssistantSetting>(true, "ChatGPT Assistant Settings", true);
        window.LoadSettings();
    }

    private void OnEnable()
    {
        InitializeStyles();
    }

    private void InitializeStyles()
    {
        if (EditorStyles.textArea == null)
        {
            // EditorStyles가 아직 준비되지 않았으므로 다시 시도
            EditorApplication.delayCall += InitializeStyles;
            return;
        }

        labelStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
    }

    void OnGUI()
    {
        // ChatGPT Assistant Settings
        GUILayout.Label("ChatGPT Assistant Settings", labelStyle);
        apiKey = EditorGUILayout.TextField("API Key", apiKey);
        GUILayout.Space(30);

        // ChatGPT Dialog Assistant Settings
        GUILayout.Label("ChatGPT Dialog Assistant Settings", labelStyle);
        dialogThread.assistantId = EditorGUILayout.TextField("Assistant ID", dialogThread.assistantId);
        dialogThread.threadId = EditorGUILayout.TextField("Thread ID", dialogThread.threadId);

        DrawCreateThreadButton(ThreadType.Dialog);
    
        dialogThread.temperature = EditorGUILayout.Slider("Temperature", dialogThread.temperature, 0.0f, 1.0f);
        dialogThread.model = GPTmodels[EditorGUILayout.Popup("Model", Array.IndexOf(GPTmodels, dialogThread.model), GPTmodels)];
        GUILayout.Space(30);

        // ChatGPT Directing Assistant Settings
        GUILayout.Label("ChatGPT Directing Assistant Settings", labelStyle);
        directingThread.assistantId = EditorGUILayout.TextField("Assistant ID", directingThread.assistantId);
        directingThread.threadId = EditorGUILayout.TextField("Thread ID", directingThread.threadId);

        DrawCreateThreadButton(ThreadType.Directing);

        directingThread.temperature = EditorGUILayout.Slider("Temperature", directingThread.temperature, 0.0f, 1.0f);
        directingThread.model = GPTmodels[EditorGUILayout.Popup("Model", Array.IndexOf(GPTmodels, directingThread.model), GPTmodels)];


        // Additional Options
        GUILayout.Space(30);
        GUILayout.Label("Additional Options", labelStyle);
        
        // Add Node to End
        GUIContent addNodeToEndContent = new GUIContent("Add Node to End", "노드 추가 옵션을 선택합니다. 체크 시 현재 창의 좌상단 1/3 지점에서 노드가 추가되며, 노드 번호는 현재 그래프의 최대값+1부터 시작합니다.");
        addNodeToEnd = EditorGUILayout.Toggle(addNodeToEndContent, addNodeToEnd);

        // Comment for AI Directing
        GUIContent directingCommentModeContent = new GUIContent("Comment for AI Directing", "AI Directing 코멘터리 창의 표시 여부를 선택합니다. 체크 시 Apply with AI Directing 버튼을 누르면 코멘터리 창이 열립니다.");
        directingCommentMode = EditorGUILayout.Toggle(directingCommentModeContent, directingCommentMode);

        // Save Button
        GUILayout.Space(30);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Save", GUILayout.Width(300)))
        {
            SaveSettings();
            EditorUtility.DisplayDialog("Settings Saved", "Your settings have been saved successfully.", "OK");
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 스레드 생성 버튼을 그리는 메서드
    /// </summary>
    /// <param name="threadType">스레드 타입</param>
    /// <param name="threadId">스레드 ID 참조</param>
    private void DrawCreateThreadButton(ThreadType threadType)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button($"Create New {threadType.ToString()} Thread", GUILayout.Width(300)))
        {
            if (EditorUtility.DisplayDialog($"Create New {threadType.ToString()} Thread", $"Are you sure you want to create a new {threadType.ToString().ToLower()} thread?", "Yes", "No"))
            {
                CreateNewThreadAsync(threadType);
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 스레드를 비동기적으로 생성하는 메서드
    /// </summary>
    /// <param name="threadType">스레드 타입</param>
    private async void CreateNewThreadAsync(ThreadType threadType)
    {
        try
        {
            // 비동기 스레드 생성 시작
            var (success, newThreadId) = await ChatGPTThreadAPIAsync.CreateThreadAsync();

            if (success && !string.IsNullOrEmpty(newThreadId))
            {
                if (threadType == ThreadType.Dialog)
                {
                    dialogThread.threadId = newThreadId;
                    SecurePlayerPrefs.SetString("ChatGPTAssistant_ThreadId", dialogThread.threadId);
                }
                else if (threadType == ThreadType.Directing)
                {
                    directingThread.threadId = newThreadId;
                    SecurePlayerPrefs.SetString("ChatGPTAssistant_ThreadDirectingId", directingThread.threadId);
                }

                SecurePlayerPrefs.Save();

                Debug.Log($"Thread Created ({threadType}): " + newThreadId);
                EditorUtility.DisplayDialog("Thread Created", $"A new {threadType.ToString().ToLower()} thread has been created successfully.\nThread ID: {newThreadId}", "OK");
            }
            else
            {
                Debug.LogError($"Failed to create {threadType} thread: " + newThreadId);
                EditorUtility.DisplayDialog("Thread Creation Failed", $"Failed to create a new {threadType.ToString().ToLower()} thread.\nError: {newThreadId}", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating {threadType} thread: {ex.Message}");
            EditorUtility.DisplayDialog("Thread Creation Failed", $"An error occurred while creating a new {threadType.ToString().ToLower()} thread.\nError: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// 설정을 저장하는 메서드
    /// </summary>
    private void SaveSettings()
    {
        SecurePlayerPrefs.SetString("ChatGPTAssistant_ApiKey", apiKey.Trim());

        // ChatGPT Regular Assistant Settings
        SecurePlayerPrefs.SetString("ChatGPTAssistant_AssistantId", dialogThread.assistantId.Trim());
        SecurePlayerPrefs.SetString("ChatGPTAssistant_ThreadId", dialogThread.threadId.Trim());
        SecurePlayerPrefs.SetFloat("ChatGPTAssistant_Temperature", dialogThread.temperature);
        SecurePlayerPrefs.SetString("ChatGPTAssistant_Model", dialogThread.model);

        // ChatGPT Directing Assistant Settings
        SecurePlayerPrefs.SetString("ChatGPTAssistant_AssistantIdDirecting", directingThread.assistantId.Trim());
        SecurePlayerPrefs.SetString("ChatGPTAssistant_ThreadDirectingId", directingThread.threadId.Trim());
        SecurePlayerPrefs.SetFloat("ChatGPTAssistant_TemperatureDirecting", directingThread.temperature);
        SecurePlayerPrefs.SetString("ChatGPTAssistant_ModelDirecting", directingThread.model);

        // Additional Options
        SecurePlayerPrefs.SetInt("ChatGPTAssistant_AddNodeToEnd", addNodeToEnd ? 1 : 0);
        SecurePlayerPrefs.SetInt("ChatGPTAssistant_DirectingCommentMode", directingCommentMode ? 1 : 0);

        SecurePlayerPrefs.Save();
    }

    /// <summary>
    /// 설정을 로드하는 메서드
    /// </summary>
    private void LoadSettings()
    {
        apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");

        // ChatGPT Regular Assistant Settings
        dialogThread.assistantId = SecurePlayerPrefs.GetString("ChatGPTAssistant_AssistantId", "");
        dialogThread.threadId = SecurePlayerPrefs.GetString("ChatGPTAssistant_ThreadId", "");
        dialogThread.temperature = SecurePlayerPrefs.GetFloat("ChatGPTAssistant_Temperature", 0.5f);
        dialogThread.model = SecurePlayerPrefs.GetString("ChatGPTAssistant_Model", GPTmodels[0]);

        // ChatGPT Directing Assistant Settings
        directingThread.assistantId = SecurePlayerPrefs.GetString("ChatGPTAssistant_AssistantIdDirecting", "");
        directingThread.threadId = SecurePlayerPrefs.GetString("ChatGPTAssistant_ThreadDirectingId", "");
        directingThread.temperature = SecurePlayerPrefs.GetFloat("ChatGPTAssistant_TemperatureDirecting", 0.5f);
        directingThread.model = SecurePlayerPrefs.GetString("ChatGPTAssistant_ModelDirecting", GPTmodels[0]);

        // Additional Options
        addNodeToEnd = SecurePlayerPrefs.GetInt("ChatGPTAssistant_AddNodeToEnd", 0) == 1;
        directingCommentMode = SecurePlayerPrefs.GetInt("ChatGPTAssistant_DirectingCommentMode", 0) == 1;
    }
}
```

CustomUI/ChatMessage.cs
```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[System.Serializable]
public class ChatMessage
{
    public string request = "";
    public string message = "";
    public CodeFileListWrapper codeFiles = null;
    public int currentCodePage = 0;
    public Vector2 codeScrollPosition = Vector2.zero;

    public ChatMessage(string request, string message, List<CodeFile> codes = null)
    {
        this.request = request;
        this.message = message;
        if (codes != null)
        {
            this.codeFiles = new CodeFileListWrapper(codes);
        }
    }
}
// 직렬화를 위한 List<CodeFile> wrapper 클래스
[System.Serializable]
public class CodeFileListWrapper
{
    public List<CodeFile> codes;

    public CodeFileListWrapper(List<CodeFile> codes)
    {
        this.codes = codes;
    }
}
```

CustomUI/ClaudeSonnetSetting.cs
```csharp
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public class ClaudeSonnetSetting : EditorWindow
{
    private const string modelName = "ClaudeSonnet";
    private string apiKey = "";
    private float temperature = 0.5f;

    [MenuItem("Leman/Settings/Claude Sonnet Settings", false, 102)]
    public static void OpenWindow()
    {
        var window = GetWindow<ClaudeSonnetSetting>(true, "Claude Sonnet Settings", true);
        window.LoadSettings();

    }

    void OnGUI()
    {
        GUILayout.Label("Claude Sonnet Settings", EditorStyles.boldLabel);

        apiKey = EditorGUILayout.TextField("API Key", apiKey);
        temperature = EditorGUILayout.Slider("Temperature", temperature, 0.0f, 1.0f);

        if (GUILayout.Button("Save"))
        {
            SaveSettings();
            EditorUtility.DisplayDialog("Settings Saved", "Your settings have been saved successfully.", "OK");
        }
    }
    private void SaveSettings()
    {
        SecurePlayerPrefs.SetString("ClaudeSonnet_ApiKey", apiKey);
        SecurePlayerPrefs.SetFloat("ClaudeSonnet_Temperature", temperature);
        SecurePlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        apiKey = SecurePlayerPrefs.GetString("ClaudeSonnet_ApiKey", "None");
        temperature = SecurePlayerPrefs.GetFloat("ClaudeSonnet_Temperature", 0.5f);
    }
}
```

CustomUI/CustomAIPanel.cs
```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Dunward.Capricorn;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;



public class CustomAIPanel : EditorWindow
{
    // UI 관련 필드
    private GUIStyle textAreaStyle;
    private string userInput = "";
    private char userInputLastCharacter;
    private const float MinUserInputHeight = 23;
    private const float MaxUserInputHeight = 150;
    private Vector2 userInputScrollPosition;

    private Vector2 scrollPosition;

    private GUIStyle messageStyle;
    private GUIStyle codeNameStyle;
    private GUIStyle codeTextStyle;
    private GUIStyle loadingLabelStyle;
    private List<ChatMessage> ChatMessages = new List<ChatMessage>();
    string[] models = new string[] { "GPT Assistant", "Claude 3.5 Sonnet"};
    private int selectedAIModelIndex = 0;
    private int previousSelectedAIModelIndex = -1;

    private ChatMessageService chatMessageService;
    private AIManager aiManager;

    private bool stylesInitialized = false;

    // **로딩 인디케이터 관련 변수 추가**
    private bool isLoading = false; // 로딩 상태 플래그
    private float loadingTimer = 0f; // 로딩 타이머
    private float loadingInterval = 0.5f; // 로딩 애니메이션 업데이트 간격 (초)
    private int loadingDotCount = 0; // 로딩 애니메이션의 현재 점 개수
    private double lastUpdateTime; // 마지막 업데이트 시간

    // 추가 옵션 관련 변수
    bool addNodeToEnd = false;
    private bool directingCommentMode = false;

    static JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto, //필수옵션
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.Indented
    };

    [MenuItem("Leman/Chat Panel",false, 1)]
    public static void ShowWindow()
    {
        GetWindow<CustomAIPanel>("Chat with AI");
    }

    [InitializeOnLoadMethod]
    private static void OnProjectLoadOrRecompile()
    {
        EditorApplication.delayCall += () =>
        {
            CustomAIPanel[] windows = Resources.FindObjectsOfTypeAll<CustomAIPanel>();
            foreach (CustomAIPanel window in windows)
            {
                window.Initialize();
            }
        };
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        // EditorStyles가 초기화될 때까지 대기
        EditorApplication.delayCall += InitializeStyles;
        InitializeDependencies();

        // **로딩 인디케이터 초기화**
        lastUpdateTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdateLoading; // 로딩 애니메이션 업데이트 메서드 등록
    }

    private void InitializeStyles()
    {
        if (EditorStyles.textArea == null)
        {
            // EditorStyles가 아직 준비되지 않았으므로 다시 시도
            EditorApplication.delayCall += InitializeStyles;
            return;
        }

        // Set user input text area style
        textAreaStyle = new GUIStyle(EditorStyles.textArea);
        textAreaStyle.wordWrap = true;

        // Set message text area style
        messageStyle = new GUIStyle(EditorStyles.textArea);
        messageStyle.normal.background = Texture2D.blackTexture;
        messageStyle.padding = new RectOffset(0, 0, 10, 10);

        // Custom style for code text area
        codeNameStyle = new GUIStyle(EditorStyles.textArea);
        codeNameStyle.normal.textColor = Color.white;
        codeNameStyle.normal.background = Texture2D.blackTexture;

        codeTextStyle = new GUIStyle(EditorStyles.textArea);
        codeTextStyle.normal.textColor = Color.white;
        codeTextStyle.hover.textColor = Color.white;
        codeTextStyle.wordWrap = false;

        loadingLabelStyle = new GUIStyle(EditorStyles.label);
        loadingLabelStyle.alignment = TextAnchor.UpperLeft;
        loadingLabelStyle.fontSize = 15;
        loadingLabelStyle.normal.textColor = Color.white;
        loadingLabelStyle.hover.textColor = Color.white;

        stylesInitialized = true;
        Repaint();
    }

    private void InitializeDependencies()
    {
        if (chatMessageService == null) chatMessageService = new ChatMessageService();
        if (aiManager == null) aiManager = new AIManager(new ChatGPTAssistant());
    }

    private void OnDisable()
    {
        // **로딩 인디케이터 업데이트 메서드 해제**
        EditorApplication.update -= UpdateLoading;
        SaveChatMessages();
    }
    
    void OnGUI()
    {
        if (!stylesInitialized)
        {
            EditorGUILayout.LabelField("Initializing...");
            return;
        }

        // Null check and initialize dependencies if needed
        if (aiManager == null || chatMessageService == null)
        {
            InitializeDependencies();
        }

        DrawToolbar();
        DrawMessages();
        DrawLoadingIndicator();
        DrawUserInput();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("AI Status: Standby", EditorStyles.label);
        selectedAIModelIndex = EditorGUILayout.Popup(selectedAIModelIndex, models, EditorStyles.toolbarPopup, GUILayout.Width(100));

        if (selectedAIModelIndex != previousSelectedAIModelIndex)
        {
            UpdateModel();
            previousSelectedAIModelIndex = selectedAIModelIndex;
        }

        if (GUILayout.Button("Clear", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Clear Chat Messages", "Are you sure you want to clear all chat messages?", "Yes", "No"))
            {
                ClearChatMessages();
            }
        }

        if (GUILayout.Button("Settings", GUILayout.Width(80)))
        {
            OpenSettingsWindow();
        }

        GUILayout.EndHorizontal();
    }

    private void UpdateModel()
    {
        if (aiManager == null)
        {
            InitializeDependencies();
        }

        switch (selectedAIModelIndex)
        {
            case 0:
                aiManager.SetAIModel(new ChatGPTAssistant());
                break;
            case 1:
                aiManager.SetAIModel(new ClaudeSonnet());
                break;
            default:
                Debug.LogError("알 수 없는 AI 모델 인덱스입니다.");
                break;
        }
    }

    private void OpenSettingsWindow()
    {
        switch (selectedAIModelIndex)
        {
            case 0:
                ChatGPTAssistantSetting.OpenWindow();
                break;
            case 1:
                ClaudeSonnetSetting.OpenWindow();
                break;
            default:
                Debug.LogError("Selected model index is out of range.");
                break;
        }
    }

    private void DrawMessages()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DisplayMessages();
        EditorGUILayout.EndScrollView();
    }
    private void DisplayMessages()
    {
        var messages = chatMessageService.GetAllMessages();
        foreach (var message in messages)
        {
            if (message.message == null)
            {
                GUILayout.TextArea($"You: {message.request}\n\n", messageStyle, GUILayout.Height(40)); 
            }
            else
            {
                GUILayout.TextArea($"You: {message.request}\n\nAI: {message.message}", messageStyle); 
            }
            
            if (message.codeFiles != null)
            {
                if (message.codeFiles.codes.Count > 0)
                {
                    DisplayCodeViewer(message);
                }
            }
        }
    }

    private void DisplayCodeViewer(ChatMessage message)
    {
        // 상단의 Scene 정보 및 네비게이션 버튼 표시
        EditorGUILayout.BeginHorizontal();
        GUILayout.TextArea($"Scene : ({message.currentCodePage + 1}/{message.codeFiles.codes.Count}) {message.codeFiles.codes[message.currentCodePage].filename}", codeNameStyle, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("<", GUILayout.Width(30)))
        {
            message.currentCodePage = Mathf.Max(0, message.currentCodePage - 1);
        }
        if (GUILayout.Button(">", GUILayout.Width(30)))
        {
            message.currentCodePage = Mathf.Min(message.codeFiles.codes.Count - 1, message.currentCodePage + 1);
        }
        EditorGUILayout.EndHorizontal();

        // 코드 내용 가져오기
        string code = message.codeFiles.codes[message.currentCodePage].code;

        // GUIContent을 사용하여 코드의 높이 계산
        GUIContent content = new GUIContent(code);
        float requiredHeight = codeTextStyle.CalcHeight(content, position.width - 40); // 40은 좌우 여백을 고려한 값

        // 최소 및 최대 높이 설정
        float minHeight = 100f;
        float maxHeight = 250f;
        float textAreaHeight = Mathf.Clamp(requiredHeight, minHeight, maxHeight);

        // ScrollView 시작
        message.codeScrollPosition = EditorGUILayout.BeginScrollView(message.codeScrollPosition, GUILayout.Height(textAreaHeight));

        // 코드 TextArea 표시
        GUILayout.TextArea(code, codeTextStyle, GUILayout.ExpandHeight(false));

        // ScrollView 종료
        EditorGUILayout.EndScrollView();

        // Apply Nodes 버튼 표시
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUI.enabled = !isLoading;
        if (GUILayout.Button("Apply Nodes Only", GUILayout.Width(200)))
        {
            ApplyCurrentCode(message);
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Apply with AI Directing", GUILayout.Width(200)))
        {
            directingCommentMode = SecurePlayerPrefs.GetInt("ChatGPTAssistant_DirectingCommentMode", 0) == 1;
            if (directingCommentMode)
            {
                DirectingCommentaryWindow.ShowWindow((commentary) => 
                {
                    ApplyAIDirecting(message, commentary);
                });
            }
            else
            {
                ApplyAIDirecting(message, "");
            }
        }

        GUI.enabled = true;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void SaveChatMessages()
    {
        chatMessageService.SaveChatMessages();
        Debug.Log("Chat messages saved.");
    }

    private void ClearChatMessages()
    {
        chatMessageService.ClearChatMessages();
        Debug.Log("Chat messages cleared.");
        Repaint();
    }

    private void DrawLoadingIndicator()
    {
        if (isLoading)
        {
            GUILayout.Label($"  Creating{new string('.', loadingDotCount)}", loadingLabelStyle);
            GUILayout.FlexibleSpace();
        }
    }

    private void StartLoading()
    {
        isLoading = true;
        loadingDotCount = 0;
        loadingTimer = 0f;
        lastUpdateTime = EditorApplication.timeSinceStartup;
    }

    private void StopLoading()
    {
        isLoading = false;
    }

    private void UpdateLoading()
    {
        if (isLoading)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - lastUpdateTime);
            lastUpdateTime = currentTime;

            loadingTimer += deltaTime;
            if (loadingTimer >= loadingInterval)
            {
                loadingTimer -= loadingInterval;
                loadingDotCount = (loadingDotCount + 1) % 4; // 0 to 3
                Repaint();
            }
        }
    }

    private void DrawUserInput()
    {
        EditorGUILayout.BeginHorizontal();

        userInputScrollPosition = EditorGUILayout.BeginScrollView(userInputScrollPosition, GUILayout.Height(Mathf.Clamp(EditorStyles.textArea.CalcHeight(new GUIContent(userInput), position.width), MinUserInputHeight, MaxUserInputHeight)), GUILayout.ExpandHeight(true));

        GUILayout.FlexibleSpace();

        if (userInput.Length > 0 )
        {
            userInputLastCharacter = userInput[userInput.Length-1];
        }

        EditorGUI.BeginChangeCheck();
        GUI.SetNextControlName("UserInput");
        userInput = EditorGUILayout.TextArea(userInput, textAreaStyle);
        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

        bool textChanged = EditorGUI.EndChangeCheck();

        if (textChanged && (userInput.Length > 0))
        {
            if(userInputLastCharacter != userInput[^1])
            {
                userInputScrollPosition.y = EditorStyles.textArea.CalcHeight(new GUIContent(userInput), position.width);
            }
        }

        EditorGUILayout.EndScrollView();

        // **Send 버튼 비활성화**
        GUI.enabled = !isLoading;

        bool sendButton = GUILayout.Button("Send", EditorStyles.miniButtonRight, GUILayout.Width(50));
        bool sendKeyPressed = Event.current.type == EventType.KeyDown &&
                              Event.current.keyCode == KeyCode.Return &&
                              (Event.current.command || Event.current.control);

        if (sendButton || sendKeyPressed || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && Event.current.shift))
        {
            HandleUserInputAsync();
            GUI.FocusControl(null);
            Event.current.Use(); // 이벤트 사용 표시
        }

        // **Send 버튼 활성화**
        GUI.enabled = true;

        bool fileSelectionButton = GUILayout.Button("Select File", EditorStyles.miniButtonRight, GUILayout.Width(80));
        if (fileSelectionButton)
        {
            FileSelectorWindow.ShowWindow();
        }

    #region test
        bool testButton = GUILayout.Button("Test", EditorStyles.miniButtonRight, GUILayout.Width(50));
        if (testButton) {
            Test();
            Event.current.Use();
        }
    #endregion
        EditorGUILayout.EndHorizontal();
    }

    private async void HandleUserInputAsync()
    {
        if (string.IsNullOrEmpty(userInput))
        {
            Debug.LogWarning("User input is empty.");
            return;
        }

        string uiUserInput = userInput;
        string aiUserPrompt = userInput;

        // Add selected script files to the user prompt
        aiUserPrompt = PromptManager.AddFilesToUserPrompt(aiUserPrompt);
        uiUserInput = AddSelectedFilesList(uiUserInput);

        userInput = ""; // Clear user input field

        chatMessageService.AddUserMessage(uiUserInput); // Add user input message

        // **로딩 상태 시작**
        StartLoading();

        try
        {
            AIResponse response = await aiManager.GetResponseAsync(aiUserPrompt);
            response.ParseCsvResponse();
            chatMessageService.AddAIResponse(response);
            Repaint();
        }
        catch (Exception ex)
        {
            Debug.LogError($"HandleUserInputAsync 오류: {ex.Message}");
            chatMessageService.AddAIResponse(new AIResponse("AI 응답 생성 중 오류가 발생했습니다.", false, ex.Message));
        }
        finally
        {
            // **로딩 상태 종료**
            StopLoading();
            Repaint();
        }

    }
   
    private void ApplyCurrentCode(ChatMessage message)
    {
        var currentCodeFile = message.codeFiles.codes[message.currentCodePage];
        string json = CsvToJsonConverter.CsvToJsonConverterMethod(currentCodeFile.code);

        // Add node to end 옵션
        addNodeToEnd = SecurePlayerPrefs.GetInt("ChatGPTAssistant_AddNodeToEnd", 0) == 1;
        if (addNodeToEnd) json = AddNodeToEnd(json);
        
        AddNodesFromJson(json);
        Debug.Log("Applying current scene: " + currentCodeFile.filename);
    }

    private async void ApplyAIDirecting(ChatMessage message, string commentary)
    {
        string csvContents = message.codeFiles.codes[message.currentCodePage].code;
        string jsonContents = CsvToJsonConverter.CsvToJsonConverterMethod(csvContents);
        string directionPrompt = PromptManager.DirectingPrompt(csvContents, commentary);

        StartLoading();
        
        try
        {
            AIResponse response = await aiManager.GetDirectingResponseAsync(directionPrompt);
            response.ParseJsonResponse();
            if (response.Codes != null)
            {
                string direction = response.Codes[0].code;
                Debug.Log($"Directing Response: {direction}");
                string mergedJson = MergeDirecting(jsonContents, direction);

                // Add node to end 옵션
                addNodeToEnd = SecurePlayerPrefs.GetInt("ChatGPTAssistant_AddNodeToEnd", 0) == 1;
                if (addNodeToEnd) mergedJson = AddNodeToEnd(mergedJson);

                AddNodesFromJson(mergedJson);
                Debug.Log("Applying AI Directing");
            }
            else
            {
                Debug.LogWarning("Directing Response is null");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ApplyAIDirecting 오류: {ex.Message}");
        }
        finally
        {
            StopLoading();
            Repaint();
        }        
    }

    private string AddSelectedFilesList(string userInput)
    {
        List<string> selectedFiles = FileSelectorWindow.LoadSelectedFiles();
        if (selectedFiles.Count == 0)
        {
            return userInput;
        }

        string selectedFileList = "\n\n첨부된 파일 목록:\n";
        foreach (string file in selectedFiles)
        {
            selectedFileList += $"- {file.Replace(Application.dataPath, "Assets")}\n";
        }

        return userInput+selectedFileList;
    }

    public void AddNodesFromJson(string json)
    {
        var capriconWindow = GetWindow<CapricornEditorWindow>();
        capriconWindow.AddNodesFromJson(json);
    }

    public string MergeDirecting(string actionNodes, string coroutineData)
    {        
        //ToDo: AI directing 결과 json에서 Type 텍스트 대치
        var jObject = JObject.Parse(coroutineData);
        foreach (var node in jObject["nodes"])
        {
            var coroutineDataObj = node["coroutineData"];
            if (coroutineDataObj != null)
            {
                var coroutines = coroutineDataObj["coroutines"];
                if (coroutines != null && coroutines.HasValues)
                {
                    foreach (var coroutine in coroutines)
                    {
                        var coroutineObj = coroutine as JObject;
                        if (coroutineObj != null && coroutineObj["type"] != null)
                        {
                            string type = (string)coroutineObj["type"];
                            // 새로운 $type 값 생성
                            string newType = $"Dunward.Capricorn.{type}Unit, Assembly-CSharp";
                            // $type 필드 추가
                            coroutineObj.AddFirst(new JProperty("$type", newType));
                            // 기존 type 필드 제거
                            coroutineObj.Remove("type");
                        }
                    }
                }
            }
        }

        string updatedJson = JsonConvert.SerializeObject(jObject, jsonSettings);
        Debug.Log($"coroutineData \n{coroutineData}");
        Debug.Log($"updatedJson \n{updatedJson}");

        var jsonNodes = JsonConvert.DeserializeObject<GraphData>(actionNodes,jsonSettings);
        var jsonCoroutine = JsonConvert.DeserializeObject<GraphData>(updatedJson,jsonSettings);

        foreach (var node in jsonCoroutine.nodes)
        {
            // Debug.Log($"id : {node.id}");
            // Debug.Log($"count : {node.coroutineData.coroutines.Count}");

            int id = node.id;
            var targetNode = jsonNodes.nodes.Find(x => x.id == id);
            if (targetNode != null)
            {
                targetNode.coroutineData = node.coroutineData;
            }
        }

        string json = JsonConvert.SerializeObject(jsonNodes, jsonSettings);
        Debug.Log($"merge \n{json}");

        return json;
    }

    private static string AddNodeToEnd(string json)
    {
        GraphData root = JsonConvert.DeserializeObject<GraphData>(json, jsonSettings);
        root = AddNodeToEnd(root);
        return JsonConvert.SerializeObject(root, jsonSettings);
    }

    private static GraphData AddNodeToEnd(GraphData root)
    {
        var capriconWindow = EditorWindow.GetWindow<CapricornEditorWindow>();

        (var translate, var scale, var lastId) = capriconWindow.GetGraphViewData();

        JsonStatistics stats = JsonStatisticsCalculator.CalculateStatistics(root);

        // CapricornEditorWindow의 GraphView 위치 및 크기
        var graphWindow = capriconWindow.position;
        var graphWindowWidth = graphWindow.width;
        var graphWindowHeight = graphWindow.height - 21; // 21은 상단 메뉴바 높이 제외

        var addId = lastId - stats.MinId + 1;
        var addX = (graphWindowWidth/3 - translate.x)/scale - stats.MinX;
        var addY = (graphWindowHeight/3 - translate.y)/scale - stats.MinY;

        // Debug.Log($"translate : {translate}, scale : {scale}, lastId : {lastId}, MinId : {stats.MinId}");
        // Debug.Log($"graphWindowWidth : {graphWindowWidth}, graphWindowHeight : {graphWindowHeight}");
        // Debug.Log($"addId : {addId}, addX : {addX}, addY : {addY}");

        // ToDo: (if needed) Adjust node positions 

        foreach (var node in root.nodes)
        {
            node.id += addId;
            node.x += addX;
            node.y += addY;
            
            node.actionData.action.connections = node.actionData.action.connections.ConvertAll(conn => conn + addId);

            // Debug.Log($"id : {node.id}, x : {node.x}, y : {node.y}");
        }

        return root;
    }

#region test

    private void Test()
    {
        // **cs 파일 리포트 생성**
        string rootFolder = Application.dataPath + "/3rd Party/AI-Panel/Editor";
        string outputFilePath = rootFolder + "/Report.md";
        Debug.Log($"Root Folder: {rootFolder} \nOutput File Path: {outputFilePath}");

        try
        {
            CsFileReportGenerator.GenerateCsFileReport(rootFolder, outputFilePath);
            Debug.LogWarning("Report successfully generated.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error generating report: {ex.Message}");
        }

        // **파일 선택 후 통계값 계산 및 CSV 변환**
        List<string> selectedFiles = FileSelectorWindow.LoadSelectedFiles();
        if (selectedFiles.Count == 0)
        {
            Debug.Log("No files selected.");
            return;
        }
        else
        {
            List<FileStatisticsCsv> fileInfoList = new List<FileStatisticsCsv>(); // **파일 정보 리스트 초기화**

            foreach (string file in selectedFiles)
            {
                try
                {
                    string filename = Path.GetFileNameWithoutExtension(file) + ".csv";
                    string json = File.ReadAllText(file);
                    string csv = JsonToCsvConverter.JsonToCsvConverterMethod(json);

                    // 통계값 계산
                    JsonStatistics stats = JsonStatisticsCalculator.CalculateStatistics(json);

                    // 파일 정보 구조체에 저장
                    fileInfoList.Add(new FileStatisticsCsv
                    {
                        Filename = filename,
                        Statistics = stats,
                        CsvData = csv
                    });

                    string test = $"Regex test response\n{stats.ToString()}\n\n{filename}\n```csv\n{csv}```";

                    AIResponse response = new AIResponse(test, true, "");
                    response.ParseCsvResponse();
                    chatMessageService.AddMessage("Regex test user message", response);
                    Repaint();
                        
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"파일 처리 중 오류 발생: {file}\n{ex}");
                }
            }
        }
    }

    /// <summary>
    /// 파일의 통계값과 CSV 데이터를 포함하는 구조체
    /// </summary>
    private struct FileStatisticsCsv
    {
        public string Filename { get; set; }
        public JsonStatistics Statistics { get; set; }
        public string CsvData { get; set; }
    }

#endregion
}
```

CustomUI/DirectingCommentaryWindow.cs
```csharp
using System;
using UnityEngine;
using UnityEditor;

public class DirectingCommentaryWindow : EditorWindow
{
    private string commentary = "";
    private Action<string> onSubmit;

    public static void ShowWindow(Action<string> onSubmitCallback)
    {
        var window = GetWindow<DirectingCommentaryWindow>("Enter Directing Commentary");
        window.onSubmit = onSubmitCallback;
        window.minSize = new Vector2(400, 200);
    }

    private void OnGUI()
    {
        GUILayout.Label("Enter your directing commentary below:", EditorStyles.boldLabel);
        commentary = EditorGUILayout.TextArea(commentary, GUILayout.ExpandHeight(true));

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Cancel", GUILayout.Width(100)))
        {
            Close();
        }

        if (GUILayout.Button("OK", GUILayout.Width(100)))
        {
            if (onSubmit != null)
            {
                onSubmit.Invoke(commentary);
            }
            Close();
        }

        GUILayout.EndHorizontal();
    }
}
```

CustomUI/FileSelectionWindow.cs
```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class FileSelectorWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private bool showAllFolders = true; // 모든 폴더를 표시할지 여부
    private string selectedFolderPath = "Assets"; // 기본 검색 경로 (Assets 폴더)
    private Dictionary<string, bool> folderExpandedStates = new Dictionary<string, bool>();
    private Dictionary<string, List<string>> filesInFolders = new Dictionary<string, List<string>>();
    private Dictionary<string, bool> fileSelectionStates = new Dictionary<string, bool>();
    private List<string> loadSelectedFiles = new List<string>();

    // 파일 확장자 필터 (JSON 파일)
    private string fileSearchPattern = "*.json";

    // 저장된 선택된 파일들을 로드하기 위한 키
    private const string SelectedFilesKey = "SelectedFiles";

    // 탐색에서 제외할 폴더 목록
    private List<string> excludedFolders = new List<string> { "Library", "obj", "bin", "Build", "ProjectSettings", "Gizmos", "StreamingAssets", "Resources", "Editor", "3rd Party" };

    // 선택된 폴더 경로를 저장하기 위한 EditorPrefs 키
    private const string SelectedFolderPathKey = "FileSelectorWindow_SelectedFolderPath";

    public static void ShowWindow()
    {
        GetWindow<FileSelectorWindow>("File Selector");
    }

    private void OnEnable()
    {
        // 저장된 폴더 경로 로드 (없으면 기본값 "Assets")
        selectedFolderPath = EditorPrefs.GetString(SelectedFolderPathKey, "Assets");

        PopulateFiles();
        loadSelectedFiles = LoadSelectedFiles();
        foreach (string file in loadSelectedFiles)
        {
            fileSelectionStates[file] = true;
        }
    }

    private void OnDisable()
    {
        // 창이 닫힐 때 현재 선택된 폴더 경로를 저장
        EditorPrefs.SetString(SelectedFolderPathKey, selectedFolderPath);
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        // 폴더 선택 버튼 및 현재 선택된 폴더 경로 표시
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Selected Folder:", GUILayout.Width(100));
        EditorGUILayout.TextField(selectedFolderPath);
        if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder to Search JSON Files", Application.dataPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // 선택된 폴더가 Assets 폴더 내에 있는지 확인
                if (path.StartsWith(Application.dataPath))
                {
                    // Assets 폴더의 상대 경로로 변환 (예: Assets/AINovel)
                    selectedFolderPath = "Assets" + path.Substring(Application.dataPath.Length).Replace("\\", "/");
                    Debug.Log($"Selected folder path set to: {selectedFolderPath}");

                    // 선택된 폴더 경로를 EditorPrefs에 저장
                    EditorPrefs.SetString(SelectedFolderPathKey, selectedFolderPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder within the Assets directory.", "OK");
                    Debug.LogWarning($"User attempted to select an invalid folder: {path}");
                }
                // 파일 목록 초기화 및 재검색
                ResetFileData();
                PopulateFiles();
            }
        }
        EditorGUILayout.EndHorizontal();

        // 모든 폴더를 표시할지 사용자에게 선택할 수 있는 토글 추가
        EditorGUILayout.BeginHorizontal();
        showAllFolders = EditorGUILayout.ToggleLeft("Show All Subfolders", showAllFolders);
        EditorGUILayout.EndHorizontal();

        // 스크롤 뷰 시작
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        try
        {
            bool anyFolderDisplayed = false;
            string absoluteFolderPath = Path.Combine(Application.dataPath, selectedFolderPath.Substring("Assets".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (Directory.Exists(absoluteFolderPath))
            {
                DisplayFoldersAndFilesRecursively(absoluteFolderPath, 0);
                // 모든 폴더에 JSON 파일이 존재하는지 확인
                anyFolderDisplayed = filesInFolders.Values.Any(fileList => fileList.Count > 0);
            }
            else
            {
                EditorGUILayout.HelpBox($"The selected folder path does not exist: {selectedFolderPath}", MessageType.Warning);
            }

            if (!anyFolderDisplayed)
            {
                EditorGUILayout.HelpBox("No JSON files found in the selected folder or all target folders are empty.", MessageType.Info);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to display files: {e.Message}");
            EditorGUILayout.HelpBox($"Error: {e.Message}", MessageType.Error);
        }

        EditorGUILayout.EndScrollView();

        // OK 버튼
        if (GUILayout.Button("OK", GUILayout.Height(30)))
        {
            SaveSelectedFiles();
            Close();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 파일 데이터 초기화
    /// </summary>
    private void ResetFileData()
    {
        folderExpandedStates.Clear();
        filesInFolders.Clear();
        fileSelectionStates.Clear();
        loadSelectedFiles.Clear();
    }

    /// <summary>
    /// 파일 목록을 채우는 메서드
    /// </summary>
    private void PopulateFiles()
    {
        string absoluteFolderPath = Path.Combine(Application.dataPath, selectedFolderPath.Substring("Assets".Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (Directory.Exists(absoluteFolderPath))
        {
            try
            {
                PopulateFilesRecursive(absoluteFolderPath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to populate folder: {absoluteFolderPath}. {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Directory does not exist: {absoluteFolderPath}");
        }
    }

    /// <summary>
    /// 재귀적으로 파일과 폴더를 탐색하여 데이터를 채우는 메서드
    /// </summary>
    /// <param name="currentPath">현재 탐색 중인 폴더 경로</param>
    /// <returns>현재 폴더 또는 하위 폴더에 JSON 파일이 있는지 여부</returns>
    private bool PopulateFilesRecursive(string currentPath)
    {
        bool hasJsonFiles = false;

        // 현재 폴더 내의 JSON 파일을 가져옵니다.
        string[] files = Directory.GetFiles(currentPath, fileSearchPattern);
        if (files.Length > 0)
        {
            hasJsonFiles = true;
            if (!filesInFolders.ContainsKey(currentPath))
            {
                filesInFolders[currentPath] = new List<string>();
            }

            foreach (string file in files)
            {
                if (!fileSelectionStates.ContainsKey(file))
                {
                    fileSelectionStates[file] = false;  // 기본 비선택 상태
                }
                filesInFolders[currentPath].Add(file);
            }
        }

        // 모든 하위 디렉토리를 가져옵니다.
        string[] directories = Directory.GetDirectories(currentPath);
        // "Packages" 폴더는 제외합니다.
        directories = directories.Where(d => Path.GetFileName(d) != "Packages").ToArray();

        foreach (string directory in directories)
        {
            string directoryName = Path.GetFileName(directory);
            // 배제할 폴더인지 확인
            if (excludedFolders.Contains(directoryName))
            {
                //Debug.Log($"Excluded folder skipped: {directory}");
                continue; // 배제 폴더는 건너뜁니다.
            }

            // 사용자가 모든 서브폴더를 표시하지 않도록 선택한 경우, 하위 폴더 탐색을 건너뜁니다.
            if (!showAllFolders && directory != currentPath)
            {
                continue;
            }

            // 재귀적으로 탐색
            bool subFolderHasJson = PopulateFilesRecursive(directory);
            if (subFolderHasJson)
            {
                hasJsonFiles = true;
                if (!filesInFolders.ContainsKey(directory))
                {
                    filesInFolders[directory] = new List<string>(); // 빈 폴더도 추가
                }
            }
        }

        // 현재 폴더에 JSON 파일이 있거나, 하위 폴더 중 하나라도 JSON 파일이 있는 경우
        if (hasJsonFiles)
        {
            if (!folderExpandedStates.ContainsKey(currentPath))
            {
                folderExpandedStates[currentPath] = true;  // 기본 열림 상태
            }
        }

        return hasJsonFiles;
    }

    /// <summary>
    /// 폴더와 파일을 재귀적으로 표시하는 메서드
    /// </summary>
    /// <param name="currentPath">현재 폴더 경로</param>
    /// <param name="indentLevel">들여쓰기 레벨</param>
    private void DisplayFoldersAndFilesRecursively(string currentPath, int indentLevel)
    {
        // 현재 폴더의 하위 디렉토리를 가져옵니다.
        string[] directories = Directory.GetDirectories(currentPath);

        foreach (string directory in directories)
        {
            // 폴더가 JSON 파일을 포함하거나 하위 폴더 중 하나라도 JSON 파일을 포함하는 경우에만 표시
            if (!filesInFolders.ContainsKey(directory))
            {
                continue; // JSON 파일이 없는 폴더는 건너뜁니다.
            }

            string directoryName = Path.GetFileName(directory);
            GUILayout.BeginHorizontal();
            GUILayout.Space(indentLevel * 15 + 10); // 들여쓰기
            bool isExpanded = EditorGUILayout.Foldout(GetFolderExpandedState(directory), directoryName, true);
            bool allSelected = CheckAllFilesSelected(directory);
            bool toggleValue = EditorGUILayout.ToggleLeft("Select All", allSelected, GUILayout.Width(80));
            if (toggleValue != allSelected)
            {
                SetAllFilesSelected(directory, toggleValue);
            }
            GUILayout.EndHorizontal();

            SetFolderExpandedState(directory, isExpanded);

            if (isExpanded)
            {
                // 재귀적으로 서브폴더 표시
                DisplayFoldersAndFilesRecursively(directory, indentLevel + 1);
            }
        }

        // 현재 폴더 내의 파일을 가져옵니다.
        if (filesInFolders.ContainsKey(currentPath))
        {
            string[] files = filesInFolders[currentPath].ToArray();
            foreach (string file in files)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(indentLevel * 15 + 25); // 들여쓰기
                bool isSelected = EditorGUILayout.ToggleLeft(Path.GetFileName(file), GetFileSelectionState(file));
                SetFileSelectionState(file, isSelected);
                GUILayout.EndHorizontal();
            }
        }
    }

    /// <summary>
    /// 폴더의 확장 상태를 가져오는 메서드
    /// </summary>
    /// <param name="path">폴더 경로</param>
    /// <returns>확장 상태</returns>
    private bool GetFolderExpandedState(string path)
    {
        if (!folderExpandedStates.ContainsKey(path))
        {
            folderExpandedStates[path] = true;
        }
        return folderExpandedStates[path];
    }

    /// <summary>
    /// 폴더의 확장 상태를 설정하는 메서드
    /// </summary>
    /// <param name="path">폴더 경로</param>
    /// <param name="isExpanded">확장 여부</param>
    private void SetFolderExpandedState(string path, bool isExpanded)
    {
        if (folderExpandedStates.ContainsKey(path))
        {
            folderExpandedStates[path] = isExpanded;
        }
        else
        {
            folderExpandedStates.Add(path, isExpanded);
        }
    }

    /// <summary>
    /// 파일의 선택 상태를 가져오는 메서드
    /// </summary>
    /// <param name="file">파일 경로</param>
    /// <returns>선택 상태</returns>
    private bool GetFileSelectionState(string file)
    {
        if (!fileSelectionStates.ContainsKey(file))
        {
            fileSelectionStates[file] = false;
        }
        return fileSelectionStates[file];
    }

    /// <summary>
    /// 파일의 선택 상태를 설정하는 메서드
    /// </summary>
    /// <param name="file">파일 경로</param>
    /// <param name="isSelected">선택 여부</param>
    private void SetFileSelectionState(string file, bool isSelected)
    {
        if (fileSelectionStates.ContainsKey(file))
        {
            fileSelectionStates[file] = isSelected;
        }
        else
        {
            fileSelectionStates.Add(file, isSelected);
        }
    }

    /// <summary>
    /// 모든 파일이 선택되었는지 확인하는 메서드
    /// </summary>
    /// <param name="currentPath">현재 폴더 경로</param>
    /// <returns>모든 파일이 선택되었는지 여부</returns>
    private bool CheckAllFilesSelected(string currentPath)
    {
        // 현재 폴더의 파일 검사
        if (filesInFolders.ContainsKey(currentPath))
        {
            foreach (var file in filesInFolders[currentPath])
            {
                if (!fileSelectionStates[file]) return false;
            }
        }

        // 하위 폴더의 파일 검사
        string[] directories = Directory.GetDirectories(currentPath);
        foreach (string directory in directories)
        {
            if (filesInFolders.ContainsKey(directory))
            {
                if (!CheckAllFilesSelected(directory)) return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 모든 파일의 선택 상태를 설정하는 메서드
    /// </summary>
    /// <param name="folder">폴더 경로</param>
    /// <param name="selected">선택 상태</param>
    private void SetAllFilesSelected(string folder, bool selected)
    {
        // 현재 폴더의 파일 선택 상태 설정
        if (filesInFolders.ContainsKey(folder))
        {
            foreach (var file in filesInFolders[folder])
            {
                fileSelectionStates[file] = selected;
            }
        }

        // 하위 폴더의 파일 선택 상태 설정
        string[] directories = Directory.GetDirectories(folder);
        foreach (string directory in directories)
        {
            if (filesInFolders.ContainsKey(directory))
            {
                SetAllFilesSelected(directory, selected);
            }
        }
    }

    /// <summary>
    /// 선택된 파일들을 저장하는 메서드
    /// </summary>
    private void SaveSelectedFiles()
    {
        // 선택된 모든 파일 경로를 추출
        List<string> selectedFiles = fileSelectionStates.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();

        // SerializableFileList 객체로 변환하여 JSON으로 직렬화
        SerializableFileList fileList = new SerializableFileList(selectedFiles);
        string json = JsonUtility.ToJson(fileList);

        // SecurePlayerPrefs에 저장 (SecurePlayerPrefs는 사용자 정의 클래스일 수 있습니다. 실제 사용 시 해당 클래스의 구현을 확인하세요.)
        SecurePlayerPrefs.SetString(SelectedFilesKey, json);
        SecurePlayerPrefs.Save();
    }

    /// <summary>
    /// 저장된 선택된 파일들을 로드하는 메서드
    /// </summary>
    /// <returns>선택된 파일 목록</returns>
    public static List<string> LoadSelectedFiles()
    {
        string json = SecurePlayerPrefs.GetString(SelectedFilesKey, "{}");
        SerializableFileList fileList = JsonUtility.FromJson<SerializableFileList>(json);

        if (fileList == null || fileList.files == null)
        {
            return new List<string>();
        }

        // 존재하지 않는 파일은 목록에서 제거
        fileList.files.RemoveAll(file => !File.Exists(file));

        return fileList.files;
    }

    /// <summary>
    /// 선택된 파일들을 직렬화하기 위한 클래스
    /// </summary>
    [System.Serializable]
    public class SerializableFileList
    {
        public List<string> files;

        public SerializableFileList(List<string> files)
        {
            this.files = files;
        }
    }
}
```

Utils/CsvToJsonConverter.cs
```csharp
using System;
using System.Collections.Generic;
using System.Text;
using Dunward.Capricorn;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;

public class CsvToJsonConverter
{
    /// <summary>
    /// CSV 문자열을 JSON 형식으로 변환하는 메서드
    /// </summary>
    /// <param name="csv">입력 CSV 문자열</param>
    /// <returns>변환된 JSON 문자열</returns> 

    // JSON 직렬화 설정
    static JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto, //필수옵션
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.Indented
    };
    
    public static string CsvToJsonConverterMethod(string csv)
    {
        // RootObject 초기화 (position, zoomFactor, debugNodeIndex는 예시 값으로 설정)
        var root = new GraphData
        {
            position = new UnityEngine.Vector2(62.0f,438.0f), // 예시 값
            zoomFactor = 0.657516241f, // 예시 값
            debugNodeIndex = -1, // 예시 값
        };

        // CSV를 줄 단위로 분리
        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            throw new ArgumentException("CSV 데이터가 충분하지 않습니다.");
        }

        // 헤더 파싱
        var headers = ParseCsvLine(lines[0]);
        var headerMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Count; i++)
        {
            headerMap[headers[i].Trim()] = i;
        }

        // 데이터 줄 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i]);

            // 필드 패딩: 필드 수가 헤더 수보다 적으면 빈 문자열로 채움
            while (fields.Count < headers.Count)
            {
                fields.Add(string.Empty);
            }

            // 각 필드 추출 (안전한 접근을 위해 인덱스 확인)
            string[] requiredHeaders = { "id", "title", "x", "y", "actionType", "SelectionCount", "connections", "name", "subName" };
            bool missingHeader = false;
            foreach (var header in requiredHeaders)
            {
                if (!headerMap.ContainsKey(header))
                {
                    missingHeader = true;
                    break;
                }
            }
            if (missingHeader)
            {
                throw new ArgumentException("CSV 헤더가 누락되었습니다.");
            }

            // 필드 값 추출 및 정제
            if (!int.TryParse(fields[headerMap["id"]].Trim(), out int id))
            {
                throw new ArgumentException($"Invalid id value at line {i + 1}");
            }

            string title = fields[headerMap["title"]].Trim('"').Trim();

            if (!float.TryParse(fields[headerMap["x"]].Trim(), out float x))
            {
                throw new ArgumentException($"Invalid x value at line {i + 1}");
            }

            if (!float.TryParse(fields[headerMap["y"]].Trim(), out float y))
            {
                throw new ArgumentException($"Invalid y value at line {i + 1}");
            }

            if (!int.TryParse(fields[headerMap["actionType"]].Trim(), out int actionType))
            {
                throw new ArgumentException($"Invalid actionType value at line {i + 1}");
            }

            if (!int.TryParse(fields[headerMap["SelectionCount"]].Trim(), out int selectionCount))
            {
                throw new ArgumentException($"Invalid SelectionCount value at line {i + 1}");
            }

            string connectionsStr = fields[headerMap["connections"]].Trim('"').Trim();
            string name = fields[headerMap["name"]].Trim('"').Trim();
            string subName = fields[headerMap["subName"]].Trim('"').Trim();

            // script_1 ~ script_4 필드 추출
            List<string> scripts = new List<string>();
            for (int j = 1; j <= 4; j++)
            {
                string scriptField = $"script_{j}";
                if (headerMap.ContainsKey(scriptField) && !string.IsNullOrWhiteSpace(fields[headerMap[scriptField]]))
                {
                    scripts.Add(fields[headerMap[scriptField]].Trim('"').Trim());
                }
            }

            object scriptContent = null;
            if (actionType == 1)
            {
                // actionType=1인 경우 script는 script_1의 내용
                scriptContent = scripts.Count > 0 ? scripts[0] : string.Empty;
            }
            else if (actionType == 2)
            {
                // actionType=2인 경우 script는 script_1 ~ script_4의 리스트
                scriptContent = scripts;
            }
            else
            {
                // 다른 actionType이 추가될 경우 대비
                scriptContent = scripts.Count > 0 ? scripts[0] : string.Empty;
            }

            // connections 파싱 (콤마로 분리하여 정수 리스트로 변환)
            List<int> connections = new List<int>();
            if (!string.IsNullOrWhiteSpace(connectionsStr))
            {
                var connStrings = connectionsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var conn in connStrings)
                {
                    if (int.TryParse(conn.Trim(), out int connId))
                    {
                        connections.Add(connId);
                    }
                }
            }

            // nodeType 추론
            NodeType nodeType = NodeType.Connector; // 기본값 (중간 노드)
            if (id == -1)
            {
                nodeType = NodeType.Input; // 시작 노드
            }
            else if (connections.Count == 0)
            {
                nodeType = NodeType.Output; // 종료 노드
            }

            // $type 설정 based on actionType

            var node = new NodeMainData
            {
                id = id,
                title = string.IsNullOrWhiteSpace(title) ? null : title,
                x = x,
                y = y,
                nodeType = nodeType,
                actionData = new NodeActionData()
            };

            if (actionType == 1)
            {
                var action = new TextTypingUnit(){
                    SelectionCount = selectionCount,
                    connections = connections,

                    name = name,
                    subName = subName,
                    script = (string)scriptContent
                };

                node.actionData.action = action;
            }
            else if (actionType == 2)
            {
                var action = new SelectionUnit(){
                    SelectionCount = selectionCount,
                    connections = connections,
                    scripts = (List<string>)scriptContent
                };

                node.actionData.action = action;
            }
            else
            {
                // 기본값 또는 추가 actionType에 따른 처리
                var action = new TextTypingUnit();
                node.actionData.action = action;
            }
            
            // Debug.Log($"{id}, {title}, {x}, {y}, {actionType}, {selectionCount}, {connections}, {name}, {subName}, {scriptContent}");

            // nodes 리스트에 추가
            root.nodes.Add(node);
        }

        // JSON 문자열 생성
        string json = JsonConvert.SerializeObject(root, jsonSettings);
        return json;
    }

    /// <summary>
    /// CSV 한 줄을 필드 리스트로 파싱하는 헬퍼 메서드
    /// </summary>
    /// <param name="line">CSV 한 줄</param>
    /// <returns>필드 리스트</returns>
    public static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        StringBuilder fieldBuilder = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') // 이스케이프된 따옴표
                {
                    fieldBuilder.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(fieldBuilder.ToString());
                fieldBuilder.Clear();
            }
            else
            {
                fieldBuilder.Append(c);
            }
        }

        // 마지막 필드 추가
        fields.Add(fieldBuilder.ToString());

        return fields;
    }
}
```

Utils/JsonStatisticsCalculator.cs
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dunward.Capricorn;
using Newtonsoft.Json;
using UnityEngine;

public struct JsonStatistics
{
    public int MaxId { get; set; }
    public int MinId { get; set; }
    public float MaxX { get; set; }
    public float MinX { get; set; }
    public float MaxY { get; set; }
    public float MinY { get; set; }

    public override string ToString()
    {
        return $"Max ID: {MaxId},Min Id: {MinId}, Max X: {MaxX}, Min X: {MinX}, Max Y: {MaxY}, Min Y: {MinY}";
    }
}

public class JsonStatisticsCalculator
{
    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    public static JsonStatistics CalculateStatistics(string json)
    {
        // JSON 디시리얼라이즈
        GraphData root;
        try
        {
            root = JsonConvert.DeserializeObject<GraphData>(json, settings);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("유효하지 않은 JSON 형식입니다.", ex);
        }

        if (root == null || root.nodes == null)
        {
            throw new ArgumentException("JSON 데이터에 노드 정보가 없습니다.");
        }

        // 초기 통계값 설정
        JsonStatistics stats = new JsonStatistics
        {
            MaxId = root.nodes.Max(node => node.id),
            MinId = root.nodes.Min(node => node.id),
            MaxX = root.nodes.Max(node => node.x),
            MinX = root.nodes.Min(node => node.x),
            MaxY = root.nodes.Max(node => node.y),
            MinY = root.nodes.Min(node => node.y)
        };

        return stats;
    }

    public static JsonStatistics CalculateStatistics(GraphData root)
    {
        if (root == null || root.nodes == null)
        {
            throw new ArgumentException("JSON 데이터에 노드 정보가 없습니다.");
        }

        JsonStatistics stats = new JsonStatistics
        {
            MaxId = root.nodes.Max(node => node.id),
            MinId = root.nodes.Min(node => node.id),
            MaxX = root.nodes.Max(node => node.x),
            MinX = root.nodes.Min(node => node.x),
            MaxY = root.nodes.Max(node => node.y),
            MinY = root.nodes.Min(node => node.y)
        };

        return stats;
    }
}
```

Utils/JsonToCsvConverter.cs
```csharp
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using Dunward.Capricorn;
using Unity.VisualScripting;
using TMPro;
using UnityEngine.UI;

public class JsonToCsvConverter
{
    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    public static string JsonToCsvConverterMethod(string json)
    {
        // Deserialize JSON to RootObject
        GraphData root;
        try
        {
            root = JsonConvert.DeserializeObject<GraphData>(json, settings);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("유효하지 않은 JSON 형식입니다.", ex);
        }

        if (root == null || root.nodes == null)
        {
            throw new ArgumentException("JSON 데이터에 노드 정보가 없습니다.");
        }

        // Define CSV headers
        string[] headers = new string[]
        {
            "id", "title", "x", "y", "actionType", "SelectionCount", "connections", "name", "subName",
            "script_1", "script_2", "script_3", "script_4"
        };

        StringBuilder csvBuilder = new StringBuilder();

        // Write headers
        csvBuilder.AppendLine(string.Join(",", headers));

        // Process each node
        foreach (var node in root.nodes)
        {

            int id = node.id;
            string title = node.title;
            float x = node.x;
            float y = node.y;

            int actionType = 1;
            int selectionCount = 0;
            string connections = "-999";
            string name = "";
            string subName = "";

            // script fields
            string script1 = "";
            string script2 = "";
            string script3 = "";
            string script4 = "";

            // Determine actionType based on TypeValue
            string type = node.actionData?.action?.GetType().ToString();

            if (type == typeof(TextTypingUnit).ToString())
            {
                TextTypingUnit textTypingUnit = (TextTypingUnit)node.actionData.action;

                actionType = 1; 
                selectionCount = 1;
                name = textTypingUnit.name;
                subName = textTypingUnit.subName;
                script1 = textTypingUnit.script;
                connections = string.Join(",", textTypingUnit.connections);
            }
            else if (type == typeof(SelectionUnit).ToString())
            {
                SelectionUnit selectionUnit = (SelectionUnit)node.actionData.action;

                actionType = 2;
                selectionCount = selectionUnit.SelectionCount;
                connections = string.Join(",", selectionUnit.connections);

                var scripts = selectionUnit.scripts;
                if (scripts != null)
                {
                    for (int i = 0; i < Math.Min(scripts.Count, 4); i++)
                    {
                        switch (i)
                        {
                            case 0:
                                script1 = scripts[i];
                                break;
                            case 1:
                                script2 = scripts[i];
                                break;
                            case 2:
                                script3 = scripts[i];
                                break;
                            case 3:
                                script4 = scripts[i];
                                break;
                        }
                    }
                }
            }
            // Create an array of CSV fields in order
            string[] csvFields = new string[]
            {
                id.ToString(),
                AddQuotes(title),
                x.ToString(),
                y.ToString(),
                actionType.ToString(),
                selectionCount.ToString(),
                AddQuotes(connections),
                AddQuotes(name),
                AddQuotes(subName),
                AddQuotes(script1),
                AddQuotes(script2),
                AddQuotes(script3),
                AddQuotes(script4)
            };

            // Join fields with comma separator
            string csvLine = string.Join(",", csvFields);
            csvBuilder.AppendLine(csvLine);
        }

        return csvBuilder.ToString();
    }

    private static string AddQuotes(string field)
    {
        if (!string.IsNullOrEmpty(field))
            return $"\"{field}\"";

        return field;
    }
}
```

Utils/SecurePlayerPrefs.cs
```csharp
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
 

public static class SecurePlayerPrefs
{
    // Set false if you don't want to use encrypt/decrypt value
    // You could use #if UNITY_EDITOR for check your value
    public static bool useSecure = true;

    const int Iterations = 555;

    // You should Change following password and IV value using Initialize
    static string strPassword = "tp9ZCtjhEBjb4YpeDRk4";
    static string strSalt = "1513DLMELKJFsdsefwv";
    static bool hasSetPassword = true;

    public static void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
    }

    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

    public static float GetFloat(string key)
    {
        return GetFloat(key, 0.0f);
    }

    public static float GetFloat(string key, float defaultValue, bool isDecrypt = true)
    {
        float retValue = defaultValue;

        string strValue = GetString(key);

        if (float.TryParse(strValue, out retValue))
        {
            return retValue;
        }
        else
        {
            return defaultValue;
        }
    }

    public static int GetInt(string key)
    {
        return GetInt(key, 0);
    }

    public static int GetInt(string key, int defaultValue, bool isDecrypt = true)
    {
        int retValue = defaultValue;

        string strValue = GetString(key);

        if (int.TryParse(strValue, out retValue))
        {
            return retValue;
        }
        else
        {
            return defaultValue;
        }
    }

    
    public static string GetString(string key)
    {
        string strEncryptValue = GetRowString(key);

        return Decrypt(strEncryptValue, strPassword);
    }

    public static string GetRowString(string key)
    {
        CheckPasswordSet();

        string strEncryptKey = Encrypt(key, strPassword);
        string strEncryptValue = PlayerPrefs.GetString(strEncryptKey);

        return strEncryptValue;
    }

    public static string GetString(string key, string defaultValue)
    {
        string strEncryptValue = GetRowString(key, defaultValue);
        return Decrypt(strEncryptValue, strPassword);
    }

    public static string GetRowString(string key, string defaultValue)
    {
        CheckPasswordSet();

        string strEncryptKey = Encrypt(key, strPassword);
        string strEncryptDefaultValue = Encrypt(defaultValue, strPassword);

        string strEncryptValue = PlayerPrefs.GetString(strEncryptKey, strEncryptDefaultValue);

        return strEncryptValue;
    }

    public static bool HasKey(string key)
    {
        CheckPasswordSet();
        return PlayerPrefs.HasKey(Encrypt(key, strPassword));
    }
    
    public static void Save()
    {
        CheckPasswordSet();
        PlayerPrefs.Save();
    }
    
    public static void SetFloat(string key, float value)
    {
        string strValue = System.Convert.ToString(value);
        SetString(key, strValue);
    }
    
    public static void SetInt(string key, int value)
    {
        string strValue = System.Convert.ToString(value);
        SetString(key, strValue);
    }
   
    public static void SetString(string key, string value)
    {
        CheckPasswordSet();
        PlayerPrefs.SetString(Encrypt(key, strPassword), Encrypt(value, strPassword));
    }

    /////////////////////////////////////////////////////////////////
    // Help Function
    /////////////////////////////////////////////////////////////////
    public static void Initialize(string newPassword, string newSalt)
    {
        strPassword = newPassword;
        strSalt = newSalt;

        hasSetPassword = true;
    }

    
    static void CheckPasswordSet()
    {
        if (!hasSetPassword)
        {
            Debug.LogWarning("Set Your Own Password & Salt!!!");
        }
    }

    static byte[] GetIV()
    {
        byte[] IV = Encoding.UTF8.GetBytes(strSalt);
        return IV;
    }

    static string Encrypt(string strPlain, string password)
    {
        if (!useSecure)
            return strPlain;

        try
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, GetIV(), Iterations);

            byte[] key = rfc2898DeriveBytes.GetBytes(8);

            using (var memoryStream = new MemoryStream())
            using (var cryptoStream = new CryptoStream(memoryStream, des.CreateEncryptor(key, GetIV()), CryptoStreamMode.Write))
            {
                memoryStream.Write(GetIV(), 0, GetIV().Length);

                byte[] plainTextBytes = Encoding.UTF8.GetBytes(strPlain);

                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                cryptoStream.FlushFinalBlock();

                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Encrypt Exception: " + e);
            return strPlain;
        }
    }

    static string Decrypt(string strEncript, string password)
    {
        if (!useSecure)
            return strEncript;

        try
        {
            byte[] cipherBytes = Convert.FromBase64String(strEncript);

            using (var memoryStream = new MemoryStream(cipherBytes))
            {
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();

                byte[] iv = GetIV();
                memoryStream.Read(iv, 0, iv.Length);

                // use derive bytes to generate key from password and IV
                var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, iv, Iterations);

                byte[] key = rfc2898DeriveBytes.GetBytes(8);

                using (var cryptoStream = new CryptoStream(memoryStream, des.CreateDecryptor(key, iv), CryptoStreamMode.Read))
                using (var streamReader = new StreamReader(cryptoStream))
                {
                    string strPlain = streamReader.ReadToEnd();
                    return strPlain;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Decrypt Exception: " + e);
            return strEncript;
        }

    }

}



```

Utils/UnityWebRequestExtensions.cs
```csharp
using System.Threading.Tasks;
using UnityEngine.Networking;

public static class UnityWebRequestExtensions
{
    public static async Task<UnityWebRequest> SendWebRequestAsync(this UnityWebRequest request)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();

        request.SendWebRequest().completed += (asyncOp) =>
        {
            tcs.SetResult(request);
        };

        return await tcs.Task;
    }
}
```

AI/Interfaces/IAIModel.cs
```csharp
using System;
using System.Threading.Tasks;
public interface IAIModel
{
    AIResponse GenerateResponse(string input, Action<AIResponse> callback);
    AIResponse GenerateDirectingResponse(string input, Action<AIResponse> callback);
    Task<AIResponse> GenerateResponseAsync(string input);
    Task<AIResponse> GenerateDirectingResponseAsync(string input);
}
```

AI/Managers/AIManager.cs
```csharp
using System;
using System.Threading.Tasks;
using UnityEngine;

public class AIManager
{
    private IAIModel currentAIModel;

    public AIManager(IAIModel aiModel)
    {
        currentAIModel = aiModel ?? throw new ArgumentNullException(nameof(aiModel));
    }

    public AIManager() : this(new ChatGPTAssistant())
    {
    }

    public void SetAIModel(IAIModel aiModel)
    {
        currentAIModel = aiModel ?? throw new ArgumentNullException(nameof(aiModel));
    }

    public async Task<AIResponse> GetResponseAsync(string input)
    {
        if (currentAIModel == null)
        {
            Debug.LogError("AIManager: AI 모델이 설정되지 않았습니다.");
            return new AIResponse("AI 모델이 초기화되지 않았습니다.", false, null);
        }
        return await currentAIModel.GenerateResponseAsync(input);
    }

    public async Task<AIResponse> GetDirectingResponseAsync(string input)
    {
        if (currentAIModel == null)
        {
            Debug.LogError("AIManager: AI 모델이 설정되지 않았습니다.");
            return new AIResponse("AI 모델이 초기화되지 않았습니다.", false, null);
        }
        return await currentAIModel.GenerateDirectingResponseAsync(input);
    }
    
}


```

AI/Managers/PromptManager.cs
```csharp
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System;
using UnityEngine.UIElements;
using Dunward.Capricorn;
using System.Collections.ObjectModel;
using UnityEditor.U2D.Animation;

public class PromptManager
{
    private static List<string> selectedFiles = new List<string>();

    public static string AddFilesToUserPrompt(string userInput)
    {
        selectedFiles = FileSelectorWindow.LoadSelectedFiles();

        if (selectedFiles.Count == 0)
        {
            return userInput;
        }

        List<FileStatisticsCsv> fileInfoList = new();

        foreach (string file in selectedFiles)
        {
            try
            {
                string filename = Path.GetFileNameWithoutExtension(file) + ".csv";
                string json = File.ReadAllText(file);
                string csv = JsonToCsvConverter.JsonToCsvConverterMethod(json);
                //Debug.Log($"File: {file}, Filename: {filename}");

                JsonStatistics stats = JsonStatisticsCalculator.CalculateStatistics(json);

                fileInfoList.Add(new FileStatisticsCsv
                {
                    Filename = filename,
                    Statistics = stats,
                    CsvData = csv
                
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while processing file: {file}. Error: {ex.Message}");
                continue;
            }
        }

        string scriptPrompt = ScriptPromptFormat(fileInfoList);

        return userInput + scriptPrompt;
    }

    public static string ScriptPromptFormat(List<FileStatisticsCsv> fileInfoList)
    {
        StringBuilder scriptPromptBuilder = new StringBuilder();

        scriptPromptBuilder.AppendLine("\n\n**Selected Files Node Info and CSV Data:**");

        foreach (var fileInfo in fileInfoList)
        {
            scriptPromptBuilder.AppendLine($"\n**File:** {fileInfo.Filename}");

            // 통계값 추가
            scriptPromptBuilder.AppendLine("**Statistics:**");
            scriptPromptBuilder.AppendLine($"- Max node id: {fileInfo.Statistics.MaxId}");
            scriptPromptBuilder.AppendLine($"- Max x: {fileInfo.Statistics.MaxX}");
            scriptPromptBuilder.AppendLine($"- Min x: {fileInfo.Statistics.MinX}");
            scriptPromptBuilder.AppendLine($"- Max y: {fileInfo.Statistics.MaxY}");
            scriptPromptBuilder.AppendLine($"- Min y: {fileInfo.Statistics.MinY}");

            // CSV 데이터 추가
            scriptPromptBuilder.AppendLine("\n**CSV nodes Data:**");
            scriptPromptBuilder.AppendLine($"{fileInfo.Filename}");
            scriptPromptBuilder.AppendLine("```csv");
            scriptPromptBuilder.AppendLine(fileInfo.CsvData);
            scriptPromptBuilder.AppendLine("```");
        }

        return scriptPromptBuilder.ToString();
    }

    public static string DirectingPrompt(string csvContents, string commentary)
    {
        List<string> backgrounds = Resources.Load<BackgroundDatabase>("BackgroundDatabase").backgrounds.Keys.ToList();
        List<string> characters = Resources.Load<CharacterDatabase>("CharacterDatabase").characters.Keys.ToList();
        List<string> bgms = Resources.Load<AudioDatabase>("AudioDatabase").bgms.Keys.ToList();
        List<string> sfxs = Resources.Load<AudioDatabase>("AudioDatabase").sfxs.Keys.ToList();

        StringBuilder directingPromptBuilder = new StringBuilder();
        directingPromptBuilder.AppendLine("!!Note : Use only the following resources in your script!!");
        directingPromptBuilder.AppendLine("\n\n**Available Resources:**");
        directingPromptBuilder.AppendLine($"\n\nBackgrounds: {String.Join(", ", backgrounds)}");
        directingPromptBuilder.AppendLine($"\n\nCharacters: {String.Join(", ", characters)}");
        directingPromptBuilder.AppendLine($"\n\nBGMs: {String.Join(", ", bgms)}");
        directingPromptBuilder.AppendLine($"\n\nSFXs: {String.Join(", ", sfxs)}");

        if (!string.IsNullOrEmpty(commentary))
        {
            directingPromptBuilder.AppendLine("\n\n**Directing Commentary From User:**");
            directingPromptBuilder.AppendLine(commentary);
        }


        directingPromptBuilder.AppendLine("\n\n**CSV Contents:**");
        directingPromptBuilder.AppendLine("```csv");
        directingPromptBuilder.AppendLine(csvContents);
        directingPromptBuilder.AppendLine("```");

        
        // Debug.Log(String.Join(',',backgrounds));
        // Debug.Log(String.Join(',',characters));
        // Debug.Log(String.Join(',',bgms));
        // Debug.Log(String.Join(',',sfxs));

        return directingPromptBuilder.ToString();

    }
    public struct FileStatisticsCsv
    {
        public string Filename { get; set; }
        public JsonStatistics Statistics { get; set; }
        public string CsvData { get; set; }
    }
}
```

AI/Models/AIResponse.cs
```csharp
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class AIResponse
{
    // 응답 내용을 저장하는 속성
    public string Content { get; set; }

    // 추가적인 메타데이터나 상태 정보를 저장할 수 있는 속성
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public List<CodeFile> Codes { get; private set; }
    public string Message { get; private set; }

    // 생성자를 통해 기본 값을 설정할 수 있습니다.
    public AIResponse(string content, bool isSuccess = true, string errorMessage = "")
    {
        this.Content = content;
        this.IsSuccess = isSuccess;
        this.ErrorMessage = errorMessage;
    }

    public void ParseCsvResponse()
    {
        Debug.Log("Parsing csv response...");
        string pattern = @"(?:(\S+\.csv[^\n]*)?\n)?```csv\n(.+?)```";
        string extension = ".csv";
        (Message, Codes) = ExtractTextAndCodes(this.Content, pattern, extension);
    }

    public void ParseJsonResponse()
    {
        Debug.Log("Parsing json response...");
        string pattern = @"(?:(\S+\.json[^\n]*)?\n)?```json\n(.+?)```";
        string extension = ".json";
        (Message, Codes) = ExtractTextAndCodes(this.Content, pattern, extension);
    }

    private (string, List<CodeFile>) ExtractTextAndCodes(string inputText, string pattern, string extension)
    {
        // 수정된 정규식: 파일명이 없을 경우를 처리

        RegexOptions options = RegexOptions.Singleline;

        // 추출된 코드를 저장할 리스트
        List<CodeFile> codeBlocks = new();
        // 남은 텍스트를 저장할 리스트
        List<string> remainingTexts = new();

        int lastPosition = 0;
        int codeBlockCount = 0;

        foreach (Match match in Regex.Matches(inputText, pattern, options))
        {
            if (match.Success)
            {
                // 파일명이 존재할 경우 사용, 없을 경우 'temp.csv' 할당
                string filename = match.Groups[1].Success ? match.Groups[1].Value : "noname"+extension;
                string extractedCode = match.Groups[2].Value;
                
                codeBlocks.Add(new CodeFile(filename, extractedCode));
                codeBlockCount++;

                // 코드 블록 이전의 텍스트를 남은 텍스트에 추가
                string textBeforeCode = inputText.Substring(lastPosition, match.Index - lastPosition);
                remainingTexts.Add(textBeforeCode);
                remainingTexts.Add($"{filename}");
                remainingTexts.Add($"\n[Scene {codeBlockCount}]");

                // 마지막 매칭 위치 업데이트
                lastPosition = match.Index + match.Length;
            }
        }

        // 마지막 코드 블록 이후의 텍스트 추가
        if (lastPosition < inputText.Length)
        {
            string remainingText = inputText.Substring(lastPosition);
            remainingTexts.Add(remainingText);
        }

        return (string.Join("", remainingTexts), codeBlocks);
    }
}

[System.Serializable]
public class CodeFile
{
    public string filename;
    public string code;

    public CodeFile(string filename, string code)
    {
        this.filename = filename;
        this.code = code;
    }
}
```

AI/Services/ChatMessageService.cs
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ChatMessageService
{
    private readonly string chatFilePath;
    private List<ChatMessage> chatMessages;

    public ChatMessageService()
    {
        chatFilePath = Path.Combine(Application.persistentDataPath, "ChatMessages.json");
        LoadChatMessages();
    }

    /// <summary>
    /// 채팅 메시지를 로드합니다.
    /// </summary>
    private void LoadChatMessages()
    {
        if (File.Exists(chatFilePath))
        {
            string json = File.ReadAllText(chatFilePath);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    chatMessages = JsonUtility.FromJson<ListContainer>(json)?.Messages ?? new List<ChatMessage>();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"채팅 메시지 로드 실패: {ex.Message}");
                    chatMessages = new List<ChatMessage>();
                }
            }
            else
            {
                chatMessages = new List<ChatMessage>();
            }
        }
        else
        {
            chatMessages = new List<ChatMessage>();
        }
    }

    /// <summary>
    /// 채팅 메시지를 저장합니다.
    /// </summary>
    public void SaveChatMessages()
    {
        if (chatMessages.Count == 0)
        {
            File.WriteAllText(chatFilePath, "");
            return;
        }

        string json = JsonUtility.ToJson(new ListContainer(chatMessages));
        File.WriteAllText(chatFilePath, json);
    }

    /// <summary>
    /// 새로운 메시지를 추가합니다.
    /// </summary>
    public void AddMessage(string message, AIResponse response)
    {
        chatMessages.Add(new ChatMessage(message, response.Message, response.Codes));
    }

    /// <summary>
    /// 사용자 메시지를 추가합니다.
    /// </summary>
    public void AddUserMessage(string message)
    {
        chatMessages.Add(new ChatMessage(message, null, null));
    }

    /// <summary>
    /// AI 응답 메시지를 추가합니다.
    /// </summary>
    public void AddAIResponse(AIResponse response)
    {
        var lastUserMessage = chatMessages.FindLast(m => m.message == null);
        if (lastUserMessage != null)
        {
            lastUserMessage.message = response.Message;
            if (response.Codes != null && response.Codes.Count > 0)
            {
                lastUserMessage.codeFiles = new CodeFileListWrapper(response.Codes);
            }
        }
    }

    /// <summary>
    /// 모든 채팅 메시지를 반환합니다.
    /// </summary>
    public List<ChatMessage> GetAllMessages()
    {
        return chatMessages;
    }

    /// <summary>
    /// 채팅 메시지를 초기화합니다.
    /// </summary>
    public void ClearChatMessages()
    {
        chatMessages.Clear();
        SaveChatMessages();
    }

    [Serializable]
    private class ListContainer
    {
        public List<ChatMessage> Messages;

        public ListContainer(List<ChatMessage> messages)
        {
            Messages = messages;
        }
    }
}
```

AI/Models/ChatGPTAssistant/ChatGPTAPIAsync.cs
```csharp
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

#region ChatGPT API with UnityWebRequest(Async/Await)
#region ChatGPT Assistant API Async
public class ChatGPTAssistantAPIAsync
{
    private const string ApiBaseUrl = "https://api.openai.com/v1/assistants/";

    public static async Task<Assistant> RetrieveAssistantAsync()
    {
        string apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");
        string assistantId = SecurePlayerPrefs.GetString("ChatGPTAssistant_AssistantId", "");
        string url = ApiBaseUrl + assistantId;
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // Send the web request and wait for the response
        try
        {
            await request.SendWebRequestAsync();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error Retrieving Assistant: {request.error}\n{request.downloadHandler.text}");
                return null;
            }

            Debug.Log("Received: " + request.downloadHandler.text);
            Assistant assistant = JsonUtility.FromJson<Assistant>(request.downloadHandler.text);
            Debug.Log("Assistant Name: " + assistant.name);
            return assistant;

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error Retrieving Assistant: {ex.Message}");
            return null;
        }
        finally
        {
            request.Dispose();
        }
    }
}
#endregion

#region ChatGPT Thread API Async
public class ChatGPTThreadAPIAsync
{
    private const string BaseUrl = "https://api.openai.com/v1/threads";

    /// <summary>
    /// 스레드를 비동기적으로 생성합니다.
    /// </summary>
    /// <returns>성공 여부와 생성된 스레드 ID를 반환하는 튜플</returns>
    public static async Task<(bool success, string threadId)> CreateThreadAsync()
    {
        string apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");
        string url = BaseUrl;

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // 요청 바디가 필요한 경우 여기에 추가
        string requestBody = "{}"; // 필요에 따라 수정
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(requestBody));
        request.downloadHandler = new DownloadHandlerBuffer();

        try
        {
            await request.SendWebRequestAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error creating thread: {request.error}\n{request.downloadHandler.text}");
                return (false, null);
            }

            Debug.Log("Thread created: " + request.downloadHandler.text);
            Thread response = JsonUtility.FromJson<Thread>(request.downloadHandler.text);
            return (true, response.id);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating thread: {ex.Message}");
            return (false, null);
        }
        finally
        {
            request.Dispose();
        }
    }

    /// <summary>
    /// 스레드를 비동기적으로 가져옵니다.
    /// </summary>
    /// <param name="threadId">스레드 ID</param>
    /// <returns>스레드 정보를 담은 thread 객체</returns>
    public static async Task<Run> RetrieveThreadAsync(string threadId)
    {
        string apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");
        string url = $"{BaseUrl}/{threadId}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        try
        {
            await request.SendWebRequestAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error retrieving thread: {request.error}\n{request.downloadHandler.text}");
                return null;
            }

            Debug.Log("Thread retrieved: " + request.downloadHandler.text);
            Run thread = JsonUtility.FromJson<Run>(request.downloadHandler.text);
            Debug.Log("Thread ID: " + thread.id);
            return thread;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception retrieving thread: {ex.Message}");
            return null;
        }
        finally
        {
            request.Dispose();
        }
    }
}
#endregion

#region ChatGPT Message API Async
public class ChatGPTMessageAPIAsync
{
    private const string BaseUrl = "https://api.openai.com/v1/threads/";

    public static async Task<(bool success, string messageId)> CreateMessageAsync(string threadId, string role, string content)
    {
        string url = $"{BaseUrl}{threadId}/messages";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // Prepare the JSON data
        string jsonData = JsonUtility.ToJson(new MessageData(role, content));
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error creating message: {request.error}\n {request.downloadHandler.text}");
                return (false, request.downloadHandler.text);
            }

            Debug.Log("Message created\n " + request.downloadHandler.text);
            return (true, request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating message: {ex.Message}");
            return (false, ex.Message);
        }
        finally
        {
            request.Dispose();
        }
    }

    public static async Task<(bool success, string message)> RetrieveMessageAsync(string threadId, string messageId)
    {
        string url = $"{BaseUrl}{threadId}/messages/{messageId}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error retrieving message: {request.error}\n {request.downloadHandler.text}");
                return (false, request.downloadHandler.text);
            }

            Debug.Log("Message retrieved: " + request.downloadHandler.text);
            return (true, request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception retrieving message: {ex.Message}");
            return (false, ex.Message);
        }
        finally
        {
            request.Dispose();
        }
    }

    public static async Task<(bool success, MessagesListResponse response)> ListMessagesAsync(string threadId, int limit = 1, string order = "desc", string after = null, string before = null, string runId = null)
    {
        string url = $"{BaseUrl}{threadId}/messages?limit={limit}&order={order}";

        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        if (!string.IsNullOrEmpty(before))
        {
            url += $"&before={before}";
        }

        if (!string.IsNullOrEmpty(runId))
        {
            url += $"&run_id={runId}";
        }

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching messages: {request.error}\n{request.downloadHandler.text}");
                return (false, null);
            }

            Debug.Log("Messages fetched\n " + request.downloadHandler.text);
            MessagesListResponse response = JsonUtility.FromJson<MessagesListResponse>(request.downloadHandler.text);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception fetching messages: {ex.Message}");
            return (false, null);
        }
        finally
        {
            request.Dispose();
        }
    }

    public static async Task<(bool success, string message)> DeleteMessageAsync(string threadId, string messageId)
    {
        string url = $"{BaseUrl}{threadId}/messages/{messageId}";
        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error deleting message: {request.error}\n{request.downloadHandler.text}");
                return (false, request.downloadHandler.text);
            }

            Debug.Log("Message deleted: " + request.downloadHandler.text);
            return (true, request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception deleting message: {ex.Message}");
            return (false, ex.Message);
        }
        finally
        {
            request.Dispose();
        }
    }
}
#endregion

#region ChatGPT Run API Async
public class ChatGPTRunAPIAsync
{
    private const string baseUrl = "https://api.openai.com/v1/threads/";

    public static async Task<(bool success, string runId)> CreateRunAsync(string threadId, string assistantId, string model = null, string newInstructions = null, float temperature = 0.5f)
    {
        string url = $"{baseUrl}{threadId}/runs";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // JSON 데이터 준비
        RunData runData = new RunData
        {
            assistant_id = assistantId,
            model = model,
            instructions = newInstructions ?? "",
            temperature = temperature
        };
        
        string jsonData = JsonUtility.ToJson(runData);
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Run 생성 오류: {request.error}\n{request.downloadHandler.text}");
                return (false, request.downloadHandler.text);
            }

            Debug.Log("Run created\n" + request.downloadHandler.text);
            Run response = JsonUtility.FromJson<Run>(request.downloadHandler.text);
            return (true, response.id);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating run: {ex.Message}");
            return (false, ex.Message);
        }
        finally
        {
            request.Dispose();
        }
    }

    public static async Task<(bool success, Run run)> RetrieveRunAsync(string threadId, string runId)
    {
        string url = $"{baseUrl}{threadId}/runs/{runId}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error retrieving run:{request.error}\n{request.downloadHandler.text}");
                return (false, null);
            }

            Debug.Log("Run retrieved\n" + request.downloadHandler.text);
            Run run = JsonUtility.FromJson<Run>(request.downloadHandler.text);
            return (true, run);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception retrieving run: {ex.Message}");
            return (false, null);
        }
        finally
        {
            request.Dispose();
        }
    }

    public static async Task<bool> WaitForRunCompleteAsync(string threadId, string runId, int timeoutSeconds = 60)
    {
        int elapsedSeconds = 0;
        int intervalSeconds = 3;

        while (elapsedSeconds < timeoutSeconds)
        {
            await Task.Delay(intervalSeconds * 1000); // 3초 대기

            var (success, response) = await RetrieveRunAsync(threadId, runId);
            if (!success)
            {
                Debug.LogError("Failed to retrieve run status.");
                return false;
            }

            if (response.status == "completed" || response.status == "failed" || response.status == "cancelled" || response.status == "expired")
            {
                return response.status == "completed";
            }

            elapsedSeconds += intervalSeconds;
        }

        Debug.LogWarning("Run 완료 대기 타임아웃.");
        return false;
    }
}
#endregion
#endregion
```

AI/Models/ChatGPTAssistant/ChatGPTAssistant.cs
```csharp
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

public class ChatGPTAssistant : IAIModel
{
    public AIResponse GenerateResponse(string input, Action<AIResponse> onResponseGenerated)
    {
        // Retrieve the assistant ID and thread ID from the secure player prefs
        string assistantId = SecurePlayerPrefs.GetString("ChatGPTAssistant_AssistantId", "");
        string threadId = SecurePlayerPrefs.GetString("ChatGPTAssistant_ThreadId", "");
        string model = SecurePlayerPrefs.GetString("ChatGPTAssistant_Model", "gpt-4o");
        float temperature = SecurePlayerPrefs.GetFloat("ChatGPTAssistant_Temperature", 0.5f);

        //Debug.LogWarning($"GenerateResponse:\nAssistant ID: {assistantId}\n Thread ID: {threadId}\n Model: {model}\n Temperature: {temperature}");

        CreateAndRun(input, assistantId, threadId, model, temperature, generated => {
            onResponseGenerated(generated);
            return;
            });

        return null;
    }

    public AIResponse GenerateDirectingResponse(string input, Action<AIResponse> onResponseGenerated)
    {
        // Retrieve the assistant ID and thread ID from the secure player prefs
        string assistantIdDirecting = SecurePlayerPrefs.GetString("ChatGPTAssistant_AssistantIdDirecting", "");
        string threadIdDirecting = SecurePlayerPrefs.GetString("ChatGPTAssistant_ThreadIdDirecting", "");
        string modelDirecting = SecurePlayerPrefs.GetString("ChatGPTAssistant_ModelDirecting", "gpt-4o");
        float temperatureDirecting = SecurePlayerPrefs.GetFloat("ChatGPTAssistant_TemperatureDirecting", 0.5f);

        CreateAndRun(input, assistantIdDirecting, threadIdDirecting, modelDirecting, temperatureDirecting, generated => {
            onResponseGenerated(generated);
            return;
            });
            
        return null;
    }
    
    public AIResponse CreateAndRun(string input, string assistantId, string threadId, string model, float temperature, Action<AIResponse> onResponseGenerated)
    {
        // Create a new message in the thread
        ChatGPTMessageAPI.CreateMessage(threadId, "user", input, (success, response) =>{
            if (!success){
                onResponseGenerated(new AIResponse("Error creating message", false, response));
                return;
            }
    
            ChatGPTRunAPI
        .CreateRun(threadId, assistantId, (runSuccess, runResponse) =>{
                if (!runSuccess){
                    onResponseGenerated(new AIResponse("Error creating run", false, runResponse));
                    return;
                }
    
                Run run = JsonUtility.FromJson<Run>(runResponse);
                string runId = run.id;
    
                // Wait for the run to complete
                ChatGPTRunAPI
            .WaitForRunComplete(threadId, runId, (completeSuccess) =>{
                    if (!completeSuccess)
                    {
                        onResponseGenerated(new AIResponse("Run did not complete", false, ""));
                        return;
                    }
                    
                    ChatGPTMessageAPI.ListMessages(threadId, 1, "desc", null, null, runId, (messageResponse) => 
                    {
                        onResponseGenerated(new AIResponse(messageResponse.data[0].content[0].text.value, true, ""));
                    });
    
                });
            },
            model:model,
            temperature:temperature
            );
        });
    
        // Add a default return statement
        return null;
    }

#region Async Methods
    public async Task<AIResponse> GenerateResponseAsync(string input)
    {
        // Retrieve the assistant ID and thread ID from the secure player prefs
        string assistantId = SecurePlayerPrefs.GetString("ChatGPTAssistant_AssistantId", "");
        string threadId = SecurePlayerPrefs.GetString("ChatGPTAssistant_ThreadId", "");
        string model = SecurePlayerPrefs.GetString("ChatGPTAssistant_Model", "gpt-4o");
        float temperature = SecurePlayerPrefs.GetFloat("ChatGPTAssistant_Temperature", 0.5f);
        
        try
        {
            return await CreateAndRunAsync(input, assistantId, threadId, model, temperature);
        }
        catch (Exception ex)
        {
            Debug.LogError($"ChatGPTAssistant GenerateResponseAsync 오류: {ex.Message}");
            return new AIResponse("GenerateResponseAsync에서 CreateAndRunAsync 시도 중 오류가 발생했습니다.", false, ex.Message);
        }
    }

    public async Task<AIResponse> GenerateDirectingResponseAsync(string input)
    {
        // Retrieve the assistant ID and thread ID from the secure player prefs
        string assistantIdDirecting = SecurePlayerPrefs.GetString("ChatGPTAssistant_AssistantIdDirecting", "");
        string threadIdDirecting = SecurePlayerPrefs.GetString("ChatGPTAssistant_ThreadIdDirecting", "");
        string modelDirecting = SecurePlayerPrefs.GetString("ChatGPTAssistant_ModelDirecting", "gpt-4o");
        float temperatureDirecting = SecurePlayerPrefs.GetFloat("ChatGPTAssistant_TemperatureDirecting", 0.5f);
        
        try
        {
            return await CreateAndRunAsync(input, assistantIdDirecting, threadIdDirecting, modelDirecting, temperatureDirecting);
        }
        catch (Exception ex)
        {
            Debug.LogError($"ChatGPTAssistant GenerateDirectingResponseAsync 오류: {ex.Message}");
            return new AIResponse("GenerateDirectingResponseAsync에서 CreateAndRunAsync 시도 중 오류가 발생했습니다.", false, ex.Message);
        }
    }


    /// <summary>
    /// 메시지 생성, 런 생성, 런 완료 대기, 메시지 수신 단계를 비동기적으로 수행합니다.
    /// </summary>
    /// <param name="input">사용자 입력 텍스트</param>
    /// <returns>AI 응답을 담은 AIResponse 객체</returns>
    public async Task<AIResponse> CreateAndRunAsync(string input, string assistantId, string threadId, string model, float temperature)
    {
        // 스레드 ID가 없다면 새로운 스레드를 생성합니다.
        if (string.IsNullOrEmpty(threadId))
        {
            var (threadSuccess, newThreadId) = await ChatGPTThreadAPIAsync.CreateThreadAsync();
            if (!threadSuccess || string.IsNullOrEmpty(newThreadId))
            {
                return new AIResponse("새로운 스레드 생성에 실패했습니다.", false, newThreadId);
            }
            threadId = newThreadId;
            SecurePlayerPrefs.SetString("ChatGPTAssistant_ThreadId", threadId);
            Debug.Log($"새로운 스레드 생성: {threadId}");
        }

        // 2. 메시지 생성
        var (messageSuccess, messageIdOrError) = await ChatGPTMessageAPIAsync.CreateMessageAsync(threadId, "user", input);
        if (!messageSuccess)
        {
            return new AIResponse("메시지 생성에 실패했습니다.", false, messageIdOrError);
        }
        string messageId = messageIdOrError;

        // 3. 런 생성
        var (runSuccess, runIdOrError) = await ChatGPTRunAPIAsync.CreateRunAsync(threadId, assistantId, model, null, temperature);
        if (!runSuccess || string.IsNullOrEmpty(runIdOrError))
        {
            return new AIResponse("런 생성에 실패했습니다.", false, runIdOrError);
        }
        string runId = runIdOrError;

        // 4. 런 완료 대기
        bool isRunComplete = await ChatGPTRunAPIAsync.WaitForRunCompleteAsync(threadId, runId, timeoutSeconds: 60);
        if (!isRunComplete)
        {
            return new AIResponse("런 완료 대기 중 문제가 발생했습니다.", false, "런이 완료되지 않았습니다.");
        }

        // 5. 최신 메시지 목록 가져오기
        var (listSuccess, messagesList) = await ChatGPTMessageAPIAsync.ListMessagesAsync(threadId, limit: 1, order: "desc", runId: runId);
        if (!listSuccess || messagesList == null || messagesList.data.Length == 0)
        {
            return new AIResponse("AI 응답 메시지를 가져오는 데 실패했습니다.", false, "AI 응답 메시지가 없습니다.");
        }

        string aiMessage = messagesList.data[0].content[0].text.value;
        return new AIResponse(aiMessage, true, null);
    }
}
#endregion
```