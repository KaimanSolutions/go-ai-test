<script>
  import { onMount } from 'svelte';
  import { fetchUsers, adminCreateUser, adminDeleteUser, setUserLockout, fetchCompanies, fetchUserById } from '../lib/auth.js';

  let { token = '' } = $props();

  let allUsers   = $state([]);
  let companies  = $state([]);
  let loading    = $state(true);
  let error      = $state('');
  let activeTab  = $state('Broker');
  let search     = $state('');

  // Detail panel
  let detailUser   = $state(null);
  let detailLoading = $state(false);

  async function openUser(id) {
    detailLoading = true;
    detailUser    = null;
    const res = await fetchUserById(id, token);
    detailLoading = false;
    if (!res.error) detailUser = res;
  }

  // Modal state
  let showModal  = $state(false);
  let saving     = $state(false);
  let modalError = $state('');
  let form = $state({
    firstName: '', lastName: '', email: '', password: '',
    phone: '', role: 'Admin', companyId: '', licenseNumber: ''
  });

  const tabs = [
    { key: 'Broker', label: 'Brokers' },
    { key: 'Client', label: 'Clients' },
    { key: 'Admin',  label: 'Admins'  }
  ];

  const tabStyle = {
    Admin:  { active: 'bg-slate-500/10 text-slate-300',   badge: 'bg-slate-500/20 text-slate-400',   avatar: 'bg-slate-500/20 text-slate-300',   ring: 'border-slate-500/30'  },
    Broker: { active: 'bg-violet-500/10 text-violet-400', badge: 'bg-violet-500/20 text-violet-400', avatar: 'bg-violet-500/20 text-violet-400', ring: 'border-violet-500/30' },
    Client: { active: 'bg-sky-500/10 text-sky-400',       badge: 'bg-sky-500/20 text-sky-400',       avatar: 'bg-sky-500/20 text-sky-400',       ring: 'border-sky-500/30'    }
  };

  onMount(async () => {
    await Promise.all([load(), loadCompanies()]);
  });

  async function load() {
    loading = true;
    error   = '';
    const result = await fetchUsers(token);
    loading = false;
    if (result?.error) { error = result.error; return; }
    allUsers = Object.entries(result ?? {}).flatMap(([, list]) => list);
  }

  async function loadCompanies() {
    const res = await fetchCompanies();
    if (!res.error) companies = res;
  }

  function openModal() {
    form = { firstName: '', lastName: '', email: '', password: '', phone: '', role: 'Admin', companyId: '', licenseNumber: '' };
    modalError = '';
    showModal  = true;
  }

  async function submitCreate() {
    modalError = '';
    if (!form.firstName || !form.lastName || !form.email || !form.password) {
      modalError = 'First name, last name, email and password are required.';
      return;
    }
    if (form.role === 'Broker' && !form.companyId) {
      modalError = 'Please select a company for the broker.';
      return;
    }
    saving = true;
    const res = await adminCreateUser({
      firstName:     form.firstName,
      lastName:      form.lastName,
      email:         form.email,
      password:      form.password,
      phone:         form.phone,
      role:          form.role,
      companyId:     form.role === 'Broker' ? Number(form.companyId) : null,
      licenseNumber: form.licenseNumber || null
    }, token);
    saving = false;
    if (res.error) { modalError = res.error; return; }
    showModal = false;
    activeTab = form.role;
    await load();
  }

  async function toggleLockout(u) {
    const res = await setUserLockout(u.id, !u.isLockedOut, token);
    if (res.error) { error = res.error; return; }
    await load();
  }

  async function deleteUser(id, name) {
    if (!confirm(`Delete ${name}? This cannot be undone.`)) return;
    const res = await adminDeleteUser(id, token);
    if (res.error) { error = res.error; return; }
    await load();
  }

  function matches(u, q) {
    if (!q) return true;
    const lq = q.toLowerCase();
    return (
      `${u.firstName} ${u.lastName}`.toLowerCase().includes(lq) ||
      u.email?.toLowerCase().includes(lq)                        ||
      u.phone?.toLowerCase().includes(lq)                        ||
      u.companyName?.toLowerCase().includes(lq)                  ||
      u.licenseNumber?.toLowerCase().includes(lq)
    );
  }

  const q        = $derived(search.trim());
  const tabUsers = $derived(allUsers.filter(u => u.role === activeTab && matches(u, q)));

  function countOf(role) {
    return allUsers.filter(u => u.role === role && matches(u, q)).length;
  }

  function initials(u) {
    const f = u.firstName?.charAt(0) ?? '';
    const l = u.lastName?.charAt(0)  ?? '';
    return (f + l).toUpperCase() || u.userName?.charAt(0)?.toUpperCase() || '?';
  }

  function joined(dateStr) {
    return new Date(dateStr).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }
