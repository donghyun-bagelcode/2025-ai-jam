using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

[Serializable]
public struct AssistantSettings
{
    public string assistantId;
    public string threadId;
    public float temperature;
    public string model;
    public AssistantType assistantType;
}

public class ChatGPTAssistantSetting : EditorWindow
{
    private const string modelName = "ChatGPTAssistant";
    private string apiKey = "";
    private string[] GPTmodels = new string[] { "gpt-4o", "gpt-4o-mini"};

    private AssistantSettings writingAssistant = new AssistantSettings(){ assistantType = AssistantType.Writing };
    private AssistantSettings dialogAssistant = new AssistantSettings(){ assistantType = AssistantType.Dialog };
    private AssistantSettings directingAssistant = new AssistantSettings(){ assistantType = AssistantType.Directing };

    private int timeout = 180;
    private bool dialogCommentMode = false;
    private bool directingCommentMode = false;
    private bool addWorldDataToPrompt = false;
    private GUIStyle labelStyle;
    private GUIStyle textAreaStyle;
    private Vector2 scrollPosition;
    private string dialogDefaultCommentaryPrompt;

    private const string DEFAULT_COMMENTARY_PROMPT = "아래 스토리를 csv형식의 노드로 만들어줘. 상황 설명문도 나레션으로 변환해. 나레이션의 경우 Name에 해설자 내용은 쓰지말고 비워줘.대사에 escape 문자를 포함해선 안돼. 한 노드에 글자수가 100자를 넘어가면 노드를 나눠서 생성해줘.";

    [MenuItem("Leman/Settings/ChatGPT Assistant Settings", false, 101)]
    public static void OpenWindow()
    {
        var window = GetWindow<ChatGPTAssistantSetting>(true, "ChatGPT Assistant Settings", true);
        //window.LoadSettings();
    }

