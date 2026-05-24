using LudoKing.Data.Context;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LudoKing.Data.Repositories;

internal sealed class RuleConfigurationRepository : IRuleConfigurationRepository
{
    private readonly LudoKingDbContext _ctx;

    public RuleConfigurationRepository(LudoKingDbContext ctx) => _ctx = ctx;

    public Task<RuleConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.RuleConfigurations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(RuleConfiguration config, CancellationToken ct = default)
        => await _ctx.RuleConfigurations.AddAsync(config, ct);

    public void Update(RuleConfiguration config)
        => _ctx.RuleConfigurations.Update(config);
}
