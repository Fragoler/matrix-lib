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
    private readonly uint _rows;
    private readonly uint _columns;
    private readonly ReaderWriterLockSlim[] _locks = new ReaderWriterLockSlim[LocksCount];
    private volatile bool _disposed;

    /// <summary>
    /// Init matrix rows * columns with default value
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if at least one argument is not positive</exception>
    public Matrix(uint rows, uint columns)
    {
        if (rows == 0 || columns == 0)
            throw new ArgumentException($"Matrix's params should be positive. But has: rows={rows}, columns={columns}");

        _rows = rows;
        _columns = columns;
        _data = new T[rows, columns];

        for (var i = 0; i < LocksCount; ++i)   
            _locks[i] = new ReaderWriterLockSlim();
    }

    /// <summary>
    /// Init matrix with already init data
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if data is null</exception>
    /// <exception cref="ArgumentException">thrown if data has one 0 dimension</exception>
    public Matrix(T[,] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        _rows = Convert.ToUInt32(data.GetLength(0));
        _columns = Convert.ToUInt32(data.GetLength(1));

        if (_rows == 0 || _columns == 0)
            throw new ArgumentException("Array should have any data");

        _data = (T[,])data.Clone();
        
        for (var i = 0; i < LocksCount; ++i)   
            _locks[i] = new ReaderWriterLockSlim();
    }

    public uint Rows => _rows;
    public uint Columns => _columns;

    /// <summary>
    /// Get element
    /// </summary>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public T Get(uint row, uint column)
    {
        ThrowIfDisposed();
        ValidateIndices(row, column);
        var @lock = GetElementLock(row, column);
        
        @lock.EnterReadLock();
        try
        {
            return _data[row, column];
        }
        finally
        {
            @lock.ExitReadLock();
        }
    }
    
    /// <summary>
    /// Set element 
    /// </summary>
    public void Set(uint row, uint column, T value)
    {
        ThrowIfDisposed();
        ValidateIndices(row, column);
        var @lock = GetElementLock(row, column);
        
        @lock.EnterWriteLock();
        try
        {
            _data[row, column] = value;
        }
        finally
        {
            @lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Fill async
    /// </summary>
    public async Task FillAsync(Func<uint, uint, T> factory, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var tasks = new List<Task>();
        for (uint row = 0; row < _rows; row++)
        {
            for (uint col = 0; col < _columns; col++)
            {
                uint r = row, c = col;
                
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
        for (uint row = 0; row < _rows; row++)
        {
            for (uint col = 0; col < _columns; col++)
            {
                uint r = row, c = col;
                
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
    /// Get row
    /// </summary>
    public T[] GetRow(uint rowIndex)
    {
        ThrowIfDisposed();
        
        if (rowIndex >= _rows)
            throw new IndexOutOfRangeException($"{rowIndex}");

        var row = new T[_columns];
        for (uint col = 0; col < _columns; col++)
            row[col] = Get(rowIndex, col);
        return row;
    }

    /// <summary>
    /// Get column
    /// </summary>
    public T[] GetColumn(uint columnIndex)
    {
        ThrowIfDisposed();
        
        if (columnIndex >= _columns)
            throw new IndexOutOfRangeException($"{columnIndex}");
        
        var column = new T[_rows];
        for (uint row = 0; row < _rows; row++)
            column[row] = Get(row, columnIndex);
        return column;
    }
    
    
    public override string ToString()
    {
        if (_disposed)
            return "[Matrix disposed]";

        var sb = new StringBuilder();
        sb.AppendLine($"Matrix<{typeof(T).Name}> ({_rows}x{_columns})");
        
        for (uint row = 0; row < _rows; row++)
        {
            sb.Append("[ ");
            for (uint col = 0; col < _columns; col++)
            {
                sb.Append(Get(row, col)).Append(' ');
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
            foreach (var @lock in _locks)
                @lock.Dispose();
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
    
    
    private ReaderWriterLockSlim GetElementLock(uint row, uint column)
    {
        ThrowIfDisposed();
        ValidateIndices(row, column);

        return _locks[(row + column) % LocksCount];
    }

    
    private void ValidateIndices(uint row, uint column)
    {
        if (row >= _rows)
            throw new IndexOutOfRangeException($"{row}");

        if (column >= _columns)
            throw new IndexOutOfRangeException($"{column}");
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
