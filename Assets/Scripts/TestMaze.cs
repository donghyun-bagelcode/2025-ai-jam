using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

public class TestMaze : MonoBehaviour
{
    [ContextMenu("GenerateMaze")]
    public void Generate()
    {
        // var graphAsset = Create();

        // var graph = graphAsset.graph; // 그래프 가져오기
        // var setVariableUnit = new VariableUnit();
        // graph.elements.Add(setVariableUnit);
        // node.variable.name = "myVariable";
        // node.value = 10;
    }

    private ScriptGraphAsset Create()
    {
        ScriptGraphAsset scriptGraph = ScriptableObject.CreateInstance<ScriptGraphAsset>();
        AssetDatabase.CreateAsset(scriptGraph, "Assets/Foundation/Graphs/Subgraphs/NewScriptGraph.asset");
        AssetDatabase.SaveAssets();
        return scriptGraph;
    }
}
