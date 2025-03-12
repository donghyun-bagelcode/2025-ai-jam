using System;
using System.Threading.Tasks;
using UnityEngine;

public class AIManager
{
    private IAIModel currentAIModel;

    public AIManager(IAIModel aiModel)
    {
        currentAIModel = aiModel ?? throw new ArgumentNullException(nameof(aiModel));
    }

    public AIManager() : this(new ChatGPTAssistant())
    {
    }

    public void SetAIModel(IAIModel aiModel)
    {
        currentAIModel = aiModel ?? throw new ArgumentNullException(nameof(aiModel));
    }

    public async Task<AIResponse> GetResponseAsync(AssistantType assistantType, string input)
    {
        if (currentAIModel == null)
        {
            Debug.LogError("AIManager: AI 모델이 설정되지 않았습니다.");
            return new AIResponse("AI 모델이 초기화되지 않았습니다.", false, null);
        }
        return await currentAIModel.GenerateResponseAsync(assistantType, input);
    }    
}

