using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

#region ChatGPT API with UnityWebRequest(Callback)
#region ChatGPT Assistant API
public class ChatGPTAssistantAPI
{
    private const string ApiBaseUrl = "https://api.openai.com/v1/assistants/";
    
    public static void RetrieveAssistant()
    {
        //ToDo: 모종의 이유로 GetEnvironmentVariable() 함수가 작동하지 않음..
        //string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        string apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");
        string assistantId = SecurePlayerPrefs.GetString("ChatGPTAssistant_AssistantId", "");
        string url = ApiBaseUrl + assistantId;
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // Send the web request and attach event handlers
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += OnRequestComplete;
    }

    
    private static void OnRequestComplete(AsyncOperation obj)
    {
        UnityWebRequestAsyncOperation asyncOperation = obj as UnityWebRequestAsyncOperation;
        UnityWebRequest request = asyncOperation.webRequest;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error: {request.error}\n{request.downloadHandler.text}");
        }
        else
        {
            Debug.Log("Received: " + request.downloadHandler.text);
            Assistant assistant = JsonUtility.FromJson<Assistant>(request.downloadHandler.text);
            Debug.Log("Assistant Name: " + assistant.name);
        }

        // Clean up
        request.Dispose();
    }
}
#endregion

#region ChatGPT Thread API
public class ChatGPTThreadAPI
{
    private const string baseUrl = "https://api.openai.com/v1/threads";

    // Method to create a thread
    public static void CreateThread(Action<bool, string> onComplete)
    {
        string apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");
        UnityWebRequest request = new UnityWebRequest(baseUrl, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // Optionally, include body data for starting the thread with messages or resources
        string requestBody = "{}";  // Adjust the request body as per API documentation if needed
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
        request.downloadHandler = new DownloadHandlerBuffer();

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += (op) =>
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error creating thread: {request.error}\n{request.downloadHandler.text}");
                onComplete(false, null);
            }
            else
            {
                Debug.Log("Thread created: " + request.downloadHandler.text);
                Thread thread = JsonUtility.FromJson<Thread>(request.downloadHandler.text);
                onComplete(true, thread.id);
            }
            request.Dispose();
        };
    }

    public static void RetrieveThread()
    {
        string apiKey = SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", "");
        string threadId = SecurePlayerPrefs.GetString("ChatGPTAssistant_ThreadId", "");
        string url = baseUrl + "/" + threadId;
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += OnThreadRequestComplete;
    }


    // Callback for handling the response
    private static void OnThreadRequestComplete(AsyncOperation obj)
    {
        UnityWebRequestAsyncOperation asyncOperation = obj as UnityWebRequestAsyncOperation;
        UnityWebRequest request = asyncOperation.webRequest;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Error thread request: {request.error}\n{request.downloadHandler.text}");
        }
        else
        {
            Debug.Log("Response: " + request.downloadHandler.text);
            Thread thread = JsonUtility.FromJson<Thread>(request.downloadHandler.text);
            Debug.Log("Thread ID: " + thread.id);
        }

        request.Dispose();
    }
}
#endregion

#region ChatGPT Message API
public class ChatGPTMessageAPI
{
    private const string baseUrl = "https://api.openai.com/v1/threads/";

    // Delegate for handling the response
    public delegate void ResponseCallback(MessagesListResponse response);

