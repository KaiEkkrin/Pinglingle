using Pinglingle.Shared.Model;

namespace Pinglingle.Server;

internal sealed class TargetEventArgs : EventArgs
{
    public TargetEventArgs(Target target)
    {
        Target = target;
    }

    public Target Target { get; }
}