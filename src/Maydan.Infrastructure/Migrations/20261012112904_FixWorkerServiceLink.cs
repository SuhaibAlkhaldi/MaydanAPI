using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maydan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixWorkerServiceLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GroupNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.GroupId);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PermissionNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoleNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CalculationType = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmtpHost = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SmtpUsername = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SmtpPasswordProtected = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SenderDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GroupPermissions",
                columns: table => new
                {
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPermissions", x => new { x.GroupId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_GroupPermissions_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MustResetPassword = table.Column<bool>(type: "bit", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Associations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    LocationOnGoogleMaps = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Associations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Associations_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssociationsV2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    LocationOnGoogleMaps = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationsV2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationsV2_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CityLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityLocations_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsSelfRegistered = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionCompanies_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByTokenId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_UserGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CivilId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CivilIdHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    MaritalStatus = table.Column<int>(type: "int", nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: true),
                    AssociationId = table.Column<int>(type: "int", nullable: false),
                    QrCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssociationV2Id = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workers_AssociationsV2_AssociationV2Id",
                        column: x => x.AssociationV2Id,
                        principalTable: "AssociationsV2",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Workers_Associations_AssociationId",
                        column: x => x.AssociationId,
                        principalTable: "Associations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Workers_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Workers_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProjectNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProjectTypeId = table.Column<int>(type: "int", nullable: false),
                    ProducerUserId = table.Column<int>(type: "int", nullable: false),
                    LocationManagerUserId = table.Column<int>(type: "int", nullable: false),
                    WorkPermitImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProductionCompanyId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.CheckConstraint("CK_Projects_EndDate_After_StartDate", "[EndDate] > [StartDate]");
                    table.CheckConstraint("CK_Projects_Producer_LocationManager", "[ProducerUserId] <> [LocationManagerUserId]");
                    table.ForeignKey(
                        name: "FK_Projects_ProductionCompanies_ProductionCompanyId",
                        column: x => x.ProductionCompanyId,
                        principalTable: "ProductionCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_ProjectTypes_ProjectTypeId",
                        column: x => x.ProjectTypeId,
                        principalTable: "ProjectTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Users_LocationManagerUserId",
                        column: x => x.LocationManagerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Users_ProducerUserId",
                        column: x => x.ProducerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkerServiceLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkerServiceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkerServiceLinks_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkerServiceLinks_Workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssociationProjectSupervisors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssociationProjectSupervisors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssociationProjectSupervisors_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssociationProjectSupervisors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Countries",
                columns: new[] { "Id", "ArabicName", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "UpdatedAt" },
                values: new object[] { 1, "الأردن", null, null, null, "Jordan", true, false, null });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "CreatedBy", "DeletedAt", "Id", "IsActive", "IsDeleted", "Module", "PermissionNameAr", "PermissionNameEn", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Users", "عرض المستخدمين", "View Users", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Users", "إنشاء المستخدمين", "Create Users", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Users", "إدارة المستخدمين", "Manage Users", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Groups", "عرض المجموعات", "View Groups", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Groups", "إنشاء المجموعات", "Create Groups", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Groups", "تعديل المجموعات", "Edit Groups", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Groups", "حذف المجموعات", "Delete Groups", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Groups", "إدارة المجموعات", "Manage Groups", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Associations", "عرض الجمعيات", "View Associations", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Associations", "إنشاء الجمعيات", "Create Associations", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Associations", "تعديل الجمعيات", "Edit Associations", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Associations", "حذف الجمعيات", "Delete Associations", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Associations", "إدارة الجمعيات", "Manage Associations", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Associations", "عرض مستخدمي الجمعيات", "View Association Users", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "ServiceRequests", "طلب خدمة", "Request Service", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "ServiceRequests", "عرض طلبات الخدمة", "View Service Requests", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "ServiceRequests", "إدارة طلبات الخدمة", "Manage Service Requests", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "ServiceRequests", "إدارة الخدمات", "Manage Services", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "ProductionCompanies", "عرض شركات الإنتاج", "View Production Companies", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "ProductionCompanies", "إدارة شركات الإنتاج", "Manage Production Companies", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "ProductionHouses", "عرض بيوت الإنتاج", "View Production Houses", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "ProductionHouses", "إدارة بيوت الإنتاج", "Manage Production Houses", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Workers", "عرض العمال", "View Workers", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Workers", "إدارة العمال", "Manage Workers", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 25, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Projects", "عرض المشاريع", "View Projects", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 26, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Projects", "إنشاء المشاريع", "Create Projects", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 27, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Projects", "تعديل المشاريع", "Edit Projects", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 28, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Projects", "حذف المشاريع", "Delete Projects", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 29, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Projects", "مراجعة المشاريع", "Review Projects", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 30, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Projects", "إدارة المشاريع", "Manage Projects", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 31, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Locations", "عرض المواقع", "View Locations", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 32, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Locations", "إدارة المواقع", "Manage Locations", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 33, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Attendance", "عرض الحضور", "View Attendance", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 34, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Attendance", "إدارة الحضور", "Manage Attendance", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 35, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Payments", "عرض الرواتب", "View Payments", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 36, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Payments", "إدارة الرواتب", "Manage Payments", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 37, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Onboarding", "استيعاب الجهات", "Onboard Entities", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 38, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "SystemConfiguration", "إدارة إعدادات النظام", "Manage System Configuration", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 39, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "Associations", "إدارة مستخدمي الجمعيات", "Manage Association Users", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 40, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, true, false, "ProductionCompanies", "إدارة مستخدمي شركة الإنتاج", "Manage Production Company Users", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ProjectTypes",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "IsActive", "IsDeleted", "NameAr", "NameEn", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, null, null, true, false, "عرض واقعي", "Reality Show", null },
                    { 2, null, null, null, true, false, "فيديو موسيقي", "Music Video", null },
                    { 3, null, null, null, true, false, "إعلانات متلفزة", "Commercials", null },
                    { 4, null, null, null, true, false, "فيلم قصير", "Short Film", null },
                    { 5, null, null, null, true, false, "فيلم طويل", "Feature Film", null },
                    { 6, null, null, null, true, false, "صور متحركة", "Animation", null },
                    { 7, null, null, null, true, false, "تصوير فوتوغرافي", "Photography", null },
                    { 8, null, null, null, true, false, "برامج", "TV Program", null },
                    { 9, null, null, null, true, false, "مسلسل", "Series", null },
                    { 10, null, null, null, true, false, "ألعاب تفاعلية", "Interactive/Game", null },
                    { 11, null, null, null, true, false, "وثائقي طويل", "Feature Documentary", null },
                    { 12, null, null, null, true, false, "وثائقي قصير", "Short Documentary", null },
                    { 13, null, null, null, true, false, "مسلسل وثائقي", "Documentary Series", null },
                    { 14, null, null, null, true, false, "وثائقي صناعي/شركات", "Corporate/Industrial Documentary", null },
                    { 15, null, null, null, true, false, "إنتاج طلابي", "Student Film", null }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "CreatedAt", "CreatedBy", "DeletedAt", "Id", "IsActive", "IsDeleted", "RoleNameAr", "RoleNameEn", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, false, false, "بيت الأردن", "Bayt-AlUrdon", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, false, false, "سلطة منطقة العقبة الاقتصادية الخاصة", "ASEZA", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, false, false, "شركة الإنتاج", "ProductionHouse", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, false, false, "الجمعية", "Association", new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Cities",
                columns: new[] { "Id", "ArabicName", "CountryId", "CreatedAt", "CreatedBy", "DeletedAt", "EnglishName", "IsActive", "IsDeleted", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "عمان", 1, null, null, null, "Amman", true, false, null },
                    { 2, "إربد", 1, null, null, null, "Irbid", true, false, null },
                    { 3, "الزرقاء", 1, null, null, null, "Zarqa", true, false, null },
                    { 4, "البلقاء", 1, null, null, null, "Balqa", true, false, null },
                    { 5, "مأدبا", 1, null, null, null, "Madaba", true, false, null },
                    { 6, "الكرك", 1, null, null, null, "Karak", true, false, null },
                    { 7, "الطفيلة", 1, null, null, null, "Tafilah", true, false, null },
                    { 8, "معان", 1, null, null, null, "Ma'an", true, false, null },
                    { 9, "العقبة", 1, null, null, null, "Aqaba", true, false, null },
                    { 10, "جرش", 1, null, null, null, "Jerash", true, false, null },
                    { 11, "عجلون", 1, null, null, null, "Ajloun", true, false, null },
                    { 12, "المفرق", 1, null, null, null, "Mafraq", true, false, null }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId", "IsActive" },
                values: new object[,]
                {
                    { 1, 1, true },
                    { 2, 1, true },
                    { 3, 1, true },
                    { 4, 1, true },
                    { 5, 1, true },
                    { 6, 1, true },
                    { 7, 1, true },
                    { 8, 1, true },
                    { 9, 1, true },
                    { 10, 1, true },
                    { 11, 1, true },
                    { 12, 1, true },
                    { 13, 1, true },
                    { 14, 1, true },
                    { 15, 1, true },
                    { 16, 1, true },
                    { 17, 1, true },
                    { 18, 1, true },
                    { 19, 1, true },
                    { 20, 1, true },
                    { 21, 1, true },
                    { 22, 1, true },
                    { 23, 1, true },
                    { 24, 1, true },
                    { 25, 1, true },
                    { 26, 1, true },
                    { 27, 1, true },
                    { 28, 1, true },
                    { 29, 1, true },
                    { 30, 1, true },
                    { 31, 1, true },
                    { 32, 1, true },
                    { 33, 1, true },
                    { 34, 1, true },
                    { 35, 1, true },
                    { 36, 1, true },
                    { 37, 1, true },
                    { 38, 1, true },
                    { 39, 1, true },
                    { 40, 1, true },
                    { 1, 2, true },
                    { 2, 2, true },
                    { 4, 2, true },
                    { 8, 2, true },
                    { 14, 2, true },
                    { 18, 2, true },
                    { 22, 2, true },
                    { 1, 3, true },
                    { 4, 3, true },
                    { 8, 3, true },
                    { 15, 3, true },
                    { 16, 3, true },
                    { 23, 3, true },
                    { 25, 3, true },
                    { 26, 3, true },
                    { 27, 3, true },
                    { 33, 3, true },
                    { 34, 3, true },
                    { 35, 3, true },
                    { 36, 3, true },
                    { 40, 3, true },
                    { 1, 4, true },
                    { 4, 4, true },
                    { 8, 4, true },
                    { 9, 4, true },
                    { 11, 4, true },
                    { 14, 4, true },
                    { 18, 4, true },
                    { 24, 4, true },
                    { 33, 4, true },
                    { 34, 4, true },
                    { 35, 4, true },
                    { 39, 4, true }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "CreatedBy", "DeletedAt", "Email", "EntityId", "EntityType", "FirstNameAr", "FirstNameEn", "Id", "IsActive", "IsDeleted", "LastNameAr", "LastNameEn", "MustResetPassword", "PasswordHash", "PhoneNumber", "RoleId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "ghaith@baytalurdon.org", 1, "BaytAlUrdon", "غيث", "Ghaith", 0, true, false, "الكردي", "Al-Kurdi", false, "PBKDF2-SHA256.100000.kr2Do1moATCbhMGahUAANg==.2I1SQILgcLQCpWA+3XSAEXhoilqTklMJh0t7BV2LKxo=", "+962770000023", 1, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 1000, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "aseza-admin@aseza.jo", 1, "Aseza", "أسيزا", "Aseza", 0, true, false, "أدمن", "Admin", false, "PBKDF2-SHA256.100000.NMQd4g85bDIwAs//rBeXvw==.haZXxr9bl/ZmbtsVxJH5pBI2Tz5x42PyO004Wsuv3bo=", "+962770000024", 2, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 1001, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "association-tester@example.org", 1, "Association", "جمعية", "Association", 0, true, false, "اختبار", "Tester", false, "PBKDF2-SHA256.100000.Neiqo3DL9KA6VnPDZZgaWQ==.euR5zQ/Skjf64sJn7SnvR/IDgkd5c/CgJLLfx9HjaXU=", "+962770000025", 4, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 1003, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "production-tester@example.org", 1, "ProductionCompany", "شركة", "Production", 0, true, false, "اختبار", "Tester", false, "PBKDF2-SHA256.100000.7DqYchDowXom9IfwCgW+4g==.d41vmNOYVi3nthYMOioczS+W871M/cTKq6Ps58ISAcg=", "+962770000026", 3, new DateTime(2026, 9, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "UserPermissions",
                columns: new[] { "PermissionId", "UserId", "IsActive" },
                values: new object[,]
                {
                    { 9, 1000, true },
                    { 19, 1000, true },
                    { 21, 1000, true },
                    { 23, 1000, true },
                    { 25, 1000, true },
                    { 33, 1000, true },
                    { 35, 1000, true }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssociationProjectSupervisors_ProjectId",
                table: "AssociationProjectSupervisors",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationProjectSupervisors_UserId",
                table: "AssociationProjectSupervisors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Associations_ArabicName",
                table: "Associations",
                column: "ArabicName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Associations_ArabicName_EnglishName",
                table: "Associations",
                columns: new[] { "ArabicName", "EnglishName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Associations_CityId",
                table: "Associations",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Associations_ContactPhone",
                table: "Associations",
                column: "ContactPhone",
                unique: true,
                filter: "[ContactPhone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Associations_EnglishName",
                table: "Associations",
                column: "EnglishName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Associations_Latitude_Longitude",
                table: "Associations",
                columns: new[] { "Latitude", "Longitude" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_ArabicName",
                table: "AssociationsV2",
                column: "ArabicName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_CityId",
                table: "AssociationsV2",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_ContactEmail",
                table: "AssociationsV2",
                column: "ContactEmail",
                unique: true,
                filter: "[ContactEmail] IS NOT NULL AND [ContactEmail] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_ContactPhone",
                table: "AssociationsV2",
                column: "ContactPhone",
                unique: true,
                filter: "[ContactPhone] IS NOT NULL AND [ContactPhone] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_EnglishName",
                table: "AssociationsV2",
                column: "EnglishName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssociationsV2_Latitude_Longitude",
                table: "AssociationsV2",
                columns: new[] { "Latitude", "Longitude" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_CountryId",
                table: "Cities",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_CityLocations_CityId",
                table: "CityLocations",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupPermissions_PermissionId",
                table: "GroupPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_EntityType_EntityId_GroupNameAr",
                table: "Groups",
                columns: new[] { "EntityType", "EntityId", "GroupNameAr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_EntityType_EntityId_GroupNameEn",
                table: "Groups",
                columns: new[] { "EntityType", "EntityId", "GroupNameEn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionNameAr",
                table: "Permissions",
                column: "PermissionNameAr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionNameEn",
                table: "Permissions",
                column: "PermissionNameEn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompanies_CityId",
                table: "ProductionCompanies",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCompanies_RegistrationNumber",
                table: "ProductionCompanies",
                column: "RegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_LocationManagerUserId",
                table: "Projects",
                column: "LocationManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProducerUserId",
                table: "Projects",
                column: "ProducerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProductionCompanyId",
                table: "Projects",
                column: "ProductionCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectTypeId",
                table: "Projects",
                column: "ProjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTypes_NameAr",
                table: "ProjectTypes",
                column: "NameAr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTypes_NameEn",
                table: "ProjectTypes",
                column: "NameEn",
                unique: true);

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
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleNameAr",
                table: "Roles",
                column: "RoleNameAr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleNameEn",
                table: "Roles",
                column: "RoleNameEn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_NameAr",
                table: "Services",
                column: "NameAr",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Services_NameEn",
                table: "Services",
                column: "NameEn",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GroupId",
                table: "UserGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirstNameAr_LastNameAr",
                table: "Users",
                columns: new[] { "FirstNameAr", "LastNameAr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirstNameEn_LastNameEn",
                table: "Users",
                columns: new[] { "FirstNameEn", "LastNameEn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_AssociationId",
                table: "Workers",
                column: "AssociationId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_AssociationV2Id",
                table: "Workers",
                column: "AssociationV2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_CityId",
                table: "Workers",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_CivilIdHash",
                table: "Workers",
                column: "CivilIdHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workers_CountryId",
                table: "Workers",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Workers_QrCode",
                table: "Workers",
                column: "QrCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkerServiceLinks_ServiceId",
                table: "WorkerServiceLinks",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkerServiceLinks_WorkerId_ServiceId",
                table: "WorkerServiceLinks",
                columns: new[] { "WorkerId", "ServiceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssociationProjectSupervisors");

            migrationBuilder.DropTable(
                name: "CityLocations");

            migrationBuilder.DropTable(
                name: "GroupPermissions");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "WorkerServiceLinks");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Workers");

            migrationBuilder.DropTable(
                name: "ProductionCompanies");

            migrationBuilder.DropTable(
                name: "ProjectTypes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AssociationsV2");

            migrationBuilder.DropTable(
                name: "Associations");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}
