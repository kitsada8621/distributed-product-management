namespace Catalog.Application.Products;

public static class PriceConverter
{
    public static decimal ToDecimal(long priceMinor)
    {
        // Adding 0.00m fixes the scale at 2 so JSON shows 1290.00, not 1290.
        return priceMinor / 100m + 0.00m;
    }

    public static bool TryToMinor(decimal price, out long priceMinor)
    {
        priceMinor = 0;
        if (price <= 0)
        {
            return false;
        }

        var minor = price * 100;
        if (minor != decimal.Truncate(minor))
        {
            return false;
        }

        priceMinor = (long)minor;
        return true;
    }
}
