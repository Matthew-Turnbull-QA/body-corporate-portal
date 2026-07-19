using Bcmp.Application.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bcmp.Api.Controllers;

[ApiController]
[Route("api/properties")]
[Authorize]
public sealed class PropertiesController(IPropertyService propertyService) : ControllerBase
{
    public sealed record CreatePropertyRequest(string Name, string AddressLine1, string Suburb, string State, string Postcode);

    public sealed record UpdatePropertyRequest(string Name, string AddressLine1, string Suburb, string State, string Postcode);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var properties = await propertyService.GetAllAsync(cancellationToken);
        return Ok(properties);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var property = await propertyService.GetByIdAsync(id, cancellationToken);
        return property is null ? NotFound() : Ok(property);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        var created = await propertyService.AddPropertyAsync(request.Name, request.AddressLine1, request.Suburb, request.State, request.Postcode, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        var updated = await propertyService.UpdatePropertyAsync(id, request.Name, request.AddressLine1, request.Suburb, request.State, request.Postcode, cancellationToken);
        return Ok(updated);
    }
}
