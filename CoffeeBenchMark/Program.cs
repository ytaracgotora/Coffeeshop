using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

// This is the entry point that kicks off the process
var summary = BenchmarkRunner.Run<CoffeeBench>();

[MemoryDiagnoser] // This turns on the allocation tracking we want to see
public class CoffeeBench
{
    private readonly string _order = "DRINK:TripleShotLatte";

    [Benchmark]
    public string UseSplit() => _order.Split(':')[1];

    [Benchmark]
    public string UseSpan()
    {
        ReadOnlySpan<char> span = _order.AsSpan();
        int colonPos = span.IndexOf(':');
        // We call .ToString() here to make the comparison fair, 
        // as Split creates strings by default.
        return span.Slice(colonPos + 1).ToString();
    }
}



//using System;
//using System.Security.Cryptography;
//using BenchmarkDotNet.Attributes;
//using BenchmarkDotNet.Running;
//
//namespace MyBenchmarks
//{
//    public class Md5VsSha256
//    {
//        private const int N = 10000;
//        private readonly byte[] data;
//
//        private readonly SHA256 sha256 = SHA256.Create();
//        private readonly MD5 md5 = MD5.Create();
//
//        public Md5VsSha256()
//        {
//            data = new byte[N];
//            new Random(42).NextBytes(data);
//        }
//
//        [Benchmark]
//        public byte[] Sha256() => sha256.ComputeHash(data);
//
//        [Benchmark]
//        public byte[] Md5() => md5.ComputeHash(data);
//    }
//
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            var summary = BenchmarkRunner.Run<Md5VsSha256>();
//        }
//    }
//}
//