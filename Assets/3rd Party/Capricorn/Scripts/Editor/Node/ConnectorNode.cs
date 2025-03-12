#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace Dunward.Capricorn
{
    public class ConnectorNode : BaseNode
    {
        public ConnectorNode(GraphView graphView, int id, float x, float y) : base(graphView, id, x, y)
        {
            Initialize();
        }

        public ConnectorNode(GraphView graphView, int id, Vector2 mousePosition) : base(graphView, id, mousePosition)
        {
            Initialize();
        }

        public ConnectorNode(GraphView graphView, NodeMainData mainData) : base(graphView, mainData)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            nodeType = NodeType.Connector;
        }

        protected override void SetupTitleContainer()
        {
            title = $"{id}";
        }
    }
}
#endif