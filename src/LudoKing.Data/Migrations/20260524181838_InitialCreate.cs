using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LudoKing.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CountryCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsBanned = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FriendRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RequesterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FriendRequests_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FriendRequests_Users_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PresetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaxPlayers = table.Column<int>(type: "int", nullable: false),
                    TokensPerPlayer = table.Column<int>(type: "int", nullable: false),
                    DiceCount = table.Column<int>(type: "int", nullable: false),
                    RequireSixToStart = table.Column<bool>(type: "bit", nullable: false),
                    ExtraTurnOnSix = table.Column<bool>(type: "bit", nullable: false),
                    ExtraTurnOnCut = table.Column<bool>(type: "bit", nullable: false),
                    MaxConsecutiveSixes = table.Column<int>(type: "int", nullable: false),
                    HomeEntryRule = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StackProtectionEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SafeSquaresMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TurnTimerSeconds = table.Column<int>(type: "int", nullable: false),
                    AutoKickOnDisconnectSeconds = table.Column<int>(type: "int", nullable: false),
                    StopOnFirstWinner = table.Column<bool>(type: "bit", nullable: false),
                    AllowSpectators = table.Column<bool>(type: "bit", nullable: false),
                    AutomatedPlayerCount = table.Column<int>(type: "int", nullable: false),
                    BotRequireSixToStart = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleConfigurations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GamesPlayed = table.Column<int>(type: "int", nullable: false),
                    GamesWon = table.Column<int>(type: "int", nullable: false),
                    GamesLost = table.Column<int>(type: "int", nullable: false),
                    GamesAbandoned = table.Column<int>(type: "int", nullable: false),
                    TotalTokensCut = table.Column<int>(type: "int", nullable: false),
                    TotalTokensLost = table.Column<int>(type: "int", nullable: false),
                    TotalPlayTimeSeconds = table.Column<long>(type: "bigint", nullable: false),
                    EloRating = table.Column<int>(type: "int", nullable: false),
                    CurrentWinStreak = table.Column<int>(type: "int", nullable: false),
                    BestWinStreak = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameMoves",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MoveType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiceValue = table.Column<int>(type: "int", nullable: false),
                    TokenIndex = table.Column<int>(type: "int", nullable: true),
                    FromSquare = table.Column<int>(type: "int", nullable: true),
                    ToSquare = table.Column<int>(type: "int", nullable: true),
                    WasCut = table.Column<bool>(type: "bit", nullable: false),
                    CutTokenOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BoardStateSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameMoves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameMoves_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GamePlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ColorAssigned = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SeatPosition = table.Column<int>(type: "int", nullable: false),
                    FinishPosition = table.Column<int>(type: "int", nullable: true),
                    IsConnected = table.Column<bool>(type: "bit", nullable: false),
                    IsBot = table.Column<bool>(type: "bit", nullable: false),
                    TokensHome = table.Column<int>(type: "int", nullable: false),
                    TokensCut = table.Column<int>(type: "int", nullable: false),
                    TokensLost = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamePlayers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GameRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    HostUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JoinCode = table.Column<string>(type: "nchar(6)", fixedLength: true, maxLength: 6, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    MaxPlayers = table.Column<int>(type: "int", nullable: false),
                    CurrentPlayerCount = table.Column<int>(type: "int", nullable: false),
                    RuleConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActiveGameId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameRooms_RuleConfigurations_RuleConfigurationId",
                        column: x => x.RuleConfigurationId,
                        principalTable: "RuleConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GameRooms_Users_HostUserId",
                        column: x => x.HostUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WinnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    TotalMoves = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_GameRooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "GameRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Games_RuleConfigurations_RuleConfigurationId",
                        column: x => x.RuleConfigurationId,
                        principalTable: "RuleConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Games_Users_WinnerUserId",
                        column: x => x.WinnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "CountryCode", "CreatedAt", "DisplayName", "Email", "EmailConfirmed", "IsActive", "IsBanned", "LastLoginAt", "PasswordHash", "Role", "UpdatedAt" },
                values: new object[] { new Guid("a0000000-0000-0000-0000-000000000001"), null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "funnyAdmin", "funnyadmin@ludoking.local", true, true, false, null, "350000.6ZYBin8T8Y/1QDb/eI1I3Q==./x0zVi2LeqecCdOzQVyF6nRv/dkyrx77IWpPK01SqcE=", "Admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "UserStats",
                columns: new[] { "Id", "BestWinStreak", "CurrentWinStreak", "EloRating", "GamesAbandoned", "GamesLost", "GamesPlayed", "GamesWon", "TotalPlayTimeSeconds", "TotalTokensCut", "TotalTokensLost", "UpdatedAt", "UserId" },
                values: new object[] { new Guid("b0000000-0000-0000-0000-000000000001"), 0, 0, 1000, 0, 0, 0, 0, 0L, 0, 0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_RequesterId_TargetId",
                table: "FriendRequests",
                columns: new[] { "RequesterId", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FriendRequests_TargetId",
                table: "FriendRequests",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_GameMoves_GameId_SequenceNumber",
                table: "GameMoves",
                columns: new[] { "GameId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameMoves_UserId",
                table: "GameMoves",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_GameId",
                table: "GamePlayers",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_UserId",
                table: "GamePlayers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRooms_ActiveGameId",
                table: "GameRooms",
                column: "ActiveGameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRooms_HostUserId",
                table: "GameRooms",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRooms_JoinCode",
                table: "GameRooms",
                column: "JoinCode",
                unique: true,
                filter: "[JoinCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GameRooms_RuleConfigurationId",
                table: "GameRooms",
                column: "RuleConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_RoomId",
                table: "Games",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_RuleConfigurationId",
                table: "Games",
                column: "RuleConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_WinnerUserId",
                table: "Games",
                column: "WinnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RuleConfigurations_CreatedByUserId",
                table: "RuleConfigurations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DisplayName",
                table: "Users",
                column: "DisplayName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStats_UserId",
                table: "UserStats",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GameMoves_Games_GameId",
                table: "GameMoves",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GamePlayers_Games_GameId",
                table: "GamePlayers",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GameRooms_Games_ActiveGameId",
                table: "GameRooms",
                column: "ActiveGameId",
                principalTable: "Games",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameRooms_Users_HostUserId",
                table: "GameRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_Users_WinnerUserId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_RuleConfigurations_Users_CreatedByUserId",
                table: "RuleConfigurations");

            migrationBuilder.DropForeignKey(
                name: "FK_GameRooms_Games_ActiveGameId",
                table: "GameRooms");

            migrationBuilder.DropTable(
                name: "FriendRequests");

            migrationBuilder.DropTable(
                name: "GameMoves");

            migrationBuilder.DropTable(
                name: "GamePlayers");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserStats");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "GameRooms");

            migrationBuilder.DropTable(
                name: "RuleConfigurations");
        }
    }
}
