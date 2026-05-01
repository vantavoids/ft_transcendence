import { Controller, Get, Patch, Param, Query, Request } from '@nestjs/common';

@Controller()
export class NotificationController {

	@Get('hello-world')
		helloworld() : string {
			return 'Hello World !';
		}
}
