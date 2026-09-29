# BloomFilter.NET

[![CI](https://github.com/joeyrobert/bloomfilter/actions/workflows/ci.yml/badge.svg)](https://github.com/joeyrobert/bloomfilter/actions/workflows/ci.yml)

A small, generic [Bloom filter](https://en.wikipedia.org/wiki/Bloom_filter) for .NET. A Bloom filter is a
space-efficient probabilistic set: false positives are possible, but false negatives are not. Items can be added,
but not removed.

Targets `netstandard2.0` and `net8.0`.

## Usage

```csharp
using DataTypes;

// 200 bits, sized for about 20 items; the optimal number of hashes is chosen for you
var bf = new BloomFilter<string>(200, 20);

bf.Add("testing");
bf.AddRange(new[] { "nottesting", "testingagain" });

bf.Contains("testing");    // true
bf.Contains("badstring");  // false (probably)

var items = new List<string> { "badstring", "testing", "test" };
bf.ContainsAll(items);     // false
bf.ContainsAny(items);     // true

bf.FalsePositiveProbability(); // expected false positive rate at the expected set size
bf.Clear();
```

Other options:

- `new BloomFilter<T>(bitSize, setSize, numberOfHashes)` sets the number of hash functions manually.
- Every constructor takes an optional `IEqualityComparer<T>` (e.g. `StringComparer.OrdinalIgnoreCase`).

## Notes

- Hashing uses `GetHashCode()` (or the supplied comparer) plus double hashing. `string.GetHashCode()` is randomized
  per process on .NET Core, so don't persist a filter across processes.
- Instances are not safe for concurrent writes.

## Changes in 2.0

- SDK-style projects targeting `netstandard2.0`/`net8.0` (was .NET 3.5); nullable annotations enabled.
- Membership no longer creates a `Random` per call and no longer shares mutable state, so `Contains` is safe to call
  concurrently once the filter is built.
- Fixed the optimal hash count, which used integer division (`bitSize / setSize`) and was often wrong.
- Added argument validation, `AddRange`, `Clear`, `BitSize`/`SetSize`, and custom comparers.
- `ContainsAny`/`ContainsAll` accept `IEnumerable<T>` (was `List<T>`).
- Added an xUnit test project and GitHub Actions CI.
- Because hash positions changed, filters built with 1.x are not compatible.

## Build and test

```
dotnet test
dotnet run --project DataTypes.BloomFilterExample
dotnet pack DataTypes.BloomFilter -c Release
```

## License

LGPL-3.0-or-later. See [LICENSE](LICENSE).
