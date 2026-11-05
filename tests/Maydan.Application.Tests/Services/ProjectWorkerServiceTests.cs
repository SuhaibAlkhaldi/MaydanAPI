using Maydan.Application.DTOs.Projects;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Constants;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;
using Moq;
using Xunit;

namespace Maydan.Application.Tests.Services;

public class ProjectWorkerServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ProjectWorkerService _service;

    public ProjectWorkerServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new ProjectWorkerService(_unitOfWorkMock.Object);
    }

    // 1. POST /api/ProjectWorkers - Bad Scenario (Negative Wage)
    [Fact]
    public async Task CreateAsync_BadScenario_NegativeWageFailsWith400()
    {
        var dto = new CreateProjectWorkerDto { ProjectId = 1, WorkerId = 1, DailyWageAmount = -10.0m };

        var result = await _service.CreateAsync(dto, 1);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("negative", result.MessageEn);
    }

    // 2. PUT /api/ProjectWorkers/{id} - Happy Scenario (Update Wage)
    [Fact]
    public async Task UpdateAsync_HappyScenario_UpdatesWageSuccessfully()
    {
        var superAdmin = new User { Id = 1, RoleId = SystemRoles.BaytAlUrdon, IsActive = true };
        var existingEntity = new ProjectWorker { Id = 1, ProjectId = 1, WorkerId = 1, DailyWageAmount = 20.0m, Project = new Project { Id = 1 } };

        _unitOfWorkMock.Setup(u => u.ProjectWorkers.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(superAdmin);
        _unitOfWorkMock.Setup(u => u.ProjectWorkers.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        var dto = new UpdateProjectWorkerDto { WorkerId = 1, DailyWageAmount = 35.0m, IsWorkerActive = true };

        var result = await _service.UpdateAsync(1, dto, 1);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(35.0m, existingEntity.DailyWageAmount);
    }

    // 3 & 4. PATCH/PUT /api/ProjectWorkers/{id}/activate - Happy Scenario
    [Fact]
    public async Task ActivateAsync_HappyScenario_SetsIsActiveToTrue()
    {
        var superAdmin = new User { Id = 1, RoleId = SystemRoles.BaytAlUrdon, IsActive = true };
        var existingEntity = new ProjectWorker { Id = 1, IsWorkerActive = false, Project = new Project { Id = 1 } };

        _unitOfWorkMock.Setup(u => u.ProjectWorkers.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(superAdmin);
        _unitOfWorkMock.Setup(u => u.ProjectWorkers.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        var result = await _service.ActivateAsync(1, 1);

        Assert.True(result.Success);
        Assert.True(existingEntity.IsWorkerActive);
    }

    // 5 & 6. PATCH/PUT /api/ProjectWorkers/{id}/deactivate - Bad Scenario (Production Company trying to deactivate outside their project)
    [Fact]
    public async Task DeactivateAsync_BadScenario_WrongProdCompanyGets403()
    {
        var wrongProdCompany = new User { Id = 2, RoleId = SystemRoles.ProductionHouse, EntityType = EntityType.ProductionCompany, EntityId = 88, IsActive = true };
        var existingEntity = new ProjectWorker { Id = 1, IsWorkerActive = true, Project = new Project { Id = 1, ProductionCompanyId = 99 } };

        _unitOfWorkMock.Setup(u => u.ProjectWorkers.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(wrongProdCompany);

        var result = await _service.DeactivateAsync(1, 2);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    // 7. GET /api/ProjectWorkers/{id} - Edge Case (Association trying to view another association's worker)
    [Fact]
    public async Task GetByIdAsync_EdgeCase_AssociationGets403ForOtherWorkers()
    {
        var assocUser = new User { Id = 3, RoleId = SystemRoles.Association, EntityType = EntityType.Association, EntityId = 5, IsActive = true };
        var existingWorkerAssignment = new ProjectWorker
        {
            Id = 1,
            Project = new Project { Id = 1 },
            Worker = new Worker { Id = 1, AssociationId = 10 } // Different Association
        };

        _unitOfWorkMock.Setup(u => u.ProjectWorkers.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingWorkerAssignment);
        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(assocUser);

        var result = await _service.GetByIdAsync(1, 3);

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    // 8. GET /api/ProjectWorkers/project/{projectId} - Happy Scenario (Asiza gets all active)
    [Fact]
    public async Task GetByProjectIdAsync_HappyScenario_AsizaCanViewAll()
    {
        var asiza = new User { Id = 2, RoleId = SystemRoles.Aseza, IsActive = true };
        var project = new Project { Id = 1 };
        var workers = new List<ProjectWorker> { new ProjectWorker { Id = 1 }, new ProjectWorker { Id = 2 } };

        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(asiza);
        _unitOfWorkMock.Setup(u => u.Projects.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        _unitOfWorkMock.Setup(u => u.ProjectWorkers.GetByProjectIdAsync(1, null, null, true, It.IsAny<CancellationToken>())).ReturnsAsync(workers);

        var result = await _service.GetByProjectIdAsync(1, null, null, true, 2);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
    }
}