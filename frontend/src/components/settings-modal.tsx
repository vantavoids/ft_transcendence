'use client';

import { useEffect, useRef, useState, type ChangeEvent } from 'react';
import { useRouter } from 'next/navigation';
import { Camera, ImagePlus, LogOut, Settings, Trash2, X } from 'lucide-react';
import { deleteAccount, getIdentity, updateIdentity, type AuthIdentity } from '../shared/api/auth';
import {
  deleteUserAvatar,
  deleteUserBanner,
  updateUserProfile,
  uploadUserAvatar,
  uploadUserBanner,
  type PublicUserProfileDto,
  type UserStatus
} from '../shared/api/user';
import { clearSession } from '../shared/lib/session';
import { describeAccountUpdateError, describeDeleteAccountError } from '../shared/lib/auth-errors';
import { checkEmail, checkPassword } from '../shared/lib/validators/auth';

type SettingsModalProps = {
  currentUser: PublicUserProfileDto | null;
  onProfileUpdated: (profile: PublicUserProfileDto) => void;
  onClose: () => void;
  onDisconnect: () => void;
};

type Panel = 'menu' | 'credentials' | 'delete';

const statusOptions: Array<{ value: UserStatus; label: string }> = [
  { value: 'online', label: 'Online' },
  { value: 'idle', label: 'Idle' },
  { value: 'dnd', label: 'Do not disturb' },
  { value: 'offline', label: 'Offline' }
];

