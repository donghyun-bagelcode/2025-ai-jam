using System.Collections.Generic;

[System.Serializable]
public class Run
{
    public string id;
    public string object_type;
    public int created_at;
    public string assistant_id;
    public string thread_id;
    public string status;
    public int? started_at;
    public int? expires_at;
    public int? cancelled_at;
    public int? failed_at;
    public int? completed_at;
    public Error last_error;
    public string model;
    public string instructions;
    public Tool[] tools;
    public Dictionary<string, string> metadata;
    public IncompleteDetails incomplete_details;
    public Usage usage;
    public float? temperature;
    public float? top_p;
    public int? max_prompt_tokens;
    public int? max_completion_tokens;
    public TruncationStrategy truncation_strategy;
    public object tool_choice;
    public bool parallel_tool_calls;
    public object response_format;

    [System.Serializable]
    public class Tool
    {
        public string type;
    }

    [System.Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    [System.Serializable]
    public class Error
    {
        public string message;
    }

    [System.Serializable]
    public class IncompleteDetails
    {
        public string reason;
    }

    [System.Serializable]
    public class TruncationStrategy
    {
        public string type;
        public string last_messages;
    }
}

[System.Serializable]
public class RunData
{
    public string assistant_id;
    public string model;
    public string instructions;
    public float temperature;
    public int max_prompt_tokens;
    public int max_completion_tokens;
}