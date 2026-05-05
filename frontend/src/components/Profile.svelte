<script>
  import { onMount } from 'svelte';
  import { fetchUserProfile, saveUserProfile } from '../lib/auth.js';

  let { token, user } = $props();

  let profile = $state({ displayName: '', jobTitle: '', department: '' });
  let saved = $state(false);
  let error = $state('');
  let loading = $state(true);

  onMount(async () => {
    const response = await fetchUserProfile(token);
    if (response.error) {
      error = response.error;
    } else {
      profile = response;
    }
    loading = false;
  });

  async function handleSave() {
    error = '';
    saved = false;
    const response = await saveUserProfile(profile, token);
    if (response.error) {
      error = response.error;
      return;
    }
    profile = response;
    saved = true;
    setTimeout(() => saved = false, 3000);
  }
</script>

<div class="space-y-6">
  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <div class="flex items-center gap-5">
      <div class="flex h-16 w-16 items-center justify-center rounded-full bg-sky-500/20 text-2xl font-bold text-sky-400">
        {(profile.displayName || user?.userName || 'A').charAt(0).toUpperCase()}
      </div>
      <div>
        <p class="text-xl font-semibold text-white">{profile.displayName || user?.userName}</p>
        <p class="mt-1 text-sm text-slate-400">{user?.email}</p>
      </div>
    </div>
  </div>

  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <h3 class="text-lg font-semibold text-white">Account info</h3>
    <p class="mt-1 text-sm text-slate-400">Read-only details from your login.</p>
    <div class="mt-5 grid gap-4 sm:grid-cols-2">
      <div>
        <label for="profile-username" class="block text-sm font-medium text-slate-400">Username</label>
        <input
          id="profile-username"
          type="text"
          value={user?.userName}
          disabled
          class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950/60 px-4 py-3 text-slate-500 cursor-not-allowed"
        />
      </div>
      <div>
        <label for="profile-email" class="block text-sm font-medium text-slate-400">Email</label>
        <input
          id="profile-email"
          type="text"
          value={user?.email}
          disabled
          class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950/60 px-4 py-3 text-slate-500 cursor-not-allowed"
        />
      </div>
    </div>
  </div>

  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <h3 class="text-lg font-semibold text-white">Profile details</h3>
    <p class="mt-1 text-sm text-slate-400">Update your display name, role, and team.</p>

    {#if error}
      <div class="mt-4 rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</div>
    {/if}
    {#if saved}
      <div class="mt-4 rounded-2xl bg-green-500/10 px-4 py-3 text-sm font-medium text-green-400">Profile saved successfully.</div>
    {/if}

    {#if loading}
      <p class="mt-6 text-sm text-slate-400">Loading...</p>
    {:else}
      <div class="mt-5 grid gap-4 sm:grid-cols-2">
        <div class="sm:col-span-2">
          <label for="displayName" class="block text-sm font-medium text-white">Display name</label>
          <input
            id="displayName"
            type="text"
            bind:value={profile.displayName}
            placeholder="Your full name"
            class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-500/20"
          />
        </div>
        <div>
          <label for="jobTitle" class="block text-sm font-medium text-white">Job title</label>
          <input
            id="jobTitle"
            type="text"
            bind:value={profile.jobTitle}
            placeholder="e.g. Senior Underwriter"
            class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-500/20"
          />
        </div>
        <div>
          <label for="department" class="block text-sm font-medium text-white">Department</label>
          <input
            id="department"
            type="text"
            bind:value={profile.department}
            placeholder="e.g. Mortgage Originations"
            class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-500/20"
          />
        </div>
      </div>
      <div class="mt-6 flex justify-end">
        <button
          onclick={handleSave}
          class="rounded-2xl bg-sky-500 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400"
        >
          Save profile
        </button>
      </div>
    {/if}
  </div>
</div>
