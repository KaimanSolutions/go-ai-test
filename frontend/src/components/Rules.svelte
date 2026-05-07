<script>
  import { onMount } from 'svelte';
  import { fetchRules, saveRule, deleteRule, fetchSchemas, fetchSchema } from '../lib/auth.js';

  let { token = '' } = $props();

  let view      = $state('list');
  let rules     = $state([]);
  let schemas   = $state([]);
  let loading   = $state(true);
  let saving    = $state(false);
  let error     = $state('');
  let editingId = $state(null);

  // ── List filters ───────────────────────────────────────────────────────────
  let searchQuery    = $state('');
  let formFilter     = $state('');
  let statusFilter   = $state('all');   // all | active | inactive
  let decisionFilter = $state('all');   // all | Decline | Refer

  const filteredRules = $derived.by(() => {
    const q = searchQuery.trim().toLowerCase();
    return rules.filter(rule => {
      if (formFilter && rule.formSchemaId !== formFilter) return false;
      if (statusFilter === 'active'   && !rule.isActive) return false;
      if (statusFilter === 'inactive' &&  rule.isActive) return false;
      if (decisionFilter !== 'all' && rule.decisionType !== decisionFilter) return false;
      if (q) {
        const ref  = (rule.ruleReference ?? '').toLowerCase();
        const name = (rule.name ?? '').toLowerCase();
        const desc = (rule.description ?? '').toLowerCase();
        if (!ref.includes(q) && !name.includes(q) && !desc.includes(q)) return false;
      }
      return true;
    });
  });

  const hasFilter = $derived(
    searchQuery.trim() !== '' || formFilter !== '' || statusFilter !== 'all' || decisionFilter !== 'all'
  );

  function clearFilters() {
    searchQuery = ''; formFilter = ''; statusFilter = 'all'; decisionFilter = 'all';
  }

  // ── Editor identity fields ─────────────────────────────────────────────────
  let ref          = $state('');
  let name         = $state('');
  let description  = $state('');
  let clientDesc   = $state('');
  let brokerDesc   = $state('');
  let isActive        = $state(true);
  let isClientVisible = $state(false);
  let isBrokerVisible = $state(false);
  let decisionType    = $state('Decline');
  let formSchemaId    = $state('');

  // ── Schema field list (fetched when form is chosen) ────────────────────────
  let schemaFields = $state([]); // [{ name, label }]
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

  // ── Condition groups ───────────────────────────────────────────────────────
  // Groups are OR-ed; conditions within a group are AND-ed.
  // Each condition: { leftType, leftValue, operator, rightType, rightValue, failMessage }
  let groups = $state([]);

  const OPERATORS = ['<=', '>=', '<', '>', '==', '!='];

  function newCondition() {
    return { leftType: 'field', leftValue: '', operator: '<=', rightType: 'expression', rightValue: '', failMessage: '' };
  }

  function addGroup() {
    groups = [...groups, { conditions: [newCondition()] }];
  }

  function removeGroup(gi) {
    if (groups.length === 1) return; // always keep at least one group
    groups = groups.filter((_, i) => i !== gi);
  }

  function addCondition(gi) {
    groups = groups.map((g, i) => i !== gi ? g : { ...g, conditions: [...g.conditions, newCondition()] });
  }

  function removeCondition(gi, ci) {
    const g = groups[gi];
    if (g.conditions.length === 1) { removeGroup(gi); return; }
    groups = groups.map((g, i) => i !== gi ? g : { ...g, conditions: g.conditions.filter((_, j) => j !== ci) });
  }

  function patchCond(gi, ci, key, val) {
    groups = groups.map((g, i) => i !== gi ? g : {
      ...g,
      conditions: g.conditions.map((c, j) => j !== ci ? c : { ...c, [key]: val })
    });
  }

  // ── Expression helpers ─────────────────────────────────────────────────────
  // Convert UI state → stored expression string
  function toExpr(type, value) {
    if (!value?.trim()) return '';
    return type === 'field' ? `{${value.trim()}}` : value.trim();
  }

  // Detect if a stored expression is a bare field reference
  function parseExpr(expr) {
    const m = (expr ?? '').match(/^\{([^}]+)\}$/);
    return m ? { type: 'field', value: m[1] } : { type: 'expression', value: expr ?? '' };
  }

  // ── Data loading ───────────────────────────────────────────────────────────
  async function load() {
    loading = true;
    const [r, s] = await Promise.all([fetchRules(token), fetchSchemas()]);
    loading = false;
    if (Array.isArray(r)) rules = r; else error = r.error;
    if (Array.isArray(s)) schemas = s;
  }

  function openCreate() {
    editingId = null; ref = ''; name = ''; description = '';
    clientDesc = ''; brokerDesc = ''; isActive = true; decisionType = 'Decline'; formSchemaId = '';
    groups = [{ conditions: [newCondition()] }];
    error = ''; view = 'editor';
  }

  function openEdit(rule) {
    editingId       = rule.id;
    ref             = rule.ruleReference;
    name            = rule.name;
    description     = rule.description  ?? '';
    clientDesc      = rule.clientDescription ?? '';
    brokerDesc      = rule.brokerDescription ?? '';
    isActive        = rule.isActive;
    isClientVisible = rule.isClientVisible ?? false;
    isBrokerVisible = rule.isBrokerVisible ?? false;
    decisionType    = rule.decisionType ?? 'Decline';
    formSchemaId    = rule.formSchemaId ?? '';

    // Rebuild groups from flat condition list
    const byGroup = {};
    for (const c of (rule.conditions ?? [])) {
      const g = c.conditionGroup ?? 0;
      if (!byGroup[g]) byGroup[g] = [];
      byGroup[g].push(c);
    }
    const sortedGroupKeys = Object.keys(byGroup).map(Number).sort((a, b) => a - b);
    groups = sortedGroupKeys.length
      ? sortedGroupKeys.map(gk => ({
          conditions: byGroup[gk].map(c => {
            const left  = parseExpr(c.leftExpression);
            const right = parseExpr(c.rightExpression);
            return {
              leftType:     left.type,
              leftValue:    left.value,
              operator:     c.operator,
              rightType:    right.type,
              rightValue:   right.value,
              failMessage:  c.failMessage ?? ''
            };
          })
        }))
      : [{ conditions: [newCondition()] }];

    error = ''; view = 'editor';
  }

  async function save() {
    error = '';
    if (!ref.trim())          { error = 'Rule reference is required.'; return; }
    if (!name.trim())         { error = 'Rule name is required.'; return; }
    if (!formSchemaId)        { error = 'Please select a form for this rule.'; return; }

    const allConditions = groups.flatMap((g, gi) =>
      g.conditions.map((c, ci) => ({
        conditionGroup:  gi,
        order:           ci,
        leftExpression:  toExpr(c.leftType, c.leftValue),
        operator:        c.operator,
        rightExpression: toExpr(c.rightType, c.rightValue),
        failMessage:     c.failMessage.trim() || null
      }))
    );

    if (allConditions.some(c => !c.leftExpression)) {
      error = 'All conditions must have a left-hand value.'; return;
    }

    saving = true;
    const result = await saveRule(editingId, {
      ruleReference:     ref.trim(),
      name:              name.trim(),
      description:       description.trim() || null,
      clientDescription: clientDesc.trim() || null,
      brokerDescription: brokerDesc.trim() || null,
      isActive,
      isClientVisible,
      isBrokerVisible,
      decisionType,
      formSchemaId,
      conditions:        allConditions
    }, token);
    saving = false;

    if (result.error) { error = result.error; return; }
    view = 'list';
    await load();
  }

  async function confirmDelete(id, ruleName) {
    if (!confirm(`Delete rule "${ruleName}"? This cannot be undone.`)) return;
    const result = await deleteRule(id, token);
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
        <h3 class="text-base font-semibold text-white">Business Rules</h3>
        <p class="mt-1 text-sm text-slate-400">Define policy rules that run against submitted application data.</p>
      </div>
      <button onclick={openCreate} class="rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400">
        + New Rule
      </button>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</p>
    {/if}

    {#if loading}
      <p class="py-12 text-center text-sm text-slate-500">Loading rules…</p>
    {:else if rules.length === 0}
      <div class="rounded-3xl border border-dashed border-slate-700 p-12 text-center">
        <svg class="mx-auto h-10 w-10 text-slate-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z"/>
        </svg>
        <p class="mt-3 text-sm font-medium text-slate-400">No rules yet</p>
        <p class="mt-1 text-xs text-slate-600">Create your first rule to start enforcing policy against applications.</p>
        <button onclick={openCreate} class="mt-4 rounded-2xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white hover:bg-sky-400 transition">+ New Rule</button>
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
            placeholder="Search reference, name or description…"
            bind:value={searchQuery}
            class="w-full rounded-2xl border border-slate-700 bg-slate-900 py-2.5 pl-9 pr-4 text-sm text-white placeholder-slate-500 focus:border-sky-500 focus:outline-none"
          />
          {#if searchQuery}
            <button aria-label="Clear search" onclick={() => searchQuery = ''} class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 hover:text-white">
              <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
            </button>
          {/if}
        </div>
        {#if schemas.length > 1}
          <select bind:value={formFilter} class="rounded-2xl border border-slate-700 bg-slate-900 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none">
            <option value="">All forms</option>
            {#each schemas as s}<option value={s.id}>{s.title}</option>{/each}
          </select>
        {/if}
        <select bind:value={decisionFilter} class="rounded-2xl border border-slate-700 bg-slate-900 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none">
          <option value="all">All decisions</option>
          <option value="Decline">Decline</option>
          <option value="Refer">Refer</option>
        </select>
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

      {#if filteredRules.length === 0}
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 py-12 text-center">
          <p class="text-sm font-medium text-slate-400">No rules match your filters</p>
          <button onclick={clearFilters} class="mt-2 text-xs font-medium text-sky-400 hover:text-sky-300">Clear filters</button>
        </div>
      {:else}
      <div class="overflow-hidden rounded-3xl border border-slate-800 bg-slate-900/95">
        {#if hasFilter}
          <div class="border-b border-slate-800 px-5 py-2.5">
            <span class="text-xs text-slate-500">{filteredRules.length} of {rules.length} rules</span>
          </div>
        {/if}
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-800">
              <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Ref</th>
              <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Name</th>
              <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 md:table-cell">Form</th>
              <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 sm:table-cell">Conditions</th>
              <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Status</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-800">
            {#each filteredRules as rule (rule.id)}
              <tr class="transition hover:bg-slate-800/40">
                <td class="px-5 py-4">
                  <span class="font-mono text-xs font-semibold text-sky-400">{rule.ruleReference}</span>
                </td>
                <td class="px-5 py-4">
                  <p class="font-medium text-white">{rule.name}</p>
                  {#if rule.description}<p class="mt-0.5 text-xs text-slate-500 line-clamp-1">{rule.description}</p>{/if}
                </td>
                <td class="hidden px-5 py-4 text-xs text-slate-400 md:table-cell">
                  {#if rule.formTitle}{rule.formTitle}{:else}<span class="italic text-slate-600">—</span>{/if}
                </td>
                <td class="hidden px-5 py-4 sm:table-cell">
                  <span class="text-xs text-slate-400">{(rule.conditions ?? []).length} condition{(rule.conditions ?? []).length !== 1 ? 's' : ''}</span>
                </td>
                <td class="px-5 py-4">
                  {#if rule.isActive}
                    <span class="inline-flex items-center rounded-full bg-emerald-500/15 px-2 py-0.5 text-xs font-semibold text-emerald-400">Active</span>
                  {:else}
                    <span class="inline-flex items-center rounded-full bg-slate-700/40 px-2 py-0.5 text-xs font-semibold text-slate-500">Inactive</span>
                  {/if}
                </td>
                <td class="px-5 py-4 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button onclick={() => openEdit(rule)} class="rounded-xl border border-slate-700 px-3 py-1.5 text-xs font-semibold text-slate-300 transition hover:bg-slate-800">Edit</button>
                    <button onclick={() => confirmDelete(rule.id, rule.name)} class="rounded-xl border border-red-900/40 px-3 py-1.5 text-xs font-semibold text-red-500 transition hover:bg-red-950">Delete</button>
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
        Rules
      </button>
      <span class="text-slate-700">/</span>
      <h3 class="text-base font-semibold text-white">{editingId ? 'Edit rule' : 'New rule'}</h3>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</p>
    {/if}

    <div class="grid gap-6 xl:grid-cols-3">

      <!-- Left: main content -->
      <div class="space-y-5 xl:col-span-2">

        <!-- Identity -->
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
          <p class="mb-4 text-xs font-semibold uppercase tracking-wider text-slate-500">Rule identity</p>
          <div class="grid gap-4 sm:grid-cols-2">
            <div>
              <label for="rl-ref" class={lbl}>Rule reference *</label>
              <input id="rl-ref" type="text" bind:value={ref} placeholder="e.g. BR-001" class={ic} />
            </div>
            <div>
              <label for="rl-name" class={lbl}>Rule name *</label>
              <input id="rl-name" type="text" bind:value={name} placeholder="e.g. Maximum LTV Check" class={ic} />
            </div>
          </div>
          <div class="mt-4">
            <label for="rl-desc" class={lbl}>Internal description</label>
            <textarea id="rl-desc" bind:value={description} rows="2" placeholder="Internal notes…" class="{ic} resize-none"></textarea>
          </div>
          <div class="mt-4 grid gap-4 sm:grid-cols-2">
            <div>
              <label for="rl-cdesc" class={lbl}>Client-facing description</label>
              <textarea id="rl-cdesc" bind:value={clientDesc} rows="2" placeholder="Shown to clients when rule fails…" class="{ic} resize-none"></textarea>
            </div>
            <div>
              <label for="rl-bdesc" class={lbl}>Broker-facing description</label>
              <textarea id="rl-bdesc" bind:value={brokerDesc} rows="2" placeholder="Shown to brokers when rule fails…" class="{ic} resize-none"></textarea>
            </div>
          </div>
        </div>

        <!-- Conditions -->
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
          <div class="mb-5 flex items-start justify-between gap-4">
            <div>
              <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Conditions</p>
              <p class="mt-1 text-xs text-slate-600 leading-relaxed">
                Conditions within a group are <span class="text-sky-400 font-medium">AND</span>-ed together.
                Groups are <span class="text-violet-400 font-medium">OR</span>-ed — the rule passes if any group passes.
              </p>
            </div>
          </div>

          {#if fieldsLoading}
            <p class="mb-4 text-xs text-slate-500">Loading form fields…</p>
          {/if}

          <div class="space-y-3">
            {#each groups as group, gi}

              <!-- OR separator between groups -->
              {#if gi > 0}
                <div class="flex items-center gap-3 py-1">
                  <div class="h-px flex-1 bg-slate-800"></div>
                  <span class="rounded-full bg-violet-500/15 px-3 py-0.5 text-xs font-bold text-violet-400">OR</span>
                  <div class="h-px flex-1 bg-slate-800"></div>
                  <button
                    onclick={() => removeGroup(gi)}
                    class="rounded-lg px-2 py-0.5 text-xs text-slate-600 transition hover:bg-red-500/10 hover:text-red-400"
                  >Remove group</button>
                </div>
              {/if}

              <!-- Group card -->
              <div class="rounded-2xl border border-slate-700 bg-slate-950/50 p-4 space-y-3">
                {#if groups.length > 1}
                  <p class="text-xs font-semibold text-slate-500">Group {gi + 1}</p>
                {/if}

                {#each group.conditions as cond, ci}

                  <!-- AND label between conditions -->
                  {#if ci > 0}
                    <div class="flex items-center gap-2">
                      <div class="h-px flex-1 bg-slate-800/60"></div>
                      <span class="text-xs font-bold text-sky-500">AND</span>
                      <div class="h-px flex-1 bg-slate-800/60"></div>
                    </div>
                  {/if}

                  <!-- Condition row -->
                  <div class="space-y-2">
                    <div class="flex flex-wrap items-end gap-2">

                      <!-- Left side -->
                      <div class="flex-1 min-w-[160px]">
                        <label for="rl-left-{gi}-{ci}" class="mb-1 block text-xs text-slate-500">Left side</label>
                        <div class="space-y-1">
                          <!-- Type toggle -->
                          <div class="flex rounded-lg overflow-hidden border border-slate-700 text-xs">
                            <button
                              onclick={() => patchCond(gi, ci, 'leftType', 'field')}
                              class="flex-1 py-1 font-medium transition {cond.leftType === 'field' ? 'bg-sky-500 text-white' : 'bg-slate-900 text-slate-400 hover:text-slate-200'}"
                            >Field</button>
                            <button
                              onclick={() => patchCond(gi, ci, 'leftType', 'expression')}
                              class="flex-1 py-1 font-medium transition {cond.leftType === 'expression' ? 'bg-sky-500 text-white' : 'bg-slate-900 text-slate-400 hover:text-slate-200'}"
                            >Expression</button>
                          </div>
                          <!-- Input -->
                          {#if cond.leftType === 'field'}
                            <select
                              id="rl-left-{gi}-{ci}"
                              value={cond.leftValue}
                              onchange={e => patchCond(gi, ci, 'leftValue', e.target.value)}
                              class="{ic2} w-full"
                            >
                              <option value="">Select field…</option>
                              {#each schemaFields as f}
                                <option value={f.name}>{f.label}</option>
                              {/each}
                            </select>
                          {:else}
                            <input
                              id="rl-left-{gi}-{ci}"
                              type="text"
                              value={cond.leftValue}
                              oninput={e => patchCond(gi, ci, 'leftValue', e.target.value)}
                              placeholder="{'{'}loanAmount{'}'} / {'{'}propertyValue{'}'} * 100"
                              class="{ic2} w-full"
                            />
                          {/if}
                        </div>
                      </div>

                      <!-- Operator -->
                      <div class="w-20 shrink-0">
                        <label for="rl-op-{gi}-{ci}" class="mb-1 block text-xs text-slate-500">Op</label>
                        <select
                          id="rl-op-{gi}-{ci}"
                          value={cond.operator}
                          onchange={e => patchCond(gi, ci, 'operator', e.target.value)}
                          class="{ic2} w-full"
                        >
                          {#each OPERATORS as op}
                            <option value={op}>{op}</option>
                          {/each}
                        </select>
                      </div>

                      <!-- Right side -->
                      <div class="flex-1 min-w-[160px]">
                        <label for="rl-right-{gi}-{ci}" class="mb-1 block text-xs text-slate-500">Right side</label>
                        <div class="space-y-1">
                          <!-- Type toggle -->
                          <div class="flex rounded-lg overflow-hidden border border-slate-700 text-xs">
                            <button
                              onclick={() => patchCond(gi, ci, 'rightType', 'field')}
                              class="flex-1 py-1 font-medium transition {cond.rightType === 'field' ? 'bg-sky-500 text-white' : 'bg-slate-900 text-slate-400 hover:text-slate-200'}"
                            >Field</button>
                            <button
                              onclick={() => patchCond(gi, ci, 'rightType', 'expression')}
                              class="flex-1 py-1 font-medium transition {cond.rightType === 'expression' ? 'bg-sky-500 text-white' : 'bg-slate-900 text-slate-400 hover:text-slate-200'}"
                            >Value / Expr</button>
                          </div>
                          <!-- Input -->
                          {#if cond.rightType === 'field'}
                            <select
                              id="rl-right-{gi}-{ci}"
                              value={cond.rightValue}
                              onchange={e => patchCond(gi, ci, 'rightValue', e.target.value)}
                              class="{ic2} w-full"
                            >
                              <option value="">Select field…</option>
                              {#each schemaFields as f}
                                <option value={f.name}>{f.label}</option>
                              {/each}
                            </select>
                          {:else}
                            <input
                              id="rl-right-{gi}-{ci}"
                              type="text"
                              value={cond.rightValue}
                              oninput={e => patchCond(gi, ci, 'rightValue', e.target.value)}
                              placeholder="90 or {'{'}maxLtv{'}'}"
                              class="{ic2} w-full"
                            />
                          {/if}
                        </div>
                      </div>

                      <!-- Delete condition -->
                      <div class="shrink-0 pb-px">
                        <button
                          onclick={() => removeCondition(gi, ci)}
                          class="flex h-7 w-7 items-center justify-center rounded-lg text-slate-600 transition hover:bg-red-500/10 hover:text-red-400"
                          title="Remove condition"
                        >
                          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
                          </svg>
                        </button>
                      </div>
                    </div>

                    <!-- Fail message -->
                    <input
                      type="text"
                      value={cond.failMessage}
                      oninput={e => patchCond(gi, ci, 'failMessage', e.target.value)}
                      placeholder="Fail message (optional) — shown when this condition fails"
                      class="{ic2} w-full"
                    />
                  </div>

                {/each}

                <!-- Add condition (AND) -->
                <button
                  onclick={() => addCondition(gi)}
                  class="mt-1 flex items-center gap-1.5 rounded-xl px-3 py-1.5 text-xs font-semibold text-sky-400 transition hover:bg-sky-500/10"
                >
                  <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
                  </svg>
                  AND condition
                </button>
              </div>

            {/each}

            <!-- Add OR group -->
            <div class="flex items-center gap-3 pt-1">
              <div class="h-px flex-1 bg-slate-800"></div>
              <button
                onclick={addGroup}
                class="flex items-center gap-1.5 rounded-xl border border-dashed border-violet-500/40 px-4 py-2 text-xs font-semibold text-violet-400 transition hover:border-violet-500/70 hover:bg-violet-500/5"
              >
                <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                  <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
                </svg>
                Add OR group
              </button>
              <div class="h-px flex-1 bg-slate-800"></div>
            </div>

          </div>
        </div>
      </div>

      <!-- Right: settings + save -->
      <div class="space-y-5">
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
          <p class="mb-4 text-xs font-semibold uppercase tracking-wider text-slate-500">Settings</p>

          <div class="mb-4">
            <label for="rl-form" class={lbl}>Form *</label>
            <select id="rl-form" bind:value={formSchemaId} class={ic}>
              <option value="">Select a form…</option>
              {#each schemas as s}
                <option value={s.id}>{s.title}</option>
              {/each}
            </select>
            {#if !formSchemaId}
              <p class="mt-1 text-xs text-amber-500/80">A form must be selected to use field references in conditions.</p>
            {/if}
          </div>

          <div class="space-y-3">
            <label class="flex cursor-pointer items-center gap-3">
              <div class="relative shrink-0">
                <input type="checkbox" bind:checked={isActive} class="sr-only" />
                <div class="h-5 w-9 rounded-full transition {isActive ? 'bg-emerald-500' : 'bg-slate-700'}"></div>
                <div class="absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white shadow transition-transform {isActive ? 'translate-x-4' : ''}"></div>
              </div>
              <span class="text-sm text-slate-300">Rule is active</span>
            </label>

            <div class="border-t border-slate-800 pt-3">
              <p class="mb-2 text-xs font-semibold text-slate-500">Portal visibility</p>
              <p class="mb-3 text-xs text-slate-600">When enabled, rule outcomes are shown in the respective portal. Admins always see all outcomes.</p>
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

            <div class="border-t border-slate-800 pt-3">
              <p class="mb-2 text-xs font-semibold text-slate-500">Decision on fail</p>
              <p class="mb-3 text-xs text-slate-600">What decision this rule contributes when it fails.</p>
              <div class="flex gap-2">
                <button
                  type="button"
                  onclick={() => decisionType = 'Decline'}
                  class="flex-1 rounded-xl border py-2 text-xs font-semibold transition
                    {decisionType === 'Decline' ? 'border-red-500/60 bg-red-500/10 text-red-400' : 'border-slate-700 text-slate-500 hover:border-slate-600'}"
                >Decline</button>
                <button
                  type="button"
                  onclick={() => decisionType = 'Refer'}
                  class="flex-1 rounded-xl border py-2 text-xs font-semibold transition
                    {decisionType === 'Refer' ? 'border-amber-500/60 bg-amber-500/10 text-amber-400' : 'border-slate-700 text-slate-500 hover:border-slate-600'}"
                >Refer</button>
              </div>
            </div>
          </div>
        </div>

        <!-- Expression syntax hint -->
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
          <p class="mb-3 text-xs font-semibold uppercase tracking-wider text-slate-500">Expression syntax</p>
          <div class="space-y-1.5 text-xs text-slate-400 leading-relaxed">
            <p><code class="text-sky-400">{'{'}field{'}'}</code> — field reference</p>
            <p><code class="text-sky-400">{'{'}a{'}'} / {'{'}b{'}'} * 100</code> — arithmetic</p>
            <p><code class="text-sky-400">SUM({'{'}rep.field{'}'})</code> — repeater sum</p>
            <p><code class="text-sky-400">AVG({'{'}rep.field{'}'})</code> — repeater average</p>
            <p><code class="text-sky-400">COUNT({'{'}rep{'}'})</code> — repeater count</p>
          </div>
        </div>

        <button
          onclick={save}
          disabled={saving}
          class="w-full rounded-2xl bg-sky-500 py-3 text-sm font-semibold text-white transition hover:bg-sky-400 disabled:opacity-50"
        >{saving ? 'Saving…' : editingId ? 'Save changes' : 'Create rule'}</button>
      </div>

    </div>
  </div>
{/if}
