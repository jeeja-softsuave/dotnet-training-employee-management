using EmployeeManagement.Core.Exceptions;
using EmployeeManagement.Core.Services;

namespace EmployeeManagement.Tests.Services;

public class InMemoryEmployeeRepositoryTests
{
    private readonly InMemoryEmployeeRepository _repository = new();

    [Fact]
    public void Add_NewEmployee_CanBeRetrievedById()
    {
        // Arrange
        var employee = TestData.FullTime(id: 42);

        // Act
        _repository.Add(employee);

        // Assert
        Assert.Same(employee, _repository.GetById(42));
        Assert.Equal(1, _repository.Count);
    }

    [Fact]
    public void Add_DuplicateId_ThrowsDuplicateEmployeeException()
    {
        _repository.Add(TestData.FullTime(id: 1));

        var ex = Assert.Throws<DuplicateEmployeeException>(() => _repository.Add(TestData.Contract(id: 1)));
        Assert.Equal(1, ex.EmployeeId);
    }

    [Fact]
    public void Add_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _repository.Add(null!));
    }

    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        Assert.Null(_repository.GetById(999));
    }

    [Fact]
    public void GetAll_AddedOutOfOrder_ReturnsOrderedById()
    {
        _repository.Add(TestData.FullTime(id: 3));
        _repository.Add(TestData.FullTime(id: 1));
        _repository.Add(TestData.FullTime(id: 2));

        var ids = _repository.GetAll().Select(e => e.Id);

        Assert.Equal([1, 2, 3], ids);
    }

    [Fact]
    public void GetAll_Empty_ReturnsEmptyList()
    {
        Assert.Empty(_repository.GetAll());
    }

    [Fact]
    public void Remove_ExistingId_ReturnsTrueAndRemoves()
    {
        _repository.Add(TestData.FullTime(id: 5));

        var removed = _repository.Remove(5);

        Assert.True(removed);
        Assert.Null(_repository.GetById(5));
    }

    [Fact]
    public void Remove_UnknownId_ReturnsFalse()
    {
        Assert.False(_repository.Remove(5));
    }
}
