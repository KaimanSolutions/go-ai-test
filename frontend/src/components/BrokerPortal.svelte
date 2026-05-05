<script>
  import FormRunner from './FormRunner.svelte';
  let { user } = $props();

  let activePage = $state('home');
  const firstName = user?.firstName || user?.userName?.split('@')[0] || 'there';
  const company = user?.companyName || '';
</script>

{#if activePage === 'apply'}
  <div>
    <button
      onclick={() => activePage = 'home'}
      class="mb-4 flex items-center gap-1.5 text-xs font-medium text-slate-400 hover:text-white transition"
    >
      <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
      </svg>
      Back to dashboard
    </button>
    <FormRunner />
  </div>

{:else}
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

    <!-- Stats -->
    <div class="grid gap-4 sm:grid-cols-3">
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
        <p class="text-xs font-medium text-slate-400">Active clients</p>
        <p class="mt-2 text-3xl font-semibold text-white">0</p>
      </div>
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
        <p class="text-xs font-medium text-slate-400">Submitted applications</p>
        <p class="mt-2 text-3xl font-semibold text-white">0</p>
      </div>
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
        <p class="text-xs font-medium text-slate-400">Pending reviews</p>
        <p class="mt-2 text-3xl font-semibold text-white">0</p>
      </div>
    </div>

    <!-- Quick actions -->
    <div class="grid gap-4 sm:grid-cols-2">
      <button
        onclick={() => activePage = 'apply'}
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

      <div class="flex items-center gap-4 rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
        <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-slate-800">
          <svg class="h-6 w-6 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"/>
          </svg>
        </div>
        <div>
          <p class="font-semibold text-white">My clients</p>
          <p class="text-xs text-slate-400 mt-0.5">No clients added yet</p>
        </div>
      </div>
    </div>

    <!-- Recent applications -->
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
          onclick={() => activePage = 'apply'}
          class="mt-5 rounded-2xl bg-violet-500 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-violet-500/20 hover:bg-violet-400 transition"
        >Submit application</button>
      </div>
    </div>
  </div>
{/if}
