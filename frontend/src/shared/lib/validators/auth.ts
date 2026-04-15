export type RegisterFormValues = {
  username: string;
  password: string;
  confirm: string;
};

export type RegisterFormErrors = Partial<Record<keyof RegisterFormValues, string>>;

export function validateRegisterForm(values: RegisterFormValues): RegisterFormErrors {
  const errors: RegisterFormErrors = {};

  if (!values.username.trim()) {
    errors.username = 'Username is required.';
  }

  if (!values.password) {
    errors.password = 'Password is required.';
  }

  if (!values.confirm) {
    errors.confirm = 'Password confirmation is required.';
  } else if (values.password !== values.confirm) {
    errors.confirm = 'Passwords do not match.';
  }

  return errors;
}
