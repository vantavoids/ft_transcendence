import { Controller, Get } from '@nestjs/common';

@Controller('v1')
export class V1Controller {
  @Get('hello-world')
  helloWorld(): string {
    return 'tu compiles hein';
  }
}
