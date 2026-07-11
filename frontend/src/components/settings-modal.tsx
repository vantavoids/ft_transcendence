'use client';

import { useEffect, useRef, useState } from 'react';
import type { ChangeEvent } from 'react';
import { useRouter } from 'next/navigation';
import {
  Image as ImageIcon,
  LogOut,
  Mail,
  Settings,
  ShieldCheck,
  Trash2,
  Upload,
  X
} from 'lucide-react';
import { deleteAccount, getIdentity, updateIdentity, type AuthIdentity } from '../shared/api/auth';
import { clearSession } from '../shared/lib/session';
import { describeAccountUpdateError, describeDeleteAccountError } from '../shared/lib/auth-errors';
import { checkEmail, checkPassword } from '../shared/lib/validators/auth';
import { useCloseOnEscape } from '../shared/hooks/use-close-on-escape';
import { toSidebarStatus, type CurrentUserProfile } from '../shared/mappers/user';
import { AvatarWithStatus } from './avatar-with-status';
import { useCurrentUserProfile } from '../shared/user/user-store';
import {
  PROFILE_BIO_MAX_LENGTH,
  PROFILE_DISPLAY_NAME_MAX_LENGTH,
  validateProfileImageFile,
  validateProfileUpdateInput
} from '../shared/lib/validators/profile';

type SettingsModalProps = {
  currentUser: CurrentUserProfile | null;
  onClose: () => void;
  onDisconnect: () => void;
};

type Panel = 'menu' | 'profile' | 'credentials' | 'delete';

export function SettingsModal({ currentUser, onClose, onDisconnect }: SettingsModalProps) {
  const router = useRouter();
  const {
    updateCurrentUserProfile,
    uploadCurrentUserAvatar,
    removeCurrentUserAvatar,
    uploadCurrentUserBanner,
    removeCurrentUserBanner
  } = useCurrentUserProfile();
  const [identity, setIdentity] = useState<AuthIdentity | null>(null);
  const [panel, setPanel] = useState<Panel>('menu');

  useCloseOnEscape(onClose);

  useEffect(() => {
    let active = true;
    getIdentity()
      .then((value) => {
        if (active) setIdentity(value);
      })
      .catch(() => {
        // identity is best-effort; the rest of the modal still works
      });
    return () => {
      active = false;
    };
  }, []);

  const isOAuthOnly = identity !== null && identity.email === null;

  async function handleAccountDeleted() {
    clearSession();
    router.push('/auth/login');
    router.refresh();
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 px-4 py-6">
      <button
        type="button"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
        aria-label="Close settings"
      />
      <section className="relative w-full max-w-[26rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10">
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
                Account
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

        <div className="px-5 py-5">
          <div className="rounded-[1rem] border border-white/8 bg-panel p-4">
            <div className="flex items-center gap-4">
              <AvatarWithStatus
                size="lg"
                name={currentUser?.displayName ?? currentUser?.username ?? 'Guest'}
                accent="aqua"
                status={currentUser ? toSidebarStatus(currentUser.status) : 'offline'}
                avatarUrl={currentUser?.avatarUrl}
              />
              <div className="min-w-0">
                <h3 className="truncate text-[1.35rem] font-bold tracking-[-0.04em] text-white">
                  {currentUser?.displayName ?? currentUser?.username ?? 'Guest'}
                </h3>
                <p className="mono-detail mt-1 truncate text-sm text-white/40">
                  {currentUser?.username ? `@${currentUser.username}` : 'profile loading'}
                </p>
                <p className="mt-1 flex items-center gap-2 text-xs text-white/35">
                  <Mail className="h-3.5 w-3.5" strokeWidth={1.9} />
                  {identity?.email ?? 'No email available'}
                </p>
                <p className="mt-1 flex items-center gap-2 text-xs text-white/35">
                  <ShieldCheck className="h-3.5 w-3.5" strokeWidth={1.9} />
                  {currentUser ? currentUser.status : 'offline'}
                </p>
              </div>
            </div>
            {currentUser?.bio ? (
              <p className="mt-4 text-sm leading-6 text-white/60">{currentUser.bio}</p>
            ) : (
              <p className="mt-4 text-sm leading-6 text-white/35">No bio set.</p>
            )}
          </div>

          {panel === 'menu' ? (
            <div className="mt-6 grid gap-2.5">
              <button
                type="button"
                onClick={() => setPanel('profile')}
                className="flex h-11 w-full items-center justify-center gap-2 rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/80 transition hover:border-aqua/40 hover:text-white"
              >
                <ImageIcon className="h-4 w-4" strokeWidth={1.9} />
                Edit profile
              </button>
              <button
                type="button"
                onClick={() => setPanel('credentials')}
                disabled={isOAuthOnly}
                className="flex h-11 w-full items-center justify-center rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/80 transition hover:border-aqua/40 hover:text-white disabled:cursor-not-allowed disabled:opacity-40"
              >
                Change email or password
              </button>
              {isOAuthOnly ? (
                <p className="text-xs text-white/35">
                  This account signs in with an OAuth provider, so it has no password to change
                  here.
                </p>
              ) : null}
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
                className="flex h-9 w-full items-center justify-center gap-2 rounded-md text-sm font-semibold text-white/40 transition hover:text-pink"
              >
                <Trash2 className="h-4 w-4" strokeWidth={1.9} />
                Delete account
              </button>
            </div>
          ) : null}

          {panel === 'profile' ? (
            <ProfileForm currentUser={currentUser} onBack={() => setPanel('menu')} />
          ) : null}

          {panel === 'credentials' ? (
            <CredentialsForm onBack={() => setPanel('menu')} currentEmail={identity?.email ?? ''} />
          ) : null}

          {panel === 'delete' ? (
            <DeleteAccountPanel onBack={() => setPanel('menu')} onDeleted={handleAccountDeleted} />
          ) : null}
        </div>
      </section>
    </div>
  );
}

