<script>
  import { onMount } from 'svelte';
  import { fetchChecklist, saveChecklistItem, deleteChecklistItem, fetchSchemas, fetchSchema } from '../lib/auth.js';

  let { token = '' } = $props();

  let view      = $state('list');
  let items     = $state([]);
  let schemas   = $state([]);
  let loading   = $state(true);
  let saving    = $state(false);
  let error     = $state('');
  let editingId = $state(null);

  // ── List filters ───────────────────────────────────────────────────────────
  let searchQuery    = $state('');
  let typeFilter     = $state('all');    // all | Document | Information
  let formFilter     = $state('');
  let statusFilter   = $state('all');   // all | active | inactive

  const filteredItems = $derived.by(() => {
    const q = searchQuery.trim().toLowerCase();
    return items.filter(item => {
      if (typeFilter !== 'all' && item.itemType !== typeFilter) return false;
      if (formFilter && item.formSchemaId !== formFilter) return false;
      if (statusFilter === 'active'   && !item.isActive) return false;
      if (statusFilter === 'inactive' &&  item.isActive) return false;
      if (q) {
        const name = (item.name ?? '').toLowerCase();
        const desc = (item.description ?? '').toLowerCase();
        if (!name.includes(q) && !desc.includes(q)) return false;
      }
      return true;
    });
  });

  const hasFilter = $derived(
    searchQuery.trim() !== '' || typeFilter !== 'all' || formFilter !== '' || statusFilter !== 'all'
  );

  function clearFilters() {
    searchQuery = ''; typeFilter = 'all'; formFilter = ''; statusFilter = 'all';
  }

  // ── Editor fields ──────────────────────────────────────────────────────────
  let name            = $state('');
  let description     = $state('');
  let itemType        = $state('Document');
  let formSchemaId    = $state('');
  let isActive        = $state(true);
  let isClientVisible = $state(false);
  let isBrokerVisible = $state(false);

  // ── Conditions ─────────────────────────────────────────────────────────────
  let conditions = $state([]);

  function newCondition() {
    return { fieldName: '', operator: 'equals', value: '' };
  }

  function addCondition() {
    conditions = [...conditions, newCondition()];
  }

  function removeCondition(i) {
    conditions = conditions.filter((_, idx) => idx !== i);
  }

  function patchCond(i, key, val) {
    conditions = conditions.map((c, idx) => idx !== i ? c : { ...c, [key]: val });
  }

  // ── Schema field list (fetched when form is chosen) ────────────────────────
  let schemaFields  = $state([]);
  let fieldsLoading = $state(false);

  $effect(() => {
    if (formSchemaId && token) loadSchemaFields(formSchemaId);
    else schemaFields = [];
  });

  async function loadSchemaFields(id) {
    fieldsLoading = true;
    const res = await fetchSchema(id);
    fieldsLoading = false;
    if (res?.steps) {
      schemaFields = res.steps.flatMap(s =>
        (s.fields ?? [])
          .filter(f => f.type !== 'info' && f.name)
          .map(f => ({ name: f.name, label: f.label || f.name }))
      );
    }
  }

  // ── Data loading ───────────────────────────────────────────────────────────
  async function load() {
    loading = true;
    const [cl, sc] = await Promise.all([fetchChecklist(token), fetchSchemas()]);
    loading = false;
    if (Array.isArray(cl)) items = cl; else error = cl.error;
    if (Array.isArray(sc)) schemas = sc;
  }

  function openCreate() {
    editingId       = null;
    name            = '';
    description     = '';
    itemType        = 'Document';
    formSchemaId    = '';
    isActive        = true;
    isClientVisible = false;
    isBrokerVisible = false;
    conditions      = [];
    error           = '';
    view            = 'editor';
  }

  function openEdit(item) {
    editingId       = item.id;
    name            = item.name;
    description     = item.description ?? '';
    itemType        = item.itemType ?? 'Document';
    formSchemaId    = item.formSchemaId ?? '';
    isActive        = item.isActive;
    isClientVisible = item.isClientVisible ?? false;
    isBrokerVisible = item.isBrokerVisible ?? false;
    conditions      = (item.conditions ?? []).map(c => ({
      fieldName: c.fieldName,
      operator:  c.operator,
      value:     c.value
    }));
    error = '';
    view  = 'editor';
  }

  async function save() {
    error = '';
    if (!name.trim())     { error = 'Name is required.'; return; }
    if (!formSchemaId)    { error = 'Please select a form for this checklist item.'; return; }
    if (!itemType)        { error = 'Item type is required.'; return; }

    saving = true;
    const payload = {
      id:             editingId ?? undefined,
      name:           name.trim(),
      description:    description.trim(),
      itemType,
      formSchemaId,
      isActive,
      isClientVisible,
      isBrokerVisible,
      conditions: conditions
        .filter(c => c.fieldName.trim())
        .map(c => ({ fieldName: c.fieldName.trim(), operator: c.operator, value: c.value.trim() }))
    };

    const result = await saveChecklistItem(payload, token);
    saving = false;

    if (result.error) { error = result.error; return; }
    view = 'list';
    await load();
  }

  async function confirmDelete(id, itemName) {
    if (!confirm(`Delete checklist item "${itemName}"? This cannot be undone.`)) return;
    const result = await deleteChecklistItem(id, token);
    if (result.error) { error = result.error; return; }
    await load();
  }

  onMount(load);

  // ── Styles ─────────────────────────────────────────────────────────────────
  const ic  = 'w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none';
  const ic2 = 'rounded-xl border border-slate-700 bg-slate-950 px-2.5 py-1.5 text-xs text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none';
  const lbl = 'block text-xs font-semibold uppercase tracking-wider text-slate-500 mb-1.5';
