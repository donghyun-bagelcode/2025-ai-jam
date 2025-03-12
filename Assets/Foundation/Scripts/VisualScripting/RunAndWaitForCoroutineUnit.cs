using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// Start coroutine and wait until it is over.
[UnitTitle("Run and Wait For Coroutine")]
[UnitShortTitle("Coroutine")]
public class RunAndWaitForCoroutineUnit : WaitUnit
{
    /// The coroutine to start and wait for.
    [DoNotSerialize]
    public ValueInput enumerator { get; private set; }

    [DoNotSerialize]
    public ValueOutput target;

    protected override void Definition()
    {
        base.Definition();

        enumerator = ValueInput<IEnumerator>(nameof(enumerator));
        Requirement(enumerator, enter);
    }

    protected override IEnumerator Await(Flow flow)
    {
        MonoBehaviour scriptInstanceValue = flow.stack.machine as ScriptMachine;
        IEnumerator coroutineEnumeratorValue = flow.GetValue<IEnumerator>(this.enumerator);
        yield return scriptInstanceValue.StartCoroutine(coroutineEnumeratorValue);
        yield return exit;
    }
}