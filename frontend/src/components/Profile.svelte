<script>
  import { onMount } from 'svelte';
  import { fetchUserProfile, saveUserProfile } from '../lib/auth.js';

  let { token, user } = $props();

  let profile  = $state({ firstName: '', lastName: '', phone: '', jobTitle: '', department: '' });
  let saved    = $state(false);
  let error    = $state('');
  let loading  = $state(true);

  onMount(async () => {
    const res = await fetchUserProfile(token);
    if (res.error) { error = res.error; }
    else { profile = { firstName: res.firstName ?? '', lastName: res.lastName ?? '', phone: res.phone ?? '', jobTitle: res.jobTitle ?? '', department: res.department ?? '' }; }
    loading = false;
  });

  async function handleSave() {
    error = '';
    saved = false;
    const res = await saveUserProfile(profile, token);
    if (res.error) { error = res.error; return; }
    profile = { firstName: res.firstName ?? '', lastName: res.lastName ?? '', phone: res.phone ?? '', jobTitle: res.jobTitle ?? '', department: res.department ?? '' };
    saved = true;
    setTimeout(() => saved = false, 3000);
  }

  const ic = 'mt-1.5 w-full rounded-2xl border border-slate-800 bg-slate-950 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-500/20';
  const lc = 'block text-sm font-medium text-white';
</script>

<div class="space-y-6">
  <!-- Avatar header -->
  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <div class="flex items-center gap-5">
      <div class="flex h-16 w-16 items-center justify-center rounded-full bg-sky-500/20 text-2xl font-bold text-sky-400">
        {(profile.firstName || user?.userName || 'U').charAt(0).toUpperCase()}
      </div>
      <div>
        <p class="text-xl font-semibold text-white">
          {profile.firstName || profile.lastName ? `${profile.firstName} ${profile.lastName}`.trim() : user?.userName}
        </p>
        <p class="mt-0.5 text-sm text-slate-400">{user?.email}</p>
        {#if profile.jobTitle}
          <p class="mt-0.5 text-xs text-slate-500">{profile.jobTitle}{profile.department ? ` · ${profile.department}` : ''}</p>
        {/if}
      </div>
    </div>
  </div>

  <!-- Read-only account info -->
  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <h3 class="text-base font-semibold text-white">Account info</h3>
    <p class="mt-1 text-sm text-slate-400">Read-only details from your login.</p>
    <div class="mt-5 grid gap-4 sm:grid-cols-2">
      <div>
        <label for="prof-email" class="block text-sm font-medium text-slate-400">Email</label>
        <input id="prof-email" type="text" value={user?.email} disabled
          class="mt-1.5 w-full rounded-2xl border border-slate-800 bg-slate-950/60 px-4 py-3 text-slate-500 cursor-not-allowed" />
      </div>
      <div>
        <label for="prof-role" class="block text-sm font-medium text-slate-400">Role</label>
        <input id="prof-role" type="text" value={user?.role} disabled
          class="mt-1.5 w-full rounded-2xl border border-slate-800 bg-slate-950/60 px-4 py-3 text-slate-500 cursor-not-allowed" />
      </div>
    </div>
  </div>

  <!-- Editable profile details -->
  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <h3 class="text-base font-semibold text-white">Profile details</h3>
    <p class="mt-1 text-sm text-slate-400">Update your personal details and role information.</p>

    {#if error}
      <div class="mt-4 rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</div>
    {/if}
    {#if saved}
      <div class="mt-4 rounded-2xl bg-emerald-500/10 px-4 py-3 text-sm font-medium text-emerald-400">Profile saved.</div>
    {/if}

    {#if loading}
      <p class="mt-6 text-sm text-slate-400">Loading…</p>
    {:else}
      <div class="mt-5 grid gap-4 sm:grid-cols-2">
        <div>
          <label for="p-first" class={lc}>First name</label>
          <input id="p-first" type="text" class={ic} bind:value={profile.firstName} placeholder="Jane" />
        </div>
        <div>
          <label for="p-last" class={lc}>Last name</label>
          <input id="p-last" type="text" class={ic} bind:value={profile.lastName} placeholder="Smith" />
        </div>
        <div>
          <label for="p-phone" class={lc}>Phone</label>
          <input id="p-phone" type="tel" class={ic} bind:value={profile.phone} placeholder="+44 7700 000000" />
        </div>
        <div>
          <label for="p-job" class={lc}>Job title</label>
          <input id="p-job" type="text" class={ic} bind:value={profile.jobTitle} placeholder="e.g. Senior Underwriter" />
        </div>
        <div class="sm:col-span-2">
          <label for="p-dept" class={lc}>Department</label>
          <input id="p-dept" type="text" class={ic} bind:value={profile.department} placeholder="e.g. Mortgage Originations" />
        </div>
      </div>
      <div class="mt-6 flex justify-end">
        <button onclick={handleSave}
          class="rounded-2xl bg-sky-500 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400">
          Save profile
        </button>
      </div>
    {/if}
  </div>
</div>
