<script>
  import { onMount } from 'svelte';
  import { fetchApplications, fetchApplication, createApplication, updateApplication, fetchSchemas, evaluateRules, fetchRuleOutcomes, fetchApplicationChecklist, generateApplicationChecklist, respondToChecklistItem, uploadChecklistDocument, updateChecklistItemStatus, addChecklistComment } from '../lib/auth.js';

  let { token, user } = $props();

  // ── State ─────────────────────────────────────────────────────────────────────
  let view            = $state('list');   // 'list' | 'new' | 'form' | 'submitted'
  let applications    = $state([]);
  let selected        = $state(null);
  let schemas         = $state([]);
  let selectedFormId  = $state('');
  let saving          = $state(false);
  let loading         = $state(true);
  let error           = $state('');
  let formValues      = $state({});
  let currentStep     = $state(0);
  let toast           = $state('');
  let toastTimer      = null;
  let ruleOutcomes        = $state([]);  // persisted history from /api/rules/outcomes
  let rulesLoading        = $state(false);
  let selectedRuleOutcome = $state(null); // outcome shown in detail panel

  // ── List filters ──────────────────────────────────────────────────────────────
  let searchQuery  = $state('');
  let statusFilter = $state('all');   // 'all' | 'submitted' | 'in-progress'
  let formFilter   = $state('');      // '' | formSchemaId

  const filteredApplications = $derived.by(() => {
    const q = searchQuery.trim().toLowerCase();
    return applications.filter(app => {
      if (statusFilter === 'submitted'   && !app.submittedAt) return false;
      if (statusFilter === 'in-progress' &&  app.submittedAt) return false;
      if (formFilter && app.formSchemaId !== formFilter)       return false;
      if (q) {
        const ref   = (app.publicReference ?? '').toLowerCase();
        const title = (app.formTitle ?? '').toLowerCase();
        if (!ref.includes(q) && !title.includes(q)) return false;
      }
      return true;
    });
  });

  const hasActiveFilter = $derived(
    searchQuery.trim() !== '' || statusFilter !== 'all' || formFilter !== ''
  );

  function clearFilters() {
    searchQuery  = '';
    statusFilter = 'all';
    formFilter   = '';
  }

  let checklistItems     = $state([]);
  let checklistLoading   = $state(false);
  let checklistResponses = $state({});   // { [itemId]: string } draft text responses
  let checklistUploading = $state({});   // { [itemId]: boolean }
  let checklistComments  = $state({});   // { [itemId]: string } draft comments

  function showToast(msg) {
    toast = msg;
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => toast = '', 3000);
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────────
  onMount(async () => {
    await Promise.all([loadApplications(), loadSchemas()]);
  });

  async function loadApplications() {
    loading = true;
    const res = await fetchApplications(token);
    loading = false;
    if (res.error) { error = res.error; return; }
    applications = res;
  }

  async function loadSchemas() {
    const res = await fetchSchemas();
    if (!res.error) schemas = res;
  }

  // ── New application ────────────────────────────────────────────────────────────
  async function submitNew() {
    if (!selectedFormId) return;
    saving = true; error = '';
    const res = await createApplication(selectedFormId, token);
    saving = false;
    if (res.error) { error = res.error; return; }
    await loadApplications();
    await openDetail(res.id);
  }

  // ── Open detail — route based on submission status ────────────────────────────
  async function openDetail(id) {
    loading = true; error = '';
    const res = await fetchApplication(id, token);
    loading = false;
    if (res.error) { error = res.error; return; }
    selected    = res;
    formValues  = res.formData ? JSON.parse(res.formData) : {};

    // Ensure every repeater field has at least one empty row
    for (const step of (res.formSchema?.steps ?? [])) {
      for (const field of (step.fields ?? [])) {
        if (field.type === 'repeater' && !Array.isArray(formValues[field.name])) {
          formValues[field.name] = [makeEmptyRepeaterItem(field.subFields)];
        }
      }
    }
    currentStep = 0;
    ruleOutcomes        = [];
    selectedRuleOutcome = null;
    view                = res.submittedAt ? 'submitted' : 'form';

    // Load persisted rule outcome history and checklist for submitted applications
    if (res.submittedAt) {
      rulesLoading = true;
      const ro = await fetchRuleOutcomes(res.id, token);
      rulesLoading = false;
      if (Array.isArray(ro)) ruleOutcomes = ro;

      checklistLoading = true;
      await generateApplicationChecklist(res.id, token);
      const cl = await fetchApplicationChecklist(res.id, token);
      checklistLoading = false;
      if (Array.isArray(cl)) {
        checklistItems = cl;
        checklistResponses = {};
        checklistUploading = {};
        checklistComments  = {};
      }
    }
  }

  // ── Step navigation ────────────────────────────────────────────────────────────
  function stepVisible(step) {
    if (!step.conditions?.length) return true;
    return step.conditions.every(c => condMatch(c, formValues));
  }

  function condMatch(c, values) {
    const v = values[c.fieldName];
    if (c.operator === 'equals')    return String(v ?? '') === String(c.value);
    if (c.operator === 'notEquals') return String(v ?? '') !== String(c.value);
    if (c.operator === 'contains')  return String(v ?? '').includes(String(c.value));
    return true;
  }

  const steps      = $derived((selected?.formSchema?.steps ?? []).filter(stepVisible));
  const totalSteps = $derived(steps.length);
  const isLastStep = $derived(currentStep === totalSteps - 1);

  const overallDecision = $derived.by(() => {
    const failed = ruleOutcomes.filter(o => !o.current.passed);
    if (failed.some(o => (o.decisionType ?? 'Decline') === 'Decline')) return 'Decline';
    if (failed.some(o => o.decisionType === 'Refer')) return 'Refer';
    return 'Accept';
  });

  // ── Checklist helpers ──────────────────────────────────────────────────────────
  async function submitChecklistResponse(item) {
    const text = checklistResponses[item.id] ?? '';
    if (!text.trim()) return;
    const res = await respondToChecklistItem(item.id, text, token);
    if (!res.error) {
      checklistItems = checklistItems.map(i =>
        i.id === item.id ? { ...i, status: 'Pending Review', textResponse: text, completedAt: new Date().toISOString() } : i
      );
      checklistResponses = { ...checklistResponses, [item.id]: '' };
    }
  }

  async function handleDocumentUpload(item, file) {
    if (!file) return;
    checklistUploading = { ...checklistUploading, [item.id]: true };
    const res = await uploadChecklistDocument(item.id, file, token);
    checklistUploading = { ...checklistUploading, [item.id]: false };
    if (!res.error) {
      checklistItems = checklistItems.map(i =>
        i.id === item.id ? { ...i, status: 'Pending Review', documentName: file.name, completedAt: new Date().toISOString() } : i
      );
    }
  }

  async function changeChecklistStatus(item, newStatus) {
    const res = await updateChecklistItemStatus(item.id, newStatus, token);
    if (!res.error) {
      checklistItems = checklistItems.map(i =>
        i.id === item.id ? { ...i, status: newStatus } : i
      );
    }
  }

  async function submitChecklistComment(item) {
    const text = checklistComments[item.id] ?? '';
    if (!text.trim()) return;
    const now = new Date().toISOString();
    const res = await addChecklistComment(item.id, text, token);
    if (!res.error) {
      const newComment = { comment: text, authorName: user?.userName ?? 'You', createdAt: now };
      checklistItems = checklistItems.map(i =>
        i.id === item.id ? { ...i, comments: [...(i.comments ?? []), newComment] } : i
      );
      checklistComments = { ...checklistComments, [item.id]: '' };
    }
  }

  const API_BASE = '/api';

  // ── Repeater helpers ───────────────────────────────────────────────────────────
  function makeEmptyRepeaterItem(subFields) {
    const item = {};
    (subFields ?? []).forEach(sf => { item[sf.name] = sf.type === 'checkbox' ? false : ''; });
    return item;
  }

  function addRepeaterItem(fieldName, subFields) {
    formValues = { ...formValues, [fieldName]: [...(formValues[fieldName] ?? []), makeEmptyRepeaterItem(subFields)] };
  }

  function removeRepeaterItem(fieldName, index) {
    const items = (formValues[fieldName] ?? []).filter((_, i) => i !== index);
    formValues = { ...formValues, [fieldName]: items };
  }

  // ── Validation ─────────────────────────────────────────────────────────────────
  function validateStep(stepIndex) {
    const step = steps[stepIndex];
    const errors = [];

    for (const field of (step?.fields ?? [])) {
      if (field.type === 'info' || !isVisible(field)) continue;

      if (field.type === 'repeater') {
        const items = formValues[field.name] ?? [];
        items.forEach(item => {
          (field.subFields ?? []).forEach(sf => {
            if (sf.required && (item[sf.name] === '' || item[sf.name] === undefined || item[sf.name] === null)) {
              errors.push(`${sf.label} is required`);
            }
          });
        });
        continue;
      }

      const val = formValues[field.name];
      const empty = val === undefined || val === null || val === '' || val === false;

      if (field.required && empty) {
        errors.push(`${field.label} is required`);
        continue; // skip validators when empty — required error is enough
      }

      if (empty) continue; // no value, nothing to validate further

      for (const v of (field.validators ?? [])) {
        // Respect validator visibility conditions
        if (v.conditions?.length && !v.conditions.every(c => condMatch(c, formValues))) continue;

        let failed = false;
        const strVal = String(val);
        const numVal = Number(val);
        const ruleNum = Number(v.value);

        switch (v.ruleType) {
          case 'min':
            if (!isNaN(numVal) && !isNaN(ruleNum) && numVal < ruleNum) failed = true;
            break;
          case 'max':
            if (!isNaN(numVal) && !isNaN(ruleNum) && numVal > ruleNum) failed = true;
            break;
          case 'minLength':
            if (strVal.length < ruleNum) failed = true;
            break;
          case 'maxLength':
            if (strVal.length > ruleNum) failed = true;
            break;
          case 'regex':
            try { if (v.value && !new RegExp(v.value).test(strVal)) failed = true; } catch {}
            break;
        }

        if (failed) errors.push(v.message || `${field.label} is invalid`);
      }
    }

    return errors;
  }

  async function goNext() {
    error = '';
    const errors = validateStep(currentStep);
    if (errors.length) {
      error = errors.join(' · ');
      return;
    }
    await silentSave();
    currentStep = Math.min(currentStep + 1, totalSteps - 1);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function goBack() {
    currentStep = Math.max(currentStep - 1, 0);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  async function silentSave() {
    if (!selected) return;
    await updateApplication(selected.id, { formData: JSON.stringify(formValues) }, token);
  }

  // ── Submit form ────────────────────────────────────────────────────────────────
  async function submitForm() {
    if (!selected) return;

    error = '';
    const errors = validateStep(currentStep);
    if (errors.length) {
      error = errors.join(' · ');
      return;
    }

    saving = true;
    const res = await updateApplication(selected.id, {
      formData: JSON.stringify(formValues),
      submit:   true
    }, token);
    saving = false;
    if (res.error) { error = res.error; return; }
    selected = { ...selected, ...res, submittedAt: res.submittedAt };
    // Evaluate and persist rule outcomes for the newly submitted application
    if (selected.formSchemaId) {
      await evaluateRules(selected.formSchemaId, JSON.stringify(formValues), token,
        selected.id, selected.currentStageId ?? null);
    }
    await openDetail(selected.id);
    showToast('Application submitted');
  }

  // ── Stage advance ──────────────────────────────────────────────────────────────
  async function goToStage(stageId) {
    if (!selected) return;
    saving = true; error = '';
    const res = await updateApplication(selected.id, { currentStageId: stageId }, token);
    saving = false;
    if (res.error) { error = res.error; return; }
    // Re-evaluate rules now the application is at a new stage
    if (selected.formSchemaId && selected.formData) {
      await evaluateRules(selected.formSchemaId, selected.formData, token,
        selected.id, stageId);
    }
    await openDetail(selected.id);
    showToast('Stage updated');
  }

  // ── Helpers ────────────────────────────────────────────────────────────────────
  function formatDate(d) {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  function formatFieldDate(iso) {
    if (!iso) return '—';
    const [y, m, d] = iso.split('-');
    return d && m && y ? `${d}/${m}/${y}` : iso;
  }

  function formatDateTime(d) {
    if (!d) return '—';
    return new Date(d).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  function isVisible(field) {
    return !field.conditions?.length || field.conditions.every(c => condMatch(c, formValues));
  }

  function displayValue(field) {
    const v = formValues[field.name];
    if (v === undefined || v === null || v === '') return '—';
    if (field.type === 'checkbox') return v ? 'Yes' : 'No';
    return String(v);
  }

  function stagesOrdered(wf) {
    return [...(wf?.stages ?? [])].sort((a, b) => a.order - b.order);
  }

  function nextStages(wf, currentStageId) {
    const stage = wf?.stages?.find(s => s.id === currentStageId);
    if (!stage) return [];
    const ids = (stage.transitionsOut ?? []).map(t => t.toStageId);
    return wf.stages.filter(s => ids.includes(s.id));
  }

  const backBtn = 'flex items-center gap-1.5 text-xs font-medium text-slate-400 hover:text-white transition';
</script>

<!-- ── Toast ───────────────────────────────────────────────────────────────────── -->
{#if toast}
  <div class="fixed bottom-6 right-6 z-50 flex items-center gap-2.5 rounded-2xl border border-emerald-500/30 bg-slate-900 px-4 py-3 shadow-2xl">
    <div class="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-emerald-500/20">
      <svg class="h-3 w-3 text-emerald-400" viewBox="0 0 12 12" fill="currentColor">
        <path d="M10.28 2.28a.75.75 0 00-1.06 0L4.5 7 2.78 5.28a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.06 0l5.25-5.25a.75.75 0 000-1.06z"/>
      </svg>
    </div>
    <span class="text-sm font-medium text-white">{toast}</span>
  </div>
{/if}

<!-- ── List ────────────────────────────────────────────────────────────────────── -->
{#if view === 'list'}
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h2 class="text-lg font-semibold text-white">Applications</h2>
        <p class="mt-0.5 text-sm text-slate-400">All mortgage applications you have access to.</p>
      </div>
      <button
        onclick={() => { view = 'new'; selectedFormId = schemas[0]?.id ?? ''; error = ''; }}
        class="flex items-center gap-2 rounded-2xl bg-sky-500 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400"
      >
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path d="M10.75 4.75a.75.75 0 00-1.5 0v4.5h-4.5a.75.75 0 000 1.5h4.5v4.5a.75.75 0 001.5 0v-4.5h4.5a.75.75 0 000-1.5h-4.5v-4.5z"/>
        </svg>
        New application
      </button>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</p>
    {/if}

    <!-- Filters -->
    {#if !loading && applications.length > 0}
      <div class="flex flex-wrap items-center gap-3">
        <!-- Search -->
        <div class="relative min-w-48 flex-1">
          <svg class="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-500" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M9 3.5a5.5 5.5 0 100 11 5.5 5.5 0 000-11zM2 9a7 7 0 1112.452 4.391l3.328 3.329a.75.75 0 11-1.06 1.06l-3.329-3.328A7 7 0 012 9z" clip-rule="evenodd"/>
          </svg>
          <input
            type="text"
            placeholder="Search reference or form…"
            bind:value={searchQuery}
            class="w-full rounded-2xl border border-slate-700 bg-slate-900 py-2.5 pl-9 pr-4 text-sm text-white placeholder-slate-500 focus:border-sky-500 focus:outline-none"
          />
          {#if searchQuery}
            <button onclick={() => searchQuery = ''} class="absolute right-3 top-1/2 -translate-y-1/2 text-slate-500 hover:text-white">
              <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
              </svg>
            </button>
          {/if}
        </div>

        <!-- Status -->
        <select
          bind:value={statusFilter}
          class="rounded-2xl border border-slate-700 bg-slate-900 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none"
        >
          <option value="all">All statuses</option>
          <option value="submitted">Submitted</option>
          <option value="in-progress">In progress</option>
        </select>

        <!-- Form -->
        {#if schemas.length > 1}
          <select
            bind:value={formFilter}
            class="rounded-2xl border border-slate-700 bg-slate-900 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none"
          >
            <option value="">All forms</option>
            {#each schemas as s}
              <option value={s.id}>{s.title}</option>
            {/each}
          </select>
        {/if}

        <!-- Clear -->
        {#if hasActiveFilter}
          <button
            onclick={clearFilters}
            class="flex items-center gap-1.5 rounded-2xl border border-slate-700 px-3 py-2.5 text-sm font-medium text-slate-400 transition hover:border-slate-600 hover:text-white"
          >
            <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
              <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
            </svg>
            Clear filters
          </button>
        {/if}
      </div>
    {/if}

    {#if loading}
      <div class="flex items-center justify-center py-16">
        <div class="h-6 w-6 animate-spin rounded-full border-2 border-sky-500 border-t-transparent"></div>
      </div>
    {:else if applications.length === 0}
      <div class="flex flex-col items-center justify-center rounded-3xl border border-slate-800 bg-slate-900/95 py-16 text-center">
        <div class="flex h-14 w-14 items-center justify-center rounded-full bg-slate-800">
          <svg class="h-6 w-6 text-slate-500" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
          </svg>
        </div>
        <p class="mt-4 text-sm font-medium text-slate-300">No applications yet</p>
        <p class="mt-1 text-xs text-slate-500">Create your first application to get started.</p>
        <button
          onclick={() => { view = 'new'; selectedFormId = schemas[0]?.id ?? ''; error = ''; }}
          class="mt-5 rounded-2xl bg-sky-500 px-5 py-2.5 text-sm font-semibold text-white hover:bg-sky-400 transition"
        >New application</button>
      </div>
    {:else if filteredApplications.length === 0 && hasActiveFilter}
      <div class="flex flex-col items-center justify-center rounded-3xl border border-slate-800 bg-slate-900/95 py-14 text-center">
        <svg class="h-8 w-8 text-slate-600" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 15.803M10.5 7.5v6m3-3h-6"/>
        </svg>
        <p class="mt-3 text-sm font-medium text-slate-300">No applications match your filters</p>
        <button onclick={clearFilters} class="mt-3 text-xs font-medium text-sky-400 hover:text-sky-300">Clear filters</button>
      </div>
    {:else}
      <div class="overflow-hidden rounded-3xl border border-slate-800 bg-slate-900/95">
        {#if hasActiveFilter}
          <div class="border-b border-slate-800 px-5 py-2.5">
            <span class="text-xs text-slate-500">{filteredApplications.length} of {applications.length} applications</span>
          </div>
        {/if}
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-slate-800">
              <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Reference</th>
              <th class="px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500">Form</th>
              <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 md:table-cell">Status</th>
              <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 lg:table-cell">Stage</th>
              <th class="hidden px-5 py-3.5 text-left text-xs font-semibold uppercase tracking-wider text-slate-500 sm:table-cell">Created</th>
              <th class="px-5 py-3.5"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-800">
            {#each filteredApplications as app}
              <tr class="transition hover:bg-slate-800/40">
                <td class="px-5 py-4">
                  <span class="font-mono text-xs font-semibold text-sky-400">{app.publicReference || '—'}</span>
                </td>
                <td class="px-5 py-4">
                  <span class="font-medium text-white">{app.formTitle}</span>
                  <span class="ml-2 text-xs text-slate-500">v{app.formSchemaVersion}</span>
                </td>
                <td class="hidden px-5 py-4 md:table-cell">
                  {#if app.submittedAt}
                    <span class="inline-flex items-center gap-1.5 rounded-full bg-emerald-500/15 px-2.5 py-1 text-xs font-semibold text-emerald-400">
                      <svg class="h-3 w-3" viewBox="0 0 12 12" fill="currentColor">
                        <path d="M10.28 2.28a.75.75 0 00-1.06 0L4.5 7 2.78 5.28a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.06 0l5.25-5.25a.75.75 0 000-1.06z"/>
                      </svg>
                      Submitted
                    </span>
                  {:else}
                    <span class="inline-flex items-center rounded-full bg-amber-500/15 px-2.5 py-1 text-xs font-semibold text-amber-400">In progress</span>
                  {/if}
                </td>
                <td class="hidden px-5 py-4 lg:table-cell">
                  {#if app.currentStage}
                    <span class="inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold
                      {app.currentStage.isFinal ? 'bg-emerald-500/15 text-emerald-400' : app.currentStage.isInitial ? 'bg-sky-500/15 text-sky-400' : 'bg-violet-500/15 text-violet-400'}">
                      {app.currentStage.name}
                    </span>
                  {:else}
                    <span class="text-xs text-slate-600">—</span>
                  {/if}
                </td>
                <td class="hidden px-5 py-4 text-slate-400 sm:table-cell">{formatDate(app.createdAt)}</td>
                <td class="px-5 py-4 text-right">
                  <button
                    onclick={() => openDetail(app.id)}
                    class="rounded-xl border px-3 py-1.5 text-xs font-semibold transition
                      {app.submittedAt
                        ? 'border-slate-700 text-slate-300 hover:border-sky-500/50 hover:text-sky-400'
                        : 'border-amber-500/30 bg-amber-500/5 text-amber-400 hover:bg-amber-500/10'}"
                  >{app.submittedAt ? 'View' : 'Continue'}</button>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </div>

<!-- ── New application ─────────────────────────────────────────────────────────── -->
{:else if view === 'new'}
  <div class="space-y-6">
    <button onclick={() => { view = 'list'; error = ''; }} class={backBtn}>
      <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
        <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
      </svg>
      Back to applications
    </button>

    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <h2 class="text-lg font-semibold text-white">Start new application</h2>
      <p class="mt-1 text-sm text-slate-400">Select a form to begin.</p>

      {#if error}
        <p class="mt-4 rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</p>
      {/if}

      <div class="mt-6 space-y-3">
        {#each schemas as s}
          <button
            onclick={() => selectedFormId = s.id}
            class="flex w-full items-start gap-4 rounded-2xl border p-4 text-left transition
              {selectedFormId === s.id ? 'border-sky-500/60 bg-sky-500/5' : 'border-slate-700 hover:border-slate-600 hover:bg-slate-800/40'}"
          >
            <div class="mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-xl {selectedFormId === s.id ? 'bg-sky-500/20' : 'bg-slate-800'}">
              <svg class="h-4 w-4 {selectedFormId === s.id ? 'text-sky-400' : 'text-slate-500'}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
            </div>
            <div>
              <p class="font-semibold {selectedFormId === s.id ? 'text-sky-300' : 'text-white'}">{s.title}</p>
              {#if s.description}<p class="mt-0.5 text-xs text-slate-400">{s.description}</p>{/if}
            </div>
          </button>
        {/each}
      </div>

      <button
        onclick={submitNew}
        disabled={saving || !selectedFormId}
        class="mt-6 w-full rounded-2xl bg-sky-500 px-4 py-3 text-sm font-semibold text-white transition hover:bg-sky-400 disabled:cursor-not-allowed disabled:opacity-50"
      >{saving ? 'Creating…' : 'Start application'}</button>
    </div>
  </div>

<!-- ── Multi-step form ─────────────────────────────────────────────────────────── -->
{:else if view === 'form' && selected}
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-wrap items-center gap-3">
      <button onclick={() => { view = 'list'; selected = null; error = ''; }} class={backBtn}>
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
        </svg>
        All applications
      </button>
      <span class="font-mono text-xs font-bold text-sky-400">{selected.publicReference}</span>
      <span class="ml-auto text-xs text-amber-400 font-medium">In progress</span>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</p>
    {/if}

    <!-- Step indicator -->
    {#if totalSteps > 1}
      <div class="flex items-center gap-2">
        {#each steps as step, i}
          <div class="flex items-center gap-2 {i < totalSteps - 1 ? 'flex-1' : ''}">
            <div class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-xs font-bold
              {i < currentStep  ? 'bg-emerald-500/20 text-emerald-400'
              : i === currentStep ? 'bg-sky-500 text-white'
              :                    'bg-slate-800 text-slate-600'}">
              {#if i < currentStep}
                <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd"/>
                </svg>
              {:else}
                {i + 1}
              {/if}
            </div>
            <span class="hidden text-xs sm:block
              {i === currentStep ? 'font-semibold text-white' : i < currentStep ? 'text-slate-400' : 'text-slate-600'}">
              {step.title}
            </span>
            {#if i < totalSteps - 1}
              <div class="h-px flex-1 bg-slate-800 mx-1"></div>
            {/if}
          </div>
        {/each}
      </div>
    {/if}

    <!-- Current step -->
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <h3 class="text-base font-semibold text-white">{steps[currentStep]?.title}</h3>
      <p class="mt-0.5 text-xs text-slate-500">{selected.formSchema?.title} · Step {currentStep + 1} of {totalSteps}</p>

      <div class="mt-6 space-y-5">
        {#each (steps[currentStep]?.fields ?? []) as field}
          {#if isVisible(field)}

            <!-- Info / heading — no input, just rendered content -->
            {#if field.type === 'info'}
              {#if field.infoVariant === 'heading'}
                <h2 class="text-xl font-bold text-white">{field.label}</h2>
              {:else if field.infoVariant === 'subheading'}
                <h3 class="text-base font-semibold text-slate-200">{field.label}</h3>
              {:else if field.infoVariant === 'warning'}
                <div class="rounded-2xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-300">{field.label}</div>
              {:else}
                <p class="text-sm leading-relaxed text-slate-300">{field.label}</p>
              {/if}

            {:else if field.type === 'repeater'}
              <div>
                <div class="mb-3 flex items-center justify-between">
                  <span class="text-sm font-medium text-white">
                    {field.label}{#if field.required}<span class="ml-0.5 text-red-400">*</span>{/if}
                  </span>
                  <button
                    onclick={() => addRepeaterItem(field.name, field.subFields)}
                    class="rounded-xl border border-slate-700 px-3 py-1.5 text-xs font-semibold text-slate-300 transition hover:bg-slate-800"
                  >+ Add {field.label}</button>
                </div>
                <div class="space-y-3">
                  {#each (formValues[field.name] ?? []) as item, itemIndex}
                    <div class="rounded-2xl border border-slate-700 bg-slate-900/60 p-4">
                      <div class="mb-3 flex items-center justify-between">
                        <span class="text-xs font-semibold uppercase tracking-widest text-slate-500">{field.label} {itemIndex + 1}</span>
                        {#if (formValues[field.name] ?? []).length > 1}
                          <button
                            onclick={() => removeRepeaterItem(field.name, itemIndex)}
                            class="rounded-lg bg-red-500/10 px-2 py-1 text-xs font-semibold text-red-400 transition hover:bg-red-500 hover:text-white"
                          >Remove</button>
                        {/if}
                      </div>
                      <div class="grid gap-3 sm:grid-cols-2">
                        {#each (field.subFields ?? []) as sf}
                          <div class="{sf.type === 'checkbox' ? 'flex items-center gap-2 pt-4' : ''}">
                            {#if sf.type === 'checkbox'}
                              <input
                                type="checkbox"
                                checked={!!item[sf.name]}
                                onchange={(e) => { item[sf.name] = e.target.checked; formValues = { ...formValues }; }}
                                class="h-4 w-4 rounded border-slate-600 bg-slate-900 accent-sky-500"
                              />
                              <label class="text-sm text-slate-300 select-none">{sf.label}</label>
                            {:else}
                              <label class="mb-1 block text-xs font-medium text-slate-400">
                                {sf.label}{#if sf.required}<span class="ml-0.5 text-red-400">*</span>{/if}
                              </label>
                              {#if sf.type === 'select'}
                                <select
                                  value={item[sf.name] ?? ''}
                                  onchange={(e) => { item[sf.name] = e.target.value; formValues = { ...formValues }; }}
                                  class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white focus:border-sky-500 focus:outline-none"
                                >
                                  <option value="">Select…</option>
                                  {#each (sf.options ?? []) as opt}<option value={opt}>{opt}</option>{/each}
                                </select>
                              {:else}
                                <input
                                  type={sf.type === 'email' ? 'email' : sf.type === 'tel' ? 'tel' : sf.type === 'number' ? 'number' : sf.type === 'date' ? 'date' : 'text'}
                                  value={item[sf.name] ?? ''}
                                  oninput={(e) => { item[sf.name] = e.target.value; formValues = { ...formValues }; }}
                                  placeholder={sf.label}
                                  class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none"
                                />
                              {/if}
                            {/if}
                          </div>
                        {/each}
                      </div>
                    </div>
                  {/each}
                </div>
              </div>

            {:else}
            <div>
              <label class="mb-1.5 block text-sm font-medium text-white">
                {field.label}{#if field.required}<span class="ml-0.5 text-red-400">*</span>{/if}
              </label>

              {#if field.type === 'select'}
                <select
                  value={formValues[field.name] ?? ''}
                  onchange={(e) => formValues = { ...formValues, [field.name]: e.target.value }}
                  class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none"
                >
                  <option value="">Select…</option>
                  {#each field.options as opt}<option value={opt}>{opt}</option>{/each}
                </select>

              {:else if field.type === 'radio'}
                <div class="space-y-2">
                  {#each field.options as opt}
                    <label class="flex cursor-pointer items-center gap-3 rounded-xl border px-4 py-2.5 transition
                      {formValues[field.name] === opt ? 'border-sky-500/60 bg-sky-500/5' : 'border-slate-700 hover:border-slate-600'}">
                      <input
                        type="radio"
                        name={field.name}
                        value={opt}
                        checked={formValues[field.name] === opt}
                        onchange={() => formValues = { ...formValues, [field.name]: opt }}
                        class="accent-sky-500"
                      />
                      <span class="text-sm text-slate-200">{opt}</span>
                    </label>
                  {/each}
                </div>

              {:else if field.type === 'checkbox'}
                <label class="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-700 bg-slate-950 px-4 py-3">
                  <input
                    type="checkbox"
                    checked={!!formValues[field.name]}
                    onchange={(e) => formValues = { ...formValues, [field.name]: e.target.checked }}
                    class="h-4 w-4 rounded border-slate-600 bg-slate-900 accent-sky-500"
                  />
                  <span class="text-sm text-slate-300">{field.label}</span>
                </label>

              {:else if field.type === 'textarea'}
                <textarea
                  value={formValues[field.name] ?? ''}
                  oninput={(e) => formValues = { ...formValues, [field.name]: e.target.value }}
                  rows="4"
                  class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none"
                ></textarea>

              {:else if field.type === 'address'}
                {@const addrFields = [
                  { key: 'organisationName', label: 'Organisation name', full: true },
                  { key: 'buildingNumber',   label: 'Building number',   full: false },
                  { key: 'buildingName',     label: 'Building name',     full: false },
                  { key: 'street',           label: 'Street',            full: true  },
                  { key: 'locality',         label: 'Locality',          full: false },
                  { key: 'town',             label: 'Town / City',       full: false },
                  { key: 'postcode',         label: 'Postcode',          full: false },
                  { key: 'country',          label: 'Country',           full: false },
                ]}
                <div class="rounded-xl border border-slate-700 bg-slate-950 p-4">
                  <div class="grid gap-3 sm:grid-cols-2">
                    {#each addrFields as af}
                      {@const fk = `${field.name}_${af.key}`}
                      <div class="{af.full ? 'sm:col-span-2' : ''}">
                        <label class="mb-1 block text-xs font-medium text-slate-500">{af.label}</label>
                        <input
                          type="text"
                          value={formValues[fk] ?? ''}
                          oninput={(e) => formValues = { ...formValues, [fk]: e.target.value }}
                          placeholder={af.label}
                          class="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none"
                        />
                      </div>
                    {/each}
                  </div>
                </div>

              {:else if field.type === 'currency'}
                <div class="relative">
                  <span class="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm text-slate-400">£</span>
                  <input
                    type="number" step="0.01" min="0"
                    value={formValues[field.name] ?? ''}
                    oninput={(e) => formValues = { ...formValues, [field.name]: e.target.value }}
                    placeholder="0.00"
                    class="w-full rounded-xl border border-slate-700 bg-slate-950 py-2.5 pl-8 pr-3 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none"
                  />
                </div>

              {:else}
                <input
                  type={field.type === 'email' ? 'email' : field.type === 'tel' ? 'tel' : field.type === 'number' ? 'number' : field.type === 'date' ? 'date' : 'text'}
                  value={formValues[field.name] ?? ''}
                  oninput={(e) => formValues = { ...formValues, [field.name]: e.target.value }}
                  placeholder={field.label}
                  class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none"
                />
              {/if}
            </div>
            {/if}
          {/if}
        {/each}
      </div>

      <!-- Navigation -->
      <div class="mt-8 flex items-center justify-between gap-3">
        {#if currentStep > 0}
          <button
            onclick={goBack}
            class="flex items-center gap-2 rounded-2xl border border-slate-700 px-5 py-2.5 text-sm font-semibold text-slate-300 transition hover:bg-slate-800"
          >
            <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
            </svg>
            Back
          </button>
        {:else}
          <div></div>
        {/if}

        {#if isLastStep}
          <button
            onclick={submitForm}
            disabled={saving}
            class="flex items-center gap-2 rounded-2xl bg-emerald-500 px-6 py-2.5 text-sm font-semibold text-white transition hover:bg-emerald-400 disabled:opacity-50"
          >
            {saving ? 'Submitting…' : 'Submit application'}
            {#if !saving}
              <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd"/>
              </svg>
            {/if}
          </button>
        {:else}
          <button
            onclick={goNext}
            disabled={saving}
            class="flex items-center gap-2 rounded-2xl bg-sky-500 px-6 py-2.5 text-sm font-semibold text-white transition hover:bg-sky-400 disabled:opacity-50"
          >
            {saving ? 'Saving…' : 'Save & continue'}
            <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
            </svg>
          </button>
        {/if}
      </div>
    </div>
  </div>

<!-- ── Submitted view ──────────────────────────────────────────────────────────── -->
{:else if view === 'submitted' && selected}
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-wrap items-center gap-3">
      <button onclick={() => { view = 'list'; selected = null; error = ''; }} class={backBtn}>
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
        </svg>
        All applications
      </button>
      <span class="font-mono text-xs font-bold text-sky-400">{selected.publicReference}</span>
      <span class="inline-flex items-center gap-1.5 rounded-full bg-emerald-500/15 px-2.5 py-1 text-xs font-semibold text-emerald-400">
        <svg class="h-3 w-3" viewBox="0 0 12 12" fill="currentColor">
          <path d="M10.28 2.28a.75.75 0 00-1.06 0L4.5 7 2.78 5.28a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.06 0l5.25-5.25a.75.75 0 000-1.06z"/>
        </svg>
        Submitted {formatDate(selected.submittedAt)}
      </span>
    </div>

    {#if error}
      <p class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</p>
    {/if}

    <div class="grid gap-6 xl:grid-cols-3">

      <!-- Left: read-only form summary -->
      <div class="space-y-4 xl:col-span-2">
        {#each (selected.formSchema?.steps ?? []).filter(s => !s.conditions?.length || s.conditions.every(c => condMatch(c, formValues))) as step}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
            <h3 class="mb-4 text-sm font-semibold text-white">{step.title}</h3>
            <dl class="space-y-4">
              {#each step.fields as field}
                {#if isVisible(field)}
                  {#if field.type === 'info'}
                    <!-- Info fields rendered as content, not data rows -->
                    {#if field.infoVariant === 'heading'}
                      <h2 class="pt-2 text-lg font-bold text-white">{field.label}</h2>
                    {:else if field.infoVariant === 'subheading'}
                      <h3 class="pt-1 text-sm font-semibold text-slate-300">{field.label}</h3>
                    {:else if field.infoVariant === 'warning'}
                      <div class="rounded-xl border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-xs text-amber-300">{field.label}</div>
                    {:else}
                      <p class="text-sm text-slate-400 leading-relaxed">{field.label}</p>
                    {/if}
                  {:else if field.type === 'address'}
                    <!-- Address block -->
                    {@const addrKeys = ['organisationName','buildingNumber','buildingName','street','locality','town','postcode','country']}
                    {@const addrLabels = { organisationName:'Organisation', buildingNumber:'Building no.', buildingName:'Building name', street:'Street', locality:'Locality', town:'Town / City', postcode:'Postcode', country:'Country' }}
                    <div class="sm:col-span-3">
                      <dt class="mb-2 text-xs font-medium text-slate-500">{field.label}</dt>
                      <dd class="rounded-xl border border-slate-800 bg-slate-950/60 p-3 grid gap-1.5 sm:grid-cols-2">
                        {#each addrKeys as ak}
                          {@const v = formValues[`${field.name}_${ak}`]}
                          {#if v}
                            <div class="flex gap-2 text-xs">
                              <span class="w-28 shrink-0 text-slate-500">{addrLabels[ak]}</span>
                              <span class="text-white">{v}</span>
                            </div>
                          {/if}
                        {/each}
                      </dd>
                    </div>
                  {:else if field.type === 'repeater'}
                    <div>
                      <dt class="mb-2 text-xs font-medium text-slate-500">{field.label}</dt>
                      <dd class="space-y-2">
                        {#each (formValues[field.name] ?? []) as item, i}
                          <div class="rounded-xl border border-slate-800 bg-slate-950/60 p-3">
                            <p class="mb-2 text-xs font-semibold uppercase tracking-widest text-slate-600">{field.label} {i + 1}</p>
                            <div class="grid gap-1.5 sm:grid-cols-2">
                              {#each (field.subFields ?? []) as sf}
                                {@const v = item[sf.name]}
                                <div class="flex gap-2 text-xs">
                                  <span class="w-28 shrink-0 text-slate-500">{sf.label}</span>
                                  <span class="text-white">
                                    {#if v === undefined || v === null || v === ''}
                                      <span class="text-slate-600">—</span>
                                    {:else if sf.type === 'checkbox'}
                                      {v ? 'Yes' : 'No'}
                                    {:else if sf.type === 'date'}
                                      {formatFieldDate(v)}
                                    {:else}
                                      {v}
                                    {/if}
                                  </span>
                                </div>
                              {/each}
                            </div>
                          </div>
                        {/each}
                      </dd>
                    </div>
                  {:else}
                    <div class="grid gap-1 sm:grid-cols-3">
                      <dt class="text-xs font-medium text-slate-500 sm:col-span-1 pt-0.5">{field.label}</dt>
                      <dd class="text-sm text-white sm:col-span-2">
                        {#if formValues[field.name] === undefined || formValues[field.name] === null || formValues[field.name] === ''}
                          <span class="text-slate-600">—</span>
                        {:else if field.type === 'checkbox'}
                          <span class="{formValues[field.name] ? 'text-emerald-400' : 'text-slate-400'}">
                            {formValues[field.name] ? 'Yes' : 'No'}
                          </span>
                        {:else if field.type === 'currency'}
                          £{Number(formValues[field.name]).toLocaleString('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                        {:else if field.type === 'date'}
                          {formatFieldDate(formValues[field.name])}
                        {:else}
                          {formValues[field.name]}
                        {/if}
                      </dd>
                    </div>
                  {/if}
                {/if}
              {/each}
            </dl>
          </div>
        {/each}

        <!-- Checklist -->
        {#if checklistLoading}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
            <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Checklist</p>
            <p class="mt-3 text-xs text-slate-500">Loading checklist…</p>
          </div>
        {:else if checklistItems.length > 0}
          {@const statusStyle = (s) =>
            s === 'Approved'         ? 'border-emerald-500/30 bg-emerald-500/5' :
            s === 'Pending Review'   ? 'border-amber-500/30 bg-amber-500/5' :
            s === 'More Info Needed' ? 'border-sky-500/30 bg-sky-500/5' :
            s === 'Rejected'         ? 'border-red-500/30 bg-red-500/5' :
                                       'border-slate-700 bg-slate-950/60'}
          {@const statusBadge = (s) =>
            s === 'Approved'         ? 'bg-emerald-500/20 text-emerald-400' :
            s === 'Pending Review'   ? 'bg-amber-500/20 text-amber-400' :
            s === 'More Info Needed' ? 'bg-sky-500/20 text-sky-400' :
            s === 'Rejected'         ? 'bg-red-500/20 text-red-400' :
                                       'bg-slate-700 text-slate-400'}
          {@const needsResponse = (s) => s === 'Outstanding' || s === 'More Info Needed' || s === 'Pending'}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
            <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Checklist</p>
            <div class="mt-4 space-y-4">
              {#each checklistItems as item}
                <div class="rounded-2xl border {statusStyle(item.status)} p-4">

                  <!-- Header -->
                  <div class="flex flex-wrap items-start justify-between gap-2">
                    <div class="min-w-0">
                      <div class="flex flex-wrap items-center gap-2">
                        <span class="rounded-full px-2 py-0.5 text-xs font-semibold
                          {item.itemType === 'Document' ? 'bg-sky-500/20 text-sky-400' : 'bg-amber-500/20 text-amber-400'}">
                          {item.itemType}
                        </span>
                        <span class="rounded-full px-2 py-0.5 text-xs font-semibold {statusBadge(item.status)}">
                          {item.status}
                        </span>
                      </div>
                      <p class="mt-1.5 text-sm font-semibold text-white">{item.itemName}</p>
                      {#if item.itemDescription}
                        <p class="mt-0.5 text-xs text-slate-400">{item.itemDescription}</p>
                      {/if}
                    </div>

                    <!-- Admin status actions -->
                    {#if user?.role === 'Admin' && (item.status === 'Pending Review' || item.status === 'More Info Needed')}
                      <div class="flex shrink-0 gap-1.5">
                        <button
                          onclick={() => changeChecklistStatus(item, 'Approved')}
                          class="rounded-lg bg-emerald-500/20 px-2.5 py-1 text-xs font-semibold text-emerald-400 transition hover:bg-emerald-500 hover:text-white"
                        >Approve</button>
                        {#if item.status === 'Pending Review'}
                          <button
                            onclick={() => changeChecklistStatus(item, 'More Info Needed')}
                            class="rounded-lg bg-sky-500/20 px-2.5 py-1 text-xs font-semibold text-sky-400 transition hover:bg-sky-500 hover:text-white"
                          >More Info</button>
                        {/if}
                        <button
                          onclick={() => changeChecklistStatus(item, 'Rejected')}
                          class="rounded-lg bg-red-500/20 px-2.5 py-1 text-xs font-semibold text-red-400 transition hover:bg-red-500 hover:text-white"
                        >Reject</button>
                      </div>
                    {/if}
                  </div>

                  <!-- Submitted content (all statuses that have a response) -->
                  {#if item.textResponse}
                    <div class="mt-3 rounded-xl bg-slate-900/80 px-3 py-2 text-sm text-slate-300">{item.textResponse}</div>
                  {/if}
                  {#if item.documentName}
                    <div class="mt-3 flex items-center gap-2">
                      <svg class="h-4 w-4 shrink-0 text-slate-400" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M15.621 4.379a3 3 0 00-4.242 0l-7 7a3 3 0 004.241 4.243h.001l.497-.5a.75.75 0 011.064 1.057l-.498.501-.002.002a4.5 4.5 0 01-6.364-6.364l7-7a4.5 4.5 0 016.368 6.36l-3.455 3.553A2.625 2.625 0 119.52 9.52l3.45-3.451a.75.75 0 111.061 1.06l-3.45 3.451a1.125 1.125 0 001.587 1.595l3.454-3.553a3 3 0 000-4.242z" clip-rule="evenodd"/>
                      </svg>
                      <a href="{API_BASE}/checklist/application/item/{item.id}/download" target="_blank"
                        class="text-xs text-sky-400 hover:underline">{item.documentName}</a>
                    </div>
                  {/if}

                  <!-- Input area: shown when awaiting a response -->
                  {#if needsResponse(item.status)}
                    {#if item.itemType === 'Information'}
                      <div class="mt-3 space-y-2">
                        {#if item.status === 'More Info Needed'}
                          <p class="text-xs font-medium text-sky-400">Additional information has been requested. Please provide updated details below.</p>
                        {/if}
                        <textarea
                          rows="3"
                          placeholder="Enter information here…"
                          value={checklistResponses[item.id] ?? ''}
                          oninput={(e) => checklistResponses = { ...checklistResponses, [item.id]: e.target.value }}
                          class="w-full rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none"
                        ></textarea>
                        <button
                          onclick={() => submitChecklistResponse(item)}
                          disabled={!(checklistResponses[item.id] ?? '').trim()}
                          class="rounded-xl bg-sky-500 px-4 py-2 text-xs font-semibold text-white transition hover:bg-sky-400 disabled:opacity-40"
                        >Submit</button>
                      </div>

                    {:else if item.itemType === 'Document'}
                      <div class="mt-3">
                        {#if item.status === 'More Info Needed'}
                          <p class="mb-2 text-xs font-medium text-sky-400">A replacement document has been requested.</p>
                        {/if}
                        {#if checklistUploading[item.id]}
                          <p class="text-xs text-slate-400">Uploading…</p>
                        {:else}
                          <label class="flex cursor-pointer items-center gap-2 rounded-xl border border-dashed border-slate-600 px-4 py-3 text-xs text-slate-400 transition hover:border-sky-500 hover:text-sky-400">
                            <svg class="h-4 w-4 shrink-0" viewBox="0 0 20 20" fill="currentColor">
                              <path fill-rule="evenodd" d="M15.621 4.379a3 3 0 00-4.242 0l-7 7a3 3 0 004.241 4.243h.001l.497-.5a.75.75 0 011.064 1.057l-.498.501-.002.002a4.5 4.5 0 01-6.364-6.364l7-7a4.5 4.5 0 016.368 6.36l-3.455 3.553A2.625 2.625 0 119.52 9.52l3.45-3.451a.75.75 0 111.061 1.06l-3.45 3.451a1.125 1.125 0 001.587 1.595l3.454-3.553a3 3 0 000-4.242z" clip-rule="evenodd"/>
                            </svg>
                            {item.documentName ? 'Upload replacement document' : 'Click to upload document'}
                            <input type="file" class="sr-only" onchange={(e) => handleDocumentUpload(item, e.target.files?.[0])} />
                          </label>
                        {/if}
                      </div>
                    {/if}
                  {/if}

                  <!-- Comments -->
                  {#if (item.comments ?? []).length > 0}
                    <div class="mt-3 space-y-2 border-t border-slate-700/50 pt-3">
                      {#each (item.comments ?? []) as c}
                        <div class="rounded-xl bg-slate-900/70 px-3 py-2">
                          <div class="flex items-center justify-between gap-2">
                            <span class="text-xs font-semibold text-slate-300">{c.authorName}</span>
                            <span class="text-xs text-slate-600">{formatDate(c.createdAt)}</span>
                          </div>
                          <p class="mt-1 text-xs text-slate-400">{c.comment}</p>
                        </div>
                      {/each}
                    </div>
                  {/if}

                  <!-- Add comment (any authenticated user) -->
                  <div class="mt-3 flex gap-2 {(item.comments ?? []).length > 0 ? '' : 'border-t border-slate-700/50 pt-3'}">
                    <input
                      type="text"
                      placeholder="Add a comment…"
                      value={checklistComments[item.id] ?? ''}
                      oninput={(e) => checklistComments = { ...checklistComments, [item.id]: e.target.value }}
                      onkeydown={(e) => e.key === 'Enter' && submitChecklistComment(item)}
                      class="min-w-0 flex-1 rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-xs text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none"
                    />
                    <button
                      onclick={() => submitChecklistComment(item)}
                      disabled={!(checklistComments[item.id] ?? '').trim()}
                      class="rounded-xl border border-slate-700 px-3 py-2 text-xs font-semibold text-slate-300 transition hover:bg-slate-800 disabled:opacity-40"
                    >Post</button>
                  </div>

                </div>
              {/each}
            </div>
          </div>
        {/if}
      </div>

      <!-- Right: meta + workflow -->
      <div class="space-y-6">

        <!-- Details -->
        <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
          <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Details</p>
          <dl class="mt-3 space-y-2.5 text-sm">
            <div class="flex justify-between">
              <dt class="text-slate-500">Submitted</dt>
              <dd class="text-white">{formatDateTime(selected.submittedAt)}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-slate-500">Created</dt>
              <dd class="text-white">{formatDate(selected.createdAt)}</dd>
            </div>
            {#if selected.client}
              <div class="flex justify-between">
                <dt class="text-slate-500">Client</dt>
                <dd class="text-white">{selected.client.firstName} {selected.client.lastName}</dd>
              </div>
            {/if}
            {#if selected.broker}
              <div class="flex justify-between">
                <dt class="text-slate-500">Broker</dt>
                <dd class="text-white">{selected.broker.firstName} {selected.broker.lastName}</dd>
              </div>
            {/if}
            {#if selected.company}
              <div class="flex justify-between">
                <dt class="text-slate-500">Company</dt>
                <dd class="text-white">{selected.company.name}</dd>
              </div>
            {/if}
            {#if selected.network}
              <div class="flex justify-between">
                <dt class="text-slate-500">Network</dt>
                <dd class="text-white">{selected.network.name}</dd>
              </div>
            {/if}
          </dl>
        </div>

        <!-- Workflow stage tracker -->
        {#if selected.workflow}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
            <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Workflow</p>
            <p class="mt-1 text-sm font-semibold text-white">{selected.workflow.name}</p>
            <ol class="mt-4 space-y-2">
              {#each stagesOrdered(selected.workflow) as stage, i}
                {@const isCurrent = stage.id === selected.currentStageId}
                {@const currentOrder = selected.workflow.stages.find(s => s.id === selected.currentStageId)?.order ?? -1}
                {@const isPast = currentOrder > stage.order}
                <li class="flex items-center gap-3">
                  <div class="flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-xs font-bold
                    {isCurrent ? 'bg-sky-500 text-white' : isPast ? 'bg-emerald-500/20 text-emerald-400' : 'bg-slate-800 text-slate-600'}">
                    {#if isPast}
                      <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd"/>
                      </svg>
                    {:else}{i + 1}{/if}
                  </div>
                  <span class="text-sm {isCurrent ? 'font-semibold text-white' : isPast ? 'text-slate-400' : 'text-slate-600'}">{stage.name}</span>
                </li>
              {/each}
            </ol>
            {#if nextStages(selected.workflow, selected.currentStageId).length}
              <div class="mt-4 space-y-2">
                <p class="text-xs text-slate-500">Advance to</p>
                {#each nextStages(selected.workflow, selected.currentStageId) as ns}
                  <button
                    onclick={() => goToStage(ns.id)}
                    disabled={saving}
                    class="w-full rounded-xl border border-slate-700 px-3 py-2 text-xs font-semibold text-slate-300 transition hover:border-sky-500/50 hover:text-sky-400 disabled:opacity-50"
                  >{saving ? '…' : ns.name}</button>
                {/each}
              </div>
            {:else if selected.currentStage?.isFinal}
              <p class="mt-4 rounded-xl bg-emerald-500/10 px-3 py-2 text-xs font-semibold text-emerald-400">Application complete</p>
            {/if}
          </div>
        {/if}

        <!-- Policy rule outcomes — overall decision + summary list -->
        {#if rulesLoading}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
            <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Policy Rules</p>
            <p class="mt-3 text-xs text-slate-500">Loading outcomes…</p>
          </div>
        {:else if ruleOutcomes.length > 0}
          {@const dec = overallDecision}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
            <p class="text-xs font-semibold uppercase tracking-wider text-slate-500">Policy Rules</p>

            <!-- Overall decision badge -->
            <div class="mt-3 flex items-center gap-3 rounded-2xl border p-3
              {dec === 'Accept'  ? 'border-emerald-500/30 bg-emerald-500/10'
              : dec === 'Refer'  ? 'border-amber-500/30 bg-amber-500/10'
              :                    'border-red-500/30 bg-red-500/10'}">
              <div class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full
                {dec === 'Accept' ? 'bg-emerald-500/20' : dec === 'Refer' ? 'bg-amber-500/20' : 'bg-red-500/20'}">
                {#if dec === 'Accept'}
                  <svg class="h-4 w-4 text-emerald-400" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd"/>
                  </svg>
                {:else if dec === 'Refer'}
                  <svg class="h-4 w-4 text-amber-400" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/>
                  </svg>
                {:else}
                  <svg class="h-4 w-4 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd"/>
                  </svg>
                {/if}
              </div>
              <div>
                <p class="text-xs text-slate-500">Overall decision</p>
                <p class="text-sm font-bold
                  {dec === 'Accept' ? 'text-emerald-400' : dec === 'Refer' ? 'text-amber-400' : 'text-red-400'}">
                  {dec}
                </p>
              </div>
            </div>

            <!-- Rule summary list -->
            <div class="mt-3 space-y-1.5">
              {#each ruleOutcomes as outcome}
                {@const cur = outcome.current}
                {@const dt = outcome.decisionType ?? 'Decline'}
                <button
                  onclick={() => selectedRuleOutcome = outcome}
                  class="flex w-full items-center gap-2.5 rounded-xl border px-3 py-2.5 text-left transition hover:bg-slate-800/60
                    {cur.passed ? 'border-slate-800' : dt === 'Refer' ? 'border-amber-500/20 bg-amber-500/5' : 'border-red-500/20 bg-red-500/5'}"
                >
                  <!-- Pass/fail dot -->
                  <div class="flex h-4 w-4 shrink-0 items-center justify-center rounded-full
                    {cur.passed ? 'bg-emerald-500/20' : dt === 'Refer' ? 'bg-amber-500/20' : 'bg-red-500/20'}">
                    {#if cur.passed}
                      <svg class="h-2.5 w-2.5 text-emerald-400" viewBox="0 0 12 12" fill="currentColor">
                        <path d="M10.28 2.28a.75.75 0 00-1.06 0L4.5 7 2.78 5.28a.75.75 0 00-1.06 1.06l2.25 2.25a.75.75 0 001.06 0l5.25-5.25a.75.75 0 000-1.06z"/>
                      </svg>
                    {:else}
                      <svg class="h-2.5 w-2.5 {dt === 'Refer' ? 'text-amber-400' : 'text-red-400'}" viewBox="0 0 12 12" fill="currentColor">
                        <path d="M6 4.25a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0V5A.75.75 0 016 4.25zm0 6a.75.75 0 100-1.5.75.75 0 000 1.5z"/>
                      </svg>
                    {/if}
                  </div>
                  <div class="min-w-0 flex-1">
                    <div class="flex items-center gap-2">
                      <span class="font-mono text-[10px] text-slate-600">{outcome.ruleReference}</span>
                      <span class="truncate text-xs font-medium {cur.passed ? 'text-slate-300' : dt === 'Refer' ? 'text-amber-300' : 'text-red-300'}">{outcome.ruleName}</span>
                    </div>
                  </div>
                  {#if !cur.passed}
                    <span class="shrink-0 rounded-full px-1.5 py-0.5 text-[10px] font-bold
                      {dt === 'Refer' ? 'bg-amber-500/15 text-amber-400' : 'bg-red-500/15 text-red-400'}">
                      {dt}
                    </span>
                  {:else}
                    <span class="shrink-0 rounded-full bg-emerald-500/10 px-1.5 py-0.5 text-[10px] font-bold text-emerald-400">Pass</span>
                  {/if}
                  <svg class="h-3.5 w-3.5 shrink-0 text-slate-600" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
                  </svg>
                </button>
              {/each}
            </div>
          </div>
        {/if}

        <!-- Rule detail panel (shown when a rule row is clicked) -->
        {#if selectedRuleOutcome}
          {@const o = selectedRuleOutcome}
          {@const cur = o.current}
          {@const dt = o.decisionType ?? 'Decline'}
          <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-5 shadow-xl">
            <div class="flex items-start justify-between gap-3">
              <div>
                <p class="font-mono text-xs text-slate-500">{o.ruleReference}</p>
                <p class="mt-0.5 text-sm font-semibold text-white">{o.ruleName}</p>
              </div>
              <button
                onclick={() => selectedRuleOutcome = null}
                class="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-slate-800 text-slate-500 hover:text-white transition"
              >
                <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                  <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
                </svg>
              </button>
            </div>

            <!-- Current outcome -->
            <div class="mt-3 rounded-2xl border px-3 py-2.5
              {cur.passed ? 'border-emerald-500/20 bg-emerald-500/5' : dt === 'Refer' ? 'border-amber-500/20 bg-amber-500/10' : 'border-red-500/20 bg-red-500/5'}">
              <div class="flex items-center gap-2">
                <span class="text-xs font-bold {cur.passed ? 'text-emerald-400' : dt === 'Refer' ? 'text-amber-400' : 'text-red-400'}">
                  {cur.passed ? 'Pass' : dt}
                </span>
                {#if cur.stageName}
                  <span class="text-xs text-slate-500">@ {cur.stageName}</span>
                {/if}
                <span class="ml-auto text-xs text-slate-600">
                  {new Date(cur.recordedAt).toLocaleString('en-GB', { day:'2-digit', month:'short', year:'numeric', hour:'2-digit', minute:'2-digit' })}
                </span>
              </div>
              {#if !cur.passed && cur.failReasons.length > 0}
                <ul class="mt-2 space-y-1">
                  {#each cur.failReasons as reason}
                    <li class="text-xs {dt === 'Refer' ? 'text-amber-400' : 'text-red-400'}">· {reason}</li>
                  {/each}
                </ul>
              {/if}
            </div>

            <!-- Visibility badges -->
            {#if o.isClientVisible || o.isBrokerVisible}
              <div class="mt-2 flex gap-1.5">
                {#if o.isClientVisible}
                  <span class="rounded-full bg-sky-500/10 px-2 py-0.5 text-[10px] font-semibold text-sky-400">Client visible</span>
                {/if}
                {#if o.isBrokerVisible}
                  <span class="rounded-full bg-violet-500/10 px-2 py-0.5 text-[10px] font-semibold text-violet-400">Broker visible</span>
                {/if}
              </div>
            {/if}

            <!-- History -->
            {#if o.history.length > 0}
              <div class="mt-4">
                <p class="mb-2 text-xs font-semibold uppercase tracking-wider text-slate-600">History</p>
                <div class="space-y-1.5">
                  {#each o.history as h}
                    {@const hdt = o.decisionType ?? 'Decline'}
                    <div class="flex items-start gap-2 rounded-xl px-3 py-2
                      {h.passed ? 'bg-emerald-500/5' : hdt === 'Refer' ? 'bg-amber-500/5' : 'bg-red-500/5'}">
                      <div class="flex-1 min-w-0">
                        <div class="flex flex-wrap items-center gap-2 text-xs">
                          <span class="font-medium {h.passed ? 'text-emerald-400' : hdt === 'Refer' ? 'text-amber-400' : 'text-red-400'}">
                            {h.passed ? 'Pass' : hdt}
                          </span>
                          {#if h.stageName}<span class="text-slate-500">@ {h.stageName}</span>{/if}
                          <span class="text-slate-600">{new Date(h.recordedAt).toLocaleString('en-GB', { day:'2-digit', month:'short', year:'numeric', hour:'2-digit', minute:'2-digit' })}</span>
                        </div>
                        {#if !h.passed && h.failReasons.length > 0}
                          <ul class="mt-0.5 space-y-0.5">
                            {#each h.failReasons as r}
                              <li class="text-xs text-slate-500">· {r}</li>
                            {/each}
                          </ul>
                        {/if}
                      </div>
                    </div>
                  {/each}
                </div>
              </div>
            {/if}
          </div>
        {/if}

      </div>
    </div>
  </div>
{/if}
