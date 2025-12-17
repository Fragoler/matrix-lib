using FluentAssertions;
using Matrix.Core;

namespace Matrix.Tests;

/// <summary>
///     Unit tests for Matrix{T}
///     Use xUnit and FluentAssertions
/// </summary>
public class MatrixTests
{
    [Fact]
    public void Constructor_ValidDimensions_ShouldCreateMatrix()
    {
        // Arrange
        const int width = 5;
        const int height = 10;

        // Act
        var matrix = new Matrix<int>(width, height);

        // Assert
        matrix.Width.Should().Be(width);
        matrix.Height.Should().Be(height);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(5, 0)]
    public void Constructor_InvalidDimensions_ShouldThrowException(uint width, uint height)
    {
        Action act = () => _ = new Matrix<int>(width, height);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithArray_ShouldInitializeMatrix()
    {
        // Arrange
        var data = new[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } };

        // Act
        var matrix = new Matrix<int>(data);

        // Assert
        matrix.Width.Should().Be(3);
        matrix.Height.Should().Be(2);
        matrix.Get(0, 0).Should().Be(1);
        matrix.Get(1, 1).Should().Be(4);
    }

    [Fact]
    public void Get_ValidIndices_ShouldReturnValue()
    {
        // Arrange
        var matrix = new Matrix<string>(2, 2);
        matrix.Set(0, 1, "hello");

        // Act
        var value = matrix.Get(0, 1);

        // Assert
        value.Should().Be("hello");
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(0, 5)]
    public void Get_InvalidIndices_ShouldThrowMatrixException(uint row, uint column)
    {
        // Arrange
        var matrix = new Matrix<int>(3, 3);

        // Act
        Action act = () => matrix.Get(row, column);

        // Assert
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void Set_ValidIndices_ShouldUpdateValue()
    {
        // Arrange
        var matrix = new Matrix<double>(2, 2);

        // Act
        matrix.Set(0, 0, 3.14);
        var value = matrix.Get(0, 0);

        // Assert
        value.Should().Be(3.14);
    }

    [Fact]
    public void Set_MultipleValues_ShouldPreserveAllValues()
    {
        // Arrange
        var matrix = new Matrix<int>(3, 3);
        var values = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        // Act
        uint index = 0;
        for (uint row = 0; row < 3; row++)
        for (uint col = 0; col < 3; col++)
            matrix.Set(row, col, values[index++]);

        // Assert
        index = 0;
        for (uint row = 0; row < 3; row++)
        for (uint col = 0; col < 3; col++)
            matrix.Get(row, col).Should().Be(values[index++]);
    }


    [Fact]
    public async Task FillAsync_ShouldPopulateAllElements()
    {
        // Arrange
        var matrix = new Matrix<uint>(3, 3);

        // Act
        await matrix.FillAsync((row, col) => row * 3 + col);

        // Assert
        for (uint row = 0; row < 3; row++)
        for (uint col = 0; col < 3; col++)
            matrix.Get(row, col).Should().Be(row * 3 + col);
    }

    [Fact]
    public void GetRow_ShouldReturnRowData()
    {
        // Arrange
        var matrix = new Matrix<uint>(4, 3);
        for (uint col = 0; col < 4; col++)
            matrix.Set(col, 1, col + 10);

        // Act
        var row = matrix.GetRow(1);

        // Assert
        row.Should().HaveCount(4);
        row.Should().Equal(10, 11, 12, 13);
    }

    [Fact]
    public void GetColumn_ShouldReturnColumnData()
    {
        // Arrange
        var matrix = new Matrix<uint>(3, 3);
        for (uint row = 0; row < 3; row++)
            matrix.Set(2, row, row * 5);

        // Act
        var column = matrix.GetColumn(2);

        // Assert
        column.Should().HaveCount(3);
        column.Should().Equal(0, 5, 10);
    }

    [Fact]
    public void GetRow_InvalidIndex_ShouldThrowMatrixException()
    {
        // Arrange
        var matrix = new Matrix<int>(2, 2);

        // Act & Assert
        Action act = () => matrix.GetRow(5);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void GetColumn_InvalidIndex_ShouldThrowMatrixException()
    {
        // Arrange
        var matrix = new Matrix<int>(2, 2);

        // Act & Assert
        Action act = () => matrix.GetColumn(5);
        act.Should().Throw<IndexOutOfRangeException>();
    }

    [Fact]
    public void GetEnumerator_ShouldIterateAllElements()
    {
        // Arrange
        var matrix = new Matrix<uint>(2, 3);
        for (uint i = 0; i < 6; i++)
            matrix.Set(i % 2, i / 2, i + 1);

        // Act
        using var matrixEnum = matrix.GetEnumerator();
        var list = matrixEnum.ToList();
        matrixEnum.MoveNext();

        // Assert
        list.Should().HaveCount(6);
        list.Should().Equal(1, 2, 3, 4, 5, 6);
        
        
        matrixEnum.Current.Should().Be(2);
        matrixEnum.Reset();
        matrixEnum.Current.Should().Be(1);
    }

    [Fact]
    public void Dispose_ShouldPreventFurtherOperations()
    {
        // Arrange
        var matrix = new Matrix<int>(2, 2);

        // Act
        matrix.Dispose();

        // Assert
        Action act = () => matrix.Get(0, 0);
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ToString_ShouldReturnMatrixInfo()
    {
        // Arrange
        var matrix = new Matrix<int>(3, 3);

        // Act
        var str = matrix.ToString();

        // Assert
        str.Should().Contain("Matrix<Int32>");
        str.Should().Contain("3x3");
    }

    [Fact]
    public void IDisposable_UsingStatement_ShouldDisposeCorrectly()
    {
        // Act & Assert
        var act = () =>
        {
            using var matrix = new Matrix<int>(2, 2);
            matrix.Set(0, 0, 5);
        };

        act.Should().NotThrow();
    }
    
    [Fact]
    public void Brackets_ShouldGetCell()
    {
        // Arrange
        var matrix = new Matrix<uint>(4, 3);
        for (uint col = 0; col < 4; col++)
            matrix.Set(col, 1, col + 10);

        // Act
        var val = matrix[2, 1];

        // Assert
        val.Should().Be(12);
    }
    
    [Fact]
    public void Brackets_ShouldSetCell()
    {
        // Arrange
        var matrix = new Matrix<uint>(4, 3);
        for (uint col = 0; col < 4; col++)
            matrix.Set(col, 1, col + 10);

        // Act
        matrix[2, 1] = 1000;

        // Assert
        matrix.Get(2, 1).Should().Be(1000);
    }
    
    
    [Fact]
    public async Task ForEachAsync_ShouldDoSomething()
    {
        // Arrange
        var matrix = new Matrix<uint>(4, 3);
        for (uint col = 0; col < 4; col++)
            matrix.Set(col, 1, 1);

        // Act
        var cnt = 0;
        await matrix.ForEachAsync((_, _, _) =>
        {
            Interlocked.Increment(ref cnt); 
            return Task.CompletedTask;
        }, CancellationToken.None);
        
        // Assert
        cnt.Should().Be(12);
    }
    
    
    [Fact]
    public void To2DArrayAsync_ShouldReturn2DArray()
    {
        // Arrange
        var matrix = new Matrix<uint>(4, 3);
        for (uint x = 0; x < 4; x++)
        for (uint y = 0; y < 3; y++)
            matrix.Set(x, y, x + y);

        // Act
        var array = matrix.To2DArray();
        
        // Assert
        for (uint x = 0; x < 4; x++)
        for (uint y = 0; y < 3; y++)
            array[x, y].Should().Be(x + y);
    }
    
    
        
    [Fact]
    public void DoubleDispose_ShouldWorkCorrectly()
    {
        // Act
        var act = () =>
        {
            var matrix = new Matrix<uint>(4, 3);

            matrix.Dispose();
            matrix.Dispose();

            GC.Collect();
        };

        // Assert
        act.Should().NotThrow();
    }
}