export function SettingsModal({
  currentUser,
  onProfileUpdated,
  onClose,
  onDisconnect
}: SettingsModalProps) {
  const router = useRouter();
  const [identity, setIdentity] = useState<AuthIdentity | null>(null);
  const [panel, setPanel] = useState<Panel>('menu');
  const [profileFormVersion, setProfileFormVersion] = useState(0);
  const [profileError, setProfileError] = useState('');
  const [profileSuccess, setProfileSuccess] = useState('');
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const bannerInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    function handleEscape(event: KeyboardEvent) {
      if (event.key !== 'Escape' && event.key !== 'Esc' && event.code !== 'Escape') {
        return;
      }

      onClose();
    }

    window.addEventListener('keydown', handleEscape);
    return () => window.removeEventListener('keydown', handleEscape);
  }, [onClose]);

  useEffect(() => {
    let active = true;
    getIdentity()
      .then((value) => {
        if (active) {
          setIdentity(value);
        }
      })
      .catch(() => {
        // Best-effort. The auth section still renders from the available state.
      });
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    setProfileFormVersion((current) => current + 1);
  }, [currentUser]);

  const isOAuthOnly = identity !== null && identity.email === null;
  const displayName = currentUser?.display_name?.trim() || currentUser?.username || 'Loading...';
  const handleAccountDeleted = async () => {
    clearSession();
    router.push('/auth/login');
    router.refresh();
  };

  async function handleProfileSave(formData: FormData) {
    if (!currentUser) {
      return;
    }

    const nextDisplayName = String(formData.get('display_name') ?? '').trim();
    const nextBio = String(formData.get('bio') ?? '');
    const nextStatus = String(formData.get('status') ?? currentUser.status) as UserStatus;

    setProfileError('');
    setProfileSuccess('');

    try {
      setIsSavingProfile(true);
      const updated = await updateUserProfile(currentUser.id, {
        display_name: nextDisplayName.length > 0 ? nextDisplayName : undefined,
        bio: nextBio.length > 0 ? nextBio : '',
        status: nextStatus
      });

      onProfileUpdated(updated);
      setProfileSuccess('Profile updated.');
    } catch (error) {
      setProfileError(error instanceof Error ? error.message : 'Unable to update profile.');
    } finally {
      setIsSavingProfile(false);
    }
  }

  async function handleAvatarChange(event: ChangeEvent<HTMLInputElement>) {
    if (!currentUser) {
      return;
    }

    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) {
      return;
    }

    setProfileError('');
    setProfileSuccess('');

    try {
      const result = await uploadUserAvatar(currentUser.id, file);
      const updated = { ...currentUser, avatar_url: result.avatar_url };
      onProfileUpdated(updated);
      setProfileSuccess('Avatar updated.');
    } catch (error) {
      setProfileError(error instanceof Error ? error.message : 'Unable to upload avatar.');
    }
  }

  async function handleBannerChange(event: ChangeEvent<HTMLInputElement>) {
    if (!currentUser) {
      return;
    }

    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) {
      return;
    }

    setProfileError('');
    setProfileSuccess('');

    try {
      const result = await uploadUserBanner(currentUser.id, file);
      const updated = { ...currentUser, banner_url: result.banner_url };
      onProfileUpdated(updated);
      setProfileSuccess('Banner updated.');
    } catch (error) {
      setProfileError(error instanceof Error ? error.message : 'Unable to upload banner.');
    }
  }

  async function handleDeleteAvatar() {
    if (!currentUser) {
      return;
    }

    setProfileError('');
    setProfileSuccess('');

    try {
      await deleteUserAvatar(currentUser.id);
      onProfileUpdated({ ...currentUser, avatar_url: null });
      setProfileSuccess('Avatar removed.');
    } catch (error) {
      setProfileError(error instanceof Error ? error.message : 'Unable to delete avatar.');
    }
  }

  async function handleDeleteBanner() {
    if (!currentUser) {
      return;
    }

    setProfileError('');
    setProfileSuccess('');

    try {
      await deleteUserBanner(currentUser.id);
      onProfileUpdated({ ...currentUser, banner_url: null });
      setProfileSuccess('Banner removed.');
    } catch (error) {
      setProfileError(error instanceof Error ? error.message : 'Unable to delete banner.');
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close settings"
      />
      <section className="relative w-full max-w-[42rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10">
        <div className="flex h-[4.75rem] items-center justify-between border-b border-white/8 px-5">
          <div className="flex min-w-0 items-center gap-3">
            <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-aqua/10 text-aqua">
              <Settings className="h-5 w-5" strokeWidth={1.9} />
            </span>
            <div className="min-w-0">
              <h2 className="truncate text-[1.15rem] font-bold tracking-[-0.03em] text-white">
                Settings
              </h2>
              <p className="font-category text-[0.7rem] uppercase tracking-[0.14em] text-white/35">
                Profile and account
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="flex h-9 w-9 items-center justify-center rounded-md text-[#8b8b8f] transition hover:bg-frame hover:text-white"
            aria-label="Close settings"
          >
            <X className="h-4 w-4" strokeWidth={2} />
          </button>
        </div>

        <div className="max-h-[calc(100vh-8rem)] overflow-y-auto px-5 py-5">
          {panel === 'menu' ? (
            <div className="grid gap-5 lg:grid-cols-[1.15fr_0.85fr]">
              <section className="rounded-[0.9rem] border border-white/8 bg-panel p-4">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <h3 className="text-lg font-bold tracking-[-0.03em] text-white">
                      Public profile
                    </h3>
                    <p className="mt-1 text-sm text-white/40">
                      This is the profile other users see.
                    </p>
                  </div>
                  <div className="flex h-14 w-14 items-center justify-center rounded-xl bg-frame text-sm font-bold text-white">
                    {displayName.slice(0, 1).toUpperCase()}
                  </div>
                </div>

                <form
                  key={`${currentUser?.id ?? 'profile'}-${profileFormVersion}`}
                  action={handleProfileSave}
                  className="mt-4 grid gap-3"
                >
                  <Field
                    name="display_name"
                    label="Display name"
                    type="text"
                    placeholder={currentUser?.display_name || currentUser?.username || 'Your name'}
                    defaultValue={currentUser?.display_name ?? ''}
                    autoComplete="nickname"
                  />

                  <div className="grid gap-1.5">
                    <label
                      htmlFor="bio"
                      className="text-xs font-semibold uppercase tracking-[0.1em] text-white/45"
                    >
                      Bio
                    </label>
                    <textarea
                      id="bio"
                      name="bio"
                      defaultValue={currentUser?.bio ?? ''}
                      rows={4}
                      className="min-h-[6rem] w-full rounded-md border border-transparent bg-input-bg px-4 py-3 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
                      placeholder="Tell people a little about yourself"
                    />
                  </div>

                  <div className="grid gap-1.5">
                    <label
                      htmlFor="status"
                      className="text-xs font-semibold uppercase tracking-[0.1em] text-white/45"
                    >
                      Status
                    </label>
                    <select
                      id="status"
                      name="status"
                      defaultValue={currentUser?.status ?? 'online'}
                      className="h-11 rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition focus:border-aqua/35"
                    >
                      {statusOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="grid gap-3 sm:grid-cols-2">
                    <ActionButton
                      label="Upload avatar"
                      icon={Camera}
                      onClick={() => avatarInputRef.current?.click()}
                    />
                    <ActionButton
                      label="Upload banner"
                      icon={ImagePlus}
                      onClick={() => bannerInputRef.current?.click()}
                    />
                  </div>

                  <div className="grid gap-2 sm:grid-cols-2">
                    <button
                      type="button"
                      onClick={handleDeleteAvatar}
                      disabled={!currentUser?.avatar_url}
                      className="flex h-10 items-center justify-center rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/70 transition hover:text-white disabled:cursor-not-allowed disabled:opacity-40"
                    >
                      Remove avatar
                    </button>
                    <button
                      type="button"
                      onClick={handleDeleteBanner}
                      disabled={!currentUser?.banner_url}
                      className="flex h-10 items-center justify-center rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/70 transition hover:text-white disabled:cursor-not-allowed disabled:opacity-40"
                    >
                      Remove banner
                    </button>
                  </div>

                  <input
                    ref={avatarInputRef}
                    type="file"
                    accept="image/png,image/jpeg,image/webp"
                    className="hidden"
                    onChange={handleAvatarChange}
                  />
                  <input
                    ref={bannerInputRef}
                    type="file"
                    accept="image/png,image/jpeg,image/webp"
                    className="hidden"
                    onChange={handleBannerChange}
                  />

                  {profileError ? (
                    <p className="rounded-md border border-pink/25 bg-pink/10 px-3 py-2 text-sm text-pink">
                      {profileError}
                    </p>
                  ) : null}
                  {profileSuccess ? (
                    <p className="rounded-md border border-lime/25 bg-lime/10 px-3 py-2 text-sm text-lime">
                      {profileSuccess}
                    </p>
                  ) : null}

                  <div className="mt-1 grid grid-cols-2 gap-2.5">
                    <button
                      type="button"
                      onClick={() => setProfileFormVersion((current) => current + 1)}
                      className="flex h-11 items-center justify-center rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/70 transition hover:text-white"
                    >
                      Reset
                    </button>
                    <button
                      type="submit"
                      disabled={isSavingProfile || !currentUser}
                      className="flex h-11 items-center justify-center rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
                    >
                      {isSavingProfile ? 'Saving...' : 'Save profile'}
                    </button>
                  </div>
                </form>
              </section>

              <section className="grid gap-4">
                <div className="rounded-[0.9rem] border border-white/8 bg-panel p-4">
                  <h3 className="text-lg font-bold tracking-[-0.03em] text-white">Account auth</h3>
                  <p className="mt-1 text-sm text-white/40">
                    Email and password are handled by the Auth Service.
                  </p>

                  <div className="mt-4 flex items-center gap-4">
                    <div className="h-12 w-12 shrink-0 rounded-full bg-frame" />
                    <div className="min-w-0">
                      <p className="truncate text-sm font-semibold text-white">
                        {identity?.email ?? 'OAuth account'}
                      </p>
                      <p className="text-xs text-white/35">
                        {isOAuthOnly
                          ? 'This account does not have a local password.'
                          : 'Password change is available.'}
                      </p>
                    </div>
                  </div>

                  <div className="mt-4 grid gap-2.5">
                    <button
                      type="button"
                      onClick={() => setPanel('credentials')}
                      disabled={isOAuthOnly}
                      className="flex h-11 w-full items-center justify-center rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/80 transition hover:border-aqua/40 hover:text-white disabled:cursor-not-allowed disabled:opacity-40"
                    >
                      Change email or password
                    </button>
                    <button
                      type="button"
                      onClick={onDisconnect}
                      className="flex h-11 w-full items-center justify-center gap-2 rounded-md border border-pink/25 bg-pink/10 text-sm font-bold text-pink transition hover:border-pink/45 hover:bg-pink/15"
                    >
                      <LogOut className="h-4 w-4" strokeWidth={1.9} />
                      Disconnect
                    </button>
                    <button
                      type="button"
                      onClick={() => setPanel('delete')}
                      className="flex h-10 w-full items-center justify-center gap-2 rounded-md text-sm font-semibold text-white/40 transition hover:text-pink"
                    >
                      <Trash2 className="h-4 w-4" strokeWidth={1.9} />
                      Delete account
                    </button>
                  </div>
                </div>

                <div className="rounded-[0.9rem] border border-white/8 bg-panel p-4">
                  <h3 className="text-lg font-bold tracking-[-0.03em] text-white">
                    Current profile
                  </h3>
                  <p className="mt-1 text-sm text-white/40">Live data from User Service.</p>
                  <dl className="mt-4 grid gap-3 text-sm">
                    <div className="flex items-center justify-between gap-4">
                      <dt className="text-white/45">Username</dt>
                      <dd className="truncate font-semibold text-white">
                        {currentUser?.username ?? '...'}
                      </dd>
                    </div>
                    <div className="flex items-center justify-between gap-4">
                      <dt className="text-white/45">Display</dt>
                      <dd className="truncate font-semibold text-white">
                        {currentUser?.display_name ?? 'No display name'}
                      </dd>
                    </div>
                    <div className="flex items-center justify-between gap-4">
                      <dt className="text-white/45">Status</dt>
                      <dd className="truncate font-semibold capitalize text-white">
                        {currentUser?.status ?? 'offline'}
                      </dd>
                    </div>
                  </dl>
                </div>
              </section>
            </div>
          ) : panel === 'credentials' ? (
            <CredentialsForm currentEmail={identity?.email ?? ''} onBack={() => setPanel('menu')} />
          ) : (
            <DeleteAccountPanel onBack={() => setPanel('menu')} onDeleted={handleAccountDeleted} />
          )}
        </div>
      </section>
    </div>
  );
}

type ActionButtonProps = {
  label: string;
  icon: typeof Camera;
  onClick: () => void;
};

function ActionButton({ label, icon: Icon, onClick }: ActionButtonProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="flex h-10 items-center justify-center gap-2 rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/80 transition hover:border-aqua/40 hover:text-white"
    >
      <Icon className="h-4 w-4" strokeWidth={1.9} />
      {label}
    </button>
  );
}

