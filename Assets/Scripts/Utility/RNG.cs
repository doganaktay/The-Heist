using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RNG
{
    private uint seed;
    private int position;

    public RNG(uint seed = 0)
    {
        this.seed = seed;
    }

    #region Seed

    public void ResetSeed(uint seed, int position = 0) { this.seed = seed; this.position = position; }
    public uint GetSeed() => seed;
    public void SetCurrentPosition(int position) => this.position = position;
    public int GetCurrentPosition() => position;

    #endregion

    #region Roll

    public float Roll() => NextSingleInclusive();
    public bool Roll(float successPercent) => NextSingleInclusive() <= successPercent;

    #endregion

    #region RNG

    const double REAL_UNIT_INT_INCLUSIVE = 1.0 / ((double)int.MaxValue);
    const double REAL_UNIT_UINT_INCLUSIVE = 1.0 / ((double)uint.MaxValue);
    const double REAL_UNIT_INT_EXCLUSIVE = 1.0 / ((double)int.MaxValue + 1.0);
    const double REAL_UNIT_UINT_EXCLUSIVE = 1.0 / ((double)uint.MaxValue + 1.0);

    /// <summary>
    /// Generates a uint over [uint.MinValue, uint.MaxValue]
    /// </summary>
    /// <returns></returns>
    public uint NextUint() => Noise(position++, seed);

    /// <summary>
    /// Generates a random int over [0, int.MaxValue)
    /// </summary>
    /// <returns></returns>
    public int Next()
    {
        uint rtn = NextUint() & 0x7FFFFFFF;
        if (rtn == 0x7FFFFFFF)
            return Next();

        return (int)rtn;
    }

    /// <summary>
    /// Generates a random int over [0, upperBound)
    /// </summary>
    /// <param name="upperBound"></param>
    /// <returns></returns>
    public int Next(int upperBound)
    {
        if (upperBound < 0)
            throw new ArgumentOutOfRangeException("upperBound", upperBound, "upperBound must be >= 0");

        return (int)(REAL_UNIT_INT_EXCLUSIVE * (int)(0x7FFFFFFF & NextUint()) * upperBound);
    }

    /// <summary>
    /// Generates a random int over [lowerBound, upperBound)
    /// upperBound must be >= lowerBound. lowerBound may be negative.
    /// </summary>
    /// <param name="lowerBound"></param>
    /// <param name="upperBound"></param>
    /// <returns></returns>
    public int Next(int lowerBound, int upperBound)
    {
        if (lowerBound > upperBound)
            throw new ArgumentOutOfRangeException("upperBound", upperBound, "upperBound must be >= lowerBound");

        int range = upperBound - lowerBound;
        if(range < 0)
        {
            // if range is < 0 here, we have overflow.
            // must resort to using longs and full 32-bit precision

            return lowerBound + (int)((REAL_UNIT_UINT_EXCLUSIVE * (double)NextUint()) * (double)((long)upperBound - (long)lowerBound));
        }

        // if range <= int.MaxValue, 31-bit precision suffices
        return lowerBound + (int)((REAL_UNIT_INT_EXCLUSIVE * (double)(int)(0x7FFFFFFF & NextUint())) * (double)range);
    }

    /// <summary>
    /// Generates a random double over [0.0, 1.0)
    /// </summary>
    /// <returns></returns>
    public double NextDoubleExclusive() => REAL_UNIT_INT_EXCLUSIVE * (int)(0x7FFFFFFF & NextUint());

    /// <summary>
    /// Generates a random single over [0.0, 1.0)
    /// </summary>
    /// <returns></returns>
    public float NextSingleExclusive(float min = 0f, float max = 1f) => Lerp(min, max, (float)NextDoubleExclusive());

    /// <summary>
    /// Generates a random double over [0.0, 1.0)
    /// </summary>
    /// <returns></returns>
    public double NextDoubleInclusive() => REAL_UNIT_INT_INCLUSIVE * (int)(0x7FFFFFFF & NextUint());

    /// <summary>
    /// Generates a random single over [0.0, 1.0)
    /// </summary>
    /// <returns></returns>
    public float NextSingleInclusive(float min = 0f, float max = 1f) => Lerp(min, max, (float)NextDoubleInclusive());

    #endregion

    #region Unity Random equivalents

    /// <summary>
    /// Generates a random int over [min, max)
    /// max must be >= min. min may be negative.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public int Range(int min, int max) => Next(min, max);

    /// <summary>
    /// Generates a random float over [min, max]
    /// max must be >= min. min may be negative.
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public float Range(float min, float max) => NextSingleInclusive(min, max);

    public Vector2 NextOnUnitCircle()
    {
        var angle = NextSingleExclusive(0, 360f) * Mathf.Deg2Rad;
        var x = Mathf.Cos(angle);
        var y = Mathf.Sin(angle);
        return new Vector2(x, y);
    }

    public Vector3 NextOnUnitSphere()
    {
        var angle = NextSingleExclusive(0, 360f) * Mathf.Deg2Rad;
        var z = NextSingleExclusive(-1f, 1f);
        var mp = Mathf.Sqrt(1 - z * z);
        var x = mp * Mathf.Cos(angle);
        var y = mp * Mathf.Sin(angle);
        return new Vector3(x, y, z);
    }

    public Vector3 NextInsideUnitSphere()
    {
        return NextOnUnitSphere() * NextSingleExclusive();
    }

    public Vector2 NextInsideUnitCircle()
    {
        return NextOnUnitCircle() * NextSingleExclusive();
    }

    public Quaternion NextRotation()
    {
        var x = NextSingleExclusive(0f, 360f);
        var y = NextSingleExclusive(0f, 360f);
        var z = NextSingleExclusive(0f, 360f);
        return Quaternion.Euler(x, y, z);
    }

    public Quaternion NextRotationUniform()
    {
        float normal, w, x, y, z;

        do
        {
            w = NextSingleExclusive(-1f, 1f);
            x = NextSingleExclusive(-1f, 1f);
            y = NextSingleExclusive(-1f, 1f);
            z = NextSingleExclusive(-1f, 1f);
            normal = w * w + x * x + y * y + z * z;
        }
        while (normal > 1f || normal == 0f);

        normal = Mathf.Sqrt(normal);
        return new Quaternion(x / normal, y / normal, z / normal, w / normal);
    }

    public Color NextColorHsv()
    {
        return NextColorHsv(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f);
    }

    public Color NextColorHsv(float minHue, float maxHue)
    {
        return NextColorHsv(minHue, maxHue, 0f, 1f, 0f, 1f, 1f, 1f);
    }

    public Color NextColorHsv(
        float minHue,
        float maxHue,
        float minSaturation,
        float maxSaturation)
    {
        return NextColorHsv(minHue, maxHue, minSaturation, maxSaturation, 0f, 1f, 1f, 1f);
    }

    public Color NextColorHsv(
        float minHue,
        float maxHue,
        float minSaturation,
        float maxSaturation,
        float minValue,
        float maxValue)
    {
        return NextColorHsv(minHue, maxHue, minSaturation, maxSaturation, minValue, maxValue, 1f, 1f);
    }

    public Color NextColorHsv(
        float minHue,
        float maxHue,
        float minSaturation,
        float maxSaturation,
        float minValue,
        float maxValue,
        float minAlpha,
        float maxAlpha)
    {
        var h = Lerp(minHue, maxHue, NextSingleExclusive());
        var s = Lerp(minSaturation, maxSaturation, NextSingleExclusive());
        var v = Lerp(minValue, maxValue, NextSingleExclusive());
        var color = Color.HSVToRGB(h, s, v, true);
        color.a = Lerp(minAlpha, maxAlpha, NextSingleExclusive());
        return color;
    }

    #endregion

    #region Squirrel 3

    static uint Noise(int position, uint seed = 0)
    {
        const uint BIT_NOISE1 = 0xB5297A4D;
        const uint BIT_NOISE2 = 0x68E31DA4;
        const uint BIT_NOISE3 = 0x1B56C4E9;

        uint mangled = (uint)position;
        mangled *= BIT_NOISE1;
        mangled += seed;
        mangled ^= (mangled >> 8);
        mangled += BIT_NOISE2;
        mangled ^= (mangled << 8);
        mangled *= BIT_NOISE3;
        mangled ^= (mangled >> 8);

        return mangled;
    }

    static uint Noise2D(int posX, int posY, uint seed = 0)
    {
        const int PRIME = 198491317;
        return Noise(posX + (PRIME * posY), seed);
    }

    static uint Noise3D(int posX, int posY, int posZ, uint seed = 0)
    {
        const int PRIME1 = 198491317;
        const int PRIME2 = 6542989;
        return Noise(posX + (PRIME1 * posY) + (PRIME2 * posZ), seed);
    }

    #endregion

    #region Utility

    public static float Clamp01(float value)
    {
        if (value < 0f)
            return 0f;
        else if (value > 1f)
            return 1f;
        else
            return value;
    }

    public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);

    #endregion
}
