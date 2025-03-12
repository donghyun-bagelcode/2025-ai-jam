using System;
using System.Threading.Tasks;
public interface IAIModel
{
    Task<AIResponse> GenerateResponseAsync(AssistantType assistantType, string input);
}