type ProfileFormProps = {
  currentUser: CurrentUserProfile | null;
  onBack: () => void;
};

function ProfileForm({ currentUser, onBack }: ProfileFormProps) {
  const {
    updateCurrentUserProfile,
    uploadCurrentUserAvatar,
    removeCurrentUserAvatar,
    uploadCurrentUserBanner,
    removeCurrentUserBanner
  } = useCurrentUserProfile();
  const [displayName, setDisplayName] = useState(currentUser?.displayName ?? '');
  const [bio, setBio] = useState(currentUser?.bio ?? '');
  const [status, setStatus] = useState<CurrentUserProfile['status']>(
    currentUser?.status ?? 'offline'
  );
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [isBusyAvatar, setIsBusyAvatar] = useState(false);
  const [isBusyBanner, setIsBusyBanner] = useState(false);
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const bannerInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (!currentUser) {
      return;
    }

    setDisplayName(currentUser.displayName);
    setBio(currentUser.bio ?? '');
    setStatus(currentUser.status);
  }, [currentUser]);

  if (!currentUser) {
    return (
      <div className="mt-6 grid gap-3">
        <p className="text-sm text-white/45">Profile is still loading.</p>
        <button
          type="button"
          onClick={onBack}
          className="flex h-11 items-center justify-center rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/70 transition hover:text-white"
        >
          Back
        </button>
      </div>
    );
  }

  async function handleSave() {
    setError('');
    setSuccess('');

    try {
      setIsSaving(true);
      const payload = validateProfileUpdateInput({
        display_name: displayName,
        bio,
        status
      });
      await updateCurrentUserProfile(payload);
      setSuccess('Profile updated.');
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Failed to update profile.');
    } finally {
      setIsSaving(false);
    }
  }

  async function handleAvatarUpload(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) {
      return;
    }

    setError('');
    setSuccess('');

    try {
      setIsBusyAvatar(true);
      validateProfileImageFile(file, 'avatar');
      await uploadCurrentUserAvatar(file);
      setSuccess('Avatar updated.');
    } catch (uploadError) {
      setError(uploadError instanceof Error ? uploadError.message : 'Failed to upload avatar.');
    } finally {
      setIsBusyAvatar(false);
    }
  }

  async function handleBannerUpload(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) {
      return;
    }

    setError('');
    setSuccess('');

    try {
      setIsBusyBanner(true);
      validateProfileImageFile(file, 'banner');
      await uploadCurrentUserBanner(file);
      setSuccess('Banner updated.');
    } catch (uploadError) {
      setError(uploadError instanceof Error ? uploadError.message : 'Failed to upload banner.');
    } finally {
      setIsBusyBanner(false);
    }
  }

  async function handleRemoveAvatar() {
    setError('');
    setSuccess('');

    try {
      setIsBusyAvatar(true);
      await removeCurrentUserAvatar();
      setSuccess('Avatar removed.');
    } catch (removeError) {
      setError(removeError instanceof Error ? removeError.message : 'Failed to remove avatar.');
    } finally {
      setIsBusyAvatar(false);
    }
  }

  async function handleRemoveBanner() {
    setError('');
    setSuccess('');

    try {
      setIsBusyBanner(true);
      await removeCurrentUserBanner();
      setSuccess('Banner removed.');
    } catch (removeError) {
      setError(removeError instanceof Error ? removeError.message : 'Failed to remove banner.');
    } finally {
      setIsBusyBanner(false);
    }
  }

  return (
    <div className="mt-6 grid gap-3">
      <div
        className="overflow-hidden rounded-[1rem] border border-white/8 bg-panel"
        style={
          currentUser.bannerUrl
            ? {
                backgroundImage: `url(${currentUser.bannerUrl})`,
                backgroundSize: 'cover',
                backgroundPosition: 'center'
              }
            : undefined
        }
      >
        <div className="flex h-28 items-end justify-between gap-4 bg-[linear-gradient(135deg,rgba(18,18,24,0.65),rgba(18,18,24,0.2))] px-4 py-4">
          <div className="flex items-center gap-3">
            <AvatarWithStatus
              size="md"
              name={currentUser.displayName}
              accent="aqua"
              status={toSidebarStatus(status)}
              avatarUrl={currentUser.avatarUrl}
            />
            <div className="min-w-0">
              <p className="truncate text-base font-bold text-white">{currentUser.displayName}</p>
              <p className="truncate text-xs text-white/45">@{currentUser.username}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => avatarInputRef.current?.click()}
              disabled={isBusyAvatar}
              className="flex h-9 items-center gap-2 rounded-md bg-black/40 px-3 text-xs font-semibold text-white transition hover:bg-black/60 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Upload className="h-3.5 w-3.5" strokeWidth={2} />
              Avatar
            </button>
            <button
              type="button"
              onClick={() => bannerInputRef.current?.click()}
              disabled={isBusyBanner}
              className="flex h-9 items-center gap-2 rounded-md bg-black/40 px-3 text-xs font-semibold text-white transition hover:bg-black/60 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Upload className="h-3.5 w-3.5" strokeWidth={2} />
              Banner
            </button>
          </div>
        </div>
      </div>

      <input
        ref={avatarInputRef}
        type="file"
        accept="image/png,image/jpeg,image/webp"
        className="hidden"
        onChange={handleAvatarUpload}
      />
      <input
        ref={bannerInputRef}
        type="file"
        accept="image/png,image/jpeg,image/webp"
        className="hidden"
        onChange={handleBannerUpload}
      />

      <div className="grid gap-1.5">
        <label className="text-xs font-semibold uppercase tracking-[0.1em] text-white/45">
          Display name
        </label>
        <input
          value={displayName}
          onChange={(event) => setDisplayName(event.target.value)}
          maxLength={PROFILE_DISPLAY_NAME_MAX_LENGTH}
          className="h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
        />
      </div>

      <div className="grid gap-1.5">
        <label className="text-xs font-semibold uppercase tracking-[0.1em] text-white/45">
          Status
        </label>
        <select
          value={status}
          onChange={(event) => setStatus(event.target.value as CurrentUserProfile['status'])}
          className="h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition focus:border-aqua/35"
        >
          <option value="online">Online</option>
          <option value="idle">Idle</option>
          <option value="dnd">Do not disturb</option>
          <option value="offline">Offline</option>
        </select>
      </div>

      <div className="grid gap-1.5">
        <label className="text-xs font-semibold uppercase tracking-[0.1em] text-white/45">
          Bio
        </label>
        <textarea
          value={bio}
          onChange={(event) => setBio(event.target.value)}
          rows={4}
          placeholder="Tell people what you are up to"
          maxLength={PROFILE_BIO_MAX_LENGTH}
          className="min-h-[6rem] w-full rounded-md border border-transparent bg-input-bg px-4 py-3 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
        />
      </div>

      <div className="grid grid-cols-2 gap-2.5">
        <button
          type="button"
          onClick={onBack}
          className="flex h-11 items-center justify-center rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/70 transition hover:text-white"
        >
          Back
        </button>
        <button
          type="button"
          onClick={() => void handleSave()}
          disabled={isSaving}
          className="flex h-11 items-center justify-center rounded-md bg-aqua text-sm font-bold text-primary-bg transition hover:bg-white disabled:cursor-not-allowed disabled:bg-frame disabled:text-white/25"
        >
          {isSaving ? 'Saving...' : 'Save changes'}
        </button>
      </div>

      <div className="grid grid-cols-2 gap-2.5">
        <button
          type="button"
          onClick={() => void handleRemoveAvatar()}
          disabled={isBusyAvatar || !currentUser.avatarUrl}
          className="flex h-10 items-center justify-center gap-2 rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/70 transition hover:text-white disabled:cursor-not-allowed disabled:opacity-50"
        >
          Remove avatar
        </button>
        <button
          type="button"
          onClick={() => void handleRemoveBanner()}
          disabled={isBusyBanner || !currentUser.bannerUrl}
          className="flex h-10 items-center justify-center gap-2 rounded-md border border-white/10 bg-frame text-sm font-semibold text-white/70 transition hover:text-white disabled:cursor-not-allowed disabled:opacity-50"
        >
          Remove banner
        </button>
      </div>

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
      <div className="rounded-md border border-white/8 bg-panel px-3 py-2 text-xs text-white/40">
        <div className="flex items-center gap-2">
          <ImageIcon className="h-3.5 w-3.5" strokeWidth={1.9} />
          Avatars and banners are stored by the user service. Upload a JPEG, PNG, or WebP file.
        </div>
      </div>
    </div>
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
    <form action={handleSubmit} className="mt-6 grid gap-3">
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
    <div className="mt-6 grid gap-3">
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
};

function Field({ name, label, type, placeholder, autoComplete }: FieldProps) {
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
        placeholder={placeholder}
        autoComplete={autoComplete}
        className="h-11 w-full rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
      />
    </div>
  );
}
