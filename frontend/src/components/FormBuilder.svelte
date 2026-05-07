<script>
  import { untrack } from 'svelte';
  let { schema, onsave } = $props();

  let currentSchema = $state(
    untrack(() => schema
      ? JSON.parse(JSON.stringify(schema))
      : { id: '', title: '', description: '', steps: [] })
  );

  $effect(() => { if (schema) currentSchema = JSON.parse(JSON.stringify(schema)); });

  // ── Preview state ─────────────────────────────────────────────────────────────
  let showPreview    = $state(false);
  let previewStep    = $state(0);
  let previewValues  = $state({});

  function openPreview() {
    previewValues = {};
    previewStep   = 0;
    showPreview   = true;
  }

  // ── Drag state ────────────────────────────────────────────────────────────────
  let stepDragIdx     = $state(-1);
  let stepDragOverIdx = $state(-1);
  let fDrag  = $state({ si: -1, fi: -1 });
  let fDragO = $state({ si: -1, fi: -1 });

  // ── Constants ─────────────────────────────────────────────────────────────────
  const FIELD_TYPES = [
    { value: 'text',      label: 'Text' },
    { value: 'email',     label: 'Email' },
    { value: 'tel',       label: 'Phone' },
    { value: 'number',    label: 'Number' },
    { value: 'currency',  label: 'Currency (£)' },
    { value: 'textarea',  label: 'Text area' },
    { value: 'select',    label: 'Select (dropdown)' },
    { value: 'radio',     label: 'Radio buttons' },
    { value: 'checkbox',  label: 'Checkbox' },
    { value: 'date',      label: 'Date' },
    { value: 'address',   label: 'Address' },
    { value: 'info',      label: 'Info / Heading' },
    { value: 'repeater',  label: 'Repeater' },
  ];

  const INFO_VARIANTS = [
    { value: 'heading',    label: 'Heading' },
    { value: 'subheading', label: 'Sub-heading' },
    { value: 'paragraph',  label: 'Paragraph' },
    { value: 'warning',    label: 'Warning banner' },
  ];

  const ADDRESS_FIELDS = [
    { key: 'organisationName', label: 'Organisation name', full: true  },
    { key: 'buildingNumber',   label: 'Building number',   full: false },
    { key: 'buildingName',     label: 'Building name',     full: false },
    { key: 'street',           label: 'Street',            full: true  },
    { key: 'locality',         label: 'Locality',          full: false },
    { key: 'town',             label: 'Town / City',       full: false },
    { key: 'postcode',         label: 'Postcode',          full: false },
    { key: 'country',          label: 'Country',           full: false },
  ];

  const OPERATORS = [
    { value: 'equals',    label: 'equals' },
    { value: 'notEquals', label: 'does not equal' },
    { value: 'contains',  label: 'contains' },
  ];

  const OPTS_TYPES = new Set(['select', 'radio']);
  const NO_KEY_TYPES = new Set(['info']);

  function validatorsFor(type) {
    if (['number', 'currency'].includes(type))
      return [{ value: 'min', label: 'Min value' }, { value: 'max', label: 'Max value' }];
    if (['text', 'textarea', 'email', 'tel'].includes(type))
      return [
        { value: 'minLength', label: 'Min length' },
        { value: 'maxLength', label: 'Max length' },
        { value: 'regex',     label: 'Pattern (regex)' },
      ];
    if (type === 'date')
      return [{ value: 'min', label: 'Earliest date' }, { value: 'max', label: 'Latest date' }];
    return [];
  }

  // All named fields — used in condition dropdowns
  const allFields = $derived(
    currentSchema.steps.flatMap(step =>
      step.fields.filter(f => f.name && !NO_KEY_TYPES.has(f.type))
                 .map(f => ({ name: f.name, label: f.label || f.name }))
    )
  );

  // ── Helpers ───────────────────────────────────────────────────────────────────
  function toCamelCase(s) {
    return s.trim().replace(/[^a-zA-Z0-9\s]/g, '').split(/\s+/).filter(Boolean)
      .map((w, i) => i === 0
        ? w.charAt(0).toLowerCase() + w.slice(1).toLowerCase()
        : w.charAt(0).toUpperCase() + w.slice(1).toLowerCase())
      .join('');
  }

  function dupesInStep(si) {
    const names = currentSchema.steps[si].fields
      .filter(f => !NO_KEY_TYPES.has(f.type))
      .map(f => f.name).filter(Boolean);
    const seen = new Set(), dupes = new Set();
    for (const n of names) { if (seen.has(n)) dupes.add(n); seen.add(n); }
    return dupes;
  }

  function subDupesInField(si, fi) {
    const names = (currentSchema.steps[si].fields[fi].subFields ?? []).map(sf => sf.name).filter(Boolean);
    const seen = new Set(), dupes = new Set();
    for (const n of names) { if (seen.has(n)) dupes.add(n); seen.add(n); }
    return dupes;
  }

  let hasErrors = $derived(
    currentSchema.steps.some((_, i) => dupesInStep(i).size > 0) ||
    currentSchema.steps.some((step, i) =>
      step.fields.some((f, j) => f.type === 'repeater' && subDupesInField(i, j).size > 0)
    )
  );

  function condMatch(cond, values) {
    const v = values[cond.fieldName];
    if (cond.operator === 'equals')    return String(v ?? '') === String(cond.value);
    if (cond.operator === 'notEquals') return String(v ?? '') !== String(cond.value);
    if (cond.operator === 'contains')  return String(v ?? '').includes(String(cond.value));
    return true;
  }

  function fieldVisible(field, values) {
    return !field.conditions?.length || field.conditions.every(c => condMatch(c, values));
  }

  function stepVisible(step, values) {
    return !step.conditions?.length || step.conditions.every(c => condMatch(c, values));
  }

  // ── Mutations ─────────────────────────────────────────────────────────────────
  function mutate() { currentSchema = { ...currentSchema }; }

  function handleLabelChange(si, fi, value) {
    const f = currentSchema.steps[si].fields[fi];
    f.label = value;
    if (!NO_KEY_TYPES.has(f.type)) f.name = toCamelCase(value);
    mutate();
  }

  function handleSubLabelChange(si, fi, sfi, value) {
    const sf = currentSchema.steps[si].fields[fi].subFields[sfi];
    sf.label = value; sf.name = toCamelCase(value); mutate();
  }

  function addStep() {
    currentSchema.steps.push({ title: 'New Step', fields: [], conditions: [] });
    mutate();
  }

  function removeStep(i) { currentSchema.steps.splice(i, 1); mutate(); }

  function addField(si) {
    currentSchema.steps[si].fields.push({
      name: '', label: '', type: 'text', infoVariant: 'paragraph', required: false,
      options: [], subFields: [], conditions: [], validators: []
    });
    mutate();
  }

  function removeField(si, fi) { currentSchema.steps[si].fields.splice(fi, 1); mutate(); }

  function addSubField(si, fi) {
    const f = currentSchema.steps[si].fields[fi];
    if (!f.subFields) f.subFields = [];
    f.subFields.push({ name: '', label: '', type: 'text', required: false, options: [] });
    mutate();
  }

  function removeSubField(si, fi, sfi) {
    currentSchema.steps[si].fields[fi].subFields.splice(sfi, 1); mutate();
  }

  function addCondition(si, fi) {
    const f = currentSchema.steps[si].fields[fi];
    if (!f.conditions) f.conditions = [];
    f.conditions.push({ fieldName: '', operator: 'equals', value: '' });
    mutate();
  }

  function removeCondition(si, fi, ci) {
    currentSchema.steps[si].fields[fi].conditions.splice(ci, 1); mutate();
  }

  function addStepCondition(si) {
    const s = currentSchema.steps[si];
    if (!s.conditions) s.conditions = [];
    s.conditions.push({ fieldName: '', operator: 'equals', value: '' });
    mutate();
  }

  function removeStepCondition(si, ci) {
    currentSchema.steps[si].conditions.splice(ci, 1); mutate();
  }

  function addValidator(si, fi) {
    const f = currentSchema.steps[si].fields[fi];
    if (!f.validators) f.validators = [];
    const types = validatorsFor(f.type);
    f.validators.push({ ruleType: types[0]?.value ?? 'min', value: '', message: '' });
    mutate();
  }

  function removeValidator(si, fi, vi) {
    currentSchema.steps[si].fields[fi].validators.splice(vi, 1); mutate();
  }

  // ── Step drag ─────────────────────────────────────────────────────────────────
  function stepDragStart(e, i) {
    const t = e.target;
    if (t instanceof HTMLInputElement || t instanceof HTMLSelectElement ||
        t instanceof HTMLTextAreaElement || t instanceof HTMLButtonElement) { e.preventDefault(); return; }
    stepDragIdx = i; e.dataTransfer.effectAllowed = 'move';
  }
  function stepDragOver(e, i)  { e.preventDefault(); e.dataTransfer.dropEffect = 'move'; stepDragOverIdx = i; }
  function stepDrop(e, i) {
    e.preventDefault();
    if (stepDragIdx !== -1 && stepDragIdx !== i) {
      const steps = [...currentSchema.steps];
      const [moved] = steps.splice(stepDragIdx, 1);
      steps.splice(i, 0, moved);
      currentSchema = { ...currentSchema, steps };
    }
    stepDragIdx = stepDragOverIdx = -1;
  }
  function stepDragEnd() { stepDragIdx = stepDragOverIdx = -1; }

  // ── Field drag ────────────────────────────────────────────────────────────────
  function fieldDragStart(e, si, fi) {
    const t = e.target;
    if (t instanceof HTMLInputElement || t instanceof HTMLSelectElement ||
        t instanceof HTMLTextAreaElement || t instanceof HTMLButtonElement) { e.preventDefault(); return; }
    fDrag = { si, fi }; e.dataTransfer.effectAllowed = 'move';
  }
  function fieldDragOver(e, si, fi) {
    e.preventDefault(); e.dataTransfer.dropEffect = 'move'; fDragO = { si, fi };
  }
  function fieldDrop(e, si, fi) {
    e.preventDefault();
    if (fDrag.si === si && fDrag.fi !== -1 && fDrag.fi !== fi) {
      const fields = [...currentSchema.steps[si].fields];
      const [moved] = fields.splice(fDrag.fi, 1);
      fields.splice(fi, 0, moved);
      currentSchema.steps[si].fields = fields;
      mutate();
    }
    fDrag = { si: -1, fi: -1 }; fDragO = { si: -1, fi: -1 };
  }
  function fieldDragEnd() { fDrag = { si: -1, fi: -1 }; fDragO = { si: -1, fi: -1 }; }

  // ── Normalize + save ──────────────────────────────────────────────────────────
  function normalizeField(f) {
    const clean = {
      ...f,
      conditions: f.conditions ?? [],
      validators: (f.validators ?? []).map(v => ({ ...v })),
    };
    if (NO_KEY_TYPES.has(clean.type)) {
      clean.name = clean.name || `_info_${Math.random().toString(36).slice(2, 7)}`;
      clean.options = []; clean.subFields = [];
    } else if (OPTS_TYPES.has(clean.type)) {
      clean.options = typeof clean.options === 'string'
        ? clean.options.split(',').map(o => o.trim()).filter(Boolean)
        : (Array.isArray(clean.options) ? clean.options : []);
      clean.subFields = [];
    } else if (clean.type === 'repeater') {
      clean.options = [];
      clean.subFields = (clean.subFields ?? []).map(sf => {
        const s = { ...sf };
        s.options = s.type === 'select'
          ? (typeof s.options === 'string' ? s.options.split(',').map(o => o.trim()).filter(Boolean) : s.options ?? [])
          : [];
        return s;
      });
    } else {
      clean.options = []; clean.subFields = [];
    }
    return clean;
  }

  function normalizeSchema(s) {
    return {
      ...s,
      steps: s.steps.map(step => ({
        ...step,
        conditions: step.conditions ?? [],
        fields: step.fields.map(normalizeField)
      }))
    };
  }

  function save() { if (hasErrors) return; onsave(normalizeSchema(currentSchema)); }

  // ── Styles ────────────────────────────────────────────────────────────────────
  const ic   = 'w-full rounded-2xl border border-slate-800 bg-slate-900 px-3 py-2 text-white text-sm focus:border-sky-500 focus:outline-none';
  const ic2  = 'w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-1.5 text-white text-xs focus:border-sky-500 focus:outline-none';
  const sec  = 'mt-4 border-t border-slate-800 pt-4';
  const shdr = 'mb-2 flex items-center justify-between';
  const slbl = 'text-xs font-semibold uppercase tracking-widest text-slate-500';
  const sadd = 'rounded-xl bg-slate-800 px-3 py-1 text-xs font-semibold text-slate-300 hover:bg-slate-700 transition';
  const cRow = 'flex items-center gap-2';
  const cSel = 'rounded-xl border border-slate-700 bg-slate-950 px-2.5 py-1.5 text-xs text-white focus:border-sky-500 focus:outline-none';
  const cInp = 'flex-1 rounded-xl border border-slate-700 bg-slate-950 px-2.5 py-1.5 text-xs text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none';
  const cDel = 'flex h-6 w-6 shrink-0 items-center justify-center rounded-lg text-slate-600 transition hover:bg-red-500/10 hover:text-red-400';
