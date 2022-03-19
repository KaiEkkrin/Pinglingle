using Pinglingle.Shared.Model;
using Plotly.Blazor;
using Plotly.Blazor.LayoutLib;
using Plotly.Blazor.Traces;
using Plotly.Blazor.Traces.ScatterLib;

namespace Pinglingle.Client.ViewModels;

/// <summary>
/// A viewmodel for a Plotly chart of a collection of digests.
/// </summary>
public class DigestsViewModel
{
    public DigestsViewModel(string address, IReadOnlyCollection<Digest> digests)
    {
        Layout = new Layout
        {
            Title = new Title
            {
                Text = address
            },
            XAxis = new List<XAxis>
            {
                new XAxis
                {
                    Title = new Plotly.Blazor.LayoutLib.XAxisLib.Title
                    {
                        Text = "Hour"
                    }
                }
            },
            YAxis = new List<YAxis>
            {
                new YAxis
                {
                    Title = new Plotly.Blazor.LayoutLib.YAxisLib.Title
                    {
                        Text = "Response Time (ms)"
                    }
                }
            },
            Height = 200
        };

        var earliestStartTime = digests.Min(d => d.StartTime);
        Data = new List<ITrace>
        {
            new Scatter
            {
                Name = "50th %ile Response Time",
                Mode = ModeFlag.Lines | ModeFlag.Markers,
                X = digests.Select(d => (object)(d.StartTime - earliestStartTime).TotalHours)
                    .ToList(),
                Y = digests.Select(d => (object)d.Percentile50).ToList()
            }
        };
    }

    public PlotlyChart Chart { get; } = new();
    public Config Config { get; } = new();
    public Layout Layout { get; }

    public IList<ITrace> Data { get; }
}