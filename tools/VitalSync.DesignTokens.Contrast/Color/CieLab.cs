namespace VitalSync.DesignTokens.Contrast.Color;

internal readonly record struct CieLab(double Lightness, double A, double B)
{
    private const double WhiteX = 0.9504559270516716;
    private const double WhiteZ = 1.0890577507598784;
    private const double Epsilon = 0.008856;
    private const double Kappa = 7.787;

    public static CieLab FromColor(SrgbColor color)
    {
        var red = SrgbColor.Linearize(color.Red);
        var green = SrgbColor.Linearize(color.Green);
        var blue = SrgbColor.Linearize(color.Blue);

        var x = ((0.4123907992659595 * red) + (0.35758433938387796 * green) + (0.1804807884018343 * blue)) / WhiteX;
        var y = (0.21263900587151036 * red) + (0.7151686787677559 * green) + (0.07219231536073371 * blue);
        var z = ((0.019330818715591851 * red) + (0.11919477979462599 * green) + (0.9505321522496607 * blue)) / WhiteZ;

        return new CieLab(
            (116.0 * Pivot(y)) - 16.0,
            500.0 * (Pivot(x) - Pivot(y)),
            200.0 * (Pivot(y) - Pivot(z)));
    }

    public static double Distance(CieLab first, CieLab second)
    {
        var lightness = first.Lightness - second.Lightness;
        var a = first.A - second.A;
        var b = first.B - second.B;

        return Math.Sqrt((lightness * lightness) + (a * a) + (b * b));
    }

    private static double Pivot(double value) =>
        value > Epsilon
            ? Math.Cbrt(value)
            : ((Kappa * value) + (16.0 / 116.0));
}
