using CloudAwesome.PipeLine.Contracts;

namespace CloudAwesome.PipeLine.Providers;

public interface IPipelineDashboardProvider
{
    PipelineProvider Provider { get; }

    Task<DashboardData> GetDashboardDataAsync(CancellationToken cancellationToken);
}
