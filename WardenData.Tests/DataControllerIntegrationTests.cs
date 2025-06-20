using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WardenData.Models; // For AppDbContext and core entities like Order, Session
// Using WardenData.Tests.Helpers; // Not strictly needed as WAF handles services

using System.Net;
using System.Net.Http; // For HttpClient
using System.Net.Http.Json; // For PostAsJsonAsync, ReadFromJsonAsync
using System.Text.Json; // For JsonElement, JsonDocument
using System.Text.Json.Serialization; // For JsonPropertyName

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

// Explicitly reference the namespace of the Program class for WebApplicationFactory
// For top-level statements, the class is 'Program' in the global namespace or assembly's default namespace.
// If WardenData has a specific namespace for Program.cs, it would be needed here.
// Assuming global or default namespace for 'Program' based on typical top-level statement projects.

namespace WardenData.Tests
{
    [TestClass]
    public class DataControllerIntegrationTests
    {
        private WebApplicationFactory<Program> _factory = null!; // Initialize in Setup
        private HttpClient _client = null!; // Initialize in Setup

        [TestInitialize]
        public void Setup()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(services =>
                    {
                        // Remove the existing AppDbContext registration
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        // Add AppDbContext with an in-memory database for testing
                        var dbName = Guid.NewGuid().ToString();
                        services.AddDbContext<AppDbContext>(options =>
                        {
                            options.UseInMemoryDatabase(dbName);
                        });

                        // Ensure the database is created for each test
                        var sp = services.BuildServiceProvider();
                        using (var scope = sp.CreateScope())
                        {
                            var scopedServices = scope.ServiceProvider;
                            var db = scopedServices.GetRequiredService<AppDbContext>();
                            // db.Database.EnsureDeleted(); // Ensure clean state
                            db.Database.EnsureCreated(); // Create schema
                        }
                    });
                });
            _client = _factory.CreateClient();
        }

        [TestCleanup]
        public void Teardown()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        // Minimal DTOs for testing, mirroring structure of actual DTOs in DataController
        public class TestOrderDTO { public int Id { get; set; } public string? Name { get; set; } }

        public class TestOrderEffectDTO
        {
            public int Id { get; set; }
            [JsonPropertyName("OrderId")]
            public int OrderId { get; set; }
            [JsonPropertyName("EffectName")]
            public string? EffectName { get; set; }
            [JsonPropertyName("MinValue")]
            public long MinValue { get; set; }
            [JsonPropertyName("MaxValue")]
            public long MaxValue { get; set; }
            [JsonPropertyName("DesiredValue")]
            public long DesiredValue { get; set; }
        }

        public class TestSessionDTO
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
            public long Timestamp { get; set; }
            public string? InitialEffects { get; set; }
            public string? RunesPrices { get; set; }
        }

        public class TestRuneHistoryDTO
        {
            public int Id { get; set; }
            [JsonPropertyName("session_id")]
            public int SessionId { get; set; }
            [JsonPropertyName("rune_id")]
            public int RuneId { get; set; }
            [JsonPropertyName("is_tenta")]
            public bool IsTenta { get; set; }
            [JsonPropertyName("effects_after")]
            public JsonElement EffectsAfter { get; set; }
            [JsonPropertyName("has_succeed")]
            public bool HasSucceed { get; set; }
        }

        public class ApiResponse { public int Received { get; set; } }

        [TestMethod]
        public async Task ReceiveOrders_PostValidOrders_ReturnsOkAndSavesData()
        {
            // Arrange
            var testOrdersDto = new List<TestOrderDTO>
            {
                new TestOrderDTO { Id = 101, Name = "Integration Test Order 1" },
                new TestOrderDTO { Id = 102, Name = "Integration Test Order 2" }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Data/orders", testOrdersDto);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.IsNotNull(apiResponse);
            Assert.AreEqual(testOrdersDto.Count, apiResponse.Received);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.AreEqual(testOrdersDto.Count, await context.Orders.CountAsync());
                foreach (var orderDto in testOrdersDto)
                {
                    var savedOrder = await context.Orders.FirstOrDefaultAsync(o => o.OriginalId == orderDto.Id);
                    Assert.IsNotNull(savedOrder);
                    Assert.AreEqual(orderDto.Name, savedOrder.Name);
                }
            }
        }

        [TestMethod]
        public async Task ReceiveOrderEffects_PostValidEffectsWithExistingOrder_ReturnsOkAndSavesData()
        {
            // Arrange
            var parentOrderOriginalId = 201;
            var parentOrder = new Order { Id = Guid.NewGuid(), OriginalId = parentOrderOriginalId, Name = "Parent Order for Effects" };
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Orders.Add(parentOrder);
                await context.SaveChangesAsync();
            }

            var testEffectsDto = new List<TestOrderEffectDTO>
            {
                new TestOrderEffectDTO { Id = 301, OrderId = parentOrderOriginalId, EffectName = "IntTest Effect", MinValue = 1, MaxValue = 10, DesiredValue = 5 }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Data/order-effects", testEffectsDto);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.IsNotNull(apiResponse);
            Assert.AreEqual(testEffectsDto.Count, apiResponse.Received);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Assert.AreEqual(testEffectsDto.Count, await context.OrderEffects.CountAsync());
                var savedEffect = await context.OrderEffects.FirstOrDefaultAsync(oe => oe.OriginalId == testEffectsDto[0].Id);
                Assert.IsNotNull(savedEffect);
                Assert.AreEqual(parentOrder.Id, savedEffect.OrderId);
            }
        }

        [TestMethod]
        public async Task ReceiveOrderEffects_PostWithMissingOrder_ReturnsBadRequest()
        {
            // Arrange
            var testEffectsDto = new List<TestOrderEffectDTO>
            {
                new TestOrderEffectDTO { Id = 401, OrderId = 9999, EffectName = "MissingParentTest" }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Data/order-effects", testEffectsDto);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(error.Contains("Order avec OriginalId 9999 introuvable."));
        }

        [TestMethod]
        public async Task ReceiveSessions_PostValidSessionsWithExistingOrder_ReturnsOkAndSavesData()
        {
            // Arrange
            var parentOrderOriginalId = 501;
            var parentOrderGuid = Guid.NewGuid();
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.Orders.Add(new Order { Id = parentOrderGuid, OriginalId = parentOrderOriginalId, Name = "Parent Order for Sessions" });
                await context.SaveChangesAsync();
            }

            var testSessionsDto = new List<TestSessionDTO>
            {
                new TestSessionDTO { Id = 601, OrderId = parentOrderOriginalId, Timestamp = 12345L, InitialEffects = "{}", RunesPrices = "{}" }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Data/sessions", testSessionsDto);

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.IsNotNull(apiResponse);
            Assert.AreEqual(testSessionsDto.Count, apiResponse.Received);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var savedSession = await context.Sessions.FirstOrDefaultAsync(s => s.OriginalId == testSessionsDto[0].Id);
                Assert.IsNotNull(savedSession);
                Assert.AreEqual(parentOrderGuid, savedSession.OrderId);
            }
        }

        [TestMethod]
        public async Task ReceiveRuneHistory_PostValidHistoryWithExistingSession_ReturnsOkAndSavesData()
        {
            // Arrange
            var orderOriginalId = 701;
            var orderGuid = Guid.NewGuid();
            var sessionOriginalId = 801;
            var sessionGuid = Guid.NewGuid();

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var order = new Order { Id = orderGuid, OriginalId = orderOriginalId, Name = "Parent Order for RH" };
                context.Orders.Add(order);
                // await context.SaveChangesAsync(); // SaveChangesAsync here will cause issues if session is added immediately after

                var session = new Session { Id = sessionGuid, OriginalId = sessionOriginalId, OrderId = order.Id, Timestamp = 123, InitialEffects = "{}", RunesPrices = "{}" };
                context.Sessions.Add(session);
                await context.SaveChangesAsync(); // Consolidate SaveChangesAsync
            }

            var effectsJson = JsonDocument.Parse("{\"key\":\"value\"}").RootElement;
            var testHistoryDto = new List<TestRuneHistoryDTO>
            {
                new TestRuneHistoryDTO { Id = 901, SessionId = sessionOriginalId, RuneId = 1, IsTenta = false, EffectsAfter = effectsJson, HasSucceed = true }
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Data/rune-history", testHistoryDto);

            // Assert
            response.EnsureSuccessStatusCode();
            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
            Assert.IsNotNull(apiResponse);
            Assert.AreEqual(testHistoryDto.Count, apiResponse.Received);

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var savedHistory = await context.RuneHistories.FirstOrDefaultAsync(rh => rh.OriginalId == testHistoryDto[0].Id);
                Assert.IsNotNull(savedHistory);
                Assert.AreEqual(sessionGuid, savedHistory.SessionId);
            }
        }
    }
}
