using Auth.Application.Abstractions;
using Auth.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure;

internal sealed class SnowflakeIdGenerator : IIdGenerator
{
    private const int        WorkerIdBits = 10;
    private const int        SequenceBits = 12;
    private const int        TimestampLeftShift = SequenceBits + WorkerIdBits;
    private const long       MaxWorkerId = (1L << WorkerIdBits) - 1;
    private const long       MaxSequence = (1L << SequenceBits) - 1;

    private readonly long   _workerId;
    private readonly long   _epoch;
    private long            _sequence = 0;
    private long            _lastTimestamp = -1;
    private readonly IClock _clock;
    private readonly Lock   _lock = new();

    public SnowflakeIdGenerator(IClock clock, IOptions<SnowFlakeOptions> options)
    {
        var snowflakeOptions = options.Value;
        
        _workerId = snowflakeOptions.WorkerId;
        _epoch = snowflakeOptions.Epoch;
        _clock = clock;

        if (_workerId < 0 || _workerId > MaxWorkerId)
            throw new ArgumentException(
                $"Worker ID must be between 0 and {MaxWorkerId}, but got {_workerId}",
                nameof(snowflakeOptions.WorkerId)
            );
    }

    public long NextId()
    {
        lock (_lock)
        {
            long timestamp = GetCurrentTimestamp();

            if (timestamp < _lastTimestamp)
                throw new InvalidOperationException(
                    "Clock moved backwards. Refusing to generate ID for timestamp " +
                    $"{timestamp} ms. Last known timestamp was {_lastTimestamp} ms."
                );

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0) // ? Sequence overflow
                    timestamp = WaitUntilNextMillisecond(_lastTimestamp);
            }
            else
                _sequence = 0;

            _lastTimestamp = timestamp;

            return ((timestamp - _epoch) << TimestampLeftShift)
                | (_workerId << SequenceBits)
                | _sequence;
        }
    }

    private long GetCurrentTimestamp() => _clock.UtcNow.ToUnixTimeMilliseconds();

    private long WaitUntilNextMillisecond(long lastTimestamp)
    {
        long timestamp = GetCurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            timestamp = GetCurrentTimestamp();
        }
        return timestamp;
    }
}
