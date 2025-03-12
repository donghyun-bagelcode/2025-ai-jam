using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

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

    public void ParseResponse()
    {
        Debug.Log("Parsing csv response...");
        string pattern = @"(?:(?<title>.+?)\n)?```(?<blockType>\S+)?\s*\n(?<contents>[\s\S]*?)```";
        (Message, Codes) = ExtractTextAndCodes(this.Content, pattern);
    }

    private (string, List<CodeFile>) ExtractTextAndCodes(string inputText, string pattern)
    {
        // 수정된 정규식: 파일명이 없을 경우를 처리

        RegexOptions options = RegexOptions.Multiline;

        // 추출된 코드를 저장할 리스트
        List<CodeFile> codeBlocks = new();
        // 남은 텍스트를 저장할 리스트
        List<string> remainingTexts = new();

        int lastPosition = 0;
        int codeBlockCount = 0;

        // 모든 코드를 하나의 문자열로 저장
        string filename = "";
        string totalCode = "";

        MatchCollection matches = Regex.Matches(inputText, pattern, options);
        
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                string extractedCode = match.Groups["contents"].Value;
                string title = match.Groups["title"].Success ? match.Groups["title"].Value : "noname";
                totalCode += $"{extractedCode}\n\n";
                
                // 코드 블록 이전의 텍스트를 남은 텍스트에 추가
                string textBeforeCode = inputText.Substring(lastPosition, match.Index - lastPosition);
                remainingTexts.Add(textBeforeCode);
                remainingTexts.Add($"{title}");
                //remainingTexts.Add($"\n[Scene {codeBlockCount}]");

                // 마지막 매칭 위치 업데이트
                lastPosition = match.Index + match.Length;
            }
        }
        
        if (totalCode != "")
        {
            filename = matches[0].Groups["title"].Success ? matches[0].Groups["title"].Value : "noname";
            codeBlocks.Add(new CodeFile(filename, totalCode.TrimEnd('\n')));
            codeBlockCount++;
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