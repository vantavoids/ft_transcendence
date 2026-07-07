import * as z from "zod";

export type RegisterFormValues = {
  email: string;
  password: string;
  confirm: string;
};

export type LoginFormValues = {
  email: string;
  password: string;
};

export type RegisterFormErrors = Partial<Record<keyof RegisterFormValues, string>>;
export type LoginFormErrors = Partial<Record<keyof LoginFormValues, string>>;

export const emailSchema = z
  .string()
  .trim()
  .min(1, "Email is required.")
  .email("Enter a valid email address.");

export const passwordSchema = z
  .string()
  .min(1, "Password is required.")
  .min(12, "Password must be at least 12 characters long.")
  .regex(/[a-z]/, "Password must contain at least one lowercase letter.")
  .regex(/[A-Z]/, "Password must contain at least one uppercase letter.")
  .regex(/[0-9]/, "Password must contain at least one number.")
  .regex(/[^A-Za-z0-9]/, "Password must contain at least one special character.")
  .regex(/^\S+$/, "Password must not contain spaces.");

export function checkPassword(value: string): string | null {
  const result = passwordSchema.safeParse(value);
  return result.success ? null : result.error.issues[0].message;
}

export function checkEmail(value: string): string | null {
  const result = emailSchema.safeParse(value);
  return result.success ? null : result.error.issues[0].message;
}

const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, "Password is required."),
});

const registerSchema = z
  .object({
    email: emailSchema,
    password: passwordSchema,
    confirm: z.string().min(1, "Password confirmation is required."),
  })
  .refine((values) => values.password === values.confirm, {
    path: ["confirm"],
    message: "Passwords do not match.",
  });

export function validateRegisterForm(values: RegisterFormValues): RegisterFormErrors {
  const result = registerSchema.safeParse({
    email: values.email.trim(),
    password: values.password,
    confirm: values.confirm,
  });

  if (result.success) {
    return {};
  }

  const errors: RegisterFormErrors = {};

  for (const issue of result.error.issues) {
    const field = issue.path[0];

    if ((field === "email" || field === "password" || field === "confirm") && !errors[field]) {
      errors[field] = issue.message;
    }
  }

  return errors;
}

export function validateLoginForm(values: LoginFormValues): LoginFormErrors {
  const result = loginSchema.safeParse({
    email: values.email.trim(),
    password: values.password,
  });

  if (result.success) {
    return {};
  }

  const errors: LoginFormErrors = {};

  for (const issue of result.error.issues) {
    const field = issue.path[0];

    if ((field === "email" || field === "password") && !errors[field]) {
      errors[field] = issue.message;
    }
  }

  return errors;
}
