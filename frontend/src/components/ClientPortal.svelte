<script>
  let { user, onApplications = () => {} } = $props();

  const firstName = user?.firstName || user?.userName?.split('@')[0] || 'there';
</script>

<div class="space-y-6">
  <!-- Welcome -->
  <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
    <p class="text-sm text-slate-400">Welcome back</p>
    <h2 class="mt-1 text-2xl font-semibold text-white">Hello, {firstName}</h2>
    <p class="mt-2 text-sm text-slate-400">Manage your mortgage application and documents from one place.</p>
  </div>

  <!-- Quick actions -->
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

    <div class="flex flex-col items-start gap-3 rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="flex h-10 w-10 items-center justify-center rounded-2xl bg-slate-800">
        <svg class="h-5 w-5 text-slate-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
        </svg>
      </div>
      <div>
        <p class="font-semibold text-white">Documents</p>
        <p class="mt-1 text-xs text-slate-400">Upload supporting documents</p>
      </div>
    </div>
  </div>

  <!-- Status -->
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
</div>
