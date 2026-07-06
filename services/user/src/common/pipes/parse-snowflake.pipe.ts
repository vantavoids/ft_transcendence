import { BadRequestException, Injectable, PipeTransform } from '@nestjs/common';

@Injectable()
export class ParseSnowflakePipe implements PipeTransform<string, string> {
  transform(value: string): string {
    if (!/^\d+$/.test(value)) {
      throw new BadRequestException('snowflake must be a positive integer');
    }

    return value;
  }
}
