using LudoKing.Interfaces.Dtos;
using LudoKing.Interfaces.Entities;
using LudoKing.Interfaces.Infrastructure;
using LudoKing.Interfaces.Repositories;
using LudoKing.Interfaces.Services;
using LudoKing.Shared.Constants;
using LudoKing.Shared.Helpers;

namespace LudoKing.Domain.Services;

public sealed class LobbyService : ILobbyService
{
    private readonly IUnitOfWork _uow;
    private readonly IGameEngine _gameEngine;
    private readonly IGameStateStore _gameStateStore;

    public LobbyService(
        IUnitOfWork unitOfWork,
        IGameEngine gameEngine,
        IGameStateStore gameStateStore)
    {
        _uow = unitOfWork;
        _gameEngine = gameEngine;
        _gameStateStore = gameStateStore;
    }

    // -------------------------------------------------------------------------
    // CreateRoomAsync
    // -------------------------------------------------------------------------
    public async Task<GameRoom> CreateRoomAsync(
        Guid hostUserId,
        CreateRoomRequest req,
        CancellationToken ct = default)
    {
        ValidateRules(req.Rules);

        var ruleConfig = new RuleConfiguration
        {
            Id = Guid.NewGuid(),
            MaxPlayers = req.Rules.MaxPlayers,
            TokensPerPlayer = req.Rules.TokensPerPlayer,
            DiceCount = req.Rules.DiceCount,
            RequireSixToStart = req.Rules.RequireSixToStart,
            ExtraTurnOnSix = req.Rules.ExtraTurnOnSix,
            ExtraTurnOnCut = req.Rules.ExtraTurnOnCut,
            MaxConsecutiveSixes = req.Rules.MaxConsecutiveSixes,
            HomeEntryRule = req.Rules.HomeEntryRule,
            StackProtectionEnabled = req.Rules.StackProtectionEnabled,
            SafeSquaresMode = req.Rules.SafeSquaresMode,
            TurnTimerSeconds = req.Rules.TurnTimerSeconds,
            AutoKickOnDisconnectSeconds = req.Rules.AutoKickOnDisconnectSeconds,
            StopOnFirstWinner = req.Rules.StopOnFirstWinner,
            AutomatedPlayerCount = req.Rules.AutomatedPlayerCount,
            BotRequireSixToStart = req.Rules.BotRequireSixToStart,
            CreatedByUserId = hostUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.RuleConfigurations.AddAsync(ruleConfig, ct);

        var room = new GameRoom
        {
            Id = Guid.NewGuid(),
            HostUserId = hostUserId,
            Name = req.Name,
            JoinCode = JoinCodeGenerator.Generate(),
            Status = "Waiting",
            IsPrivate = req.IsPrivate,
            MaxPlayers = req.Rules.MaxPlayers,
            CurrentPlayerCount = 1,
            RuleConfigurationId = ruleConfig.Id,
            ActiveGameId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.Rooms.AddAsync(room, ct);
        await _uow.SaveChangesAsync(ct);
        return room;
    }

    // -------------------------------------------------------------------------
    // JoinRoomAsync
    // -------------------------------------------------------------------------
    public async Task<GameRoom> JoinRoomAsync(
        Guid userId,
        Guid roomId,
        string? joinCode,
        CancellationToken ct = default)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId, ct)
            ?? throw new InvalidOperationException("Room not found.");

        if (!string.Equals(room.Status, "Waiting", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Room is not accepting players.");

        if (room.CurrentPlayerCount >= room.MaxPlayers)
            throw new InvalidOperationException("Room is full.");

        if (room.IsPrivate && !string.Equals(room.JoinCode, joinCode, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Invalid join code.");

        // Check if user is already in the room
        var existingPlayers = await _uow.Rooms.GetRoomPlayerIdsAsync(roomId, ct);
        if (existingPlayers.Contains(userId))
            return room; // already joined — idempotent

        room.CurrentPlayerCount++;
        room.UpdatedAt = DateTime.UtcNow;
        _uow.Rooms.Update(room);
        await _uow.SaveChangesAsync(ct);
        return room;
    }

    // -------------------------------------------------------------------------
    // LeaveRoomAsync
    // -------------------------------------------------------------------------
    public async Task LeaveRoomAsync(Guid userId, Guid roomId, CancellationToken ct = default)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId, ct);
        if (room is null)
            return;

        if (!string.Equals(room.Status, "Waiting", StringComparison.OrdinalIgnoreCase))
            return; // can't leave a room that is in-game via this method

        room.CurrentPlayerCount = Math.Max(0, room.CurrentPlayerCount - 1);
        room.UpdatedAt = DateTime.UtcNow;

        if (room.HostUserId == userId)
        {
            if (room.CurrentPlayerCount == 0)
            {
                // Close the room
                room.Status = "Closed";
                room.ClosedAt = DateTime.UtcNow;
            }
            else
            {
                // Transfer host to another player
                var playerIds = await _uow.Rooms.GetRoomPlayerIdsAsync(roomId, ct);
                var nextHost = playerIds.FirstOrDefault(id => id != userId);
                if (nextHost != Guid.Empty)
                    room.HostUserId = nextHost;
            }
        }

        _uow.Rooms.Update(room);
        await _uow.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // GetRoomAsync
    // -------------------------------------------------------------------------
    public async Task<GameRoom> GetRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        return await _uow.Rooms.GetByIdAsync(roomId, ct)
            ?? throw new InvalidOperationException("Room not found.");
    }

    // -------------------------------------------------------------------------
    // GetPublicRoomsAsync
    // -------------------------------------------------------------------------
    public async Task<List<GameRoom>> GetPublicRoomsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        return await _uow.Rooms.GetOpenPublicRoomsAsync(page, pageSize, ct);
    }

    // -------------------------------------------------------------------------
    // KickPlayerAsync
    // -------------------------------------------------------------------------
    public async Task KickPlayerAsync(
        Guid hostUserId,
        Guid roomId,
        Guid targetUserId,
        CancellationToken ct = default)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId, ct)
            ?? throw new InvalidOperationException("Room not found.");

