<script>
  import { onMount } from 'svelte';
  import { fetchWorkflows, fetchWorkflow, saveWorkflow, deleteWorkflow } from '../lib/auth.js';

  let { token = '' } = $props();

  // ── View state ──────────────────────────────────────────────────────────────
  let view       = $state('list'); // 'list' | 'builder'
  let editingId  = $state(null);   // null = new, number = edit
  let isLoading  = $state(true);
  let isSaving   = $state(false);
  let error      = $state('');

  // ── List data ───────────────────────────────────────────────────────────────
  let workflows = $state([]);

  // ── Builder state ───────────────────────────────────────────────────────────
  let wfName        = $state('');
  let wfDescription = $state('');
  let stages        = $state([]); // WorkflowStageLocal[]
  let selectedId    = $state(null);

  // Selected stage (derived for easy access)
  let selectedStage = $derived(stages.find(s => s._id === selectedId) ?? null);

  // ── Helpers ─────────────────────────────────────────────────────────────────
  function uid() { return `s-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`; }
  function taskId() { return `t-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`; }
  function transId() { return `tr-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`; }

  function newStage(order) {
    return { _id: uid(), dbId: null, name: '', description: '', isInitial: order === 0, isFinal: false, order, tasks: [], transitionsOut: [] };
  }

  // ── List operations ─────────────────────────────────────────────────────────
  async function load() {
    isLoading = true; error = '';
    const result = await fetchWorkflows(token);
    isLoading = false;
    if (Array.isArray(result)) workflows = result;
    else error = result.error ?? 'Failed to load workflows.';
  }

  async function openCreate() {
    editingId = null;
    wfName = ''; wfDescription = '';
    stages = [newStage(0)];
    selectedId = stages[0]._id;
    view = 'builder';
  }

  async function openEdit(id) {
    error = '';
    const result = await fetchWorkflow(id, token);
    if (result.error) { error = result.error; return; }

    editingId     = result.id;
    wfName        = result.name;
    wfDescription = result.description ?? '';

    // Rebuild stage objects with client _id keyed by DB id for transition resolution
    const idMap = {}; // dbId → _id
    stages = (result.stages ?? []).map(s => {
      const _id = uid();
      idMap[s.id] = _id;
      return {
        _id,
        dbId:      s.id,
        name:      s.name,
        description: s.description ?? '',
        isInitial: s.isInitial,
        isFinal:   s.isFinal,
        order:     s.order,
        tasks: (s.tasks ?? []).map(t => ({
          _id: taskId(), title: t.title, description: t.description ?? '', required: t.required
        })),
        transitionsOut: [] // filled below after all _ids are known
      };
    });

    // Resolve transitions using idMap
    (result.stages ?? []).forEach((s, i) => {
      stages[i].transitionsOut = (s.transitionsOut ?? []).map(tr => ({
        _id:       transId(),
        toStageId: idMap[tr.toStageId] ?? '',
        label:     tr.label,
        condition: tr.condition ?? ''
      }));
    });

    selectedId = stages[0]?._id ?? null;
    view = 'builder';
  }

  async function confirmDelete(id, name) {
    if (!confirm(`Delete workflow "${name}"? This cannot be undone.`)) return;
    error = '';
    const result = await deleteWorkflow(id, token);
    if (result.error) { error = result.error; return; }
    await load();
  }

  // ── Builder: stage management ───────────────────────────────────────────────
  function addStage() {
    const s = newStage(stages.length);
    stages = [...stages, s];
    selectedId = s._id;
  }

  function removeStage(_id) {
    stages = stages
      .filter(s => s._id !== _id)
      .map((s, i) => ({ ...s, order: i }));
    // Remove any transitions pointing to this stage
    stages = stages.map(s => ({
      ...s,
      transitionsOut: s.transitionsOut.filter(t => t.toStageId !== _id)
    }));
    if (selectedId === _id) selectedId = stages[0]?._id ?? null;
  }

  function moveStage(_id, dir) {
    const idx = stages.findIndex(s => s._id === _id);
    const target = idx + dir;
    if (target < 0 || target >= stages.length) return;
    const next = [...stages];
    [next[idx], next[target]] = [next[target], next[idx]];
    stages = next.map((s, i) => ({ ...s, order: i }));
  }

  // Mutate a field on the selected stage
  function patchStage(field, value) {
    stages = stages.map(s => s._id === selectedId ? { ...s, [field]: value } : s);
  }

  // ── Builder: task management ────────────────────────────────────────────────
  function addTask() {
    stages = stages.map(s => s._id !== selectedId ? s : {
      ...s,
      tasks: [...s.tasks, { _id: taskId(), title: '', description: '', required: true }]
    });
  }

  function removeTask(taskId) {
    stages = stages.map(s => s._id !== selectedId ? s : {
      ...s,
      tasks: s.tasks.filter(t => t._id !== taskId)
    });
  }

  function patchTask(taskId, field, value) {
    stages = stages.map(s => s._id !== selectedId ? s : {
      ...s,
      tasks: s.tasks.map(t => t._id === taskId ? { ...t, [field]: value } : t)
    });
  }

  // ── Builder: transition management ─────────────────────────────────────────
  function addTransition() {
    stages = stages.map(s => s._id !== selectedId ? s : {
      ...s,
      transitionsOut: [...s.transitionsOut, { _id: transId(), toStageId: '', label: '', condition: '' }]
    });
  }

  function removeTransition(trId) {
    stages = stages.map(s => s._id !== selectedId ? s : {
      ...s,
      transitionsOut: s.transitionsOut.filter(t => t._id !== trId)
    });
  }

  function patchTransition(trId, field, value) {
    stages = stages.map(s => s._id !== selectedId ? s : {
      ...s,
      transitionsOut: s.transitionsOut.map(t => t._id === trId ? { ...t, [field]: value } : t)
    });
  }

  // ── Builder: save ───────────────────────────────────────────────────────────
  async function save() {
    error = '';
    if (!wfName.trim()) { error = 'Workflow name is required.'; return; }

    const invalidStage = stages.find(s => !s.name.trim());
    if (invalidStage) {
      selectedId = invalidStage._id;
      error = 'All stages must have a name.';
      return;
    }

    const payload = {
      name:        wfName.trim(),
      description: wfDescription.trim() || null,
      stages: stages.map((s, i) => ({
        tempId:      s._id,
        dbId:        s.dbId ?? null,
        name:        s.name.trim(),
        description: s.description.trim() || null,
        order:       i,
        isInitial:   s.isInitial,
        isFinal:     s.isFinal,
        tasks: s.tasks.map((t, j) => ({
          title:       t.title.trim(),
          description: t.description.trim() || null,
          required:    t.required,
          order:       j
        })).filter(t => t.title),
        transitionsOut: s.transitionsOut
          .filter(tr => tr.toStageId && tr.label.trim())
          .map(tr => ({
            toStageTempId: tr.toStageId,
            label:         tr.label.trim(),
            condition:     tr.condition.trim() || null
          }))
      }))
    };

    isSaving = true;
    const result = await saveWorkflow(editingId, payload, token);
    isSaving = false;

    if (result.error) { error = result.error; return; }
    view = 'list';
    await load();
  }

  onMount(load);
