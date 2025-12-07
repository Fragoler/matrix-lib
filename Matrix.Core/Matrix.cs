namespace Matrix.Core;


/// <summary>
/// Template container matrix class NxM with multithreading support.
/// </summary>
/// <typeparam name="T">Matrix's element type</typeparam>
public partial class Matrix<T> : IDisposable
    where T : notnull
{
    private const uint LocksCount = 16;
    
    private readonly T[,] _data;
    private readonly uint _width;
    private readonly uint _height;
    private readonly ReaderWriterLockSlim _lock = new();
    private volatile bool _disposed;

    /// <summary>
    /// Init matrix width * height with default value
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if at least one argument is not positive</exception>
    public Matrix(uint width, uint height)
    {
        if (width == 0 || height == 0)
            throw new ArgumentException($"Matrix's params should be positive. But has: width={width}, height={height}");

        _width = width;
        _height = height;
        _data = new T[width, height];
    }

    /// <summary>
    /// Init matrix with already init data
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if data is null</exception>
    /// <exception cref="ArgumentException">Thrown if data has one 0 dimension</exception>
    public Matrix(T[,] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _width = Convert.ToUInt32(data.GetLength(0));
        _height = Convert.ToUInt32(data.GetLength(1));

        if (_width == 0 || _height == 0)
            throw new ArgumentException("Array should have any data");

        _data = (T[,])data.Clone();
    }
    
    
    public uint Width => _width;
    public uint Height => _height;
    

    /// <summary>
    /// Get element
    /// </summary>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public T Get(uint x, uint y)
    {
        ThrowIfDisposed();
        ValidateIndices(x, y);
        
        _lock.EnterReadLock();
        try
        {
            return _data[x, y];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    /// <summary>
    /// Set element 
    /// </summary>
    public void Set(uint x, uint y, T value)
    {
        ThrowIfDisposed();
        ValidateIndices(x, y);

        _lock.EnterWriteLock();
        try
        {
            _data[x, y] = value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public T this[uint x, uint y]
    {
        get => Get(x, y);
        set => Set(x, y, value);
    }
    
    
    /// <summary>
    /// Fill async
    /// </summary>
    public async Task FillAsync(Func<uint, uint, T> factory, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var tasks = new List<Task>();
        for (uint x = 0; x < _width; x++)
        {
            for (uint col = 0; col < _height; col++)
            {
                uint r = x, c = col;
                
                tasks.Add(Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Set(r, c, factory(r, c));
                }, cancellationToken));
            }
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    
    /// <summary>
    /// For each do action
    /// </summary>
    public async Task ForEachAsync(Func<uint, uint, T, Task> action, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var tasks = new List<Task>();
        for (uint x = 0; x < _width; x++)
        {
            for (uint col = 0; col < _height; col++)
            {
                uint r = x, c = col;
                
                tasks.Add(Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var value = Get(r, c);
                    await action(r, c, value).ConfigureAwait(false);
                }, cancellationToken));
            }
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    

    /// <summary>
    /// Get column
    /// </summary>
    public T[] GetColumn(uint xIndex)
    {
        ThrowIfDisposed();
        
        if (xIndex >= _width)
            throw new IndexOutOfRangeException($"{xIndex}");

        var x = new T[_height];
        for (uint col = 0; col < _height; col++)
            x[col] = Get(xIndex, col);
        return x;
    }

    /// <summary>
    /// Get row
    /// </summary>
    public T[] GetRow(uint yIndex)
    {
        ThrowIfDisposed();
        
        if (yIndex >= _height)
            throw new IndexOutOfRangeException($"{yIndex}");
        
        var y = new T[_width];
        for (uint x = 0; x < _width; x++)
            y[x] = Get(x, yIndex);
        return y;
    }
    
    public T[,] To2DArray()
    {
        ThrowIfDisposed();

        _lock.EnterWriteLock();
        try
        {
            return (T[,])_data.Clone();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public override string ToString()
    {
        if (_disposed)
            return "[Matrix disposed]";

        var sb = new StringBuilder();
        sb.AppendLine($"Matrix<{typeof(T).Name}> ({_width}x{_height})");
        
        for (uint x = 0; x < _width; x++)
        {
            sb.Append("[ ");
            for (uint col = 0; col < _height; col++)
            {
                sb.Append(Get(x, col)).Append(' ');
            }
            sb.AppendLine("]");
        }

        return sb.ToString();
    }
    

    /// <summary>
    /// For correct clean up locks
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _lock.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~Matrix()
    {
        Dispose(false);
    }
    
    
    private void ValidateIndices(uint x, uint y)
    {
        if (x >= _width)
            throw new IndexOutOfRangeException($"{x}");

        if (y >= _height)
            throw new IndexOutOfRangeException($"{y}");
    }
    
    /// <summary>
    /// Check if object has already disposed
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Matrix<T>), "Matrix was disposed");
    }


}
