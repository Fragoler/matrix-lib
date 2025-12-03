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

public class MatrixEnumerator<T>(Matrix<T> matrix) : IEnumerator<T>, ICloneable
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

    public IList<T> ToList()
    {
        var list = new List<T>();

        var en = (MatrixEnumerator<T>)Clone();
        do
        {
            list.Add(en.Current);
        } while(en.MoveNext()); 
        
        return list;
    }


    public object Clone()
    {
        return new MatrixEnumerator<T>(matrix)
        {
            _column = _column,
            _row = _row
        };
    }

    public void Dispose()
    {
        // do nothing
    }
}