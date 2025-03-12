using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

public class ChatMessageService
{
    private readonly string chatFilePath;
    private List<ChatMessage> chatMessages;

    public ChatMessageService()
    {
        chatFilePath = Path.Combine(Application.persistentDataPath, "ChatMessages.json");
        //Debug.Log($"Chat messages file path: {chatFilePath}");  // 경로 확인용
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

                    // 로드된 메시지들 중 가장 큰 id 값을 찾아서 nextId 초기화
                    if (chatMessages.Count > 0)
                    {
                        int maxId = chatMessages.Max(m => m.id);
                        ChatMessage.ResetNextId(maxId + 1);
                    }

                    foreach (var message in chatMessages)
                    {
                        if (message.message == null)
                        {
                            continue;
                        }

                        if (message.codeFiles == null)
                        {
                            try
                            {
                                AIResponse aiResponse = new AIResponse(message.message);
                                aiResponse.ParseResponse();
                                if (aiResponse.Codes != null && aiResponse.Codes.Count > 0)
                                {
                                    message.codeFiles = new CodeFileListWrapper(aiResponse.Codes);
                                    message.message = aiResponse.Message;
                                }
                                
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Message Loading : AIResponse 파싱 실패: {ex.Message}");
                            }
                        }
                    }
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
    public void AddMessage(AssistantType type, string message, AIResponse response)
    {
        // 메시지가 없는 상태라면 nextId 리셋
        if (chatMessages.Count == 0)
        {
            ChatMessage.ResetNextId(0);
        }
        
        chatMessages.Add(new ChatMessage(type, message, response.Message, response.Codes));
        SaveChatMessages();
    }

    /// <summary>
    /// 사용자 메시지를 추가합니다.
    /// </summary>
    public void AddUserMessage(AssistantType type, string message)
    {
        // 메시지가 없는 상태라면 nextId 리셋
        if (chatMessages.Count == 0)
        {
            ChatMessage.ResetNextId(0);
        }

        chatMessages.Add(new ChatMessage(type, message, null, null));
        SaveChatMessages();
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
            SaveChatMessages();
        }
    }

    /// <summary>
    /// 모든 채팅 메시지를 반환합니다.
    /// </summary>
    public List<ChatMessage> GetAllMessages()
    {
        return chatMessages;
    }

    public void RemoveMessage(int id)
    {
        chatMessages.RemoveAll(m => m.id == id);
        SaveChatMessages();
    }

    /// <summary>
    /// 채팅 메시지를 초기화합니다.
    /// </summary>
    public void ClearChatMessages()
    {
        chatMessages.Clear();
        ChatMessage.ResetNextId(0); // nextId 리셋
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