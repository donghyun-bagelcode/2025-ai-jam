using System.Collections.Generic;

[System.Serializable]
public class Thread
{
    public string id;
    public string object_type; // Using object_type to avoid keyword conflict
    public int created_at;
    public ToolResources tool_resources;
    public Dictionary<string, string> metadata;

    [System.Serializable]
    public class ToolResources
    {
        // Define as per the specific tools used, e.g., code interpreter or file search
        public List<string> file_ids; // Example for code interpreter tools
    }
}