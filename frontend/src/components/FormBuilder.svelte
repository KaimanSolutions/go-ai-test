<script>
  import { untrack } from 'svelte';
  let { schema, onsave } = $props();

  let currentSchema = $state(
    untrack(() => schema
      ? JSON.parse(JSON.stringify(schema))
      : { id: '', title: '', description: '', steps: [] })
  );

  let dragIndex = $state(-1);
  let dragOverIndex = $state(-1);

  $effect(() => {
    if (schema) currentSchema = JSON.parse(JSON.stringify(schema));
  });

  function toCamelCase(label) {
    return label
      .trim()
      .replace(/[^a-zA-Z0-9\s]/g, '')
      .split(/\s+/)
      .filter(Boolean)
      .map((word, i) =>
        i === 0
          ? word.charAt(0).toLowerCase() + word.slice(1).toLowerCase()
          : word.charAt(0).toUpperCase() + word.slice(1).toLowerCase()
      )
      .join('');
  }

  function duplicateKeysInStep(stepIndex) {
    const names = currentSchema.steps[stepIndex].fields.map(f => f.name).filter(Boolean);
    const seen = new Set();
    const dupes = new Set();
    for (const name of names) {
      if (seen.has(name)) dupes.add(name);
      seen.add(name);
    }
    return dupes;
  }

  function duplicateSubKeysInField(stepIndex, fieldIndex) {
    const subFields = currentSchema.steps[stepIndex].fields[fieldIndex].subFields ?? [];
    const names = subFields.map(sf => sf.name).filter(Boolean);
    const seen = new Set();
    const dupes = new Set();
    for (const name of names) {
      if (seen.has(name)) dupes.add(name);
      seen.add(name);
    }
    return dupes;
  }

  let hasErrors = $derived(
    currentSchema.steps.some((_, i) => duplicateKeysInStep(i).size > 0) ||
    currentSchema.steps.some((step, i) =>
      step.fields.some((field, j) =>
        field.type === 'repeater' && duplicateSubKeysInField(i, j).size > 0
      )
    )
  );

  function handleLabelChange(stepIndex, fieldIndex, value) {
    const field = currentSchema.steps[stepIndex].fields[fieldIndex];
    field.label = value;
    field.name = toCamelCase(value);
    currentSchema = { ...currentSchema };
  }

  function handleSubLabelChange(stepIndex, fieldIndex, subIndex, value) {
    const subField = currentSchema.steps[stepIndex].fields[fieldIndex].subFields[subIndex];
    subField.label = value;
    subField.name = toCamelCase(value);
    currentSchema = { ...currentSchema };
  }

  function addStep() {
    currentSchema.steps.push({ title: 'New Step', fields: [] });
    currentSchema = { ...currentSchema };
  }

  function removeStep(index) {
    currentSchema.steps.splice(index, 1);
    currentSchema = { ...currentSchema };
  }

  function addField(stepIndex) {
    currentSchema.steps[stepIndex].fields.push({ name: '', label: '', type: 'text', required: false, options: [], subFields: [] });
    currentSchema = { ...currentSchema };
  }

  function removeField(stepIndex, fieldIndex) {
    currentSchema.steps[stepIndex].fields.splice(fieldIndex, 1);
    currentSchema = { ...currentSchema };
  }

  function addSubField(stepIndex, fieldIndex) {
    const field = currentSchema.steps[stepIndex].fields[fieldIndex];
    if (!field.subFields) field.subFields = [];
    field.subFields.push({ name: '', label: '', type: 'text', required: false, options: [] });
    currentSchema = { ...currentSchema };
  }

  function removeSubField(stepIndex, fieldIndex, subIndex) {
    currentSchema.steps[stepIndex].fields[fieldIndex].subFields.splice(subIndex, 1);
    currentSchema = { ...currentSchema };
  }

  function onDragStart(e, index) {
    dragIndex = index;
    e.dataTransfer.effectAllowed = 'move';
  }

  function onDragOver(e, index) {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    dragOverIndex = index;
  }

  function onDrop(e, index) {
    e.preventDefault();
    if (dragIndex !== -1 && dragIndex !== index) {
      const steps = [...currentSchema.steps];
      const [moved] = steps.splice(dragIndex, 1);
      steps.splice(index, 0, moved);
      currentSchema = { ...currentSchema, steps };
    }
    dragIndex = -1;
    dragOverIndex = -1;
  }

  function onDragEnd() {
    dragIndex = -1;
    dragOverIndex = -1;
  }

  function normalizeSubField(sf) {
    const clean = { ...sf };
    if (clean.type === 'select') {
      if (typeof clean.options === 'string') {
        clean.options = clean.options.split(',').map(o => o.trim()).filter(Boolean);
      } else if (!Array.isArray(clean.options)) {
        clean.options = [];
      }
    } else {
      clean.options = [];
    }
    return clean;
  }

  function normalizeSchema(s) {
    return {
      ...s,
      steps: s.steps.map(step => ({
        ...step,
        fields: step.fields.map(field => {
          const clean = { ...field };
          if (clean.type === 'select') {
            if (typeof clean.options === 'string') {
              clean.options = clean.options.split(',').map(o => o.trim()).filter(Boolean);
            } else if (!Array.isArray(clean.options)) {
              clean.options = [];
            }
            clean.subFields = [];
          } else if (clean.type === 'repeater') {
            clean.options = [];
            clean.subFields = (clean.subFields ?? []).map(normalizeSubField);
          } else {
            clean.options = [];
            clean.subFields = [];
          }
          return clean;
        })
      }))
    };
  }

  function save() {
    if (hasErrors) return;
    onsave(normalizeSchema(currentSchema));
  }
