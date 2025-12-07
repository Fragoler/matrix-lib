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
    private uint _x = 0, _y = 0;

    public T Current => matrix.Get(_y, _x);
    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_x + 1 != matrix.Width)
        {
            ++_x;
            return true;
        }

        if (_y + 1 != matrix.Height)
        {
            _x = 0;
            ++_y;
            return true;
        }

        return false;
    }

    public void Reset() =>
        _y = _x = 0;

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
            _x = _x,
            _y = _y
        };
    }

    public void Dispose()
    {
        // do nothing
    }
}