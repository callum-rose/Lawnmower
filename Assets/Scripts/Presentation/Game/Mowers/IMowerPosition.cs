using System;
using System.Collections.Generic;
using Game.Core;
using Lawnmower.Model;

namespace Game.Mowers
{
    public interface IMowerPosition
    {
        IListenableProperty<GridVector> CurrentPosition { get; }

        /// <summary>
        /// The path segment behind the latest change of <see cref="CurrentPosition"/>, read while it is raised: how the
        /// mower travels (step, spring jump or burrow). Null for snaps (start placement, undo).
        /// </summary>
        PathSegment? CurrentSegment { get; }

        /// <summary>
        /// The mower went down a rabbit hole whose exit is blocked and came back. The position didn't change; the
        /// segments are the path it tried, ending with the blocked burrow.
        /// </summary>
        event Action<IReadOnlyList<PathSegment>> Bounced;
    }
}