</script>

<!-- ── List view ─────────────────────────────────────────────────────────── -->
{#if view === 'list'}
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h3 class="text-base font-semibold text-white">Workflows</h3>
        <p class="mt-1 text-sm text-slate-400">Create and manage application workflows with stages, tasks, and branching paths.</p>
      </div>
      <button
        onclick={openCreate}
        class="rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400"
      >
        + New Workflow
      </button>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</p>
    {/if}

    {#if isLoading}
      <p class="py-12 text-center text-sm text-slate-500">Loading workflows…</p>
    {:else if workflows.length === 0}
      <div class="rounded-3xl border border-dashed border-slate-700 p-12 text-center">
        <svg class="mx-auto h-10 w-10 text-slate-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 12h16.5m-16.5 3.75h16.5M3.75 19.5h16.5M5.625 4.5h12.75a1.875 1.875 0 010 3.75H5.625a1.875 1.875 0 010-3.75z"/>
        </svg>
        <p class="mt-3 text-sm font-medium text-slate-400">No workflows yet</p>
        <p class="mt-1 text-xs text-slate-600">Create your first workflow to get started.</p>
        <button onclick={openCreate} class="mt-4 rounded-2xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-sky-400">
          + New Workflow
        </button>
      </div>
    {:else}
      <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {#each workflows as wf (wf.id)}
          <div class="flex flex-col rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
            <div class="flex-1">
              <h4 class="font-semibold text-white">{wf.name}</h4>
              {#if wf.description}
                <p class="mt-1 text-sm text-slate-400 line-clamp-2">{wf.description}</p>
              {/if}
              <div class="mt-3 flex items-center gap-3 text-xs text-slate-500">
                <span class="flex items-center gap-1">
                  <svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 6.75h12M8.25 12h12m-12 5.25h12M3.75 6.75h.007v.008H3.75V6.75zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zM3.75 12h.007v.008H3.75V12zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm-.375 5.25h.007v.008H3.75v-.008zm.375 0a.375.375 0 11-.75 0 .375.375 0 01.75 0z"/>
                  </svg>
                  {wf.stageCount} {wf.stageCount === 1 ? 'stage' : 'stages'}
                </span>
                <span>{new Date(wf.createdAt).toLocaleDateString()}</span>
              </div>
            </div>
            <div class="mt-4 flex gap-2 border-t border-slate-800 pt-4">
              <button
                onclick={() => openEdit(wf.id)}
                class="flex-1 rounded-xl border border-slate-700 px-3 py-2 text-xs font-semibold text-slate-300 transition hover:bg-slate-800 hover:text-white"
              >
                Edit
              </button>
              <button
                onclick={() => confirmDelete(wf.id, wf.name)}
                class="rounded-xl border border-red-900/50 px-3 py-2 text-xs font-semibold text-red-500 transition hover:bg-red-950"
              >
                Delete
              </button>
            </div>
          </div>
        {/each}
      </div>
    {/if}
  </div>

<!-- ── Builder view ──────────────────────────────────────────────────────── -->
{:else}
  <div class="flex h-full flex-col gap-4">

    <!-- Top bar -->
    <div class="flex shrink-0 items-center gap-4">
      <button
        onclick={() => { view = 'list'; error = ''; }}
        class="flex items-center gap-1.5 text-xs font-medium text-slate-400 transition hover:text-white"
      >
        <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
        </svg>
        Workflows
      </button>
      <span class="text-slate-700">/</span>
      <input
        type="text"
        placeholder="Workflow name…"
        bind:value={wfName}
        class="flex-1 rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm font-semibold text-white placeholder-slate-500 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20"
      />
      <button
        onclick={save}
        disabled={isSaving}
        class="rounded-2xl bg-sky-500 px-5 py-2 text-sm font-semibold text-white transition hover:bg-sky-400 disabled:opacity-50"
      >
        {isSaving ? 'Saving…' : 'Save workflow'}
      </button>
    </div>

    <!-- Description -->
    <input
      type="text"
      placeholder="Optional description…"
      bind:value={wfDescription}
      class="shrink-0 rounded-xl border border-slate-800 bg-slate-900/60 px-3 py-2 text-sm text-slate-300 placeholder-slate-600 outline-none focus:border-sky-500"
    />

    {#if error}
      <p class="shrink-0 rounded-2xl bg-red-500/10 px-4 py-2 text-sm text-red-400">{error}</p>
    {/if}

    <!-- Flow diagram (read-only visual) -->
    {#if stages.length > 0}
      <div class="shrink-0 overflow-x-auto rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
        <div class="flex min-w-max items-start gap-0">
          {#each stages as s, i (s._id)}
            <!-- Stage node -->
            <button
              onclick={() => selectedId = s._id}
              class="flex flex-col items-center"
            >
              <div class="relative flex w-28 flex-col items-center rounded-2xl border px-3 py-2.5 text-center transition
                {selectedId === s._id ? 'border-sky-500 bg-sky-500/10' : 'border-slate-700 bg-slate-900 hover:border-slate-500'}">
                {#if s.isInitial}
                  <span class="mb-1 rounded-full bg-emerald-500/20 px-1.5 py-0.5 text-[10px] font-semibold text-emerald-400">START</span>
                {/if}
                {#if s.isFinal}
                  <span class="mb-1 rounded-full bg-rose-500/20 px-1.5 py-0.5 text-[10px] font-semibold text-rose-400">END</span>
                {/if}
                <p class="text-xs font-semibold leading-snug text-slate-200 {!s.name ? 'italic text-slate-500' : ''}">{s.name || 'Unnamed'}</p>
                {#if s.tasks.length > 0}
                  <p class="mt-1 text-[10px] text-slate-500">{s.tasks.length} task{s.tasks.length !== 1 ? 's' : ''}</p>
                {/if}
              </div>

              <!-- Outgoing transition labels below node -->
              {#if s.transitionsOut.length > 0}
                <div class="mt-1.5 flex flex-col items-center gap-0.5">
                  {#each s.transitionsOut as tr}
                    {@const target = stages.find(t => t._id === tr.toStageId)}
                    {#if target}
                      <span class="rounded-full bg-violet-500/10 px-2 py-0.5 text-[10px] text-violet-400">
                        → {tr.label || '…'} → {target.name || 'Unnamed'}
                      </span>
                    {/if}
                  {/each}
                </div>
              {/if}
            </button>

            <!-- Arrow between stages -->
            {#if i < stages.length - 1}
              <div class="mt-4 flex items-center px-1 text-slate-600">
                <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
                </svg>
              </div>
            {/if}
          {/each}

          <!-- Add stage button in diagram -->
          <div class="flex items-center">
            {#if stages.length > 0}<div class="mt-4 px-1 text-slate-700"><svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/></svg></div>{/if}
            <button
              aria-label="Add stage"
              onclick={addStage}
              class="mt-0 flex h-10 w-10 items-center justify-center rounded-full border border-dashed border-slate-600 text-slate-500 transition hover:border-sky-500 hover:text-sky-400"
            >
              <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
              </svg>
            </button>
          </div>
        </div>
      </div>
    {/if}

    <!-- Two-column editor -->
    <div class="flex min-h-0 flex-1 gap-4">

      <!-- Stage list (left) -->
      <div class="flex w-56 shrink-0 flex-col gap-1 overflow-y-auto rounded-2xl border border-slate-800 bg-slate-950/60 p-2">
        <p class="mb-1 px-2 text-xs font-semibold uppercase tracking-widest text-slate-600">Stages</p>
        {#each stages as s, i (s._id)}
          <!-- div instead of button to allow nested action buttons -->
          <div
            role="button"
            tabindex="0"
            onclick={() => selectedId = s._id}
            onkeydown={(e) => e.key === 'Enter' && (selectedId = s._id)}
            class="group flex w-full cursor-pointer items-center gap-2 rounded-xl px-3 py-2.5 text-left transition
              {selectedId === s._id ? 'bg-sky-500/10 text-sky-300' : 'text-slate-300 hover:bg-slate-800'}"
          >
            <span class="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-slate-800 text-[10px] font-bold text-slate-400
              {selectedId === s._id ? 'bg-sky-500/20 text-sky-400' : ''}">{i + 1}</span>
            <span class="flex-1 truncate text-sm {!s.name ? 'italic text-slate-500' : ''}">{s.name || 'Unnamed stage'}</span>
            <div class="hidden shrink-0 items-center gap-0.5 group-hover:flex">
              <button aria-label="Move stage up" onclick={(e) => { e.stopPropagation(); moveStage(s._id, -1); }} disabled={i === 0}
                class="rounded p-0.5 text-slate-500 hover:text-slate-200 disabled:opacity-30">
                <svg class="h-3 w-3" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M14.77 12.79a.75.75 0 01-1.06-.02L10 8.832 6.29 12.77a.75.75 0 11-1.08-1.04l4.25-4.5a.75.75 0 011.08 0l4.25 4.5a.75.75 0 01-.02 1.06z" clip-rule="evenodd"/></svg>
              </button>
              <button aria-label="Move stage down" onclick={(e) => { e.stopPropagation(); moveStage(s._id, 1); }} disabled={i === stages.length - 1}
                class="rounded p-0.5 text-slate-500 hover:text-slate-200 disabled:opacity-30">
                <svg class="h-3 w-3" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 11.168l3.71-3.938a.75.75 0 111.08 1.04l-4.25 4.5a.75.75 0 01-1.08 0l-4.25-4.5a.75.75 0 01.02-1.06z" clip-rule="evenodd"/></svg>
              </button>
              <button aria-label="Remove stage" onclick={(e) => { e.stopPropagation(); removeStage(s._id); }}
                class="rounded p-0.5 text-slate-600 hover:text-red-400">
                <svg class="h-3 w-3" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
              </button>
            </div>
          </div>
        {/each}
        <button
          onclick={addStage}
          class="mt-1 flex w-full items-center gap-2 rounded-xl px-3 py-2 text-sm text-slate-500 transition hover:bg-slate-800 hover:text-slate-300"
        >
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
          </svg>
          Add stage
        </button>
      </div>

      <!-- Stage detail panel (right) -->
      {#if selectedStage}
        <div class="flex min-w-0 flex-1 flex-col gap-5 overflow-y-auto rounded-2xl border border-slate-800 bg-slate-900/80 p-5">

          <!-- Stage name + toggles -->
          <div class="space-y-3">
            <div>
              <label for="wf-stage-name" class="block text-xs font-semibold uppercase tracking-wider text-slate-500">Stage name</label>
              <input
                id="wf-stage-name"
                type="text"
                placeholder="e.g. Initial Assessment"
                value={selectedStage.name}
                oninput={(e) => patchStage('name', e.target.value)}
                class="mt-1.5 w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-500 outline-none focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20"
              />
            </div>
            <div>
              <label for="wf-stage-desc" class="block text-xs font-semibold uppercase tracking-wider text-slate-500">Description</label>
              <input
                id="wf-stage-desc"
                type="text"
                placeholder="Optional description…"
                value={selectedStage.description}
                oninput={(e) => patchStage('description', e.target.value)}
                class="mt-1.5 w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-300 placeholder-slate-600 outline-none focus:border-sky-500"
              />
            </div>
            <div class="flex gap-4">
              <label class="flex cursor-pointer items-center gap-2.5">
                <div class="relative">
                  <input type="checkbox" checked={selectedStage.isInitial} onchange={(e) => patchStage('isInitial', e.target.checked)} class="sr-only" />
                  <div class="h-5 w-9 rounded-full transition {selectedStage.isInitial ? 'bg-emerald-500' : 'bg-slate-700'}"></div>
                  <div class="absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white shadow transition-transform {selectedStage.isInitial ? 'translate-x-4' : 'translate-x-0'}"></div>
                </div>
                <span class="text-sm text-slate-300">Starting stage</span>
              </label>
              <label class="flex cursor-pointer items-center gap-2.5">
                <div class="relative">
                  <input type="checkbox" checked={selectedStage.isFinal} onchange={(e) => patchStage('isFinal', e.target.checked)} class="sr-only" />
                  <div class="h-5 w-9 rounded-full transition {selectedStage.isFinal ? 'bg-rose-500' : 'bg-slate-700'}"></div>
                  <div class="absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white shadow transition-transform {selectedStage.isFinal ? 'translate-x-4' : 'translate-x-0'}"></div>
                </div>
                <span class="text-sm text-slate-300">End stage</span>
              </label>
            </div>
          </div>

          <hr class="border-slate-800" />

          <!-- Tasks -->
          <div>
            <div class="mb-3 flex items-center justify-between">
              <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Tasks</p>
              <button onclick={addTask} class="rounded-lg bg-slate-800 px-2.5 py-1 text-xs font-semibold text-slate-300 transition hover:bg-slate-700 hover:text-white">+ Add task</button>
            </div>

            {#if selectedStage.tasks.length === 0}
              <p class="text-sm text-slate-600">No tasks yet. Tasks are actions that must be completed in this stage.</p>
            {:else}
              <div class="space-y-2">
                {#each selectedStage.tasks as task (task._id)}
                  <div class="flex items-start gap-2 rounded-xl border border-slate-800 bg-slate-950/60 p-3">
                    <div class="flex-1 space-y-2">
                      <input
                        type="text"
                        placeholder="Task title…"
                        value={task.title}
                        oninput={(e) => patchTask(task._id, 'title', e.target.value)}
                        class="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm text-white placeholder-slate-500 outline-none focus:border-sky-500"
                      />
                      <input
                        type="text"
                        placeholder="Description (optional)…"
                        value={task.description}
                        oninput={(e) => patchTask(task._id, 'description', e.target.value)}
                        class="w-full rounded-lg border border-slate-800 bg-slate-900/60 px-3 py-1.5 text-xs text-slate-400 placeholder-slate-600 outline-none focus:border-sky-500"
                      />
                    </div>
                    <div class="flex shrink-0 items-center gap-2 pt-1">
                      <label class="flex cursor-pointer items-center gap-1.5">
                        <div class="relative">
                          <input type="checkbox" checked={task.required} onchange={(e) => patchTask(task._id, 'required', e.target.checked)} class="sr-only" />
                          <div class="h-4 w-7 rounded-full transition {task.required ? 'bg-sky-500' : 'bg-slate-700'}"></div>
                          <div class="absolute top-0.5 left-0.5 h-3 w-3 rounded-full bg-white shadow transition-transform {task.required ? 'translate-x-3' : 'translate-x-0'}"></div>
                        </div>
                        <span class="text-xs text-slate-500">Required</span>
                      </label>
                      <button aria-label="Remove task" onclick={() => removeTask(task._id)} class="text-slate-600 transition hover:text-red-400">
                        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
                      </button>
                    </div>
                  </div>
                {/each}
              </div>
            {/if}
          </div>

          <hr class="border-slate-800" />

          <!-- Branching paths / transitions -->
          <div>
            <div class="mb-3 flex items-center justify-between">
              <div>
                <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Branching paths</p>
                <p class="mt-0.5 text-xs text-slate-600">Define which stages this one can transition to.</p>
              </div>
              <button onclick={addTransition} class="rounded-lg bg-slate-800 px-2.5 py-1 text-xs font-semibold text-slate-300 transition hover:bg-slate-700 hover:text-white">+ Add path</button>
            </div>

            {#if selectedStage.transitionsOut.length === 0}
              <p class="text-sm text-slate-600">No paths yet. Add a path to connect this stage to another.</p>
            {:else}
              <div class="space-y-2">
                {#each selectedStage.transitionsOut as tr (tr._id)}
                  <div class="rounded-xl border border-slate-800 bg-slate-950/60 p-3 space-y-2">
                    <div class="flex items-center gap-2">
                      <!-- Label -->
                      <input
                        type="text"
                        placeholder="Path label (e.g. Approved)"
                        value={tr.label}
                        oninput={(e) => patchTransition(tr._id, 'label', e.target.value)}
                        class="flex-1 rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm text-white placeholder-slate-500 outline-none focus:border-sky-500"
                      />
                      <!-- Target stage -->
                      <select
                        value={tr.toStageId}
                        onchange={(e) => patchTransition(tr._id, 'toStageId', e.target.value)}
                        class="w-44 rounded-lg border border-slate-700 bg-slate-900 px-3 py-1.5 text-sm text-slate-300 outline-none focus:border-sky-500"
                      >
                        <option value="">— Target stage —</option>
                        {#each stages.filter(s => s._id !== selectedId) as s}
                          <option value={s._id}>{s.name || 'Unnamed stage'}</option>
                        {/each}
                      </select>
                      <button aria-label="Remove path" onclick={() => removeTransition(tr._id)} class="text-slate-600 transition hover:text-red-400">
                        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
                      </button>
                    </div>
                    <!-- Condition (optional) -->
                    <input
                      type="text"
                      placeholder="Condition (optional, e.g. Credit score ≥ 700)"
                      value={tr.condition}
                      oninput={(e) => patchTransition(tr._id, 'condition', e.target.value)}
                      class="w-full rounded-lg border border-slate-800 bg-slate-900/60 px-3 py-1.5 text-xs text-slate-400 placeholder-slate-600 outline-none focus:border-sky-500"
                    />
                  </div>
                {/each}
              </div>
            {/if}
          </div>

        </div>
      {:else}
        <div class="flex flex-1 items-center justify-center rounded-2xl border border-dashed border-slate-800 text-sm text-slate-600">
          Select a stage to edit its tasks and paths
        </div>
      {/if}
    </div>
  </div>
{/if}
