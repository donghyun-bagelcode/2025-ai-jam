using System.Collections.Concurrent;
using System;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;

public class QueueProcessor
{
    private ConcurrentQueue<string> plotQueue = new ConcurrentQueue<string>();
    public bool isProcessing = false;

    /// <summary>
    /// 데이터를 Queue에 추가하고, 처리 루프를 시작
    /// </summary>
    public void EnqueueAndProcess(string chunk, Func<string, Task> displayAction, float delay=0.1f)
    {
        plotQueue.Enqueue(chunk);

        if (!isProcessing)
        {
            _ = ProcessQueueAdaptiveAsync(displayAction,delay: delay);
        }
    }

    /// <summary>
    /// Queue의 데이터를 처리하면서 순차적으로 UI에 반영
    /// </summary>
    private async Task ProcessQueueAsync(Func<string, Task> displayAction, float delay)
    {
        isProcessing = true;

        while (plotQueue.TryDequeue(out string chunk))
        {
            await displayAction(chunk);  // UI에 출력
            await Task.Delay(TimeSpan.FromSeconds(delay)); // 딜레이 적용
        }

        isProcessing = false;
    }

    private async Task ProcessQueueAdaptiveAsync(Func<string, Task> displayAction, int initialChunkSize = 1, int finalChunkSize = 5,int threshold = 10, float delay = 0.1f)
    {
        isProcessing = true;

        int processedChunks = 0;

        int chunkSize = initialChunkSize;

        while (plotQueue.Count > 0)
        { 
            StringBuilder combinedChunks = new StringBuilder();

            if (processedChunks < threshold)
            {
                if (plotQueue.TryDequeue(out string chunk))
                {
                    await displayAction(chunk);
                    processedChunks++;
                }
            }
            else
            {
                for (int i = 0; i < finalChunkSize; i++)
                {
                    if (plotQueue.TryDequeue(out string chunk))
                    {
                        combinedChunks.Append(chunk);
                    }
                    else
                    {
                        break; // Queue가 비어 있으면 조기 종료
                    }
                }

                if (combinedChunks.Length > 0)
                {
                    await displayAction(combinedChunks.ToString());
                }
            }
            
            await Task.Delay(TimeSpan.FromSeconds(delay));
        }

        isProcessing = false;
    }
}