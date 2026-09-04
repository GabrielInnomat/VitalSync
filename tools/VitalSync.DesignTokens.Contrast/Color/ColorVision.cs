namespace VitalSync.DesignTokens.Contrast.Color;

internal enum ColorVision
{
    Normal,
    Protanopia,
    Deuteranopia,
    Tritanopia,
}

internal static class ColorVisionSimulator
{
    private static readonly double[] Protanopia =
    [
        0.152286, 1.052583, -0.204868,
        0.114503, 0.786281, 0.099216,
        -0.003882, -0.048116, 1.051998,
    ];

    private static readonly double[] Deuteranopia =
    [
        0.367322, 0.860646, -0.227968,
        0.280085, 0.672501, 0.047413,
        -0.011820, 0.042940, 0.968881,
    ];

    private static readonly double[] Tritanopia =
    [
        1.255528, -0.076749, -0.178779,
        -0.078411, 0.930809, 0.147602,
        0.004733, 0.691367, 0.303900,
    ];

    public static SrgbColor Simulate(SrgbColor color, ColorVision vision)
    {
        if (vision == ColorVision.Normal)
        {
            return color;
        }

        var matrix = MatrixFor(vision);
        var red = SrgbColor.Linearize(color.Red);
        var green = SrgbColor.Linearize(color.Green);
        var blue = SrgbColor.Linearize(color.Blue);

        return SrgbColor.FromLinear(
            (matrix[0] * red) + (matrix[1] * green) + (matrix[2] * blue),
            (matrix[3] * red) + (matrix[4] * green) + (matrix[5] * blue),
            (matrix[6] * red) + (matrix[7] * green) + (matrix[8] * blue));
    }

    private static double[] MatrixFor(ColorVision vision) =>
        vision switch
        {
            ColorVision.Protanopia => Protanopia,
            ColorVision.Deuteranopia => Deuteranopia,
            ColorVision.Tritanopia => Tritanopia,
            _ => throw new ArgumentOutOfRangeException(nameof(vision)),
        };
}
