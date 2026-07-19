using Bcmp.Application.Properties;
using Bcmp.Application.Tests.TestDoubles;
using Bcmp.Domain.Properties;
using FluentAssertions;
using NSubstitute;

namespace Bcmp.Application.Tests.Properties;

[TestFixture]
public class PropertyServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private IPropertyRepository _repository = null!;
    private PropertyService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IPropertyRepository>();
        _sut = new PropertyService(_repository, new FixedTimeProvider(Now));
    }

    [Test]
    public async Task AddPropertyAsync_WithValidData_CreatesAndPersistsProperty()
    {
        _repository.GetByNameAsync("Sunset Villas").Returns((Property?)null);

        var result = await _sut.AddPropertyAsync("Sunset Villas", "12 Ocean Drive", "North Shore", "NSW", "2000");

        result.Name.Should().Be("Sunset Villas");
        result.AddressLine1.Should().Be("12 Ocean Drive");
        result.Suburb.Should().Be("North Shore");
        result.State.Should().Be("NSW");
        result.Postcode.Should().Be("2000");
        await _repository.Received(1).AddAsync(Arg.Is<Property>(p => p.Name == "Sunset Villas"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddPropertyAsync_WithDuplicateName_Throws()
    {
        var existing = Property.Create(Guid.NewGuid(), "Sunset Villas", "12 Ocean Drive", "North Shore", "NSW", "2000", Now);
        _repository.GetByNameAsync("sunset villas").Returns(existing);

        var act = async () => await _sut.AddPropertyAsync("Sunset Villas", "12 Ocean Drive", "North Shore", "NSW", "2000");

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Property>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdatePropertyAsync_UnknownProperty_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((Property?)null);

        var act = async () => await _sut.UpdatePropertyAsync(Guid.NewGuid(), "New Name", "New Address", "New Suburb", "VIC", "3000");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public async Task GetAllAsync_MapsDomainPropertiesToDtos()
    {
        var property = Property.Create(Guid.NewGuid(), "Harbour View", "1 Bay Road", "Port Melbourne", "VIC", "3207", Now);
        _repository.GetAllAsync().Returns([property]);

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle(p => p.Id == property.Id && p.Name == property.Name);
    }
}
