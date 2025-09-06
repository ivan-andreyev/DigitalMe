using System.Text.Json.Serialization;

namespace DigitalMe.Integrations.External.ClickUp.Models;

#region Core Entities

/// <summary>
/// ClickUp Task entity with all properties from ClickUp API.
/// </summary>
public class ClickUpTask
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("status")]
    public ClickUpTaskStatus Status { get; set; } = new();
    
    [JsonPropertyName("orderindex")]
    public string OrderIndex { get; set; } = string.Empty;
    
    [JsonPropertyName("date_created")]
    public string DateCreated { get; set; } = string.Empty;
    
    [JsonPropertyName("date_updated")]
    public string DateUpdated { get; set; } = string.Empty;
    
    [JsonPropertyName("date_closed")]
    public string? DateClosed { get; set; }
    
    [JsonPropertyName("creator")]
    public ClickUpUser Creator { get; set; } = new();
    
    [JsonPropertyName("assignees")]
    public List<ClickUpUser> Assignees { get; set; } = new();
    
    [JsonPropertyName("group_assignees")]
    public List<object> GroupAssignees { get; set; } = new();
    
    [JsonPropertyName("watchers")]
    public List<ClickUpUser> Watchers { get; set; } = new();
    
    [JsonPropertyName("checklists")]
    public List<ClickUpChecklist> Checklists { get; set; } = new();
    
    [JsonPropertyName("tags")]
    public List<ClickUpTag> Tags { get; set; } = new();
    
    [JsonPropertyName("parent")]
    public string? Parent { get; set; }
    
    [JsonPropertyName("priority")]
    public ClickUpPriority? Priority { get; set; }
    
    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }
    
    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }
    
    [JsonPropertyName("points")]
    public int? Points { get; set; }
    
    [JsonPropertyName("time_estimate")]
    public long? TimeEstimate { get; set; }
    
    [JsonPropertyName("time_spent")]
    public long? TimeSpent { get; set; }
    
    [JsonPropertyName("custom_fields")]
    public List<ClickUpCustomField> CustomFields { get; set; } = new();
    
    [JsonPropertyName("list")]
    public ClickUpListBasic List { get; set; } = new();
    
    [JsonPropertyName("folder")]
    public ClickUpFolderBasic Folder { get; set; } = new();
    
    [JsonPropertyName("space")]
    public ClickUpSpaceBasic Space { get; set; } = new();
    
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// ClickUp List entity.
/// </summary>
public class ClickUpList
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("orderindex")]
    public int OrderIndex { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("priority")]
    public ClickUpPriority? Priority { get; set; }
    
    [JsonPropertyName("assignee")]
    public ClickUpUser? Assignee { get; set; }
    
    [JsonPropertyName("task_count")]
    public int TaskCount { get; set; }
    
    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }
    
    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }
    
    [JsonPropertyName("folder")]
    public ClickUpFolderBasic Folder { get; set; } = new();
    
    [JsonPropertyName("space")]
    public ClickUpSpaceBasic Space { get; set; } = new();
    
    [JsonPropertyName("archived")]
    public bool Archived { get; set; }
    
    [JsonPropertyName("override_statuses")]
    public bool OverrideStatuses { get; set; }
    
    [JsonPropertyName("permission_level")]
    public string PermissionLevel { get; set; } = string.Empty;
}

/// <summary>
/// ClickUp Space entity.
/// </summary>
public class ClickUpSpace
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("private")]
    public bool Private { get; set; }
    
    [JsonPropertyName("statuses")]
    public List<ClickUpStatus> Statuses { get; set; } = new();
    
    [JsonPropertyName("multiple_assignees")]
    public bool MultipleAssignees { get; set; }
    
    [JsonPropertyName("features")]
    public ClickUpSpaceFeatures Features { get; set; } = new();
}

