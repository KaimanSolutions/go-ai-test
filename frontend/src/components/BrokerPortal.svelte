<script>
  import { onMount } from 'svelte';
  import { fetchMyChecklistActionItems, fetchMyRecentNotes, fetchApplications } from '../lib/auth.js';

  let { user, token, onApplications = () => {}, onOpenApplication = () => {} } = $props();

  const firstName = $derived(user?.firstName || user?.userName?.split('@')[0] || 'there');
  const company   = $derived(user?.companyName || '');

  let actionItems       = $state([]);
  let recentNotes       = $state([]);
  let applications      = $state([]);
  let loading           = $state(true);
  let showItemsPanel    = $state(false);
  let showInProgressPanel = $state(false);

  const inProgressApps = $derived(applications.filter(a => !a.submittedAt));

  onMount(async () => {
    const [ai, rn, apps] = await Promise.all([
      fetchMyChecklistActionItems(token),
      fetchMyRecentNotes(token),
      fetchApplications(token),
    ]);
    if (Array.isArray(ai))   actionItems  = ai;
    if (Array.isArray(rn))   recentNotes  = rn;
    if (Array.isArray(apps)) applications = apps;
    loading = false;
  });

  function fmtDate(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  const itemsByApp = $derived.by(() => {
    const map = new Map();
    for (const item of actionItems) {
      if (!map.has(item.applicationId)) map.set(item.applicationId, { ref: item.applicationReference, items: [] });
      map.get(item.applicationId).items.push(item);
    }
    return [...map.values()];
  });

  function noteTimestamp(iso) {
    return new Date(iso).toLocaleString('en-GB', {
      day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit'
    });
  }
</script>

<div class="space-y-6">
  <!-- Welcome -->
  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <div class="flex items-center justify-between">
      <div>
        <p class="text-sm text-slate-400">Broker portal</p>
        <h2 class="mt-1 text-2xl font-semibold text-white">Hello, {firstName}</h2>
        {#if company}<p class="mt-1 text-sm text-sky-400">{company}</p>{/if}
        <p class="mt-2 text-sm text-slate-400">Submit and manage mortgage applications on behalf of your clients.</p>
      </div>
      <div class="hidden sm:flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-violet-500/20">
        <svg class="h-7 w-7 text-violet-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
          <path stroke-linecap="round" stroke-linejoin="round" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"/>
        </svg>
      </div>
    </div>
  </div>

  <!-- Stats row -->
  <div class="grid gap-4 sm:grid-cols-3">

    <!-- In-progress applications card -->
    <button
      onclick={() => { if (inProgressApps.length > 0) showInProgressPanel = true; }}
      class="group flex flex-col gap-2 rounded-3xl border p-5 text-left shadow-xl transition
        {inProgressApps.length > 0
          ? 'border-sky-500/40 bg-sky-500/5 hover:border-sky-500/60 hover:bg-sky-500/10 cursor-pointer'
          : 'border-slate-800 bg-slate-900/95 cursor-default'}"
    >
      <div class="flex items-center justify-between gap-2">
        <p class="text-xs font-medium {inProgressApps.length > 0 ? 'text-sky-400' : 'text-slate-400'}">
          In progress
        </p>
      </div>
      <p class="text-3xl font-semibold {inProgressApps.length > 0 ? 'text-white' : 'text-white'}">
        {loading ? '…' : inProgressApps.length}
      </p>
      <p class="text-xs text-slate-500">
        {inProgressApps.length === 1 ? 'application' : 'applications'} in progress
      </p>
    </button>

    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
      <p class="text-xs font-medium text-slate-400">Submitted applications</p>
      <p class="mt-2 text-3xl font-semibold text-white">
        {loading ? '…' : applications.filter(a => !!a.submittedAt).length}
      </p>
    </div>

    <!-- Action-required card -->
    <button
      onclick={() => { if (actionItems.length > 0) showItemsPanel = true; }}
      class="group flex flex-col gap-2 rounded-3xl border p-5 text-left shadow-xl transition
        {actionItems.length > 0
          ? 'border-amber-500/40 bg-amber-500/5 hover:border-amber-500/70 hover:bg-amber-500/10 cursor-pointer'
          : 'border-slate-800 bg-slate-900/95 cursor-default'}"
    >
      <div class="flex items-center justify-between gap-2">
        <p class="text-xs font-medium {actionItems.length > 0 ? 'text-amber-400' : 'text-slate-400'}">
          Action required
        </p>
        {#if !loading && actionItems.length > 0}
          <span class="rounded-full bg-amber-500 px-2 py-0.5 text-xs font-bold text-white">{actionItems.length}</span>
        {/if}
      </div>
      <p class="text-3xl font-semibold {actionItems.length > 0 ? 'text-amber-300' : 'text-white'}">
        {loading ? '…' : actionItems.length}
      </p>
      <p class="text-xs {actionItems.length > 0 ? 'text-amber-400/70' : 'text-slate-500'}">
        {actionItems.length === 1 ? 'checklist item' : 'checklist items'} outstanding
      </p>
    </button>
  </div>

  <!-- Quick actions -->
  <div class="grid gap-4 sm:grid-cols-2">
    <button
      onclick={onApplications}
      class="group flex items-center gap-4 rounded-3xl border border-violet-500/30 bg-violet-500/5 p-6 text-left shadow-xl transition hover:border-violet-500/60 hover:bg-violet-500/10"
    >
      <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-violet-500/20">
        <svg class="h-6 w-6 text-violet-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4"/>
        </svg>
      </div>
      <div>
        <p class="font-semibold text-white">Submit application</p>
        <p class="text-xs text-slate-400 mt-0.5">Submit on behalf of a client</p>
      </div>
    </button>

    <button
      onclick={onApplications}
      class="group flex items-center gap-4 rounded-3xl border border-slate-800 bg-slate-900/95 p-6 text-left shadow-xl transition hover:border-slate-700 hover:bg-slate-800/60"
    >
      <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-slate-800">
        <svg class="h-6 w-6 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
        </svg>
      </div>
      <div>
        <p class="font-semibold text-white">All applications</p>
        <p class="text-xs text-slate-400 mt-0.5">View and manage your pipeline</p>
      </div>
    </button>
  </div>

  <!-- Recent notes from case team -->
  {#if recentNotes.length > 0}
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <h3 class="mb-4 text-base font-semibold text-white">Recent case updates</h3>
      <div class="space-y-3">
        {#each recentNotes as note (note.id)}
          <div class="rounded-2xl border border-slate-700/60 bg-slate-950/60 p-4">
            <div class="flex flex-wrap items-start justify-between gap-2">
              <div class="flex flex-wrap items-center gap-2">
                <span class="rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide
                  {note.authorRole === 'Admin' ? 'bg-slate-700 text-slate-300' : 'bg-sky-500/20 text-sky-400'}">
                  {note.authorRole}
                </span>
                <span class="text-xs font-semibold text-white">{note.authorName}</span>
                {#if note.category}
                  <span class="rounded-full bg-amber-500/15 px-2 py-0.5 text-[10px] font-medium text-amber-400">{note.category}</span>
                {/if}
              </div>
              <span class="shrink-0 text-[11px] text-slate-500">{noteTimestamp(note.createdAt)}</span>
            </div>
            {#if note.applicationReference}
              <p class="mt-1 font-mono text-[10px] text-sky-600">Ref: {note.applicationReference}</p>
            {/if}
            <p class="mt-2 text-sm leading-relaxed text-slate-300">{note.content}</p>
          </div>
        {/each}
      </div>
    </div>
  {:else if !loading}
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <h3 class="text-base font-semibold text-white">Recent applications</h3>
      <div class="mt-6 flex flex-col items-center justify-center py-8 text-center">
        <div class="flex h-14 w-14 items-center justify-center rounded-full bg-slate-800">
          <svg class="h-6 w-6 text-slate-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
        </div>
        <p class="mt-4 text-sm font-medium text-slate-300">No applications submitted yet</p>
        <p class="mt-1 text-xs text-slate-500">Submit an application on behalf of a client to get started.</p>
        <button
          onclick={onApplications}
          class="mt-5 rounded-2xl bg-violet-500 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-violet-500/20 hover:bg-violet-400 transition"
        >Submit application</button>
      </div>
    </div>
  {/if}
</div>

<!-- Action-required panel -->
{#if showItemsPanel}
  <div
    role="presentation"
    class="fixed inset-0 z-50 flex items-start justify-end bg-black/60 backdrop-blur-sm"
    onclick={(e) => { if (e.target === e.currentTarget) showItemsPanel = false; }}
    onkeydown={() => {}}
  >
    <div class="flex h-full w-full max-w-lg flex-col overflow-y-auto border-l border-slate-700 bg-slate-900 shadow-2xl">
      <div class="flex shrink-0 items-center justify-between border-b border-slate-800 px-6 py-5">
        <div>
          <h3 class="text-base font-semibold text-white">Action required</h3>
          <p class="mt-0.5 text-xs text-slate-400">{actionItems.length} {actionItems.length === 1 ? 'item' : 'items'} need attention across your applications</p>
        </div>
        <button
          aria-label="Close"
          onclick={() => showItemsPanel = false}
          class="flex h-8 w-8 items-center justify-center rounded-xl text-slate-500 transition hover:bg-slate-800 hover:text-white"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
          </svg>
        </button>
      </div>

      <div class="flex-1 space-y-5 p-6">
        {#each itemsByApp as group}
          <div>
            <p class="mb-2 font-mono text-xs font-semibold text-violet-400">{group.ref}</p>
            <div class="space-y-2">
              {#each group.items as item}
                <div class="rounded-2xl border border-amber-500/30 bg-amber-500/5 p-4">
                  <div class="flex items-center gap-2">
                    <span class="rounded-full px-2 py-0.5 text-[10px] font-semibold
                      {item.itemType === 'Document' ? 'bg-sky-500/20 text-sky-400' : 'bg-amber-500/20 text-amber-400'}">
                      {item.itemType}
                    </span>
                    <span class="rounded-full bg-amber-500/20 px-2 py-0.5 text-[10px] font-semibold text-amber-400">
                      {item.status}
                    </span>
                  </div>
                  <p class="mt-1.5 text-sm font-semibold text-white">{item.itemName}</p>
                  {#if item.itemDescription}
                    <p class="mt-0.5 text-xs text-slate-400">{item.itemDescription}</p>
                  {/if}
                  <button
                    onclick={() => { showItemsPanel = false; onOpenApplication(item.applicationId); }}
                    class="mt-3 text-xs font-semibold text-violet-400 hover:text-violet-300 transition"
                  >View application →</button>
                </div>
              {/each}
            </div>
          </div>
        {/each}
      </div>
    </div>
  </div>
{/if}

<!-- In-progress applications panel -->
{#if showInProgressPanel}
  <div
    role="presentation"
    class="fixed inset-0 z-50 flex items-start justify-end bg-black/60 backdrop-blur-sm"
    onclick={(e) => { if (e.target === e.currentTarget) showInProgressPanel = false; }}
    onkeydown={() => {}}
  >
    <div class="flex h-full w-full max-w-lg flex-col overflow-y-auto border-l border-slate-700 bg-slate-900 shadow-2xl">
      <div class="flex shrink-0 items-center justify-between border-b border-slate-800 px-6 py-5">
        <div>
          <h3 class="text-base font-semibold text-white">In progress</h3>
          <p class="mt-0.5 text-xs text-slate-400">{inProgressApps.length} {inProgressApps.length === 1 ? 'application' : 'applications'} currently in progress</p>
        </div>
        <button
          aria-label="Close"
          onclick={() => showInProgressPanel = false}
          class="flex h-8 w-8 items-center justify-center rounded-xl text-slate-500 transition hover:bg-slate-800 hover:text-white"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
          </svg>
        </button>
      </div>

      <div class="flex-1 space-y-3 p-6">
        {#each inProgressApps as app (app.id)}
          <button
            onclick={() => { showInProgressPanel = false; onOpenApplication(app.id); }}
            class="w-full rounded-2xl border border-slate-700 bg-slate-950/60 p-4 text-left transition hover:border-sky-500/40 hover:bg-sky-500/5"
          >
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0">
                <p class="font-mono text-xs font-semibold text-sky-400">{app.publicReference}</p>
                <p class="mt-1 truncate text-sm font-semibold text-white">{app.formTitle ?? 'Untitled form'}</p>
                {#if app.client}
                  <p class="mt-0.5 text-xs text-slate-400">{app.client.firstName} {app.client.lastName}</p>
                {/if}
              </div>
              <div class="shrink-0 text-right">
                {#if app.currentStage}
                  <span class="rounded-full bg-slate-800 px-2 py-0.5 text-[10px] font-medium text-slate-300">{app.currentStage.name}</span>
                {:else}
                  <span class="rounded-full bg-slate-800 px-2 py-0.5 text-[10px] font-medium text-slate-500">Draft</span>
                {/if}
                <p class="mt-1.5 text-[11px] text-slate-600">{fmtDate(app.createdAt)}</p>
              </div>
            </div>
          </button>
        {/each}
      </div>
    </div>
  </div>
{/if}
