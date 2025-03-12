using System;
using System.Threading.Tasks;
using UnityEngine;

public class ClaudeSonnet : IAIModel
{
    public async Task<AIResponse> GenerateResponseAsync(AssistantType assistantType, string input)
    {
        try
        {
            // 실제 AI 요청 로직 구현 (예: HTTP 요청)
            await Task.Delay(500); // 예시: 비동기 작업 시뮬레이션
            return new AIResponse($"Echo: {input}", true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"ChatGPTAssistant GenerateResponseAsync 오류: {ex.Message}");
            return new AIResponse("AI 응답 생성 중 오류가 발생했습니다.", false, ex.Message);
        }
    }

}