using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Diagnosers;
using Matrix.Core;

namespace Matrix.Benchmarks;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, baseline: true)]
[Config(typeof(BenchmarkConfig))]
public class MatrixBenchmarks
{
    private class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddExporter(CsvExporter.Default);
            AddExporter(MarkdownExporter.GitHub);
            AddExporter(HtmlExporter.Default);
            AddExporter(RPlotExporter.Default);
            
            AddColumn(StatisticColumn.Mean);
            AddColumn(StatisticColumn.StdDev);
            AddColumn(StatisticColumn.Median);
            AddColumn(StatisticColumn.Min);
            AddColumn(StatisticColumn.Max);
            
            AddDiagnoser(MemoryDiagnoser.Default);
            AddDiagnoser(ThreadingDiagnoser.Default);
        }
    }

    [Params(10, 50, 100, 500, 1000)]
    public int Size { get; set; }

    private Matrix<int> _matrix = null!;
    private Matrix<int> _sourceMatrix = null!;

    [GlobalSetup]
    public void Setup()
    {
        _matrix = new Matrix<int>((uint)Size, (uint)Size);
        _sourceMatrix = new Matrix<int>((uint)Size, (uint)Size);
        
        for (uint x = 0; x < Size; x++)
        for (uint y = 0; y < Size; y++)
            _sourceMatrix.Set(x, y, (int)(x * Size + y));
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _matrix.Dispose();
        _sourceMatrix.Dispose();
    }

    [Benchmark(Description = "Fill_Async")]
    public async Task FillAsync()
    {
        await _matrix.FillAsync((x, y) => (int)(x * Size + y));
    }

    [Benchmark(Baseline = true, Description = "Fill_Sync_Sequential")]
    public void FillSyncSequential()
    {
        for (uint x = 0; x < Size; x++)
        for (uint y = 0; y < Size; y++)
            _matrix.Set(x, y, (int)(x * Size + y));
    }

    [Benchmark(Description = "Fill_Sync_Parallel")]
    public void FillSyncParallel()
    {
        Parallel.For(0, Size, x =>
        {
            for (uint y = 0; y < Size; y++)
                _matrix.Set((uint)x, y, (int)(x * Size + y));
        });
    }

    [Benchmark(Description = "GetRow")]
    public int[] GetRow()
    {
        return _sourceMatrix.GetRow((uint)(Size / 2));
    }

    [Benchmark(Description = "GetColumn")]
    public int[] GetColumn()
    {
        return _sourceMatrix.GetColumn((uint)(Size / 2));
    }

    [Benchmark(Description = "To2DArray")]
    public int[,] To2DArray()
    {
        return _sourceMatrix.To2DArray();
    }

    [Benchmark(Description = "ForEach_Async")]
    public async Task ForEachAsync()
    {
        var _ = 0;
        await _sourceMatrix.ForEachAsync((x, y, value) =>
        {
            Interlocked.Add(ref _, value);
            return Task.CompletedTask;
        });
    }

    [Benchmark(Description = "ForEach_Sync")]
    public void ForEachSync()
    {
        var _ = 0;
        for (uint x = 0; x < Size; x++)
        for (uint y = 0; y < Size; y++)
            _ += _sourceMatrix.Get(x, y);
    }

    [Benchmark(Description = "Enumerate_ToList")]
    public IList<int> EnumerateToList()
    {
        using var enumerator = _sourceMatrix.GetEnumerator();
        return enumerator.ToList();
    }
}
