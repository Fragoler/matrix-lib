namespace Matrix.Core;


/// <summary>
/// Template container matrix class NxM with multithreading support.
/// </summary>
/// <typeparam name="T">Matrix's element type</typeparam>
public partial class Matrix<T> : IDisposable
    where T : notnull
{
    private readonly T[,] _data;
    private readonly uint _width;
    private readonly uint _height;
    private readonly ReaderWriterLockSlim _lock = new();
    private volatile bool _disposed;

    
    /// <summary>
    /// Initializes a new matrix with the specified dimensions and default values.
    /// </summary>
    /// <param name="width">Number of columns.</param>
    /// <param name="height">Number of rows.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when width or height is zero.
    /// </exception>
    public Matrix(uint width, uint height)
    {
        if (width == 0 || height == 0)
            throw new ArgumentException($"Matrix's params should be positive. But has: width={width}, height={height}");

        _width = width;
        _height = height;
        _data = new T[width, height];
    }

    /// <summary>
    /// Initializes a new matrix from an existing 2D array copy.
    /// </summary>
    /// <param name="data">Source 2D array.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when data is null.
    /// </exception>
    public Matrix(T[,] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _width = Convert.ToUInt32(data.GetLength(0));
        _height = Convert.ToUInt32(data.GetLength(1));

        _data = (T[,])data.Clone();
    }
    
    /// <summary>
    /// Gets the matrix width (number of columns).
    /// </summary>
    public uint Width => _width;
    
    /// <summary>
    /// Gets the matrix height (number of rows).
    /// </summary>
    public uint Height => _height;
    

    /// <summary>
    /// Returns the element at the specified position.
    /// </summary>
    /// <param name="x">Column index.</param>
    /// <param name="y">Row index.</param>
    /// <returns>Element at (x, y).</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when indices are outside matrix bounds.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>
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
    /// Sets the element at the specified position.
    /// </summary>
    /// <param name="x">Column index.</param>
    /// <param name="y">Row index.</param>
    /// <param name="value">Value to assign.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when indices are outside matrix bounds.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>
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
    
    
    /// <summary>
    /// Gets or sets the element at the specified position.
    /// </summary>
    /// <param name="x">Column index.</param>
    /// <param name="y">Row index.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when indices are outside matrix bounds.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>
    public T this[uint x, uint y]
    {
        get => Get(x, y);
        set => Set(x, y, value);
    }
    
    
    /// <summary>
    /// Asynchronously fills the matrix using the provided factory function.
    /// </summary>
    /// <param name="factory">
    /// Function that produces a value for a given (x, y) position.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to observe for cancellation.
    /// </param>
    /// <returns>A task that represents the fill operation.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>
    public Task FillAsync(Func<uint, uint, T> factory, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return Task.Run(() =>
        {
            _lock.EnterWriteLock();

            try
            {
                Parallel.For(0, (int)_width, new ParallelOptions
                {
                    CancellationToken = cancellationToken
                }, x =>
                {
                    for (uint y = 0; y < _height; y++)
                    {
                        _data[x, y] = factory((uint)x, y);
                    }
                });
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Asynchronously executes an action for each element in the matrix.
    /// </summary>
    /// <param name="action">
    /// Asynchronous action that receives position and value.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to observe for cancellation.
    /// </param>
    /// <returns>A task that represents the iteration operation.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>
    public Task ForEachAsync(Func<uint, uint, T, Task> action, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return Task.Run(async () =>
        {
            _lock.EnterWriteLock();

            try
            {
                var tasks = new List<Task>((int)(_width * _height));
                for (uint x = 0; x < _width; x++)
                {
                    for (uint y = 0; y < _height; y++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        tasks.Add(action(x, y, _data[x, y]));
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Returns all elements of the specified column.
    /// </summary>
    /// <param name="xIndex">Column index.</param>
    /// <returns>Array containing the column values.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when xIndex is outside matrix bounds.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>
    public T[] GetColumn(uint xIndex)
    {
        ThrowIfDisposed();
        
        if (xIndex >= _width)
            throw new IndexOutOfRangeException($"{xIndex}");

        _lock.EnterWriteLock();
        try
        {
            var x = new T[_height];
            for (uint y = 0; y < _height; y++)
                x[y] = _data[xIndex, y];
            return x;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Returns all elements of the specified row.
    /// </summary>
    /// <param name="yIndex">Row index.</param>
    /// <returns>Array containing the row values.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when yIndex is outside matrix bounds.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>
    public T[] GetRow(uint yIndex)
    {
        ThrowIfDisposed();
        
        if (yIndex >= _height)
            throw new IndexOutOfRangeException($"{yIndex}");
        
        _lock.EnterWriteLock();
        try
        {
            var row = new T[_width];
            for (uint x = 0; x < _width; x++)
                row[x] = _data[x, yIndex];
            return row;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Returns a cloned 2D array with the matrix contents.
    /// </summary>
    /// <returns>New 2D array with matrix data.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>s
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
    
    /// <summary>
    /// Returns a string representation of the matrix contents.
    /// </summary>
    /// <returns>Human-readable matrix representation.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>
    public override string ToString()
    {
        ThrowIfDisposed();
        
        _lock.EnterWriteLock();
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Matrix<{typeof(T).Name}> ({_width}x{_height})");

            for (uint x = 0; x < _width; x++)
            {
                sb.Append("[ ");
                for (uint col = 0; col < _height; col++)
                {
                    sb.Append(_data[x, col]).Append(' ');
                }

                sb.AppendLine("]");
            }

            return sb.ToString();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    

    /// <summary>
    /// Releases all resources used by the matrix.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Releases unmanaged resources and optionally managed resources.
    /// </summary>
    /// <param name="disposing">
    /// True to release managed resources; false to release only unmanaged.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;


        if (disposing)
        {
            _lock.Dispose();
        }
    }

    /// <summary>
    /// Finalizer that ensures resources are released if Dispose was not called.
    /// </summary>
    ~Matrix()
    {
        Dispose(false);
    }
    
    
    /// <summary>
    /// Validates that the given indices are within matrix bounds.
    /// </summary>
    /// <param name="x">Column index.</param>
    /// <param name="y">Row index.</param>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when indices are outside matrix bounds.
    /// </exception>
    private void ValidateIndices(uint x, uint y)
    {
        if (x >= _width)
            throw new IndexOutOfRangeException($"{x}");

        if (y >= _height)
            throw new IndexOutOfRangeException($"{y}");
    }
    
    /// <summary>
    /// Throws if the matrix has already been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the matrix has been disposed.
    /// </exception>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Matrix<T>), "Matrix was disposed");
    }
}
