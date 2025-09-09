using Microsoft.AspNetCore.Mvc;
using TransactionProcessor.DTO;
using TransactionProcessor.Services;
using TransactionProcessor.Api;
using TransactionProcessor.Models;

namespace TransactionProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _svc;

    public AccountsController(IAccountService svc)
    {
        _svc = svc;
    }

    // POST: api/accounts
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AccountCommandDto dto)
    {
        var result = await _svc.CreateAsync(dto);
        return AccountHttpMapper.ToHttpResponse(result);
    }

    // GET: api/accounts/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var result = await _svc.GetByIdAsync(id);
        return AccountHttpMapper.ToHttpResponse(result);
    }

    // GET: api/accounts
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var accounts = await _svc.GetAllAsync();

        // Mapear enum Status para string para exibição
        var result = accounts.Select(a => new
        {
            a.AccountIdentifier,
            a.HolderName,
            a.Balance,
            a.ReservedBalance,
            a.CreditLimit,
            Status = a.Status.ToString(),
            a.ErrorCode,
            a.ErrorMessage
        }).ToList();

        return Ok(result);
    }

    // DELETE: api/accounts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _svc.DeleteAsync(id);
        return AccountHttpMapper.ToHttpResponse(result);
    }
}
