using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Tests.Models;

public class AppUserTests
{
    [Fact]
    public void Constructor_ValidValues_NormalizesUserName()
    {
        var user = new AppUser(0, "  Admin ", Roles.Admin);

        Assert.Equal("admin", user.UserName);
        Assert.Equal(Roles.Admin, user.Role);
        Assert.Equal(string.Empty, user.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_BlankUserName_Throws(string userName)
    {
        Assert.Throws<ArgumentException>(() => new AppUser(0, userName, Roles.HR));
    }

    [Theory]
    [InlineData("SuperUser")]
    [InlineData("admin")]   // role names are case-sensitive constants
    public void Constructor_UnknownRole_Throws(string role)
    {
        Assert.Throws<ArgumentException>(() => new AppUser(0, "someone", role));
    }

    [Fact]
    public void SetPasswordHash_Blank_Throws()
    {
        var user = new AppUser(0, "someone", Roles.Viewer);

        Assert.Throws<ArgumentException>(() => user.SetPasswordHash(" "));
    }
}
