using System.Collections;
using UnityEngine;

public class MiniGameBehaviour : MonoBehaviour
{
    private bool flag = true;

    protected void Exit()
    {
        flag = false;
    }

    public IEnumerator Run()
    {
        while (flag)
        {
            yield return null;
        }
    }
}