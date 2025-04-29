public static class LogicUtils
{
    public static bool ApplyComparison(int a, string comparator, int b)
    {
        switch (comparator)
        {
            case "LessThan": return a < b;
            case "MoreThan": return a > b;
            case "EqualOrLess": return a <= b;
            case "EqualOrMore": return a >= b;
            case "Equals": return a == b;
            default: return false;
        }
    }
}