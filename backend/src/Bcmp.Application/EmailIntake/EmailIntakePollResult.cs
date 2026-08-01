namespace Bcmp.Application.EmailIntake;

public sealed record EmailIntakePollResult(
    int Fetched,
    int Created,
    int DuplicatesSkipped,
    int Failed);
