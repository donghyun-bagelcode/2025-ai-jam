using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor.PackageManager.Requests;
using System.Threading;

#region ChatGPT API with UnityWebRequest(Async/Await)
#region ChatGPT Assistant API Async
public class ChatGPTAssistantAPIAsync
{
    private const string ApiBaseUrl = "https://api.openai.com/v1/assistants";

    public static async Task<Assistant> CreateAssistantAsync(string instructions, string assistantName)
    {
        UnityWebRequest request = null;
        try
        {
            string apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");
            string url = ApiBaseUrl;
            request = new UnityWebRequest(url, "POST");
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

            // Prepare the JSON data
            var jsonData = JsonConvert.SerializeObject(new
            {
                model = "gpt-4o",
                instructions = instructions,
                name = assistantName
            });

            byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();

            await request.SendWebRequestAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error Creating Assistant: {request.error}\n{request.downloadHandler.text}");
                return null;
            }

            Debug.Log("Assistant Created: " + request.downloadHandler.text);
            Assistant assistant = JsonConvert.DeserializeObject<Assistant>(request.downloadHandler.text);
            Debug.Log("Assistant Name: " + assistant.name);
            return assistant;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error Creating Assistant: {ex.Message}");
            return null;
        }
        finally
        {
            if (request != null) request.Dispose();
        }
    }

    public static async Task<List<Assistant>> ListAssistantsAsync(int limit = 20, string order = "desc", string after = null, string before = null)
    {
        try
        {
            var url = $"{ApiBaseUrl}?limit={limit}&order={order}";
            if (!string.IsNullOrEmpty(after)) url += $"&after={after}";
            if (!string.IsNullOrEmpty(before)) url += $"&before={before}";

            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey"));
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

            await request.SendWebRequestAsync();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                var result = JsonConvert.DeserializeObject<AssistantListResponse>(request.downloadHandler.text);
                return result.data;
            }
            else
            {
                Debug.LogError($"Failed to list assistants: {request.downloadHandler.text}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error listing assistants: {ex.Message}");
            return null;
        }
    }

    public static async Task<bool> DeleteAssistantAsync(string assistantId)
    {
        try
        {
            string url = $"{ApiBaseUrl}/{assistantId}";
            UnityWebRequest request = UnityWebRequest.Delete(url);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey"));
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
            request.downloadHandler = new DownloadHandlerBuffer();

            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<AssistantDeleteResponse>(request.downloadHandler.text);
                return response.deleted;
            }
            
            Debug.LogError($"Failed to delete assistant: {request.downloadHandler.text}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error deleting assistant: {ex.Message}");
            return false;
        }
    }
}
#endregion

#region ChatGPT Assistant Thread API Async
public class ChatGPTThreadAPIAsync
{
    private const string BaseUrl = "https://api.openai.com/v1/threads";

    /// <summary>
    /// 스레드를 비동기적으로 생성합니다.
    /// </summary>
    /// <returns>성공 여부와 생성된 스레드 ID를 반환하는 튜플</returns>
    public static async Task<(bool success, string threadId)> CreateThreadAsync()
    {
        string apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");
        string url = BaseUrl;

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // 요청 바디가 필요한 경우 여기에 추가
        string requestBody = "{}"; // 필요에 따라 수정
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(requestBody));
        request.downloadHandler = new DownloadHandlerBuffer();

        try
        {
            await request.SendWebRequestAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error creating thread: {request.error}\n{request.downloadHandler.text}");
                return (false, null);
            }

            Debug.Log("Thread created: " + request.downloadHandler.text);
            Thread response = JsonConvert.DeserializeObject<Thread>(request.downloadHandler.text);
            return (true, response.id);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating thread: {ex.Message}");
            return (false, null);
        }
        finally
        {
            request.Dispose();
        }
    }

    /// <summary>
    /// 스레드를 비동기적으로 가져옵니다.
    /// </summary>
    /// <param name="threadId">스레드 ID</param>
    /// <returns>스레드 정보를 담은 thread 객체</returns>
    public static async Task<Run> RetrieveThreadAsync(string threadId)
    {
        string apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");
        string url = $"{BaseUrl}/{threadId}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        try
        {
            await request.SendWebRequestAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error retrieving thread: {request.error}\n{request.downloadHandler.text}");
                return null;
            }

            Debug.Log("Thread retrieved: " + request.downloadHandler.text);
            Run thread = JsonConvert.DeserializeObject<Run>(request.downloadHandler.text);
            Debug.Log("Thread ID: " + thread.id);
            return thread;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception retrieving thread: {ex.Message}");
            return null;
        }
        finally
        {
            request.Dispose();
        }
    }
}
#endregion

#region ChatGPT Assistant Message API Async
public class ChatGPTMessageAPIAsync
{
    private const string BaseUrl = "https://api.openai.com/v1/threads/";

