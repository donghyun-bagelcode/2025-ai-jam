using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Dunward.Capricorn;
using Newtonsoft.Json;
using Unity.VisualScripting;

public class NovelManager : MonoBehaviour
{
    [SerializeField]
    private ScriptMachine mainFlow;

    [SerializeField]
    private CapricornRunner runner;

    [SerializeField]
    private NavigationManager navigationManager;

    [SerializeField]
    private NovelHistory history;

    [SerializeField]
    private GameObject dialogueArea;

    [SerializeField]
    private GameObject navigationArea;

    [SerializeField]
    private Button interactionPanel;
    
    [SerializeField]
    private Transform selectionArea;

    [SerializeField]
    private GameObject selectionPrefab;

    private bool isReady = false;
    private string currentSceneName;
    public float playTime = 0f;

    public void Initialize()
    {
        navigationManager.Initialize();

        // runner.Load(asset.text);
        runner.bindingInteraction = interactionPanel.onClick;

        runner.onBeforeTextTyping += (name, subName, script) =>
        {
            history.AddHistory(name, subName, script);
        };

        runner.onSelectionCreate += (selections) =>
        {
            var buttons = new List<Button>();

            foreach (var selection in selections)
            {
                var button = Instantiate(selectionPrefab, selectionArea).GetComponent<Button>();
                button.GetComponentInChildren<Text>().text = selection;
                buttons.Add(button);

                button.onClick.AddListener(() =>
                {
                    foreach (var button in buttons)
                    {
                        if (button == null) continue;
                        Destroy(button.gameObject);
                    }
                });
            }

            return buttons;
        };
        
        runner.AddCustomCoroutines += (unit, runner, settings, cache, data) => CustomCoroutines(unit, runner, settings, cache, data);

        // StartCoroutine(runner.Run());
        isReady = true;
    }

    private IEnumerator CustomCoroutines(CoroutineUnit unit, CapricornRunner runner, CapricornSettings settings, CapricornCache cache, CapricornData data)
    {
        switch (unit)
        {
            case SetActiveDialogueUnit setActiveDialogueUnit:
                yield return setActiveDialogueUnit.Execute(dialogueArea);
                break;

            case SetActiveNavigationUnit setActiveNavigationUnit:
                yield return setActiveNavigationUnit.Execute(navigationArea);
                break;
        }
    }

    public IEnumerator Load(TextAsset asset)
    {
        currentSceneName = asset.name;
        runner.Load(asset.text);
        yield return runner.Run();
    }

    public void SaveGame(int index)
    {
        var data = GetComponent<CapricornData>();

        var saveData = new NovelSaveData();
        saveData.sceneName = currentSceneName;
        saveData.playTime = playTime;

        foreach (var variable in data.variables)
        {
            saveData.variables.Add(variable.Key, variable.Value);
        }

        PlayerPrefs.SetString($"SaveData{index}", JsonConvert.SerializeObject(saveData));
    }

    public void LoadGame(int index)
    {
        Reset();

        var data = PlayerPrefs.GetString($"SaveData{index}", string.Empty);
        var saveData = JsonConvert.DeserializeObject<NovelSaveData>(data);

        mainFlow.StopAllCoroutines();
        playTime = saveData.playTime;
        var cd = GetComponent<CapricornData>();
        
        foreach (var variable in saveData.variables)
        {
            cd.variables[variable.Key] = new Ref<int>(variable.Value);
        }

        CustomEvent.Trigger(ScreenFlow.Instance.gameObject, "OnChangeNovelScreen");
        CustomEvent.Trigger(mainFlow.gameObject, "LoadNovel", saveData.sceneName);
    }

    public void Clear()
    {
        runner.Clear();
    }

    public void Reset()
    {
        foreach (Transform transform in selectionArea.transform)
        {
            Destroy(transform.gameObject);
        }

        runner.Clear();
        runner.ClearVariables();
        history.Clear();
        playTime = 0f;
    }

    private void Update()
    {
        playTime += Time.deltaTime;
    }
}