</script>

<div class="rounded-3xl border border-slate-800 bg-slate-950 p-6 shadow-xl">
  <div class="grid gap-4 sm:grid-cols-[1fr_2fr] sm:items-end">
    <div>
      <label for="formTitle" class="block text-sm font-semibold text-white">Form Title</label>
      <input
        id="formTitle"
        class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
        bind:value={currentSchema.title}
      />
    </div>
    <div>
      <label for="formDescription" class="block text-sm font-semibold text-white">Form Description</label>
      <textarea
        id="formDescription"
        rows="3"
        class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
        bind:value={currentSchema.description}
      ></textarea>
    </div>
  </div>

  <div class="mt-6 space-y-4" role="list">
    {#each currentSchema.steps as step, stepIndex}
      {@const dupes = duplicateKeysInStep(stepIndex)}
      <div
        role="listitem"
        class="rounded-3xl border bg-slate-900 p-5 transition-all duration-150
          {dragIndex === stepIndex ? 'opacity-40 scale-[0.98]' : ''}
          {dragOverIndex === stepIndex && dragIndex !== stepIndex ? 'border-sky-500 border-dashed bg-sky-500/5' : 'border-slate-800'}"
        draggable="true"
        ondragstart={(e) => {
          const t = e.target;
          if (t instanceof HTMLInputElement || t instanceof HTMLSelectElement || t instanceof HTMLTextAreaElement || t instanceof HTMLButtonElement) {
            e.preventDefault();
            return;
          }
          onDragStart(e, stepIndex);
        }}
        ondragover={(e) => onDragOver(e, stepIndex)}
        ondrop={(e) => onDrop(e, stepIndex)}
        ondragend={onDragEnd}
      >
        <div class="flex items-center gap-3">
          <div class="cursor-grab active:cursor-grabbing select-none text-slate-500 hover:text-slate-300 transition-colors shrink-0" title="Drag to reorder">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <circle cx="7" cy="4" r="1.5"/><circle cx="13" cy="4" r="1.5"/>
              <circle cx="7" cy="10" r="1.5"/><circle cx="13" cy="10" r="1.5"/>
              <circle cx="7" cy="16" r="1.5"/><circle cx="13" cy="16" r="1.5"/>
            </svg>
          </div>
          <span class="text-xs font-semibold uppercase tracking-widest text-slate-500 shrink-0">Step {stepIndex + 1}</span>
          <input
            class="flex-1 rounded-2xl border border-slate-800 bg-slate-950 px-4 py-2.5 text-white text-sm focus:border-sky-500 focus:outline-none"
            bind:value={step.title}
            placeholder="Step title"
          />
          <button
            class="shrink-0 rounded-2xl bg-red-500/10 px-3 py-2 text-xs font-semibold text-red-400 hover:bg-red-500 hover:text-white transition"
            onclick={() => removeStep(stepIndex)}
          >Remove</button>
        </div>

        {#if dupes.size > 0}
          <div class="mt-3 rounded-2xl bg-amber-500/10 border border-amber-500/30 px-4 py-2.5 text-xs font-medium text-amber-400">
            Duplicate keys in this step: {[...dupes].join(', ')} — each field key must be unique within a step.
          </div>
        {/if}

        <div class="mt-4 space-y-3">
          {#each step.fields as field, fieldIndex}
            {@const isDupe = field.name && dupes.has(field.name)}
            {@const subDupes = field.type === 'repeater' ? duplicateSubKeysInField(stepIndex, fieldIndex) : new Set()}
            <div class="rounded-2xl border bg-slate-950 p-4 transition {isDupe ? 'border-amber-500/60' : 'border-slate-800'}">
              <div class="flex items-center justify-between mb-3">
                <span class="text-xs font-semibold uppercase tracking-widest text-slate-500">Field {fieldIndex + 1}</span>
                <button
                  class="rounded-2xl bg-red-500/10 px-2 py-1 text-xs font-semibold text-red-400 hover:bg-red-500 hover:text-white transition"
                  onclick={() => removeField(stepIndex, fieldIndex)}
                >Remove</button>
              </div>

              <div class="grid gap-3 sm:grid-cols-2">
                <div>
                  <label for="label-{stepIndex}-{fieldIndex}" class="block text-xs font-medium text-slate-400 mb-1">Label</label>
                  <input
                    id="label-{stepIndex}-{fieldIndex}"
                    class="w-full rounded-2xl border border-slate-800 bg-slate-900 px-3 py-2 text-white text-sm focus:border-sky-500 focus:outline-none"
                    value={field.label}
                    oninput={(e) => handleLabelChange(stepIndex, fieldIndex, e.target.value)}
                    placeholder="Field Label"
                  />
                </div>

                <div>
                  <label for="key-{stepIndex}-{fieldIndex}" class="block text-xs font-medium text-slate-400 mb-1 flex items-center gap-1.5">
                    Key
                    <span class="rounded-full bg-slate-800 px-2 py-0.5 text-[10px] text-slate-400">auto-generated</span>
                  </label>
                  <input
                    id="key-{stepIndex}-{fieldIndex}"
                    class="w-full rounded-2xl border px-3 py-2 text-sm font-mono cursor-not-allowed
                      {isDupe
                        ? 'border-amber-500/60 bg-amber-500/5 text-amber-300'
                        : 'border-slate-800 bg-slate-800/50 text-slate-400'}"
                    value={field.name || '—'}
                    readonly
                  />
                </div>

                <div>
                  <label for="type-{stepIndex}-{fieldIndex}" class="block text-xs font-medium text-slate-400 mb-1">Type</label>
                  <select
                    id="type-{stepIndex}-{fieldIndex}"
                    class="w-full rounded-2xl border border-slate-800 bg-slate-900 px-3 py-2 text-white text-sm focus:border-sky-500 focus:outline-none"
                    bind:value={field.type}
                  >
                    <option value="text">Text</option>
                    <option value="email">Email</option>
                    <option value="number">Number</option>
                    <option value="select">Select</option>
                    <option value="checkbox">Checkbox</option>
                    <option value="date">Date</option>
                    <option value="repeater">Repeater</option>
                  </select>
                </div>

                <div class="flex items-center gap-3 pt-5">
                  <input
                    id="req-{stepIndex}-{fieldIndex}"
                    type="checkbox"
                    class="rounded border-slate-700 bg-slate-900 text-sky-500"
                    bind:checked={field.required}
                  />
                  <label for="req-{stepIndex}-{fieldIndex}" class="text-sm text-slate-300 select-none">Required</label>
                </div>
              </div>

              {#if field.type === 'select'}
                <div class="mt-3">
                  <label for="opts-{stepIndex}-{fieldIndex}" class="block text-xs font-medium text-slate-400 mb-1">Options <span class="text-slate-500">(comma separated)</span></label>
                  <input
                    id="opts-{stepIndex}-{fieldIndex}"
                    class="w-full rounded-2xl border border-slate-800 bg-slate-900 px-3 py-2 text-white text-sm focus:border-sky-500 focus:outline-none"
                    bind:value={field.options}
                    placeholder="Option 1, Option 2, Option 3"
                  />
                </div>
              {/if}

              {#if field.type === 'repeater'}
                <div class="mt-4 border-t border-slate-800 pt-4">
                  <div class="mb-3 flex items-center justify-between">
                    <span class="text-xs font-semibold uppercase tracking-widest text-slate-400">Sub-fields</span>
                    <button
                      class="rounded-xl bg-slate-800 px-3 py-1 text-xs font-semibold text-slate-300 hover:bg-slate-700 transition"
                      onclick={() => addSubField(stepIndex, fieldIndex)}
                    >+ Add sub-field</button>
                  </div>

                  {#if subDupes.size > 0}
                    <div class="mb-3 rounded-xl bg-amber-500/10 border border-amber-500/30 px-3 py-2 text-xs text-amber-400">
                      Duplicate sub-keys: {[...subDupes].join(', ')} — sub-field keys must be unique.
                    </div>
                  {/if}

                  {#if (field.subFields ?? []).length === 0}
                    <p class="text-xs text-slate-500 italic">No sub-fields yet. Add one to define what each repeated item contains.</p>
                  {:else}
                    <div class="space-y-2">
                      {#each field.subFields as subField, subIndex}
                        {@const isSubDupe = subField.name && subDupes.has(subField.name)}
                        <div class="rounded-xl border bg-slate-900 p-3 {isSubDupe ? 'border-amber-500/60' : 'border-slate-800'}">
                          <div class="flex items-center justify-between mb-2">
                            <span class="text-xs text-slate-500">Sub-field {subIndex + 1}</span>
                            <button
                              class="rounded-xl bg-red-500/10 px-2 py-0.5 text-xs font-semibold text-red-400 hover:bg-red-500 hover:text-white transition"
                              onclick={() => removeSubField(stepIndex, fieldIndex, subIndex)}
                            >Remove</button>
                          </div>
                          <div class="grid gap-2 sm:grid-cols-2">
                            <div>
                              <label for="sf-label-{stepIndex}-{fieldIndex}-{subIndex}" class="block text-xs font-medium text-slate-400 mb-1">Label</label>
                              <input
                                id="sf-label-{stepIndex}-{fieldIndex}-{subIndex}"
                                class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-1.5 text-white text-xs focus:border-sky-500 focus:outline-none"
                                value={subField.label}
                                oninput={(e) => handleSubLabelChange(stepIndex, fieldIndex, subIndex, e.target.value)}
                                placeholder="Sub-field label"
                              />
                            </div>
                            <div>
                              <label for="sf-key-{stepIndex}-{fieldIndex}-{subIndex}" class="block text-xs font-medium text-slate-400 mb-1">
                                Key <span class="rounded-full bg-slate-800 px-1.5 py-0.5 text-[10px] text-slate-400">auto</span>
                              </label>
                              <input
                                id="sf-key-{stepIndex}-{fieldIndex}-{subIndex}"
                                class="w-full rounded-xl border px-3 py-1.5 text-xs font-mono cursor-not-allowed
                                  {isSubDupe ? 'border-amber-500/60 bg-amber-500/5 text-amber-300' : 'border-slate-700 bg-slate-800/50 text-slate-400'}"
                                value={subField.name || '—'}
                                readonly
                              />
                            </div>
                            <div>
                              <label for="sf-type-{stepIndex}-{fieldIndex}-{subIndex}" class="block text-xs font-medium text-slate-400 mb-1">Type</label>
                              <select
                                id="sf-type-{stepIndex}-{fieldIndex}-{subIndex}"
                                class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-1.5 text-white text-xs focus:border-sky-500 focus:outline-none"
                                bind:value={subField.type}
                              >
                                <option value="text">Text</option>
                                <option value="email">Email</option>
                                <option value="number">Number</option>
                                <option value="select">Select</option>
                                <option value="checkbox">Checkbox</option>
                                <option value="date">Date</option>
                              </select>
                            </div>
                            <div class="flex items-center gap-2 pt-4">
                              <input
                                id="sf-req-{stepIndex}-{fieldIndex}-{subIndex}"
                                type="checkbox"
                                class="rounded border-slate-700 bg-slate-900 text-sky-500"
                                bind:checked={subField.required}
                              />
                              <label for="sf-req-{stepIndex}-{fieldIndex}-{subIndex}" class="text-xs text-slate-300 select-none">Required</label>
                            </div>
                          </div>
                          {#if subField.type === 'select'}
                            <div class="mt-2">
                              <label for="sf-opts-{stepIndex}-{fieldIndex}-{subIndex}" class="block text-xs font-medium text-slate-400 mb-1">Options <span class="text-slate-500">(comma separated)</span></label>
                              <input
                                id="sf-opts-{stepIndex}-{fieldIndex}-{subIndex}"
                                class="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-1.5 text-white text-xs focus:border-sky-500 focus:outline-none"
                                bind:value={subField.options}
                                placeholder="Option 1, Option 2, Option 3"
                              />
                            </div>
                          {/if}
                        </div>
                      {/each}
                    </div>
                  {/if}
                </div>
              {/if}
            </div>
          {/each}

          <button
            class="rounded-2xl border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-300 hover:bg-slate-800 transition"
            onclick={() => addField(stepIndex)}
          >+ Add field</button>
        </div>
      </div>
    {/each}

    <button
      class="rounded-2xl bg-green-500/10 border border-green-500/30 px-4 py-3 text-sm font-semibold text-green-400 hover:bg-green-500 hover:text-white transition"
      onclick={addStep}
    >+ Add step</button>
  </div>

  <div class="mt-6 flex items-center justify-end gap-4">
    {#if hasErrors}
      <p class="text-sm text-amber-400">Fix duplicate keys before saving.</p>
    {/if}
    <button
      class="rounded-2xl px-6 py-3 text-sm font-semibold text-white shadow-lg transition
        {hasErrors ? 'bg-slate-700 cursor-not-allowed opacity-50' : 'bg-sky-500 shadow-sky-500/20 hover:bg-sky-400'}"
      onclick={save}
      disabled={hasErrors}
    >Save form</button>
  </div>
</div>
