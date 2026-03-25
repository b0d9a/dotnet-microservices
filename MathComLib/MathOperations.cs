using System.Runtime.InteropServices;

namespace MathComLib;

/// <summary>
/// COM-класс (соклас) реализующий IMathOperations.
/// Демонстрирует раздел методички 1.5–1.8:
///   - ComVisible(true)        — открывает тип для COM-клиентов
///   - ClassInterface(None)    — скрывает авто-интерфейс; клиент видит только IMathOperations
///   - ProgId                  — удобное имя для CreateObject("MathComLib.MathOperations")
///   - Guid                    — фиксированный CLSID
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[ProgId("MathComLib.MathOperations")]
[Guid("B2C3D4E5-F6A7-8901-BCDE-F12345678901")]
public class MathOperations : IMathOperations
{
    public double Add(double a, double b) => a + b;

    public double Subtract(double a, double b) => a - b;

    public double Multiply(double a, double b) => a * b;

    public double Divide(double a, double b)
    {
        if (b == 0)
            throw new DivideByZeroException("Divisor cannot be zero.");
        return a / b;
    }

    public double Power(double baseVal, double exp) => Math.Pow(baseVal, exp);

    public double SquareRoot(double value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Cannot take square root of a negative number.");
        return Math.Sqrt(value);
    }

    // Принимает градусы — более понятно для демонстрации
    public double Sin(double angleDeg) => Math.Sin(angleDeg * Math.PI / 180.0);

    public double Cos(double angleDeg) => Math.Cos(angleDeg * Math.PI / 180.0);

    public double Log(double value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Logarithm argument must be positive.");
        return Math.Log(value);
    }

    public double Log10(double value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Logarithm argument must be positive.");
        return Math.Log10(value);
    }
}