</script>

<!-- ── List ─────────────────────────────────────────────────────────────── -->
{#if view === 'list'}
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h3 class="text-base font-semibold text-white">Checklist Items</h3>
        <p class="mt-1 text-sm text-slate-400">Define documents and information required for mortgage applications.</p>
      </div>
      <button onclick={openCreate} class="rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400">
        + New Item
      </button>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</p>
    {/if}

    {#if loading}
      <p class="py-12 text-center text-sm text-slate-500">Loading checklist…</p>
    {:else if items.length === 0}
      <div class="rounded-3xl border border-dashed border-slate-700 p-12 text-center">
        <svg class="mx-auto h-10 w-10 text-slate-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4"/>
        </svg>
        <p class="mt-3 text-sm font-medium text-slate-400">No checklist items yet</p>
        <p class="mt-1 text-xs text-slate-600">Create your first checklist item to define required documents and information.</p>
        <button onclick={openCreate} class="mt-4 rounded-2xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white hover:bg-sky-400 transition">+ New Item</button>
      </div>
    {:else}
      <!-- Filters -->
      <div class="flex flex-wrap items-center gap-3">
        <div class="relative min-w-48 flex-1">
          <svg class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd"/>
          </svg>
          <input
            type="text"
            placeholder="Search name or description…"
            bind:value={searchQuery}
            class="w-full rounded-2xl border border-slate-700 bg-slate-900 py-2.5 pl-9 pr-4 text-sm text-white placeholder-slate-500 focus:border-sky-500 focus:outline-none"
          />
          {#if searchQuery}
            <button onclick={() => searchQuery = ''} class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 hover:text-white">
              <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
            </button>
          {/if}
        </div>
        <select bind:value={typeFilter} class="rounded-2xl border border-slate-700 bg-slate-900 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none">
          <option value="all">All types</option>
          <option value="Document">Document</option>
          <option value="Information">Information</option>
        </select>
        {#if schemas.length > 1}
          <select bind:value={formFilter} class="rounded-2xl border border-slate-700 bg-slate-900 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none">
            <option value="">All forms</option>
            {#each schemas as s}<option value={s.id}>{s.title}</option>{/each}
          </select>
        {/if}
        <select bind:value={statusFilter} class="rounded-2xl border border-slate-700 bg-slate-900 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none">
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
        {#if hasFilter}
          <button onclick={clearFilters} class="flex items-center gap-1.5 rounded-2xl border border-slate-700 px-3 py-2.5 text-sm font-medium text-slate-400 transition hover:text-white">
            <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
            Clear
          </button>
        {/if}
      </div>

      {#if filteredItems.length === 0}
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 py-12 text-center">
          <p class="text-sm font-medium text-slate-400">No items match your filters</p>
          <button onclick={clearFilters} class="mt-2 text-xs font-medium text-sky-400 hover:text-sky-300">Clear filters</button>
        </div>
      {:else}
      <div class="overflow-hidden rounded-3xl border border-slate-800 bg-slate-900/95">
        {#if hasFilter}
          <div class="border-b border-slate-800 px-5 py-2.5">
            <span class="text-xs text-slate-500">{filteredItems.length} of {items.length} items</span>
          </div>
        {/if}
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-800">
              <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Name</th>
              <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Type</th>
              <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 md:table-cell">Form</th>
              <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 sm:table-cell">Visibility</th>
              <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Status</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-800">
            {#each filteredItems as item (item.id)}
              <tr class="transition hover:bg-slate-800/40">
                <td class="px-5 py-4">
                  <p class="font-medium text-white">{item.name}</p>
                  {#if item.description}<p class="mt-0.5 text-xs text-slate-500 line-clamp-1">{item.description}</p>{/if}
                </td>
                <td class="px-5 py-4">
                  {#if item.itemType === 'Document'}
                    <span class="inline-flex items-center rounded-full bg-sky-500/15 px-2 py-0.5 text-xs font-semibold text-sky-400">Document</span>
                  {:else}
                    <span class="inline-flex items-center rounded-full bg-amber-500/15 px-2 py-0.5 text-xs font-semibold text-amber-400">Information</span>
                  {/if}
                </td>
                <td class="hidden px-5 py-4 text-xs text-slate-400 md:table-cell">
                  {#if item.formTitle}{item.formTitle}{:else}<span class="italic text-slate-600">—</span>{/if}
                </td>
                <td class="hidden px-5 py-4 sm:table-cell">
                  <div class="flex items-center gap-1.5">
                    {#if item.isClientVisible}
                      <span class="inline-flex items-center rounded-full bg-sky-500/15 px-2 py-0.5 text-xs font-semibold text-sky-400">Client</span>
                    {/if}
                    {#if item.isBrokerVisible}
                      <span class="inline-flex items-center rounded-full bg-violet-500/15 px-2 py-0.5 text-xs font-semibold text-violet-400">Broker</span>
                    {/if}
                    {#if !item.isClientVisible && !item.isBrokerVisible}
                      <span class="text-xs text-slate-600">Admin only</span>
                    {/if}
                  </div>
                </td>
                <td class="px-5 py-4">
                  {#if item.isActive}
                    <span class="inline-flex items-center rounded-full bg-emerald-500/15 px-2 py-0.5 text-xs font-semibold text-emerald-400">Active</span>
                  {:else}
                    <span class="inline-flex items-center rounded-full bg-slate-700/40 px-2 py-0.5 text-xs font-semibold text-slate-500">Inactive</span>
                  {/if}
                </td>
                <td class="px-5 py-4 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button onclick={() => openEdit(item)} class="rounded-xl border border-slate-700 px-3 py-1.5 text-xs font-semibold text-slate-300 transition hover:bg-slate-800">Edit</button>
                    <button onclick={() => confirmDelete(item.id, item.name)} class="rounded-xl border border-red-900/40 px-3 py-1.5 text-xs font-semibold text-red-500 transition hover:bg-red-950">Delete</button>
                  </div>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
      {/if}
    {/if}
  </div>

<!-- ── Editor ────────────────────────────────────────────────────────────── -->
{:else}
  <div class="space-y-6">
    <!-- Back + title -->
    <div class="flex items-center gap-4">
      <button onclick={() => { view = 'list'; error = ''; }} class="flex items-center gap-1.5 text-xs font-medium text-slate-400 transition hover:text-white">
        <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
        </svg>
        Checklist
      </button>
      <span class="text-slate-700">/</span>
      <h3 class="text-base font-semibold text-white">{editingId ? 'Edit item' : 'New item'}</h3>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</p>
    {/if}

    <div class="grid gap-6 xl:grid-cols-3">

      <!-- Left: main content -->
      <div class="space-y-5 xl:col-span-2">

        <!-- Identity -->
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
          <p class="mb-4 text-xs font-semibold uppercase tracking-wider text-slate-500">Item details</p>
          <div class="space-y-4">
            <div>
              <label class={lbl}>Name *</label>
              <input type="text" bind:value={name} placeholder="e.g. Proof of Income" class={ic} />
            </div>
            <div>
              <label class={lbl}>Description</label>
              <textarea bind:value={description} rows="2" placeholder="Describe what is required…" class="{ic} resize-none"></textarea>
            </div>
          </div>
        </div>

        <!-- Conditions -->
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
          <div class="mb-5 flex items-start justify-between gap-4">
            <div>
              <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Conditions</p>
              <p class="mt-1 text-xs text-slate-600 leading-relaxed">
                Optionally restrict when this item is required based on form field values.
              </p>
            </div>
          </div>

          {#if fieldsLoading}
            <p class="mb-4 text-xs text-slate-500">Loading form fields…</p>
          {/if}

          {#if !formSchemaId}
            <p class="mb-4 rounded-xl bg-amber-500/10 px-3 py-2 text-xs text-amber-400/80">Select a form in the settings panel to add field-based conditions.</p>
          {/if}

          <div class="space-y-3">
            {#each conditions as cond, i}
              <div class="flex flex-wrap items-end gap-2 rounded-2xl border border-slate-700 bg-slate-950/50 p-3">
                <!-- Field -->
                <div class="flex-1 min-w-[160px]">
                  <label class="mb-1 block text-xs text-slate-500">Field</label>
                  {#if schemaFields.length > 0}
                    <select
                      value={cond.fieldName}
                      onchange={e => patchCond(i, 'fieldName', e.target.value)}
                      class="{ic2} w-full"
                    >
                      <option value="">Select field…</option>
                      {#each schemaFields as f}
                        <option value={f.name}>{f.label}</option>
                      {/each}
                    </select>
                  {:else}
                    <input
                      type="text"
                      value={cond.fieldName}
                      oninput={e => patchCond(i, 'fieldName', e.target.value)}
                      placeholder="Field name"
                      class="{ic2} w-full"
                    />
                  {/if}
                </div>

                <!-- Operator -->
                <div class="w-32 shrink-0">
                  <label class="mb-1 block text-xs text-slate-500">Operator</label>
                  <select
                    value={cond.operator}
                    onchange={e => patchCond(i, 'operator', e.target.value)}
                    class="{ic2} w-full"
                  >
                    <option value="equals">equals</option>
                    <option value="notEquals">not equals</option>
                    <option value="contains">contains</option>
                  </select>
                </div>

                <!-- Value -->
                <div class="flex-1 min-w-[120px]">
                  <label class="mb-1 block text-xs text-slate-500">Value</label>
                  <input
                    type="text"
                    value={cond.value}
                    oninput={e => patchCond(i, 'value', e.target.value)}
                    placeholder="e.g. yes"
                    class="{ic2} w-full"
                  />
                </div>

                <!-- Remove -->
                <div class="shrink-0 pb-px">
                  <button
                    onclick={() => removeCondition(i)}
                    class="flex h-7 w-7 items-center justify-center rounded-lg text-slate-600 transition hover:bg-red-500/10 hover:text-red-400"
                    title="Remove condition"
                  >
                    <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                      <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
                    </svg>
                  </button>
                </div>
              </div>
            {/each}

            <button
              onclick={addCondition}
              class="flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-semibold text-sky-400 transition hover:bg-sky-500/10"
            >
              <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
              </svg>
              Add condition
            </button>
          </div>
        </div>
      </div>

      <!-- Right: settings + save -->
      <div class="space-y-5">
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
          <p class="mb-4 text-xs font-semibold uppercase tracking-wider text-slate-500">Settings</p>

          <div class="mb-4">
            <label class={lbl}>Item type *</label>
            <select bind:value={itemType} class={ic}>
              <option value="Document">Document</option>
              <option value="Information">Information</option>
            </select>
          </div>

          <div class="mb-4">
            <label class={lbl}>Form *</label>
            <select bind:value={formSchemaId} class={ic}>
              <option value="">Select a form…</option>
              {#each schemas as s}
                <option value={s.id}>{s.title}</option>
              {/each}
            </select>
            {#if !formSchemaId}
              <p class="mt-1 text-xs text-amber-500/80">A form must be selected for this checklist item.</p>
            {/if}
          </div>

          <div class="space-y-3">
            <label class="flex cursor-pointer items-center gap-3">
              <div class="relative shrink-0">
                <input type="checkbox" bind:checked={isActive} class="sr-only" />
                <div class="h-5 w-9 rounded-full transition {isActive ? 'bg-emerald-500' : 'bg-slate-700'}"></div>
                <div class="absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white shadow transition-transform {isActive ? 'translate-x-4' : ''}"></div>
              </div>
              <span class="text-sm text-slate-300">Item is active</span>
            </label>

            <div class="border-t border-slate-800 pt-3">
              <p class="mb-2 text-xs font-semibold text-slate-500">Portal visibility</p>
              <p class="mb-3 text-xs text-slate-600">When enabled, this item is shown in the respective portal.</p>
              <label class="flex cursor-pointer items-center gap-3 mb-2">
                <div class="relative shrink-0">
                  <input type="checkbox" bind:checked={isClientVisible} class="sr-only" />
                  <div class="h-5 w-9 rounded-full transition {isClientVisible ? 'bg-sky-500' : 'bg-slate-700'}"></div>
                  <div class="absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white shadow transition-transform {isClientVisible ? 'translate-x-4' : ''}"></div>
                </div>
                <span class="text-sm text-slate-300">Visible to clients</span>
              </label>
              <label class="flex cursor-pointer items-center gap-3">
                <div class="relative shrink-0">
                  <input type="checkbox" bind:checked={isBrokerVisible} class="sr-only" />
                  <div class="h-5 w-9 rounded-full transition {isBrokerVisible ? 'bg-violet-500' : 'bg-slate-700'}"></div>
                  <div class="absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white shadow transition-transform {isBrokerVisible ? 'translate-x-4' : ''}"></div>
                </div>
                <span class="text-sm text-slate-300">Visible to brokers</span>
              </label>
            </div>
          </div>
        </div>

        <button
          onclick={save}
          disabled={saving}
          class="w-full rounded-2xl bg-sky-500 py-3 text-sm font-semibold text-white transition hover:bg-sky-400 disabled:opacity-50"
        >{saving ? 'Saving…' : editingId ? 'Save changes' : 'Create item'}</button>

        <button
          onclick={() => { view = 'list'; error = ''; }}
          class="w-full rounded-2xl border border-slate-700 py-3 text-sm font-semibold text-slate-400 transition hover:bg-slate-800 hover:text-white"
        >Cancel</button>
      </div>

    </div>
  </div>
{/if}
