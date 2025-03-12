using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[System.Serializable]
public class ChatMessage
{
    public int id;
    public AssistantType type = AssistantType.Writing;
    public string request = "";
    public string message = "";
    public CodeFileListWrapper codeFiles = null;
    public int currentCodePage = 0;
    public Vector2 userMessageScrollPosition = Vector2.zero;
    public Vector2 aiMessageScrollPosition = Vector2.zero;
    public Vector2 codeScrollPosition = Vector2.zero;

    private static int nextId = 0;

    public ChatMessage(AssistantType type, string request, string message, List<CodeFile> codes = null)
    {
        this.id = nextId++;
        this.type = type;
        this.request = request;
        this.message = message;
        if (codes != null)
        {
            this.codeFiles = new CodeFileListWrapper(codes);
        }
    }

    public static void ResetNextId(int newNextId)
    {
        nextId = newNextId;
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