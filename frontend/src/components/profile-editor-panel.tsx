'use client';

import { useEffect, useRef, useState } from 'react';
import type { ChangeEvent } from 'react';
import { Image as ImageIcon, Upload } from 'lucide-react';
import { AvatarWithStatus } from './avatar-with-status';
import { toSidebarStatus, type CurrentUserProfile } from '../shared/mappers/user';
import {
  PROFILE_BIO_MAX_LENGTH,
  PROFILE_DISPLAY_NAME_MAX_LENGTH,
  validateProfileImageFile,
  validateProfileUpdateInput
} from '../shared/lib/validators/profile';

type ProfileEditorPanelProps = {
  currentUser: CurrentUserProfile;
  onBack: () => void;
  onSaveProfile: (payload: {
    display_name?: string;
    bio?: string;
    status?: CurrentUserProfile['status'];
  }) => Promise<void>;
  onUploadAvatar: (file: File) => Promise<void>;
  onRemoveAvatar: () => Promise<void>;
  onUploadBanner: (file: File) => Promise<void>;
  onRemoveBanner: () => Promise<void>;
};

export function ProfileEditorPanel({
  currentUser,
  onBack,
  onSaveProfile,
  onUploadAvatar,
  onRemoveAvatar,
  onUploadBanner,
  onRemoveBanner
}: ProfileEditorPanelProps) {
  const [displayName, setDisplayName] = useState(currentUser.displayName);
  const [bio, setBio] = useState(currentUser.bio ?? '');
  const [status, setStatus] = useState<CurrentUserProfile['status']>(currentUser.status);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [isBusyAvatar, setIsBusyAvatar] = useState(false);
  const [isBusyBanner, setIsBusyBanner] = useState(false);
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const bannerInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    setDisplayName(currentUser.displayName);
    setBio(currentUser.bio ?? '');
    setStatus(currentUser.status);
  }, [currentUser]);

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
      await onSaveProfile(payload);
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
      await onUploadAvatar(file);
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
      await onUploadBanner(file);
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
      await onRemoveAvatar();
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
      await onRemoveBanner();
      setSuccess('Banner removed.');
    } catch (removeError) {
      setError(removeError instanceof Error ? removeError.message : 'Failed to remove banner.');
    } finally {
      setIsBusyBanner(false);
    }
  }

  return (
    <div className="grid w-full min-w-0 gap-3">
      <div
        className="w-full overflow-hidden rounded-[1rem] border border-white/8 bg-panel"
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
        <div className="flex flex-col gap-4 bg-[linear-gradient(135deg,rgba(18,18,24,0.65),rgba(18,18,24,0.2))] px-4 py-4 xl:h-28 xl:flex-row xl:items-end xl:justify-between">
          <div className="flex min-w-0 items-center gap-3">
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
          <div className="grid grid-cols-2 gap-2 sm:max-w-full xl:flex xl:flex-nowrap xl:items-center">
            <button
              type="button"
              onClick={() => avatarInputRef.current?.click()}
              disabled={isBusyAvatar}
              className="flex h-9 min-w-0 items-center justify-center gap-2 rounded-md bg-black/40 px-3 text-xs font-semibold text-white transition hover:bg-black/60 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Upload className="h-3.5 w-3.5" strokeWidth={2} />
              Avatar
            </button>
            <button
              type="button"
              onClick={() => bannerInputRef.current?.click()}
              disabled={isBusyBanner}
              className="flex h-9 min-w-0 items-center justify-center gap-2 rounded-md bg-black/40 px-3 text-xs font-semibold text-white transition hover:bg-black/60 disabled:cursor-not-allowed disabled:opacity-50"
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

      <div className="grid min-w-0 gap-1.5">
        <label className="text-xs font-semibold uppercase tracking-[0.1em] text-white/45">
          Display name
        </label>
        <input
          value={displayName}
          onChange={(event) => setDisplayName(event.target.value)}
          maxLength={PROFILE_DISPLAY_NAME_MAX_LENGTH}
          className="h-11 w-full min-w-0 rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
        />
      </div>

      <div className="grid min-w-0 gap-1.5">
        <label className="text-xs font-semibold uppercase tracking-[0.1em] text-white/45">
          Status
        </label>
        <select
          value={status}
          onChange={(event) => setStatus(event.target.value as CurrentUserProfile['status'])}
          className="h-11 w-full min-w-0 rounded-md border border-transparent bg-input-bg px-4 text-base text-white outline-none transition focus:border-aqua/35"
        >
          <option value="online">Online</option>
          <option value="idle">Idle</option>
          <option value="dnd">Do not disturb</option>
          <option value="offline">Offline</option>
        </select>
      </div>

      <div className="grid min-w-0 gap-1.5">
        <label className="text-xs font-semibold uppercase tracking-[0.1em] text-white/45">
          Bio
        </label>
        <textarea
          value={bio}
          onChange={(event) => setBio(event.target.value)}
          rows={4}
          placeholder="Tell people what you are up to"
          maxLength={PROFILE_BIO_MAX_LENGTH}
          className="min-h-[6rem] w-full min-w-0 rounded-md border border-transparent bg-input-bg px-4 py-3 text-base text-white outline-none transition placeholder:text-input-placeholder focus:border-aqua/35"
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
