import {
  CanActivate,
  ExecutionContext,
  Injectable,
  UnauthorizedException,
} from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { createHmac, timingSafeEqual } from 'node:crypto';
import { AuthenticatedRequest } from './current-user.decorator';

interface JwtPayload {
  sub?: string;
  exp?: number;
  iat?: number;
}

@Injectable()
export class JwtAuthGuard implements CanActivate {
  private readonly secret: string;

  constructor(config: ConfigService) {
    this.secret = config.getOrThrow<string>('JWT_SECRET');
  }

  canActivate(context: ExecutionContext): boolean {
    const request = context.switchToHttp().getRequest<AuthenticatedRequest & { headers: Record<string, string | undefined> }>();
    const token = this.extractBearerToken(request.headers.authorization);
    const payload = this.verifyToken(token);

    if (!payload.sub || !/^\d+$/.test(payload.sub)) {
      throw new UnauthorizedException('JWT sub claim is missing or invalid');
    }

    request.userId = payload.sub;
    return true;
  }

  private extractBearerToken(authorization: string | undefined): string {
    if (!authorization?.startsWith('Bearer ')) {
      throw new UnauthorizedException('Missing Authorization bearer token');
    }

    const token = authorization.slice('Bearer '.length).trim();
    if (!token) {
      throw new UnauthorizedException('Missing Authorization bearer token');
    }

    return token;
  }

  private verifyToken(token: string): JwtPayload {
    const parts = token.split('.');
    if (parts.length !== 3) {
      throw new UnauthorizedException('Invalid JWT format');
    }

    const [headerPart, payloadPart, signaturePart] = parts;
    const header = this.decodeJson<Record<string, unknown>>(headerPart);
    if (header.alg !== 'HS256') {
      throw new UnauthorizedException('Unsupported JWT algorithm');
    }

    const signature = this.base64UrlDecode(signaturePart);
    const expected = createHmac('sha256', this.secret)
      .update(`${headerPart}.${payloadPart}`)
      .digest();

    if (signature.length !== expected.length || !timingSafeEqual(signature, expected)) {
      throw new UnauthorizedException('Invalid JWT signature');
    }

    const payload = this.decodeJson<JwtPayload>(payloadPart);
    if (typeof payload.exp === 'number' && Date.now() >= payload.exp * 1000) {
      throw new UnauthorizedException('JWT has expired');
    }

    return payload;
  }

  private decodeJson<T>(part: string): T {
    try {
      return JSON.parse(this.base64UrlDecode(part).toString('utf8')) as T;
    } catch {
      throw new UnauthorizedException('Invalid JWT encoding');
    }
  }

  private base64UrlDecode(part: string): Buffer {
    const normalized = part.replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
    return Buffer.from(padded, 'base64');
  }
}