    // Method to create a message in a specific thread
    public static void CreateMessage(string threadId, string role, string content, Action<bool, string> onComplete)
    {
        string url = $"{baseUrl}{threadId}/messages";
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        // Prepare the JSON data
        string jsonData = JsonUtility.ToJson(new MessageData(role, content));
        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();

        //Debug.Log("Creating message: " + jsonData);

        // Send the request
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += (op) =>
        {
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error creating message: {request.error}\n {request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
            else
            {
                Debug.Log("Message created\n " + request.downloadHandler.text);
                onComplete?.Invoke(true, request.downloadHandler.text);
            }
            request.Dispose();
        };
    }

    public static void RetrieveMessage(string threadId, string messageId, Action<bool, string> onComplete)
    {
        string url = $"{baseUrl}{threadId}/messages/{messageId}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += (op) =>
        {
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error retrieving message: {request.error}\n {request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
            else
            {
                Debug.Log("Message retrieved: " + request.downloadHandler.text);
                onComplete?.Invoke(true, request.downloadHandler.text);
            }
            request.Dispose();
        };
    }

    // Get list of messages for a given thread
    public static void ListMessages(string threadId, int limit = 1, string order = "desc", string after = null, string before = null, string runId = null, ResponseCallback callback = null)
    {
        string url = $"{baseUrl}{threadId}/messages?limit={limit}&order={order}";

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

        var operation = request.SendWebRequest();
        operation.completed += (op) =>
        {
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error fetching messages: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                Debug.Log("Messages fetched\n " + request.downloadHandler.text);
                MessagesListResponse response = JsonUtility.FromJson<MessagesListResponse>(request.downloadHandler.text);
                callback?.Invoke(response);
            }
            request.Dispose();
        };
    }

    // Method to delete a specific message
    public static void DeleteMessage(string threadId, string messageId, Action<bool, string> onComplete)
    {
        string url = $"{baseUrl}{threadId}/messages/{messageId}";
        UnityWebRequest request = UnityWebRequest.Delete(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += (op) =>
        {
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error deleting message: {request.error}\n{request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
            else
            {
                Debug.Log("Message deleted: " + request.downloadHandler.text);
                onComplete?.Invoke(true, request.downloadHandler.text);
            }
            request.Dispose();
        };
    }


}
#endregion

#region ChatGPT Run API
public class ChatGPTRunAPI
{
    private const string baseUrl = "https://api.openai.com/v1/threads/";

    // 특정 스레드에 대한 Run 생성 메서드
    public static void CreateRun(string threadId, string assistantId, Action<bool, string> onComplete, string model = null, string newInstructions = null, float temperature = 0.5f)
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
            temperature = temperature
        };
        string jsonData = JsonUtility.ToJson(runData);
        //Debug.Log("Run 생성 데이터: " + jsonData);

        byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();

        // 요청 보내기
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += (op) =>
        {
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Run 생성 오류: {request.error}\n{request.downloadHandler.text}");
                onComplete(false, request.downloadHandler.text);
            }
            else
            {
                Debug.Log("Run created\n" + request.downloadHandler.text);
                onComplete(true, request.downloadHandler.text);
            }
            request.Dispose();
        };
    }

    public static void RetrieveRun(string threadId, string runId, Action<bool, string> onComplete)
    {
        string url = $"{baseUrl}{threadId}/runs/{runId}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SecurePlayerPrefs.GetString("ChatGPTAssistant_ApiKey", ""));
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        operation.completed += (op) =>
        {
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error retrieving run:{request.error}\n{request.downloadHandler.text}");
                onComplete?.Invoke(false, request.downloadHandler.text);
            }
            else
            {
                Debug.Log("Run retrieved\n" + request.downloadHandler.text);
                onComplete?.Invoke(true, request.downloadHandler.text);
            }
            request.Dispose();
        };
    }

    public static void WaitForRunComplete(string threadId, string runId, Action<bool> onComplete, int timeoutSeconds = 60)
    {
        float nextTime = Time.realtimeSinceStartup + 3f; // Set the initial delay to 3 seconds
        int elapsedSeconds = 0;

        EditorApplication.CallbackFunction checkRunCompletion = null;

        checkRunCompletion = () =>
        {
            // Check if the current time has reached the next scheduled time
            if (Time.realtimeSinceStartup >= nextTime)
            {
                if (elapsedSeconds >= timeoutSeconds)
                {
                    EditorApplication.update -= checkRunCompletion;
                    Debug.LogWarning("Timeout while waiting for run to complete.");
                    onComplete(false);
                    return;
                }

                RetrieveRun(threadId, runId, (success, response) =>
                {
                    if (!success)
                    {
                        Debug.LogError("Failed to retrieve run status.");
                        EditorApplication.update -= checkRunCompletion;
                        onComplete(false);
                        return;
                    }

                    Run runResponse = JsonUtility.FromJson<Run>(response);
                    if (runResponse.status == "completed" || runResponse.status == "failed" || runResponse.status == "cancelled" || runResponse.status == "expired")
                    {
                        EditorApplication.update -= checkRunCompletion;
                        onComplete(true);
                        return;
                    }
                });

                // Reset the timer
                nextTime = Time.realtimeSinceStartup + 3f;
                elapsedSeconds += 3;
            }
        };

        // Attach the check to the Editor's update loop
        EditorApplication.update += checkRunCompletion;
    }

}
#endregion
#endregion