</script>

<!-- ── Live Preview Modal ─────────────────────────────────────────────────────── -->
{#if showPreview}
  {@const previewSchemaSteps = currentSchema.steps.filter(s => stepVisible(s, previewValues))}
  {@const previewTotalSteps  = previewSchemaSteps.length}
  {@const previewCurrentStep = previewSchemaSteps[previewStep]}

  <div role="presentation" class="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/70 p-4 backdrop-blur-sm" onclick={(e) => { if (e.target === e.currentTarget) showPreview = false; }} onkeydown={() => {}}>
    <div class="my-8 w-full max-w-2xl rounded-3xl border border-slate-700 bg-slate-900 shadow-2xl">

      <!-- Preview header -->
      <div class="flex items-center justify-between border-b border-slate-800 px-6 py-4">
        <div>
          <p class="text-xs font-semibold uppercase tracking-wider text-sky-400">Live Preview</p>
          <h3 class="mt-0.5 text-base font-semibold text-white">{currentSchema.title || 'Untitled form'}</h3>
        </div>
        <button aria-label="Close preview" onclick={() => showPreview = false}
          class="flex h-8 w-8 items-center justify-center rounded-xl text-slate-500 transition hover:bg-slate-800 hover:text-white">
          <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
            <path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/>
          </svg>
        </button>
      </div>

      <!-- Step indicator -->
      {#if previewTotalSteps > 1}
        <div class="flex items-center gap-2 overflow-x-auto border-b border-slate-800 px-6 py-3">
          {#each previewSchemaSteps as s, i}
            <div class="flex shrink-0 items-center gap-2">
              <div class="flex h-6 w-6 items-center justify-center rounded-full text-xs font-bold
                {i < previewStep ? 'bg-emerald-500/20 text-emerald-400' : i === previewStep ? 'bg-sky-500 text-white' : 'bg-slate-800 text-slate-600'}">
                {#if i < previewStep}
                  <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd"/>
                  </svg>
                {:else}{i + 1}{/if}
              </div>
              <span class="text-xs {i === previewStep ? 'font-semibold text-white' : 'text-slate-500'}">{s.title}</span>
              {#if i < previewTotalSteps - 1}<div class="w-4 h-px bg-slate-700"></div>{/if}
            </div>
          {/each}
        </div>
      {/if}

      <!-- Step content -->
      <div class="px-6 py-6 space-y-5">
        {#if previewCurrentStep}
          <h4 class="text-base font-semibold text-white">{previewCurrentStep.title}</h4>
          {#each previewCurrentStep.fields as field}
            {#if fieldVisible(field, previewValues)}
              <!-- Info/heading types -->
              {#if field.type === 'info'}
                {#if field.infoVariant === 'heading'}
                  <h2 class="text-xl font-bold text-white">{field.label}</h2>
                {:else if field.infoVariant === 'subheading'}
                  <h3 class="text-base font-semibold text-slate-200">{field.label}</h3>
                {:else if field.infoVariant === 'warning'}
                  <div class="rounded-2xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-300">{field.label}</div>
                {:else}
                  <p class="text-sm text-slate-300 leading-relaxed">{field.label}</p>
                {/if}

              {:else}
                <div>
                  <label for="prev-{field.name}" class="mb-1.5 block text-sm font-medium text-white">
                    {field.label}{#if field.required}<span class="ml-0.5 text-red-400">*</span>{/if}
                  </label>

                  {#if field.type === 'select'}
                    <select id="prev-{field.name}" value={previewValues[field.name] ?? ''} onchange={(e) => previewValues = { ...previewValues, [field.name]: e.target.value }}
                      class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none">
                      <option value="">Select…</option>
                      {#each (Array.isArray(field.options) ? field.options : (field.options ?? '').split(',').map(o=>o.trim()).filter(Boolean)) as opt}
                        <option value={opt}>{opt}</option>
                      {/each}
                    </select>

                  {:else if field.type === 'radio'}
                    <div class="space-y-2">
                      {#each (Array.isArray(field.options) ? field.options : (field.options ?? '').split(',').map(o=>o.trim()).filter(Boolean)) as opt}
                        <label class="flex cursor-pointer items-center gap-3 rounded-xl border px-4 py-2.5 transition
                          {previewValues[field.name] === opt ? 'border-sky-500/60 bg-sky-500/5' : 'border-slate-700'}">
                          <input type="radio" name="prev_{field.name}" value={opt}
                            checked={previewValues[field.name] === opt}
                            onchange={() => previewValues = { ...previewValues, [field.name]: opt }}
                            class="accent-sky-500" />
                          <span class="text-sm text-slate-200">{opt}</span>
                        </label>
                      {/each}
                    </div>

                  {:else if field.type === 'checkbox'}
                    <label class="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-700 bg-slate-950 px-4 py-3">
                      <input type="checkbox" checked={!!previewValues[field.name]}
                        onchange={(e) => previewValues = { ...previewValues, [field.name]: e.target.checked }}
                        class="h-4 w-4 rounded border-slate-600 bg-slate-900 accent-sky-500" />
                      <span class="text-sm text-slate-300">{field.label}</span>
                    </label>

                  {:else if field.type === 'textarea'}
                    <textarea value={previewValues[field.name] ?? ''} rows="3"
                      oninput={(e) => previewValues = { ...previewValues, [field.name]: e.target.value }}
                      class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white focus:border-sky-500 focus:outline-none"></textarea>

                  {:else if field.type === 'currency'}
                    <div class="relative">
                      <span class="absolute left-3.5 top-1/2 -translate-y-1/2 text-sm text-slate-400">£</span>
                      <input type="number" step="0.01" min="0" value={previewValues[field.name] ?? ''} placeholder="0.00"
                        oninput={(e) => previewValues = { ...previewValues, [field.name]: e.target.value }}
                        class="w-full rounded-xl border border-slate-700 bg-slate-950 py-2.5 pl-8 pr-3 text-sm text-white focus:border-sky-500 focus:outline-none" />
                    </div>

                  {:else if field.type === 'address'}
                    <div class="rounded-xl border border-slate-700 bg-slate-950 p-4 grid gap-3 sm:grid-cols-2">
                      {#each ADDRESS_FIELDS as af}
                        {@const fk = `${field.name}_${af.key}`}
                        <div class="{af.full ? 'sm:col-span-2' : ''}">
                          <label for="prev-{fk}" class="mb-1 block text-xs text-slate-500">{af.label}</label>
                          <input id="prev-{fk}" type="text" value={previewValues[fk] ?? ''} placeholder={af.label}
                            oninput={(e) => previewValues = { ...previewValues, [fk]: e.target.value }}
                            class="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white focus:border-sky-500 focus:outline-none" />
                        </div>
                      {/each}
                    </div>

                  {:else}
                    <input
                      type={field.type === 'email' ? 'email' : field.type === 'tel' ? 'tel' : field.type === 'number' ? 'number' : field.type === 'date' ? 'date' : 'text'}
                      value={previewValues[field.name] ?? ''} placeholder={field.label}
                      oninput={(e) => previewValues = { ...previewValues, [field.name]: e.target.value }}
                      class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-sm text-white placeholder-slate-600 focus:border-sky-500 focus:outline-none" />
                  {/if}
                </div>
              {/if}
            {/if}
          {/each}
        {:else}
          <p class="text-sm text-slate-500">No visible steps. Add conditions to steps to test step-level visibility.</p>
        {/if}
      </div>

      <!-- Preview nav -->
      <div class="flex items-center justify-between border-t border-slate-800 px-6 py-4">
        <button onclick={() => previewStep = Math.max(previewStep - 1, 0)} disabled={previewStep === 0}
          class="flex items-center gap-2 rounded-2xl border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-300 transition hover:bg-slate-800 disabled:opacity-30">
          ← Back
        </button>
        <span class="text-xs text-slate-500">Step {previewStep + 1} of {previewTotalSteps || 1}</span>
        {#if previewStep < previewTotalSteps - 1}
          <button onclick={() => previewStep = previewStep + 1}
            class="flex items-center gap-2 rounded-2xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white transition hover:bg-sky-400">
            Next →
          </button>
        {:else}
          <button disabled class="rounded-2xl bg-emerald-500/30 px-4 py-2 text-sm font-semibold text-emerald-400 cursor-not-allowed">
            Submit
          </button>
        {/if}
      </div>
    </div>
  </div>
{/if}

<!-- ── Builder ─────────────────────────────────────────────────────────────────── -->
<div class="rounded-3xl border border-slate-800 bg-slate-950 p-6 shadow-xl">

  <!-- Form meta + preview button -->
  <div class="flex flex-wrap items-start gap-4">
    <div class="flex-1 min-w-0 grid gap-4 sm:grid-cols-[1fr_2fr]">
      <div>
        <label for="formTitle" class="block text-sm font-semibold text-white">Form title</label>
        <input id="formTitle" class="mt-2 {ic}" bind:value={currentSchema.title} />
      </div>
      <div>
        <label for="formDesc" class="block text-sm font-semibold text-white">Form description</label>
        <textarea id="formDesc" rows="2" class="mt-2 {ic}" bind:value={currentSchema.description}></textarea>
      </div>
    </div>
    <div class="pt-6 sm:pt-7">
      <button onclick={openPreview}
        class="flex items-center gap-2 rounded-2xl border border-sky-500/40 bg-sky-500/10 px-4 py-2.5 text-sm font-semibold text-sky-400 transition hover:bg-sky-500/20">
        <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
          <path d="M10 12.5a2.5 2.5 0 100-5 2.5 2.5 0 000 5z"/>
          <path fill-rule="evenodd" d="M.664 10.59a1.651 1.651 0 010-1.186A10.004 10.004 0 0110 3c4.257 0 7.893 2.66 9.336 6.41.147.381.146.804 0 1.186A10.004 10.004 0 0110 17c-4.257 0-7.893-2.66-9.336-6.41zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"/>
        </svg>
        Preview
      </button>
    </div>
  </div>

  <!-- Steps -->
  <div class="mt-6 space-y-4" role="list">
    {#each currentSchema.steps as step, si}
      {@const dupes = dupesInStep(si)}
      <div
        role="listitem"
        class="rounded-3xl border bg-slate-900 p-5 transition-all duration-150
          {stepDragIdx === si ? 'opacity-40 scale-[0.98]' : ''}
          {stepDragOverIdx === si && stepDragIdx !== si ? 'border-sky-500 border-dashed bg-sky-500/5' : 'border-slate-800'}"
        draggable="true"
        ondragstart={(e) => stepDragStart(e, si)}
        ondragover={(e) => stepDragOver(e, si)}
        ondrop={(e) => stepDrop(e, si)}
        ondragend={stepDragEnd}
      >
        <!-- Step header -->
        <div class="flex items-center gap-3">
          <div class="cursor-grab active:cursor-grabbing shrink-0 select-none text-slate-500 hover:text-slate-300 transition">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <circle cx="7" cy="4" r="1.5"/><circle cx="13" cy="4" r="1.5"/>
              <circle cx="7" cy="10" r="1.5"/><circle cx="13" cy="10" r="1.5"/>
              <circle cx="7" cy="16" r="1.5"/><circle cx="13" cy="16" r="1.5"/>
            </svg>
          </div>
          <span class="shrink-0 text-xs font-semibold uppercase tracking-widest text-slate-500">Step {si + 1}</span>
          <input class="flex-1 {ic}" bind:value={step.title} placeholder="Step title" />
          <button class="shrink-0 rounded-2xl bg-red-500/10 px-3 py-2 text-xs font-semibold text-red-400 hover:bg-red-500 hover:text-white transition"
            onclick={() => removeStep(si)}>Remove</button>
        </div>

        <!-- Step conditions -->
        <div class="{sec}">
          <div class="{shdr}">
            <span class="{slbl}">
              Step visibility
              {#if (step.conditions ?? []).length > 0}
                <span class="ml-1.5 rounded-full bg-violet-500/20 px-1.5 py-0.5 text-violet-400">{step.conditions.length}</span>
              {/if}
            </span>
            <button class={sadd} onclick={() => addStepCondition(si)}>+ Add condition</button>
          </div>
          {#if !(step.conditions ?? []).length}
            <p class="text-xs italic text-slate-600">Always shown. Add conditions to hide this step based on previous answers.</p>
          {:else}
            <p class="mb-2 text-xs text-slate-500">Show step when <strong class="text-slate-300">all</strong> are true:</p>
            <div class="space-y-2">
              {#each step.conditions as cond, ci}
                <div class="{cRow}">
                  <select class="flex-1 {cSel}" bind:value={cond.fieldName} onchange={mutate}>
                    <option value="">Select field…</option>
                    {#each allFields as af}<option value={af.name}>{af.label || af.name}</option>{/each}
                  </select>
                  <select class="{cSel}" bind:value={cond.operator} onchange={mutate}>
                    {#each OPERATORS as op}<option value={op.value}>{op.label}</option>{/each}
                  </select>
                  <input class="{cInp}" bind:value={cond.value} oninput={mutate} placeholder="value" />
                  <button aria-label="Remove condition" class="{cDel}" onclick={() => removeStepCondition(si, ci)}>
                    <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
                  </button>
                </div>
              {/each}
            </div>
          {/if}
        </div>

        {#if dupes.size > 0}
          <div class="mt-3 rounded-2xl border border-amber-500/30 bg-amber-500/10 px-4 py-2.5 text-xs font-medium text-amber-400">
            Duplicate keys: {[...dupes].join(', ')}
          </div>
        {/if}

        <!-- Fields -->
        <div class="mt-4 space-y-3">
          {#each step.fields as field, fi}
            {@const isDupe   = field.name && dupes.has(field.name)}
            {@const subDupes = field.type === 'repeater' ? subDupesInField(si, fi) : new Set()}
            {@const vTypes   = validatorsFor(field.type)}

            <div
              role="listitem"
              class="rounded-2xl border bg-slate-950 p-4 transition-all
                {fDrag.si === si && fDrag.fi === fi ? 'opacity-40 scale-[0.99]' : ''}
                {fDragO.si === si && fDragO.fi === fi && !(fDrag.si === si && fDrag.fi === fi) ? 'border-sky-500/60 border-dashed' : isDupe ? 'border-amber-500/60' : 'border-slate-800'}"
              draggable="true"
              ondragstart={(e) => fieldDragStart(e, si, fi)}
              ondragover={(e) => fieldDragOver(e, si, fi)}
              ondrop={(e) => fieldDrop(e, si, fi)}
              ondragend={fieldDragEnd}
            >
              <!-- Field header -->
              <div class="mb-3 flex items-center gap-2">
                <div class="cursor-grab active:cursor-grabbing shrink-0 select-none text-slate-600 hover:text-slate-400 transition">
                  <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                    <circle cx="7" cy="5" r="1.2"/><circle cx="13" cy="5" r="1.2"/>
                    <circle cx="7" cy="10" r="1.2"/><circle cx="13" cy="10" r="1.2"/>
                    <circle cx="7" cy="15" r="1.2"/><circle cx="13" cy="15" r="1.2"/>
                  </svg>
                </div>
                <span class="flex-1 text-xs font-semibold uppercase tracking-widest text-slate-500">Field {fi + 1}</span>
                <button class="rounded-2xl bg-red-500/10 px-2 py-1 text-xs font-semibold text-red-400 hover:bg-red-500 hover:text-white transition"
                  onclick={() => removeField(si, fi)}>Remove</button>
              </div>

              <!-- Core config -->
              <div class="grid gap-3 sm:grid-cols-2">
                <!-- Label / content -->
                <div class="{field.type === 'info' ? 'sm:col-span-2' : ''}">
                  <label for="fb-lbl-{si}-{fi}" class="block text-xs font-medium text-slate-400 mb-1">
                    {field.type === 'info' ? 'Content / text' : 'Label'}
                  </label>
                  {#if field.type === 'info'}
                    <textarea id="fb-lbl-{si}-{fi}" class="{ic}" rows="2" value={field.label}
                      oninput={(e) => { field.label = e.target.value; mutate(); }}
                      placeholder="Heading or paragraph text…"></textarea>
                  {:else}
                    <input id="fb-lbl-{si}-{fi}" class={ic} value={field.label}
                      oninput={(e) => handleLabelChange(si, fi, e.target.value)} placeholder="Field label" />
                  {/if}
                </div>

                <!-- Key (hidden for info) -->
                {#if !NO_KEY_TYPES.has(field.type)}
                  <div>
                    <label for="fb-key-{si}-{fi}" class="block text-xs font-medium text-slate-400 mb-1">
                      Key <span class="rounded-full bg-slate-800 px-2 py-0.5 text-[10px] text-slate-400">auto-generated</span>
                    </label>
                    <input id="fb-key-{si}-{fi}" class="w-full rounded-2xl border px-3 py-2 text-sm font-mono cursor-not-allowed
                      {isDupe ? 'border-amber-500/60 bg-amber-500/5 text-amber-300' : 'border-slate-800 bg-slate-800/50 text-slate-400'}"
                      value={field.name || '—'} readonly />
                  </div>
                {/if}

                <!-- Type -->
                <div>
                  <label for="fb-type-{si}-{fi}" class="block text-xs font-medium text-slate-400 mb-1">Type</label>
                  <select id="fb-type-{si}-{fi}" class={ic} bind:value={field.type}>
                    {#each FIELD_TYPES as t}<option value={t.value}>{t.label}</option>{/each}
                  </select>
                </div>

                <!-- Info variant -->
                {#if field.type === 'info'}
                  <div>
                    <label for="fb-var-{si}-{fi}" class="block text-xs font-medium text-slate-400 mb-1">Style</label>
                    <select id="fb-var-{si}-{fi}" class={ic} bind:value={field.infoVariant}>
                      {#each INFO_VARIANTS as v}<option value={v.value}>{v.label}</option>{/each}
                    </select>
                  </div>

                <!-- Required (not shown for info) -->
                {:else}
                  <div class="flex items-center gap-3 pt-5">
                    <input id="req-{si}-{fi}" type="checkbox" class="rounded border-slate-700 bg-slate-900 text-sky-500" bind:checked={field.required} />
                    <label for="req-{si}-{fi}" class="text-sm text-slate-300 select-none">Required</label>
                  </div>
                {/if}
              </div>

              <!-- Options (select / radio) -->
              {#if OPTS_TYPES.has(field.type)}
                <div class="mt-3">
                  <label for="fb-opts-{si}-{fi}" class="block text-xs font-medium text-slate-400 mb-1">Options <span class="text-slate-500">(comma separated)</span></label>
                  <input id="fb-opts-{si}-{fi}" class={ic} bind:value={field.options} placeholder="Option 1, Option 2, Option 3" />
                </div>
              {/if}

              <!-- Address info -->
              {#if field.type === 'address'}
                <div class="mt-3 rounded-xl border border-slate-700 bg-slate-900 px-4 py-3">
                  <p class="text-xs font-medium text-slate-400 mb-2">Auto-generated address sub-fields</p>
                  <div class="flex flex-wrap gap-2">
                    {#each ADDRESS_FIELDS as af}
                      <span class="rounded-full bg-slate-800 px-2.5 py-1 text-xs text-slate-400">{af.label}</span>
                    {/each}
                  </div>
                </div>
              {/if}

              <!-- Repeater sub-fields -->
              {#if field.type === 'repeater'}
                <div class="{sec}">
                  <div class="{shdr}">
                    <span class="{slbl}">Sub-fields</span>
                    <button class={sadd} onclick={() => addSubField(si, fi)}>+ Add sub-field</button>
                  </div>
                  {#if subDupes.size > 0}
                    <div class="mb-3 rounded-xl border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-xs text-amber-400">
                      Duplicate sub-keys: {[...subDupes].join(', ')}
                    </div>
                  {/if}
                  {#if !(field.subFields ?? []).length}
                    <p class="text-xs italic text-slate-500">No sub-fields yet.</p>
                  {:else}
                    <div class="space-y-2">
                      {#each field.subFields as sf, sfi}
                        {@const isSubDupe = sf.name && subDupes.has(sf.name)}
                        <div class="rounded-xl border bg-slate-900 p-3 {isSubDupe ? 'border-amber-500/60' : 'border-slate-800'}">
                          <div class="mb-2 flex justify-between">
                            <span class="text-xs text-slate-500">Sub-field {sfi + 1}</span>
                            <button class="rounded-xl bg-red-500/10 px-2 py-0.5 text-xs font-semibold text-red-400 hover:bg-red-500 hover:text-white transition"
                              onclick={() => removeSubField(si, fi, sfi)}>Remove</button>
                          </div>
                          <div class="grid gap-2 sm:grid-cols-2">
                            <div>
                              <label for="fb-sf-lbl-{si}-{fi}-{sfi}" class="block text-xs font-medium text-slate-400 mb-1">Label</label>
                              <input id="fb-sf-lbl-{si}-{fi}-{sfi}" class={ic2} value={sf.label} oninput={(e) => handleSubLabelChange(si, fi, sfi, e.target.value)} placeholder="Label" />
                            </div>
                            <div>
                              <label for="fb-sf-key-{si}-{fi}-{sfi}" class="block text-xs font-medium text-slate-400 mb-1">Key <span class="rounded-full bg-slate-800 px-1.5 py-0.5 text-[10px] text-slate-400">auto</span></label>
                              <input id="fb-sf-key-{si}-{fi}-{sfi}" class="w-full rounded-xl border px-3 py-1.5 text-xs font-mono cursor-not-allowed
                                {isSubDupe ? 'border-amber-500/60 bg-amber-500/5 text-amber-300' : 'border-slate-700 bg-slate-800/50 text-slate-400'}"
                                value={sf.name || '—'} readonly />
                            </div>
                            <div>
                              <label for="fb-sf-type-{si}-{fi}-{sfi}" class="block text-xs font-medium text-slate-400 mb-1">Type</label>
                              <select id="fb-sf-type-{si}-{fi}-{sfi}" class={ic2} bind:value={sf.type}>
                                {#each FIELD_TYPES.filter(t => t.value !== 'info' && t.value !== 'repeater') as t}
                                  <option value={t.value}>{t.label}</option>
                                {/each}
                              </select>
                            </div>
                            <div class="flex items-center gap-2 pt-4">
                              <input id="sf-req-{si}-{fi}-{sfi}" type="checkbox" class="rounded border-slate-700 bg-slate-900 text-sky-500" bind:checked={sf.required} />
                              <label for="sf-req-{si}-{fi}-{sfi}" class="text-xs text-slate-300 select-none">Required</label>
                            </div>
                          </div>
                          {#if sf.type === 'select'}
                            <div class="mt-2">
                              <label for="fb-sf-opts-{si}-{fi}-{sfi}" class="block text-xs font-medium text-slate-400 mb-1">Options <span class="text-slate-500">(comma)</span></label>
                              <input id="fb-sf-opts-{si}-{fi}-{sfi}" class={ic2} bind:value={sf.options} placeholder="A, B, C" />
                            </div>
                          {/if}
                        </div>
                      {/each}
                    </div>
                  {/if}
                </div>
              {/if}

              <!-- Validators -->
              {#if vTypes.length > 0}
                <div class="{sec}">
                  <div class="{shdr}">
                    <span class="{slbl}">
                      Validation rules
                      {#if (field.validators ?? []).length > 0}
                        <span class="ml-1.5 rounded-full bg-emerald-500/20 px-1.5 py-0.5 text-emerald-400">{field.validators.length}</span>
                      {/if}
                    </span>
                    <button class={sadd} onclick={() => addValidator(si, fi)}>+ Add rule</button>
                  </div>
                  {#if !(field.validators ?? []).length}
                    <p class="text-xs italic text-slate-600">No validation rules.</p>
                  {:else}
                    <div class="space-y-2">
                      {#each field.validators as vld, vi}
                        <div class="grid gap-2 sm:grid-cols-[auto_1fr_1fr_auto] items-center">
                          <select class="{cSel}" bind:value={vld.ruleType} onchange={mutate}>
                            {#each vTypes as vt}<option value={vt.value}>{vt.label}</option>{/each}
                          </select>
                          <input class="{cInp}" bind:value={vld.value} oninput={mutate}
                            placeholder={vld.ruleType === 'regex' ? 'e.g. ^[A-Z]' : 'value'} />
                          <input class="{cInp}" bind:value={vld.message} oninput={mutate}
                            placeholder="Error message" />
                          <button aria-label="Remove rule" class="{cDel}" onclick={() => removeValidator(si, fi, vi)}>
                            <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
                          </button>
                        </div>
                      {/each}
                    </div>
                  {/if}
                </div>
              {/if}

              <!-- Field conditions -->
              {#if field.type !== 'info'}
                <div class="{sec}">
                  <div class="{shdr}">
                    <span class="{slbl}">
                      Visibility conditions
                      {#if (field.conditions ?? []).length > 0}
                        <span class="ml-1.5 rounded-full bg-sky-500/20 px-1.5 py-0.5 text-sky-400">{field.conditions.length}</span>
                      {/if}
                    </span>
                    <button class={sadd} onclick={() => addCondition(si, fi)}>+ Add condition</button>
                  </div>
                  {#if !(field.conditions ?? []).length}
                    <p class="text-xs italic text-slate-600">Always visible.</p>
                  {:else}
                    <p class="mb-2 text-xs text-slate-500">Show when <strong class="text-slate-300">all</strong> are true:</p>
                    <div class="space-y-2">
                      {#each field.conditions as cond, ci}
                        <div class="{cRow}">
                          <select class="flex-1 {cSel}" bind:value={cond.fieldName} onchange={mutate}>
                            <option value="">Select field…</option>
                            {#each allFields.filter(f => f.name !== field.name) as af}
                              <option value={af.name}>{af.label || af.name}</option>
                            {/each}
                          </select>
                          <select class="{cSel}" bind:value={cond.operator} onchange={mutate}>
                            {#each OPERATORS as op}<option value={op.value}>{op.label}</option>{/each}
                          </select>
                          <input class="{cInp}" bind:value={cond.value} oninput={mutate} placeholder="value" />
                          <button aria-label="Remove condition" class="{cDel}" onclick={() => removeCondition(si, fi, ci)}>
                            <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor"><path d="M6.28 5.22a.75.75 0 00-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 101.06 1.06L10 11.06l3.72 3.72a.75.75 0 101.06-1.06L11.06 10l3.72-3.72a.75.75 0 00-1.06-1.06L10 8.94 6.28 5.22z"/></svg>
                          </button>
                        </div>
                      {/each}
                    </div>
                  {/if}
                </div>
              {/if}

            </div>
          {/each}

          <button class="rounded-2xl border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-300 hover:bg-slate-800 transition"
            onclick={() => addField(si)}>+ Add field</button>
        </div>
      </div>
    {/each}

    <button class="rounded-2xl border border-emerald-500/30 bg-emerald-500/10 px-4 py-3 text-sm font-semibold text-emerald-400 hover:bg-emerald-500 hover:text-white transition"
      onclick={addStep}>+ Add step</button>
  </div>

  <!-- Save -->
  <div class="mt-6 flex items-center justify-end gap-4">
    {#if hasErrors}
      <p class="text-sm text-amber-400">Fix duplicate keys before saving.</p>
    {/if}
    <button
      class="rounded-2xl px-6 py-3 text-sm font-semibold text-white shadow-lg transition
        {hasErrors ? 'cursor-not-allowed bg-slate-700 opacity-50' : 'bg-sky-500 shadow-sky-500/20 hover:bg-sky-400'}"
      onclick={save} disabled={hasErrors}>Save form</button>
  </div>
</div>
