using System.Text;
using System.Text.Json;
using DigitalMe.Configuration;
using DigitalMe.Integrations.External.ClickUp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalMe.Integrations.External.ClickUp;

/// <summary>
/// ClickUp API integration service for task management, time tracking, and project organization.
/// Implements comprehensive ClickUp API v2 functionality with rate limiting and error handling.
/// </summary>
public class ClickUpService : IClickUpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClickUpService> _logger;
    private readonly ClickUpSettings _settings;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string BaseUrl = "https://api.clickup.com/api/v2";

    public ClickUpService(
        HttpClient httpClient,
        ILogger<ClickUpService> logger,
        IOptions<ClickUpSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", _settings.ApiToken);
        _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        _logger.LogInformation("ClickUpService initialized with team ID: {TeamId}", _settings.TeamId);
    }

    #region Task Management

    public async Task<ClickUpTask> CreateTaskAsync(string listId, CreateTaskRequest request)
    {
        try
        {
            _logger.LogInformation("Creating task in list {ListId}: {TaskName}", listId, request.Name);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/list/{listId}/task", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var task = JsonSerializer.Deserialize<ClickUpTask>(responseJson, _jsonOptions);

            _logger.LogInformation("Task created successfully: {TaskId}", task?.Id);
            return task!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create task in list {ListId}", listId);
            throw;
        }
    }

    public async Task<ClickUpTask> GetTaskAsync(string taskId)
    {
        try
        {
            _logger.LogDebug("Getting task: {TaskId}", taskId);

            var response = await _httpClient.GetAsync($"/task/{taskId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var task = JsonSerializer.Deserialize<ClickUpTask>(json, _jsonOptions);

            return task!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get task {TaskId}", taskId);
            throw;
        }
    }

    public async Task<ClickUpTask> UpdateTaskAsync(string taskId, UpdateTaskRequest request)
    {
        try
        {
            _logger.LogInformation("Updating task: {TaskId}", taskId);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/task/{taskId}", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var task = JsonSerializer.Deserialize<ClickUpTask>(responseJson, _jsonOptions);

            _logger.LogInformation("Task updated successfully: {TaskId}", taskId);
            return task!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task {TaskId}", taskId);
            throw;
        }
    }

    public async Task<bool> DeleteTaskAsync(string taskId)
    {
        try
        {
            _logger.LogInformation("Deleting task: {TaskId}", taskId);

            var response = await _httpClient.DeleteAsync($"/task/{taskId}");
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Task deleted successfully: {TaskId}", taskId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete task {TaskId}", taskId);
            return false;
        }
    }

    #endregion

    #region Task Search & Filtering

    public async Task<TasksResponse> GetTasksAsync(string listId, TaskFilters? filters = null)
    {
        try
        {
            _logger.LogDebug("Getting tasks for list: {ListId}", listId);

            var queryString = BuildTaskFilterQuery(filters);
            var response = await _httpClient.GetAsync($"/list/{listId}/task{queryString}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tasksResponse = JsonSerializer.Deserialize<TasksResponse>(json, _jsonOptions);

            _logger.LogDebug("Retrieved {TaskCount} tasks from list {ListId}", 
                tasksResponse?.Tasks?.Count ?? 0, listId);

            return tasksResponse!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tasks for list {ListId}", listId);
            throw;
        }
    }

    public async Task<TasksResponse> GetTasksByTeamAsync(string teamId, TaskFilters? filters = null)
    {
        try
        {
            _logger.LogDebug("Getting tasks for team: {TeamId}", teamId);

            var queryString = BuildTaskFilterQuery(filters);
            var response = await _httpClient.GetAsync($"/team/{teamId}/task{queryString}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tasksResponse = JsonSerializer.Deserialize<TasksResponse>(json, _jsonOptions);

            _logger.LogDebug("Retrieved {TaskCount} tasks from team {TeamId}", 
                tasksResponse?.Tasks?.Count ?? 0, teamId);

            return tasksResponse!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tasks for team {TeamId}", teamId);
            throw;
        }
    }

    public async Task<TasksResponse> SearchTasksAsync(string query, TaskFilters? filters = null)
    {
        try
        {
            _logger.LogDebug("Searching tasks with query: {Query}", query);

            // Use team search with query parameter
            var queryString = BuildTaskFilterQuery(filters);
            if (!string.IsNullOrEmpty(queryString))
            {
                queryString += $"&search={Uri.EscapeDataString(query)}";
            }
            else
            {
                queryString = $"?search={Uri.EscapeDataString(query)}";
            }

            var response = await _httpClient.GetAsync($"/team/{_settings.TeamId}/task{queryString}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tasksResponse = JsonSerializer.Deserialize<TasksResponse>(json, _jsonOptions);

            _logger.LogDebug("Found {TaskCount} tasks matching query: {Query}", 
                tasksResponse?.Tasks?.Count ?? 0, query);

            return tasksResponse!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search tasks with query {Query}", query);
            throw;
        }
    }

    #endregion

    #region Lists Management

    public async Task<ClickUpList> CreateListAsync(string folderId, CreateListRequest request)
    {
        try
        {
            _logger.LogInformation("Creating list in folder {FolderId}: {ListName}", folderId, request.Name);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/folder/{folderId}/list", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<ClickUpList>(responseJson, _jsonOptions);

            _logger.LogInformation("List created successfully: {ListId}", list?.Id);
            return list!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create list in folder {FolderId}", folderId);
            throw;
        }
    }

    public async Task<ClickUpList> GetListAsync(string listId)
    {
        try
        {
            _logger.LogDebug("Getting list: {ListId}", listId);

            var response = await _httpClient.GetAsync($"/list/{listId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<ClickUpList>(json, _jsonOptions);

            return list!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get list {ListId}", listId);
            throw;
        }
    }

    public async Task<List<ClickUpList>> GetListsAsync(string folderId)
    {
        try
        {
            _logger.LogDebug("Getting lists for folder: {FolderId}", folderId);

            var response = await _httpClient.GetAsync($"/folder/{folderId}/list");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var listsResponse = JsonSerializer.Deserialize<ListsResponse>(json, _jsonOptions);

            return listsResponse?.Lists ?? new List<ClickUpList>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lists for folder {FolderId}", folderId);
            throw;
        }
    }

    #endregion

    #region Folders & Spaces

    public async Task<List<ClickUpSpace>> GetSpacesAsync(string teamId)
    {
        try
        {
            _logger.LogDebug("Getting spaces for team: {TeamId}", teamId);

            var response = await _httpClient.GetAsync($"/team/{teamId}/space");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var spacesResponse = JsonSerializer.Deserialize<SpacesResponse>(json, _jsonOptions);

            return spacesResponse?.Spaces ?? new List<ClickUpSpace>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get spaces for team {TeamId}", teamId);
            throw;
        }
    }

    public async Task<List<ClickUpFolder>> GetFoldersAsync(string spaceId)
    {
        try
        {
            _logger.LogDebug("Getting folders for space: {SpaceId}", spaceId);

            var response = await _httpClient.GetAsync($"/space/{spaceId}/folder");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var foldersResponse = JsonSerializer.Deserialize<FoldersResponse>(json, _jsonOptions);

            return foldersResponse?.Folders ?? new List<ClickUpFolder>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get folders for space {SpaceId}", spaceId);
            throw;
        }
    }

    #endregion

    #region Time Tracking

    public async Task<TimeEntry> CreateTimeEntryAsync(string taskId, CreateTimeEntryRequest request)
    {
        try
        {
            _logger.LogInformation("Creating time entry for task: {TaskId}", taskId);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/task/{taskId}/time", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var timeEntry = JsonSerializer.Deserialize<TimeEntry>(responseJson, _jsonOptions);

            _logger.LogInformation("Time entry created successfully: {EntryId}", timeEntry?.Id);
            return timeEntry!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create time entry for task {TaskId}", taskId);
            throw;
        }
    }

    public async Task<List<TimeEntry>> GetTimeEntriesAsync(string taskId)
    {
        try
        {
            _logger.LogDebug("Getting time entries for task: {TaskId}", taskId);

            var response = await _httpClient.GetAsync($"/task/{taskId}/time");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var timeEntries = JsonSerializer.Deserialize<List<TimeEntry>>(json, _jsonOptions);

            return timeEntries ?? new List<TimeEntry>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get time entries for task {TaskId}", taskId);
            throw;
        }
    }

    public async Task<TimeEntry> UpdateTimeEntryAsync(string entryId, UpdateTimeEntryRequest request)
    {
        try
        {
            _logger.LogInformation("Updating time entry: {EntryId}", entryId);

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/team/{_settings.TeamId}/time_entries/{entryId}", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var timeEntry = JsonSerializer.Deserialize<TimeEntry>(responseJson, _jsonOptions);

            _logger.LogInformation("Time entry updated successfully: {EntryId}", entryId);
            return timeEntry!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update time entry {EntryId}", entryId);
            throw;
        }
    }

    public async Task<bool> DeleteTimeEntryAsync(string entryId)
    {
        try
        {
            _logger.LogInformation("Deleting time entry: {EntryId}", entryId);

            var response = await _httpClient.DeleteAsync($"/team/{_settings.TeamId}/time_entries/{entryId}");
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Time entry deleted successfully: {EntryId}", entryId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete time entry {EntryId}", entryId);
            return false;
        }
    }

    #endregion

    #region Comments

    public async Task<ClickUpComment> CreateCommentAsync(string taskId, string commentText)
    {
        try
        {
            _logger.LogInformation("Creating comment for task: {TaskId}", taskId);

            var request = new { comment_text = commentText, notify_all = true };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/task/{taskId}/comment", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var comment = JsonSerializer.Deserialize<ClickUpComment>(responseJson, _jsonOptions);

            _logger.LogInformation("Comment created successfully: {CommentId}", comment?.Id);
            return comment!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create comment for task {TaskId}", taskId);
            throw;
        }
    }

    public async Task<List<ClickUpComment>> GetCommentsAsync(string taskId)
    {
        try
        {
            _logger.LogDebug("Getting comments for task: {TaskId}", taskId);

            var response = await _httpClient.GetAsync($"/task/{taskId}/comment");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var commentsResponse = JsonSerializer.Deserialize<Dictionary<string, List<ClickUpComment>>>(json, _jsonOptions);

            return commentsResponse?.GetValueOrDefault("comments") ?? new List<ClickUpComment>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get comments for task {TaskId}", taskId);
            throw;
        }
    }

    #endregion

    #region Teams & Users

    public async Task<List<ClickUpTeam>> GetTeamsAsync()
    {
        try
        {
            _logger.LogDebug("Getting teams");

            var response = await _httpClient.GetAsync("/team");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var teamsResponse = JsonSerializer.Deserialize<TeamsResponse>(json, _jsonOptions);

            return teamsResponse?.Teams ?? new List<ClickUpTeam>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get teams");
            throw;
        }
    }

    public async Task<List<ClickUpUser>> GetTeamMembersAsync(string teamId)
    {
        try
        {
            _logger.LogDebug("Getting members for team: {TeamId}", teamId);

            var response = await _httpClient.GetAsync($"/team/{teamId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var teamResponse = JsonSerializer.Deserialize<Dictionary<string, ClickUpTeam>>(json, _jsonOptions);
            var team = teamResponse?.GetValueOrDefault("team");

            return team?.Members ?? new List<ClickUpUser>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get members for team {TeamId}", teamId);
            throw;
        }
    }

    #endregion

    #region Status Management

    public async Task<List<ClickUpStatus>> GetStatusesAsync(string listId)
    {
        try
        {
            _logger.LogDebug("Getting statuses for list: {ListId}", listId);

            var response = await _httpClient.GetAsync($"/list/{listId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var listResponse = JsonSerializer.Deserialize<ClickUpList>(json, _jsonOptions);

            // Get statuses from space if list doesn't override statuses
            if (!listResponse!.OverrideStatuses)
            {
                var spaceResponse = await _httpClient.GetAsync($"/space/{listResponse.Space.Id}");
                spaceResponse.EnsureSuccessStatusCode();

                var spaceJson = await spaceResponse.Content.ReadAsStringAsync();
                var space = JsonSerializer.Deserialize<ClickUpSpace>(spaceJson, _jsonOptions);

                return space?.Statuses ?? new List<ClickUpStatus>();
            }

            // For now, return empty list if list has custom statuses
            // Would need additional API call to get list-specific statuses
            return new List<ClickUpStatus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statuses for list {ListId}", listId);
            throw;
        }
    }

    public async Task<ClickUpTask> UpdateTaskStatusAsync(string taskId, string statusId)
    {
        try
        {
            _logger.LogInformation("Updating task status: {TaskId} to {StatusId}", taskId, statusId);

            var request = new { status = statusId };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/task/{taskId}", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var task = JsonSerializer.Deserialize<ClickUpTask>(responseJson, _jsonOptions);

            _logger.LogInformation("Task status updated successfully: {TaskId}", taskId);
            return task!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update task status {TaskId} to {StatusId}", taskId, statusId);
            throw;
        }
    }

    #endregion

    #region Private Helper Methods

    private string BuildTaskFilterQuery(TaskFilters? filters)
    {
        if (filters == null) return string.Empty;

        var queryParams = new List<string>();

        if (filters.Archived.HasValue)
            queryParams.Add($"archived={filters.Archived.Value.ToString().ToLower()}");

        if (filters.Page.HasValue)
            queryParams.Add($"page={filters.Page.Value}");

        if (!string.IsNullOrEmpty(filters.OrderBy))
            queryParams.Add($"order_by={Uri.EscapeDataString(filters.OrderBy)}");

        if (filters.Reverse.HasValue)
            queryParams.Add($"reverse={filters.Reverse.Value.ToString().ToLower()}");

        if (filters.Subtasks.HasValue)
            queryParams.Add($"subtasks={filters.Subtasks.Value.ToString().ToLower()}");

        if (filters.Statuses?.Any() == true)
            queryParams.AddRange(filters.Statuses.Select(s => $"statuses[]={Uri.EscapeDataString(s)}"));

        if (filters.IncludeClosed.HasValue)
            queryParams.Add($"include_closed={filters.IncludeClosed.Value.ToString().ToLower()}");

        if (filters.Assignees?.Any() == true)
            queryParams.AddRange(filters.Assignees.Select(a => $"assignees[]={a}"));

        if (filters.Tags?.Any() == true)
            queryParams.AddRange(filters.Tags.Select(t => $"tags[]={Uri.EscapeDataString(t)}"));

        if (filters.DueDateGt.HasValue)
            queryParams.Add($"due_date_gt={filters.DueDateGt.Value}");

        if (filters.DueDateLt.HasValue)
            queryParams.Add($"due_date_lt={filters.DueDateLt.Value}");

        if (filters.DateCreatedGt.HasValue)
            queryParams.Add($"date_created_gt={filters.DateCreatedGt.Value}");

        if (filters.DateCreatedLt.HasValue)
            queryParams.Add($"date_created_lt={filters.DateCreatedLt.Value}");

        if (filters.DateUpdatedGt.HasValue)
            queryParams.Add($"date_updated_gt={filters.DateUpdatedGt.Value}");

        if (filters.DateUpdatedLt.HasValue)
            queryParams.Add($"date_updated_lt={filters.DateUpdatedLt.Value}");

        if (!string.IsNullOrEmpty(filters.CustomFields))
            queryParams.Add($"custom_fields={Uri.EscapeDataString(filters.CustomFields)}");

        if (filters.CustomItems?.Any() == true)
            queryParams.AddRange(filters.CustomItems.Select(ci => $"custom_items[]={Uri.EscapeDataString(ci)}"));

        return queryParams.Any() ? "?" + string.Join("&", queryParams) : string.Empty;
    }

    #endregion
}