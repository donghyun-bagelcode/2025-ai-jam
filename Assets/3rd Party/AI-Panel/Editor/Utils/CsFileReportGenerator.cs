using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class CsFileReportGenerator
{
    /// <summary>
    /// 주어진 루트 폴더 내의 모든 .cs 파일을 재귀적으로 탐색하고,
    /// 폴더 구조 트리와 각 파일의 내용을 지정된 형식으로 출력하여 파일에 저장합니다.
    /// </summary>
    /// <param name="rootFolder">탐색을 시작할 루트 폴더의 경로.</param>
    /// <param name="outputFilePath">생성된 보고서를 저장할 파일의 경로.</param>
    public static void GenerateCsFileReport(string rootFolder, string outputFilePath)
    {
        if (!Directory.Exists(rootFolder))
        {
            Debug.LogWarning($"The directory '{rootFolder}' does not exist.");
            throw new DirectoryNotFoundException($"The directory '{rootFolder}' does not exist.");
        }

        StringBuilder sb = new StringBuilder();

        // Step 1: 폴더 구조 트리 생성
        sb.AppendLine("## 파일 구조 트리");
        sb.AppendLine();
        string tree = GetDirectoryTree(rootFolder);
        sb.AppendLine(tree);
        sb.AppendLine();

        // Step 2: 각 .cs 파일의 내용 수집
        sb.AppendLine("## 파일 내용");
        sb.AppendLine();

        var csFiles = Directory.GetFiles(rootFolder, "*.cs", SearchOption.AllDirectories);
        foreach (var csFile in csFiles)
        {
            string relativePath = Path.GetRelativePath(rootFolder, csFile);
            sb.AppendLine(relativePath);
            sb.AppendLine("```csharp");
            string code = File.ReadAllText(csFile);
            sb.AppendLine(code);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        // Step 3: 결과를 파일에 저장
        try
        {
            File.WriteAllText(outputFilePath, sb.ToString(), Encoding.UTF8);
            Debug.LogWarning($"Report successfully generated at '{outputFilePath}'.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error writing to file: {ex.Message}");
        }
    }

    /// <summary>
    /// 주어진 루트 폴더의 폴더 구조 트리를 문자열로 생성합니다.
    /// </summary>
    /// <param name="rootFolder">루트 폴더의 경로.</param>
    /// <returns>폴더 구조 트리를 나타내는 문자열.</returns>
    private static string GetDirectoryTree(string rootFolder)
    {
        StringBuilder sb = new StringBuilder();
        DirectoryInfo rootDir = new DirectoryInfo(rootFolder);
        BuildDirectoryTree(rootDir, "", sb, true);
        return sb.ToString();
    }

    /// <summary>
    /// 재귀적으로 폴더 구조 트리를 빌드합니다.
    /// </summary>
    /// <param name="directory">현재 탐색 중인 폴더.</param>
    /// <param name="indent">현재 들여쓰기 문자열.</param>
    /// <param name="sb">트리 구조를 저장할 StringBuilder.</param>
    /// <param name="isLast">현재 폴더가 부모 폴더의 마지막 항목인지 여부.</param>
    private static void BuildDirectoryTree(DirectoryInfo directory, string indent, StringBuilder sb, bool isLast)
    {
        sb.Append(indent);
        sb.Append(isLast ? "└── " : "├── ");
        sb.AppendLine(directory.Name + "/");

        // 다음 들여쓰기를 위한 문자열 업데이트
        string newIndent = indent + (isLast ? "    " : "│   ");

        // 현재 폴더 내의 .cs 파일과 하위 폴더 가져오기
        var files = directory.GetFiles("*.cs");
        var subDirs = directory.GetDirectories();

        int total = files.Length + subDirs.Length;
        for (int i = 0; i < total; i++)
        {
            bool lastEntry = (i == total - 1);
            if (i < files.Length)
            {
                // 파일인 경우
                var file = files[i];
                sb.Append(newIndent);
                sb.Append(lastEntry && subDirs.Length == 0 ? "└── " : "├── ");
                sb.AppendLine(file.Name);
            }
            else
            {
                // 폴더인 경우 재귀 호출
                var subDir = subDirs[i - files.Length];
                BuildDirectoryTree(subDir, newIndent, sb, lastEntry);
            }
        }
    }
}