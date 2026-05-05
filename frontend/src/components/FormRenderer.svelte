<script>
  import { isFieldVisible } from '../lib/formBuilder.js';

  let { schema, validationResult = null, onsubmit = null } = $props();

  let currentStep = $state(0);
  let values = $state({});
  let stepErrors = $state({});
  let maxReachedStep = $state(0);

  $effect(() => {
    if (schema) {
      const v = {};
      schema.steps.forEach(step => {
        step.fields.forEach(field => {
          if (field.type === 'repeater') {
            const emptyItem = {};
            (field.subFields ?? []).forEach(sf => { emptyItem[sf.name] = sf.type === 'checkbox' ? false : ''; });
            v[field.name] = [emptyItem];
          } else {
            v[field.name] = field.defaultValue ?? (field.type === 'checkbox' ? false : '');
          }
        });
      });
      values = v;
      currentStep = 0;
      maxReachedStep = 0;
      stepErrors = {};
    }
  });

  function validateCurrentStep() {
    const errors = {};
    const step = schema.steps[currentStep];
    for (const field of step.fields) {
      if (!isFieldVisible(field, values)) continue;
      if (field.type === 'repeater') {
        const items = values[field.name] ?? [];
        items.forEach((item, i) => {
          (field.subFields ?? []).forEach(sf => {
            if (sf.required && sf.type !== 'checkbox' && !item[sf.name]) {
              errors[`${field.name}_${i}_${sf.name}`] = `${sf.label} is required`;
            }
          });
        });
      } else if (field.required && field.type !== 'checkbox') {
        const val = values[field.name];
        if (val === '' || val === null || val === undefined) {
          errors[field.name] = `${field.label} is required`;
        }
      }
    }
    stepErrors = errors;
    return Object.keys(errors).length === 0;
  }

  function goToStep(index) {
    if (index > maxReachedStep) return;
    stepErrors = {};
    currentStep = index;
  }

  function nextStep() {
    if (!validateCurrentStep()) return;
    const next = currentStep + 1;
    if (next > maxReachedStep) maxReachedStep = next;
    currentStep = next;
    stepErrors = {};
  }

  function prevStep() {
    stepErrors = {};
    if (currentStep > 0) currentStep--;
  }

  function submit() {
    if (!validateCurrentStep()) return;
    onsubmit?.(values);
  }

  function addRepeaterItem(fieldName, subFields) {
    const newItem = {};
    (subFields ?? []).forEach(sf => { newItem[sf.name] = sf.type === 'checkbox' ? false : ''; });
    values[fieldName] = [...(values[fieldName] ?? []), newItem];
  }

  function removeRepeaterItem(fieldName, index) {
    const items = [...(values[fieldName] ?? [])];
    items.splice(index, 1);
    values[fieldName] = items;
  }

  let hasStepErrors = $derived(Object.keys(stepErrors).length > 0);
</script>

