using DataTypes;
using Xunit;

public class BloomFilterTests
{
    [Fact]
    public void NoFalseNegatives()
    {
        var bf = new BloomFilter<int>(10_000, 1_000);
        for (int i = 0; i < 1_000; i++) bf.Add(i);
        for (int i = 0; i < 1_000; i++) Assert.True(bf.Contains(i));
    }

    [Fact]
    public void FalsePositiveRateIsNearPrediction()
    {
        var bf = new BloomFilter<int>(10_000, 1_000);
        for (int i = 0; i < 1_000; i++) bf.Add(i);
        int fp = 0;
        for (int i = 1_000_000; i < 1_010_000; i++) if (bf.Contains(i)) fp++;
        double observed = fp / 10_000.0;
        Assert.InRange(observed, 0, bf.FalsePositiveProbability() * 2 + 0.005);
    }

    [Fact]
    public void OptimalHashesUsesFloatingPointMath()
    {
        Assert.Equal(7, new BloomFilter<string>(10_000, 1_000).NumberOfHashes);
        Assert.Equal(1, new BloomFilter<string>(10, 20).NumberOfHashes);
    }

    [Fact]
    public void ContainsAnyAndAll()
    {
        var bf = new BloomFilter<string>(1_000, 10);
        bf.AddRange(new[] { "a", "b", "c" });
        Assert.True(bf.ContainsAll(new[] { "a", "b" }));
        Assert.True(bf.ContainsAny(new[] { "zzz-not-here-1", "c" }));
        Assert.False(new BloomFilter<string>(1_000, 10).ContainsAny(new[] { "a" }));
    }

    [Fact]
    public void ClearEmptiesFilter()
    {
        var bf = new BloomFilter<string>(1_000, 10);
        bf.Add("x");
        bf.Clear();
        Assert.False(bf.Contains("x"));
    }

    [Fact]
    public void CustomComparerIsUsed()
    {
        var bf = new BloomFilter<string>(1_000, 10, StringComparer.OrdinalIgnoreCase);
        bf.Add("Hello");
        Assert.True(bf.Contains("HELLO"));
    }

    [Fact]
    public void NullItemsAreSupported()
    {
        var bf = new BloomFilter<string?>(100, 10);
        bf.Add(null);
        Assert.True(bf.Contains(null));
    }

    [Fact]
    public void InvalidArgumentsThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BloomFilter<int>(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BloomFilter<int>(10, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BloomFilter<int>(10, 1, 0));
        Assert.Throws<ArgumentNullException>(() => new BloomFilter<int>(10, 1).ContainsAll(null!));
    }
}
