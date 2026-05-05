<script>
  import { onMount } from 'svelte';
  import {
    fetchIntegrationServices, updateIntegrationCredentials,
    fetchApiLogs, fetchApiLogDetail, fetchApiIntegrations
  } from '../lib/auth.js';

  let { token = '' } = $props();

  // ── Tabs ──────────────────────────────────────────────────────────────────
  let activeTab = $state('services');

  // ── Services tab ──────────────────────────────────────────────────────────
  let services        = $state([]);
  let servicesLoading = $state(true);
  let servicesError   = $state('');
  let editingService  = $state(null);   // name of service whose form is open
  let credFields      = $state({});     // { [fieldKey]: value }
  let saveError       = $state('');
  let saving          = $state(false);
  let saveSuccess     = $state('');

  // ── Logs tab ──────────────────────────────────────────────────────────────
  let integrations  = $state([]);
  let selectedInt   = $state('');
  let logs          = $state([]);
  let total         = $state(0);
  let page          = $state(1);
  const pageSize    = 50;
  let loading       = $state(false);
  let detail        = $state(null);
  let detailLoading = $state(false);
  let logsError     = $state('');

  onMount(async () => {
    await loadServices();
    const result = await fetchApiIntegrations(token);
    if (!result.error) {
      integrations = result;
      selectedInt  = result[0] ?? '';
    }
    await loadLogs();
  });

  // ── Services logic ────────────────────────────────────────────────────────

  async function loadServices() {
    servicesLoading = true;
    servicesError   = '';
    const result = await fetchIntegrationServices(token);
    servicesLoading = false;
    if (result?.error) {
      servicesError = result.error;
    } else if (!Array.isArray(result)) {
      servicesError = 'Unexpected response from server.';
    } else {
      services = result;
    }
  }

  function openEdit(svc) {
    if (editingService === svc.name) { editingService = null; return; }
    editingService = svc.name;
    credFields = Object.fromEntries(svc.fields.map(f => [f.key, '']));
    saveError  = '';
    saveSuccess = '';
  }

  async function saveCredentials(svc) {
    saveError   = '';
    saveSuccess = '';
    const filled = Object.entries(credFields).filter(([, v]) => v.trim());
    if (!filled.length) { saveError = 'Enter at least one credential value.'; return; }

    saving = true;
    const payload = Object.fromEntries(filled.map(([k, v]) => [k, v.trim()]));
    const result  = await updateIntegrationCredentials(svc.name, payload, token);
    saving = false;

    if (result.error) { saveError = result.error; return; }

    saveSuccess    = 'Credentials saved successfully.';
    editingService = null;
    await loadServices();
  }

  function statusBadge(svc) {
    return svc.configured
      ? { text: 'Active',          cls: 'bg-emerald-500/15 text-emerald-400' }
      : { text: 'Not configured',  cls: 'bg-amber-500/15 text-amber-400'    };
  }

  function fmtLastActivity(iso) {
    if (!iso) return null;
    return new Date(iso).toLocaleString('en-GB', { dateStyle: 'short', timeStyle: 'short' });
  }

  // ── Logs logic ────────────────────────────────────────────────────────────

  async function loadLogs(newPage = 1) {
    loading  = true;
    logsError = '';
    page     = newPage;
    const result = await fetchApiLogs(token, { integration: selectedInt, page, pageSize });
    loading  = false;
    if (result.error) { logsError = result.error; return; }
    logs  = result.items ?? [];
    total = result.total ?? 0;
  }

  async function openDetail(log) {
    detail = null;
    detailLoading = true;
    const result = await fetchApiLogDetail(log.id, token);
    detailLoading = false;
    if (!result.error) detail = result;
  }

  function closeDetail() { detail = null; }

  function statusColor(code) {
    if (!code)    return 'text-red-400';
    if (code < 300) return 'text-emerald-400';
    if (code < 400) return 'text-amber-400';
    return 'text-red-400';
  }

  function fmtDate(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('en-GB', { dateStyle: 'short', timeStyle: 'medium' });
  }

  function shortEndpoint(ep) {
    try { return new URL(ep).pathname; } catch { return ep; }
  }

  const totalPages = $derived(Math.max(1, Math.ceil(total / pageSize)));

  function tryPretty(s) {
    try { return JSON.stringify(JSON.parse(s), null, 2); } catch { return s; }
  }
