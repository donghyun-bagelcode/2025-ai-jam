using System.Collections.Generic;

[System.Serializable]
public class Message
{
    public string id;
    public string object_type;  // Using object_type to avoid keyword conflict
    public int created_at;
    public string thread_id;
    public string status;
    public IncompleteDetails incomplete_details;
    public int? completed_at;
    public int? incomplete_at;
    public string role;
    public Content[] content;
    public string assistant_id;
    public string run_id;
    public Attachment[] attachments;
    public Dictionary<string, string> metadata;

    [System.Serializable]
    public class IncompleteDetails
    {
        // Define properties specific to incomplete details
        public string reason;
    }

    [System.Serializable]
    public class Content
    {
        public string type;
        public TextContent text;
    }

    [System.Serializable]
    public class TextContent
    {
        public string value;
        public Annotation[] annotations;
    }

    [System.Serializable]
    public class Annotation
    {
        // Define properties specific to annotations if necessary
        public string type;
    }

    [System.Serializable]
    public class Attachment
    {
        public string file_id;
        public string tool_used;
    }
}

[System.Serializable]
public class MessageData
{
    public string role;
    public string content;

    public MessageData(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[System.Serializable]
public class MessagesListResponse
{
    public string object_type;
    public Message[] data;
    public string first_id;
    public string last_id;
    public bool has_more;
}