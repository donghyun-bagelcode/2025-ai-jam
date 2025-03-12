namespace Dunward.Capricorn
{
    [System.Serializable]
    public class NodeMainData
    {
        public int id;
        public float x;
        public float y;
        public NodeType nodeType;
        public NodeCoroutineData coroutineData;
        public NodeActionData actionData;
    }
}