import { Controller, Get, Patch, Param, Query, Req, ParseBoolPipe, ParseIntPipe } from '@nestjs/common';
import { NotificationService } from "../application/notification.service";

@Controller('notifications')
export class NotificationController {
  constructor(private readonly service: NotificationService) {}

  @Get()
  async get(
    @Req() req: Request,
    @Query('read', new ParseBoolPipe({ optional: true })) read?: boolean,
    @Query('limit', new ParseIntPipe({ optional: true })) limit?: number,
    @Query('before') before?: bigint,
  ) {
    const uid = (req as any).user.id as bigint; // TODO: typer req.user selon le JWT guard
    const bbi = before ? BigInt(before): undefined;
    const notifs = await this.service.get(uid, read, limit, bbi);
    return notifs.map(n => ({
      id: n.id.toString(),
      user_id: n.user_id.toString(),
      type: n.type,
      payload: n.payload,
      read: n.read,
      created_at: n.created_at,
    }));
  }

  @Patch(':id/read')
  async read(
    @Req() req: Request,
    @Param('id') id: bigint, 
  ) {
    const uid = (req as any).user.id as bigint; // TODO: typer req.user selon le JWT guard
    const notif = await this.service.read(uid, id);
    return {
      id: notif.id.toString(),
      read: notif.read,
    }
  }

  @Patch('read-all')
  async readAll(
    @Req() req: Request,
  ) {
    const uid = (req as any).user.id as bigint; // TODO: typer req.user selon le JWT guard
    return this.service.readAll(uid);
  }
}