        if (room.HostUserId != hostUserId)
            throw new UnauthorizedAccessException("Only the host can kick players.");

        if (targetUserId == hostUserId)
            throw new InvalidOperationException("Host cannot kick themselves.");

        room.CurrentPlayerCount = Math.Max(0, room.CurrentPlayerCount - 1);
        room.UpdatedAt = DateTime.UtcNow;
        _uow.Rooms.Update(room);
        await _uow.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // UpdateRulesAsync
    // -------------------------------------------------------------------------
    public async Task UpdateRulesAsync(
        Guid hostUserId,
        Guid roomId,
        RuleSetDto rules,
        CancellationToken ct = default)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId, ct)
            ?? throw new InvalidOperationException("Room not found.");

        if (room.HostUserId != hostUserId)
            throw new UnauthorizedAccessException("Only the host can update rules.");

        ValidateRules(rules);

        var config = await _uow.RuleConfigurations.GetByIdAsync(room.RuleConfigurationId, ct)
            ?? throw new InvalidOperationException("Rule configuration not found.");

        config.MaxPlayers = rules.MaxPlayers;
        config.TokensPerPlayer = rules.TokensPerPlayer;
        config.DiceCount = rules.DiceCount;
        config.RequireSixToStart = rules.RequireSixToStart;
        config.ExtraTurnOnSix = rules.ExtraTurnOnSix;
        config.ExtraTurnOnCut = rules.ExtraTurnOnCut;
        config.MaxConsecutiveSixes = rules.MaxConsecutiveSixes;
        config.HomeEntryRule = rules.HomeEntryRule;
        config.StackProtectionEnabled = rules.StackProtectionEnabled;
        config.SafeSquaresMode = rules.SafeSquaresMode;
        config.TurnTimerSeconds = rules.TurnTimerSeconds;
        config.AutoKickOnDisconnectSeconds = rules.AutoKickOnDisconnectSeconds;
        config.StopOnFirstWinner = rules.StopOnFirstWinner;
        config.AutomatedPlayerCount = rules.AutomatedPlayerCount;
        config.BotRequireSixToStart = rules.BotRequireSixToStart;

        _uow.RuleConfigurations.Update(config);

        room.MaxPlayers = rules.MaxPlayers;
        room.UpdatedAt = DateTime.UtcNow;
        _uow.Rooms.Update(room);

        await _uow.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // TransferHostAsync
    // -------------------------------------------------------------------------
    public async Task TransferHostAsync(
        Guid currentHostId,
        Guid roomId,
        Guid newHostId,
        CancellationToken ct = default)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId, ct)
            ?? throw new InvalidOperationException("Room not found.");

        if (room.HostUserId != currentHostId)
            throw new UnauthorizedAccessException("Only the current host can transfer host status.");

        var playerIds = await _uow.Rooms.GetRoomPlayerIdsAsync(roomId, ct);
        if (!playerIds.Contains(newHostId))
            throw new InvalidOperationException("Target user is not in the room.");

        room.HostUserId = newHostId;
        room.UpdatedAt = DateTime.UtcNow;
        _uow.Rooms.Update(room);
        await _uow.SaveChangesAsync(ct);
    }

    // -------------------------------------------------------------------------
    // SetPlayerReadyAsync — stub; ready state would be tracked separately
    // -------------------------------------------------------------------------
    public Task SetPlayerReadyAsync(Guid userId, Guid roomId, bool isReady, CancellationToken ct = default)
    {
        // Ready state is managed via SignalR group state and/or a separate in-memory store.
        // This method is a no-op in the DB layer for Phase 1.
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // StartGameAsync
    // -------------------------------------------------------------------------
    public async Task<Game?> StartGameAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await _uow.Rooms.GetByIdAsync(roomId, ct)
            ?? throw new InvalidOperationException("Room not found.");

        if (!string.Equals(room.Status, "Waiting", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Room is not in Waiting status.");

        var ruleConfig = await _uow.RuleConfigurations.GetByIdAsync(room.RuleConfigurationId, ct)
            ?? throw new InvalidOperationException("Rule configuration not found.");

        // Load human players
        var humanPlayerIds = await _uow.Rooms.GetRoomPlayerIdsAsync(roomId, ct);

        int botCount = ruleConfig.AutomatedPlayerCount;
        int totalPlayers = humanPlayerIds.Count + botCount;

        if (totalPlayers < 2)
            throw new InvalidOperationException("At least 2 players are required to start a game.");

        if (totalPlayers > ruleConfig.MaxPlayers)
            throw new InvalidOperationException("Too many players.");

        var game = new Game
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            RuleConfigurationId = ruleConfig.Id,
            Status = "InProgress",
            WinnerUserId = null,
            StartedAt = DateTime.UtcNow,
            EndedAt = null,
            DurationSeconds = null,
            TotalMoves = 0
        };

        await _uow.Games.AddAsync(game, ct);

        // Assign colors from available pool
        var availableColors = new List<string>(BoardConstants.Colors);
        var gamePlayers = new List<GamePlayer>();
        var playerStateDtos = new List<PlayerStateDto>();

        int seat = 0;
        foreach (var userId in humanPlayerIds)
        {
            string color = availableColors[seat];
            var gp = new GamePlayer
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                UserId = userId,
                ColorAssigned = color,
                SeatPosition = seat,
                IsConnected = true,
                IsBot = false,
                TokensHome = 0,
                TokensCut = 0,
                TokensLost = 0,
                JoinedAt = DateTime.UtcNow
            };
            gamePlayers.Add(gp);
            playerStateDtos.Add(new PlayerStateDto
            {
                UserId = userId.ToString(),
                Color = color,
                SeatPosition = seat,
                IsConnected = true,
                IsBot = false
            });
            seat++;
        }

        // Add bot players
        for (int b = 0; b < botCount; b++)
        {
            Guid botId = Guid.NewGuid();
            string color = availableColors[seat];
            var gp = new GamePlayer
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                UserId = botId,
                ColorAssigned = color,
                SeatPosition = seat,
                IsConnected = true,
                IsBot = true,
                JoinedAt = DateTime.UtcNow
            };
            gamePlayers.Add(gp);
            playerStateDtos.Add(new PlayerStateDto
            {
                UserId = botId.ToString(),
                Color = color,
                SeatPosition = seat,
                IsConnected = true,
                IsBot = true
            });
            seat++;
        }

        await _uow.GamePlayers.AddRangeAsync(gamePlayers, ct);

        // Build RuleSetDto from config
        var rules = MapToRuleSetDto(ruleConfig);

        // Initialize game state
        var gameState = _gameEngine.InitializeGame(game.Id, playerStateDtos, rules);
        gameState.Status = "InProgress";

        // Save to Redis/state store
        await _gameStateStore.SaveAsync(game.Id.ToString(), gameState);

        // Update room
        room.Status = "InProgress";
        room.ActiveGameId = game.Id;
        room.UpdatedAt = DateTime.UtcNow;
        _uow.Rooms.Update(room);

        await _uow.SaveChangesAsync(ct);
        return game;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------
    private static void ValidateRules(RuleSetDto rules)
    {
        if (rules.MaxPlayers < 2 || rules.MaxPlayers > 4)
            throw new ArgumentException("MaxPlayers must be between 2 and 4.");

        if (rules.TokensPerPlayer < 1 || rules.TokensPerPlayer > 4)
            throw new ArgumentException("TokensPerPlayer must be between 1 and 4.");

        if (rules.DiceCount < 1 || rules.DiceCount > 2)
            throw new ArgumentException("DiceCount must be 1 or 2.");

        if (string.IsNullOrEmpty(rules.HomeEntryRule))
            throw new ArgumentException("HomeEntryRule must be specified.");

        if (string.IsNullOrEmpty(rules.SafeSquaresMode))
            throw new ArgumentException("SafeSquaresMode must be specified.");
    }

    private static RuleSetDto MapToRuleSetDto(RuleConfiguration config) => new()
    {
        MaxPlayers = config.MaxPlayers,
        TokensPerPlayer = config.TokensPerPlayer,
        DiceCount = config.DiceCount,
        RequireSixToStart = config.RequireSixToStart,
        ExtraTurnOnSix = config.ExtraTurnOnSix,
        ExtraTurnOnCut = config.ExtraTurnOnCut,
        MaxConsecutiveSixes = config.MaxConsecutiveSixes,
        HomeEntryRule = config.HomeEntryRule,
        StackProtectionEnabled = config.StackProtectionEnabled,
        SafeSquaresMode = config.SafeSquaresMode,
        TurnTimerSeconds = config.TurnTimerSeconds,
        AutoKickOnDisconnectSeconds = config.AutoKickOnDisconnectSeconds,
        StopOnFirstWinner = config.StopOnFirstWinner,
        AutomatedPlayerCount = config.AutomatedPlayerCount,
        BotRequireSixToStart = config.BotRequireSixToStart
    };
}