/// <summary>
/// ClickUp Folder entity.
/// </summary>
public class ClickUpFolder
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("orderindex")]
    public int OrderIndex { get; set; }
    
    [JsonPropertyName("override_statuses")]
    public bool OverrideStatuses { get; set; }
    
    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; }
    
    [JsonPropertyName("space")]
    public ClickUpSpaceBasic Space { get; set; } = new();
    
    [JsonPropertyName("task_count")]
    public string TaskCount { get; set; } = string.Empty;
    
    [JsonPropertyName("lists")]
    public List<ClickUpList> Lists { get; set; } = new();
}

#endregion

#region Supporting Entities

/// <summary>
/// ClickUp User entity.
/// </summary>
public class ClickUpUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("profilePicture")]
    public string? ProfilePicture { get; set; }
    
    [JsonPropertyName("initials")]
    public string Initials { get; set; } = string.Empty;
}

/// <summary>
/// ClickUp Team entity.
/// </summary>
public class ClickUpTeam
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
    
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
    
    [JsonPropertyName("members")]
    public List<ClickUpUser> Members { get; set; } = new();
}

/// <summary>
/// ClickUp Status entity.
/// </summary>
public class ClickUpStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
    
    [JsonPropertyName("orderindex")]
    public int OrderIndex { get; set; }
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// ClickUp Task Status for task entities.
/// </summary>
public class ClickUpTaskStatus
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("orderindex")]
    public int OrderIndex { get; set; }
}

/// <summary>
/// ClickUp Priority entity.
/// </summary>
public class ClickUpPriority
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;
    
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
    
    [JsonPropertyName("orderindex")]
    public string OrderIndex { get; set; } = string.Empty;
}

/// <summary>
/// ClickUp Tag entity.
/// </summary>
public class ClickUpTag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("tag_fg")]
    public string TagFg { get; set; } = string.Empty;
    
    [JsonPropertyName("tag_bg")]
    public string TagBg { get; set; } = string.Empty;
    
    [JsonPropertyName("creator")]
    public int Creator { get; set; }
}

/// <summary>
/// ClickUp Checklist entity.
/// </summary>
public class ClickUpChecklist
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("orderindex")]
    public int OrderIndex { get; set; }
    
    [JsonPropertyName("creator")]
    public int Creator { get; set; }
    
    [JsonPropertyName("resolved")]
    public int Resolved { get; set; }
    
    [JsonPropertyName("unresolved")]
    public int Unresolved { get; set; }
    
    [JsonPropertyName("items")]
    public List<ClickUpChecklistItem> Items { get; set; } = new();
}

/// <summary>
/// ClickUp Checklist Item entity.
/// </summary>
public class ClickUpChecklistItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("orderindex")]
    public int OrderIndex { get; set; }
    
    [JsonPropertyName("assignee")]
    public ClickUpUser? Assignee { get; set; }
    
    [JsonPropertyName("resolved")]
    public bool Resolved { get; set; }
    
    [JsonPropertyName("date_created")]
    public string DateCreated { get; set; } = string.Empty;
}

/// <summary>
/// ClickUp Custom Field entity.
/// </summary>
public class ClickUpCustomField
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public object? Value { get; set; }
    
    [JsonPropertyName("required")]
    public bool Required { get; set; }
}

/// <summary>
/// ClickUp Comment entity.
/// </summary>
public class ClickUpComment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("comment_text")]
    public string CommentText { get; set; } = string.Empty;
    
    [JsonPropertyName("user")]
    public ClickUpUser User { get; set; } = new();
    
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;
    
    [JsonPropertyName("parent")]
    public string? Parent { get; set; }
}

#endregion

#region Time Tracking

/// <summary>
/// ClickUp Time Entry entity.
/// </summary>
public class TimeEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("task")]
    public ClickUpTaskBasic Task { get; set; } = new();
    
    [JsonPropertyName("wid")]
    public string Wid { get; set; } = string.Empty;
    
    [JsonPropertyName("user")]
    public ClickUpUser User { get; set; } = new();
    
    [JsonPropertyName("billable")]
    public bool Billable { get; set; }
    
    [JsonPropertyName("start")]
    public string Start { get; set; } = string.Empty;
    
    [JsonPropertyName("end")]
    public string End { get; set; } = string.Empty;
    
    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;
    
    [JsonPropertyName("at")]
    public string At { get; set; } = string.Empty;
}