    private void OnEnable()
    {
        InitializeStyles();
        LoadSettings();
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
        
        // TextArea 스타일 추가
        textAreaStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            richText = true
        };
    }

    void OnGUI()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // ChatGPT Assistant Settings
        GUILayout.Label("ChatGPT Assistant Settings", labelStyle);
        apiKey = EditorGUILayout.TextField("API Key", apiKey);
        DrawInitilizeAssistantButton();
        GUILayout.Space(30);

        // ChatGPT Writing Assistant Settings
        GUILayout.Label("Writing Assistant Settings", labelStyle);
        CreateAssistantSettings(ref writingAssistant);
        GUILayout.Space(30);
    
        // ChatGPT Dialog Assistant Settings
        GUILayout.Label("Dialog Assistant Settings", labelStyle);
        CreateAssistantSettings(ref dialogAssistant);

        // Default Commentary Prompt
        GUILayout.Label(
            new GUIContent(
                "Default Commentary Prompt",
                "Make Node 버튼 클릭 시 사용되는 기본 프롬프트입니다.\n" +
                "스토리를 CSV 노드로 변환하는 방식을 지정할 수 있습니다.\n" +
                "Commentary for AI Dialog가 활성화된 경우 이 프롬프트가 기본값으로 표시됩니다."
            )
        );
        
        // 텍스트 내용의 높이 계산
        GUIContent content = new GUIContent(dialogDefaultCommentaryPrompt);
        float height = textAreaStyle.CalcHeight(content, position.width - 40);
        float minHeight = 40f;
        height = Mathf.Max(height, minHeight);

        string newPrompt = EditorGUILayout.TextArea(
            dialogDefaultCommentaryPrompt, 
            textAreaStyle,
            GUILayout.Height(height),
            GUILayout.ExpandWidth(true)
        );

        // 프롬프트가 비어있으면 기본값으로 복원
        if (string.IsNullOrWhiteSpace(newPrompt))
        {
            dialogDefaultCommentaryPrompt = DEFAULT_COMMENTARY_PROMPT;
        }
        else
        {
            dialogDefaultCommentaryPrompt = newPrompt;
        }
        GUILayout.Space(30);

        // ChatGPT Directing Assistant Settings
        GUILayout.Label("Directing Assistant Settings", labelStyle);
        CreateAssistantSettings(ref directingAssistant);
        GUILayout.Space(30);

        // Additional Options
        GUILayout.Space(30);
        GUILayout.Label("Additional Options", labelStyle);
        
        // Timeout Setting
        GUIContent timeoutContent = new GUIContent("Timeout (sec)", "API 요청 시간 초과 시간을 설정합니다. 최소값은 60 초입니다. (Default: 180)");
        timeout = EditorGUILayout.IntField(timeoutContent, timeout, GUILayout.Width(200));   

        // Comment for AI Dialog
        GUIContent dialogCommentModeContent = new GUIContent("Comment for AI Dialog", "AI Dialog 코멘터리 창의 표시 여부를 선택합니다. 체크 시 Apply with AI Dialog 버튼을 누르면 코멘터리 창이 열립니다.");
        dialogCommentMode = EditorGUILayout.Toggle(dialogCommentModeContent, dialogCommentMode);

        // Comment for AI Directing
        GUIContent directingCommentModeContent = new GUIContent("Comment for AI Directing", "AI Directing 코멘터리 창의 표시 여부를 선택합니다. 체크 시 Apply with AI Directing 버튼을 누르면 코멘터리 창이 열립니다.");
        directingCommentMode = EditorGUILayout.Toggle(directingCommentModeContent, directingCommentMode);

        // Add World Data To Prompt Option
        GUIContent addWorldDataToPromptContent = new GUIContent("World Data To Prompt", "Prompt 마지막에 World Panel Data를 추가합니다. 체크 시 World Data가 Prompt에 추가됩니다.");
        addWorldDataToPrompt = EditorGUILayout.Toggle(addWorldDataToPromptContent, addWorldDataToPrompt);

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

        GUILayout.EndScrollView();
    }
    
    private void CreateAssistantSettings(ref AssistantSettings assistant)
    {
        assistant.assistantId = EditorGUILayout.TextField("Assistant ID", assistant.assistantId);
        assistant.threadId = EditorGUILayout.TextField("Thread ID", assistant.threadId);
        DrawCreateThreadButton(assistant.assistantType);
        assistant.temperature = EditorGUILayout.Slider("Temperature", assistant.temperature, 0.0f, 2.0f);
        assistant.model = GPTmodels[EditorGUILayout.Popup("Model", Array.IndexOf(GPTmodels, assistant.model), GPTmodels)];
    }

    private void DrawInitilizeAssistantButton()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button($"Initialize All Assistants", GUILayout.Width(300)))
        {
            SaveSettings();
            if (EditorUtility.DisplayDialog("Initialize All Assistants", "Are you sure you want to initialize all assistants?", "Yes", "No"))
            {
                InitializeAssistant();
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 스레드 생성 버튼을 그리는 메서드
    /// </summary>
    /// <param name="AssistantType">스레드 타입</param>
    /// <param name="threadId">스레드 ID 참조</param>
    private void DrawCreateThreadButton(AssistantType AssistantType)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button($"Create New {AssistantType.ToString()} Thread", GUILayout.Width(300)))
        {
            if (EditorUtility.DisplayDialog($"Create New {AssistantType.ToString()} Thread", 
                $"Are you sure you want to create a new {AssistantType.ToString().ToLower()} thread?", "Yes", "No"))
            {
                CreateThreadWithProgress(AssistantType);
            }
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private async void CreateThreadWithProgress(AssistantType assistantType)
    {
        EditorUtility.DisplayProgressBar("Creating Thread", "Creating new thread...", 0.5f);
        try 
        {
            await CreateNewThreadAsync(assistantType);
        }
        finally 
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private async void InitializeAssistant()
    {
        //Todo: writing, dialog, directing 어시스턴트 생성후 해당 타입의 스레드 생성
        Debug.Log("Initialize Assistant");
        Assistant writing = await ChatGPTAssistantAPIAsync.CreateAssistantAsync(PromptManager.AssistantInstructions(AssistantType.Writing), "Writing Assistant");
        Assistant dialog = await ChatGPTAssistantAPIAsync.CreateAssistantAsync(PromptManager.AssistantInstructions(AssistantType.Dialog), "Dialog Assistant");
        Assistant directing = await ChatGPTAssistantAPIAsync.CreateAssistantAsync(PromptManager.AssistantInstructions(AssistantType.Directing), "Directing Assistant");

        SetAssistantId(AssistantType.Writing, writing.id);
        SetAssistantId(AssistantType.Dialog, dialog.id);
        SetAssistantId(AssistantType.Directing, directing.id);

        await CreateNewThreadAsync(AssistantType.Writing);
        await CreateNewThreadAsync(AssistantType.Dialog);
        await CreateNewThreadAsync(AssistantType.Directing);

        SaveSettings();

        EditorUtility.DisplayDialog("Assistant Initialized", "All assistants have been initialized successfully.", "OK");
    }


    /// <summary>
    /// 스레드를 비동기적으로 생성하는 메서드
    /// </summary>
    /// <param name="AssistantType">스레드 타입</param>
    private async Task CreateNewThreadAsync(AssistantType AssistantType)
    {
        try
        {
            // 비동기 스레드 생성 시작
            var (success, newThreadId) = await ChatGPTThreadAPIAsync.CreateThreadAsync();

            if (success && !string.IsNullOrEmpty(newThreadId))
            {
                Debug.Log($"Thread Created ({AssistantType}): " + newThreadId);
                SetThreadId(AssistantType, newThreadId);

                EditorUtility.DisplayDialog("Thread Created", $"A new {AssistantType.ToString().ToLower()} thread has been created successfully.\nThread ID: {newThreadId}", "OK");
            }
            else
            {
                Debug.LogError($"Failed to create {AssistantType} thread: " + newThreadId);
                EditorUtility.DisplayDialog("Thread Creation Failed", $"Failed to create a new {AssistantType.ToString().ToLower()} thread.\nError: {newThreadId}", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating {AssistantType} thread: {ex.Message}");
            EditorUtility.DisplayDialog("Thread Creation Failed", $"An error occurred while creating a new {AssistantType.ToString().ToLower()} thread.\nError: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// 설정을 저장하는 메서드
    /// </summary>
    private void SaveSettings()
    {
        SecurePlayerPrefs.SetString("ChatGPTAssistant_ApiKey", apiKey.Trim());

        SaveAssistantSettings(writingAssistant);
        SaveAssistantSettings(dialogAssistant);
        SaveAssistantSettings(directingAssistant);

        // Additional Options
        if (timeout < 60){
            timeout = 60;
            EditorUtility.DisplayDialog("Timeout Error", "Timeout value must be at least 60 seconds. The value has been set to 60 seconds.", "OK");
            Repaint();
        }
        SecurePlayerPrefs.SetInt("ChatGPTAssistant_Timeout", timeout);
        SecurePlayerPrefs.SetInt("ChatGPTAssistant_DialogCommentMode", dialogCommentMode ? 1 : 0);
        SecurePlayerPrefs.SetInt("ChatGPTAssistant_DirectingCommentMode", directingCommentMode ? 1 : 0);
        SecurePlayerPrefs.SetInt("ChatGPTAssistant_AddWorldDataToPrompt", addWorldDataToPrompt ? 1 : 0);

        // Save Default Commentary Prompt
        SecurePlayerPrefs.SetString("ChatGPTAssistant_DialogDefaultCommentaryPrompt", dialogDefaultCommentaryPrompt);

        SecurePlayerPrefs.Save();
    }
    private void SaveAssistantSettings(AssistantSettings assistant)
    {
        Debug.Log($"Saving Assistant Settings: {assistant.assistantType}");
        Debug.Log($"Assistant ID: {assistant.assistantId}, Thread ID: {assistant.threadId}");
        SecurePlayerPrefs.SetString($"ChatGPTAssistant_AssistantId_{assistant.assistantType}", assistant.assistantId.Trim());
        SecurePlayerPrefs.SetString($"ChatGPTAssistant_ThreadId_{assistant.assistantType}", assistant.threadId.Trim());
        SecurePlayerPrefs.SetFloat($"ChatGPTAssistant_Temperature_{assistant.assistantType}", assistant.temperature);
        SecurePlayerPrefs.SetString($"ChatGPTAssistant_Model_{assistant.assistantType}", assistant.model);
    }

    /// <summary>
    /// 설정을 로드하는 메서드
    /// </summary>
    private void LoadSettings()
    {
        apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");

        Debug.Log($"Loading Settings");
        LoadAssistantSettings(ref writingAssistant);
        LoadAssistantSettings(ref dialogAssistant);
        LoadAssistantSettings(ref directingAssistant);

        // Additional Options
        timeout = SecurePlayerPrefs.GetInt("ChatGPTAssistant_Timeout", 180);
        dialogCommentMode = SecurePlayerPrefs.GetInt("ChatGPTAssistant_DialogCommentMode", 0) == 1;
        directingCommentMode = SecurePlayerPrefs.GetInt("ChatGPTAssistant_DirectingCommentMode", 1) == 1;
        addWorldDataToPrompt = SecurePlayerPrefs.GetInt("ChatGPTAssistant_AddWorldDataToPrompt", 1) == 1;

        // Load Default Commentary Prompt
        dialogDefaultCommentaryPrompt = SecurePlayerPrefs.GetString(
            "ChatGPTAssistant_DialogDefaultCommentaryPrompt",
            DEFAULT_COMMENTARY_PROMPT
        );
    }

    private void LoadAssistantSettings(ref AssistantSettings assistant)
    {
        //Debug.Log($"Loading Assistant Settings: {assistant.assistantType}");
        assistant.assistantId = SecurePlayerPrefs.GetString($"ChatGPTAssistant_AssistantId_{assistant.assistantType}", "");
        assistant.threadId = SecurePlayerPrefs.GetString($"ChatGPTAssistant_ThreadId_{assistant.assistantType}", "");
        assistant.temperature = SecurePlayerPrefs.GetFloat($"ChatGPTAssistant_Temperature_{assistant.assistantType}", 0.5f);
        assistant.model = SecurePlayerPrefs.GetString($"ChatGPTAssistant_Model_{assistant.assistantType}", GPTmodels[0]);

        //Debug.Log($"Loaded Assistant ID: {assistant.assistantId}, Thread ID: {assistant.threadId}");

        try
        {
            assistant.model = GPTmodels[Array.IndexOf(GPTmodels, assistant.model)];
        }
        catch (Exception)
        {
            Debug.LogWarning($"Model not found: {assistant.model}");
            assistant.model = GPTmodels[0];
        }
    }

    private void SetAssistantId(AssistantType assistantType, string assistantId)
    {
        switch (assistantType)
        {
            case AssistantType.Writing:
                writingAssistant.assistantId = assistantId.Trim();
                break;
            case AssistantType.Dialog:
                dialogAssistant.assistantId = assistantId.Trim();
                break;
            case AssistantType.Directing:
                directingAssistant.assistantId = assistantId.Trim();
                break;
        }
        SecurePlayerPrefs.SetString($"ChatGPTAssistant_AssistantId_{assistantType}", assistantId.Trim());
        SecurePlayerPrefs.Save();
        Repaint();
    }

    private void SetThreadId(AssistantType assistantType, string threadId)
    {
        switch (assistantType)
        {
            case AssistantType.Writing:
                writingAssistant.threadId = threadId.Trim();
                break;
            case AssistantType.Dialog:
                dialogAssistant.threadId = threadId.Trim();
                break;
            case AssistantType.Directing:
                directingAssistant.threadId = threadId.Trim();
                break;
        }
        
        SecurePlayerPrefs.SetString($"ChatGPTAssistant_ThreadId_{assistantType}", threadId.Trim());
        SecurePlayerPrefs.Save();
        Repaint();
    }
}