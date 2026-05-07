<script>
  import { onMount } from 'svelte';
  import { fetchLoginEvents, fetchLoginEventSummary } from '../lib/auth.js';

  let { token } = $props();

  // ── Summary state ─────────────────────────────────────────────────────────────
  let summary     = $state(null);
  let sumLoading  = $state(true);

  // ── Table state ───────────────────────────────────────────────────────────────
  let events      = $state([]);
  let total       = $state(0);
  let page        = $state(1);
  let loading     = $state(false);

  // Filters
  let userTypeFilter   = $state('');
  let deviceTypeFilter = $state('');
  let authMethodFilter = $state('');
  let fromFilter       = $state('');
  let toFilter         = $state('');

  const PAGE_SIZE = 50;
  const totalPages = $derived(Math.ceil(total / PAGE_SIZE));

  onMount(async () => {
    await Promise.all([loadSummary(), loadEvents()]);
  });

  async function loadSummary() {
    sumLoading = true;
    const res = await fetchLoginEventSummary(token);
    sumLoading = false;
    if (!res.error) summary = res;
  }

  async function loadEvents(p = 1) {
    loading = true;
    page = p;
    const res = await fetchLoginEvents(token, {
      page: p, pageSize: PAGE_SIZE,
      userType:   userTypeFilter,
      deviceType: deviceTypeFilter,
      authMethod: authMethodFilter,
      from: fromFilter,
      to:   toFilter,
    });
    loading = false;
    if (!res.error) { events = res.items; total = res.total; }
  }

  function applyFilters() { loadEvents(1); }

  function clearFilters() {
    userTypeFilter = ''; deviceTypeFilter = ''; authMethodFilter = '';
    fromFilter = ''; toFilter = '';
    loadEvents(1);
  }

  const hasFilter = $derived(
    userTypeFilter || deviceTypeFilter || authMethodFilter || fromFilter || toFilter
  );

  function fmtTs(iso) {
    return new Date(iso).toLocaleString('en-GB', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit', second: '2-digit'
    });
  }

  function pct(n, total) {
    if (!total) return '0%';
    return Math.round((n / total) * 100) + '%';
  }

  function roleColor(r) {
    if (r === 'Admin')  return 'bg-slate-700 text-slate-300';
    if (r === 'Broker') return 'bg-violet-500/20 text-violet-400';
    return 'bg-sky-500/20 text-sky-400';
  }
  function deviceColor(d) {
    return d === 'Mobile' ? 'bg-emerald-500/15 text-emerald-400' : 'bg-slate-700 text-slate-300';
  }
  function methodColor(m) {
    return m === 'SSO' ? 'bg-amber-500/15 text-amber-400' : 'bg-slate-700 text-slate-300';
  }
</script>

