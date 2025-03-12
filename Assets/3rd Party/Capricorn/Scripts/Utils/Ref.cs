using System;
using UnityEngine;

namespace Dunward.Capricorn
{
    [Serializable]
    public class Ref<T>
    {
        public Action<T> onValueChanged;

        [SerializeField][HideInInspector] private T _value;

        public T Value 
        {
            get => _value;
            set
            {
                _value = value;
                onValueChanged?.Invoke(_value);
            }
        }

        public Ref(T value)
        {
            Value = value;
        }

        public static implicit operator T(Ref<T> reference)
        {
            return reference.Value;
        }
    }
}