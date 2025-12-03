using System.ComponentModel;

namespace Matrix.Core;


public partial class Matrix<T>
{
    public MatrixEnumerator<T> GetEnumerator()
    {
        ThrowIfDisposed();
        
        return new MatrixEnumerator<T>(this);
    }
}

public class MatrixEnumerator<T>(Matrix<T> matrix) : IEnumerator<T>
    where T : notnull
{
    private uint _column = 0, _row = 0;

    public T Current => matrix.Get(_row, _column);
    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_column + 1 != matrix.Columns)
        {
            ++_column;
            return true;
        }

        if (_row + 1 != matrix.Rows)
        {
            _column = 0;
            ++_row;
            return true;
        }

        return false;
    }

    public void Reset() =>
        _row = _column = 0;


    public void Dispose()
    {
        // do nothing
    }
}