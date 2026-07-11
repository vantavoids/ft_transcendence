import { z } from 'zod';

export const PROFILE_DISPLAY_NAME_MAX_LENGTH = 64;
export const PROFILE_BIO_MAX_LENGTH = 280;
export const PROFILE_AVATAR_MAX_SIZE_BYTES = 5 * 1024 * 1024;
export const PROFILE_BANNER_MAX_SIZE_BYTES = 8 * 1024 * 1024;
const PROFILE_IMAGE_MIME_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);

const profileStatusSchema = z.enum(['online', 'idle', 'dnd', 'offline']);

const optionalDisplayNameSchema = z
  .string()
  .transform((value) => value.trim())
  .pipe(z.string().min(1, 'Display name is required.').max(PROFILE_DISPLAY_NAME_MAX_LENGTH))
  .optional();

const optionalBioSchema = z
  .string()
  .transform((value) => value.trim())
  .pipe(z.string().max(PROFILE_BIO_MAX_LENGTH))
  .optional();

export const profileUpdateSchema = z
  .object({
    display_name: optionalDisplayNameSchema,
    bio: optionalBioSchema,
    status: profileStatusSchema.optional()
  })
  .refine(
    (value) =>
      value.display_name !== undefined || value.bio !== undefined || value.status !== undefined,
    'Enter a new display name, bio, or status.'
  );

export type ProfileUpdateInput = z.infer<typeof profileUpdateSchema>;

export function validateProfileUpdateInput(input: unknown): ProfileUpdateInput {
  return profileUpdateSchema.parse(input);
}

export function validateProfileImageFile(file: File, kind: 'avatar' | 'banner'): void {
  if (!PROFILE_IMAGE_MIME_TYPES.has(file.type)) {
    throw new Error('Only JPEG, PNG, and WebP images are allowed.');
  }

  const maxSize = kind === 'avatar' ? PROFILE_AVATAR_MAX_SIZE_BYTES : PROFILE_BANNER_MAX_SIZE_BYTES;
  if (file.size > maxSize) {
    const maxSizeLabel = kind === 'avatar' ? '5 MB' : '8 MB';
    throw new Error(`Image must be smaller than ${maxSizeLabel}.`);
  }
}
