using LudoKing.Interfaces.Entities;

namespace LudoKing.Interfaces.Repositories;

public interface IRuleConfigurationRepository
{
    Task<RuleConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(RuleConfiguration config, CancellationToken ct = default);
    void Update(RuleConfiguration config);
}