    public static async Task<(bool success, string messageId)> CreateMessageAsync(string threadId, string role, string content)
    {
        string url = $"{BaseUrl}{threadId}/messages";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // Prepare the JSON data
        string jsonData = JsonConvert.SerializeObject(new MessageData(role, content));
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error creating message: {request.error}\n {request.downloadHandler.text}");
                return (false, request.downloadHandler.text);
            }

            Debug.Log("Message created\n " + request.downloadHandler.text);
            return (true, request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating message: {ex.Message}");
            return (false, ex.Message);
        }
        finally
        {
            request.Dispose();
        }
    }

    public static async Task<(bool success, string message)> RetrieveMessageAsync(string threadId, string messageId)
    {
        string url = $"{BaseUrl}{threadId}/messages/{messageId}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error retrieving message: {request.error}\n {request.downloadHandler.text}");
                return (false, request.downloadHandler.text);
            }

            Debug.Log("Message retrieved: " + request.downloadHandler.text);
            return (true, request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception retrieving message: {ex.Message}");
            return (false, ex.Message);
        }
        finally
        {
            request.Dispose();
        }
    }

    public static async Task<(bool success, MessagesListResponse response)> ListMessagesAsync(string threadId, int limit = 1, string order = "desc", string after = null, string before = null, string runId = null)
    {
        string url = $"{BaseUrl}{threadId}/messages?limit={limit}&order={order}";

        if (!string.IsNullOrEmpty(after))
        {
            url += $"&after={after}";
        }

        if (!string.IsNullOrEmpty(before))
        {
            url += $"&before={before}";
        }

        if (!string.IsNullOrEmpty(runId))
        {
            url += $"&run_id={runId}";
        }

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching messages: {request.error}\n{request.downloadHandler.text}");
                return (false, null);
            }

            Debug.Log("Messages fetched\n " + request.downloadHandler.text);
            MessagesListResponse response = JsonConvert.DeserializeObject<MessagesListResponse>(request.downloadHandler.text);
            return (true, response);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception fetching messages: {ex.Message}");
            return (false, null);
        }
        finally
        {
            request.Dispose();
        }
    }

    public static async Task<(bool success, string message)> DeleteMessageAsync(string threadId, string messageId)
    {
        string url = $"{BaseUrl}{threadId}/messages/{messageId}";
        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error deleting message: {request.error}\n{request.downloadHandler.text}");
                return (false, request.downloadHandler.text);
            }

            Debug.Log("Message deleted: " + request.downloadHandler.text);
            return (true, request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception deleting message: {ex.Message}");
            return (false, ex.Message);
        }
        finally
        {
            request.Dispose();
        }
    }
}
#endregion

#region ChatGPT Assistant Run API Async
public class ChatGPTRunAPIAsync
{
    private const string baseUrl = "https://api.openai.com/v1/threads/";

    public static async Task<(bool success, string runId)> CreateRunAsync(string threadId, string assistantId, string model = null, string newInstructions = null, float temperature = 0.5f)
    {
        string url = $"{baseUrl}{threadId}/runs";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // JSON 데이터 준비
        RunData runData = new RunData
        {
            assistant_id = assistantId,
            model = model,
            instructions = newInstructions ?? "",
            temperature = temperature,
            max_prompt_tokens = 50000,
            max_completion_tokens = 15000
        };
        
        string jsonData = JsonConvert.SerializeObject(runData);
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Run 생성 오류: {request.error}\n{request.downloadHandler.text}");
                return (false, request.downloadHandler.text);
            }