type CredentialsFormProps = {
  currentEmail: string;
  onBack: () => void;
};

function CredentialsForm({ currentEmail, onBack }: CredentialsFormProps) {
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(formData: FormData) {
    const email = String(formData.get('email') ?? '').trim();
    const newPassword = String(formData.get('new_password') ?? '');
    const currentPassword = String(formData.get('current_password') ?? '');

    setError('');
    setSuccess('');

    const emailChanged = email.length > 0 && email !== currentEmail;
    const passwordChanged = newPassword.length > 0;

    if (!emailChanged && !passwordChanged) {
      setError('Enter a new email or a new password.');
      return;
    }
    if (!currentPassword) {
      setError('Enter your current password to confirm the change.');
      return;
    }
    if (emailChanged) {
      const emailError = checkEmail(email);
      if (emailError) {
        setError(emailError);
        return;
      }
    }
    if (passwordChanged) {
      const passwordError = checkPassword(newPassword);
      if (passwordError) {
        setError(passwordError);
        return;
      }
    }

    try {
      setIsSubmitting(true);
      await updateIdentity({
        email: emailChanged ? email : undefined,
        new_password: passwordChanged ? newPassword : undefined,
        current_password: currentPassword
      });
      setSuccess('Account updated.');
    } catch (err) {
      setError(describeAccountUpdateError(err));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form action={handleSubmit} className="grid gap-3">
      <Field
        name="email"
        label="New email"
        type="email"
        placeholder={currentEmail || 'you@example.com'}
        autoComplete="email"
      />
      <Field
        name="new_password"
        label="New password"
        type="password"
        placeholder="leave blank to keep"
        autoComplete="new-password"
      />
      <Field
        name="current_password"
        label="Current password"
        type="password"
        placeholder="required to confirm"
        autoComplete="current-password"
      />

      {error ? (
        <p className="rounded-md border border-pink/25 bg-pink/10 px-3 py-2 text-sm text-pink">
          {error}
        </p>
      ) : null}
      {success ? (
        <p className="rounded-md border border-lime/25 bg-lime/10 px-3 py-2 text-sm text-lime">
          {success}
        </p>
      ) : null}

      <div className="mt-1 grid grid-cols-2 gap-2.5">
        <button
          type="button"
          onClick={onBack}
          className="flex h-11 items-center justify-center rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/70 transition hover:text-white"
        >
          Back
        </button>
        <button
          type="submit"
          disabled={isSubmitting}
          className="flex h-11 items-center justify-center rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
        >
          {isSubmitting ? 'Saving...' : 'Save changes'}
        </button>
      </div>
    </form>
  );
}

type DeleteAccountPanelProps = {
  onBack: () => void;
  onDeleted: () => void;
};

function DeleteAccountPanel({ onBack, onDeleted }: DeleteAccountPanelProps) {
  const [error, setError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleDelete() {
    setError('');
    try {
      setIsSubmitting(true);
      await deleteAccount();
      onDeleted();
    } catch (err) {
      setError(describeDeleteAccountError(err));
      setIsSubmitting(false);
    }
  }

  return (
    <div className="grid gap-3">
      <p className="text-sm leading-6 text-white/60">
        This permanently deletes your account and frees your email for re-registration. This cannot
        be undone.
      </p>

      {error ? (
        <p className="rounded-md border border-pink/25 bg-pink/10 px-3 py-2 text-sm text-pink">
          {error}
        </p>
      ) : null}

      <div className="grid grid-cols-2 gap-2.5">
        <button
          type="button"
          onClick={onBack}
          disabled={isSubmitting}
          className="flex h-11 items-center justify-center rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/70 transition hover:text-white disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          type="button"
          onClick={handleDelete}
          disabled={isSubmitting}
          className="flex h-11 items-center justify-center gap-2 rounded-md bg-pink text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:opacity-60"
        >
          <Trash2 className="h-4 w-4" strokeWidth={2} />
          {isSubmitting ? 'Deleting...' : 'Delete'}
        </button>
      </div>
    </div>
  );
}

type FieldProps = {
  name: string;
  label: string;
  type: string;
  placeholder: string;
  autoComplete: string;
  defaultValue?: string;
};

function Field({ name, label, type, placeholder, autoComplete, defaultValue }: FieldProps) {
  return (
    <div className="grid gap-1.5">
      <label
        htmlFor={name}
        className="text-xs font-semibold uppercase tracking-[0.1em] text-white/45"
      >
        {label}
      </label>
      <input
        id={name}
        name={name}
        type={type}
        defaultValue={defaultValue}
        placeholder={placeholder}
        autoComplete={autoComplete}
        className="h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
      />
    </div>
  );
}
