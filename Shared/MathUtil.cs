namespace Pinglingle.Shared;

public static class MathUtil
{
    public static double Percentile(IReadOnlyList<int> numbers, double p)
    {
        if (numbers.Count == 0) return 0;

        // Returns the pth percentile of the numbers, which are presumed to be sorted.
        var maxIndex = numbers.Count - 1;
        var normP = p / 100.0;
        var lowerIndex = Math.Max(0, Math.Min(maxIndex,
            (int)Math.Floor(maxIndex * normP)));
        var upperIndex = Math.Max(0, Math.Min(maxIndex,
            (int)Math.Ceiling(maxIndex * normP)));

        if (lowerIndex == upperIndex) return numbers[lowerIndex];

        var lowerP = (double)lowerIndex / maxIndex;
        var upperP = (double)upperIndex / maxIndex;
        var lerpValue = (normP - lowerP) / (upperP - lowerP);

        return lerpValue * numbers[lowerIndex] + (1.0 - lerpValue) * numbers[upperIndex];
    }
}