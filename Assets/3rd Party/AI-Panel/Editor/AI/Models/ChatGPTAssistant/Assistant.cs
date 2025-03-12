[System.Serializable]
public class Assistant
{
    public string id;
    public string Object;
    public long created_at;
    public string name;
    public string description;
    public string model;
    public string instructions;
    public Tool[] tools;
    public Metadata metadata;
    public float top_p;
    public float temperature;
    public string response_format;
}

[System.Serializable]
public class Tool
{
    public string type;
}

[System.Serializable]
public class Metadata
{
    // 이 클래스는 추가적인 메타데이터를 저장하는데 사용할 수 있습니다.
}