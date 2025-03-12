using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dunward.Capricorn;
using UnityEngine;
using UnityEngine.UI;

[UnitDirectory("Other")]
public class SetActiveNavigationUnit : CoroutineUnit
{
    public bool isActive;

#if UNITY_EDITOR
    protected override string info => "Set Active Navigation Unit";
    protected override bool supportWaitingFinish => false;

    public override void OnGUI(Rect rect, ref float height)
    {
        isActive = UnityEditor.EditorGUI.Toggle(new Rect(rect.x, rect.y + height, rect.width, UnityEditor.EditorGUIUtility.singleLineHeight), "Is Active", isActive);
        height += UnityEditor.EditorGUIUtility.singleLineHeight;
    }

    public override float GetHeight()
    {
        return base.GetHeight() + UnityEditor.EditorGUIUtility.singleLineHeight;
    }
#endif

    public override IEnumerator Execute(params object[] args)
    {
        var obj = args[0] as GameObject;
        obj.SetActive(isActive);
        yield return null;
    }
}