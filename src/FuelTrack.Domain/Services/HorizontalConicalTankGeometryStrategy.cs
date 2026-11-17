using FuelTrack.Domain.Enums;

namespace FuelTrack.Domain.Services;

/// <summary>
/// Horizontal cylinder with two conical end-caps.
///
/// Coordinate convention:
///   - diameterCm        : outer cross-section diameter (D). Radius R = D/2.
///   - cylinderHeightCm  : length of the straight cylindrical barrel (L). Reuses the existing column name.
///   - coneHeightCm      : axial depth of EACH conical end-cap (d). Both ends are identical.
///   - liquidLevelCm     : liquid height measured from the bottom of the tank (h), 0 ≤ h ≤ D.
///   - factor            : multiplication correction factor (density / calibration).
///
/// Total tank length = L + 2d.
/// Full capacity = cylindrical volume + 2 × conical-end volume.
///
/// Wetted area formula for a horizontal circle of radius r at liquid height h (0 ≤ h ≤ 2r):
///   A(r, h) = r²·arccos((r − h)/r) − (r − h)·√(2rh − h²)
///
/// For the cylindrical section:  V_cyl = L × A(R, h)
/// For each conical end:         integrate A(R(1 − x/d), h) dx from 0 to d  (Simpson, n=200 intervals)
/// Total liquid volume (cm³) = V_cyl + 2 × V_cone_end
/// Result in litres = total_cm³ × factor / 1000
/// </summary>
[Obsolete("Not used by any station mode")]
public sealed class HorizontalConicalTankGeometryStrategy : ITankGeometryStrategy
{
    public TankShape Shape => TankShape.HorizontalCylinderWithConicalEnds;

    // Number of Simpson sub-intervals for each conical end. Must be even.
    private const int SimpsonN = 200;

    public decimal CalculateLitres(
        decimal diameterCm,
        decimal cylinderHeightCm,  // reused as barrel length L
        decimal coneHeightCm,      // axial depth of ONE conical end-cap
        decimal factor,
        decimal liquidLevelCm)
    {
        ValidateInputs(diameterCm, cylinderHeightCm, coneHeightCm, factor, liquidLevelCm);

        double R = (double)(diameterCm / 2m);
        double L = (double)cylinderHeightCm;
        double d = (double)coneHeightCm;
        double h = (double)liquidLevelCm;
        double f = (double)factor;

        // --- cylindrical barrel ---
        double aCyl = WettedCircleArea(R, h);
        double vCyl = L * aCyl;

        // --- two conical end-caps (Simpson's rule) ---
        double vOneCone = SimpsonConeEndVolume(R, d, h);
        double vTotal = vCyl + 2.0 * vOneCone;

        return decimal.Round((decimal)(vTotal * f / 1000.0), 4, MidpointRounding.AwayFromZero);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Wetted cross-sectional area of a horizontal circle of radius <paramref name="r"/>
    /// at liquid height <paramref name="h"/> (measured from bottom, 0 ≤ h ≤ 2r).
    /// A(r, h) = r²·arccos((r − h)/r) − (r − h)·√(2rh − h²)
    /// </summary>
    private static double WettedCircleArea(double r, double h)
    {
        if (h <= 0.0) return 0.0;
        if (h >= 2.0 * r) return Math.PI * r * r;
        double arg = (r - h) / r;                 // in (−1, 1)
        double inner = 2.0 * r * h - h * h;       // = r²−(r−h)² ≥ 0
        return r * r * Math.Acos(arg) - (r - h) * Math.Sqrt(inner);
    }

    /// <summary>
    /// Volume of liquid in ONE conical end-cap (apex toward the outside, base radius R, axial depth d).
    /// At axial position x from the base, the local radius = R·(1 − x/d).
    /// Integrates WettedCircleArea(r(x), h) over x ∈ [0, d] using composite Simpson's rule.
    /// </summary>
    private static double SimpsonConeEndVolume(double R, double d, double h)
    {
        if (d <= 0.0) return 0.0;
        int n = SimpsonN; // must be even
        double step = d / n;
        double sum = ConeEndIntegrand(R, d, h, 0.0) + ConeEndIntegrand(R, d, h, d);
        for (int i = 1; i < n; i++)
        {
            double x = i * step;
            sum += (i % 2 == 0 ? 2.0 : 4.0) * ConeEndIntegrand(R, d, h, x);
        }
        return sum * step / 3.0;
    }

    private static double ConeEndIntegrand(double R, double d, double h, double x)
    {
        double localR = R * (1.0 - x / d);
        return WettedCircleArea(localR, h);
    }

    private static void ValidateInputs(decimal diameterCm, decimal cylinderHeightCm, decimal coneHeightCm, decimal factor, decimal liquidLevelCm)
    {
        if (diameterCm <= 0)
            throw new ArgumentOutOfRangeException(nameof(diameterCm), "Tank diameter must be positive.");
        if (cylinderHeightCm < 0)
            throw new ArgumentOutOfRangeException(nameof(cylinderHeightCm), "Cylinder barrel length must be non-negative.");
        if (coneHeightCm <= 0)
            throw new ArgumentOutOfRangeException(nameof(coneHeightCm), "Cone end-cap depth must be positive.");
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "Multiplication factor must be positive.");
        if (liquidLevelCm < 0 || liquidLevelCm > diameterCm)
            throw new ArgumentOutOfRangeException(nameof(liquidLevelCm),
                $"Liquid level {liquidLevelCm} cm is outside calibrated tank range [0, {diameterCm}] cm.");
    }
}
