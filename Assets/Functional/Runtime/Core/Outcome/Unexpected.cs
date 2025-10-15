using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Functional.Core.Outcome
{
    public static class Unexpected
    {
        [Pure] public static Exception Error { get; } = new ("Execution is Aborted by Exception");
        [Pure] public static Exception Impossible { get; } = new UnreachableException ("Execution shouldn't have happened");
    }
}