</script>

<!-- ── Modal ─────────────────────────────────────────────────────────────────── -->
{#if showModal}
  <div
    class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm"
    onclick={(e) => { if (e.target === e.currentTarget) showModal = false; }}
  >
    <div class="w-full max-w-md rounded-3xl border border-slate-700 bg-slate-900 p-6 shadow-2xl">
      <div class="flex items-center justify-between">
        <h3 class="text-base font-semibold text-white">Add user</h3>
        <button
          onclick={() => showModal = false}
          class="flex h-8 w-8 items-center justify-center rounded-xl text-slate-500 transition hover:bg-slate-800 hover:text-white"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
          </svg>
        </button>
      </div>

      {#if modalError}
        <p class="mt-4 rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{modalError}</p>
      {/if}

      <div class="mt-5 space-y-4">
        <!-- Role -->
        <div>
          <label class="block text-xs font-medium text-slate-400 mb-1.5">Role</label>
          <div class="grid grid-cols-3 gap-2">
            {#each ['Admin', 'Broker', 'Client'] as r}
              <button
                onclick={() => form.role = r}
                class="rounded-xl border py-2 text-xs font-semibold transition
                  {form.role === r
                    ? r === 'Admin'  ? 'border-slate-500 bg-slate-500/10 text-slate-300'
                    : r === 'Broker' ? 'border-violet-500/60 bg-violet-500/10 text-violet-400'
                    :                  'border-sky-500/60 bg-sky-500/10 text-sky-400'
                    : 'border-slate-700 text-slate-500 hover:border-slate-600 hover:text-slate-300'}"
              >{r}</button>
            {/each}
          </div>
        </div>

        <!-- Name row -->
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-xs font-medium text-slate-400 mb-1.5">First name <span class="text-red-400">*</span></label>
            <input bind:value={form.firstName} placeholder="Jane"
              class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none" />
          </div>
          <div>
            <label class="block text-xs font-medium text-slate-400 mb-1.5">Last name <span class="text-red-400">*</span></label>
            <input bind:value={form.lastName} placeholder="Smith"
              class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none" />
          </div>
        </div>

        <!-- Email -->
        <div>
          <label class="block text-xs font-medium text-slate-400 mb-1.5">Email <span class="text-red-400">*</span></label>
          <input bind:value={form.email} type="email" placeholder="jane@example.com"
            class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none" />
        </div>

        <!-- Password -->
        <div>
          <label class="block text-xs font-medium text-slate-400 mb-1.5">Password <span class="text-red-400">*</span></label>
          <input bind:value={form.password} type="password" placeholder="Min. 8 characters"
            class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none" />
        </div>

        <!-- Phone -->
        <div>
          <label class="block text-xs font-medium text-slate-400 mb-1.5">Phone</label>
          <input bind:value={form.phone} type="tel" placeholder="+44 7700 900000"
            class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none" />
        </div>

        <!-- Company (Broker only) -->
        {#if form.role === 'Broker'}
          <div>
            <label class="block text-xs font-medium text-slate-400 mb-1.5">Company <span class="text-red-400">*</span></label>
            <select bind:value={form.companyId}
              class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none">
              <option value="">Select company…</option>
              {#each companies as c}
                <option value={c.id}>{c.name}</option>
              {/each}
            </select>
          </div>
          <div>
            <label class="block text-xs font-medium text-slate-400 mb-1.5">Licence number</label>
            <input bind:value={form.licenseNumber} placeholder="Optional"
              class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none" />
          </div>
        {/if}
      </div>

      <div class="mt-6 flex gap-3">
        <button
          onclick={() => showModal = false}
          class="flex-1 rounded-2xl border border-slate-700 py-2.5 text-sm font-semibold text-slate-300 transition hover:bg-slate-800"
        >Cancel</button>
        <button
          onclick={submitCreate}
          disabled={saving}
          class="flex-1 rounded-2xl bg-sky-500 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400 disabled:opacity-50"
        >{saving ? 'Creating…' : 'Create user'}</button>
      </div>
    </div>
  </div>
{/if}

<!-- ── User detail panel ─────────────────────────────────────────────────────── -->
{#if detailUser || detailLoading}
  <div
    class="fixed inset-0 z-40 flex justify-end bg-black/50 backdrop-blur-sm"
    onclick={(e) => { if (e.target === e.currentTarget) detailUser = null; }}
  >
    <div class="flex h-full w-full max-w-md flex-col overflow-y-auto border-l border-slate-700 bg-slate-900 shadow-2xl">

      <!-- Panel header -->
      <div class="flex shrink-0 items-center justify-between border-b border-slate-800 px-6 py-5">
        <h3 class="text-base font-semibold text-white">User details</h3>
        <button
          onclick={() => detailUser = null}
          class="flex h-8 w-8 items-center justify-center rounded-xl text-slate-500 transition hover:bg-slate-800 hover:text-white"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
          </svg>
        </button>
      </div>

      {#if detailLoading}
        <div class="flex flex-1 items-center justify-center">
          <div class="h-8 w-8 animate-spin rounded-full border-2 border-slate-700 border-t-sky-500"></div>
        </div>
      {:else if detailUser}
        {@const u = detailUser}
        {@const style = tabStyle[u.role] ?? tabStyle['Admin']}

        <!-- Avatar + name -->
        <div class="border-b border-slate-800 px-6 py-6">
          <div class="flex items-center gap-4">
            <div class="flex h-14 w-14 shrink-0 items-center justify-center rounded-full {style.avatar} text-xl font-bold">
              {initials(u)}
            </div>
            <div>
              <p class="text-lg font-semibold text-white">{u.firstName} {u.lastName}</p>
              <a href="mailto:{u.email}" class="text-sm text-sky-400 hover:underline">{u.email}</a>
              <div class="mt-1.5 flex items-center gap-2">
                <span class="rounded-full px-2.5 py-0.5 text-xs font-semibold {style.active}">{u.role}</span>
                {#if u.isLockedOut}
                  <span class="rounded-full bg-red-500/15 px-2.5 py-0.5 text-xs font-semibold text-red-400">Locked</span>
                {/if}
              </div>
            </div>
          </div>
        </div>

        <!-- Details -->
        <div class="flex-1 space-y-6 px-6 py-6">

          <section>
            <p class="mb-3 text-xs font-semibold uppercase tracking-wider text-slate-500">Contact</p>
            <dl class="space-y-2.5 text-sm">
              {#if u.phone}
                <div class="flex justify-between">
                  <dt class="text-slate-500">Phone</dt>
                  <dd><a href="tel:{u.phone}" class="text-slate-200 hover:text-white">{u.phone}</a></dd>
                </div>
              {/if}
              <div class="flex justify-between">
                <dt class="text-slate-500">Joined</dt>
                <dd class="text-slate-200">{joined(u.createdAt)}</dd>
              </div>
            </dl>
          </section>

          {#if u.jobTitle || u.department}
            <section>
              <p class="mb-3 text-xs font-semibold uppercase tracking-wider text-slate-500">Role</p>
              <dl class="space-y-2.5 text-sm">
                {#if u.jobTitle}
                  <div class="flex justify-between">
                    <dt class="text-slate-500">Job title</dt>
                    <dd class="text-slate-200">{u.jobTitle}</dd>
                  </div>
                {/if}
                {#if u.department}
                  <div class="flex justify-between">
                    <dt class="text-slate-500">Department</dt>
                    <dd class="text-slate-200">{u.department}</dd>
                  </div>
                {/if}
              </dl>
            </section>
          {/if}

          {#if u.companyName || u.licenseNumber}
            <section>
              <p class="mb-3 text-xs font-semibold uppercase tracking-wider text-slate-500">Company</p>
              <dl class="space-y-2.5 text-sm">
                {#if u.companyName}
                  <div class="flex justify-between">
                    <dt class="text-slate-500">Company</dt>
                    <dd class="text-slate-200">{u.companyName}</dd>
                  </div>
                {/if}
                {#if u.licenseNumber}
                  <div class="flex justify-between">
                    <dt class="text-slate-500">Licence</dt>
                    <dd class="font-mono text-slate-200">{u.licenseNumber}</dd>
                  </div>
                {/if}
              </dl>
            </section>
          {/if}
        </div>

        <!-- Actions -->
        <div class="shrink-0 border-t border-slate-800 px-6 py-4 space-y-2">
          <button
            onclick={async () => { await toggleLockout(u); await openUser(u.id); }}
            class="flex w-full items-center gap-3 rounded-2xl border px-4 py-2.5 text-sm font-semibold transition
              {u.isLockedOut
                ? 'border-amber-500/30 bg-amber-500/5 text-amber-400 hover:bg-amber-500/10'
                : 'border-slate-700 text-slate-300 hover:bg-slate-800'}"
          >
            <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
              {#if u.isLockedOut}
                <path fill-rule="evenodd" d="M14.5 1A4.5 4.5 0 0010 5.5V9H3a2 2 0 00-2 2v6a2 2 0 002 2h10a2 2 0 002-2v-6a2 2 0 00-2-2h-1.5V5.5a3 3 0 116 0v2.75a.75.75 0 001.5 0V5.5A4.5 4.5 0 0014.5 1zm-5 12a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/>
              {:else}
                <path fill-rule="evenodd" d="M10 1a4.5 4.5 0 00-4.5 4.5V9H5a2 2 0 00-2 2v6a2 2 0 002 2h10a2 2 0 002-2v-6a2 2 0 00-2-2h-.5V5.5A4.5 4.5 0 0010 1zm3 8V5.5a3 3 0 10-6 0V9h6zm-3 4a1 1 0 100 2 1 1 0 000-2z" clip-rule="evenodd"/>
              {/if}
            </svg>
            {u.isLockedOut ? 'Unlock account' : 'Lock out account'}
          </button>
          <button
            onclick={async () => { await deleteUser(u.id, `${u.firstName} ${u.lastName}`); detailUser = null; }}
            class="flex w-full items-center gap-3 rounded-2xl border border-red-500/20 bg-red-500/5 px-4 py-2.5 text-sm font-semibold text-red-400 transition hover:bg-red-500/10"
          >
            <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z" clip-rule="evenodd"/>
            </svg>
            Delete account
          </button>
        </div>
      {/if}

    </div>
  </div>
{/if}

<!-- ── Main ───────────────────────────────────────────────────────────────────── -->
<div class="space-y-5">

  <!-- Header -->
  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h3 class="text-base font-semibold text-white">User accounts</h3>
        <p class="mt-1 text-sm text-slate-400">View and manage registered users by role.</p>
      </div>
      <div class="flex shrink-0 gap-2">
        <button
          onclick={load}
          class="inline-flex items-center gap-2 rounded-2xl border border-slate-700 px-4 py-2.5 text-sm font-semibold text-slate-300 transition hover:bg-slate-800"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M15.312 11.424a5.5 5.5 0 01-9.201 2.466l-.312-.311h2.433a.75.75 0 000-1.5H3.989a.75.75 0 00-.75.75v4.242a.75.75 0 001.5 0v-2.43l.31.31a7 7 0 0011.712-3.138.75.75 0 00-1.449-.39zm1.23-3.723a.75.75 0 00.219-.53V2.929a.75.75 0 00-1.5 0v2.43l-.31-.31A7 7 0 003.239 8.188a.75.75 0 101.448.389A5.5 5.5 0 0113.89 6.11l.311.31h-2.432a.75.75 0 000 1.5h4.243a.75.75 0 00.53-.219z" clip-rule="evenodd"/>
          </svg>
          Refresh
        </button>
        <button
          onclick={openModal}
          class="inline-flex items-center gap-2 rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
          </svg>
          Add user
        </button>
      </div>
    </div>

    <!-- Search -->
    <div class="mt-4 relative">
      <svg class="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd"/>
      </svg>
      <input
        type="text"
        bind:value={search}
        placeholder="Search by name, email, phone, company or licence…"
        class="w-full rounded-2xl border border-slate-800 bg-slate-950/60 py-2.5 pl-10 pr-4 text-sm text-slate-100 outline-none transition placeholder:text-slate-600 focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20"
      />
      {#if search}
        <button onclick={() => search = ''} class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-300 transition">
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
          </svg>
        </button>
      {/if}
    </div>

    <!-- Tabs -->
    <div class="mt-4 flex gap-1 rounded-2xl border border-slate-800 bg-slate-950/60 p-1">
      {#each tabs as tab}
        {@const style = tabStyle[tab.key]}
        <button
          onclick={() => activeTab = tab.key}
          class="flex flex-1 items-center justify-center gap-2 rounded-xl py-2 text-sm font-semibold transition
            {activeTab === tab.key ? style.active : 'text-slate-400 hover:text-white'}"
        >
          {tab.label}
          <span class="rounded-full px-1.5 py-0.5 text-xs
            {activeTab === tab.key ? style.badge : 'bg-slate-800 text-slate-500'}">
            {countOf(tab.key)}
          </span>
        </button>
      {/each}
    </div>
  </div>

  <!-- Error -->
  {#if error}
    <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</p>
  {/if}

  <!-- Content -->
  {#if loading}
    <div class="flex items-center justify-center py-24">
      <div class="h-8 w-8 animate-spin rounded-full border-2 border-slate-700 border-t-sky-500"></div>
    </div>

  {:else if tabUsers.length === 0}
    <div class="flex flex-col items-center justify-center gap-3 py-20 text-center">
      <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-slate-800">
        <svg class="h-6 w-6 text-slate-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
          <path stroke-linecap="round" stroke-linejoin="round" d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z"/>
        </svg>
      </div>
      {#if q}
        <p class="text-sm font-medium text-slate-400">No {activeTab.toLowerCase()}s match <span class="text-white">"{search}"</span></p>
        <button onclick={() => search = ''} class="text-xs text-sky-400 hover:underline">Clear search</button>
      {:else}
        <p class="text-sm font-medium text-slate-400">No {activeTab.toLowerCase()}s registered yet.</p>
        <button onclick={openModal} class="mt-2 rounded-2xl bg-sky-500 px-4 py-2 text-xs font-semibold text-white hover:bg-sky-400 transition">Add {activeTab.toLowerCase()}</button>
      {/if}
    </div>

  {:else}
    {@const style = tabStyle[activeTab]}
    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {#each tabUsers as u (u.id)}
        <div class="flex flex-col gap-4 rounded-3xl border {style.ring} bg-slate-900/95 p-5 shadow-xl transition hover:border-slate-600">

          <!-- Avatar + name (clickable) -->
          <button class="flex items-start gap-3 text-left" onclick={() => openUser(u.id)}>
            <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full {style.avatar} text-sm font-bold">
              {initials(u)}
            </div>
            <div class="min-w-0 flex-1">
              <p class="truncate font-semibold text-white">{u.firstName} {u.lastName}</p>
              <p class="truncate text-xs text-sky-400">{u.email}</p>
            </div>
          </button>

          <!-- Details -->
          <div class="space-y-1.5 text-xs">
            {#if u.phone}
              <div class="flex items-center gap-2 text-slate-400">
                <svg class="h-3.5 w-3.5 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M2 3.5A1.5 1.5 0 013.5 2h1.148a1.5 1.5 0 011.465 1.175l.716 3.223a1.5 1.5 0 01-1.052 1.767l-.933.267c-.41.117-.643.555-.48.95a11.542 11.542 0 006.254 6.254c.395.163.833-.07.95-.48l.267-.933a1.5 1.5 0 011.767-1.052l3.223.716A1.5 1.5 0 0118 15.352V16.5a1.5 1.5 0 01-1.5 1.5H15c-1.149 0-2.263-.15-3.326-.43A13.022 13.022 0 012.43 8.326 13.019 13.019 0 012 5V3.5z" clip-rule="evenodd"/>
                </svg>
                <a href="tel:{u.phone}" class="hover:text-slate-200 transition">{u.phone}</a>
              </div>
            {/if}
            {#if u.companyName}
              <div class="flex items-center gap-2 text-slate-400">
                <svg class="h-3.5 w-3.5 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M4 16.5v-13h-.25a.75.75 0 010-1.5h12.5a.75.75 0 010 1.5H16v13h.25a.75.75 0 010 1.5h-3.5a.75.75 0 01-.75-.75v-2.5a.75.75 0 00-.75-.75h-2.5a.75.75 0 00-.75.75v2.5a.75.75 0 01-.75.75h-3.5a.75.75 0 010-1.5H4zm3-11a.75.75 0 01.75-.75h.5a.75.75 0 010 1.5h-.5A.75.75 0 017 5.5zm.75 2.25a.75.75 0 000 1.5h.5a.75.75 0 000-1.5h-.5zm-.75 4a.75.75 0 01.75-.75h.5a.75.75 0 010 1.5h-.5a.75.75 0 01-.75-.75zm5.25-6a.75.75 0 000 1.5h.5a.75.75 0 000-1.5h-.5zm-.75 4a.75.75 0 01.75-.75h.5a.75.75 0 010 1.5h-.5a.75.75 0 01-.75-.75z" clip-rule="evenodd"/>
                </svg>
                <span class="truncate">{u.companyName}</span>
              </div>
            {/if}
            {#if u.licenseNumber}
              <div class="flex items-center gap-2 text-slate-400">
                <svg class="h-3.5 w-3.5 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M4.5 2A1.5 1.5 0 003 3.5v13A1.5 1.5 0 004.5 18h11a1.5 1.5 0 001.5-1.5V7.621a1.5 1.5 0 00-.44-1.06l-4.12-4.122A1.5 1.5 0 0011.378 2H4.5zm2.25 8.5a.75.75 0 000 1.5h6.5a.75.75 0 000-1.5h-6.5zm0 3a.75.75 0 000 1.5h6.5a.75.75 0 000-1.5h-6.5zm0-6a.75.75 0 000 1.5H9a.75.75 0 000-1.5H6.75z" clip-rule="evenodd"/>
                </svg>
                <span class="font-mono">{u.licenseNumber}</span>
              </div>
            {/if}
          </div>

          <!-- Footer -->
          <div class="flex items-center justify-between border-t border-slate-800 pt-3">
            <div class="flex items-center gap-2">
              <span class="text-xs text-slate-500">Joined {joined(u.createdAt)}</span>
              {#if u.isLockedOut}
                <span class="rounded-full bg-red-500/15 px-2 py-0.5 text-xs font-semibold text-red-400">Locked</span>
              {/if}
            </div>
            <div class="flex items-center gap-1">
              <span class="rounded-full px-2 py-0.5 text-xs font-medium {style.active}">{u.role}</span>
              <!-- Lock / unlock -->
              <button
                onclick={() => toggleLockout(u)}
                class="flex h-6 w-6 items-center justify-center rounded-lg transition
                  {u.isLockedOut
                    ? 'text-amber-400 hover:bg-amber-500/10'
                    : 'text-slate-600 hover:bg-slate-700 hover:text-slate-300'}"
                title={u.isLockedOut ? 'Unlock account' : 'Lock out account'}
              >
                {#if u.isLockedOut}
                  <!-- Unlocked padlock -->
                  <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M14.5 1A4.5 4.5 0 0010 5.5V9H3a2 2 0 00-2 2v6a2 2 0 002 2h10a2 2 0 002-2v-6a2 2 0 00-2-2h-1.5V5.5a3 3 0 116 0v2.75a.75.75 0 001.5 0V5.5A4.5 4.5 0 0014.5 1zm-5 12a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/>
                  </svg>
                {:else}
                  <!-- Locked padlock -->
                  <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M10 1a4.5 4.5 0 00-4.5 4.5V9H5a2 2 0 00-2 2v6a2 2 0 002 2h10a2 2 0 002-2v-6a2 2 0 00-2-2h-.5V5.5A4.5 4.5 0 0010 1zm3 8V5.5a3 3 0 10-6 0V9h6zm-3 4a1 1 0 100 2 1 1 0 000-2z" clip-rule="evenodd"/>
                  </svg>
                {/if}
              </button>
              <!-- Delete -->
              <button
                onclick={() => deleteUser(u.id, `${u.firstName} ${u.lastName}`)}
                class="flex h-6 w-6 items-center justify-center rounded-lg text-slate-600 transition hover:bg-red-500/10 hover:text-red-400"
                title="Delete user"
              >
                <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M8.75 1A2.75 2.75 0 006 3.75v.443c-.795.077-1.584.176-2.365.298a.75.75 0 10.23 1.482l.149-.022.841 10.518A2.75 2.75 0 007.596 19h4.807a2.75 2.75 0 002.742-2.53l.841-10.52.149.023a.75.75 0 00.23-1.482A41.03 41.03 0 0014 4.193V3.75A2.75 2.75 0 0011.25 1h-2.5zM10 4c.84 0 1.673.025 2.5.075V3.75c0-.69-.56-1.25-1.25-1.25h-2.5c-.69 0-1.25.56-1.25 1.25v.325C8.327 4.025 9.16 4 10 4zM8.58 7.72a.75.75 0 00-1.5.06l.3 7.5a.75.75 0 101.5-.06l-.3-7.5zm4.34.06a.75.75 0 10-1.5-.06l-.3 7.5a.75.75 0 101.5.06l.3-7.5z" clip-rule="evenodd"/>
                </svg>
              </button>
            </div>
          </div>
        </div>
      {/each}
    </div>
  {/if}

</div>