</script>

<div class="space-y-6">

  <!-- Tab bar -->
  <div class="flex gap-1 rounded-2xl border border-slate-800 bg-slate-950/60 p-1">
    <button
      onclick={() => activeTab = 'services'}
      class="flex flex-1 items-center justify-center gap-2 rounded-xl py-2.5 text-sm font-semibold transition
        {activeTab === 'services' ? 'bg-sky-500/10 text-sky-400' : 'text-slate-400 hover:text-white'}"
    >
      <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M14.5 10a4.5 4.5 0 004.284-5.882c-.105-.324-.51-.391-.752-.15L15.34 6.66a.454.454 0 01-.493.11 3.01 3.01 0 01-1.618-1.616.455.455 0 01.11-.494l2.694-2.692c.24-.241.174-.647-.15-.752a4.5 4.5 0 00-5.873 4.575c.055.873-.128 1.808-.8 2.368l-7.23 6.024a2.724 2.724 0 103.837 3.837l6.024-7.23c.56-.672 1.495-.855 2.368-.8.096.007.193.01.291.01zM5 16a1 1 0 11-2 0 1 1 0 012 0z" clip-rule="evenodd"/>
      </svg>
      Connected services
    </button>
    <button
      onclick={() => activeTab = 'logs'}
      class="flex flex-1 items-center justify-center gap-2 rounded-xl py-2.5 text-sm font-semibold transition
        {activeTab === 'logs' ? 'bg-sky-500/10 text-sky-400' : 'text-slate-400 hover:text-white'}"
    >
      <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M2 4.75A.75.75 0 012.75 4h14.5a.75.75 0 010 1.5H2.75A.75.75 0 012 4.75zm0 10.5a.75.75 0 01.75-.75h7.5a.75.75 0 010 1.5h-7.5a.75.75 0 01-.75-.75zM2 10a.75.75 0 01.75-.75h14.5a.75.75 0 010 1.5H2.75A.75.75 0 012 10z" clip-rule="evenodd"/>
      </svg>
      API logs
    </button>
  </div>

  <!-- ── Services tab ──────────────────────────────────────────────────────── -->
  {#if activeTab === 'services'}

    <div class="flex items-center justify-between">
      <div>
        <h3 class="text-base font-semibold text-white">Connected services</h3>
        <p class="mt-0.5 text-sm text-slate-400">View and manage credentials for external API integrations.</p>
      </div>
      <button
        onclick={loadServices}
        class="rounded-xl bg-slate-800 px-3 py-2 text-xs font-medium text-slate-300 transition hover:bg-slate-700"
      >Refresh</button>
    </div>

    {#if saveSuccess}
      <p class="rounded-2xl bg-emerald-500/10 px-4 py-3 text-sm font-medium text-emerald-400">{saveSuccess}</p>
    {/if}

    {#if servicesLoading}
      <div class="flex items-center justify-center py-16">
        <div class="h-7 w-7 animate-spin rounded-full border-2 border-slate-700 border-t-sky-500"></div>
      </div>
    {:else if servicesError}
      <div class="rounded-2xl border border-red-500/20 bg-red-500/5 px-4 py-4">
        <p class="text-sm font-medium text-red-400">Failed to load integration services</p>
        <p class="mt-1 text-xs text-red-400/70">{servicesError}</p>
        <button onclick={loadServices} class="mt-3 rounded-xl bg-red-500/10 px-3 py-1.5 text-xs font-medium text-red-400 transition hover:bg-red-500/20">
          Try again
        </button>
      </div>
    {:else if services.length === 0}
      <p class="py-8 text-center text-sm text-slate-500">No integration services found.</p>
    {:else}
      <div class="space-y-4">
        {#each services as svc (svc.name)}
          {@const badge = statusBadge(svc)}
          {@const isEditing = editingService === svc.name}
          <div class="rounded-3xl border {isEditing ? 'border-sky-500/30' : 'border-slate-800'} bg-slate-900/95 shadow-xl transition">

            <!-- Service header -->
            <div class="flex flex-wrap items-start gap-4 p-6">
              <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-slate-800">
                <svg class="h-5 w-5 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1"/>
                </svg>
              </div>

              <div class="min-w-0 flex-1 space-y-1">
                <div class="flex flex-wrap items-center gap-2">
                  <h4 class="font-semibold text-white">{svc.name}</h4>
                  <span class="rounded-full px-2.5 py-0.5 text-xs font-medium {badge.cls}">{badge.text}</span>
                </div>
                <p class="text-sm text-slate-400">{svc.description}</p>

                <!-- Field status pills -->
                <div class="flex flex-wrap gap-2 pt-1">
                  {#each svc.fields as field}
                    <span class="flex items-center gap-1 rounded-full border {field.configured ? 'border-emerald-500/20 bg-emerald-500/5 text-emerald-400' : 'border-amber-500/20 bg-amber-500/5 text-amber-400'} px-2.5 py-0.5 text-xs">
                      {#if field.configured}
                        <svg class="h-3 w-3" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.857-9.809a.75.75 0 00-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 10-1.06 1.061l2.5 2.5a.75.75 0 001.137-.089l4-5.5z" clip-rule="evenodd"/>
                        </svg>
                      {:else}
                        <svg class="h-3 w-3" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a.75.75 0 01.75.75v4.5a.75.75 0 01-1.5 0v-4.5A.75.75 0 0110 5zm0 10a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/>
                        </svg>
                      {/if}
                      {field.label}
                    </span>
                  {/each}
                </div>
              </div>

              <div class="flex shrink-0 flex-col items-end gap-2">
                {#if svc.lastActivity}
                  <p class="text-xs text-slate-500">Last call {fmtLastActivity(svc.lastActivity)}</p>
                {:else}
                  <p class="text-xs text-slate-600">No activity yet</p>
                {/if}
                <button
                  onclick={() => openEdit(svc)}
                  class="inline-flex items-center gap-1.5 rounded-xl border px-3 py-1.5 text-xs font-medium transition
                    {isEditing
                      ? 'border-sky-500/40 bg-sky-500/10 text-sky-400'
                      : 'border-slate-700 bg-slate-800 text-slate-300 hover:bg-slate-700'}"
                >
                  <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M2.695 14.763l-1.262 3.154a.5.5 0 00.65.65l3.155-1.262a4 4 0 001.343-.885L17.5 5.5a2.121 2.121 0 00-3-3L3.58 13.42a4 4 0 00-.885 1.343z"/>
                  </svg>
                  {isEditing ? 'Cancel' : 'Edit credentials'}
                </button>
              </div>
            </div>

            <!-- Credential editor (inline expand) -->
            {#if isEditing}
              <div class="border-t border-slate-800 p-6 space-y-4">
                <p class="text-xs text-slate-400">
                  Leave a field blank to keep its current value. New values take effect immediately — no restart needed.
                </p>

                {#if saveError}
                  <p class="rounded-xl bg-red-500/10 px-3 py-2 text-xs font-medium text-red-400">{saveError}</p>
                {/if}

                <div class="grid gap-3 sm:grid-cols-2">
                  {#each svc.fields as field}
                    <div>
                      <label for="cred-{svc.name}-{field.key}" class="block text-xs font-medium text-slate-400">
                        {field.label}
                        {#if field.configured}
                          <span class="ml-1.5 text-emerald-500">● set</span>
                        {:else}
                          <span class="ml-1.5 text-amber-500">● not set</span>
                        {/if}
                      </label>
                      <input
                        id="cred-{svc.name}-{field.key}"
                        type={field.type === 'password' ? 'password' : field.type}
                        bind:value={credFields[field.key]}
                        placeholder={field.configured ? '••••••••  (leave blank to keep)' : `Enter ${field.label}`}
                        autocomplete="off"
                        class="mt-1 w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-slate-100 outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20 font-mono placeholder:font-sans placeholder:text-slate-600"
                      />
                    </div>
                  {/each}
                </div>

                <div class="flex justify-end gap-2">
                  <button
                    onclick={() => { editingService = null; saveError = ''; }}
                    class="rounded-xl px-4 py-2 text-xs font-medium text-slate-400 transition hover:text-white"
                  >Cancel</button>
                  <button
                    onclick={() => saveCredentials(svc)}
                    disabled={saving}
                    class="rounded-xl bg-sky-500 px-4 py-2 text-xs font-semibold text-white transition hover:bg-sky-400 disabled:opacity-50"
                  >{saving ? 'Saving…' : 'Save credentials'}</button>
                </div>
              </div>
            {/if}

          </div>
        {/each}
      </div>
    {/if}

  <!-- ── Logs tab ──────────────────────────────────────────────────────────── -->
  {:else}

    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h3 class="text-base font-semibold text-white">API request logs</h3>
        <p class="mt-0.5 text-sm text-slate-400">Outbound requests made to third-party integrations.</p>
      </div>
      <div class="flex items-center gap-2">
        <label class="text-xs font-medium text-slate-400">Integration</label>
        <select
          class="rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 outline-none focus:border-sky-500"
          bind:value={selectedInt}
          onchange={() => loadLogs(1)}
        >
          <option value="">All</option>
          {#each integrations as i}
            <option value={i}>{i}</option>
          {/each}
        </select>
        <button
          onclick={() => loadLogs(1)}
          class="rounded-xl bg-slate-800 px-3 py-2 text-xs font-medium text-slate-300 transition hover:bg-slate-700"
        >Refresh</button>
      </div>
    </div>

    {#if logsError}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm text-red-400">{logsError}</p>
    {/if}

    <div class="overflow-hidden rounded-2xl border border-slate-800">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-slate-800 bg-slate-950/80">
            <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Date / Time</th>
            <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Integration</th>
            <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Method</th>
            <th class="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Endpoint</th>
            <th class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-slate-500">Status</th>
            <th class="px-4 py-3 text-right text-xs font-semibold uppercase tracking-wider text-slate-500">Duration</th>
          </tr>
        </thead>
        <tbody>
          {#if loading}
            <tr><td colspan="6" class="px-4 py-8 text-center text-sm text-slate-500">Loading…</td></tr>
          {:else if logs.length === 0}
            <tr><td colspan="6" class="px-4 py-8 text-center text-sm text-slate-500">No log entries found.</td></tr>
          {:else}
            {#each logs as log}
              <tr
                class="cursor-pointer border-b border-slate-800/60 transition hover:bg-slate-800/40"
                onclick={() => openDetail(log)}
              >
                <td class="px-4 py-3 text-slate-300">{fmtDate(log.requestedAt)}</td>
                <td class="px-4 py-3">
                  <span class="rounded-full bg-sky-500/10 px-2 py-0.5 text-xs font-medium text-sky-400">{log.integration}</span>
                </td>
                <td class="px-4 py-3 font-mono text-xs text-slate-400">{log.method}</td>
                <td class="max-w-xs truncate px-4 py-3 font-mono text-xs text-slate-300" title={log.endpoint}>{shortEndpoint(log.endpoint)}</td>
                <td class="px-4 py-3 text-right font-mono text-xs font-semibold {statusColor(log.statusCode)}">{log.statusCode ?? 'ERR'}</td>
                <td class="px-4 py-3 text-right text-xs text-slate-400">{log.durationMs}ms</td>
              </tr>
            {/each}
          {/if}
        </tbody>
      </table>
    </div>

    {#if totalPages > 1}
      <div class="flex items-center justify-between text-sm text-slate-400">
        <span>{total} total</span>
        <div class="flex items-center gap-2">
          <button onclick={() => loadLogs(page - 1)} disabled={page <= 1}
            class="rounded-xl border border-slate-700 px-3 py-1.5 text-xs transition hover:border-slate-500 disabled:opacity-30">Previous</button>
          <span class="text-xs">Page {page} of {totalPages}</span>
          <button onclick={() => loadLogs(page + 1)} disabled={page >= totalPages}
            class="rounded-xl border border-slate-700 px-3 py-1.5 text-xs transition hover:border-slate-500 disabled:opacity-30">Next</button>
        </div>
      </div>
    {/if}

  {/if}

</div>

<!-- Detail drawer -->
{#if detail || detailLoading}
  <div class="fixed inset-0 z-50 flex items-start justify-end bg-black/50 backdrop-blur-sm" onclick={closeDetail}>
    <div class="relative flex h-full w-full max-w-2xl flex-col overflow-y-auto bg-slate-900 shadow-2xl" onclick={(e) => e.stopPropagation()}>
      <div class="flex items-center justify-between border-b border-slate-800 px-6 py-4">
        <h4 class="text-base font-semibold text-white">Request Detail</h4>
        <button onclick={closeDetail} class="text-slate-400 transition hover:text-white">
          <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
          </svg>
        </button>
      </div>
      {#if detailLoading}
        <div class="flex flex-1 items-center justify-center text-slate-500">Loading…</div>
      {:else if detail}
        <div class="flex-1 space-y-5 p-6">
          <div class="grid grid-cols-2 gap-4 rounded-2xl border border-slate-800 bg-slate-950/60 p-4 text-sm">
            <div><p class="text-xs text-slate-500">Integration</p><p class="mt-0.5 font-medium text-sky-400">{detail.integration}</p></div>
            <div><p class="text-xs text-slate-500">Date / Time</p><p class="mt-0.5 text-slate-200">{fmtDate(detail.requestedAt)}</p></div>
            <div><p class="text-xs text-slate-500">Method</p><p class="mt-0.5 font-mono text-slate-200">{detail.method}</p></div>
            <div><p class="text-xs text-slate-500">Status</p><p class="mt-0.5 font-mono font-semibold {statusColor(detail.statusCode)}">{detail.statusCode ?? 'Error'}</p></div>
            <div class="col-span-2"><p class="text-xs text-slate-500">Endpoint</p><p class="mt-0.5 break-all font-mono text-xs text-slate-200">{detail.endpoint}</p></div>
            <div><p class="text-xs text-slate-500">Duration</p><p class="mt-0.5 text-slate-200">{detail.durationMs}ms</p></div>
            {#if detail.errorMessage}
              <div class="col-span-2"><p class="text-xs text-slate-500">Error</p><p class="mt-0.5 text-red-400">{detail.errorMessage}</p></div>
            {/if}
          </div>
          {#if detail.requestHeaders}
            <div>
              <p class="mb-1.5 text-xs font-semibold uppercase tracking-wider text-slate-500">Request Headers</p>
              <pre class="overflow-x-auto rounded-xl bg-slate-950 px-4 py-3 text-xs text-slate-300">{detail.requestHeaders}</pre>
            </div>
          {/if}
          {#if detail.requestBody}
            <div>
              <p class="mb-1.5 text-xs font-semibold uppercase tracking-wider text-slate-500">Request Body</p>
              <pre class="overflow-x-auto rounded-xl bg-slate-950 px-4 py-3 text-xs text-slate-300">{detail.requestBody}</pre>
            </div>
          {/if}
          <div>
            <p class="mb-1.5 text-xs font-semibold uppercase tracking-wider text-slate-500">Response Body</p>
            {#if detail.responseBody}
              <pre class="max-h-96 overflow-auto rounded-xl bg-slate-950 px-4 py-3 text-xs text-slate-300">{tryPretty(detail.responseBody)}</pre>
            {:else}
              <p class="text-xs text-slate-500">No response body captured.</p>
            {/if}
          </div>
        </div>
      {/if}
    </div>
  </div>
{/if}
