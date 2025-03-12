using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UnitDirectory : Attribute
{
    public string Directory
    {
        get;
    }

    public UnitDirectory(string directory)
    {
        Directory = directory;
    }
}