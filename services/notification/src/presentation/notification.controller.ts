import { Controller, Get, Patch, Param, Query, Request } from '@nestjs/common';
import { NotificationService } from "src/application/notification.service";
import { NotificationEntity } from "src/domain/notification.entity";

@Controller('notification')
export class NotificationController {
	constructor(private readonly notificationService: NotificationService) {}

	@Get()
		helloworld() : string {
			return 'Hello World !';
		}

	@Get()
		getNotif(@Request() req, @Query('read') read?: boolean, @Query('limit') limit?: number) {
			const uid = req.user.sub;
			return this.notificationService.get(uid);
		}

	@Patch(':id/read')
		read(@Param('id') uid: string) {
			return this.notificationService.read(uid);
		}

	@Patch('read-all')
		readAll(@Request() req) {
			const uid = req.user.sub;
			return this.notificationService.readAll(uid);
		}
}
