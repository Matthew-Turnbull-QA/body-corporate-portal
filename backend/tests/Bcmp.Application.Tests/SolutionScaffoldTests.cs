using FluentAssertions;

namespace Bcmp.Application.Tests;

[TestFixture]
public class SolutionScaffoldTests
{
    [Test]
    public void ApplicationTestProject_IsWiredUp()
    {
        true.Should().BeTrue();
    }
}
