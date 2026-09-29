using DataTypes;

var bf = new BloomFilter<string>(200, 20);

bf.Add("testing");
bf.Add("nottesting");
bf.Add("testingagain");

Console.WriteLine(bf.Contains("badstring")); // False (probably)
Console.WriteLine(bf.Contains("testing"));   // True

var testItems = new List<string> { "badstring", "testing", "test" };

Console.WriteLine(bf.ContainsAll(testItems)); // False
Console.WriteLine(bf.ContainsAny(testItems)); // True

Console.WriteLine($"False Positive Probability: {bf.FalsePositiveProbability()}");
