'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Image as ImageIcon, LogOut, Mail, Settings, ShieldCheck, Trash2, X } from 'lucide-react';
import { deleteAccount, getIdentity, updateIdentity, type AuthIdentity } from '../shared/api/auth';
import { clearSession } from '../shared/lib/session';
import { describeAccountUpdateError, describeDeleteAccountError } from '../shared/lib/auth-errors';
import { checkEmail, checkPassword } from '../shared/lib/validators/auth';
import { useCloseOnEscape } from '../shared/hooks/use-close-on-escape';
import { toSidebarStatus, type CurrentUserProfile } from '../shared/mappers/user';
import { AvatarWithStatus } from './avatar-with-status';
import { ProfileEditorPanel } from './profile-editor-panel';
import { useCurrentUserProfile } from '../shared/user/user-store';

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
      <section className="relative w-full max-w-[72rem] overflow-hidden rounded-[1rem] bg-secondary-bg shadow-2xl shadow-black/50 ring-1 ring-white/10">
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

        <div className="grid gap-5 px-5 py-5 lg:grid-cols-[20rem_minmax(0,1fr)] lg:items-start">
          <aside className="grid min-w-0 gap-3">
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

            <div className="grid gap-2.5 rounded-[1rem] border border-white/8 bg-panel p-3 sm:grid-cols-2 lg:grid-cols-1">
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
                <p className="sm:col-span-2 text-xs text-white/35">
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
                className="flex h-9 w-full items-center justify-center gap-2 rounded-md text-sm font-semibold text-white/40 transition hover:text-pink sm:col-span-2"
              >
                <Trash2 className="h-4 w-4" strokeWidth={1.9} />
                Delete account
              </button>
            </div>
          </aside>

          <div className="min-w-0">
            {panel === 'profile' ? (
              <ProfileEditorPanel
                currentUser={currentUser}
                onBack={() => setPanel('menu')}
                onSaveProfile={updateCurrentUserProfile}
                onUploadAvatar={uploadCurrentUserAvatar}
                onRemoveAvatar={removeCurrentUserAvatar}
                onUploadBanner={uploadCurrentUserBanner}
                onRemoveBanner={removeCurrentUserBanner}
              />
            ) : null}

            {panel === 'credentials' ? (
              <CredentialsForm
                onBack={() => setPanel('menu')}
                currentEmail={identity?.email ?? ''}
              />
            ) : null}

            {panel === 'delete' ? (
              <DeleteAccountPanel
                onBack={() => setPanel('menu')}
                onDeleted={handleAccountDeleted}
              />
            ) : null}
          </div>
        </div>
      </section>
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
