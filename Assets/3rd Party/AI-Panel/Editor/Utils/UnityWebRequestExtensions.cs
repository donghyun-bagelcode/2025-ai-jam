using System.Threading.Tasks;
using UnityEngine.Networking;

public static class UnityWebRequestExtensions
{
    public static async Task<UnityWebRequest> SendWebRequestAsync(this UnityWebRequest request)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();

        request.SendWebRequest().completed += (asyncOp) =>
        {
            tcs.SetResult(request);
        };

        return await tcs.Task;
    }
}