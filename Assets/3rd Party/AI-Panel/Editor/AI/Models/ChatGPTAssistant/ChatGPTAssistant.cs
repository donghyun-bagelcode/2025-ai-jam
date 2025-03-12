using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Reflection;


public class ChatGPTAssistant : IAIModel
{
#region Async Methods
    public async Task<AIResponse> GenerateResponseAsync(AssistantType assistantType, string input)
    {
        // Retrieve the assistant ID and thread ID from the secure player prefs
        string assistantId = SecurePlayerPrefs.GetString($"ChatGPTAssistant_AssistantId_{assistantType}", "");
        string threadId = SecurePlayerPrefs.GetString($"ChatGPTAssistant_ThreadId_{assistantType}", "");
        string model = SecurePlayerPrefs.GetString($"ChatGPTAssistant_Model_{assistantType}", "gpt-4o");
        float temperature = SecurePlayerPrefs.GetFloat($"ChatGPTAssistant_Temperature_{assistantType}", 1.0f);
        string newInstructions = PromptManager.AssistantInstructions(assistantType);
        try
        {
            return await CreateAndRunAsync(input, assistantId, threadId, model, newInstructions, temperature);
        }
        catch (Exception ex)
        {
            Debug.LogError($"ChatGPTAssistant GenerateResponseAsync 오류: {ex.Message}");
            return new AIResponse("GenerateResponseAsync에서 CreateAndRunAsync 시도 중 오류가 발생했습니다.", false, ex.Message);
        }
    }

