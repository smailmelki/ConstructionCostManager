using ConstructionCostManager.Core.Entities;
using ConstructionCostManager.Data.Services;

namespace ConstructionCostManager.Web.Services;

public class ActiveProjectState
{
    public int? ActiveProjectId { get; private set; }
    public Project? ActiveProject { get; private set; }
    public bool IsDarkMode { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync(IConstructionCostService service)
    {
        if (ActiveProjectId.HasValue)
        {
            ActiveProject = await service.GetProjectByIdAsync(ActiveProjectId.Value);
            if (ActiveProject is not null) return;
        }

        var projects = await service.GetProjectsAsync();
        if (projects.Count > 0)
        {
            ActiveProject = projects[0];
            ActiveProjectId = projects[0].Id;
        }
        else
        {
            ActiveProject = null;
            ActiveProjectId = null;
        }
    }

    public async Task SetActiveProjectAsync(IConstructionCostService service, int projectId)
    {
        ActiveProjectId = projectId;
        ActiveProject = await service.GetProjectByIdAsync(projectId);
        OnChange?.Invoke();
    }

    public void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        OnChange?.Invoke();
    }

    public void NotifyChanged() => OnChange?.Invoke();
}
