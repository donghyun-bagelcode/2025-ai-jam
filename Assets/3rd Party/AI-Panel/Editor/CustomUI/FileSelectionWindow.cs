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