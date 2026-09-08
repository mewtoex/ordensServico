using Microsoft.AspNetCore.Identity;
using Moq;
using Os.Api.Application;
using Os.Api.Application.Abstractions;
using Os.Api.Application.Interfaces.Repositories;
using Os.Api.Application.Interfaces.Services;
using Os.Api.Application.Services;
using Os.Api.Domain;
using Xunit;

namespace Os.Tests;

public class ServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrderListingUsesAuthenticatedScopeAndMapsDto(bool isAdmin)
    {
        var actorId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(user => user.Id).Returns(actorId);
        currentUser.SetupGet(user => user.IsAdmin).Returns(isAdmin);
        var repository = new Mock<IOrderRepository>(MockBehavior.Strict);
        var filter = new OrderFilter { Page = 2, PageSize = 3, Search = "Ana" };
        Guid? scope = isAdmin ? null : actorId;
        var order = new ServiceOrder { Customer = new Customer { Name = "Ana" } };
        repository.Setup(repo => repo.ListAsync(filter, scope))
            .ReturnsAsync(new PagedResponse<ServiceOrder>(7, 2, 3, [order]));
        var service = CreateOrdersService(repository.Object, currentUser.Object);

        var result = await service.Orders(filter);

        Assert.Equal(7, result.Total);
        Assert.Equal(2, result.Page);
        Assert.Equal("Ana", Assert.Single(result.Data).CustomerName);
        repository.VerifyAll();
    }

    [Fact]
    public async Task InaccessibleOrderDoesNotExposeHistory()
    {
        var actorId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(user => user.Id).Returns(actorId);
        var repository = new Mock<IOrderRepository>(MockBehavior.Strict);
        repository.Setup(repo => repo.GetByIdAsync(orderId, actorId)).ReturnsAsync((ServiceOrder?)null);
        var service = CreateOrdersService(repository.Object, currentUser.Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.History(orderId));

        repository.Verify(repo => repo.GetHistoryAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AddingItemPersistsAuditAndOrderInOneUnitOfWork()
    {
        var actorId = Guid.NewGuid();
        var order = new ServiceOrder { Customer = new Customer { Name = "Ana" } };
        var item = new CatalogItem { Name = "Reparo", Kind = ItemKind.Servico, Price = 45m };
        var repository = new Mock<IOrderRepository>();
        repository.Setup(repo => repo.GetByIdAsync(order.Id, actorId)).ReturnsAsync(order);
        var catalog = new Mock<ICatalogRepository>();
        catalog.Setup(repo => repo.GetByIdAsync(item.Id)).ReturnsAsync(item);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(user => user.Id).Returns(actorId);
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.SaveChangesAsync(default)).ReturnsAsync(1);
        var service = CreateOrdersService(repository.Object, currentUser.Object, catalog.Object, unitOfWork.Object);

        var result = await service.AddItem(order.Id, new ItemRequest(item.Id, 2));

        Assert.Equal(90m, result.Total);
        Assert.Equal(actorId, Assert.Single(order.History).ActorId);
        repository.Verify(repo => repo.MarkChanged(order), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ClosedOrderDoesNotReadCatalogOrSaveChanges()
    {
        var actorId = Guid.NewGuid();
        var order = new ServiceOrder();
        order.ChangeStatus(OrderStatus.Cancelada, actorId);
        var repository = new Mock<IOrderRepository>();
        repository.Setup(repo => repo.GetByIdAsync(order.Id, actorId)).ReturnsAsync(order);
        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(user => user.Id).Returns(actorId);
        var catalog = new Mock<ICatalogRepository>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var service = CreateOrdersService(repository.Object, currentUser.Object, catalog.Object, unitOfWork.Object);

        await Assert.ThrowsAsync<BusinessException>(() => service.AddItem(order.Id, new ItemRequest(Guid.NewGuid(), 1)));

        catalog.VerifyNoOtherCalls();
        unitOfWork.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task InvalidPaginationDoesNotQueryRepository()
    {
        var repository = new Mock<IOrderRepository>(MockBehavior.Strict);
        var service = CreateOrdersService(repository.Object, Mock.Of<ICurrentUser>());

        await Assert.ThrowsAsync<BusinessException>(() => service.Orders(new OrderFilter { PageSize = 101 }));

        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoginNormalizesEmailAndDoesNotIssueTokenForInvalidPassword()
    {
        var user = new User { Email = "ana@example.com", Active = true };
        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, "correct-password");
        var users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(repo => repo.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        var tokens = new Mock<ITokenIssuer>(MockBehavior.Strict);
        var service = new AuthService(users.Object, hasher, Mock.Of<ICurrentUser>(), tokens.Object);

        var result = await service.Login(new LoginRequest(" ANA@EXAMPLE.COM ", "wrong-password"));

        Assert.Null(result);
        tokens.VerifyNoOtherCalls();
        users.VerifyAll();
    }

    [Fact]
    public async Task ExportUsesOrderDtoAndKeepsFormattedTotal()
    {
        var order = new OrderResponse(Guid.NewGuid(), Guid.NewGuid(), "Ana", Guid.NewGuid(), "Reparo",
            OrderStatus.Aberta, DateTimeOffset.UtcNow, null, 12.50m, []);
        var orders = new Mock<IOrdersService>();
        orders.Setup(service => service.GetOrder(order.Id)).ReturnsAsync(order);
        var service = new OrderSummaryService(orders.Object);

        var file = await service.Export(order.Id);
        var text = System.Text.Encoding.UTF8.GetString(file.Content);

        Assert.Contains("Cliente: Ana", text);
        Assert.Contains("12,50", text);
        Assert.Equal($"os-{order.Id}.txt", file.FileName);
    }

    private static OrdersService CreateOrdersService(IOrderRepository orders, ICurrentUser currentUser,
        ICatalogRepository? catalog = null, IUnitOfWork? unitOfWork = null) => new(
            orders, Mock.Of<ICustomerRepository>(), Mock.Of<IUserRepository>(),
            catalog ?? Mock.Of<ICatalogRepository>(), unitOfWork ?? Mock.Of<IUnitOfWork>(), currentUser);
}
