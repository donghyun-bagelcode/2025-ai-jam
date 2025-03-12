using UnityEngine.UI;

using TMPro;

namespace Dunward.Capricorn
{
    public static class SmartComponentUtils
    {
        public static void SetText(this object textComponent, string text)
        {
            switch (textComponent)
            {
                case TMP_Text tmpText:
                    tmpText.text = text;
                    break;
                case Text uiText:
                    uiText.text = text;
                    break;
                case null:
                    break;
                default:
                    throw new System.Exception("TextUtils.SetText() is not implemented for this type.");
            }
        }

        public static void Operator(this CapricornData data, OperationType operation, string key, int value)
        {
            if (!data.variables.ContainsKey(key))
                data.variables[key] = new Ref<int>(0);

            switch (operation)
            {
                case OperationType.Set:
                    data.variables[key].Value = value;
                    break;
                case OperationType.Add:
                    data.variables[key].Value += value;
                    break;
                case OperationType.Subtract:
                    data.variables[key].Value -= value;
                    break;
                case OperationType.Multiply:
                    data.variables[key].Value *= value;
                    break;
                case OperationType.Divide:
                    data.variables[key].Value /= value;
                    break;
            }
        }
    }
}