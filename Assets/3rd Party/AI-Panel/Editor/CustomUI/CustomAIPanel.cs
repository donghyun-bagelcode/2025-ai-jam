using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Dunward.Capricorn;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Threading;

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
    private GUIStyle boldLabelStyle;
    private List<ChatMessage> ChatMessages = new List<ChatMessage>();
    string[] models = new string[] { "GPT Assistant"};
    private int selectedAIModelIndex = 0;
    private int previousSelectedAIModelIndex = -1;

    private ChatMessageService chatMessageService;
    private int previouseMessageCount = 0;
    private AIManager aiManager;

    private bool stylesInitialized = false;

    // World panel data
    private WorldPanelData worldData;
    
    // **로딩 인디케이터 관련 변수 추가**
    private bool isLoading = false; // 로딩 상태 플래그
    private float loadingTimer = 0f; // 로딩 타이머
    private float loadingInterval = 0.5f; // 로딩 애니메이션 업데이트 간격 (초)
    private int loadingDotCount = 0; // 로딩 애니메이션의 현재 점 개수
    private double lastUpdateTime; // 마지막 업데이트 시간

    // 추가 옵션 관련 변수
    private bool dialogCommentMode = false;
    private bool directingCommentMode = false;
    private bool addWorldDataToPrompt = false;

    static JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto, //필수옵션
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.Indented
    };

    [MenuItem("Leman/Chat Panel",false, 2)]
    public static void ShowWindow()
    {
        GetWindow<CustomAIPanel>("Chat Panel");
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
        if (!EditorGUIUtility.isProSkin)
        {
            // EditorStyles가 아직 준비되지 않았으므로 다시 시도
            EditorApplication.delayCall += InitializeStyles;
            return;
        }

        try
        {
            // Set user input text area style
            textAreaStyle = new GUIStyle(EditorStyles.textArea ?? GUI.skin.textArea)
            {
                wordWrap = true
            };

            // Set message text area style
            messageStyle = new GUIStyle(EditorStyles.textArea ?? GUI.skin.textArea)
            {
                padding = new RectOffset(20, 10, 10, 20),
                wordWrap = true
            };

            // Custom style for code text area
            codeNameStyle = new GUIStyle(EditorStyles.textArea ?? GUI.skin.textArea)
            {
                normal = new GUIStyleState 
                { 
                    textColor = Color.white,
                    background = Texture2D.blackTexture 
                }
            };

            codeTextStyle = new GUIStyle(EditorStyles.textArea ?? GUI.skin.textArea)
            {
                padding = new RectOffset(20, 10, 10, 20),
                wordWrap = true
            };

            loadingLabelStyle = new GUIStyle(EditorStyles.label ?? GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 15,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white }
            };

            boldLabelStyle = new GUIStyle(EditorStyles.label ?? GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 18
            };

            stylesInitialized = true;
            Repaint();
        }
        catch
        {
            // 스타일 초기화 실패 시 다시 시도
            EditorApplication.delayCall += InitializeStyles;
        }
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
        var messagesCopy = new List<ChatMessage>(messages);

        if (messagesCopy.Count > previouseMessageCount)
        {
            previouseMessageCount = messagesCopy.Count;
            ScrollToBottom();
        }

        foreach (var message in messagesCopy)
        {
            // 메시지 간 간격
            GUILayout.Space(40);

            bool isFirstDisplay = true;  // 각 메시지의 첫 번째 표시 여부

            // Writing Assistant일 때만 request 표시
            if (!string.IsNullOrEmpty(message.request) && message.type != AssistantType.Dialog)
            {
                DisplayUserMessage(message, isFirstDisplay);
                isFirstDisplay = false;
            }

            // message가 있을 때만 AI 응답 표시
            if (!string.IsNullOrEmpty(message.message))
            {
                DisplayAIMessage(message, isFirstDisplay);
            }
            
            // 코드 파일이 있을 때만 코드 뷰어 표시
            if (message.codeFiles?.codes.Count > 0)
            {
                DisplayCodeViewer(message);
            }
        }
    }

    private void DisplayUserMessage(ChatMessage message, bool showDeleteButton)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("You", boldLabelStyle);
        if (showDeleteButton)
        {
            GUILayout.FlexibleSpace();
            GUI.enabled = !isLoading;
            if (GUILayout.Button("Delete", GUILayout.Width(80)))
            {
                chatMessageService.RemoveMessage(message.id);
                Repaint();
            }
            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        float requiredHeight = messageStyle.CalcHeight(new GUIContent(message.request), position.width - 40);
        float textAreaHeight = Mathf.Clamp(requiredHeight, 30f, 250f);
        bool showScroll = requiredHeight > 250f;
        
        if (showScroll)
        {
            message.userMessageScrollPosition = GUILayout.BeginScrollView(message.userMessageScrollPosition, GUILayout.Height(textAreaHeight));
            GUILayout.TextArea($"{message.request}", messageStyle, GUILayout.ExpandHeight(false)); 
            GUILayout.EndScrollView();
        }
        else
        {
            GUILayout.TextArea($"{message.request}", messageStyle, GUILayout.Height(textAreaHeight), GUILayout.ExpandHeight(false));
        }
        EditorGUILayout.EndVertical();
    }

    private void DisplayAIMessage(ChatMessage message, bool showDeleteButton)
    {
        GUILayout.Space(20);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"> {message.type.ToString()} Assistant", boldLabelStyle);
        if (showDeleteButton)
        {
            GUILayout.FlexibleSpace();
            GUI.enabled = !isLoading;
            if (GUILayout.Button("Delete", GUILayout.Width(80)))
            {
                chatMessageService.RemoveMessage(message.id);
                Repaint();
            }
            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical(GUI.skin.box);
        float requiredHeight = messageStyle.CalcHeight(new GUIContent(message.message), position.width - 40);
        float textAreaHeight = Mathf.Clamp(requiredHeight, 40f, 250f);
        bool showScroll = requiredHeight > 250f;

        if (showScroll)
        {
            message.aiMessageScrollPosition = EditorGUILayout.BeginScrollView(message.aiMessageScrollPosition, GUILayout.Height(textAreaHeight));
             GUILayout.TextArea($"{message.message}", messageStyle, GUILayout.ExpandHeight(false));
              EditorGUILayout.EndScrollView();
        }
        else
        {
            GUILayout.TextArea($"{message.message}", messageStyle, GUILayout.Height(textAreaHeight), GUILayout.ExpandHeight(false));
        }
         EditorGUILayout.EndVertical();
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
        string filename = message.codeFiles.codes[message.currentCodePage].filename;
        string code = message.codeFiles.codes[message.currentCodePage].code;

        // GUIContent을 사용하여 코드의 높이 계산
        GUIContent content = new GUIContent(code);
        float requiredHeight = codeTextStyle.CalcHeight(content, position.width - 40); // 40은 좌우 여백을 고려한 값

        // 최소 및 최대 높이 설정
        float minHeight = 100f;
        float maxHeight = 250f;
        float textAreaHeight = Mathf.Clamp(requiredHeight, minHeight, maxHeight);
        bool showScroll = requiredHeight > maxHeight;

        EditorGUILayout.BeginVertical(GUI.skin.box);
        if (showScroll)
        {
            message.codeScrollPosition = EditorGUILayout.BeginScrollView(message.codeScrollPosition, GUILayout.Height(textAreaHeight));

            // 코드 TextArea 표시
            string editedCode = EditorGUILayout.TextArea(code, codeTextStyle, GUILayout.ExpandHeight(false));
            if (editedCode != code)
            {
                message.codeFiles.codes[message.currentCodePage].code = editedCode;
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            string editedCode = EditorGUILayout.TextArea(code, codeTextStyle, GUILayout.ExpandHeight(false));
            if (editedCode != code)
            {
                message.codeFiles.codes[message.currentCodePage].code = editedCode;
            }
        }

        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.enabled = !isLoading;

        if (message.type == AssistantType.Writing)
        {
            if (GUILayout.Button("Make Node", GUILayout.Width(200)))
            {
                string defaultComment = SecurePlayerPrefs.GetString(
                    "ChatGPTAssistant_DialogDefaultCommentaryPrompt",
                    ""
                );
                
                dialogCommentMode = SecurePlayerPrefs.GetInt("ChatGPTAssistant_DialogCommentMode", 0) == 1;

                if (dialogCommentMode)
                {
                    CommentaryWindow.ShowWindow((comment) =>
                    {
                        comment += $"\n\n{filename}\n```\n{code}```";
                        GenerateCsvNodes(comment);
                    },
                    defaultComment);
                }
                else
                {
                    string comment = $"{defaultComment}\n\n{filename}\n```\n{code}```";
                    GenerateCsvNodes(comment);
                }
            }
            // GUILayout.Space(10);

            // if (GUILayout.Button("Make Node (All)", GUILayout.Width(200)))
            // {
            //     string defaultComment = SecurePlayerPrefs.GetString(
            //         "ChatGPTAssistant_DialogDefaultCommentaryPrompt",
            //         ""
            //     );
                
            //     dialogCommentMode = SecurePlayerPrefs.GetInt("ChatGPTAssistant_DialogCommentMode", 0) == 1;
            //     if (dialogCommentMode)
            //     {
            //         CommentaryWindow.ShowWindow((comment) =>
            //         {
            //             foreach (var codeFile in message.codeFiles.codes)
            //             {
            //                 comment += $"\n\n{codeFile.filename}\n```\n{codeFile.code}```";
            //             }
            //             GenerateCsvNodes(comment);
            //         },
            //         defaultComment);
            //     }
            //     else
            //     {
            //         string comment = defaultComment;
            //         foreach (var codeFile in message.codeFiles.codes)
            //         {
            //             comment += $"\n\n{codeFile.filename}\n```\n{codeFile.code}```";
            //         }
            //         GenerateCsvNodes(comment);
            //     }
            // }
        }
        else if (message.type == AssistantType.Dialog)
        {
            if (GUILayout.Button("Apply Node", GUILayout.Width(200)))
            {
                ApplyCurrentNode(code);
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Apply Node with AI Directing", GUILayout.Width(200)))
            {
                directingCommentMode = SecurePlayerPrefs.GetInt("ChatGPTAssistant_DirectingCommentMode", 0) == 1;
                if (directingCommentMode)
                {
                    string defaultComment = $"연출을 가능한 상세히 지시하세요.";

                    CommentaryWindow.ShowWindow((comment) =>
                    {
                        ApplyAIDirecting(code, comment);
                    }, 
                    defaultComment);
                }
                else
                {
                    ApplyAIDirecting(code, "주어진 스크립트에 대해 적절한 연출을 생성하세요.");
                }
            }
        }

        GUI.enabled = true;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void ScrollToBottom()
    {
        Debug.Log("Scrolling to bottom");
        scrollPosition.y = float.MaxValue;
        Repaint();
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
        // 수직 레이아웃 시작
        // 전체 레이아웃을 수평으로 분할
        EditorGUILayout.BeginHorizontal();
        int buttonGroupWidth = 240;
        // 왼쪽: User Input 창
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width - buttonGroupWidth)); // 전체 너비의 60% 할당

        // User Input 창의 높이를 동적으로 계산
        float calculatedHeight = textAreaStyle.CalcHeight(new GUIContent(userInput), position.width - buttonGroupWidth - 20);
        float minHeight = textAreaStyle.CalcHeight(new GUIContent("Test"), position.width - buttonGroupWidth - 20) * 2; // 최소 2줄
        float maxHeight = textAreaStyle.CalcHeight(new GUIContent("Test"), position.width - buttonGroupWidth - 20) * 10; // 최대 10줄

        float userInputHeight = Mathf.Clamp(calculatedHeight, MinUserInputHeight, MaxUserInputHeight);

        // TextArea를 사용하여 User Input 창 생성
        userInputScrollPosition = EditorGUILayout.BeginScrollView(userInputScrollPosition, GUILayout.Height(userInputHeight));
        userInput = EditorGUILayout.TextArea(userInput, textAreaStyle, GUILayout.ExpandHeight(false));
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // 오른쪽: 버튼 그룹
        EditorGUILayout.BeginVertical(GUILayout.Width(buttonGroupWidth)); // 전체 너비의 40% 할당

            // 첫 번째 버튼 그룹: Send, Select File, Test
            EditorGUILayout.BeginHorizontal();

                // Send 버튼
                GUI.enabled = !isLoading; // 로딩 중일 때 Send 버튼 비활성화
                if (GUILayout.Button("Send", GUILayout.Width(buttonGroupWidth*0.45f)))
                {
                    HandleUserInputAsync();
                }
                GUI.enabled = true; // Send 버튼 이후 버튼들은 활성화 상태로

                // Select File 버튼
                if (GUILayout.Button("Select File", GUILayout.Width(buttonGroupWidth*0.45f)))
                {
                    FileSelectorWindow.ShowWindow();
                }

                // Test 버튼
                // if (GUILayout.Button("Test", GUILayout.Width(60)))
                // {
                //     Test();
                // }

            EditorGUILayout.EndHorizontal();

            // 두 번째 버튼 그룹: Directing Selected Nodes
            EditorGUILayout.BeginHorizontal();


                if (GUILayout.Button("Directing Selected Nodes", GUILayout.Width(buttonGroupWidth - 20)))
                {
                    var capriconWindow = EditorWindow.GetWindow<CapricornEditorWindow>();
                
                    string json = capriconWindow.GetGraphSerializedData();
                    string jsonSelected = capriconWindow.GetSelectionGraphSerializedData();
                    Debug.Log($"json \n{json}");
                    Debug.Log($"jsonSelected \n{jsonSelected}");

                    var data = JsonConvert.DeserializeObject<GraphData>(jsonSelected);
                    if (data.nodes == null || data.nodes.Count == 0)
                    {
                        EditorUtility.DisplayDialog("선택된 노드가 없습니다.", "연출을 적용할 노드를 선택하세요.", "확인");

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        return;
                    }

                    directingCommentMode = SecurePlayerPrefs.GetInt("ChatGPTAssistant_DirectingCommentMode", 0) == 1;
                    if (directingCommentMode)
                    {
                        string defaultComment = $"선택된 노드에 대한 연출을 가능한 상세히 지시하세요.";

                        CommentaryWindow.ShowWindow((comment) =>
                        {
                            ApplyAIDirectingSelectedNodes(jsonSelected, comment);
                        },
                        defaultComment);
                    }
                    else
                    {
                        ApplyAIDirectingSelectedNodes(jsonSelected, "주어진 스크립트에 대해 적절한 연출을 생성하세요.");
                    }
                }

                GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private async void HandleUserInputAsync()
    {
        if (string.IsNullOrEmpty(userInput))
        {
            Debug.LogWarning("User input is empty.");
            return;
        }

        // World Data 필요 여부 확인
        addWorldDataToPrompt = SecurePlayerPrefs.GetInt("ChatGPTAssistant_AddWorldDataToPrompt", 0) == 1;
        if (addWorldDataToPrompt)
        {
            string worldDataPrompt = WorldDataPrompt();
            if (string.IsNullOrEmpty(worldDataPrompt))
            {
                EditorUtility.DisplayDialog(
                    "World Data 필요",
                    "World Data 첨부 옵션이 활성화되어 있지만 유효한 World Data를 찾을 수 없습니다.\n\n" +
                    "World Panel에서 Logline과 캐릭터 정보를 입력하거나, World Data 첨부 옵션을 비활성화하고 다시 시도해주세요.",
                    "확인"
                );
                return;
            }
        }

        // 입력 데이터 준비 및 처리
        var inputData = PrepareUserInput();
        ClearUserInput();
        await ProcessUserMessage(inputData);
    }

    private (string UserMessage, string AIPrompt) PrepareUserInput()
    {
        string userMessage = AddSelectedFilesList(userInput);
        string aiPrompt = userInput;
        addWorldDataToPrompt = SecurePlayerPrefs.GetInt("ChatGPTAssistant_AddWorldDataToPrompt", 0) == 1;
        if (addWorldDataToPrompt)
        {
            aiPrompt += WorldDataPrompt();
        }

        // Add selected scene csv to prompt
        aiPrompt = PromptManager.AddFilesToUserPrompt(aiPrompt);
        
        return (userMessage, aiPrompt);
    }

    private void ClearUserInput()
    {
        userInput = "";
    }

    private async Task ProcessUserMessage((string UserMessage, string AIPrompt) input)
    {
        chatMessageService.AddUserMessage(AssistantType.Writing, input.UserMessage);
        StartLoading();

        try
        {
            AIResponse response = await aiManager.GetResponseAsync(AssistantType.Writing, input.AIPrompt);
            response.ParseResponse();
            chatMessageService.AddAIResponse(response);
            Repaint();
        }
        catch (TimeoutException ex)
        {
            Debug.LogError(ex.Message);
            chatMessageService.AddAIResponse(new AIResponse(ex.Message, false, "Local Timeout"));
        }
        catch (Exception ex)
        {
            Debug.LogError($"HandleUserInputAsync 오류: {ex.Message}");
            chatMessageService.AddAIResponse(new AIResponse("AI 응답 생성 중 오류가 발생했습니다.", false, ex.Message));
        }
        finally
        {
            StopLoading();
            ScrollToBottom();
            Repaint();
        }
    }

    private string WorldDataPrompt()
    {
        try
        {
            WorldPanelData worldData = null;

            // 1. 열려있는 WorldPanel 찾기
            var worldPanel = EditorWindow.focusedWindow as WorldPanel;
            if (worldPanel == null)
            {
                var windows = Resources.FindObjectsOfTypeAll(typeof(WorldPanel));
                if (windows.Length > 0)
                {
                    worldPanel = windows[0] as WorldPanel;
                    Debug.Log("현재 포커스된 World Panel이 없어 열려있는 첫 번째 World Panel을 사용합니다.");
                }
                else
                {
                    Debug.LogWarning("열려있는 World Panel을 찾을 수 없습니다. 마지막으로 저장된 파일을 확인합니다.");
                }
            }

            // 2. 열려있는 창에서 데이터 가져오기
            if (worldPanel != null)
            {
                try
                {
                    worldData = worldPanel.GetCurrentWorldData();
                    if (worldData == null)
                    {
                        Debug.LogWarning("World Panel이 존재하지만 데이터가 비어있습니다.");
                    }
                    else if (string.IsNullOrEmpty(worldData.logline))
                    {
                        Debug.LogWarning("World Panel에 Logline이 작성되지 않았습니다.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"World Panel에서 데이터를 가져오는 중 오류 발생: {ex.Message}");
                }
            }

            // 3. 창이 없거나 데이터가 유효하지 않으면 파일에서 읽기
            if (worldData == null || string.IsNullOrEmpty(worldData.logline))
            {
                string lastFile = EditorPrefs.GetString("WorldPanel_LastOpenedFile", "");
                if (string.IsNullOrEmpty(lastFile))
                {
                    Debug.LogWarning("마지막으로 저장된 World Panel 파일 경로를 찾을 수 없습니다.");
                    return "";
                }

                if (!File.Exists(lastFile))
                {
                    Debug.LogWarning($"저장된 파일을 찾을 수 없습니다: {lastFile}");
                    return "";
                }

                try
                {
                    string savedJson = File.ReadAllText(lastFile);
                    worldData = JsonConvert.DeserializeObject<WorldPanelData>(savedJson);
                    
                    if (worldData == null)
                    {
                        Debug.LogWarning("저장된 파일에서 World Data를 읽을 수 없습니다.");
                        return "";
                    }
                    
                    if (string.IsNullOrEmpty(worldData.logline))
                    {
                        Debug.LogWarning("저장된 파일에 Logline이 없습니다.");
                        return "";
                    }

                    Debug.Log($"저장된 파일에서 World Data를 성공적으로 로드했습니다: {lastFile}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"World Data 파일 로드 중 오류 발생: {ex.Message}");
                    return "";
                }
            }

            // 4. 데이터가 있고 유효한 경우 프롬프트 생성
            if (worldData != null && !string.IsNullOrEmpty(worldData.logline))
            {
                return $"\n\n**아래는 본 소설의 세계관 정보입니다.**\n\n" +
                       $"**Genres**: {worldData.genres}\n" +
                       $"**Keywords**: {worldData.keywords}\n" +
                       $"**Logline**: {worldData.logline}\n\n" +
                       $"**Main Characters**:\n[{string.Join(",\n", worldData.mainCharacters?.Select(x => JsonConvert.SerializeObject(x, jsonSettings)) ?? new string[0])}]\n\n" +
                       $"**Sub Characters**:\n[{string.Join(",\n", worldData.subCharacters?.Select(x => JsonConvert.SerializeObject(x, jsonSettings)) ?? new string[0])}]\n\n";
            }
            else
            {
                Debug.LogWarning("World Data가 없거나 유효하지 않습니다.");
            }

            return "";
        }
        catch (Exception ex)
        {
            Debug.LogError($"World Data 처리 중 예기치 않은 오류 발생: {ex.Message}\n{ex.StackTrace}");
            return "";
        }
    }

    private async void GenerateCsvNodes(string input)
    {
        chatMessageService.AddUserMessage(AssistantType.Dialog, input);
        StartLoading();

        try
        {
            AIResponse response = await aiManager.GetResponseAsync(AssistantType.Dialog, input);
            response.ParseResponse();
            chatMessageService.AddAIResponse(response);
            Repaint();
        }
        catch (TimeoutException ex)
        {
            Debug.LogError(ex.Message);
            chatMessageService.AddAIResponse(new AIResponse(ex.Message, false, "Local Timeout"));
        }
        catch (Exception ex)
        {
            Debug.LogError($"GenerateCsvNodes 오류: {ex.Message}");
            chatMessageService.AddAIResponse(new AIResponse("AI 응답 생성 중 오류가 발생했습니다.", false, ex.Message));
        }
        finally
        {
            StopLoading();
            ScrollToBottom();
            Repaint();
        }
    }

    // Deprecated. Designed to add selected source code list to prompt.
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

    private void ApplyCurrentNode(string code)
    {
        string json = CsvToJsonConverter.CsvToJsonConverterMethod(code);
        json = AddNodeToEnd(json);
        AddNodesFromJson(json);
    }

    private async void ApplyAIDirecting(string code, string comment)
    {
        string directionPrompt = $"{comment}\n\n```\n{code}```";

        string mergedJson = await GetAIDirecting(code, directionPrompt);
        
        mergedJson = AddNodeToEnd(mergedJson);
        AddNodesFromJson(mergedJson);
        
        Debug.Log("Applying AI Directing");
    }

    private async void ApplyAIDirectingSelectedNodes(string selectedNodes, string commentary)
    {
        string csvContents = JsonToCsvConverter.JsonToCsvConverterMethod(selectedNodes);
        string mergedJson = await GetAIDirecting(csvContents, commentary);
        ReplaceNodesFromJson(mergedJson);
        Debug.Log("csvContents \n" + csvContents);
        Debug.Log("Applying AI Directing to selected nodes");
    }
    
    private async Task<string> GetAIDirecting(string csvContents, string commentary)
    {
        string jsonContents = CsvToJsonConverter.CsvToJsonConverterMethod(csvContents);
        string directionPrompt = PromptManager.DirectingPrompt(csvContents, commentary);

        StartLoading();
        
        try
        {
            AIResponse response = await aiManager.GetResponseAsync(AssistantType.Directing, directionPrompt);
            response.ParseResponse();
            if (response.Codes != null)
            {
                string direction = response.Codes[0].code;
                Debug.Log($"Directing Response: {direction}");
                string mergedJson = MergeDirecting(jsonContents, direction);

                return mergedJson;
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
        }
        
        return null;
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
                            
                            if (type == "SetAllCharacterHighlight")
                            {
                                newType = $"Dunward.Capricorn.{type}, Assembly-CSharp";
                            }
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
        var addY = (graphWindowHeight/3 - translate.y)/scale - stats.MedY;

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

    public void AddNodesFromJson(string json)
    {
        var capriconWindow = GetWindow<CapricornEditorWindow>();
        capriconWindow.AddNodesFromJson(json);
    }

    public void ReplaceNodesFromJson(string json)
    {
        var capriconWindow = GetWindow<CapricornEditorWindow>();
        capriconWindow.ReplaceNodesFromJson(json);
    }

// #region test

//     private async void Test()
//     {
//         Debug.Log("Test");
//     }

// #endregion
}