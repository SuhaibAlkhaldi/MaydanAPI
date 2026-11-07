using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Constants;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Moq;
using Xunit;

namespace Maydan.Application.Tests.Services;

public class ProjectAssociationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ProjectAssociationService _service;

    public ProjectAssociationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new ProjectAssociationService(_unitOfWorkMock.Object);
    }

    // 1. POST /api/ProjectAssociations - Happy Scenario (Ghaith creates)
    [Fact]
    public async Task CreateAsync_HappyScenario_GhaithCreatesSuccessfully()
    {
        var superAdmin = new User { Id = 1, RoleId = SystemRoles.BaytAlUrdon, IsActive = true };
        var project = new Project { Id = 1 };
        var association = new Association { Id = 1 };

        // التجهيز القديم
        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(superAdmin);
        _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _unitOfWorkMock.Setup(u => u.Associations.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(association);
        _unitOfWorkMock.Setup(u => u.ProjectAssociations.ExistsAsync(1, 1, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // 🌟 السطر السحري الجديد اللي بيحل المشكلة:
        // بنحكي للـ Mock: لما ينطلب منك الكيان بعد الإنشاء، رجع كيان وهمي مع مصفوفة Locations فارغة عشان ما يضرب الـ MapToDto
        var createdEntity = new ProjectAssociation { Id = 1, ProjectId = 1, AssociationId = 1, Locations = new List<ProjectAssociationLocation>() };
        _unitOfWorkMock.Setup(u => u.ProjectAssociations.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(createdEntity);

        var dto = new CreateProjectAssociationDto { ProjectId = 1, AssociationId = 1, Locations = new List<CreateUpdateProjectAssociationLocationDto>() };

        var result = await _service.CreateAsync(dto, 1);

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
    }

    // 2. POST /api/ProjectAssociations - Edge Case (Duplicate Assignment)
    [Fact]
    public async Task CreateAsync_EdgeCase_DuplicateFailsWith400()
    {
        var superAdmin = new User { Id = 1, RoleId = SystemRoles.BaytAlUrdon, IsActive = true };
        var project = new Project { Id = 1 };
        var association = new Association { Id = 1 };

        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(superAdmin);
        _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _unitOfWorkMock.Setup(u => u.Associations.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(association);

        // Edge Case: System says this association is already linked!
        _unitOfWorkMock.Setup(u => u.ProjectAssociations.ExistsAsync(1, 1, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var dto = new CreateProjectAssociationDto { ProjectId = 1, AssociationId = 1 };

        var result = await _service.CreateAsync(dto, 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("already linked", result.MessageEn);
    }

    // 3. PUT /api/ProjectAssociations/{id} - Bad Scenario (Asiza tries to update)
    [Fact]
    public async Task UpdateAsync_BadScenario_AsizaGets403Forbidden()
    {
        var asiza = new User { Id = 2, RoleId = SystemRoles.Aseza, IsActive = true };
        var existingEntity = new ProjectAssociation { Id = 1, ProjectId = 1, AssociationId = 1, Project = new Project { Id = 1 } };

        _unitOfWorkMock.Setup(u => u.ProjectAssociations.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(asiza);

        var dto = new UpdateProjectAssociationDto { AssociationId = 2 };

        var result = await _service.UpdateAsync(1, dto, 2);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    // 4. DELETE /api/ProjectAssociations/{id} - Happy Scenario
    [Fact]
    public async Task DeleteAsync_HappyScenario_DeletesSuccessfully()
    {
        var prodHouse = new User { Id = 3, RoleId = SystemRoles.ProductionHouse, EntityId = 99, IsActive = true };
        var existingEntity = new ProjectAssociation
        {
            Id = 1,
            Project = new Project { Id = 1, ProductionCompanyId = 99 },
            Locations = new List<ProjectAssociationLocation> { new ProjectAssociationLocation { Id = 1 } }
        };

        _unitOfWorkMock.Setup(u => u.ProjectAssociations.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(prodHouse);

        var result = await _service.DeleteAsync(1, 3);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.True(existingEntity.Locations.First().IsDeleted); // Verifies cascade soft-delete
        _unitOfWorkMock.Verify(u => u.ProjectAssociations.Remove(existingEntity), Times.Once);
    }

    // 5. GET /api/ProjectAssociations/project/{projectId} - Edge Case (Association sees only its own data)
    [Fact]
    public async Task GetByProjectIdAsync_EdgeCase_AssociationUserIsFiltered()
    {
        var targetAssocId = 5;
        var assocUser = new User { Id = 4, RoleId = SystemRoles.Association, EntityType = EntityType.Association, EntityId = targetAssocId, IsActive = true };
        var project = new Project { Id = 1 };

        var associations = new List<ProjectAssociation>
        {
            new ProjectAssociation { Id = 1, ProjectId = 1, AssociationId = 5 }, // Owns this
            new ProjectAssociation { Id = 2, ProjectId = 1, AssociationId = 10 } // Competitor
        };

        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(assocUser);
        _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _unitOfWorkMock.Setup(u => u.ProjectAssociations.GetByProjectIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(associations);

        var result = await _service.GetByProjectIdAsync(1, 4);

        Assert.True(result.Success);
        Assert.Single(result.Data!);
        Assert.Equal(targetAssocId, result.Data!.First().AssociationId);
    }
}