<div class="space-y-6">

  <!-- Summary cards -->
  {#if sumLoading}
    <div class="grid gap-4 sm:grid-cols-4">
      {#each [1,2,3,4] as _}
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl animate-pulse">
          <div class="h-3 w-24 rounded bg-slate-800"></div>
          <div class="mt-3 h-8 w-16 rounded bg-slate-800"></div>
        </div>
      {/each}
    </div>
  {:else if summary}
    <div class="grid gap-4 sm:grid-cols-4">
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
        <p class="text-xs font-medium text-slate-400">Total logins (all time)</p>
        <p class="mt-2 text-3xl font-semibold text-white">{summary.totalAllTime.toLocaleString()}</p>
      </div>
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
        <p class="text-xs font-medium text-slate-400">Last 30 days</p>
        <p class="mt-2 text-3xl font-semibold text-white">{summary.totalLast30.toLocaleString()}</p>
        <p class="mt-1 text-xs text-slate-500">{summary.totalLast7} in last 7 days</p>
      </div>
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
        <p class="text-xs font-medium text-slate-400">By user type (30 days)</p>
        <div class="mt-2 space-y-1">
          {#each Object.entries(summary.byUserType ?? {}) as [type, count]}
            <div class="flex items-center justify-between gap-2">
              <span class="rounded-full px-1.5 py-0.5 text-[10px] font-semibold {roleColor(type)}">{type}</span>
              <div class="flex items-center gap-1.5">
                <div class="h-1.5 rounded-full bg-sky-500/40" style="width: {Math.max(4, (count / summary.totalLast30) * 64)}px"></div>
                <span class="text-xs font-semibold text-white">{count}</span>
              </div>
            </div>
          {/each}
        </div>
      </div>
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
        <p class="text-xs font-medium text-slate-400">By device (30 days)</p>
        <div class="mt-2 space-y-1.5">
          {#each Object.entries(summary.byDeviceType ?? {}) as [d, count]}
            <div class="flex items-center justify-between">
              <span class="text-xs text-slate-400">{d}</span>
              <span class="text-xs font-semibold text-white">{count} <span class="text-slate-500">({pct(count, summary.totalLast30)})</span></span>
            </div>
          {/each}
          <div class="mt-1 border-t border-slate-800 pt-1.5">
            {#each Object.entries(summary.byAuthMethod ?? {}) as [m, count]}
              <div class="flex items-center justify-between">
                <span class="text-xs text-slate-400">{m}</span>
                <span class="text-xs font-semibold text-white">{count} <span class="text-slate-500">({pct(count, summary.totalLast30)})</span></span>
              </div>
            {/each}
          </div>
        </div>
      </div>
    </div>
  {/if}

  <!-- Filters -->
  <div class="flex flex-wrap items-end gap-3 rounded-3xl border border-slate-800 bg-slate-900/95 px-5 py-4 shadow-xl">
    <div>
      <p class="mb-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500">User type</p>
      <select bind:value={userTypeFilter}
        class="rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white focus:border-sky-500 focus:outline-none">
        <option value="">All</option>
        <option value="Admin">Admin</option>
        <option value="Broker">Broker</option>
        <option value="Client">Client / Borrower</option>
      </select>
    </div>
    <div>
      <p class="mb-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500">Device</p>
      <select bind:value={deviceTypeFilter}
        class="rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white focus:border-sky-500 focus:outline-none">
        <option value="">All</option>
        <option value="Desktop">Desktop</option>
        <option value="Mobile">Mobile</option>
        <option value="Unknown">Unknown</option>
      </select>
    </div>
    <div>
      <p class="mb-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500">Auth method</p>
      <select bind:value={authMethodFilter}
        class="rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white focus:border-sky-500 focus:outline-none">
        <option value="">All</option>
        <option value="Password">Password</option>
        <option value="SSO">SSO</option>
      </select>
    </div>
    <div>
      <p class="mb-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500">From</p>
      <input type="date" bind:value={fromFilter}
        class="rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white focus:border-sky-500 focus:outline-none" />
    </div>
    <div>
      <p class="mb-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500">To</p>
      <input type="date" bind:value={toFilter}
        class="rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white focus:border-sky-500 focus:outline-none" />
    </div>
    <button onclick={applyFilters}
      class="rounded-xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-sky-400">
      Apply
    </button>
    {#if hasFilter}
      <button onclick={clearFilters}
        class="rounded-xl border border-slate-700 px-4 py-2 text-sm font-medium text-slate-400 transition hover:text-white">
        Clear
      </button>
    {/if}
    <span class="ml-auto text-xs text-slate-500">{total.toLocaleString()} {total === 1 ? 'event' : 'events'}</span>
  </div>

  <!-- Table -->
  <div class="overflow-hidden rounded-3xl border border-slate-800 bg-slate-900/95 shadow-xl">
    {#if loading}
      <div class="px-6 py-10 text-center text-sm text-slate-500">Loading…</div>
    {:else if events.length === 0}
      <div class="px-6 py-10 text-center text-sm text-slate-500">No login events found.</div>
    {:else}
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-800">
            <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Timestamp</th>
            <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Email</th>
            <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">User type</th>
            <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 sm:table-cell">Device</th>
            <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 md:table-cell">Auth method</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-800">
          {#each events as ev (ev.id)}
            <tr class="hover:bg-slate-800/30 transition">
              <td class="px-5 py-3.5 font-mono text-xs text-slate-400">{fmtTs(ev.timestampUtc)}</td>
              <td class="max-w-[200px] truncate px-5 py-3.5 text-slate-300">{ev.email ?? '—'}</td>
              <td class="px-5 py-3.5">
                <span class="rounded-full px-2 py-0.5 text-[10px] font-semibold {roleColor(ev.userType)}">{ev.userType}</span>
              </td>
              <td class="hidden px-5 py-3.5 sm:table-cell">
                <span class="rounded-full px-2 py-0.5 text-[10px] font-semibold {deviceColor(ev.deviceType)}">{ev.deviceType}</span>
              </td>
              <td class="hidden px-5 py-3.5 md:table-cell">
                <span class="rounded-full px-2 py-0.5 text-[10px] font-semibold {methodColor(ev.authMethod)}">{ev.authMethod}</span>
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    {/if}
  </div>

  <!-- Pagination -->
  {#if totalPages > 1}
    <div class="flex items-center justify-between text-sm text-slate-400">
      <span>{total.toLocaleString()} total events</span>
      <div class="flex items-center gap-2">
        <button onclick={() => loadEvents(page - 1)} disabled={page <= 1}
          class="rounded-xl border border-slate-700 px-3 py-1.5 text-xs transition hover:border-slate-500 disabled:opacity-30">
          Previous
        </button>
        <span class="text-xs">Page {page} of {totalPages}</span>
        <button onclick={() => loadEvents(page + 1)} disabled={page >= totalPages}
          class="rounded-xl border border-slate-700 px-3 py-1.5 text-xs transition hover:border-slate-500 disabled:opacity-30">
          Next
        </button>
      </div>
    </div>
  {/if}

</div>
