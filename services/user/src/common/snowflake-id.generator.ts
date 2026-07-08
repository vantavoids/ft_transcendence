import { Injectable } from '@nestjs/common';

@Injectable()
export class SnowflakeIdGenerator {
  private static readonly workerIdBits = 10n;
  private static readonly sequenceBits = 12n;
  private static readonly timestampLeftShift =
    SnowflakeIdGenerator.workerIdBits + SnowflakeIdGenerator.sequenceBits;
  private static readonly maxWorkerId =
    (1n << SnowflakeIdGenerator.workerIdBits) - 1n;
  private static readonly maxSequence =
    (1n << SnowflakeIdGenerator.sequenceBits) - 1n;

  private readonly workerId: bigint;
  private readonly epoch: bigint;
  private sequence = 0n;
  private lastTimestamp = -1n;

  constructor() {
    const workerIdRaw = process.env.SNOWFLAKE_WORKER_ID ?? '1';
    const epochRaw = process.env.SNOWFLAKE_EPOCH ?? '1704067200000';
    const workerId = BigInt(workerIdRaw);
    const epoch = BigInt(epochRaw);

    if (workerId < 0n || workerId > SnowflakeIdGenerator.maxWorkerId) {
      throw new Error(
        `SNOWFLAKE_WORKER_ID must be between 0 and ${SnowflakeIdGenerator.maxWorkerId.toString()}`,
      );
    }

    this.workerId = workerId;
    this.epoch = epoch;
  }

  nextId(): string {
    const timestamp = this.currentTimestamp();

    if (timestamp < this.lastTimestamp) {
      throw new Error('Clock moved backwards while generating a snowflake id');
    }

    let nextTimestamp = timestamp;
    if (timestamp === this.lastTimestamp) {
      this.sequence = (this.sequence + 1n) & SnowflakeIdGenerator.maxSequence;
      if (this.sequence === 0n) {
        nextTimestamp = this.waitUntilNextMillisecond(this.lastTimestamp);
      }
    } else {
      this.sequence = 0n;
    }

    this.lastTimestamp = nextTimestamp;

    const id =
      ((nextTimestamp - this.epoch) << SnowflakeIdGenerator.timestampLeftShift) |
      (this.workerId << SnowflakeIdGenerator.sequenceBits) |
      this.sequence;

    return id.toString();
  }

  private currentTimestamp(): bigint {
    return BigInt(Date.now());
  }

  private waitUntilNextMillisecond(lastTimestamp: bigint): bigint {
    let timestamp = this.currentTimestamp();
    while (timestamp <= lastTimestamp) {
      timestamp = this.currentTimestamp();
    }

    return timestamp;
  }
}
