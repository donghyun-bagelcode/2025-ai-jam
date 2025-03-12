#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using System.IO;


public class WorldPanel : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset worldPanelAsset;
    [SerializeField]
    private VisualTreeAsset mainCharacterAsset;
    [SerializeField]
    private VisualTreeAsset subCharacterAsset;
    [SerializeField]
    private VisualTreeAsset characterPlotAsset;

    private List<WorldMainCharacter> mainCharacters = new List<WorldMainCharacter>();
    private List<WorldCharacterPlot> mainCharacterPlots = new List<WorldCharacterPlot>();
    private List<WorldSubCharacter> subCharacters = new List<WorldSubCharacter>();
    private List<WorldCharacterPlot> subCharacterPlots = new List<WorldCharacterPlot>();
    private List<Button> allButtons;

    private const string LastOpenedFileKey = "WorldPanel_LastOpenedFile";

    private const string DefaultGenreText = "ex) 액션, 코메디, 판타지, SF, 스릴러, 호러 등";
    private const string DefaultKeywordText = "ex) 운명, 기억상실, 평행 세계, 미제 사건 등";

    [MenuItem("Leman/World Panel", false, 1)]
    public static void ShowWindow()
    {
        var panel = GetWindow<WorldPanel>();
        panel.titleContent = new GUIContent("World Panel");
        panel.Show();
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
        string lastFile = EditorPrefs.GetString(LastOpenedFileKey, "");
        if (!string.IsNullOrEmpty(lastFile) && File.Exists(lastFile))
        {
            try
            {
                string savedJson = File.ReadAllText(lastFile);
                var savedData = JsonConvert.DeserializeObject<WorldPanelData>(savedJson);
                var currentData = GetCurrentWorldData();

                if (HasUnsavedChanges(savedData, currentData))
                {
                    bool shouldSave = EditorUtility.DisplayDialog(
                        "저장되지 않은 변경사항",
                        "World Panel에 저장되지 않은 변경사항이 있습니다. 저장하시겠습니까?",
                        "저장",
                        "저장하지 않음"
                    );

                    if (shouldSave)
                    {
                        SaveToJson();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"데이터 비교 중 오류 발생: {ex.Message}");
            }
        }
    }

    public void CreateGUI()
    {
        var chatGPTAssistant = new ChatGPTAssistant();

        var root = rootVisualElement;
        worldPanelAsset.CloneTree(root);

        var buttonContainer = root.Q<VisualElement>("world");
        allButtons = root.Query<Button>().ToList();

        var genreInput = root.Q<TextField>("genre-input");
        var keywordInput = root.Q<TextField>("keyword-input");
        
        var saveButton = root.Q<Button>("save-button");
        var loadButton = root.Q<Button>("load-button");
        var clearButton = root.Q<Button>("clear-button");

        var loglineCreateButton = root.Q<Button>("logline-create-button");
        var loglineContainer = root.Q<VisualElement>("logline-container");
        var loglineInput = loglineContainer.Q<TextField>("input");

        var mainScrollView = rootVisualElement.Q<ScrollView>("MainScroll");
        var loading = new VisualElement();

        // 기본값 설정을 상수로 변경
        genreInput.value = DefaultGenreText;
        keywordInput.value = DefaultKeywordText;

        genreInput.RegisterCallback<FocusEvent>(evt =>
        {
            if (genreInput.value == DefaultGenreText)
            {
                genreInput.value = "";
            }
        });

        keywordInput.RegisterCallback<FocusEvent>(evt =>
        {
            if (keywordInput.value == DefaultKeywordText)
            {
                keywordInput.value = "";
            }
        });

        genreInput.RegisterCallback<BlurEvent>(evt =>
        {
            if (string.IsNullOrEmpty(genreInput.value))
            {
                genreInput.value = DefaultGenreText;
            }
        });

        keywordInput.RegisterCallback<BlurEvent>(evt =>
        {
            if (string.IsNullOrEmpty(keywordInput.value))
            {
                keywordInput.value = DefaultKeywordText;
            }
        });

        // 로그라인 생성 버튼 클릭
        loglineCreateButton.clicked += async () =>
        {
            List<string> genres = genreInput.value.Split(',').ToList();
            List<string> keywords = keywordInput.value.Split(',').ToList();
            loglineInput.value = "Creating...";

            string logline = "";

            await chatGPTAssistant.GenerateLoglineAsync(genres, keywords, 
            (chunk) => 
            {
                logline += chunk;
                //loglineInput.value = logline;
            });

            // 일부러 출력에 딜레이를 줌
            DelayedDisplayer displayer = new DelayedDisplayer();
            await displayer.DisplayAdaptive(logline, (display) =>
            {
                loglineInput.value = display;
            });

            //SavePanelData();
        };

        var mainCharacterCreateButton = root.Q<Button>("main-character-create-button");
        var mainCaharacterTemplateCreateButton = root.Q<Button>("main-character-template-create-button");
        var mainCharacterContainer = root.Q<VisualElement>("main-character-container");

        // 주연프로필 생성 버튼 클릭
        mainCharacterCreateButton.clicked += async () =>
        {
            mainCharacterContainer.Clear();
            mainCharacters.Clear();
            
            mainCharacterContainer.style.alignItems = Align.Center;
            var loading = CreateLoading();
            mainCharacterContainer.Add(loading);

            string logline = loglineInput.value;
            try
            {
                (bool success, string result) = await chatGPTAssistant.GenerateMainCharacterAsync(logline, 
                (chunk) =>
                {
                    Debug.Log("Main Character:  \n\n " + chunk);
                    List<WorldCharacter> characters = JsonConvert.DeserializeObject<List<WorldCharacter>>(chunk);
                    foreach (var character in characters)
                    {
                        // 캐릭터 UI 추가
                        AddMainCharacterUI(character, true);
                    }
                });
                
                if (!success)
                {
                    throw new Exception($"메인 캐릭터 생성 실패: {result}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Main Character 생성 중 오류 발생: {ex.Message}");
            }
            finally
            {
                if (mainCharacterContainer.Contains(loading))
                {
                    mainCharacterContainer.Remove(loading);
                    mainCharacterContainer.style.alignItems = new StyleEnum<Align>(StyleKeyword.Null);
                }
            }
        };

        // 주연프로필 옆 + 버튼 클릭
        mainCaharacterTemplateCreateButton.clicked += () =>
        {
            AddMainCharacterUI();
        };

        var mainCharacterPlotCreateButton = root.Q<Button>("main-character-plot-create-button");
        var mainCharacterPlotContainer = root.Q<VisualElement>("main-character-plot-container");

        // 주연플롯 생성 버튼 클릭
        mainCharacterPlotCreateButton.clicked += async () =>
        {
            mainCharacterPlotContainer.Clear();
            mainCharacterPlots.Clear();
            
            var tasks = new List<Task>();
            var processors = new List<QueueProcessor>();  // 프로세서들을 추적

            foreach (var mainCharacter in mainCharacters)
            {
                var mainCharacterPlot = characterPlotAsset.Instantiate();
                mainCharacterPlotContainer.Add(mainCharacterPlot);
                mainCharacterPlot.Q<Label>("name").text = mainCharacter.basicInformation.name;
                mainCharacterPlot.Q<TextField>("input").value = "Creating...";

                // 새로운 WorldCharacterPlot 객체 생성 및 리스트에 추가
                var plot = new WorldCharacterPlot(mainCharacterPlot, mainCharacter.basicInformation.name, "");
                mainCharacterPlots.Add(plot);

                var processor = new QueueProcessor();
                processors.Add(processor);
                
                StringBuilder plotBuilder = new StringBuilder();
                Func<string, Task> displayFunction = chunk =>
                {
                    plotBuilder.Append(chunk);
                    string currentPlot = plotBuilder.ToString();
                    mainCharacterPlot.Q<TextField>("input").value = currentPlot;
                    plot.Plot = currentPlot;  // 플롯 내용 업데이트
                    return Task.CompletedTask;
                };

                tasks.Add(chatGPTAssistant.GenerateMainCharacterPlotAsync(
                    loglineInput.value, 
                    mainCharacter.ToString(),
                    chunk => processor.EnqueueAndProcess(chunk, displayFunction)
                ));
            }

            // AI 응답 생성 완료 대기
            await Task.WhenAll(tasks);
            
            // 모든 큐의 처리가 완료될 때까지 대기
            while (processors.Any(p => p.isProcessing))
            {
                await Task.Delay(100);  // 적절한 간격으로 체크
            }
        };

        var subCharacterCreateButton = root.Q<Button>("sub-character-create-button");
        var subCharacterTemplateCreateButton = root.Q<Button>("sub-character-template-create-button");
        var subCharacterContainer = root.Q<VisualElement>("sub-character-container");

        // 조연프로필 생성 버튼 클릭
        subCharacterCreateButton.clicked += async () =>
        {
            subCharacterContainer.Clear();
            subCharacters.Clear();

            subCharacterContainer.style.alignItems = Align.Center;
            var loading = CreateLoading();
            subCharacterContainer.Add(loading);

            try
            {
                string logline = loglineInput.value;
                string characterProfiles = string.Join("\n\n", mainCharacters.Select(mc => mc.ToString()).ToList());

                (bool success, string result) = await chatGPTAssistant.GenerateSubCharacterAsync(logline, characterProfiles,
                (chunk) =>
                {
                    List<WorldCharacter> characters = JsonConvert.DeserializeObject<List<WorldCharacter>>(chunk);
                    foreach (var character in characters)
                    {
                        // 캐릭터 UI 추가
                        AddSubCharacterUI(character);
                    }
                });

                if (!success)
                {
                    throw new Exception($"조연 캐릭터 생성 실패: {result}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Sub Character 생성 중 오류 발생: {ex.Message}");
            }
            finally
            {
                if (subCharacterContainer.Contains(loading))
                {
                    subCharacterContainer.Remove(loading);
                    subCharacterContainer.style.alignItems = new StyleEnum<Align>(StyleKeyword.Null);
                }
            }
        };

        // 조연프로필 옆 + 버튼 클릭
        subCharacterTemplateCreateButton.clicked += () =>
        {
            AddSubCharacterUI();
        };

        var subCharacterPlotCreateButton = root.Q<Button>("sub-character-plot-create-button");
        var subCharacterPlotContainer = root.Q<VisualElement>("sub-character-plot-container");

        // 조연플롯 생성 버튼 클릭
        subCharacterPlotCreateButton.clicked += async () =>
        {
            subCharacterPlotContainer.Clear();
            subCharacterPlots.Clear();
            
            var tasks = new List<Task>();
            var processors = new List<QueueProcessor>();

            foreach (var subCharacter in subCharacters)
            {
                var subCharacterPlot = characterPlotAsset.Instantiate();
                subCharacterPlotContainer.Add(subCharacterPlot);
                subCharacterPlot.Q<Label>("name").text = subCharacter.basicInformation.name;
                subCharacterPlot.Q<TextField>("input").value = "Creating...";

                // 새로운 WorldCharacterPlot 객체 생성 및 리스트에 추가
                var plot = new WorldCharacterPlot(subCharacterPlot, subCharacter.basicInformation.name, "");
                subCharacterPlots.Add(plot);

                var processor = new QueueProcessor();
                processors.Add(processor);
                
                StringBuilder plotBuilder = new StringBuilder();
                Func<string, Task> displayFunction = chunk =>
                {
                    plotBuilder.Append(chunk);
                    string currentPlot = plotBuilder.ToString();
                    subCharacterPlot.Q<TextField>("input").value = currentPlot;
                    plot.Plot = currentPlot;  // 플롯 내용 업데이트
                    return Task.CompletedTask;
                };

                tasks.Add(chatGPTAssistant.GenerateSubCharacterPlotAsync(
                    loglineInput.value, 
                    subCharacter.ToString(),
                    chunk => processor.EnqueueAndProcess(chunk, displayFunction)
                ));
            }

            // AI 응답 생성 완료 대기
            await Task.WhenAll(tasks);
            
            // 모든 큐의 처리가 완료될 때까지 대기
            while (processors.Any(p => p.isProcessing))
            {
                await Task.Delay(100);
            }
        };
    
        // 저장 버튼 클릭
        saveButton.clicked += () =>
        {
            if (SaveToJson())
            {
                EditorUtility.DisplayDialog("저장 완료", "World Panel 데이터가 성공적으로 저장되었습니다.", "확인");
            }
        };

        // 불러오기 버튼 클릭
        loadButton.clicked += () =>
        {
            if (LoadFromJson())
            {
                EditorUtility.DisplayDialog("불러오기 완료", "World Panel 데이터를 성공적으로 불러왔습니다.", "확인");
            }
        };

        // 초기화 버튼 클릭
        clearButton.clicked += () =>
        {
            Debug.LogWarning("clear");
            ClearPanelData();
        };

        // GUI 생성 완료 후 마지막 저장 파일 로드
        string lastFile = EditorPrefs.GetString(LastOpenedFileKey, "");
        if (!string.IsNullOrEmpty(lastFile) && File.Exists(lastFile))
        {
            LoadFromJson(lastFile);
        }
    }

    private void AddMainCharacterUI(WorldCharacter character = null, bool autoScroll = true)
    {
        var mainCharacterContainer = rootVisualElement.Q<VisualElement>("main-character-container");
        var mainScrollView = rootVisualElement.Q<ScrollView>("MainScroll");
        var main = mainCharacterAsset.Instantiate();

        mainCharacterContainer.Add(main);

        var mainCharacter = new WorldMainCharacter(main);

        if (character != null)
        {
            mainCharacter.UpdateUIFromWorldCharacter(character);
        }

        mainCharacter.onRemove = () =>
        {
            mainCharacters.Remove(mainCharacter);
            mainCharacterContainer.Remove(main);
            ScrollUp(mainScrollView);
        };

        mainCharacter.onRefresh = async () =>
        {
            var buttons = main.Query<Button>().ToList();
            var refreshButton = buttons.FirstOrDefault(b => b.name == "refresh");
            var removeButton = buttons.FirstOrDefault(b => b.name == "remove");
            var buttonContainer = refreshButton?.parent;  // 버튼의 부모 컨테이너

            if (refreshButton != null) refreshButton.SetEnabled(false);
            if (removeButton != null) removeButton.SetEnabled(false);

            // 로딩 아이콘 추가
            var loading = CreateLoading();
            loading.style.width = 20;
            loading.style.height = 20;
            loading.style.marginLeft = 10;
            buttonContainer?.Add(loading);

            try
            {
                ChatGPTAssistant chatGPTAssistant = new ChatGPTAssistant();
                string logline = rootVisualElement.Q<TextField>("input").value;
                string otherMainCharacters = string.Join("\n\n", mainCharacters.Where(mc => mc != mainCharacter).Select(mc => mc.ToString()).ToList());

                await chatGPTAssistant.GenerateMainCharacterSingleAsync(logline, otherMainCharacters, mainCharacter.ToString(),
                (chunk) =>
                {
                    var json = JsonConvert.DeserializeObject<List<WorldCharacter>>(chunk);
                    if (json.Count > 1)
                    {
                        Debug.LogWarning("More than one character generated.");
                    }
                    mainCharacter.UpdateUIFromWorldCharacter(json[0]);
                });
            }
            finally
            {
                if (refreshButton != null) refreshButton.SetEnabled(true);
                if (removeButton != null) removeButton.SetEnabled(true);
                if (loading != null && buttonContainer != null)
                {
                    buttonContainer.Remove(loading);
                }
            }
        };

        mainCharacters.Add(mainCharacter);
        
        if (autoScroll)
        {
            SmoothScrollTo(mainScrollView, main);
        }
    }

    private void AddSubCharacterUI(WorldCharacter character = null, bool autoScroll = true)
    {
        var subCharacterContainer = rootVisualElement.Q<VisualElement>("sub-character-container");
        var mainScrollView = rootVisualElement.Q<ScrollView>("MainScroll");
        var sub = subCharacterAsset.Instantiate();

        subCharacterContainer.Add(sub);

        var subCharacter = new WorldSubCharacter(sub);

        if (character != null)
        {
            subCharacter.UpdateUIFromWorldCharacter(character);
        }

        subCharacter.onRemove = () =>
        {
            subCharacters.Remove(subCharacter);
            subCharacterContainer.Remove(sub);
            ScrollUp(mainScrollView);
        };

        subCharacter.onRefresh = async () =>
        {
            var buttons = sub.Query<Button>().ToList();
            var refreshButton = buttons.FirstOrDefault(b => b.name == "refresh");
            var removeButton = buttons.FirstOrDefault(b => b.name == "remove");
            var buttonContainer = refreshButton?.parent;  // 버튼의 부모 컨테이너

            if (refreshButton != null) refreshButton.SetEnabled(false);
            if (removeButton != null) removeButton.SetEnabled(false);

            // 로딩 아이콘 추가
            var loading = CreateLoading();
            loading.style.width = 20;
            loading.style.height = 20;
            loading.style.marginLeft = 10;
            buttonContainer?.Add(loading);

            try
            {
                ChatGPTAssistant chatGPTAssistant = new ChatGPTAssistant();
                string logline = rootVisualElement.Q<TextField>("input").value;
                string mainCharacters = string.Join("\n\n", this.mainCharacters.Select(mc => mc.ToString()).ToList());
                string otherSubCharacters = string.Join("\n\n", subCharacters.Where(sc => sc != subCharacter).Select(sc => sc.ToString()).ToList());

                await chatGPTAssistant.GenerateSubCharacterSingleAsync(logline, mainCharacters, subCharacter.ToString(),
                (chunk) =>
                {
                    var json = JsonConvert.DeserializeObject<List<WorldCharacter>>(chunk);
                    if (json.Count > 1)
                    {
                        Debug.LogWarning("More than one character generated.");
                    }
                    subCharacter.UpdateUIFromWorldCharacter(json[0]);
                });
            }
            finally
            {
                if (refreshButton != null) refreshButton.SetEnabled(true);
                if (removeButton != null) removeButton.SetEnabled(true);
                if (loading != null && buttonContainer != null)
                {
                    buttonContainer.Remove(loading);
                }
            }
        };

        subCharacters.Add(subCharacter);
        if (autoScroll)
        {
            SmoothScrollTo(mainScrollView, sub);
        }
    }

    private void ClearPanelData()
    {
        var root = rootVisualElement;
        
        // 장르와 키워드를 기본값으로 설정
        root.Q<TextField>("genre-input").value = DefaultGenreText;
        root.Q<TextField>("keyword-input").value = DefaultKeywordText;

        // 로그라인 초기화
        root.Q<TextField>("input").value = "";

        // 캐릭터 컨테이너 초기화
        var mainCharacterContainer = root.Q<VisualElement>("main-character-container");
        var subCharacterContainer = root.Q<VisualElement>("sub-character-container");
        mainCharacterContainer.Clear();
        subCharacterContainer.Clear();

        // 캐릭터 플롯 컨테이너 초기화
        var mainCharacterPlotContainer = root.Q<VisualElement>("main-character-plot-container");
        var subCharacterPlotContainer = root.Q<VisualElement>("sub-character-plot-container");
        mainCharacterPlotContainer.Clear();
        subCharacterPlotContainer.Clear();

        // 리스트 초기화
        mainCharacters.Clear();
        mainCharacterPlots.Clear();
        subCharacters.Clear();
        subCharacterPlots.Clear();
    }

    private void SmoothScrollTo(ScrollView scrollView, VisualElement targetElement, float duration = 0.3f)
    {
        if (targetElement == null)
        {
            Debug.LogError("targetElement is null.");
            return;
        }

        // Ensure the element is part of the contentContainer
        if (!scrollView.contentContainer.Contains(targetElement))
        {
            Debug.LogError("targetElement is not a child of the ScrollView's contentContainer.");
            return;
        }

        scrollView.schedule.Execute(() =>
        {
            // Ensure layout is updated
            var targetLocalY = scrollView.contentContainer.WorldToLocal(targetElement.worldBound.center).y;
            if (float.IsNaN(targetLocalY))
            {
                Debug.LogError("targetLocalY is NaN even after layout update.");
                return;
            }

            var start = scrollView.scrollOffset.y;
            var end = targetLocalY - scrollView.layout.height *1/8 - targetElement.layout.height / 2;

            float elapsed = 0;

            scrollView.schedule.Execute(() =>
            {
                elapsed += Mathf.Max(Time.deltaTime, 0.016f);
                float t = Mathf.Clamp01(elapsed / duration);
                scrollView.scrollOffset = new Vector2(0, Mathf.Lerp(start, end, t));
            }).Every(16).Until(() => elapsed >= duration);
        }).ExecuteLater(100);
    }
    private void ScrollUp(ScrollView scrollView, float scrollAmount = 30f)
    {
        var currentOffset = scrollView.scrollOffset.y;
        var newOffset = Mathf.Max(0, currentOffset - scrollAmount);
        scrollView.scrollOffset = new Vector2(0, newOffset);

    }

    public enum TaskType
    {
        Logline,
        MainCharacter,
        MainCharacterPlot,
        SubCharacter,
        SubCharacterPlot
    }

    private VisualElement CreateLoading()
    {
        var loading = new VisualElement();
        loading.AddToClassList("loading");
        loading.schedule.Execute(() =>
        {
            var rotate = loading.style.rotate;
            loading.style.rotate = new StyleRotate(new Rotate(rotate.value.angle.value + 2f));
        }).ForDuration(long.MaxValue).Every(1);
        return loading;
    }

    public class DelayedDisplayer
    {
        public async Task Display(string chunk, Action<string> updateUI, float delay = 0.001f)
        {
            if (string.IsNullOrEmpty(chunk)) return;

            StringBuilder currentDisplay = new StringBuilder();
            foreach (char c in chunk)
            {
                currentDisplay.Append(c);
                updateUI?.Invoke(currentDisplay.ToString());
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        public async Task DisplayAdaptive(string chunk, Action<string> updateUI, int initialUnitSize = 1, int finalUnitSize = 5, int threshold = 10, float delay = 0.01f)
        {
            if (string.IsNullOrEmpty(chunk)) return;

            StringBuilder currentDisplay = new StringBuilder();
            int unitSize = initialUnitSize;

            for (int i = 0; i < chunk.Length; i += unitSize)
            {
                if (i >= threshold)
                {
                    unitSize = finalUnitSize; // 단위 크기 변경
                }

                int length = Math.Min(unitSize, chunk.Length - i);
                currentDisplay.Append(chunk.Substring(i, length));
                updateUI?.Invoke(currentDisplay.ToString());
                
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }
    }

    public WorldPanelData GetCurrentWorldData()
    {
        var genreValue = rootVisualElement.Q<TextField>("genre-input").value;
        var keywordValue = rootVisualElement.Q<TextField>("keyword-input").value;

        return new WorldPanelData
        {
            // 기본값인 경우 빈 문자열로 처리
            genres = genreValue == DefaultGenreText ? "" : genreValue,
            keywords = keywordValue == DefaultKeywordText ? "" : keywordValue,
            logline = rootVisualElement.Q<TextField>("input").value,
            mainCharacters = mainCharacters.Select(mc => new WorldCharacter
            {
                basicInformation = mc.basicInformation,
                appearance = mc.appearance,
                personality = mc.personality,
                values = mc.values,
                relationship = mc.relationship
            }).ToList(),
            mainCharacterPlots = mainCharacterPlots.Select(mp => new WorldCharacterPlot
            {
                Name = mp.Name,
                Plot = mp.Plot
            }).ToList(),
            subCharacters = subCharacters.Select(sc => new WorldCharacter
            {
                basicInformation = sc.basicInformation,
                appearance = sc.appearance,
                personality = sc.personality
            }).ToList(),
            subCharacterPlots = subCharacterPlots.Select(sp => new WorldCharacterPlot
            {
                Name = sp.Name,
                Plot = sp.Plot
            }).ToList()
        };
    }

    public bool SaveToJson(string filePath = null)
    {
        var worldData = GetCurrentWorldData();
        string json = JsonConvert.SerializeObject(worldData, Formatting.Indented);

        if (string.IsNullOrEmpty(filePath))
        {
            string previousFilePath = EditorPrefs.GetString(LastOpenedFileKey, "WorldData");
            previousFilePath = Path.GetFileNameWithoutExtension(previousFilePath);  

            filePath = EditorUtility.SaveFilePanel(
                "Save World Data", 
                "", 
                previousFilePath,  // 기본 파일명을 .world 확장자로 변경
                "world"  // 필터를 .world로 변경
            );
            if (string.IsNullOrEmpty(filePath))
                return false;
        }
        
        try
        {
            File.WriteAllText(filePath, json);
            EditorPrefs.SetString(LastOpenedFileKey, filePath);  // 파일 경로 저장
            Debug.Log($"World data saved to: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save world data: {ex.Message}");
            return false;
        }
    }

    public bool LoadFromJson(string filePath = null)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = EditorUtility.OpenFilePanel(
                "Load World Data", 
                "", 
                "world"  // 필터를 .world로 변경
            );
            if (string.IsNullOrEmpty(filePath))
                return false;
        }

        if (!File.Exists(filePath))
            return false;

        try
        {
            string json = File.ReadAllText(filePath);
            var worldData = JsonConvert.DeserializeObject<WorldPanelData>(json);

            // UI 업데이트
            rootVisualElement.Q<TextField>("genre-input").value = worldData.genres;
            rootVisualElement.Q<TextField>("keyword-input").value = worldData.keywords;
            rootVisualElement.Q<TextField>("input").value = worldData.logline;

            // 캐릭터 컨테이너 초기화
            var mainCharacterContainer = rootVisualElement.Q<VisualElement>("main-character-container");
            var subCharacterContainer = rootVisualElement.Q<VisualElement>("sub-character-container");
            mainCharacterContainer.Clear();
            subCharacterContainer.Clear();
            mainCharacters.Clear();
            subCharacters.Clear();

            // 캐릭터 데이터 로드
            foreach (var mc in worldData.mainCharacters)
            {
                AddMainCharacterUI(mc, false);
            }

            foreach (var sc in worldData.subCharacters)
            {
                AddSubCharacterUI(sc, false);
            }

            // 플롯 데이터 로드
            var mainCharacterPlotContainer = rootVisualElement.Q<VisualElement>("main-character-plot-container");
            var subCharacterPlotContainer = rootVisualElement.Q<VisualElement>("sub-character-plot-container");
            mainCharacterPlotContainer.Clear();
            subCharacterPlotContainer.Clear();
            mainCharacterPlots.Clear();
            subCharacterPlots.Clear();

            foreach (var plot in worldData.mainCharacterPlots)
            {
                var plotElement = characterPlotAsset.Instantiate();
                mainCharacterPlotContainer.Add(plotElement);
                plotElement.Q<Label>("name").text = plot.Name;
                plotElement.Q<TextField>("input").value = plot.Plot;

                var plotObj = new WorldCharacterPlot(plotElement, plot.Name, plot.Plot);
                mainCharacterPlots.Add(plotObj);

                // TextField 변경 이벤트 추가
                plotElement.Q<TextField>("input").RegisterCallback<ChangeEvent<string>>(evt =>
                {
                    plotObj.Plot = evt.newValue;
                });
            }

            foreach (var plot in worldData.subCharacterPlots)
            {
                var plotElement = characterPlotAsset.Instantiate();
                subCharacterPlotContainer.Add(plotElement);
                plotElement.Q<Label>("name").text = plot.Name;
                plotElement.Q<TextField>("input").value = plot.Plot;

                var plotObj = new WorldCharacterPlot(plotElement, plot.Name, plot.Plot);
                subCharacterPlots.Add(plotObj);

                // TextField 변경 이벤트 추가
                plotElement.Q<TextField>("input").RegisterCallback<ChangeEvent<string>>(evt =>
                {
                    plotObj.Plot = evt.newValue;
                });
            }

            EditorPrefs.SetString(LastOpenedFileKey, filePath);  // 파일 경로 저장
            Debug.Log($"World data loaded from: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load world data: {ex.Message}");
            return false;
        }
    }

    private bool HasUnsavedChanges(WorldPanelData savedData, WorldPanelData currentData)
    {
        // 기본 필드 비교
        if (savedData.genres != currentData.genres ||
            savedData.keywords != currentData.keywords ||
            savedData.logline != currentData.logline)
            return true;

        // 주연 캐릭터 수 비교
        if (savedData.mainCharacters.Count != currentData.mainCharacters.Count)
            return true;

        // 주연 캐릭터 내용 비교
        for (int i = 0; i < savedData.mainCharacters.Count; i++)
        {
            var saved = savedData.mainCharacters[i];
            var current = currentData.mainCharacters[i];

            if (!AreCharactersEqual(saved, current))
                return true;
        }

        // 조연 캐릭터 수 비교
        if (savedData.subCharacters.Count != currentData.subCharacters.Count)
            return true;

        // 조연 캐릭터 내용 비교
        for (int i = 0; i < savedData.subCharacters.Count; i++)
        {
            if (!AreCharactersEqual(savedData.subCharacters[i], currentData.subCharacters[i]))
                return true;
        }

        // 플롯 비교
        if (!ArePlotsEqual(savedData.mainCharacterPlots, currentData.mainCharacterPlots) ||
            !ArePlotsEqual(savedData.subCharacterPlots, currentData.subCharacterPlots))
            return true;

        return false;
    }

    private bool AreCharactersEqual(WorldCharacter a, WorldCharacter b)
    {
        if (a == null || b == null || a.basicInformation == null || b.basicInformation == null) 
            return false;

        // BasicInformation 비교
        var basicInfoEqual = 
            a.basicInformation.name == b.basicInformation.name &&
            a.basicInformation.age == b.basicInformation.age &&
            a.basicInformation.gender == b.basicInformation.gender &&
            a.basicInformation.occupation == b.basicInformation.occupation;

        // 다른 필드들 비교 (null 체크 추가)
        var otherFieldsEqual =
            (a.appearance?.ToString() ?? "") == (b.appearance?.ToString() ?? "") &&
            (a.personality?.ToString() ?? "") == (b.personality?.ToString() ?? "") &&
            (a.values?.ToString() ?? "") == (b.values?.ToString() ?? "") &&
            (a.relationship?.ToString() ?? "") == (b.relationship?.ToString() ?? "");

        return basicInfoEqual && otherFieldsEqual;
    }

    private bool ArePlotsEqual(List<WorldCharacterPlot> a, List<WorldCharacterPlot> b)
    {
        if (a == null || b == null) return a == b;
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] == null || b[i] == null) return false;
            if ((a[i].Name ?? "") != (b[i].Name ?? "") || 
                (a[i].Plot ?? "") != (b[i].Plot ?? ""))
                return false;
        }

        return true;
    }
}

#endif