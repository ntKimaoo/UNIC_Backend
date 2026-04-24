using BusinessLogic.DTOs;
using DataAccess.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

[Route("api/fund-types")]
[ApiController]
public class FundTypeController : ControllerBase
{
    private readonly IFundTypeRepository _fundTypeRepository;

    public FundTypeController(IFundTypeRepository fundTypeRepository)
    {
        _fundTypeRepository = fundTypeRepository;
    }

    [HttpGet]
    public async Task<IActionResult> ListActive(CancellationToken cancellationToken)
    {
        var items = await _fundTypeRepository.ListActiveAsync(cancellationToken);
        var dtos = items
            .Select(x => new FundTypeDto
            {
                FundTypeId = x.FundTypeId,
                Name = x.Name,
                IsActive = x.IsActive,
                SortOrder = x.SortOrder
            })
            .ToList();

        return Ok(new { success = true, data = dtos });
    }
}

