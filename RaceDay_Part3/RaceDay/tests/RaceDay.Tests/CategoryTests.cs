using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Tests;

public class CategoryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CategoryTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetCategories_Public_ReturnsSeededList()
    {
        var response = await _factory.CreateClient().GetAsync("/api/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await TestHelpers.ReadJsonAsync(response);
        Assert.True(json.GetArrayLength() > 0);
    }

    [Fact]
    public async Task CreateCategory_Organiser_Returns201()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/categories",
            new { categoryName = "15km Trail", distanceKm = 15, activityType = "run", description = "Trail run" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Run", (await TestHelpers.ReadJsonAsync(response)).GetProperty("activityType").GetString());
    }

    [Fact]
    public async Task CreateCategory_Participant_Returns403()
    {
        var participant = await TestHelpers.CreateUserClientAsync(_factory, "Participant");
        var response = await participant.PostAsJsonAsync("/api/categories",
            new { categoryName = "Nope", distanceKm = 5, activityType = "Run" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_InvalidActivityType_Returns400()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var response = await organiser.PostAsJsonAsync("/api/categories",
            new { categoryName = "Swim", distanceKm = 1, activityType = "Swim" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_InUse_Returns409()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var create = await organiser.PostAsJsonAsync("/api/categories",
            new { categoryName = "Used Category", distanceKm = 7, activityType = "Walk" });
        var categoryId = (await TestHelpers.ReadJsonAsync(create)).GetProperty("categoryId").GetInt32();

        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var attach = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryId, entryFee = 50, maximumParticipants = 10 });
        Assert.Equal(HttpStatusCode.Created, attach.StatusCode);

        var delete = await organiser.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_Unused_Returns204()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var create = await organiser.PostAsJsonAsync("/api/categories",
            new { categoryName = "Throwaway", distanceKm = 3, activityType = "Walk" });
        var categoryId = (await TestHelpers.ReadJsonAsync(create)).GetProperty("categoryId").GetInt32();

        var delete = await organiser.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task EventCategories_AttachUpdateAndDelete_WorkForOwner()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var ecId = await TestHelpers.AttachCategoryAsync(organiser, eventId, 50);

        var list = await _factory.CreateClient().GetAsync($"/api/events/{eventId}/categories"); // public
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(1, (await TestHelpers.ReadJsonAsync(list)).GetArrayLength());

        var update = await organiser.PutAsJsonAsync($"/api/event-categories/{ecId}",
            new { entryFee = 200, maximumParticipants = 75 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(75, (await TestHelpers.ReadJsonAsync(update)).GetProperty("maximumParticipants").GetInt32());

        var delete = await organiser.DeleteAsync($"/api/event-categories/{ecId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task EventCategories_AttachSameCategoryTwice_Returns409()
    {
        var organiser = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        await TestHelpers.AttachCategoryAsync(organiser, eventId);

        var categoryId = await TestHelpers.GetFirstCategoryIdAsync(organiser);
        var second = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryId, entryFee = 10, maximumParticipants = 10 });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task EventCategories_AttachByOtherOrganiser_Returns403()
    {
        var owner = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var other = await TestHelpers.CreateUserClientAsync(_factory, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);
        var categoryId = await TestHelpers.GetFirstCategoryIdAsync(owner);

        var response = await other.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryId, entryFee = 10, maximumParticipants = 10 });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EventCategories_ForUnknownEvent_Returns404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/999999/categories");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
