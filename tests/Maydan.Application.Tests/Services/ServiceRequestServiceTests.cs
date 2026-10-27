using Maydan.Application.DTOs.Workers;
using Maydan.Application.Interfaces;
using Maydan.Application.Services;
using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Tests.Services;

public class ServiceRequestServiceTests
{
    private const int CurrentUserId = 100;
    private const int AssociationId = 700;
    private const int TargetProjectId = 1000;
    private const int TargetServiceId = 50;
    private const int TargetRequestId = 9000;

    [Fact]
    public async Task ApproveAndAssignAsync_InactiveWorker_Fails()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 11));

        fixture.AddWorker(
            501,
            AssociationId,
            isActive: false,
            serviceIds: new[] { TargetServiceId });

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 501 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_SoftDeletedWorker_Fails()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 11));

        fixture.AddWorker(
            502,
            AssociationId,
            isDeleted: true,
            serviceIds: new[] { TargetServiceId });

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 502 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_WrongAssociation_Fails()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 11));

        fixture.AddWorker(
            503,
            associationId: 701,
            serviceIds: new[] { TargetServiceId });

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 503 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_UnqualifiedWorker_Fails()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 11));

        fixture.AddWorker(
            504,
            AssociationId,
            serviceIds: new[] { 51 });

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 504 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_DifferentServiceInSameProject_Fails()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 12));

        fixture.AddWorker(
            505, AssociationId, serviceIds: new[] { TargetServiceId });

        fixture.SeedAssignment(
            workerId: 505,
            requestId: 8001,
            projectId: TargetProjectId,
            serviceId: 51,
            requestStartDate: new DateTime(2026, 5, 11),
            requestEndDate: new DateTime(2026, 5, 13),
            projectStartDate: new DateTime(2026, 5, 1),
            projectEndDate: new DateTime(2026, 6, 30));

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 505 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_OverlappingSameServiceRequestInProject_Fails()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 12));

        fixture.AddWorker(
            506, AssociationId, serviceIds: new[] { TargetServiceId });

        fixture.SeedAssignment(
            workerId: 506,
            requestId: 8002,
            projectId: TargetProjectId,
            serviceId: TargetServiceId,
            requestStartDate: new DateTime(2026, 5, 12),
            requestEndDate: new DateTime(2026, 5, 14),
            projectStartDate: new DateTime(2026, 5, 1),
            projectEndDate: new DateTime(2026, 6, 30));

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 506 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_OverlappingProjects_Fails()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 11));

        fixture.AddWorker(
            507, AssociationId, serviceIds: new[] { TargetServiceId });

        fixture.SeedAssignment(
            workerId: 507,
            requestId: 8003,
            projectId: 2000,
            serviceId: 51,
            requestStartDate: new DateTime(2026, 6, 2),
            requestEndDate: new DateTime(2026, 6, 3),
            projectStartDate: new DateTime(2026, 6, 1),
            projectEndDate: new DateTime(2026, 7, 15));

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 507 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_NonOverlappingProjects_Succeeds()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 11));

        fixture.AddWorker(
            508, AssociationId, serviceIds: new[] { TargetServiceId });

        fixture.SeedAssignment(
            workerId: 508,
            requestId: 8004,
            projectId: 2001,
            serviceId: 51,
            requestStartDate: new DateTime(2026, 3, 10),
            requestEndDate: new DateTime(2026, 3, 11),
            projectStartDate: new DateTime(2026, 3, 1),
            projectEndDate: new DateTime(2026, 4, 30));

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 508 });

        AssertSucceeded(fixture, response, expectedWorkerIds: new[] { 508 });
    }

    [Fact]
    public async Task ApproveAndAssignAsync_SameProjectDifferentService_FailsEvenWhenSequential()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 12), new DateTime(2026, 5, 13));

        fixture.AddWorker(
            509, AssociationId, serviceIds: new[] { TargetServiceId });

        fixture.SeedAssignment(
            workerId: 509,
            requestId: 8005,
            projectId: TargetProjectId,
            serviceId: 51,
            requestStartDate: new DateTime(2026, 5, 5),
            requestEndDate: new DateTime(2026, 5, 6),
            projectStartDate: new DateTime(2026, 5, 1),
            projectEndDate: new DateTime(2026, 6, 30));

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 509 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_SameProjectSameServiceNonOverlappingRequests_Succeeds()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 12), new DateTime(2026, 5, 13));

        fixture.AddWorker(
            510, AssociationId, serviceIds: new[] { TargetServiceId });

        fixture.SeedAssignment(
            workerId: 510,
            requestId: 8006,
            projectId: TargetProjectId,
            serviceId: TargetServiceId,
            requestStartDate: new DateTime(2026, 5, 5),
            requestEndDate: new DateTime(2026, 5, 6),
            projectStartDate: new DateTime(2026, 5, 1),
            projectEndDate: new DateTime(2026, 6, 30));

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 510 });

        AssertSucceeded(fixture, response, expectedWorkerIds: new[] { 510 });
    }

    [Fact]
    public async Task ApproveAndAssignAsync_ProjectBoundarySameDate_Fails()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 6, 1, 15, 0, 0), new DateTime(2026, 6, 10),
            TargetServiceId,
            new DateTime(2026, 6, 1, 15, 0, 0), new DateTime(2026, 6, 2));

        fixture.AddWorker(
            511, AssociationId, serviceIds: new[] { TargetServiceId });

        fixture.SeedAssignment(
            workerId: 511,
            requestId: 8007,
            projectId: 2002,
            serviceId: 51,
            requestStartDate: new DateTime(2026, 5, 30),
            requestEndDate: new DateTime(2026, 5, 31),
            projectStartDate: new DateTime(2026, 5, 20),
            projectEndDate: new DateTime(2026, 6, 1, 0, 0, 0));

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 511 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_ProjectStartsFollowingDay_Succeeds()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 6, 2), new DateTime(2026, 6, 10),
            TargetServiceId,
            new DateTime(2026, 6, 2), new DateTime(2026, 6, 3));

        fixture.AddWorker(
            512, AssociationId, serviceIds: new[] { TargetServiceId });

        fixture.SeedAssignment(
            workerId: 512,
            requestId: 8008,
            projectId: 2003,
            serviceId: 51,
            requestStartDate: new DateTime(2026, 5, 30),
            requestEndDate: new DateTime(2026, 5, 31),
            projectStartDate: new DateTime(2026, 5, 20),
            projectEndDate: new DateTime(2026, 6, 1, 0, 0, 0));

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 512 });

        AssertSucceeded(fixture, response, expectedWorkerIds: new[] { 512 });
    }

    [Fact]
    public async Task ApproveAndAssignAsync_DirectWorkerIdsWithConflict_Fails()
    {
        var fixture = CreateFixture(
            1, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 11));

        fixture.AddWorker(
            513, AssociationId, serviceIds: new[] { TargetServiceId });

        fixture.SeedAssignment(
            workerId: 513,
            requestId: 8009,
            projectId: 2004,
            serviceId: 51,
            requestStartDate: new DateTime(2026, 6, 2),
            requestEndDate: new DateTime(2026, 6, 3),
            projectStartDate: new DateTime(2026, 6, 1),
            projectEndDate: new DateTime(2026, 7, 1));

        // Call the assignment operation directly with worker IDs. The service must
        // enforce the conflict rules even when no availability-list operation runs.
        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 513 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_LaterWorkerFails_NoAssignmentsAreStaged()
    {
        var fixture = CreateFixture(
            2, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 11));

        fixture.AddWorker(
            514, AssociationId, serviceIds: new[] { TargetServiceId });
        fixture.AddWorker(
            515, AssociationId, isActive: false,
            serviceIds: new[] { TargetServiceId });

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 514, 515 });

        AssertRejectedWithoutChanges(fixture, response);
    }

    [Fact]
    public async Task ApproveAndAssignAsync_ValidWorkers_SucceedsAndSetsRequestInProgress()
    {
        var fixture = CreateFixture(
            2, TargetProjectId,
            new DateTime(2026, 5, 1), new DateTime(2026, 6, 30),
            TargetServiceId,
            new DateTime(2026, 5, 10), new DateTime(2026, 5, 11));

        fixture.AddWorker(
            516, AssociationId, serviceIds: new[] { TargetServiceId });
        fixture.AddWorker(
            517, AssociationId, serviceIds: new[] { TargetServiceId });

        var response = await fixture.Service.ApproveAndAssignAsync(
            CurrentUserId, TargetRequestId, new List<int> { 516, 517 });

        AssertSucceeded(fixture, response, expectedWorkerIds: new[] { 516, 517 });
    }

    private static void AssertRejectedWithoutChanges(
        TestFixture fixture,
        Maydan.Application.DTOs.Common.ApiResponse<object?> response)
    {
        Assert.False(response.Success);
        Assert.Empty(fixture.ServiceRequests.StagedAssignments);
        Assert.Equal(0, fixture.UnitOfWork.SaveChangesCount);
        Assert.Equal(ServiceRequestStatus.PendingWorkerSelection, fixture.TargetRequest.Status);
        Assert.Equal(0, fixture.TargetRequest.SelectedWorkersCount);
    }

    private static void AssertSucceeded(
        TestFixture fixture,
        Maydan.Application.DTOs.Common.ApiResponse<object?> response,
        int[] expectedWorkerIds)
    {
        Assert.True(response.Success);
        Assert.Equal(expectedWorkerIds.Length, fixture.TargetRequest.SelectedWorkersCount);
        Assert.Equal(ServiceRequestStatus.InProgress, fixture.TargetRequest.Status);
        Assert.Equal(1, fixture.UnitOfWork.SaveChangesCount);
        Assert.Equal(
            expectedWorkerIds.OrderBy(id => id),
            fixture.ServiceRequests.StagedAssignments
                .Select(assignment => assignment.WorkerId)
                .OrderBy(id => id));
    }

    private static TestFixture CreateFixture(
        int requestedWorkersCount,
        int projectId,
        DateTime projectStartDate,
        DateTime projectEndDate,
        int serviceId,
        DateTime requestStartDate,
        DateTime requestEndDate)
    {
        var association = new Association
        {
            Id = AssociationId,
            ArabicName = "الجمعية",
            EnglishName = "Association",
            IsActive = true
        };

        var service = new Service
        {
            Id = serviceId,
            NameEn = $"Service {serviceId}",
            NameAr = $"الخدمة {serviceId}",
            IsActive = true
        };

        var project = NewProject(projectId, projectStartDate, projectEndDate);

        var targetRequest = new ServiceRequest
        {
            Id = TargetRequestId,
            ProjectId = projectId,
            Project = project,
            ServiceId = serviceId,
            Service = service,
            AssociationId = AssociationId,
            Association = association,
            ProductionCompanyId = 300,
            ProductionCompany = new ProductionCompany
            {
                Id = 300,
                IsActive = true
            },
            StartDate = requestStartDate,
            EndDate = requestEndDate,
            RequestedWorkersCount = requestedWorkersCount,
            SelectedWorkersCount = 0,
            Status = ServiceRequestStatus.PendingWorkerSelection,
            IsActive = true
        };

        var permission = new Permission
        {
            PermissionId = 1,
            PermissionNameEn = "Manage Service Requests",
            PermissionNameAr = "إدارة طلبات الخدمات",
            IsActive = true
        };

        var currentUser = new User
        {
            UserId = CurrentUserId,
            EntityType = EntityType.Association,
            EntityId = AssociationId,
            IsActive = true,
            Role = new Role
            {
                RoleId = 1,
                IsActive = true,
                RolePermissions = new List<RolePermission>
                {
                    new()
                    {
                        RoleId = 1,
                        PermissionId = permission.PermissionId,
                        IsActive = true,
                        Permission = permission
                    }
                }
            }
        };

        var users = new FakeUserRepository(currentUser);
        var workers = new FakeWorkerRepository();
        var serviceRequests = new FakeServiceRequestRepository(targetRequest);
        var unitOfWork = new FakeUnitOfWork(users, workers, serviceRequests);
        var serviceRequestService = new ServiceRequestService(unitOfWork, new FakeEmailSender());

        return new TestFixture(
            targetRequest,
            workers,
            serviceRequests,
            unitOfWork,
            serviceRequestService);
    }

    private static Project NewProject(
        int projectId,
        DateTime startDate,
        DateTime endDate) =>
        new()
        {
            Id = projectId,
            ProjectNameEn = $"Project {projectId}",
            ProjectNameAr = $"المشروع {projectId}",
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true
        };

    private sealed class TestFixture
    {
        public TestFixture(
            ServiceRequest targetRequest,
            FakeWorkerRepository workers,
            FakeServiceRequestRepository serviceRequests,
            FakeUnitOfWork unitOfWork,
            ServiceRequestService service)
        {
            TargetRequest = targetRequest;
            Workers = workers;
            ServiceRequests = serviceRequests;
            UnitOfWork = unitOfWork;
            Service = service;
        }

        public ServiceRequest TargetRequest { get; }
        public FakeWorkerRepository Workers { get; }
        public FakeServiceRequestRepository ServiceRequests { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public ServiceRequestService Service { get; }

        public void AddWorker(
            int workerId,
            int associationId,
            bool isActive = true,
            bool isDeleted = false,
            int[]? serviceIds = null)
        {
            var worker = new Worker
            {
                Id = workerId,
                AssociationId = associationId,
                Association = new Association
                {
                    Id = associationId,
                    IsActive = true
                },
                FirstName = $"Worker{workerId}",
                LastName = "Test",
                IsActive = isActive,
                IsDeleted = isDeleted,
                WorkerServices = (serviceIds ?? Array.Empty<int>())
                    .Select(id => new WorkerServiceLink
                    {
                        WorkerId = workerId,
                        ServiceId = id,
                        IsActive = true,
                        Service = new Service
                        {
                            Id = id,
                            NameEn = $"Service {id}",
                            NameAr = $"الخدمة {id}",
                            IsActive = true
                        }
                    })
                    .ToList()
            };

            Workers.Add(worker);
        }

        public void SeedAssignment(
            int workerId,
            int requestId,
            int projectId,
            int serviceId,
            DateTime requestStartDate,
            DateTime requestEndDate,
            DateTime projectStartDate,
            DateTime projectEndDate)
        {
            var existingProject = NewProject(projectId, projectStartDate, projectEndDate);
            var existingRequest = new ServiceRequest
            {
                Id = requestId,
                ProjectId = projectId,
                Project = existingProject,
                ServiceId = serviceId,
                Service = new Service
                {
                    Id = serviceId,
                    NameEn = $"Service {serviceId}",
                    NameAr = $"الخدمة {serviceId}",
                    IsActive = true
                },
                AssociationId = AssociationId,
                Association = new Association
                {
                    Id = AssociationId,
                    IsActive = true
                },
                ProductionCompanyId = 300,
                ProductionCompany = new ProductionCompany
                {
                    Id = 300,
                    IsActive = true
                },
                StartDate = requestStartDate,
                EndDate = requestEndDate,
                RequestedWorkersCount = 1,
                SelectedWorkersCount = 1,
                Status = ServiceRequestStatus.InProgress,
                IsActive = true
            };

            ServiceRequests.SeedAssignment(workerId, existingRequest);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _currentUser;

        public FakeUserRepository(User currentUser)
        {
            _currentUser = currentUser;
        }

        public Task<User?> GetWithPermissionsAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(userId == _currentUser.UserId ? _currentUser : null);

        public Task<User?> GetByIdAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(userId == _currentUser.UserId ? _currentUser : null);

        public Task<User?> GetByEmailWithAccessAsync(
            string email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<List<User>> GetByEntityAsync(
            EntityType entityType,
            int entityId,
            string? search,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<User>());

        public Task<(List<User> Users, int TotalCount)> GetPagedByEntityAsync(
            EntityType entityType,
            int entityId,
            string? search,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((new List<User>(), 0));

        public Task<User?> GetDetailsAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetDetailsReadOnlyAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<bool> EmailExistsAsync(
            string email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> PhoneExistsAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<User?> GetByUserNameEnAsync(
            User user,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<User?> GetByUserNameArAsync(
            User user,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<List<User>> GetByIdsInEntityAsync(
            IEnumerable<int> userIds,
            EntityType entityType,
            int entityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<User>());

        public Task<List<User>> GetDeletedByEntityAsync(
            EntityType entityType,
            int entityId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<User>());

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Remove(User user)
        {
        }

        public Task<User?> GetAssociationAdminForUpdateAsync(int associationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> EmailExistsForAnotherUserAsync(string email, int excludedUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PhoneExistsForAnotherUserAsync(string phoneNumber, int excludedUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<User>> GetAssociationUsersForUpdateAsync(int associationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<User>> GetDeletedAssociationUsersForUpdateAsync(int associationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeWorkerRepository : IWorkerRepository
    {
        private readonly Dictionary<int, Worker> _workers = new();

        public void Add(Worker worker) => _workers[worker.Id] = worker;

        public Task<Worker?> GetByIdWithServicesAsync(
            int workerId,
            CancellationToken cancellationToken = default)
        {
            if (!_workers.TryGetValue(workerId, out var worker) || worker.IsDeleted)
            {
                return Task.FromResult<Worker?>(null);
            }

            return Task.FromResult<Worker?>(worker);
        }

        public Task<Worker?> GetByIdAsync(
            int workerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_workers.TryGetValue(workerId, out var worker) ? worker : null);

        public Task<Worker?> GetByIdIncludingDeletedAsync(
            int workerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_workers.TryGetValue(workerId, out var worker) ? worker : null);

        public Task<Worker?> GetByCivilIdHashAsync(
            string civilIdHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Worker?>(null);

        public Task<Worker?> GetByPhoneNumberAsync(
            string phoneNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Worker?>(null);

        public Task<List<Worker>> GetByAssociationIdAsync(
            int associationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_workers.Values
                .Where(worker => worker.AssociationId == associationId && !worker.IsDeleted)
                .ToList());

        public Task<List<Worker>> GetDeletedByAssociationIdAsync(
            int associationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_workers.Values
                .Where(worker => worker.AssociationId == associationId && worker.IsDeleted)
                .ToList());

        public Task<bool> Remove(
            Worker worker,
            CancellationToken cancellationToken = default)
        {
            if (!_workers.TryGetValue(worker.Id, out var existing))
            {
                return Task.FromResult(false);
            }

            existing.IsDeleted = true;
            return Task.FromResult(true);
        }

        public Task AddAsync(
            Worker worker,
            CancellationToken cancellationToken = default)
        {
            _workers[worker.Id] = worker;
            return Task.CompletedTask;
        }

        public Task<List<Worker>> QueryAsync(
            bool isDeleted,
            int? associationId = null,
            bool? orderByYearsOfExperienceAscending = null,
            bool? orderByNameEnAscending = null,
            bool? orderByNameArAscending = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Worker> query = _workers.Values
                .Where(worker => worker.IsDeleted == isDeleted);

            if (associationId.HasValue)
            {
                query = query.Where(worker => worker.AssociationId == associationId.Value);
            }

            return Task.FromResult(query.ToList());
        }

        public Task<(List<WorkerSummaryDto> Items, int TotalCount)> GetAllProjectedAsync(
            int? associationId,
            string? search,
            int? serviceId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((new List<WorkerSummaryDto>(), 0));

        public Task<WorkerDto?> GetByIdProjectedAsync(
            int workerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WorkerDto?>(null);

        public Task<List<Worker>> GetAvailableWorkersForRequestAsync(
            int associationId,
            int serviceId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Worker>());

        Task<List<WorkerDto>> IWorkerRepository.GetAvailableWorkersForRequestAsync(int associationId, int serviceId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<Worker>> GetByAssociationIdForUpdateAsync(int associationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeServiceRequestRepository : IServiceRequestRepository
    {
        private readonly Dictionary<int, ServiceRequest> _requests = new();
        private readonly List<ServiceRequestWorker> _assignments = new();

        public FakeServiceRequestRepository(ServiceRequest targetRequest)
        {
            _requests[targetRequest.Id] = targetRequest;
        }

        public List<ServiceRequestWorker> StagedAssignments { get; } = new();

        public void SeedAssignment(int workerId, ServiceRequest request)
        {
            _requests[request.Id] = request;
            _assignments.Add(new ServiceRequestWorker
            {
                ServiceRequestId = request.Id,
                ServiceRequest = request,
                WorkerId = workerId,
                IsActive = true
            });
        }

        public Task<ServiceRequest?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.TryGetValue(id, out var request) ? request : null);

        public Task AddAsync(
            ServiceRequest serviceRequest,
            CancellationToken cancellationToken = default)
        {
            _requests[serviceRequest.Id] = serviceRequest;
            return Task.CompletedTask;
        }

        public void Remove(ServiceRequest serviceRequest)
        {
            _requests.Remove(serviceRequest.Id);
        }

        public Task<List<ServiceRequest>> GetByProductionCompanyIdAsync(
            int productionCompanyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.Values
                .Where(request => request.ProductionCompanyId == productionCompanyId)
                .ToList());

        public Task<List<ServiceRequest>> GetByAssociationIdAsync(
            int associationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.Values
                .Where(request => request.AssociationId == associationId)
                .ToList());

        public Task<List<ServiceRequest>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.Values.ToList());

        public Task<List<ServiceRequest>> GetPendingOlderThanAsync(
            DateTime threshold,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_requests.Values
                .Where(request =>
                    request.Status == ServiceRequestStatus.PendingWorkerSelection &&
                    !request.ReminderSent &&
                    request.CreatedAt <= threshold)
                .ToList());

        public Task<bool> HasDifferentServiceAssignmentInProjectAsync(
            int workerId,
            int serviceRequestId,
            int projectId,
            int serviceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_assignments.Any(assignment =>
                assignment.WorkerId == workerId &&
                assignment.ServiceRequestId != serviceRequestId &&
                assignment.ServiceRequest.ProjectId == projectId &&
                assignment.ServiceRequest.ServiceId != serviceId));

        public Task<bool> HasOverlappingServiceRequestAssignmentInProjectAsync(
            int workerId,
            int serviceRequestId,
            int projectId,
            DateTime requestStartDate,
            DateTime requestEndDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_assignments.Any(assignment =>
                assignment.WorkerId == workerId &&
                assignment.ServiceRequestId != serviceRequestId &&
                assignment.ServiceRequest.ProjectId == projectId &&
                assignment.ServiceRequest.StartDate <= requestEndDate &&
                assignment.ServiceRequest.EndDate >= requestStartDate));

        public Task<bool> HasOverlappingProjectAssignmentAsync(
            int workerId,
            int serviceRequestId,
            int projectId,
            DateTime projectStartDate,
            DateTime projectEndDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_assignments.Any(assignment =>
                assignment.WorkerId == workerId &&
                assignment.ServiceRequestId != serviceRequestId &&
                assignment.ServiceRequest.ProjectId != projectId &&
                assignment.ServiceRequest.Project.StartDate.Date <= projectEndDate.Date &&
                assignment.ServiceRequest.Project.EndDate.Date >= projectStartDate.Date));

        public Task AssignToServiceRequestAsync(
            int serviceRequestId,
            int workerId,
            CancellationToken cancellationToken = default)
        {
            StagedAssignments.Add(new ServiceRequestWorker
            {
                ServiceRequestId = serviceRequestId,
                WorkerId = workerId
            });

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(
            IUserRepository users,
            IWorkerRepository workers,
            IServiceRequestRepository serviceRequests)
        {
            Users = users;
            Workers = workers;
            ServiceRequests = serviceRequests;
        }

        public IUserRepository Users { get; }
        public IWorkerRepository Workers { get; }
        public IServiceRequestRepository ServiceRequests { get; }

        public IRoleRepository Roles => null!;
        public IPermissionRepository Permissions => null!;
        public IGroupRepository Groups => null!;
        public IProjectTypeRepository ProjectTypes => null!;
        public ICountryRepository Countries => null!;
        public ICityRepository Cities => null!;
        public ICityLocationRepository CityLocations => null!;
        public IAssociationRepository Associations => null!;
        public IAssociationProjectSupervisorRepository AssociationProjectSupervisors => null!;
        public IProductionCompanyRepository ProductionCompanies => null!;
        public IProjectRepository Projects => null!;
        public IPasswordResetTokenRepository PasswordResetTokens => null!;
        public IRefreshTokenRepository RefreshTokens => null!;
        public ISystemConfigurationRepository SystemConfigurations => null!;
        public IServiceRepository Services => null!;

        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(
            Func<Task> operation,
            CancellationToken cancellationToken = default) =>
            operation();
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task SendAsync(
            string toEmail,
            string subject,
            string bodyHtml,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
