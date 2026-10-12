using Catalog.Application.Products;

namespace Catalog.UnitTests.Products;

public class PriceConverterTests
{
    [Fact]
    public void ToDecimal_KeepsTwoDecimals()
    {
        var price = PriceConverter.ToDecimal(129000);

        Assert.Equal(1290.00m, price);
        Assert.Equal("1290.00", price.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TryToMinor_TwoDecimals_Converts()
    {
        var ok = PriceConverter.TryToMinor(12.34m, out var minor);

        Assert.True(ok);
        Assert.Equal(1234, minor);
    }

    [Fact]
    public void TryToMinor_MoreThanTwoDecimals_Fails()
    {
        Assert.False(PriceConverter.TryToMinor(12.345m, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TryToMinor_ZeroOrNegative_Fails(int price)
    {
        Assert.False(PriceConverter.TryToMinor(price, out _));
    }
}
