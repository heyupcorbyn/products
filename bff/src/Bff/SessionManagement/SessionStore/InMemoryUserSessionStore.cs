// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Collections.Concurrent;

namespace Duende.Bff.SessionManagement.SessionStore;

/// <summary>
/// In-memory user session store
/// </summary>
internal class InMemoryUserSessionStore : IUserSessionStore
{
    private readonly ConcurrentDictionary<string, UserSession> _store = new();

    /// <inheritdoc />
    public Task CreateUserSessionAsync(UserSession session, CT ct = default)
    {
        if (!_store.TryAdd(session.Key, session.Clone()))
        {
            throw new InvalidOperationException("Key already exists");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<UserSession?> GetUserSessionAsync(string key, CT ct = default)
    {
        _store.TryGetValue(key, out var item);
        return Task.FromResult(item?.Clone());
    }

    /// <inheritdoc />
    public Task UpdateUserSessionAsync(string key, UserSessionUpdate session, CT ct = default)
    {
        var item = _store[key].Clone();
        session.CopyTo(item);
        _store[key] = item;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteUserSessionAsync(string key, CT ct = default)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<UserSession>> GetUserSessionsAsync(UserSessionsFilter filter, CT ct = default)
    {
        filter.Validate();

        var query = _store.Values.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.SubjectId))
        {
            query = query.Where(x => x.SubjectId == filter.SubjectId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x => x.SessionId == filter.SessionId);
        }

        var results = query.Select(x => x.Clone()).ToArray();
        return Task.FromResult((IReadOnlyCollection<UserSession>)results);
    }

    /// <inheritdoc />
    public Task DeleteUserSessionsAsync(UserSessionsFilter filter, CT ct = default)
    {
        filter.Validate();

        var query = _store.Values.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.SubjectId))
        {
            query = query.Where(x => x.SubjectId == filter.SubjectId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SessionId))
        {
            query = query.Where(x => x.SessionId == filter.SessionId);
        }

        var keys = query.Select(x => x.Key).ToArray();

        foreach (var key in keys)
        {
            _store.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
