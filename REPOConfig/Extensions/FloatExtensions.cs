using System.Globalization;

namespace REPOConfig.Extensions
{
    internal static class FloatExtensions
    {
        internal static int GetDecimalPlaces(this float value)
        {
            var valueAsString = value.ToString(CultureInfo.InvariantCulture);

            var decimalPoint = valueAsString.IndexOf('.');

            return decimalPoint == -1 ? 0 : valueAsString[(decimalPoint + 1)..].Length;
        }
    }
}