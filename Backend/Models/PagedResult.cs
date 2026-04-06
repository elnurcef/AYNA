namespace Backend.Models;

public sealed record PagedResult<T>(int Total, IReadOnlyList<T> Items);
