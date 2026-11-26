namespace VitalSync.DesignSystem.Patterns.Data;

internal sealed record MetricChartScale(decimal Low, decimal High, decimal Step, int TickDecimals)
{
    private const int MaximumIntervals = 4;

    private static readonly decimal[] NiceFactors = [1m, 2m, 2.5m, 5m];

    public IReadOnlyList<decimal> Ticks
    {
        get
        {
            var ticks = new List<decimal>();
            for (var tick = Low; tick <= High; tick += Step)
            {
                ticks.Add(tick);
            }

            return ticks;
        }
    }

    public decimal Position(decimal value) => (High - value) / (High - Low);

    public static MetricChartScale Create(decimal minimum, decimal maximum, bool fromZero, int decimals)
    {
        var smallestStep = PowerOfTen(-decimals);
        var low = fromZero ? Math.Min(0m, minimum) : minimum;
        var high = maximum;

        if (high == low)
        {
            if (fromZero)
            {
                high = low + (MaximumIntervals * smallestStep);
            }
            else
            {
                low -= 2m * smallestStep;
                high += 2m * smallestStep;
            }
        }

        var rawStep = (high - low) / MaximumIntervals;
        var magnitude = PowerOfTen((int)Math.Floor(Math.Log10((double)rawStep)));

        while (true)
        {
            foreach (var factor in NiceFactors)
            {
                var step = factor * magnitude;
                if (step < rawStep || step < smallestStep || step % smallestStep != 0m)
                {
                    continue;
                }

                var tickLow = Math.Floor(low / step) * step;
                var tickHigh = Math.Ceiling(high / step) * step;
                if ((tickHigh - tickLow) / step <= MaximumIntervals)
                {
                    return new MetricChartScale(tickLow, tickHigh, step, DecimalPlaces(step));
                }
            }

            magnitude *= 10m;
        }
    }

    private static decimal PowerOfTen(int exponent)
    {
        var result = 1m;
        for (var i = 0; i < Math.Abs(exponent); i++)
        {
            result = exponent > 0 ? result * 10m : result / 10m;
        }

        return result;
    }

    private static int DecimalPlaces(decimal step)
    {
        var places = 0;
        while (step % 1m != 0m)
        {
            step *= 10m;
            places++;
        }

        return places;
    }
}
