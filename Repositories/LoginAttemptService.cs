using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RouteCardProcess.Model.DTOs.Login;

public class LoginAttemptService
{
    private readonly ConcurrentDictionary<string, LoginAttemptInfo> _attempts = new();

    private readonly int _maxAttempts;
    private readonly TimeSpan _lockoutDuration;

    public LoginAttemptService(IOptions<LoginAttemptSettings> settings)
    {
        _maxAttempts = settings.Value.MaxAttempts;
        _lockoutDuration = TimeSpan.FromMinutes(settings.Value.LockoutDurationInMinutes);
    }

    public LoginAttemptResult CheckAttempt(string key)
    {
        key = key.Trim().ToLower();

        var info = _attempts.GetOrAdd(key, new LoginAttemptInfo());

        if (info.LockoutEndTime.HasValue && info.LockoutEndTime > DateTime.UtcNow)
        {
            return new LoginAttemptResult
            {
                IsLocked = true,
                RemainingLockout = info.LockoutEndTime.Value - DateTime.UtcNow
            };
        }

        return new LoginAttemptResult { IsLocked = false };
    }

    public void RegisterFailedAttempt(string key)
    {
        key = key.Trim().ToLower();

        var info = _attempts.GetOrAdd(key, new LoginAttemptInfo());

        if (info.LockoutEndTime.HasValue && info.LockoutEndTime > DateTime.UtcNow)
            return;

        info.AttemptCount++;

        if (info.AttemptCount >= _maxAttempts)
        {
            info.LockoutEndTime = DateTime.UtcNow.Add(_lockoutDuration);
        }
    }

    public void ResetAttempts(string key)
    {
        key = key.Trim().ToLower();

        _attempts[key] = new LoginAttemptInfo
        {
            AttemptCount = 0,
            LockoutEndTime = null
        };
    }
}

public class LoginAttemptInfo
{
    public int AttemptCount { get; set; }
    public DateTime? LockoutEndTime { get; set; }
}

public class LoginAttemptResult
{
    public bool IsLocked { get; set; }
    public TimeSpan? RemainingLockout { get; set; }
}