{#if schema}
  <div class="space-y-0">

    <!-- Tab bar -->
    <div class="flex overflow-x-auto border-b border-slate-800">
      {#each schema.steps as step, i}
        {@const isActive = i === currentStep}
        {@const isCompleted = i < currentStep}
        {@const isAccessible = i <= maxReachedStep}
        <button
          onclick={() => goToStep(i)}
          disabled={!isAccessible}
          class="flex shrink-0 items-center gap-2.5 border-b-2 px-5 py-4 text-sm font-medium whitespace-nowrap transition
            {isActive
              ? 'border-sky-500 text-sky-400'
              : isCompleted
                ? 'border-transparent text-slate-300 hover:border-slate-600 hover:text-white'
                : isAccessible
                  ? 'border-transparent text-slate-400 hover:border-slate-600 hover:text-white'
                  : 'border-transparent text-slate-600 cursor-not-allowed'}"
        >
          <span class="flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-xs font-bold
            {isCompleted
              ? 'bg-green-500/20 text-green-400'
              : isActive
                ? 'bg-sky-500/20 text-sky-400'
                : 'bg-slate-800 text-slate-500'}">
            {#if isCompleted}
              <svg class="h-3 w-3" viewBox="0 0 12 12" fill="currentColor">
                <path d="M10.28 2.28L4 8.56 1.72 6.28a.75.75 0 00-1.06 1.06l2.83 2.83a.75.75 0 001.06 0l6.83-6.83a.75.75 0 00-1.06-1.06z"/>
              </svg>
            {:else}
              {i + 1}
            {/if}
          </span>
          {step.title}
        </button>
      {/each}
    </div>

    <!-- Step content -->
    <div class="space-y-4 pt-6">
      {#each schema.steps[currentStep].fields as field}
        {#if isFieldVisible(field, values)}
          {@const fieldErr = stepErrors[field.name]}

          <div class="rounded-3xl border bg-slate-950 p-5 shadow-sm transition
            {fieldErr ? 'border-red-500/40' : 'border-slate-800'}">

            {#if field.type === 'checkbox'}
              <div class="flex items-center gap-3">
                <input
                  id="field-{field.name}"
                  type="checkbox"
                  class="h-5 w-5 rounded border-slate-700 bg-slate-900 text-sky-600 focus:ring-sky-500"
                  bind:checked={values[field.name]}
                />
                <label for="field-{field.name}" class="text-sm font-medium text-white">{field.label}</label>
              </div>

            {:else if field.type === 'repeater'}
              <p class="text-sm font-medium text-white mb-3">{field.label}{field.required ? ' *' : ''}</p>
              <div class="space-y-3">
                {#each values[field.name] ?? [] as item, itemIndex}
                  <div class="rounded-2xl border border-slate-700 bg-slate-900/60 p-4">
                    <div class="flex items-center justify-between mb-3">
                      <span class="text-xs font-semibold uppercase tracking-widest text-slate-400">{field.label} {itemIndex + 1}</span>
                      {#if (values[field.name] ?? []).length > 1}
                        <button
                          onclick={() => removeRepeaterItem(field.name, itemIndex)}
                          class="rounded-xl bg-red-500/10 px-2 py-1 text-xs font-semibold text-red-400 hover:bg-red-500 hover:text-white transition"
                        >Remove</button>
                      {/if}
                    </div>
                    <div class="grid gap-3 sm:grid-cols-2">
                      {#each field.subFields ?? [] as subField}
                        {@const sfErr = stepErrors[`${field.name}_${itemIndex}_${subField.name}`]}
                        <div class="{subField.type === 'checkbox' ? 'flex items-center gap-2 pt-4' : ''}">
                          {#if subField.type === 'checkbox'}
                            <input
                              id="rf-{field.name}-{itemIndex}-{subField.name}"
                              type="checkbox"
                              class="h-4 w-4 rounded border-slate-700 bg-slate-900 text-sky-500"
                              bind:checked={item[subField.name]}
                            />
                            <label for="rf-{field.name}-{itemIndex}-{subField.name}" class="text-sm text-slate-300 select-none">{subField.label}</label>
                          {:else}
                            <label for="rf-{field.name}-{itemIndex}-{subField.name}" class="block text-xs font-medium text-slate-400 mb-1">
                              {subField.label}{subField.required ? ' *' : ''}
                            </label>
                            {#if subField.type === 'select'}
                              <select
                                id="rf-{field.name}-{itemIndex}-{subField.name}"
                                class="w-full rounded-xl border px-3 py-2 text-white text-sm bg-slate-950 focus:outline-none transition
                                  {sfErr ? 'border-red-500/60' : 'border-slate-800 focus:border-sky-500'}"
                                bind:value={item[subField.name]}
                              >
                                <option value="">Select…</option>
                                {#each subField.options ?? [] as opt}
                                  <option value={opt}>{opt}</option>
                                {/each}
                              </select>
                            {:else}
                              <input
                                id="rf-{field.name}-{itemIndex}-{subField.name}"
                                type={subField.type}
                                class="w-full rounded-xl border px-3 py-2 text-white text-sm bg-slate-950 focus:outline-none transition
                                  {sfErr ? 'border-red-500/60' : 'border-slate-800 focus:border-sky-500'}"
                                bind:value={item[subField.name]}
                              />
                            {/if}
                            {#if sfErr}
                              <p class="mt-1 text-xs text-red-400">{sfErr}</p>
                            {/if}
                          {/if}
                        </div>
                      {/each}
                    </div>
                  </div>
                {/each}
                <button
                  onclick={() => addRepeaterItem(field.name, field.subFields)}
                  class="rounded-2xl border border-slate-700 px-4 py-2 text-sm font-semibold text-slate-300 hover:bg-slate-800 transition"
                >+ Add {field.label}</button>
              </div>

            {:else}
              <label for="field-{field.name}" class="block text-sm font-medium text-white mb-2">
                {field.label}{field.required ? ' *' : ''}
              </label>
              {#if field.type === 'select'}
                <select
                  id="field-{field.name}"
                  class="w-full rounded-2xl border px-4 py-3 text-white bg-slate-900 shadow-sm focus:outline-none focus:ring-2 focus:ring-sky-200 transition
                    {fieldErr ? 'border-red-500/60' : 'border-slate-800 focus:border-sky-500'}"
                  bind:value={values[field.name]}
                >
                  <option value="">Select…</option>
                  {#each field.options as option}
                    <option value={option}>{option}</option>
                  {/each}
                </select>
              {:else}
                <input
                  id="field-{field.name}"
                  type={field.type}
                  class="w-full rounded-2xl border px-4 py-3 text-white bg-slate-900 shadow-sm focus:outline-none focus:ring-2 focus:ring-sky-200 transition
                    {fieldErr ? 'border-red-500/60' : 'border-slate-800 focus:border-sky-500'}"
                  bind:value={values[field.name]}
                />
              {/if}
              {#if fieldErr}
                <p class="mt-1.5 text-xs text-red-400">{fieldErr}</p>
              {/if}
            {/if}

          </div>
        {/if}
      {/each}

      <!-- Validation errors from server -->
      {#if validationResult && !validationResult.isValid}
        <div class="rounded-3xl border border-red-500/30 bg-red-500/10 p-5">
          <h4 class="text-sm font-semibold text-red-400">Please fix the following errors</h4>
          <ul class="mt-2 space-y-1 list-disc list-inside text-sm text-red-300">
            {#each Object.entries(validationResult.errors) as [, messages]}
              {#each messages as message}
                <li>{message}</li>
              {/each}
            {/each}
          </ul>
        </div>
      {/if}

      <!-- Step error summary -->
      {#if hasStepErrors}
        <p class="text-sm text-red-400 text-right">Please fill in all required fields before continuing.</p>
      {/if}

      <!-- Navigation -->
      <div class="flex items-center justify-between pt-2">
        {#if currentStep > 0}
          <button
            onclick={prevStep}
            class="rounded-2xl border border-slate-700 px-5 py-2.5 text-sm font-semibold text-slate-300 hover:bg-slate-800 transition"
          >Previous</button>
        {:else}
          <span></span>
        {/if}

        {#if currentStep < schema.steps.length - 1}
          <button
            onclick={nextStep}
            class="rounded-2xl bg-sky-500 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 hover:bg-sky-400 transition"
          >Next step</button>
        {:else}
          <button
            onclick={submit}
            class="rounded-2xl bg-green-500 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-green-500/20 hover:bg-green-400 transition"
          >Submit</button>
        {/if}
      </div>
    </div>

  </div>
{/if}