            Debug.Log("Run created\n" + request.downloadHandler.text);
            Run response = JsonConvert.DeserializeObject<Run>(request.downloadHandler.text);
            return (true, response.id);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating run: {ex.Message}");
            return (false, ex.Message);
        }
        finally
        {
            request.Dispose();
        }
    }

    public static async Task<(bool success, Run response)> RetrieveRunAsync(string threadId, string runId)
    {
        UnityWebRequest request = null;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));  // 30초로 변경
            
            string url = $"{baseUrl}{threadId}/runs/{runId}";
            request = new UnityWebRequest(url, "GET");
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "")}");
            request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
            request.downloadHandler = new DownloadHandlerBuffer();

            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    request.Abort();
                    Debug.LogWarning("RetrieveRunAsync request timed out");
                    return (false, null);
                }
                await Task.Delay(100);
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error retrieving run: {request.error}\n{request.downloadHandler?.text}");
                return (false, null);
            }

            try
            {
                var run = JsonConvert.DeserializeObject<Run>(request.downloadHandler.text);
                if (run == null)
                {
                    Debug.LogError("Failed to parse run response");
                    return (false, null);
                }
                Debug.Log($"Run retrieved : {run.status}");
                return (true, run);
            }
            catch (JsonException ex)
            {
                Debug.LogError($"JSON parsing error: {ex.Message}");
                return (false, null);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unexpected error in RetrieveRunAsync: {ex}");
            return (false, null);
        }
        finally
        {
            request?.Dispose();
        }
    }

    public static async Task<bool> WaitForRunCompleteAsync(string threadId, string runId, int timeoutSeconds = 180)
    {
        DateTime startTime = DateTime.Now;
        int intervalSeconds = 3;
        List<string> doneStatus = new List<string> { "completed", "incomplete", "failed", "cancelled", "expired" };
        
        Debug.Log($"WaitForRunCompleteAsync started : timeout {timeoutSeconds}초");

        while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
        {
            await Task.Delay(intervalSeconds * 1000); // 3초 대기

            var (success, response) = await RetrieveRunAsync(threadId, runId);
            if (!success)
            {
                Debug.LogError("Failed to retrieve run status.");
            }

            if (doneStatus.Contains(response.status))
            {
                Run.Usage usage = response.usage;
                Debug.Log($"Run Usage:\n Prompt : {usage.prompt_tokens}\n Completion : {usage.completion_tokens}\n Total : {usage.total_tokens}");

                if (response.status == "incomplete")
                {
                    Debug.LogWarning($"Run is incomplete. Reason : {response.incomplete_details.reason}");
                    return true;
                }
                else if (response.status == "failed")
                {
                    Debug.LogWarning($"Run is failed. Reason : {response.last_error.message}");
                    return false;
                }
                else if (response.status == "cancelled")
                {
                    Debug.LogWarning("Run is cancelled.");
                    return false;
                }
                else if (response.status == "expired")
                {
                    Debug.LogWarning("Run is expired.");
                    return false;
                }

                return true;
            }

            Debug.Log($"Waiting... Elapsed: {(DateTime.Now - startTime).TotalSeconds:F1}초");
        }

        Debug.LogWarning($"Run completion timeout after {timeoutSeconds}초");
        return false;
    }
}
#endregion

#region ChatGPT Completion API Async
public class ChatGPTCompletionAPIAsync
{
    private const string BaseUrl = "https://api.openai.com/v1/chat/completions";

    public static async Task<(bool success, string completion)> CreateCompletionAsync(string model, List<CompletionMessage> messages, Action<string> onChunkReceived, int maxTokens = 150, float temperature = 0.5f, int topP = 1, int n = 1, bool stream = false)
    {
        string url = BaseUrl;
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));

        // JSON 데이터 준비
        CompletionData completionData = new CompletionData
        {
            model = model,
            messages = messages,
            max_completion_tokens = maxTokens,
            temperature = temperature,
            top_p = topP,
            n = n,
            stream = stream
        };

        string jsonData = JsonConvert.SerializeObject(completionData);
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();

        try
        {
            await request.SendWebRequestAsync();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error creating completion: {request.error}\n{request.downloadHandler.text}");
                return (false, request.downloadHandler.text);
            }

            if (stream)
            {
                // 스트리밍 응답 처리
                var responseStream = new MemoryStream(request.downloadHandler.data);
                var reader = new StreamReader(responseStream);

                bool firstStream = true;

                // 스트림이 끝날 때까지 반복
                while (!reader.EndOfStream)
                {
                    // ToDo: 있는 데이터 한번에 받아오기
                    string line = await reader.ReadLineAsync();

                    if (firstStream)
                    {
                        Debug.Log($"First Stream: {line}");
                        firstStream = false;
                    }

                    // JSON 데이터만 처리
                    string jsonLine = line.TrimStart().StartsWith("data: ") ? line.Substring(6).Trim() : line.Trim();

                    // 빈 줄은 무시
                    if (string.IsNullOrEmpty(jsonLine))
                        continue;

                    // `[DONE]` 처리
                    if (jsonLine == "[DONE]")
                    {
                        Debug.Log("Stream completed: [DONE]");
                        break;
                    }

                    // Delta content 추출
                    try
                    {
                        // ToDo: 여기서 데이터를 처리해버리면 choices가 여러개인 경우는 어떻게?
                        var json = JsonConvert.DeserializeObject<CompletionChunk>(jsonLine);
                        onChunkReceived?.Invoke(json.choices[0].delta.Content);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error parsing chunk: {jsonLine}\n\n Error Message: {ex.Message}");
                    }
                }

                return (true, "Streamed completion");
            }
            else
            {
                string chunk = request.downloadHandler.text;
                Debug.Log("Completion created\n" + chunk);

                // Handle Json output
                Completion completion = JsonConvert.DeserializeObject<Completion>(chunk);
                onChunkReceived?.Invoke(completion.choices[0].message.content);

                return (true, "Non-streamed Completion Completed");
            }
            
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception creating completion: {ex.Message}");
            return (false, ex.Message);
        }
        finally
        {
            request.Dispose();
        }
    }
}
#endregion

public class AssistantListResponse
{
    public List<Assistant> data { get; set; }
    public string @object { get; set; }
    public string first_id { get; set; }
    public string last_id { get; set; }
    public bool has_more { get; set; }
}

public class AssistantDeleteResponse
{
    public string id { get; set; }
    public string @object { get; set; }
    public bool deleted { get; set; }
}
#endregion