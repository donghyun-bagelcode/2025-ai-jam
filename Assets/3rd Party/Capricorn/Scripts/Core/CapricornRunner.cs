using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Dunward.Capricorn
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapricornDialogue))]
    [RequireComponent(typeof(CapricornData))]
    [RequireComponent(typeof(CapricornSettings))]
    [RequireComponent(typeof(CapricornCache))]
    public partial class CapricornRunner : MonoBehaviour
    {
#region Unity Inspector Fields
        public bool isDebug;
#endregion

        private CapricornDialogue dialogue;
        private CapricornData data;
        private CapricornSettings settings;
        private CapricornCache cache;

        private GraphData graphData;

        internal Dictionary<int, NodeMainData> nodes = new Dictionary<int, NodeMainData>();

        internal UnityEvent bindingInteraction;
        public Func<List<string>, List<UnityEngine.UI.Button>> onSelectionCreate;
        public Action<string, string, string> onBeforeTextTyping;

        public event CoroutineDelegate AddCustomCoroutines;
        public delegate IEnumerator CoroutineDelegate(CoroutineUnit unit, CapricornRunner runner, CapricornSettings settings, CapricornCache cache, CapricornData data);

        public float selectionDestroyAfterDelay = 1f;

        private int nextNodeIndex = -1;
        private bool isInitialized = false;

        public NodeMainData StartNode
        {
            get => graphData.nodes.Find(node => node.nodeType == NodeType.Input);
        }

        public NodeMainData DebugNode
        {
            get => graphData.nodes.Find(node => node.id == graphData.debugNodeIndex);
        }

        private void Initialize()
        {
            dialogue = GetComponent<CapricornDialogue>();
            data = GetComponent<CapricornData>();
            settings = GetComponent<CapricornSettings>();
            cache = GetComponent<CapricornCache>();

            dialogue.Initialize();
            isInitialized = true;
        }

        public void Load(string json)
        {
            if (!isInitialized) Initialize();

            graphData = JsonConvert.DeserializeObject<GraphData>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });

            nodes.Clear();

            foreach (var node in graphData.nodes)
            {
                nodes.Add(node.id, node);
            }
        }

        public void Clear()
        {
            if (!isInitialized) Initialize();

            dialogue.NameTarget.SetText("");
            dialogue.SubNameTarget.SetText("");
            dialogue.ScriptTarget.SetText("");

            Destroy(cache.lastBackground);
            Destroy(cache.lastForeground);
            Destroy(cache.bgmObject);

            foreach (var character in data.characters)
            {
                Destroy(character.Value);
            }

            data.characters.Clear();
        }

        public void ClearVariables()
        {
            foreach (var variable in data.variables)
            {
                variable.Value.Value = 0;
            }
        }

        public IEnumerator Run()
        {
            var currentNode = isDebug ? DebugNode : StartNode;

            while (true)
            {
                yield return RunCoroutine(currentNode.coroutineData);

                yield return RunAction(currentNode.actionData.action);

                if (currentNode.nodeType == NodeType.Output) yield break;
                currentNode = Next();
            }
        }

        private IEnumerator RunCoroutine(NodeCoroutineData data)
        {
            foreach (var coroutine in data.coroutines)
            {
                if (coroutine.isWaitingUntilFinish)
                {
                    yield return StartCoroutine(ExecuteCoroutine(coroutine));
                }
                else
                {
                    StartCoroutine(ExecuteCoroutine(coroutine));
                }
            }
        }

        private IEnumerator ExecuteCoroutine(CoroutineUnit unit)
        {
            switch (unit)
            {
                case WaitUnit waitUnit:
                    yield return waitUnit.Execute();
                    break;
                case ShowCharacterUnit showCharacterUnit:
                    yield return showCharacterUnit.Execute(settings.characterArea, data.characters);
                    break;
                case ChangeBackgroundUnit changeBackgroundUnit:
                    yield return changeBackgroundUnit.Execute(settings.backgroundArea, cache.lastBackground);
                    break;
                case ChangeForegroundUnit changeForegroundUnit:
                    yield return changeForegroundUnit.Execute(settings.foregroundArea, cache.lastForeground);
                    break;
                case DeleteForegroundUnit deleteForegroundUnit:
                    yield return deleteForegroundUnit.Execute(cache.lastForeground);
                    break;
                case DeleteCharacterUnit deleteCharacterUnit:
                    yield return deleteCharacterUnit.Execute(data.characters);
                    break;
                case DeleteAllCharacterUnit deleteAllCharacterUnit:
                    yield return deleteAllCharacterUnit.Execute(data.characters);
                    break;
                case SetAllCharacterHighlight setAllCharacterHighlight:
                    yield return setAllCharacterHighlight.Execute(data.characters);
                    break;
                case ClearDialogueTextUnit clearDialogueTextUnit:
                    yield return clearDialogueTextUnit.Execute(dialogue.NameTarget, dialogue.SubNameTarget, dialogue.ScriptTarget);
                    break;
                case PlayBGMUnit playBGMUnit:
                    yield return playBGMUnit.Execute(cache.bgmObject);
                    break;
                case StopBGMUnit stopBGMUnit:
                    yield return stopBGMUnit.Execute(cache.bgmObject);
                    break;
                case PlaySFXUnit playSFXUnit:
                    yield return playSFXUnit.Execute();
                    break;
                case TransformCharacterUnit transformCharacterUnit:
                    yield return transformCharacterUnit.Execute(data.characters);
                    break;
                case SetVariableUnit setVariableUnit:
                    yield return setVariableUnit.Execute(data);
                    break;
                case SetRandomVariableUnit setRandomVariableUnit:
                    yield return setRandomVariableUnit.Execute(data);
                    break;
                default:
                    if (AddCustomCoroutines != null)
                    {
                        foreach (CoroutineDelegate coroutine in AddCustomCoroutines.GetInvocationList())
                        {
                            yield return coroutine(unit, this, settings, cache, data);
                        }
                    }
                    break;
            }
        }

        private IEnumerator RunAction(ActionUnit action)
        {
            nextNodeIndex = action.GetNextNodeIndex();
            switch (action)
            {
                case TextTypingUnit typing:
                {
                    onBeforeTextTyping?.Invoke(typing.name, typing.subName, typing.script);
                    bindingInteraction.AddListener(typing.Interaction);
                    yield return typing.Execute(dialogue.NameTarget, dialogue.SubNameTarget, dialogue.ScriptTarget);
                    bindingInteraction.RemoveListener(typing.Interaction);
                    break;
                }

                case SelectionUnit selection:
                {
                    var selections = onSelectionCreate.Invoke(selection.scripts);
                    yield return selection.Execute(selections, selectionDestroyAfterDelay);
                    nextNodeIndex = selection.GetNextNodeIndex();
                    break;
                }

                case VariableSelectionUnit selection:
                {
                    var selections = onSelectionCreate.Invoke(selection.selections.ConvertAll(s => s.script));
                    yield return selection.Execute(selections, data, selectionDestroyAfterDelay);
                    nextNodeIndex = selection.GetNextNodeIndex();
                    break;
                }
            }

        }

        private NodeMainData Next()
        {
            return nodes[nextNodeIndex];
        }
    }
}