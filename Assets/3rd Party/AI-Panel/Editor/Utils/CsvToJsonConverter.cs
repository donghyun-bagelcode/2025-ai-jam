using System;
using System.Collections.Generic;
using System.Text;
using Dunward.Capricorn;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;

public class CsvToJsonConverter
{
    /// <summary>
    /// CSV 문자열을 JSON 형식으로 변환하는 메서드
    /// </summary>
    /// <param name="csv">입력 CSV 문자열</param>
    /// <returns>변환된 JSON 문자열</returns> 

    // JSON 직렬화 설정
    static JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto, //필수옵션
        NullValueHandling = NullValueHandling.Include,
        Formatting = Formatting.Indented
    };
    
    public static string CsvToJsonConverterMethod(string csv)
    {
        // RootObject 초기화 (position, zoomFactor, debugNodeIndex는 예시 값으로 설정)
        var root = new GraphData
        {
            position = new UnityEngine.Vector2(62.0f,438.0f), // 예시 값
            zoomFactor = 0.657516241f, // 예시 값
            debugNodeIndex = -1, // 예시 값
        };

        // CSV를 줄 단위로 분리
        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
        {
            throw new ArgumentException("CSV 데이터가 충분하지 않습니다.");
        }

        // 헤더 파싱
        var headers = ParseCsvLine(lines[0]);
        var headerMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Count; i++)
        {
            headerMap[headers[i].Trim()] = i;
        }

        // 데이터 줄 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i]);

            // 필드 패딩: 필드 수가 헤더 수보다 적으면 빈 문자열로 채움
            while (fields.Count < headers.Count)
            {
                fields.Add(string.Empty);
            }

            // 각 필드 추출 (안전한 접근을 위해 인덱스 확인)
            string[] requiredHeaders = { "id", "x", "y", "actionType", "SelectionCount", "connections", "name", "subName" };
            bool missingHeader = false;
            foreach (var header in requiredHeaders)
            {
                if (!headerMap.ContainsKey(header))
                {
                    missingHeader = true;
                    break;
                }
            }
            if (missingHeader)
            {
                throw new ArgumentException("CSV 헤더가 누락되었습니다.");
            }

            // 필드 값 추출 및 정제
            if (!int.TryParse(fields[headerMap["id"]].Trim(), out int id))
            {
                throw new ArgumentException($"Invalid id value at line {i + 1}");
            }

            if (!float.TryParse(fields[headerMap["x"]].Trim(), out float x))
            {
                throw new ArgumentException($"Invalid x value at line {i + 1}");
            }

            if (!float.TryParse(fields[headerMap["y"]].Trim(), out float y))
            {
                throw new ArgumentException($"Invalid y value at line {i + 1}");
            }

            if (!int.TryParse(fields[headerMap["actionType"]].Trim(), out int actionType))
            {
                throw new ArgumentException($"Invalid actionType value at line {i + 1}");
            }

            if (!int.TryParse(fields[headerMap["SelectionCount"]].Trim(), out int selectionCount))
            {
                throw new ArgumentException($"Invalid SelectionCount value at line {i + 1}");
            }

            string connectionsStr = fields[headerMap["connections"]].Trim('"').Trim();
            string name = fields[headerMap["name"]].Trim('"').Trim();
            string subName = fields[headerMap["subName"]].Trim('"').Trim();

            // script_1 ~ script_4 필드 추출
            List<string> scripts = new List<string>();
            for (int j = 1; j <= 4; j++)
            {
                string scriptField = $"script_{j}";
                if (headerMap.ContainsKey(scriptField) && !string.IsNullOrWhiteSpace(fields[headerMap[scriptField]]))
                {
                    scripts.Add(fields[headerMap[scriptField]].Trim('"').Trim());
                }
            }

            object scriptContent = null;
            if (actionType == 1)
            {
                // actionType=1인 경우 script는 script_1의 내용
                scriptContent = scripts.Count > 0 ? scripts[0] : string.Empty;
            }
            else if (actionType == 2)
            {
                // actionType=2인 경우 script는 script_1 ~ script_4의 리스트
                scriptContent = scripts;
            }
            else
            {
                // 다른 actionType이 추가될 경우 대비
                scriptContent = scripts.Count > 0 ? scripts[0] : string.Empty;
            }

            // connections 파싱 (콤마로 분리하여 정수 리스트로 변환)
            List<int> connections = new List<int>();
            if (!string.IsNullOrWhiteSpace(connectionsStr))
            {
                var connStrings = connectionsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var conn in connStrings)
                {
                    if (int.TryParse(conn.Trim(), out int connId))
                    {
                        connections.Add(connId);
                    }
                }
            }

            // nodeType 추론
            NodeType nodeType = NodeType.Connector; // 기본값 (중간 노드)
            if (id == -1)
            {
                nodeType = NodeType.Input; // 시작 노드
            }
            else if (connections.Count == 0)
            {
                //nodeType = NodeType.Output; // 종료 노드
                nodeType = NodeType.Connector;
            }

            // $type 설정 based on actionType

            var node = new NodeMainData
            {
                id = id,
                x = x,
                y = y,
                nodeType = nodeType,
                actionData = new NodeActionData()
            };

            if (actionType == 1)
            {
                var action = new TextTypingUnit(){
                    SelectionCount = selectionCount,
                    connections = connections,

                    name = name,
                    subName = subName,
                    script = (string)scriptContent
                };

                node.actionData.action = action;
            }
            else if (actionType == 2)
            {
                var action = new SelectionUnit(){
                    SelectionCount = selectionCount,
                    connections = connections,
                    scripts = (List<string>)scriptContent
                };

                node.actionData.action = action;
            }
            else
            {
                // 기본값 또는 추가 actionType에 따른 처리
                var action = new TextTypingUnit();
                node.actionData.action = action;
            }
            
            // Debug.Log($"{id}, {x}, {y}, {actionType}, {selectionCount}, {connections}, {name}, {subName}, {scriptContent}");

            // nodes 리스트에 추가
            root.nodes.Add(node);
        }

        // JSON 문자열 생성
        string json = JsonConvert.SerializeObject(root, jsonSettings);
        return json;
    }

    /// <summary>
    /// CSV 한 줄을 필드 리스트로 파싱하는 헬퍼 메서드
    /// </summary>
    /// <param name="line">CSV 한 줄</param>
    /// <returns>필드 리스트</returns>
    public static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        StringBuilder fieldBuilder = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') // 이스케이프된 따옴표
                {
                    fieldBuilder.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(fieldBuilder.ToString());
                fieldBuilder.Clear();
            }
            else
            {
                fieldBuilder.Append(c);
            }
        }

        // 마지막 필드 추가
        fields.Add(fieldBuilder.ToString());

        return fields;
    }
}