#endregion

#region Basic Entities (for references)

public class ClickUpListBasic
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("access")]
    public bool Access { get; set; }
}

public class ClickUpFolderBasic
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; }
    
    [JsonPropertyName("access")]
    public bool Access { get; set; }
}

public class ClickUpSpaceBasic
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("access")]
    public bool Access { get; set; }
}

public class ClickUpTaskBasic
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class ClickUpSpaceFeatures
{
    [JsonPropertyName("due_dates")]
    public ClickUpFeatureConfig DueDates { get; set; } = new();
    
    [JsonPropertyName("start_date")]
    public ClickUpFeatureConfig StartDate { get; set; } = new();
    
    [JsonPropertyName("time_tracking")]
    public ClickUpFeatureConfig TimeTracking { get; set; } = new();
    
    [JsonPropertyName("tags")]
    public ClickUpFeatureConfig Tags { get; set; } = new();
    
    [JsonPropertyName("time_estimates")]
    public ClickUpFeatureConfig TimeEstimates { get; set; } = new();
    
    [JsonPropertyName("checklists")]
    public ClickUpFeatureConfig Checklists { get; set; } = new();
    
    [JsonPropertyName("custom_fields")]
    public ClickUpFeatureConfig CustomFields { get; set; } = new();
    
    [JsonPropertyName("remap_dependencies")]
    public ClickUpFeatureConfig RemapDependencies { get; set; } = new();
    
    [JsonPropertyName("dependency_warning")]
    public ClickUpFeatureConfig DependencyWarning { get; set; } = new();
    
    [JsonPropertyName("portfolios")]
    public ClickUpFeatureConfig Portfolios { get; set; } = new();
}

public class ClickUpFeatureConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

#endregion

#region Request DTOs

/// <summary>
/// Request for creating a new task.
/// </summary>
public class CreateTaskRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("assignees")]
    public List<int>? Assignees { get; set; }
    
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }
    
    [JsonPropertyName("due_date")]
    public long? DueDate { get; set; }
    
    [JsonPropertyName("due_date_time")]
    public bool? DueDateTime { get; set; }
    
    [JsonPropertyName("time_estimate")]
    public long? TimeEstimate { get; set; }
    
    [JsonPropertyName("start_date")]
    public long? StartDate { get; set; }
    
    [JsonPropertyName("start_date_time")]
    public bool? StartDateTime { get; set; }
    
    [JsonPropertyName("notify_all")]
    public bool? NotifyAll { get; set; }
    
    [JsonPropertyName("parent")]
    public string? Parent { get; set; }
    
    [JsonPropertyName("links_to")]
    public string? LinksTo { get; set; }
    
    [JsonPropertyName("check_required_custom_fields")]
    public bool? CheckRequiredCustomFields { get; set; }
    
    [JsonPropertyName("custom_fields")]
    public List<CustomFieldValue>? CustomFields { get; set; }
}

/// <summary>
/// Request for updating an existing task.
/// </summary>
public class UpdateTaskRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }
    
    [JsonPropertyName("due_date")]
    public long? DueDate { get; set; }
    
    [JsonPropertyName("due_date_time")]
    public bool? DueDateTime { get; set; }
    
    [JsonPropertyName("parent")]
    public string? Parent { get; set; }
    
    [JsonPropertyName("time_estimate")]
    public long? TimeEstimate { get; set; }
    
    [JsonPropertyName("start_date")]
    public long? StartDate { get; set; }
    
    [JsonPropertyName("start_date_time")]
    public bool? StartDateTime { get; set; }
    
    [JsonPropertyName("assignees")]
    public UpdateAssigneesRequest? Assignees { get; set; }
    
    [JsonPropertyName("archived")]
    public bool? Archived { get; set; }
}

