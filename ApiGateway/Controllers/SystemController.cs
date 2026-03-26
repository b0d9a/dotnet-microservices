using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ApiGateway.Interop;
using MathComLib;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// Раздел 1.14 методички: атрибут RuntimeCompatibility.
// WrapNonExceptionThrows=true (по умолчанию) — CLR оборачивает не-CLS исключения
// из COM в RuntimeWrappedException, чтобы их можно было поймать через catch(Exception).
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]

namespace ApiGateway.Controllers;

[ApiController]
[Route("system")]
// [Authorize]
public class SystemController : ControllerBase
{
    // ── PInvoke: память (GlobalMemoryStatusEx) ────────────────────────────────
    [HttpGet("info")]
    public IActionResult GetSystemInfo()
    {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
            return Ok(new
            {
                source    = "Win32 API — kernel32.dll GlobalMemoryStatusEx (PInvoke)",
                note      = "P/Invoke is a Windows-only feature. This endpoint requires Windows OS to call kernel32.dll.",
                platform  = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                timestamp = DateTime.UtcNow,
            });

        try
        {
            var info = Win32Memory.GetMemoryInfo();
            return Ok(new
            {
                source              = "Win32 API — kernel32.dll GlobalMemoryStatusEx (PInvoke)",
                memoryLoadPercent   = info.MemoryLoadPercent,
                totalPhysicalMB     = info.TotalPhysicalMB,
                availablePhysicalMB = info.AvailablePhysicalMB,
                usedPhysicalMB      = info.TotalPhysicalMB - info.AvailablePhysicalMB,
                totalPageFileMB     = info.TotalPageFileMB,
                availablePageFileMB = info.AvailablePageFileMB,
                totalVirtualMB      = info.TotalVirtualMB,
                availableVirtualMB  = info.AvailableVirtualMB,
                timestamp           = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ── PInvoke: процессор (GetSystemInfo) ────────────────────────────────────
    [HttpGet("cpu")]
    public IActionResult GetCpuInfo()
    {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows))
            return Ok(new
            {
                source    = "Win32 API — kernel32.dll GetSystemInfo (PInvoke)",
                note      = "P/Invoke is a Windows-only feature. This endpoint requires Windows OS to call kernel32.dll.",
                platform  = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                timestamp = DateTime.UtcNow,
            });

        try
        {
            var cpu = Win32Memory.GetCpuInfo();
            return Ok(new
            {
                source               = "Win32 API — kernel32.dll GetSystemInfo (PInvoke)",
                architecture         = cpu.Architecture,
                numberOfProcessors   = cpu.NumberOfProcessors,
                pageSizeBytes        = cpu.PageSizeBytes,
                processorLevel       = cpu.ProcessorLevel,
                processorRevision    = cpu.ProcessorRevision,
                allocationGranularity = cpu.AllocationGranularity,
                timestamp            = DateTime.UtcNow,
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ── COM Interop: калькулятор через MathComLib (RCW) ───────────────────────
    /// <summary>
    /// Демонстрирует раздел 1.5–1.8 методички:
    /// .NET-клиент обращается к COM-объекту MathOperations через интерфейс IMathOperations.
    /// CLR создаёт Runtime Callable Wrapper (RCW) автоматически.
    /// </summary>
    [HttpGet("com-calc")]
    public IActionResult ComCalc(
        [FromQuery] double a = 10,
        [FromQuery] double b = 3,
        [FromQuery] string op = "all")
    {
        // Раздел 1.13: создание COM-объекта в коде .NET
        // В .NET 5+ ComVisible-типы внутри одной сборки используются напрямую через интерфейс.
        // Здесь IMathOperations выступает как COM-интерфейс (InterfaceIsDual).
        IMathOperations calc = new MathOperations();

        try
        {
            // Раздел 1.14: обработка COM-исключений.
            // DivideByZeroException и ArgumentOutOfRangeException — управляемые исключения,
            // но в реальном COM-сервере они передавались бы как HRESULT через IErrorInfo.
            var result = op.ToLower() switch
            {
                "add"      => (object)new { operation = "add",      result = calc.Add(a, b) },
                "sub"      => new { operation = "subtract",  result = calc.Subtract(a, b) },
                "mul"      => new { operation = "multiply",  result = calc.Multiply(a, b) },
                "div"      => new { operation = "divide",    result = calc.Divide(a, b) },
                "pow"      => new { operation = "power",     result = calc.Power(a, b) },
                "sqrt"     => new { operation = "sqrt",      result = calc.SquareRoot(a) },
                "sin"      => new { operation = "sin(deg)",  result = calc.Sin(a) },
                "cos"      => new { operation = "cos(deg)",  result = calc.Cos(a) },
                "log"      => new { operation = "ln",        result = calc.Log(a) },
                "log10"    => new { operation = "log10",     result = calc.Log10(a) },
                _          => new
                {
                    operation = "all",
                    result    = 0.0,    // placeholder — заменяется объектом ниже
                },
            };

            // Если запрошены все операции — возвращаем полную таблицу
            if (op.ToLower() == "all")
            {
                return Ok(new
                {
                    source        = "COM Interop — MathComLib.MathOperations via IMathOperations (RCW)",
                    comInterface  = nameof(IMathOperations),
                    comClass      = nameof(MathOperations),
                    progId        = "MathComLib.MathOperations",
                    a, b,
                    add           = calc.Add(a, b),
                    subtract      = calc.Subtract(a, b),
                    multiply      = calc.Multiply(a, b),
                    divide        = b != 0 ? (double?)calc.Divide(a, b) : null,
                    power         = calc.Power(a, b),
                    sqrtA         = a >= 0 ? (double?)calc.SquareRoot(a) : null,
                    sinA_deg      = calc.Sin(a),
                    cosA_deg      = calc.Cos(a),
                    lnA           = a > 0 ? (double?)calc.Log(a) : null,
                    log10A        = a > 0 ? (double?)calc.Log10(a) : null,
                    timestamp     = DateTime.UtcNow,
                });
            }

            return Ok(new
            {
                source       = "COM Interop — MathComLib.MathOperations (RCW)",
                a, b,
                data         = result,
                timestamp    = DateTime.UtcNow,
            });
        }
        catch (DivideByZeroException ex)
        {
            // Раздел 1.14: перехват CLS-совместимого исключения из COM-метода
            return BadRequest(new { message = "COM method raised DivideByZeroException", detail = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = "COM method raised ArgumentOutOfRangeException", detail = ex.Message });
        }
        catch (COMException ex)
        {
            // Раздел 1.14: COMException — обёртка для HRESULT-ошибок из COM-сервера
            return StatusCode(500, new { message = "COM error", hresult = ex.HResult, detail = ex.Message });
        }
        catch (Exception ex)
        {
            // RuntimeWrappedException: non-CLS исключение из COM оборачивается CLR
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
