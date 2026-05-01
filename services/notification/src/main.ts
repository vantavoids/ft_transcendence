import { NestFactory } from '@nestjs/core';
import { AppModule } from './app.module';
import { DocumentBuilder, SwaggerModule } from '@nestjs/swagger';

async function bootstrap() {
  const app = await NestFactory.create(AppModule);
  app.enableShutdownHooks();
  app.setGlobalPrefix('v1');

  if (process.env.NODE_ENV === 'development') {
    const config = new DocumentBuilder()
      .setTitle('Notification Service')
      .setVersion('1.0')
      .addServer('/api/notification')
      .build();
    const document = SwaggerModule.createDocument(app, config);
    // Mount outside the v1 global prefix
    app.getHttpAdapter().getInstance().get('/openapi/v1.json', (_req: unknown, res: any) => {
      res.json(document);
    });
  }

  await app.listen(process.env.APP_PORT ?? 3000, '0.0.0.0');
}
bootstrap();
