public static class LogicUtils
{
    public static bool ApplyComparison(int a, string comparator, int b)
    {
        return comparator switch {
            "LessThan" => a < b,
            "MoreThan" => a > b,
            "EqualOrLess" => a <= b,
            "EqualOrMore" => a >= b,
            "Equals" => a == b,
            _ => false,
        };
    }
}