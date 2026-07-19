using FluentAssertions;

namespace Bcmp.Domain.Tests;

[TestFixture]
public class SolutionScaffoldTests
{
    [Test]
    public void DomainTestProject_IsWiredUp()
    {
        true.Should().BeTrue();
    }
}
