<script>
  import { onMount } from 'svelte';
  import { fetchMyChecklistActionItems, fetchMyRecentNotes } from '../lib/auth.js';

  let { user, token, onApplications = () => {}, onOpenApplication = () => {} } = $props();

  const firstName = $derived(user?.firstName || user?.userName?.split('@')[0] || 'there');

  let actionItems     = $state([]);
  let recentNotes     = $state([]);
  let loading         = $state(true);
  let showItemsPanel  = $state(false);

  onMount(async () => {
    const [ai, rn] = await Promise.all([
      fetchMyChecklistActionItems(token),
      fetchMyRecentNotes(token),
    ]);
    if (Array.isArray(ai)) actionItems = ai;
    if (Array.isArray(rn)) recentNotes = rn;
    loading = false;
  });

  // Group action items by application for the panel view
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
    <p class="text-sm text-slate-400">Welcome back</p>
    <h2 class="mt-1 text-2xl font-semibold text-white">Hello, {firstName}</h2>
    <p class="mt-2 text-sm text-slate-400">Manage your mortgage application and documents from one place.</p>
  </div>

  <!-- Quick actions + action-required count -->
  <div class="grid gap-4 sm:grid-cols-3">
    <button
      onclick={onApplications}
      class="group flex flex-col items-start gap-3 rounded-3xl border border-sky-500/30 bg-sky-500/5 p-6 text-left shadow-xl transition hover:border-sky-500/60 hover:bg-sky-500/10"
    >
      <div class="flex h-10 w-10 items-center justify-center rounded-2xl bg-sky-500/20">
        <svg class="h-5 w-5 text-sky-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 4v16m8-8H4"/>
        </svg>
      </div>
      <div>
        <p class="font-semibold text-white">New application</p>
        <p class="mt-1 text-xs text-slate-400">Start a mortgage application</p>
      </div>
    </button>

    <button
      onclick={onApplications}
      class="group flex flex-col items-start gap-3 rounded-3xl border border-slate-800 bg-slate-900/95 p-6 text-left shadow-xl transition hover:border-slate-700 hover:bg-slate-800/60"
    >
      <div class="flex h-10 w-10 items-center justify-center rounded-2xl bg-slate-800">
        <svg class="h-5 w-5 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
        </svg>
      </div>
      <div>
        <p class="font-semibold text-white">My applications</p>
        <p class="mt-1 text-xs text-slate-400">View all your applications</p>
      </div>
    </button>

    <!-- Action-required card -->
    <button
      onclick={() => { if (actionItems.length > 0) showItemsPanel = true; }}
      class="group flex flex-col items-start gap-3 rounded-3xl border p-6 text-left shadow-xl transition
        {actionItems.length > 0
          ? 'border-amber-500/40 bg-amber-500/5 hover:border-amber-500/70 hover:bg-amber-500/10 cursor-pointer'
          : 'border-slate-800 bg-slate-900/95 cursor-default'}"
    >
      <div class="flex w-full items-start justify-between gap-2">
        <div class="flex h-10 w-10 items-center justify-center rounded-2xl
          {actionItems.length > 0 ? 'bg-amber-500/20' : 'bg-slate-800'}">
          <svg class="h-5 w-5 {actionItems.length > 0 ? 'text-amber-400' : 'text-slate-400'}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
          </svg>
        </div>
        {#if loading}
          <span class="text-xs text-slate-600">…</span>
        {:else if actionItems.length > 0}
          <span class="rounded-full bg-amber-500 px-2.5 py-0.5 text-sm font-bold text-white">{actionItems.length}</span>
        {/if}
      </div>
      <div>
        <p class="font-semibold {actionItems.length > 0 ? 'text-amber-300' : 'text-white'}">
          Action required
        </p>
        <p class="mt-1 text-xs {actionItems.length > 0 ? 'text-amber-400/70' : 'text-slate-400'}">
          {#if loading}Loading…
          {:else if actionItems.length === 0}No outstanding items
          {:else}{actionItems.length} checklist {actionItems.length === 1 ? 'item needs' : 'items need'} attention
          {/if}
        </p>
      </div>
    </button>
  </div>

  <!-- Recent notes from your case team -->
  {#if recentNotes.length > 0}
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <h3 class="mb-4 text-base font-semibold text-white">Recent updates from your case team</h3>
      <div class="space-y-3">
        {#each recentNotes as note (note.id)}
          <div class="rounded-2xl border border-slate-700/60 bg-slate-950/60 p-4">
            <div class="flex flex-wrap items-start justify-between gap-2">
              <div class="flex flex-wrap items-center gap-2">
                <span class="rounded-full bg-slate-700 px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide text-slate-300">
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
              <p class="mt-1 font-mono text-[10px] text-slate-600">Ref: {note.applicationReference}</p>
            {/if}
            <p class="mt-2 text-sm leading-relaxed text-slate-300">{note.content}</p>
          </div>
        {/each}
      </div>
    </div>
  {:else if !loading}
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <h3 class="text-base font-semibold text-white">Application status</h3>
      <div class="mt-6 flex flex-col items-center justify-center py-8 text-center">
        <div class="flex h-14 w-14 items-center justify-center rounded-full bg-slate-800">
          <svg class="h-6 w-6 text-slate-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
        </div>
        <p class="mt-4 text-sm font-medium text-slate-300">No applications yet</p>
        <p class="mt-1 text-xs text-slate-500">Start a new mortgage application to get going.</p>
        <button
          onclick={onApplications}
          class="mt-5 rounded-2xl bg-sky-500 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 hover:bg-sky-400 transition"
        >Start application</button>
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
          <p class="mt-0.5 text-xs text-slate-400">{actionItems.length} {actionItems.length === 1 ? 'item' : 'items'} need your attention</p>
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
            <p class="mb-2 font-mono text-xs font-semibold text-sky-400">{group.ref}</p>
            <div class="space-y-2">
              {#each group.items as item}
                <div class="rounded-2xl border border-amber-500/30 bg-amber-500/5 p-4">
                  <div class="flex items-start justify-between gap-2">
                    <div>
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
                    </div>
                  </div>
                  <button
                    onclick={() => { showItemsPanel = false; onOpenApplication(item.applicationId); }}
                    class="mt-3 text-xs font-semibold text-sky-400 hover:text-sky-300 transition"
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
