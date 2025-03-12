public enum ConditionMode
{
    Greater, // a > b
    GreaterOrEqual, // a >= b
    Less, // a < b
    LessOrEqual, // a <= b
    Equals, // a == b
    NotEqual // a != b
}

public enum ConditionResult
{
    DisableInteraction,
    Destroy
}