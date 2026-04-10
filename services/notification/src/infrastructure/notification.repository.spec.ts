import { DeepMockProxy, mockDeep } from 'jest-mock-extended';
import { PrismaClient } from '../../generated/prisma/client';
import { PrismaNotificationRepository } from './prisma.repository';
import { ForbiddenException, NotFoundException } from '@nestjs/common';

describe('PrismaNotificationRepository', () => {
  let repository: PrismaNotificationRepository;
  let prismaMock: DeepMockProxy<PrismaClient>;

  beforeEach(() => {
    prismaMock = mockDeep<PrismaClient>();
    repository = new PrismaNotificationRepository(prismaMock);
  });

  describe('get()', () => {
      it ('get have called the query \'read\'', async () => {
      prismaMock.notificationData.findMany.mockResolvedValue([]);

      await repository.get(1n, true);
  
      expect(prismaMock.notificationData.findMany).toHaveBeenCalledWith(
        expect.objectContaining({
          where: expect.objectContaining({
            user_id: 1n,
            read: true,
          })
        })
      );
    });

    it ('get have the number of notification set to the default 50 if the query \'limit\' is missing', async () => {
      prismaMock.notificationData.findMany.mockResolvedValue([]);

      await repository.get(1n, undefined);
  
      expect(prismaMock.notificationData.findMany).toHaveBeenCalledWith(
        expect.objectContaining({
          take: 50
        })
      );
    });

    it('get have called the cursor \'before\', even if the date doesnt exist in the db', async () => {
      const date = new Date('2026-01-01');

      prismaMock.notificationData.findUnique.mockResolvedValue({
        id: 100n,
        created_at: date,
      } as any);

      prismaMock.notificationData.findMany.mockResolvedValue([]);

      await repository.get(1n, undefined, 20, 100n);
  
      expect(prismaMock.notificationData.findMany).toHaveBeenCalledWith(
        expect.objectContaining({
          where: expect.objectContaining({
            created_at: { lt: date }
          })
        })
      );
    });
  });

  describe('read()', () => {
    it('read should throw \'NotFoundException\', if he doesnt find the notif with the same id', async () => {
      prismaMock.notificationData.findUnique.mockResolvedValue(null);

      await expect(repository.read(1n, 2n)).rejects.toThrow(NotFoundException);
    });

    it('read should throw \'ForbiddenException\', if he find a notif with the same id, but doesnt belong to the same uid', async () => {
      prismaMock.notificationData.findUnique.mockResolvedValue({
        id: 2n,
        user_id: 999n,
        read: false,
        created_at: new Date(),
      } as any);

      await expect(repository.read(1n, 2n)).rejects.toThrow(ForbiddenException);
      expect(prismaMock.notificationData.update).not.toHaveBeenCalled();
    });

    it('read have updated the notif from \'read: false\' to \'read: true\'', async () => {
      const mockNotif = {
        id: 2n,
        user_id: 1n,
        read: false,
        created_at: new Date()
      };
      
      prismaMock.notificationData.findUnique.mockResolvedValue(mockNotif as any);
      prismaMock.notificationData.update.mockResolvedValue({ ...mockNotif, read: true } as any);

      const result = await repository.read(1n, 2n);

      expect(result.read).toBe(true);
      expect(prismaMock.notificationData.update).toHaveBeenCalledWith({
        where: { id: 2n },
        data: { read: true },
      });
    });
  });

  describe('readAll()', () => {
    it('readAll have send the right number of notifications he have updated', async () => {
      prismaMock.notificationData.updateMany.mockResolvedValue({ count: 10 });

      const uid = 1n;
      const result = await repository.readAll(uid);

      expect(result).toEqual({ updated: 10 });
    });

    it('readAll have filtered the updated notifications with \'user_id\' and not \'id\'', async () => {
      prismaMock.notificationData.updateMany.mockResolvedValue({ count: 1 });

      const uid = 1n;
      await repository.readAll(uid);

      expect(prismaMock.notificationData.updateMany).toHaveBeenCalledWith({
        where: {
          user_id: uid,
          read: false,
        },
        data: {read: true},
      });
    });
  });
});