using System.Collections;
using System.Collections.Generic;
using Dunward.Capricorn;
using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    [SerializeField]
    private NavigationDatabase database;

    [SerializeField]
    private CapricornData data;

    [SerializeField]
    private Transform parent;

    [SerializeField]
    private GameObject template;

    public void Initialize()
    {
        foreach (var navigation in database.navigations)
        {
            if (data.variables[navigation.Key] == null)
                data.variables[navigation.Key] = new Ref<int>(0);
                
            var go = Instantiate(template, parent);
            var instance = go.GetComponent<NavigationInstance>();
            instance.Initialize(navigation.Value, data.variables[navigation.Key]);
        }
    }
}
