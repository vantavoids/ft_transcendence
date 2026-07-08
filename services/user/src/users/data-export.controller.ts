import {
  Controller,
  Get,
  NotFoundException,
  Param,
  Post,
  UseGuards,
} from '@nestjs/common';
import { CurrentUserId } from '../auth/current-user.decorator';
import { JwtAuthGuard } from '../auth/jwt-auth.guard';
import { ParseSnowflakePipe } from '../common/pipes/parse-snowflake.pipe';
import { CreateDataExportResponse, DataExportStatusResponse } from './data-export.types';
import { DataExportService } from './data-export.service';

@Controller('v1/users')
@UseGuards(JwtAuthGuard)
export class DataExportController {
  constructor(private readonly dataExport: DataExportService) {}

  @Post('me/data-export')
  async requestExport(
    @CurrentUserId() userId: string,
  ): Promise<CreateDataExportResponse> {
    return this.dataExport.requestExport(userId);
  }

  @Get('me/data-export/:exportId')
  async getExportStatus(
    @CurrentUserId() userId: string,
    @Param('exportId', ParseSnowflakePipe) exportId: string,
  ): Promise<DataExportStatusResponse> {
    const job = await this.dataExport.getExportStatus(userId, exportId);
    if (!job) {
      throw new NotFoundException('Export not found');
    }

    return job;
  }
}