    /// <summary>
    /// 메시지 생성, 런 생성, 런 완료 대기, 메시지 수신 단계를 비동기적으로 수행합니다.
    /// </summary>
    /// <param name="input">사용자 입력 텍스트</param>
    /// <returns>AI 응답을 담은 AIResponse 객체</returns>
    public async Task<AIResponse> CreateAndRunAsync(string input, string assistantId, string threadId, string model, string newInstructions = null, float temperature = 0.5f)
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
            return new AIResponse("메시지 생성에 실패했습니다. 잠시 뒤 다시 시도해 주십시오.", false, messageIdOrError);
        }
        string messageId = messageIdOrError;

        // 3. 런 생성
        var (runSuccess, runIdOrError) = await ChatGPTRunAPIAsync.CreateRunAsync(threadId, assistantId, model, newInstructions, temperature);
        if (!runSuccess || string.IsNullOrEmpty(runIdOrError))
        {
            return new AIResponse("런 생성에 실패했습니다.", false, runIdOrError);
        }
        string runId = runIdOrError;

        // 4. 런 완료 대기
        int timeout = SecurePlayerPrefs.GetInt("ChatGPTAssistant_Timeout", 180);
        bool isRunComplete = await ChatGPTRunAPIAsync.WaitForRunCompleteAsync(threadId, runId, timeoutSeconds: timeout);
        if (!isRunComplete)
        {
            Debug.LogWarning("런이 정상적으로 완료되지 않았습니다.");
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

    public async Task<Message> CheckNewMessage(AssistantType assistantType)
    {
        // Retrieve the assistant ID and thread ID from the secure player prefs
        string assistantId = SecurePlayerPrefs.GetString($"ChatGPTAssistant_AssistantId_{assistantType}", "");
        string threadId = SecurePlayerPrefs.GetString($"ChatGPTAssistant_ThreadId_{assistantType}", "");
        string model = SecurePlayerPrefs.GetString($"ChatGPTAssistant_Model_{assistantType}", "gpt-4o");
        float temperature = SecurePlayerPrefs.GetFloat($"ChatGPTAssistant_Temperature_{assistantType}", 1.0f);

        // 5. 최신 메시지 목록 가져오기
        (bool success, MessagesListResponse response) = await ChatGPTMessageAPIAsync.ListMessagesAsync(threadId, limit: 1, order: "desc");
        Debug.Log($"Last Message : {response?.data[0].id}");

        return response.data[0];
    }
#endregion

    public async Task<string> GenerateCompletion(string input, Action<string> onChunkReceived, Action onComplete)
    {
        Debug.Log("Generating Completion...");

        // Model parameter settings
        string model  = "gpt-4o";
        float temperature = 1.0f;
        int maxTokens = 500;
        bool stream = true;

        // system instructions
        string systemInstruction = PromptManager.Completion;

        // Message data
        List<CompletionMessage> messages = new List<CompletionMessage>();
        messages.Add(new CompletionMessage { role = "system", content = systemInstruction });
        messages.Add(new CompletionMessage { role = "user", content = input});
        (bool success, string result) = await ChatGPTCompletionAPIAsync.CreateCompletionAsync(model, messages,
        (chunk) => {
            if (stream)
            {
                // Handle Chunk Json
                CompletionChunk completionChunk = JsonConvert.DeserializeObject<CompletionChunk>(chunk);
                string content = completionChunk.choices[0].delta.Content;
                onChunkReceived?.Invoke(content);
            }
            else
            {
                // Handle Json output
                Completion completion = JsonConvert.DeserializeObject<Completion>(chunk);
                onChunkReceived?.Invoke(completion.choices[0].message.content);
            }
        }, 
        temperature:temperature, 
        maxTokens:maxTokens, 
        stream:stream);

        onComplete?.Invoke();

        return result;
    }

    public async Task<string> GenerateLoglineAsync(List<string> genres, List<string> keyWords, Action<string> onChunkReceived)
    {
        Debug.Log("Generating Logline...");
        // Model parameter settings
        string model  = "gpt-4o";
        float temperature = 0.8f;
        bool stream = true;
        int n = 1;

        // system instructions
        string systemInstruction = PromptManager.Logline;
        string prompt = $"장르: {string.Join(',',genres)}\n키워드: {string.Join(',',keyWords)}";
        
        //Debug.Log($"Debug Logline\n Prompt: {prompt}\n System Instruction: {systemInstruction}");

        // Message data
        List<CompletionMessage> messages = new List<CompletionMessage>();
        messages.Add(new CompletionMessage { role = "system", content = systemInstruction });
        messages.Add(new CompletionMessage { role = "user", content = prompt});

        (bool success, string result) = await ChatGPTCompletionAPIAsync.CreateCompletionAsync(model, messages,
        (chunk) => {
            onChunkReceived?.Invoke(chunk);
        },
        temperature:temperature,
        n:n,
        stream:stream);

        return result;
    }
        
    public async Task<(bool success, string result)> GenerateMainCharacterAsync(string logline, Action<string> onChunkReceived)
    {
        Debug.Log("Generating Main Character...");

        // Model parameter settings
        string model  = "gpt-4o";
        float temperature = 1.0f;
        int maxTokens = 2000;
        bool stream = false;

        // system instructions
        string systemInstruction = PromptManager.MainCharacterAuto;

        // Message data
        List<CompletionMessage> messages = new List<CompletionMessage>();
        messages.Add(new CompletionMessage { role = "system", content = systemInstruction });
        messages.Add(new CompletionMessage { role = "user", content = logline});
        (bool success, string result) = await ChatGPTCompletionAPIAsync.CreateCompletionAsync(model, messages,
        (chunk) => {
            if (stream)
            {
                onChunkReceived?.Invoke(chunk);
            }
            else
            {
                onChunkReceived?.Invoke(ParseJson(chunk));
            }
        }, 
        temperature:temperature, 
        maxTokens:maxTokens, 
        stream:stream);

        return (success, result);
    }

    public async Task<string> GenerateMainCharacterPlotAsync(string logline, string characterProfile, Action<string> onChunkReceived)
    {
        Debug.Log("Generating Main Character Plot...");

        // Model parameter settings
        string model  = "gpt-4o";
        float temperature = 1.0f;
        int maxTokens = 600;
        bool stream = true;
        // system instructions
        string systemInstruction = PromptManager.MainCharacterPlot;
        string prompt = $"Logline: {logline}\n\n메인 캐릭터 정보:\n{characterProfile}";

        // Message data
        List<CompletionMessage> messages = new List<CompletionMessage>();
        messages.Add(new CompletionMessage { role = "system", content = systemInstruction });
        messages.Add(new CompletionMessage { role = "user", content = prompt});

        (bool success, string result) = await ChatGPTCompletionAPIAsync.CreateCompletionAsync(
            model, 
            messages,
            (chunk) => {
                onChunkReceived?.Invoke(chunk);
            }, 
            temperature:temperature, 
            maxTokens:maxTokens, 
            stream:stream
        );

        return result;
    }

    public async Task<string> GenerateMainCharacterSingleAsync(string logline, string mainCharacterProfile, string characterProfile, Action<string> onChunkReceived)
    {
        Debug.Log("Generating Main Character Single...");

        // Model parameter settings
        string model  = "gpt-4o";
        float temperature = 1.0f;
        int maxTokens = 600;
        bool stream = false;

        // system instructions
        string systemInstruction = PromptManager.MainCharacterSingle;
        string prompt = $"Logline: {logline}\n\n기존의 다른 메인 캐릭터 정보:\n{mainCharacterProfile}\n\n작성해야 하는 메인 캐릭터의 주어진 정보:\n{characterProfile}";

        //Debug.Log($"Debug Main Character Single\n Prompt: {prompt}\n System Instruction: {systemInstruction}");

        // Message data
        List<CompletionMessage> messages = new List<CompletionMessage>();
        messages.Add(new CompletionMessage { role = "system", content = systemInstruction });
        messages.Add(new CompletionMessage { role = "user", content = prompt});
        (bool success, string result) = await ChatGPTCompletionAPIAsync.CreateCompletionAsync(model, messages,
        (chunk) => {
            if (stream)
            {
                onChunkReceived?.Invoke(chunk);
            }
            else
            {
                onChunkReceived?.Invoke(ParseJson(chunk));
            }
        }, 
        temperature:temperature, 
        maxTokens:maxTokens, 
        stream:stream);

        return result;
    }

    public async Task<(bool success, string result)> GenerateSubCharacterAsync(string logline, string characterProfile, Action<string> onChunkReceived)
    {
        Debug.Log("Generating Sub Character...");

        // Model parameter settings
        string model  = "gpt-4o";
        float temperature = 1.0f;
        int maxTokens = 2000;
        bool stream = false;

        // system instructions
        string systemInstruction = PromptManager.SubCharacterAuto;
        string prompt = $"Logline: {logline}\n\n메인 캐릭터 정보:\n{characterProfile}";

        // Message data
        List<CompletionMessage> messages = new List<CompletionMessage>();
        messages.Add(new CompletionMessage { role = "system", content = systemInstruction });
        messages.Add(new CompletionMessage { role = "user", content = prompt});
        (bool success, string result) = await ChatGPTCompletionAPIAsync.CreateCompletionAsync(model, messages,
        (chunk) => {
            if (stream)
            {
                onChunkReceived?.Invoke(chunk);
            }
            else
            {
                onChunkReceived?.Invoke(ParseJson(chunk));
            }
        }, 
        temperature:temperature, 
        maxTokens:maxTokens, 
        stream:stream);


        return (success, result);
    }

    public async Task<string> GenerateSubCharacterSingleAsync(string logline, string mainCharacterProfile, string subCharacterProfile, Action<string> onChunkReceived)
    {
        Debug.Log("Generating Sub Character Single...");

        // Model parameter settings
        string model  = "gpt-4o";
        float temperature = 1.0f;
        int maxTokens = 600;
        bool stream = false;

        // system instructions
        string systemInstruction = PromptManager.SubCharacterSingle;
        string prompt = $"Logline: {logline}\n\n메인 캐릭터 정보:\n{mainCharacterProfile}\n\n작성해야 하는 서브 캐릭터의 주어진 정보:\n{subCharacterProfile}";

        // Message data
        List<CompletionMessage> messages = new List<CompletionMessage>();
        messages.Add(new CompletionMessage { role = "system", content = systemInstruction });
        messages.Add(new CompletionMessage { role = "user", content = prompt});
        (bool success, string result) = await ChatGPTCompletionAPIAsync.CreateCompletionAsync(model, messages,
        (chunk) => {
            if (stream)
            {
                onChunkReceived?.Invoke(chunk);
            }
            else
            {
                onChunkReceived?.Invoke(ParseJson(chunk));
            }
        }, 
        temperature:temperature, 
        maxTokens:maxTokens, 
        stream:stream);

        return result;
    }

    public async Task<string> GenerateSubCharacterPlotAsync(string logline, string subCharacterProfile, Action<string> onChunkReceived)
    {
        Debug.Log("Generating Sub Character Plot...");

        // Model parameter settings
        string model  = "gpt-4o";
        float temperature = 1.0f;
        int maxTokens = 600;
        bool stream = true;
        // system instructions
        string systemInstruction = PromptManager.SubCharacterPlot;
        string prompt = $"Logline: {logline}\n\n서브 캐릭터 정보:\n{subCharacterProfile}";

        // Message data
        List<CompletionMessage> messages = new List<CompletionMessage>();
        messages.Add(new CompletionMessage { role = "system", content = systemInstruction});
        messages.Add(new CompletionMessage { role = "user", content = prompt});
        (bool success, string result) = await ChatGPTCompletionAPIAsync.CreateCompletionAsync(model, messages,
        (chunk) => {
           onChunkReceived?.Invoke(chunk);
        }, 
        temperature:temperature, 
        maxTokens:maxTokens, 
        stream:stream);
        
        return result;
    }

    public string ParseJson(string content)
    {
        string pattern = @"```(?<blockType>\S+)?\s*\n(?<contents>[\s\S]*?)```";
        RegexOptions options = RegexOptions.Multiline;

        Match match = Regex.Match(content, pattern, options);

        if (match.Success)
        {
            return match.Groups["contents"].Value;
        }
        else
        {
            return content;
        }
    }
}