/// <summary>
/// Request for updating task assignees.
/// </summary>
public class UpdateAssigneesRequest
{
    [JsonPropertyName("add")]
    public List<int>? Add { get; set; }
    
    [JsonPropertyName("rem")]
    public List<int>? Remove { get; set; }
}

/// <summary>
/// Request for creating a new list.
/// </summary>
public class CreateListRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string? Content { get; set; }
    
    [JsonPropertyName("due_date")]
    public long? DueDate { get; set; }
    
    [JsonPropertyName("due_date_time")]
    public bool? DueDateTime { get; set; }
    
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }
    
    [JsonPropertyName("assignee")]
    public int? Assignee { get; set; }
    
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Request for creating a time entry.
/// </summary>
public class CreateTimeEntryRequest
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
    
    [JsonPropertyName("start")]
    public long Start { get; set; }
    
    [JsonPropertyName("billable")]
    public bool? Billable { get; set; }
    
    [JsonPropertyName("duration")]
    public long Duration { get; set; }
    
    [JsonPropertyName("assignee")]
    public int? Assignee { get; set; }
    
    [JsonPropertyName("tid")]
    public string? Tid { get; set; }
}

/// <summary>
/// Request for updating a time entry.
/// </summary>
public class UpdateTimeEntryRequest
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
    
    [JsonPropertyName("start")]
    public long? Start { get; set; }
    
    [JsonPropertyName("billable")]
    public bool? Billable { get; set; }
    
    [JsonPropertyName("duration")]
    public long? Duration { get; set; }
    
    [JsonPropertyName("end")]
    public long? End { get; set; }
}

/// <summary>
/// Custom field value for task creation/update.
/// </summary>
public class CustomFieldValue
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

#endregion

#region Response DTOs

/// <summary>
/// Response containing multiple tasks.
/// </summary>
public class TasksResponse
{
    [JsonPropertyName("tasks")]
    public List<ClickUpTask> Tasks { get; set; } = new();
    
    [JsonPropertyName("last_page")]
    public bool? LastPage { get; set; }
}

/// <summary>
/// Response containing multiple lists.
/// </summary>
public class ListsResponse
{
    [JsonPropertyName("lists")]
    public List<ClickUpList> Lists { get; set; } = new();
}

/// <summary>
/// Response containing multiple spaces.
/// </summary>
public class SpacesResponse
{
    [JsonPropertyName("spaces")]
    public List<ClickUpSpace> Spaces { get; set; } = new();
}

/// <summary>
/// Response containing multiple folders.
/// </summary>
public class FoldersResponse
{
    [JsonPropertyName("folders")]
    public List<ClickUpFolder> Folders { get; set; } = new();
}

/// <summary>
/// Response containing multiple teams.
/// </summary>
public class TeamsResponse
{
    [JsonPropertyName("teams")]
    public List<ClickUpTeam> Teams { get; set; } = new();
}

#endregion

#region Filter DTOs

/// <summary>
/// Filters for task queries.
/// </summary>
public class TaskFilters
{
    public bool? Archived { get; set; }
    public int? Page { get; set; }
    public string? OrderBy { get; set; }
    public bool? Reverse { get; set; }
    public bool? Subtasks { get; set; }
    public List<string>? Statuses { get; set; }
    public bool? IncludeClosed { get; set; }
    public List<int>? Assignees { get; set; }
    public List<string>? Tags { get; set; }
    public long? DueDateGt { get; set; }
    public long? DueDateLt { get; set; }
    public long? DateCreatedGt { get; set; }
    public long? DateCreatedLt { get; set; }
    public long? DateUpdatedGt { get; set; }
    public long? DateUpdatedLt { get; set; }
    public string? CustomFields { get; set; }
    public List<string>? CustomItems { get; set; }
}

#endregion