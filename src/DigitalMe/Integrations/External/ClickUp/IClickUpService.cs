using DigitalMe.Integrations.External.ClickUp.Models;

namespace DigitalMe.Integrations.External.ClickUp;

/// <summary>
/// Interface for ClickUp API integration service.
/// Provides task management, time tracking, and project organization capabilities.
/// </summary>
public interface IClickUpService
{
    // Task Management
    Task<ClickUpTask> CreateTaskAsync(string listId, CreateTaskRequest request);
    Task<ClickUpTask> GetTaskAsync(string taskId);
    Task<ClickUpTask> UpdateTaskAsync(string taskId, UpdateTaskRequest request);
    Task<bool> DeleteTaskAsync(string taskId);

    // Task Search & Filtering
    Task<TasksResponse> GetTasksAsync(string listId, TaskFilters? filters = null);
    Task<TasksResponse> GetTasksByTeamAsync(string teamId, TaskFilters? filters = null);
    Task<TasksResponse> SearchTasksAsync(string query, TaskFilters? filters = null);

    // Lists Management
    Task<ClickUpList> CreateListAsync(string folderId, CreateListRequest request);
    Task<ClickUpList> GetListAsync(string listId);
    Task<List<ClickUpList>> GetListsAsync(string folderId);

    // Folders & Spaces
    Task<List<ClickUpSpace>> GetSpacesAsync(string teamId);
    Task<List<ClickUpFolder>> GetFoldersAsync(string spaceId);

    // Time Tracking
    Task<TimeEntry> CreateTimeEntryAsync(string taskId, CreateTimeEntryRequest request);
    Task<List<TimeEntry>> GetTimeEntriesAsync(string taskId);
    Task<TimeEntry> UpdateTimeEntryAsync(string entryId, UpdateTimeEntryRequest request);
    Task<bool> DeleteTimeEntryAsync(string entryId);

    // Comments
    Task<ClickUpComment> CreateCommentAsync(string taskId, string commentText);
    Task<List<ClickUpComment>> GetCommentsAsync(string taskId);

    // Teams & Users
    Task<List<ClickUpTeam>> GetTeamsAsync();
    Task<List<ClickUpUser>> GetTeamMembersAsync(string teamId);

    // Status Management
    Task<List<ClickUpStatus>> GetStatusesAsync(string listId);
    Task<ClickUpTask> UpdateTaskStatusAsync(string taskId, string statusId);
}
