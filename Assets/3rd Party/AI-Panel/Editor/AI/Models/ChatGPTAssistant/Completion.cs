using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class Completion
{
    public string id;
    public string object_type;  // Using object_type to avoid keyword conflict
    public int created_at;
    public string model;
    public List<Choice> choices;
    public Usage usage;
    public string system_fingerprint;

    [System.Serializable]
    public class Choice
    {
        public int index;
        public ChoiceMessage message;
        public LogProbs logprobs;
        public string finish_reason;
    }

    [System.Serializable]
    public class ChoiceMessage
    {
        public string role;
        public string content;
        public string refusal;
        public List<ToolCall> tool_calls;
        public Audio audio;
    }

    [System.Serializable]
    public class ToolCall
    {
        public string id;
        public string type;
    }

    [System.Serializable]
    public class Audio
    {
        public string id;
        public int expires_at;
        public string data;
        public string transcript;
    }

    [System.Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
        public PromptTokensDetails prompt_tokens_details;
        public CompletionTokensDetails completion_tokens_details;
    }

    [System.Serializable]
    public class PromptTokensDetails
    {
        public int cached_tokens;
    }

    [System.Serializable]
    public class CompletionTokensDetails
    {
        public int reasoning_tokens;
        public int accepted_prediction_tokens;
        public int rejected_prediction_tokens;
    }

    [System.Serializable]
    public class LogProbs
    {
        public List<ContentToken> content;
        public List<RefusalToken> refusal;
        public List<List<TopLogProb>> top_logprobs;
    }

    [System.Serializable]
    public class ContentToken
    {
        public string token;
        public float logprob;
        public List<int> bytes;
        public List<TopLogProb> top_logprobs;
    }

    [System.Serializable]
    public class RefusalToken
    {
        public string token;
        public float logprob;
        public List<int> bytes;
        public List<TopLogProb> top_logprobs;
    }

    [System.Serializable]
    public class TopLogProb
    {
        public string token;
        public float logprob;
        public List<int> bytes;
    }
}

[System.Serializable]
public class CompletionChunk
{
    public string id;
    public string object_type;
    public int created;
    public string model;
    public string system_fingerprint;
    public Choice[] choices;

    [System.Serializable]
    public class Choice
    {
        public int index;
        public Delta delta;
        public Completion.LogProbs logprobs;
        public string finish_reason;
    }

    [System.Serializable]
    public class Delta
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        // Delta 필드가 비어 있을 경우를 대비한 처리
        [JsonConstructor]
        public Delta(string role = null, string content = null)
        {
            Role = role;
            Content = content;
        }
    }
}


[System.Serializable]
public class CompletionData
{
    public string model;
    public List<CompletionMessage> messages;
    public int max_completion_tokens;
    public float temperature;
    public float top_p;
    public object response_format;
    public int n;
    public bool stream;
}

[System.Serializable]
public class CompletionMessage
{
    public string role;
    public string content;
    public string refusal;
}