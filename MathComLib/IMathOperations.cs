using System.Runtime.InteropServices;

namespace MathComLib;

/// <summary>
/// COM-интерфейс математических операций.
/// Раздел методички 1.5 — подання типів .NET як типів COM.
/// InterfaceType(ComInterfaceType.InterfaceIsDual) — доступен как IDispatch и IUnknown.
/// </summary>
[ComVisible(true)]
[Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
[InterfaceType(ComInterfaceType.InterfaceIsDual)]
public interface IMathOperations
{
    double Add(double a, double b);
    double Subtract(double a, double b);
    double Multiply(double a, double b);
    double Divide(double a, double b);
    double Power(double baseVal, double exp);
    double SquareRoot(double value);
    double Sin(double angleDeg);
    double Cos(double angleDeg);
    double Log(double value);
    double Log10(double value);
}
