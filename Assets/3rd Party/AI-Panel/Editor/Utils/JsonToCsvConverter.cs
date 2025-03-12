using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using Dunward.Capricorn;
using Unity.VisualScripting;
using TMPro;
using UnityEngine.UI;

public class JsonToCsvConverter
{
    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    public static string JsonToCsvConverterMethod(string json)
    {
        // Deserialize JSON to RootObject
        GraphData root;
        try
        {
            root = JsonConvert.DeserializeObject<GraphData>(json, settings);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("유효하지 않은 JSON 형식입니다.", ex);
        }

        if (root == null || root.nodes == null)
        {
            throw new ArgumentException("JSON 데이터에 노드 정보가 없습니다.");
        }

        // Define CSV headers
        string[] headers = new string[]
        {
            "id", "x", "y", "actionType", "SelectionCount", "connections", "name", "subName",
            "script_1", "script_2", "script_3", "script_4"
        };

        StringBuilder csvBuilder = new StringBuilder();

        // Write headers
        csvBuilder.AppendLine(string.Join(",", headers));

        // Process each node
        foreach (var node in root.nodes)
        {

            int id = node.id;
            float x = node.x;
            float y = node.y;

            int actionType = 1;
            int selectionCount = 0;
            string connections = "-999";
            string name = "";
            string subName = "";

            // script fields
            string script1 = "";
            string script2 = "";
            string script3 = "";
            string script4 = "";

            // Determine actionType based on TypeValue
            string type = node.actionData?.action?.GetType().ToString();

            if (type == typeof(TextTypingUnit).ToString())
            {
                TextTypingUnit textTypingUnit = (TextTypingUnit)node.actionData.action;

                actionType = 1; 
                selectionCount = 1;
                name = textTypingUnit.name;
                subName = textTypingUnit.subName;
                script1 = textTypingUnit.script;
                connections = string.Join(",", textTypingUnit.connections);
            }
            else if (type == typeof(SelectionUnit).ToString())
            {
                SelectionUnit selectionUnit = (SelectionUnit)node.actionData.action;

                actionType = 2;
                selectionCount = selectionUnit.SelectionCount;
                connections = string.Join(",", selectionUnit.connections);

                var scripts = selectionUnit.scripts;
                if (scripts != null)
                {
                    for (int i = 0; i < Math.Min(scripts.Count, 4); i++)
                    {
                        switch (i)
                        {
                            case 0:
                                script1 = scripts[i];
                                break;
                            case 1:
                                script2 = scripts[i];
                                break;
                            case 2:
                                script3 = scripts[i];
                                break;
                            case 3:
                                script4 = scripts[i];
                                break;
                        }
                    }
                }
            }
            // Create an array of CSV fields in order
            string[] csvFields = new string[]
            {
                id.ToString(),
                x.ToString(),
                y.ToString(),
                actionType.ToString(),
                selectionCount.ToString(),
                AddQuotes(connections),
                AddQuotes(name),
                AddQuotes(subName),
                AddQuotes(script1),
                AddQuotes(script2),
                AddQuotes(script3),
                AddQuotes(script4)
            };

            // Join fields with comma separator
            string csvLine = string.Join(",", csvFields);
            csvBuilder.AppendLine(csvLine);
        }

        return csvBuilder.ToString();
    }

    private static string AddQuotes(string field)
    {
        if (!string.IsNullOrEmpty(field))
            return $"\"{field}\"";

        return field;
    }
}