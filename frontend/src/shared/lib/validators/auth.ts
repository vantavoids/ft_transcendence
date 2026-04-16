import * as z from 'zod';

export type RegisterFormValues = {
  username: string;
  password: string;
  confirm: string;
};

export type RegisterFormErrors = Partial<Record<keyof RegisterFormValues, string>>;

const registerSchema = z
  .object({
    username: z
      .string()
      .trim()
      .min(1, 'Username is required.')
      .min(4, 'Username must be at least 4 characters long.')
      .regex(/^[A-Za-z0-9_]+$/, 'Username must contain only letters, numbers, and underscores.'),
    password: z
      .string()
      .min(1, 'Password is required.')
      .min(12, 'Password must be at least 12 characters long.')
      .regex(/[a-z]/, 'Password must contain at least one lowercase letter.')
      .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.')
      .regex(/[0-9]/, 'Password must contain at least one number.')
      .regex(/[^A-Za-z0-9]/, 'Password must contain at least one special character.')
      .regex(/^\S+$/, 'Password must not contain spaces.'),
    confirm: z.string().min(1, 'Password confirmation is required.')
  })
  .refine((values) => values.password === values.confirm, {
    path: ['confirm'],
    message: 'Passwords do not match.'
  });

export function validateRegisterForm(values: RegisterFormValues): RegisterFormErrors {
  const result = registerSchema.safeParse({
    username: values.username.trim(),
    password: values.password,
    confirm: values.confirm
  });

  if (result.success) {
    return {};
  }

  const errors: RegisterFormErrors = {};

  for (const issue of result.error.issues) {
    const field = issue.path[0];

    if ((field === 'username' || field === 'password' || field === 'confirm') && !errors[field]) {
      errors[field] = issue.message;
    }
  }

  return errors;
}
