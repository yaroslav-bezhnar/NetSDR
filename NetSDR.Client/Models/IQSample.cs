namespace NetSDR.Client.Models;

public readonly struct IQSample(short i, short q)
{
    #region properties

    public short I { get; } = i;
    public short Q { get; } = q;

    public double Magnitude => Math.Sqrt(I * I + Q * Q);
    public double Phase => Math.Atan2(Q, I);

    #endregion
}