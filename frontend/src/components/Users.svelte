<script>
  import { onMount } from 'svelte';
  import { fetchUsers } from '../lib/auth.js';

  let { token = '' } = $props();

  let allUsers   = $state([]);
  let loading    = $state(true);
  let error      = $state('');
  let activeTab  = $state('Broker');
  let search     = $state('');

  const tabs = [
    { key: 'Broker', label: 'Brokers' },
    { key: 'Client', label: 'Clients' },
    { key: 'Admin',  label: 'Admins'  }
  ];

  const tabStyle = {
    Admin:  { active: 'bg-slate-500/10 text-slate-300',  badge: 'bg-slate-500/20 text-slate-400',  avatar: 'bg-slate-500/20 text-slate-300',  ring: 'border-slate-500/30' },
    Broker: { active: 'bg-violet-500/10 text-violet-400', badge: 'bg-violet-500/20 text-violet-400', avatar: 'bg-violet-500/20 text-violet-400', ring: 'border-violet-500/30' },
    Client: { active: 'bg-sky-500/10 text-sky-400',      badge: 'bg-sky-500/20 text-sky-400',      avatar: 'bg-sky-500/20 text-sky-400',      ring: 'border-sky-500/30'    }
  };

  onMount(load);

  async function load() {
    loading = true;
    error   = '';
    const result = await fetchUsers(token);
    loading = false;
    if (result?.error) { error = result.error; return; }
    allUsers = Object.entries(result ?? {}).flatMap(([, list]) => list);
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

<div class="space-y-5">

  <!-- Header -->
  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h3 class="text-base font-semibold text-white">User accounts</h3>
        <p class="mt-1 text-sm text-slate-400">View and manage registered users by role.</p>
      </div>
      <button
        onclick={load}
        class="inline-flex shrink-0 items-center gap-2 rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400"
      >
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M15.312 11.424a5.5 5.5 0 01-9.201 2.466l-.312-.311h2.433a.75.75 0 000-1.5H3.989a.75.75 0 00-.75.75v4.242a.75.75 0 001.5 0v-2.43l.31.31a7 7 0 0011.712-3.138.75.75 0 00-1.449-.39zm1.23-3.723a.75.75 0 00.219-.53V2.929a.75.75 0 00-1.5 0v2.43l-.31-.31A7 7 0 003.239 8.188a.75.75 0 101.448.389A5.5 5.5 0 0113.89 6.11l.311.31h-2.432a.75.75 0 000 1.5h4.243a.75.75 0 00.53-.219z" clip-rule="evenodd"/>
        </svg>
        Refresh
      </button>
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
        <button
          onclick={() => search = ''}
          class="absolute right-3.5 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-300 transition"
        >
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
      {/if}
    </div>

  {:else}
    {@const style = tabStyle[activeTab]}
    <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {#each tabUsers as u (u.id)}
        <div class="flex flex-col gap-4 rounded-3xl border {style.ring} bg-slate-900/95 p-5 shadow-xl">

          <!-- Avatar + name -->
          <div class="flex items-start gap-3">
            <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full {style.avatar} text-sm font-bold">
              {initials(u)}
            </div>
            <div class="min-w-0 flex-1">
              <p class="truncate font-semibold text-white">{u.firstName} {u.lastName}</p>
              <a href="mailto:{u.email}" class="truncate block text-xs text-sky-400 hover:underline">{u.email}</a>
            </div>
          </div>

          <!-- Details grid -->
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
            <span class="text-xs text-slate-500">Joined {joined(u.createdAt)}</span>
            <span class="rounded-full px-2 py-0.5 text-xs font-medium {style.active}">{u.role}</span>
          </div>
        </div>
      {/each}
    </div>
  {/if}

</div>
