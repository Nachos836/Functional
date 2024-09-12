using System;
using System.Diagnostics.Contracts;

namespace Functional.Core.Outcome
{
    public static class Unexpected
    {
        [Pure] public static Exception Error { get; } = new ("Execution is Aborted by Exception");
        [Pure] public static Exception Impossible { get; } = new ("Execution shouldn't have happened");
    }
}
