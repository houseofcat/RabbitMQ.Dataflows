using HouseofCat.Utilities.Errors;

namespace Utilities;

public class GuardTests
{
    public GuardTests()
    { }

    [Fact]
    public void AgainstNullTest()
    {
        var argumentName = "argumentName";
        var argumentValue = "argumentValue";

        Guard.AgainstNull(argumentValue, argumentName);
        Guard.AgainstNull(string.Empty, argumentName);

        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(null, argumentName));
    }

    [Fact]
    public void AgainstEmptyTest()
    {
        var argumentName = "argumentName";
        var argumentValue = new ArraySegment<string>(Array.Empty<string>());

        Assert.Throws<ArgumentException>(() => Guard.AgainstEmpty(argumentValue, argumentName));
    }

    [Fact]
    public void AgainstNullOrEmptyTest()
    {
        var argumentName = "argumentName";
        var argumentValue = new List<string> { "NotEmpty" };

        Guard.AgainstNullOrEmpty(argumentValue, argumentName);
        argumentValue.Clear();

        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(argumentValue, argumentName));
    }

    [Fact]
    public void AgainstNullOrEmptyStringTest()
    {
        var argumentName = "argumentName";
        var argumentValue = "argumentValue";

        Guard.AgainstNullOrEmpty(argumentValue, argumentName);

        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(string.Empty, argumentName));
    }

    [Fact]
    public void AgainstNullOrEmptyStreamTest()
    {
        var argumentName = "argumentName";
        var argumentValue = new MemoryStream();

        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(argumentValue, argumentName));
    }

    [Fact]
    public void AgainstBothNullOrEmptyTest()
    {
        var argumentName = "argumentName";
        var argumentValue = "argumentValue";
        var secondArgumentName = "secondArgumentName";
        var secondArgumentValue = "secondArgumentValue";

        Guard.AgainstBothNullOrEmpty(argumentValue, argumentName, secondArgumentValue, secondArgumentName);

        Assert.Throws<ArgumentException>(() => Guard.AgainstBothNullOrEmpty("", argumentName, "", secondArgumentName));
    }
}