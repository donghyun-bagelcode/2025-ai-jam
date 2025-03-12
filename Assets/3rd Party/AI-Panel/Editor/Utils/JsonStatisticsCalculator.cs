using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dunward.Capricorn;
using Newtonsoft.Json;
using UnityEngine;

public struct JsonStatistics
{
    public int MaxId { get; set; }
    public int MinId { get; set; }
    public float MaxX { get; set; }
    public float MedX { get; set; }
    public float MinX { get; set; }
    public float MaxY { get; set; }
    public float MedY { get; set; }
    public float MinY { get; set; }

    public override string ToString()
    {
        return $"Max ID: {MaxId},Min Id: {MinId}, Max X: {MaxX}, Min X: {MinX}, Max Y: {MaxY}, Min Y: {MinY}, Med X: {MedX}, Med Y: {MedY}";
    }
}

public class JsonStatisticsCalculator
{
    private static JsonSerializerSettings settings = new JsonSerializerSettings
    {
        TypeNameHandling = TypeNameHandling.Auto
    };

    public static JsonStatistics CalculateStatistics(string json)
    {
        // JSON 디시리얼라이즈
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

        return CalculateStatistics(root);
    }

    public static JsonStatistics CalculateStatistics(GraphData root)
    {
        if (root == null || root.nodes == null)
        {
            throw new ArgumentException("JSON 데이터에 노드 정보가 없습니다.");
        }

        JsonStatistics stats = new JsonStatistics
        {
            MaxId = root.nodes.Max(node => node.id),
            MinId = root.nodes.Min(node => node.id),
            MaxX = root.nodes.Max(node => node.x),
            MinX = root.nodes.Min(node => node.x),
            MaxY = root.nodes.Max(node => node.y),
            MinY = root.nodes.Min(node => node.y),
            
            // median X, Y
            MedX = root.nodes.OrderBy(node => node.x).ElementAt(root.nodes.Count / 2).x,
            MedY = root.nodes.OrderBy(node => node.y).ElementAt(root.nodes.Count / 2).y
        };

        return stats;
    }
}